using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCombat : MonoBehaviour
{
    private Character character;
    private CharacterStats stats;
    private CharacterVisual visual;
    private CharacterMovement movement;
    private CharacterJump jumpSystem;
    
    // 총알 발사
    [Header("Bullet Settings")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 5f;
    
    // 방향에 따른 총알 스프라이트
    [Header("방향별 총알 스프라이트")]
    public Sprite bulletUpDirectionSprite;
    public Sprite bulletDownDirectionSprite;
    
    // 탄환 들어갈 패널
    private RectTransform bulletPanel;
    
    // 지역2 전용 탄환 패널
    [Tooltip("지역2 (areaIndex=2) 캐릭터 총알이 들어갈 Opponent BulletPanel")]
    public RectTransform opponentBulletPanel;
    
    // 클릭 시 변경될 새로운 공격 범위
    [Header("클릭 시 변경될 새로운 공격 범위(예: 4.0f)")]
    public float newAttackRangeOnClick = 4f;
    
    // 공격 정보 표시용 UI
    [Header("공격 정보 표시용 UI")]
    [Tooltip("공격 범위/형태를 보여줄 TextMeshProUGUI (씬에서 연결)")]
    public TMPro.TextMeshProUGUI attackInfoText;
    [Tooltip("공격 정보 텍스트 위치 오프셋(월드 좌표)")]
    public Vector3 attackInfoOffset = new Vector3(1f, 0f, 0f);
    
    public void Initialize(Character character, CharacterStats stats, CharacterVisual visual, CharacterMovement movement, CharacterJump jumpSystem)
    {
        this.character = character;
        this.stats = stats;
        this.visual = visual;
        this.movement = movement;
        this.jumpSystem = jumpSystem;
        
        InitializeBulletPanel();
        StartCoroutine(AttackRoutine());
    }
    
    private void InitializeBulletPanel()
    {
        if (bulletPanel == null)
        {
            GameObject bulletPanelObj = GameObject.Find("BulletPanel");
            if (bulletPanelObj != null)
            {
                bulletPanel = bulletPanelObj.GetComponent<RectTransform>();
                Debug.Log($"[Character] {character.characterName}의 bulletPanel을 자동으로 찾음: {bulletPanelObj.name}");
            }
            
            if (bulletPanel == null)
            {
                Canvas mainCanvas = FindFirstObjectByType<Canvas>();
                if (mainCanvas != null)
                {
                    foreach (RectTransform rt in mainCanvas.GetComponentsInChildren<RectTransform>())
                    {
                        if (rt.name.Contains("Bullet") || rt.name.Contains("bullet") || rt.name.Contains("Panel"))
                        {
                            bulletPanel = rt;
                            Debug.Log($"[Character] {character.characterName}의 bulletPanel을 캔버스에서 찾음: {rt.name}");
                            break;
                        }
                    }
                    
                    if (bulletPanel == null)
                    {
                        GameObject newPanel = new GameObject("BulletPanel");
                        newPanel.transform.SetParent(mainCanvas.transform, false);
                        bulletPanel = newPanel.AddComponent<RectTransform>();
                        bulletPanel.anchorMin = Vector2.zero;
                        bulletPanel.anchorMax = Vector2.one;
                        bulletPanel.offsetMin = Vector2.zero;
                        bulletPanel.offsetMax = Vector2.zero;
                        Debug.Log($"[Character] {character.characterName}을 위한 새 bulletPanel 생성");
                    }
                }
            }
        }
    }
    
    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(stats.GetAttackCooldown());
            
            character.currentCharTarget = null;
            character.currentTarget = null;
            IDamageable targetToDamage = null;
            
            // walkable 타일 위의 캐릭터는 사정거리 내 적만 공격, 추적하지 않음
            bool isOnWalkable = (character.currentTile == null || 
                               character.currentTile.IsWalkable() || character.currentTile.IsWalkableLeft() || 
                               character.currentTile.IsWalkableCenter() || character.currentTile.IsWalkableRight() ||
                               character.currentTile.IsWalkable2() || character.currentTile.IsWalkable2Left() || 
                               character.currentTile.IsWalkable2Center() || character.currentTile.IsWalkable2Right());
            
            switch (character.attackTargetType)
            {
                case AttackTargetType.All:
                    character.currentCharTarget = FindCharacterTargetInRange();
                    if (character.currentCharTarget != null)
                    {
                        targetToDamage = character.currentCharTarget;
                        Debug.Log($"[Character] {character.characterName}이(가) 캐릭터 {character.currentCharTarget.characterName}을(를) 공격!");
                    }
                    else
                    {
                        character.currentTarget = character.isHero ? FindHeroTargetInRange() : FindMonsterTargetInRange();
                        if (character.currentTarget != null)
                        {
                            targetToDamage = character.currentTarget;
                            Debug.Log($"[Character] {character.characterName}이(가) 몬스터 {character.currentTarget.name}을(를) 공격!");
                        }
                    }
                    break;
                    
                case AttackTargetType.CharacterOnly:
                    character.currentCharTarget = FindCharacterTargetInRange();
                    if (character.currentCharTarget != null)
                    {
                        targetToDamage = character.currentCharTarget;
                        Debug.Log($"[Character] {character.characterName}이(가) 캐릭터 {character.currentCharTarget.characterName}을(를) 공격!");
                    }
                    break;
                    
                case AttackTargetType.CastleOnly:
                    // 성만 공격 - 이동만 하고 전투하지 않음
                    break;
            }
            
            if (targetToDamage != null)
            {
                if (character.isHero)
                {
                    float originalDamage = character.attackPower;
                    character.attackPower *= 1.5f;
                    
                    AttackTarget(targetToDamage);
                    
                    character.attackPower = originalDamage;
                }
                else
                {
                    AttackTarget(targetToDamage);
                }
            }
        }
    }
    
    private Monster FindMonsterTargetInRange()
    {
        string targetTag = (character.areaIndex == 1) ? "EnemyMonster" : "Monster";
        
        GameObject[] foundObjs = GameObject.FindGameObjectsWithTag(targetTag);
        
        Monster nearest = null;
        float nearestDist = Mathf.Infinity;
        
        foreach (GameObject mo in foundObjs)
        {
            if (mo == null) continue;
            Monster m = mo.GetComponent<Monster>();
            if (m == null) continue;
            
            float dist = Vector3.Distance(transform.position, m.transform.position);
            if (dist <= character.attackRange && dist < nearestDist)
            {
                nearest = m;
                nearestDist = dist;
            }
        }
        
        return nearest;
    }
    
    private Monster FindHeroTargetInRange()
    {
        float heroAttackRange = character.attackRange * 1.5f;
        
        string targetTag = (character.areaIndex == 1) ? "Monster" : "EnemyMonster";
        GameObject[] foundObjs = GameObject.FindGameObjectsWithTag(targetTag);
        
        Monster nearest = null;
        float nearestDist = Mathf.Infinity;
        
        foreach (GameObject mo in foundObjs)
        {
            if (mo == null) continue;
            Monster m = mo.GetComponent<Monster>();
            if (m == null) continue;
            
            float dist = Vector2.Distance(transform.position, m.transform.position);
            if (dist < heroAttackRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = m;
            }
        }
        
        if (nearest != null)
        {
            Debug.Log($"[Character] 히어로 {character.characterName}이(가) 몬스터 {nearest.name}을(를) 탐지! (거리: {nearestDist}, 히어로 공격범위: {heroAttackRange})");
        }
        
        return nearest;
    }
    
    private Character FindCharacterTargetInRange()
    {
        if (character.isHero || !movement.HasJumped())
        {
            return null;
        }
        
        Character[] allCharacters = FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        Character nearest = null;
        float nearestDist = Mathf.Infinity;
        
        foreach (Character otherChar in allCharacters)
        {
            if (otherChar == null || otherChar == character) continue;
            
            if (character.isHero && otherChar.isHero)
            {
                continue;
            }
            
            bool canAttack = false;
            
            if (character.areaIndex != otherChar.areaIndex)
            {
                canAttack = true;
            }
            
            if (!canAttack) continue;
            
            float dist = Vector3.Distance(transform.position, otherChar.transform.position);
            if (dist <= character.attackRange && dist < nearestDist)
            {
                nearest = otherChar;
                nearestDist = dist;
            }
        }
        
        return nearest;
    }
    
    private void AttackTarget(IDamageable target)
    {
        if (target == null) return;
        
        int targetAreaIndex = 0;
        Vector3 targetPosition = Vector3.zero;
        string targetName = "Unknown";
        GameObject targetGameObject = null;
        
        if (target is Monster monster)
        {
            targetAreaIndex = monster.areaIndex;
            targetPosition = monster.transform.position;
            targetName = monster.name;
            targetGameObject = monster.gameObject;
        }
        else if (target is Character targetChar)
        {
            targetAreaIndex = targetChar.areaIndex;
            targetPosition = targetChar.transform.position;
            targetName = targetChar.characterName;
            targetGameObject = targetChar.gameObject;
        }
        else
        {
            Debug.LogWarning($"[Character] 알 수 없는 타겟 타입입니다: {target.GetType()}");
            return;
        }
        
        GameObject bulletPrefabToUse = GetBulletPrefab();
        if (bulletPrefabToUse != null)
        {
            try
            {
                GameObject bulletObj = Instantiate(bulletPrefabToUse);
                
                RectTransform parentPanel = SelectBulletPanel(targetAreaIndex);
                
                if (parentPanel != null && parentPanel.gameObject.scene.IsValid())
                {
                    bulletObj.transform.SetParent(parentPanel, false);
                    Debug.Log($"[Character] 총알을 {parentPanel.name}에 생성함");
                }
                else
                {
                    Debug.LogWarning($"[Character] 총알을 생성할 패널이 없음, 대체 로직 사용");
                    bulletObj.transform.position = transform.position;
                    bulletObj.transform.localRotation = Quaternion.identity;
                    
                    target.TakeDamage(character.attackPower);
                    Destroy(bulletObj);
                    return;
                }
                
                RectTransform bulletRect = bulletObj.GetComponent<RectTransform>();
                if (bulletRect != null && parentPanel != null)
                {
                    Vector2 localPos = parentPanel.InverseTransformPoint(transform.position);
                    bulletRect.anchoredPosition = localPos;
                    bulletRect.localRotation = Quaternion.identity;
                }
                else
                {
                    bulletObj.transform.position = transform.position;
                    bulletObj.transform.localRotation = Quaternion.identity;
                }
                
                Bullet bulletComp = bulletObj.GetComponent<Bullet>();
                if (bulletComp != null)
                {
                    float bulletSpeed = 10.0f;
                    
                    if (character.isHero)
                    {
                        bulletComp.isAreaAttack = true;
                        bulletComp.areaRadius = 2.0f;
                        
                        if (character.star == CharacterStar.OneStar)
                        {
                            bulletComp.bulletType = BulletType.Normal;
                        }
                        else if (character.star == CharacterStar.TwoStar)
                        {
                            bulletComp.bulletType = BulletType.Explosive;
                            bulletComp.speed = bulletSpeed * 1.2f;
                        }
                        else if (character.star == CharacterStar.ThreeStar)
                        {
                            bulletComp.bulletType = BulletType.Chain;
                            bulletComp.chainMaxBounces = 3;
                            bulletComp.chainBounceRange = 3.0f;
                            bulletComp.speed = bulletSpeed * 1.2f;
                        }
                        
                        Debug.Log($"[Character] 히어로 {character.characterName}이(가) 특수 총알을 발사! (타입: {bulletComp.bulletType})");
                    }
                    else
                    {
                        bulletComp.bulletType = BulletType.Normal;
                        bulletComp.isAreaAttack = character.isAreaAttack;
                        bulletComp.areaRadius = character.areaAttackRadius;
                    }
                    
                    bulletComp.speed = bulletSpeed;
                    
                    bool isTargetAbove = targetPosition.y > transform.position.y;
                    bulletComp.bulletUpDirectionSprite = bulletUpDirectionSprite;
                    bulletComp.bulletDownDirectionSprite = bulletDownDirectionSprite;
                    
                    bulletComp.target = targetGameObject;
                    
                    bulletComp.Init(target, character.attackPower, bulletSpeed, bulletComp.isAreaAttack, bulletComp.areaRadius, character.areaIndex);
                    Debug.Log($"[Character] {character.characterName}의 총알이 {targetName}을(를) 향해 발사됨! (속도: {bulletSpeed})");
                }
                else
                {
                    target.TakeDamage(character.attackPower);
                    Destroy(bulletObj);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Character] 총알 생성 중 오류 발생: {e.Message}");
                target.TakeDamage(character.attackPower);
            }
        }
        else
        {
            target.TakeDamage(character.attackPower);
            Debug.Log($"[Character] {character.characterName}이(가) {targetName}에게 직접 {character.attackPower} 데미지!");
        }
    }
    
    private RectTransform SelectBulletPanel(int targetAreaIndex)
    {
        RectTransform parentPanel = null;
        
        if (character.areaIndex == 2 || targetAreaIndex != character.areaIndex)
        {
            if (opponentBulletPanel != null)
            {
                parentPanel = opponentBulletPanel;
                Debug.Log($"[Character] {character.characterName}의 총알이 opponentBulletPanel에 생성됨");
            }
            else
            {
                Debug.LogWarning($"[Character] {character.characterName}에게 opponentBulletPanel이 설정되지 않음");
                parentPanel = bulletPanel;
                
                if (parentPanel == null)
                {
                    Canvas canvas = FindFirstObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        GameObject bulletPanelObj = new GameObject("EmergencyBulletPanel");
                        parentPanel = bulletPanelObj.AddComponent<RectTransform>();
                        bulletPanelObj.transform.SetParent(canvas.transform, false);
                        bulletPanel = parentPanel;
                        Debug.LogWarning($"[Character] 비상용 총알 패널을 생성했습니다.");
                    }
                }
            }
        }
        else
        {
            parentPanel = bulletPanel;
            
            if (parentPanel == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (Canvas canvas in canvases)
                {
                    if (canvas.isActiveAndEnabled)
                    {
                        GameObject bulletPanelObj = new GameObject("EmergencyBulletPanel");
                        parentPanel = bulletPanelObj.AddComponent<RectTransform>();
                        bulletPanelObj.transform.SetParent(canvas.transform, false);
                        bulletPanel = parentPanel;
                        Debug.LogWarning($"[Character] 비상용 총알 패널을 {canvas.name}에 생성했습니다.");
                        break;
                    }
                }
            }
        }
        
        return parentPanel;
    }
    
    private GameObject GetBulletPrefab()
    {
        if (bulletPrefab != null)
        {
            return bulletPrefab;
        }
        else
        {
            Debug.LogError($"[Character] {character.characterName}에 bulletPrefab이 설정되지 않음!");
            return null;
        }
    }
    
    public void SetBulletPanel(RectTransform panel)
    {
        bulletPanel = panel;
    }
    
    private void OnMouseDown()
    {
        character.attackRange = newAttackRangeOnClick;
        Debug.Log($"[Character] '{character.characterName}' 클릭 -> 사거리 {character.attackRange}로 변경");
        visual.CreateRangeIndicator();
        
        if (attackInfoText == null) return;
        
        attackInfoText.gameObject.SetActive(true);
        attackInfoText.text = $"공격 범위: {character.attackRange}\n공격 형태: {character.rangeType}";
        
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + attackInfoOffset);
        attackInfoText.transform.position = screenPos;
    }
}