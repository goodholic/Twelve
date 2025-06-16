using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 캐릭터의 시각적 효과를 담당하는 컴포넌트
/// 월드 좌표 기반으로 작동합니다.
/// </summary>
public class CharacterVisual : MonoBehaviour
{
    private Character character;
    private SpriteRenderer spriteRenderer;
    private Canvas hpBarCanvas;
    private Image hpBarFillImage;
    
    [Header("별 등급 효과")]
    [Tooltip("1성 효과 색상")]
    public Color oneStar_Color = Color.white;
    [Tooltip("2성 효과 색상")]
    public Color twoStar_Color = new Color(0.5f, 0.8f, 1f);
    [Tooltip("3성 효과 색상")]
    public Color threeStar_Color = new Color(1f, 0.8f, 0.2f);
    
    [Header("이펙트 프리팹")]
    public GameObject hitEffectPrefab;
    public GameObject deathEffectPrefab;
    public GameObject starEffectPrefab;
    
    [Header("애니메이션 설정")]
    public float hitFlashDuration = 0.1f;
    public float deathFadeDuration = 0.5f;
    
    private Color originalColor;
    private Coroutine currentEffectCoroutine;

    public void Initialize(Character character, SpriteRenderer spriteRenderer, Image uiImage)
    {
        this.character = character;
        this.spriteRenderer = spriteRenderer;
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }
    
    /// <summary>
    /// HP바 참조 설정
    /// </summary>
    public void SetHPBarReferences(Canvas hpCanvas, Image fillImage)
    {
        hpBarCanvas = hpCanvas;
        hpBarFillImage = fillImage;
    }
    
    /// <summary>
    /// HP바 업데이트
    /// </summary>
    public void UpdateHPBar(float fillAmount)
    {
        if (hpBarFillImage != null)
        {
            hpBarFillImage.fillAmount = fillAmount;
            
            // HP에 따른 색상 변경
            if (fillAmount > 0.5f)
                hpBarFillImage.color = Color.green;
            else if (fillAmount > 0.25f)
                hpBarFillImage.color = Color.yellow;
            else
                hpBarFillImage.color = Color.red;
        }
    }
    
    /// <summary>
    /// 구버전 호환용 메서드
    /// </summary>
    public void UpdateHpBar()
    {
        if (character != null)
        {
            UpdateHPBar(character.currentHP / character.maxHP);
        }
    }
    
    /// <summary>
    /// 별 등급에 따른 효과 적용
    /// </summary>
    public void ApplyStarEffect(CharacterStar star)
    {
        if (spriteRenderer == null) return;
        
        Color starColor = Color.white;
        float glowIntensity = 0f;
        
        switch (star)
        {
            case CharacterStar.OneStar:
                starColor = oneStar_Color;
                glowIntensity = 0f;
                break;
            case CharacterStar.TwoStar:
                starColor = twoStar_Color;
                glowIntensity = 0.3f;
                CreateStarParticles(1);
                break;
            case CharacterStar.ThreeStar:
                starColor = threeStar_Color;
                glowIntensity = 0.6f;
                CreateStarParticles(2);
                break;
        }
        
        // 색상 적용
        spriteRenderer.color = starColor;
        originalColor = starColor;
        
        // 글로우 효과 (Material이 있다면)
        if (spriteRenderer.material != null && spriteRenderer.material.HasProperty("_GlowIntensity"))
        {
            spriteRenderer.material.SetFloat("_GlowIntensity", glowIntensity);
        }
    }
    
