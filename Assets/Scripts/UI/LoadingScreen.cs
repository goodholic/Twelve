using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace GuildMaster.UI
{
    public class LoadingScreen : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI versionText;
        
        [Header("Animation")]
        [SerializeField] private GameObject loadingIcon;
        [SerializeField] private float rotationSpeed = 180f;
        
        private float currentProgress = 0f;
        private string currentStatus = "";
        
        void Start()
        {
            Initialize();
        }
        
        void Update()
        {
            // 로딩 아이콘 회전
            if (loadingIcon != null)
            {
                loadingIcon.transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
            }
        }
        
        void Initialize()
        {
            if (progressBar != null)
                progressBar.fillAmount = 0f;
                
            if (progressText != null)
                progressText.text = "0%";
                
            if (statusText != null)
                statusText.text = "Loading...";
        }
        
        public void UpdateProgress(float progress, string status = "")
        {
            currentProgress = Mathf.Clamp01(progress);
            currentStatus = status;
            
            if (progressBar != null)
                progressBar.fillAmount = currentProgress;
                
            if (progressText != null)
                progressText.text = $"{Mathf.RoundToInt(currentProgress * 100)}%";
                
            if (statusText != null && !string.IsNullOrEmpty(status))
                statusText.text = status;
        }
        
        public void SetStatus(string status)
        {
            currentStatus = status;
            if (statusText != null)
                statusText.text = status;
        }
        
        public void SetVersion(string version)
        {
            if (versionText != null)
                versionText.text = version;
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        public void FadeOut()
        {
            // 페이드 아웃 애니메이션
            StartCoroutine(FadeOutCoroutine());
        }
        
        private IEnumerator FadeOutCoroutine()
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            float duration = 0.5f;
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
} 