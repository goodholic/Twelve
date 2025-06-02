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
        RouteType jumpRoute = ConvertRouteType(selectedRoute);
        
        Debug.Log($"[Character] {character.characterName} 점프 체크: 지역{character.areaIndex}, 선택된 루트: {selectedRoute} → JumpController 루트: {jumpRoute}");
        
        // 현재 지역의 점프 시작 지점과 도착 지점 모두 확인
        RectTransform jumpStartPoint = jumpController.GetJumpStartPoint(character.areaIndex, jumpRoute);
        RectTransform jumpEndPoint = jumpController.GetJumpEndPoint(character.areaIndex == 1 ? 2 : 1, jumpRoute);
        
        // 점프 지점이 제대로 설정되지 않았으면 기본 점프 조건 사용
        if (jumpStartPoint == null || jumpEndPoint == null)
        {
            Debug.LogWarning($"[Character] {character.characterName} - 점프 지점 미설정! 지역{character.areaIndex}, {jumpRoute} 루트");
            
            // 웨이포인트 진행률로 점프 허용
            if (movement.pathWaypoints != null && movement.pathWaypoints.Length > 0)
            {
                float progressRatio = (float)movement.currentWaypointIndex / (float)movement.pathWaypoints.Length;
                // 진행률 70% 이상일 때 점프 (기획서: 캐릭터가 상대 진영으로 진격)
                if (progressRatio >= 0.7f)
                {
                    Debug.Log($"[Character] {character.characterName} - 점프 지점 미설정이지만 진행률 {progressRatio:F2}로 점프 허용");
                    return true;
                }
            }
            return false;
        }
        
        // UI 좌표계에서 거리 계산
        RectTransform charRect = GetComponent<RectTransform>();
        if (charRect == null)
        {
            return false;
        }
        
        Vector2 currentUIPos = charRect.anchoredPosition;
        Vector2 jumpUIPos = jumpStartPoint.anchoredPosition;
        float distToJumpPoint = Vector2.Distance(currentUIPos, jumpUIPos);
        
        // 점프 거리를 더 멀리 설정 (3라인 시스템에 맞게)
        if (distToJumpPoint < 800f)
        {
            Debug.Log($"[Character] {character.characterName}이(가) 지역{character.areaIndex} 점프 포인트에 도달!");
            return true;
        }
        
        return false;
    }
    
    public void StartJumpToOtherRegion(System.Action<int> onComplete)
    {
        if (movement.IsJumpingAcross()) return;
        
        // CharacterJumpController 사용
        if (jumpController != null)
        {
            RouteType jumpRoute = ConvertRouteType(character.selectedRoute);
            
            // 점프 지점이 제대로 설정되어 있는지 미리 확인
            int targetRegion = (character.areaIndex == 1) ? 2 : 1;
            RectTransform startPoint = jumpController.GetJumpStartPoint(character.areaIndex, jumpRoute);
            RectTransform endPoint = jumpController.GetJumpEndPoint(character.areaIndex, jumpRoute);
            
            Debug.Log($"[Character] {character.characterName} 점프 지점 확인: 지역{character.areaIndex}→{targetRegion}, {jumpRoute} 루트");
            
            if (startPoint == null || endPoint == null)
            {
                // 점프 지점이 설정되지 않았어도 기본 점프 실행
                Debug.LogWarning($"[Character] {character.characterName} - 점프 지점이 설정되지 않았지만 기본 점프를 실행합니다.");
                
                // 웨이포인트 정보 백업
                Transform[] backupWaypoints = movement.pathWaypoints;
                int backupCurrentIndex = movement.currentWaypointIndex;
                int backupMaxIndex = movement.maxWaypointIndex;
                
                movement.SetHasJumped(true);
                
                // 패널 변경
                UpdatePanelForRegion(targetRegion);
                
                // 웨이포인트 정보 복원
                movement.pathWaypoints = backupWaypoints;
                movement.currentWaypointIndex = backupCurrentIndex;
                movement.maxWaypointIndex = backupMaxIndex;
                
                movement.SetWaypointsForNewRegion(targetRegion);
                
                onComplete?.Invoke(targetRegion);
                return;
            }
            
            movement.SetJumpingAcross(true);
            
            // 지역1 → 지역2로 점프
            if (character.areaIndex == 1)
            {
                jumpController.onJumpComplete = () => onComplete?.Invoke(2);
                jumpController.JumpBetweenRegions(1, 2, jumpRoute);
                Debug.Log($"[Character] {character.characterName} CharacterJumpController로 지역1→지역2 점프 시작 (루트: {jumpRoute})");
            }
            // 지역2 → 지역1로 점프
            else if (character.areaIndex == 2)
            {
                jumpController.onJumpComplete = () => onComplete?.Invoke(1);
                jumpController.JumpBetweenRegions(2, 1, jumpRoute);
                Debug.Log($"[Character] {character.characterName} CharacterJumpController로 지역2→지역1 점프 시작 (루트: {jumpRoute})");
            }
        }
        else
        {
            // CharacterJumpController가 없으면 점프 건너뛰기
            Debug.LogWarning($"[Character] {character.characterName}에 CharacterJumpController가 없어 점프를 건너뜁니다.");
            movement.SetJumpingAcross(false);
        }
    }
    
    private RouteType ConvertRouteType(RouteType route)
    {
        // 이제 동일한 타입이므로 변환 없이 그대로 반환
        return route;
    }
    
    private void UpdatePanelForRegion(int newAreaIndex)
    {
        RectTransform charRect = GetComponent<RectTransform>();
        if (charRect != null)
        {
            PlacementManager pm = PlacementManager.Instance;
            if (pm == null)
            {
                Debug.LogError($"[Character] {character.characterName} PlacementManager.Instance가 null!");
                return;
            }
            
            if (newAreaIndex == 1)
            {
                // 지역1로 이동 시 ourMonsterPanel로 변경
                if (pm.ourMonsterPanel != null)
                {
                    Debug.Log($"[Character] {character.characterName} 지역1 패널로 이동 (원래 지역: {character.areaIndex})");
                    charRect.SetParent(pm.ourMonsterPanel, false);
                    
                    Vector3 worldPos = transform.position;
                    Vector2 localPos = pm.ourMonsterPanel.InverseTransformPoint(worldPos);
                    charRect.anchoredPosition = localPos;
                    charRect.localRotation = Quaternion.identity;
                    
                    Debug.Log($"[Character] {character.characterName} 지역1 패널 이동 완료 - 월드 위치: {worldPos}, 로컬 위치: {localPos}");
                }
                else
                {
                    Debug.LogError($"[Character] {character.characterName} ourMonsterPanel이 null!");
                }
            }
            else if (newAreaIndex == 2)
            {
                // 지역2로 이동 시 opponentOurMonsterPanel로 변경
                if (pm.opponentOurMonsterPanel != null)
                {
                    Debug.Log($"[Character] {character.characterName} 지역2 패널로 이동 (원래 지역: {character.areaIndex})");
                    charRect.SetParent(pm.opponentOurMonsterPanel, false);
                    
                    Vector3 worldPos = transform.position;
                    Vector2 localPos = pm.opponentOurMonsterPanel.InverseTransformPoint(worldPos);
                    charRect.anchoredPosition = localPos;
                    charRect.localRotation = Quaternion.identity;
                    
                    Debug.Log($"[Character] {character.characterName} 지역2 패널 이동 완료 - 월드 위치: {worldPos}, 로컬 위치: {localPos}");
                }
                else
                {
                    Debug.LogError($"[Character] {character.characterName} opponentOurMonsterPanel이 null!");
                }
            }
        }
        else
        {
            Debug.LogError($"[Character] {character.characterName} RectTransform이 null!");
        }
    }
}