// Assets\Scripts\Bullet.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 10종의 탄환 타입 정의
/// </summary>
public enum BulletType
{
    Normal,
    Piercing,
    Explosive,
    Slow,
    Bleed,
    Chain,
    Stun,
    ArmorPenetration,
    Split,
    Energy
}

/// <summary>
/// 타워(또는 유닛)에서 발사되는 탄환.
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Bullet Type (10종)")]
    public BulletType bulletType = BulletType.Normal;

    [Header("공통: 공격력 / 속도 / 최대 생존시간")]
    public float damage = 10f;
    public float speed = 5f;
    public float maxLifeTime = 3f;

    [Header("광역 공격 여부(폭발탄 등)")]
    public bool isAreaAttack = false;
    public float areaRadius = 1.5f;

    [Header("타겟 / 기타")]
    public Monster target;
    private float aliveTime = 0f;

    // -------------------------------------------------------
    // [탄환별 추가 옵션들]
    // -------------------------------------------------------
    [Header("관통탄 옵션")]
    public int maxPierceCount = 3;

    [Header("슬로우탄 옵션")]
    public float slowDuration = 2f;
    public float slowAmount = 0.5f;

    [Header("출혈탄 옵션(DoT)")]
    public float bleedDuration = 3f;
    public float bleedDamagePerSec = 2f;

    [Header("연쇄탄 옵션")]
    public int chainMaxBounces = 3;
    public float chainBounceRange = 2f;

    [Header("마비탄 옵션")]
    public float stunDuration = 1f;

    [Header("파괴탄 옵션(방어 무시)")]
    public bool ignoreDefense = true;

    [Header("분열탄 옵션")]
    public int splitCount = 3;
    public float splitBulletAngleSpread = 30f;
    public GameObject subBulletPrefab;

    [Header("에너지탄 옵션")]
    public float extraRange = 5f;
    public float energySpeedFactor = 1.5f;

    [Header("Impact / 폭발 이펙트 (옵션)")]
    public GameObject impactEffectPrefab;
    public GameObject explosionEffectPrefab;

    private int piercedCount = 0;
    private List<Monster> chainAttackedMonsters = new List<Monster>();
    private int currentBounceCount = 0;

    // VFX 패널 (정적 참조)
    private static RectTransform vfxPanel;

    // === [수정 부분] ===
    [Header("Area 구분 (1 or 2)")]
    public int areaIndex = 1;
    // =================

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
    public void Init(Monster targetMonster, float baseDamage, float baseSpeed,
                     bool areaAtk, float areaAtkRadius, int areaIndex)
    {
        this.target = targetMonster;
        this.damage = baseDamage;
        this.speed = baseSpeed;
        this.isAreaAttack = areaAtk;
        this.areaRadius = areaAtkRadius;
        this.areaIndex = areaIndex;

        if (bulletType == BulletType.Energy)
        {
            this.speed *= energySpeedFactor;
        }
    }

    private void Start()
    {
        piercedCount = 0;
        currentBounceCount = 0;
        chainAttackedMonsters.Clear();

        // ▼▼ [추가] RectTransform 초기화 (UI 상에서 총알이 안 보이는 현상 방지) ▼▼
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            // 캔버스 자식 UI로써 표시될 수 있게 기본 앵커/피봇 설정
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            // 부모 패널 크기에 맞춰 축소되지 않도록 localScale 보정
            rt.localScale = Vector3.one;

            // (선택) 가장 위로 올리기
            rt.SetAsLastSibling();
        }
        // ▲▲ [추가 끝] ▲▲
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
            {
                // ▼▼ [수정] 타겟이 없거나 이미 비활성(파괴) 상태라면 소멸 ▼▼
                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    Destroy(gameObject);
                    break;
                }
                // ▲▲ [수정끝] ▲▲

                Vector3 dir = (target.transform.position - transform.position).normalized;
                transform.position += dir * speed * Time.deltaTime;

                float dist = Vector2.Distance(transform.position, target.transform.position);
                if (dist < 0.2f)
                {
                    OnHitTarget(target);
                }
                break;
            }

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
        // ▼▼ [수정] 원래 코드에서 areaIndex 비교로 리턴하던 부분 주석 처리 ▼▼
        // if (hitMonster != null && this.areaIndex != hitMonster.areaIndex)
        // {
        //     // 다른 구역이면 데미지 적용 안 함
        //     return;
        // }
        // ▲▲ [수정끝] ▲▲

        if (hitMonster == null) return;

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
                // "같은 구역"이어야만 데미지(원래 코드)
                // if (m.areaIndex == this.areaIndex)
                // {
                //     DealDamage(m);
                // }
                // ---------------------------------
                // [수정] 필요하다면 아래처럼 바꿀 수 있음
                DealDamage(m);
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

            // 체인탄이던 에너지탄이던, 원래는 'this.areaIndex == m.areaIndex' 조건을 써서 같은 구역만 찾을 수도 있음
            // 필요에 따라 제거/수정 가능
            // if (m.areaIndex != this.areaIndex) continue;

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

                // 동일 areaIndex 유지(원한다면 바꿀 수도 있음)
                subB.areaIndex = this.areaIndex;

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
            Destroy(effect, 1f);
        }
    }
}
