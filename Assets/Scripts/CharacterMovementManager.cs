using System.Collections;
using UnityEngine;

/// <summary>
/// 캐릭터 이동 관리 컴포넌트
/// 게임 기획서: 3라인 시스템 (왼쪽/중앙/오른쪽 웨이포인트)
/// </summary>
public class CharacterMovement : MonoBehaviour
{
    private Character character;
    private CharacterStats stats;
    private CharacterVisual visual;
    
    [Header("이동 설정")]
    public float moveSpeed = 3f;
    public float jumpSpeed = 5f;
    
    // 웨이포인트 관련
    [HideInInspector] public Transform[] pathWaypoints;
    [HideInInspector] public int currentWaypointIndex = -1;
    [HideInInspector] public int maxWaypointIndex = -1;
    
    // 이동 상태
    public bool isMoving { get; private set; }
    private bool isJumpingAcross = false;
    private bool hasJumped = false;
    private bool isInCombat = false;
    
    // 경로 및 지역 정보
    private int currentRegion = 1;
    private Transform castleTarget;
    
    // 코루틴 참조
    private Coroutine moveCoroutine;
    
    /// <summary>
    /// 이동 시스템 초기화
    /// </summary>
    public void Initialize(Character character, CharacterStats stats, CharacterVisual visual)
    {
        this.character = character;
        this.stats = stats;
        this.visual = visual;
        
        // 초기 지역 설정
        currentRegion = character.areaIndex;
        
        Debug.Log($"[CharacterMovement] {character.characterName} 이동 시스템 초기화");
    }
    
    /// <summary>
    /// 웨이포인트 설정
    /// </summary>
    public void SetWaypoints(Transform[] waypoints, int startIndex = 0)
    {
        pathWaypoints = waypoints;
        currentWaypointIndex = startIndex;
        maxWaypointIndex = waypoints.Length - 1;
        
        Debug.Log($"[CharacterMovement] 웨이포인트 설정 완료 - 총 {waypoints.Length}개");
    }
    
    /// <summary>
    /// 이동 시작
    /// </summary>
    public void StartMoving()
    {
        if (pathWaypoints == null || pathWaypoints.Length == 0)
        {
            Debug.LogWarning($"[CharacterMovement] {character.characterName} 웨이포인트가 설정되지 않음");
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
    /// 경로를 따라 이동하는 코루틴
    /// </summary>
    private IEnumerator MoveAlongPath()
    {
        while (isMoving && pathWaypoints != null && currentWaypointIndex <= maxWaypointIndex)
        {
            // 전투 중이 아닐 때만 이동
            if (!isInCombat)
            {
                Transform targetWaypoint = pathWaypoints[currentWaypointIndex];
                
                if (targetWaypoint != null)
                {
                    // 목표 지점으로 이동
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
            Debug.Log($"[CharacterMovement] {character.characterName} 경로 끝 도달");
            OnReachedPathEnd();
        }
    }
    
    /// <summary>
    /// 경로 끝에 도달했을 때
    /// </summary>
    private void OnReachedPathEnd()
    {
        // 성 공격 시도
        if (currentRegion == 1)
        {
            // Region1 캐릭터는 Region2 성 공격
            FindAndAttackCastle(2);
        }
        else if (currentRegion == 2)
        {
            // Region2 캐릭터는 Region1 성 공격
            FindAndAttackCastle(1);
        }
    }
    
    /// <summary>
    /// 성 찾아서 공격
    /// </summary>
    private void FindAndAttackCastle(int targetRegion)
    {
        // GameManager를 통해 성 공격
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            if (targetRegion == 2)
            {
                gameManager.TakeDamageToRegion2(character.attackPower);
                Debug.Log($"[CharacterMovement] {character.characterName}이(가) Region2 성 공격!");
            }
            else
            {
                // Region1 성 공격 메서드 추가 필요
                Debug.Log($"[CharacterMovement] {character.characterName}이(가) Region1 성 공격!");
            }
            
            // 공격 후 캐릭터 제거
            DestroyCharacter();
        }
    }
    
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
    public bool IsJumping() => isJumpingAcross;
    public bool IsJumpingAcross() => isJumpingAcross;
    public bool HasJumped() => hasJumped;
    public bool IsInCombat() => isInCombat;
    public Transform GetCurrentWaypoint() => (pathWaypoints != null && currentWaypointIndex >= 0 && currentWaypointIndex < pathWaypoints.Length) ? pathWaypoints[currentWaypointIndex] : null;
    
    public void SetJumpingAcross(bool value) => isJumpingAcross = value;
    public void SetHasJumped(bool value) => hasJumped = value;
}