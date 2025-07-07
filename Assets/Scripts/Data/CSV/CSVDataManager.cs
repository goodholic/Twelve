using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GuildMaster.Battle;

namespace GuildMaster.Data
{
    /// <summary>
    /// 상용 게임 수준 CSV 기반 데이터 관리 시스템
    /// - 캐릭터, 스킬, 건물, 아이템 등 모든 게임 데이터를 CSV로 관리
    /// - 런타임 로딩 및 핫픽스 지원
    /// - 다국어 지원 (한국어, 영어)
    /// - 데이터 검증 및 오류 처리
    /// </summary>
    public class CSVDataManager : MonoBehaviour
    {
        public static CSVDataManager Instance { get; private set; }
        
        [Header("데이터 파일 경로")]
        [SerializeField] private string dataFolderPath = "CSV/";
        [SerializeField] private bool loadFromResources = true;
        [SerializeField] private bool enableHotReload = true;
        
        [Header("지원 언어")]
        [SerializeField] private SystemLanguage currentLanguage = SystemLanguage.Korean;
        [SerializeField] private List<SystemLanguage> supportedLanguages = new List<SystemLanguage> 
        { 
            SystemLanguage.Korean, 
            SystemLanguage.English 
        };
        
        // 데이터 저장소
        private Dictionary<string, CharacterData> characterDataDict = new Dictionary<string, CharacterData>();
        private Dictionary<string, SkillData> skillDataDict = new Dictionary<string, SkillData>();
        // private Dictionary<string, BuildingDataCSV> buildingDataDict = new Dictionary<string, BuildingDataCSV>(); // Commented out - BuildingDataCSV removed
        private Dictionary<string, ItemData> itemDataDict = new Dictionary<string, ItemData>();
        private Dictionary<string, QuestData> questDataDict = new Dictionary<string, QuestData>();
        // LocalizationData 타입이 제거되어 주석 처리
        // private Dictionary<string, LocalizationData> localizationDict = new Dictionary<string, LocalizationData>();
        private Dictionary<string, EnemyData> enemyDataDict = new Dictionary<string, EnemyData>();
        private Dictionary<string, FormationData> formationDataDict = new Dictionary<string, FormationData>();
        
        // 이벤트
        public event Action OnDataLoaded;
        public event Action<string> OnDataLoadError;
        public event Action<SystemLanguage> OnLanguageChanged;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDataManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 공개 초기화 메서드 (GameManager에서 호출)
        /// </summary>
        public void Initialize()
        {
            InitializeDataManager();
        }
        
        /// <summary>
        /// 데이터 매니저 초기화
        /// </summary>
        private void InitializeDataManager()
        {
            currentLanguage = Application.systemLanguage;
            if (!supportedLanguages.Contains(currentLanguage))
            {
                currentLanguage = SystemLanguage.Korean; // 기본 언어
            }
            
            LoadAllData();
        }
        
        /// <summary>
        /// 모든 CSV 데이터 로드
        /// </summary>
        public void LoadAllData()
        {
            try
            {
                Debug.Log("CSV 데이터 로딩 시작...");
                
                // 각 데이터 타입별 로드
                LoadCharacterData();
                LoadSkillData();
                // LoadBuildingData(); // Commented out - building data removed
                LoadItemData();
                LoadQuestData();
                LoadEnemyData();
                LoadFormationData();
                LoadLocalizationData();
                
                OnDataLoaded?.Invoke();
                Debug.Log($"CSV 데이터 로딩 완료! (언어: {currentLanguage})");
            }
            catch (Exception e)
            {
                string errorMessage = $"데이터 로딩 실패: {e.Message}";
                Debug.LogError(errorMessage);
                OnDataLoadError?.Invoke(errorMessage);
            }
        }
        
        /// <summary>
        /// 캐릭터 데이터 로드
        /// </summary>
        private void LoadCharacterData()
        {
            string csvContent = LoadCSVFile("CharacterData");
            if (string.IsNullOrEmpty(csvContent)) return;
            
            characterDataDict.Clear();
            string[] lines = csvContent.Split('\n');
            
            // 헤더 스킵하고 데이터 파싱
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var data = ParseCharacterData(lines[i]);
                if (data != null)
                {
                    characterDataDict[data.id] = data;
                }
            }
            
            Debug.Log($"캐릭터 데이터 로드 완료: {characterDataDict.Count}개");
        }
        
