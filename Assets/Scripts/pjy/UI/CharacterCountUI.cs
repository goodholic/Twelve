using UnityEngine;
using TMPro;

namespace pjy.UI
{
    /// <summary>
    /// 게임 기획서 요구사항: 캐릭터 소환 제한 (50마리) UI 표시
    /// 현재 소환된 캐릭터 수와 최대 제한을 실시간으로 표시
    /// </summary>
    public class CharacterCountUI : MonoBehaviour
    {
        [Header("UI 요소")]
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI opponentCountText;
        
        [Header("UI 설정")]
        [SerializeField] private string playerCountFormat = "캐릭터: {0}/{1}";
        [SerializeField] private string opponentCountFormat = "상대: {0}/{1}";
        
        [Header("색상 설정")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0f); // 노란색
        [SerializeField] private Color maxColor = new Color(1f, 0.3f, 0.3f); // 빨간색
        
        [Header("경고 임계값")]
        [SerializeField] private float warningThreshold = 0.8f; // 80% 이상일 때 경고
        
        private PlacementManager placementManager;
        
        private void Awake()
        {
            // PlacementManager 찾기
            placementManager = PlacementManager.Instance;
            if (placementManager == null)
            {
                placementManager = FindFirstObjectByType<PlacementManager>();
            }
            
            // UI 요소 검증
            if (playerCountText == null)
            {
                Debug.LogWarning("[CharacterCountUI] playerCountText가 설정되지 않았습니다!");
            }
            
            if (opponentCountText == null)
            {
                Debug.LogWarning("[CharacterCountUI] opponentCountText가 설정되지 않았습니다!");
            }
        }
        
        private void Start()
        {
            // 초기 업데이트
            UpdateCharacterCounts();
        }
        
        private void Update()
        {
            // 매 프레임 카운트 업데이트
            UpdateCharacterCounts();
        }
        
        /// <summary>
        /// 캐릭터 수 UI 업데이트
        /// </summary>
        private void UpdateCharacterCounts()
        {
            if (placementManager == null) return;
            
            // 플레이어 캐릭터 수 업데이트
            if (playerCountText != null)
            {
                int currentCount = placementManager.GetCharacterCount(false);
                int maxCount = 50; // 게임 기획서 요구사항: 최대 50마리
                
                // 텍스트 업데이트
                playerCountText.text = string.Format(playerCountFormat, currentCount, maxCount);
                
                // 색상 업데이트
                UpdateTextColor(playerCountText, currentCount, maxCount);
            }
            
            // 상대 캐릭터 수 업데이트
            if (opponentCountText != null)
            {
                int currentCount = placementManager.GetCharacterCount(true);
                int maxCount = 50; // 게임 기획서 요구사항: 최대 50마리
                
                // 텍스트 업데이트
                opponentCountText.text = string.Format(opponentCountFormat, currentCount, maxCount);
                
                // 색상 업데이트
                UpdateTextColor(opponentCountText, currentCount, maxCount);
            }
        }
        
        /// <summary>
        /// 캐릭터 수에 따른 텍스트 색상 업데이트
        /// </summary>
        private void UpdateTextColor(TextMeshProUGUI text, int currentCount, int maxCount)
        {
            float ratio = (float)currentCount / maxCount;
            
            if (currentCount >= maxCount)
            {
                // 최대치 도달: 빨간색
                text.color = maxColor;
            }
            else if (ratio >= warningThreshold)
            {
                // 경고 임계값 이상: 노란색
                text.color = warningColor;
            }
            else
            {
                // 정상: 기본 색상
                text.color = normalColor;
            }
        }
        
        /// <summary>
        /// 캐릭터 소환 가능 여부 확인 (외부에서 호출 가능)
        /// </summary>
        public bool CanSummonCharacter(bool isOpponent)
        {
            if (placementManager == null) return false;
            return placementManager.CanSummonCharacter(isOpponent);
        }
        
        /// <summary>
        /// 소환 제한 도달 시 경고 메시지 표시
        /// </summary>
        public void ShowMaxLimitWarning(bool isOpponent)
        {
            string message = isOpponent ? 
                "상대방이 캐릭터 소환 제한(50마리)에 도달했습니다!" : 
                "캐릭터 소환 제한(50마리)에 도달했습니다!";
                
            Debug.LogWarning($"[CharacterCountUI] {message}");
            
            // TODO: 화면에 팝업 메시지 표시
            // 예: UIManager.Instance.ShowPopup(message);
        }
    }
}