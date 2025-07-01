using UnityEngine;
using UnityEngine.EventSystems;

namespace GuildMaster.UI
{
    public class DraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [Header("Settings")]
        [SerializeField] private bool constrainToScreen = true;
        [SerializeField] private bool returnToOriginalPosition = false;
        [SerializeField] private float dragAlpha = 0.8f;
        [SerializeField] private bool useSnapping = false;
        [SerializeField] private float snapGridSize = 10f;
        
        private RectTransform rectTransform;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private Vector2 originalPosition;
        private Vector2 dragOffset;
        private bool isDragging = false;
        
        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            // 드래그 우선순위를 위해 최상위로 이동
            transform.SetAsLastSibling();
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            originalPosition = rectTransform.anchoredPosition;
            
            // 드래그 오프셋 계산
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint
            );
            
            dragOffset = rectTransform.anchoredPosition - localPoint;
            
            // 드래그 중 투명도 조절
            canvasGroup.alpha = dragAlpha;
            
            // 드래그 중 레이캐스트 비활성화
            canvasGroup.blocksRaycasts = false;
            
            // 사운드 재생
            Systems.SoundSystem.Instance?.PlaySound("ui_drag_start");
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            
            // 새 위치 계산
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint
            );
            
            Vector2 newPosition = localPoint + dragOffset;
            
            // 스냅 적용
            if (useSnapping)
            {
                newPosition.x = Mathf.Round(newPosition.x / snapGridSize) * snapGridSize;
                newPosition.y = Mathf.Round(newPosition.y / snapGridSize) * snapGridSize;
            }
            
            rectTransform.anchoredPosition = newPosition;
            
            // 화면 제약
            if (constrainToScreen)
            {
                ClampToScreen();
            }
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            
            // 원래 투명도로 복원
            canvasGroup.alpha = 1f;
            
            // 레이캐스트 다시 활성화
            canvasGroup.blocksRaycasts = true;
            
            // 원래 위치로 돌아가기
            if (returnToOriginalPosition)
            {
                rectTransform.anchoredPosition = originalPosition;
            }
            
            // 드롭 이벤트 처리
            CheckDropTarget(eventData);
            
            // 사운드 재생
            Systems.SoundSystem.Instance?.PlaySound("ui_drag_end");
        }
        
        void ClampToScreen()
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            
            Vector2 min = corners[0];
            Vector2 max = corners[2];
            
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            
            Vector2 adjustment = Vector2.zero;
            
            if (max.x > screenWidth)
                adjustment.x = screenWidth - max.x;
            else if (min.x < 0)
                adjustment.x = -min.x;
                
            if (max.y > screenHeight)
                adjustment.y = screenHeight - max.y;
            else if (min.y < 0)
                adjustment.y = -min.y;
            
            if (adjustment != Vector2.zero)
            {
                rectTransform.anchoredPosition += adjustment;
            }
        }
        
        void CheckDropTarget(PointerEventData eventData)
        {
            // 드롭 타겟 확인
            GameObject dropTarget = eventData.pointerEnter;
            
            if (dropTarget != null)
            {
                IDropHandler dropHandler = dropTarget.GetComponent<IDropHandler>();
                if (dropHandler != null)
                {
                    dropHandler.OnDrop(eventData);
                }
            }
        }
        
        // 드래그 가능 여부 설정
        public void SetDraggable(bool draggable)
        {
            enabled = draggable;
        }
        
        // 위치 리셋
        public void ResetPosition()
        {
            rectTransform.anchoredPosition = originalPosition;
        }
        
        // 스냅 설정
        public void SetSnapping(bool enable, float gridSize = 10f)
        {
            useSnapping = enable;
            snapGridSize = gridSize;
        }
    }
    
    // 드롭 타겟 인터페이스
    public interface IDropHandler
    {
        void OnDrop(PointerEventData eventData);
    }
}