using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using pjy.Characters;

namespace pjy.UI
{
    /// <summary>
    /// 보스 체력바 UI - 화면 상단에 표시되는 보스 전용 체력바
    /// </summary>
    public class BossHealthBarUI : MonoBehaviour
    {
        [Header("UI 요소")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider delayedHealthSlider; // 지연된 체력바 (피격 효과)
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI phaseText;
        
        [Header("페이즈 인디케이터")]
        [SerializeField] private GameObject[] phaseIndicators;
        [SerializeField] private Color activePhaseColor = Color.red;
        [SerializeField] private Color inactivePhaseColor = Color.gray;
        
        [Header("효과")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Gradient healthGradient;
        [SerializeField] private float delayedHealthSpeed = 2f;
        [SerializeField] private AnimationCurve shakeCurve;
        [SerializeField] private float shakeDuration = 0.3f;
        
        [Header("보스 아이콘")]
        [SerializeField] private Image bossIcon;
        [SerializeField] private Image bossFrame;
        [SerializeField] private Sprite[] bossIcons;
        
        [Header("버프/디버프 표시")]
        [SerializeField] private Transform buffContainer;
        [SerializeField] private GameObject buffIconPrefab;
        
        // 참조
        private BossMonster targetBoss;
        private RectTransform rectTransform;
        private Vector3 originalPosition;
        private float lastHealth;
        
        // 애니메이션
        private Coroutine shakeCoroutine;
        private Coroutine phaseTransitionCoroutine;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            originalPosition = rectTransform.anchoredPosition;
            
            if (healthSlider != null)
            {
                healthSlider.value = 1f;
            }
            
            if (delayedHealthSlider != null)
            {
                delayedHealthSlider.value = 1f;
            }
        }
        
        /// <summary>
        /// 보스 설정
        /// </summary>
        public void SetBoss(BossMonster boss)
        {
            targetBoss = boss;
            
            if (boss == null)
            {
                gameObject.SetActive(false);
                return;
            }
            
            gameObject.SetActive(true);
            
            // 보스 정보 설정
            if (bossNameText != null)
            {
                bossNameText.text = boss.name.Replace("(Clone)", "").Trim();
            }
            
            // 초기 체력 설정
            lastHealth = boss.health;
            UpdateHealthBar(boss.health, boss.health);
            
            // 페이즈 인디케이터 초기화
            UpdatePhaseIndicators(1);
            
            // 보스 아이콘 설정
            SetBossIcon(boss.chapterIndex);
            
            // 등장 애니메이션
            PlayAppearAnimation();
        }
        
        private void Update()
        {
            if (targetBoss == null)
            {
                // 보스가 죽었으면 UI 숨기기
                if (gameObject.activeSelf)
                {
                    PlayDisappearAnimation();
                }
                return;
            }
            
            // 체력 업데이트
            UpdateHealthBar(targetBoss.health, targetBoss.health);
            
            // 지연된 체력바 업데이트
            if (delayedHealthSlider != null)
            {
                float targetValue = targetBoss.health / targetBoss.health;
                delayedHealthSlider.value = Mathf.Lerp(delayedHealthSlider.value, targetValue, 
                    Time.deltaTime * delayedHealthSpeed);
            }
            
            // 피격 시 흔들림 효과
            if (targetBoss.health < lastHealth)
            {
                float damage = lastHealth - targetBoss.health;
                OnBossDamaged(damage);
                lastHealth = targetBoss.health;
            }
        }
        
        /// <summary>
        /// 체력바 업데이트
        /// </summary>
        private void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (healthSlider == null) return;
            
            float healthPercent = currentHealth / maxHealth;
            healthSlider.value = healthPercent;
            
            // 체력 텍스트
            if (healthText != null)
            {
                healthText.text = $"{Mathf.RoundToInt(currentHealth)} / {Mathf.RoundToInt(maxHealth)}";
            }
            
            // 체력 색상 변경
            if (healthBarFill != null && healthGradient != null)
            {
                healthBarFill.color = healthGradient.Evaluate(healthPercent);
            }
        }
        
        /// <summary>
        /// 페이즈 인디케이터 업데이트
        /// </summary>
        public void UpdatePhaseIndicators(int currentPhase)
        {
            if (phaseIndicators == null) return;
            
            for (int i = 0; i < phaseIndicators.Length; i++)
            {
                if (phaseIndicators[i] != null)
                {
                    Image indicator = phaseIndicators[i].GetComponent<Image>();
                    if (indicator != null)
                    {
                        indicator.color = (i < currentPhase) ? activePhaseColor : inactivePhaseColor;
                    }
                }
            }
            
            // 페이즈 텍스트
            if (phaseText != null)
            {
                phaseText.text = $"PHASE {currentPhase}";
            }
            
            // 페이즈 전환 효과
            if (currentPhase > 1)
            {
                PlayPhaseTransitionEffect(currentPhase);
            }
        }
        
        /// <summary>
        /// 보스 피격 효과
        /// </summary>
        private void OnBossDamaged(float damage)
        {
            // 흔들림 효과
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }
            shakeCoroutine = StartCoroutine(ShakeEffect());
            
            // 데미지 텍스트 (선택사항)
            // ShowDamageText(damage);
        }
        
