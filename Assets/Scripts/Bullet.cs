using UnityEngine;

/// <summary>
/// 총알 클래스 - 캐릭터와 성이 발사하는 투사체
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("총알 설정")]
    public float damage = 10f;
    public float speed = 5f;
    public float lifetime = 5f;
    public GameObject targetObject;
    public Vector3 direction;
    
    [Header("소유자 정보")]
    public bool isFromCastle = false;
    public int ownerAreaIndex = 1;
    
    [Header("이펙트")]
    public GameObject hitEffectPrefab;
    public GameObject trailEffectPrefab;
    
    [Header("방향별 스프라이트")]
    public Sprite bulletUpDirectionSprite;
    public Sprite bulletDownDirectionSprite;
    
    [Header("소유자 및 타겟 정보")]
    public GameObject target;
    public Character owner;
    
    private float spawnTime;
    private SpriteRenderer spriteRenderer;
    private TrailRenderer trailRenderer;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // Layer 설정
        gameObject.layer = LayerMask.NameToLayer("Bullet");
        
        // Collider 설정
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = true;
        collider.radius = 0.1f;
        
        // Trail 효과 추가
        if (trailEffectPrefab == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
            trailRenderer.time = 0.2f;
            trailRenderer.startWidth = 0.1f;
            trailRenderer.endWidth = 0f;
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            
            // 소유자에 따른 색상 설정
            if (isFromCastle)
            {
                trailRenderer.startColor = new Color(1f, 0.8f, 0f, 1f); // 황금색
                trailRenderer.endColor = new Color(1f, 0.8f, 0f, 0f);
            }
            else
            {
                trailRenderer.startColor = new Color(0f, 1f, 1f, 1f); // 청록색
                trailRenderer.endColor = new Color(0f, 1f, 1f, 0f);
            }
        }
    }
    
    private void Start()
    {
        spawnTime = Time.time;
        
        // Sorting Layer 설정
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = "Bullets";
            spriteRenderer.sortingOrder = 10;
        }
    }
    
    private void Update()
    {
        // 수명 체크
        if (Time.time - spawnTime > lifetime)
        {
            DestroyBullet();
            return;
        }
        
        // 타겟이 있으면 추적
        if (targetObject != null && targetObject.activeInHierarchy)
        {
            direction = (targetObject.transform.position - transform.position).normalized;
            
            // 회전 업데이트
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
        
        // 이동
        transform.position += direction * speed * Time.deltaTime;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        
        // 같은 지역의 오브젝트는 무시
        bool shouldIgnore = false;
        
        // 몬스터 체크
        Monster monster = other.GetComponent<Monster>();
        if (monster != null)
        {
            if (monster.areaIndex == ownerAreaIndex)
            {
                shouldIgnore = true;
            }
            else if (monster.IsAlive())
            {
                monster.TakeDamage(damage);
                CreateHitEffect(other.transform.position);
                DestroyBullet();
                return;
            }
        }
        
        // 캐릭터 체크
        Character character = other.GetComponent<Character>();
        if (character != null)
        {
            if (character.areaIndex == ownerAreaIndex)
            {
                shouldIgnore = true;
            }
            else if (character.currentHP > 0)
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
    /// 총알 방향 설정
    /// </summary>
    public void SetBulletDirection(Vector3 dir)
    {
        direction = dir.normalized;
        
        // 방향에 따른 스프라이트 변경
        if (spriteRenderer != null)
        {
            if (dir.y > 0 && bulletUpDirectionSprite != null)
            {
                spriteRenderer.sprite = bulletUpDirectionSprite;
            }
            else if (dir.y < 0 && bulletDownDirectionSprite != null)
            {
                spriteRenderer.sprite = bulletDownDirectionSprite;
            }
        }
        
        // 회전 설정
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
    }
    
    /// <summary>
    /// 총알 초기화 (CharacterCombat에서 호출)
    /// </summary>
    public void Init(float damage, float speed, GameObject targetObj, int areaIndex, bool fromCastle = false)
    {
        this.damage = damage;
        this.speed = speed;
        this.target = targetObj;
        this.targetObject = targetObj;
        this.ownerAreaIndex = areaIndex;
        this.isFromCastle = fromCastle;
        
        if (targetObj != null)
        {
            direction = (targetObj.transform.position - transform.position).normalized;
            SetBulletDirection(direction);
        }
    }
    
    /// <summary>
    /// 소스 캐릭터 설정
    /// </summary>
    public void SetSourceCharacter(Character sourceCharacter)
    {
        owner = sourceCharacter;
        if (sourceCharacter != null)
        {
            ownerAreaIndex = sourceCharacter.areaIndex;
        }
    }
}