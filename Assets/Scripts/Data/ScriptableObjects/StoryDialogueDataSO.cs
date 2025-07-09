using UnityEngine;
using System.Collections.Generic;

namespace TacticalTileGame.Data
{
    /// <summary>
    /// 스토리 대화 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "StoryDialogueData", menuName = "TacticalTileGame/Data/StoryDialogue", order = 3)]
    public class StoryDialogueDataSO : ScriptableObject
    {
        [Header("대화 기본 정보")]
        public string dialogueId;
        public string chapterId;
        public string sceneId;
        
        [Header("대화 내용")]
        public string speakerName;
        public string dialogueText;
        public Sprite speakerPortrait;
        
        [Header("대화 옵션")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();
        public string nextDialogueId; // 다음 대화 ID
        
        [Header("대화 조건")]
        public List<DialogueCondition> conditions = new List<DialogueCondition>();
        
        [Header("감정 및 효과")]
        public EmotionType emotion = EmotionType.Normal;
        public string voiceClipName;
        public string backgroundMusicName;
        public string soundEffectName;
        
        [Header("카메라 효과")]
        public bool cameraShake = false;
        public float shakeDuration = 0.5f;
        public float shakeIntensity = 0.1f;
        
        /// <summary>
        /// CSV 데이터로부터 대화 데이터 생성
        /// </summary>
        public void InitializeFromCSV(Dictionary<string, string> csvData)
        {
            if (csvData.ContainsKey("id")) dialogueId = csvData["id"];
            if (csvData.ContainsKey("chapter")) chapterId = csvData["chapter"];
            if (csvData.ContainsKey("scene")) sceneId = csvData["scene"];
            if (csvData.ContainsKey("speaker")) speakerName = csvData["speaker"];
            if (csvData.ContainsKey("dialogue")) dialogueText = csvData["dialogue"];
            if (csvData.ContainsKey("nextId")) nextDialogueId = csvData["nextId"];
            
            // 감정 파싱
            if (csvData.ContainsKey("emotion") && System.Enum.TryParse<EmotionType>(csvData["emotion"], out EmotionType emo))
                emotion = emo;
            
            // 사운드 효과 파싱
            if (csvData.ContainsKey("voice")) voiceClipName = csvData["voice"];
            if (csvData.ContainsKey("bgm")) backgroundMusicName = csvData["bgm"];
            if (csvData.ContainsKey("sfx")) soundEffectName = csvData["sfx"];
            
            // 선택지 파싱 (CSV 형식: "choice1Text:choice1NextId|choice2Text:choice2NextId")
            if (csvData.ContainsKey("choices") && !string.IsNullOrEmpty(csvData["choices"]))
            {
                choices.Clear();
                string[] choiceStrings = csvData["choices"].Split('|');
                foreach (string choiceStr in choiceStrings)
                {
                    string[] parts = choiceStr.Split(':');
                    if (parts.Length == 2)
                    {
                        choices.Add(new DialogueChoice
                        {
                            choiceText = parts[0].Trim(),
                            nextDialogueId = parts[1].Trim()
                        });
                    }
                }
            }
            
            // 조건 파싱 (CSV 형식: "level>=10|item:sword")
            if (csvData.ContainsKey("conditions") && !string.IsNullOrEmpty(csvData["conditions"]))
            {
                conditions.Clear();
                string[] conditionStrings = csvData["conditions"].Split('|');
                foreach (string condStr in conditionStrings)
                {
                    conditions.Add(DialogueCondition.ParseFromString(condStr));
                }
            }
        }
    }
    
    /// <summary>
    /// 대화 선택지
    /// </summary>
    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public string nextDialogueId;
        public List<DialogueCondition> requireConditions = new List<DialogueCondition>();
        public List<DialogueEffect> choiceEffects = new List<DialogueEffect>();
    }
    
    /// <summary>
    /// 대화 조건
    /// </summary>
    [System.Serializable]
    public class DialogueCondition
    {
        public ConditionType conditionType;
        public string targetId;
        public int requiredValue;
        
        public static DialogueCondition ParseFromString(string conditionStr)
        {
            var condition = new DialogueCondition();
            
            if (conditionStr.Contains(">="))
            {
                string[] parts = conditionStr.Split(new string[] { ">=" }, System.StringSplitOptions.None);
                condition.conditionType = ConditionType.MinValue;
                condition.targetId = parts[0].Trim();
                int.TryParse(parts[1].Trim(), out condition.requiredValue);
            }
            else if (conditionStr.Contains(":"))
            {
                string[] parts = conditionStr.Split(':');
                condition.conditionType = ConditionType.HasItem;
                condition.targetId = parts[1].Trim();
            }
            
            return condition;
        }
    }
    
    /// <summary>
    /// 대화 효과
    /// </summary>
    [System.Serializable]
    public class DialogueEffect
    {
        public EffectType effectType;
        public string targetId;
        public int value;
    }
    
    /// <summary>
    /// 감정 타입
    /// </summary>
    public enum EmotionType
    {
        Normal,     // 보통
        Happy,      // 기쁨
        Sad,        // 슬픔
        Angry,      // 분노
        Surprised,  // 놀람
        Fear,       // 두려움
        Thinking,   // 생각
        Determined  // 결의
    }
    
    /// <summary>
    /// 조건 타입
    /// </summary>
    public enum ConditionType
    {
        MinLevel,       // 최소 레벨
        HasItem,        // 아이템 보유
        HasCharacter,   // 캐릭터 보유
        MinValue,       // 최소값 (골드, 경험치 등)
        StoryProgress,  // 스토리 진행도
        BattleWin       // 전투 승리
    }
    
    /// <summary>
    /// 효과 타입
    /// </summary>
    public enum EffectType
    {
        GainItem,       // 아이템 획득
        GainCharacter,  // 캐릭터 획득
        GainGold,       // 골드 획득
        GainExp,        // 경험치 획득
        UnlockStory,    // 스토리 해금
        SetFlag         // 플래그 설정
    }
}