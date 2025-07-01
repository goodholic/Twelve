using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using GuildMaster.Battle;

namespace GuildMaster.UI
{
    /// <summary>
    /// 부대 UI 컴포넌트 - 3x3 그리드로 유닛들을 표시
    /// </summary>
    public class SquadUIComponent : MonoBehaviour
    {
        [Header("부대 정보")]
        [SerializeField] private TextMeshProUGUI squadNameText;
        [SerializeField] private Slider squadHealthBar;
        [SerializeField] private TextMeshProUGUI squadHealthText;
        [SerializeField] private Image squadIcon;
        
        [Header("유닛 그리드")]
        [SerializeField] private GridLayoutGroup unitGrid;
        [SerializeField] private GameObject unitSlotPrefab;
        
        [Header("상태 표시")]
        [SerializeField] private GameObject activeIndicator;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color activeColor = Color.yellow;
        [SerializeField] private Color inactiveColor = Color.gray;
        
        private Squad squad;
        private List<UnitUISlot> unitSlots = new List<UnitUISlot>();
        private bool isPlayerSquad;
        
        /// <summary>
        /// 부대 UI 초기화
        /// </summary>
        public void Initialize(Squad squad, GameObject unitSlotPrefab, bool isPlayerSquad)
        {
            this.squad = squad;
            this.isPlayerSquad = isPlayerSquad;
            this.unitSlotPrefab = unitSlotPrefab;
            
            // 부대 이름 설정
            if (squadNameText != null)
                squadNameText.text = squad.Name;
            
            // 유닛 슬롯 생성 (3x3 그리드)
            CreateUnitSlots();
            
            // 초기 UI 업데이트
            UpdateUI();
        }
        
        /// <summary>
        /// 3x3 유닛 슬롯 생성
        /// </summary>
        private void CreateUnitSlots()
        {
            // 기존 슬롯 정리
            foreach (Transform child in unitGrid.transform)
            {
                Destroy(child.gameObject);
            }
            unitSlots.Clear();
            
            // 3x3 = 9개 슬롯 생성
            for (int i = 0; i < 9; i++)
            {
                GameObject slotObj = Instantiate(unitSlotPrefab, unitGrid.transform);
                UnitUISlot slot = slotObj.GetComponent<UnitUISlot>();
                
                if (slot != null)
                {
                    // 그리드 위치 계산 (0-8 인덱스를 row, col로 변환)
                    int row = i / 3;
                    int col = i % 3;
                    
                    slot.Initialize(row, col, isPlayerSquad);
                    unitSlots.Add(slot);
                    
                    // 해당 위치에 유닛이 있다면 설정
                    Unit unit = squad.GetUnitAt(row, col);
                    if (unit != null)
                    {
                        slot.SetUnit(unit);
                    }
                }
            }
        }
        
        /// <summary>
        /// UI 업데이트
        /// </summary>
        public void UpdateUI()
        {
            if (squad == null) return;
            
            // 부대 체력 업데이트
            UpdateHealthBar();
            
            // 각 유닛 슬롯 업데이트
            foreach (var slot in unitSlots)
            {
                slot.UpdateUI();
            }
        }
        
        /// <summary>
        /// 부대 체력바 업데이트
        /// </summary>
        private void UpdateHealthBar()
        {
            float totalHealth = 0f;
            float maxHealth = 0f;
            
            foreach (var unit in squad.GetAllUnits())
            {
                if (unit != null)
                {
                    totalHealth += unit.CurrentHealth;
                    maxHealth += unit.MaxHealth;
                }
            }
            
            if (squadHealthBar != null)
            {
                squadHealthBar.value = maxHealth > 0 ? totalHealth / maxHealth : 0f;
            }
            
            if (squadHealthText != null)
            {
                squadHealthText.text = $"{totalHealth:F0}/{maxHealth:F0}";
            }
        }
        
        /// <summary>
        /// 활성 상태 설정
        /// </summary>
        public void SetActive(bool active)
        {
            if (activeIndicator != null)
                activeIndicator.SetActive(active);
                
            if (backgroundImage != null)
                backgroundImage.color = active ? activeColor : inactiveColor;
        }
        
        /// <summary>
        /// 특정 유닛의 UI 슬롯 찾기
        /// </summary>
        public UnitUISlot GetUnitSlot(Unit unit)
        {
            foreach (var slot in unitSlots)
            {
                if (slot.GetUnit() == unit)
                    return slot;
            }
            return null;
        }
        
        /// <summary>
        /// 위치별 유닛 슬롯 가져오기
        /// </summary>
        public UnitUISlot GetSlotAt(int row, int col)
        {
            int index = row * 3 + col;
            if (index >= 0 && index < unitSlots.Count)
                return unitSlots[index];
            return null;
        }
        
        /// <summary>
        /// 부대 정보 가져오기
        /// </summary>
        public Squad GetSquad()
        {
            return squad;
        }
        
        /// <summary>
        /// 부대 프로퍼티 (호환성)
        /// </summary>
        public Squad Squad => squad;
        
        /// <summary>
        /// 플레이어 부대 여부
        /// </summary>
        public bool IsPlayerSquad()
        {
            return isPlayerSquad;
        }
    }
} 