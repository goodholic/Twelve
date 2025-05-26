// Assets\Scripts\CharacterJumpController.cs

using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 2D Canvas 환경에서, 특정 타일(A) 위치에서 B 타일 위치로
/// 캐릭터가 "점프" 애니메이션을 하듯 이동시키는 컨트롤러 예시 스크립트.
///
/// [사용법 예시]
/// 1) 캐릭터 오브젝트(또는 UI Image, RectTransform)를 이 스크립트와 함께 두고,
/// 2) JumpToTile() 메서드로 타일 A, B의 위치를 전달하여 코루틴 실행.
///
/// - 일반적인 2D UI 좌표(anchoredPosition)에서 포물선 형태 이동을 구현.
/// - Arc 곡률, 이동 시간(duration) 등을 파라미터로 조절 가능.
///
/// ---
/// *** 주의 ***
/// StartTile Rect, EndTile Rect 등이 'Canvas'의 자식 RectTransform이라면,
/// 좌표 변환 시 Canvas 내부의 로컬 좌표를 사용해야 합니다.
/// 즉, A/B 타일(=StartTileRect, EndTileRect)은 모두 Canvas 아래에 있는지 확인하세요.
/// ---
/// </summary>
public class CharacterJumpController : MonoBehaviour
{
    [Header("점프 애니메이션 설정값")]
    [Tooltip("점프하는 동안의 최고 높이(arc). 숫자가 클수록 포물선이 크게 보임.")]
    public float jumpArcHeight = 80f;

    [Tooltip("점프 이동 전체 소요 시간(초)")]
    public float jumpDuration = 0.8f;

    [Header("지역 1 캐릭터 점프 지점 (3개 루트)")]
    [Tooltip("지역 1 좌측 루트 점프 시작 지점 (지역1 내의 점프 시작 위치)")]
    public RectTransform region1LeftJumpStart;
    [Tooltip("지역 1 좌측 루트 점프 도착 지점 (지역2 내의 점프 도착 위치)")]
    public RectTransform region1LeftJumpEnd;
    
    [Tooltip("지역 1 중앙 루트 점프 시작 지점 (지역1 내의 점프 시작 위치)")]
    public RectTransform region1CenterJumpStart;
    [Tooltip("지역 1 중앙 루트 점프 도착 지점 (지역2 내의 점프 도착 위치)")]
    public RectTransform region1CenterJumpEnd;
    
    [Tooltip("지역 1 우측 루트 점프 시작 지점 (지역1 내의 점프 시작 위치)")]
    public RectTransform region1RightJumpStart;
    [Tooltip("지역 1 우측 루트 점프 도착 지점 (지역2 내의 점프 도착 위치)")]
    public RectTransform region1RightJumpEnd;

    [Header("지역 2 캐릭터 점프 지점 (3개 루트)")]
    [Tooltip("지역 2 좌측 루트 점프 시작 지점 (지역2 내의 점프 시작 위치)")]
    public RectTransform region2LeftJumpStart;
    [Tooltip("지역 2 좌측 루트 점프 도착 지점 (지역1 내의 점프 도착 위치)")]
    public RectTransform region2LeftJumpEnd;
    
    [Tooltip("지역 2 중앙 루트 점프 시작 지점 (지역2 내의 점프 시작 위치)")]
    public RectTransform region2CenterJumpStart;
    [Tooltip("지역 2 중앙 루트 점프 도착 지점 (지역1 내의 점프 도착 위치)")]
    public RectTransform region2CenterJumpEnd;
    
    [Tooltip("지역 2 우측 루트 점프 시작 지점 (지역2 내의 점프 시작 위치)")]
    public RectTransform region2RightJumpStart;
    [Tooltip("지역 2 우측 루트 점프 도착 지점 (지역1 내의 점프 도착 위치)")]
    public RectTransform region2RightJumpEnd;

    [Header("테스트용 (자동점프)")]
    [Tooltip("시작 Tile의 RectTransform (테스트용). Canvas의 자식에 배치되어 있어야 함.")]
    public RectTransform startTileRect;

    [Tooltip("도착 Tile의 RectTransform (테스트용). Canvas의 자식에 배치되어 있어야 함.")]
    public RectTransform endTileRect;

    [Tooltip("씬 시작 시 즉시 점프를 테스트하려면 true")]
    public bool autoJumpOnStart = false;

    // 루트 타입 열거형
    public enum RouteType
    {
        Left = 0,
        Center = 1,
        Right = 2
    }

