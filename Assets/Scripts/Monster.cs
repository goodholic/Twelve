using System;
using UnityEngine;
using Fusion;
using UnityEngine.UI;

public class Monster : NetworkBehaviour
{
    [Header("Monster Stats")]
    public float moveSpeed = 3f;
    public float health = 50f;

    [Header("Waypoint Path (2D)")]
    [Tooltip("이 몬스터가 처음 이동할 웨이포인트들")]
    public Transform[] pathWaypoints;

    [Header("Team/Ally Settings")]
    [Tooltip("true이면 아군 몬스터, false이면 적 몬스터")]
    public bool isAlly = false;

    // 죽을 때 이벤트 (필요시 외부에서 구독)
    public event Action OnDeath;

    // 성 도달 시 이벤트 (옵션)
    public event Action<Monster> OnReachedCastle;

    [Header("Castle Attack Damage (적 몬스터용)")]
    [Tooltip("적 몬스터가 성에 도달했을 때 깎을 체력량")]
    public int damageToCastle = 1;

    // 웨이포인트 이동용
    private int currentWaypointIndex = 0;
    private bool isDead = false;

    // ======================= (상태 효과) =======================
    [Header("상태 효과")]
    private float slowDuration = 0f;        // 슬로우 지속 시간
    private float slowAmount = 0f;          // 슬로우 비율 (0~1)
    private float originalMoveSpeed;        // 원래 이동 속도
    private float bleedDuration = 0f;       // 출혈 지속 시간
    private float bleedDamagePerSec = 0f;   // 초당 출혈 데미지
    private float stunDuration = 0f;        // 스턴 지속 시간
    private bool isStunned = false;         // 스턴 상태 여부

    // ======================== (체력바) ========================
    [Header("Overhead HP Bar (머리 위 체력바)")]
    [Tooltip("몬스터 머리 위에 표시될 HP Bar용 Canvas (WorldSpace 또는 기타)")]
    public Canvas hpBarCanvas;
    [Tooltip("HP Bar Fill 이미지 (체력 비율을 나타낼 Image)")]
    public Image hpFillImage;

    [Header("아군 몬스터 소멸 이펙트 (끝지점 도달 시)")]
    [Tooltip("아군 몬스터가 끝 지점에서 사라질 때 보여줄 이펙트 프리팹")]
    public GameObject vanishEffectPrefab;

    // ======================= [수정] 추가된 부분 =======================
    [Header("Ally Conversion Effects")]
    [Tooltip("적 → 아군 전환 시 나타날 이펙트 프리팹 (VFX Panel 자식으로 생성)")]
    public GameObject allyConversionEffectPrefab;

    [Tooltip("아군이 된 이후 적용할 아웃라인 프리팹 (몬스터의 자식으로 생성)")]
    public GameObject allyOutlinePrefab;
    // ================================================================

    private void Awake()
    {
        // 초기 이동 속도 저장
        originalMoveSpeed = moveSpeed;

        // **HP Bar 세팅 확인 및 활성화**
        if (hpBarCanvas == null || hpFillImage == null)
        {
            Debug.LogWarning($"[Monster] HP Bar Canvas 또는 Fill Image가 Inspector에 연결되지 않았습니다. ( {name} )");
        }
        else
        {
            hpBarCanvas.gameObject.SetActive(true);
            UpdateHpBar();  // 초기 체력 UI 반영
        }
    }

    private void Update()
    {
        if (isDead) return;

        // 상태 효과 업데이트
        UpdateStatusEffects();

        // 스턴 상태가 아닐 때만 이동
        if (!isStunned)
        {
            MoveAlongPath2D();
        }
    }

    // ========== (체력바 위치 갱신: 2D 상황에서 "머리 위"로) ==========
    private void LateUpdate()
    {
        if (hpBarCanvas != null && hpBarCanvas.transform.parent == null)
        {
            Vector3 offset = new Vector3(0f, 1.2f, 0f);
            hpBarCanvas.transform.position = transform.position + offset;
        }
    }

