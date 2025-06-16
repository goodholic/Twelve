using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// ================================
// 게임 기획서: 타워 디펜스 X 실시간 대전
// 캐릭터는 소환 시 고정 타워처럼 작동
// 드래그로 3라인(좌/중/우) 변경 가능
// ================================

public interface IDamageable
{
    void TakeDamage(float damage);
}

[System.Serializable]
public enum CharacterStar
{
    OneStar = 1,
    TwoStar = 2,
    ThreeStar = 3
}

[System.Serializable]
public enum CharacterRace
{
    Human,
    Orc,
    Elf
}

[System.Serializable]
public enum RouteType
{
    Left,
    Center,
    Right
}

[System.Serializable]
public enum RangeType
{
    Melee,      // 근접
    Ranged,     // 원거리
    LongRange   // 장거리
}

/// <summary>
/// 월드 좌표 기반 캐릭터 클래스
/// UI가 아닌 월드 공간에서 작동합니다.
/// </summary>
public class Character : MonoBehaviour, IDamageable
{
    // ================================
    // 기본 정보
    // ================================
    [Header("캐릭터 기본 정보")]
    public string characterName = "Unknown";
    public int characterIndex = -1;
    public CharacterRace race = CharacterRace.Human;
    public CharacterStar star = CharacterStar.OneStar;
    public Sprite characterSprite;
    
    [Header("앞뒤 이미지 (SD 캐주얼 스타일)")]
    public Sprite frontSprite;
    public Sprite backSprite;
    
    // ================================
    // 전투 스탯
    // ================================
    [Header("전투 스탯")]
    public float attackPower = 10f;
    public float attackRange = 3f;
    public float attackSpeed = 1f;
    public float currentHP = 100f;
    public float maxHP = 100f;
    
    // ================================
    // 특수 속성
    // ================================
    [Header("특수 속성")]
    public bool isHero = false;
    public int level = 1;
    public float experience = 0f;
    
    // ================================
    // 위치/타일 정보
    // ================================
    [Header("위치 정보")]
    public Tile currentTile;
    public int areaIndex = 1;
    
    [Header("전투 타겟")]
    public Monster currentTarget;
    public Character currentCharTarget;
    
    // ================================
    // 이동 플래그
    // ================================
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
    
    // [추가] HP바 캔버스 - World Space Canvas로 변경
    [HideInInspector] public Canvas hpBarCanvas;
    private UnityEngine.UI.Image hpBarFillImage;

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
            spriteRenderer.sortingLayerName = "Characters";
            spriteRenderer.sortingOrder = 10;
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
        
        // Collider 추가 (드래그용)
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
        }
        
        // DraggableCharacter 컴포넌트 추가
        if (GetComponent<DraggableCharacter>() == null)
        {
            gameObject.AddComponent<DraggableCharacter>();
        }
        
        // Layer 설정
        gameObject.layer = LayerMask.NameToLayer("Character");
    }

    private void Start()
    {
        // HP바 생성 - World Space Canvas 사용
        CreateHPBarWorldSpace();
        
        // 스프라이트 설정
        if (characterSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = characterSprite;
        }
        
        // 별 등급에 따른 시각 효과
        ApplyStarVisual();
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
        canvasRect.localScale = Vector3.one * 0.01f; // 월드 스케일 조정

        // CanvasScaler 추가
        UnityEngine.UI.CanvasScaler scaler = hpBarObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;

        // GraphicRaycaster 추가
        hpBarObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 배경 이미지
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(hpBarObj.transform);
        UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRect = bgImage.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        // HP 채움 이미지
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(hpBarObj.transform);
        UnityEngine.UI.Image fillImage = fillObj.AddComponent<UnityEngine.UI.Image>();
        fillImage.color = Color.green;
        RectTransform fillRect = fillImage.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        hpBarCanvas = canvas;
        hpBarFillImage = fillImage;
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
    
    /// <summary>
    /// 최대 HP 설정
    /// </summary>
    public void SetMaxHP(float hp)
    {
        maxHP = hp;
        if (currentHP > maxHP)
        {
            currentHP = maxHP;
        }
    }
    
    /// <summary>
    /// 캐릭터가 타일 위에 있는지 확인
    /// </summary>
    public bool IsOnTile(Tile tile)
    {
        return currentTile == tile;
    }
    
    /// <summary>
    /// 캐릭터의 월드 위치를 타일 중앙으로 설정
    /// </summary>
    public void SetPositionToTile(Tile tile)
    {
        if (tile != null)
        {
            transform.position = tile.transform.position;
            transform.position = new Vector3(transform.position.x, transform.position.y, -1f); // 캐릭터를 약간 앞으로
        }
    }
    
    /// <summary>
    /// 캐릭터 정보 디버그 출력
    /// </summary>
    public void DebugInfo()
    {
        Debug.Log($"[Character] {characterName} - " +
                  $"Race: {race}, Star: {star}, Level: {level}, " +
                  $"HP: {currentHP}/{maxHP}, ATK: {attackPower}, " +
                  $"Range: {attackRange}, Speed: {attackSpeed}, " +
                  $"Tile: {(currentTile != null ? currentTile.name : "None")}");
    }
}