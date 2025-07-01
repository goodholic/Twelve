using UnityEngine;
using System;
using System.Collections.Generic;

namespace GuildMaster.Data
{
    /// <summary>
    /// 스토리 대화 ScriptableObject
    /// CSV 데이터를 기반으로 생성되는 대화 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "StoryDialogue", menuName = "GuildMaster/Data/StoryDialogue", order = 7)]
    public class StoryDialogueSO : ScriptableObject
    {
        [Header("Dialogue Identity")]
        public string dialogueId;
        public string chapterId;
        public string nodeId;
        
        [Header("Speaker Info")]
        public string speaker;
        public StoryCharacterSO characterData;
        
        [Header("Dialogue Content")]
        [TextArea(3, 6)]
        public string dialogueText;
        public EmotionType emotion;
        public VoiceEffectType voiceEffect;
        
        [Header("Dialogue Choices")]
        public DialogueChoice[] choices;
        
        [Header("Audio Settings")]
        public AudioClip voiceClip;
        public float audioDelay = 0f;
        public bool autoPlay = true;
        
        [Header("Visual Effects")]
        public CameraShakeSettings cameraShake;
        public ParticleEffectSettings particleEffect;
        public ScreenEffectSettings screenEffect;
        
        [Header("Localization")]
        public LocalizedText[] localizedTexts;
        
        public enum EmotionType
        {
            Default,
            슬픔,
            냉정,
            우려,
            놀람,
            혼란,
            불신,
            진지함,
            설명,
            연민,
            분노,
            논리적,
            단정적,
            진지한_질문,
            설득,
            반문,
            친근함,
            간절함,
            명령,
            긴급,
            절규,
            마지막_숨,
            후회,
            용서,
            조롱,
            반박,
            위협,
            최후통첩,
            주장,
            격앙,
            진정,
            비난,
            아픔,
            조언,
            지혜,
            결단,
            확고함,
            선언,
            당당함,
            경고,
            전투,
            희생,
            희망,
            기대,
            부드러움,
            따뜻함,
            외침,
            약해지는_목소리
        }
        
        public enum VoiceEffectType
        {
            None,
            한숨,
            단호함,
            강조,
            헐떡임,
            떨림,
            코웃음,
            낮은_목소리,
            차분함,
            목멘_소리,
            고함,
            차가움,
            조용함,
            열정적,
            힘있게,
            거친_숨소리,
            온화함,
            떨리는_목소리,
            외침,
            효과음,
            약해지는_목소리,
            따뜻함,
            예의바른_독,
            침착함,
            차가운_미소,
            주먹질,
            설득,
            아픔,
            확고함,
            당당함,
            싸늘함,
            숨찬_소리,
            부드러움
        }
        
        [System.Serializable]
        public class DialogueChoice
        {
            public string choiceId;
            [TextArea(1, 3)]
            public string choiceText;
            public string nextNodeId;
            public ChoiceRequirement[] requirements;
            public ChoiceEffect[] effects;
            public bool isEnabled = true;
        }
        
        [System.Serializable]
        public class ChoiceRequirement
        {
            public RequirementType type;
            public string targetId;
            public int requiredValue;
            public ComparisonType comparison;
        }
        
        [System.Serializable]
        public class ChoiceEffect
        {
            public EffectType type;
            public string targetId;
            public int value;
            public string description;
        }
        
        [System.Serializable]
        public class CameraShakeSettings
        {
            public bool enabled = false;
            public float intensity = 1f;
            public float duration = 0.5f;
            public ShakeType shakeType = ShakeType.Random;
        }
        
        [System.Serializable]
        public class ParticleEffectSettings
        {
            public bool enabled = false;
            public GameObject particlePrefab;
            public Vector3 spawnPosition;
            public float duration = 2f;
        }
        
        [System.Serializable]
        public class ScreenEffectSettings
        {
            public bool enabled = false;
            public ScreenEffectType effectType;
            public Color effectColor = Color.white;
            public float intensity = 0.5f;
            public float duration = 1f;
        }
        
        [System.Serializable]
        public class LocalizedText
        {
            public string languageCode;
            [TextArea(3, 6)]
            public string localizedDialogue;
        }
        
        public enum RequirementType
        {
            GuildLevel,
            Reputation,
            Gold,
            StoryProgress,
            CharacterRelation,
            ItemOwned
        }
        
        public enum ComparisonType
        {
            GreaterThan,
            LessThan,
            EqualTo,
            GreaterOrEqual,
            LessOrEqual
        }
        
        public enum EffectType
        {
            ChangeReputation,
            ChangeGold,
            ChangeRelation,
            UnlockStory,
            TriggerEvent,
            ChangeState
        }
        
        public enum ShakeType
        {
            Random,
            Horizontal,
            Vertical,
            Circular
        }
        
        public enum ScreenEffectType
        {
            Flash,
            Fade,
            Tint,
            Blur,
            Distortion
        }
        
        /// <summary>
        /// CSV 데이터로부터 대화 생성
        /// </summary>
        public void InitializeFromCSV(string csvLine)
        {
            string[] values = csvLine.Split(',');
            
            if (values.Length >= 8)
            {
                dialogueId = values[0];
                chapterId = values[1];
                nodeId = values[2];
                speaker = values[3];
                dialogueText = values[4];
                
                if (Enum.TryParse<EmotionType>(values[5], true, out EmotionType parsedEmotion))
                    emotion = parsedEmotion;
                
                if (Enum.TryParse<VoiceEffectType>(values[6].Replace(" ", "_"), true, out VoiceEffectType parsedEffect))
                    voiceEffect = parsedEffect;
                
                // 선택지가 있는 경우 파싱
                if (values.Length > 7 && !string.IsNullOrEmpty(values[7]))
                {
                    ParseChoices(values[7]);
                }
            }
        }
        
