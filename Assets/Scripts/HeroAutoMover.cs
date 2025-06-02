// Assets\Scripts\HeroAutoMover.cs

using UnityEngine;

/// <summary>
/// 히어로(주인공) 자동 이동 컴포넌트
/// 
/// [게임 기획서 연계]
/// - 3라인 시스템: 왼쪽/중앙/오른쪽 웨이포인트를 따라 이동
/// - 전투 우선순위:
///   1. 적 탐지 시 → 사정거리 내면 공격, 밖이면 웨이포인트 이동
///   2. 웨이포인트 상실 시 → 중간성 목표
///   3. 중간성 파괴 시 → 최종성 목표
/// 
/// 현재는 단순히 가장 가까운 몬스터를 추적하는 방식이지만,
/// 추후 웨이포인트 시스템과 연동 필요
/// </summary>
public class HeroAutoMover : MonoBehaviour
{
    [Header("주인공 이동 속도(초당)")]
    public float moveSpeed = 2f;

    [Header("몬스터와 얼마나 가까워져야 멈출지 (가까이 다가가기 범위)")]
    public float stopDistance = 1.0f;

    [Header("[게임 기획서] 3라인 시스템 설정")]
    [Tooltip("히어로가 따라갈 웨이포인트 (왼쪽/중앙/오른쪽 중 하나)")]
    public Transform[] heroWaypoints;
    
    [Tooltip("현재 목표 웨이포인트 인덱스")]
    public int currentWaypointIndex = 0;
    
    [Tooltip("웨이포인트 도달 거리")]
    public float waypointReachDistance = 0.5f;

    private Character heroCharacter;
    private bool hasWaypoints = false;

    private void Start()
    {
        heroCharacter = GetComponent<Character>();
        
        // 웨이포인트 설정 확인
        if (heroWaypoints != null && heroWaypoints.Length > 0)
        {
            hasWaypoints = true;
            Debug.Log($"[HeroAutoMover] 웨이포인트 {heroWaypoints.Length}개 설정됨");
        }
        else
        {
            Debug.LogWarning("[HeroAutoMover] 웨이포인트가 설정되지 않음. 몬스터 추적 모드로 작동합니다.");
        }
    }

    private void Update()
    {
        // 게임 기획서에 따른 우선순위 처리
        
        // 1) 가장 가까운 '적' 몬스터(동일 areaIndex) 찾기
        Monster nearest = FindNearestMonster();
        
        if (nearest != null)
        {
            float dist = Vector2.Distance(transform.position, nearest.transform.position);
            
            // 사정거리 확인 (게임 기획서: 사정거리 내면 공격, 밖이면 웨이포인트 이동)
            if (heroCharacter != null && dist <= heroCharacter.attackRange)
            {
                // 사정거리 내 - 공격만 하고 이동하지 않음
                return;
            }
            else if (dist <= stopDistance)
            {
                // stopDistance 내에 있지만 사정거리 밖 - 멈춤
                return;
            }
        }
        
        // 2) 웨이포인트가 있다면 웨이포인트 따라 이동
        if (hasWaypoints && currentWaypointIndex < heroWaypoints.Length)
        {
            MoveAlongWaypoint();
        }
        // 3) 웨이포인트가 없거나 끝났다면 몬스터 추적
        else if (nearest != null)
        {
            MoveTowardsMonster(nearest);
        }
        // 4) 몬스터도 없다면 대기
    }

    /// <summary>
    /// 웨이포인트를 따라 이동
    /// </summary>
    private void MoveAlongWaypoint()
    {
        if (heroWaypoints[currentWaypointIndex] == null)
        {
            currentWaypointIndex++;
            return;
        }
        
        Vector2 targetPos = heroWaypoints[currentWaypointIndex].position;
        Vector2 currentPos = transform.position;
        float dist = Vector2.Distance(currentPos, targetPos);
        
        if (dist > waypointReachDistance)
        {
            Vector2 dir = (targetPos - currentPos).normalized;
            transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
        }
        else
        {
            // 웨이포인트 도달
            currentWaypointIndex++;
            
            if (currentWaypointIndex >= heroWaypoints.Length)
            {
                Debug.Log("[HeroAutoMover] 모든 웨이포인트 도달 완료");
                hasWaypoints = false; // 웨이포인트 상실 상태로 전환
                
                // [게임 기획서] 웨이포인트 상실 시 중간성 목표
                // TODO: MiddleCastle 타겟팅 로직 추가
            }
        }
    }

    /// <summary>
    /// 몬스터를 향해 이동
    /// </summary>
    private void MoveTowardsMonster(Monster target)
    {
        if (target == null) return;
        
        Vector2 dir = (target.transform.position - transform.position).normalized;
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

    /// <summary>
    /// [게임 기획서] 히어로를 특정 라인으로 변경
    /// 드래그 이동 시스템과 연동하여 사용
    /// </summary>
    public void ChangeToLine(int lineIndex)
    {
        if (!hasWaypoints || heroWaypoints == null || heroWaypoints.Length == 0)
        {
            Debug.LogWarning("[HeroAutoMover] 웨이포인트가 설정되지 않아 라인 변경 불가");
            return;
        }
        
        // lineIndex: 0=왼쪽, 1=중앙, 2=오른쪽
        if (lineIndex < 0 || lineIndex >= 3)
        {
            Debug.LogWarning($"[HeroAutoMover] 잘못된 라인 인덱스: {lineIndex}");
            return;
        }
        
        // TODO: RouteManager와 연동하여 해당 라인의 웨이포인트로 변경
        Debug.Log($"[HeroAutoMover] {lineIndex}번 라인으로 변경 요청 (구현 필요)");
    }
}