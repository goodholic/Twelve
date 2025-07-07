using UnityEngine;
using System;
using System.Collections.Generic;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class DialogueData
    {
        [Header("기본 정보")]
        public string dialogueId;
        public string dialogueName;
        public string speakerName;
        
        // CSVDataManager 호환성 속성들
        public int id { get => int.TryParse(dialogueId, out int result) ? result : 0; set => dialogueId = value.ToString(); }
        public string speaker { get => speakerName; set => speakerName = value; }
        public string content { get => dialogueText; set => dialogueText = value; }
        public string background { get => backgroundImage; set => backgroundImage = value; }
        public string bgm { get => backgroundMusic; set => backgroundMusic = value; }
        public string sfx { get => soundEffect; set => soundEffect = value; }
        public string expression { get => emotion.ToString(); set { if (Enum.TryParse<DialogueEmotion>(value, out var e)) emotion = e; } }
        public string effect { get => animation; set => animation = value; }
        public float duration { get => displayDuration; set => displayDuration = value; }
        public string characterName { get => speakerName; set => speakerName = value; }
        
        [Header("대화 내용")]
        [TextArea(3, 6)]
        public string dialogueText;
        public string localizedKey = "";
        
        [Header("대화 타입")]
        public DialogueType dialogueType;
        public DialogueCategory category;
        
        [Header("화자 정보")]
        public string speakerId;
        public Sprite speakerPortrait;
        public DialogueEmotion emotion = DialogueEmotion.Neutral;
        public DialoguePosition speakerPosition = DialoguePosition.Left;
        
        [Header("선택지")]
        public bool hasChoices = false;
        public List<DialogueChoice> choices = new List<DialogueChoice>();
        
        [Header("조건")]
        public List<DialogueCondition> conditions = new List<DialogueCondition>();
        public bool checkConditions = false;
        
        [Header("효과")]
        public List<DialogueEffect> effects = new List<DialogueEffect>();
        public bool hasEffects = false;
        
        [Header("연결")]
        public string nextDialogueId = "";
        public bool isEndNode = false;
        public string parentDialogueId = "";
        
        [Header("타이밍")]
        public float displayDuration = 0f; // 0이면 사용자 입력 대기
        public float typingSpeed = 50f; // 글자 수/초
        public bool autoAdvance = false;
        
        [Header("비주얼")]
        public string backgroundImage = "";
        public string backgroundMusic = "";
        public string soundEffect = "";
        public string animation = "";
        
        [Header("메타데이터")]
        public int priority = 0;
        public bool isRepeatable = true;
        public bool isOneTime = false;
        public bool hasBeenSeen = false;
        public DateTime lastSeen;
        
        public DialogueData()
        {
            dialogueId = System.Guid.NewGuid().ToString();
            dialogueName = "New Dialogue";
            dialogueText = "";
            speakerName = "Unknown";
        }
        
        public DialogueData(string id, string speaker, string text)
        {
            dialogueId = id;
            speakerName = speaker;
            dialogueText = text;
            dialogueName = $"{speaker} - {id}";
        }
        
        public bool CanShow()
        {
            if (isOneTime && hasBeenSeen)
                return false;
                
            if (checkConditions)
            {
                foreach (var condition in conditions)
                {
                    if (!condition.IsMet())
                        return false;
                }
            }
            
            return true;
        }
        
        public void MarkAsSeen()
        {
            hasBeenSeen = true;
            lastSeen = DateTime.Now;
        }
        
        public void ApplyEffects()
        {
            if (!hasEffects)
                return;
                
            foreach (var effect in effects)
            {
                effect.Apply();
            }
        }
        
        public List<DialogueChoice> GetAvailableChoices()
        {
            List<DialogueChoice> availableChoices = new List<DialogueChoice>();
            
            foreach (var choice in choices)
            {
                if (choice.CanSelect())
                {
                    availableChoices.Add(choice);
                }
            }
            
            return availableChoices;
        }
        
        public string GetLocalizedText()
        {
            if (!string.IsNullOrEmpty(localizedKey))
            {
                // 실제 게임에서는 로컬라이제이션 시스템 사용
                return dialogueText;
            }
            return dialogueText;
        }
        
        public float GetEstimatedDisplayTime()
        {
            if (displayDuration > 0)
                return displayDuration;
                
            // 타이핑 속도를 기반으로 예상 시간 계산
            return dialogueText.Length / typingSpeed;
        }
        
        public string GetDialogueInfo()
        {
            string info = $"<b>{dialogueName}</b>\n";
            info += $"화자: {speakerName}\n";
            info += $"타입: {GetDialogueTypeName()}\n";
            
            if (!string.IsNullOrEmpty(dialogueText))
            {
                string preview = dialogueText.Length > 50 ? 
                    dialogueText.Substring(0, 50) + "..." : dialogueText;
                info += $"\n내용: {preview}\n";
            }
            
            if (hasChoices && choices.Count > 0)
                info += $"선택지: {choices.Count}개\n";
                
            if (checkConditions && conditions.Count > 0)
                info += $"조건: {conditions.Count}개\n";
                
            if (hasEffects && effects.Count > 0)
                info += $"효과: {effects.Count}개\n";
                
            if (isOneTime)
                info += "일회성 대화\n";
                
            if (hasBeenSeen)
                info += $"마지막 확인: {lastSeen:yyyy-MM-dd HH:mm}\n";
                
            return info;
        }
        
        public void Initialize()
        {
            // Initialize method for compatibility
            if (string.IsNullOrEmpty(dialogueId))
            {
                dialogueId = System.Guid.NewGuid().ToString();
            }
            if (string.IsNullOrEmpty(dialogueName))
            {
                dialogueName = "New Dialogue";
            }
            if (choices == null)
            {
                choices = new List<DialogueChoice>();
            }
            if (conditions == null)
            {
                conditions = new List<DialogueCondition>();
            }
            if (effects == null)
            {
                effects = new List<DialogueEffect>();
            }
        }
        
        private string GetDialogueTypeName()
        {
            return dialogueType switch
            {
                DialogueType.Normal => "일반",
                DialogueType.Quest => "퀘스트",
                DialogueType.Shop => "상점",
                DialogueType.Story => "스토리",
                DialogueType.Tutorial => "튜토리얼",
                DialogueType.System => "시스템",
                _ => "알 수 없음"
            };
        }
    }
    
    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceId;
        public string choiceText;
        public string nextDialogueId;
        public List<DialogueCondition> conditions = new List<DialogueCondition>();
        public List<DialogueEffect> effects = new List<DialogueEffect>();
        public bool isDefault = false;
        public bool endsDialogue = false;
        public int sortOrder = 0;
        
        public bool CanSelect()
        {
            foreach (var condition in conditions)
            {
                if (!condition.IsMet())
                    return false;
            }
            return true;
        }
        
        public void SelectChoice()
        {
            foreach (var effect in effects)
            {
                effect.Apply();
            }
        }
    }
    
    [System.Serializable]
    public class DialogueCondition
    {
        public DialogueConditionType type;
        public string targetId;
        public string comparisonValue;
        public DialogueComparison comparison = DialogueComparison.Equals;
        public bool negate = false;
        
        public bool IsMet()
        {
            // 실제 게임에서는 조건 검사 로직 구현
            return true;
        }
    }
    
    [System.Serializable]
    public class DialogueEffect
    {
        public DialogueEffectType type;
        public string targetId;
        public string value;
        public int amount;
        
        public void Apply()
        {
            // 실제 게임에서는 효과 적용 로직 구현
        }
    }
    
    public enum DialogueType
    {
        Normal,
        Quest,
        Shop,
        Story,
        Tutorial,
        System,
        Event,
        Battle
    }
    
    public enum DialogueCategory
    {
        Main,
        Side,
        Flavor,
        Tutorial,
        Commerce,
        Battle,
        System
    }
    
    public enum DialogueEmotion
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Surprised,
        Confused,
        Excited,
        Worried
    }
    
    public enum DialoguePosition
    {
        Left,
        Right,
        Center,
        OffScreen
    }
    
    public enum DialogueConditionType
    {
        PlayerLevel,
        QuestCompleted,
        QuestActive,
        ItemOwned,
        VariableValue,
        RelationshipLevel,
        TimeOfDay,
        GameState
    }
    
    public enum DialogueComparison
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterOrEqual,
        LessOrEqual
    }
    
    public enum DialogueEffectType
    {
        GiveItem,
        RemoveItem,
        GiveQuest,
        CompleteQuest,
        ChangeVariable,
        ChangeRelationship,
        PlaySound,
        ChangeScene,
        ShowMessage
    }
} 