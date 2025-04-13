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
        // GameManager 등에서 pathWaypoints 설정하거나, Inspector에서 수동으로 세팅
        // 예시로 여기서는 그냥 Inspector에서 할당받아 사용
    }

    private void Update()
    {
        if (isDead) return;
        MoveAlongPath();
    }

    /// <summary>
    /// 웨이포인트를 따라 이동하는 기본 로직
    /// </summary>
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
                // 마지막 웨이포인트에 도달한 경우
                OnReachEndPoint();
            }
        }
    }

    /// <summary>
    /// 몬스터가 최종 지점에 도달했을 때(예: 플레이어 라이프를 깎는다거나, 파괴 등)
    /// </summary>
    private void OnReachEndPoint()
    {
        // 예: 여기서는 몬스터를 제거만 처리
        Destroy(gameObject);
    }

    /// <summary>
    /// 데미지를 받았을 때 체력 처리
    /// </summary>
    /// <param name="damage"></param>
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
        // 추가 사망 처리(이펙트, 사운드, 점수 증가 등)
        Destroy(gameObject);
    }
}
