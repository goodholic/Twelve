using System.Collections;
using UnityEngine;

/// <summary>
/// 단순 2D 바(구버전 Image Fill이 없다고 가정) 예시
/// </summary>
public class MineralBar : MonoBehaviour
{
    [Header("Bar Foreground RectTransform")]
    public RectTransform barForeground;

    [Header("Mineral Bar Settings")]
    public int maxSegments = 10;
    public int fillPerSecond = 1;
    public float maxBarWidth = 200f;
    public int initialSegments = 0;
    public bool autoStartFilling = true;

    private int currentSegments = 0;

    private void Start()
    {
        currentSegments = Mathf.Clamp(initialSegments, 0, maxSegments);
        UpdateBarVisual();

        if (autoStartFilling)
        {
            StartCoroutine(FillMineralBarCoroutine());
        }
    }

    /// <summary>
    /// [수정] 바가 가득 차 있어도 계속 루프를 돌며,
    /// 소모 후 다시 currentSegments < maxSegments이면 재충전되도록 변경.
    /// </summary>
    private IEnumerator FillMineralBarCoroutine()
    {
        // 무한루프로 전환
        while (true)
        {
            // 현재 바가 maxSegments 미만이면 충전
            if (currentSegments < maxSegments)
            {
                currentSegments += fillPerSecond;
                if (currentSegments > maxSegments)
                {
                    currentSegments = maxSegments;
                }
                UpdateBarVisual();
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void UpdateBarVisual()
    {
        float fillRatio = (float)currentSegments / maxSegments;

        if (barForeground != null)
        {
            Vector2 size = barForeground.sizeDelta;
            size.x = maxBarWidth * fillRatio;
            barForeground.sizeDelta = size;
        }
    }

    public void StartFilling()
    {
        StopAllCoroutines();
        StartCoroutine(FillMineralBarCoroutine());
    }

    public void ResetBar()
    {
        StopAllCoroutines();
        currentSegments = 0;
        UpdateBarVisual();
    }

    /// <summary>
    /// 지정된 비용만큼 미네랄을 소모합니다. 충분한 미네랄이 있는 경우만 소모하고 true를 반환합니다.
    /// </summary>
    /// <param name="cost">소모할 미네랄 비용</param>
    /// <returns>성공적으로 소모했으면 true, 미네랄이 부족하면 false</returns>
    public bool TrySpend(int cost)
    {
        if (currentSegments >= cost)
        {
            currentSegments -= cost;
            UpdateBarVisual();
            return true;
        }
        return false;
    }
}
