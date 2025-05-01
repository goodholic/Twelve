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

    // ===================== (상태 효과) =====================
    [Header("상태 효과")]
    private float slowDuration = 0f;        // 슬로우 지속 시간
    private float slowAmount = 0f;          // 슬로우 비율 (0~1)
    private float originalMoveSpeed;        // 원래 이동 속도
    private float bleedDuration = 0f;       // 출혈 지속 시간
    private float bleedDamagePerSec = 0f;   // 초당 출혈 데미지
    private float stunDuration = 0f;        // 스턴 지속 시간
    private bool isStunned = false;         // 스턴 상태 여부

    // ===================== (체력바) =====================
    [Header("Overhead HP Bar (머리 위 체력바)")]
    [Tooltip("몬스터 머리 위에 표시될 HP Bar용 Canvas (WorldSpace 또는 기타)")]
    public Canvas hpBarCanvas;
    [Tooltip("HP Bar Fill 이미지 (체력 비율을 나타낼 Image)")]
    public Image hpFillImage;

    [Header("아군 몬스터 소멸 이펙트 (끝지점 도달 시)")]
    [Tooltip("아군 몬스터가 끝 지점에서 사라질 때 보여줄 이펙트 프리팹")]
    public GameObject vanishEffectPrefab;

    // ======================= [추가된 부분] =======================
    [Header("Ally Conversion Effects")]
    [Tooltip("적 → 아군 전환 시 나타날 이펙트 프리팹 (VFX Panel 자식으로 생성)")]
    public GameObject allyConversionEffectPrefab;

    [Tooltip("아군이 된 이후 적용할 아웃라인 프리팹 (몬스터의 자식으로 생성)")]
    public GameObject allyOutlinePrefab;

    // (기존) 체력 복원용 최댓값 보관
    private float maxHealth;

    // ==================================================
    // (추가) 상대편 위치에서 부활하기 위한 참조들
    // ==================================================
    [Header("[Opponent Side Settings]")]
    [Tooltip("이 몬스터가 '상대편'에서 다시 부활할 때 사용할 웨이포인트들")]
    public Transform[] opponentWaypoints;

    [Tooltip("상대편(적) 몬스터를 담을 부모 패널 (우리 편일 때 ourMonsterPanel처럼 사용)")]
    public Transform opponentMonsterPanel;

    private void Awake()
    {
        // 초기 이동 속도 저장
        originalMoveSpeed = moveSpeed;

        // 처음 설정된 health 값을 maxHealth로 보관
        maxHealth = health;

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

    private void LateUpdate()
    {
        // HP Bar가 따로 WorldSpace Canvas일 경우, "머리 위" 위치를 갱신
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

    /// <summary>
    /// (중요) 웨이포인트 배열의 끝에 도달했을 때 호출됨.
    /// 여기서 '아군/적' 구분 없이 몬스터가 **무조건** 사라지도록 수정.
    /// </summary>
    private void OnReachEndPoint()
    {
        // 도달 이벤트
        OnReachedCastle?.Invoke(this);

        // [중요] 2웨이브부터 몬스터가 등장하지 않는 문제 해결을 위해
        //       여기서도 OnDeath를 호출하여 WaveSpawner의 aliveMonsters를 감소시킵니다.
        OnDeath?.Invoke(); // ← 추가된 한 줄

        if (isAlly)
        {
            // 아군 몬스터면 'vanishEffectPrefab' 표시(옵션)
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

                // === 수정된 부분: 3초 → 1초 후 파괴 ===
                Destroy(effectObj, 1f);
            }
        }
        else
        {
            // 적 몬스터면 성 체력 깎기
            CastleHealthManager.Instance?.TakeDamage(damageToCastle);
        }

        // ★★★ 공통: waypoint 끝까지 왔으니 "무조건" 제거 ★★★
        if (Object != null && Object.IsValid) // Fusion NetworkObject
        {
            Runner.Despawn(Object);
        }
        else
        {
            Destroy(gameObject);
        }
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

        if (isAlly)
        {
            // 아군 -> 적(상대편)으로 부활
            StartCoroutine(RespawnAsEnemyInOpponentMapCoroutine());
        }
        else
        {
            // 적 -> 아군으로 부활
            StartCoroutine(RespawnInOurMonsterCoroutine());
        }
    }

    // ==================================================
    // (A) 아군이었던 몬스터가 상대편(=적) 맵에서 부활
    // ==================================================
    private System.Collections.IEnumerator RespawnAsEnemyInOpponentMapCoroutine()
    {
        // 1) 즉시 자기 자신 비활성 -> 무적
        gameObject.SetActive(false);

        // 2) 1초 후 부활
        yield return new WaitForSeconds(1f);

        // 3) "상대 편으로 이동 + 적 몬스터로 설정"
        MoveToOpponentSideAndRevive();

        // 4) 다시 활성화
        gameObject.SetActive(true);
    }

    private void MoveToOpponentSideAndRevive()
    {
        Debug.Log($"[Monster] {name} 죽음 -> 이제 상대편으로 부활 (적 몬스터).");

        // 상태효과 초기화
        slowDuration = 0f;
        slowAmount = 0f;
        bleedDuration = 0f;
        bleedDamagePerSec = 0f;
        stunDuration = 0f;
        isStunned = false;

        // 체력 회복
        health = maxHealth;
        UpdateHpBar();

        // 다시 살아남
        isDead = false;
        // 팀: 적
        isAlly = false;

        // 상대쪽 웨이포인트 사용
        if (opponentWaypoints != null && opponentWaypoints.Length > 0)
        {
            pathWaypoints = opponentWaypoints;
            transform.position = opponentWaypoints[0].position;
            currentWaypointIndex = 0;
        }
        else
        {
            Debug.LogWarning("[Monster] opponentWaypoints가 비어있음 -> 부활 후 위치/경로 설정 불가");
        }

        // 이동 속도 복구
        moveSpeed = originalMoveSpeed;

        // 상대편(=적) 패널에 넣기
        if (opponentMonsterPanel != null)
        {
            transform.SetParent(opponentMonsterPanel, false);
        }
    }

    // ==================================================
    // (B) 적이었던 몬스터를 '우리 편'에서 부활
    //     (원래 코드에 있던 부분)
    // ==================================================
    private System.Collections.IEnumerator RespawnInOurMonsterCoroutine()
    {
        // (1) 즉시 자기 자신 비활성 -> 총알 등으로부터 무적
        gameObject.SetActive(false);

        // (2) 1초 후 부활
        yield return new WaitForSeconds(1f);

        // (3) "ourMonsterPanel" 자식으로 이동 + 풀 체력 + 아군으로 전환
        MoveToOurMonsterAndRevive();

        // (4) 다시 활성화
        gameObject.SetActive(true);
    }

    private void MoveToOurMonsterAndRevive()
    {
        Debug.Log($"[Monster] {name} 죽음 → 우리 몬스터 쪽으로 부활합니다.");

        // 모든 상태효과 초기화
        slowDuration = 0f;
        slowAmount = 0f;
        bleedDuration = 0f;
        bleedDamagePerSec = 0f;
        stunDuration = 0f;
        isStunned = false;

        // 체력 완전 회복
        health = maxHealth;
        UpdateHpBar();

        // 다시 살아남
        isDead = false;
        // 팀 변경(우리 몬스터)
        isAlly = true;

        // 아군 몬스터 이동 경로 재설정
        WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
        if (spawner != null && spawner.pathWaypoints != null && spawner.pathWaypoints.Length > 0)
        {
            pathWaypoints = spawner.pathWaypoints;
            transform.position = spawner.pathWaypoints[0].position;
            currentWaypointIndex = 0;
        }
        else
        {
            Debug.LogWarning("[Monster] 아군용 웨이포인트를 찾지 못했습니다. 이동 경로가 없으면 제자리에 머무릅니다.");
        }

        // 이동 속도 복구
        moveSpeed = originalMoveSpeed;

        // ourMonsterPanel 자식으로 이동
        if (PlacementManager.Instance != null && PlacementManager.Instance.ourMonsterPanel != null)
        {
            transform.SetParent(PlacementManager.Instance.ourMonsterPanel, false);
        }

        // 아군 전환 이펙트
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

            // === 수정된 부분: 3초 → 1초 후 파괴 ===
            Destroy(effectObj, 1f);
        }

        // 아군 아웃라인 적용
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

        float ratio = Mathf.Clamp01(health / maxHealth);
        hpFillImage.fillAmount = ratio;
    }
}
