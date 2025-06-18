using System.Collections;
using UnityEngine;

/// <summary>
/// 캐릭터의 이동을 관리하는 컴포넌트
/// </summary>
public class CharacterMovementManager : MonoBehaviour
{
    [Header("캐릭터 참조")]
    private Character character;
    
    [Header("이동 경로")]
    private Transform[] pathWaypoints;
    
    [Header("이동 상태")]
    public bool isMoving = false;
    private int currentWaypointIndex = 0;
    private int maxWaypointIndex = 0;
    
    [Header("현재 지역")]
    public int currentRegion = 1; // 1 또는 2
    
    // 코루틴
    private Coroutine moveCoroutine;
    
    // 전투 상태
    private bool isInCombat = false;
    
    private void Awake()
    {
        character = GetComponent<Character>();
        if (character == null)
        {
            Debug.LogError("[CharacterMovementManager] Character 컴포넌트를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 이동 경로 설정
    /// </summary>
    public void SetPath(Transform[] waypoints)
    {
        pathWaypoints = waypoints;
        currentWaypointIndex = 0;
        
        if (waypoints != null && waypoints.Length > 0)
        {
            maxWaypointIndex = waypoints.Length - 1;
        }
    }
    
    /// <summary>
    /// 이동 시작
    /// </summary>
    public void StartMoving()
    {
        if (pathWaypoints == null || pathWaypoints.Length == 0)
        {
            Debug.LogWarning("[CharacterMovementManager] 경로가 설정되지 않았습니다!");
            return;
        }
        
        isMoving = true;
        
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        
        moveCoroutine = StartCoroutine(MoveAlongPath());
    }
    
    /// <summary>
    /// 이동 중지
    /// </summary>
    public void StopMoving()
    {
        isMoving = false;
        
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
    }
    
    /// <summary>
    /// 특정 라인으로 이동 경로 변경
    /// </summary>
    public void ChangeRoute(RouteType newRoute, Transform[] newWaypoints)
    {
        Debug.Log($"[CharacterMovementManager] {character.characterName} 경로 변경: {newRoute}");
        
        // 새로운 경로 설정
        pathWaypoints = newWaypoints;
        currentWaypointIndex = FindClosestWaypointIndex();
        maxWaypointIndex = newWaypoints.Length - 1;
        
        // 이동 재시작
        if (isMoving)
        {
            StopMoving();
            StartMoving();
        }
    }
    
    /// <summary>
    /// 가장 가까운 웨이포인트 인덱스 찾기
    /// </summary>
    private int FindClosestWaypointIndex()
    {
        if (pathWaypoints == null || pathWaypoints.Length == 0) return 0;
        
        int closestIndex = 0;
        float closestDistance = float.MaxValue;
        
        for (int i = 0; i < pathWaypoints.Length; i++)
        {
            if (pathWaypoints[i] == null) continue;
            
            float distance = Vector3.Distance(transform.position, pathWaypoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        
        return closestIndex;
    }
    
    /// <summary>
    /// 경로를 따라 이동
    /// </summary>
    private IEnumerator MoveAlongPath()
    {
        while (currentWaypointIndex <= maxWaypointIndex && character.currentHP > 0)
        {
            // 전투 중이 아닐 때만 이동
            if (!isInCombat)
            {
                if (currentWaypointIndex < pathWaypoints.Length)
                {
                    Transform targetWaypoint = pathWaypoints[currentWaypointIndex];
                    if (targetWaypoint == null)
                    {
                        currentWaypointIndex++;
                        continue;
                    }
                    
                    // 웨이포인트로 이동
                    float moveSpeed = character.moveSpeed;
                    
                    while (Vector3.Distance(transform.position, targetWaypoint.position) > 0.1f)
                    {
                        if (!isMoving || isInCombat) break;
                        
                        Vector3 direction = (targetWaypoint.position - transform.position).normalized;
                        transform.position += direction * moveSpeed * Time.deltaTime;
                        
                        // 방향에 따른 스프라이트 업데이트
                        UpdateCharacterDirectionSprite();
                        
                        yield return null;
                    }
                    
                    // 다음 웨이포인트로
                    currentWaypointIndex++;
                }
            }
            else
            {
                // 전투 중일 때는 대기
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        // 경로 끝에 도달했을 때
        if (currentWaypointIndex > maxWaypointIndex)
        {
            Debug.Log($"[CharacterMovementManager] {character.characterName} 경로 끝 도달");
            OnReachedPathEnd();
        }
    }
    
    /// <summary>
    /// 경로 끝에 도달했을 때 - 성을 직접 찾아서 공격
    /// </summary>
    private void OnReachedPathEnd()
    {
        // 가장 가까운 적 성 찾기
        bool foundCastle = false;
        float closestDistance = float.MaxValue;
        IDamageable closestCastle = null;
        string castleType = "";
        int targetAreaIndex = (currentRegion == 1) ? 2 : 1; // 반대 지역
        
        // 중간성 찾기
        MiddleCastle[] middleCastles = FindObjectsByType<MiddleCastle>(FindObjectsSortMode.None);
        foreach (var castle in middleCastles)
        {
            if (castle == null || castle.IsDestroyed()) continue;
            if (castle.areaIndex != targetAreaIndex) continue; // 적 지역의 성만 공격
            
            float distance = Vector3.Distance(transform.position, castle.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCastle = castle;
                castleType = "중간성";
                foundCastle = true;
            }
        }
        
        // 중간성을 못 찾았거나 모두 파괴된 경우 최종성 찾기
        if (!foundCastle)
        {
            FinalCastle[] finalCastles = FindObjectsByType<FinalCastle>(FindObjectsSortMode.None);
            foreach (var castle in finalCastles)
            {
                if (castle == null || castle.IsDestroyed()) continue;
                if (castle.areaIndex != targetAreaIndex) continue; // 적 지역의 성만 공격
                
                float distance = Vector3.Distance(transform.position, castle.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCastle = castle;
                    castleType = "최종성";
                    foundCastle = true;
                }
            }
        }
        
        // 찾은 성 공격
        if (foundCastle && closestCastle != null)
        {
            Debug.Log($"[CharacterMovementManager] {character.characterName}이(가) 지역{targetAreaIndex} {castleType}을 공격!");
            closestCastle.TakeDamage(character.attackPower);
            
            // 공격 이펙트 (선택사항)
            if (character.attackEffectPrefab != null)
            {
                GameObject effect = Instantiate(character.attackEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 1f);
            }
        }
        else
        {
            Debug.LogWarning($"[CharacterMovementManager] {character.characterName}이(가) 공격할 성을 찾을 수 없습니다!");
        }
        
        // 캐릭터 제거
        DestroyCharacter();
    }
    
    /// <summary>
    /// 캐릭터 제거
    /// </summary>
    private void DestroyCharacter()
    {
        Debug.Log($"[CharacterMovementManager] {character.characterName} 임무 완료, 제거됨");
        
        // 매니저에서 제거 (CharacterManager 없음)
        // if (CharacterManager.Instance != null)
        // {
        //     CharacterManager.Instance.RemoveCharacter(character);
        // }
        
        // 오브젝트 제거
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 새로운 지역으로 웨이포인트 변경
    /// </summary>
    public void SetWaypointsForNewRegion(Transform[] newWaypoints)
    {
        pathWaypoints = newWaypoints;
        currentWaypointIndex = 0;
        maxWaypointIndex = newWaypoints.Length - 1;
        
        Debug.Log($"[CharacterMovementManager] 새로운 웨이포인트로 변경 - 총 {newWaypoints.Length}개");
        
        // 이동 재시작
        if (isMoving)
        {
            StopMoving();
            StartMoving();
        }
    }
    
    /// <summary>
    /// 전투 상태 설정
    /// </summary>
    public void SetInCombat(bool inCombat)
    {
        isInCombat = inCombat;
    }
    
    /// <summary>
    /// GameObject 배열을 Transform 배열로 변환
    /// </summary>
    private Transform[] ConvertToTransforms(GameObject[] gameObjects)
    {
        if (gameObjects == null) return null;
        
        Transform[] transforms = new Transform[gameObjects.Length];
        for (int i = 0; i < gameObjects.Length; i++)
        {
            transforms[i] = gameObjects[i] != null ? gameObjects[i].transform : null;
        }
        
        return transforms;
    }
    
    /// <summary>
    /// 캐릭터 방향에 따른 스프라이트 업데이트
    /// </summary>
    private void UpdateCharacterDirectionSprite()
    {
        // 캐릭터의 이동 방향에 따라 스프라이트 플립 또는 변경
        // 구현은 캐릭터의 시각적 요구사항에 따라 달라질 수 있음
    }
    
    /// <summary>
    /// 현재 웨이포인트 인덱스 가져오기
    /// </summary>
    public int GetCurrentWaypointIndex()
    {
        return currentWaypointIndex;
    }
    
    /// <summary>
    /// 디버그용 - 경로 표시 (에디터에서만 실행)
    /// </summary>
    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (pathWaypoints == null || pathWaypoints.Length == 0) return;
        
        Gizmos.color = Color.blue;
        
        // 현재 위치에서 다음 웨이포인트까지 선 그리기
        if (currentWaypointIndex < pathWaypoints.Length && pathWaypoints[currentWaypointIndex] != null)
        {
            Gizmos.DrawLine(transform.position, pathWaypoints[currentWaypointIndex].position);
        }
        
        // 전체 경로 표시
        for (int i = 0; i < pathWaypoints.Length - 1; i++)
        {
            if (pathWaypoints[i] != null && pathWaypoints[i + 1] != null)
            {
                Gizmos.DrawLine(pathWaypoints[i].position, pathWaypoints[i + 1].position);
            }
        }
    }
    #endif
}