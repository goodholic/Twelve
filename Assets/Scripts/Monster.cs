using System;
using UnityEngine;
using UnityEngine.UI;

public class Monster : MonoBehaviour, IDamageable
{
    [Header("Monster Stats")]
    public float moveSpeed = 0.7f;  // 1.0f → 0.7f로 속도 더 감소
    public float health = 50f;

    [Header("Monster Size")]
    [Tooltip("몬스터 크기 배율 (0.3 = 30% 크기)")]
    public float sizeScale = 0.3f;  // 30% 크기로 축소

    [Header("Waypoint Path (2D)")]
    public Transform[] pathWaypoints;

    [Header("Castle Attack Damage")]
    public int damageToCastle = 10;  // 기본값을 1에서 10으로 변경

    [Header("챕터 설정")]
    [Tooltip("몬스터의 현재 챕터 (기본값: 1)")]
    public int currentChapter = 1;
    [Tooltip("챕터당 스탯 증가 비율 (1.05 = 5% 증가)")]
    public float chapterStatMultiplier = 1.05f;  // 기존 1.1f → 1.05f로 감소

    public event Action OnDeath;
    public event Action<Monster> OnReachedCastle;

    private int currentWaypointIndex = 0;
    private bool isDead = false;
    private bool hasReachedEnd = false;  // 추가: 중복 처리 방지

    // 상태 효과 (슬로우/출혈/스턴)
    private float slowDuration = 0f;
    private float slowAmount = 0f;
    private float originalMoveSpeed;
    private float bleedDuration = 0f;
    private float bleedDamagePerSec = 0f;
    private float stunDuration = 0f;
    private bool isStunned = false;

    [Header("Overhead HP Bar (머리 위 체력바)")]
    public Canvas hpBarCanvas;
    public Image hpFillImage;
    private float maxHealth;

    [Header("Area 구분 (1 or 2)")]
    public int areaIndex = 1;
    
    // [추가] 중간성과 최종성 타겟팅을 위한 변수
    private bool isTargetingCastle = false;
    private MiddleCastle targetMiddleCastle;
    private FinalCastle targetFinalCastle;
    private RouteType monsterRoute = RouteType.Center;  // 몬스터가 속한 라인

    private void Awake()
    {
        // 몬스터 크기 조정
        transform.localScale = Vector3.one * sizeScale;
        Debug.Log($"[Monster] {gameObject.name} 크기 설정: {sizeScale * 100}%");

        // 챕터에 따른 스탯 증가 적용
        float multiplier = Mathf.Pow(chapterStatMultiplier, currentChapter - 1);
        health *= multiplier;
        damageToCastle = Mathf.RoundToInt(damageToCastle * multiplier);
        
        Debug.Log($"[Monster] 챕터 {currentChapter} 몬스터 - 체력: {health}, 성 데미지: {damageToCastle}");
        
        maxHealth = health;
        originalMoveSpeed = moveSpeed;

        // HP바 초기화
        UpdateHpBar();
    }

    private void Update()
    {
        if (isDead || hasReachedEnd) return;

        // 상태 효과 업데이트
        UpdateStatusEffects();

        // 스턴 상태면 이동 안함
        if (isStunned) return;

        // 성을 타겟팅 중이면 성으로 이동
        if (isTargetingCastle)
        {
            MoveTowardsCastle();
        }
        else
        {
            // 일반 웨이포인트 이동
            MoveAlongPath();
        }
    }

    /// <summary>
    /// 몬스터가 속한 라인 설정
    /// </summary>
    public void SetMonsterRoute(RouteType route)
    {
        monsterRoute = route;
        Debug.Log($"[Monster] {gameObject.name} 라인 설정: {route}");
    }

    private void UpdateStatusEffects()
    {
        // 슬로우 효과
        if (slowDuration > 0)
        {
            slowDuration -= Time.deltaTime;
            if (slowDuration <= 0)
            {
                moveSpeed = originalMoveSpeed;
                slowAmount = 0f;
                Debug.Log($"[Monster] {gameObject.name} 슬로우 효과 종료");
            }
        }

        // 출혈 효과
        if (bleedDuration > 0)
        {
            bleedDuration -= Time.deltaTime;
            TakeDamage(bleedDamagePerSec * Time.deltaTime);
            
            if (bleedDuration <= 0)
            {
                bleedDamagePerSec = 0f;
                Debug.Log($"[Monster] {gameObject.name} 출혈 효과 종료");
            }
        }

        // 스턴 효과
        if (stunDuration > 0)
        {
            stunDuration -= Time.deltaTime;
            if (stunDuration <= 0)
            {
                isStunned = false;
                Debug.Log($"[Monster] {gameObject.name} 스턴 효과 종료");
            }
        }
    }

