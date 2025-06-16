using System.Collections;
using UnityEngine;

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
    
    // 클릭 시 변경될 새로운 공격 범위
    [Header("클릭 시 변경될 새로운 공격 범위(예: 4.0f)")]
    public float newAttackRangeOnClick = 4f;
    
    // 공격 정보 표시용 UI - World Space Text로 변경
    [Header("공격 정보 표시용 UI")]
    [Tooltip("공격 범위/형태를 보여줄 TextMeshPro (World Space)")]
    public TMPro.TextMeshPro attackInfoText;
    [Tooltip("공격 정보 텍스트 위치 오프셋(월드 좌표)")]
    public Vector3 attackInfoOffset = new Vector3(1f, 0f, 0f);
    
    public void Initialize(Character character, CharacterStats stats, CharacterVisual visual, CharacterMovement movement, CharacterJump jumpSystem)
    {
        this.character = character;
        this.stats = stats;
        this.visual = visual;
        this.movement = movement;
        this.jumpSystem = jumpSystem;
        
        // bulletPrefab이 설정되지 않았으면 자동으로 설정 시도
        if (bulletPrefab == null)
        {
            Debug.Log($"[CharacterCombat] {character.characterName}의 bulletPrefab이 null입니다. 자동 설정을 시도합니다.");
            GameObject foundPrefab = GetBulletPrefab();
            
            if (foundPrefab == null)
            {
                Debug.LogWarning($"[CharacterCombat] {character.characterName}의 총알 프리팹을 찾을 수 없습니다. 공격 시 직접 데미지로 처리됩니다.");
            }
        }
        
        // 공격 정보 텍스트 생성 (World Space)
        CreateAttackInfoText();
        
        StartCoroutine(AttackRoutine());
    }
    
    private void CreateAttackInfoText()
    {
        if (attackInfoText == null)
        {
            GameObject textObj = new GameObject("AttackInfoText");
            textObj.transform.SetParent(transform);
            textObj.transform.localPosition = attackInfoOffset;
            
            attackInfoText = textObj.AddComponent<TMPro.TextMeshPro>();
            attackInfoText.text = $"공격범위: {character.attackRange:F1}";
            attackInfoText.fontSize = 2f;
            attackInfoText.alignment = TMPro.TextAlignmentOptions.Center;
            attackInfoText.color = Color.white;
            
            // 텍스트가 항상 카메라를 바라보도록 설정
            textObj.AddComponent<LookAtCamera>();
        }
    }
    
    private GameObject GetBulletPrefab()
    {
        var coreData = CoreDataManager.Instance;
        if (coreData == null)
        {
            Debug.LogError("[CharacterCombat] CoreDataManager.Instance가 null입니다!");
            return null;
        }

        if (character.areaIndex == 1)
        {
            if (coreData.allyBulletPrefabs != null && 
                character.characterIndex >= 0 && 
                character.characterIndex < coreData.allyBulletPrefabs.Length)
            {
                bulletPrefab = coreData.allyBulletPrefabs[character.characterIndex];
                return bulletPrefab;
            }
        }
        else if (character.areaIndex == 2)
        {
            if (coreData.enemyBulletPrefabs != null && 
                character.characterIndex >= 0 && 
                character.characterIndex < coreData.enemyBulletPrefabs.Length)
            {
                bulletPrefab = coreData.enemyBulletPrefabs[character.characterIndex];
                return bulletPrefab;
            }
        }

        Debug.LogWarning($"[CharacterCombat] {character.characterName}의 총알 프리팹을 데이터베이스에서 찾을 수 없습니다.");
        return null;
    }
    
    private IEnumerator AttackRoutine()
    {
        while (character != null && character.currentHP > 0)
        {
            yield return new WaitForSeconds(1f / character.attackSpeed);
            
            if (character.isCharAttack && CanAttack())
            {
                PerformAttack();
            }
        }
    }
    
    private bool CanAttack()
    {
        return !movement.IsJumping() && !movement.isMoving;
    }
    
    private void PerformAttack()
    {
        IDamageable target = null;
        GameObject targetGameObject = null;
        string targetName = "";
        
        // 공격 우선순위에 따라 타겟 찾기
        switch (character.attackTargetType)
        {
            case AttackTargetType.CastleOnly:
                var (castle, castleName) = FindCastleTargetInRange();
                if (castle != null)
                {
                    target = castle;
                    targetGameObject = castle is MiddleCastle mc ? mc.gameObject : (castle as FinalCastle).gameObject;
                    targetName = castleName;
                }
                break;
                
            case AttackTargetType.CharacterOnly:
                Character charTarget = FindCharacterTargetInRange();
                if (charTarget != null)
                {
                    character.currentCharTarget = charTarget;
                    target = charTarget;
                    targetGameObject = charTarget.gameObject;
                    targetName = charTarget.characterName;
                }
                break;
                
            case AttackTargetType.All:
            default:
                // 1. 캐릭터 타겟 찾기
                Character charTargetAll = FindCharacterTargetInRange();
                if (charTargetAll != null)
                {
                    character.currentCharTarget = charTargetAll;
                    target = charTargetAll;
                    targetGameObject = charTargetAll.gameObject;
                    targetName = charTargetAll.characterName;
                }
                
                // 2. 몬스터 타겟 찾기
                if (target == null)
                {
                    Monster monsterTarget = FindMonsterTargetInRange();
                    if (monsterTarget != null)
                    {
                        character.currentTarget = monsterTarget;
                        target = monsterTarget;
                        targetGameObject = monsterTarget.gameObject;
                        targetName = monsterTarget.monsterName;
                    }
                }
                
                // 3. 성 타겟 찾기
                if (target == null)
                {
                    var (castleAll, castleNameAll) = FindCastleTargetInRange();
                    if (castleAll != null)
                    {
                        target = castleAll;
                        targetGameObject = castleAll is MiddleCastle mc ? mc.gameObject : (castleAll as FinalCastle).gameObject;
                        targetName = castleNameAll;
                    }
                }
                break;
        }
        
        if (target != null && targetGameObject != null)
        {
            // 총알 생성
            CreateBullet(target, targetGameObject, targetName);
        }
    }
    
    private void CreateBullet(IDamageable target, GameObject targetGameObject, string targetName)
    {
        if (bulletPrefab != null)
        {
            try
            {
                // 총알을 월드 공간에 생성
                Vector3 spawnPosition = transform.position + Vector3.up * 0.5f; // 캐릭터 중앙에서 발사
                GameObject bulletObj = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
                
                Bullet bullet = bulletObj.GetComponent<Bullet>();
                if (bullet != null)
                {
                    // 방향에 따른 스프라이트 설정
                    Vector3 direction = (targetGameObject.transform.position - transform.position).normalized;
                    bullet.SetBulletDirection(direction);
                    
                    // 방향별 스프라이트 설정
                    if (bulletUpDirectionSprite != null || bulletDownDirectionSprite != null)
                    {
                        Bullet bulletComp = bulletObj.GetComponent<Bullet>();
                        if (bulletComp != null)
                        {
                            bulletComp.bulletUpDirectionSprite = bulletUpDirectionSprite;
                            bulletComp.bulletDownDirectionSprite = bulletDownDirectionSprite;
                        }
                    }
                    
                    // 총알 초기화
                    bullet.Init(target, character.attackPower, bulletSpeed,
                        character.isAreaAttack, character.areaAttackRadius, character.areaIndex);
                    bullet.SetSourceCharacter(character);
                    
                    // 시각 효과
                    visual?.PlayAttackAnimation();
                    
                    Debug.Log($"[CharacterCombat] {character.characterName}이(가) {targetName}을(를) 공격! (데미지: {character.attackPower})");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CharacterCombat] 총알 생성 실패: {e.Message}");
            }
        }
        else
        {
            // 총알 프리팹이 없으면 직접 데미지 처리
            target.TakeDamage(character.attackPower);
            visual?.PlayAttackAnimation();
            Debug.Log($"[CharacterCombat] {character.characterName}이(가) {targetName}에게 직접 데미지! (데미지: {character.attackPower})");
        }
    }
    
    private Monster FindMonsterTargetInRange()
    {
        string targetTag = character.areaIndex == 1 ? "EnemyMonster" : "Monster";
        GameObject[] monsters = GameObject.FindGameObjectsWithTag(targetTag);
        
        Monster closestMonster = null;
        float closestDistance = character.attackRange;
        
        foreach (GameObject obj in monsters)
        {
            if (obj == null) continue;
            
            Monster monster = obj.GetComponent<Monster>();
            if (monster == null || monster.currentHealth <= 0) continue;
            
            float distance = Vector3.Distance(transform.position, monster.transform.position);
            if (distance <= character.attackRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestMonster = monster;
            }
        }
        
        return closestMonster;
    }
    
    private Character FindCharacterTargetInRange()
    {
        Character[] allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        Character closestChar = null;
        float closestDistance = character.attackRange;
        
        foreach (Character ch in allCharacters)
        {
            if (ch == null || ch == character || ch.currentHP <= 0) continue;
            if (ch.areaIndex == character.areaIndex) continue;
            
            float distance = Vector3.Distance(transform.position, ch.transform.position);
            if (distance <= character.attackRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestChar = ch;
            }
        }
        
        return closestChar;
    }
    
    private (IDamageable, string) FindCastleTargetInRange()
    {
        // 중간성 찾기
        MiddleCastle[] middleCastles = Object.FindObjectsByType<MiddleCastle>(FindObjectsSortMode.None);
        foreach (var castle in middleCastles)
        {
            if (castle == null || castle.currentHealth <= 0) continue;
            if (castle.areaIndex == character.areaIndex) continue;
            
            float distance = Vector3.Distance(transform.position, castle.transform.position);
            if (distance <= character.attackRange)
            {
                return (castle, "중간성");
            }
        }
        
        // 최종성 찾기
        FinalCastle[] finalCastles = Object.FindObjectsByType<FinalCastle>(FindObjectsSortMode.None);
        foreach (var castle in finalCastles)
        {
            if (castle == null || castle.currentHealth <= 0) continue;
            if (castle.areaIndex == character.areaIndex) continue;
            
            float distance = Vector3.Distance(transform.position, castle.transform.position);
            if (distance <= character.attackRange)
            {
                return (castle, "최종성");
            }
        }
        
        return (null, null);
    }
    
    public void OnMouseDown()
    {
        if (!character.isDraggable) return;
        
        character.attackRange = newAttackRangeOnClick;
        
        // 공격 범위 표시
        if (attackInfoText != null)
        {
            attackInfoText.text = $"공격범위: {character.attackRange:F1}";
        }
    }
    
    private void OnDestroy()
    {
        if (attackInfoText != null && attackInfoText.gameObject != null)
        {
            Destroy(attackInfoText.gameObject);
        }
    }
}

// 텍스트가 항상 카메라를 바라보도록 하는 컴포넌트
public class LookAtCamera : MonoBehaviour
{
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
    }
    
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward);
        }
    }
}