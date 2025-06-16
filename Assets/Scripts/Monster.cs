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
        gameObject.tag = areaIndex == 1 ? "Monster" : "EnemyMonster";
    }
    
    private void Start()
    {
        Initialize();
        CreateHPBarWorldSpace();
        
        if (pathWaypoints != null && pathWaypoints.Length > 0)
        {
            transform.position = pathWaypoints[0].position;
            StartMoving();
        }
    }
    
    private void Update()
    {
        // HP바 위치 업데이트
        if (hpBarCanvas != null)
        {
            hpBarCanvas.transform.position = transform.position + Vector3.up * 0.8f;
            hpBarCanvas.transform.rotation = Quaternion.identity;
        }
    }
    
    /// <summary>
    /// 몬스터 초기화
    /// </summary>
    private void Initialize()
    {
        // 챕터에 따른 스탯 조정
        float statMultiplier = Mathf.Pow(chapterStatMultiplier, currentChapter - 1);
        health *= statMultiplier;
        maxHealth = health;
        currentHealth = health;
        originalMoveSpeed = moveSpeed;
        
        // 크기 설정
        transform.localScale = Vector3.one * sizeScale;
        
        // 스프라이트 설정
        if (monsterSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = monsterSprite;
        }
        
        Debug.Log($"[Monster] {monsterName} 초기화 - 챕터: {currentChapter}, HP: {health:F0}, 속도: {moveSpeed:F1}");
    }
    
    /// <summary>
    /// World Space HP바 생성
    /// </summary>
    private void CreateHPBarWorldSpace()
    {
        GameObject hpBarObj = new GameObject("HPBar");
        hpBarObj.transform.SetParent(transform);
        hpBarObj.transform.localPosition = Vector3.up * 0.8f;
        
        // Canvas 설정
        Canvas canvas = hpBarObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        
        // Canvas 크기 설정
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(0.8f, 0.15f);
        canvasRect.localScale = Vector3.one * 0.01f;
        
        // CanvasScaler 추가
        UnityEngine.UI.CanvasScaler scaler = hpBarObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;
        
        // GraphicRaycaster 추가
        hpBarObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // 배경 이미지
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(hpBarObj.transform);
        UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRect = bgImage.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // HP 채움 이미지
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(hpBarObj.transform);
        UnityEngine.UI.Image fillImage = fillObj.AddComponent<UnityEngine.UI.Image>();
        fillImage.color = Color.red;
        RectTransform fillRect = fillImage.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        hpBarCanvas = canvas;
        hpBarFillImage = fillImage;
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
    /// 웨이포인트를 따라 이동
    /// </summary>
    private IEnumerator MoveAlongPath()
    {
        while (currentWaypointIndex < pathWaypoints.Length && !isDead)
        {
            if (isStunned)
            {
                yield return null;
                continue;
            }
            
            Transform targetWaypoint = pathWaypoints[currentWaypointIndex];
            
            while (Vector3.Distance(transform.position, targetWaypoint.position) > 0.1f)
            {
                if (isDead || isStunned) break;
                
                // 이동
                float currentSpeed = moveSpeed * slowAmount;
                Vector3 direction = (targetWaypoint.position - transform.position).normalized;
                transform.position += direction * currentSpeed * Time.deltaTime;
                
                // 스프라이트 방향 업데이트
                UpdateSpriteDirection(direction);
                
                yield return null;
            }
            
            if (!isDead && !isStunned)
            {
                currentWaypointIndex++;
                
                if (currentWaypointIndex >= pathWaypoints.Length)
                {
                    OnReachEnd();
                    yield break;
                }
            }
        }
    }
    
    /// <summary>
    /// 스프라이트 방향 업데이트
    /// </summary>
    private void UpdateSpriteDirection(Vector3 direction)
    {
        if (spriteRenderer != null && direction.x != 0)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }
    
    /// <summary>
    /// 데미지 받기 (IDamageable 구현)
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // HP바 업데이트
        UpdateHPBar();
        
        // 피격 효과
        PlayHitEffect();
        
        Debug.Log($"[Monster] {monsterName}이(가) {damage} 데미지를 받음! 남은 HP: {currentHealth:F0}/{maxHealth:F0}");
        
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
            
            // HP에 따른 색상 변경
            if (hpBarFillImage.fillAmount > 0.5f)
                hpBarFillImage.color = Color.red;
            else if (hpBarFillImage.fillAmount > 0.25f)
                hpBarFillImage.color = new Color(1f, 0.5f, 0f); // 주황색
            else
                hpBarFillImage.color = new Color(0.5f, 0f, 0f); // 어두운 빨강
        }
    }
    
    /// <summary>
    /// 피격 효과
    /// </summary>
    private void PlayHitEffect()
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
        
        // 색상 플래시
        StartCoroutine(ColorFlashCoroutine());
    }
    
    private IEnumerator ColorFlashCoroutine()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
    
    /// <summary>
    /// 몬스터 사망
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        Debug.Log($"[Monster] {monsterName} 사망!");
        
        // 사망 이벤트
        OnDeath?.Invoke();
        
        // 사망 효과
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // 미네랄 보상
        RewardMineral();
        
        // HP바 제거
        if (hpBarCanvas != null)
        {
            Destroy(hpBarCanvas.gameObject);
        }
        
        // 오브젝트 제거
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 미네랄 보상
    /// </summary>
    private void RewardMineral()
    {
        int mineralReward = Mathf.RoundToInt(10 * Mathf.Pow(chapterStatMultiplier, currentChapter - 1));
        
        if (areaIndex == 1)
        {
            CoreDataManager.Instance?.region1MineralBar?.AddMineral(mineralReward);
            Debug.Log($"[Monster] 지역1 미네랄 +{mineralReward}");
        }
        else if (areaIndex == 2)
        {
            CoreDataManager.Instance?.region2MineralBar?.AddMineral(mineralReward);
            Debug.Log($"[Monster] 지역2 미네랄 +{mineralReward}");
        }
    }
    
    /// <summary>
    /// 경로 끝 도달
    /// </summary>
    private void OnReachEnd()
    {
        if (hasReachedEnd || isDead) return;
        
        hasReachedEnd = true;
        
        Debug.Log($"[Monster] {monsterName}이(가) 성에 도달!");
        
        // 성에 데미지
        DealDamageToCastle();
        
        // 도달 이벤트
        OnReachedCastle?.Invoke(this);
        
        // 오브젝트 제거
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 성에 데미지
    /// </summary>
    private void DealDamageToCastle()
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            if (areaIndex == 1)
            {
                gameManager.TakeDamageToRegion1(damageToCastle);
                Debug.Log($"[Monster] 지역1 성에 {damageToCastle} 데미지!");
            }
            else if (areaIndex == 2)
            {
                gameManager.TakeDamageToRegion2(damageToCastle);
                Debug.Log($"[Monster] 지역2 성에 {damageToCastle} 데미지!");
            }
        }
    }
    
    /// <summary>
    /// 몬스터 라우트 설정
    /// </summary>
    public void SetMonsterRoute(RouteType route)
    {
        currentRoute = route;
    }
    
    // ===== 상태 효과 메서드들 =====
    
    /// <summary>
    /// 둔화 효과 적용
    /// </summary>
    public void ApplySlow(float duration, float amount)
    {
        if (slowDuration <= 0)
        {
            StartCoroutine(SlowCoroutine(duration, amount));
        }
        else
        {
            // 기존 둔화 효과 갱신
            slowDuration = Mathf.Max(slowDuration, duration);
            slowAmount = Mathf.Min(slowAmount, amount);
        }
    }
    
    private IEnumerator SlowCoroutine(float duration, float amount)
    {
        slowDuration = duration;
        slowAmount = amount;
        
        while (slowDuration > 0)
        {
            slowDuration -= Time.deltaTime;
            yield return null;
        }
        
        slowAmount = 1f;
    }
    
    /// <summary>
    /// 출혈 효과 적용
    /// </summary>
    public void ApplyBleed(float duration, float damagePerSec)
    {
        if (bleedCoroutine != null)
        {
            StopCoroutine(bleedCoroutine);
        }
        bleedCoroutine = StartCoroutine(BleedCoroutine(duration, damagePerSec));
    }
    
    private IEnumerator BleedCoroutine(float duration, float damagePerSec)
    {
        bleedDuration = duration;
        bleedDamagePerSec = damagePerSec;
        
        float tickInterval = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration && !isDead)
        {
            yield return new WaitForSeconds(tickInterval);
            
            if (!isDead)
            {
                TakeDamage(damagePerSec * tickInterval);
            }
            
            elapsed += tickInterval;
        }
        
        bleedDuration = 0f;
        bleedDamagePerSec = 0f;
    }
    
    /// <summary>
    /// 기절 효과 적용
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (!isStunned)
        {
            StartCoroutine(StunCoroutine(duration));
        }
    }
    
    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        stunDuration = duration;
        
        // 기절 시각 효과
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f);
            
            yield return new WaitForSeconds(duration);
            
            spriteRenderer.color = originalColor;
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }
        
        isStunned = false;
        stunDuration = 0f;
    }
    
    /// <summary>
    /// 디버그 정보
    /// </summary>
    public void DebugInfo()
    {
        Debug.Log($"[Monster] {monsterName} - " +
                  $"Chapter: {currentChapter}, HP: {currentHealth:F0}/{maxHealth:F0}, " +
                  $"Speed: {moveSpeed:F1}, Route: {currentRoute}, " +
                  $"Waypoint: {currentWaypointIndex}/{pathWaypoints?.Length ?? 0}");
    }
    
    private void OnDestroy()
    {
        // 코루틴 정리
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        if (bleedCoroutine != null)
        {
            StopCoroutine(bleedCoroutine);
        }
        
        // 이벤트 정리
        OnDeath = null;
        OnReachedCastle = null;
    }
}