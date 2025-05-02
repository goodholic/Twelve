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

public class Character : NetworkBehaviour
{
    [Header("Character Star Info (별)")]
    public CharacterStar star = CharacterStar.OneStar;

    [Tooltip("캐릭터 이름")]
    public string characterName;

    // ==================
    // 전투 스탯
    // ==================
    public float attackPower = 10f;
    public float attackSpeed = 1f;
    public float attackRange = 1.5f;
    public float currentHP = 100f;

    // ==================
    // 타일/몬스터 관련
    // ==================
    public Tile currentTile;
    public Monster currentTarget;
    public bool isAreaAttack = false;
    public float areaAttackRadius = 1f;

    [Header("Ally/Hero Settings")]
    public bool isAlly = false;
    public bool isHero = false;

    // (추가) 길 따라 이동할 때 사용하는 웨이포인트
    [Header("Path Waypoints for Auto Movement")]
    public Transform[] pathWaypoints;
    public int currentWaypointIndex = 0;

    // (추가) 상대 진영 웨이포인트
    [Header("[Network] Opponent Side Waypoints (성 도달 시 스폰용)")]
    public Transform[] opponentWaypoints;

    // (추가) 적 캐릭터(몬스터)로 다시 나타날 네트워크 프리팹
    [Header("[Network] 적 몬스터 프리팹(네트워크)")]
    [SerializeField] private NetworkPrefabRef enemyMonsterPrefab;

    // 사거리 표시용
    [Header("Range Indicator Settings")]
    public GameObject rangeIndicatorPrefab;
    public bool showRangeIndicator = true;
    private GameObject rangeIndicatorInstance;

