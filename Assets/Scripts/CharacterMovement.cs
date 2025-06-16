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
    private Transform[] pathWaypoints;
    private int currentWaypointIndex = -1;
    private int maxWaypointIndex = -1;
    
    [Header("점프 설정")]
    public float jumpHeight = 2f;
    public float jumpDuration = 1f;
    private bool isJumping = false;
    private bool isJumpingAcross = false;
    private bool hasJumped = false;
    
    [Header("전투 중 이동")]
    private bool isInCombat = false;
    private Vector3 combatStartPosition;
    
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
        while (pathWaypoints != null && currentWaypointIndex >= 0 && currentWaypointIndex < pathWaypoints.Length)
        {
            Transform currentWaypoint = pathWaypoints[currentWaypointIndex];
            
            if (currentWaypoint == null)
            {
                Debug.LogWarning($"[CharacterMovement] {character.characterName}: 웨이포인트 {currentWaypointIndex}가 null입니다!");
                currentWaypointIndex++;
                continue;
            }
            
            // 타겟이 있으면 전투 모드
            if (character.currentTarget != null || character.currentCharTarget != null)
            {
                isInCombat = true;
                combatStartPosition = transform.position;
                
                // 전투 중에도 웨이포인트 방향으로 이동
                yield return MoveTowardsTarget(currentWaypoint.position);
            }
            else
            {
                isInCombat = false;
                
                // 웨이포인트로 이동
                yield return MoveTowardsTarget(currentWaypoint.position);
                
                // 웨이포인트 도달
                if (Vector3.Distance(transform.position, currentWaypoint.position) <= stoppingDistance)
                {
                    OnReachWaypoint();
                    
                    // 다음 웨이포인트로
                    currentWaypointIndex++;
                    character.currentWaypointIndex = currentWaypointIndex;
                    
                    // 마지막 웨이포인트 도달 시 처리
                    if (currentWaypointIndex > maxWaypointIndex)
                    {
                        OnReachFinalWaypoint();
                        yield break;
                    }
                }
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 목표 위치로 이동
    /// </summary>
    private IEnumerator MoveTowardsTarget(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > stoppingDistance)
        {
            // 이동 방향 계산
            Vector3 direction = (targetPosition - transform.position).normalized;
            
            // 위치 이동
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // 회전 (선택적)
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            // 스프라이트 방향 업데이트
            UpdateCharacterDirectionSprite();
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 웨이포인트 도달 시 처리
    /// </summary>
    private void OnReachWaypoint()
    {
        Debug.Log($"[CharacterMovement] {character.characterName}이(가) 웨이포인트 {currentWaypointIndex}에 도달!");
        
        // 점프 지점 확인
        if (ShouldJumpAtCurrentWaypoint())
        {
            StartJump();
        }
    }
    
    /// <summary>
    /// 최종 웨이포인트 도달 시 처리
    /// </summary>
    private void OnReachFinalWaypoint()
    {
        Debug.Log($"[CharacterMovement] {character.characterName}이(가) 최종 웨이포인트에 도달!");
        
        // 상대 지역으로 점프했으면 성에 데미지
        if (hasJumped)
        {
            if (character.areaIndex == 1)
            {
                GameManager gameManager = FindFirstObjectByType<GameManager>();
                if (gameManager != null)
                {
                    gameManager.TakeDamageToRegion2(1);
                    Debug.Log($"[CharacterMovement] 지역1 캐릭터 {character.characterName}이(가) 지역2 성에 1 데미지를 입혔습니다!");
                }
                
                Debug.Log($"[CharacterMovement] 1지역 캐릭터 {character.characterName}이(가) 지역2 웨이포인트 끝에 도달하여 삭제됩니다.");
                DestroyCharacter();
                return;
            }
            else if (character.areaIndex == 2)
            {
                GameManager gameManager = FindFirstObjectByType<GameManager>();
                if (gameManager != null)
                {
                    gameManager.TakeDamageToRegion1(1);
                    Debug.Log($"[CharacterMovement] 지역2 캐릭터 {character.characterName}이(가) 지역1 성에 1 데미지를 입혔습니다!");
                }
                
                Debug.Log($"[CharacterMovement] 2지역 캐릭터 {character.characterName}이(가) 지역1 웨이포인트 끝에 도달하여 삭제됩니다.");
                DestroyCharacter();
                return;
            }
        }
        else
        {
            Debug.Log($"[CharacterMovement] {character.characterName}이(가) 자기 지역 웨이포인트 끝에 도달했지만 점프하지 않았으므로 삭제하지 않습니다.");
            return;
        }
    }

    /// <summary>
    /// 현재 웨이포인트에서 점프해야 하는지 확인
    /// </summary>
    private bool ShouldJumpAtCurrentWaypoint()
    {
        // CharacterJumpController의 점프 지점과 비교
        CharacterJumpController jumpController = FindFirstObjectByType<CharacterJumpController>();
        if (jumpController == null) return false;
        
        Transform currentWaypoint = pathWaypoints[currentWaypointIndex];
        
        // 지역1 점프 지점 확인
        if (character.areaIndex == 1)
        {
            if (IsNearPosition(currentWaypoint.position, jumpController.region1LeftJumpStart) ||
                IsNearPosition(currentWaypoint.position, jumpController.region1CenterJumpStart) ||
                IsNearPosition(currentWaypoint.position, jumpController.region1RightJumpStart))
            {
                return true;
            }
        }
        // 지역2 점프 지점 확인
        else if (character.areaIndex == 2)
        {
            if (IsNearPosition(currentWaypoint.position, jumpController.region2LeftJumpStart) ||
                IsNearPosition(currentWaypoint.position, jumpController.region2CenterJumpStart) ||
                IsNearPosition(currentWaypoint.position, jumpController.region2RightJumpStart))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 두 위치가 가까운지 확인
    /// </summary>
    private bool IsNearPosition(Vector3 pos1, Transform pos2Transform)
    {
        if (pos2Transform == null) return false;
        return Vector3.Distance(pos1, pos2Transform.position) < 0.5f;
    }
    
    /// <summary>
    /// 점프 시작
    /// </summary>
    private void StartJump()
    {
        if (isJumping) return;
        
        CharacterJumpController jumpController = FindFirstObjectByType<CharacterJumpController>();
        if (jumpController == null)
        {
            Debug.LogWarning($"[CharacterMovement] CharacterJumpController를 찾을 수 없습니다!");
            return;
        }
        
        Transform jumpStart = null;
        Transform jumpEnd = null;
        
        // 현재 위치에서 가장 가까운 점프 지점 찾기
        GetNearestJumpPoints(jumpController, out jumpStart, out jumpEnd);
        
        if (jumpStart != null && jumpEnd != null)
        {
            if (jumpCoroutine != null)
            {
                StopCoroutine(jumpCoroutine);
            }
            
            jumpCoroutine = StartCoroutine(JumpToPosition(jumpStart.position, jumpEnd.position));
        }
    }
    
    /// <summary>
    /// 가장 가까운 점프 지점 찾기
    /// </summary>
    private void GetNearestJumpPoints(CharacterJumpController jumpController, out Transform jumpStart, out Transform jumpEnd)
    {
        jumpStart = null;
        jumpEnd = null;
        
        float minDistance = float.MaxValue;
        
        if (character.areaIndex == 1)
        {
            // 지역1 → 지역2 점프
            CheckJumpPoint(jumpController.region1LeftJumpStart, jumpController.region1LeftJumpEnd, ref jumpStart, ref jumpEnd, ref minDistance);
            CheckJumpPoint(jumpController.region1CenterJumpStart, jumpController.region1CenterJumpEnd, ref jumpStart, ref jumpEnd, ref minDistance);
            CheckJumpPoint(jumpController.region1RightJumpStart, jumpController.region1RightJumpEnd, ref jumpStart, ref jumpEnd, ref minDistance);
        }
        else if (character.areaIndex == 2)
        {
            // 지역2 → 지역1 점프
            CheckJumpPoint(jumpController.region2LeftJumpStart, jumpController.region2LeftJumpEnd, ref jumpStart, ref jumpEnd, ref minDistance);
            CheckJumpPoint(jumpController.region2CenterJumpStart, jumpController.region2CenterJumpEnd, ref jumpStart, ref jumpEnd, ref minDistance);
            CheckJumpPoint(jumpController.region2RightJumpStart, jumpController.region2RightJumpEnd, ref jumpStart, ref jumpEnd, ref minDistance);
        }
    }
    
    /// <summary>
    /// 점프 지점 확인
    /// </summary>
    private void CheckJumpPoint(Transform start, Transform end, ref Transform jumpStart, ref Transform jumpEnd, ref float minDistance)
    {
        if (start == null || end == null) return;
        
        float distance = Vector3.Distance(transform.position, start.position);
        if (distance < minDistance)
        {
            minDistance = distance;
            jumpStart = start;
            jumpEnd = end;
        }
    }
    
    /// <summary>
    /// 포물선 점프 코루틴
    /// </summary>
    private IEnumerator JumpToPosition(Vector3 startPos, Vector3 endPos)
    {
        isJumping = true;
        isJumpingAcross = true;
        hasJumped = true;
        
        float elapsed = 0f;
        Vector3 initialPos = transform.position;
        
        Debug.Log($"[CharacterMovement] {character.characterName}이(가) 점프 시작! {startPos} → {endPos}");
        
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;
            
            // 수평 이동 (선형 보간)
            Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, t);
            
            // 수직 이동 (포물선)
            float height = jumpHeight * 4f * t * (1f - t);
            
            // 최종 위치
            transform.position = new Vector3(horizontalPos.x, horizontalPos.y + height, horizontalPos.z);
            
            yield return null;
        }
        
        // 점프 완료
        transform.position = endPos;
        isJumping = false;
        
        // 상대 지역 웨이포인트로 변경
        UpdateWaypointsAfterJump();
        
        Debug.Log($"[CharacterMovement] {character.characterName}이(가) 점프 완료!");
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
    public bool IsJumpingAcross() => isJumpingAcross;
    public bool HasJumped() => hasJumped;
    public bool IsInCombat() => isInCombat;
    public Transform GetCurrentWaypoint() => (pathWaypoints != null && currentWaypointIndex >= 0 && currentWaypointIndex < pathWaypoints.Length) ? pathWaypoints[currentWaypointIndex] : null;
    
    public void SetJumpingAcross(bool value) => isJumpingAcross = value;
    public void SetHasJumped(bool value) => hasJumped = value;
}