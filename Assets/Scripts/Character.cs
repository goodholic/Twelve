using System.Collections;
using UnityEngine;
using UnityEngine.UI;
// using Fusion; // 임시로 주석처리

/// <summary>
/// 데미지를 받을 수 있는 모든 오브젝트(몬스터, 캐릭터)가 구현해야 하는 인터페이스
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 데미지를 받는 메서드
    /// </summary>
    /// <param name="damage">받을 데미지 양</param>
    void TakeDamage(float damage);
}

public enum CharacterStar
{
    OneStar = 1,
    TwoStar = 2,
    ThreeStar = 3
}

// [추가] 3종족 예시
public enum CharacterRace
{
    Human,
    Orc,
    Elf,
    Undead  // 언데드 종족 추가
}

// [추가] 공격 타입 enum
public enum AttackTargetType
{
    All,            // 모든 대상 공격 (기본)
    CastleOnly,     // 성만 공격
    CharacterOnly   // 캐릭터만 공격
}

// [추가] 이동 타입 enum
public enum MovementType
{
    Ground,  // 지상
    Air      // 공중
}

public enum RangeType
{
    Melee,
    Ranged,
    LongRange  // 장거리 타입 추가
}

public class Character : MonoBehaviour, IDamageable // 임시로 MonoBehaviour 사용
{
    [Header("Character Star Info (별)")]
    public CharacterStar star = CharacterStar.OneStar;

    [Tooltip("캐릭터 이름")]
    public string characterName;

    // [추가] 종족 정보
    [Header("종족 정보 (Human / Orc / Elf)")]
    public CharacterRace race = CharacterRace.Human;

    // [추가] 공격 및 이동 타입
    [Header("공격 및 이동 타입")]
    [Tooltip("공격 대상 타입")]
    public AttackTargetType attackTargetType = AttackTargetType.All;
    
    [Tooltip("이동 타입 (지상/공중)")]
    public MovementType movementType = MovementType.Ground;

    // ==================
    // 전투 스탯
    // ==================
    public float attackPower = 10f;
    public float attackSpeed = 1f;
    public float attackRange = 1.5f;
    public float currentHP = 100f;
    private float maxHP = 100f;

    // ======== 이동 속도 추가 =========
    public float moveSpeed = 3f;

    // ==================
    // 타일/몬스터 관련
    // ==================
    public Tile currentTile;
    public Monster currentTarget;
    public Character currentCharTarget;
    public MiddleCastle currentMiddleCastleTarget;  // [추가] 중간성 타겟
    public FinalCastle currentFinalCastleTarget;    // [추가] 최종성 타겟
    public bool isAreaAttack = false;
    public float areaAttackRadius = 1f;

    [Header("Hero Settings")]
    [Tooltip("주인공(히어로) 여부")]
    public bool isHero = false;

    [Header("Draggable Settings")]
    [Tooltip("드래그 가능 여부")]
    public bool isDraggable = true;

    [Header("Area 구분 (1 or 2)")]
    [Tooltip("1번 공간인지, 2번 공간인지 구분하기 위한 인덱스")]
    public int areaIndex = 1;

    // ▼▼ [추가] 선택된 루트 정보 저장
    [Header("Route Selection")]
    [Tooltip("캐릭터가 선택한 루트 (Default/Left/Center/Right)")]
    public RouteType selectedRoute = RouteType.Default;

    // 공격 형태
    [Header("공격 형태")]
    public RangeType rangeType = RangeType.Melee;

    // ===============================
    // (★ 추가) 몬스터인가? (캐릭터 공격 / 몬스터 공격 여부)
    // ===============================
    [Header("플레이어인가? (캐릭터 공격 / 몬스터 공격 여부)")]
    public bool isCharAttack = false;

    // ================================
    // 컴포넌트 참조
    // ================================
    private CharacterStats stats;
    private CharacterMovement movement;
    private CharacterCombat combat;
    private CharacterJump jumpSystem;
    private CharacterVisual visual;

    // 스프라이트 렌더러 참조 - Transform 버전으로 변경
    private SpriteRenderer spriteRenderer;

    // [추가] 웨이포인트 이동 관련
    [HideInInspector] public Transform[] pathWaypoints;
    [HideInInspector] public int currentWaypointIndex = -1;
    [HideInInspector] public int maxWaypointIndex = -1;
    
