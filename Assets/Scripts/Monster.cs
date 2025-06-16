using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 몬스터 클래스 - 웨이브로 생성되어 경로를 따라 이동하며 성을 공격
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
        gameObject.tag = areaIndex == 1 ? "Area1Monster" : "Area2Monster";
    }
    
    private void Start()
    {
        originalMoveSpeed = moveSpeed;
        InitializeMonster();
        CreateHealthBar();
        
        // 체력에 챕터 보너스 적용
        float chapterBonus = Mathf.Pow(chapterStatMultiplier, currentChapter - 1);
        health *= chapterBonus;
        currentHealth = health;
        maxHealth = health;
        
        UpdateHealthBar();
    }
    
    private void Update()
    {
        // 상태 효과 업데이트
        UpdateStatusEffects();
        
        // 성 공격 체크 (목적지에 도달하지 않은 경우에만)
        if (!isDead && !isStunned && !isAttacking && !hasReachedEnd)
        {
            FindAndAttackCastle();
        }
    }
    
    /// <summary>
    /// 몬스터 초기화
    /// </summary>
    private void InitializeMonster()
    {
        if (monsterSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = monsterSprite;
            
            // 크기 조정
            transform.localScale = Vector3.one * sizeScale;
        }
        
        currentHealth = health;
        maxHealth = health;
    }
    
    /// <summary>
    /// 체력바 생성
    /// </summary>
    private void CreateHealthBar()
    {
        // 체력바 캔버스 생성
        GameObject hpBarObj = new GameObject("HPBar");
        hpBarObj.transform.SetParent(transform);
        hpBarObj.transform.localPosition = new Vector3(0, 1.5f, 0);
        
        hpBarCanvas = hpBarObj.AddComponent<Canvas>();
        hpBarCanvas.renderMode = RenderMode.WorldSpace;
        hpBarCanvas.sortingLayerName = "UI";
        hpBarCanvas.sortingOrder = 10;
        
        // 체력바 배경
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(hpBarCanvas.transform);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(100, 10);
        
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        // 체력바 Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(bgObj.transform);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        hpBarFillImage = fillObj.AddComponent<Image>();
        hpBarFillImage.color = Color.green;
        hpBarFillImage.type = Image.Type.Filled;
        hpBarFillImage.fillMethod = Image.FillMethod.Horizontal;
        hpBarFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        
        // 캔버스 크기 조정
        hpBarCanvas.transform.localScale = Vector3.one * 0.01f;
    }
    
    /// <summary>
    /// 경로 설정
    /// </summary>
    public void SetPath(Transform[] waypoints, RouteType route)
    {
        pathWaypoints = waypoints;
        currentWaypointIndex = 0;
        currentRoute = route;
        
        if (waypoints != null && waypoints.Length > 0)
        {
            StartMoving();
        }
    }
    
    /// <summary>
    /// 챕터 설정
    /// </summary>
    public void SetChapter(int chapter)
    {
        currentChapter = Mathf.Max(1, chapter);
        
        // 체력 재계산
        float chapterBonus = Mathf.Pow(chapterStatMultiplier, currentChapter - 1);
        health = health * chapterBonus;
        currentHealth = health;
        maxHealth = health;
        
        UpdateHealthBar();
    }
    
    /// <summary>
    /// 이동 시작
    /// </summary>
    public void StartMoving()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        
        moveCoroutine = StartCoroutine(MoveAlongPath());
    }
    
    /// <summary>
    /// 경로를 따라 이동
    /// </summary>
    private IEnumerator MoveAlongPath()
    {
        while (currentWaypointIndex < pathWaypoints.Length && !isDead)
        {
            if (isStunned)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }
            
            Transform targetWaypoint = pathWaypoints[currentWaypointIndex];
            
            // 웨이포인트로 이동
            while (Vector3.Distance(transform.position, targetWaypoint.position) > 0.1f)
            {
                if (isDead || isStunned || isAttacking) break;
                
                // 이동 속도 계산 (슬로우 효과 적용)
                float currentSpeed = moveSpeed * slowAmount;
                
                // 이동
                Vector3 direction = (targetWaypoint.position - transform.position).normalized;
                transform.position += direction * currentSpeed * Time.deltaTime;
                
                yield return null;
            }
            
            if (!isDead && !isStunned && !isAttacking)
            {
                currentWaypointIndex++;
            }
            
            yield return null;
        }
        
        // 경로 끝에 도달
        if (!isDead && currentWaypointIndex >= pathWaypoints.Length)
        {
            OnReachEnd();
        }
    }
    
    /// <summary>
    /// 근처 성 찾아서 공격
    /// </summary>
    private void FindAndAttackCastle()
    {
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
    /// 경로 끝 도달 처리 - 성을 직접 찾아서 공격
    /// </summary>
    private void OnReachEnd()
    {
        Debug.Log($"[Monster] {monsterName}이(가) 목적지에 도달했습니다!");
        hasReachedEnd = true;
        
        // 가장 가까운 성 찾기
        bool foundCastle = false;
        float closestDistance = float.MaxValue;
        IDamageable closestCastle = null;
        string castleType = "";
        
        // 중간성 찾기
        MiddleCastle[] middleCastles = FindObjectsByType<MiddleCastle>(FindObjectsSortMode.None);
        foreach (var castle in middleCastles)
        {
            if (castle == null || castle.IsDestroyed()) continue;
            if (castle.areaIndex == areaIndex) continue; // 같은 지역 성은 공격하지 않음
            
            // 자신의 루트에 해당하는 중간성만 공격
            if (castle.routeType == currentRoute)
            {
                float distance = Vector3.Distance(transform.position, castle.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCastle = castle;
                    castleType = "중간성";
                    foundCastle = true;
                }
            }
        }
        
        // 중간성을 못 찾았거나 파괴된 경우 최종성 찾기
        if (!foundCastle)
        {
            FinalCastle[] finalCastles = FindObjectsByType<FinalCastle>(FindObjectsSortMode.None);
            foreach (var castle in finalCastles)
            {
                if (castle == null || castle.IsDestroyed()) continue;
                if (castle.areaIndex == areaIndex) continue; // 같은 지역 성은 공격하지 않음
                
                float distance = Vector3.Distance(transform.position, castle.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCastle = castle;
                    castleType = "최종성";
                    foundCastle = true;
                }
            }
        }
        
        // 찾은 성 공격
        if (foundCastle && closestCastle != null)
        {
            Debug.Log($"[Monster] {monsterName}이(가) {castleType}을 공격합니다!");
            closestCastle.TakeDamage(damageToCastle);
        }
        
        OnReachedCastle?.Invoke(this);
        
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
        
        // 피격 이펙트
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
        
        UpdateHealthBar();
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 체력바 업데이트
    /// </summary>
    private void UpdateHealthBar()
    {
        if (hpBarFillImage != null)
        {
            float ratio = currentHealth / maxHealth;
            hpBarFillImage.fillAmount = ratio;
            
            // 체력에 따른 색상 변경
            if (ratio > 0.6f)
                hpBarFillImage.color = Color.green;
            else if (ratio > 0.3f)
                hpBarFillImage.color = Color.yellow;
            else
                hpBarFillImage.color = Color.red;
        }
    }
    
    /// <summary>
    /// 상태 효과 업데이트
    /// </summary>
    private void UpdateStatusEffects()
    {
        // 슬로우 효과
        if (slowDuration > 0)
        {
            slowDuration -= Time.deltaTime;
            if (slowDuration <= 0)
            {
                slowAmount = 1f;
            }
        }
        
        // 스턴 효과
        if (stunDuration > 0)
        {
            stunDuration -= Time.deltaTime;
            if (stunDuration <= 0)
            {
                isStunned = false;
                // 이동 재개
                if (!isDead && moveCoroutine == null)
                {
                    StartMoving();
                }
            }
        }
        
        // 출혈 효과는 코루틴에서 처리
    }
    
    /// <summary>
    /// 슬로우 효과 적용
    /// </summary>
    public void ApplySlow(float slowPercentage, float duration)
    {
        slowAmount = 1f - (slowPercentage / 100f);
        slowDuration = duration;
        
        Debug.Log($"[Monster] {monsterName}에게 슬로우 {slowPercentage}% 적용 ({duration}초)");
    }
    
    /// <summary>
    /// 스턴 효과 적용
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (isDead) return;
        
        isStunned = true;
        stunDuration = duration;
        
        // 이동 중지
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        
        Debug.Log($"[Monster] {monsterName}에게 스턴 {duration}초 적용");
    }
    
    /// <summary>
    /// 출혈 효과 적용
    /// </summary>
    public void ApplyBleed(float damagePerSec, float duration)
    {
        if (isDead) return;
        
        bleedDamagePerSec = damagePerSec;
        bleedDuration = duration;
        
        if (bleedCoroutine != null)
        {
            StopCoroutine(bleedCoroutine);
        }
        
        bleedCoroutine = StartCoroutine(BleedCoroutine());
        
        Debug.Log($"[Monster] {monsterName}에게 출혈 효과 적용 (초당 {damagePerSec} 데미지, {duration}초)");
    }
    
    /// <summary>
    /// 출혈 코루틴
    /// </summary>
    private IEnumerator BleedCoroutine()
    {
        float elapsed = 0f;
        
        while (elapsed < bleedDuration && !isDead)
        {
            TakeDamage(bleedDamagePerSec);
            
            // 출혈 이펙트
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                effect.transform.localScale = Vector3.one * 0.5f;
                
                // 빨간색으로 변경
                SpriteRenderer effectSprite = effect.GetComponent<SpriteRenderer>();
                if (effectSprite != null)
                {
                    effectSprite.color = Color.red;
                }
                
                Destroy(effect, 0.5f);
            }
            
            yield return new WaitForSeconds(1f);
            elapsed += 1f;
        }
        
        bleedDuration = 0f;
        bleedDamagePerSec = 0f;
    }
    
    /// <summary>
    /// 몬스터 사망
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        Debug.Log($"[Monster] {monsterName} 사망");
        
        // 이동 중지
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        
        // 출혈 중지
        if (bleedCoroutine != null)
        {
            StopCoroutine(bleedCoroutine);
            bleedCoroutine = null;
        }
        
        // 사망 이펙트
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // 이벤트 발생
        OnDeath?.Invoke();
        
        // 오브젝트 제거
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 몬스터가 살아있는지 확인
    /// </summary>
    public bool IsAlive()
    {
        return !isDead && currentHealth > 0;
    }
    
    /// <summary>
    /// 디버그용 기즈모
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 공격 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // 경로 표시
        if (pathWaypoints != null && pathWaypoints.Length > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < pathWaypoints.Length - 1; i++)
            {
                if (pathWaypoints[i] != null && pathWaypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(pathWaypoints[i].position, pathWaypoints[i + 1].position);
                }
            }
        }
    }

}