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

    private IEnumerator FillMineralBarCoroutine()
    {
        while (currentSegments < maxSegments)
        {
            currentSegments += fillPerSecond;
            if (currentSegments > maxSegments)
            {
                currentSegments = maxSegments;
            }
            UpdateBarVisual();
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
}
