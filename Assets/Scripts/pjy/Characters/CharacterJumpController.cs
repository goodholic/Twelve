using System.Collections;
using System;
using UnityEngine;

/// <summary>
/// 월드 공간에서 캐릭터가 점프 애니메이션을 하며 이동하는 컨트롤러
/// RectTransform 대신 Transform을 사용하여 월드 좌표 기반으로 작동
/// </summary>
public class CharacterJumpController : MonoBehaviour
{
    [Header("점프 애니메이션 설정값")]
    [Tooltip("점프하는 동안의 최고 높이(arc). 숫자가 클수록 포물선이 크게 보임.")]
    public float jumpArcHeight = 3f; // 월드 좌표에 맞게 조정

    [Tooltip("점프 이동 전체 소요 시간(초)")]
    public float jumpDuration = 1.2f;

    [Header("지역 1 캐릭터 점프 지점 (3개 루트)")]
    [Tooltip("지역 1 좌측 루트 점프 시작 지점")]
    public Transform region1LeftJumpStart;
    [Tooltip("지역 1 좌측 루트 점프 도착 지점")]
    public Transform region1LeftJumpEnd;
    
    [Tooltip("지역 1 중앙 루트 점프 시작 지점")]
    public Transform region1CenterJumpStart;
    [Tooltip("지역 1 중앙 루트 점프 도착 지점")]
    public Transform region1CenterJumpEnd;
    
    [Tooltip("지역 1 우측 루트 점프 시작 지점")]
    public Transform region1RightJumpStart;
    [Tooltip("지역 1 우측 루트 점프 도착 지점")]
    public Transform region1RightJumpEnd;

    [Header("지역 2 캐릭터 점프 지점 (3개 루트)")]
    [Tooltip("지역 2 좌측 루트 점프 시작 지점")]
    public Transform region2LeftJumpStart;
    [Tooltip("지역 2 좌측 루트 점프 도착 지점")]
    public Transform region2LeftJumpEnd;
    
    [Tooltip("지역 2 중앙 루트 점프 시작 지점")]
    public Transform region2CenterJumpStart;
    [Tooltip("지역 2 중앙 루트 점프 도착 지점")]
    public Transform region2CenterJumpEnd;
    
    [Tooltip("지역 2 우측 루트 점프 시작 지점")]
    public Transform region2RightJumpStart;
    [Tooltip("지역 2 우측 루트 점프 도착 지점")]
    public Transform region2RightJumpEnd;

    [Header("테스트용 (자동점프)")]
    [Tooltip("시작 Tile의 Transform")]
    public Transform startTile;
    [Tooltip("도착 Tile의 Transform")]
    public Transform endTile;
    [Tooltip("씬 시작 시 즉시 점프를 테스트하려면 true")]
    public bool autoJumpOnStart = false;

    /// <summary>
    /// 점프가 진행 중인지 여부
    /// </summary>
    public bool isJumping = false;

    /// <summary>
    /// 점프 애니메이션 완료 시 호출될 콜백
    /// </summary>
    public Action onJumpComplete;

    private void Start()
    {
        // 테스트용 자동 실행
        if (autoJumpOnStart && startTile != null && endTile != null)
        {
            // 시작 위치로 이동 후 즉시 점프
            transform.position = startTile.position;
            JumpToTile(startTile, endTile);
        }
    }

    /// <summary>
    /// 타일 간 점프 실행
    /// </summary>
    public void JumpToTile(Transform fromTile, Transform toTile)
    {
        if (fromTile == null || toTile == null)
        {
            Debug.LogWarning("[CharacterJumpController] fromTile 또는 toTile이 null입니다. 점프 불가.");
            return;
        }

        // 시작점 - 도착점 월드 좌표
        Vector3 fromPos = fromTile.position;
        Vector3 toPos = toTile.position;

        // 캐릭터를 fromTile 위치로 이동
        transform.position = fromPos;

        // 점프 중 상태 세팅
        isJumping = true;

        // 점프 코루틴 실행
        StartCoroutine(JumpCoroutine(fromPos, toPos, jumpArcHeight, jumpDuration));
    }

    /// <summary>
    /// 직접 위치 지정 점프
    /// </summary>
    public void JumpToPosition(Vector3 startPos, Vector3 endPos, float arcHeight, float duration)
    {
        Debug.Log($"[CharacterJumpController] JumpToPosition 호출됨! {startPos} → {endPos}, 높이: {arcHeight}, 시간: {duration}");
        
        if (isJumping)
        {
            Debug.LogWarning("[CharacterJumpController] 이미 점프 중입니다!");
            return;
        }

        isJumping = true;
        StartCoroutine(JumpCoroutine(startPos, endPos, arcHeight, duration));
    }

