// Assets\Scripts\Character.cs

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
        // 별 등급에 따라 공격력/사거리/공격속도를 가중치로 적용
        switch (star)
        {
            case CharacterStar.OneStar:
                attackPower *= 1f;
                attackRange *= 1f;
                attackSpeed *= 1f;
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

        // 초당 attackSpeed 번 공격 -> 1/attackSpeed 초 간격
        attackCooldown = 1f / attackSpeed;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackCooldown);

            // 사거리 내 몬스터 탐색
            currentTarget = FindTargetInRange();
            if (currentTarget != null)
            {
                Attack(currentTarget);
            }
        }
    }

    private Monster FindTargetInRange()
    {
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

    private void Attack(Monster target)
    {
        if (target != null)
        {
            target.TakeDamage(attackPower);
        }
    }
}
