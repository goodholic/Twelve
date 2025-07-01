using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GuildMaster.Battle;

namespace GuildMaster.UI
{
    /// <summary>
    /// 개별 유닛 UI 슬롯 - 체력, 마나, 상태 효과 등을 표시
    /// </summary>
    public class UnitUISlot : MonoBehaviour
    {
        [Header("유닛 정보")]
        [SerializeField] private Image unitIcon;
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Image jobClassIcon;
        
        [Header("상태 바")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider manaBar;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI manaText;
        
        [Header("상태 효과")]
        [SerializeField] private Transform statusEffectContainer;
        [SerializeField] private GameObject statusEffectPrefab;
        
        [Header("시각적 효과")]
        [SerializeField] private GameObject selectionHighlight;
        [SerializeField] private GameObject attackHighlight;
        [SerializeField] private GameObject deathOverlay;
        [SerializeField] private Image backgroundImage;
        
        [Header("색상 설정")]
        [SerializeField] private Color playerColor = Color.blue;
        [SerializeField] private Color enemyColor = Color.red;
        [SerializeField] private Color emptySlotColor = Color.gray;
        
        private Unit unit;
        private int row, col;
        private bool isPlayerSlot;
        
        /// <summary>
        /// 슬롯 초기화
        /// </summary>
        public void Initialize(int row, int col, bool isPlayerSlot)
        {
            this.row = row;
            this.col = col;
            this.isPlayerSlot = isPlayerSlot;
            
            // 배경색 설정
            if (backgroundImage != null)
            {
                backgroundImage.color = emptySlotColor;
            }
            
            // 초기 상태 설정
            SetEmpty();
        }
        
        /// <summary>
        /// 유닛 설정
        /// </summary>
        public void SetUnit(Unit unit)
        {
            this.unit = unit;
            
            if (unit != null)
            {
                // 유닛 정보 표시
                ShowUnitInfo();
                
                // 배경색 설정
                if (backgroundImage != null)
                {
                    backgroundImage.color = isPlayerSlot ? playerColor : enemyColor;
                }
                
                // 사망 오버레이 숨기기
                if (deathOverlay != null)
                    deathOverlay.SetActive(false);
            }
            else
            {
                SetEmpty();
            }
        }
        
        /// <summary>
        /// 빈 슬롯으로 설정
        /// </summary>
        private void SetEmpty()
        {
            // 모든 UI 요소 숨기기
            if (unitIcon != null) unitIcon.gameObject.SetActive(false);
            if (unitNameText != null) unitNameText.gameObject.SetActive(false);
            if (levelText != null) levelText.gameObject.SetActive(false);
            if (jobClassIcon != null) jobClassIcon.gameObject.SetActive(false);
            if (healthBar != null) healthBar.gameObject.SetActive(false);
            if (manaBar != null) manaBar.gameObject.SetActive(false);
            if (healthText != null) healthText.gameObject.SetActive(false);
            if (manaText != null) manaText.gameObject.SetActive(false);
            if (deathOverlay != null) deathOverlay.SetActive(false);
            
            // 상태 효과 정리
            ClearStatusEffects();
            
            // 배경색을 빈 슬롯 색상으로
            if (backgroundImage != null)
                backgroundImage.color = emptySlotColor;
        }
        
        /// <summary>
        /// 유닛 정보 표시
        /// </summary>
        private void ShowUnitInfo()
        {
            if (unit == null) return;
            
            // 유닛 아이콘
            if (unitIcon != null)
            {
                unitIcon.gameObject.SetActive(true);
                unitIcon.sprite = unit.Icon; // Unit 클래스에 Icon 프로퍼티가 있다고 가정
            }
            
            // 유닛 이름
            if (unitNameText != null)
            {
                unitNameText.gameObject.SetActive(true);
                unitNameText.text = unit.Name;
            }
            
            // 레벨
            if (levelText != null)
            {
                levelText.gameObject.SetActive(true);
                levelText.text = $"Lv.{unit.Level}";
            }
            
            // 직업 아이콘
            if (jobClassIcon != null)
            {
                jobClassIcon.gameObject.SetActive(true);
                jobClassIcon.sprite = GetJobClassSprite(unit.JobClass);
            }
            
            // 체력바와 마나바 표시
            if (healthBar != null) healthBar.gameObject.SetActive(true);
            if (manaBar != null) manaBar.gameObject.SetActive(true);
            if (healthText != null) healthText.gameObject.SetActive(true);
            if (manaText != null) manaText.gameObject.SetActive(true);
            
            // 초기 상태 업데이트
            UpdateUI();
        }
        
        /// <summary>
        /// UI 업데이트
        /// </summary>
        public void UpdateUI()
        {
            if (unit == null) return;
            
            // 체력바 업데이트
            if (healthBar != null)
            {
                healthBar.value = unit.MaxHealth > 0 ? unit.CurrentHealth / unit.MaxHealth : 0f;
            }
            
            if (healthText != null)
            {
                healthText.text = $"{unit.CurrentHealth:F0}/{unit.MaxHealth:F0}";
            }
            
            // 마나바 업데이트
            if (manaBar != null)
            {
                manaBar.value = unit.MaxMana > 0 ? unit.CurrentMana / unit.MaxMana : 0f;
            }
            
            if (manaText != null)
            {
                manaText.text = $"{unit.CurrentMana:F0}/{unit.MaxMana:F0}";
            }
            
            // 상태 효과 업데이트
            UpdateStatusEffects();
            
            // 사망 상태 체크
            if (unit.CurrentHealth <= 0)
            {
                SetDefeated();
            }
        }
        
        /// <summary>
        /// 사망 상태로 설정
        /// </summary>
        public void SetDefeated()
        {
            if (deathOverlay != null)
                deathOverlay.SetActive(true);
                
            // 체력바를 0으로 설정
            if (healthBar != null)
                healthBar.value = 0f;
                
            if (healthText != null)
                healthText.text = "0/0";
        }
        
        /// <summary>
        /// 선택 하이라이트 표시/숨기기
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
                selectionHighlight.SetActive(selected);
        }
        
