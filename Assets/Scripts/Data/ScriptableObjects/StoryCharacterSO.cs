using UnityEngine;
using System;
using System.Collections.Generic;

namespace GuildMaster.Data
{
    /// <summary>
    /// 스토리 캐릭터 ScriptableObject
    /// CSV 데이터를 기반으로 생성되는 캐릭터 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "StoryCharacter", menuName = "GuildMaster/Data/StoryCharacter", order = 6)]
    public class StoryCharacterSO : ScriptableObject
    {
        [Header("Character Identity")]
        public string characterId;
        public string characterName;
        public int age;
        public Gender gender;
        public string role;
        
        [Header("Character Description")]
        [TextArea(3, 5)]
        public string description;
        [TextArea(2, 4)]
        public string personality;
        
        [Header("Visual & Audio")]
        public Sprite characterPortrait;
        public VoiceType voiceType;
        public AudioClip[] voiceSamples;
        
        [Header("Story Progression")]
        public string firstAppearance;
        public bool isMainCharacter;
        public bool isAvailableFromStart;
        
        [Header("Character Relationships")]
        public CharacterRelationship[] relationships;
        
        [Header("Voice Effects")]
        public float voicePitch = 1.0f;
        public float voiceSpeed = 1.0f;
        public AudioEffect[] voiceEffects;
        
        public enum Gender
        {
            Male,
            Female,
            Unknown
        }
        
        public enum VoiceType
        {
            중후한_중년_남성,
            차가운_지성형,
            낮고_거친_목소리,
            부드러운_지성형,
            젊고_힘찬_목소리,
            날카롭고_단호한,
            청년의_희망찬_목소리,
            경쾌한_중년,
            차분한_여성,
            어린아이
        }
        
        [System.Serializable]
        public class CharacterRelationship
        {
            public string relatedCharacterId;
            public RelationType relationType;
            public int relationshipValue; // -100 to 100
            public string relationshipDescription;
        }
        
        public enum RelationType
        {
            Family,      // 가족
            Friend,      // 친구
            Enemy,       // 적
            Rival,       // 라이벌
            Mentor,      // 멘토
            Student,     // 제자
            Ally,        // 동맹
            Neutral,     // 중립
            Unknown      // 미지
        }
        
        [System.Serializable]
        public class AudioEffect
        {
            public EffectType effectType;
            public float intensity;
            public bool isActive;
        }
        
        public enum EffectType
        {
            Echo,        // 에코
            Reverb,      // 리버브
            Distortion,  // 왜곡
            LowPass,     // 저음 필터
            HighPass,    // 고음 필터
            Tremolo      // 트레몰로
        }
        
        /// <summary>
        /// CSV 데이터로부터 캐릭터 생성
        /// </summary>
        public void InitializeFromCSV(string csvLine)
        {
            string[] values = csvLine.Split(',');
            
            if (values.Length >= 9)
            {
                characterId = values[0];
                characterName = values[1];
                
                if (int.TryParse(values[2], out int parsedAge))
                    age = parsedAge;
                
                if (Enum.TryParse<Gender>(values[3], true, out Gender parsedGender))
                    gender = parsedGender;
                
                role = values[4];
                description = values[5];
                personality = values[6];
                
                if (Enum.TryParse<VoiceType>(values[7].Replace(" ", "_"), true, out VoiceType parsedVoice))
                    voiceType = parsedVoice;
                
                firstAppearance = values[8];
                
                // 메인 캐릭터 판정
                isMainCharacter = role.Contains("길드 마스터") || role.Contains("주인공") || role.Contains("족장");
                isAvailableFromStart = firstAppearance.Contains("chapter_1");
            }
        }
        
