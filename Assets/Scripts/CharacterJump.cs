using System.Collections;
using UnityEngine;

public class CharacterJump : MonoBehaviour
{
    private Character character;
    private CharacterMovement movement;
    private CharacterJumpController jumpController;
    
    // 웨이브 스포너 참조
    private WaveSpawner waveSpawner;
    private WaveSpawnerRegion2 waveSpawnerRegion;
    
    public void Initialize(Character character, CharacterMovement movement)
    {
        this.character = character;
        this.movement = movement;
        
        // JumpController 찾기
        jumpController = GetComponent<CharacterJumpController>();
        
        // WaveSpawner 찾기
        waveSpawner = FindFirstObjectByType<WaveSpawner>();
        waveSpawnerRegion = FindFirstObjectByType<WaveSpawnerRegion2>();
    }
    
    public bool CheckIfAtJumpPoint(RouteType selectedRoute, bool hasJumped)
    {
        // 히어로는 점프 불가
        if (character.isHero)
        {
            return false;
        }
        
        // 이미 점프를 완료했으면 더 이상 체크하지 않음
        if (hasJumped)
        {
            return false;
        }
        
        // CharacterJumpController가 없으면 점프 불가
        if (jumpController == null)
        {
            return false;
        }
        
        // CharacterJumpController의 점프 지점 확인
        RouteType jumpRoute = selectedRoute;
        
        Debug.Log($"[Character] {character.characterName} 점프 체크: 지역{character.areaIndex}, 선택된 루트: {selectedRoute} → JumpController 루트: {jumpRoute}");
        
        // 현재 지역의 점프 시작 지점과 도착 지점 모두 확인
        Transform jumpStartPoint = jumpController.GetJumpStartPoint(character.areaIndex, jumpRoute);
        Transform jumpEndPoint = jumpController.GetJumpEndPoint(character.areaIndex == 1 ? 2 : 1, jumpRoute);
        
        // 점프 지점이 제대로 설정되지 않았으면 기본 점프 조건 사용
        if (jumpStartPoint == null || jumpEndPoint == null)
        {
            Debug.LogWarning($"[Character] {character.characterName} - 점프 지점 미설정! jumpStartPoint={jumpStartPoint}, jumpEndPoint={jumpEndPoint}");
            
            // 기본 점프 조건: 현재 웨이포인트 인덱스가 일정 비율 이상
            if (movement.pathWaypoints != null && movement.pathWaypoints.Length > 0)
            {
                float progress = (float)movement.currentWaypointIndex / movement.pathWaypoints.Length;
                return progress >= 0.8f; // 80% 이상 진행했으면 점프
            }
        }
        
        // 점프 지점이 있으면 현재 위치와 비교
        if (jumpStartPoint != null)
        {
            float distance = Vector3.Distance(transform.position, jumpStartPoint.position);
            if (distance < 1.0f) // 점프 지점 근처에 있으면
            {
                Debug.Log($"[Character] {character.characterName} 점프 지점 도달!");
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 점프 시작
    /// </summary>
    public void StartJumpToRegion(int targetRegion)
    {
        if (jumpController == null)
        {
            Debug.LogWarning($"[CharacterJump] CharacterJumpController가 없습니다!");
            return;
        }
        
        // 현재 라우트 확인
        RouteType currentRoute = DetermineCurrentRoute();
        
        // 점프 목표 지점 가져오기
        Transform jumpEnd = jumpController.GetJumpEndPoint(targetRegion, currentRoute);
        
        if (jumpEnd == null)
        {
            Debug.LogWarning($"[CharacterJump] 점프 목표 지점을 찾을 수 없습니다! 지역: {targetRegion}, 루트: {currentRoute}");
            return;
        }
        
        StartCoroutine(JumpCoroutine(jumpEnd.position));
    }
    
    /// <summary>
    /// 현재 라우트 결정
    /// </summary>
    private RouteType DetermineCurrentRoute()
    {
        // 캐릭터의 selectedRoute 사용
        if (character.selectedRoute >= 0)
        {
            return (RouteType)character.selectedRoute;
        }
        
        // 위치 기반으로 결정
        if (transform.position.x < -2f)
            return RouteType.Left;
        else if (transform.position.x > 2f)
            return RouteType.Right;
        else
            return RouteType.Center;
    }
    
    /// <summary>
    /// 점프 코루틴
    /// </summary>
    private IEnumerator JumpCoroutine(Vector3 targetPosition)
    {
        Debug.Log($"[CharacterJump] {character.characterName} 점프 시작!");
        
        movement.SetJumpingAcross(true);
        
        Vector3 startPos = transform.position;
        float jumpDuration = 2f;
        float jumpHeight = 3f;
        float elapsed = 0f;
        
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;
            
            // 수평 이동
            Vector3 horizontalPos = Vector3.Lerp(startPos, targetPosition, t);
            
            // 수직 이동 (포물선)
            float height = jumpHeight * 4f * t * (1f - t);
            
            transform.position = new Vector3(horizontalPos.x, horizontalPos.y + height, horizontalPos.z);
            
            yield return null;
        }
        
        transform.position = targetPosition;
        movement.SetJumpingAcross(false);
        movement.SetHasJumped(true);
        
        // 지역 변경
        character.areaIndex = (character.areaIndex == 1) ? 2 : 1;
        
        // 새로운 웨이포인트 설정
        UpdateWaypointsAfterJump();
        
        Debug.Log($"[CharacterJump] {character.characterName} 점프 완료! 새 지역: {character.areaIndex}");
    }
    
    /// <summary>
    /// 점프 후 웨이포인트 업데이트
    /// </summary>
    private void UpdateWaypointsAfterJump()
    {
        WaypointManager waypointManager = WaypointManager.Instance;
        if (waypointManager == null)
        {
            Debug.LogWarning("[CharacterJump] WaypointManager를 찾을 수 없습니다!");
            return;
        }
        
        RouteType route = DetermineCurrentRoute();
        Transform[] newWaypoints = waypointManager.GetWaypointsForRoute(character.areaIndex, route);
        
        if (newWaypoints != null && newWaypoints.Length > 0)
        {
            movement.SetWaypoints(newWaypoints, 0);
            movement.StartMoving();
        }
    }
}