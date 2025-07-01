using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MineralBar : MonoBehaviour
{
    [Header("미네랄 설정")]
    public int maxMinerals = 100;
    public int currentMinerals = 0;
    public float mineralGainRate = 1f; // 초당 미네랄 획득량
    
    [Header("UI 요소")]
    public Slider mineralSlider;
    public TextMeshProUGUI mineralText;
    public Image fillImage;
    
    [Header("색상 설정")]
    public Color normalColor = Color.blue;
    public Color fullColor = Color.yellow;
    
    private bool isGaining = true;
    
    private void Start()
    {
        InitializeMineralBar();
    }
    
    private void Update()
    {
        if (isGaining)
        {
            GainMinerals(mineralGainRate * Time.deltaTime);
        }
    }
    
    private void InitializeMineralBar()
    {
        if (mineralSlider != null)
        {
            mineralSlider.maxValue = maxMinerals;
            mineralSlider.value = currentMinerals;
        }
        
        UpdateUI();
    }
    
    public void GainMinerals(float amount)
    {
        currentMinerals = Mathf.Min(currentMinerals + (int)amount, maxMinerals);
        UpdateUI();
    }
    
    public bool SpendMinerals(int amount)
    {
        if (currentMinerals >= amount)
        {
            currentMinerals -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }
    
    public void SetMinerals(int amount)
    {
        currentMinerals = Mathf.Clamp(amount, 0, maxMinerals);
        UpdateUI();
    }
    
    public void AddMinerals(int amount)
    {
        currentMinerals = Mathf.Min(currentMinerals + amount, maxMinerals);
        UpdateUI();
    }
    
    public bool IsFull()
    {
        return currentMinerals >= maxMinerals;
    }
    
    public float GetMineralPercentage()
    {
        return (float)currentMinerals / maxMinerals;
    }
    
    private void UpdateUI()
    {
        if (mineralSlider != null)
        {
            mineralSlider.value = currentMinerals;
        }
        
        if (mineralText != null)
        {
            mineralText.text = $"{currentMinerals}/{maxMinerals}";
        }
        
        if (fillImage != null)
        {
            fillImage.color = IsFull() ? fullColor : normalColor;
        }
    }
    
    public void SetGaining(bool gaining)
    {
        isGaining = gaining;
    }
    
    public void SetGainRate(float rate)
    {
        mineralGainRate = rate;
    }
    
    public void SetMaxMinerals(int max)
    {
        maxMinerals = max;
        if (mineralSlider != null)
        {
            mineralSlider.maxValue = maxMinerals;
        }
        UpdateUI();
    }
} 