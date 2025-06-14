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
/// - 히어로도 지역 간 이동 가능
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
    private CharacterMovement heroMovement;
    private bool hasWaypoints = false;

    private void Start()
    {
        heroCharacter = GetComponent<Character>();
        heroMovement = GetComponent<CharacterMovement>();
        
        // 웨이포인트 설정 확인
        if (heroWaypoints != null && heroWaypoints.Length > 0)
        {
            hasWaypoints = true;
            Debug.Log($"[HeroAutoMover] 웨이포인트 {heroWaypoints.Length}개 설정됨");
        }
        else
        {
            Debug.LogWarning("[HeroAutoMover] 웨이포인트가 설정되지 않음. CharacterMovement의 웨이포인트를 사용합니다.");
            
            // CharacterMovement의 웨이포인트 사용
            if (heroMovement != null)
            {
                Debug.Log("[HeroAutoMover] CharacterMovement의 웨이포인트 시스템 사용");
            }
        }

        // 히어로 초기 설정
        if (heroCharacter != null)
        {
            Debug.Log($"[HeroAutoMover] 히어로 {heroCharacter.characterName} 자동 이동 시작 (지역{heroCharacter.areaIndex})");
        }
    }

    private void Update()
    {
        if (heroCharacter == null) return;
        
        // CharacterMovement가 있으면 그것에 맡김
        if (heroMovement != null && heroMovement.enabled)
        {
            // CharacterMovement가 알아서 웨이포인트 이동과 지역 간 점프를 처리함
            return;
        }
        
        // CharacterMovement가 없거나 비활성화된 경우 기본 이동 로직
        MoveToNearestMonster();
    }

    /// <summary>
    /// 가장 가까운 몬스터를 찾아서 이동 (기본 로직)
    /// </summary>
    private void MoveToNearestMonster()
    {
        // 현재 위치에서 가장 가까운 몬스터 찾기
        Monster nearestMonster = FindNearestMonster();
        
        if (nearestMonster != null)
        {
            Vector3 dirToMonster = (nearestMonster.transform.position - transform.position).normalized;
            float distToMonster = Vector3.Distance(transform.position, nearestMonster.transform.position);

            // 사정거리 내에 있으면 공격 (이동하지 않음)
            if (distToMonster <= heroCharacter.attackRange)
            {
                // CharacterCombat이 알아서 공격 처리
                return;
            }

            // 사정거리 밖이면 웨이포인트 따라 이동
            if (hasWaypoints && currentWaypointIndex < heroWaypoints.Length)
            {
                MoveToWaypoint();
            }
            else
            {
                // 웨이포인트가 없으면 몬스터 방향으로 이동
                if (distToMonster > stopDistance)
                {
                    transform.position += dirToMonster * moveSpeed * Time.deltaTime;
                }
            }
        }
        else
        {
            // 몬스터가 없으면 웨이포인트 따라 이동
            if (hasWaypoints && currentWaypointIndex < heroWaypoints.Length)
            {
                MoveToWaypoint();
            }
        }
    }

    /// <summary>
    /// 웨이포인트로 이동
    /// </summary>
    private void MoveToWaypoint()
    {
        if (currentWaypointIndex >= heroWaypoints.Length) return;
        
        Transform targetWaypoint = heroWaypoints[currentWaypointIndex];
        if (targetWaypoint == null) return;
        
        Vector3 direction = (targetWaypoint.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetWaypoint.position);
        
        if (distance > waypointReachDistance)
        {
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
        else
        {
            // 웨이포인트 도달, 다음 웨이포인트로
            currentWaypointIndex++;
            Debug.Log($"[HeroAutoMover] 웨이포인트 {currentWaypointIndex} 도달");
        }
    }

    /// <summary>
    /// 가장 가까운 몬스터 찾기
    /// </summary>
    private Monster FindNearestMonster()
    {
        string targetTag = (heroCharacter.areaIndex == 1) ? "EnemyMonster" : "Monster";
        GameObject[] monsterObjs = GameObject.FindGameObjectsWithTag(targetTag);
        
        Monster nearestMonster = null;
        float nearestDistance = Mathf.Infinity;

        foreach (GameObject obj in monsterObjs)
        {
            if (obj == null) continue;
            
            Monster monster = obj.GetComponent<Monster>();
            if (monster == null) continue;

            float distance = Vector3.Distance(transform.position, monster.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestMonster = monster;
            }
        }

        return nearestMonster;
    }
}