        /// <summary>
        /// 스킬 데이터 로드
        /// </summary>
        private void LoadSkillData()
        {
            string csvContent = LoadCSVFile("SkillData");
            if (string.IsNullOrEmpty(csvContent)) return;
            
            skillDataDict.Clear();
            string[] lines = csvContent.Split('\n');
            
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var data = ParseSkillData(lines[i]);
                if (data != null)
                {
                    skillDataDict[data.id] = data;
                }
            }
            
            Debug.Log($"스킬 데이터 로드 완료: {skillDataDict.Count}개");
        }
        
        // /// <summary>
        // /// 건물 데이터 로드
        // /// </summary>
        // private void LoadBuildingData()
        // {
        //     string csvContent = LoadCSVFile("BuildingData");
        //     if (string.IsNullOrEmpty(csvContent)) return;
        //     
        //     buildingDataDict.Clear();
        //     string[] lines = csvContent.Split('\n');
        //     
        //     for (int i = 1; i < lines.Length; i++)
        //     {
        //         if (string.IsNullOrWhiteSpace(lines[i])) continue;
        //         
        //         var data = ParseBuildingData(lines[i]);
        //         if (data != null)
        //         {
        //             buildingDataDict[data.id] = data;
        //         }
        //     }
        //     
        //     Debug.Log($"건물 데이터 로드 완료: {buildingDataDict.Count}개");
        // }
        
        /// <summary>
        /// 아이템 데이터 로드
        /// </summary>
        private void LoadItemData()
        {
            string csvContent = LoadCSVFile("ItemData");
            if (string.IsNullOrEmpty(csvContent)) return;
            
            itemDataDict.Clear();
            string[] lines = csvContent.Split('\n');
            
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var data = ParseItemData(lines[i]);
                if (data != null)
                {
                    itemDataDict[data.id] = data;
                }
            }
            
            Debug.Log($"아이템 데이터 로드 완료: {itemDataDict.Count}개");
        }
        
        /// <summary>
        /// 퀘스트 데이터 로드
        /// </summary>
        private void LoadQuestData()
        {
            string csvContent = LoadCSVFile("QuestData");
            if (string.IsNullOrEmpty(csvContent)) return;
            
            questDataDict.Clear();
            string[] lines = csvContent.Split('\n');
            
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var data = ParseQuestData(lines[i]);
                if (data != null)
                {
                    questDataDict[data.id] = data;
                }
            }
            
            Debug.Log($"퀘스트 데이터 로드 완료: {questDataDict.Count}개");
        }
        
        /// <summary>
        /// 적 데이터 로드
        /// </summary>
        private void LoadEnemyData()
        {
            string csvContent = LoadCSVFile("EnemyData");
            if (string.IsNullOrEmpty(csvContent)) return;
            
            enemyDataDict.Clear();
            string[] lines = csvContent.Split('\n');
            
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var data = ParseEnemyData(lines[i]);
                if (data != null)
                {
                    enemyDataDict[data.enemyId] = data;
                }
            }
            
            Debug.Log($"적 데이터 로드 완료: {enemyDataDict.Count}개");
        }
        
        /// <summary>
        /// 편성 데이터 로드
        /// </summary>
        private void LoadFormationData()
        {
            string csvContent = LoadCSVFile("FormationData");
            if (string.IsNullOrEmpty(csvContent)) return;
            
            formationDataDict.Clear();
            string[] lines = csvContent.Split('\n');
            
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var data = ParseFormationData(lines[i]);
                if (data != null)
                {
                    formationDataDict[data.formationId] = data;
                }
            }
            
            Debug.Log($"편성 데이터 로드 완료: {formationDataDict.Count}개");
        }
        
        /// <summary>
        /// 다국어 데이터 로드
        /// </summary>
        // LocalizationData 타입이 제거되어 주석 처리
        private void LoadLocalizationData()
        {
            // string fileName = $"Localization_{GetLanguageCode(currentLanguage)}";
            // string csvContent = LoadCSVFile(fileName);
            // if (string.IsNullOrEmpty(csvContent)) return;
            // 
            // localizationDict.Clear();
            // string[] lines = csvContent.Split('\n');
            // 
            // for (int i = 1; i < lines.Length; i++)
            // {
            //     if (string.IsNullOrWhiteSpace(lines[i])) continue;
            //     
            //     var data = ParseLocalizationData(lines[i]);
            //     if (data != null)
            //     {
            //         localizationDict[data.key] = data;
            //     }
            // }
            // 
            // Debug.Log($"다국어 데이터 로드 완료: {localizationDict.Count}개 ({currentLanguage})");
            
            Debug.Log("LocalizationData 타입이 제거되어 다국어 데이터를 로드할 수 없습니다.");
        }
        
        /// <summary>
        /// CSV 파일 로드
        /// </summary>
        private string LoadCSVFile(string fileName)
        {
            try
            {
                if (loadFromResources)
                {
                    string resourcePath = Path.Combine(dataFolderPath, fileName).Replace("\\", "/");
                    TextAsset csvFile = Resources.Load<TextAsset>(resourcePath);
                    
                    if (csvFile != null)
                    {
                        return csvFile.text;
                    }
                    else
                    {
                        Debug.LogWarning($"CSV 파일을 찾을 수 없습니다: {resourcePath}");
                        return null;
                    }
                }
                else
                {
                    string filePath = Path.Combine(Application.streamingAssetsPath, dataFolderPath, fileName + ".csv");
                    if (File.Exists(filePath))
                    {
                        return File.ReadAllText(filePath, Encoding.UTF8);
                    }
                    else
                    {
                        Debug.LogWarning($"CSV 파일을 찾을 수 없습니다: {filePath}");
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"CSV 파일 로드 실패 ({fileName}): {e.Message}");
                return null;
            }
        }
        
        // 데이터 파싱 메서드들
        private CharacterData ParseCharacterData(string csvLine)
        {
            try
            {
                string[] values = SplitCSVLine(csvLine);
                if (values.Length < 15) return null;
                
                return new CharacterData
                {
                    id = values[0],
                    nameKey = values[1],
                    jobClass = (JobClass)Enum.Parse(typeof(JobClass), values[2]),
                    rarity = (CharacterRarity)Enum.Parse(typeof(CharacterRarity), values[3]),
                    baseHP = int.Parse(values[4]),
                    baseAttack = int.Parse(values[5]),
                    baseDefense = int.Parse(values[6]),
                    baseMagicPower = int.Parse(values[7]),
                    baseSpeed = int.Parse(values[8]),
                    critRate = float.Parse(values[9]),
                    skill1Id = values[10],
                    skill2Id = values[11],
                    skill3Id = values[12],
                    level = int.Parse(values[13]),
                    description = values[14]
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"캐릭터 데이터 파싱 실패: {csvLine} - {e.Message}");
                return null;
            }
        }
        
        private SkillData ParseSkillData(string csvLine)
        {
            try
            {
                string[] values = SplitCSVLine(csvLine);
                if (values.Length < 12) return null;
                
                return new SkillData
                {
                    id = values[0],
                    nameKey = values[1],
                    descriptionKey = values[2],
                    skillType = (SkillType)Enum.Parse(typeof(SkillType), values[3]),
                    targetType = (TargetType)Enum.Parse(typeof(TargetType), values[4]),
                    baseDamage = int.Parse(values[5]),
                    cooldown = int.Parse(values[6]),
                    manaCost = int.Parse(values[7]),
                    requiredLevel = int.Parse(values[8]),
                    requiredJobClass = (JobClass)Enum.Parse(typeof(JobClass), values[9]),
                    effectValues = ParseFloatList(values[10]).ToArray(),
                    iconPath = values[11]
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"스킬 데이터 파싱 실패: {csvLine} - {e.Message}");
                return null;
            }
        }
        
        // private BuildingDataCSV ParseBuildingData(string csvLine)
        // {
        //     try
        //     {
        //         string[] values = SplitCSVLine(csvLine);
        //         if (values.Length < 13) return null;
        //         
        //         return new BuildingDataCSV
        //         {
        //             id = values[0],
        //             nameKey = values[1],
        //             descriptionKey = values[2],
        //             buildingType = (BuildingType)Enum.Parse(typeof(BuildingType), values[3]),
        //             maxLevel = int.Parse(values[4]),
        //             sizeX = int.Parse(values[5]),
        //             sizeY = int.Parse(values[6]),
        //             constructionTime = (int)float.Parse(values[7]),
        //             baseCostGold = int.Parse(values[8]),
        //             baseCostWood = int.Parse(values[9]),
        //             baseCostStone = int.Parse(values[10]),
        //             baseProductionGold = int.Parse(values[11]),
        //             iconPath = values[12]
        //         };
        //     }
        //     catch (Exception e)
        //     {
        //         Debug.LogError($"건물 데이터 파싱 실패: {csvLine} - {e.Message}");
        //         return null;
        //     }
        // }
        
        private ItemData ParseItemData(string csvLine)
        {
            try
            {
                string[] values = SplitCSVLine(csvLine);
                if (values.Length < 10) return null;
                
                return new ItemData
                {
                    id = values[0],
                    nameKey = values[1],
                    descriptionKey = values[2],
                    itemType = (ItemType)Enum.Parse(typeof(ItemType), values[3]),
                    rarity = (Rarity)Enum.Parse(typeof(Rarity), values[4]),
                    maxStack = int.Parse(values[5]),
                    sellPrice = int.Parse(values[6]),
                    buyPrice = int.Parse(values[7]),
                    effectValues = ParseFloatList(values[8]).ToArray(),
                    iconPath = values[9]
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"아이템 데이터 파싱 실패: {csvLine} - {e.Message}");
                return null;
            }
        }
        
        private QuestData ParseQuestData(string csvLine)
        {
            try
            {
                string[] values = SplitCSVLine(csvLine);
                if (values.Length < 8) return null;
                
                return new QuestData
                {
                    questId = values[0],
                    questName = values[1],
                    description = values[2],
                    questType = (QuestType)Enum.Parse(typeof(QuestType), values[3]),
                    targetQuantity = int.Parse(values[4]),
                    goldReward = int.Parse(values[5]),
                    experienceReward = int.Parse(values[6]),
                    itemRewards = ParseIntList(values[7]).Select(i => i.ToString()).ToList()
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"퀘스트 데이터 파싱 실패: {csvLine} - {e.Message}");
                return null;
            }
        }
        
        private EnemyData ParseEnemyData(string csvLine)
        {
            try
            {
                string[] values = SplitCSVLine(csvLine);
                if (values.Length < 12) return null;
                
                var enemyData = new EnemyData
                {
                    enemyId = values[0],
                    nameKey = values[1],
                    level = int.Parse(values[2]),
                    hp = (int)float.Parse(values[3]),
                    attack = (int)float.Parse(values[4]),
                    defense = (int)float.Parse(values[5]),
                    speed = (int)float.Parse(values[6]),
                    skillIds = ParseIntList(values[7]),
                    expReward = int.Parse(values[8]),
                    goldReward = int.Parse(values[9]),
                    spritePath = values[11]
                };
                enemyData.dropItems.AddRange(ParseIntList(values[10]));
                return enemyData;
            }
            catch (Exception e)
            {
                Debug.LogError($"적 데이터 파싱 실패: {csvLine} - {e.Message}");
                return null;
            }
        }
        
        private FormationData ParseFormationData(string csvLine)
        {
            try
            {
                string[] values = SplitCSVLine(csvLine);
                if (values.Length < 5) return null;
                
                var formationData = new FormationData
                {
                    formationId = values[0],
                    nameKey = values[1],
                    formationType = (FormationType)Enum.Parse(typeof(FormationType), values[2]),
                };
                formationData.positions.AddRange(ParseVector2IntList(values[3]).Select(v => new FormationPosition { localPosition = new Vector2(v.x, v.y) }));
                formationData.bonusEffects.AddRange(ParseFloatList(values[4]));
                return formationData;
            }
            catch (Exception e)
            {
                Debug.LogError($"편성 데이터 파싱 실패: {csvLine} - {e.Message}");
                return null;
            }
        }
        
        // LocalizationData 타입이 제거되어 주석 처리
        // private LocalizationData ParseLocalizationData(string csvLine)
        // {
        //     try
        //     {
        //         string[] values = SplitCSVLine(csvLine);
        //         if (values.Length < 2) return null;
        //         
        //         return new LocalizationData
        //         {
        //             key = values[0],
        //             value = values[1].Replace("\\n", "\n") // 개행 문자 처리
        //         };
        //     }
        //     catch (Exception e)
        //     {
        //         Debug.LogError($"다국어 데이터 파싱 실패: {csvLine} - {e.Message}");
        //         return null;
        //     }
        // }
        
        // 유틸리티 메서드들
        private string[] SplitCSVLine(string line)
        {
            // CSV 파싱 (콤마로 구분, 따옴표 내부의 콤마는 무시)
            List<string> result = new List<string>();
            bool inQuotes = false;
            StringBuilder currentField = new StringBuilder();
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            
            result.Add(currentField.ToString().Trim());
            return result.ToArray();
        }
        
        private List<int> ParseIntList(string value)
        {
            if (string.IsNullOrEmpty(value)) return new List<int>();
            
            return value.Split(';')
                       .Where(s => !string.IsNullOrWhiteSpace(s))
                       .Select(s => int.Parse(s.Trim()))
                       .ToList();
        }
        
        private List<float> ParseFloatList(string value)
        {
            if (string.IsNullOrEmpty(value)) return new List<float>();
            
            return value.Split(';')
                       .Where(s => !string.IsNullOrWhiteSpace(s))
                       .Select(s => float.Parse(s.Trim()))
                       .ToList();
        }
        
        private List<Vector2Int> ParseVector2IntList(string value)
        {
            if (string.IsNullOrEmpty(value)) return new List<Vector2Int>();
            
            return value.Split(';')
                       .Where(s => !string.IsNullOrWhiteSpace(s))
                       .Select(s => {
                           var coords = s.Split(',');
                           return new Vector2Int(int.Parse(coords[0]), int.Parse(coords[1]));
                       })
                       .ToList();
        }
        
        private string GetLanguageCode(SystemLanguage language)
        {
            return language switch
            {
                SystemLanguage.Korean => "KR",
                SystemLanguage.English => "EN",
                SystemLanguage.Japanese => "JP",
                SystemLanguage.Chinese => "CN",
                _ => "KR"
            };
        }
        
        /// <summary>
        /// 언어 변경
        /// </summary>
        public void ChangeLanguage(SystemLanguage newLanguage)
        {
            if (currentLanguage == newLanguage) return;
            
            currentLanguage = newLanguage;
            LoadLocalizationData();
            OnLanguageChanged?.Invoke(currentLanguage);
        }
        
        /// <summary>
        /// 다국어 텍스트 가져오기
        /// </summary>
        public string GetLocalizedText(string key)
        {
            // LocalizationData 타입이 제거되어 주석 처리
            // if (localizationDict.ContainsKey(key))
            // {
            //     return localizationDict[key].value ?? key;
            // }
            return key; // 항상 키를 반환
        }
        
        // 데이터 접근 메서드들
        public CharacterData GetCharacterData(string id) => characterDataDict.ContainsKey(id) ? characterDataDict[id] : null;
        public SkillData GetSkillData(string id) => skillDataDict.ContainsKey(id) ? skillDataDict[id] : null;
        // public BuildingDataCSV GetBuildingData(string id) => buildingDataDict.ContainsKey(id) ? buildingDataDict[id] : null; // Commented out
        public ItemData GetItemData(string id) => itemDataDict.ContainsKey(id) ? itemDataDict[id] : null;
        public QuestData GetQuestData(string id) => questDataDict.ContainsKey(id) ? questDataDict[id] : null;
        public EnemyData GetEnemyData(string id) => enemyDataDict.ContainsKey(id) ? enemyDataDict[id] : null;
        public FormationData GetFormationData(string id) => formationDataDict.ContainsKey(id) ? formationDataDict[id] : null;
        
        public List<CharacterData> GetAllCharacterData() => characterDataDict.Values.ToList();
        public List<SkillData> GetAllSkillData() => skillDataDict.Values.ToList();
        // public List<BuildingDataCSV> GetAllBuildingData() => buildingDataDict.Values.ToList(); // Commented out
        public List<ItemData> GetAllItemData() => itemDataDict.Values.ToList();
        public List<QuestData> GetAllQuestData() => questDataDict.Values.ToList();
        public List<EnemyData> GetAllEnemyData() => enemyDataDict.Values.ToList();
        public List<FormationData> GetAllFormationData() => formationDataDict.Values.ToList();
        
        // 필터링 메서드들
        public List<CharacterData> GetCharactersByJob(GuildMaster.Battle.JobClass jobClass)
        {
            return characterDataDict.Values.Where(c => c.jobClass == jobClass).ToList();
        }
        
        public List<CharacterData> GetCharactersByRarity(GuildMaster.Data.CharacterRarity rarity)
        {
            return characterDataDict.Values.Where(c => (CharacterRarity)c.rarity == rarity).ToList();
        }
        
        public List<SkillData> GetSkillsByJob(GuildMaster.Battle.JobClass jobClass)
        {
            return skillDataDict.Values.Where(s => s.requiredJobClass == jobClass).ToList();
        }
        
        // public List<BuildingDataCSV> GetBuildingsByType(BuildingType buildingType)
        // {
        //     return buildingDataDict.Values.Where(b => b.buildingType == buildingType).ToList();
        // }
        
        public List<ItemData> GetItemsByType(ItemType itemType)
        {
            return itemDataDict.Values.Where(i => i.itemType == itemType).ToList();
        }
    }
    
    // CSVDataManager 전용 데이터 클래스들은 GuildBattleCore.cs에 통합됨
} 