        /// <summary>
        /// 흔들림 효과
        /// </summary>
        private IEnumerator ShakeEffect()
        {
            float elapsed = 0f;
            
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float strength = shakeCurve.Evaluate(elapsed / shakeDuration);
                
                Vector3 shake = new Vector3(
                    Random.Range(-1f, 1f) * strength * 10f,
                    Random.Range(-1f, 1f) * strength * 5f,
                    0f
                );
                
                rectTransform.anchoredPosition = originalPosition + shake;
                
                yield return null;
            }
            
            rectTransform.anchoredPosition = originalPosition;
        }
        
        /// <summary>
        /// 등장 애니메이션
        /// </summary>
        private void PlayAppearAnimation()
        {
            StartCoroutine(AppearAnimationCoroutine());
        }
        
        private IEnumerator AppearAnimationCoroutine()
        {
            // 위에서 아래로 슬라이드
            Vector3 startPos = originalPosition + Vector3.up * 100f;
            rectTransform.anchoredPosition = startPos;
            
            // 페이드 인
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = 1f - Mathf.Pow(1f - t, 3f); // Ease out back 근사
                
                rectTransform.anchoredPosition = Vector3.Lerp(startPos, originalPosition, t);
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                
                yield return null;
            }
            
            rectTransform.anchoredPosition = originalPosition;
            canvasGroup.alpha = 1f;
        }
        
        /// <summary>
        /// 사라짐 애니메이션
        /// </summary>
        private void PlayDisappearAnimation()
        {
            StartCoroutine(DisappearAnimationCoroutine());
        }
        
        private IEnumerator DisappearAnimationCoroutine()
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            Vector3 startPos = rectTransform.anchoredPosition;
            Vector3 endPos = originalPosition + Vector3.up * 50f;
            
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                
                yield return null;
            }
            
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 페이즈 전환 효과
        /// </summary>
        private void PlayPhaseTransitionEffect(int newPhase)
        {
            if (phaseTransitionCoroutine != null)
            {
                StopCoroutine(phaseTransitionCoroutine);
            }
            phaseTransitionCoroutine = StartCoroutine(PhaseTransitionEffect(newPhase));
        }
        
        /// <summary>
        /// 페이즈 전환 효과 코루틴
        /// </summary>
        private IEnumerator PhaseTransitionEffect(int newPhase)
        {
            // 화면 플래시
            if (phaseText != null)
            {
                // 텍스트 강조
                StartCoroutine(ScaleTextAnimation(phaseText.transform));
            }
            
            // 체력바 색상 변경
            if (healthBarFill != null)
            {
                Color originalColor = healthBarFill.color;
                healthBarFill.color = Color.white;
                
                yield return new WaitForSeconds(0.1f);
                
                healthBarFill.color = originalColor;
            }
            
            yield return null;
        }
        
        /// <summary>
        /// 텍스트 스케일 애니메이션
        /// </summary>
        private IEnumerator ScaleTextAnimation(Transform textTransform)
        {
            Vector3 originalScale = textTransform.localScale;
            Vector3 targetScale = originalScale * 2f;
            
            float duration = 0.5f;
            float elapsed = 0f;
            
            // 확대
            while (elapsed < duration * 0.3f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.3f);
                textTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }
            
            // 축소
            elapsed = 0f;
            while (elapsed < duration * 0.7f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.7f);
                t = 1f - Mathf.Pow(1f - t, 2f); // Ease out
                textTransform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }
            
            textTransform.localScale = originalScale;
        }
        
        /// <summary>
        /// 보스 아이콘 설정
        /// </summary>
        private void SetBossIcon(int chapterIndex)
        {
            if (bossIcon == null) return;
            
            // 챕터별 아이콘이 있으면 사용
            if (bossIcons != null && chapterIndex <= bossIcons.Length && bossIcons[chapterIndex - 1] != null)
            {
                bossIcon.sprite = bossIcons[chapterIndex - 1];
            }
            
            // 프레임 색상 변경 (선택사항)
            if (bossFrame != null)
            {
                // 챕터가 높을수록 붉은색
                float hue = Mathf.Lerp(0f, 0.1f, (float)chapterIndex / 10f);
                bossFrame.color = Color.HSVToRGB(hue, 0.8f, 1f);
            }
        }
        
        /// <summary>
        /// 버프 아이콘 추가
        /// </summary>
        public void AddBuffIcon(Sprite icon, string buffName, float duration = 0f)
        {
            if (buffContainer == null || buffIconPrefab == null) return;
            
            GameObject buffIcon = Instantiate(buffIconPrefab, buffContainer);
            Image buffImage = buffIcon.GetComponent<Image>();
            
            if (buffImage != null && icon != null)
            {
                buffImage.sprite = icon;
            }
            
            // 툴팁 설정
            var tooltip = buffIcon.AddComponent<SimpleTooltip>();
            tooltip.tooltipText = buffName;
            
            // 지속시간이 있으면 자동 제거
            if (duration > 0f)
            {
                Destroy(buffIcon, duration);
            }
        }
        
        /// <summary>
        /// 모든 버프 아이콘 제거
        /// </summary>
        public void ClearBuffIcons()
        {
            if (buffContainer == null) return;
            
            foreach (Transform child in buffContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 간단한 툴팁 컴포넌트
    /// </summary>
    public class SimpleTooltip : MonoBehaviour
    {
        public string tooltipText;
        
        private void OnMouseEnter()
        {
            // 툴팁 표시 로직
            Debug.Log($"[Tooltip] {tooltipText}");
        }
        
        private void OnMouseExit()
        {
            // 툴팁 숨기기 로직
        }
    }
}