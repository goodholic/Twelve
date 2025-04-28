using UnityEngine;
using System;

/// <summary>
/// 2D 몬스터 예시.
/// - isAlly=true 이면 아군 몬스터(끝 지점 도달해도 성 체력에 영향을 주지 않고 그냥 사라짐).
/// - 적 몬스터가 죽으면 => 해당 위치에서 아군 몬스터로 변함(OurMonsterPanel에 생성).
/// </summary>
public class Monster : MonoBehaviour
{
    [Header("Monster Stats")]
    public float moveSpeed = 3f;
    public float health = 50f;

    [Header("Waypoint Path (2D)")]
    public Transform[] pathWaypoints;

    public bool isAlly = false; // 아군 여부

    // 죽을 때 이벤트
    public event Action OnDeath;

    // 성 도달 시
    public event Action<Monster> OnReachedCastle;

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
        // 아군 몬스터면 성 체력 감소 없이 즉시 파괴
        OnReachedCastle?.Invoke(this);
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
        if (!isAlly)
        {
            // === 적 몬스터 사망 시 => 해당 위치에서 아군 몬스터로 부활 ===
            WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
            PlacementManager pm = FindAnyObjectByType<PlacementManager>();

            if (spawner != null && spawner.monsterPrefab != null && pm != null)
            {
                RectTransform allyPanel = pm.ourMonsterPanel;
                if (allyPanel == null)
                {
                    Debug.LogWarning("[Monster] ourMonsterPanel이 null => 아군 몬스터 생성 불가");
                }
                else
                {
                    // OurMonster Panel에 부활
                    GameObject allyObj = Instantiate(spawner.monsterPrefab, allyPanel);

                    // 같은 위치로 UI 좌표 변환
                    RectTransform allyRect = allyObj.GetComponent<RectTransform>();
                    RectTransform deadRect = GetComponent<RectTransform>();

                    if (allyRect != null && deadRect != null)
                    {
                        Vector2 localPos = allyPanel.InverseTransformPoint(deadRect.transform.position);
                        allyRect.anchoredPosition = localPos;
                        allyRect.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        // 3D 월드라면 worldPos
                        allyObj.transform.position = this.transform.position;
                        allyObj.transform.localRotation = Quaternion.identity;
                    }

                    // 설정
                    Monster ally = allyObj.GetComponent<Monster>();
                    if (ally != null)
                    {
                        ally.isAlly = true;
                        ally.pathWaypoints = spawner.pathWaypoints;
                        ally.health = 50f; // 임의값
                    }

                    Debug.Log("[Monster] 적 몬스터 사망 -> 해당 위치에서 아군 몬스터로 부활!");
                }
            }
        }

        isDead = true;
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}
