using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuildMaster.UI
{
    public class ConfirmDialog : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private GameObject backgroundOverlay;
        
        [Header("Button Texts")]
        [SerializeField] private TextMeshProUGUI confirmButtonText;
        [SerializeField] private TextMeshProUGUI cancelButtonText;
        
        [Header("Animation")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private Action onConfirmAction;
        private Action onCancelAction;
        
        void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
                
            if (backgroundOverlay != null)
            {
                Button bgButton = backgroundOverlay.GetComponent<Button>();
                if (bgButton == null)
                    bgButton = backgroundOverlay.AddComponent<Button>();
                    
                bgButton.onClick.AddListener(OnCancel);
            }
        }
        
        public void Setup(string title, string message, Action onConfirm, Action onCancel = null, 
                         string confirmText = "확인", string cancelText = "취소")
        {
            // 텍스트 설정
            if (titleText != null)
                titleText.text = title;
                
            if (messageText != null)
                messageText.text = message;
                
            if (confirmButtonText != null)
                confirmButtonText.text = confirmText;
                
            if (cancelButtonText != null)
                cancelButtonText.text = cancelText;
            
            // 액션 저장
            onConfirmAction = onConfirm;
            onCancelAction = onCancel;
            
            // 버튼 리스너 설정
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(OnConfirm);
            }
            
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(OnCancel);
                
                // Cancel 액션이 없으면 취소 버튼 숨기기
                cancelButton.gameObject.SetActive(onCancel != null);
            }
            
            // 애니메이션 시작
            StartCoroutine(ShowAnimation());
        }
        
        void OnConfirm()
        {
            // 사운드 재생
            Systems.SoundSystem.Instance?.PlaySound("ui_click");
            
            // 애니메이션 후 액션 실행
            StartCoroutine(HideAnimation(() =>
            {
                onConfirmAction?.Invoke();
            }));
        }
        
        void OnCancel()
        {
            // 사운드 재생
            Systems.SoundSystem.Instance?.PlaySound("ui_click");
            
            // 애니메이션 후 액션 실행
            StartCoroutine(HideAnimation(() =>
            {
                onCancelAction?.Invoke();
            }));
        }
        
        System.Collections.IEnumerator ShowAnimation()
        {
            // 초기 설정
            transform.localScale = Vector3.zero;
            canvasGroup.alpha = 0;
            
            float elapsed = 0;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                
                // 스케일 애니메이션
                float scaleT = scaleCurve.Evaluate(t);
                transform.localScale = Vector3.one * scaleT;
                
                // 페이드 인
                canvasGroup.alpha = t;
                
                yield return null;
            }
            
            transform.localScale = Vector3.one;
            canvasGroup.alpha = 1;
        }
        
        System.Collections.IEnumerator HideAnimation(Action onComplete)
        {
            float elapsed = 0;
            Vector3 startScale = transform.localScale;
            
            while (elapsed < animationDuration * 0.5f) // 숨기는 애니메이션은 더 빠르게
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (animationDuration * 0.5f);
                
                // 스케일 애니메이션
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                
                // 페이드 아웃
                canvasGroup.alpha = 1 - t;
                
                yield return null;
            }
            
            onComplete?.Invoke();
            Destroy(gameObject);
        }
        
        // 특별한 다이얼로그 프리셋
        public static void ShowYesNoDialog(string title, string message, Action onYes, Action onNo = null)
        {
            UIManager.Instance?.ShowConfirmDialog(title, message, onYes, onNo);
        }
        
        public static void ShowOkDialog(string title, string message, Action onOk = null)
        {
            UIManager.Instance?.ShowConfirmDialog(title, message, onOk ?? (() => { }), null);
        }
        
        public static void ShowErrorDialog(string message, Action onOk = null)
        {
            UIManager.Instance?.ShowConfirmDialog("오류", message, onOk ?? (() => { }), null);
        }
    }
}