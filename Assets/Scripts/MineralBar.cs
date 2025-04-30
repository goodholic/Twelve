using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MineralBar : MonoBehaviour
{
    [Header("Bar Foreground RectTransform")]
    public RectTransform barForeground;

    [Header("Mineral Bar Settings")]
    public int maxSegments = 10;         // 미네랄바를 10칸으로 분할
    public int fillPerSecond = 1;        // 1초마다 얼마나 차오르는지
    public float maxBarWidth = 200f;     // 바의 최대 길이
    public int initialSegments = 0;      // 시작 시 몇 칸 채워둘지

    [Tooltip("씬 시작 시 자동으로 차오르기 시작할지 여부")]
    public bool autoStartFilling = true;

    [Header("분할된 Segment Image들 (예: 10개)")]
    [Tooltip("인스펙터에서 총 maxSegments(기본 10)개 Image를 넣어주세요.")]
    public List<Image> segmentImages;

    // 현재 충전된 미네랄 칸 수 (0 ~ maxSegments)
    private int currentSegments = 0;

    private void Start()
    {
        // 시작 시 현재 미네랄을 초기화
        currentSegments = Mathf.Clamp(initialSegments, 0, maxSegments);
        UpdateBarVisual();

        if (autoStartFilling)
        {
            // 자동 충전 코루틴 시작
            StartCoroutine(FillMineralBarCoroutine());
        }
    }

    /// <summary>
    /// 1초 간격으로 미네랄바를 채우는 코루틴.
    /// currentSegments가 maxSegments보다 작으면 채우고,
    /// 이미 최대치면 그대로 유지하다가,
    /// 소환으로 cost가 소모되어 여유가 생기면 다시 채움.
    /// => "가득 찼어도 cost만큼 차감되면 다음부터 계속 1초마다 다시 차오름"
    /// </summary>
    private IEnumerator FillMineralBarCoroutine()
    {
        while (true)
        {
            // 최대치가 아니면 fillPerSecond만큼 채움
            if (currentSegments < maxSegments)
            {
                currentSegments += fillPerSecond;
                if (currentSegments > maxSegments)
                    currentSegments = maxSegments;

                UpdateBarVisual();
            }

            // 1초 대기 후 반복
            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// cost(캐릭터 소환 비용)만큼 미네랄이 있는지 확인하고, 
    /// 충분하면 해당 cost만큼 차감하고 true 반환,
    /// 부족하면 false 반환.
    /// </summary>
    public bool TrySpend(int cost)
    {
        if (currentSegments < cost)
        {
            // 미네랄 부족
            return false;
        }

        // 미네랄이 충분 -> cost만큼 소모
        currentSegments -= cost;
        if (currentSegments < 0)
            currentSegments = 0;

        // 바 갱신
        UpdateBarVisual();
        return true;
    }

    /// <summary>
    /// 실제 Bar 표시 + 분할 아이콘 On/Off
    /// </summary>
    private void UpdateBarVisual()
    {
        float ratio = (float)currentSegments / maxSegments;

        // A) 바의 가로길이 조절
        if (barForeground != null)
        {
            Vector2 size = barForeground.sizeDelta;
            size.x = maxBarWidth * ratio;
            barForeground.sizeDelta = size;
        }

        // B) 10칸 세그먼트 아이콘 보이기/숨기기
        if (segmentImages != null && segmentImages.Count == maxSegments)
        {
            for (int i = 0; i < maxSegments; i++)
            {
                segmentImages[i].gameObject.SetActive(i < currentSegments);
            }
        }
    }

    /// <summary>
    /// 바를 다시 0부터 충전 시작
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