    private void UpdateHpBar()
    {
        if (hpFillImage != null)
        {
            float ratio = health / maxHealth;
            hpFillImage.fillAmount = ratio;

            // 체력에 따른 색상 변경
            if (ratio > 0.6f)
                hpFillImage.color = Color.green;
            else if (ratio > 0.3f)
                hpFillImage.color = Color.yellow;
            else
                hpFillImage.color = Color.red;
        }
    }

    private void MoveAlongPath()
    {
        if (pathWaypoints == null || pathWaypoints.Length == 0)
        {
            Debug.LogWarning($"[Monster] {gameObject.name}의 웨이포인트가 설정되지 않았습니다!");
            return;
        }

        if (currentWaypointIndex >= pathWaypoints.Length)
        {
            // 웨이포인트 끝에 도달 -> 중간성으로 전환
            SwitchToMiddleCastleTarget();
            return;
        }

        Transform target = pathWaypoints[currentWaypointIndex];
        if (target == null)
        {
            Debug.LogWarning($"[Monster] 웨이포인트[{currentWaypointIndex}]가 null입니다!");
            currentWaypointIndex++;
            return;
        }

        Vector2 currentPos = transform.position;
        Vector2 targetPos = target.position;
        Vector2 dir = (targetPos - currentPos).normalized;

        transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);

