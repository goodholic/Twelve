using System;
using UnityEngine;
using Fusion;
using UnityEngine.UI;

public class Monster : NetworkBehaviour, IDamageable
{
    [Header("Monster Stats")]
    public float moveSpeed = 3f;
    public float health = 50f;

    [Header("Waypoint Path (2D)")]
    public Transform[] pathWaypoints;

    [Header("Castle Attack Damage")]
    public int damageToCastle = 1;

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

    private void Awake()
    {
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
        // HP 바 위치 보정(머리 위)
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
            return;

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
        // 끝 지점 도달 시
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
