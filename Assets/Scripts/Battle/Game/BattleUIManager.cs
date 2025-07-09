using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 전투 UI 관리자
    /// </summary>
    public class BattleUIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject placementPanel;
        [SerializeField] private GameObject battlePanel;
        [SerializeField] private GameObject resultPanel;
        
        [Header("Battle Info")]
        [SerializeField] private TextMeshProUGUI turnIndicatorText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI phaseText;
        
        [Header("Character Info")]
        [SerializeField] private GameObject characterInfoPanel;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI characterJobText;
        [SerializeField] private TextMeshProUGUI characterStatsText;
        [SerializeField] private Image characterToPlaceImage;
        
        [Header("Unit Info")]
        [SerializeField] private GameObject unitInfoPanel;
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI unitHPText;
        [SerializeField] private TextMeshProUGUI unitStatsText;
        
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI boardAScoreText;
        [SerializeField] private TextMeshProUGUI boardBScoreText;
        
        [Header("Result Display")]
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private Button returnToMenuButton;
        
        [Header("Damage Text")]
        [SerializeField] private GameObject damageTextPrefab;
        [SerializeField] private Transform damageTextContainer;
        
        private void Awake()
        {
            if (returnToMenuButton != null)
                returnToMenuButton.onClick.AddListener(ReturnToMenu);
        }
        
        public void ShowPlacementUI()
        {
            HideAllPanels();
            if (placementPanel != null)
                placementPanel.SetActive(true);
            
            UpdatePhaseText("배치 페이즈");
        }
        
        public void ShowBattleUI()
        {
            HideAllPanels();
            if (battlePanel != null)
                battlePanel.SetActive(true);
            
            UpdatePhaseText("전투 페이즈");
        }
        
        public void ShowCharacterToPlace(Character character)
        {
            if (characterInfoPanel != null)
                characterInfoPanel.SetActive(true);
            
            if (characterNameText != null)
                characterNameText.text = character.characterName;
            
            if (characterJobText != null)
            {
                characterJobText.text = JobClassSystem.GetJobClassName(character.jobClass);
                characterJobText.color = JobClassSystem.GetJobColor(character.jobClass);
            }
            
            if (characterStatsText != null)
            {
                var stats = character.CalculateFinalStats();
                characterStatsText.text = $"HP: {stats.maxHP:F0}\n" +
                                        $"공격력: {stats.attack:F0}\n" +
                                        $"방어력: {stats.defense:F0}\n" +
                                        $"속도: {stats.speed:F0}";
            }
            
            // 캐릭터 이미지 표시 (실제로는 스프라이트 사용)
            if (characterToPlaceImage != null)
                characterToPlaceImage.color = JobClassSystem.GetJobColor(character.jobClass);
        }
        
        public void ShowUnitInfo(CharacterUnit unit)
        {
            if (unitInfoPanel != null)
                unitInfoPanel.SetActive(true);
            
            if (unitNameText != null)
                unitNameText.text = unit.unitName;
            
            if (unitHPText != null)
                unitHPText.text = $"HP: {unit.currentHP:F0}/{unit.maxHP:F0}";
            
            if (unitStatsText != null)
            {
                unitStatsText.text = $"공격력: {unit.attack:F0}\n" +
                                   $"방어력: {unit.defense:F0}\n" +
                                   $"속도: {unit.speed:F0}\n" +
                                   $"크리티컬: {unit.critRate:P0}";
            }
        }
        
        public void UpdateTurnIndicator(string turnText)
        {
            if (turnIndicatorText != null)
                turnIndicatorText.text = turnText;
        }
        
        public void UpdateTimer(float timeRemaining)
        {
            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(timeRemaining);
                timerText.text = $"남은 시간: {seconds}초";
                
                // 시간이 적을 때 색상 변경
                if (seconds <= 10)
                    timerText.color = Color.red;
                else if (seconds <= 20)
                    timerText.color = Color.yellow;
                else
                    timerText.color = Color.white;
            }
        }
        
        public void UpdatePhaseText(string phase)
        {
            if (phaseText != null)
                phaseText.text = phase;
        }
        
        public void UpdateBoardScores(int playerScoreA, int enemyScoreA, int playerScoreB, int enemyScoreB)
        {
            if (boardAScoreText != null)
                boardAScoreText.text = $"보드 A - 아군: {playerScoreA} / 적군: {enemyScoreA}";
            
            if (boardBScoreText != null)
                boardBScoreText.text = $"보드 B - 아군: {playerScoreB} / 적군: {enemyScoreB}";
        }
        
        public void ShowDamageText(Vector3 worldPosition, float damage)
        {
            if (damageTextPrefab == null) return;
            
            GameObject damageObj = Instantiate(damageTextPrefab, damageTextContainer);
            TextMeshProUGUI damageText = damageObj.GetComponent<TextMeshProUGUI>();
            
            if (damageText == null)
            {
                damageText = damageObj.AddComponent<TextMeshProUGUI>();
            }
            
            damageText.text = damage.ToString("F0");
            damageText.fontSize = 24;
            damageText.color = Color.red;
            
            // 월드 좌표를 스크린 좌표로 변환
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
            damageObj.transform.position = screenPos;
            
            // 데미지 텍스트 애니메이션
            StartCoroutine(AnimateDamageText(damageObj));
        }
        
        private IEnumerator AnimateDamageText(GameObject damageObj)
        {
            float duration = 1.5f;
            float elapsed = 0;
            Vector3 startPos = damageObj.transform.position;
            TextMeshProUGUI text = damageObj.GetComponent<TextMeshProUGUI>();
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 위로 올라가면서 페이드 아웃
                damageObj.transform.position = startPos + Vector3.up * (50f * t);
                
                if (text != null)
                {
                    Color color = text.color;
                    color.a = 1f - t;
                    text.color = color;
                }
                
                yield return null;
            }
            
            Destroy(damageObj);
        }
        
        public void ShowBattleResult(string result, int score)
        {
            HideAllPanels();
            if (resultPanel != null)
                resultPanel.SetActive(true);
            
            if (resultText != null)
                resultText.text = result;
            
            if (finalScoreText != null)
            {
                string scoreDetail = "";
                switch (score)
                {
                    case 2:
                        scoreDetail = "완벽한 승리!\n두 보드 모두 점령했습니다.";
                        break;
                    case 1:
                        scoreDetail = "무승부\n한 보드만 점령했습니다.";
                        break;
                    case 0:
                        scoreDetail = "패배...\n어떤 보드도 점령하지 못했습니다.";
                        break;
                }
                finalScoreText.text = scoreDetail;
            }
        }
        
        private void HideAllPanels()
        {
            if (placementPanel != null) placementPanel.SetActive(false);
            if (battlePanel != null) battlePanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (characterInfoPanel != null) characterInfoPanel.SetActive(false);
            if (unitInfoPanel != null) unitInfoPanel.SetActive(false);
        }
        
        private void ReturnToMenu()
        {
            // 메인 메뉴로 돌아가기 (실제로는 씬 전환)
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}