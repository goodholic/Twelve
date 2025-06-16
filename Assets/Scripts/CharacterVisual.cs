using UnityEngine;
using System.Collections;

/// <summary>
/// 캐릭터 시각적 표현 관리 컴포넌트
/// 게임 기획서: 2.5등신 SD 캐주얼 스타일
/// </summary>
public class CharacterVisual : MonoBehaviour
{
    private Character character;
    private SpriteRenderer spriteRenderer;
    
    [Header("애니메이션 설정")]
    public float attackAnimationDuration = 0.3f;
    public AnimationCurve attackAnimationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    
    [Header("이펙트")]
    public GameObject attackEffect;
    public GameObject hitEffect;
    public GameObject deathEffect;
    
    // 애니메이션 상태
    private bool isPlayingAnimation = false;
    private Coroutine currentAnimationCoroutine;
    
    /// <summary>
    /// 시각적 시스템 초기화
    /// </summary>
    public void Initialize(Character character, SpriteRenderer spriteRenderer)
    {
        this.character = character;
        this.spriteRenderer = spriteRenderer;
        
        // 기본 스프라이트 설정
        if (spriteRenderer != null && character.characterSprite != null)
        {
            spriteRenderer.sprite = character.characterSprite;
        }
        
        Debug.Log($"[CharacterVisual] {character.characterName} 시각적 시스템 초기화");
    }
    
    /// <summary>
    /// HP바 업데이트
    /// </summary>
    public void UpdateHpBar()
    {
        if (character != null)
        {
            character.UpdateHPBar();
        }
    }
    
    /// <summary>
    /// 공격 애니메이션 재생
    /// </summary>
    public void PlayAttackAnimation()
    {
        if (isPlayingAnimation) return;
        
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
        }
        
        currentAnimationCoroutine = StartCoroutine(AttackAnimationCoroutine());
    }
    
    /// <summary>
    /// 공격 애니메이션 코루틴
    /// </summary>
    private IEnumerator AttackAnimationCoroutine()
    {
        isPlayingAnimation = true;
        
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        
        // 공격 이펙트 재생
        if (attackEffect != null)
        {
            GameObject effect = Instantiate(attackEffect, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(effect, 1f);
        }
        
        // 스케일 애니메이션
        float elapsed = 0f;
        while (elapsed < attackAnimationDuration)
        {
            float t = elapsed / attackAnimationDuration;
            float curveValue = attackAnimationCurve.Evaluate(t);
            
            transform.localScale = Vector3.Lerp(originalScale, targetScale, curveValue);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = originalScale;
        isPlayingAnimation = false;
    }
    
    /// <summary>
    /// 피격 애니메이션 재생
    /// </summary>
    public void PlayHitAnimation()
    {
        StartCoroutine(HitAnimationCoroutine());
    }
    
    /// <summary>
    /// 피격 애니메이션 코루틴
    /// </summary>
    private IEnumerator HitAnimationCoroutine()
    {
        // 피격 이펙트 재생
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
        
        // 색상 변경 애니메이션
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            
            yield return new WaitForSeconds(0.1f);
            
            spriteRenderer.color = originalColor;
        }
    }
    
    /// <summary>
    /// 사망 애니메이션 재생
    /// </summary>
    public void PlayDeathAnimation()
    {
        StartCoroutine(DeathAnimationCoroutine());
    }
    
    /// <summary>
    /// 사망 애니메이션 코루틴
    /// </summary>
    private IEnumerator DeathAnimationCoroutine()
    {
        // 사망 이펙트 재생
        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // 페이드 아웃 애니메이션
        if (spriteRenderer != null)
        {
            float elapsed = 0f;
            float duration = 0.5f;
            Color originalColor = spriteRenderer.color;
            
            while (elapsed < duration)
            {
                float alpha = 1f - (elapsed / duration);
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
    
    /// <summary>
    /// 캐릭터 방향에 따른 스프라이트 업데이트
    /// </summary>
    public void UpdateCharacterDirectionSprite(CharacterMovement movement)
    {
        if (spriteRenderer == null || character == null) return;
        
        // 이동 방향에 따른 스프라이트 변경
        Transform currentWaypoint = movement.GetCurrentWaypoint();
        if (currentWaypoint != null)
        {
            Vector3 direction = (currentWaypoint.position - transform.position).normalized;
            
            // 앞/뒤 스프라이트 변경
            if (direction.y > 0.1f && character.backSprite != null)
            {
                spriteRenderer.sprite = character.backSprite;
            }
            else if (direction.y < -0.1f && character.frontSprite != null)
            {
                spriteRenderer.sprite = character.frontSprite;
            }
            else
            {
                // 기본 스프라이트
                if (character.characterSprite != null)
                {
                    spriteRenderer.sprite = character.characterSprite;
                }
            }
            
            // 좌우 반전
            if (direction.x > 0.1f)
            {
                spriteRenderer.flipX = false;
            }
            else if (direction.x < -0.1f)
            {
                spriteRenderer.flipX = true;
            }
        }
    }
    
    /// <summary>
    /// 선택 효과 표시
    /// </summary>
    public void ShowSelectionEffect(bool show)
    {
        // 선택 시 외곽선 효과 (구현 필요)
        if (show)
        {
            // 외곽선 쉐이더 적용 또는 선택 이펙트 표시
            Debug.Log($"[CharacterVisual] {character.characterName} 선택됨");
        }
        else
        {
            // 선택 해제
            Debug.Log($"[CharacterVisual] {character.characterName} 선택 해제");
        }
    }
    
    /// <summary>
    /// 종족별 시각 효과
    /// </summary>
    public void ApplyRaceVisualEffect()
    {
        // 종족별 특수 시각 효과 적용
        switch (character.race)
        {
            case CharacterRace.Human:
                // 휴먼 시각 효과
                break;
            case CharacterRace.Orc:
                // 오크 시각 효과
                break;
            case CharacterRace.Elf:
                // 엘프 시각 효과
                break;
        }
    }
    
    /// <summary>
    /// 별 등급에 따른 시각적 효과 적용
    /// </summary>
    public void ApplyStarVisual(CharacterStar star)
    {
        // 별 등급에 따른 시각적 효과
        switch (star)
        {
            case CharacterStar.OneStar:
                // 1성 효과 (기본)
                break;
            case CharacterStar.TwoStar:
                // 2성 효과 (약간의 글로우)
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
                }
                break;
            case CharacterStar.ThreeStar:
                // 3성 효과 (강한 글로우)
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(1.2f, 1.2f, 1f, 1f);
                }
                break;
        }
    }
}