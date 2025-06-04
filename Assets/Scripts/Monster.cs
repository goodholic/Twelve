using System;
using UnityEngine;
using Fusion;
using UnityEngine.UI;

public class Monster : NetworkBehaviour, IDamageable
{
    [Header("Monster Stats")]
    public float moveSpeed = 1.5f;  // 기존 3f → 1.5f로 속도 감소
    public float health = 50f;

    [Header("Monster Size")]
    [Tooltip("몬스터 크기 배율 (0.3 = 30% 크기)")]
    public float sizeScale = 0.3f;  // 30% 크기로 축소

    [Header("Waypoint Path (2D)")]
    public Transform[] pathWaypoints;

    [Header("Castle Attack Damage")]
    public int damageToCastle = 1;

    [Header("챕터 설정")]
    [Tooltip("몬스터의 현재 챕터 (기본값: 1)")]
    public int currentChapter = 1;
    [Tooltip("챕터당 스탯 증가 비율 (1.05 = 5% 증가)")]
    public float chapterStatMultiplier = 1.05f;  // 기존 1.1f → 1.05f로 감소

    public event Action OnDeath;
    public event Action<Monster> OnReachedCastle;

    private int currentWaypointIndex = 0;
    private bool isDead = false;

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
        if (currentChapter > 1)
        {
            ApplyChapterStatBonus();
        }

        originalMoveSpeed = moveSpeed;
        maxHealth = health;

