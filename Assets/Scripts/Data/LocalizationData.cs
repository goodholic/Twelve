using UnityEngine;
using System;
using System.Collections.Generic;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class LocalizationData
    {
        [Header("기본 정보")]
        public string key;
        public string category;
        public LocalizationType type = LocalizationType.UI;
        
        // CSVDataManager 호환성 속성
        public string value { get => GetText(SystemLanguage.Korean); set => SetText(SystemLanguage.Korean, value); }
        
        [Header("언어별 텍스트")]
        public string korean = "";
        public string english = "";
        public string japanese = "";
        public string chinese = "";
        public string spanish = "";
        
        [Header("메타데이터")]
        public string context = "";
        public string description = "";
        public bool isPlural = false;
        public int maxLength = 0;
        
        [Header("상태")]
        public bool needsTranslation = false;
        public bool isCompleted = false;
        public DateTime lastModified;
        
        public LocalizationData()
        {
            key = "";
            lastModified = DateTime.Now;
        }
        
        public LocalizationData(string locKey, string kr, string en = "")
        {
            key = locKey;
            korean = kr;
            english = en;
            lastModified = DateTime.Now;
        }
        
        public string GetText(SystemLanguage language)
        {
            return language switch
            {
                SystemLanguage.Korean => korean,
                SystemLanguage.English => english,
                SystemLanguage.Japanese => japanese,
                SystemLanguage.Chinese => chinese,
                SystemLanguage.Spanish => spanish,
                _ => korean // 기본값은 한국어
            };
        }
        
        public bool HasTranslation(SystemLanguage language)
        {
            string text = GetText(language);
            return !string.IsNullOrEmpty(text);
        }
        
        public void SetText(SystemLanguage language, string text)
        {
            switch (language)
            {
                case SystemLanguage.Korean:
                    korean = text;
                    break;
                case SystemLanguage.English:
                    english = text;
                    break;
                case SystemLanguage.Japanese:
                    japanese = text;
                    break;
                case SystemLanguage.Chinese:
                    chinese = text;
                    break;
                case SystemLanguage.Spanish:
                    spanish = text;
                    break;
            }
            lastModified = DateTime.Now;
        }
        
        public bool IsValidLength(SystemLanguage language)
        {
            if (maxLength <= 0) return true;
            
            string text = GetText(language);
            return text.Length <= maxLength;
        }
        
        public float GetCompletionPercentage()
        {
            int totalLanguages = 5; // 지원하는 언어 수
            int translatedCount = 0;
            
            if (!string.IsNullOrEmpty(korean)) translatedCount++;
            if (!string.IsNullOrEmpty(english)) translatedCount++;
            if (!string.IsNullOrEmpty(japanese)) translatedCount++;
            if (!string.IsNullOrEmpty(chinese)) translatedCount++;
            if (!string.IsNullOrEmpty(spanish)) translatedCount++;
            
            return (float)translatedCount / totalLanguages;
        }
    }
    
    public enum LocalizationType
    {
        UI,
        Dialogue,
        Item,
        Skill,
        Quest,
        Character,
        Error,
        Tutorial,
        Story
    }
} 