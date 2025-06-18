using UnityEngine;
using System.Collections;

/// <summary>
/// 캐릭터 전투 시스템 - 공격 로직 담당
/// </summary>
public class CharacterCombat : MonoBehaviour
{
    private Character character;
    private CharacterMovement movement;
    private CharacterVisual visual;
    
    [Header("공격 상태")]
    public bool isAttacking = false;
    private float lastAttackTime = 0f;
    private IDamageable currentTarget;
    
    [Header("타겟 검색")]
    private float targetSearchInterval = 0.5f;
    private Coroutine targetSearchCoroutine;
    private Coroutine attackCoroutine;
    
    [Header("투사체")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    
    private void Start()
    {
        character = GetComponent<Character>();
        movement = GetComponent<CharacterMovement>();
        visual = GetComponent<CharacterVisual>();
        
        if (character == null)
        {
            Debug.LogError("[CharacterCombat] Character 컴포넌트를 찾을 수 없습니다!");
            enabled = false;
            return;
        }
        
        // movement와 visual은 선택적 컴포넌트이므로 null이어도 오류를 발생시키지 않음
        if (movement == null)
        {
            Debug.LogWarning("[CharacterCombat] CharacterMovement 컴포넌트를 찾을 수 없습니다. 이동 관련 기능이 제한됩니다.");
        }
        
        if (visual == null)
        {
            Debug.LogWarning("[CharacterCombat] CharacterVisual 컴포넌트를 찾을 수 없습니다. 시각 효과가 제한됩니다.");
        }
        
        // 발사 위치가 없으면 생성
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = new Vector3(0, 0.5f, 0);
            firePoint = fp.transform;
        }
        
        // 코루틴 시작
        StartCombatCoroutines();
    }
    
    private void OnEnable()
    {
        StartCombatCoroutines();
    }
    
    private void OnDisable()
    {
        StopCombatCoroutines();
    }
    
    private void OnDestroy()
    {
        StopCombatCoroutines();
    }
    
    /// <summary>
    /// 전투 코루틴 시작
    /// </summary>
    private void StartCombatCoroutines()
    {
        if (character == null) return;
        
        StopCombatCoroutines();
        targetSearchCoroutine = StartCoroutine(TargetSearchRoutine());
        attackCoroutine = StartCoroutine(AttackRoutine());
    }
    
    /// <summary>
    /// 전투 코루틴 중지
    /// </summary>
    private void StopCombatCoroutines()
    {
        if (targetSearchCoroutine != null)
        {
            StopCoroutine(targetSearchCoroutine);
            targetSearchCoroutine = null;
        }
        
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
    }
    
    /// <summary>
    /// 타겟 검색 코루틴
    /// </summary>
    private IEnumerator TargetSearchRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(targetSearchInterval);
        
