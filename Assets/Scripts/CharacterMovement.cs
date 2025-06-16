using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 월드 좌표 기반 캐릭터 이동 시스템
/// 웨이포인트를 따라 이동하고 점프를 처리합니다.
/// </summary>
public class CharacterMovement : MonoBehaviour
{
    private Character character;
    private CharacterStats stats;
    private CharacterVisual visual;
    
    [Header("이동 설정")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 10f;
    public float stoppingDistance = 0.1f;
    
    [Header("웨이포인트")]
    public Transform[] pathWaypoints;
    public int currentWaypointIndex = -1;
    public int maxWaypointIndex = -1;
    
    [Header("점프 설정")]
    public float jumpHeight = 2f;
    public float jumpDuration = 1f;
    private bool isJumping = false;
    private bool isJumpingAcross = false;
    private bool hasJumped = false;
    
    [Header("전투 중 이동")]
    private bool isInCombat = false;
    private Vector3 combatStartPosition;
    
    // 이동 상태 프로퍼티 추가
    public bool isMoving { get; private set; }
    
    // 코루틴 참조
    private Coroutine moveCoroutine;
    private Coroutine jumpCoroutine;
    
    public void Initialize(Character character, CharacterStats stats)
    {
        this.character = character;
        this.stats = stats;
        this.visual = GetComponent<CharacterVisual>();
        
        // 캐릭터의 웨이포인트 정보 동기화
        if (character.pathWaypoints != null)
        {
            pathWaypoints = character.pathWaypoints;
            currentWaypointIndex = character.currentWaypointIndex;
            maxWaypointIndex = character.maxWaypointIndex;
        }
    }
    
    /// <summary>
    /// 웨이포인트 설정
    /// </summary>
    public void SetWaypoints(Transform[] waypoints, int startIndex = 0)
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"[CharacterMovement] {character.characterName}: 웨이포인트가 없습니다!");
            return;
        }
        
        pathWaypoints = waypoints;
        currentWaypointIndex = startIndex;
        maxWaypointIndex = waypoints.Length - 1;
        
        // Character 컴포넌트에도 동기화
        character.pathWaypoints = pathWaypoints;
        character.currentWaypointIndex = currentWaypointIndex;
        character.maxWaypointIndex = maxWaypointIndex;
        
        Debug.Log($"[CharacterMovement] {character.characterName}: 웨이포인트 설정 완료. " +
                  $"경로 길이: {waypoints.Length}, 시작 인덱스: {startIndex}");
    }
    
    /// <summary>
    /// 이동 시작
    /// </summary>
    public void StartMoving()
    {
        if (character.isHero)
        {
            Debug.Log($"[CharacterMovement] 히어로 {character.characterName}은(는) 이동하지 않습니다.");
            return;
        }
        
        if (pathWaypoints == null || pathWaypoints.Length == 0)
        {
            Debug.LogWarning($"[CharacterMovement] {character.characterName}: 웨이포인트가 없어 이동할 수 없습니다!");
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
        
        if (jumpCoroutine != null)
        {
            StopCoroutine(jumpCoroutine);
            jumpCoroutine = null;
        }
        
        isInCombat = false;
    }
    
    /// <summary>
    /// 웨이포인트를 따라 이동하는 코루틴
    /// </summary>
    private IEnumerator MoveAlongPath()
    {
        while (pathWaypoints != null && currentWaypointIndex >= 0 && currentWaypointIndex < pathWaypoints.Length && isMoving)
        {
            Transform currentWaypoint = pathWaypoints[currentWaypointIndex];
            
            if (currentWaypoint == null)
            {
                Debug.LogWarning($"[CharacterMovement] {character.characterName}: 웨이포인트 {currentWaypointIndex}가 null입니다!");
                currentWaypointIndex++;
                continue;
            }
            
            // 목표 지점으로 이동
            while (Vector3.Distance(transform.position, currentWaypoint.position) > stoppingDistance && isMoving)
            {
                if (!isInCombat)
                {
                    Vector3 direction = (currentWaypoint.position - transform.position).normalized;
                    transform.position += direction * moveSpeed * Time.deltaTime;
                    
                    // 방향 업데이트
                    UpdateCharacterDirectionSprite();
                }
                
                yield return null;
            }
            
            // 현재 웨이포인트 도달
            OnReachWaypoint();
            
            // 다음 웨이포인트로
            currentWaypointIndex++;
            
            // 경로 끝에 도달
            if (currentWaypointIndex >= pathWaypoints.Length)
            {
                OnReachFinalWaypoint();
                break;
            }
        }
        
        isMoving = false;
    }
    
    /// <summary>
    /// 웨이포인트 도달 시 처리
    /// </summary>
    private void OnReachWaypoint()
    {
        Debug.Log($"[CharacterMovement] {character.characterName}이(가) 웨이포인트 {currentWaypointIndex}에 도달!");
        
        // 점프 체크
        if (CheckForJumpPoint())
        {
            StartJump();
        }
    }
    
    /// <summary>
    /// 최종 웨이포인트 도달 시 처리
    /// </summary>
    private void OnReachFinalWaypoint()
    {
        Debug.Log($"[CharacterMovement] {character.characterName}이(가) 최종 목적지에 도달!");
        
        // 게임 기획서: 중간성 또는 최종성 도달 시 처리
        GameObject targetCastle = GetTargetCastle();
        if (targetCastle != null)
        {
            // 성 공격 또는 파괴
            MiddleCastle middleCastle = targetCastle.GetComponent<MiddleCastle>();
            FinalCastle finalCastle = targetCastle.GetComponent<FinalCastle>();
            
            if (middleCastle != null)
            {
                middleCastle.TakeDamage(character.attackPower);
            }
            else if (finalCastle != null)
            {
                finalCastle.TakeDamage(character.attackPower);
            }
        }
        
        // 캐릭터 제거
        DestroyCharacter();
    }
    
    /// <summary>
    /// 목표 성 찾기
    /// </summary>
    private GameObject GetTargetCastle()
    {
        RouteManager routeManager = RouteManager.Instance;
        if (routeManager == null) return null;
        
        // 현재 라우트와 지역에 따라 목표 성 결정
        RouteType currentRoute = (RouteType)character.selectedRoute;
        
        if (character.areaIndex == 1)
        {
            // 지역1 캐릭터는 지역2의 성을 목표로
            switch (currentRoute)
            {
                case RouteType.Left:
                    return routeManager.region2LeftMiddleCastle;
                case RouteType.Center:
                    return routeManager.region2CenterMiddleCastle;
                case RouteType.Right:
                    return routeManager.region2RightMiddleCastle;
            }
        }
        else if (character.areaIndex == 2)
        {
            // 지역2 캐릭터는 지역1의 성을 목표로
            switch (currentRoute)
            {
                case RouteType.Left:
                    return routeManager.region1LeftMiddleCastle;
                case RouteType.Center:
                    return routeManager.region1CenterMiddleCastle;
                case RouteType.Right:
                    return routeManager.region1RightMiddleCastle;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 점프 포인트 체크
    /// </summary>
    private bool CheckForJumpPoint()
    {
        // 점프 로직은 별도의 CharacterJump 컴포넌트에서 처리
        CharacterJump jump = GetComponent<CharacterJump>();
        if (jump != null)
        {
            RouteType route = (RouteType)character.selectedRoute;
            return jump.CheckIfAtJumpPoint(route, hasJumped);
        }
        return false;
    }
    
    /// <summary>
    /// 점프 시작
    /// </summary>
    private void StartJump()
    {
        if (isJumping || hasJumped) return;
        
        isJumping = true;
        if (jumpCoroutine != null)
        {
            StopCoroutine(jumpCoroutine);
        }
        
        jumpCoroutine = StartCoroutine(JumpCoroutine());
    }
    
    /// <summary>
    /// 점프 코루틴
    /// </summary>
    private IEnumerator JumpCoroutine()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = GetJumpEndPosition();
        float elapsedTime = 0f;
        
        while (elapsedTime < jumpDuration)
        {
            float t = elapsedTime / jumpDuration;
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += height;
            transform.position = pos;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        transform.position = endPos;
        isJumping = false;
        hasJumped = true;
        
        // 점프 완료 후 처리
        OnJumpComplete();
    }
    
    /// <summary>
    /// 점프 종료 위치 계산
    /// </summary>
    private Vector3 GetJumpEndPosition()
    {
        // 다음 웨이포인트 또는 지정된 점프 위치
        if (currentWaypointIndex + 1 < pathWaypoints.Length)
        {
            return pathWaypoints[currentWaypointIndex + 1].position;
        }
        return transform.position + Vector3.forward * 5f;
    }
    
    /// <summary>
    /// 점프 완료 시 처리
    /// </summary>
    private void OnJumpComplete()
    {
        // 지역 변경
        character.areaIndex = character.areaIndex == 1 ? 2 : 1;
        
        // 새로운 웨이포인트 설정
        UpdateWaypointsAfterJump();
        
        Debug.Log($"[CharacterJump] {character.characterName} 점프 완료! 새 지역: {character.areaIndex}");
    }
    
    /// <summary>
    /// 점프 후 웨이포인트 업데이트
    /// </summary>
    private void UpdateWaypointsAfterJump()
    {
        RouteManager routeManager = RouteManager.Instance;
        if (routeManager == null)
        {
            Debug.LogWarning($"[CharacterMovement] RouteManager를 찾을 수 없습니다!");
            return;
        }
        
        // 라우트 타입 결정
        RouteType route = DetermineRouteFromPosition();
        
        // 새 웨이포인트 설정
        Transform[] newWaypoints = null;
        
        if (character.areaIndex == 1)
        {
            // 지역1 → 지역2로 점프했으므로 지역2 웨이포인트 사용
            newWaypoints = GetRegion2Waypoints(routeManager, route);
            character.areaIndex = 2; // 지역 변경
        }
        else if (character.areaIndex == 2)
        {
            // 지역2 → 지역1로 점프했으므로 지역1 웨이포인트 사용
            newWaypoints = GetRegion1Waypoints(routeManager, route);
            character.areaIndex = 1; // 지역 변경
        }
        
        if (newWaypoints != null && newWaypoints.Length > 0)
        {
            SetWaypoints(newWaypoints, 0);
            StartMoving();
        }
    }
    
    /// <summary>
    /// 현재 위치에서 라우트 타입 결정
    /// </summary>
    private RouteType DetermineRouteFromPosition()
    {
        if (transform.position.x < -2f)
            return RouteType.Left;
        else if (transform.position.x > 2f)
            return RouteType.Right;
        else
            return RouteType.Center;
    }
    
    /// <summary>
    /// 지역1 웨이포인트 가져오기
    /// </summary>
    private Transform[] GetRegion1Waypoints(RouteManager routeManager, RouteType route)
    {
        GameObject[] waypoints = routeManager.GetWaypointsForRegion1(route);
        return ConvertToTransforms(waypoints);
    }
    
    /// <summary>
    /// 지역2 웨이포인트 가져오기
    /// </summary>
    private Transform[] GetRegion2Waypoints(RouteManager routeManager, RouteType route)
    {
        GameObject[] waypoints = routeManager.GetWaypointsForRegion2(route);
        return ConvertToTransforms(waypoints);
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
    /// 캐릭터 제거
    /// </summary>
    private void DestroyCharacter()
    {
        if (character.currentTile != null)
        {
            Tile tile = character.currentTile;
            if (PlacementManager.Instance != null && PlacementManager.Instance.gameObject.activeInHierarchy)
            {
                PlacementManager.Instance.ClearCharacterTileReference(character);
                PlacementManager.Instance.OnCharacterRemovedFromTile(tile);
            }
            else
            {
                character.currentTile = null;
            }
        }
        
        character.currentTarget = null;
        character.currentCharTarget = null;
        pathWaypoints = null;
        
        StopAllCoroutines();
        Destroy(character.gameObject);
    }
    
    /// <summary>
    /// 캐릭터 방향 스프라이트 업데이트
    /// </summary>
    private void UpdateCharacterDirectionSprite()
    {
        if (visual != null)
        {
            visual.UpdateCharacterDirectionSprite(this);
        }
    }
    
    // Public 접근자들
    public bool IsJumping() => isJumping;
    public bool IsJumpingAcross() => isJumpingAcross;
    public bool HasJumped() => hasJumped;
    public bool IsInCombat() => isInCombat;
    public Transform GetCurrentWaypoint() => (pathWaypoints != null && currentWaypointIndex >= 0 && currentWaypointIndex < pathWaypoints.Length) ? pathWaypoints[currentWaypointIndex] : null;
    
    public void SetJumpingAcross(bool value) => isJumpingAcross = value;
    public void SetHasJumped(bool value) => hasJumped = value;
    
    /// <summary>
    /// 새로운 지역으로 웨이포인트 변경
    /// </summary>
    public void SetWaypointsForNewRegion(Transform[] newWaypoints)
    {
        pathWaypoints = newWaypoints;
        currentWaypointIndex = 0;
        maxWaypointIndex = newWaypoints.Length - 1;
        
        Debug.Log($"[CharacterMovement] 새로운 웨이포인트로 변경 - 총 {newWaypoints.Length}개");
        
        // 이동 재시작
        if (isMoving)
        {
            StopMoving();
            StartMoving();
        }
    }
}