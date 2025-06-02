using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 중간성 시스템 - 3라인의 중간 지점에 위치한 성
/// </summary>
public class MiddleCastle : MonoBehaviour, IDamageable
{
    [Header("중간성 설정")]
    public float maxHealth = 500f;
    public float currentHealth;
    
    [Header("라인 정보")]
    [Tooltip("이 중간성이 속한 라인 (Left/Center/Right)")]
    public RouteType routeType = RouteType.Center;
    
    [Header("지역 정보")]
    [Tooltip("이 중간성이 속한 지역 (1 또는 2)")]
    public int areaIndex = 1;
    
    [Header("UI 요소")]
    [SerializeField] private Canvas hpBarCanvas;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TextMeshProUGUI castleNameText;
    
    [Header("파괴 시 이펙트")]
    [SerializeField] private GameObject destroyEffectPrefab;
    
    private bool isDestroyed = false;
    
    // 중간성 파괴 이벤트
    public System.Action<RouteType, int> OnMiddleCastleDestroyed;
    
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
            string routeName = "";
            switch (routeType)
            {
                case RouteType.Left:
                    routeName = "좌측";
                    break;
                case RouteType.Center:
                    routeName = "중앙";
                    break;
                case RouteType.Right:
                    routeName = "우측";
                    break;
            }
            castleNameText.text = $"지역{areaIndex} {routeName} 중간성";
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;
        
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        
        UpdateHealthBar();
        
        Debug.Log($"[MiddleCastle] {gameObject.name} 피격! 남은 체력: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            DestroyMiddleCastle();
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
    }
    
    private void DestroyMiddleCastle()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        
        Debug.Log($"[MiddleCastle] {gameObject.name} 파괴됨! (지역{areaIndex}, {routeType} 라인)");
        
        // 파괴 이펙트 생성
        if (destroyEffectPrefab != null)
        {
            GameObject effect = Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // 이벤트 호출
        OnMiddleCastleDestroyed?.Invoke(routeType, areaIndex);
        
        // HP바 비활성화
        if (hpBarCanvas != null)
        {
            hpBarCanvas.gameObject.SetActive(false);
        }
        
        // 시각적으로 파괴된 모습 표현 (투명도 조절)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0.3f;
            sr.color = c;
        }
        
        Image img = GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = 0.3f;
            img.color = c;
        }
        
        // 콜라이더 비활성화
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
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