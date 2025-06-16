using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 최종성 시스템 - 각 지역의 최종 방어선
/// </summary>
public class FinalCastle : MonoBehaviour, IDamageable
{
    [Header("최종성 설정")]
    public float maxHealth = 1000f;
    public float currentHealth;
    
    [Header("지역 정보")]
    [Tooltip("이 최종성이 속한 지역 (1 또는 2)")]
    public int areaIndex = 1;
    
    [Header("UI 요소")]
    [SerializeField] private Canvas hpBarCanvas;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TextMeshProUGUI castleNameText;
    [SerializeField] private TextMeshProUGUI healthText;
    
    [Header("파괴 시 이펙트")]
    [SerializeField] private GameObject destroyEffectPrefab;
    
    private bool isDestroyed = false;
    
    // 최종성 파괴 이벤트 (게임 종료)
    public System.Action<int> OnFinalCastleDestroyed;
    
    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
        UpdateCastleName();
    }
    
    private void UpdateCastleName()
    {
        if (castleNameText != null)
        {
            castleNameText.text = $"지역{areaIndex} 최종성";
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;
        
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        
        UpdateHealthBar();
        
        Debug.Log($"[FinalCastle] 지역{areaIndex} 최종성 피격! 남은 체력: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            DestroyFinalCastle();
        }
    }
    
    private void UpdateHealthBar()
    {
        if (hpFillImage != null)
        {
            float ratio = currentHealth / maxHealth;
            hpFillImage.fillAmount = ratio;
            
            // 체력에 따른 색상 변경
            if (ratio > 0.6f)
                hpFillImage.color = Color.green;
            else if (ratio > 0.3f)
                hpFillImage.color = Color.yellow;
            else
                hpFillImage.color = Color.red;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
        }
    }
    
    private void DestroyFinalCastle()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        
        Debug.Log($"[FinalCastle] 지역{areaIndex} 최종성 파괴됨! 게임 종료!");
        
        // 파괴 이펙트 생성
        if (destroyEffectPrefab != null)
        {
            GameObject effect = Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        
        // 이벤트 호출
        OnFinalCastleDestroyed?.Invoke(areaIndex);
        
        // GameManager에 게임 종료 알림
        if (GameManager.Instance != null)
        {
            // 지역1 최종성 파괴 = 플레이어 패배
            // 지역2 최종성 파괴 = 플레이어 승리
            bool isVictory = (areaIndex == 2);
            GameManager.Instance.SetGameOver(isVictory);
        }
        
        // HP바 비활성화 (성 자체는 비활성화하지 않음)
        if (hpBarCanvas != null)
        {
            hpBarCanvas.gameObject.SetActive(false);
        }
        
        // 최종성 오브젝트는 비활성화하지 않음
        // gameObject.SetActive(false);
    }
    
    public bool IsDestroyed()
    {
        return isDestroyed;
    }
    
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public float GetMaxHealth()
    {
        return maxHealth;
    }
}

/// <summary>
/// 최종성 클래스 - 게임 기획서: 체력 1000
/// </summary>
public class FinalCastle : MonoBehaviour, IDamageable
{
    [Header("성 정보")]
    public string castleName = "최종성";
    public int maxHealth = 1000;
    public int currentHealth = 1000;
    public int areaIndex = 1; // 1: Region1, 2: Region2
    
    [Header("UI")]
    public Slider healthBar;
    public TextMeshProUGUI healthText;
    
    [Header("효과")]
    public GameObject damageEffect;
    public GameObject destroyEffect;
    
    private void Start()
    {
        UpdateHealthUI();
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= Mathf.RoundToInt(damage);
        currentHealth = Mathf.Max(0, currentHealth);
        
        UpdateHealthUI();
        
        if (damageEffect != null)
        {
            GameObject effect = Instantiate(damageEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        Debug.Log($"[FinalCastle] {castleName}이(가) {damage} 데미지를 받았습니다! 남은 체력: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            OnDestroyed();
        }
    }
    
    private void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }
    
    private void OnDestroyed()
    {
        Debug.Log($"[FinalCastle] {castleName}이(가) 파괴되었습니다! 게임 종료!");
        
        if (destroyEffect != null)
        {
            GameObject effect = Instantiate(destroyEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        
        // GameManager에 게임 종료 알림
        if (GameManager.Instance != null)
        {
            if (areaIndex == 1)
            {
                GameManager.Instance.TakeDamageToRegion1(currentHealth);
            }
            else
            {
                GameManager.Instance.TakeDamageToRegion2(currentHealth);
            }
        }
        
        gameObject.SetActive(false);
    }
}