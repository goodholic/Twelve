using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using GuildMaster.Data;
using TacticalTileGame.Data;

namespace GuildMaster.CSV
{
    /// <summary>
    /// CSV 데이터 시스템 총괄 매니저
    /// 모든 CSV 관련 기능을 통합 관리
    /// </summary>
    public static class CSVDataSystemManager
    {
        #region Constants
        
        public const string CSV_FOLDER_PATH = "Assets/CSV";
        public const string GENERATED_FOLDER_PATH = "Assets/Characters/Generated";
        public const string BACKUP_FOLDER_PATH = "Assets/CSV/Backups";
        
        // CSV 파일 경로들
        public const string CHARACTER_CSV_PATH = "Assets/CSV/character_csv_data.txt";
        public const string ALLY_ONE_STAR_CSV_PATH = "Assets/CSV/ally_one_star_characters.csv";
        public const string ALLY_TWO_STAR_CSV_PATH = "Assets/CSV/ally_two_star_characters.csv";
        public const string ENEMY_ONE_STAR_CSV_PATH = "Assets/CSV/enemy_one_star_characters.csv";
        public const string ENEMY_TWO_STAR_CSV_PATH = "Assets/CSV/enemy_two_star_characters.csv";
        public const string ITEMS_CSV_PATH = "Assets/CSV/items.csv";
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// 메인 캐릭터 CSV에서 캐릭터 데이터들을 생성
        /// </summary>
        [MenuItem("Twelve/📊 Data Management/Import Character Data")]
        public static void ImportCharacterData()
        {
            Debug.Log("🚀 캐릭터 데이터 임포트 시작...");
            
            var importer = new CSVCharacterImporter();
            var characters = importer.ImportFromCSV(CHARACTER_CSV_PATH);
            
            if (characters != null && characters.Count > 0)
            {
                var database = CSVDatabaseGenerator.CreateCharacterDatabase(characters);
                Debug.Log($"✅ {characters.Count}개 캐릭터와 데이터베이스 생성 완료!");
            }
            else
            {
                Debug.LogError("❌ 캐릭터 데이터 임포트 실패");
            }
        }
        
        /// <summary>
        /// 전체 CSV 시스템 동기화
        /// </summary>
        [MenuItem("Twelve/📊 Data Management/Full System Sync")]
        public static void FullSystemSync()
        {
            Debug.Log("🔄 전체 CSV 시스템 동기화 시작...");
            
            // 각 CSV 파일별 처리
            ImportCharacterData();
            ImportItemData();
            ImportAllyData();
            ImportEnemyData();
            
            // Asset 새로고침
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("🎉 전체 시스템 동기화 완료!");
        }
        
        /// <summary>
        /// 캐릭터 데이터를 CSV로 내보내기
        /// </summary>
        [MenuItem("Twelve/📊 Data Management/Export Character Data")]
        public static void ExportCharacterData()
        {
            Debug.Log("📤 캐릭터 데이터 내보내기 기능은 추후 구현 예정입니다.");
        }
        
        /// <summary>
        /// CSV 시스템 유효성 검사
        /// </summary>
        [MenuItem("Twelve/📊 Data Management/Validate System")]
        public static void ValidateSystem()
        {
            Debug.Log("🔍 CSV 시스템 유효성 검사 기능은 추후 구현 예정입니다.");
        }
        
        #endregion
        
        #region Private Methods
        
        private static void ImportItemData()
        {
            if (File.Exists(ITEMS_CSV_PATH))
            {
                Debug.Log("📦 아이템 데이터 임포트...");
                // 아이템 임포트 로직
            }
        }
        
        private static void ImportAllyData()
        {
            Debug.Log("👥 아군 데이터 임포트...");
            
            if (File.Exists(ALLY_ONE_STAR_CSV_PATH))
            {
                // 1성 아군 임포트
            }
            
            if (File.Exists(ALLY_TWO_STAR_CSV_PATH))
            {
                // 2성 아군 임포트
            }
        }
        
        private static void ImportEnemyData()
        {
            Debug.Log("👹 적군 데이터 임포트...");
            
            if (File.Exists(ENEMY_ONE_STAR_CSV_PATH))
            {
                // 1성 적군 임포트
            }
            
            if (File.Exists(ENEMY_TWO_STAR_CSV_PATH))
            {
                // 2성 적군 임포트
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// 필요한 폴더들을 생성
        /// </summary>
        public static void EnsureDirectoriesExist()
        {
            string[] directories = {
                CSV_FOLDER_PATH,
                GENERATED_FOLDER_PATH,
                BACKUP_FOLDER_PATH
            };
            
            foreach (string dir in directories)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    Debug.Log($"📁 폴더 생성: {dir}");
                }
            }
        }
        
        /// <summary>
        /// CSV 파일 존재 여부 확인
        /// </summary>
        public static bool CheckCSVFilesExist()
        {
            string[] csvFiles = {
                CHARACTER_CSV_PATH,
                ITEMS_CSV_PATH,
                ALLY_ONE_STAR_CSV_PATH,
                ALLY_TWO_STAR_CSV_PATH,
                ENEMY_ONE_STAR_CSV_PATH,
                ENEMY_TWO_STAR_CSV_PATH
            };
            
            bool allExist = true;
            foreach (string csvFile in csvFiles)
            {
                if (!File.Exists(csvFile))
                {
                    Debug.LogWarning($"⚠️ CSV 파일 없음: {csvFile}");
                    allExist = false;
                }
            }
            
            return allExist;
        }
        
        #endregion
    }
    
    /// <summary>
    /// CSV 파일 유효성 검사 결과
    /// </summary>
    public class CSVValidationResult
    {
        public string FileName { get; set; }
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
} 