    // 이동 대상 캐릭터의 RectTransform
    private RectTransform charRect;

    /// <summary>
    /// 점프가 진행 중인지 여부
    /// </summary>
    public bool isJumping = false;

    /// <summary>
    /// 점프 애니메이션 완료 시 호출될 콜백
    /// </summary>
    public Action onJumpComplete;

    private void Awake()
    {
        charRect = GetComponent<RectTransform>();
        if (charRect == null)
        {
            Debug.LogError($"[CharacterJumpController] {name} 오브젝트에 RectTransform이 없습니다. " +
                           "UI 상에서 점프를 구현하려면 RectTransform이 필요합니다!");
        }
        else
        {
            Debug.Log($"[CharacterJumpController] {name}에 RectTransform 컴포넌트 찾음!");
        }
    }

    private void Start()
    {
        // 테스트용 자동 실행
        if (autoJumpOnStart && startTileRect != null && endTileRect != null)
        {
            // 시작 위치로 이동 후 즉시 점프
            Vector2 startPos = startTileRect.anchoredPosition;
            charRect.anchoredPosition = startPos;

            JumpToTile(startTileRect, endTileRect);
        }
    }

    /// <summary>
    /// 실제 점프 코루틴을 실행하는 메서드.
    /// 예: JumpToTile(A타일, B타일)
    /// </summary>
    /// <param name="fromTile">시작 타일(RectTransform, 반드시 Canvas의 자식)</param>
    /// <param name="toTile">목표 타일(RectTransform, 반드시 Canvas의 자식)</param>
    public void JumpToTile(RectTransform fromTile, RectTransform toTile)
    {
        if (charRect == null) return;
        if (fromTile == null || toTile == null)
        {
            Debug.LogWarning("[CharacterJumpController] fromTile 또는 toTile이 null입니다. 점프 불가.");
            return;
        }

        // 시작점 - 도착점 UI 좌표 구하기
        Vector2 fromPos = fromTile.anchoredPosition;
        Vector2 toPos   = toTile.anchoredPosition;

        // 우선 캐릭터를 fromTile 위치로 이동
        charRect.anchoredPosition = fromPos;

        // 점프 중 상태 세팅
        isJumping = true;

        // 점프 코루틴 실행
        StartCoroutine(JumpCoroutine(fromPos, toPos, jumpArcHeight, jumpDuration));
    }

    /// <summary>
    /// (오버라이드) Tile의 anchoredPosition이 아닌 직접적인 Vector2 위치를 받아 점프하는 함수
    /// </summary>
    /// <param name="startPos">시작점</param>
    /// <param name="endPos">도착점</param>
    /// <param name="arcHeight">포물선 높이</param>
    /// <param name="duration">이동 시간(초)</param>
    public void JumpToPosition(Vector2 startPos, Vector2 endPos, float arcHeight, float duration)
    {
        Debug.Log($"[CharacterJumpController] JumpToPosition 호출됨! {startPos} → {endPos}, 높이: {arcHeight}, 시간: {duration}");
        
        if (charRect == null) 
        {
            Debug.LogError("[CharacterJumpController] JumpToPosition: charRect가 null입니다!");
            return;
        }

        if (isJumping)
        {
            Debug.LogWarning("[CharacterJumpController] JumpToPosition: 이미 점프 중입니다!");
            return;
        }

        isJumping = true;
        Debug.Log("[CharacterJumpController] 점프 코루틴 시작!");
        StartCoroutine(JumpCoroutine(startPos, endPos, arcHeight, duration));
    }

