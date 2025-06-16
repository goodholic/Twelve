using System.Collections.Generic;
using UnityEngine;

public enum BulletType
{
    Normal,
    Piercing,
    Explosive,
    Chain,
    Slow,
    Bleed,
    Stun,
    ArmorPenetration,
    Split,
    Energy
}

public class Bullet : MonoBehaviour
{
    [Header("Bullet Stats")]
    public float damage = 10f;
    public float speed = 5f;
    public float maxLifeTime = 5f;
    public BulletType bulletType = BulletType.Normal;

    [Header("Special Effects")]
    public float areaRadius = 0f;
    public bool isAreaAttack = false;
    public int maxPierceCount = 3;
    public float chainRange = 3f;
    public int maxChainCount = 3;
    public float slowDuration = 2f;
    public float slowAmount = 0.5f;
    public float bleedDuration = 3f;
    public float bleedDamagePerSec = 2f;
    public float stunDuration = 1f;
    public float armorPenRatio = 0.5f;
    public int splitCount = 3;
    public GameObject subBulletPrefab;

    [Header("Movement")]
    public float arcHeight = 0f;
    public float speedMultiplier = 1f;

    private GameObject target;
    private IDamageable damageableTarget;
    private Vector3 startPosition;
    private float totalDistance;
    private float currentDistance;
    private float aliveTime = 0f;

    [Header("VFX")]
    public GameObject impactEffectPrefab;
    public GameObject explosionEffectPrefab;
    public GameObject chainEffectPrefab;

    [Header("소환된 캐릭터가 위/아래로 공격할 때 보여줄 스프라이트")]
    public Sprite bulletUpDirectionSprite;
    public Sprite bulletDownDirectionSprite;

    private SpriteRenderer spriteRenderer;

    private int piercedCount = 0;
    private List<Monster> chainAttackedMonsters = new List<Monster>();
    private int currentBounceCount = 0;

    [Header("Area 구분 (1 or 2)")]
    public int areaIndex = 1;

    private Character sourceCharacter;

    /// <summary>
    /// 탄환 초기화 (몬스터/캐릭터/성 등 IDamageable 대상)
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
            else if (targetObject is MiddleCastle targetMiddleCastle)
            {
                this.target = targetMiddleCastle.gameObject;
                this.damageableTarget = targetMiddleCastle;
            }
            else if (targetObject is FinalCastle targetFinalCastle)
            {
                this.target = targetFinalCastle.gameObject;
                this.damageableTarget = targetFinalCastle;
            }

            this.damage = baseDamage;
            this.speed = baseSpeed;
            this.isAreaAttack = areaAtk;
            this.areaRadius = areaAtkRadius;
            this.areaIndex = areaIndex;

