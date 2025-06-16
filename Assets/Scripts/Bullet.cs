using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 월드 좌표 기반 총알 클래스
/// 다양한 총알 타입과 효과를 지원합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("총알 기본 설정")]
    public BulletType bulletType = BulletType.Normal;
    public float damage = 10f;
    public float speed = 5f;
    public float maxLifeTime = 5f;
    
    [Header("타겟 정보")]
    public IDamageable target;
    public Vector3 targetPosition;
    
    [Header("발사 정보")]
    public Character owner;
    public Vector3 startPosition;
    
    [Header("특수 효과")]
    public float explosionRadius = 2f; // 폭발형
    public float slowAmount = 0.5f; // 둔화형
    public float slowDuration = 2f;
    public float bleedDamage = 2f; // 출혈형
    public float bleedDuration = 3f;
    public float stunDuration = 1f; // 기절형
    public int chainCount = 3; // 연쇄형
    public float chainRange = 3f;
    
    [Header("시각 효과")]
    public GameObject hitEffectPrefab;
    public GameObject explosionEffectPrefab;
    public TrailRenderer trailRenderer;
    
    [Header("포물선 설정")]
    public float arcHeight = 1f;
    public bool useParabolicPath = true;
    
    // 컴포넌트
    private SpriteRenderer spriteRenderer;
    private Collider2D col2D;
    
    // 상태
    private float aliveTime = 0f;
    private bool hasHit = false;
    private float totalDistance;
    private float currentDistance;
    private float speedMultiplier = 1f;
    
    // 연쇄 공격용
    private List<IDamageable> hitTargets = new List<IDamageable>();

    public enum BulletType
    {
        Normal,      // 일반
        Energy,      // 에너지
        Piercing,    // 관통
        Explosive,   // 폭발
        Slow,        // 둔화
        Bleed,       // 출혈
        Stun,        // 기절
        ArmorPenetration, // 방어력 무시
        Split,       // 분열
        Chain        // 연쇄
    }

    private void Awake()
    {
        col2D = GetComponent<Collider2D>();
        if (col2D == null)
        {
            col2D = gameObject.AddComponent<CircleCollider2D>();
        }
        col2D.isTrigger = true;
        
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
            spriteRenderer.sortingLayerName = "Bullets";
            spriteRenderer.sortingOrder = 100;
        }

        // TrailRenderer 설정
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
        }
        
        // Layer 설정
        gameObject.layer = LayerMask.NameToLayer("Bullet");
    }

    private void Start()
    {
        startPosition = transform.position;
        
        // 타겟이 있으면 거리 계산
        if (target != null)
        {
            GameObject targetObj = (target as MonoBehaviour)?.gameObject;
            if (targetObj != null)
            {
                targetPosition = targetObj.transform.position;
                totalDistance = Vector3.Distance(startPosition, targetPosition);
            }
        }
        else if (targetPosition != Vector3.zero)
        {
            totalDistance = Vector3.Distance(startPosition, targetPosition);
        }
        
        // 방향에 따른 스프라이트 설정
        SetBulletDirection();
        
        // 총알 타입에 따른 시각 효과
        ApplyBulletTypeVisuals();
    }

    private void Update()
    {
        aliveTime += Time.deltaTime;

        if (aliveTime >= maxLifeTime || hasHit)
        {
            DestroyBullet();
            return;
        }

        MoveBullet();
    }

    /// <summary>
    /// 총알 이동 처리
    /// </summary>
    private void MoveBullet()
    {
        if (target != null)
        {
            GameObject targetObj = (target as MonoBehaviour)?.gameObject;
            if (targetObj != null && targetObj.activeInHierarchy)
            {
                targetPosition = targetObj.transform.position;
            }
            else
            {
                // 타겟이 사라졌으면 마지막 위치로 계속 이동
                target = null;
            }
        }

        float moveDistance = speed * speedMultiplier * Time.deltaTime;
        currentDistance += moveDistance;
        float progressRatio = Mathf.Clamp01(currentDistance / totalDistance);

        if (useParabolicPath && bulletType != BulletType.Energy)
        {
            // 포물선 경로
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, progressRatio);
            float height = arcHeight * 4f * progressRatio * (1f - progressRatio);
            currentPos.y += height;
            transform.position = currentPos;
        }
        else
        {
            // 직선 경로
            Vector3 direction = (targetPosition - transform.position).normalized;
            transform.position += direction * moveDistance;
        }

        // 회전 업데이트
        UpdateRotation();

        // 목표 도달 확인
        if (progressRatio >= 1f || Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            OnReachTarget();
        }
    }

    /// <summary>
    /// 총알 회전 업데이트
    /// </summary>
    private void UpdateRotation()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
    }

    /// <summary>
    /// 목표 도달 시 처리
    /// </summary>
    private void OnReachTarget()
    {
        if (hasHit) return;
        
        hasHit = true;
        
        // 타겟에 데미지 적용
        if (target != null)
        {
            ApplyDamage(target);
        }
        
        // 범위 공격 확인
        if (bulletType == BulletType.Explosive)
        {
            ApplyExplosiveDamage();
        }
        else if (bulletType == BulletType.Chain)
        {
            StartCoroutine(ChainAttack());
        }
        else
        {
            DestroyBullet();
        }
    }

    /// <summary>
    /// 충돌 처리
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        
        // 자기 자신이나 같은 팀은 무시
        if (other.gameObject == owner?.gameObject) return;
        
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            // 팀 확인
            Character otherChar = other.GetComponent<Character>();
            Monster otherMonster = other.GetComponent<Monster>();
            
            bool shouldHit = false;
            
            if (owner != null)
            {
                // 캐릭터가 쏜 총알
                if (owner.isCharAttack && otherChar != null && otherChar.areaIndex != owner.areaIndex)
                {
                    shouldHit = true; // 다른 지역 캐릭터 공격
                }
                else if (!owner.isCharAttack && otherMonster != null)
                {
                    shouldHit = true; // 몬스터 공격
                }
            }
            
            if (shouldHit)
            {
                target = damageable;
                targetPosition = other.transform.position;
                OnReachTarget();
            }
        }
    }

    /// <summary>
    /// 데미지 적용
    /// </summary>
    private void ApplyDamage(IDamageable target)
    {
        if (target == null) return;
        
        float finalDamage = damage;
        
        // 총알 타입별 추가 효과
        switch (bulletType)
        {
            case BulletType.ArmorPenetration:
                finalDamage *= 1.5f; // 방어력 무시 보너스
                break;
                
            case BulletType.Slow:
                ApplySlowEffect(target);
                break;
                
            case BulletType.Bleed:
                ApplyBleedEffect(target);
                break;
                
            case BulletType.Stun:
                ApplyStunEffect(target);
                break;
        }
        
        target.TakeDamage(finalDamage);
        hitTargets.Add(target);
        
        // 피격 효과
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    /// <summary>
    /// 폭발 데미지 적용
    /// </summary>
    private void ApplyExplosiveDamage()
    {
        // 폭발 효과
        if (explosionEffectPrefab != null)
        {
            GameObject explosion = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 2f);
        }
        
        // 범위 내 모든 적에게 데미지
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var col in colliders)
        {
            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable != null && !hitTargets.Contains(damageable))
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                float damageRatio = 1f - (distance / explosionRadius);
                damageable.TakeDamage(damage * damageRatio);
                hitTargets.Add(damageable);
            }
        }
        
        DestroyBullet();
    }

    /// <summary>
    /// 연쇄 공격
    /// </summary>
    private IEnumerator ChainAttack()
    {
        int currentChain = 0;
        Vector3 lastPosition = transform.position;
        
        while (currentChain < chainCount)
        {
            // 가장 가까운 적 찾기
            IDamageable nextTarget = FindNearestEnemy(lastPosition, chainRange);
            if (nextTarget == null) break;
            
            // 체인 이펙트 생성
            GameObject chainBullet = Instantiate(gameObject);
            Bullet chainComponent = chainBullet.GetComponent<Bullet>();
            chainComponent.target = nextTarget;
            chainComponent.targetPosition = (nextTarget as MonoBehaviour).transform.position;
            chainComponent.startPosition = lastPosition;
            chainComponent.damage = damage * 0.7f; // 연쇄 데미지 감소
            chainComponent.bulletType = BulletType.Normal; // 무한 연쇄 방지
            chainComponent.owner = owner;
            
            lastPosition = chainComponent.targetPosition;
            currentChain++;
            
            yield return new WaitForSeconds(0.1f);
        }
        
        DestroyBullet();
    }

    /// <summary>
    /// 가장 가까운 적 찾기
    /// </summary>
    private IDamageable FindNearestEnemy(Vector3 position, float range)
    {
        IDamageable nearest = null;
        float minDistance = float.MaxValue;
        
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, range);
        foreach (var col in colliders)
        {
            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable != null && !hitTargets.Contains(damageable))
            {
                float distance = Vector3.Distance(position, col.transform.position);
                if (distance < minDistance)
                {
                    // 팀 확인
                    bool isEnemy = false;
                    Character targetChar = col.GetComponent<Character>();
                    Monster targetMonster = col.GetComponent<Monster>();
                    
                    if (owner != null)
                    {
                        if (owner.isCharAttack && targetChar != null && targetChar.areaIndex != owner.areaIndex)
                            isEnemy = true;
                        else if (!owner.isCharAttack && targetMonster != null)
                            isEnemy = true;
                    }
                    
                    if (isEnemy)
                    {
                        minDistance = distance;
                        nearest = damageable;
                    }
                }
            }
        }
        
        return nearest;
    }

    /// <summary>
    /// 둔화 효과 적용
    /// </summary>
    private void ApplySlowEffect(IDamageable target)
    {
        MonoBehaviour targetMono = target as MonoBehaviour;
        if (targetMono != null)
        {
            StartCoroutine(SlowCoroutine(targetMono));
        }
    }

    private IEnumerator SlowCoroutine(MonoBehaviour target)
    {
        CharacterMovement movement = target.GetComponent<CharacterMovement>();
        if (movement != null)
        {
            float originalSpeed = movement.moveSpeed;
            movement.moveSpeed *= slowAmount;
            
            yield return new WaitForSeconds(slowDuration);
            
            if (movement != null)
                movement.moveSpeed = originalSpeed;
        }
    }

    /// <summary>
    /// 출혈 효과 적용
    /// </summary>
    private void ApplyBleedEffect(IDamageable target)
    {
        StartCoroutine(BleedCoroutine(target));
    }

    private IEnumerator BleedCoroutine(IDamageable target)
    {
        float elapsed = 0f;
        int ticks = 0;
        
        while (elapsed < bleedDuration && target != null)
        {
            if (ticks > 0) // 첫 틱은 건너뛰기
            {
                target.TakeDamage(bleedDamage);
            }
            
            ticks++;
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }
    }

    /// <summary>
    /// 기절 효과 적용
    /// </summary>
    private void ApplyStunEffect(IDamageable target)
    {
        MonoBehaviour targetMono = target as MonoBehaviour;
        if (targetMono != null)
        {
            CharacterMovement movement = targetMono.GetComponent<CharacterMovement>();
            if (movement != null)
            {
                movement.StopMoving();
                targetMono.StartCoroutine(StunRecoveryCoroutine(movement));
            }
        }
    }

    private IEnumerator StunRecoveryCoroutine(CharacterMovement movement)
    {
        yield return new WaitForSeconds(stunDuration);
        
        if (movement != null)
        {
            movement.StartMoving();
        }
    }

    /// <summary>
    /// 총알 방향 설정
    /// </summary>
    private void SetBulletDirection()
    {
        if (spriteRenderer == null) return;
        
        Vector3 direction = (targetPosition - transform.position).normalized;
        
        // 위/아래 방향에 따른 스프라이트 변경
        CharacterCombat combat = owner?.GetComponent<CharacterCombat>();
        if (combat != null)
        {
            if (direction.y > 0 && combat.bulletUpDirectionSprite != null)
            {
                spriteRenderer.sprite = combat.bulletUpDirectionSprite;
            }
            else if (direction.y < 0 && combat.bulletDownDirectionSprite != null)
            {
                spriteRenderer.sprite = combat.bulletDownDirectionSprite;
            }
        }
    }

    /// <summary>
    /// 총알 타입별 시각 효과
    /// </summary>
    private void ApplyBulletTypeVisuals()
    {
        if (spriteRenderer == null) return;
        
        switch (bulletType)
        {
            case BulletType.Energy:
                spriteRenderer.color = Color.cyan;
                if (trailRenderer != null)
                {
                    trailRenderer.startColor = Color.cyan;
                    trailRenderer.endColor = Color.blue;
                }
                break;
                
            case BulletType.Explosive:
                spriteRenderer.color = Color.red;
                transform.localScale *= 1.5f;
                break;
                
            case BulletType.Piercing:
                spriteRenderer.color = Color.yellow;
                speedMultiplier = 1.5f;
                break;
                
            case BulletType.Slow:
                spriteRenderer.color = Color.blue;
                break;
                
            case BulletType.Bleed:
                spriteRenderer.color = new Color(0.5f, 0f, 0f);
                break;
                
            case BulletType.Stun:
                spriteRenderer.color = Color.magenta;
                break;
                
            case BulletType.Chain:
                spriteRenderer.color = Color.green;
                if (trailRenderer != null)
                {
                    trailRenderer.startColor = Color.green;
                    trailRenderer.endColor = Color.yellow;
                }
                break;
        }
    }

    /// <summary>
    /// 총알 제거
    /// </summary>
    private void DestroyBullet()
    {
        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;
        }
        
        Destroy(gameObject, 0.1f);
    }

    /// <summary>
    /// 디버그용 기즈모
    /// </summary>
    private void OnDrawGizmos()
    {
        if (bulletType == BulletType.Explosive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}