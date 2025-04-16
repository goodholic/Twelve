using System.Collections;
using UnityEngine;

/// <summary>
/// 2D 캐릭터(배치 가능 오브젝트) 예시
/// </summary>
public enum CharacterStar
{
    OneStar = 1,
    TwoStar = 2,
    ThreeStar = 3
}

public class Character : MonoBehaviour
{
    [Header("Character Stats")]
    public CharacterStar star = CharacterStar.OneStar;
    public float attackPower = 10f;

    [Tooltip("1초당 공격 횟수(AttackRoutine 쿨타임 결정)")]
    public float attackSpeed = 1f;

    [Tooltip("공격 사거리(원 범위)")]
    public float attackRange = 1.5f;

    [Tooltip("현재 배치된 타일")]
    public Tile currentTile;

    [Tooltip("현재 공격 중인 몬스터(2D)")]
    public Monster currentTarget;

    [Header("Range Indicator Settings")]
    [Tooltip("원(서클) 형태로 시각화해줄 프리팹(예: 반투명 Circle Sprite 등)")]
    public GameObject rangeIndicatorPrefab;

    [Tooltip("사거리 원을 보여줄지 여부(체크 해제 시 숨길 수 있음)")]
    public bool showRangeIndicator = true;

    private GameObject rangeIndicatorInstance; // 사거리 표시용 런타임 오브젝트

    private float attackCooldown;

    // ===========================
    // [변경] 총알 발사 관련 설정
    // ===========================
    [Header("Bullet Settings")]
    [Tooltip("캐릭터가 발사할 총알(Projectile) 프리팹 (Bullet.cs가 붙은 오브젝트)")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 5f;

    // [제거] -> Character.cs에서 bulletPanel을 Inspector로 받지 않음
    // 대신 PlacementManager가 Scene의 bulletPanel을 가지고 있다가 할당해줄 예정
    private RectTransform bulletPanel;  // 내부적으로만 사용할 참조

    /// <summary>
    /// 배치 시, PlacementManager가 bulletPanel을 할당해주는 용도
    /// </summary>
    public void SetBulletPanel(RectTransform panel)
    {
        bulletPanel = panel;
    }

    private void Start()
    {
        // 별 등급에 따른 능력치 보정
        switch (star)
        {
            case CharacterStar.OneStar:
                // 그대로
                break;
            case CharacterStar.TwoStar:
                attackPower *= 1.3f;
                attackRange *= 1.1f;
                attackSpeed *= 1.1f;
                break;
            case CharacterStar.ThreeStar:
                attackPower *= 1.6f;
                attackRange *= 1.2f;
                attackSpeed *= 1.2f;
                break;
        }

        // 공격속도 -> 쿨타임 계산
        attackCooldown = 1f / attackSpeed;

        // 사거리 표시용 원 생성
        CreateRangeIndicator();

        // 공격 루틴 시작
        StartCoroutine(AttackRoutine());
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

        if (bulletPrefab == null)
        {
            // 총알 프리팹이 없다면 근접 공격처럼 즉시 데미지
            target.TakeDamage(attackPower);
            return;
        }

        // bulletPrefab(프로젝트 자산) -> 런타임 인스턴스화
        GameObject bulletObj = Instantiate(bulletPrefab);

        // bulletPanel이 존재한다면, bulletObj를 그 자식으로 붙임
        if (bulletPanel != null && bulletPanel.gameObject.scene.IsValid())
        {
            bulletObj.transform.SetParent(bulletPanel, false);
        }
        else
        {
            Debug.LogWarning($"[Character] bulletPanel이 유효하지 않음. (bulletObj 단독 생성)");
        }

        // UI(RectTransform)로 좌표 잡기
        RectTransform bulletRect = bulletObj.GetComponent<RectTransform>();
        if (bulletRect != null && bulletPanel != null)
        {
            Vector2 localPos = bulletPanel.InverseTransformPoint(transform.position);
            bulletRect.anchoredPosition = localPos;
            bulletRect.localRotation = Quaternion.identity;
        }
        else
        {
            // 3D Transform 경우 -> worldPosition
            bulletObj.transform.position = transform.position;
        }

        // 초기값 세팅
        Bullet bulletComp = bulletObj.GetComponent<Bullet>();
        if (bulletComp != null)
        {
            bulletComp.Init(target, attackPower, bulletSpeed);
        }
    }

    private void CreateRangeIndicator()
    {
        if (!showRangeIndicator) return;
        if (rangeIndicatorPrefab == null) return;

        if (rangeIndicatorInstance != null)
        {
            Destroy(rangeIndicatorInstance);
            rangeIndicatorInstance = null;
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
}
