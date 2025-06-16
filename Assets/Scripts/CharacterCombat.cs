using System.Collections;
using UnityEngine;

/// <summary>
/// 캐릭터 전투 관리 컴포넌트
/// 게임 기획서: 타워형 캐릭터 - 사정거리 내 적 공격
/// </summary>
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
    
    // 전투 상태
    private float lastAttackTime = 0f;
    private bool isAttacking = false;
    
    /// <summary>
    /// 전투 시스템 초기화
    /// </summary>
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
    
    /// <summary>
    /// 공격 정보 텍스트 생성
    /// </summary>
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
    
    /// <summary>
    /// 총알 프리팹 자동 찾기
    /// </summary>
    private GameObject GetBulletPrefab()
    {
        var coreData = CoreDataManager.Instance;
        if (coreData == null)
        {
            Debug.LogError("[CharacterCombat] CoreDataManager.Instance가 null입니다!");
            return null;
        }
        
        // 기본 총알 프리팹 사용 (defaultBulletPrefab이 CoreDataManager에 추가됨)
        if (coreData.defaultBulletPrefab != null)
        {
            return coreData.defaultBulletPrefab;
        }
        
        // bulletPrefab이 이미 설정되어 있으면 사용
        if (bulletPrefab != null)
        {
            return bulletPrefab;
        }
        
        return null;
    }
    
    /// <summary>
    /// 공격 루틴
    /// </summary>
    private IEnumerator AttackRoutine()
    {
        while (character != null && character.currentHP > 0)
        {
            // 공격 쿨다운 체크
            float attackCooldown = stats != null ? stats.GetAttackSpeed() : (1f / character.attackSpeed);
            
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                // 타겟 찾기
                bool foundTarget = TryFindAndAttackTarget();
                
                if (foundTarget)
                {
                    lastAttackTime = Time.time;
                    isAttacking = true;
                }
                else
                {
                    isAttacking = false;
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    /// <summary>
    /// 공격 체크 수정
    /// </summary>
    private bool CanAttackTarget()
    {
        // 이동 중인지 확인
        if (movement != null)
        {
            // isMoving 프로퍼티 사용
            if (movement.isMoving)
            {
                return false;
            }
        }
        
        // 점프 중인지 확인
        if (jumpSystem != null && movement != null)
        {
            if (movement.IsJumping() || movement.IsJumpingAcross())
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 타겟 찾고 공격
    /// </summary>
    private bool TryFindAndAttackTarget()
    {
        if (!CanAttackTarget()) return false;
        
        IDamageable target = null;
        GameObject targetGameObject = null;
        string targetName = "";
        
        // 타겟 타입에 따라 우선순위 처리
        switch (character.attackTargetType)
        {
            case AttackTargetType.Character:
                var charResult = FindCharacterTargetInRange();
                target = charResult.Item1;
                targetGameObject = charResult.Item2;
                targetName = charResult.Item3;
                break;
                
            case AttackTargetType.Monster:
                var monResult = FindMonsterTargetInRange();
                target = monResult.Item1;
                targetGameObject = monResult.Item2;
                targetName = monResult.Item3;
                break;
                
            case AttackTargetType.Both:
                // 둘 다 찾아서 가까운 쪽 공격
                var charBoth = FindCharacterTargetInRange();
                var monBoth = FindMonsterTargetInRange();
                
                if (charBoth.Item1 != null && monBoth.Item1 != null)
                {
                    float charDist = Vector3.Distance(transform.position, charBoth.Item2.transform.position);
                    float monDist = Vector3.Distance(transform.position, monBoth.Item2.transform.position);
                    
                    if (charDist <= monDist)
                    {
                        target = charBoth.Item1;
                        targetGameObject = charBoth.Item2;
                        targetName = charBoth.Item3;
                    }
                    else
                    {
                        target = monBoth.Item1;
                        targetGameObject = monBoth.Item2;
                        targetName = monBoth.Item3;
                    }
                }
                else if (charBoth.Item1 != null)
                {
                    target = charBoth.Item1;
                    targetGameObject = charBoth.Item2;
                    targetName = charBoth.Item3;
                }
                else if (monBoth.Item1 != null)
                {
                    target = monBoth.Item1;
                    targetGameObject = monBoth.Item2;
                    targetName = monBoth.Item3;
                }
                break;
                
            case AttackTargetType.CastleOnly:
                var castleResult = FindCastleTargetInRange();
                target = castleResult.Item1;
                targetName = castleResult.Item2;
                // 성의 GameObject 찾기
                if (target != null)
                {
                    MiddleCastle mc = target as MiddleCastle;
                    FinalCastle fc = target as FinalCastle;
                    targetGameObject = mc != null ? mc.gameObject : (fc != null ? fc.gameObject : null);
                }
                break;
                
            case AttackTargetType.All:
                // 모든 타겟 중 가장 가까운 것
                var charAll = FindCharacterTargetInRange();
                var monAll = FindMonsterTargetInRange();
                var castleAll = FindCastleTargetInRange();
                
                float minDistance = float.MaxValue;
                
                if (charAll.Item1 != null)
                {
                    float dist = Vector3.Distance(transform.position, charAll.Item2.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        target = charAll.Item1;
                        targetGameObject = charAll.Item2;
                        targetName = charAll.Item3;
                    }
                }
                
                if (monAll.Item1 != null)
                {
                    float dist = Vector3.Distance(transform.position, monAll.Item2.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        target = monAll.Item1;
                        targetGameObject = monAll.Item2;
                        targetName = monAll.Item3;
                    }
                }
                
                if (castleAll.Item1 != null)
                {
                    MiddleCastle mc = castleAll.Item1 as MiddleCastle;
                    FinalCastle fc = castleAll.Item1 as FinalCastle;
                    GameObject castleObj = mc != null ? mc.gameObject : (fc != null ? fc.gameObject : null);
                    
                    if (castleObj != null)
                    {
                        float dist = Vector3.Distance(transform.position, castleObj.transform.position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            target = castleAll.Item1;
                            targetGameObject = castleObj;
                            targetName = castleAll.Item2;
                        }
                    }
                }
                break;
        }
        
        if (target != null && targetGameObject != null)
        {
            // 총알 생성
            CreateBullet(target, targetGameObject, targetName);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 총알 생성
    /// </summary>
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
                        bullet.bulletUpDirectionSprite = bulletUpDirectionSprite;
                        bullet.bulletDownDirectionSprite = bulletDownDirectionSprite;
                    }
                    
                    // 총알 초기화
                    bullet.Init(character.attackPower, bulletSpeed, targetGameObject, character.areaIndex);
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
    
    /// <summary>
    /// 범위 내 캐릭터 타겟 찾기
    /// </summary>
    private (IDamageable, GameObject, string) FindCharacterTargetInRange()
    {
        float closestDistance = float.MaxValue;
        Character closestTarget = null;
        
        Character[] allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        foreach (var target in allCharacters)
        {
            if (target == null || target == character) continue;
            if (target.areaIndex == character.areaIndex) continue; // 같은 지역 캐릭터는 공격하지 않음
            
            float distance = Vector3.Distance(transform.position, target.transform.position);
            
            if (distance <= character.attackRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }
        
        if (closestTarget != null)
        {
            return (closestTarget, closestTarget.gameObject, closestTarget.characterName);
        }
        
        return (null, null, "");
    }
    
    /// <summary>
    /// 범위 내 몬스터 타겟 찾기
    /// </summary>
    private (IDamageable, GameObject, string) FindMonsterTargetInRange()
    {
        float closestDistance = float.MaxValue;
        Monster closestTarget = null;
        
        Monster[] allMonsters = Object.FindObjectsByType<Monster>(FindObjectsSortMode.None);
        
        foreach (var target in allMonsters)
        {
            if (target == null) continue;
            if (target.areaIndex == character.areaIndex) continue; // 같은 지역 몬스터는 공격하지 않음
            
            float distance = Vector3.Distance(transform.position, target.transform.position);
            
            if (distance <= character.attackRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }
        
        if (closestTarget != null)
        {
            return (closestTarget, closestTarget.gameObject, closestTarget.monsterName);
        }
        
        return (null, null, "");
    }
    
    /// <summary>
    /// 범위 내 성 타겟 찾기
    /// </summary>
    private (IDamageable, string) FindCastleTargetInRange()
    {
        // 중간성 찾기
        MiddleCastle[] middleCastles = Object.FindObjectsByType<MiddleCastle>(FindObjectsSortMode.None);
        foreach (var castle in middleCastles)
        {
            if (castle == null) continue;
            if (castle.areaIndex == character.areaIndex) continue; // 같은 지역 성은 공격하지 않음
            
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
            if (castle == null) continue;
            if (castle.areaIndex == character.areaIndex) continue; // 같은 지역 성은 공격하지 않음
            
            float distance = Vector3.Distance(transform.position, castle.transform.position);
            if (distance <= character.attackRange)
            {
                return (castle, "최종성");
            }
        }
        
        return (null, "");
    }
    
    /// <summary>
    /// 드래그 가능 상태 확인
    /// </summary>
    public bool CanBeDragged()
    {
        return character.isDraggable && !isAttacking && (movement == null || !movement.isMoving);
    }
    
    /// <summary>
    /// 공격 애니메이션 재생
    /// </summary>
    public void PlayAttackAnimation()
    {
        visual?.PlayAttackAnimation();
    }
    
    /// <summary>
    /// 범위 공격 체크 수정
    /// </summary>
    private void CheckAreaAttack()
    {
        if (!character.isAreaAttack) return;
        
        // 이동 중이면 범위 공격 안함
        if (movement != null && movement.isMoving)
        {
            return;
        }
        
        // 범위 공격 로직...
    }
}