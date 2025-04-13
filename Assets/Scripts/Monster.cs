// Assets\Scripts\Monster.cs

using UnityEngine;

public class Monster : MonoBehaviour
{
    [Header("Monster Stats")]
    [Tooltip("몬스터 이동 속도")]
    public float moveSpeed = 2f;

    [Tooltip("몬스터 체력")]
    public float health = 50f;

    [Header("Waypoint Path")]
    [Tooltip("몬스터가 이동할 웨이포인트(또는 타일) 리스트")]
    public Transform[] pathWaypoints;

    private int currentWaypointIndex = 0;
    private bool isDead = false;

    private void Start()
    {
        // pathWaypoints를 Inspector에서 수동할당, 또는 GameManager 등에서 설정 가능
    }

    private void Update()
    {
        if (isDead) return;
        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        if (pathWaypoints == null || pathWaypoints.Length == 0) return;

        Transform targetWaypoint = pathWaypoints[currentWaypointIndex];
        Vector3 direction = (targetWaypoint.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        float distance = Vector3.Distance(transform.position, targetWaypoint.position);
        if (distance < 0.1f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= pathWaypoints.Length)
            {
                OnReachEndPoint();
            }
        }
    }

    private void OnReachEndPoint()
    {
        // 예: 마지막 지점에 도달하면 제거
        Destroy(gameObject);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        health -= damage;

        if (health <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Destroy(gameObject);
    }
}
