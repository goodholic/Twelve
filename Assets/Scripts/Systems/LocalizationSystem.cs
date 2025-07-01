using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace GuildMaster.Systems
{
    public class LocalizationSystem : MonoBehaviour
    {
        private static LocalizationSystem _instance;
        public static LocalizationSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<LocalizationSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("LocalizationSystem");
                        _instance = go.AddComponent<LocalizationSystem>();
                    }
                }
                return _instance;
            }
        }
        
        [System.Serializable]
        public class LocalizationData
        {
            public string key;
            public string value;
        }
        
        [System.Serializable]
        public class LanguageData
        {
            public SystemLanguage language;
            public string languageCode;
            public string displayName;
            public string nativeName;
            public bool isRTL; // Right-to-left
            public List<LocalizationData> translations;
        }
        
        public enum TextCategory
        {
            UI,
            Tutorial,
            Story,
            Item,
            Skill,
            Character,
            System,
            Error
        }
        
        [Header("Language Settings")]
        [SerializeField] private SystemLanguage defaultLanguage = SystemLanguage.English;
        [SerializeField] private SystemLanguage currentLanguage;
        [SerializeField] private List<LanguageData> supportedLanguages;
        
        [Header("Localization Files")]
        [SerializeField] private string localizationPath = "Localization/";
        [SerializeField] private bool loadFromResources = true;
        
        private Dictionary<SystemLanguage, Dictionary<string, string>> languageDatabase;
        private Dictionary<string, string> currentTranslations;
        
        // 폰트 설정
        [Header("Font Settings")]
        [SerializeField] private Font defaultFont;
        [SerializeField] private Font koreanFont;
        [SerializeField] private Font japaneseFont;
        [SerializeField] private Font chineseFont;
        [SerializeField] private Font arabicFont;
        
        // 이벤트
        public event Action<SystemLanguage> OnLanguageChanged;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        
        void Initialize()
        {
            languageDatabase = new Dictionary<SystemLanguage, Dictionary<string, string>>();
            currentTranslations = new Dictionary<string, string>();
            
            // 지원 언어 초기화
            InitializeSupportedLanguages();
            
            // 저장된 언어 설정 로드
            LoadLanguagePreference();
            
            // 현재 언어 데이터 로드
            LoadLanguage(currentLanguage);
        }
        
        void InitializeSupportedLanguages()
        {
            if (supportedLanguages == null)
                supportedLanguages = new List<LanguageData>();
            
            // 기본 지원 언어 추가
            AddSupportedLanguage(SystemLanguage.English, "en", "English", "English");
            AddSupportedLanguage(SystemLanguage.Korean, "ko", "Korean", "한국어");
            AddSupportedLanguage(SystemLanguage.Japanese, "ja", "Japanese", "日本語");
            AddSupportedLanguage(SystemLanguage.ChineseSimplified, "zh-CN", "Chinese (Simplified)", "简体中文");
            AddSupportedLanguage(SystemLanguage.ChineseTraditional, "zh-TW", "Chinese (Traditional)", "繁體中文");
            AddSupportedLanguage(SystemLanguage.Spanish, "es", "Spanish", "Español");
            AddSupportedLanguage(SystemLanguage.French, "fr", "French", "Français");
            AddSupportedLanguage(SystemLanguage.German, "de", "German", "Deutsch");
            AddSupportedLanguage(SystemLanguage.Russian, "ru", "Russian", "Русский");
            AddSupportedLanguage(SystemLanguage.Portuguese, "pt", "Portuguese", "Português");
            
            // 각 언어별 기본 번역 추가
            AddDefaultTranslations();
        }
        
        void AddSupportedLanguage(SystemLanguage language, string code, string displayName, string nativeName)
        {
            var langData = new LanguageData
            {
                language = language,
                languageCode = code,
                displayName = displayName,
                nativeName = nativeName,
                isRTL = (language == SystemLanguage.Arabic || language == SystemLanguage.Hebrew),
                translations = new List<LocalizationData>()
            };
            
            supportedLanguages.Add(langData);
        }
        
        void AddDefaultTranslations()
        {
            // 영어 기본 번역
            AddTranslation(SystemLanguage.English, "game_title", "Guild Master");
            AddTranslation(SystemLanguage.English, "new_game", "New Game");
            AddTranslation(SystemLanguage.English, "continue", "Continue");
            AddTranslation(SystemLanguage.English, "settings", "Settings");
            AddTranslation(SystemLanguage.English, "quit", "Quit");
            
            // UI 요소
            AddTranslation(SystemLanguage.English, "ui_gold", "Gold");
            AddTranslation(SystemLanguage.English, "ui_wood", "Wood");
            AddTranslation(SystemLanguage.English, "ui_stone", "Stone");
            AddTranslation(SystemLanguage.English, "ui_manastone", "Mana Stone");
            AddTranslation(SystemLanguage.English, "ui_reputation", "Reputation");
            AddTranslation(SystemLanguage.English, "ui_level", "Level");
            AddTranslation(SystemLanguage.English, "ui_exp", "EXP");
            
            // 한국어 번역
            AddTranslation(SystemLanguage.Korean, "game_title", "길드 마스터");
            AddTranslation(SystemLanguage.Korean, "new_game", "새 게임");
            AddTranslation(SystemLanguage.Korean, "continue", "이어하기");
            AddTranslation(SystemLanguage.Korean, "settings", "설정");
            AddTranslation(SystemLanguage.Korean, "quit", "종료");
            
            AddTranslation(SystemLanguage.Korean, "ui_gold", "골드");
            AddTranslation(SystemLanguage.Korean, "ui_wood", "목재");
            AddTranslation(SystemLanguage.Korean, "ui_stone", "석재");
            AddTranslation(SystemLanguage.Korean, "ui_manastone", "마나석");
            AddTranslation(SystemLanguage.Korean, "ui_reputation", "명성");
            AddTranslation(SystemLanguage.Korean, "ui_level", "레벨");
            AddTranslation(SystemLanguage.Korean, "ui_exp", "경험치");
            
            // 일본어 번역
            AddTranslation(SystemLanguage.Japanese, "game_title", "ギルドマスター");
            AddTranslation(SystemLanguage.Japanese, "new_game", "新しいゲーム");
            AddTranslation(SystemLanguage.Japanese, "continue", "続ける");
            AddTranslation(SystemLanguage.Japanese, "settings", "設定");
            AddTranslation(SystemLanguage.Japanese, "quit", "終了");
            
            // 전투 관련
            AddBattleTranslations();
            
            // 건물 관련
            AddBuildingTranslations();
            
            // 스킬 관련
            AddSkillTranslations();
            
            // 시스템 메시지
            AddSystemTranslations();
        }
        
        void AddBattleTranslations()
        {
            // 영어
            AddTranslation(SystemLanguage.English, "battle_victory", "Victory!");
            AddTranslation(SystemLanguage.English, "battle_defeat", "Defeat...");
            AddTranslation(SystemLanguage.English, "battle_start", "Battle Start!");
            AddTranslation(SystemLanguage.English, "battle_retreat", "Retreat");
            AddTranslation(SystemLanguage.English, "battle_auto", "Auto Battle");
            
            // 한국어
            AddTranslation(SystemLanguage.Korean, "battle_victory", "승리!");
            AddTranslation(SystemLanguage.Korean, "battle_defeat", "패배...");
            AddTranslation(SystemLanguage.Korean, "battle_start", "전투 시작!");
            AddTranslation(SystemLanguage.Korean, "battle_retreat", "후퇴");
            AddTranslation(SystemLanguage.Korean, "battle_auto", "자동 전투");
            
            // 일본어
            AddTranslation(SystemLanguage.Japanese, "battle_victory", "勝利！");
            AddTranslation(SystemLanguage.Japanese, "battle_defeat", "敗北...");
            AddTranslation(SystemLanguage.Japanese, "battle_start", "戦闘開始！");
            AddTranslation(SystemLanguage.Japanese, "battle_retreat", "撤退");
            AddTranslation(SystemLanguage.Japanese, "battle_auto", "オートバトル");
        }
        
        void AddBuildingTranslations()
        {
            // 영어
            AddTranslation(SystemLanguage.English, "building_guildhall", "Guild Hall");
            AddTranslation(SystemLanguage.English, "building_barracks", "Barracks");
            AddTranslation(SystemLanguage.English, "building_tavern", "Tavern");
            AddTranslation(SystemLanguage.English, "building_blacksmith", "Blacksmith");
            AddTranslation(SystemLanguage.English, "building_magictower", "Magic Tower");
            AddTranslation(SystemLanguage.English, "building_temple", "Temple");
            AddTranslation(SystemLanguage.English, "building_market", "Market");
            AddTranslation(SystemLanguage.English, "building_library", "Library");
            AddTranslation(SystemLanguage.English, "building_training", "Training Ground");
            AddTranslation(SystemLanguage.English, "building_warehouse", "Warehouse");
            
            // 한국어
            AddTranslation(SystemLanguage.Korean, "building_guildhall", "길드 홀");
            AddTranslation(SystemLanguage.Korean, "building_barracks", "병영");
            AddTranslation(SystemLanguage.Korean, "building_tavern", "주점");
            AddTranslation(SystemLanguage.Korean, "building_blacksmith", "대장간");
            AddTranslation(SystemLanguage.Korean, "building_magictower", "마법탑");
            AddTranslation(SystemLanguage.Korean, "building_temple", "신전");
            AddTranslation(SystemLanguage.Korean, "building_market", "시장");
            AddTranslation(SystemLanguage.Korean, "building_library", "도서관");
            AddTranslation(SystemLanguage.Korean, "building_training", "훈련장");
            AddTranslation(SystemLanguage.Korean, "building_warehouse", "창고");
        }
        
        void AddSkillTranslations()
        {
            // 영어
            AddTranslation(SystemLanguage.English, "skill_attack", "Attack");
            AddTranslation(SystemLanguage.English, "skill_defense", "Defense");
            AddTranslation(SystemLanguage.English, "skill_heal", "Heal");
            AddTranslation(SystemLanguage.English, "skill_buff", "Buff");
            AddTranslation(SystemLanguage.English, "skill_debuff", "Debuff");
            
            // 한국어
            AddTranslation(SystemLanguage.Korean, "skill_attack", "공격");
            AddTranslation(SystemLanguage.Korean, "skill_defense", "방어");
            AddTranslation(SystemLanguage.Korean, "skill_heal", "치유");
            AddTranslation(SystemLanguage.Korean, "skill_buff", "버프");
            AddTranslation(SystemLanguage.Korean, "skill_debuff", "디버프");
        }
        
        void AddSystemTranslations()
        {
            // 영어
            AddTranslation(SystemLanguage.English, "system_save", "Save Game");
            AddTranslation(SystemLanguage.English, "system_load", "Load Game");
            AddTranslation(SystemLanguage.English, "system_saved", "Game Saved");
            AddTranslation(SystemLanguage.English, "system_loaded", "Game Loaded");
            AddTranslation(SystemLanguage.English, "system_error", "Error");
            AddTranslation(SystemLanguage.English, "system_confirm", "Confirm");
            AddTranslation(SystemLanguage.English, "system_cancel", "Cancel");
            
            // 한국어
            AddTranslation(SystemLanguage.Korean, "system_save", "게임 저장");
            AddTranslation(SystemLanguage.Korean, "system_load", "게임 불러오기");
            AddTranslation(SystemLanguage.Korean, "system_saved", "저장되었습니다");
            AddTranslation(SystemLanguage.Korean, "system_loaded", "불러왔습니다");
            AddTranslation(SystemLanguage.Korean, "system_error", "오류");
            AddTranslation(SystemLanguage.Korean, "system_confirm", "확인");
            AddTranslation(SystemLanguage.Korean, "system_cancel", "취소");
        }
        
        void AddTranslation(SystemLanguage language, string key, string value)
        {
            if (!languageDatabase.ContainsKey(language))
            {
                languageDatabase[language] = new Dictionary<string, string>();
            }
            
            languageDatabase[language][key] = value;
            
            // supportedLanguages에도 추가
            var langData = supportedLanguages.Find(l => l.language == language);
            if (langData != null)
            {
                langData.translations.Add(new LocalizationData { key = key, value = value });
            }
        }
        
        // 언어 변경
        public void SetLanguage(SystemLanguage language)
        {
            if (currentLanguage == language) return;
            
            LoadLanguage(language);
            currentLanguage = language;
            SaveLanguagePreference();
            
            // 폰트 변경
            UpdateFonts();
            
            // 모든 텍스트 업데이트
            UpdateAllTexts();
            
            OnLanguageChanged?.Invoke(language);
        }
        
        void LoadLanguage(SystemLanguage language)
        {
            currentTranslations.Clear();
            
            // 데이터베이스에서 로드
            if (languageDatabase.ContainsKey(language))
            {
                currentTranslations = new Dictionary<string, string>(languageDatabase[language]);
            }
            
            // 파일에서 추가 로드 (CSV 또는 JSON)
            if (loadFromResources)
            {
                LoadLanguageFromFile(language);
            }
        }
        
        void LoadLanguageFromFile(SystemLanguage language)
        {
            var langData = supportedLanguages.Find(l => l.language == language);
            if (langData == null) return;
            
            string filePath = localizationPath + langData.languageCode + ".csv";
            TextAsset csvFile = Resources.Load<TextAsset>(filePath);
            
            if (csvFile != null)
            {
                ParseCSV(csvFile.text, language);
            }
        }
        
        void ParseCSV(string csvText, SystemLanguage language)
        {
            string[] lines = csvText.Split('\n');
            
            for (int i = 1; i < lines.Length; i++) // Skip header
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                string[] values = line.Split(',');
                if (values.Length >= 2)
                {
                    string key = values[0].Trim();
                    string value = values[1].Trim();
                    
                    // CSV 이스케이프 처리
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        value = value.Substring(1, value.Length - 2);
                        value = value.Replace("\"\"", "\"");
                    }
                    
                    currentTranslations[key] = value;
                }
            }
        }
        
        // 번역 가져오기
        public string GetTranslation(string key, params object[] args)
        {
            if (currentTranslations.ContainsKey(key))
            {
                string translation = currentTranslations[key];
                
                // 파라미터 치환
                if (args != null && args.Length > 0)
                {
                    try
                    {
                        translation = string.Format(translation, args);
                    }
                    catch (FormatException)
                    {
                        Debug.LogWarning($"Format error in translation key: {key}");
                    }
                }
                
                return translation;
            }
            
            // 기본 언어에서 찾기
            if (currentLanguage != defaultLanguage && languageDatabase.ContainsKey(defaultLanguage))
            {
                if (languageDatabase[defaultLanguage].ContainsKey(key))
                {
                    return languageDatabase[defaultLanguage][key];
                }
            }
            
            Debug.LogWarning($"Translation not found for key: {key}");
            return $"[{key}]";
        }
        
        // 짧은 별칭
        public string T(string key, params object[] args)
        {
            return GetTranslation(key, args);
        }
        
        // 복수형 처리
        public string GetPluralTranslation(string key, int count)
        {
            string pluralKey = $"{key}_plural_{GetPluralForm(count)}";
            
            if (currentTranslations.ContainsKey(pluralKey))
            {
                return string.Format(currentTranslations[pluralKey], count);
            }
            
            // 기본 키로 폴백
            return GetTranslation(key, count);
        }
        
        int GetPluralForm(int count)
        {
            // 언어별 복수형 규칙
            switch (currentLanguage)
            {
                case SystemLanguage.Korean:
                case SystemLanguage.Japanese:
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return 0; // 복수형 없음
                    
                case SystemLanguage.Russian:
                    // 러시아어 복수형 규칙
                    if (count % 10 == 1 && count % 100 != 11) return 0;
                    if (count % 10 >= 2 && count % 10 <= 4 && (count % 100 < 10 || count % 100 >= 20)) return 1;
                    return 2;
                    
                default: // 영어 및 기타
                    return count == 1 ? 0 : 1;
            }
        }
        
        // 폰트 업데이트
        void UpdateFonts()
        {
            Font targetFont = GetFontForLanguage(currentLanguage);
            
            // 모든 Text 컴포넌트 업데이트
            Text[] allTexts = FindObjectsOfType<Text>();
            foreach (var text in allTexts)
            {
                if (text.font == defaultFont || IsSystemFont(text.font))
                {
                    text.font = targetFont;
                }
            }
        }
        
        Font GetFontForLanguage(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Korean:
                    return koreanFont ?? defaultFont;
                    
                case SystemLanguage.Japanese:
                    return japaneseFont ?? defaultFont;
                    
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return chineseFont ?? defaultFont;
                    
                case SystemLanguage.Arabic:
                    return arabicFont ?? defaultFont;
                    
                default:
                    return defaultFont;
            }
        }
        
        bool IsSystemFont(Font font)
        {
            return font == defaultFont || font == koreanFont || font == japaneseFont || 
                   font == chineseFont || font == arabicFont;
        }
        
        // 모든 텍스트 업데이트
        void UpdateAllTexts()
        {
            LocalizedText[] localizedTexts = FindObjectsOfType<LocalizedText>();
            foreach (var localizedText in localizedTexts)
            {
                localizedText.UpdateText();
            }
        }
        
        // 설정 저장/로드
        void SaveLanguagePreference()
        {
            PlayerPrefs.SetString("Language", currentLanguage.ToString());
            PlayerPrefs.Save();
        }
        
        void LoadLanguagePreference()
        {
            string savedLanguage = PlayerPrefs.GetString("Language", "");
            
            if (!string.IsNullOrEmpty(savedLanguage))
            {
                try
                {
                    currentLanguage = (SystemLanguage)Enum.Parse(typeof(SystemLanguage), savedLanguage);
                }
                catch
                {
                    currentLanguage = defaultLanguage;
                }
            }
            else
            {
                // 시스템 언어 감지
                currentLanguage = Application.systemLanguage;
                
                // 지원하지 않는 언어면 기본값 사용
                if (!IsLanguageSupported(currentLanguage))
                {
                    currentLanguage = defaultLanguage;
                }
            }
        }
        
        public bool IsLanguageSupported(SystemLanguage language)
        {
            return supportedLanguages.Exists(l => l.language == language);
        }
        
        public List<LanguageData> GetSupportedLanguages()
        {
            return supportedLanguages;
        }
        
        public SystemLanguage GetCurrentLanguage()
        {
            return currentLanguage;
        }
        
        // RTL 지원
        public bool IsRTL()
        {
            var langData = supportedLanguages.Find(l => l.language == currentLanguage);
            return langData != null && langData.isRTL;
        }
        
        // 동적 번역 추가
        public void AddDynamicTranslation(string key, string value, SystemLanguage? language = null)
        {
            SystemLanguage targetLang = language ?? currentLanguage;
            
            if (!languageDatabase.ContainsKey(targetLang))
            {
                languageDatabase[targetLang] = new Dictionary<string, string>();
            }
            
            languageDatabase[targetLang][key] = value;
            
            if (targetLang == currentLanguage)
            {
                currentTranslations[key] = value;
            }
        }
        
        // CSV 내보내기 (에디터용)
        public void ExportToCSV(SystemLanguage language, string filePath)
        {
            if (!languageDatabase.ContainsKey(language))
            {
                Debug.LogError($"No translations found for {language}");
                return;
            }
            
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Key,Value");
                
                foreach (var kvp in languageDatabase[language])
                {
                    string value = kvp.Value.Replace("\"", "\"\"");
                    writer.WriteLine($"{kvp.Key},\"{value}\"");
                }
            }
            
            Debug.Log($"Exported {language} translations to {filePath}");
        }
    }
    
    // UI 컴포넌트용 헬퍼 클래스
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string localizationKey;
        [SerializeField] private object[] parameters;
        private Text textComponent;
        
        void Start()
        {
            textComponent = GetComponent<Text>();
            UpdateText();
            
            LocalizationSystem.Instance.OnLanguageChanged += OnLanguageChanged;
        }
        
        void OnDestroy()
        {
            if (LocalizationSystem.Instance != null)
            {
                LocalizationSystem.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
        }
        
        void OnLanguageChanged(SystemLanguage newLanguage)
        {
            UpdateText();
        }
        
        public void UpdateText()
        {
            if (textComponent != null && !string.IsNullOrEmpty(localizationKey))
            {
                textComponent.text = LocalizationSystem.Instance.GetTranslation(localizationKey, parameters);
            }
        }
        
        public void SetKey(string key, params object[] args)
        {
            localizationKey = key;
            parameters = args;
            UpdateText();
        }
    }
}