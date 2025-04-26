using UnityEngine;
using System;

/// <summary>
/// 2D 몬스터 예시.
/// 단순 이동. Waypoints를 따라 이동한 뒤 끝에 도달하면 제거.
/// 
/// [변경] 마지막 웨이포인트(=성)에 도달 시 OnReachedCastle 이벤트를 호출하도록 수정.
/// </summary>
public class Monster : MonoBehaviour
{
    [Header("Monster Stats")]
    public float moveSpeed = 3f;
    public float health = 50f;

    [Header("Waypoint Path (2D)")]
    [Tooltip("2D에서 몬스터가 이동할 경로(Transform[]). x,y만 사용")]
    public Transform[] pathWaypoints;

    // 죽을 때 WaveSpawner 등에서 구독하는 이벤트
    public event Action OnDeath;

    // === 추가: 성(마지막 지점) 도달 시 알리는 이벤트
    public event Action OnReachedCastle;

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

    /// <summary>
    /// 마지막 웨이포인트에 도달했을 때(=성) 호출
    /// → 성 침투 처리를 위해 OnReachedCastle 이벤트 → 그 후 파괴
    /// </summary>
    private void OnReachEndPoint()
    {
        // 기존 Destroy(gameObject); 대신, 아래 이벤트 후 파괴
        OnReachedCastle?.Invoke();
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
        
        // 사망 시 이벤트
        OnDeath?.Invoke();
        
        Destroy(gameObject);
    }
}
