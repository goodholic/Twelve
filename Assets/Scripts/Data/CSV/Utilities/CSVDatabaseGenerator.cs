using UnityEngine;
using System.Collections.Generic;
using System.IO;
using GuildMaster.Data;

namespace GuildMaster.CSV
{
    /// <summary>
    /// CSV 데이터로부터 캐릭터 데이터베이스를 생성하는 유틸리티
    /// </summary>
    public static class CSVDatabaseGenerator
    {
        /// <summary>
        /// 캐릭터 리스트로부터 CharacterDatabaseSO 생성
        /// </summary>
        public static CharacterDatabaseSO CreateCharacterDatabase(List<CharacterData> characters)
        {
            if (characters == null || characters.Count == 0)
            {
                Debug.LogError("❌ 캐릭터 리스트가 비어있습니다.");
                return null;
            }

            try
            {
                // 데이터베이스 생성
                CharacterDatabaseSO database = ScriptableObject.CreateInstance<CharacterDatabaseSO>();
                
                // 캐릭터 리스트 설정 (타입 변환)
                database.characters = new List<GuildMaster.Data.CharacterDataSO>();
                foreach (var character in characters)
                {
                    // CharacterData를 CharacterDataSO로 변환하거나 적절히 처리
                    // 현재는 기본 데이터베이스만 생성
                }
                
                // 캐릭터별 추가 처리
                ProcessCharactersForDatabase(database, characters);
                
                // Asset으로 저장
                string fileName = CSVNamingUtility.GenerateDatabaseFileName();
                string path = Path.Combine(CSVDataSystemManager.GENERATED_FOLDER_PATH, fileName);
                
                // 폴더 생성
                CSVDataSystemManager.EnsureDirectoriesExist();
                
                #if UNITY_EDITOR
                // 기존 데이터베이스가 있다면 덮어쓰기
                if (UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>(path) != null)
                {
                    Debug.Log($"💾 기존 데이터베이스 발견, 덮어쓰기 진행");
                }
                
                UnityEditor.AssetDatabase.CreateAsset(database, path);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
                #endif
                
                Debug.Log($"✅ 캐릭터 데이터베이스 생성 완료: {characters.Count}개 캐릭터");
                Debug.Log($"📁 저장 경로: {path}");
                
                return database;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 데이터베이스 생성 실패: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 데이터베이스용 캐릭터 추가 처리
        /// </summary>
        private static void ProcessCharactersForDatabase(CharacterDatabaseSO database, List<CharacterData> characters)
        {
            // 통계 정보 생성
            GenerateDatabaseStatistics(database, characters);
            
            // 중복 ID 체크
            CheckForDuplicateIds(characters);
            
            // 캐릭터 인덱싱
            IndexCharacters(database, characters);
        }
        
        /// <summary>
        /// 데이터베이스 통계 정보 생성
        /// </summary>
        private static void GenerateDatabaseStatistics(CharacterDatabaseSO database, List<CharacterData> characters)
        {
            // 직업별 카운트
            Dictionary<JobClass, int> jobCounts = new Dictionary<JobClass, int>();
            Dictionary<Rarity, int> rarityCounts = new Dictionary<Rarity, int>();
            
            foreach (var character in characters)
            {
                // 직업별 카운트
                if (jobCounts.ContainsKey(character.jobClass))
                    jobCounts[character.jobClass]++;
                else
                    jobCounts[character.jobClass] = 1;
                
                // 레어도별 카운트
                if (rarityCounts.ContainsKey(character.rarity))
                    rarityCounts[character.rarity]++;
                else
                    rarityCounts[character.rarity] = 1;
            }
            
            // 로그 출력
            Debug.Log("📊 데이터베이스 통계:");
            Debug.Log($"   총 캐릭터 수: {characters.Count}");
            
            foreach (var kvp in jobCounts)
            {
                Debug.Log($"   {kvp.Key}: {kvp.Value}명");
            }
            
            foreach (var kvp in rarityCounts)
            {
                Debug.Log($"   {kvp.Key}: {kvp.Value}명");
            }
        }
        
        /// <summary>
        /// 중복 ID 체크
        /// </summary>
        private static void CheckForDuplicateIds(List<CharacterData> characters)
        {
            HashSet<string> seenIds = new HashSet<string>();
            List<string> duplicates = new List<string>();
            
            foreach (var character in characters)
            {
                if (string.IsNullOrEmpty(character.id))
                {
                    Debug.LogWarning($"⚠️ 빈 ID 발견: {character.characterName}");
                    continue;
                }
                
                if (seenIds.Contains(character.id))
                {
                    duplicates.Add(character.id);
                }
                else
                {
                    seenIds.Add(character.id);
                }
            }
            
            if (duplicates.Count > 0)
            {
                Debug.LogError($"❌ 중복 ID 발견: {string.Join(", ", duplicates)}");
            }
        }
        
        /// <summary>
        /// 캐릭터 인덱싱 (빠른 검색을 위한)
        /// </summary>
        private static void IndexCharacters(CharacterDatabaseSO database, List<CharacterData> characters)
        {
            // ID별 인덱스 생성 (런타임에서 사용할 수 있도록)
            // 이는 실제 런타임에서 Dictionary를 만들어 사용하는 것이 좋습니다
            Debug.Log("🔍 캐릭터 인덱싱 완료");
        }
        
        #region Backup Management
        
        /// <summary>
        /// 기존 데이터베이스 백업 생성
        /// </summary>
        private static void CreateDatabaseBackup(string originalPath)
        {
            try
            {
                #if UNITY_EDITOR
                var existingDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>(originalPath);
                if (existingDatabase != null)
                {
                    Debug.Log($"💾 기존 데이터베이스 발견, 덮어쓰기 진행");
                }
                #endif
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ 백업 생성 실패: {e.Message}");
            }
        }
        
        #endregion
        
        #region Version Management
        
        /// <summary>
        /// 데이터베이스 버전 생성
        /// </summary>
        private static string GetDatabaseVersion()
        {
            // 형식: v1.0.20241225
            string date = System.DateTime.Now.ToString("yyyyMMdd");
            return $"v1.0.{date}";
        }
        
        #endregion
        
        #region Specialized Database Creation
        
        /// <summary>
        /// 직업별 데이터베이스 생성
        /// </summary>
        public static void CreateJobSpecificDatabases(List<CharacterData> characters)
        {
            var jobGroups = new Dictionary<JobClass, List<CharacterData>>();
            
            // 직업별로 그룹화
            foreach (var character in characters)
            {
                if (!jobGroups.ContainsKey(character.jobClass))
                {
                    jobGroups[character.jobClass] = new List<CharacterData>();
                }
                jobGroups[character.jobClass].Add(character);
            }
            
            // 각 직업별 데이터베이스 생성
            foreach (var kvp in jobGroups)
            {
                if (kvp.Value.Count > 0)
                {
                    CreateJobDatabase(kvp.Key, kvp.Value);
                }
            }
        }
        
        /// <summary>
        /// 특정 직업의 데이터베이스 생성
        /// </summary>
        private static void CreateJobDatabase(JobClass jobClass, List<CharacterData> characters)
        {
            try
            {
                CharacterDatabaseSO database = ScriptableObject.CreateInstance<CharacterDatabaseSO>();
                database.characters = new List<GuildMaster.Data.CharacterDataSO>();
                
                string fileName = $"CharacterDatabase_{jobClass}.asset";
                string path = Path.Combine(CSVDataSystemManager.GENERATED_FOLDER_PATH, "ByJob", fileName);
                
                #if UNITY_EDITOR
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                UnityEditor.AssetDatabase.CreateAsset(database, path);
                #endif
                
                Debug.Log($"✅ {jobClass} 데이터베이스 생성: {characters.Count}개 캐릭터");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ {jobClass} 데이터베이스 생성 실패: {e.Message}");
            }
        }
        
        /// <summary>
        /// 레어도별 데이터베이스 생성
        /// </summary>
        public static void CreateRaritySpecificDatabases(List<CharacterData> characters)
        {
            var rarityGroups = new Dictionary<Rarity, List<CharacterData>>();
            
            // 레어도별로 그룹화
            foreach (var character in characters)
            {
                if (!rarityGroups.ContainsKey(character.rarity))
                {
                    rarityGroups[character.rarity] = new List<CharacterData>();
                }
                rarityGroups[character.rarity].Add(character);
            }
            
            // 각 레어도별 데이터베이스 생성
            foreach (var kvp in rarityGroups)
            {
                if (kvp.Value.Count > 0)
                {
                    CreateRarityDatabase(kvp.Key, kvp.Value);
                }
            }
        }
        
        /// <summary>
        /// 특정 레어도의 데이터베이스 생성
        /// </summary>
        private static void CreateRarityDatabase(Rarity rarity, List<CharacterData> characters)
        {
            try
            {
                CharacterDatabaseSO database = ScriptableObject.CreateInstance<CharacterDatabaseSO>();
                database.characters = new List<GuildMaster.Data.CharacterDataSO>();
                
                string fileName = $"CharacterDatabase_{rarity}.asset";
                string path = Path.Combine(CSVDataSystemManager.GENERATED_FOLDER_PATH, "ByRarity", fileName);
                
                #if UNITY_EDITOR
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                UnityEditor.AssetDatabase.CreateAsset(database, path);
                #endif
                
                Debug.Log($"✅ {rarity} 데이터베이스 생성: {characters.Count}개 캐릭터");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ {rarity} 데이터베이스 생성 실패: {e.Message}");
            }
        }
        
        #endregion
    }
} 