        /// <summary>
        /// 공격 하이라이트 표시
        /// </summary>
        public void ShowAttackHighlight(float duration = 0.5f)
        {
            if (attackHighlight != null)
            {
                attackHighlight.SetActive(true);
                Invoke(nameof(HideAttackHighlight), duration);
            }
        }
        
        private void HideAttackHighlight()
        {
            if (attackHighlight != null)
                attackHighlight.SetActive(false);
        }
        
        /// <summary>
        /// 상태 효과 업데이트
        /// </summary>
        private void UpdateStatusEffects()
        {
            // 기존 상태 효과 정리
            ClearStatusEffects();
            
            if (unit == null || statusEffectContainer == null || statusEffectPrefab == null)
                return;
            
            // 유닛의 상태 효과들을 UI로 표시
            var statusEffects = unit.GetStatusEffects(); // Unit 클래스에 이 메서드가 있다고 가정
            foreach (var effect in statusEffects)
            {
                GameObject effectUI = Instantiate(statusEffectPrefab, statusEffectContainer);
                // 상태 효과 UI 설정 (아이콘, 지속시간 등)
                var effectComponent = effectUI.GetComponent<StatusEffectUI>();
                if (effectComponent != null)
                {
                    effectComponent.SetStatusEffect(effect);
                }
            }
        }
        
        /// <summary>
        /// 상태 효과 UI 정리
        /// </summary>
        private void ClearStatusEffects()
        {
            if (statusEffectContainer != null)
            {
                foreach (Transform child in statusEffectContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        /// <summary>
        /// 직업 클래스 스프라이트 가져오기
        /// </summary>
        private Sprite GetJobClassSprite(JobClass jobClass)
        {
            // TODO: 실제 직업별 스프라이트 로드
            // 현재는 null 반환 (나중에 Resources.Load 등으로 구현)
            return null;
        }
        
        /// <summary>
        /// 현재 유닛 반환
        /// </summary>
        public Unit GetUnit()
        {
            return unit;
        }
        
        /// <summary>
        /// 슬롯 위치 반환
        /// </summary>
        public (int row, int col) GetPosition()
        {
            return (row, col);
        }
        
        /// <summary>
        /// 플레이어 슬롯 여부
        /// </summary>
        public bool IsPlayerSlot()
        {
            return isPlayerSlot;
        }
    }
    
    /// <summary>
    /// 상태 효과 UI 컴포넌트 (간단한 구현)
    /// </summary>
    public class StatusEffectUI : MonoBehaviour
    {
        [SerializeField] private Image effectIcon;
        [SerializeField] private TextMeshProUGUI durationText;
        
        public void SetStatusEffect(object statusEffect)
        {
            // TODO: 상태 효과 정보에 따라 아이콘과 지속시간 설정
            // 현재는 기본 구현
        }
    }
} 