        float dist = Vector2.Distance(currentPos, targetPos);
        if (dist < 0.1f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= pathWaypoints.Length)
            {
                // 웨이포인트 끝 -> 중간성으로 전환
                SwitchToMiddleCastleTarget();
            }
        }
    }

    /// <summary>
    /// 웨이포인트가 끝나면 해당 라인의 중간성을 타겟으로 설정
    /// </summary>
    private void SwitchToMiddleCastleTarget()
    {
        if (isTargetingCastle || hasReachedEnd || isDead) return;
        
        // 해당 라인의 중간성 찾기
        MiddleCastle[] middleCastles = UnityEngine.Object.FindObjectsByType<MiddleCastle>(FindObjectsSortMode.None);
        foreach (var castle in middleCastles)
        {
            if (castle != null && castle.areaIndex == areaIndex && castle.routeType == monsterRoute)
            {
                if (!castle.IsDestroyed())
                {
                    targetMiddleCastle = castle;
                    isTargetingCastle = true;
                    Debug.Log($"[Monster] {gameObject.name} 중간성 타겟팅 시작: {castle.gameObject.name} (지역{castle.areaIndex})");
                    return;
                }
            }
        }
        
        // 중간성이 이미 파괴되었거나 없으면 최종성으로
        SwitchToFinalCastleTarget();
    }

    /// <summary>
    /// 중간성이 파괴되면 최종성을 타겟으로 설정
    /// </summary>
    private void SwitchToFinalCastleTarget()
    {
        if (targetFinalCastle != null || hasReachedEnd || isDead) return;
        
        FinalCastle[] finalCastles = UnityEngine.Object.FindObjectsByType<FinalCastle>(FindObjectsSortMode.None);
        foreach (var castle in finalCastles)
        {
            if (castle != null && castle.areaIndex == areaIndex && !castle.IsDestroyed())
            {
                targetFinalCastle = castle;
                targetMiddleCastle = null;
                isTargetingCastle = true;
                Debug.Log($"[Monster] {gameObject.name} 최종성 타겟팅 시작: {castle.gameObject.name} (지역{castle.areaIndex})");
                return;
            }
        }
        
        // 최종성도 없으면 몬스터 제거
        Debug.LogWarning($"[Monster] {gameObject.name} 타겟 성을 찾을 수 없음. 몬스터 제거.");
        Die();
    }

    /// <summary>
    /// 성을 향해 이동
    /// </summary>
    private void MoveTowardsCastle()
    {
        if (hasReachedEnd || isDead) return;
        
        GameObject targetCastle = null;
        
        if (targetMiddleCastle != null && !targetMiddleCastle.IsDestroyed())
        {
            targetCastle = targetMiddleCastle.gameObject;
        }
        else if (targetFinalCastle != null && !targetFinalCastle.IsDestroyed())
        {
            targetCastle = targetFinalCastle.gameObject;
        }
        else
        {
            // 타겟이 없으면 다시 찾기
            if (targetMiddleCastle != null && targetMiddleCastle.IsDestroyed())
            {
                SwitchToFinalCastleTarget();
            }
            return;
        }
        
        Vector2 currentPos = transform.position;
        Vector2 targetPos = targetCastle.transform.position;
        Vector2 dir = (targetPos - currentPos).normalized;
        
        transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
        
        float dist = Vector2.Distance(currentPos, targetPos);
        if (dist < 0.5f)  // 성에 도달
        {
            OnReachCastle();
        }
    }

    private void OnReachCastle()
    {
        if (hasReachedEnd || isDead) return;
        hasReachedEnd = true;  // 중복 처리 방지
        
        Debug.Log($"[Monster] {gameObject.name} 성에 도달! (지역{areaIndex})");
        
        // 성에 데미지 주기
        bool damageDealt = false;
        
        if (targetMiddleCastle != null && !targetMiddleCastle.IsDestroyed())
        {
            targetMiddleCastle.TakeDamage(damageToCastle);
            Debug.Log($"[Monster] {gameObject.name}이 중간성 공격! 데미지: {damageToCastle} (지역{targetMiddleCastle.areaIndex})");
            damageDealt = true;
        }
        else if (targetFinalCastle != null && !targetFinalCastle.IsDestroyed())
        {
            targetFinalCastle.TakeDamage(damageToCastle);
            Debug.Log($"[Monster] {gameObject.name}이 최종성 공격! 데미지: {damageToCastle} (지역{targetFinalCastle.areaIndex})");
            damageDealt = true;
            
            // 최종성을 공격한 경우 GameManager에도 알림
            if (targetFinalCastle.areaIndex == 1)
            {
                // 지역1 최종성 공격
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TakeDamageToRegion1(damageToCastle);
                }
            }
            else if (targetFinalCastle.areaIndex == 2)
            {
                // 지역2 최종성 공격
                WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
                if (spawner2 != null)
                {
                    spawner2.TakeDamageToRegion2(damageToCastle);
                }
            }
        }
        
        if (!damageDealt)
        {
            Debug.LogWarning($"[Monster] {gameObject.name} 성에 도달했지만 타겟을 찾을 수 없음");
        }
        
        // 이벤트 호출 후 죽음 처리
        OnReachedCastle?.Invoke(this);
        Die();
    }

    // OnReachEndPoint 메서드는 제거하거나 다음과 같이 수정
    private void OnReachEndPoint()
    {
        // 이미 처리됨
        if (hasReachedEnd || isDead) return;
        
        Debug.LogWarning($"[Monster] {gameObject.name} OnReachEndPoint 호출됨 (구 버전 호환)");
        
        // 중간성이나 최종성 타겟팅으로 전환
        if (!isTargetingCastle)
        {
            SwitchToMiddleCastleTarget();
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        health -= damageAmount;
        UpdateHpBar();

        if (health <= 0f)
        {
            Die();
        }
    }

    public void TakeDamage(float damageAmount, GameObject source)
    {
        if (source != null)
        {
            Debug.Log($"[Monster] {gameObject.name}이(가) {source.name}에게 {damageAmount} 데미지 받음");
        }
        TakeDamage(damageAmount);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[Monster] {gameObject.name} 사망!");

        // OnDeath 이벤트 호출
        OnDeath?.Invoke();

        // HP바 캔버스 제거
        if (hpBarCanvas != null)
        {
            Destroy(hpBarCanvas.gameObject);
        }

        // 즉시 파괴
        Destroy(gameObject);
    }

    public void ApplySlow(float amount, float duration)
    {
        if (amount > slowAmount || (amount == slowAmount && duration > slowDuration))
        {
            slowAmount = Mathf.Clamp01(amount);
            slowDuration = duration;
            moveSpeed = originalMoveSpeed * (1f - slowAmount);
            Debug.Log($"[Monster] {gameObject.name} 슬로우 {slowAmount * 100}% 적용 (지속 {duration}초)");
        }
    }

    public void ApplyBleed(float damagePerSec, float duration)
    {
        if (damagePerSec > bleedDamagePerSec || (damagePerSec == bleedDamagePerSec && duration > bleedDuration))
        {
            bleedDamagePerSec = damagePerSec;
            bleedDuration = duration;
            Debug.Log($"[Monster] {gameObject.name} 출혈 초당 {damagePerSec} 적용 (지속 {duration}초)");
        }
    }

    public void ApplyStun(float duration)
    {
        if (duration > stunDuration)
        {
            stunDuration = duration;
            isStunned = true;
            Debug.Log($"[Monster] {gameObject.name} 스턴 적용 (지속 {duration}초)");
        }
    }
}