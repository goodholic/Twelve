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
            IDamageable target = null;
            GameObject targetGameObject = null;
            string targetName = "";
            
            // 1. 캐릭터 타겟 찾기
            Character charTarget = FindCharacterTargetInRange();
            if (charTarget != null && charTarget.currentHP > 0)
            {
                character.currentCharTarget = charTarget;
                targetGameObject = charTarget.gameObject;
                target = charTarget;
                targetName = charTarget.characterName;
            }
            else
            {
                // 2. 몬스터 타겟 찾기
                Monster monsterTarget = FindMonsterTargetInRange();
                if (monsterTarget != null && monsterTarget.health > 0)
                {
                    character.currentTarget = monsterTarget;
                    targetGameObject = monsterTarget.gameObject;
                    target = monsterTarget;
                    targetName = monsterTarget.gameObject.name;
                }
            }
            
            // 공격 실행
            if (target != null)
            {
                Attack(target, targetGameObject, targetName);
                // movement.SetCombatState(true); // 메서드가 존재하지 않음
                
                if (visual != null)
                {
                    // visual.TriggerAttackAnimation(); // 메서드가 존재하지 않음
                }
            }
            else
            {
                // movement.SetCombatState(false); // 메서드가 존재하지 않음
                
                // 타겟이 없으면 성 공격 체크
                // movement.GetTargetMiddleCastle()와 GetTargetFinalCastle() 메서드가 존재하지 않음
                // 대신 직접 찾기
                MiddleCastle middleCastle = FindFirstObjectByType<MiddleCastle>();
                FinalCastle finalCastle = FindFirstObjectByType<FinalCastle>();
                
                if (middleCastle != null && middleCastle.currentHealth > 0)
                {
                    AttackCastle(middleCastle);
                }
                else if (finalCastle != null && finalCastle.currentHealth > 0)
                {
                    AttackCastle(finalCastle);
                }
            }
        }
    }
    
    private void Attack(IDamageable target, GameObject targetGameObject, string targetName)
    {
        GameObject bulletPrefabToUse = GetBulletPrefab();
        
        if (bulletPrefabToUse != null)
        {
            try
            {
                GameObject bulletObj = Instantiate(bulletPrefabToUse);
                
                // UI 총알이 보이도록 개선된 설정
                RectTransform parentPanel = SelectBulletPanel(character.areaIndex == 1 ? 2 : 1);
                
                if (parentPanel != null)
                {
                    bulletObj.transform.SetParent(parentPanel, false);
                    
                    // RectTransform 설정
                    RectTransform bulletRect = bulletObj.GetComponent<RectTransform>();
                    if (bulletRect == null)
                    {
                        bulletRect = bulletObj.AddComponent<RectTransform>();
                    }
                    
                    // 위치 설정
                    Vector2 localPos = parentPanel.InverseTransformPoint(transform.position);
                    bulletRect.anchoredPosition = localPos;
                    
                    // 크기 설정 (UI에서 보이도록)
                    bulletRect.sizeDelta = new Vector2(30f, 30f); // 기본 크기
                    
                    // 앵커와 피벗 설정
                    bulletRect.anchorMin = new Vector2(0.5f, 0.5f);
                    bulletRect.anchorMax = new Vector2(0.5f, 0.5f);
                    bulletRect.pivot = new Vector2(0.5f, 0.5f);
                    
                    // 스케일 확인
                    if (bulletRect.localScale.x < 0.1f || bulletRect.localScale.y < 0.1f)
                    {
                        bulletRect.localScale = Vector3.one;
                    }
                    
                    // 맨 앞으로 이동
                    bulletRect.SetAsLastSibling();
                    
                    // Image 컴포넌트 확인
                    Image bulletImage = bulletObj.GetComponent<Image>();
                    if (bulletImage != null)
                    {
                        bulletImage.enabled = true;
                        
                        // 색상이 투명하지 않도록 설정
                        if (bulletImage.color.a < 0.1f)
                        {
                            Color c = bulletImage.color;
                            c.a = 1f;
                            bulletImage.color = c;
                        }
                        
                        // 스프라이트가 없으면 기본 스프라이트 설정
                        if (bulletImage.sprite == null)
                        {
                            // 방향에 따른 스프라이트 설정 시도
                            if (bulletUpDirectionSprite != null || bulletDownDirectionSprite != null)
                            {
                                bool isUpDirection = targetGameObject.transform.position.y > transform.position.y;
                                bulletImage.sprite = isUpDirection ? bulletUpDirectionSprite : bulletDownDirectionSprite;
                            }
                        }
                        
                        // Raycast Target 비활성화 (클릭 방해 방지)
                        bulletImage.raycastTarget = false;
                    }
                    
                    // Canvas Group 추가 (알파값 제어용)
                    CanvasGroup canvasGroup = bulletObj.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = bulletObj.AddComponent<CanvasGroup>();
                    }
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
                
                // 근접 공격이고 타겟이 매우 가까우면
                if (character.rangeType == RangeType.Melee && 
                    Vector2.Distance(transform.position, targetGameObject.transform.position) < 0.5f)
                {
                    bulletObj.transform.position = transform.position;
                    bulletObj.transform.localRotation = Quaternion.identity;
                    
                    target.TakeDamage(character.attackPower);
                    Destroy(bulletObj);
                    return;
                }
                
                // Bullet 컴포넌트 초기화
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
                
                // RectTransform 설정
                RectTransform bulletRect = bulletObj.GetComponent<RectTransform>();
                if (bulletRect == null)
                {
                    bulletRect = bulletObj.AddComponent<RectTransform>();
                }
                
                Vector2 localPos = parentPanel.InverseTransformPoint(transform.position);
                bulletRect.anchoredPosition = localPos;
                bulletRect.sizeDelta = new Vector2(30f, 30f);
                bulletRect.anchorMin = new Vector2(0.5f, 0.5f);
                bulletRect.anchorMax = new Vector2(0.5f, 0.5f);
                bulletRect.pivot = new Vector2(0.5f, 0.5f);
                bulletRect.localScale = Vector3.one;
                bulletRect.SetAsLastSibling();
                
                // Image 컴포넌트 설정
                Image bulletImage = bulletObj.GetComponent<Image>();
                if (bulletImage != null)
                {
                    bulletImage.enabled = true;
                    if (bulletImage.color.a < 0.1f)
                    {
                        Color c = bulletImage.color;
                        c.a = 1f;
                        bulletImage.color = c;
                    }
                    bulletImage.raycastTarget = false;
                }
                
                // Canvas Group 설정
                CanvasGroup canvasGroup = bulletObj.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = bulletObj.AddComponent<CanvasGroup>();
                }
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
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
        // 태그 대신 FindObjectsByType 사용하여 모든 몬스터 찾기
        Monster[] allMonsters = FindObjectsByType<Monster>(FindObjectsSortMode.None);
        
        Monster nearestMonster = null;
        float nearestDistance = Mathf.Infinity;
        
        foreach (var monster in allMonsters)
        {
            if (monster == null || monster.health <= 0) continue;
            
            // 같은 지역의 몬스터는 공격하지 않음
            if (monster.areaIndex == character.areaIndex) continue;
            
            float dist = Vector2.Distance(transform.position, monster.transform.position);
            if (dist <= character.attackRange && dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestMonster = monster;
            }
        }
        
        return nearestMonster;
    }
    
    // 클릭 시 공격 범위 변경
    private void OnMouseDown()
    {
        Debug.Log($"[Character] {character.characterName} 클릭됨! 공격 범위를 {character.attackRange}에서 {newAttackRangeOnClick}로 변경");
        character.attackRange = newAttackRangeOnClick;
        
        // 공격 범위 표시
        if (attackInfoText != null)
        {
            attackInfoText.text = $"공격범위: {character.attackRange:F1}";
            
            // 텍스트 위치 업데이트
            if (attackInfoText.transform.parent != null)
            {
                Vector3 worldPos = transform.position + attackInfoOffset;
                attackInfoText.transform.position = worldPos;
            }
        }
    }
    
    private void OnDestroy()
    {
        if (attackInfoText != null && attackInfoText.gameObject != null)
        {
            Destroy(attackInfoText.gameObject);
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
    
    private int GetCharacterIndex()
    {
        // 캐릭터 이름에서 인덱스 추출 시도
        if (!string.IsNullOrEmpty(character.characterName))
        {
            // "Character1", "Character2" 등의 패턴에서 숫자 추출
            string numStr = System.Text.RegularExpressions.Regex.Match(character.characterName, @"\d+").Value;
            if (!string.IsNullOrEmpty(numStr))
            {
                if (int.TryParse(numStr, out int index))
                {
                    return index - 1; // 1-based에서 0-based로 변환
                }
            }
        }
        
        // 캐릭터 ID 속성이 없으므로 기본값 반환
        return 0; // 기본값으로 0 사용
    }
}