using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 월드 좌표 기반 몬스터 클래스
/// 웨이브로 생성되어 웨이포인트를 따라 이동합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Monster : MonoBehaviour, IDamageable
{
    [Header("Monster Stats")]
    public string monsterName = "Monster";
    public float moveSpeed = 0.7f;
    public float health = 50f;
    public float currentHealth;
    
    [Header("Monster Visual")]
    public Sprite monsterSprite;
    [Tooltip("몬스터 크기 배율 (0.3 = 30% 크기)")]
    public float sizeScale = 0.3f;
    
    [Header("Waypoint Path")]
    public Transform[] pathWaypoints;
    private int currentWaypointIndex = 0;
    
    [Header("Castle Attack")]
    public int damageToCastle = 10;
    public float attackRange = 1.5f; // 성 공격 사거리
    public float attackCooldown = 1f; // 공격 쿨다운
    private float lastAttackTime = 0f;
    
    [Header("챕터 설정")]
    [Tooltip("몬스터의 현재 챕터 (기본값: 1)")]
    public int currentChapter = 1;
    [Tooltip("챕터당 스탯 증가 비율 (1.05 = 5% 증가)")]
    public float chapterStatMultiplier = 1.05f;
    
    [Header("Area 구분")]
    public int areaIndex = 1;
    
    [Header("효과 프리팹")]
    public GameObject hitEffectPrefab;
    public GameObject deathEffectPrefab;
    
    // 이벤트
    public event Action OnDeath;
    public event Action<Monster> OnReachedCastle;
    
    // 컴포넌트
    private SpriteRenderer spriteRenderer;
    private Collider2D col2D;
    private Canvas hpBarCanvas;
    private Image hpBarFillImage;
    
    // 상태
    private bool isDead = false;
    private bool hasReachedEnd = false;
    private float maxHealth;
    private RouteType currentRoute = RouteType.Center;
    
    // 상태 효과
    private float slowDuration = 0f;
    private float slowAmount = 1f;
    private float originalMoveSpeed;
    private float bleedDuration = 0f;
    private float bleedDamagePerSec = 0f;
    private float stunDuration = 0f;
    private bool isStunned = false;
    
    // 코루틴
    private Coroutine moveCoroutine;
    private Coroutine bleedCoroutine;
    
    // 전투 관련
    private IDamageable currentTarget;
    private bool isAttacking = false;
    
    private void Awake()
    {
        // Collider 설정
        col2D = GetComponent<Collider2D>();
        if (col2D == null)
        {
            col2D = gameObject.AddComponent<CircleCollider2D>();
        }
        col2D.isTrigger = true;
        
        // SpriteRenderer 설정
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            GameObject spriteObj = new GameObject("Sprite");
            spriteObj.transform.SetParent(transform);
            spriteObj.transform.localPosition = Vector3.zero;
            spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerName = "Characters";
            spriteRenderer.sortingOrder = 5;
        }
        
        // Layer 설정
        gameObject.layer = LayerMask.NameToLayer("Monster");
        
        // 태그 설정
        gameObject.tag = areaIndex == 1 ? "Region1Monster" : "Region2Monster";
        
        // 초기 설정
        originalMoveSpeed = moveSpeed;
    }

    private void Start()
    {
        // 챕터에 따른 스탯 조정
        ApplyChapterMultiplier();
        
        maxHealth = health;
        currentHealth = health;
        
        // HP바 생성
        CreateHPBar();
        
        // 스프라이트 설정
        if (spriteRenderer != null && monsterSprite != null)
        {
            spriteRenderer.sprite = monsterSprite;
            transform.localScale = Vector3.one * sizeScale;
        }
        
        // 웨이포인트 경로 시작
        if (pathWaypoints != null && pathWaypoints.Length > 0)
        {
            StartMoving();
        }
    }

    private void Update()
    {
        // 스턴 상태 체크
        if (stunDuration > 0)
        {
            stunDuration -= Time.deltaTime;
            if (stunDuration <= 0)
            {
                isStunned = false;
                if (moveCoroutine == null && !isDead)
                {
                    StartMoving();
                }
            }
        }
        
        // 슬로우 상태 체크
        if (slowDuration > 0)
        {
            slowDuration -= Time.deltaTime;
            if (slowDuration <= 0)
            {
                moveSpeed = originalMoveSpeed;
            }
        }
        
        // 성 공격 체크
        CheckAndAttackCastle();
    }

    /// <summary>
    /// 챕터별 스탯 증가 적용
    /// </summary>
    private void ApplyChapterMultiplier()
    {
        float multiplier = Mathf.Pow(chapterStatMultiplier, currentChapter - 1);
        
        health *= multiplier;
        damageToCastle = Mathf.RoundToInt(damageToCastle * multiplier);
        
        Debug.Log($"[Monster] {monsterName} 챕터 {currentChapter} 보정 적용 - " +
                  $"체력: {health:F0}, 공격력: {damageToCastle}");
    }

    /// <summary>
    /// HP바 생성
    /// </summary>
    private void CreateHPBar()
    {
        GameObject hpBarPrefab = Resources.Load<GameObject>("Prefabs/MonsterHPBar");
        if (hpBarPrefab == null)
        {
            // HP바 수동 생성
            GameObject hpBarObj = new GameObject("HPBarCanvas");
            hpBarObj.transform.SetParent(transform);
            hpBarObj.transform.localPosition = new Vector3(0, 0.8f, 0);
            
            hpBarCanvas = hpBarObj.AddComponent<Canvas>();
            hpBarCanvas.renderMode = RenderMode.WorldSpace;
            hpBarCanvas.sortingLayerName = "UI";
            hpBarCanvas.sortingOrder = 100;
            
            RectTransform canvasRect = hpBarCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1f, 0.2f);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 1f);
            
            // 배경
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(hpBarObj.transform);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            RectTransform bgRect = bgImage.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            // 체력바
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(hpBarObj.transform);
            hpBarFillImage = fillObj.AddComponent<Image>();
            hpBarFillImage.color = Color.red;
            RectTransform fillRect = hpBarFillImage.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            hpBarFillImage.type = Image.Type.Filled;
            hpBarFillImage.fillMethod = Image.FillMethod.Horizontal;
            hpBarFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }

    /// <summary>
    /// 웨이포인트 이동 시작
    /// </summary>
    public void StartMoving()
    {
        if (moveCoroutine == null && !isDead && !isStunned)
        {
            moveCoroutine = StartCoroutine(MoveAlongPath());
        }
    }

    /// <summary>
    /// 웨이포인트 경로 이동
    /// </summary>
    private IEnumerator MoveAlongPath()
    {
        while (currentWaypointIndex < pathWaypoints.Length && !isDead)
        {
            if (isStunned || isAttacking)
            {
                yield return null;
                continue;
            }
            
            Transform targetWaypoint = pathWaypoints[currentWaypointIndex];
            
            while (Vector3.Distance(transform.position, targetWaypoint.position) > 0.1f)
            {
                if (isStunned || isDead || isAttacking)
                {
                    yield return null;
                    continue;
                }
                
                // 이동
                Vector3 direction = (targetWaypoint.position - transform.position).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;
                
                // 방향에 따른 스프라이트 반전
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = direction.x < 0;
                }
                
                yield return null;
            }
            
            currentWaypointIndex++;
        }
        
        // 경로 끝에 도달
        if (!hasReachedEnd && !isDead)
        {
            hasReachedEnd = true;
            OnReachEnd();
        }
        
        moveCoroutine = null;
    }

    /// <summary>
    /// 성 공격 체크 및 실행
    /// </summary>
    private void CheckAndAttackCastle()
    {
        if (isDead || isStunned) return;
        if (Time.time - lastAttackTime < attackCooldown) return;
        
        // 중간성 체크
        MiddleCastle[] middleCastles = FindObjectsByType<MiddleCastle>(FindObjectsSortMode.None);
        foreach (var castle in middleCastles)
        {
            if (castle == null) continue;
            if (castle.areaIndex == areaIndex) continue; // 같은 지역 성은 공격하지 않음
            
            float distance = Vector3.Distance(transform.position, castle.transform.position);
            if (distance <= attackRange)
            {
                AttackCastle(castle);
                return;
            }
        }
        
        // 최종성 체크
        FinalCastle[] finalCastles = FindObjectsByType<FinalCastle>(FindObjectsSortMode.None);
        foreach (var castle in finalCastles)
        {
            if (castle == null) continue;
            if (castle.areaIndex == areaIndex) continue; // 같은 지역 성은 공격하지 않음
            
            float distance = Vector3.Distance(transform.position, castle.transform.position);
            if (distance <= attackRange)
            {
                AttackCastle(castle);
                return;
            }
        }
    }

    /// <summary>
    /// 성 공격
    /// </summary>
    private void AttackCastle(IDamageable castle)
    {
        if (castle == null) return;
        
        currentTarget = castle;
        isAttacking = true;
        lastAttackTime = Time.time;
        
        // 공격 애니메이션 (필요시)
        // PlayAttackAnimation();
        
        // 데미지 적용
        castle.TakeDamage(damageToCastle);
        
        // 공격 이펙트
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
        
        Debug.Log($"[Monster] {monsterName}이(가) 성을 공격! 데미지: {damageToCastle}");
        
        // 공격 후 이동 재개
        StartCoroutine(ResumeMovingAfterAttack());
    }

    /// <summary>
    /// 공격 후 이동 재개
    /// </summary>
    private IEnumerator ResumeMovingAfterAttack()
    {
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
        
        if (!isDead && !isStunned && moveCoroutine == null)
        {
            StartMoving();
        }
    }

    /// <summary>
    /// 경로 끝 도달 처리
    /// </summary>
    private void OnReachEnd()
    {
        Debug.Log($"[Monster] {monsterName}이(가) 목적지에 도달했습니다!");
        
        // 이벤트 호출
        OnReachedCastle?.Invoke(this);
        
        // CastleHealthManager에 데미지 전달
        CastleHealthManager castleManager = FindFirstObjectByType<CastleHealthManager>();
        if (castleManager != null)
        {
            castleManager.TakeDamageToMidCastle(currentRoute, damageToCastle);
        }
        
        // 몬스터 제거
        Die();
    }

    /// <summary>
    /// 데미지 받기
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        
        // HP바 업데이트
        UpdateHPBar();
        
        // 피격 이펙트
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
        
        Debug.Log($"[Monster] {monsterName} 피격! 남은 체력: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// HP바 업데이트
    /// </summary>
    private void UpdateHPBar()
    {
        if (hpBarFillImage != null)
        {
            hpBarFillImage.fillAmount = currentHealth / maxHealth;
        }
    }

    /// <summary>
    /// 몬스터 사망 처리
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        isDead = true;
        
        Debug.Log($"[Monster] {monsterName} 사망!");
        
        // 사망 이펙트
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // 이벤트 호출
        OnDeath?.Invoke();
        
        // WaveSpawner에 알림
        WaveSpawner waveSpawner = FindFirstObjectByType<WaveSpawner>();
        if (waveSpawner != null)
        {
            waveSpawner.OnMonsterKilled(this);
        }
        
        // 코루틴 정리
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        
        if (bleedCoroutine != null)
        {
            StopCoroutine(bleedCoroutine);
            bleedCoroutine = null;
        }
        
        // 오브젝트 제거
        Destroy(gameObject);
    }

    /// <summary>
    /// 슬로우 효과 적용
    /// </summary>
    public void ApplySlow(float duration, float slowPercentage)
    {
        slowDuration = duration;
        slowAmount = 1f - (slowPercentage / 100f);
        moveSpeed = originalMoveSpeed * slowAmount;
        
        Debug.Log($"[Monster] {monsterName} 슬로우 적용! {slowPercentage}% 감속, {duration}초");
    }

    /// <summary>
    /// 출혈 효과 적용
    /// </summary>
    public void ApplyBleed(float duration, float damagePerSecond)
    {
        bleedDuration = duration;
        bleedDamagePerSec = damagePerSecond;
        
        if (bleedCoroutine != null)
        {
            StopCoroutine(bleedCoroutine);
        }
        
        bleedCoroutine = StartCoroutine(BleedEffect());
        
        Debug.Log($"[Monster] {monsterName} 출혈 적용! 초당 {damagePerSecond} 데미지, {duration}초");
    }

    /// <summary>
    /// 출혈 효과 코루틴
    /// </summary>
    private IEnumerator BleedEffect()
    {
        float elapsed = 0f;
        
        while (elapsed < bleedDuration && !isDead)
        {
            TakeDamage(bleedDamagePerSec);
            yield return new WaitForSeconds(1f);
            elapsed += 1f;
        }
        
        bleedCoroutine = null;
    }

    /// <summary>
    /// 스턴 효과 적용
    /// </summary>
    public void ApplyStun(float duration)
    {
        stunDuration = duration;
        isStunned = true;
        
        // 이동 중지
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        
        Debug.Log($"[Monster] {monsterName} 스턴 적용! {duration}초");
    }

    /// <summary>
    /// 웨이포인트 설정
    /// </summary>
    public void SetWaypoints(Transform[] waypoints, RouteType route)
    {
        pathWaypoints = waypoints;
        currentRoute = route;
        currentWaypointIndex = 0;
    }

    /// <summary>
    /// 현재 몬스터가 살아있는지 확인
    /// </summary>
    public bool IsAlive()
    {
        return !isDead && currentHealth > 0;
    }

    /// <summary>
    /// 디버그용 경로 그리기
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (pathWaypoints == null || pathWaypoints.Length == 0) return;
        
        Gizmos.color = Color.red;
        for (int i = 0; i < pathWaypoints.Length - 1; i++)
        {
            if (pathWaypoints[i] != null && pathWaypoints[i + 1] != null)
            {
                Gizmos.DrawLine(pathWaypoints[i].position, pathWaypoints[i + 1].position);
            }
        }
        
        // 공격 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    /// <summary>
    /// 몬스터 루트 설정 (WaveSpawner에서 호출)
    /// </summary>
    public void SetMonsterRoute(RouteType route)
    {
        currentRoute = route;
        Debug.Log($"[Monster] {monsterName} 루트 설정: {route}");
    }
}