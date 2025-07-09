using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace TileConquest.Data
{
    /// <summary>
    /// 타일 정복 게임의 모든 데이터를 관리하는 중앙 데이터베이스
    /// </summary>
    [CreateAssetMenu(fileName = "TileConquestDatabase", menuName = "TileConquest/Game Database", order = 0)]
    public class TileConquestDatabaseSO : ScriptableObject
    {
        [Header("캐릭터 데이터")]
        [SerializeField] private List<CharacterTileDataSO> allCharacters = new List<CharacterTileDataSO>();
        
        [Header("공격 범위 데이터")]
        [SerializeField] private List<AttackRangeSO> attackRanges = new List<AttackRangeSO>();
        
        [Header("게임 설정")]
        [SerializeField] private TileBattleConfigSO defaultBattleConfig;
        [SerializeField] private List<TileBattleConfigSO> battleConfigs = new List<TileBattleConfigSO>();
        
        [Header("AI 행동 패턴")]
        [SerializeField] private List<AIBehaviorSO> aiBehaviors = new List<AIBehaviorSO>();
        
        [Header("스토리/대화 데이터")]
        [SerializeField] private List<StoryDialogueDataSO> storyData = new List<StoryDialogueDataSO>();
        
        [Header("데이터베이스 설정")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool debugMode = false;
        
        // 룩업 딕셔너리
        private Dictionary<string, CharacterTileDataSO> characterLookup;
        private Dictionary<string, AttackRangeSO> rangeLookup;
        private Dictionary<string, TileBattleConfigSO> configLookup;
        private Dictionary<AIDifficulty, AIBehaviorSO> aiLookup;
        private Dictionary<string, StoryDialogueDataSO> storyLookup;
        
        private void OnEnable()
        {
            if (autoInitialize)
            {
                Initialize();
            }
        }
        
        /// <summary>
        /// 데이터베이스 초기화
        /// </summary>
        public void Initialize()
        {
            // 캐릭터 룩업 초기화
            characterLookup = new Dictionary<string, CharacterTileDataSO>();
            foreach (var character in allCharacters)
            {
                if (character != null && character.baseCharacterData != null)
                {
                    characterLookup[character.baseCharacterData.id] = character;
                }
            }
            
            // 공격 범위 룩업 초기화
            rangeLookup = new Dictionary<string, AttackRangeSO>();
            foreach (var range in attackRanges)
            {
                if (range != null)
                {
                    rangeLookup[range.rangeId] = range;
                }
            }
            
            // 전투 설정 룩업 초기화
            configLookup = new Dictionary<string, TileBattleConfigSO>();
            foreach (var config in battleConfigs)
            {
                if (config != null)
                {
                    configLookup[config.name] = config;
                }
            }
            
            // AI 룩업 초기화
            aiLookup = new Dictionary<AIDifficulty, AIBehaviorSO>();
            foreach (var ai in aiBehaviors)
            {
                if (ai != null)
                {
                    aiLookup[ai.difficulty] = ai;
                }
            }
            
            // 스토리 룩업 초기화
            storyLookup = new Dictionary<string, StoryDialogueDataSO>();
            foreach (var story in storyData)
            {
                if (story != null)
                {
                    storyLookup[story.storyId] = story;
                }
            }
            
            if (debugMode)
            {
                Debug.Log($"TileConquestDatabase 초기화 완료:");
                Debug.Log($"- 캐릭터: {characterLookup.Count}개");
                Debug.Log($"- 공격 범위: {rangeLookup.Count}개");
                Debug.Log($"- 전투 설정: {configLookup.Count}개");
                Debug.Log($"- AI 패턴: {aiLookup.Count}개");
                Debug.Log($"- 스토리: {storyLookup.Count}개");
            }
        }
        
        #region 캐릭터 관련 메서드
        
        /// <summary>
        /// ID로 캐릭터 가져오기
        /// </summary>
        public CharacterTileDataSO GetCharacter(string characterId)
        {
            if (characterLookup == null) Initialize();
            return characterLookup.TryGetValue(characterId, out var character) ? character : null;
        }
        
        /// <summary>
        /// 직업별 캐릭터 목록 가져오기
        /// </summary>
        public List<CharacterTileDataSO> GetCharactersByJob(JobClass jobClass)
        {
            return allCharacters.Where(c => c.baseCharacterData?.jobClass == jobClass).ToList();
        }
        
        /// <summary>
        /// 레어도별 캐릭터 목록 가져오기
        /// </summary>
        public List<CharacterTileDataSO> GetCharactersByRarity(GuildMaster.Data.Rarity rarity)
        {
            return allCharacters.Where(c => c.baseCharacterData?.rarity == rarity).ToList();
        }
        
        /// <summary>
        /// 랜덤 캐릭터 10개 선택
        /// </summary>
        public List<CharacterTileDataSO> GetRandomCharacters(int count = 10)
        {
            return allCharacters.OrderBy(x => Random.value).Take(count).ToList();
        }
        
        #endregion
        
        #region 공격 범위 관련 메서드
        
        /// <summary>
        /// ID로 공격 범위 가져오기
        /// </summary>
        public AttackRangeSO GetAttackRange(string rangeId)
        {
            if (rangeLookup == null) Initialize();
            return rangeLookup.TryGetValue(rangeId, out var range) ? range : null;
        }
        
        /// <summary>
        /// 타입별 공격 범위 목록 가져오기
        /// </summary>
        public List<AttackRangeSO> GetAttackRangesByType(RangeType type)
        {
            return attackRanges.Where(r => r.rangeType == type).ToList();
        }
        
        #endregion
        
        #region 게임 설정 관련 메서드
        
        /// <summary>
        /// 기본 전투 설정 가져오기
        /// </summary>
        public TileBattleConfigSO GetDefaultBattleConfig()
        {
            return defaultBattleConfig;
        }
        
        /// <summary>
        /// 이름으로 전투 설정 가져오기
        /// </summary>
        public TileBattleConfigSO GetBattleConfig(string configName)
        {
            if (configLookup == null) Initialize();
            return configLookup.TryGetValue(configName, out var config) ? config : defaultBattleConfig;
        }
        
        #endregion
        
        #region AI 관련 메서드
        
        /// <summary>
        /// 난이도별 AI 행동 패턴 가져오기
        /// </summary>
        public AIBehaviorSO GetAIBehavior(AIDifficulty difficulty)
        {
            if (aiLookup == null) Initialize();
            return aiLookup.TryGetValue(difficulty, out var ai) ? ai : aiBehaviors.FirstOrDefault();
        }
        
        /// <summary>
        /// AI가 선택할 캐릭터 목록 생성
        /// </summary>
        public List<CharacterTileDataSO> GetAICharacterSelection(AIDifficulty difficulty, int count = 10)
        {
            var aiBehavior = GetAIBehavior(difficulty);
            if (aiBehavior == null) return GetRandomCharacters(count);
            
            return aiBehavior.SelectCharacters(allCharacters, count);
        }
        
        #endregion
        
        #region 스토리 관련 메서드
        
        /// <summary>
        /// ID로 스토리 데이터 가져오기
        /// </summary>
        public StoryDialogueDataSO GetStoryData(string storyId)
        {
            if (storyLookup == null) Initialize();
            return storyLookup.TryGetValue(storyId, out var story) ? story : null;
        }
        
        /// <summary>
        /// 타입별 스토리 목록 가져오기
        /// </summary>
        public List<StoryDialogueDataSO> GetStoriesByType(StoryType type)
        {
            return storyData.Where(s => s.storyType == type).ToList();
        }
        
        #endregion
        
        #region 유틸리티 메서드
        
        /// <summary>
        /// CSV에서 데이터 로드 (에디터 전용)
        /// </summary>
        [ContextMenu("Load Data from CSV")]
        public void LoadFromCSV()
        {
#if UNITY_EDITOR
            Debug.Log("CSV 데이터 로드 시작...");
            // CSV 로드 로직 구현
            UnityEditor.AssetDatabase.Refresh();
            Initialize();
#endif
        }
        
        /// <summary>
        /// 데이터 검증
        /// </summary>
        [ContextMenu("Validate Database")]
        public void ValidateDatabase()
        {
            int errors = 0;
            
            // 캐릭터 데이터 검증
            foreach (var character in allCharacters)
            {
                if (character == null)
                {
                    Debug.LogError("Null character in database!");
                    errors++;
                    continue;
                }
                
                if (character.baseCharacterData == null)
                {
                    Debug.LogError($"Character {character.name} has no base data!");
                    errors++;
                }
                
                if (character.attackRange == null)
                {
                    Debug.LogWarning($"Character {character.name} has no attack range!");
                }
            }
            
            // AI 데이터 검증
            foreach (var ai in aiBehaviors)
            {
                if (ai == null)
                {
                    Debug.LogError("Null AI behavior in database!");
                    errors++;
                }
            }
            
            if (errors == 0)
            {
                Debug.Log("Database validation complete. No errors found!");
            }
            else
            {
                Debug.LogError($"Database validation complete. Found {errors} errors.");
            }
        }
        
        /// <summary>
        /// 데이터베이스 통계
        /// </summary>
        [ContextMenu("Show Database Statistics")]
        public void ShowStatistics()
        {
            Debug.Log("=== TileConquest Database Statistics ===");
            Debug.Log($"Total Characters: {allCharacters.Count}");
            
            // 직업별 통계
            var jobStats = allCharacters
                .Where(c => c.baseCharacterData != null)
                .GroupBy(c => c.baseCharacterData.jobClass)
                .Select(g => new { Job = g.Key, Count = g.Count() });
                
            foreach (var stat in jobStats)
            {
                Debug.Log($"  {stat.Job}: {stat.Count}");
            }
            
            Debug.Log($"Total Attack Ranges: {attackRanges.Count}");
            Debug.Log($"Total Battle Configs: {battleConfigs.Count}");
            Debug.Log($"Total AI Behaviors: {aiBehaviors.Count}");
            Debug.Log($"Total Stories: {storyData.Count}");
            Debug.Log("===================================");
        }
        
        #endregion
    }
}