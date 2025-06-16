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
    
    [Tooltip("타워형 캐릭터 여부")]
    public bool isTower = false;

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
            movement.Initialize(this, stats);
            
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
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // 어두운 회색
            
            RectTransform bgRect = hpBarBg.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(1f, 0.1f);
            bgRect.anchoredPosition = Vector2.zero;
            
            // HP바 Fill
            GameObject hpBarFill = new GameObject("HPBarFill");
            hpBarFill.transform.SetParent(hpBarBg.transform);
            hpBarFill.transform.localPosition = Vector3.zero;
            
            hpBarFillImage = hpBarFill.AddComponent<UnityEngine.UI.Image>();
            hpBarFillImage.color = GetHPBarColor();
            
            RectTransform fillRect = hpBarFill.GetComponent<RectTransform>();
            fillRect.sizeDelta = new Vector2(1f, 0.1f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.anchorMin = new Vector2(0, 0.5f);
            fillRect.anchorMax = new Vector2(0, 0.5f);
            
            UpdateHPBar();
        }
    }

    /// <summary>
    /// HP바 색상
    /// </summary>
    private Color GetHPBarColor()
    {
        if (areaIndex == 1)
            return Color.green;
        else if (areaIndex == 2)
            return Color.red;
        else
            return Color.yellow;
    }

    /// <summary>
    /// HP바 업데이트
    /// </summary>
    private void UpdateHPBar()
    {
        if (hpBarFillImage != null)
        {
            float hpRatio = currentHP / maxHP;
            hpBarFillImage.fillAmount = hpRatio;
            
            // HP 비율에 따른 색상 변경
            if (hpRatio > 0.6f)
                hpBarFillImage.color = GetHPBarColor();
            else if (hpRatio > 0.3f)
                hpBarFillImage.color = Color.yellow;
            else
                hpBarFillImage.color = Color.red;
        }
    }

    /// <summary>
    /// 데미지 받기
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isHero) 
        {
            Debug.Log($"[Character] 히어로 {characterName}은(는) 무적입니다!");
            return;
        }
        
        currentHP = Mathf.Max(0, currentHP - damage);
        UpdateHPBar();
        
        if (currentHP <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 캐릭터 사망
    /// </summary>
    private void Die()
    {
        Debug.Log($"[Character] {characterName} 사망!");
        
        // 타일에서 제거
        if (currentTile != null)
        {
            currentTile.RemoveOccupyingCharacter(this);
            
            // PlacementManager에 알림
            if (PlacementManager.Instance != null)
            {
                PlacementManager.Instance.OnCharacterRemovedFromTile(currentTile);
            }
        }
        
        // HP바 제거
        if (hpBarCanvas != null)
        {
            Destroy(hpBarCanvas.gameObject);
        }
        
        // 오브젝트 제거
        Destroy(gameObject);
    }

    /// <summary>
    /// 타일로 위치 설정
    /// </summary>
    public void SetPositionToTile(Tile tile)
    {
        if (tile != null)
        {
            transform.position = tile.transform.position;
        }
    }

    /// <summary>
    /// 총알 패널 설정
    /// </summary>
    public void SetBulletPanel(Transform panel)
    {
        opponentBulletPanel = panel;
    }

    /// <summary>
    /// 캐릭터 스프라이트 설정
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    /// <summary>
    /// 별 등급에 따른 시각 효과 적용
    /// </summary>
    public void ApplyStarVisual()
    {
        if (visual != null)
        {
            visual.ApplyStarVisual();
        }
        else
        {
            // visual 컴포넌트가 없으면 생성 후 적용
            visual = GetComponent<CharacterVisual>();
            if (visual == null)
            {
                visual = gameObject.AddComponent<CharacterVisual>();
                visual.Initialize(this, spriteRenderer);
            }
            visual.ApplyStarVisual();
        }
    }

    // 디버그용
    private void OnDrawGizmosSelected()
    {
        // 공격 범위 표시
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // 현재 타겟 표시
        if (currentCharTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentCharTarget.transform.position);
        }
    }
}