            // 포물선 이동 초기화
            this.startPosition = transform.position;
            if (target != null)
            {
                this.totalDistance = Vector3.Distance(startPosition, target.transform.position);
            }
            this.currentDistance = 0f;
        }
    }

    /// <summary>
    /// 몬스터를 타겟으로 하는 탄환 초기화 (이전 호환)
    /// </summary>
    public void Init(Monster targetMonster, float baseDamage, float baseSpeed,
                     bool areaAtk, float areaAtkRadius, int areaIndex)
    {
        Init((IDamageable)targetMonster, baseDamage, baseSpeed, areaAtk, areaAtkRadius, areaIndex);
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    public void SetSourceCharacter(Character character)
    {
        sourceCharacter = character;
    }

    public void SetBulletType(BulletType type)
    {
        bulletType = type;
    }

    public void SetBulletDirection(Vector3 direction)
    {
        if (spriteRenderer == null) return;

        bool isUpDirection = direction.y > 0;
        
        if (isUpDirection && bulletUpDirectionSprite != null)
        {
            spriteRenderer.sprite = bulletUpDirectionSprite;
        }
        else if (!isUpDirection && bulletDownDirectionSprite != null)
        {
            spriteRenderer.sprite = bulletDownDirectionSprite;
        }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // SpriteRenderer가 없으면 생성
        if (spriteRenderer == null)
        {
            GameObject spriteObj = new GameObject("Sprite");
            spriteObj.transform.SetParent(transform);
            spriteObj.transform.localPosition = Vector3.zero;
            spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 100; // 총알이 위에 표시되도록
        }

        // 기본 색상 설정 (디버그용)
        if (spriteRenderer.sprite == null)
        {
            // 임시로 작은 원형 스프라이트 생성 (실제로는 적절한 스프라이트 에셋 사용)
            spriteRenderer.color = Color.red;
        }
    }

    private void Start()
    {
        // 방향에 따른 스프라이트 설정
        if (target != null)
        {
            Vector3 direction = (target.transform.position - transform.position).normalized;
            SetBulletDirection(direction);
        }
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
            case BulletType.Chain:
            {
                float moveDistance = speed * speedMultiplier * Time.deltaTime;
                currentDistance += moveDistance;
                float progressRatio = Mathf.Clamp01(currentDistance / totalDistance);

                // 포물선 경로 계산
                Vector3 currentPos = Vector3.Lerp(startPosition, target.transform.position, progressRatio);
                
                // 포물선 높이 적용
                if (arcHeight > 0)
                {
                    float arcHeightAtPoint = arcHeight * Mathf.Sin(progressRatio * Mathf.PI);
                    currentPos.y += arcHeightAtPoint;
                }
                
                transform.position = currentPos;

                // 총알 회전 (진행 방향을 바라보도록)
                Vector3 direction = (target.transform.position - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
                }

                // 타겟에 도달했는지 확인
                float dist = Vector3.Distance(transform.position, target.transform.position);
                if (dist < 0.2f || progressRatio >= 0.98f)
                {
                    OnHitTarget();
                }
                break;
            }
        }
    }

    private void OnHitTarget()
    {
        if (damageableTarget == null)
        {
            if (target != null)
            {
                damageableTarget = target.GetComponent<IDamageable>();
            }
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
                else
                {
                    Monster nextMonster = FindNextMonsterInLine();
                    if (nextMonster != null)
                    {
                        target = nextMonster.gameObject;
                        damageableTarget = nextMonster;
                        startPosition = transform.position;
                        totalDistance = Vector3.Distance(startPosition, target.transform.position);
                        currentDistance = 0f;
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
                break;

            case BulletType.Explosive:
                ApplyDamageToTarget(damage);
                ApplyUnifiedAreaDamage(target.transform.position);
                SpawnExplosionEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Chain:
                ApplyDamageToTarget(damage);
                chainAttackedMonsters.Add(target.GetComponent<Monster>());

                Monster nextChainTarget = FindNextChainTarget();
                if (nextChainTarget != null && currentBounceCount < maxChainCount)
                {
                    currentBounceCount++;
                    target = nextChainTarget.gameObject;
                    damageableTarget = nextChainTarget;
                    startPosition = transform.position;
                    totalDistance = Vector3.Distance(startPosition, target.transform.position);
                    currentDistance = 0f;
                    SpawnChainEffect(transform.position, target.transform.position);
                }
                else
                {
                    SpawnImpactEffect(target.transform.position);
                    Destroy(gameObject);
                }
                break;

            case BulletType.Slow:
                ApplyDamageToTarget(damage);
                ApplySlowEffect();
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Bleed:
                ApplyDamageToTarget(damage);
                ApplyBleedEffect();
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Stun:
                ApplyDamageToTarget(damage);
                ApplyStunEffect();
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.ArmorPenetration:
                ApplyDamageToTarget(damage * (1f + armorPenRatio));
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Split:
                ApplyDamageToTarget(damage);
                if (subBulletPrefab != null && splitCount > 0)
                {
                    DoSplit(target.transform.position);
                }
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Energy:
                ApplyDamageToTarget(damage * 0.8f);
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;
        }
    }

    private void ApplyDamageToTarget(float dmg)
    {
        if (damageableTarget != null)
        {
            damageableTarget.TakeDamage(dmg);
        }
    }

    private void ApplyUnifiedAreaDamage(Vector3 center)
    {
        if (areaRadius <= 0f) return;

        // 몬스터 광역 데미지
        Monster[] allMonsters = Object.FindObjectsByType<Monster>(FindObjectsSortMode.None);
        foreach (var m in allMonsters)
        {
            if (m == null) continue;
            if (m.gameObject == target) continue;
            float dist = Vector3.Distance(center, m.transform.position);
            if (dist <= areaRadius)
            {
                m.TakeDamage(damage * 0.5f);
            }
        }

        // 캐릭터 광역 데미지
        Character[] allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var ch in allCharacters)
        {
            if (ch == null) continue;
            if (ch.gameObject == target) continue;
            if (ch.areaIndex == this.areaIndex) continue;

            float dist = Vector3.Distance(center, ch.transform.position);
            if (dist <= areaRadius)
            {
                ch.TakeDamage(damage * 0.5f);
            }
        }
    }

    private Monster FindNextMonsterInLine()
    {
        Monster[] allMonsters = Object.FindObjectsByType<Monster>(FindObjectsSortMode.None);
        Monster best = null;
        float minDist = float.MaxValue;

        Vector3 myPos = transform.position;
        Vector3 dir = (target.transform.position - myPos).normalized;

        foreach (var m in allMonsters)
        {
            if (m == null || m.gameObject == target) continue;

            Vector3 toMonster = m.transform.position - myPos;
            float angle = Vector3.Angle(dir, toMonster);
            
            if (angle < 30f)
            {
                float dist = toMonster.magnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    best = m;
                }
            }
        }

        return best;
    }

    private Monster FindNextChainTarget()
    {
        Monster[] allMonsters = Object.FindObjectsByType<Monster>(FindObjectsSortMode.None);
        Monster best = null;
        float minDist = chainRange;

        foreach (var m in allMonsters)
        {
            if (m == null || chainAttackedMonsters.Contains(m)) continue;

            float dist = Vector3.Distance(target.transform.position, m.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                best = m;
            }
        }

        return best;
    }

    private void DoSplit(Vector3 splitPos)
    {
        if (subBulletPrefab == null) return;

        for (int i = 0; i < splitCount; i++)
        {
            float angle = (360f / splitCount) * i;
            Vector3 dir = Quaternion.Euler(0, 0, angle) * Vector3.right;
            
            GameObject subBullet = Instantiate(subBulletPrefab, splitPos, Quaternion.identity);
            Bullet subBulletComp = subBullet.GetComponent<Bullet>();
            
            if (subBulletComp != null)
            {
                Monster nearestMonster = FindNearestMonsterFromPosition(splitPos);
                if (nearestMonster != null)
                {
                    subBulletComp.Init(nearestMonster, damage * 0.5f, speed, false, 0f, areaIndex);
                }
                else
                {
                    Destroy(subBullet);
                }
            }
        }
    }

    private Monster FindNearestMonsterFromPosition(Vector3 pos)
    {
        Monster[] allMonsters = Object.FindObjectsByType<Monster>(FindObjectsSortMode.None);
        Monster nearest = null;
        float minDist = float.MaxValue;

        foreach (var m in allMonsters)
        {
            if (m == null) continue;
            float dist = Vector3.Distance(pos, m.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = m;
            }
        }

        return nearest;
    }

    private void ApplySlowEffect()
    {
        if (target == null) return;
        
        Monster monster = target.GetComponent<Monster>();
        if (monster != null)
        {
            monster.ApplySlow(slowDuration, slowAmount);
        }
    }

    private void ApplyBleedEffect()
    {
        if (target == null) return;
        
        Monster monster = target.GetComponent<Monster>();
        if (monster != null)
        {
            monster.ApplyBleed(bleedDuration, bleedDamagePerSec);
        }
    }

    private void ApplyStunEffect()
    {
        if (target == null) return;
        
        Monster monster = target.GetComponent<Monster>();
        if (monster != null)
        {
            monster.ApplyStun(stunDuration);
        }
    }

    private void SpawnImpactEffect(Vector3 position)
    {
        if (impactEffectPrefab != null)
        {
            GameObject effect = Instantiate(impactEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    private void SpawnExplosionEffect(Vector3 position)
    {
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * (areaRadius * 2f);
            Destroy(effect, 2f);
        }
    }

    private void SpawnChainEffect(Vector3 start, Vector3 end)
    {
        if (chainEffectPrefab != null)
        {
            GameObject effect = Instantiate(chainEffectPrefab, start, Quaternion.identity);
            
            // 체인 이펙트를 시작점에서 끝점으로 늘이기
            Vector3 direction = end - start;
            effect.transform.right = direction;
            effect.transform.localScale = new Vector3(direction.magnitude, 1f, 1f);
            
            Destroy(effect, 0.5f);
        }
    }
}