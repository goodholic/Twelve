using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuildMaster.UI
{
    public class TooltipUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private LayoutElement layoutElement;
        
        [Header("Settings")]
        [SerializeField] private float maxWidth = 300f;
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float fadeOutDuration = 0.1f;
        [SerializeField] private Vector2 offset = new Vector2(10, 10);
        [SerializeField] private bool followMouse = true;
        
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private bool isShowing = false;
        private Coroutine fadeCoroutine;
        
        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
            
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
                
            if (layoutElement == null)
                layoutElement = GetComponent<LayoutElement>();
                
            if (layoutElement != null)
                layoutElement.preferredWidth = maxWidth;
            
            // 초기 상태
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }
        
        public void Show(string content, Vector3 position)
        {
            if (contentText != null)
                contentText.text = content;
            
            gameObject.SetActive(true);
            isShowing = true;
            
            // 위치 설정
            UpdatePosition(position);
            
            // 페이드 인
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
                
            fadeCoroutine = StartCoroutine(FadeIn());
        }
        
        public void Hide()
        {
            if (!isShowing) return;
            
            isShowing = false;
            
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
                
            fadeCoroutine = StartCoroutine(FadeOut());
        }
        
        void Update()
        {
            if (isShowing && followMouse)
            {
                UpdatePosition(Input.mousePosition);
            }
        }
        
        void UpdatePosition(Vector3 position)
        {
            // 스크린 좌표를 Canvas 좌표로 변환
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                position,
                parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
                out localPoint
            );
            
            rectTransform.anchoredPosition = localPoint + offset;
            
            // 화면 밖으로 나가지 않도록 조정
            ClampToScreen();
        }
        
        void ClampToScreen()
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            
            float minX = corners[0].x;
            float maxX = corners[2].x;
            float minY = corners[0].y;
            float maxY = corners[2].y;
            
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            
            Vector2 adjustment = Vector2.zero;
            
            if (maxX > screenWidth)
                adjustment.x = screenWidth - maxX - 10;
            else if (minX < 0)
                adjustment.x = -minX + 10;
                
            if (maxY > screenHeight)
                adjustment.y = screenHeight - maxY - 10;
            else if (minY < 0)
                adjustment.y = -minY + 10;
            
            if (adjustment != Vector2.zero)
            {
                rectTransform.anchoredPosition += adjustment;
            }
        }
        
        IEnumerator FadeIn()
        {
            float elapsed = 0;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = elapsed / fadeInDuration;
                yield return null;
            }
            
            canvasGroup.alpha = 1;
        }
        
        IEnumerator FadeOut()
        {
            float elapsed = 0;
            float startAlpha = canvasGroup.alpha;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0, elapsed / fadeOutDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }
        
        // 특별한 툴팁 형식
        public void ShowItemTooltip(string itemName, string description, int value, string rarity)
        {
            string content = $"<b>{itemName}</b>\n";
            content += $"<color=#{GetRarityColor(rarity)}>{rarity}</color>\n\n";
            content += $"{description}\n\n";
            content += $"가치: {value} 골드";
            
            Show(content, Input.mousePosition);
        }
        
        public void ShowSkillTooltip(string skillName, string description, int manaCost, float cooldown)
        {
            string content = $"<b>{skillName}</b>\n\n";
            content += $"{description}\n\n";
            content += $"마나 소모: <color=#3498db>{manaCost}</color>\n";
            content += $"재사용 대기시간: <color=#e74c3c>{cooldown:F1}초</color>";
            
            Show(content, Input.mousePosition);
        }
        
        public void ShowUnitTooltip(Battle.Unit unit)
        {
            string content = $"<b>{unit.unitName}</b>\n";
            content += $"Lv.{unit.level} {unit.jobClass}\n\n";
            content += $"HP: {unit.currentHP:F0}/{unit.maxHP:F0}\n";
            content += $"공격력: {unit.attack:F0}\n";
            content += $"방어력: {unit.defense:F0}\n";
            content += $"속도: {unit.speed:F0}";
            
            Show(content, Input.mousePosition);
        }
        
        string GetRarityColor(string rarity)
        {
            switch (rarity.ToLower())
            {
                case "common": return "95a5a6";
                case "uncommon": return "2ecc71";
                case "rare": return "3498db";
                case "epic": return "9b59b6";
                case "legendary": return "f39c12";
                default: return "ffffff";
            }
        }
    }
}