    // [추가] VFX 패널 - Transform 버전에서는 필요없음
    // [HideInInspector] public RectTransform vfxPanel;
    
    // [추가] HP바 캔버스 - World Space Canvas로 변경
    [HideInInspector] public Canvas hpBarCanvas;

    private void Awake()
    {
        // 스프라이트 렌더러 찾기 - UI Image 대신 SpriteRenderer 사용
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (spriteRenderer == null)
        {
            // SpriteRenderer가 없으면 추가
            GameObject spriteObj = new GameObject("Sprite");
            spriteObj.transform.SetParent(transform);
            spriteObj.transform.localPosition = Vector3.zero;
            spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 10; // 적절한 sorting order 설정
        }

        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[Character] {name} 에서 SpriteRenderer를 찾지 못했습니다.");
        }

        maxHP = currentHP;

        // 컴포넌트 초기화
        stats = GetComponent<CharacterStats>() ?? gameObject.AddComponent<CharacterStats>();
        movement = GetComponent<CharacterMovement>() ?? gameObject.AddComponent<CharacterMovement>();
        combat = GetComponent<CharacterCombat>() ?? gameObject.AddComponent<CharacterCombat>();
        jumpSystem = GetComponent<CharacterJump>() ?? gameObject.AddComponent<CharacterJump>();
        visual = GetComponent<CharacterVisual>() ?? gameObject.AddComponent<CharacterVisual>();

        // 컴포넌트 초기화 호출
        stats.Initialize(this);
        movement.Initialize(this, stats);
        combat.Initialize(this, stats, visual, movement, jumpSystem);
        jumpSystem.Initialize(this, movement);
        visual.Initialize(this, spriteRenderer, null); // UI Image 제거
    }

    private void Start()
    {
        // HP바 생성 - World Space Canvas 사용
        CreateHPBarWorldSpace();
    }

    private void Update()
    {
        // HP바 위치 업데이트 (캐릭터 머리 위)
        if (hpBarCanvas != null)
        {
            hpBarCanvas.transform.position = transform.position + Vector3.up * 1.5f;
            hpBarCanvas.transform.rotation = Quaternion.identity; // 항상 정면을 바라보도록
        }
    }

    /// <summary>
    /// World Space Canvas로 HP바 생성
    /// </summary>
    private void CreateHPBarWorldSpace()
    {
        if (isHero) return; // 히어로는 HP바 표시 안함

        GameObject hpBarObj = new GameObject("HPBar");
        hpBarObj.transform.SetParent(transform);
        hpBarObj.transform.localPosition = Vector3.up * 1.5f;

        // Canvas 설정
        Canvas canvas = hpBarObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        
        // Canvas 크기 설정
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1f, 0.2f);
        canvasRect.localScale = Vector3.one;

        // 배경 이미지
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(hpBarObj.transform);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRect = bgImage.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        // HP 채움 이미지
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(hpBarObj.transform);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = Color.green;
        RectTransform fillRect = fillImage.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        hpBarCanvas = canvas;
        visual.SetHPBarReferences(canvas, fillImage);
    }

    /// <summary>
    /// 데미지를 받는 메서드 (IDamageable 인터페이스 구현)
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (currentHP <= 0) return;

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        // HP바 업데이트
        visual?.UpdateHPBar(currentHP / maxHP);

        // 피격 효과
        visual?.PlayHitEffect();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 캐릭터 사망 처리
    /// </summary>
    private void Die()
    {
        Debug.Log($"[Character] {characterName} 사망!");

        // 타일에서 제거
        if (currentTile != null)
        {
            currentTile.RemoveOccupyingCharacter(this);
        }

        // 사망 효과
        visual?.PlayDeathEffect();

        // 오브젝트 제거
        Destroy(gameObject);
    }

    /// <summary>
    /// 캐릭터의 스프라이트를 설정합니다.
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    /// <summary>
    /// 캐릭터의 색상을 설정합니다.
    /// </summary>
    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    /// <summary>
    /// 별 등급에 따른 시각적 효과 적용
    /// </summary>
    public void ApplyStarVisual()
    {
        visual?.ApplyStarEffect(star);
    }

    // Transform 버전에서는 불필요한 메서드들 제거
    // public void SetBulletPanel(RectTransform panel) - 제거
    // public void SetVfxPanel(RectTransform panel) - 제거
    // public void SetHpBarCanvas(Canvas canvas) - 제거
}