        /// <summary>
        /// 캐릭터의 감정에 따른 보이스 설정 반환
        /// </summary>
        public VoiceSettings GetVoiceSettingsForEmotion(string emotion)
        {
            var settings = new VoiceSettings
            {
                pitch = voicePitch,
                speed = voiceSpeed,
                volume = 1.0f
            };
            
            switch (emotion?.ToLower())
            {
                case "분노":
                case "격앙":
                    settings.pitch *= 1.2f;
                    settings.speed *= 1.1f;
                    settings.volume *= 1.2f;
                    break;
                    
                case "슬픔":
                case "후회":
                    settings.pitch *= 0.8f;
                    settings.speed *= 0.9f;
                    settings.volume *= 0.8f;
                    break;
                    
                case "놀람":
                case "긴급":
                    settings.pitch *= 1.3f;
                    settings.speed *= 1.2f;
                    break;
                    
                case "조용함":
                case "차분함":
                    settings.pitch *= 0.9f;
                    settings.speed *= 0.8f;
                    settings.volume *= 0.7f;
                    break;
                    
                case "외침":
                case "명령":
                    settings.volume *= 1.5f;
                    settings.pitch *= 1.1f;
                    break;
                    
                case "약해지는_목소리":
                case "마지막":
                    settings.pitch *= 0.7f;
                    settings.speed *= 0.6f;
                    settings.volume *= 0.5f;
                    break;
            }
            
            return settings;
        }
        
        /// <summary>
        /// 캐릭터간 관계도 확인
        /// </summary>
        public CharacterRelationship GetRelationshipWith(string otherCharacterId)
        {
            foreach (var relationship in relationships)
            {
                if (relationship.relatedCharacterId == otherCharacterId)
                    return relationship;
            }
            
            return null;
        }
        
        /// <summary>
        /// 캐릭터의 감정 상태에 따른 대화 스타일 반환
        /// </summary>
        public DialogueStyle GetDialogueStyle(string emotion)
        {
            var style = new DialogueStyle();
            
            // 캐릭터별 기본 스타일
            switch (voiceType)
            {
                case VoiceType.중후한_중년_남성:
                    style.typewriterSpeed = 0.05f;
                    style.textColor = Color.white;
                    style.fontSize = 18;
                    break;
                    
                case VoiceType.차가운_지성형:
                    style.typewriterSpeed = 0.03f;
                    style.textColor = new Color(0.8f, 0.9f, 1f);
                    style.fontSize = 17;
                    break;
                    
                case VoiceType.낮고_거친_목소리:
                    style.typewriterSpeed = 0.07f;
                    style.textColor = new Color(0.9f, 0.8f, 0.6f);
                    style.fontSize = 19;
                    break;
                    
                case VoiceType.어린아이:
                    style.typewriterSpeed = 0.04f;
                    style.textColor = new Color(1f, 0.9f, 0.8f);
                    style.fontSize = 16;
                    break;
            }
            
            // 감정에 따른 스타일 조정
            switch (emotion?.ToLower())
            {
                case "분노":
                case "격앙":
                    style.textColor = Color.red;
                    style.typewriterSpeed *= 0.8f;
                    style.shakeIntensity = 2f;
                    break;
                    
                case "슬픔":
                case "후회":
                    style.textColor = new Color(0.7f, 0.8f, 1f);
                    style.typewriterSpeed *= 1.3f;
                    break;
                    
                case "놀람":
                    style.textColor = Color.yellow;
                    style.typewriterSpeed *= 0.6f;
                    style.shakeIntensity = 1f;
                    break;
                    
                case "희망":
                case "따뜻함":
                    style.textColor = new Color(1f, 0.9f, 0.7f);
                    style.glowIntensity = 0.5f;
                    break;
            }
            
            return style;
        }
        
        [System.Serializable]
        public class VoiceSettings
        {
            public float pitch = 1.0f;
            public float speed = 1.0f;
            public float volume = 1.0f;
            public bool hasEcho = false;
            public bool hasReverb = false;
        }
        
        [System.Serializable]
        public class DialogueStyle
        {
            public float typewriterSpeed = 0.05f;
            public Color textColor = Color.white;
            public int fontSize = 18;
            public float shakeIntensity = 0f;
            public float glowIntensity = 0f;
            public bool hasBoldEffect = false;
            public bool hasItalicEffect = false;
        }
    }
}