    /// <summary>
    /// 별 파티클 생성
    /// </summary>
    private void CreateStarParticles(int level)
    {
        if (starEffectPrefab == null) return;
        
        GameObject starEffect = Instantiate(starEffectPrefab, transform);
        starEffect.transform.localPosition = Vector3.zero;
        
        // 파티클 시스템 조정
        ParticleSystem ps = starEffect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startLifetime = 2f + level;
            main.startSpeed = 1f + (level * 0.5f);
            
            var emission = ps.emission;
            emission.rateOverTime = 5f * level;
        }
    }
    
    /// <summary>
    /// 피격 효과 재생
    /// </summary>
    public void PlayHitEffect()
    {
        if (currentEffectCoroutine != null)
        {
            StopCoroutine(currentEffectCoroutine);
        }
        
        currentEffectCoroutine = StartCoroutine(HitFlashCoroutine());
        
        // 피격 이펙트 생성
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    private IEnumerator HitFlashCoroutine()
    {
        if (spriteRenderer == null) yield break;
        
        // 흰색 플래시
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(hitFlashDuration);
        
        // 빨간색 플래시
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(hitFlashDuration);
        
        // 원래 색상으로 복구
        spriteRenderer.color = originalColor;
        
        currentEffectCoroutine = null;
    }
    
    /// <summary>
    /// 사망 효과 재생
    /// </summary>
    public void PlayDeathEffect()
    {
        if (currentEffectCoroutine != null)
        {
            StopCoroutine(currentEffectCoroutine);
        }
        
        currentEffectCoroutine = StartCoroutine(DeathFadeCoroutine());
        
        // 사망 이펙트 생성
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        
        // HP바 숨기기
        if (hpBarCanvas != null)
        {
            hpBarCanvas.gameObject.SetActive(false);
        }
    }
    
    private IEnumerator DeathFadeCoroutine()
    {
        if (spriteRenderer == null) yield break;
        
        float elapsed = 0f;
        Color startColor = spriteRenderer.color;
        
        while (elapsed < deathFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / deathFadeDuration;
            
            // 페이드 아웃
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = newColor;
            
            // 위로 살짝 올라가는 효과
            transform.position += Vector3.up * Time.deltaTime * 0.5f;
            
            // 회전 효과
            transform.rotation = Quaternion.Euler(0, 0, t * 180f);
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 캐릭터 방향에 따른 스프라이트 업데이트
    /// </summary>
    public void UpdateCharacterDirectionSprite(CharacterMovement movement)
    {
        if (character == null || spriteRenderer == null) return;
        
        // 전방/후방 스프라이트가 있으면 사용
        if (character.frontSprite != null || character.backSprite != null)
        {
            bool isFacingUp = false;
            
            // 이동 방향 확인
            if (movement != null && movement.GetCurrentWaypoint() != null)
            {
                Vector3 direction = (movement.GetCurrentWaypoint().position - transform.position).normalized;
                isFacingUp = direction.y > 0;
            }
            
            // 스프라이트 설정
            if (isFacingUp && character.backSprite != null)
            {
                spriteRenderer.sprite = character.backSprite;
            }
            else if (!isFacingUp && character.frontSprite != null)
            {
                spriteRenderer.sprite = character.frontSprite;
            }
            else if (character.characterSprite != null)
            {
                spriteRenderer.sprite = character.characterSprite;
            }
        }
        
        // 좌우 반전 처리
        if (movement != null && movement.GetCurrentWaypoint() != null)
        {
            Vector3 direction = (movement.GetCurrentWaypoint().position - transform.position).normalized;
            if (direction.x != 0)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }
    }
    
    /// <summary>
    /// 강화 효과 표시
    /// </summary>
    public void ShowUpgradeEffect()
    {
        StartCoroutine(UpgradeEffectCoroutine());
    }
    
    private IEnumerator UpgradeEffectCoroutine()
    {
        if (spriteRenderer == null) yield break;
        
        // 크기 증가 효과
        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;
        float duration = 0.3f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 크기 펄스
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
            transform.localScale = originalScale * scale;
            
            // 색상 펄스
            spriteRenderer.color = Color.Lerp(originalColor, Color.white, Mathf.Sin(t * Mathf.PI));
            
            yield return null;
        }
        
        transform.localScale = originalScale;
        spriteRenderer.color = originalColor;
    }
    
    /// <summary>
    /// 선택 효과 표시
    /// </summary>
    public void ShowSelectionEffect(bool show)
    {
        if (spriteRenderer == null) return;
        
        if (show)
        {
            // 외곽선 효과 (Material이 있다면)
            if (spriteRenderer.material != null && spriteRenderer.material.HasProperty("_OutlineWidth"))
            {
                spriteRenderer.material.SetFloat("_OutlineWidth", 2f);
                spriteRenderer.material.SetColor("_OutlineColor", Color.yellow);
            }
            else
            {
                // Material이 없으면 색상으로 표시
                spriteRenderer.color = Color.Lerp(originalColor, Color.yellow, 0.3f);
            }
        }
        else
        {
            // 효과 제거
            if (spriteRenderer.material != null && spriteRenderer.material.HasProperty("_OutlineWidth"))
            {
                spriteRenderer.material.SetFloat("_OutlineWidth", 0f);
            }
            else
            {
                spriteRenderer.color = originalColor;
            }
        }
    }
    
    /// <summary>
    /// 디버그용 정보 표시
    /// </summary>
    public void ShowDebugInfo(bool show)
    {
        if (show)
        {
            // 월드 공간에 텍스트 표시
            GameObject debugObj = new GameObject("DebugInfo");
            debugObj.transform.SetParent(transform);
            debugObj.transform.localPosition = Vector3.up * 2f;
            
            TMPro.TextMeshPro debugText = debugObj.AddComponent<TMPro.TextMeshPro>();
            debugText.text = $"{character.characterName}\nHP: {character.currentHP:F0}/{character.maxHP:F0}\nATK: {character.attackPower:F0}";
            debugText.fontSize = 2f;
            debugText.alignment = TMPro.TextAlignmentOptions.Center;
            debugText.color = Color.white;
            
            debugObj.AddComponent<LookAtCamera>();
        }
    }
}