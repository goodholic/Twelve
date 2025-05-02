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
    private float slowDuration = 0f;
    private float slowAmount = 0f;
    private float originalMoveSpeed;
    private float bleedDuration = 0f;
    private float bleedDamagePerSec = 0f;
    private float stunDuration = 0f;
    private bool isStunned = false;

    // ===================== (체력바) =====================
    [Header("Overhead HP Bar (머리 위 체력바)")]
    [Tooltip("몬스터 머리 위에 표시될 HP Bar용 Canvas (WorldSpace 또는 기타)")]
    public Canvas hpBarCanvas;
    [Tooltip("HP Bar Fill 이미지 (체력 비율을 나타낼 Image)")]
    public Image hpFillImage;

    [Header("아군 몬스터 소멸 이펙트 (끝지점 도달 시)")]
    [Tooltip("아군 몬스터가 끝 지점에서 사라질 때 보여줄 이펙트 프리팹")]
    public GameObject vanishEffectPrefab;

    [Header("Ally Conversion Effects")]
    [Tooltip("적 → 아군 전환 시 나타날 이펙트 프리팹 (VFX Panel 자식으로 생성)")]
    public GameObject allyConversionEffectPrefab;

    [Tooltip("아군이 된 이후 적용할 아웃라인 프리팹 (몬스터의 자식으로 생성)")]
    public GameObject allyOutlinePrefab;

    private float maxHealth;

    // ==================================================
    // (추가) 상대편 위치에서 부활하기 위한 참조들
    // ==================================================
    [Header("[Opponent Side Settings]")]
    [Tooltip("이 몬스터가 '상대편'에서 다시 부활할 때 사용할 웨이포인트들")]
    public Transform[] opponentWaypoints;

    [Tooltip("상대편(적) 몬스터를 담을 부모 패널 (우리 편일 때 ourMonsterPanel처럼 사용)")]
    public Transform opponentMonsterPanel;

    // === 수정 부분 ===
    [Header("Area 구분 (1 or 2)")]
    public int areaIndex = 1;
    // === 수정 끝 ===

    private void Awake()
    {
        originalMoveSpeed = moveSpeed;
        maxHealth = health;

        if (hpBarCanvas == null || hpFillImage == null)
        {
            Debug.LogWarning($"[Monster] HP Bar Canvas 또는 Fill Image가 Inspector에 연결되지 않았습니다. ( {name} )");
        }
        else
        {
            hpBarCanvas.gameObject.SetActive(true);
            UpdateHpBar();
        }
    }

    private void Update()
    {
        if (isDead) return;

        UpdateStatusEffects();

        if (!isStunned)
        {
            MoveAlongPath2D();
        }
    }

    private void LateUpdate()
    {
        if (hpBarCanvas != null && hpBarCanvas.transform.parent == null)
        {
            Vector3 offset = new Vector3(0f, 1.2f, 0f);
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

        // (중요) 웨이브 스포너 등에 "사망" 취급
        OnDeath?.Invoke();

        if (isAlly)
        {
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

                Destroy(effectObj, 1f);
            }
        }
        else
        {
            CastleHealthManager.Instance?.TakeDamage(damageToCastle);
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

        if (isAlly)
        {
            StartCoroutine(RespawnAsEnemyInOpponentMapCoroutine());
        }
        else
        {
            StartCoroutine(RespawnInOurMonsterCoroutine());
        }
    }

    private System.Collections.IEnumerator RespawnAsEnemyInOpponentMapCoroutine()
    {
        gameObject.SetActive(false);
        yield return new WaitForSeconds(1f);

        MoveToOpponentSideAndRevive();

        gameObject.SetActive(true);
    }

    private void MoveToOpponentSideAndRevive()
    {
        Debug.Log($"[Monster] {name} 죽음 -> 이제 상대편으로 부활 (적 몬스터).");

        slowDuration = 0f;
        slowAmount = 0f;
        bleedDuration = 0f;
        bleedDamagePerSec = 0f;
        stunDuration = 0f;
        isStunned = false;

        health = maxHealth;
        UpdateHpBar();

        isDead = false;
        isAlly = false;

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

        moveSpeed = originalMoveSpeed;

        if (opponentMonsterPanel != null)
        {
            transform.SetParent(opponentMonsterPanel, false);
        }
    }

    private System.Collections.IEnumerator RespawnInOurMonsterCoroutine()
    {
        gameObject.SetActive(false);
        yield return new WaitForSeconds(1f);

        MoveToOurMonsterAndRevive();

        gameObject.SetActive(true);
    }

    private void MoveToOurMonsterAndRevive()
    {
        Debug.Log($"[Monster] {name} 죽음 → 우리 몬스터 쪽으로 부활합니다.");

        slowDuration = 0f;
        slowAmount = 0f;
        bleedDuration = 0f;
        bleedDamagePerSec = 0f;
        stunDuration = 0f;
        isStunned = false;

        health = maxHealth;
        UpdateHpBar();

        isDead = false;
        isAlly = true;

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

        moveSpeed = originalMoveSpeed;

        if (PlacementManager.Instance != null && PlacementManager.Instance.ourMonsterPanel != null)
        {
            transform.SetParent(PlacementManager.Instance.ourMonsterPanel, false);
        }

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

            Destroy(effectObj, 1f);
        }

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

    private void UpdateHpBar()
    {
        if (hpFillImage == null) return;

        float ratio = Mathf.Clamp01(health / maxHealth);
        hpFillImage.fillAmount = ratio;
    }
}