    /// <summary>
    /// 지역과 루트에 따른 점프 시작 지점 반환
    /// </summary>
    public Transform GetJumpStartPoint(int region, RouteType route)
    {
        Transform result = null;
        
        if (region == 1)
        {
            switch (route)
            {
                case RouteType.Left:
                    result = region1LeftJumpStart;
                    break;
                case RouteType.Center:
                    result = region1CenterJumpStart;
                    break;
                case RouteType.Right:
                    result = region1RightJumpStart;
                    break;
                default:
                    result = region1CenterJumpStart;
                    break;
            }
        }
        else if (region == 2)
        {
            switch (route)
            {
                case RouteType.Left:
                    result = region2LeftJumpStart;
                    break;
                case RouteType.Center:
                    result = region2CenterJumpStart;
                    break;
                case RouteType.Right:
                    result = region2RightJumpStart;
                    break;
                default:
                    result = region2CenterJumpStart;
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"[CharacterJumpController] 잘못된 지역 번호: {region}");
            return null;
        }
        
        Debug.Log($"[CharacterJumpController] GetJumpStartPoint(지역{region}, {route}) -> {(result != null ? result.name : "null")}");
        return result;
    }

    /// <summary>
    /// 지역과 루트에 따른 점프 도착 지점 반환
    /// </summary>
    public Transform GetJumpEndPoint(int region, RouteType route)
    {
        Transform result = null;
        
        if (region == 1)
        {
            switch (route)
            {
                case RouteType.Left:
                    result = region1LeftJumpEnd;
                    break;
                case RouteType.Center:
                    result = region1CenterJumpEnd;
                    break;
                case RouteType.Right:
                    result = region1RightJumpEnd;
                    break;
                default:
                    result = region1CenterJumpEnd;
                    break;
            }
        }
        else if (region == 2)
        {
            switch (route)
            {
                case RouteType.Left:
                    result = region2LeftJumpEnd;
                    break;
                case RouteType.Center:
                    result = region2CenterJumpEnd;
                    break;
                case RouteType.Right:
                    result = region2RightJumpEnd;
                    break;
                default:
                    result = region2CenterJumpEnd;
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"[CharacterJumpController] 잘못된 지역 번호: {region}");
            return null;
        }
        
        Debug.Log($"[CharacterJumpController] GetJumpEndPoint(지역{region}, {route}) -> {(result != null ? result.name : "null")}");
        return result;
    }

    /// <summary>
    /// 지역 간 점프 실행
    /// </summary>
    public void JumpBetweenRegions(int fromRegion, int toRegion, RouteType route)
    {
        Debug.Log($"[CharacterJumpController] JumpBetweenRegions 호출됨! 지역 {fromRegion} → {toRegion}, 루트: {route}");
        
        if (isJumping)
        {
            Debug.LogWarning("[CharacterJumpController] 이미 점프 중입니다!");
            return;
        }

        // 시작점과 도착점 가져오기
        Transform startPoint = GetJumpStartPoint(fromRegion, route);
        Transform endPoint = GetJumpEndPoint(fromRegion, route);

        if (startPoint == null || endPoint == null)
        {
            Debug.LogError($"[CharacterJumpController] 점프 지점이 설정되지 않았습니다!");
            return;
        }

        // 월드 좌표로 점프
        Vector3 startPos = startPoint.position;
        Vector3 endPos = endPoint.position;
        
        // 캐릭터를 시작 위치로 이동
        transform.position = startPos;
        
        JumpToPosition(startPos, endPos, jumpArcHeight, jumpDuration);
    }

    /// <summary>
    /// 포물선 점프 코루틴
    /// </summary>
    private IEnumerator JumpCoroutine(Vector3 start, Vector3 end, float arc, float time)
    {
        Debug.Log($"[CharacterJumpController] JumpCoroutine 시작! {start} → {end}");
        
        float elapsedTime = 0f;
        
        while (elapsedTime < time)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / time;
            
            // 선형 보간으로 X, Z 위치 계산
            float x = Mathf.Lerp(start.x, end.x, normalizedTime);
            float z = Mathf.Lerp(start.z, end.z, normalizedTime);
            
            // 포물선 형태로 Y 위치 계산
            float baseY = Mathf.Lerp(start.y, end.y, normalizedTime);
            float arcY = arc * 4f * normalizedTime * (1f - normalizedTime);
            float y = baseY + arcY;
            
            // 새 위치로 이동
            transform.position = new Vector3(x, y, z);
            
            yield return null;
        }
        
        // 최종 위치 보정
        transform.position = end;
        
        // 점프 완료
        isJumping = false;
        Debug.Log($"[CharacterJumpController] 점프 완료! 최종 위치: {transform.position}");
        
        // 콜백 호출
        onJumpComplete?.Invoke();
    }
}