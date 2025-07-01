using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuildMaster.UI
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
    
    public class NotificationUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float stayDuration = 2f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Type Colors")]
        [SerializeField] private Color infoColor = new Color(0.2f, 0.6f, 1f, 0.9f);
        [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);
        [SerializeField] private Color warningColor = new Color(1f, 0.7f, 0.1f, 0.9f);
        [SerializeField] private Color errorColor = new Color(1f, 0.2f, 0.2f, 0.9f);
        
        [Header("Type Icons")]
        [SerializeField] private Sprite infoIcon;
        [SerializeField] private Sprite successIcon;
        [SerializeField] private Sprite warningIcon;
        [SerializeField] private Sprite errorIcon;
        
        private RectTransform rectTransform;
        private float lifetime;
        
        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
                
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        public void Setup(string message, NotificationType type, float duration)
        {
            lifetime = duration;
            
            // 메시지 설정
            if (messageText != null)
                messageText.text = message;
            
            // 타입별 설정
            SetupAppearance(type);
            
            // 애니메이션 시작
            StartCoroutine(NotificationAnimation());
        }
        
        void SetupAppearance(NotificationType type)
        {
            Color bgColor = infoColor;
            Sprite icon = infoIcon;
            
            switch (type)
            {
                case NotificationType.Success:
                    bgColor = successColor;
                    icon = successIcon;
                    break;
                    
                case NotificationType.Warning:
                    bgColor = warningColor;
                    icon = warningIcon;
                    break;
                    
                case NotificationType.Error:
                    bgColor = errorColor;
                    icon = errorIcon;
                    break;
            }
            
            if (backgroundImage != null)
                backgroundImage.color = bgColor;
                
            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(true);
            }
            else if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }
        }
        
        IEnumerator NotificationAnimation()
        {
            // 초기 설정
            canvasGroup.alpha = 0;
            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 targetPos = startPos;
            startPos.x += 300; // 오른쪽에서 슬라이드
            rectTransform.anchoredPosition = startPos;
            
            // Fade In + Slide In
            float elapsed = 0;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                
                canvasGroup.alpha = t;
                float slideT = slideCurve.Evaluate(t);
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, slideT);
                
                yield return null;
            }
            
            canvasGroup.alpha = 1;
            rectTransform.anchoredPosition = targetPos;
            
            // Stay
            yield return new WaitForSeconds(lifetime);
            
            // Fade Out
            elapsed = 0;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                
                canvasGroup.alpha = 1 - t;
                
                yield return null;
            }
            
            Destroy(gameObject);
        }
        
        public void OnClick()
        {
            // 클릭 시 즉시 제거
            StopAllCoroutines();
            StartCoroutine(QuickFadeOut());
        }
        
        IEnumerator QuickFadeOut()
        {
            float elapsed = 0;
            float startAlpha = canvasGroup.alpha;
            
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0, elapsed / 0.2f);
                yield return null;
            }
            
            Destroy(gameObject);
        }
    }
}