    // 총알 발사
    [Header("Bullet Settings")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 5f;
    private RectTransform bulletPanel;

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

    // === 수정 부분 ===
    [Header("Area 구분 (1 or 2)")]
    [Tooltip("1번 공간인지, 2번 공간인지 구분하기 위한 인덱스")]
    public int areaIndex = 1;
    // === 수정 끝 ===

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            uiImage = GetComponentInChildren<Image>();
        }
    }

    private void Start()
    {
        // 별에 따른 기초 스탯 보정 (예시)
        switch (star)
        {
            case CharacterStar.OneStar: break;
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

        // 사거리 표시
        CreateRangeIndicator();

        // 공격 루틴
        StartCoroutine(AttackRoutine());
        // 2성/3성 시각적 표현
        ApplyStarVisual();
    }

    private void Update()
    {
        // ----------------------------
        // (추가) 아군이면서 waypoints가 있으면 이동
        // ----------------------------
        if (isAlly && pathWaypoints != null && pathWaypoints.Length > 0)
        {
            MoveAlongWaypoints();
        }
    }

    /// <summary>
    /// (추가) 아군 캐릭터가 pathWaypoints를 순서대로 따라 이동.
    /// 마지막 웨이포인트(성)에 도달하면 OnArriveCastle().
    /// </summary>
    private void MoveAlongWaypoints()
    {
        if (currentWaypointIndex >= pathWaypoints.Length) return;

        Transform target = pathWaypoints[currentWaypointIndex];
        if (target == null) return;

        Vector2 currentPos = transform.position;
        Vector2 targetPos = target.position;
        Vector2 dir = (targetPos - currentPos).normalized;

        float distThisFrame = 3f * Time.deltaTime; // 이동속도 예시
        transform.position += (Vector3)(dir * distThisFrame);

        float dist = Vector2.Distance(currentPos, targetPos);
        if (dist < distThisFrame * 1.5f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= pathWaypoints.Length)
            {
                // -------------------------------
                // 웨이포인트 끝 => 성 도달 처리
                // -------------------------------
                OnArriveCastle();
            }
        }
    }

    /// <summary>
    /// (추가) 성 도달 시 로직
    /// </summary>
    private void OnArriveCastle()
    {
        Debug.Log($"[Character] 아군 캐릭터가 성에 도착: {characterName}");

        // 아군 캐릭터라면 => 상대 진영에 적 몬스터로 스폰
        if (isAlly)
        {
            if (Object == null || !Object.IsValid)
            {
                Debug.LogWarning("[Character] NetworkObject가 유효하지 않습니다.");
            }
            else if (Runner == null)
            {
                Debug.LogError("[Character] NetworkRunner가 null입니다. Photon Fusion이 올바르게 설정되었는지 확인하세요.");
            }
            else if (!Object.HasStateAuthority)
            {
                Debug.Log("[Character] 이 클라이언트는 상태 권한이 없습니다. 스폰 무시함.");
            }
            else
            {
                if (opponentWaypoints == null || opponentWaypoints.Length == 0)
                {
                    Debug.LogError("[Character] opponentWaypoints가 null이거나 비어 있습니다.");
                }
                else if (opponentWaypoints[0] == null)
                {
                    Debug.LogError("[Character] opponentWaypoints[0]이 null입니다.");
                }
                else if (enemyMonsterPrefab == default(NetworkPrefabRef) || !enemyMonsterPrefab.IsValid)
                {
                    Debug.LogError("[Character] enemyMonsterPrefab이 유효하지 않습니다. Inspector에서 설정해주세요.");
                }
                else
                {
                    try
                    {
                        Vector3 spawnPos = opponentWaypoints[0].position;

                        // 적 몬스터로 다시 등장
                        var newObj = Runner.Spawn(
                            enemyMonsterPrefab,
                            spawnPos,
                            Quaternion.identity,
                            Object.InputAuthority,
                            (runner, spawnedObj) =>
                            {
                                var newMonster = spawnedObj.GetComponent<Monster>();
                                if (newMonster != null)
                                {
                                    newMonster.isAlly = false; // 상대 입장에서는 적
                                    newMonster.pathWaypoints = opponentWaypoints;
                                }
                            }
                        );
                        Debug.Log($"[Character] {characterName} => 상대 진영 몬스터로 스폰 완료");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[Character] 몬스터 스폰 중 오류 발생: {e.Message}");
                    }
                }
            }
        }

        // 자신 제거 (네트워크상 동기화)
        if (Object != null && Object.IsValid)
        {
            Runner.Despawn(Object);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackCooldown);

            currentTarget = FindTargetInRange();
            if (currentTarget != null)
            {
                Attack(currentTarget);
            }
        }
    }

    private Monster FindTargetInRange()
    {
        GameObject[] monsterObjs = GameObject.FindGameObjectsWithTag("Monster");
        Monster nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (GameObject mo in monsterObjs)
        {
            Monster m = mo.GetComponent<Monster>();
            if (m == null) continue;
            // 아군 몬스터면 스킵
            if (m.isAlly) 
                continue;

            float dist = Vector2.Distance(transform.position, m.transform.position);
            if (dist <= attackRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = m;
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

            if (bulletPanel != null && bulletPanel.gameObject.scene.IsValid())
            {
                bulletObj.transform.SetParent(bulletPanel, false);
            }

            RectTransform bulletRect = bulletObj.GetComponent<RectTransform>();
            if (bulletRect != null && bulletPanel != null)
            {
                Vector2 localPos = bulletPanel.InverseTransformPoint(transform.position);
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
                // === 수정 부분 ===
                // bullet에 areaIndex 전달
                bulletComp.Init(target, attackPower, bulletSpeed, isAreaAttack, areaAttackRadius, this.areaIndex);
                // === 수정 끝 ===
            }
        }
        else
        {
            // 총알이 없으면 즉시 공격 처리
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

    private void DoAreaDamage(Vector3 centerPos)
    {
        GameObject[] monsterObjs = GameObject.FindGameObjectsWithTag("Monster");
        foreach (GameObject mo in monsterObjs)
        {
            Monster m = mo.GetComponent<Monster>();
            if (m == null) continue;
            if (m.isAlly) 
                continue;

            float dist = Vector2.Distance(centerPos, m.transform.position);
            if (dist <= areaAttackRadius)
            {
                m.TakeDamage(attackPower);
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
        if (star == CharacterStar.TwoStar)
        {
            Debug.Log($"[Character] 2성 아웃라인 적용: {twoStarOutlineColor}, Width={twoStarOutlineWidth}");
        }
        else if (star == CharacterStar.ThreeStar)
        {
            Debug.Log($"[Character] 3성 아웃라인 적용: {threeStarOutlineColor}, Width={threeStarOutlineWidth}");
        }
    }

    /// <summary>
    /// 외부(PlacementManager)에서 총알 표시할 패널을 연결할 수 있는 메서드
    /// </summary>
    public void SetBulletPanel(RectTransform panel)
    {
        bulletPanel = panel;
    }
}
