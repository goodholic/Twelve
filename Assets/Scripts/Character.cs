using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 메인 클래스 - 월드 좌표 기반
/// 게임 기획서: 타워형 캐릭터 (고정 타워처럼 작동)
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
    public int level = 1;
    
    // ================================
    // 전투 스탯
    // ================================
    [Header("전투 스탯")]
    public float attackPower = 10f;
    public float attackRange = 3f;
    public float attackSpeed = 1f;
    public float currentHP = 100f;
    public float maxHP = 100f;
    public AttackTargetType attackTargetType = AttackTargetType.Both;
    public bool isAreaAttack = false;
    public float areaAttackRadius = 1.5f;
    public bool isDraggable = true;
    
    // ================================
    // 스프라이트
    // ================================
    [Header("스프라이트")]
    public Sprite characterSprite;
    public Sprite frontSprite;
    public Sprite backSprite;
    
    // ================================
    // 타일 & 이동 정보
    // ================================
    [Header("타일 & 위치")]
    public Tile currentTile;
    public int selectedRoute = -1; // -1: 미선택, 0: 왼쪽, 1: 중앙, 2: 오른쪽
    
    [Header("타겟 정보")]
    public IDamageable currentTarget;
    public Character currentCharTarget;
    
    [Header("지역 정보")]
    public int areaIndex = 1; // 1: Region1, 2: Region2
    
    [Header("특수 상태")]
    [Tooltip("Hero 여부 (무적)")]
    public bool isHero = false;
    
    [Tooltip("캐릭터 공격 / 몬스터 공격 여부")]
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

    // [추가] 웨이포인트 이동 관련
    [HideInInspector] public Transform[] pathWaypoints;
    [HideInInspector] public int currentWaypointIndex = -1;
    [HideInInspector] public int maxWaypointIndex = -1;
    
    // [추가] HP바 캔버스 - World Space Canvas로 변경
    [HideInInspector] public Canvas hpBarCanvas;
    private UnityEngine.UI.Image hpBarFillImage;
    
    // [추가] 총알 패널 참조
    [HideInInspector] public Transform opponentBulletPanel;

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
    }

    private void Start()
    {
        // 컴포넌트 초기화
        if (stats != null)
            stats.Initialize(this);
            
        if (visual != null)
            visual.Initialize(this, spriteRenderer);
            
        if (movement != null)
            movement.Initialize(this, stats, visual);
            
        if (combat != null)
            combat.Initialize(this, stats, visual, movement, jumpSystem);
            
        if (jumpSystem != null)
            jumpSystem.Initialize(this, movement);

        // 기본 스프라이트 설정
        if (spriteRenderer != null && characterSprite != null)
        {
            spriteRenderer.sprite = characterSprite;
        }

        // HP바 생성 (월드 공간)
        CreateHPBar();
        
        Debug.Log($"[Character] {characterName} 초기화 완료 - " +
                  $"별: {star}, 종족: {race}, 레벨: {level}, " +
                  $"공격력: {attackPower}, 사거리: {attackRange}");
    }

    /// <summary>
    /// HP바 생성 (월드 공간)
    /// </summary>
    private void CreateHPBar()
    {
        if (hpBarCanvas == null)
        {
            // HP바 캔버스 생성
            GameObject hpBarObj = new GameObject("HPBarCanvas");
            hpBarObj.transform.SetParent(transform);
            hpBarObj.transform.localPosition = new Vector3(0, 1.2f, 0); // 캐릭터 위
            
            hpBarCanvas = hpBarObj.AddComponent<Canvas>();
            hpBarCanvas.renderMode = RenderMode.WorldSpace;
            hpBarCanvas.sortingLayerName = "UI";
            hpBarCanvas.sortingOrder = 100;
            
            // 캔버스 크기 설정
            RectTransform canvasRect = hpBarObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1f, 0.2f);
            
            // HP바 배경
            GameObject hpBarBg = new GameObject("HPBarBG");
            hpBarBg.transform.SetParent(hpBarObj.transform);
            hpBarBg.transform.localPosition = Vector3.zero;
            
            UnityEngine.UI.Image bgImage = hpBarBg.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            RectTransform bgRect = hpBarBg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            // HP바 채움
            GameObject hpBarFill = new GameObject("HPBarFill");
            hpBarFill.transform.SetParent(hpBarObj.transform);
            hpBarFill.transform.localPosition = Vector3.zero;
            
            hpBarFillImage = hpBarFill.AddComponent<UnityEngine.UI.Image>();
            hpBarFillImage.color = Color.green;
            
            RectTransform fillRect = hpBarFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            
            // 캔버스가 항상 카메라를 바라보도록 설정
            hpBarObj.AddComponent<LookAtCamera>();
        }
        
        UpdateHPBar();
    }

    /// <summary>
    /// HP바 업데이트
    /// </summary>
    public void UpdateHPBar()
    {
        if (hpBarFillImage != null)
        {
            float hpRatio = currentHP / maxHP;
            hpBarFillImage.fillAmount = hpRatio;
            
            // HP 비율에 따른 색상 변경
            if (hpRatio > 0.7f)
                hpBarFillImage.color = Color.green;
            else if (hpRatio > 0.3f)
                hpBarFillImage.color = Color.yellow;
            else
                hpBarFillImage.color = Color.red;
        }
    }

    /// <summary>
    /// 타일 위치로 이동
    /// </summary>
    public void SetPositionToTile(Tile tile)
    {
        if (tile != null)
        {
            transform.position = tile.transform.position;
            currentTile = tile;
        }
    }

    /// <summary>
    /// IDamageable 인터페이스 구현
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (stats != null)
        {
            stats.TakeDamage(damage);
        }
    }

    /// <summary>
    /// 최대 HP 설정
    /// </summary>
    public void SetMaxHP(float hp)
    {
        maxHP = hp;
        if (currentHP > maxHP)
            currentHP = maxHP;
        UpdateHPBar();
    }

    /// <summary>
    /// 총알 패널 설정
    /// </summary>
    public void SetBulletPanel(Transform bulletPanel)
    {
        opponentBulletPanel = bulletPanel;
    }

    private void OnDestroy()
    {
        // 타일에서 참조 제거
        if (currentTile != null)
        {
            currentTile.RemoveOccupyingCharacter(this);
        }
    }
}

/// <summary>
/// 캐릭터 종족 열거형
/// </summary>
[System.Serializable]
public enum CharacterRace
{
    Human,
    Orc,
    Elf
}

/// <summary>
/// 캐릭터 별 등급 열거형
/// </summary>
[System.Serializable]
public enum CharacterStar
{
    OneStar = 1,
    TwoStar = 2,
    ThreeStar = 3
}

/// <summary>
/// 공격 타겟 타입 열거형
/// </summary>
[System.Serializable]
public enum AttackTargetType
{
    CharacterOnly,
    MonsterOnly,
    Both
}

/// <summary>
/// 카메라를 바라보는 컴포넌트 (HP바용)
/// </summary>
public class LookAtCamera : MonoBehaviour
{
    private void LateUpdate()
    {
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0);
        }
    }
}