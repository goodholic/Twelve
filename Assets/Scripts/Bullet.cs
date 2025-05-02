using UnityEngine;
using System.Collections;

/// <summary>
/// 10종의 탄환 타입 정의
/// </summary>
public enum BulletType
{
    Normal,            // 1) 일반탄
    Piercing,          // 2) 관통탄
    Explosive,         // 3) 폭발탄
    Slow,              // 4) 감속탄
    Bleed,             // 5) 출혈탄
    Chain,             // 6) 연쇄탄
    Stun,              // 7) 마비탄
    ArmorPenetration,  // 8) 파괴탄(방어 무시)
    Split,             // 9) 분열탄
    Energy             // 10) 에너지탄
}

/// <summary>
/// 타워(또는 유닛)에서 발사되는 탄환.
/// 10종의 총알 타입에 따라 서로 다른 효과를 적용함.
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Bullet Type (10종)")]
    public BulletType bulletType = BulletType.Normal;

    [Header("공통: 공격력 / 속도 / 최대 생존시간")]
    public float damage = 10f;         // 기본 공격력
    public float speed = 5f;           // 탄환 이동 속도
    public float maxLifeTime = 3f;     // 최대 생존 시간(초) - 지나면 파괴

    [Header("광역 공격 여부(폭발탄 등)")]
    public bool isAreaAttack = false;  // 범위 폭발 유무
    public float areaRadius = 1.5f;    // 광역 반경

    [Header("타겟 / 기타")]
    public Monster target;             // 단일 타겟 (Homing/슬로우탄 등에서 사용)
    private float aliveTime = 0f;      // 탄환 생존 시간 누적

    // -------------------------------------------------------
    // [탄환별 추가 옵션]
    // -------------------------------------------------------
    [Header("관통탄 옵션")]
    public int maxPierceCount = 3;   // 최대 관통 가능한 적 수

    [Header("슬로우탄 옵션")]
    public float slowDuration = 2f;  // 감속 지속시간 (초)
    public float slowAmount = 0.5f;  // 이동속도 배율(0.5 => 50% 속도로 감소)

    [Header("출혈탄 옵션(DoT)")]
    public float bleedDuration = 3f;      // 출혈 지속(초)
    public float bleedDamagePerSec = 2f;  // 초당 출혈 데미지

    [Header("연쇄탄 옵션")]
    public int chainMaxBounces = 3;       // 최대 튕길 횟수
    public float chainBounceRange = 2f;   // 인접 몬스터 감지 범위

    [Header("마비탄 옵션")]
    public float stunDuration = 1f;       // 스턴 지속시간(초)

    [Header("파괴탄 옵션(방어 무시)")]
    public bool ignoreDefense = true;     // 방어력 계산 무시 여부

    [Header("분열탄 옵션")]
    public int splitCount = 3;               // 몇 개로 분열할지
    public float splitBulletAngleSpread = 30f;// 분열 후 퍼지는 각도
    public GameObject subBulletPrefab;       // 분열 후 생성될 탄환 프리팹

    [Header("에너지탄 옵션")]
    public float extraRange = 5f;      // 추가 사거리 등
    public float energySpeedFactor = 1.5f; // 에너지탄은 속도 빠름(기본 speed에 곱)

    [Header("Impact / 폭발 이펙트 (옵션)")]
    public GameObject impactEffectPrefab;    // 단일 피격/마지막 충돌 이펙트
    public GameObject explosionEffectPrefab; // 폭발탄 전용 이펙트 등(광역)

    // 관통/연쇄 시 중복 타격 방지용
    private int piercedCount = 0;
    private System.Collections.Generic.List<Monster> chainAttackedMonsters = new System.Collections.Generic.List<Monster>();
    private int currentBounceCount = 0;

    // VFX 패널 (정적 참조)
    private static RectTransform vfxPanel;

    // === 수정 부분 ===
    [Header("Area 구분 (1 or 2)")]
    public int areaIndex = 1;
    // === 수정 끝 ===

    /// <summary>
    /// VFX 이펙트들이 생성될 부모 패널 설정 (정적 메서드)
    /// </summary>
    public static void SetVfxPanel(RectTransform panel)
    {
        vfxPanel = panel;
        Debug.Log("[Bullet] VFX 패널 설정됨: " + (panel != null ? panel.name : "null"));
    }

    /// <summary>
    /// 탄환 초기화
    /// </summary>
    // === 수정 부분: areaIndex 인자 추가 ===
    public void Init(Monster targetMonster, float baseDamage, float baseSpeed,
                     bool areaAtk, float areaAtkRadius, int areaIndex)
    {
        this.target = targetMonster;
        this.damage = baseDamage;
        this.speed = baseSpeed;
        this.isAreaAttack = areaAtk;
        this.areaRadius = areaAtkRadius;
        this.areaIndex = areaIndex; // 추가된 부분

        // 에너지탄 속도 보정
        if (bulletType == BulletType.Energy)
        {
            this.speed *= energySpeedFactor;
        }
    }
    // === 수정 끝 ===

    private void Start()
    {
        piercedCount = 0;
        currentBounceCount = 0;
        chainAttackedMonsters.Clear();
    }

    private void Update()
    {
        aliveTime += Time.deltaTime;

        if (aliveTime >= maxLifeTime)
        {
            Destroy(gameObject);
            return;
        }

        MoveBullet();
    }

    private void MoveBullet()
    {
        switch (bulletType)
        {
            case BulletType.Energy:
            case BulletType.Normal:
            case BulletType.Piercing:
            case BulletType.Explosive:
            case BulletType.Slow:
            case BulletType.Bleed:
            case BulletType.Stun:
            case BulletType.ArmorPenetration:
            case BulletType.Split:
                if (target != null)
                {
                    Vector3 dir = (target.transform.position - transform.position).normalized;
                    transform.position += dir * speed * Time.deltaTime;

                    float dist = Vector2.Distance(transform.position, target.transform.position);
                    if (dist < 0.2f)
                    {
                        OnHitTarget(target);
                    }
                }
                else
                {
                    transform.position += transform.right * speed * Time.deltaTime;
                }
                break;

            case BulletType.Chain:
                if (target != null)
                {
                    Vector3 dirC = (target.transform.position - transform.position).normalized;
                    transform.position += dirC * speed * Time.deltaTime;

                    float distC = Vector2.Distance(transform.position, target.transform.position);
                    if (distC < 0.2f)
                    {
                        OnHitTarget(target);
                    }
                }
                else
                {
                    Destroy(gameObject);
                }
                break;
        }
    }

    private void OnHitTarget(Monster hitMonster)
    {
        // === 수정 부분 ===
        // 만약 areaIndex가 다르면 피격 로직 무시 (즉시 return)
        if (hitMonster != null && this.areaIndex != hitMonster.areaIndex)
        {
            // 충돌 무시
            return;
        }
        // === 수정 끝 ===

        switch (bulletType)
        {
            case BulletType.Normal:
                DealDamage(hitMonster);
                SpawnImpactEffect(hitMonster.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Piercing:
                DealDamage(hitMonster);
                piercedCount++;
                SpawnImpactEffect(hitMonster.transform.position);

                if (piercedCount >= maxPierceCount)
                {
                    Destroy(gameObject);
                }
                break;

            case BulletType.Explosive:
                if (isAreaAttack)
                {
                    ApplyAreaDamage(hitMonster.transform.position);
                }
                SpawnExplosionEffect(hitMonster.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Slow:
                DealDamage(hitMonster, 0.7f);
                hitMonster.ApplySlow(slowAmount, slowDuration);
                SpawnImpactEffect(hitMonster.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Bleed:
                DealDamage(hitMonster);
                hitMonster.ApplyBleed(bleedDamagePerSec, bleedDuration);
                SpawnImpactEffect(hitMonster.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Chain:
                DealDamage(hitMonster);
                chainAttackedMonsters.Add(hitMonster);
                currentBounceCount++;
                SpawnImpactEffect(hitMonster.transform.position);

                if (currentBounceCount >= chainMaxBounces)
                {
                    Destroy(gameObject);
                }
                else
                {
                    Monster newTarget = FindNearestMonster(hitMonster.transform.position, chainBounceRange);
                    if (newTarget == null)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        target = newTarget;
                    }
                }
                break;

            case BulletType.Stun:
                DealDamage(hitMonster, 0.8f);
                hitMonster.ApplyStun(stunDuration);
                SpawnImpactEffect(hitMonster.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.ArmorPenetration:
                DealDamage(hitMonster, 1.0f, ignoreDefense);
                SpawnImpactEffect(hitMonster.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Split:
                DealDamage(hitMonster);
                if (subBulletPrefab != null && splitCount > 0)
                {
                    DoSplit(hitMonster.transform.position);
                }
                SpawnImpactEffect(hitMonster.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Energy:
                DealDamage(hitMonster, 0.8f);
                SpawnImpactEffect(hitMonster.transform.position);
                Destroy(gameObject);
                break;
        }
    }

    private void ApplyAreaDamage(Vector3 center)
    {
        if (areaRadius <= 0f) return;

        Monster[] allMonsters = Object.FindObjectsByType<Monster>(FindObjectsSortMode.None);
        foreach (var m in allMonsters)
        {
            if (m == null) continue;
            float dist = Vector2.Distance(center, m.transform.position);
            if (dist <= areaRadius)
            {
                // === 수정 부분: areaIndex 다르면 무시
                if (m.areaIndex == this.areaIndex)
                {
                    DealDamage(m);
                }
            }
        }
    }

    private void DealDamage(Monster m, float damageMultiplier = 1f, bool ignoreDef = false)
    {
        float finalDamage = damage * damageMultiplier;
        m.TakeDamage(finalDamage, gameObject);
    }

    private Monster FindNearestMonster(Vector3 pos, float range)
    {
        Monster[] all = Object.FindObjectsByType<Monster>(FindObjectsSortMode.None);
        Monster nearest = null;
        float minDist = float.MaxValue;

        foreach (var m in all)
        {
            if (m == null) continue;
            if (chainAttackedMonsters.Contains(m)) continue;

            float dist = Vector2.Distance(pos, m.transform.position);
            if (dist < range && dist < minDist)
            {
                minDist = dist;
                nearest = m;
            }
        }
        return nearest;
    }

    private void DoSplit(Vector3 splitCenter)
    {
        float halfAngle = (splitBulletAngleSpread * (splitCount - 1)) / 2f;
        for (int i = 0; i < splitCount; i++)
        {
            float angle = -halfAngle + i * splitBulletAngleSpread;

            GameObject sub = Instantiate(subBulletPrefab, splitCenter, Quaternion.identity);
            Bullet subB = sub.GetComponent<Bullet>();
            if (subB != null)
            {
                subB.bulletType = BulletType.Normal;  
                subB.damage = this.damage * 0.6f;
                subB.speed = this.speed * 0.8f;
                subB.maxLifeTime = 2f;

                // === 수정 부분: 분열탄도 동일 areaIndex
                subB.areaIndex = this.areaIndex;
                // === 수정 끝

                Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                subB.Init(null, subB.damage, subB.speed, false, 0f, subB.areaIndex);

                Rigidbody2D rb = sub.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = dir * subB.speed;
                }
            }
        }
    }

    private void SpawnImpactEffect(Vector3 pos)
    {
        if (impactEffectPrefab != null)
        {
            GameObject effect;
            if (vfxPanel != null)
            {
                effect = Instantiate(impactEffectPrefab, vfxPanel);
                RectTransform rectTransform = effect.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Vector2 localPos = vfxPanel.InverseTransformPoint(pos);
                    rectTransform.anchoredPosition = localPos;
                }
                else
                {
                    effect.transform.position = pos;
                }
            }
            else
            {
                effect = Instantiate(impactEffectPrefab, pos, Quaternion.identity);
            }

            // === 수정된 부분: 3초 → 1초 후 파괴 ===
            Destroy(effect, 1f);
        }
    }

    private void SpawnExplosionEffect(Vector3 pos)
    {
        if (explosionEffectPrefab != null)
        {
            GameObject effect;
            if (vfxPanel != null)
            {
                effect = Instantiate(explosionEffectPrefab, vfxPanel);
                RectTransform rectTransform = effect.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Vector2 localPos = vfxPanel.InverseTransformPoint(pos);
                    rectTransform.anchoredPosition = localPos;
                }
                else
                {
                    effect.transform.position = pos;
                }
            }
            else
            {
                effect = Instantiate(explosionEffectPrefab, pos, Quaternion.identity);
            }

            // === 수정된 부분: 3초 → 1초 후 파괴 ===
            Destroy(effect, 1f);
        }
    }
}
