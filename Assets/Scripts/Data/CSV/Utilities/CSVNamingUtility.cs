using System.Text.RegularExpressions;
using GuildMaster.Data;

namespace GuildMaster.CSV
{
    /// <summary>
    /// CSV 시스템의 파일명 생성과 네이밍 규칙을 관리하는 유틸리티
    /// </summary>
    public static class CSVNamingUtility
    {
        #region Character File Naming
        
        /// <summary>
        /// 캐릭터 데이터 파일명 생성
        /// 형식: CHAR101_철혈로젤리아.asset
        /// </summary>
        public static string GenerateCharacterFileName(CharacterData character)
        {
            if (character == null) return "UnknownCharacter.asset";
            
            string sanitizedName = SanitizeFileName(character.characterName);
            string fileName = $"{character.id}_{sanitizedName}.asset";
            
            return fileName;
        }
        
        /// <summary>
        /// 캐릭터 프리팹 파일명 생성
        /// 형식: CHAR101_철혈로젤리아_Prefab.prefab
        /// </summary>
        public static string GenerateCharacterPrefabFileName(CharacterData character)
        {
            if (character == null) return "UnknownCharacter_Prefab.prefab";
            
            string sanitizedName = SanitizeFileName(character.characterName);
            string fileName = $"{character.id}_{sanitizedName}_Prefab.prefab";
            
            return fileName;
        }
        
        #endregion
        
        #region Database File Naming
        
        /// <summary>
        /// 캐릭터 데이터베이스 파일명 생성
        /// </summary>
        public static string GenerateDatabaseFileName()
        {
            return "CharacterDatabase.asset";
        }
        
        /// <summary>
        /// 백업 데이터베이스 파일명 생성
        /// 형식: CharacterDatabase_Backup_20241225_143022.asset
        /// </summary>
        public static string GenerateBackupDatabaseFileName()
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"CharacterDatabase_Backup_{timestamp}.asset";
        }
        
        #endregion
        
        #region CSV File Naming
        
        /// <summary>
        /// CSV 백업 파일명 생성
        /// 형식: character_csv_data_backup_20241225_143022.txt
        /// </summary>
        public static string GenerateCSVBackupFileName(string originalFileName)
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(originalFileName);
            string extension = System.IO.Path.GetExtension(originalFileName);
            
            return $"{nameWithoutExtension}_backup_{timestamp}{extension}";
        }
        
        /// <summary>
        /// 내보내기 CSV 파일명 생성
        /// 형식: character_export_20241225_143022.csv
        /// </summary>
        public static string GenerateExportCSVFileName(string baseName = "character")
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{baseName}_export_{timestamp}.csv";
        }
        
        #endregion
        
        #region Folder Naming
        
        /// <summary>
        /// 직업별 폴더명 생성
        /// </summary>
        public static string GenerateJobFolderName(JobClass jobClass)
        {
            return $"{jobClass}s"; // Warriors, Knights, Mages, etc.
        }
        
        /// <summary>
        /// 레어도별 폴더명 생성
        /// </summary>
        public static string GenerateRarityFolderName(Rarity rarity)
        {
            return rarity.ToString(); // Common, Rare, Epic, etc.
        }
        
        /// <summary>
        /// 계층적 폴더 구조 생성
        /// 형식: Generated/Epic/Warriors/
        /// </summary>
        public static string GenerateHierarchicalPath(CharacterData character)
        {
            if (character == null) return "Generated/Unknown/";
            
            string rarityFolder = GenerateRarityFolderName(character.rarity);
            string jobFolder = GenerateJobFolderName(character.jobClass);
            
            return $"Generated/{rarityFolder}/{jobFolder}/";
        }
        
        #endregion
        
        #region File Name Utilities
        
        /// <summary>
        /// 파일명에 사용할 수 없는 문자들을 제거하고 정리
        /// </summary>
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "Unknown";
            
            // 파일명에 사용할 수 없는 문자들 제거
            string invalidChars = @"[<>:""/\\|?*]";
            string sanitized = Regex.Replace(fileName, invalidChars, "");
            
            // 한국어 특수 문자 처리
            sanitized = sanitized.Replace("의", "");
            sanitized = sanitized.Replace("자", "");
            sanitized = sanitized.Replace(" ", "");
            sanitized = sanitized.Replace("\t", "");
            sanitized = sanitized.Replace("\n", "");
            sanitized = sanitized.Replace("\r", "");
            
            // 연속된 언더스코어 제거
            sanitized = Regex.Replace(sanitized, "_+", "_");
            
            // 앞뒤 언더스코어 제거
            sanitized = sanitized.Trim('_');
            
            // 빈 문자열이면 기본값 반환
            if (string.IsNullOrEmpty(sanitized))
                sanitized = "Unknown";
            
            // 길이 제한 (Windows 파일명 제한 고려)
            if (sanitized.Length > 50)
                sanitized = sanitized.Substring(0, 50);
            
            return sanitized;
        }
        
        /// <summary>
        /// 파일 확장자 확인 및 추가
        /// </summary>
        public static string EnsureFileExtension(string fileName, string extension)
        {
            if (string.IsNullOrEmpty(fileName)) return $"file{extension}";
            
            if (!extension.StartsWith("."))
                extension = "." + extension;
            
            if (!fileName.EndsWith(extension, System.StringComparison.OrdinalIgnoreCase))
                fileName += extension;
            
            return fileName;
        }
        
        /// <summary>
        /// 중복 파일명 처리 (파일이 이미 존재하는 경우)
        /// 형식: filename(1).asset, filename(2).asset
        /// </summary>
        public static string HandleDuplicateFileName(string fullPath)
        {
            if (!System.IO.File.Exists(fullPath)) return fullPath;
            
            string directory = System.IO.Path.GetDirectoryName(fullPath);
            string nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fullPath);
            string extension = System.IO.Path.GetExtension(fullPath);
            
            int counter = 1;
            string newPath;
            
            do
            {
                string newName = $"{nameWithoutExtension}({counter}){extension}";
                newPath = System.IO.Path.Combine(directory, newName);
                counter++;
            }
            while (System.IO.File.Exists(newPath));
            
            return newPath;
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// 파일명 유효성 검사
        /// </summary>
        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;
            
            // Windows 예약어 체크
            string[] reservedNames = {
                "CON", "PRN", "AUX", "NUL",
                "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
            };
            
            string nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName).ToUpper();
            
            foreach (string reserved in reservedNames)
            {
                if (nameWithoutExtension == reserved) return false;
            }
            
            // 금지 문자 체크
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (char c in fileName)
            {
                if (System.Array.IndexOf(invalidChars, c) >= 0) return false;
            }
            
            // 길이 체크 (Windows 경로 길이 제한)
            if (fileName.Length > 255) return false;
            
            return true;
        }
        
        #endregion
    }
} 