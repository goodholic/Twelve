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
        
        // bulletPrefab이 설정되지 않았으면 자동으로 설정 시도
        if (bulletPrefab == null)
        {
            Debug.Log($"[CharacterCombat] {character.characterName}의 bulletPrefab이 null입니다. 자동 설정을 시도합니다.");
            GameObject foundPrefab = GetBulletPrefab(); // 이 메서드에서 자동으로 bulletPrefab을 찾아서 설정함
            
            if (foundPrefab == null)
            {
                Debug.LogWarning($"[CharacterCombat] {character.characterName}의 총알 프리팹을 찾을 수 없습니다. 공격 시 직접 데미지로 처리됩니다.");
            }
        }
        
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
                        else if (!isOnWalkable)
                        {
                            // 성 타겟팅 (타워형 캐릭터만)
                            CheckCastleTargets();
                        }
                    }
                    break;
                    
                case AttackTargetType.CastleOnly:
                    if (!isOnWalkable)
                    {
                        CheckCastleTargets();
                    }
                    break;
                    
                case AttackTargetType.CharacterOnly:
                    character.currentCharTarget = FindCharacterTargetInRange();
                    if (character.currentCharTarget != null)
                    {
                        targetToDamage = character.currentCharTarget;
                    }
                    break;
            }
            
            if (targetToDamage != null)
            {
                Attack(targetToDamage);
            }
            else if (character.currentMiddleCastleTarget != null)
            {
                AttackCastle(character.currentMiddleCastleTarget);
            }
            else if (character.currentFinalCastleTarget != null)
            {
                AttackCastle(character.currentFinalCastleTarget);
            }
        }
    }
    
    private void CheckCastleTargets()
    {
        // 중간성 타겟팅
        MiddleCastle[] middleCastles = FindObjectsByType<MiddleCastle>(FindObjectsSortMode.None);
        foreach (var castle in middleCastles)
        {
            if (castle.areaIndex != character.areaIndex && !castle.IsDestroyed())
            {
                float dist = Vector2.Distance(transform.position, castle.transform.position);
                if (dist <= character.attackRange)
                {
                    character.currentMiddleCastleTarget = castle;
                    Debug.Log($"[Character] {character.characterName}이 중간성을 타겟팅!");
                    return;
                }
            }
        }
        
        // 최종성 타겟팅
        FinalCastle[] finalCastles = FindObjectsByType<FinalCastle>(FindObjectsSortMode.None);
        foreach (var castle in finalCastles)
        {
            if (castle.areaIndex != character.areaIndex && !castle.IsDestroyed())
            {
                float dist = Vector2.Distance(transform.position, castle.transform.position);
                if (dist <= character.attackRange)
                {
                    character.currentFinalCastleTarget = castle;
                    Debug.Log($"[Character] {character.characterName}이 최종성을 타겟팅!");
                    return;
                }
            }
        }
    }
    
    private void AttackCastle(MonoBehaviour castle)
    {
        GameObject bulletPrefabToUse = GetBulletPrefab();
        if (bulletPrefabToUse != null)
        {
            GameObject bulletObj = Instantiate(bulletPrefabToUse);
            RectTransform parentPanel = SelectBulletPanel(character.areaIndex == 1 ? 2 : 1);
            
            if (parentPanel != null)
            {
                bulletObj.transform.SetParent(parentPanel, false);
                RectTransform bulletRect = bulletObj.GetComponent<RectTransform>();
                if (bulletRect != null)
                {
                    Vector2 localPos = parentPanel.InverseTransformPoint(transform.position);
                    bulletRect.anchoredPosition = localPos;
                }
            }
            
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.Initialize(
                    castle.gameObject,
                    character.attackPower,
                    character.areaIndex,
                    true,
                    bulletSpeed,
                    character
                );
                
                if (character.isAreaAttack)
                {
                    bullet.SetAreaAttack(character.areaAttackRadius);
                }
            }
        }
        else
        {
            // 직접 데미지
            if (castle is MiddleCastle middleCastle)
            {
                middleCastle.TakeDamage(character.attackPower);
            }
            else if (castle is FinalCastle finalCastle)
            {
                finalCastle.TakeDamage(character.attackPower);
            }
        }
    }
    
    private Character FindCharacterTargetInRange()
    {
        Character[] allCharacters = FindObjectsByType<Character>(FindObjectsSortMode.None);
        Character nearestChar = null;
        float nearestDistance = Mathf.Infinity;
        
        foreach (var otherChar in allCharacters)
        {
            if (otherChar == null || otherChar == character) continue;
            if (otherChar.areaIndex == character.areaIndex) continue;
            if (otherChar.currentHP <= 0) continue;
            
            float dist = Vector2.Distance(transform.position, otherChar.transform.position);
            if (dist <= character.attackRange && dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestChar = otherChar;
            }
        }
        
        return nearestChar;
    }
    
    private Monster FindMonsterTargetInRange()
    {
        string targetTag = (character.areaIndex == 1) ? "Monster" : "EnemyMonster";
        GameObject[] monsterObjs = GameObject.FindGameObjectsWithTag(targetTag);
        
        Monster nearestMonster = null;
        float nearestDistance = Mathf.Infinity;
        
        foreach (GameObject obj in monsterObjs)
        {
            if (obj == null) continue;
            Monster monster = obj.GetComponent<Monster>();
            if (monster == null) continue;
            
            float dist = Vector2.Distance(transform.position, monster.transform.position);
            if (dist <= character.attackRange && dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestMonster = monster;
            }
        }
        
        return nearestMonster;
    }
    
    private Monster FindHeroTargetInRange()
    {
        string[] possibleTags = { "Monster", "EnemyMonster" };
        Monster nearestMonster = null;
        float nearestDistance = Mathf.Infinity;
        
        foreach (string tag in possibleTags)
        {
            GameObject[] monsterObjs = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in monsterObjs)
            {
                if (obj == null) continue;
                Monster monster = obj.GetComponent<Monster>();
                if (monster == null) continue;
                
                float dist = Vector2.Distance(transform.position, monster.transform.position);
                if (dist <= character.attackRange && dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearestMonster = monster;
                }
            }
        }
        
        return nearestMonster;
    }
    
    private void Attack(IDamageable target)
    {
        int targetAreaIndex = character.areaIndex;
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
                }
                else
                {
                    bulletObj.transform.position = transform.position;
                }
                
                Bullet bullet = bulletObj.GetComponent<Bullet>();
                if (bullet != null)
                {
                    bullet.Initialize(
                        targetGameObject,
                        character.attackPower,
                        character.areaIndex,
                        character.isAreaAttack,
                        bulletSpeed,
                        character
                    );
                    
                    if (character.isAreaAttack)
                    {
                        bullet.SetAreaAttack(character.areaAttackRadius);
                    }
                    
                    if (character.rangeType == RangeType.Melee)
                    {
                        bullet.SetAsMeleeAttack();
                    }
                    
                    Debug.Log($"[Character] {character.characterName}이(가) {targetName}에게 총알 발사! (속도: {bulletSpeed})");
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
            // bulletPrefab이 null일 때 자동으로 기본 총알 프리팹 찾기
            Debug.Log($"[CharacterCombat] {character.characterName}에 bulletPrefab이 설정되지 않음! 기본 총알 프리팹을 찾는 중...");
            
            // CoreDataManager에서 총알 프리팹 가져오기 시도
            var coreData = CoreDataManager.Instance;
            if (coreData != null)
            {
                // 캐릭터의 인덱스에 맞는 총알 프리팹 사용
                int characterIndex = GetCharacterIndex();
                
                if (character.areaIndex == 2)
                {
                    // 지역2 (적) 캐릭터는 enemy 총알 프리팹 사용
                    if (characterIndex >= 0 && characterIndex < coreData.enemyBulletPrefabs.Length && 
                        coreData.enemyBulletPrefabs[characterIndex] != null)
                    {
                        bulletPrefab = coreData.enemyBulletPrefabs[characterIndex];
                        Debug.Log($"[CharacterCombat] {character.characterName}에 적 총알 프리팹 {characterIndex}번을 자동 설정했습니다.");
                        return bulletPrefab;
                    }
                }
                else
                {
                    // 지역1 (아군) 캐릭터는 ally 총알 프리팹 사용
                    if (characterIndex >= 0 && characterIndex < coreData.allyBulletPrefabs.Length && 
                        coreData.allyBulletPrefabs[characterIndex] != null)
                    {
                        bulletPrefab = coreData.allyBulletPrefabs[characterIndex];
                        Debug.Log($"[CharacterCombat] {character.characterName}에 아군 총알 프리팹 {characterIndex}번을 자동 설정했습니다.");
                        return bulletPrefab;
                    }
                }
            }
            
            // Resources 폴더에서 기본 총알 프리팹 찾기
            GameObject defaultBullet = Resources.Load<GameObject>("BulletPrefab");
            if (defaultBullet == null)
            {
                // Assets/Prefabs/Character/Bullet/ 경로에서 찾기
                defaultBullet = Resources.Load<GameObject>("Prefabs/Character/Bullet/BulletPrefab");
            }
            
            if (defaultBullet != null)
            {
                bulletPrefab = defaultBullet;
                Debug.Log($"[CharacterCombat] {character.characterName}에 기본 총알 프리팹을 자동 설정했습니다.");
                return bulletPrefab;
            }
            
            // 프리팹을 찾을 수 없음 - null 반환
            Debug.Log($"[CharacterCombat] {character.characterName}의 총알 프리팹을 찾을 수 없습니다. 공격 시 직접 데미지로 처리됩니다.");
            return null;
        }
    }
    
    /// <summary>
    /// 캐릭터의 인덱스를 가져오는 메서드
    /// </summary>
    private int GetCharacterIndex()
    {
        if (character == null || string.IsNullOrEmpty(character.characterName))
            return 0;
            
        // 캐릭터 이름에서 인덱스 추출 시도
        string name = character.characterName.ToLower();
        
        // "character_1", "char_2" 등의 패턴에서 숫자 추출
        for (int i = 0; i <= 9; i++)
        {
            if (name.Contains(i.ToString()))
            {
                return i;
            }
        }
        
        // 기본값으로 0 반환
        return 0;
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