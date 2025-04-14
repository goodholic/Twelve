using System.Collections;
using UnityEngine;

/// <summary>
/// 유니티 구버전(가정)에서 Image Fill Method 없이
/// RectTransform을 이용해 1초마다 1칸씩 총 10칸이 차오르는 바를 구현.
/// </summary>
public class MineralBar : MonoBehaviour
{
    [Header("Bar Foreground RectTransform")]
    [Tooltip("실제 바(앞부분)에 해당하는 RectTransform. 배경과 구분하여 자식 오브젝트로 만들어 두는 것을 권장.")]
    public RectTransform barForeground;

    [Header("Mineral Bar Settings")]
    [Tooltip("총 채워질 칸 수 (예: 10칸)")]
    public int maxSegments = 10;

    [Tooltip("1초에 몇 칸씩 채우는지 (예: 1칸)")]
    public int fillPerSecond = 1;

    [Tooltip("바의 최대 가로 길이 (픽셀 단위, 예: 200f). 10칸이 전부 차올 때 바Foreground가 차지할 실제 길이")]
    public float maxBarWidth = 200f;

    [Tooltip("바가 처음 시작될 때 채워져 있는 칸(기본 0)")]
    public int initialSegments = 0;

    [Tooltip("Bar를 자동으로 채우기 시작할지 여부. False이면 필요시 Fill 코루틴을 수동으로 호출")]
    public bool autoStartFilling = true;

    // 현재 채워진 칸 수
    private int currentSegments = 0;

    private void Start()
    {
        // 시작 시 바 초기화
        currentSegments = Mathf.Clamp(initialSegments, 0, maxSegments);
        UpdateBarVisual();

        // 자동 시작 옵션이면 코루틴 시작
        if (autoStartFilling)
        {
            StartCoroutine(FillMineralBarCoroutine());
        }
    }

    /// <summary>
    /// 1초마다 1칸씩 채우는 코루틴
    /// </summary>
    private IEnumerator FillMineralBarCoroutine()
    {
        while (currentSegments < maxSegments)
        {
            // 초당 fillPerSecond 만큼 칸을 채운다
            currentSegments += fillPerSecond;
            if (currentSegments > maxSegments)
            {
                currentSegments = maxSegments;
            }

            UpdateBarVisual();

            // 1초 대기 후 반복
            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// Bar의 RectTransform이 실제로 몇 칸만큼 채워졌는지에 따라
    /// 가로 길이를 조정하여 시각적으로 채워지는 것처럼 보이게 함
    /// </summary>
    private void UpdateBarVisual()
    {
        // currentSegments / maxSegments 만큼의 비율 계산
        float fillRatio = (float)currentSegments / maxSegments;

        if (barForeground != null)
        {
            // 방법 1) sizeDelta.x 로 변경 (Anchor가 왼쪽 고정이거나 Pivot이 왼쪽이어야 함)
            Vector2 size = barForeground.sizeDelta;
            size.x = maxBarWidth * fillRatio;
            barForeground.sizeDelta = size;

            /*
            // 방법 2) localScale x만 0~1로 조절 (Pivot이 왼쪽(0)이어야 함)
            Vector3 scale = barForeground.localScale;
            scale.x = fillRatio;
            barForeground.localScale = scale;
            */
        }
    }

    /// <summary>
    /// (선택) 외부에서 강제로 채우기를 시작/다시 하게 하고 싶을 때 호출
    /// </summary>
    public void StartFilling()
    {
        StopAllCoroutines();
        StartCoroutine(FillMineralBarCoroutine());
    }

    /// <summary>
    /// (선택) 바를 비우고 싶다면 호출
    /// </summary>
    public void ResetBar()
    {
        StopAllCoroutines();
        currentSegments = 0;
        UpdateBarVisual();
    }
}
