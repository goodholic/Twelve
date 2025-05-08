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
/// </summary>
public class CharacterJumpController : MonoBehaviour
{
    [Header("점프 애니메이션 설정값")]
    [Tooltip("점프하는 동안의 최고 높이(arc). 숫자가 클수록 포물선이 크게 보임.")]
    public float jumpArcHeight = 80f;

    [Tooltip("점프 이동 전체 소요 시간(초)")]
    public float jumpDuration = 0.8f;

    [Header("테스트용 (자동점프)")]
    [Tooltip("시작 Tile의 RectTransform (테스트용)")]
    public RectTransform startTileRect;

    [Tooltip("도착 Tile의 RectTransform (테스트용)")]
    public RectTransform endTileRect;

    [Tooltip("씬 시작 시 즉시 점프를 테스트하려면 true")]
    public bool autoJumpOnStart = false;

    // 이동 대상 캐릭터의 RectTransform
    private RectTransform charRect;

    // ====================== [추가된 필드/이벤트] ======================
    /// <summary>
    /// 점프가 진행 중인지 여부
    /// </summary>
    public bool isJumping = false;

    /// <summary>
    /// 점프 애니메이션 완료 시 호출될 콜백
    /// </summary>
    public Action onJumpComplete;
    // ===============================================================

    private void Awake()
    {
        charRect = GetComponent<RectTransform>();
        if (charRect == null)
        {
            Debug.LogError($"[CharacterJumpController] {name} 오브젝트에 RectTransform이 없습니다. " +
                           "UI 상에서 점프를 구현하려면 RectTransform이 필요합니다!");
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
    /// <param name="fromTile">시작 타일(RectTransform)</param>
    /// <param name="toTile">목표 타일(RectTransform)</param>
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

        // === [추가] 점프 중 상태 세팅 ===
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
        if (charRect == null) return;
        // === [추가] 점프 중 상태 세팅 ===
        isJumping = true;

        StartCoroutine(JumpCoroutine(startPos, endPos, arcHeight, duration));
    }

    /// <summary>
    /// 포물선(arc)을 그리며 점프하는 코루틴.
    /// </summary>
    private IEnumerator JumpCoroutine(Vector2 start, Vector2 end, float arc, float time)
    {
        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / time);

            // 가로(직선) 보간
            float xPos = Mathf.Lerp(start.x, end.x, t);

            // 세로(포물선) 보간:
            float heightCurve = 4f * arc * t * (1f - t); 
            float yPos = Mathf.Lerp(start.y, end.y, t) + heightCurve;

            // 실제 UI 위치 반영
            if (charRect != null)
            {
                charRect.anchoredPosition = new Vector2(xPos, yPos);
            }

            yield return null;
        }

        // 마지막 보정
        if (charRect != null)
        {
            charRect.anchoredPosition = end;
        }

        // === [추가] 점프 완료 처리 ===
        isJumping = false;
        onJumpComplete?.Invoke();
    }
}