        if (hpBarCanvas == null || hpFillImage == null)
        {
            Debug.LogWarning($"[Monster] HP Bar Canvas 또는 Fill Image 미연결 ( {name} )");
        }
        else
        {
            hpBarCanvas.gameObject.SetActive(true);
            UpdateHpBar();
            
            // HP바도 몬스터 크기에 맞춰 조정
            if (hpBarCanvas != null)
            {
                hpBarCanvas.transform.localScale = Vector3.one * (1f / sizeScale);  // 역배율로 보정
            }
        }
    }

    /// <summary>
    /// 몬스터가 어느 라인을 따라가는지 설정
    /// </summary>
    public void SetMonsterRoute(RouteType route)
    {
        monsterRoute = route;
        Debug.Log($"[Monster] {gameObject.name} 라인 설정: {route}");
    }

    /// <summary>
    /// 챕터에 따라 몬스터 스탯을 강화합니다 (1.05배씩 증가)
    /// </summary>
    private void ApplyChapterStatBonus()
    {
        // 1챕터 이상일 때만 계산
        if (currentChapter <= 1) return;

        // 몇 번 강화할지 계산 (2챕터=1번, 3챕터=2번, ...)
        int upgradeCount = currentChapter - 1;
        
        // 각 스탯에 대해 (upgradeCount)번 곱하기
        float multiplier = Mathf.Pow(chapterStatMultiplier, upgradeCount);
        
        // 스탯 적용
        health *= multiplier;
        moveSpeed *= multiplier;
        damageToCastle = Mathf.RoundToInt(damageToCastle * multiplier);
        
        // 속도는 너무 빨라지지 않도록 제한
        moveSpeed = Mathf.Min(moveSpeed, 3f);  // 최대 속도 3f로 제한
        
        Debug.Log($"[Monster] 챕터 {currentChapter}에 따른 스탯 증가: 배율 {multiplier:F2}배 " +
                 $"(체력: {health:F1}, 이동속도: {moveSpeed:F2}, 성 공격력: {damageToCastle})");
    }

    private void Update()
    {
        if (isDead) return;
        UpdateStatusEffects();

        if (!isStunned)
        {
            if (isTargetingCastle)
            {
                MoveTowardsCastle();
            }
            else
            {
                MoveAlongPath2D();
            }
        }
    }

    private void LateUpdate()
    {
        // HP 바 위치 보정(머리 위)
        if (hpBarCanvas != null && hpBarCanvas.transform.parent == null)
        {
            Vector3 offset = new Vector3(0f, 0.4f * sizeScale, 0f);  // 크기에 맞춰 오프셋 조정
            hpBarCanvas.transform.position = transform.position + offset;
        }
    }

    private void UpdateStatusEffects()
    {
        // 슬로우
        if (slowDuration > 0)
        {
            slowDuration -= Time.deltaTime;
            if (slowDuration <= 0)
            {
                moveSpeed = originalMoveSpeed;
                slowAmount = 0f;
            }
        }

        // 출혈
        if (bleedDuration > 0)
        {
            bleedDuration -= Time.deltaTime;
            if (bleedDamagePerSec > 0)
            {
                TakeDamage(bleedDamagePerSec * Time.deltaTime);
            }
            if (bleedDuration <= 0)
            {
                bleedDamagePerSec = 0f;
            }
        }

        // 스턴
        if (stunDuration > 0)
        {
            stunDuration -= Time.deltaTime;
            isStunned = true;
            if (stunDuration <= 0)
            {
                isStunned = false;
            }
        }
    }

    private void MoveAlongPath2D()
    {
        if (pathWaypoints == null || pathWaypoints.Length == 0 || currentWaypointIndex >= pathWaypoints.Length)
        {
            // 웨이포인트가 끝났으면 해당 라인의 중간성으로 이동
            SwitchToMiddleCastleTarget();
            return;
        }

        Transform target = pathWaypoints[currentWaypointIndex];
        if (target == null)
        {
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
        if (isTargetingCastle) return;
        
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
                    Debug.Log($"[Monster] {gameObject.name} 중간성 타겟팅 시작: {castle.gameObject.name}");
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
        if (targetFinalCastle != null) return;
        
        FinalCastle[] finalCastles = UnityEngine.Object.FindObjectsByType<FinalCastle>(FindObjectsSortMode.None);
        foreach (var castle in finalCastles)
        {
            if (castle != null && castle.areaIndex == areaIndex && !castle.IsDestroyed())
            {
                targetFinalCastle = castle;
                targetMiddleCastle = null;
                isTargetingCastle = true;
                Debug.Log($"[Monster] {gameObject.name} 최종성 타겟팅 시작: {castle.gameObject.name}");
                return;
            }
        }
    }

    /// <summary>
    /// 성을 향해 이동
    /// </summary>
    private void MoveTowardsCastle()
    {
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
        if (targetMiddleCastle != null && !targetMiddleCastle.IsDestroyed())
        {
            targetMiddleCastle.TakeDamage(damageToCastle);
            Debug.Log($"[Monster] {gameObject.name}이 중간성 공격! 데미지: {damageToCastle}");
        }
        else if (targetFinalCastle != null && !targetFinalCastle.IsDestroyed())
        {
            targetFinalCastle.TakeDamage(damageToCastle);
            Debug.Log($"[Monster] {gameObject.name}이 최종성 공격! 데미지: {damageToCastle}");
        }
        
        Die();
    }

    private void OnReachEndPoint()
    {
        // 기존 코드 유지 (웨이포인트 끝 도달 시)
        OnReachedCastle?.Invoke(this);
        OnDeath?.Invoke();

        if (areaIndex == 1)
        {
            // 지역1 캐슬
            CastleHealthManager.Instance?.TakeDamage(damageToCastle);
        }
        else if (areaIndex == 2)
        {
            // 지역2 체력 감소
            var wave2 = FindFirstObjectByType<WaveSpawnerRegion2>();
            if (wave2 != null)
            {
                wave2.TakeDamageToRegion2(damageToCastle);
            }
        }

        if (Object != null && Object.IsValid)
        {
            Runner.Despawn(Object);
        }
        else
        {
            Destroy(gameObject);
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

        OnDeath?.Invoke();

        if (Object != null && Object.IsValid)
        {
            Runner.Despawn(Object);
        }
        else
        {
            Destroy(gameObject);
        }
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

    public void ApplyBleed(float damagePerSecond, float duration)
    {
        if (damagePerSecond > bleedDamagePerSec ||
            (damagePerSecond == bleedDamagePerSec && duration > bleedDuration))
        {
            bleedDamagePerSec = damagePerSecond;
            bleedDuration = duration;
            Debug.Log($"[Monster] {gameObject.name} 출혈(초당 {damagePerSecond}), {duration}초");
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

    private void UpdateHpBar()
    {
        if (hpFillImage == null) return;
        float ratio = Mathf.Clamp01(health / maxHealth);
        hpFillImage.fillAmount = ratio;
    }
}