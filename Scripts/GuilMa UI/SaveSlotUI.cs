using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GuildMaster.Core;
using GuildMaster.Data;
using GuildMaster.Systems;

namespace GuildMaster.UI
{
    public class SaveSlotUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI slotNumberText;
        [SerializeField] private TextMeshProUGUI guildNameText;
        [SerializeField] private TextMeshProUGUI guildLevelText;
        [SerializeField] private TextMeshProUGUI playTimeText;
        [SerializeField] private TextMeshProUGUI saveTimeText;
        [SerializeField] private Image screenshotImage;
        [SerializeField] private GameObject emptySlotPanel;
        [SerializeField] private GameObject filledSlotPanel;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private GameObject autoSaveIcon;
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.8f);
        [SerializeField] private Color selectedColor = new Color(0.8f, 1f, 0.8f);
        
        private SaveData saveData;
        private Action onLoadAction;
        private Image backgroundImage;
        private bool isSelected = false;
        
        void Awake()
        {
            backgroundImage = GetComponent<Image>();
            
            if (loadButton != null)
                loadButton.onClick.AddListener(OnLoadClicked);
                
            if (deleteButton != null)
                deleteButton.onClick.AddListener(OnDeleteClicked);
        }
        
        public void SetupSlot(SaveData data, Action onLoad)
        {
            saveData = data;
            onLoadAction = onLoad;
            
            // UI 업데이트
            if (slotNumberText != null)
                slotNumberText.text = $"슬롯 {data.slotIndex + 1}";
                
            if (guildNameText != null)
                guildNameText.text = data.guildName;
                
            if (guildLevelText != null)
                guildLevelText.text = $"Lv.{data.guildLevel}";
                
            if (playTimeText != null)
                playTimeText.text = FormatPlayTime(data.totalPlayTime);
                
            if (saveTimeText != null)
                saveTimeText.text = FormatSaveTime(data.saveTime);
            
            // 스크린샷 로드
            if (screenshotImage != null && !string.IsNullOrEmpty(data.screenshotPath))
            {
                LoadScreenshot(data.screenshotPath);
            }
            
            // 자동 저장 아이콘
            if (autoSaveIcon != null)
                autoSaveIcon.SetActive(data.isAutoSave);
            
            // 패널 표시
            if (emptySlotPanel != null)
                emptySlotPanel.SetActive(false);
                
            if (filledSlotPanel != null)
                filledSlotPanel.SetActive(true);
        }
        
        public void SetupEmptySlot(int slotIndex)
        {
            if (slotNumberText != null)
                slotNumberText.text = $"슬롯 {slotIndex + 1}";
            
            if (emptySlotPanel != null)
                emptySlotPanel.SetActive(true);
                
            if (filledSlotPanel != null)
                filledSlotPanel.SetActive(false);
                
            if (loadButton != null)
                loadButton.interactable = false;
                
            if (deleteButton != null)
                deleteButton.gameObject.SetActive(false);
        }
        
        void OnLoadClicked()
        {
            SoundSystem.Instance?.PlaySound("ui_click");
            onLoadAction?.Invoke();
        }
        
        void OnDeleteClicked()
        {
            SoundSystem.Instance?.PlaySound("ui_click");
            
            // 삭제 확인 다이얼로그
            string message = $"'{saveData.guildName}' 세이브를 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.";
            
            // TODO: 확인 다이얼로그 표시
            DeleteSaveData();
        }
        
        void DeleteSaveData()
        {
            if (saveData != null)
            {
                SaveManager.Instance?.DeleteSave(saveData.slotIndex);
                
                // UI 업데이트
                SetupEmptySlot(saveData.slotIndex);
                
                // 효과
                ParticleEffectsSystem.Instance?.PlayEffect("ui_sparkle", transform.position);
                SoundSystem.Instance?.PlaySound("ui_success");
            }
        }
        
        void LoadScreenshot(string path)
        {
            // 스크린샷 로드 로직
            // 실제 구현에서는 파일 시스템에서 이미지를 로드
            byte[] imageData = System.IO.File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            
            if (texture.LoadImage(imageData))
            {
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                screenshotImage.sprite = sprite;
            }
        }
        
        string FormatPlayTime(float seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            if (time.TotalDays >= 1)
            {
                return $"{(int)time.TotalDays}일 {time.Hours}시간";
            }
            else if (time.TotalHours >= 1)
            {
                return $"{(int)time.TotalHours}시간 {time.Minutes}분";
            }
            else
            {
                return $"{time.Minutes}분";
            }
        }
        
        string FormatSaveTime(long timestamp)
        {
            DateTime saveTime = new DateTime(timestamp);
            DateTime now = DateTime.Now;
            TimeSpan diff = now - saveTime;
            
            if (diff.TotalMinutes < 1)
            {
                return "방금 전";
            }
            else if (diff.TotalHours < 1)
            {
                return $"{(int)diff.TotalMinutes}분 전";
            }
            else if (diff.TotalDays < 1)
            {
                return $"{(int)diff.TotalHours}시간 전";
            }
            else if (diff.TotalDays < 7)
            {
                return $"{(int)diff.TotalDays}일 전";
            }
            else
            {
                return saveTime.ToString("yyyy-MM-dd");
            }
        }
        
        public void OnPointerEnter()
        {
            if (!isSelected && backgroundImage != null)
            {
                backgroundImage.color = hoverColor;
                transform.localScale = Vector3.one * 1.02f;
            }
        }
        
        public void OnPointerExit()
        {
            if (!isSelected && backgroundImage != null)
            {
                backgroundImage.color = normalColor;
                transform.localScale = Vector3.one;
            }
        }
        
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            if (backgroundImage != null)
            {
                backgroundImage.color = selected ? selectedColor : normalColor;
            }
        }
    }
}