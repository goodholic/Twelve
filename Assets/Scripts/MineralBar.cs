using System.Collections;
using UnityEngine;

/// <summary>
/// 미네랄 바 컨트롤러: 0부터 시작해서 1초에 1씩 자동으로 채워지는 미네랄 시스템
/// </summary>
public class MineralBar : MonoBehaviour
{
    [Header("미네랄 바 설정")]
    public RectTransform fillBar;            // 채워지는 바 이미지의 RectTransform
    public int maxMinerals = 10;             // 최대 미네랄 수
    public float fillDelay = 1.0f;           // 1 미네랄 채워지는데 걸리는 시간(초)
    public float barMaxWidth = 1080f;        // 바의 최대 너비

    private int currentMinerals = 0;         // 현재 미네랄 수
    private Coroutine fillCoroutine;         // 미네랄 채우기 코루틴 참조

    private void Start()
    {
        // 미네랄 바를 0으로 초기화
        currentMinerals = 0;
        UpdateVisual();
        
        // 자동 채우기 시작
        StartFilling();
    }

    /// <summary>
    /// 미네랄 바를 자동으로 채우는 코루틴
    /// </summary>
    private IEnumerator FillMineralRoutine()
    {
        while (true)
        {
            // 최대치에 도달하지 않았다면 미네랄 증가
            if (currentMinerals < maxMinerals)
            {
                yield return new WaitForSeconds(fillDelay);
                currentMinerals++;
                UpdateVisual();
            }
            else
            {
                // 최대치에 도달하면 대기
                yield return null;
            }
        }
    }

    /// <summary>
    /// 미네랄 바 시각적 업데이트
    /// </summary>
    private void UpdateVisual()
    {
        if (fillBar != null)
        {
            // 현재 미네랄에 비례하여 바 너비 계산
            float fillRatio = (float)currentMinerals / maxMinerals;
            Vector2 size = fillBar.sizeDelta;
            size.x = barMaxWidth * fillRatio;
            fillBar.sizeDelta = size;
        }
    }

    /// <summary>
    /// 미네랄 자동 채우기 시작
    /// </summary>
    public void StartFilling()
    {
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
        }
        
        fillCoroutine = StartCoroutine(FillMineralRoutine());
    }

    /// <summary>
    /// 미네랄을 소비합니다
    /// </summary>
    /// <param name="cost">소비할 미네랄 양</param>
    /// <returns>소비 성공 여부</returns>
    public bool TrySpend(int cost)
    {
        // 충분한 미네랄이 있는지 확인
        if (currentMinerals >= cost)
        {
            // 미네랄 차감
            currentMinerals -= cost;
            // 시각적 업데이트
            UpdateVisual();
            return true;
        }
        
        // 미네랄 부족
        return false;
    }

    /// <summary>
    /// 소비한 미네랄을 환불합니다. (소환 실패 시 사용)
    /// </summary>
    /// <param name="amount">환불할 미네랄 양</param>
    public void RefundMinerals(int amount)
    {
        // 최대치 초과하지 않도록 제한
        currentMinerals = Mathf.Min(currentMinerals + amount, maxMinerals);
        // 시각적 업데이트
        UpdateVisual();
        Debug.Log($"[MineralBar] {amount} 미네랄 환불됨. 현재: {currentMinerals}/{maxMinerals}");
    }

    /// <summary>
    /// 현재 미네랄 수 반환
    /// </summary>
    public int GetCurrentMinerals()
    {
        return currentMinerals;
    }

    /// <summary>
    /// 게임 오브젝트가 비활성화될 때 코루틴 정리
    /// </summary>
    private void OnDisable()
    {
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
        }
    }
}