    // ================================
    // (1) 상태 효과 업데이트
    // ================================
    private void UpdateStatusEffects()
    {
        // 슬로우
        if (slowDuration > 0)
        {
            slowDuration -= Time.deltaTime;
            if (slowDuration <= 0)
            {
                // 슬로우 종료
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

    // ================================
    // (2) 웨이포인트 이동 (2D)
    // ================================
    private void MoveAlongPath2D()
    {
        if (pathWaypoints == null || pathWaypoints.Length == 0 || currentWaypointIndex >= pathWaypoints.Length)
        {
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
                OnReachEndPoint();
            }
        }
    }

    private void OnReachEndPoint()
    {
        OnReachedCastle?.Invoke(this);

        if (isAlly)
        {
            // 아군 몬스터 -> 끝지점 도달 시 사라지는 이펙트
            if (vanishEffectPrefab != null)
            {
                GameObject effectObj = null;
                RectTransform vfxPanel = null;

                if (PlacementManager.Instance != null)
                {
                    vfxPanel = PlacementManager.Instance.vfxPanel;
                }

                if (vfxPanel != null)
                {
                    effectObj = Instantiate(vanishEffectPrefab, vfxPanel);
                    RectTransform effectRect = effectObj.GetComponent<RectTransform>();

                    if (effectRect != null)
                    {
                        Vector2 localPos = vfxPanel.InverseTransformPoint(transform.position);
                        effectRect.anchoredPosition = localPos;
                        effectRect.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        effectObj.transform.position = transform.position;
                    }
                }
                else
                {
                    effectObj = Instantiate(vanishEffectPrefab, transform.position, Quaternion.identity);
                }

                Destroy(effectObj, 3f);
            }
        }
        else
        {
            // 적 몬스터라면 성 체력 깎기
            CastleHealthManager.Instance?.TakeDamage(damageToCastle);
        }

        // 여기서는 더 이상 몬스터를 제거하지 않음(주석 처리)
        // 필요하면 OnReachedCastle 이후 다른 로직 가능
    }

    // ================================
    // (3) 체력 데미지 처리
    // ================================
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

        // 웨이브 스포너 등에 "사망" 알림
        OnDeath?.Invoke();

        // ** 1초 뒤 아군으로 부활 **
        StartCoroutine(RespawnAsAllyCoroutine());
    }

    /// <summary>
    /// 1) gameObject.SetActive(false) 후 1초 대기
    /// 2) ConvertToAlly() -> 체력/상태 복구 + isAlly=true
    /// 3) 다시 SetActive(true)로 부활
    /// </summary>
    private System.Collections.IEnumerator RespawnAsAllyCoroutine()
    {
        // (A) 먼저 자기 자신을 비활성화 => 총알에 맞지 않도록
        gameObject.SetActive(false);

        // (B) 1초 대기
        yield return new WaitForSeconds(1f);

        // (C) 아군 몬스터로 변환 후 재활성화
        ConvertToAlly();
        gameObject.SetActive(true);
    }

    // ================================
    // (4) 슬로우/출혈/스턴 메서드들
    // ================================
    public void ApplySlow(float amount, float duration)
    {
        if (amount > slowAmount || (amount == slowAmount && duration > slowDuration))
        {
            slowAmount = Mathf.Clamp01(amount);
            slowDuration = duration;
            moveSpeed = originalMoveSpeed * (1f - slowAmount);

            Debug.Log($"[Monster] {gameObject.name}에 슬로우 {slowAmount * 100}% 적용 (지속 {duration}초)");
        }
    }

    public void ApplyBleed(float damagePerSecond, float duration)
    {
        if (damagePerSecond > bleedDamagePerSec ||
            (damagePerSecond == bleedDamagePerSec && duration > bleedDuration))
        {
            bleedDamagePerSec = damagePerSecond;
            bleedDuration = duration;

            Debug.Log($"[Monster] {gameObject.name}에 출혈(초당 {damagePerSecond}), {duration}초");
        }
    }

    public void ApplyStun(float duration)
    {
        if (duration > stunDuration)
        {
            stunDuration = duration;
            isStunned = true;
            Debug.Log($"[Monster] {gameObject.name}에 스턴 적용 (지속 {duration}초)");
        }
    }

    // ================================
    // (5) HP Bar UI 갱신
    // ================================
    private void UpdateHpBar()
    {
        if (hpFillImage == null) return;

        // 임시로 최대HP를 50으로 가정(본인 로직에 맞게 수정 가능)
        float maxHP = 50f;
        float ratio = Mathf.Clamp01(health / maxHP);

        hpFillImage.fillAmount = ratio;
    }

    /// <summary>
    /// (적 → 아군 전환) 체력/상태 초기화 + 경로 재설정 등
    /// </summary>
    private void ConvertToAlly()
    {
        Debug.Log($"[Monster] {name} 죽음 → 이제 아군 몬스터가 됩니다.");

        // 상태효과 초기화
        slowDuration = 0f;
        slowAmount = 0f;
        bleedDuration = 0f;
        bleedDamagePerSec = 0f;
        stunDuration = 0f;
        isStunned = false;

        // 체력 다시 회복(최대치 등)
        health = 50f; // 완전히 체력 회복
        UpdateHpBar(); // HP Bar 즉시 갱신

        // 다시 살아남
        isDead = false;
        // 팀 변경
        isAlly = true;

        // 아군 몬스터 이동 경로 재설정(필요 시)
        WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
        if (spawner != null && spawner.pathWaypoints != null && spawner.pathWaypoints.Length > 0)
        {
            pathWaypoints = spawner.pathWaypoints;
            transform.position = pathWaypoints[0].position;
            currentWaypointIndex = 0;
        }
        else
        {
            Debug.LogWarning("[Monster] 아군용 웨이포인트가 없어 제자리에 머무릅니다.");
        }

        // 이동 속도 복구
        moveSpeed = originalMoveSpeed;

        // =============== [수정된 부분] ===============
        // "ourMonsterPanel"로 부모 변경 → 활성화 시 재등장
        if (PlacementManager.Instance != null && PlacementManager.Instance.ourMonsterPanel != null)
        {
            transform.SetParent(PlacementManager.Instance.ourMonsterPanel, false);
        }
        // ============================================

        // [추가] 아군 전환 이펙트
        if (allyConversionEffectPrefab != null)
        {
            GameObject effectObj = null;
            RectTransform vfxPanel = null;

            if (PlacementManager.Instance != null)
            {
                vfxPanel = PlacementManager.Instance.vfxPanel;
            }

            if (vfxPanel != null)
            {
                effectObj = Instantiate(allyConversionEffectPrefab, vfxPanel);
                RectTransform effectRect = effectObj.GetComponent<RectTransform>();

                if (effectRect != null)
                {
                    Vector2 localPos = vfxPanel.InverseTransformPoint(transform.position);
                    effectRect.anchoredPosition = localPos;
                    effectRect.localRotation = Quaternion.identity;
                }
                else
                {
                    effectObj.transform.position = transform.position;
                }
            }
            else
            {
                effectObj = Instantiate(allyConversionEffectPrefab, transform.position, Quaternion.identity);
            }

            // 3초 후 자동 파괴
            Destroy(effectObj, 3f);
        }

        // [추가] 아군 아웃라인 프리팹 적용
        if (allyOutlinePrefab != null)
        {
            Transform existingOutline = transform.Find("AllyOutline");
            if (existingOutline == null)
            {
                GameObject outlineObj = Instantiate(allyOutlinePrefab, transform);
                outlineObj.name = "AllyOutline";
                outlineObj.transform.localPosition = Vector3.zero;
                outlineObj.transform.localRotation = Quaternion.identity;
            }
            else
            {
                existingOutline.gameObject.SetActive(true);
            }
        }
    }
}
