using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 미네랄 바 컨트롤러: 0부터 시작해서 1초에 1씩 자동으로 채워지는 미네랄 시스템
/// 게임 기획서: 원 버튼 소환에 미네랄 소모
/// </summary>
public class MineralBar : MonoBehaviour
{
    [Header("미네랄 바 설정")]
    public RectTransform fillBar;            // 채워지는 바 이미지의 RectTransform
    public int maxMinerals = 10;             // 최대 미네랄 수
    public float fillDelay = 1.0f;           // 1 미네랄 채워지는데 걸리는 시간(초)
    public float barMaxWidth = 1080f;        // 바의 최대 너비
    
    [Header("미네랄 표시")]
    public TextMeshProUGUI mineralText;      // 현재 미네랄 수 표시 텍스트
    public Image mineralIcon;                // 미네랄 아이콘 이미지
    
    [Header("효과")]
    public GameObject mineralGainEffect;     // 미네랄 획득 시 효과
    public GameObject mineralSpendEffect;    // 미네랄 소비 시 효과
    
    [Header("소환 비용")]
    [Tooltip("캐릭터 소환에 필요한 기본 미네랄 비용")]
    public int defaultSummonCost = 3;        // 기본 소환 비용

    private int currentMinerals = 0;         // 현재 미네랄 수
    private Coroutine fillCoroutine;         // 미네랄 채우기 코루틴 참조
    
    // 이벤트
    public System.Action<int> OnMineralChanged;  // 미네랄 변경 시 호출
    public System.Action OnMineralFull;          // 미네랄 최대치 도달 시 호출

    private void Start()
    {
        // 미네랄 바를 0으로 초기화
        currentMinerals = 0;
        UpdateVisual();
        
        // 자동 채우기 시작
        StartFilling();
        
        Debug.Log("[MineralBar] 미네랄 시스템 초기화 완료. 1초에 1씩 자동 충전됩니다.");
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
                
                // 게임이 일시정지 상태가 아닐 때만 증가
                if (Time.timeScale > 0)
                {
                    currentMinerals++;
                    UpdateVisual();
                    PlayMineralGainEffect();
                    
                    // 이벤트 호출
                    OnMineralChanged?.Invoke(currentMinerals);
                    
                    // 최대치 도달 체크
                    if (currentMinerals >= maxMinerals)
                    {
                        OnMineralFull?.Invoke();
                        Debug.Log("[MineralBar] 미네랄이 최대치에 도달했습니다!");
                    }
                }
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
            
            // 미네랄 양에 따른 색상 변경 (선택적)
            Image fillImage = fillBar.GetComponent<Image>();
            if (fillImage != null)
            {
                if (fillRatio < 0.3f)
                {
                    fillImage.color = Color.red;        // 부족
                }
                else if (fillRatio < 0.7f)
                {
                    fillImage.color = Color.yellow;     // 보통
                }
                else
                {
                    fillImage.color = Color.green;      // 충분
                }
            }
        }
        
        // 텍스트 업데이트
        if (mineralText != null)
        {
            mineralText.text = $"{currentMinerals}/{maxMinerals}";
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
    /// 미네랄을 소비합니다 (원 버튼 소환용)
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
            PlayMineralSpendEffect();
            
            // 이벤트 호출
            OnMineralChanged?.Invoke(currentMinerals);
            
            Debug.Log($"[MineralBar] {cost} 미네랄 소비. 남은 미네랄: {currentMinerals}/{maxMinerals}");
            return true;
        }
        
        // 미네랄 부족
        Debug.LogWarning($"[MineralBar] 미네랄 부족! 필요: {cost}, 현재: {currentMinerals}");
        return false;
    }

    /// <summary>
    /// 캐릭터의 코스트에 따른 미네랄 소비 시도
    /// </summary>
    public bool TrySpendForCharacter(CharacterData characterData)
    {
        if (characterData == null) return false;
        
        int cost = characterData.cost > 0 ? characterData.cost : defaultSummonCost;
        return TrySpend(cost);
    }

    /// <summary>
    /// 소비한 미네랄을 환불합니다.
    /// </summary>
    /// <param name="amount">환불할 미네랄 양</param>
    public void Refund(int amount)
    {
        currentMinerals = Mathf.Min(currentMinerals + amount, maxMinerals);
        UpdateVisual();
        OnMineralChanged?.Invoke(currentMinerals);
        
        Debug.Log($"[MineralBar] {amount} 미네랄 환불. 현재 미네랄: {currentMinerals}/{maxMinerals}");
    }

    /// <summary>
    /// 현재 미네랄 수를 반환합니다.
    /// </summary>
    public int GetMineral()
    {
        return currentMinerals;
    }

    /// <summary>
    /// 미네랄을 추가합니다. (몬스터 처치 보상 등)
    /// </summary>
    public void AddMineral(int amount)
    {
        currentMinerals = Mathf.Min(currentMinerals + amount, maxMinerals);
        UpdateVisual();
        PlayMineralGainEffect();
        OnMineralChanged?.Invoke(currentMinerals);
        
        Debug.Log($"[MineralBar] {amount} 미네랄 추가. 현재 미네랄: {currentMinerals}/{maxMinerals}");
    }

    /// <summary>
    /// 미네랄을 사용합니다.
    /// </summary>
    public bool UseMineral(int amount)
    {
        if (currentMinerals >= amount)
        {
            currentMinerals -= amount;
            UpdateVisual();
            PlayMineralSpendEffect();
            OnMineralChanged?.Invoke(currentMinerals);
            
            Debug.Log($"[MineralBar] {amount} 미네랄 사용. 남은 미네랄: {currentMinerals}/{maxMinerals}");
            return true;
        }
        
        Debug.LogWarning($"[MineralBar] 미네랄 부족! 필요: {amount}, 현재: {currentMinerals}");
        return false;
    }

    /// <summary>
    /// 미네랄 획득 효과 재생
    /// </summary>
    private void PlayMineralGainEffect()
    {
        if (mineralGainEffect != null)
        {
            GameObject effect = Instantiate(mineralGainEffect, transform.position, Quaternion.identity, transform);
            Destroy(effect, 1f);
        }
    }

    /// <summary>
    /// 미네랄 소비 효과 재생
    /// </summary>
    private void PlayMineralSpendEffect()
    {
        if (mineralSpendEffect != null)
        {
            GameObject effect = Instantiate(mineralSpendEffect, transform.position, Quaternion.identity, transform);
            Destroy(effect, 1f);
        }
    }

    /// <summary>
    /// 미네랄 바 초기화
    /// </summary>
    public void ResetMinerals()
    {
        currentMinerals = 0;
        UpdateVisual();
        OnMineralChanged?.Invoke(currentMinerals);
        
        Debug.Log("[MineralBar] 미네랄 바가 초기화되었습니다.");
    }

    /// <summary>
    /// 최대 미네랄 수 변경
    /// </summary>
    public void SetMaxMinerals(int newMax)
    {
        maxMinerals = newMax;
        currentMinerals = Mathf.Min(currentMinerals, maxMinerals);
        UpdateVisual();
        
        Debug.Log($"[MineralBar] 최대 미네랄이 {maxMinerals}로 변경되었습니다.");
    }

    private void OnDestroy()
    {
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
        }
    }
}