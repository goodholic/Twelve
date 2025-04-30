using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum CharacterStar
{
    OneStar = 1,
    TwoStar = 2,
    ThreeStar = 3
}

public class Character : MonoBehaviour
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

    // 아군 유닛 관련 속성
    public bool isAlly = false;
    public bool isHero = false;
    public Transform[] pathWaypoints;
    public int currentWaypointIndex = 0;

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

        // 2성/3성 아웃라인 적용
        ApplyStarVisual();
    }

    /// <summary>
    /// 드래그/배치 시, PlacementManager에서 호출해줄 수 있음
    /// </summary>
    public void SetBulletPanel(RectTransform panel)
    {
        bulletPanel = panel;
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
    /// 마지막 웨이포인트에 도달하면 사라짐( Destroy(gameObject) ).
    /// </summary>
    private void MoveAlongWaypoints()
    {
        if (currentWaypointIndex >= pathWaypoints.Length) return;

        Transform target = pathWaypoints[currentWaypointIndex];
        if (target == null) return;

        Vector2 currentPos = transform.position;
        Vector2 targetPos = target.position;
        Vector2 dir = (targetPos - currentPos).normalized;

        float distThisFrame = 3f * Time.deltaTime; // 이동속도(예시). 필요시 별도 필드 활용 가능
        transform.position += (Vector3)(dir * distThisFrame);

        // 목표점에 가까워지면 다음 웨이포인트로
        float dist = Vector2.Distance(currentPos, targetPos);
        if (dist < distThisFrame * 1.5f)
        {
            currentWaypointIndex++;
            // 모든 웨이포인트를 돌았다면 즉시 사라짐
            if (currentWaypointIndex >= pathWaypoints.Length)
            {
                Destroy(gameObject);
            }
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

    /// <summary>
    /// 사거리 내 몬스터 찾기 (단, isAlly == true 몬스터는 제외 → 공격하지 않음)
    /// </summary>
    private Monster FindTargetInRange()
    {
        GameObject[] monsterObjs = GameObject.FindGameObjectsWithTag("Monster");
        Monster nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (GameObject mo in monsterObjs)
        {
            Monster m = mo.GetComponent<Monster>();
            if (m == null) continue;

            // (중요) 아군 몬스터면 스킵
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

        // (A) 총알 프리팹 사용
        if (bulletPrefab != null)
        {
            GameObject bulletObj = Instantiate(bulletPrefab);

            // bulletPanel이 있으면 자식으로 생성
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
                bulletComp.Init(target, attackPower, bulletSpeed, isAreaAttack, areaAttackRadius);
            }
        }
        else
        {
            // (B) 총알이 없다면 즉시 공격 처리
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

    /// <summary>
    /// 광역 공격 처리
    /// </summary>
    private void DoAreaDamage(Vector3 centerPos)
    {
        GameObject[] monsterObjs = GameObject.FindGameObjectsWithTag("Monster");
        foreach (GameObject mo in monsterObjs)
        {
            Monster m = mo.GetComponent<Monster>();
            if (m == null) continue;

            // 아군 몬스터는 스킵
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

    /// <summary>
    /// 사거리 보여줄 원형 인디케이터(옵션)
    /// </summary>
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

    // ========================================================
    // (합성 로직) 전부 삭제 - UpgradeStar() 등 제거
    // ========================================================

    /// <summary>
    /// 별 등급 시각적 (예시)
    /// </summary>
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
}
