using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 총알 컴포넌트
/// 게임 기획서: 타워형 캐릭터의 원거리 공격 투사체
/// </summary>
public class Bullet : MonoBehaviour
{
    // 기본 속성
    [HideInInspector] public IDamageable target;
    [HideInInspector] public Character owner;
    [HideInInspector] public float damage;
    
    [Header("총알 설정")]
    public float speed = 5f;
    public float maxLifetime = 5f;
    
    // 방향별 스프라이트
    [HideInInspector] public Sprite bulletUpDirectionSprite;
    [HideInInspector] public Sprite bulletDownDirectionSprite;
    
    // 범위 공격
    private bool isAreaAttack = false;
    private float areaRadius = 1.5f;
    private int ownerAreaIndex = 1;
    
    // 특수 효과
    [Header("특수 효과")]
    public bool hasPoisonEffect = false;
    public float poisonDamage = 2f;
    public float poisonDuration = 3f;
    
    public bool hasSlowEffect = false;
    public float slowAmount = 0.5f;
    public float slowDuration = 2f;
    
    public bool hasBleedEffect = false;
    public float bleedDamage = 3f;
    public float bleedDuration = 5f;
    
    public bool hasStunEffect = false;
    public float stunDuration = 1f;
    
    // VFX 패널
    private static Transform vfxPanel;
    
    // 컴포넌트
    private SpriteRenderer spriteRenderer;
    private Character sourceCharacter;
    private Vector3 targetPosition;
    private float lifetime = 0f;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // 콜라이더 설정
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.2f;
            collider.isTrigger = true;
        }
        
        // Rigidbody 설정
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }
    
    /// <summary>
    /// 총알 초기화
    /// </summary>
    public void Init(IDamageable target, float damage, float speed, bool isAreaAttack, float areaRadius, int ownerAreaIndex)
    {
        this.target = target;
        this.damage = damage;
        this.speed = speed;
        this.isAreaAttack = isAreaAttack;
        this.areaRadius = areaRadius;
        this.ownerAreaIndex = ownerAreaIndex;
        
        // 타겟 위치 저장
        MonoBehaviour targetMono = target as MonoBehaviour;
        if (targetMono != null)
        {
            targetPosition = targetMono.transform.position;
        }
        
        // 총알 방향 설정
        SetBulletDirection((targetPosition - transform.position).normalized);
        
        Debug.Log($"[Bullet] 총알 생성 - 데미지: {damage}, 속도: {speed}, 범위공격: {isAreaAttack}");
    }
    
    /// <summary>
    /// 소스 캐릭터 설정
    /// </summary>
    public void SetSourceCharacter(Character character)
    {
        sourceCharacter = character;
        owner = character;
    }
    
    /// <summary>
    /// VFX 패널 설정 (정적 메서드)
    /// </summary>
    public static void SetVfxPanel(Transform panel)
    {
        vfxPanel = panel;
    }
    
    private void Update()
    {
        lifetime += Time.deltaTime;
        
        // 최대 생존 시간 체크
        if (lifetime >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }
        
        // 타겟이 없으면 파괴
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }
        
        // 타겟 위치로 이동
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        
        // 타겟에 도달했는지 체크
        if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
        {
            OnReachTarget();
        }
    }
    
    /// <summary>
    /// 타겟에 도달했을 때
    /// </summary>
    private void OnReachTarget()
    {
        if (isAreaAttack)
        {
            // 범위 공격
            ApplyAreaDamage();
        }
        else
        {
            // 단일 타겟 공격
            if (target != null)
            {
                ApplyDamageToTarget(target);
            }
        }
        
        // 폭발 효과 재생
        PlayHitEffect();
        
        // 총알 파괴
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 충돌 처리
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 타겟과 충돌했는지 확인
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null && damageable == target)
        {
            OnReachTarget();
        }
    }
    
    /// <summary>
    /// 단일 타겟에 데미지 적용
    /// </summary>
    private void ApplyDamageToTarget(IDamageable target)
    {
        if (target == null) return;
        
        target.TakeDamage(damage);
        
        // 특수 효과 적용
        if (hasPoisonEffect)
            ApplyPoisonEffect(target);
        
        if (hasSlowEffect)
            ApplySlowEffect(target);
            
        if (hasBleedEffect)
            ApplyBleedEffect(target);
            
        if (hasStunEffect)
            ApplyStunEffect(target);
    }
    
    /// <summary>
    /// 범위 데미지 적용
    /// </summary>
    private void ApplyAreaDamage()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, areaRadius);
        
        foreach (var collider in colliders)
        {
            IDamageable damageable = collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // 적 타겟인지 확인
                Character targetChar = collider.GetComponent<Character>();
                Monster targetMonster = collider.GetComponent<Monster>();
                
                bool isValidTarget = false;
                
                if (targetChar != null && targetChar.areaIndex != ownerAreaIndex)
                    isValidTarget = true;
                    
                if (targetMonster != null && targetMonster.areaIndex != ownerAreaIndex)
                    isValidTarget = true;
                
                if (isValidTarget)
                {
                    ApplyDamageToTarget(damageable);
                }
            }
        }
    }
    
    /// <summary>
    /// 타격 효과 재생
    /// </summary>
    private void PlayHitEffect()
    {
        // VFX 재생 (구현 필요)
        Debug.Log($"[Bullet] 타격 효과 재생 at {transform.position}");
    }
    
    /// <summary>
    /// 독 효과 적용
    /// </summary>
    private void ApplyPoisonEffect(IDamageable target)
    {
        MonoBehaviour targetMono = target as MonoBehaviour;
        if (targetMono != null)
        {
            targetMono.StartCoroutine(PoisonCoroutine(target));
        }
    }
    
    private IEnumerator PoisonCoroutine(IDamageable target)
    {
        float elapsed = 0f;
        int ticks = 0;
        
        while (elapsed < poisonDuration && target != null)
        {
            if (ticks > 0) // 첫 틱은 건너뛰기
            {
                target.TakeDamage(poisonDamage);
            }
            
            ticks++;
            yield return new WaitForSeconds(1f);
            elapsed += 1f;
        }
    }
    
    /// <summary>
    /// 슬로우 효과 적용
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
    public void SetBulletDirection(Vector3 direction)
    {
        if (spriteRenderer == null) return;
        
        // 위/아래 방향에 따른 스프라이트 변경
        if (direction.y > 0 && bulletUpDirectionSprite != null)
        {
            spriteRenderer.sprite = bulletUpDirectionSprite;
        }
        else if (direction.y < 0 && bulletDownDirectionSprite != null)
        {
            spriteRenderer.sprite = bulletDownDirectionSprite;
        }
        
        // 총알 회전
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
    }
    
    private void OnDrawGizmos()
    {
        if (isAreaAttack)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, areaRadius);
        }
    }
}