        while (true)
        {
            if (character != null && character.currentHP > 0)
            {
                FindTarget();
            }
            
            yield return wait;
        }
    }
    
    /// <summary>
    /// 공격 코루틴
    /// </summary>
    private IEnumerator AttackRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.1f); // 공격 체크 간격
        
        while (true)
        {
            if (character != null && character.currentHP > 0 && currentTarget != null && CanAttack())
            {
                Attack();
            }
            
            yield return wait;
        }
    }
    
    /// <summary>
    /// 타겟 찾기
    /// ★★★ 핵심 로직: AttackTargetType에 따른 다중 타겟팅 시스템
    /// - Character: 적 캐릭터만 타겟
    /// - Monster: 몬스터만 타겟
    /// - Both: 캐릭터와 몬스터 모두 타겟 (가까운 것 우선)
    /// - CastleOnly: 성만 타겟
    /// - All: 모든 타입 타겟 가능 (가장 가까운 것 우선)
    /// </summary>
    private void FindTarget()
    {
        IDamageable newTarget = null;
        GameObject targetObject = null;
        string targetName = "";
        
        // ★ 게임 기획서 요구사항: 공격 대상 시스템
        // 몬스터→성, 성→몬스터/캐릭터, 캐릭터→성/캐릭터 등 다양한 공격 패턴 지원
        switch (character.attackTargetType)
        {
            case AttackTargetType.Character:
                // 적 캐릭터만 공격 (PvP 전용)
                var charResult = FindCharacterTargetInRange();
                newTarget = charResult.Item1;
                targetObject = charResult.Item2;
                targetName = charResult.Item3;
                break;
                
            case AttackTargetType.Monster:
                // 몬스터만 공격 (PvE 전용)
                var monResult = FindMonsterTargetInRange();
                newTarget = monResult.Item1;
                targetObject = monResult.Item2;
                targetName = monResult.Item3;
                break;
                
            case AttackTargetType.Both:
                // 캐릭터와 몬스터 모두 공격 가능 - 거리 비교로 우선순위 결정
                var charBoth = FindCharacterTargetInRange();
                var monBoth = FindMonsterTargetInRange();
                
                // 둘 다 있으면 거리 비교
                if (charBoth.Item1 != null && monBoth.Item1 != null)
                {
                    float charDist = Vector3.Distance(transform.position, charBoth.Item2.transform.position);
                    float monDist = Vector3.Distance(transform.position, monBoth.Item2.transform.position);
                    
                    // 더 가까운 타겟 선택
                    if (charDist < monDist)
                    {
                        newTarget = charBoth.Item1;
                        targetObject = charBoth.Item2;
                        targetName = charBoth.Item3;
                    }
                    else
                    {
                        newTarget = monBoth.Item1;
                        targetObject = monBoth.Item2;
                        targetName = monBoth.Item3;
                    }
                }
                // 하나만 있으면 그것을 선택
                else if (charBoth.Item1 != null)
                {
                    newTarget = charBoth.Item1;
                    targetObject = charBoth.Item2;
                    targetName = charBoth.Item3;
                }
                else if (monBoth.Item1 != null)
                {
                    newTarget = monBoth.Item1;
                    targetObject = monBoth.Item2;
                    targetName = monBoth.Item3;
                }
                break;
                
            case AttackTargetType.CastleOnly:
                // 성만 공격 (몬스터 전용)
                var castleResult = FindCastleTargetInRange();
                newTarget = castleResult.Item1;
                targetName = castleResult.Item2;
                if (newTarget != null)
                {
                    // 성 타입에 따라 GameObject 참조 획득
                    MiddleCastle mc = newTarget as MiddleCastle;
                    FinalCastle fc = newTarget as FinalCastle;
                    targetObject = mc != null ? mc.gameObject : (fc != null ? fc.gameObject : null);
                }
                break;
                
            case AttackTargetType.All:
                // 모든 타입을 공격 가능 - 가장 가까운 타겟 선택
                var charAll = FindCharacterTargetInRange();
                var monAll = FindMonsterTargetInRange();
                var castleAll = FindCastleTargetInRange();
                
                float minDistance = float.MaxValue;
                
                // 캐릭터 거리 확인
                if (charAll.Item1 != null)
                {
                    float dist = Vector3.Distance(transform.position, charAll.Item2.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        newTarget = charAll.Item1;
                        targetObject = charAll.Item2;
                        targetName = charAll.Item3;
                    }
                }
                
                // 몬스터 거리 확인
                if (monAll.Item1 != null)
                {
                    float dist = Vector3.Distance(transform.position, monAll.Item2.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        newTarget = monAll.Item1;
                        targetObject = monAll.Item2;
                        targetName = monAll.Item3;
                    }
                }
                
                // 성 거리 확인
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
                            newTarget = castleAll.Item1;
                            targetObject = castleObj;
                            targetName = castleAll.Item2;
                        }
                    }
                }
                break;
        }
        
        // 타겟 변경 시에만 업데이트 (성능 최적화)
        if (newTarget != currentTarget)
        {
            currentTarget = newTarget;
            if (newTarget != null && targetObject != null)
            {
                Debug.Log($"[CharacterCombat] {character.characterName}의 새 타겟: {targetName}");
            }
        }
    }
    
    /// <summary>
    /// 범위 내 캐릭터 타겟 찾기
    /// </summary>
    private (IDamageable, GameObject, string) FindCharacterTargetInRange()
    {
        Character closestTarget = null;
        float closestDistance = float.MaxValue;
        
        Character[] allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        foreach (var target in allCharacters)
        {
            if (target == null || target == character) continue;
            if (target.currentHP <= 0) continue;
            if (target.areaIndex == character.areaIndex) continue; // 같은 지역 캐릭터는 공격하지 않음
            if (target.isHero) continue; // 히어로는 공격하지 않음
            
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
        Monster closestTarget = null;
        float closestDistance = float.MaxValue;
        
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
            if (castle.IsDestroyed()) continue; // 파괴된 성은 무시
            
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
            if (castle.IsDestroyed()) continue; // 파괴된 성은 무시
            
            float distance = Vector3.Distance(transform.position, castle.transform.position);
            if (distance <= character.attackRange)
            {
                return (castle, "최종성");
            }
        }
        
        return (null, "");
    }
    
    /// <summary>
    /// 공격 가능 여부 확인
    /// </summary>
    private bool CanAttack()
    {
        if (character.currentHP <= 0) return false;
        if (Time.time - lastAttackTime < (1f / character.attackSpeed)) return false;
        
        return true;
    }
    
    /// <summary>
    /// 공격 실행
    /// </summary>
    private void Attack()
    {
        if (currentTarget == null) return;
        
        lastAttackTime = Time.time;
        isAttacking = true;
        
        // 범위 공격 처리
        if (character.isAreaAttack)
        {
            PerformAreaAttack();
        }
        else
        {
            // 단일 타겟 공격
            if (projectilePrefab != null)
            {
                // 투사체 발사
                FireProjectile();
            }
            else
            {
                // 즉시 데미지
                currentTarget.TakeDamage(character.attackPower);
                PlayAttackAnimation();
            }
        }
        
        // 공격 후 처리
        StartCoroutine(AttackCooldown());
    }
    
    /// <summary>
    /// 투사체 발사
    /// </summary>
    private void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        
        // Bullet 컴포넌트 설정
        Bullet bullet = projectile.GetComponent<Bullet>();
        if (bullet == null)
        {
            bullet = projectile.AddComponent<Bullet>();
        }
        
        // 타겟 GameObject 찾기
        GameObject targetObj = null;
        if (currentTarget is Character)
        {
            targetObj = ((Character)currentTarget).gameObject;
        }
        else if (currentTarget is Monster)
        {
            targetObj = ((Monster)currentTarget).gameObject;
        }
        else if (currentTarget is MiddleCastle)
        {
            targetObj = ((MiddleCastle)currentTarget).gameObject;
        }
        else if (currentTarget is FinalCastle)
        {
            targetObj = ((FinalCastle)currentTarget).gameObject;
        }
        
        // 총알 초기화
        bullet.Initialize(character.attackPower, 10f, targetObj, character.areaIndex, false);
        
        PlayAttackAnimation();
    }
    
    /// <summary>
    /// 범위 공격 실행
    /// </summary>
    private void PerformAreaAttack()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, character.areaAttackRadius);
        
        foreach (var collider in colliders)
        {
            if (collider == null) continue;
            
            // 공격 타입에 따라 처리
            switch (character.attackTargetType)
            {
                case AttackTargetType.Character:
                case AttackTargetType.Both:
                case AttackTargetType.All:
                    Character targetChar = collider.GetComponent<Character>();
                    if (targetChar != null && targetChar != character && 
                        targetChar.areaIndex != character.areaIndex && !targetChar.isHero)
                    {
                        targetChar.TakeDamage(character.attackPower);
                    }
                    break;
            }
            
            switch (character.attackTargetType)
            {
                case AttackTargetType.Monster:
                case AttackTargetType.Both:
                case AttackTargetType.All:
                    Monster monster = collider.GetComponent<Monster>();
                    if (monster != null && monster.areaIndex != character.areaIndex)
                    {
                        monster.TakeDamage(character.attackPower);
                    }
                    break;
            }
            
            switch (character.attackTargetType)
            {
                case AttackTargetType.CastleOnly:
                case AttackTargetType.All:
                    MiddleCastle middleCastle = collider.GetComponent<MiddleCastle>();
                    if (middleCastle != null && middleCastle.areaIndex != character.areaIndex && 
                        !middleCastle.IsDestroyed())
                    {
                        middleCastle.TakeDamage(character.attackPower);
                    }
                    
                    FinalCastle finalCastle = collider.GetComponent<FinalCastle>();
                    if (finalCastle != null && finalCastle.areaIndex != character.areaIndex && 
                        !finalCastle.IsDestroyed())
                    {
                        finalCastle.TakeDamage(character.attackPower);
                    }
                    break;
            }
        }
        
        PlayAttackAnimation();
    }
    
    /// <summary>
    /// 공격 쿨다운
    /// </summary>
    private IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
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