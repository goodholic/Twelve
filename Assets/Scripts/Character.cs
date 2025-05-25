///////////////////////////////
// Assets\Scripts\Character.cs
///////////////////////////////

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
    Elf
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
    public float currentHP = 100f;  // (기존 필드)
    private float maxHP = 100f;     // 체력바 비율 계산용

    // ======== 이동 속도 추가 =========
    public float moveSpeed = 3f;
    // ===============================

    // ==================
    // 타일/몬스터 관련
    // ==================
    public Tile currentTile;
    public Monster currentTarget;
    public Character currentCharTarget;
    public bool isAreaAttack = false;
    public float areaAttackRadius = 1f;

    [Header("Hero Settings")]
    [Tooltip("주인공(히어로) 여부")]
    public bool isHero = false;

    [Header("Path Waypoints for Auto Movement")]
    public Transform[] pathWaypoints;

    /// <summary>
    /// 현재 웨이포인트 인덱스 (기본 -1이면 이동 안 함)
    /// </summary>
    public int currentWaypointIndex = -1;

    [Header("[추가] 최대 웨이포인트 인덱스 (기본=6)")]
    public int maxWaypointIndex = 6;

    // 사거리 표시용
    [Header("Range Indicator Settings")]
    public GameObject rangeIndicatorPrefab;
    public bool showRangeIndicator = true;
    private GameObject rangeIndicatorInstance;

    // 총알 발사
    [Header("Bullet Settings")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 5f;

    // 방향에 따른 총알 스프라이트
    [Header("방향별 총알 스프라이트")]
    public Sprite bulletUpDirectionSprite;   // 위쪽으로 발사할 때 사용할 스프라이트
    public Sprite bulletDownDirectionSprite; // 아래쪽으로 발사할 때 사용할 스프라이트

    // (기존) 탄환 들어갈 패널
    private RectTransform bulletPanel;

    // ================================
    // (추가) 지역2 전용 탄환 패널
    // ================================
    [Tooltip("지역2 (areaIndex=2) 캐릭터 총알이 들어갈 Opponent BulletPanel")]
    public RectTransform opponentBulletPanel;

    // (2성/3성 아웃라인 등)
    [Header("2성 아웃라인")]
    public Color twoStarOutlineColor = Color.yellow;
    [Range(0f, 10f)] public float twoStarOutlineWidth = 1f;

    [Header("3성 아웃라인")]
    public Color threeStarOutlineColor = Color.cyan;
    [Range(0f, 10f)] public float threeStarOutlineWidth = 1.5f;

    private SpriteRenderer spriteRenderer;
    private Image uiImage;

    // 공격 쿨타임
    private float attackCooldown;

    // === [수정 부분] ===
    [Header("Area 구분 (1 or 2)")]
    [Tooltip("1번 공간인지, 2번 공간인지 구분하기 위한 인덱스")]
    public int areaIndex = 1;

    // === [추가] 캐릭터 머리 위 HP 바 표시 관련 ===
    [Header("Overhead HP Bar")]
    [Tooltip("캐릭터 머리 위에 표시할 HP Bar용 Canvas(또는 UI)")]
    public Canvas hpBarCanvas;
    [Tooltip("HP Bar Fill Image (체력 비율)")]
    public Image hpFillImage;

    // ▼▼ [추가] 선택된 루트 정보 저장
    [Header("Route Selection")]
    [Tooltip("캐릭터가 선택한 루트 (Left/Center/Right)")]
    public RouteType selectedRoute = RouteType.Center;

    // ▼▼ [수정추가] 새 공격 범위
    [Header("클릭 시 변경될 새로운 공격 범위(예: 4.0f)")]
    public float newAttackRangeOnClick = 4f;

    // ==== [새로 추가된 2성/3성 별 프리팹] ====
    [Header("별 모양(Star) 프리팹")]
    public GameObject star2Prefab;
    public GameObject star3Prefab;
    private GameObject starVisualInstance;

    // ▼▼ [수정추가] 공격 형태를 표시하기 위한 필드
    [Header("공격 형태")]
    public RangeType rangeType = RangeType.Melee;

    // ▼▼ [수정추가] 클릭 시, 캐릭터 옆에 "사거리/공격형태" 텍스트 표시
    [Header("공격 정보 표시용 UI")]
    [Tooltip("공격 범위/형태를 보여줄 TextMeshProUGUI (씬에서 연결)")]
    public TMPro.TextMeshProUGUI attackInfoText;
    [Tooltip("공격 정보 텍스트 위치 오프셋(월드 좌표)")]
    public Vector3 attackInfoOffset = new Vector3(1f, 0f, 0f);

    // ===============================
    // (★ 추가) 몬스터인가? (캐릭터 공격 / 몬스터 공격 여부)
    // ===============================
    [Header("플레이어인가? (캐릭터 공격 / 몬스터 공격 여부)")]
    public bool isCharAttack = false;

    // ================================
    // [수정 추가] 상대 지역으로 점프 이동 시
    //            웨이브 스포너 참조
    // ================================
    private WaveSpawner waveSpawner;              // 지역1 Wave
    private WaveSpawnerRegion2 waveSpawnerRegion; // 지역2 Wave

    // ================================
    // [수정 추가] 점프 모션 제어(선택)
    // ================================
    private CharacterJumpController jumpController;
    private bool isJumpingAcross = false; // 점프 중 여부

    // -------------------------------------------------------
    // (★ 추가) 한 번이라도 Region1→Region2 (또는 반대)로 넘어갔는지
    //           + 넘어간 뒤에는 walkable/walkable2를 따라가지 않음
    //           + 공격 시에는 특정 태그("Opponent Player"/"Player") 추적
    // -------------------------------------------------------
    public bool hasCrossedRegion = false;
    private string chaseTag = null;

    // [추가] 태그들
    [Header("다른 지역 캐릭터 태그 (공격용)")]
    public string playerTag = "Player";
    public string opponentPlayerTag = "Opponent Player";
    public string characterTag = "Character";

    // =========================
    // [수정] endPos를 인스펙터에서
    //        지정할 RectTransform
    // =========================
    [Header("End Tile Rect (Inspector에서 지정)")]
    [SerializeField] private RectTransform endTileRect; // [수정 추가]

    // [추가] 캔버스의 자식 오브젝트를 직접 참조
    [Header("점프할 대상 위치 (Canvas의 자식 오브젝트)")]
    [SerializeField] private Transform targetJumpPosition;

    [Header("점프 시작 위치 (Canvas의 자식 오브젝트)")]
    [SerializeField] private Transform startJumpPosition;

    // ================================
    // [수정] (아래) 위/아래 방향 캐릭터 스프라이트 추가
    // ================================
    [Header("소환된 캐릭터가 위/아래로 이동/공격할 때 보여줄 스프라이트(추가)")]
    public Sprite characterUpDirectionSprite;    // [추가] 위쪽 방향 캐릭터 스프라이트
    public Sprite characterDownDirectionSprite;  // [추가] 아래쪽 방향 캐릭터 스프라이트

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            uiImage = GetComponentInChildren<Image>();
        }

        if (spriteRenderer == null && uiImage == null)
        {
            Debug.LogWarning($"[Character] {name} 에서 SpriteRenderer나 UI Image 둘 다 찾지 못했습니다.", this);
        }
    }

    private void Start()
    {
        // 별에 따른 기초 스탯 보정
        switch (star)
        {
            case CharacterStar.OneStar:
                break;
            case CharacterStar.TwoStar:
                attackPower *= 1.3f;
                attackRange *= 1.1f;
                attackSpeed *= 1.1f;
                currentHP   *= 1.2f;
                break;
            case CharacterStar.ThreeStar:
                attackPower *= 1.6f;
                attackRange *= 1.2f;
                attackSpeed *= 1.2f;
                currentHP   *= 1.4f;
                break;
        }

        attackCooldown = 1f / attackSpeed;
        maxHP = currentHP;

        // 히어로면 HP바 비활성화, 아니면 표시
        if (isHero)
        {
            if (hpBarCanvas != null)
            {
                hpBarCanvas.gameObject.SetActive(false);
            }
        }
        else
        {
            if (hpBarCanvas != null)
            {
                hpBarCanvas.gameObject.SetActive(true);
            }
            UpdateHpBar();
        }

        // 사거리 표시
        CreateRangeIndicator();
        
        // bulletPanel이 초기화되지 않았으면 찾기 시도
        if (bulletPanel == null)
        {
            // "BulletPanel"이라는 이름의 오브젝트 찾기 시도
            GameObject bulletPanelObj = GameObject.Find("BulletPanel");
            if (bulletPanelObj != null)
            {
                bulletPanel = bulletPanelObj.GetComponent<RectTransform>();
                Debug.Log($"[Character] {characterName}의 bulletPanel을 자동으로 찾음: {bulletPanelObj.name}");
            }
            
            // 찾지 못했으면 캔버스의 자식에서 찾기 시도
            if (bulletPanel == null)
            {
                Canvas mainCanvas = FindFirstObjectByType<Canvas>();
                if (mainCanvas != null)
                {
                    foreach (RectTransform rt in mainCanvas.GetComponentsInChildren<RectTransform>())
                    {
                        if (rt.name.Contains("Bullet") || rt.name.Contains("bullet") || rt.name.Contains("Panel"))
                        {
                            bulletPanel = rt;
                            Debug.Log($"[Character] {characterName}의 bulletPanel을 캔버스에서 찾음: {rt.name}");
                            break;
                        }
                    }
                    
                    // 그래도 찾지 못했으면 생성
                    if (bulletPanel == null)
                    {
                        GameObject newPanel = new GameObject("BulletPanel");
                        newPanel.transform.SetParent(mainCanvas.transform, false);
                        bulletPanel = newPanel.AddComponent<RectTransform>();
                        bulletPanel.anchorMin = Vector2.zero;
                        bulletPanel.anchorMax = Vector2.one;
                        bulletPanel.offsetMin = Vector2.zero;
                        bulletPanel.offsetMax = Vector2.zero;
                        Debug.Log($"[Character] {characterName}을 위한 새 bulletPanel 생성");
                    }
                }
            }
        }

        // 공격 루틴
        StartCoroutine(AttackRoutine());

        // 2성/3성 별 표시
        ApplyStarVisual();

        // ▼▼ [수정추가] WaveSpawner / WaveSpawnerRegion2 찾기
        waveSpawner = FindFirstObjectByType<WaveSpawner>();
        waveSpawnerRegion = FindFirstObjectByType<WaveSpawnerRegion2>();

        // ▼▼ [수정추가] JumpController(선택)
        jumpController = GetComponent<CharacterJumpController>();
        if (jumpController == null)
        {
            // (필요시 자동 추가 가능)
            // jumpController = gameObject.AddComponent<CharacterJumpController>();
        }
    }

    private void Update()
    {
        // 히어로는 pathWaypoints 이동 안 함
        if (isHero)
        {
            return;
        }

        // 점프 후 상대 지역 캐릭터 추적 로직
        if (hasCrossedRegion)
        {
            // 점프한 캐릭터는 상대 지역의 캐릭터들을 추적
            if (currentCharTarget == null || !currentCharTarget.gameObject.activeInHierarchy)
            {
                // 타겟이 없거나 비활성화됐으면 새 타겟 탐색
                FindNewOpponentAfterCrossing();
            }
            
            if (currentCharTarget != null)
            {
                // 타겟이 있으면 해당 타겟을 향해 이동
                MoveTowardsTarget(currentCharTarget.transform.position);
            }
            else
            {
                // 캐릭터 타겟이 없으면 성을 향해 이동
                MoveTowardsCastle();
            }

            // [추가] 캐릭터 스프라이트 Up/Down 갱신
            UpdateCharacterDirectionSprite(); // [수정추가]
            return;
        }

        // 일반 이동 로직 (기존 코드)
        if (currentWaypointIndex == -1)
        {
            // [추가] 캐릭터 스프라이트 Up/Down 갱신
            UpdateCharacterDirectionSprite(); // [수정추가]
            return;
        }

        if (pathWaypoints != null && pathWaypoints.Length > 0 && currentWaypointIndex >= 0)
        {
            MoveAlongWaypoints();
        }

        // [추가] 캐릭터 스프라이트 Up/Down 갱신
        UpdateCharacterDirectionSprite(); // [수정추가]
    }

    private void MoveAlongWaypoints()
    {
        if (pathWaypoints == null || pathWaypoints.Length == 0)
        {
            return;
        }

        if (currentWaypointIndex > maxWaypointIndex)
        {
            OnArriveCastle();
            return;
        }

        if (currentWaypointIndex < 0 || currentWaypointIndex >= pathWaypoints.Length)
        {
            Debug.LogWarning($"[Character] 잘못된 waypoint 인덱스: {currentWaypointIndex}, 배열 크기: {pathWaypoints.Length}");
            currentWaypointIndex = -1;
            return;
        }

        Transform target = pathWaypoints[currentWaypointIndex];
        if (target == null) return;

        Vector2 currentPos = transform.position;
        Vector2 targetPos = target.position;
        Vector2 dir = (targetPos - currentPos).normalized;

        // 지역에 상관없이 동일한 이동 속도 적용
        // 모든 지역의 캐릭터는 동일한 이동 속도를 가짐
        float standardMoveSpeed = 1f; // 표준화된 이동 속도
        float distThisFrame = standardMoveSpeed * Time.deltaTime;
        
        transform.position += (Vector3)(dir * distThisFrame);

        float dist = Vector2.Distance(currentPos, targetPos);
        if (dist < distThisFrame * 1.5f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex > maxWaypointIndex)
            {
                OnArriveCastle();
            }
            else if (currentWaypointIndex >= pathWaypoints.Length)
            {
                OnArriveCastle();
            }
        }
    }

    private void OnArriveCastle()
    {
        // 히어로는 파괴/소멸 로직 없음
        if (isHero)
        {
            return;
        }

        // ===============================
        // [수정] '끝'에 도달하면 "상대 지역으로 점프"
        //        (단, 이미 점프중이면 중복 방지)
        // ===============================
        if (isJumpingAcross) return;

        if (areaIndex == 1)
        {
            // 지역1 -> 지역2
            StartCoroutine(CrossToOtherRegion(2));
        }
        else
        {
            // 지역2 -> 지역1
            StartCoroutine(CrossToOtherRegion(1));
        }
    }

    private IEnumerator CrossToOtherRegion(int nextAreaIndex)
    {
        isJumpingAcross = true;

        Vector3 startPos;
        Vector3 endPos;

        // 시작 위치 설정
        if (startJumpPosition != null)
        {
            // UI 요소인 경우
            RectTransform rt = startJumpPosition as RectTransform;
            if (rt != null && rt.parent != null)
            {
                Vector3[] corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                startPos = corners[0]; 
                startPos.z = 0f;
            }
            else
            {
                startPos = startJumpPosition.position;
            }
        }
        else
        {
            startPos = transform.position;
        }

        // [수정] 캔버스의 자식 오브젝트 위치 사용
        if (targetJumpPosition != null)
        {
            RectTransform rt = targetJumpPosition as RectTransform;
            if (rt != null && rt.parent != null)
            {
                Vector3[] corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                endPos = corners[0]; 
                endPos.z = 0f;
            }
            else
            {
                endPos = targetJumpPosition.position;
            }
        }
        else
        {
            if (pathWaypoints != null && pathWaypoints.Length > 0 
                && currentWaypointIndex >= 0 && currentWaypointIndex < pathWaypoints.Length)
            {
                Transform waypointTr = pathWaypoints[currentWaypointIndex];
                RectTransform rt = waypointTr as RectTransform;
                if (rt != null && rt.parent != null)
                {
                    Vector2 localPos = rt.parent.InverseTransformPoint(rt.position);
                    endPos = rt.parent.TransformPoint(localPos);
                }
                else
                {
                    endPos = waypointTr.position;
                }
            }
            else
            {
                endPos = transform.position;
                Debug.LogWarning("현재 웨이포인트 인덱스가 유효하지 않아 기본 위치로...");
            }
        }

        if (endTileRect != null)
        {
            endPos = endTileRect.position;
        }

        // 실제 점프(포물선) - 일반 점프 설정으로 복원
        if (jumpController != null)
        {
            // 기본 점프 높이와 속도 사용
            jumpController.jumpArcHeight = 80f;   // 원래 점프 높이
            jumpController.jumpDuration = 1.0f;   // 원래 점프 속도
            jumpController.JumpToPosition(startPos, endPos, jumpController.jumpArcHeight, jumpController.jumpDuration);

            while (jumpController.isJumping)
            {
                yield return null;
            }
        }
        else
        {
            // 점프 컨트롤러가 없으면 즉시 이동
            transform.position = endPos;
        }

        // === 점프 후 처리 ===
        // 지역 인덱스는 *변경하지 않는다* (요청사항)
        hasCrossedRegion = true;
        pathWaypoints = null;
        currentWaypointIndex = -1;
        maxWaypointIndex = -1;

        // region1->region2 => chaseTag="Opponent Player"
        // region2->region1 => chaseTag="Player"
        if (nextAreaIndex == 2)
        {
            chaseTag = opponentPlayerTag;
        }
        else
        {
            chaseTag = playerTag;
        }

        // 넘어간 캐릭터는 'Character' 태그로 변경
        gameObject.tag = characterTag;

        Debug.Log($"[Character] {characterName}(areaIndex={areaIndex})가 점프 후 태그 변경: {gameObject.tag}, 타겟 태그: {chaseTag}");

        Vector3 fixPos = transform.position;
        fixPos.z = 0f;
        transform.position = fixPos;

        isJumpingAcross = false;
        yield break;
    }

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            // 모든 타겟에 대해 동일한 공격 쿨다운 사용
            // 공격 속도는 일반 캐릭터와 동일하게 유지
            yield return new WaitForSeconds(attackCooldown);

            // 현재 타겟 초기화
            currentCharTarget = null;
            currentTarget = null;
            IDamageable targetToDamage = null;

            // 히어로 캐릭터는 몬스터만 공격
            if (isHero)
            {
                currentTarget = FindHeroTargetInRange();
                if (currentTarget != null)
                {
                    targetToDamage = currentTarget;
                    Debug.Log($"[Character] 히어로 {characterName}이(가) 몬스터 {currentTarget.name}을(를) 타겟으로 설정!");
                }
            }
            else if (hasCrossedRegion)
            {
                // 점프한 캐릭터는 상대 지역 캐릭터 또는 성을 공격
                // 먼저 범위 내 캐릭터를 찾음
                currentCharTarget = FindOpponentCharacterInRange();
                if (currentCharTarget != null)
                {
                    targetToDamage = currentCharTarget;
                    Debug.Log($"[Character] 점프한 {characterName}(areaIndex={areaIndex})가 " +
                              $"상대 지역 캐릭터 {currentCharTarget.characterName}(areaIndex={currentCharTarget.areaIndex})를 공격!");
                }
                else
                {
                    // 캐릭터가 없으면 성을 공격
                    if (CanAttackCastle())
                    {
                        // 성 공격 처리
                        AttackCastle();
                        // AttackRoutine은 계속 진행되므로 여기서는 targetToDamage를 null로 유지
                        targetToDamage = null;
                    }
                }
            }
            else
            {
                // 수정: 일반 캐릭터는 우선 점프해온 상대 캐릭터를 찾고, 없으면 몬스터 공격
                currentCharTarget = FindJumpedOpponentCharacterInRange();
                
                if (currentCharTarget != null)
                {
                    targetToDamage = currentCharTarget;
                    Debug.Log($"[Character] {characterName}이(가) 점프해온 상대 캐릭터 {currentCharTarget.characterName}을(를) 타겟으로 설정!");
                }
                else
                {
                    currentTarget = FindMonsterTargetInRange();
                    if (currentTarget != null)
                    {
                        targetToDamage = currentTarget;
                        Debug.Log($"[Character] {characterName}이(가) 몬스터 {currentTarget.name}을(를) 타겟으로 설정!");
                    }
                }
            }

            // 타겟이 있으면 공격 실행
            if (targetToDamage != null)
            {
                // 히어로는 추가 데미지
                if (isHero)
                {
                    float originalDamage = attackPower;
                    attackPower *= 1.5f;
                    
                    AttackTarget(targetToDamage);
                    
                    attackPower = originalDamage;
                }
                else
                {
                    AttackTarget(targetToDamage);
                }
            }
        }
    }

    private Monster FindMonsterTargetInRange()
    {
        // areaIndex=1 → "Monster" 태그
        // areaIndex=2 → "EnemyMonster" 태그
        string targetTag = (areaIndex == 1) ? "Monster" : "EnemyMonster";
        GameObject[] foundObjs = GameObject.FindGameObjectsWithTag(targetTag);

        Monster nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (GameObject mo in foundObjs)
        {
            if (mo == null) continue;
            Monster m = mo.GetComponent<Monster>();
            if (m == null) continue;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(m.transform.position);
            if (screenPos.x < 0f || screenPos.x > 1080f || screenPos.y < 0f || screenPos.y > 1920f)
            {
                continue;
            }

            float dist = Vector2.Distance(transform.position, m.transform.position);
            if (dist < attackRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = m;
            }
        }
        return nearest;
    }

    private Character FindOpponentCharacterInRange()
    {
        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        Character nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (Character c in allChars)
        {
            if (c == this) continue;
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            
            if (c.isHero) continue;

            if (c.areaIndex != this.areaIndex)
            {
                float dist = Vector2.Distance(transform.position, c.transform.position);
                
                float effectiveRange = attackRange;
                
                if (dist < effectiveRange && dist < nearestDist)
                {
                    if (this.hasCrossedRegion && tag == characterTag)
                    {
                        nearestDist = dist;
                        nearest = c;
                    }
                    else if (c.hasCrossedRegion && c.tag == characterTag)
                    {
                        nearestDist = dist;
                        nearest = c;
                    }
                }
            }
        }
        
        if (nearest != null && !nearest.Equals(null))
        {
            Debug.Log($"[Character] {characterName}(areaIndex={areaIndex}, tag={tag})가 " +
                     $"{nearest.characterName}(areaIndex={nearest.areaIndex}, tag={nearest.tag})를 발견!");
        }
        
        return nearest;
    }

    // 히어로 캐릭터를 위한 몬스터 탐색 메소드 - 더 넓은 범위 적용
    private Monster FindHeroTargetInRange()
    {
        float heroAttackRange = attackRange * 1.5f;
        
        string targetTag = (areaIndex == 1) ? "Monster" : "EnemyMonster";
        GameObject[] foundObjs = GameObject.FindGameObjectsWithTag(targetTag);

        Monster nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (GameObject mo in foundObjs)
        {
            if (mo == null) continue;
            Monster m = mo.GetComponent<Monster>();
            if (m == null) continue;

            float dist = Vector2.Distance(transform.position, m.transform.position);
            if (dist < heroAttackRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = m;
            }
        }
        
        if (nearest != null)
        {
            Debug.Log($"[Character] 히어로 {characterName}이(가) 몬스터 {nearest.name}을(를) 탐지! (거리: {nearestDist}, 히어로 공격범위: {heroAttackRange})");
        }
        
        return nearest;
    }

    private void FindNewOpponentAfterCrossing()
    {
        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        Character newTarget = null;
        float closestDist = 100f;

        foreach (Character c in allChars)
        {
            if (c == this || c == null || !c.gameObject.activeInHierarchy) continue;
            if (c.isHero) continue;
            if (c.areaIndex != this.areaIndex)
            {
                float dist = Vector2.Distance(transform.position, c.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    newTarget = c;
                }
            }
        }

        if (newTarget != null)
        {
            currentCharTarget = newTarget;
            Debug.Log($"[Character] 점프한 {characterName}(areaIndex={areaIndex})가 " +
                     $"새 타겟 {newTarget.characterName}(areaIndex={newTarget.areaIndex})을 탐색함!");
        }
    }

    private void MoveTowardsTarget(Vector3 targetPosition)
    {
        float distToTarget = Vector2.Distance(transform.position, targetPosition);
        float stopDistance = attackRange * 0.7f;
        
        if (distToTarget <= stopDistance)
        {
            return;
        }
        
        float superFastSpeed = 15.0f;
        
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * superFastSpeed * Time.deltaTime;
    }

    /// <summary>
    /// 점프한 캐릭터가 성으로 이동하는 메서드
    /// </summary>
    private void MoveTowardsCastle()
    {
        Vector3 castlePosition;
        
        // 지역2에서 지역1로 점프한 경우 (지역1의 성으로 이동)
        if (areaIndex == 2)
        {
            // 지역1 성은 보통 화면 하단 중앙
            castlePosition = new Vector3(0f, -8f, 0f);
        }
        // 지역1에서 지역2로 점프한 경우 (지역2의 성으로 이동)
        else
        {
            // 지역2 성은 보통 화면 상단 중앙
            castlePosition = new Vector3(0f, 8f, 0f);
        }
        
        float distToCastle = Vector2.Distance(transform.position, castlePosition);
        float stopDistance = attackRange * 0.9f; // 성에 더 가까이 접근
        
        if (distToCastle <= stopDistance)
        {
            // 성 공격 범위에 도달
            return;
        }
        
        // 성을 향해 이동
        float moveSpeed = 10.0f;
        Vector3 direction = (castlePosition - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    // 통합된 공격 메소드
    private void AttackTarget(IDamageable target)
    {
        if (target == null) return;

        // 타겟의 areaIndex와 위치 정보 가져오기
        int targetAreaIndex = 0;
        Vector3 targetPosition = Vector3.zero;
        string targetName = "Unknown";
        GameObject targetGameObject = null;
        bool isCharacterTarget = false;
        bool isJumpedCharacterTarget = false;
        
        if (target is Monster monster)
        {
            targetAreaIndex = monster.areaIndex;
            targetPosition = monster.transform.position;
            targetName = monster.name;
            targetGameObject = monster.gameObject;
        }
        else if (target is Character character)
        {
            targetAreaIndex = character.areaIndex;
            targetPosition = character.transform.position;
            targetName = character.characterName;
            targetGameObject = character.gameObject;
            isCharacterTarget = true;
            isJumpedCharacterTarget = character.hasCrossedRegion;
        }
        else
        {
            Debug.LogWarning($"[Character] 알 수 없는 타겟 타입입니다: {target.GetType()}");
            return;
        }

        GameObject bulletPrefabToUse = GetBulletPrefab();
        if (bulletPrefabToUse != null)
        {
            try 
            {
                GameObject bulletObj = Instantiate(bulletPrefabToUse);

                RectTransform parentPanel = SelectBulletPanel(targetAreaIndex);

                if (parentPanel != null && parentPanel.gameObject.scene.IsValid())
                {
                    bulletObj.transform.SetParent(parentPanel, false);
                    Debug.Log($"[Character] 총알을 {parentPanel.name}에 생성함");
                }
                else
                {
                    Debug.LogWarning($"[Character] 총알을 생성할 패널이 없음, 대체 로직 사용");
                    bulletObj.transform.position = transform.position;
                    bulletObj.transform.localRotation = Quaternion.identity;
                    
                    target.TakeDamage(attackPower);
                    Destroy(bulletObj);
                    return;
                }

                RectTransform bulletRect = bulletObj.GetComponent<RectTransform>();
                if (bulletRect != null && parentPanel != null)
                {
                    Vector2 localPos = parentPanel.InverseTransformPoint(transform.position);
                    bulletRect.anchoredPosition = localPos;
                    bulletRect.localRotation = Quaternion.identity;
                }
                else
                {
                    bulletObj.transform.position = transform.position;
                    bulletObj.transform.localRotation = Quaternion.identity;
                }

                Bullet bulletComp = bulletObj.GetComponent<Bullet>();
                if (bulletComp != null)
                {
                    float bulletSpeed = 10.0f;
                    
                    if (isCharacterTarget && ((Character)target).hasCrossedRegion)
                    {
                        bulletSpeed = 80.0f;
                        Debug.Log($"[Character] {characterName}이(가) 점프해온 캐릭터를 향해 초고속 총알 발사! (속도: {bulletSpeed})");
                    }
                    else if (isCharacterTarget)
                    {
                        bulletSpeed = 15.0f;
                        Debug.Log($"[Character] {characterName}이(가) 상대 캐릭터를 향해 빠른 총알 발사! (속도: {bulletSpeed})");
                    }
                    
                    if (isHero)
                    {
                        bulletComp.isAreaAttack = true;
                        bulletComp.areaRadius = 2.0f;
                        
                        if (star == CharacterStar.OneStar)
                        {
                            bulletComp.bulletType = BulletType.Normal;
                        }
                        else if (star == CharacterStar.TwoStar)
                        {
                            bulletComp.bulletType = BulletType.Explosive;
                            bulletComp.speed = bulletSpeed * 1.2f;
                        }
                        else if (star == CharacterStar.ThreeStar)
                        {
                            bulletComp.bulletType = BulletType.Chain;
                            bulletComp.chainMaxBounces = 3;
                            bulletComp.chainBounceRange = 3.0f;
                            bulletComp.speed = bulletSpeed * 1.2f;
                        }
                        
                        Debug.Log($"[Character] 히어로 {characterName}이(가) 특수 총알을 발사! (타입: {bulletComp.bulletType})");
                    }
                    else
                    {
                        bulletComp.bulletType = BulletType.Normal;
                        bulletComp.isAreaAttack = isAreaAttack;
                        bulletComp.areaRadius = areaAttackRadius;
                    }
                    
                    bulletComp.speed = bulletSpeed;
                    
                    bool isTargetAbove = targetPosition.y > transform.position.y;
                    bulletComp.bulletUpDirectionSprite = bulletUpDirectionSprite;
                    bulletComp.bulletDownDirectionSprite = bulletDownDirectionSprite;
                    
                    bulletComp.target = targetGameObject;
                    
                    bulletComp.Init(target, attackPower, bulletSpeed, bulletComp.isAreaAttack, bulletComp.areaRadius, this.areaIndex);
                    Debug.Log($"[Character] {characterName}의 총알이 {targetName}을(를) 향해 발사됨! (속도: {bulletSpeed})");
                }
                else
                {
                    target.TakeDamage(attackPower);
                    Destroy(bulletObj);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Character] 총알 생성 중 오류 발생: {e.Message}");
                target.TakeDamage(attackPower);
            }
        }
        else
        {
            target.TakeDamage(attackPower);
            Debug.Log($"[Character] {characterName}이(가) {targetName}에게 직접 {attackPower} 데미지!");
        }
    }

    private RectTransform SelectBulletPanel(int targetAreaIndex)
    {
        RectTransform parentPanel = null;
        
        if (areaIndex == 2 || targetAreaIndex != this.areaIndex)
        {
            if (opponentBulletPanel != null)
            {
                parentPanel = opponentBulletPanel;
                Debug.Log($"[Character] {characterName}의 총알이 opponentBulletPanel에 생성됨");
            }
            else
            {
                Debug.LogWarning($"[Character] {characterName}에게 opponentBulletPanel이 설정되지 않음");
                parentPanel = bulletPanel;
                
                if (parentPanel == null)
                {
                    Canvas canvas = FindFirstObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        GameObject bulletPanelObj = new GameObject("EmergencyBulletPanel");
                        parentPanel = bulletPanelObj.AddComponent<RectTransform>();
                        bulletPanelObj.transform.SetParent(canvas.transform, false);
                        bulletPanel = parentPanel;
                        Debug.LogWarning($"[Character] 비상용 총알 패널을 생성했습니다.");
                    }
                }
            }
        }
        else
        {
            parentPanel = bulletPanel;
            
            if (parentPanel == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (Canvas canvas in canvases)
                {
                    if (canvas.isActiveAndEnabled)
                    {
                        GameObject bulletPanelObj = new GameObject("EmergencyBulletPanel");
                        parentPanel = bulletPanelObj.AddComponent<RectTransform>();
                        bulletPanelObj.transform.SetParent(canvas.transform, false);
                        bulletPanel = parentPanel;
                        Debug.LogWarning($"[Character] 비상용 총알 패널을 {canvas.name}에 생성했습니다.");
                        break;
                    }
                }
            }
        }
        
        return parentPanel;
    }

    private GameObject GetBulletPrefab()
    {
        if (bulletPrefab != null)
        {
            return bulletPrefab;
        }
        else
        {
            Debug.LogError($"[Character] {characterName}에 bulletPrefab이 설정되지 않음!");
            return null;
        }
    }

    private void DoAreaDamage(Vector3 centerPos)
    {
        float damageRadius = areaAttackRadius;

        GameObject[] monsterObjs = GameObject.FindGameObjectsWithTag("Monster");
        foreach (GameObject mo in monsterObjs)
        {
            Monster m = mo.GetComponent<Monster>();
            if (m == null) continue;

            if (m.areaIndex == this.areaIndex)
            {
                float dist = Vector2.Distance(centerPos, m.transform.position);
                if (dist <= damageRadius)
                {
                    m.TakeDamage(attackPower);
                }
            }
        }

        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (Character c in allChars)
        {
            if (c == this || c == null) continue;
            if (c.isHero) continue;
            
            if (c.areaIndex != this.areaIndex && c.hasCrossedRegion)
            {
                float dist = Vector2.Distance(centerPos, c.transform.position);
                if (dist <= damageRadius)
                {
                    c.TakeDamage(attackPower);
                }
            }
        }

        Debug.Log($"[Character] 광역 공격! 범위={damageRadius}, Damage={attackPower}");
    }

    private void CreateRangeIndicator()
    {
        if (!showRangeIndicator) return;
        if (rangeIndicatorPrefab == null) return;

        if (rangeIndicatorInstance != null)
        {
            Destroy(rangeIndicatorInstance);
        }

        rangeIndicatorInstance = Instantiate(rangeIndicatorPrefab, transform);
        rangeIndicatorInstance.name = "RangeIndicator";
        rangeIndicatorInstance.transform.localPosition = Vector3.zero;

        float diameter = attackRange * 2f;
        rangeIndicatorInstance.transform.localScale = new Vector3(diameter, diameter, 1f);
    }

    private void OnValidate()
    {
        if (rangeIndicatorInstance != null)
        {
            float diameter = attackRange * 2f;
            rangeIndicatorInstance.transform.localScale = new Vector3(diameter, diameter, 1f);
        }
    }

    public void ApplyStarVisual()
    {
        if (starVisualInstance != null)
        {
            Destroy(starVisualInstance);
            starVisualInstance = null;
        }

        if (star == CharacterStar.OneStar)
        {
            return;
        }
        else if (star == CharacterStar.TwoStar)
        {
            if (star2Prefab != null)
            {
                starVisualInstance = Instantiate(star2Prefab, transform);
                starVisualInstance.transform.localPosition = Vector3.zero;
            }
        }
        else if (star == CharacterStar.ThreeStar)
        {
            if (star3Prefab != null)
            {
                starVisualInstance = Instantiate(star3Prefab, transform);
                starVisualInstance.transform.localPosition = Vector3.zero;
            }
        }
    }

    public void SetBulletPanel(RectTransform panel)
    {
        bulletPanel = panel;
    }

    private void OnMouseDown()
    {
        attackRange = newAttackRangeOnClick;
        Debug.Log($"[Character] '{characterName}' 클릭 -> 사거리 {attackRange}로 변경");
        CreateRangeIndicator();

        if (attackInfoText == null) return;

        attackInfoText.gameObject.SetActive(true);
        attackInfoText.text = $"공격 범위: {attackRange}\n공격 형태: {rangeType}";

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + attackInfoOffset);
        attackInfoText.transform.position = screenPos;
    }

    public void TakeDamage(float damage)
    {
        if (isHero)
        {
            Debug.Log($"[Character] Hero {characterName} 무적 처리, 데미지 무시!");
            return;
        }

        currentHP -= damage;
        if (currentHP < 0f) currentHP = 0f;
        UpdateHpBar();

        if (currentHP <= 0f)
        {
            Debug.Log($"[Character] {characterName} 사망 (HP=0)!");
            
            // ▼▼ [수정] 안전한 타일 참조 정리 ▼▼
            if (currentTile != null)
            {
                // 타일을 미리 저장 (ClearCharacterTileReference에서 null로 설정되기 전에)
                Tile dyingTile = currentTile;
                
                // PlacementManager가 유효한지 확인
                if (PlacementManager.Instance != null && PlacementManager.Instance.gameObject.activeInHierarchy)
                {
                    Debug.Log($"[Character] {characterName} 사망 - {dyingTile.name} 타일 참조 정리");
                    
                    // placed tile인 경우와 placable tile인 경우를 구분
                    if (dyingTile.IsPlaceTile() || dyingTile.IsPlaced2())
                    {
                        // placed tile은 자식 제거하지 않고 비주얼만 업데이트
                        dyingTile.RefreshTileVisual();
                        Debug.Log($"[Character] placed tile {dyingTile.name} 비주얼 업데이트");
                    }
                    else
                    {
                        // placable tile은 PlaceTile 자식 제거
                        PlacementManager.Instance.RemovePlaceTileChild(dyingTile);
                    }
                    
                    // 타일 참조 정리
                    PlacementManager.Instance.ClearCharacterTileReference(this);
                    
                    // 타일 상태 즉시 업데이트
                    PlacementManager.Instance.OnCharacterRemovedFromTile(dyingTile);
                }
                else
                {
                    // PlacementManager가 없으면 수동으로 참조 정리
                    currentTile = null;
                }
            }
            
            // ▼▼ [수정] 안전한 오브젝트 파괴를 위해 즉시 파괴 대신 다음 프레임에 파괴 ▼▼
            // 모든 참조를 먼저 해제
            currentTarget = null;
            currentCharTarget = null;
            pathWaypoints = null;
            
            // 코루틴 정지
            StopAllCoroutines();
            
            // 다음 프레임에 파괴
            Destroy(gameObject, 0.01f);
        }
    }

    private void UpdateHpBar()
    {
        if (hpFillImage == null) return;
        float ratio = (maxHP <= 0f) ? 0f : (currentHP / maxHP);
        if (ratio < 0f) ratio = 0f;
        hpFillImage.fillAmount = ratio;
    }

    /// <summary>
    /// HP 바를 머리 위에 표시하기 위한 LateUpdate 메서드
    /// </summary>
    private void LateUpdate()
    {
        // ▼▼ [수정] 오브젝트가 파괴 중이면 실행하지 않음 ▼▼
        if (this == null || gameObject == null) return;
        
        // HP 바 위치 보정(머리 위에 표시)
        if (hpBarCanvas != null)
        {
            if (!isHero)
            {
                hpBarCanvas.gameObject.SetActive(true);
                
                if (hpBarCanvas.transform.parent == null)
                {
                    Vector3 offset = new Vector3(0f, 1.2f, 0f);
                    hpBarCanvas.transform.position = transform.position + offset;
                }
            }
        }
    }
    
    /// <summary>
    /// 점프해온 상대 지역 캐릭터를 찾는 메서드
    /// 일반 배치된 캐릭터가 점프해온 적 캐릭터를 공격할 때 사용합니다.
    /// </summary>
    private Character FindJumpedOpponentCharacterInRange()
    {
        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        Character nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (Character c in allChars)
        {
            if (c == this) continue;
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            if (c.isHero) continue;
            if (c.areaIndex != this.areaIndex && c.hasCrossedRegion && c.tag == characterTag)
            {
                float dist = Vector2.Distance(transform.position, c.transform.position);
                if (dist < attackRange && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = c;
                }
            }
        }
        
        if (nearest != null && !nearest.Equals(null))
        {
            Debug.Log($"[Character] {characterName}(areaIndex={areaIndex})가 " +
                    $"점프해온 상대 캐릭터 {nearest.characterName}(areaIndex={nearest.areaIndex}, tag={nearest.tag})를 발견!");
        }
        
        return nearest;
    }

    // =========================
    // [추가] 캐릭터가 위/아래 방향으로 이동(또는 타겟 추적)할 때
    //       스프라이트를 바꾸기 위한 메서드
    // =========================
    private void UpdateCharacterDirectionSprite() // [추가]
    {
        // 현재 이동(또는 타겟) 방향 계산
        Vector2 velocity = Vector2.zero;

        // 웨이포인트 기반 이동 중이면
        if (pathWaypoints != null && currentWaypointIndex >= 0 && currentWaypointIndex < pathWaypoints.Length)
        {
            Transform target = pathWaypoints[currentWaypointIndex];
            if (target != null)
            {
                velocity = (target.position - transform.position);
            }
        }
        // 아니면 근접 캐릭터 타겟(점프한 상대 등)
        else if (currentCharTarget != null)
        {
            velocity = (currentCharTarget.transform.position - transform.position);
        }
        else if (currentTarget != null)
        {
            velocity = (currentTarget.transform.position - transform.position);
        }

        // 위쪽으로 이동하는지 확인 (속도가 0일 때는 기본적으로 아래쪽)
        bool isMovingUp = (velocity.y > 0f);
        
        // ▼▼ [수정] 2~3성 캐릭터의 FrontImage/BackImage 처리 추가 ▼▼
        // 먼저 2~3성 캐릭터의 앞뒤 이미지 처리
        if (star == CharacterStar.TwoStar || star == CharacterStar.ThreeStar)
        {
            Transform frontImageObj = transform.Find("FrontImage");
            Transform backImageObj = transform.Find("BackImage");
            
            if (frontImageObj != null || backImageObj != null)
            {
                // FrontImage는 위쪽을 볼 때, BackImage는 아래쪽을 볼 때 표시
                if (frontImageObj != null)
                {
                    frontImageObj.gameObject.SetActive(isMovingUp);
                }
                if (backImageObj != null)
                {
                    backImageObj.gameObject.SetActive(!isMovingUp);
                }
                
                // 2~3성 캐릭터는 FrontImage/BackImage를 사용하므로 여기서 리턴
                return;
            }
        }
        // ▲▲ [수정 끝] ▲▲
        
        // 기존 로직: 1성 캐릭터의 characterUpDirectionSprite/characterDownDirectionSprite 처리
        if (characterUpDirectionSprite == null || characterDownDirectionSprite == null)
        {
            // 만약 위/아래 스프라이트가 설정 안 됐다면 수행 X
            return;
        }
        if (spriteRenderer == null && uiImage == null)
        {
            // 스프라이트 렌더러 / UI 이미지 둘 다 없으면 수행 X
            return;
        }

        Sprite spriteToUse = isMovingUp ? characterUpDirectionSprite : characterDownDirectionSprite;

        // 스프라이트 적용
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = spriteToUse;
        }
        else if (uiImage != null)
        {
            uiImage.sprite = spriteToUse;
        }
    }

    private void OnDestroy()
    {
        // ▼▼ [수정] 안전한 파괴 처리 ▼▼
        // Unity가 이미 파괴 중인 경우 추가 작업을 하지 않음
        if (this == null) return;
        
        // 캐릭터가 파괴될 때 타일 참조 정리
        if (currentTile != null)
        {
            // 타일이 유효한지 확인
            if (currentTile.gameObject != null && currentTile.gameObject.activeInHierarchy)
            {
                Debug.Log($"[Character] {characterName} 파괴됨 - {currentTile.name} 타일 참조 정리");
                
                // 타일을 미리 저장
                Tile destroyedTile = currentTile;
                
                // PlacementManager가 유효한지 확인
                if (PlacementManager.Instance != null && PlacementManager.Instance.gameObject != null && PlacementManager.Instance.gameObject.activeInHierarchy)
                {
                    // placed tile인 경우와 placable tile인 경우를 구분
                    if (destroyedTile.IsPlaceTile() || destroyedTile.IsPlaced2())
                    {
                        // placed tile은 자식 제거하지 않고 비주얼만 업데이트
                        destroyedTile.RefreshTileVisual();
                        Debug.Log($"[Character] placed tile {destroyedTile.name} 비주얼 업데이트");
                    }
                    else
                    {
                        // placable tile은 PlaceTile 자식 제거
                        PlacementManager.Instance.RemovePlaceTileChild(destroyedTile);
                    }
                    
                    // 타일 상태 즉시 업데이트
                    PlacementManager.Instance.OnCharacterRemovedFromTile(destroyedTile);
                }
            }
            
            // 타일 참조를 null로 설정
            currentTile = null;
        }
        
        // 모든 참조 해제
        currentTarget = null;
        currentCharTarget = null;
        pathWaypoints = null;
        rangeIndicatorInstance = null;
        starVisualInstance = null;
        hpBarCanvas = null;
        hpFillImage = null;
        bulletPanel = null;
        opponentBulletPanel = null;
    }

    /// <summary>
    /// 점프한 캐릭터가 성을 공격할 수 있는지 확인
    /// </summary>
    private bool CanAttackCastle()
    {
        // 점프한 캐릭터만 성을 공격 가능
        if (!hasCrossedRegion) return false;
        
        // 성까지의 거리를 체크 (성은 보통 마지막 웨이포인트 근처에 있음)
        // 간단하게 항상 true를 반환하여 캐릭터가 없으면 성을 공격하도록 함
        return true;
    }
    
    /// <summary>
    /// 성을 공격하는 메서드
    /// </summary>
    private void AttackCastle()
    {
        // 점프해서 지역2에서 지역1로 온 경우 (areaIndex는 여전히 2)
        if (areaIndex == 2)
        {
            // 지역1의 성 공격
            if (CastleHealthManager.Instance != null)
            {
                int damage = Mathf.RoundToInt(attackPower);
                CastleHealthManager.Instance.TakeDamage(damage);
                Debug.Log($"[Character] 점프한 {characterName}이(가) 지역1 성을 공격! (데미지: {damage})");
                
                // 공격 이펙트나 애니메이션 재생 (선택사항)
                if (bulletPrefab != null)
                {
                    // 성을 향한 총알 발사 이펙트 (실제 데미지는 이미 적용됨)
                    // 총알은 시각적 효과만 제공
                    GameObject bulletObj = Instantiate(bulletPrefab);
                    if (bulletPanel != null)
                    {
                        bulletObj.transform.SetParent(bulletPanel, false);
                        RectTransform bulletRect = bulletObj.GetComponent<RectTransform>();
                        if (bulletRect != null)
                        {
                            Vector2 localPos = bulletPanel.InverseTransformPoint(transform.position);
                            bulletRect.anchoredPosition = localPos;
                        }
                    }
                    else
                    {
                        bulletObj.transform.position = transform.position;
                    }
                    
                    Bullet bulletComp = bulletObj.GetComponent<Bullet>();
                    if (bulletComp != null)
                    {
                        bulletComp.speed = 10.0f;
                        Destroy(bulletObj, 1.0f); // 1초 후 삭제
                    }
                    else
                    {
                        Destroy(bulletObj);
                    }
                }
            }
        }
        // 점프해서 지역1에서 지역2로 온 경우 (areaIndex는 여전히 1)
        else if (areaIndex == 1)
        {
            // 지역2의 체력 감소
            var wave2 = FindFirstObjectByType<WaveSpawnerRegion2>();
            if (wave2 != null)
            {
                int damage = Mathf.RoundToInt(attackPower);
                wave2.TakeDamageToRegion2(damage);
                Debug.Log($"[Character] 점프한 {characterName}이(가) 지역2를 공격! (데미지: {damage})");
                
                // 공격 이펙트나 애니메이션 재생 (선택사항)
                if (bulletPrefab != null && opponentBulletPanel != null)
                {
                    GameObject bulletObj = Instantiate(bulletPrefab);
                    bulletObj.transform.SetParent(opponentBulletPanel, false);
                    
                    RectTransform bulletRect = bulletObj.GetComponent<RectTransform>();
                    if (bulletRect != null)
                    {
                        Vector2 localPos = opponentBulletPanel.InverseTransformPoint(transform.position);
                        bulletRect.anchoredPosition = localPos;
                    }
                    
                    // 성의 위치는 보통 화면 상단
                    Vector3 castlePosition = new Vector3(transform.position.x, 10f, 0f);
                    
                    Bullet bulletComp = bulletObj.GetComponent<Bullet>();
                    if (bulletComp != null)
                    {
                        bulletComp.speed = 10.0f;
                        Destroy(bulletObj, 1.0f); // 1초 후 삭제
                    }
                    else
                    {
                        Destroy(bulletObj);
                    }
                }
            }
        }
    }
}
