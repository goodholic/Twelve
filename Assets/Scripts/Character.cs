using System.Collections;
using UnityEngine;

public enum CharacterStar
{
    OneStar = 1,
    TwoStar = 2,
    ThreeStar = 3
}

public class Character : MonoBehaviour
{
    [Header("Character Stats")]
    [Tooltip("1성, 2성, 3성 구분")]
    public CharacterStar star = CharacterStar.OneStar;

    [Tooltip("캐릭터 공격력")]
    public float attackPower = 10f;

    [Tooltip("초당 공격 횟수 (공격속도)")]
    public float attackSpeed = 1f;

    [Tooltip("사거리")]
    public float attackRange = 1.5f;

    [Tooltip("현재 배치된 타일")]
    public Tile currentTile;

    [Header("Targets")]
    [Tooltip("현재 공격 중인 몬스터")]
    public Monster currentTarget;

    private float attackCooldown;

    private void Start()
    {
        // 별에 따라 기본 능력치를 가중치로 다르게 설정하거나
        // 여기서 스위치를 활용해 차등 적용할 수 있습니다.
        switch (star)
        {
            case CharacterStar.OneStar:
                attackPower *= 1f;    // 1성
                attackRange *= 1f;
                attackSpeed *= 1f;
                break;
            case CharacterStar.TwoStar:
                attackPower *= 1.3f;  // 2성은 공격력 30% 강화 예시
                attackRange *= 1.1f;
                attackSpeed *= 1.1f;
                break;
            case CharacterStar.ThreeStar:
                attackPower *= 1.6f;  // 3성은 공격력 60% 강화 예시
                attackRange *= 1.2f;
                attackSpeed *= 1.2f;
                break;
        }

        // 쿨다운 계산 (초당 공격 속도 = 공격속도, 즉 1초에 attackSpeed 번 공격)
        attackCooldown = 1f / attackSpeed;
        StartCoroutine(AttackRoutine());
    }

    /// <summary>
    /// 일정 주기로 주변 몬스터를 탐색하고, 공격할 수 있는 대상이 있으면 공격
    /// </summary>
    /// <returns></returns>
    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackCooldown);

            // 사거리 내 몬스터 탐색
            currentTarget = FindTargetInRange();
            if (currentTarget != null)
            {
                // 공격 수행
                Attack(currentTarget);
            }
        }
    }

    /// <summary>
    /// 사거리 내에 있는 가장 가까운(또는 임의의) 몬스터를 찾습니다.
    /// </summary>
    /// <returns></returns>
    private Monster FindTargetInRange()
    {
        // 모든 몬스터를 대상으로 탐색(간단 예시)
        Monster[] allMonsters = FindObjectsOfType<Monster>();

        Monster nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (Monster m in allMonsters)
        {
            float dist = Vector3.Distance(transform.position, m.transform.position);
            if (dist <= attackRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = m;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 몬스터에게 데미지를 준다. (즉시 처리)
    /// </summary>
    /// <param name="target"></param>
    private void Attack(Monster target)
    {
        if (target != null)
        {
            target.TakeDamage(attackPower);
        }
    }
}
