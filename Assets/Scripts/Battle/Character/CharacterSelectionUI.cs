using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuildMaster.TileBattle
{
    /// <summary>
    /// 캐릭터 선택 UI 관리
    /// 플레이어가 10개의 캐릭터를 선택할 수 있는 인터페이스
    /// </summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject characterCardPrefab;
        [SerializeField] private Transform characterListContainer;
        [SerializeField] private Transform selectedCharacterContainer;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI selectionCountText;
        [SerializeField] private GameObject attackRangePreview;
        
        [Header("Settings")]
        [SerializeField] private int maxSelections = 10;
        
        // 캐릭터 데이터
        private List<TileBattleAI.Character> availableCharacters;
        private List<TileBattleAI.Character> selectedCharacters = new List<TileBattleAI.Character>();
        private Dictionary<string, CharacterCard> characterCards = new Dictionary<string, CharacterCard>();
        
        // 콜백
        private System.Action<List<TileBattleAI.Character>> onSelectionComplete;
        
        void Start()
        {
            confirmButton.onClick.AddListener(OnConfirmSelection);
            confirmButton.interactable = false;
            UpdateUI();
        }

        /// <summary>
        /// 캐릭터 선택 UI 초기화
        /// </summary>
        public void Initialize(List<TileBattleAI.Character> characters, 
            System.Action<List<TileBattleAI.Character>> callback)
        {
            availableCharacters = characters;
            onSelectionComplete = callback;
            
            // 기존 카드 제거
            foreach (Transform child in characterListContainer)
            {
                Destroy(child.gameObject);
            }
            characterCards.Clear();
            
            // 캐릭터 카드 생성
            foreach (var character in availableCharacters)
            {
                CreateCharacterCard(character);
            }
            
            UpdateUI();
        }

        /// <summary>
        /// 캐릭터 카드 생성
        /// </summary>
        void CreateCharacterCard(TileBattleAI.Character character)
        {
            GameObject cardObj = Instantiate(characterCardPrefab, characterListContainer);
            CharacterCard card = cardObj.GetComponent<CharacterCard>();
            
            if (card == null)
            {
                card = cardObj.AddComponent<CharacterCard>();
            }
            
            card.Initialize(character, OnCharacterCardClicked, OnCharacterCardHover);
            characterCards[character.id] = card;
        }

        /// <summary>
        /// 캐릭터 카드 클릭 처리
        /// </summary>
        void OnCharacterCardClicked(TileBattleAI.Character character)
        {
            if (selectedCharacters.Contains(character))
            {
                // 선택 해제
                selectedCharacters.Remove(character);
                characterCards[character.id].SetSelected(false);
            }
            else if (selectedCharacters.Count < maxSelections)
            {
                // 선택
                selectedCharacters.Add(character);
                characterCards[character.id].SetSelected(true);
            }
            else
            {
                // 최대 선택 수 도달
                Debug.Log($"최대 {maxSelections}개까지만 선택할 수 있습니다.");
            }
            
            UpdateUI();
            UpdateSelectedCharacterDisplay();
        }

        /// <summary>
        /// 캐릭터 카드 호버 처리
        /// </summary>
        void OnCharacterCardHover(TileBattleAI.Character character, bool isHovering)
        {
            if (isHovering && attackRangePreview != null)
            {
                ShowAttackRangePreview(character);
            }
            else
            {
                HideAttackRangePreview();
            }
        }

        /// <summary>
        /// 공격 범위 미리보기 표시
        /// </summary>
        void ShowAttackRangePreview(TileBattleAI.Character character)
        {
            attackRangePreview.SetActive(true);
            
            // 공격 범위 시각화
            var rangeVisualizer = attackRangePreview.GetComponent<AttackRangeVisualizer>();
            if (rangeVisualizer != null)
            {
                rangeVisualizer.ShowRange(character.attackRange);
            }
        }

        /// <summary>
        /// 공격 범위 미리보기 숨김
        /// </summary>
        void HideAttackRangePreview()
        {
            if (attackRangePreview != null)
            {
                attackRangePreview.SetActive(false);
            }
        }

        /// <summary>
        /// 선택된 캐릭터 표시 업데이트
        /// </summary>
        void UpdateSelectedCharacterDisplay()
        {
            // 기존 표시 제거
            foreach (Transform child in selectedCharacterContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 선택된 캐릭터 표시
            foreach (var character in selectedCharacters)
            {
                GameObject miniCard = new GameObject(character.name);
                miniCard.transform.SetParent(selectedCharacterContainer);
                
                var text = miniCard.AddComponent<TextMeshProUGUI>();
                text.text = character.name;
                text.fontSize = 14;
            }
        }

        /// <summary>
        /// UI 업데이트
        /// </summary>
        void UpdateUI()
        {
            selectionCountText.text = $"선택: {selectedCharacters.Count}/{maxSelections}";
            confirmButton.interactable = selectedCharacters.Count == maxSelections;
        }

        /// <summary>
        /// 선택 확정
        /// </summary>
        void OnConfirmSelection()
        {
            if (selectedCharacters.Count == maxSelections)
            {
                onSelectionComplete?.Invoke(new List<TileBattleAI.Character>(selectedCharacters));
                gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 캐릭터 카드 컴포넌트
    /// </summary>
    public class CharacterCard : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI powerText;
        [SerializeField] private Image characterIcon;
        [SerializeField] private Image selectionOutline;
        [SerializeField] private Button selectButton;
        
        private TileBattleAI.Character character;
        private System.Action<TileBattleAI.Character> onClickCallback;
        private System.Action<TileBattleAI.Character, bool> onHoverCallback;
        private bool isSelected = false;

        /// <summary>
        /// 카드 초기화
        /// </summary>
        public void Initialize(TileBattleAI.Character characterData, 
            System.Action<TileBattleAI.Character> onClick,
            System.Action<TileBattleAI.Character, bool> onHover)
        {
            character = characterData;
            onClickCallback = onClick;
            onHoverCallback = onHover;
            
            // UI 설정
            if (nameText != null) nameText.text = character.name;
            if (powerText != null) powerText.text = $"공격력: {character.attackPower}";
            
            // 버튼 이벤트
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(() => onClickCallback?.Invoke(character));
            }
            
            // 호버 이벤트
            var eventTrigger = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => onHoverCallback?.Invoke(character, true));
            eventTrigger.triggers.Add(pointerEnter);
            
            var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => onHoverCallback?.Invoke(character, false));
            eventTrigger.triggers.Add(pointerExit);
            
            SetSelected(false);
        }

        /// <summary>
        /// 선택 상태 설정
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (selectionOutline != null)
            {
                selectionOutline.gameObject.SetActive(selected);
            }
        }
    }

    /// <summary>
    /// 공격 범위 시각화 컴포넌트
    /// </summary>
    public class AttackRangeVisualizer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private Color centerColor = Color.blue;
        [SerializeField] private Color rangeColor = Color.red;
        
        private List<GameObject> rangeTiles = new List<GameObject>();

        /// <summary>
        /// 공격 범위 표시
        /// </summary>
        public void ShowRange(List<Vector2Int> attackRange)
        {
            ClearRange();
            
            // 중앙 타일
            GameObject centerTile = Instantiate(tilePrefab, transform);
            centerTile.transform.localPosition = Vector3.zero;
            var centerRenderer = centerTile.GetComponent<Renderer>();
            if (centerRenderer != null)
            {
                centerRenderer.material.color = centerColor;
            }
            rangeTiles.Add(centerTile);
            
            // 공격 범위 타일
            foreach (var offset in attackRange)
            {
                GameObject rangeTile = Instantiate(tilePrefab, transform);
                rangeTile.transform.localPosition = new Vector3(
                    offset.x * tileSize, 
                    0, 
                    offset.y * tileSize
                );
                
                var renderer = rangeTile.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = rangeColor;
                }
                rangeTiles.Add(rangeTile);
            }
        }

        /// <summary>
        /// 범위 표시 제거
        /// </summary>
        public void ClearRange()
        {
            foreach (var tile in rangeTiles)
            {
                Destroy(tile);
            }
            rangeTiles.Clear();
        }

        void OnDisable()
        {
            ClearRange();
        }
    }
}