using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    private Character character;
    private CharacterVisual visual;
    private CharacterJump jumpSystem;
    
    // WaveSpawner에서 관리하는 이동 정보
    [HideInInspector] public Transform[] pathWaypoints;
    [HideInInspector] public int currentWaypointIndex = -1;
    [HideInInspector] public int maxWaypointIndex = 6;
    
    // 웨이브 스포너 참조
    private WaveSpawner waveSpawner;
    private WaveSpawnerRegion2 waveSpawnerRegion;
    
    // 점프 상태
    private bool isJumpingAcross = false;
    private bool hasJumped = false;
    
    // 중간성/최종성 타겟팅
    private enum CastleTarget
    {
        None,
        MiddleLeft,
        MiddleRight,
        Final
    }
    
    private CastleTarget currentCastleTarget = CastleTarget.None;
    private Transform middleLeftCastle;
    private Transform middleRightCastle;
    private Transform finalCastle;
    private int middleLeftHealth = 500;
    private int middleRightHealth = 500;
    
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    
    public void Initialize(Character character, CharacterVisual visual, CharacterJump jumpSystem)
    {
        this.character = character;
        this.visual = visual;
        this.jumpSystem = jumpSystem;
        
        // WaveSpawner 찾기
        waveSpawner = FindFirstObjectByType<WaveSpawner>();
        waveSpawnerRegion = FindFirstObjectByType<WaveSpawnerRegion2>();
        
        // 중간성/최종성 오브젝트 찾기
        GameObject[] castles = GameObject.FindGameObjectsWithTag("Castle");
        foreach (var castle in castles)
        {
            if (castle.name.Contains("MiddleLeft"))
                middleLeftCastle = castle.transform;
            else if (castle.name.Contains("MiddleRight"))
                middleRightCastle = castle.transform;
            else if (castle.name.Contains("Final"))
                finalCastle = castle.transform;
        }
        
        // 점프 상태 초기화
        hasJumped = false;
        isJumpingAcross = false;
    }
    
    public void HandleMovement()
    {
        // 히어로는 pathWaypoints 이동 안 함
        if (character.isHero)
        {
            UpdateCharacterDirectionSprite();
            return;
        }
        
        // placable/placed 타일의 캐릭터는 이동하지 않음
        if (character.currentTile != null && 
            (character.currentTile.IsPlacable() || character.currentTile.IsPlacable2() || 
             character.currentTile.IsPlaceTile() || character.currentTile.IsPlaced2()))
        {
            UpdateCharacterDirectionSprite();
            return;
        }
        
        // 전투 중이면 이동하지 않음 (사정거리 내 적이 있을 때)
        if (IsInCombatRange())
        {
            UpdateCharacterDirectionSprite();
            return;
        }
        
        // 성 공격 모드
        if (currentCastleTarget != CastleTarget.None)
        {
            MoveToCastle();
            return;
        }
        
        // walkable 타일에 배치된 캐릭터는 currentWaypointIndex가 0 이상이어야 함
        if (pathWaypoints != null && pathWaypoints.Length > 0 && currentWaypointIndex >= 0)
        {
            MoveAlongWaypoints();
        }
        else if (currentWaypointIndex == -1 && pathWaypoints != null && pathWaypoints.Length > 0)
        {
            // walkable 타일에 처음 배치된 경우 이동 시작
            currentWaypointIndex = 0;
            Debug.Log($"[Character] {character.characterName} walkable 타일에서 이동 시작 - 웨이포인트 개수: {pathWaypoints.Length}, 루트: {character.selectedRoute}");
        }
        
        UpdateCharacterDirectionSprite();
    }
    
    private bool IsInCombatRange()
    {
        // 사정거리 내에 적이 있는지 확인
        if (character.currentTarget != null)
        {
            float distToTarget = Vector2.Distance(transform.position, character.currentTarget.transform.position);
            if (distToTarget <= character.attackRange)
            {
                return true;
            }
        }
        
        if (character.currentCharTarget != null)
        {
            float distToTarget = Vector2.Distance(transform.position, character.currentCharTarget.transform.position);
            if (distToTarget <= character.attackRange)
            {
                return true;
            }
        }
        
        return false;
    }
    
    private void MoveToCastle()
    {
        Transform targetCastle = null;
        
        switch (currentCastleTarget)
        {
            case CastleTarget.MiddleLeft:
                targetCastle = middleLeftCastle;
                break;
            case CastleTarget.MiddleRight:
                targetCastle = middleRightCastle;
                break;
            case CastleTarget.Final:
                targetCastle = finalCastle;
                break;
        }
        
        if (targetCastle == null)
        {
            Debug.LogWarning($"[Character] {character.characterName} 성 타겟을 찾을 수 없음");
            return;
        }
        
        Vector2 currentPos = transform.position;
        Vector2 targetPos = targetCastle.position;
        Vector2 dir = (targetPos - currentPos).normalized;
        
        float baseSpeed = 0.3f;
        float distThisFrame = baseSpeed * Time.deltaTime;
        
        transform.position += (Vector3)(dir * distThisFrame);
        
        // 성에 도달했는지 확인
        float distToCastle = Vector2.Distance(transform.position, targetPos);
        if (distToCastle <= 0.5f)
        {
            OnReachCastle();
        }
    }
    
    private void OnReachCastle()
    {
        switch (currentCastleTarget)
        {
            case CastleTarget.MiddleLeft:
                middleLeftHealth -= 10;
                Debug.Log($"[Character] {character.characterName}이(가) 왼쪽 중간성 공격! 남은 체력: {middleLeftHealth}");
                if (middleLeftHealth <= 0)
                {
                    Debug.Log("[Character] 왼쪽 중간성 파괴! 최종성으로 목표 변경");
                    currentCastleTarget = CastleTarget.Final;
                    if (middleLeftCastle != null)
                        middleLeftCastle.gameObject.SetActive(false);
                }
                break;
                
            case CastleTarget.MiddleRight:
                middleRightHealth -= 10;
                Debug.Log($"[Character] {character.characterName}이(가) 오른쪽 중간성 공격! 남은 체력: {middleRightHealth}");
                if (middleRightHealth <= 0)
                {
                    Debug.Log("[Character] 오른쪽 중간성 파괴! 최종성으로 목표 변경");
                    currentCastleTarget = CastleTarget.Final;
                    if (middleRightCastle != null)
                        middleRightCastle.gameObject.SetActive(false);
                }
                break;
                
            case CastleTarget.Final:
                if (character.areaIndex == 1)
                {
                    WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
                    if (spawner2 != null)
                    {
                        spawner2.TakeDamageToRegion2(1);
                        Debug.Log($"[Character] {character.characterName}이(가) 지역2 최종성에 1 데미지!");
                    }
                }
                else if (character.areaIndex == 2)
                {
                    GameManager gameManager = FindFirstObjectByType<GameManager>();
                    if (gameManager != null)
                    {
                        gameManager.TakeDamageToRegion1(1);
                        Debug.Log($"[Character] {character.characterName}이(가) 지역1 최종성에 1 데미지!");
                    }
                }
                break;
        }
        
        // 공격 후 캐릭터 제거
        DestroyCharacter();
    }
    
    private void MoveAlongWaypoints()
    {
        if (pathWaypoints == null || pathWaypoints.Length == 0)
        {
            // 웨이포인트가 없으면 중간성으로 이동
            DetermineCastleTarget();
            return;
        }
        
        // maxWaypointIndex를 실제 배열 길이에 맞게 조정
        if (maxWaypointIndex < 0 || maxWaypointIndex >= pathWaypoints.Length)
        {
            maxWaypointIndex = pathWaypoints.Length - 1;
        }
        
        if (currentWaypointIndex > maxWaypointIndex)
        {
            OnArriveCastle();
            return;
        }
        
        if (currentWaypointIndex < 0 || currentWaypointIndex >= pathWaypoints.Length)
        {
            Debug.LogWarning($"[Character] 잘못된 waypoint 인덱스: {currentWaypointIndex}, 배열 크기: {pathWaypoints.Length}");
            DetermineCastleTarget();
            return;
        }
        
        Transform target = pathWaypoints[currentWaypointIndex];
        if (target == null)
        {
            Debug.LogWarning($"[Character] {character.characterName}의 웨이포인트[{currentWaypointIndex}]가 null");
            DetermineCastleTarget();
            return;
        }
        
        // 점프 포인트 도달 체크
        if (!isJumpingAcross && !hasJumped && ShouldCheckForJump() && jumpSystem.CheckIfAtJumpPoint(character.selectedRoute, hasJumped))
        {
            jumpSystem.StartJumpToOtherRegion(OnJumpComplete);
            isJumpingAcross = true;
            return;
        }
        
        Vector2 currentPos = transform.position;
        Vector2 targetPos = target.position;
        
        // 순간이동 방지를 위한 거리 체크
        float distToTarget = Vector2.Distance(currentPos, targetPos);
        
        if (distToTarget > 8f)
        {
            Debug.LogWarning($"[Character] {character.characterName} 웨이포인트가 멀리 있음! 거리: {distToTarget:F2}, 천천히 이동합니다.");
            
            float slowMoveDistance = 0.15f * Time.deltaTime;
            Vector3 slowNewPosition = Vector2.MoveTowards(currentPos, targetPos, slowMoveDistance);
            transform.position = slowNewPosition;
            
            Debug.Log($"[Character] {character.characterName} 먼 웨이포인트로 천천히 이동 중... (속도: {slowMoveDistance:F3})");
            return;
        }
        
        Vector2 dir = (targetPos - currentPos).normalized;
        
        // 모든 지역과 루트에 대한 안정적인 이동 속도
        float baseSpeed = 0.3f;
        
        if (hasJumped)
        {
            baseSpeed *= 0.9f;
            Debug.Log($"[Character] {character.characterName} 점프한 캐릭터 - 안정적 이동 속도: {baseSpeed:F3}");
        }
        
        float distThisFrame = baseSpeed * Time.deltaTime;
        
        // 안전한 이동 처리
        Vector3 newPosition = transform.position + (Vector3)(dir * distThisFrame);
        
        float moveDistance = Vector2.Distance(transform.position, newPosition);
        if (moveDistance > 2f)
        {
            Debug.LogWarning($"[Character] {character.characterName} 한 프레임 이동 거리가 너무 큼: {moveDistance}, 제한함");
            newPosition = transform.position + (Vector3)(dir * 2f);
        }
        
        transform.position = newPosition;
        
        // 웨이포인트 도달 판정
        float currentDist = Vector2.Distance(transform.position, targetPos);
        float arrivalThreshold = hasJumped ? 0.2f : 0.25f;
        
        if (currentDist <= arrivalThreshold)
        {
            Debug.Log($"[Character] {character.characterName} 웨이포인트 {currentWaypointIndex} 도달! (거리: {currentDist:F3}, 임계값: {arrivalThreshold}, 지역: {character.areaIndex}, 점프: {hasJumped})");
            
            if (currentDist < 2f)
            {
                transform.position = targetPos;
                Debug.Log($"[Character] {character.characterName} 웨이포인트 위치로 정확히 이동: {targetPos}");
            }
            
            currentWaypointIndex++;
            if (currentWaypointIndex > maxWaypointIndex)
            {
                OnArriveCastle();
            }
            else if (currentWaypointIndex >= pathWaypoints.Length)
            {
                OnArriveCastle();
            }
        }
    }
    
    private void DetermineCastleTarget()
    {
        // 루트에 따라 중간성 결정
        if (character.selectedRoute == RouteType.Left)
        {
            currentCastleTarget = CastleTarget.MiddleLeft;
            Debug.Log($"[Character] {character.characterName} 왼쪽 중간성 목표 설정");
        }
        else if (character.selectedRoute == RouteType.Right)
        {
            currentCastleTarget = CastleTarget.MiddleRight;
            Debug.Log($"[Character] {character.characterName} 오른쪽 중간성 목표 설정");
        }
        else
        {
            // 중앙 루트는 기본적으로 최종성 목표
            currentCastleTarget = CastleTarget.Final;
            Debug.Log($"[Character] {character.characterName} 최종성 목표 설정");
        }
    }
    
    private bool ShouldCheckForJump()
    {
        if (pathWaypoints == null || pathWaypoints.Length == 0 || currentWaypointIndex < 0)
            return false;
        
        float progressRatio = (float)currentWaypointIndex / (float)pathWaypoints.Length;
        bool shouldCheck = progressRatio >= 0.3f;
        
        if (shouldCheck)
        {
            Debug.Log($"[Character] {character.characterName} - 점프 체크 조건 만족: 진행률 {progressRatio:F2} ({currentWaypointIndex}/{pathWaypoints.Length})");
        }
        
        return shouldCheck;
    }
    
    private void OnJumpComplete(int targetAreaIndex)
    {
        isJumpingAcross = false;
        hasJumped = true;
        
        Debug.Log($"[Character] {character.characterName} CharacterJumpController 점프 완료! 원래 지역: {character.areaIndex}, 이동한 지역: {targetAreaIndex}");
        
        if (pathWaypoints == null || pathWaypoints.Length == 0)
        {
            Debug.LogWarning($"[Character] {character.characterName} 점프 완료 후 웨이포인트 배열이 유실됨! 복구 시도");
            RestoreOriginalWaypoints();
        }
        
        SetWaypointsForNewRegion(targetAreaIndex);
    }
    
    public void SetWaypointsForNewRegion(int newAreaIndex)
    {
        Debug.Log($"[Character] {character.characterName} 점프 완료. 원래 지역 {character.areaIndex}의 웨이포인트를 계속 사용하여 지역 {newAreaIndex}를 탐방");
        
        if (pathWaypoints == null || pathWaypoints.Length == 0)
        {
            Debug.LogWarning($"[Character] {character.characterName} 웨이포인트 배열이 유실됨! 원래 지역의 웨이포인트 복구 시도");
            RestoreOriginalWaypoints();
        }
        
        if (pathWaypoints != null && pathWaypoints.Length > 0)
        {
            bool waypointsValid = true;
            for (int i = 0; i < pathWaypoints.Length; i++)
            {
                if (pathWaypoints[i] == null)
                {
                    Debug.LogError($"[Character] {character.characterName} 웨이포인트[{i}]가 null! 루트: {character.selectedRoute}");
                    waypointsValid = false;
                }
            }
            
            if (!waypointsValid)
            {
                Debug.LogError($"[Character] {character.characterName} 웨이포인트 배열에 null이 포함됨! 복구 시도");
                RestoreOriginalWaypoints();
            }
            
            if (currentWaypointIndex < 0)
            {
                currentWaypointIndex = 0;
                Debug.Log($"[Character] {character.characterName} 웨이포인트 인덱스가 음수였음. 0으로 초기화");
            }
            
            if (currentWaypointIndex < pathWaypoints.Length - 1)
            {
                currentWaypointIndex++;
                Debug.Log($"[Character] {character.characterName} 원래 지역 {character.areaIndex}의 웨이포인트 {currentWaypointIndex}부터 계속 진행 (총 {pathWaypoints.Length}개, 루트: {character.selectedRoute})");
            }
            else
            {
                Debug.Log($"[Character] {character.characterName} 이미 마지막 웨이포인트에 도달. 현재 인덱스 유지: {currentWaypointIndex}");
            }
            
            maxWaypointIndex = pathWaypoints.Length - 1;
        }
        else
        {
            Debug.LogError($"[Character] {character.characterName} 웨이포인트 복구 실패! 점프 후 이동 불가");
        }
    }
    
    private void RestoreOriginalWaypoints()
    {
        Debug.Log($"[Character] {character.characterName} 원래 지역 {character.areaIndex}의 웨이포인트 복구 시도 (루트: {character.selectedRoute})");
        
        if (character.areaIndex == 1)
        {
            if (waveSpawner != null)
            {
                Transform[] restoredWaypoints = GetWaypointsFromOriginalSpawner(waveSpawner, character.selectedRoute);
                if (restoredWaypoints != null && restoredWaypoints.Length > 0)
                {
                    bool allValid = true;
                    for (int i = 0; i < restoredWaypoints.Length; i++)
                    {
                        if (restoredWaypoints[i] == null)
                        {
                            Debug.LogError($"[Character] {character.characterName} 복구된 지역1 웨이포인트[{i}]가 null!");
                            allValid = false;
                        }
                    }
                    
                    if (allValid)
                    {
                        pathWaypoints = restoredWaypoints;
                        maxWaypointIndex = restoredWaypoints.Length - 1;
                        Debug.Log($"[Character] {character.characterName} 지역1 웨이포인트 복구 성공! ({restoredWaypoints.Length}개, 루트: {character.selectedRoute})");
                    }
                }
            }
        }
        else if (character.areaIndex == 2)
        {
            if (waveSpawnerRegion != null)
            {
                Transform[] restoredWaypoints = GetWaypointsFromOriginalSpawner(waveSpawnerRegion, character.selectedRoute);
                if (restoredWaypoints != null && restoredWaypoints.Length > 0)
                {
                    bool allValid = true;
                    for (int i = 0; i < restoredWaypoints.Length; i++)
                    {
                        if (restoredWaypoints[i] == null)
                        {
                            Debug.LogError($"[Character] {character.characterName} 복구된 지역2 웨이포인트[{i}]가 null!");
                            allValid = false;
                        }
                    }
                    
                    if (allValid)
                    {
                        pathWaypoints = restoredWaypoints;
                        maxWaypointIndex = restoredWaypoints.Length - 1;
                        Debug.Log($"[Character] {character.characterName} 지역2 웨이포인트 복구 성공! ({restoredWaypoints.Length}개, 루트: {character.selectedRoute})");
                    }
                }
            }
        }
    }
    
    private Transform[] GetWaypointsFromOriginalSpawner(WaveSpawner spawner, RouteType route)
    {
        switch (route)
        {
            case RouteType.Left:
                return spawner.walkableLeft;
            case RouteType.Center:
                return spawner.walkableCenter;
            case RouteType.Right:
                return spawner.walkableRight;
            case RouteType.Default:
            default:
                return spawner.walkableCenter;
        }
    }
    
    private Transform[] GetWaypointsFromOriginalSpawner(WaveSpawnerRegion2 spawner, RouteType route)
    {
        switch (route)
        {
            case RouteType.Left:
                return spawner.walkableLeft2;
            case RouteType.Center:
                return spawner.walkableCenter2;
            case RouteType.Right:
                return spawner.walkableRight2;
            case RouteType.Default:
            default:
                return spawner.walkableCenter2;
        }
    }
    
    private void OnArriveCastle()
    {
        if (character.isHero)
        {
            return;
        }
        
        if (isJumpingAcross) return;
        
        Debug.Log($"[Character] {character.characterName}이(가) 웨이포인트 끝에 도달했습니다! (원래 지역: {character.areaIndex}, 점프했음: {hasJumped})");
        
        if (hasJumped)
        {
            if (character.areaIndex == 1)
            {
                WaveSpawnerRegion2 spawner2 = FindFirstObjectByType<WaveSpawnerRegion2>();
                if (spawner2 != null)
                {
                    spawner2.TakeDamageToRegion2(1);
                    Debug.Log($"[Character] 지역1 캐릭터 {character.characterName}이(가) 지역2 성에 1 데미지를 입혔습니다!");
                }
                
                Debug.Log($"[Character] 1지역 캐릭터 {character.characterName}이(가) 지역2 웨이포인트 끝에 도달하여 삭제됩니다.");
                DestroyCharacter();
                return;
            }
            else if (character.areaIndex == 2)
            {
                GameManager gameManager = FindFirstObjectByType<GameManager>();
                if (gameManager != null)
                {
                    gameManager.TakeDamageToRegion1(1);
                    Debug.Log($"[Character] 지역2 캐릭터 {character.characterName}이(가) 지역1 성에 1 데미지를 입혔습니다!");
                }
                
                Debug.Log($"[Character] 2지역 캐릭터 {character.characterName}이(가) 지역1 웨이포인트 끝에 도달하여 삭제됩니다.");
                DestroyCharacter();
                return;
            }
        }
        else
        {
            Debug.Log($"[Character] {character.characterName}이(가) 자기 지역 웨이포인트 끝에 도달했지만 점프하지 않았으므로 삭제하지 않습니다.");
            return;
        }
    }
    
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
    
    private void UpdateCharacterDirectionSprite()
    {
        if (visual != null)
        {
            visual.UpdateCharacterDirectionSprite(this);
        }
    }
    
    public bool IsJumpingAcross() => isJumpingAcross;
    public bool HasJumped() => hasJumped;
    
    public void SetJumpingAcross(bool value) => isJumpingAcross = value;
    public void SetHasJumped(bool value) => hasJumped = value;
}