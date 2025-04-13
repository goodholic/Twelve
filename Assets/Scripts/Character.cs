// Assets\Scripts\Character.cs

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
    public float attackSpeed = 1f;
    public float attackRange = 1.5f;

    [Tooltip("현재 배치된 타일")]
    public Tile currentTile;

    [Tooltip("현재 공격 중인 몬스터(2D)")]
    public Monster currentTarget;

    private float attackCooldown;

    private void Start()
    {
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

        attackCooldown = 1f / attackSpeed;
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
        Monster[] allMonsters = FindObjectsOfType<Monster>();
        Monster nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (Monster m in allMonsters)
        {
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
        if (target != null)
        {
            target.TakeDamage(attackPower);
        }
    }
}
