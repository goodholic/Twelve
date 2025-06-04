using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

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

public class Character : NetworkBehaviour, IDamageable
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

    // 스프라이트 렌더러 참조
    private SpriteRenderer spriteRenderer;
    private Image uiImage;

    // [추가] 웨이포인트 이동 관련
    [HideInInspector] public Transform[] pathWaypoints;
    [HideInInspector] public int currentWaypointIndex = -1;
    [HideInInspector] public int maxWaypointIndex = -1;
    
    // [추가] 패널 참조
    [HideInInspector] public RectTransform bulletPanel;
    [HideInInspector] public RectTransform opponentBulletPanel;
    
    // [추가] VFX 패널
    [HideInInspector] public RectTransform vfxPanel;
    
    // [추가] HP바 캔버스
    [HideInInspector] public Canvas hpBarCanvas;

    private void Awake()
    {
        // 스프라이트 렌더러 찾기
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            uiImage = GetComponentInChildren<Image>();
        }

        if (spriteRenderer == null && uiImage == null)
        {
            Debug.LogWarning($"[Character] {name} 에서 SpriteRenderer나 UI Image 둘 다 찾지 못했습니다.", this);
        }

        // 컴포넌트 초기화
        stats = GetComponent<CharacterStats>();
        if (stats == null) stats = gameObject.AddComponent<CharacterStats>();

        movement = GetComponent<CharacterMovement>();
        if (movement == null) movement = gameObject.AddComponent<CharacterMovement>();

        combat = GetComponent<CharacterCombat>();
        if (combat == null) combat = gameObject.AddComponent<CharacterCombat>();

        jumpSystem = GetComponent<CharacterJump>();
        if (jumpSystem == null) jumpSystem = gameObject.AddComponent<CharacterJump>();

        visual = GetComponent<CharacterVisual>();
        if (visual == null) visual = gameObject.AddComponent<CharacterVisual>();
    }

    private void Start()
    {
        // 스탯 초기화
        stats.InitializeStats(this);
        maxHP = currentHP;

        // 비주얼 초기화
        visual.Initialize(this);

        // 전투 시스템 초기화
        combat.Initialize(this, stats, visual, movement, jumpSystem);

        // 이동 시스템 초기화
        movement.Initialize(this, visual, jumpSystem);

        // 점프 시스템 초기화
        jumpSystem.Initialize(this, movement);
    }

    private void Update()
    {
        // 이동 처리
        movement.HandleMovement();
    }

    // IDamageable 구현
    public void TakeDamage(float damage)
    {
        stats.TakeDamage(damage);
    }

    // 외부에서 접근 가능한 메서드들
    public float GetMaxHP() => maxHP;
    public void SetMaxHP(float value) => maxHP = value;

    public SpriteRenderer GetSpriteRenderer() => spriteRenderer;
    public Image GetUIImage() => uiImage;
    
    // [추가] 패널 설정 메서드
    public void SetBulletPanel(RectTransform panel)
    {
        bulletPanel = panel;
    }
    
    // [추가] 별 비주얼 적용
    public void ApplyStarVisual()
    {
        if (visual != null)
        {
            visual.ApplyStarVisual();
        }
        else
        {
            Debug.LogWarning($"[Character] {characterName} ApplyStarVisual: visual 컴포넌트가 null입니다.");
        }
    }
    
    // [추가] 현재 타겟이 있는지 확인
    public bool HasValidTarget()
    {
        return currentTarget != null || currentCharTarget != null || 
               currentMiddleCastleTarget != null || currentFinalCastleTarget != null;
    }
    
    // [추가] 모든 타겟 초기화
    public void ClearAllTargets()
    {
        currentTarget = null;
        currentCharTarget = null;
        currentMiddleCastleTarget = null;
        currentFinalCastleTarget = null;
    }

    // 캐릭터 파괴 시 정리
    private void OnDestroy()
    {
        // 타일 참조 정리
        if (currentTile != null)
        {
            if (currentTile.gameObject != null && currentTile.gameObject.activeInHierarchy)
            {
                Debug.Log($"[Character] {characterName} 파괴됨 - {currentTile.name} 타일 참조 정리");
                
                Tile destroyedTile = currentTile;
                
                if (PlacementManager.Instance != null && PlacementManager.Instance.gameObject != null && PlacementManager.Instance.gameObject.activeInHierarchy)
                {
                    if (destroyedTile.IsPlaceTile() || destroyedTile.IsPlaced2())
                    {
                        destroyedTile.RefreshTileVisual();
                    }
                    else
                    {
                        TileManager.Instance.RemovePlaceTileChild(destroyedTile);
                    }
                    
                    TileManager.Instance.OnCharacterRemovedFromTile(destroyedTile);
                }
            }
            
            currentTile = null;
        }
        
        // 모든 참조 해제
        ClearAllTargets();
    }
}