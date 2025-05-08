// Assets\Scripts\HeroAutoMover.cs

using UnityEngine;

public class HeroAutoMover : MonoBehaviour
{
    [Header("주인공 이동 속도(초당)")]
    public float moveSpeed = 2f;

    [Header("몬스터와 얼마나 가까워져야 멈출지 (가까이 다가가기 범위)")]
    public float stopDistance = 1.0f;

    private Character heroCharacter;

    private void Start()
    {
        heroCharacter = GetComponent<Character>();
    }

    private void Update()
    {
        // 1) 가장 가까운 '적' 몬스터(동일 areaIndex) 찾기
        Monster nearest = FindNearestMonster();
        if (nearest == null)
        {
            // 주변에 적이 없으면 이동 안 함
            return;
        }

        // 2) 거리 계산
        float dist = Vector2.Distance(transform.position, nearest.transform.position);

        // 3) 멈출지/이동할지 결정
        if (dist <= stopDistance)
        {
            // 일정 거리 이내면 이동 중지
            return;
        }

        // 그 외에는 몬스터를 향해 이동
        Vector2 dir = (nearest.transform.position - transform.position).normalized;
        transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 현재 heroCharacter.areaIndex와 동일한 몬스터 중,
    /// 가장 가까운 몬스터를 찾는다.
    /// </summary>
    private Monster FindNearestMonster()
    {
        Monster[] all = FindObjectsByType<Monster>(FindObjectsSortMode.None);
        Monster nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var m in all)
        {
            if (m == null) continue;

            // heroCharacter의 areaIndex와 일치하는 몬스터만 추적
            if (heroCharacter != null && m.areaIndex != heroCharacter.areaIndex)
            {
                continue;
            }

            float dist = Vector2.Distance(transform.position, m.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = m;
            }
        }
        return nearest;
    }
}
