using UnityEngine;

/// <summary>
/// 총알 시스템 - 캐릭터, 몬스터, 성이 발사하는 투사체
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("총알 설정")]
    public float damage = 10f;
    public float speed = 5f;
    public float lifeTime = 5f;
    
    [Header("타겟 정보")]
    public GameObject targetObject;
    public Vector3 direction;
    public bool isHoming = false;
    public float homingStrength = 5f;
    
    [Header("소유자 정보")]
    public int ownerAreaIndex = 1;
    public bool isFromCastle = false; // 성에서 발사된 총알인지
    
    [Header("이펙트")]
    public GameObject hitEffectPrefab;
    public GameObject trailEffectPrefab;
    
    // 컴포넌트
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private TrailRenderer trailRenderer;
    
    // 상태
    private float elapsedTime = 0f;
    private bool hasHit = false;
    
    private void Awake()
    {
        // Rigidbody2D 설정
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        // Collider 설정
        col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
        }
        col.isTrigger = true;
        
        // SpriteRenderer 설정
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // 기본 스프라이트 설정 (원형)
        if (spriteRenderer.sprite == null)
        {
            // 기본 원형 스프라이트 생성
            Texture2D texture = new Texture2D(16, 16);
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(8, 8));
                    if (distance < 8)
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }
            texture.Apply();
            
            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
            spriteRenderer.color = Color.yellow;
        }
        
        // Layer 설정
        gameObject.layer = LayerMask.NameToLayer("Projectile");
        
        // Trail 효과
        if (trailEffectPrefab != null)
        {
            GameObject trail = Instantiate(trailEffectPrefab, transform);
            trailRenderer = trail.GetComponent<TrailRenderer>();
        }
    }
    
    private void Start()
    {
        // 방향이 설정되지 않았고 타겟이 있으면 타겟 방향으로 설정
        if (direction == Vector3.zero && targetObject != null)
        {
            direction = (targetObject.transform.position - transform.position).normalized;
        }
        
        // 방향으로 회전
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
    }
    
    private void Update()
    {
        if (hasHit) return;
        
        // 수명 체크
        elapsedTime += Time.deltaTime;
        if (elapsedTime >= lifeTime)
        {
            DestroyBullet();
            return;
        }
        
        // 이동
        if (isHoming && targetObject != null)
        {
            // 유도 미사일
            Vector3 targetDirection = (targetObject.transform.position - transform.position).normalized;
            direction = Vector3.Lerp(direction, targetDirection, homingStrength * Time.deltaTime).normalized;
            
            // 회전 업데이트
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
        
        // 이동
        transform.position += direction * speed * Time.deltaTime;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (other == null) return;
        
        // 자기 자신은 무시
        if (other.transform == transform.parent) return;
        
        // 몬스터 체크
        Monster monster = other.GetComponent<Monster>();
        if (monster != null && monster.IsAlive())
        {
            // 같은 지역 몬스터는 맞추지 않음
            if (monster.areaIndex != ownerAreaIndex)
            {
                monster.TakeDamage(damage);
                CreateHitEffect(other.transform.position);
                DestroyBullet();
                return;
            }
        }
        
        // 캐릭터 체크
        Character character = other.GetComponent<Character>();
        if (character != null && character.currentHP > 0)
        {
            // 같은 지역 캐릭터는 맞추지 않음
            if (character.areaIndex != ownerAreaIndex)
            {
                character.TakeDamage(damage);
                CreateHitEffect(other.transform.position);
                DestroyBullet();
                return;
            }
        }
        
        // 성 체크 (성에서 발사한 총알은 성을 맞추지 않음)
        if (!isFromCastle)
        {
            MiddleCastle middleCastle = other.GetComponent<MiddleCastle>();
            if (middleCastle != null)
            {
                if (middleCastle.areaIndex != ownerAreaIndex && !middleCastle.IsDestroyed())
                {
                    middleCastle.TakeDamage(damage);
                    CreateHitEffect(other.transform.position);
                    DestroyBullet();
                    return;
                }
            }
            
            FinalCastle finalCastle = other.GetComponent<FinalCastle>();
            if (finalCastle != null)
            {
                if (finalCastle.areaIndex != ownerAreaIndex && !finalCastle.IsDestroyed())
                {
                    finalCastle.TakeDamage(damage);
                    CreateHitEffect(other.transform.position);
                    DestroyBullet();
                    return;
                }
            }
        }
        
        // 장애물이나 벽에 부딪힌 경우
        if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            DestroyBullet();
        }
    }
    
    /// <summary>
    /// 타격 이펙트 생성
    /// </summary>
    private void CreateHitEffect(Vector3 position)
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }
    
    /// <summary>
    /// 총알 제거
    /// </summary>
    private void DestroyBullet()
    {
        hasHit = true;
        
        // 트레일이 자연스럽게 사라지도록
        if (trailRenderer != null)
        {
            trailRenderer.transform.SetParent(null);
            Destroy(trailRenderer.gameObject, trailRenderer.time);
        }
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 총알 초기화 (외부에서 호출)
    /// </summary>
    public void Initialize(float damage, float speed, GameObject target, int areaIndex, bool fromCastle = false)
    {
        this.damage = damage;
        this.speed = speed;
        this.targetObject = target;
        this.ownerAreaIndex = areaIndex;
        this.isFromCastle = fromCastle;
        
        if (target != null)
        {
            direction = (target.transform.position - transform.position).normalized;
        }
    }
    
    /// <summary>
    /// 방향 설정
    /// </summary>
    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;
        
        // 회전 업데이트
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
    }
}