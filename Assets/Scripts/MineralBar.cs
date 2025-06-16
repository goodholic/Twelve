using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 미네랄 바 UI 관리
/// 게임 기획서: 미네랄 소모하여 캐릭터 소환
/// </summary>
public class MineralBar : MonoBehaviour
{
    [Header("미네랄 설정")]
    [SerializeField] private int maxMinerals = 100;
    [SerializeField] private int currentMinerals = 50;
    [SerializeField] private float mineralRegenRate = 5f; // 초당 미네랄 회복량
    [SerializeField] private int defaultSummonCost = 10;
    
    [Header("UI 요소")]
    [SerializeField] private Slider mineralSlider;
    [SerializeField] private TextMeshProUGUI mineralText;
    [SerializeField] private Image fillImage;
    
    [Header("색상 설정")]
    [SerializeField] private Color normalColor = Color.blue;
    [SerializeField] private Color lowColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    
    [Header("효과")]
    [SerializeField] private GameObject mineralGainEffect;
    [SerializeField] private GameObject mineralSpendEffect;
    
    // 이벤트
    public event Action<int> OnMineralChanged;
    
    private float regenTimer = 0f;
    
    private void Awake()
    {
        // 슬라이더 초기 설정
        if (mineralSlider != null)
        {
            mineralSlider.maxValue = maxMinerals;
            mineralSlider.value = currentMinerals;
        }
        
        UpdateVisual();
    }
    
    private void Update()
    {
        // 미네랄 자동 회복
        regenTimer += Time.deltaTime;
        if (regenTimer >= 1f)
        {
            regenTimer = 0f;
            AddMineral(Mathf.RoundToInt(mineralRegenRate));
        }
    }
    
    /// <summary>
    /// 미네랄 UI 업데이트
    /// </summary>
    private void UpdateVisual()
    {
        if (mineralSlider != null)
        {
            mineralSlider.value = currentMinerals;
        }
        
        if (mineralText != null)
        {
            mineralText.text = $"{currentMinerals}/{maxMinerals}";
        }
        
        // 미네랄 양에 따른 색상 변경
        if (fillImage != null)
        {
            float ratio = (float)currentMinerals / maxMinerals;
            if (ratio > 0.5f)
                fillImage.color = normalColor;
            else if (ratio > 0.2f)
                fillImage.color = lowColor;
            else
                fillImage.color = criticalColor;
        }
    }
    
    /// <summary>
    /// 미네랄 소비 시도
    /// </summary>
    public bool TrySpend(int cost)
    {
        if (currentMinerals >= cost)
        {
            currentMinerals -= cost;
            UpdateVisual();
            PlayMineralSpendEffect();
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
    /// 현재 미네랄 수를 반환합니다. (GetCurrentMinerals 추가)
    /// </summary>
    public int GetCurrentMinerals()
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
    /// 최대 미네랄 설정
    /// </summary>
    public void SetMaxMinerals(int max)
    {
        maxMinerals = max;
        if (mineralSlider != null)
        {
            mineralSlider.maxValue = maxMinerals;
        }
        UpdateVisual();
    }

    /// <summary>
    /// 미네랄 회복률 설정
    /// </summary>
    public void SetRegenRate(float rate)
    {
        mineralRegenRate = rate;
    }
}