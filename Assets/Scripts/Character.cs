using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

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

public class Character : NetworkBehaviour
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
    public bool isAreaAttack = false;
    public float areaAttackRadius = 1f;

    [Header("Hero Settings")]
    [Tooltip("주인공(히어로) 여부")]
    public bool isHero = false;

    // (추가) 길 따라 이동할 때 사용하는 웨이포인트
    [Header("Path Waypoints for Auto Movement")]
    public Transform[] pathWaypoints;

    /// <summary>
    /// 현재 웨이포인트 인덱스 (기본 -1이면 이동 안 함)
    /// </summary>
    public int currentWaypointIndex = -1;

    [Header("[추가] 최대 웨이포인트 인덱스 (기본=6)")]
    public int maxWaypointIndex = 6;

    // (추가) 적 캐릭터(몬스터)로 다시 나타날 네트워크 프리팹 - 현재 사용 안 함
    [Header("[Network] Opponent Side Waypoints (성 도달 시 스폰용)")]
    [SerializeField] private NetworkPrefabRef enemyMonsterPrefab;

    // ========= [추가] 텔레포트 관련 필드 =========
    [Header("Teleport Spawn Points")]
    [Tooltip("지역1 → 지역2로 갈 때 도착할 위치(캔버스의 Tile Panel 자식 Tile)")]
    public Transform region2TeleportSpawn;

    [Tooltip("지역2 → 지역1로 갈 때 도착할 위치(캔버스의 Tile Panel 자식 Tile)")]
    public Transform region1TeleportSpawn;
    // ===========================================

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
    // (★ 추가) 몬스터인지 여부 -> 수정!
    // 이름: isMonster → isCharAttack
    // 의미 반대:  true = '캐릭터'(같은 편/적 캐릭터) 공격,
    //            false = '몬스터(태그=Monster / EnemyMonster)' 공격
    // ===============================
    [Header("플레이어인가? (캐릭터 공격 / 몬스터 공격 여부)")]
    [Tooltip("true면 '캐릭터(=태그 Monster, EnemyMonster 등)'를 찾고 공격, false면 진짜 몬스터(태그)들을 공격")]
    public bool isCharAttack = false;

    // =======================================================================
    // ★ [추가 코드] 텔레포트 횟수를 기록하여, 조건(teleportCount > 0) 충족 시
    //   다른 areaIndex + isCharAttack == false인 캐릭터와도 서로 공격 가능하게 만듦
    // =======================================================================
    [Header("[추가] 텔레포트 횟수 기록")]
    public int teleportCount = 0; // 텔레포트 시 +1
    // =======================================================================

    private Character currentTargetChar; // ★ 추가: isCharAttack==false 캐릭터를 직접 공격 시

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

        // 공격 루틴
        StartCoroutine(AttackRoutine());

        // 2성/3성 별 표시
        ApplyStarVisual();
    }

    private void Update()
    {
        if (isHero)
        {
            // 히어로는 pathWaypoints 이동 안 함
            return;
        }

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

        // 웨이포인트 인덱스가 maxWaypointIndex 초과면 목적지 도착
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

        float distThisFrame = moveSpeed * Time.deltaTime;
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

    /// <summary>
    /// 웨이포인트 끝(성)에 도착했을 때
    /// </summary>
    private void OnArriveCastle()
    {
        // 히어로는 파괴/소멸 로직 없음
        if (isHero)
        {
            return;
        }

        // 몬스터(또는 isCharAttack=false)라면 소멸
        if (!isCharAttack)
        {
            if (Object != null && Object.IsValid)
            {
                Runner.Despawn(Object);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else
        {
            // =============================================================================
            // [수정] "목표한 타일 위치로 단순 위치 이동 + areaIndex 전환 + teleportCount 증가"만 수행
            // =============================================================================
            if (areaIndex == 1 && region2TeleportSpawn != null)
            {
                transform.position = region2TeleportSpawn.position;
                areaIndex = 2;
                teleportCount++;
            }
            else if (areaIndex == 2 && region1TeleportSpawn != null)
            {
                transform.position = region1TeleportSpawn.position;
                areaIndex = 1;
                teleportCount++;
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackCooldown);

            if (isCharAttack)
            {
                currentTarget = FindPlacableTargetInRange();

                // ★ 추가: 만약 찾지 못했고(==null), 내가 teleportCount>0 이라면
                //         areaIndex가 다른 & isCharAttack==false 인 캐릭터를 따로 검색
                if (currentTarget == null && teleportCount > 0)
                {
                    currentTargetChar = FindNoAttackCharInRange();
                    if (currentTargetChar != null)
                    {
                        AttackCharacter(currentTargetChar);
                        continue;
                    }
                }

                if (currentTarget != null)
                {
                    Attack(currentTarget);
                }
            }
            else
            {
                currentTarget = FindMonsterTargetInRange();

                // ★ 추가: 만약 찾지 못했고(==null), "teleportCount>0인 isCharAttack==true 캐릭터" 공격
                if (currentTarget == null)
                {
                    currentTargetChar = FindTeleportedCharInRange();
                    if (currentTargetChar != null)
                    {
                        AttackCharacter(currentTargetChar);
                        continue;
                    }
                }

                if (currentTarget != null)
                {
                    Attack(currentTarget);
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

            // 화면 밖이면 패스(옵션)
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

    private Monster FindPlacableTargetInRange()
    {
        Character[] allChars = GameObject.FindObjectsByType<Character>(FindObjectsSortMode.None);
        Character nearestC = null;
        float nearestDist = Mathf.Infinity;

        foreach (var c in allChars)
        {
            if (c == null) continue;
            if (c == this) continue;
            if (!c.isCharAttack) continue;

            // areaIndex가 달라야 공격
            if (c.areaIndex != this.areaIndex)
            {
                float dist = Vector2.Distance(transform.position, c.transform.position);
                if (dist < attackRange && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestC = c;
                }
            }
        }

        if (nearestC == null) return null;

        Monster possibleMon = nearestC.GetComponent<Monster>();
        if (possibleMon != null)
        {
            return possibleMon;
        }
        return null;
    }

    private Character FindTeleportedCharInRange()
    {
        Character[] all = GameObject.FindObjectsByType<Character>(FindObjectsSortMode.None);
        Character nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var c in all)
        {
            if (c == null) continue;
            if (c == this) continue;

            // 조건: c.isCharAttack==true && c.teleportCount>0
            if (!c.isCharAttack) continue;
            if (c.teleportCount <= 0) continue;
            if (c.areaIndex == this.areaIndex) continue;

            float dist = Vector2.Distance(transform.position, c.transform.position);
            if (dist < attackRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = c;
            }
        }

        return nearest;
    }

    private Character FindNoAttackCharInRange()
    {
        Character[] allChars = GameObject.FindObjectsByType<Character>(FindObjectsSortMode.None);
        Character nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (var c in allChars)
        {
            if (c == null) continue;
            if (c == this) continue;

            // 조건: c.isCharAttack==false, c.areaIndex != 내 areaIndex
            if (c.isCharAttack) continue;
            if (c.areaIndex == this.areaIndex) continue;

            float dist = Vector2.Distance(transform.position, c.transform.position);
            if (dist < attackRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = c;
            }
        }

        return nearest;
    }

    private void Attack(Monster target)
    {
        if (target == null) return;

        if (bulletPrefab != null)
        {
            GameObject bulletObj = Instantiate(bulletPrefab);

            RectTransform parentPanel = null;
            if (areaIndex == 2 && opponentBulletPanel != null)
            {
                parentPanel = opponentBulletPanel;
            }
            else
            {
                parentPanel = bulletPanel;
            }

            if (parentPanel != null && parentPanel.gameObject.scene.IsValid())
            {
                bulletObj.transform.SetParent(parentPanel, false);
            }

            RectTransform bulletRect = bulletObj.GetComponent<RectTransform>();
            if (bulletRect != null && parentPanel != null)
            {
                Vector2 localPos = parentPanel.InverseTransformPoint(transform.position);
                bulletRect.anchoredPosition = localPos;
                bulletRect.localRotation    = Quaternion.identity;
            }
            else
            {
                bulletObj.transform.position = transform.position;
                bulletObj.transform.localRotation = Quaternion.identity;
            }

            Bullet bulletComp = bulletObj.GetComponent<Bullet>();
            if (bulletComp != null)
            {
                bulletComp.Init(target, attackPower, bulletSpeed, isAreaAttack, areaAttackRadius, this.areaIndex);
            }
        }
        else
        {
            if (isAreaAttack)
            {
                DoAreaDamage(target.transform.position);
            }
            else
            {
                target.TakeDamage(attackPower);
            }
        }
    }

    private void AttackCharacter(Character c)
    {
        if (c == null) return;

        if (bulletPrefab != null)
        {
            GameObject bulletObj = Instantiate(bulletPrefab);

            RectTransform parentPanel = null;
            if (areaIndex == 2 && opponentBulletPanel != null)
            {
                parentPanel = opponentBulletPanel;
            }
            else
            {
                parentPanel = bulletPanel;
            }

            if (parentPanel != null && parentPanel.gameObject.scene.IsValid())
            {
                bulletObj.transform.SetParent(parentPanel, false);
            }

            RectTransform bulletRect = bulletObj.GetComponent<RectTransform>();
            if (bulletRect != null && parentPanel != null)
            {
                Vector2 localPos = parentPanel.InverseTransformPoint(transform.position);
                bulletRect.anchoredPosition = localPos;
                bulletRect.localRotation    = Quaternion.identity;
            }
            else
            {
                bulletObj.transform.position = transform.position;
                bulletObj.transform.localRotation = Quaternion.identity;
            }

            Bullet bulletComp = bulletObj.GetComponent<Bullet>();
            if (bulletComp != null)
            {
                bulletComp.Init(null, attackPower, bulletSpeed, isAreaAttack, areaAttackRadius, this.areaIndex);
            }
        }
        else
        {
            if (isAreaAttack)
            {
                DoAreaDamage(c.transform.position);
            }
            else
            {
                c.TakeDamage(attackPower);
            }
        }
    }

    private void DoAreaDamage(Vector3 centerPos)
    {
        GameObject[] monsterObjs = GameObject.FindGameObjectsWithTag("Monster");
        foreach (GameObject mo in monsterObjs)
        {
            Monster m = mo.GetComponent<Monster>();
            if (m == null) continue;

            if (m.areaIndex == this.areaIndex)
            {
                float dist = Vector2.Distance(centerPos, m.transform.position);
                if (dist <= areaAttackRadius)
                {
                    m.TakeDamage(attackPower);
                }
            }
        }
        Debug.Log($"[Character] 광역 공격! 범위={areaAttackRadius}, Damage={attackPower}");
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

    public void SetTeleportTargets(Transform targetTile, RectTransform targetPanel, bool isRegion1)
    {
        if (isRegion1)
        {
            region1TeleportSpawn = targetTile;
        }
        else
        {
            region2TeleportSpawn = targetTile;
        }
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
            // 필요 시 Destroy(gameObject) 등 (현재는 생략)
        }
    }

    private void UpdateHpBar()
    {
        if (hpFillImage == null) return;
        float ratio = (maxHP <= 0f) ? 0f : (currentHP / maxHP);
        if (ratio < 0f) ratio = 0f;
        hpFillImage.fillAmount = ratio;
    }

    private void OnMouseDown()
    {
        attackRange = newAttackRangeOnClick;
        Debug.Log($"[Character] '{characterName}' 클릭 -> 사거리 {attackRange}로 변경");
        CreateRangeIndicator();

        if (attackInfoText != null)
        {
            attackInfoText.gameObject.SetActive(true);
            attackInfoText.text = $"공격 범위: {attackRange}\n공격 형태: {rangeType}";

            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + attackInfoOffset);
            attackInfoText.transform.position = screenPos;
        }
    }
}
