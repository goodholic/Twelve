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

public class Character : NetworkBehaviour, IDamageable
{
    [Header("Character Star Info (별)")]
    public CharacterStar star = CharacterStar.OneStar;

    [Tooltip("캐릭터 이름")]
    public string characterName;

    // [추가] 종족 정보
    [Header("종족 정보 (Human / Orc / Elf)")]
    public CharacterRace race = CharacterRace.Human;

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
    private bool hasCrossedRegion = false;
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
            else
            {
                // 타겟이 있으면 해당 타겟을 향해 이동
                MoveTowardsTarget(currentCharTarget.transform.position);
            }
            return;
        }

        // 일반 이동 로직 (기존 코드)
        if (currentWaypointIndex == -1)
        {
            return;
        }

        if (pathWaypoints != null && pathWaypoints.Length > 0 && currentWaypointIndex >= 0)
        {
            MoveAlongWaypoints();
        }
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
                Debug.LogWarning("[Character] 유효하지 않은 웨이포인트 인덱스로 인해 현재 위치를 사용합니다.");
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
        // (상대편 캐릭터들이 공격할 수 있도록)
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
                // 히어로는 특별한 몬스터 탐색 메소드 사용
                currentTarget = FindHeroTargetInRange();
                if (currentTarget != null)
                {
                    targetToDamage = currentTarget;
                    Debug.Log($"[Character] 히어로 {characterName}이(가) 몬스터 {currentTarget.name}을(를) 타겟으로 설정!");
                }
            }
            else if (hasCrossedRegion)
            {
                // 점프한 캐릭터는 오직 상대 지역 캐릭터만 공격 (몬스터 무시)
                currentCharTarget = FindOpponentCharacterInRange();
                if (currentCharTarget != null)
                {
                    targetToDamage = currentCharTarget;
                    Debug.Log($"[Character] 점프한 {characterName}(areaIndex={areaIndex})가 " +
                              $"상대 지역 캐릭터 {currentCharTarget.characterName}(areaIndex={currentCharTarget.areaIndex})를 공격!");
                }
            }
            else
            {
                // 수정: 일반 캐릭터는 우선 점프해온 상대 캐릭터를 찾고, 없으면 몬스터 공격
                // 상대 지역에서 점프해온 캐릭터 찾기
                currentCharTarget = FindJumpedOpponentCharacterInRange();
                
                if (currentCharTarget != null)
                {
                    targetToDamage = currentCharTarget;
                    Debug.Log($"[Character] {characterName}이(가) 점프해온 상대 캐릭터 {currentCharTarget.characterName}을(를) 타겟으로 설정!");
                }
                else
                {
                    // 점프해온 상대 캐릭터가 없으면 몬스터 공격
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
                    // 히어로의 기본 공격력을 임시로 증가
                    float originalDamage = attackPower;
                    attackPower *= 1.5f;
                    
                    AttackTarget(targetToDamage);
                    
                    // 공격력 복원
                    attackPower = originalDamage;
                }
                else
                {
                    AttackTarget(targetToDamage);
                }
            }
        }
    }

    /// <summary>
    /// 점프 후 chaseTag로 적(몬스터)을 찾는다. 
    /// 예: chaseTag="Opponent Player" or "Player"
    /// </summary>
    private Monster FindCrossedTargetInRange(string tagToChase)
    {
        GameObject[] foundObjs = GameObject.FindGameObjectsWithTag(tagToChase);

        Monster nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (GameObject mo in foundObjs)
        {
            if (mo == null) continue;
            Monster m = mo.GetComponent<Monster>();
            if (m == null) continue;

            // 다른 지역이어야 함
            if (m.areaIndex != this.areaIndex)
            {
                float dist = Vector2.Distance(transform.position, m.transform.position);
                if (dist < attackRange && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = m;
                }
            }
        }
        return nearest;
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

    /// <summary>
    /// 상대 지역 "캐릭터"(=gameObject.tag로 구분) 찾는 로직
    /// 1. 점프한 캐릭터(tag="Character") → 상대 지역 캐릭터 공격
    /// 2. 상대 지역 캐릭터 → 점프한 캐릭터 공격
    /// 3. Player와 Opponent Player 태그 캐릭터끼리는 서로 싸우지 않음
    /// </summary>
    private Character FindOpponentCharacterInRange()
    {
        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        Character nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (Character c in allChars)
        {
            if (c == this) continue;
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            
            // 히어로는 타겟으로 잡지 않음
            if (c.isHero) continue;

            // 기본 조건: 다른 지역의 캐릭터만 타겟텍
            if (c.areaIndex != this.areaIndex)
            {
                float dist = Vector2.Distance(transform.position, c.transform.position);
                
                // 모든 캐릭터는 동일한 공격 범위 (점프해도 동일)
                float effectiveRange = attackRange;
                
                if (dist < effectiveRange && dist < nearestDist)
                {
                    // 케이스 1: 내가 점프한 캐릭터인 경우, 상대 지역의 모든 캐릭터를 공격
                    if (this.hasCrossedRegion && tag == characterTag)
                    {
                        nearestDist = dist;
                        nearest = c;
                        Debug.Log($"[Character] 점프한 캐릭터 {characterName}(areaIndex={areaIndex}, tag={tag})가 " +
                                $"상대 지역 캐릭터 {c.characterName}(areaIndex={c.areaIndex}, tag={c.tag})를 공격 대상으로 선택");
                    }
                    // 케이스 2: 상대 지역 캐릭터가 점프한 캐릭터를 공격
                    else if (c.hasCrossedRegion && c.tag == characterTag)
                    {
                        nearestDist = dist;
                        nearest = c;
                        Debug.Log($"[Character] 상대 지역 캐릭터 {characterName}(areaIndex={areaIndex}, tag={tag})가 " +
                                $"점프한 캐릭터 {c.characterName}(areaIndex={c.areaIndex}, tag={c.tag})를 공격 대상으로 선택");
                    }
                    // 케이스 3: Player와 Opponent Player 태그 캐릭터끼리는 서로 싸우지 않음
                    // (Player와 Opponent Player 태그 조건 제거)
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
        // 히어로는 일반 캐릭터보다 넓은 공격 범위를 가짐
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

            // 히어로는 화면 외부 몬스터도 공격 가능 (화면 제한 체크 없음)
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

    // 점프 후 새로운 상대 캐릭터 찾기
    private void FindNewOpponentAfterCrossing()
    {
        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        Character newTarget = null;
        float closestDist = 100f; // 최대 탐색 거리

        foreach (Character c in allChars)
        {
            if (c == this || c == null || !c.gameObject.activeInHierarchy) continue;
            
            // 히어로는 타겟으로 잡지 않음
            if (c.isHero) continue;
            
            // 지역 1에서 점프한 캐릭터는 지역 2의 캐릭터를 찾음
            // 지역 2에서 점프한 캐릭터는 지역 1의 캐릭터를 찾음
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

    // 지정된 위치로 이동 (히어로와 비슷한 이동 로직)
    private void MoveTowardsTarget(Vector3 targetPosition)
    {
        // 일정 거리 이내면 이동 멈춤 (공격 범위)
        float distToTarget = Vector2.Distance(transform.position, targetPosition);
        
        // 히어로무브 스크립트와 동일한 로직 적용
        float stopDistance = attackRange * 0.7f; // 공격 범위의 70%에서 정지
        
        if (distToTarget <= stopDistance)
        {
            // 이미 충분히 가까워서 이동할 필요 없음
            return;
        }
        
        // 점프해온 캐릭터의 이동 속도를 극도로 빠르게 설정
        float superFastSpeed = 15.0f; // 매우 빠른 이동 속도 (기존 10.0f에서 15.0f로 증가)
        
        // 타겟을 향해 매우 빠르게 이동
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * superFastSpeed * Time.deltaTime;
    }

    // 통합된 공격 패널 선택 로직을 포함하는 추가 메서드
    private RectTransform SelectBulletPanel(int targetAreaIndex)
    {
        RectTransform parentPanel = null;
        
        // 지역 2 캐릭터이거나 다른 지역 타겟 공격시 항상 opponentBulletPanel 사용
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
                
                // 대체 패널로 bulletPanel 시도
                parentPanel = bulletPanel;
                
                // 패널을 찾을 수 없는 경우 Canvas에서 찾기 시도
                if (parentPanel == null)
                {
                    Canvas canvas = FindFirstObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        GameObject bulletPanelObj = new GameObject("EmergencyBulletPanel");
                        parentPanel = bulletPanelObj.AddComponent<RectTransform>();
                        bulletPanelObj.transform.SetParent(canvas.transform, false);
                        bulletPanel = parentPanel; // 찾은 패널 저장
                        Debug.LogWarning($"[Character] 비상용 총알 패널을 생성했습니다.");
                    }
                }
            }
        }
        // 그 외에는 기본 패널 사용
        else
        {
            parentPanel = bulletPanel;
            
            // 기본 패널도 없는 경우 찾기 시도
            if (parentPanel == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    GameObject bulletPanelObj = new GameObject("EmergencyBulletPanel");
                    parentPanel = bulletPanelObj.AddComponent<RectTransform>();
                    bulletPanelObj.transform.SetParent(canvas.transform, false);
                    bulletPanel = parentPanel; // 찾은 패널 저장
                    Debug.LogWarning($"[Character] 비상용 총알 패널을 생성했습니다.");
                }
            }
        }
        
        // 유효성 검사
        if (parentPanel == null || !parentPanel.gameObject.scene.IsValid())
        {
            Debug.LogWarning($"[Character] 유효한 총알 패널이 없음, 대체 패널 생성 시도");
            
            // 마지막 시도: 활성화된 캔버스 찾기
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.isActiveAndEnabled)
                {
                    GameObject bulletPanelObj = new GameObject("EmergencyBulletPanel");
                    parentPanel = bulletPanelObj.AddComponent<RectTransform>();
                    bulletPanelObj.transform.SetParent(canvas.transform, false);
                    bulletPanel = parentPanel; // 찾은 패널 저장
                    Debug.LogWarning($"[Character] 비상용 총알 패널을 {canvas.name}에 생성했습니다.");
                    break;
                }
            }
        }
        
        return parentPanel;
    }

    // 통합된 총알 프리팹 선택 메서드
    private GameObject GetBulletPrefab()
    {
        // 캐릭터에서 사용할 표준 총알 프리팹을 여기서 명시적으로 지정합니다.
        // (모든 캐릭터가 인스펙터에서 다른 프리팹을 사용하더라도 이 코드에서 통일)
        
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

    // 통합된 공격 메소드 - IDamageable 인터페이스를 구현한 모든 대상을 공격할 수 있음
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

        // 총알 프리팹 가져오기
        GameObject bulletPrefabToUse = GetBulletPrefab();
        if (bulletPrefabToUse != null)
        {
            try 
            {
                GameObject bulletObj = Instantiate(bulletPrefabToUse);

                // 통합된 패널 선택 로직 사용
                RectTransform parentPanel = SelectBulletPanel(targetAreaIndex);

                if (parentPanel != null && parentPanel.gameObject.scene.IsValid())
                {
                    bulletObj.transform.SetParent(parentPanel, false);
                    Debug.Log($"[Character] 총알을 {parentPanel.name}에 생성함");
                }
                else
                {
                    Debug.LogWarning($"[Character] 총알을 생성할 패널이 없음, 대체 로직 사용");
                    // 패널이 없으면 월드에 생성
                    bulletObj.transform.position = transform.position;
                    bulletObj.transform.localRotation = Quaternion.identity;
                    
                    // 직접 데미지 적용 후 총알 제거
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
                    // 타겟 타입에 따라 총알 속도 조정
                    float bulletSpeed = 10.0f; // 기본 총알 속도
                    
                    // 모든 지역(지역1과 지역2 모두)의 배치된 캐릭터가 점프해온 캐릭터를 공격할 때 총알 속도 증가
                    if (isCharacterTarget && ((Character)target).hasCrossedRegion)
                    {
                        // 점프해온 캐릭터를 공격할 때 극도로 빠른 총알
                        bulletSpeed = 80.0f; // 매우 매우 빠른 총알 속도
                        Debug.Log($"[Character] {characterName}이(가) 점프해온 캐릭터를 향해 초고속 총알 발사! (속도: {bulletSpeed})");
                    }
                    else if (isCharacterTarget)
                    {
                        // 일반 캐릭터 공격용 빠른 총알
                        bulletSpeed = 15.0f;
                        Debug.Log($"[Character] {characterName}이(가) 상대 캐릭터를 향해 빠른 총알 발사! (속도: {bulletSpeed})");
                    }
                    
                    // 모든 타입의 타겟에 대해 동일한 총알 설정 사용
                    // 단, 히어로만 특별 총알 유지
                    if (isHero)
                    {
                        // 히어로는 기본적으로 광역 공격을 가짐
                        bulletComp.isAreaAttack = true;
                        bulletComp.areaRadius = 2.0f;
                        
                        // 히어로 레벨이나 특성에 따라 다른 총알 타입도 설정 가능
                        if (star == CharacterStar.OneStar)
                        {
                            bulletComp.bulletType = BulletType.Normal; // 기본 공격
                        }
                        else if (star == CharacterStar.TwoStar)
                        {
                            bulletComp.bulletType = BulletType.Explosive; // 폭발 공격
                            bulletComp.speed = bulletSpeed * 1.2f; // 히어로는 약간 더 빠름
                        }
                        else if (star == CharacterStar.ThreeStar)
                        {
                            bulletComp.bulletType = BulletType.Chain; // 체인 공격
                            bulletComp.chainMaxBounces = 3; // 3번 튕김
                            bulletComp.chainBounceRange = 3.0f; // 넓은 튕김 범위
                            bulletComp.speed = bulletSpeed * 1.2f; // 히어로는 약간 더 빠름
                        }
                        
                        Debug.Log($"[Character] 히어로 {characterName}이(가) 특수 총알을 발사! (타입: {bulletComp.bulletType})");
                    }
                    else
                    {
                        // 일반 캐릭터는 항상 기본 설정 사용 (몬스터/캐릭터 타겟 동일)
                        bulletComp.bulletType = BulletType.Normal;
                        bulletComp.isAreaAttack = isAreaAttack;
                        bulletComp.areaRadius = areaAttackRadius;
                    }
                    
                    // 모든 총알의 속도를 설정
                    bulletComp.speed = bulletSpeed;
                    
                    // 총알의 대상을 설정 - 한 번에 하나의 타겟만 공격
                    bulletComp.target = targetGameObject;
                    
                    // 초기화 시 속도 값으로 bulletSpeed 사용
                    bulletComp.Init(target, attackPower, bulletSpeed, bulletComp.isAreaAttack, bulletComp.areaRadius, this.areaIndex);
                    Debug.Log($"[Character] {characterName}의 총알이 {targetName}을(를) 향해 발사됨! (속도: {bulletSpeed})");
                }
                else
                {
                    // Bullet 컴포넌트가 없으면 직접 데미지 적용
                    target.TakeDamage(attackPower);
                    Destroy(bulletObj);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Character] 총알 생성 중 오류 발생: {e.Message}");
                // 오류 발생 시 직접 데미지 적용
                target.TakeDamage(attackPower);
            }
        }
        else
        {
            // 모든 타겟 타입에 대해 동일한 로직 적용
            target.TakeDamage(attackPower);
            Debug.Log($"[Character] {characterName}이(가) {targetName}에게 직접 {attackPower} 데미지!");
        }
    }

    private void DoAreaDamage(Vector3 centerPos)
    {
        // 몬스터와 캐릭터 모두에게 광역 데미지 적용
        float damageRadius = areaAttackRadius;

        // 몬스터에 대한 광역 데미지
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

        // 점프한 캐릭터에 대한 광역 데미지
        Character[] allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (Character c in allChars)
        {
            if (c == this || c == null) continue;
            
            // 히어로는 광역 데미지에서 제외
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
            Destroy(gameObject);
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
    /// 점프해온 상대 지역 캐릭터를 찾는 메서드
    /// 일반 배치된 캐릭터가 점프해온 적 캐릭터를 찾을 때 사용합니다.
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
            
            // 히어로는 타겟으로 잡지 않음
            if (c.isHero) continue;

            // 상대 지역에서 점프해온 캐릭터만 찾기
            if (c.areaIndex != this.areaIndex && c.hasCrossedRegion && c.tag == characterTag)
            {
                float dist = Vector2.Distance(transform.position, c.transform.position);
                
                // 공격 범위 내에 있는지 확인
                if (dist < attackRange && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = c;
                    Debug.Log($"[Character] {characterName}(areaIndex={areaIndex})가 " +
                            $"점프해온 상대 캐릭터 {c.characterName}(areaIndex={c.areaIndex})를 공격 대상으로 선택");
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
}