        /// <summary>
        /// 선택지 문자열을 파싱하여 DialogueChoice 배열로 변환
        /// </summary>
        private void ParseChoices(string choicesString)
        {
            if (string.IsNullOrEmpty(choicesString)) return;
            
            string[] choiceStrings = choicesString.Split('|');
            choices = new DialogueChoice[choiceStrings.Length];
            
            for (int i = 0; i < choiceStrings.Length; i++)
            {
                var choice = new DialogueChoice();
                var parts = choiceStrings[i].Split(':');
                
                if (parts.Length >= 2)
                {
                    choice.choiceId = $"{dialogueId}_choice_{i}";
                    choice.choiceText = parts[0].Trim();
                    choice.nextNodeId = parts[1].Trim();
                }
                
                choices[i] = choice;
            }
        }
        
        /// <summary>
        /// 현재 게임 상태에서 사용 가능한 선택지 반환
        /// </summary>
        public DialogueChoice[] GetAvailableChoices()
        {
            if (choices == null) return new DialogueChoice[0];
            
            List<DialogueChoice> availableChoices = new List<DialogueChoice>();
            
            foreach (var choice in choices)
            {
                if (IsChoiceAvailable(choice))
                {
                    availableChoices.Add(choice);
                }
            }
            
            return availableChoices.ToArray();
        }
        
        /// <summary>
        /// 선택지 사용 가능 여부 확인
        /// </summary>
        private bool IsChoiceAvailable(DialogueChoice choice)
        {
            if (!choice.isEnabled) return false;
            if (choice.requirements == null) return true;
            
            foreach (var requirement in choice.requirements)
            {
                if (!CheckRequirement(requirement))
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 요구사항 확인
        /// </summary>
        private bool CheckRequirement(ChoiceRequirement requirement)
        {
            // TODO: 실제 게임 상태와 비교
            // 지금은 기본적으로 true 반환
            return true;
        }
        
        /// <summary>
        /// 선택지 효과 적용
        /// </summary>
        public void ApplyChoiceEffects(DialogueChoice choice)
        {
            if (choice.effects == null) return;
            
            foreach (var effect in choice.effects)
            {
                ApplyEffect(effect);
            }
        }
        
        /// <summary>
        /// 개별 효과 적용
        /// </summary>
        private void ApplyEffect(ChoiceEffect effect)
        {
            switch (effect.type)
            {
                case EffectType.ChangeReputation:
                    // TODO: 명성 변경
                    Debug.Log($"Reputation changed by {effect.value}");
                    break;
                    
                case EffectType.ChangeGold:
                    // TODO: 골드 변경
                    Debug.Log($"Gold changed by {effect.value}");
                    break;
                    
                case EffectType.ChangeRelation:
                    // TODO: 관계도 변경
                    Debug.Log($"Relation with {effect.targetId} changed by {effect.value}");
                    break;
                    
                case EffectType.UnlockStory:
                    // TODO: 스토리 언락
                    Debug.Log($"Story {effect.targetId} unlocked");
                    break;
                    
                case EffectType.TriggerEvent:
                    // TODO: 이벤트 트리거
                    Debug.Log($"Event {effect.targetId} triggered");
                    break;
            }
        }
        
        /// <summary>
        /// 다국어 대화 텍스트 반환
        /// </summary>
        public string GetLocalizedText(string languageCode = "ko")
        {
            if (localizedTexts != null)
            {
                foreach (var localizedText in localizedTexts)
                {
                    if (localizedText.languageCode == languageCode)
                        return localizedText.localizedDialogue;
                }
            }
            
            return dialogueText; // 기본 텍스트 반환
        }
        
        /// <summary>
        /// 감정에 따른 텍스트 스타일 반환
        /// </summary>
        public TextStyle GetTextStyle()
        {
            var style = new TextStyle();
            
            switch (emotion)
            {
                case EmotionType.분노:
                case EmotionType.격앙:
                    style.color = Color.red;
                    style.shakeIntensity = 2f;
                    style.typewriterSpeed = 0.03f;
                    break;
                    
                case EmotionType.슬픔:
                case EmotionType.후회:
                    style.color = new Color(0.7f, 0.8f, 1f);
                    style.typewriterSpeed = 0.08f;
                    break;
                    
                case EmotionType.놀람:
                case EmotionType.긴급:
                    style.color = Color.yellow;
                    style.shakeIntensity = 1f;
                    style.typewriterSpeed = 0.02f;
                    break;
                    
                case EmotionType.희망:
                case EmotionType.따뜻함:
                    style.color = new Color(1f, 0.9f, 0.7f);
                    style.glowIntensity = 0.5f;
                    break;
                    
                case EmotionType.외침:
                case EmotionType.명령:
                    style.fontSize = 22;
                    style.isBold = true;
                    break;
                    
                case EmotionType.마지막_숨:
                case EmotionType.약해지는_목소리:
                    style.color = new Color(0.8f, 0.8f, 0.8f, 0.7f);
                    style.typewriterSpeed = 0.1f;
                    style.fontSize = 16;
                    break;
            }
            
            return style;
        }
        
        [System.Serializable]
        public class TextStyle
        {
            public Color color = Color.white;
            public int fontSize = 18;
            public float typewriterSpeed = 0.05f;
            public float shakeIntensity = 0f;
            public float glowIntensity = 0f;
            public bool isBold = false;
            public bool isItalic = false;
        }
    }
}