using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuildMaster.UI
{
    public class LoadingScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image loadingBarBackground;
        [SerializeField] private Image loadingBarFill;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private TextMeshProUGUI tipsText;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Loading Animation")]
        [SerializeField] private GameObject loadingSpinner;
        [SerializeField] private float spinSpeed = 180f;
        [SerializeField] private AnimationCurve loadingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Tips System")]
        [SerializeField] private float tipDisplayDuration = 3f;
        [SerializeField] private float tipFadeDuration = 0.5f;
        private string[] loadingTips;
        private Coroutine tipsCoroutine;
        
        [Header("Visual Settings")]
        [SerializeField] private Gradient loadingBarGradient;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        
        private float currentProgress = 0f;
        private float targetProgress = 0f;
        private float progressVelocity = 0f;
        
        void Awake()
        {
            InitializeTips();
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                StartCoroutine(FadeIn());
            }
        }
        
        void Start()
        {
            if (tipsText != null)
            {
                tipsCoroutine = StartCoroutine(DisplayTips());
            }
        }
        
        void Update()
        {
            // 로딩 바 부드러운 애니메이션
            if (currentProgress != targetProgress)
            {
                currentProgress = Mathf.SmoothDamp(currentProgress, targetProgress, ref progressVelocity, 0.3f);
                UpdateLoadingBar(currentProgress);
            }
            
            // 로딩 스피너 회전
            if (loadingSpinner != null && loadingSpinner.activeSelf)
            {
                loadingSpinner.transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);
            }
        }
        
        void InitializeTips()
        {
            loadingTips = new string[]
            {
                "모험가들의 직업 조합을 잘 구성하면 전투에서 유리합니다.",
                "길드 시설을 업그레이드하면 더 강력한 모험가를 모집할 수 있습니다.",
                "일일 퀘스트를 완료하면 추가 보상을 획득할 수 있습니다.",
                "탱커는 전열에, 딜러와 힐러는 후열에 배치하세요.",
                "연구를 통해 길드의 능력을 영구적으로 강화할 수 있습니다.",
                "영토를 점령하면 지속적인 자원 수입을 얻을 수 있습니다.",
                "각성한 모험가는 새로운 스킬을 배울 수 있습니다.",
                "길드 레벨이 오르면 더 많은 콘텐츠가 해금됩니다.",
                "자동 전투 기능을 활용하여 효율적으로 플레이하세요.",
                "시즌 패스 보상을 놓치지 마세요!",
                "NPC와의 거래로 희귀한 아이템을 획득할 수 있습니다.",
                "전략적인 부대 편성이 승리의 열쇠입니다.",
                "건물 배치에 따라 시너지 효과가 발생합니다.",
                "무한 던전에서는 더 깊이 들어갈수록 보상이 좋아집니다.",
                "업적을 달성하면 다양한 보상을 받을 수 있습니다."
            };
        }
        
        public void UpdateProgress(float progress, string statusText = null)
        {
            targetProgress = Mathf.Clamp01(progress);
            
            if (!string.IsNullOrEmpty(statusText) && loadingText != null)
            {
                loadingText.text = statusText;
            }
        }
        
        void UpdateLoadingBar(float progress)
        {
            if (loadingBarFill != null)
            {
                loadingBarFill.fillAmount = loadingCurve.Evaluate(progress);
                
                // 그라디언트 색상 적용
                if (loadingBarGradient != null)
                {
                    loadingBarFill.color = loadingBarGradient.Evaluate(progress);
                }
            }
            
            // 100% 도달 시 스피너 숨기기
            if (progress >= 0.99f && loadingSpinner != null)
            {
                loadingSpinner.SetActive(false);
            }
        }
        
        IEnumerator DisplayTips()
        {
            while (true)
            {
                // 랜덤 팁 선택
                string tip = loadingTips[UnityEngine.Random.Range(0, loadingTips.Length)];
                
                // 페이드 인
                yield return FadeText(tipsText, 0f, 1f, tipFadeDuration);
                tipsText.text = tip;
                
                // 표시 유지
                yield return new WaitForSeconds(tipDisplayDuration);
                
                // 페이드 아웃
                yield return FadeText(tipsText, 1f, 0f, tipFadeDuration);
            }
        }
        
        IEnumerator FadeText(TextMeshProUGUI text, float startAlpha, float endAlpha, float duration)
        {
            if (text == null) yield break;
            
            float elapsed = 0f;
            Color textColor = text.color;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                textColor.a = Mathf.Lerp(startAlpha, endAlpha, t);
                text.color = textColor;
                yield return null;
            }
            
            textColor.a = endAlpha;
            text.color = textColor;
        }
        
        IEnumerator FadeIn()
        {
            float elapsed = 0f;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = elapsed / fadeInDuration;
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
        
        public void FadeOut(Action onComplete = null)
        {
            StartCoroutine(FadeOutCoroutine(onComplete));
        }
        
        IEnumerator FadeOutCoroutine(Action onComplete)
        {
            if (tipsCoroutine != null)
            {
                StopCoroutine(tipsCoroutine);
            }
            
            if (canvasGroup != null)
            {
                float elapsed = 0f;
                
                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = 1f - (elapsed / fadeOutDuration);
                    yield return null;
                }
                
                canvasGroup.alpha = 0f;
            }
            
            onComplete?.Invoke();
        }
        
        // 특별 로딩 화면 효과
        public void ShowSpecialLoadingScreen(string specialEvent)
        {
            switch (specialEvent)
            {
                case "boss_battle":
                    if (backgroundImage != null)
                    {
                        // 보스 전투 특별 배경
                        backgroundImage.color = new Color(0.5f, 0f, 0f, 1f);
                    }
                    loadingText.text = "강력한 적과의 전투를 준비하는 중...";
                    break;
                    
                case "season_change":
                    // 시즌 변경 효과
                    StartCoroutine(SeasonChangeEffect());
                    break;
                    
                case "major_update":
                    // 대규모 업데이트 효과
                    loadingText.text = "새로운 콘텐츠를 준비하는 중...";
                    break;
            }
        }
        
        IEnumerator SeasonChangeEffect()
        {
            if (backgroundImage == null) yield break;
            
            Color[] seasonColors = new Color[]
            {
                new Color(0.3f, 0.8f, 0.3f, 1f), // 봄 - 초록
                new Color(1f, 0.8f, 0.2f, 1f),   // 여름 - 노랑
                new Color(0.8f, 0.4f, 0.2f, 1f), // 가을 - 주황
                new Color(0.6f, 0.8f, 1f, 1f)    // 겨울 - 하늘색
            };
            
            int currentSeason = (int)(Time.time / 10f) % 4;
            Color targetColor = seasonColors[currentSeason];
            
            float duration = 2f;
            float elapsed = 0f;
            Color startColor = backgroundImage.color;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                backgroundImage.color = Color.Lerp(startColor, targetColor, elapsed / duration);
                yield return null;
            }
        }
    }
}