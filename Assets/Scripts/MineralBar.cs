using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MineralBar : MonoBehaviour
{
    [Header("Bar Foreground RectTransform")]
    public RectTransform barForeground;

    [Header("Mineral Bar Settings")]
    public int maxSegments = 10;
    public int fillPerSecond = 1;
    public float maxBarWidth = 200f;
    public int initialSegments = 0;

    [Tooltip("씬 시작 시 자동으로 차오르기 시작할지 여부")]
    public bool autoStartFilling = true;

    [Header("분할된 Segment Image들 (예: 10개)")]
    [Tooltip("인스펙터에서 총 maxSegments개(기본 10) Image를 넣어주세요.")]
    public List<UnityEngine.UI.Image> segmentImages;

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
    /// (수정) 무한 루프로 돌면서, currentSegments가 maxSegments보다 작을 때만 1초마다 채움.
    /// 가득 찼다가 소환으로 감소해도 다시 알아서 차오르도록 유지.
    /// </summary>
    private IEnumerator FillMineralBarCoroutine()
    {
        while (true)
        {
            // 만약 현재 미네랄이 최대치보다 작다면 -> 1초마다 fillPerSecond만큼 채운다.
            if (currentSegments < maxSegments)
            {
                currentSegments += fillPerSecond;

                // 범위 초과 방지
                if (currentSegments > maxSegments)
                {
                    currentSegments = maxSegments;
                }

                UpdateBarVisual();
            }

            // 1초 간격으로 진행
            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// 실제 Bar 표시 및 세그먼트 이미지 On/Off
    /// </summary>
    private void UpdateBarVisual()
    {
        float fillRatio = (float)currentSegments / maxSegments;

        // (A) 전체 바의 가로 길이를 비율만큼
        if (barForeground != null)
        {
            Vector2 size = barForeground.sizeDelta;
            size.x = maxBarWidth * fillRatio;
            barForeground.sizeDelta = size;
        }

        // (B) 세그먼트 이미지들 On/Off
        if (segmentImages != null && segmentImages.Count == maxSegments)
        {
            for (int i = 0; i < maxSegments; i++)
            {
                segmentImages[i].gameObject.SetActive(i < currentSegments);
            }
        }
    }

    /// <summary>
    /// 비용(cost)만큼 소모 시도 -> 소모 성공하면 true, 실패면 false
    /// </summary>
    public bool TrySpend(int cost)
    {
        if (currentSegments < cost)
        {
            // 미네랄 부족
            return false;
        }

        currentSegments -= cost;
        if (currentSegments < 0)
            currentSegments = 0;

        UpdateBarVisual();
        return true;
    }

    /// <summary>
    /// 바를 다시 0부터 자동 충전 시작
    /// </summary>
    public void StartFilling()
    {
        StopAllCoroutines();
        StartCoroutine(FillMineralBarCoroutine());
    }

    /// <summary>
    /// 바를 0으로 리셋
    /// </summary>
    public void ResetBar()
    {
        StopAllCoroutines();
        currentSegments = 0;
        UpdateBarVisual();
    }
}
