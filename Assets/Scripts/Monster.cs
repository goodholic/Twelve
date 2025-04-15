using UnityEngine;

/// <summary>
/// 2D 몬스터 예시.
/// 단순 이동. Waypoints를 따라 이동한 뒤 끝에 도달하면 제거.
/// </summary>
public class Monster : MonoBehaviour
{
    [Header("Monster Stats")]
    public float moveSpeed = 3f;
    public float health = 50f;

    [Header("Waypoint Path (2D)")]
    [Tooltip("2D에서 몬스터가 이동할 경로(Transform[]). x,y만 사용")]
    public Transform[] pathWaypoints;

    private int currentWaypointIndex = 0;
    private bool isDead = false;

    private void Update()
    {
        if (isDead) return;
        MoveAlongPath2D();
    }

    private void MoveAlongPath2D()
    {
        if (pathWaypoints == null || pathWaypoints.Length == 0) return;

        Transform target = pathWaypoints[currentWaypointIndex];
        Vector2 targetPos = target.position;
        Vector2 currentPos = transform.position;
        Vector2 direction = (targetPos - currentPos).normalized;

        transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);

        float distance = Vector2.Distance(currentPos, targetPos);
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
