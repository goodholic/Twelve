using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 전투 UI를 관리하는 컨트롤러
    /// </summary>
    public class BattleUIController : MonoBehaviour
    {
        [Header("메인 UI 패널")]
        [SerializeField] private GameObject battleUIPanel;
        [SerializeField] private GameObject deploymentPanel;
        [SerializeField] private GameObject battleResultPanel;
        
        [Header("턴 정보")]
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private TextMeshProUGUI turnOwnerText;
        [SerializeField] private Image turnIndicatorImage;
        [SerializeField] private Color playerTurnColor = Color.blue;
        [SerializeField] private Color enemyTurnColor = Color.red;
        
        [Header("점수 표시")]
        [SerializeField] private TextMeshProUGUI scoreTextA_Ally;
        [SerializeField] private TextMeshProUGUI scoreTextA_Enemy;
        [SerializeField] private TextMeshProUGUI scoreTextB_Ally;
        [SerializeField] private TextMeshProUGUI scoreTextB_Enemy;
        
        [Header("배치 UI")]
        [SerializeField] private GameObject currentCharacterPanel;
        [SerializeField] private Image currentCharacterImage;
        [SerializeField] private TextMeshProUGUI currentCharacterName;
        [SerializeField] private TextMeshProUGUI currentCharacterInfo;
        [SerializeField] private TextMeshProUGUI remainingCharactersText;
        
        [Header("캐릭터 목록")]
        [SerializeField] private Transform playerCharacterListParent;
        [SerializeField] private Transform enemyCharacterListParent;
        [SerializeField] private GameObject characterListItemPrefab;
        
        [Header("전투 결과")]
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultScoreText;
        [SerializeField] private TextMeshProUGUI resultDetailText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button exitButton;
        
        [Header("애니메이션")]
        [SerializeField] private float scoreUpdateSpeed = 2f;
        [SerializeField] private AnimationCurve scoreAnimationCurve;
        
        // 내부 상태
        private List<GameObject> playerCharacterItems = new List<GameObject>();
        private List<GameObject> enemyCharacterItems = new List<GameObject>();
        private Coroutine scoreUpdateCoroutine;
        
        void Start()
        {
            // 버튼 이벤트 연결
            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);
                
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
                
            // 초기 UI 숨기기
            HideAllPanels();
        }
        
        /// <summary>
        /// 전투 UI 초기 설정
        /// </summary>
        public void SetupBattle(List<CharacterUnit> playerCharacters, List<CharacterUnit> enemyCharacters)
        {
            ShowBattleUI(true);
            
            // 캐릭터 목록 생성
            CreateCharacterList(playerCharacters, true);
            CreateCharacterList(enemyCharacters, false);
            
            // 점수 초기화
            UpdateScores();
        }
        
        /// <summary>
        /// 캐릭터 목록 생성
        /// </summary>
        void CreateCharacterList(List<CharacterUnit> characters, bool isPlayer)
        {
            Transform parent = isPlayer ? playerCharacterListParent : enemyCharacterListParent;
            List<GameObject> itemList = isPlayer ? playerCharacterItems : enemyCharacterItems;
            
            // 기존 아이템 제거
            foreach (var item in itemList)
            {
                if (item != null)
                    Destroy(item);
            }
            itemList.Clear();
            
            // 새 아이템 생성
            foreach (var character in characters)
            {
                if (characterListItemPrefab != null && parent != null)
                {
                    GameObject item = Instantiate(characterListItemPrefab, parent);
                    
                    // 아이템 정보 설정
                    var image = item.GetComponentInChildren<Image>();
                    var text = item.GetComponentInChildren<TextMeshProUGUI>();
                    
                    if (image != null && character.GetComponent<SpriteRenderer>() != null)
                    {
                        image.sprite = character.GetComponent<SpriteRenderer>().sprite;
                    }
                    
                    if (text != null)
                    {
                        text.text = character.name;
                    }
                    
                    itemList.Add(item);
                }
            }
        }
        
        /// <summary>
        /// 턴 정보 업데이트
        /// </summary>
        public void UpdateTurnInfo(int turn, bool isPlayerTurn)
        {
            if (turnText != null)
                turnText.text = $"턴 {turn}";
                
            if (turnOwnerText != null)
                turnOwnerText.text = isPlayerTurn ? "플레이어 턴" : "적 턴";
                
            if (turnIndicatorImage != null)
                turnIndicatorImage.color = isPlayerTurn ? playerTurnColor : enemyTurnColor;
        }
        
        /// <summary>
        /// 점수 업데이트
        /// </summary>
        public void UpdateScores()
        {
            if (TileGridManager.Instance == null) return;
            
            // A 타일 점수
            int allyA = TileGridManager.Instance.GetCharacterCount(Tile.TileType.A, Tile.Team.Ally);
            int enemyA = TileGridManager.Instance.GetCharacterCount(Tile.TileType.A, Tile.Team.Enemy);
            
            // B 타일 점수
            int allyB = TileGridManager.Instance.GetCharacterCount(Tile.TileType.B, Tile.Team.Ally);
            int enemyB = TileGridManager.Instance.GetCharacterCount(Tile.TileType.B, Tile.Team.Enemy);
            
            // 애니메이션으로 점수 업데이트
            if (scoreUpdateCoroutine != null)
                StopCoroutine(scoreUpdateCoroutine);
                
            scoreUpdateCoroutine = StartCoroutine(AnimateScoreUpdate(allyA, enemyA, allyB, enemyB));
        }
        
        /// <summary>
        /// 점수 업데이트 애니메이션
        /// </summary>
        IEnumerator AnimateScoreUpdate(int targetAllyA, int targetEnemyA, int targetAllyB, int targetEnemyB)
        {
            float elapsed = 0f;
            
            // 현재 값 가져오기
            int currentAllyA = GetCurrentScore(scoreTextA_Ally);
            int currentEnemyA = GetCurrentScore(scoreTextA_Enemy);
            int currentAllyB = GetCurrentScore(scoreTextB_Ally);
            int currentEnemyB = GetCurrentScore(scoreTextB_Enemy);
            
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * scoreUpdateSpeed;
                float t = scoreAnimationCurve.Evaluate(elapsed);
                
                // 점수 보간
                SetScoreText(scoreTextA_Ally, Mathf.RoundToInt(Mathf.Lerp(currentAllyA, targetAllyA, t)));
                SetScoreText(scoreTextA_Enemy, Mathf.RoundToInt(Mathf.Lerp(currentEnemyA, targetEnemyA, t)));
                SetScoreText(scoreTextB_Ally, Mathf.RoundToInt(Mathf.Lerp(currentAllyB, targetAllyB, t)));
                SetScoreText(scoreTextB_Enemy, Mathf.RoundToInt(Mathf.Lerp(currentEnemyB, targetEnemyB, t)));
                
                // 우세한 쪽 강조
                HighlightWinningScore(scoreTextA_Ally, scoreTextA_Enemy, targetAllyA > targetEnemyA);
                HighlightWinningScore(scoreTextB_Ally, scoreTextB_Enemy, targetAllyB > targetEnemyB);
                
                yield return null;
            }
            
            // 최종 값 설정
            SetScoreText(scoreTextA_Ally, targetAllyA);
            SetScoreText(scoreTextA_Enemy, targetEnemyA);
            SetScoreText(scoreTextB_Ally, targetAllyB);
            SetScoreText(scoreTextB_Enemy, targetEnemyB);
        }
        
        /// <summary>
        /// 현재 점수 가져오기
        /// </summary>
        int GetCurrentScore(TextMeshProUGUI scoreText)
        {
            if (scoreText == null) return 0;
            
            if (int.TryParse(scoreText.text, out int score))
                return score;
                
            return 0;
        }
        
        /// <summary>
        /// 점수 텍스트 설정
        /// </summary>
        void SetScoreText(TextMeshProUGUI scoreText, int score)
        {
            if (scoreText != null)
                scoreText.text = score.ToString();
        }
        
        /// <summary>
        /// 우세한 점수 강조
        /// </summary>
        void HighlightWinningScore(TextMeshProUGUI allyText, TextMeshProUGUI enemyText, bool allyWinning)
        {
            if (allyText != null)
            {
                allyText.fontStyle = allyWinning ? FontStyles.Bold : FontStyles.Normal;
                allyText.color = allyWinning ? Color.green : Color.white;
            }
            
            if (enemyText != null)
            {
                enemyText.fontStyle = !allyWinning ? FontStyles.Bold : FontStyles.Normal;
                enemyText.color = !allyWinning ? Color.red : Color.white;
            }
        }
        
        /// <summary>
        /// 배치 UI 표시
        /// </summary>
        public void ShowDeploymentUI(bool show)
        {
            if (deploymentPanel != null)
                deploymentPanel.SetActive(show);
        }
        
        /// <summary>
        /// 현재 배치할 캐릭터 표시
        /// </summary>
        public void ShowCurrentCharacterToPlace(CharacterUnit character)
        {
            if (currentCharacterPanel != null)
                currentCharacterPanel.SetActive(true);
                
            if (currentCharacterImage != null && character.GetComponent<SpriteRenderer>() != null)
            {
                currentCharacterImage.sprite = character.GetComponent<SpriteRenderer>().sprite;
            }
            
            if (currentCharacterName != null)
                currentCharacterName.text = character.name;
                
            if (currentCharacterInfo != null)
                currentCharacterInfo.text = character.GetInfo();
        }
        
        /// <summary>
        /// 남은 캐릭터 수 업데이트
        /// </summary>
        public void UpdateRemainingCharacters(int playerRemaining, int enemyRemaining)
        {
            if (remainingCharactersText != null)
            {
                remainingCharactersText.text = $"남은 캐릭터: 아군 {playerRemaining} / 적 {enemyRemaining}";
            }
        }
        
        /// <summary>
        /// 전투 결과 표시
        /// </summary>
        public void ShowBattleResult(GameResult result, int allyScore, int enemyScore)
        {
            if (battleResultPanel != null)
                battleResultPanel.SetActive(true);
                
            // 결과 제목
            if (resultTitleText != null)
            {
                switch (result)
                {
                    case GameResult.Victory:
                        resultTitleText.text = "승리!";
                        resultTitleText.color = Color.green;
                        break;
                    case GameResult.Draw:
                        resultTitleText.text = "무승부";
                        resultTitleText.color = Color.yellow;
                        break;
                    case GameResult.Defeat:
                        resultTitleText.text = "패배...";
                        resultTitleText.color = Color.red;
                        break;
                }
            }
            
            // 점수
            if (resultScoreText != null)
            {
                resultScoreText.text = $"아군 {allyScore} : {enemyScore} 적군";
            }
            
            // 상세 설명
            if (resultDetailText != null)
            {
                string detail = "";
                
                // A 타일 결과
                int allyA = TileGridManager.Instance.GetCharacterCount(Tile.TileType.A, Tile.Team.Ally);
                int enemyA = TileGridManager.Instance.GetCharacterCount(Tile.TileType.A, Tile.Team.Enemy);
                detail += $"A 타일: {allyA} vs {enemyA}\n";
                
                // B 타일 결과
                int allyB = TileGridManager.Instance.GetCharacterCount(Tile.TileType.B, Tile.Team.Ally);
                int enemyB = TileGridManager.Instance.GetCharacterCount(Tile.TileType.B, Tile.Team.Enemy);
                detail += $"B 타일: {allyB} vs {enemyB}\n\n";
                
                // 승리 조건 설명
                switch (result)
                {
                    case GameResult.Victory:
                        detail += "두 타일 모두에서 우위를 점했습니다!";
                        break;
                    case GameResult.Draw:
                        detail += "한 타일에서만 우위를 점했습니다.";
                        break;
                    case GameResult.Defeat:
                        detail += "어느 타일에서도 우위를 점하지 못했습니다.";
                        break;
                }
                
                resultDetailText.text = detail;
            }
        }
        
        /// <summary>
        /// 전투 UI 표시
        /// </summary>
        public void ShowBattleUI(bool show)
        {
            if (battleUIPanel != null)
                battleUIPanel.SetActive(show);
        }
        
        /// <summary>
        /// 모든 패널 숨기기
        /// </summary>
        void HideAllPanels()
        {
            ShowBattleUI(false);
            ShowDeploymentUI(false);
            
            if (battleResultPanel != null)
                battleResultPanel.SetActive(false);
                
            if (currentCharacterPanel != null)
                currentCharacterPanel.SetActive(false);
        }
        
        /// <summary>
        /// 다시하기 버튼 클릭
        /// </summary>
        void OnRetryClicked()
        {
            // 전투 재시작
            if (TurnBasedBattleManager.Instance != null)
            {
                // 현재 팀 데이터로 재시작
                // TurnBasedBattleManager.Instance.RestartBattle();
            }
            
            if (battleResultPanel != null)
                battleResultPanel.SetActive(false);
        }
        
        /// <summary>
        /// 나가기 버튼 클릭
        /// </summary>
        void OnExitClicked()
        {
            // 메인 메뉴로 돌아가기
            // SceneManager.LoadScene("MainMenu");
            
            if (battleResultPanel != null)
                battleResultPanel.SetActive(false);
        }
        
        void OnDestroy()
        {
            if (scoreUpdateCoroutine != null)
            {
                StopCoroutine(scoreUpdateCoroutine);
            }
        }
    }
}