    /// <summary>
    /// 지역과 루트에 따른 점프 시작 지점 반환
    /// </summary>
    /// <param name="region">지역 (1 또는 2)</param>
    /// <param name="route">루트 타입</param>
    /// <returns>점프 시작 지점 RectTransform</returns>
    public RectTransform GetJumpStartPoint(int region, RouteType route)
    {
        RectTransform result = null;
        
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
                    result = region1CenterJumpStart; // 기본값
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
                    result = region2CenterJumpStart; // 기본값
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
    /// <param name="region">지역 (1 또는 2)</param>
    /// <param name="route">루트 타입</param>
    /// <returns>점프 도착 지점 RectTransform</returns>
    public RectTransform GetJumpEndPoint(int region, RouteType route)
    {
        RectTransform result = null;
        
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
                    result = region1CenterJumpEnd; // 기본값
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
                    result = region2CenterJumpEnd; // 기본값
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
    /// 지역과 루트에 따라 점프 실행
    /// </summary>
    /// <param name="fromRegion">시작 지역 (1 또는 2)</param>
    /// <param name="toRegion">도착 지역 (1 또는 2)</param>
    /// <param name="route">사용할 루트</param>
    public void JumpBetweenRegions(int fromRegion, int toRegion, RouteType route)
    {
        Debug.Log($"[CharacterJumpController] JumpBetweenRegions 호출됨! 지역 {fromRegion} → {toRegion}, 루트: {route}");
        
        if (isJumping)
        {
            Debug.LogWarning("[CharacterJumpController] 이미 점프 중입니다!");
            return;
        }

        if (charRect == null)
        {
            Debug.LogError("[CharacterJumpController] charRect가 null입니다. RectTransform 컴포넌트가 필요합니다!");
            return;
        }

        // ▼▼ [수정] 점프 지점 설정 방식 변경
        // 지역1 → 지역2: region1의 Start에서 region1의 End로 (region1End가 실제로는 지역2 위치)
        // 지역2 → 지역1: region2의 Start에서 region2의 End로 (region2End가 실제로는 지역1 위치)
        RectTransform startPoint = GetJumpStartPoint(fromRegion, route);
        RectTransform endPoint = GetJumpEndPoint(fromRegion, route); // 같은 지역의 End 포인트 사용

        if (startPoint == null || endPoint == null)
        {
            Debug.LogError($"[CharacterJumpController] 점프 지점이 설정되지 않았습니다! 점프를 실행할 수 없습니다.");
            Debug.LogError($"  startPoint: {startPoint}, endPoint: {endPoint}");
            Debug.LogError($"  Unity 에디터에서 CharacterJumpController의 점프 지점들을 설정해주세요.");
            return;
        }

        Debug.Log($"[CharacterJumpController] 지역 {fromRegion} → 지역 {toRegion} ({route} 루트) 점프 시작");
        Debug.Log($"[CharacterJumpController] 시작점: {startPoint.name} ({startPoint.anchoredPosition})");
        Debug.Log($"[CharacterJumpController] 도착점: {endPoint.name} ({endPoint.anchoredPosition})");

        // 시작점과 도착점의 anchoredPosition 사용
        Vector2 startPos = startPoint.anchoredPosition;
        Vector2 endPos = endPoint.anchoredPosition;
        
        Debug.Log($"[CharacterJumpController] UI 좌표 점프: {startPos} → {endPos}");
        
        // 점프 시작 전에 캐릭터를 시작 위치로 이동
        charRect.anchoredPosition = startPos;
        Debug.Log($"[CharacterJumpController] 캐릭터를 시작 위치로 이동: {startPos}");
        
        JumpToPosition(startPos, endPos, jumpArcHeight, jumpDuration);
    }



    /// <summary>
    /// 포물선(arc)을 그리며 점프하는 코루틴.
    /// </summary>
    private IEnumerator JumpCoroutine(Vector2 start, Vector2 end, float arc, float time)
    {
        Debug.Log($"[CharacterJumpController] JumpCoroutine 시작! {start} → {end}");
        float elapsed = 0f;
        int frameCount = 0;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / time);

            // 가로(직선) 보간
            float xPos = Mathf.Lerp(start.x, end.x, t);

            // 세로(포물선) 보간:
            float heightCurve = 4f * arc * t * (1f - t);
            float yPos = Mathf.Lerp(start.y, end.y, t) + heightCurve;

            Vector2 currentPos = new Vector2(xPos, yPos);

            // 실제 UI 위치 반영
            if (charRect != null)
            {
                charRect.anchoredPosition = currentPos;
                
                // 10프레임마다 위치 로그 출력
                if (frameCount % 10 == 0)
                {
                    Debug.Log($"[CharacterJumpController] 점프 진행 중... t={t:F2}, 위치={currentPos}");
                }
                frameCount++;
            }
            else
            {
                Debug.LogError("[CharacterJumpController] JumpCoroutine: charRect가 null이 되었습니다!");
                break;
            }

            yield return null;
        }

        // 마지막 보정
        if (charRect != null)
        {
            charRect.anchoredPosition = end;
            Debug.Log($"[CharacterJumpController] 점프 완료! 최종 위치: {end}");
        }

        // === 점프 완료 처리 ===
        isJumping = false;
        Debug.Log("[CharacterJumpController] 점프 완료 콜백 호출!");
        onJumpComplete?.Invoke();
    }
}
