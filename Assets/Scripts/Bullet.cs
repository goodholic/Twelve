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
    public GameObject target;        // 몬스터나 캐릭터 모두 사용 가능하도록 GameObject로 변경
    private float aliveTime = 0f; // 이게 뭐지???
    
    // 모든 대상에 대해 동일한 처리를 위한 참조
    private IDamageable damageableTarget;  // 인터페이스 사용

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
    public float energySpeedFactor = 1.5f; // <-- 기존 필드 유지

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
    /// 탄환 초기화 (몬스터/캐릭터 모두 가능)
    /// 
    /// [수정사항] 
    /// 모든 타겟 유형에 동일한 설정 적용, 속도 포함
    /// </summary>
    public void Init(IDamageable targetObject, float baseDamage, float baseSpeed,
                     bool areaAtk, float areaAtkRadius, int areaIndex)
    {
        if (targetObject != null)
        {
            if (targetObject is Monster targetMonster)
            {
                this.target = targetMonster.gameObject;
                this.damageableTarget = targetMonster;
            }
            else if (targetObject is Character targetChar)
            {
                this.target = targetChar.gameObject;
                this.damageableTarget = targetChar;
            }
            
            // 모든 타겟 유형에 대해 동일한 설정 적용
            this.damage = baseDamage;
            this.speed = baseSpeed;
            this.isAreaAttack = areaAtk;
            this.areaRadius = areaAtkRadius;
            this.areaIndex = areaIndex;

            // 총알 타입별 추가 설정은 사용하지 않음 - 모든 타겟에 동일한 설정 적용
        }
    }

    /// <summary>
    /// 몬스터를 타겟으로 하는 탄환 초기화 (이전 버전과의 호환성 유지)
    /// </summary>
    public void Init(Monster targetMonster, float baseDamage, float baseSpeed,
                     bool areaAtk, float areaAtkRadius, int areaIndex)
    {
        Init((IDamageable)targetMonster, baseDamage, baseSpeed, areaAtk, areaAtkRadius, areaIndex);
    }

    /// <summary>
    /// 캐릭터를 타겟으로 하는 탄환 초기화 (이전 버전과의 호환성 유지)
    /// </summary>
    public void InitForCharacter(Character targetChar, float baseDamage, float baseSpeed,
                                bool areaAtk, float areaAtkRadius, int areaIndex)
    {
        Init((IDamageable)targetChar, baseDamage, baseSpeed, areaAtk, areaAtkRadius, areaIndex);
    }

    private void Start()
    {
        piercedCount = 0;
        currentBounceCount = 0;
        chainAttackedMonsters.Clear(); // 이전에 기록된 연쇄 공격 대상 목록도 완전히 비워서 깨끗한 상태에서 로직을 시작하기 위해서입니다.

        // ▼▼ [추가] RectTransform 초기화 (UI 상에서 총알이 안 보이는 현상 방지) ▼▼
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            // 캔버스 자식 UI로써 표시될 수 있게 기본 앵커/피봇 설정
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            // 결과적으로 이 UI는 “부모 캔버스의 중앙”에 놓이고, 너비·높이를 바꿀 때도 중심을 기준으로 동작합니다.


            // 부모 패널 크기에 맞춰 축소되지 않도록 localScale 보정
            rt.localScale = Vector3.one;

            // (선택) 가장 위로 올리기 // 같은 부모 아래 있는 UI 요소들 중에서 이 오브젝트를 맨 뒤(가장 위에 렌더링) 로 이동시킵니다. 
            // 즉, 다른 UI 위에 가려질 일이 없도록 “포그라운드”로 배치하는 기능이에요.
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
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        // 모든 타겟에 대해 동일한 이동 로직 사용
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
                Vector3 dir = (target.transform.position - transform.position).normalized;
                transform.position += dir * speed * Time.deltaTime;

                float dist = Vector2.Distance(transform.position, target.transform.position);
                if (dist < 0.2f)
                {
                    OnHitTarget(); // “이 오브젝트가 목표(target)에 충분히 가까워졌을 때” 실제로 히트 처리를 한 번만 실행해 주기 위해 넣는 수동 충돌 검사 역할
                }
                break;
            }

            case BulletType.Chain:
                Vector3 dirC = (target.transform.position - transform.position).normalized;
                transform.position += dirC * speed * Time.deltaTime;

                float distC = Vector2.Distance(transform.position, target.transform.position);
                if (distC < 0.2f)
                {
                    OnHitTarget();
                }
                break;
        }
    }

    // 모든 타입의 타겟에 대해 통합된 히트 처리
    private void OnHitTarget()
    {
        if (target == null) return;
        
        // 타겟이 제거되었거나 비활성화된 경우
        if (!target.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        switch (bulletType)
        {
            case BulletType.Normal:
                ApplyDamageToTarget(damage);
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Piercing:
                ApplyDamageToTarget(damage);
                piercedCount++;
                SpawnImpactEffect(target.transform.position);

                if (piercedCount >= maxPierceCount)
                {
                    Destroy(gameObject);
                }
                break;

            case BulletType.Explosive:
                ApplyDamageToTarget(damage);
                if (isAreaAttack)
                {
                    // 통합된 광역 데미지 적용
                    ApplyUnifiedAreaDamage(target.transform.position);
                }
                SpawnExplosionEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Slow:
                ApplyDamageToTarget(damage * 0.7f);
                // 모든 타겟에 슬로우 효과 적용 시도 (몬스터만 실제로 적용됨)
                if (damageableTarget is Monster monsterSlow)
                {
                    monsterSlow.ApplySlow(slowAmount, slowDuration);
                }
                // 캐릭터에게는 단순 데미지
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Bleed:
                ApplyDamageToTarget(damage);
                // 모든 타겟에 출혈 효과 적용 시도 (몬스터만 실제로 적용됨)
                if (damageableTarget is Monster monsterBleed)
                {
                    monsterBleed.ApplyBleed(bleedDamagePerSec, bleedDuration);
                }
                // 캐릭터에게는 단순 데미지
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Chain:
                ApplyDamageToTarget(damage);
                currentBounceCount++;
                SpawnImpactEffect(target.transform.position);

                if (currentBounceCount >= chainMaxBounces)
                {
                    Destroy(gameObject);
                }
                else
                {
                    // 통합된 다음 타겟 찾기 (몬스터/캐릭터 모두 가능)
                    FindNextUnifiedTarget(target.transform.position);
                }
                break;

            case BulletType.Stun:
                ApplyDamageToTarget(damage * 0.8f);
                // 모든 타겟에 스턴 효과 적용 시도 (몬스터만 실제로 적용됨)
                if (damageableTarget is Monster monsterStun)
                {
                    monsterStun.ApplyStun(stunDuration);
                }
                // 캐릭터에게는 단순 데미지
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.ArmorPenetration:
                // 모든 타겟에 방어력 무시 데미지 적용 (몬스터/캐릭터 동일)
                ApplyDamageToTarget(damage);
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Split:
                ApplyDamageToTarget(damage);
                // 몬스터/캐릭터 모두에게 동일하게 분열 효과 적용
                if (subBulletPrefab != null && splitCount > 0)
                {
                    DoSplit(target.transform.position);
                }
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Energy:
                // 모든 타겟에 동일한 데미지 적용 (몬스터/캐릭터 동일)
                ApplyDamageToTarget(damage * 0.8f);
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;
        }
    }
    
    // 타겟 유형에 맞게 데미지 적용 (몬스터/캐릭터 동일한 방식으로)
    private void ApplyDamageToTarget(float damageAmount)
    {
        // IDamageable 인터페이스를 통해 동일하게 데미지 적용
        if (damageableTarget != null)
        {
            damageableTarget.TakeDamage(damageAmount);
        }
    }

    // 통합된 광역 데미지 적용 (몬스터와 캐릭터 모두)
    private void ApplyUnifiedAreaDamage(Vector3 center)
    {
        if (areaRadius <= 0f) return;

        // 몬스터 광역 데미지
        Monster[] allMonsters = Object.FindObjectsByType<Monster>(FindObjectsSortMode.None);
        foreach (var m in allMonsters)
        {
            if (m == null) continue;
            if (m.gameObject == target) continue; // 이미 때린 대상은 제외
            
            float dist = Vector2.Distance(center, m.transform.position);
            if (dist <= areaRadius)
            {
                // 모든 몬스터에게 동일한 데미지 적용
                m.TakeDamage(damage);
            }
        }
        
        // 캐릭터 광역 데미지
        Character[] allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var c in allCharacters)
        {
            if (c == null) continue;
            if (c.gameObject == target) continue; // 이미 때린 대상은 제외
            if (c.isHero) continue; // 히어로는 광역 공격에서 제외
            
            float dist = Vector2.Distance(center, c.transform.position);
            if (dist <= areaRadius)
            {
                // 다른 지역의 캐릭터에게만 데미지 (몬스터와 동일한 양의 데미지)
                if (c.areaIndex != this.areaIndex)
                {
                    c.TakeDamage(damage);
                }
            }
        }
    }

    // 통합된 다음 타겟 찾기 (몬스터/캐릭터 모두 가능)
    private void FindNextUnifiedTarget(Vector3 position)
    {
        // 가장 가까운 대상 찾기(몬스터/캐릭터)
        GameObject bestTarget = null;
        IDamageable bestDamageable = null;
        float minDist = float.MaxValue; // float.MaxValue 는 “가능한 가장 큰 실수 값” 으로,
        // 첫 번째 비교 시 어떤 실제 거리값도 이 값보다 작기 때문에
        // 무조건 최초 후보가 이 minDist 를 갱신 하게 만듭니다.
        
        // 1. 몬스터 찾기
        Monster[] monsters = Object.FindObjectsByType<Monster>(FindObjectsSortMode.None);
        foreach (var m in monsters)
        {
            if (m == null) continue;
            if (m.gameObject == target) continue; // 이미 때린 대상은 제외
            
            float dist = Vector2.Distance(position, m.transform.position);
            if (dist < chainBounceRange && dist < minDist)
            {
                minDist = dist;
                bestTarget = m.gameObject;
                bestDamageable = m;
            }
        }
        
        // 2. 캐릭터 찾기
        Character[] characters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var c in characters)
        {
            if (c == null) continue;
            if (c.gameObject == target) continue; // 이미 때린 대상은 제외
            if (c.isHero) continue; // 히어로는 타겟으로 잡지 않음
            
            // 다른 지역의 캐릭터만 찾기
            if (c.areaIndex != this.areaIndex)
            {
                float dist = Vector2.Distance(position, c.transform.position);
                if (dist < chainBounceRange && dist < minDist)
                {
                    minDist = dist;
                    bestTarget = c.gameObject;
                    bestDamageable = c;
                }
            }
        }
        
        if (bestTarget != null && bestDamageable != null)
        {
            // 새 타겟 설정
            target = bestTarget;
            damageableTarget = bestDamageable;
        }
        else
        {
            // 타겟을 찾지 못했으면 총알 제거
            Destroy(gameObject);
        }
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

                // 동일 areaIndex 유지
                subB.areaIndex = this.areaIndex;
                
                // 추가 속성 설정
                subB.isAreaAttack = false;
                subB.areaRadius = 0f;

                Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                
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
