using UnityEngine;
using System.Collections.Generic;
using System.IO;
using GuildMaster.Data;
using TacticalTileGame.Data;

namespace GuildMaster.CSV
{
    /// <summary>
    /// CSV 캐릭터 데이터 임포터
    /// character_csv_data.txt 파일로부터 CharacterData들을 생성
    /// </summary>
    public class CSVCharacterImporter
    {
        public List<CharacterData> ImportFromCSV(string csvPath)
        {
            List<CharacterData> characters = new List<CharacterData>();
            
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"CSV 파일을 찾을 수 없습니다: {csvPath}");
                return characters;
            }
            
            try
            {
                string[] lines = File.ReadAllLines(csvPath);
                if (lines.Length < 2)
                {
                    Debug.LogError("CSV 파일에 데이터가 없습니다.");
                    return characters;
                }
                
                // 헤더 파싱
                string[] headers = ParseCSVLine(lines[0]);
                
                // 각 라인 처리
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = ParseCSVLine(lines[i]);
                    if (values.Length != headers.Length)
                    {
                        Debug.LogWarning($"라인 {i + 1}: 헤더와 값의 개수가 맞지 않습니다.");
                        continue;
                    }
                    
                    CharacterData character = CreateCharacterFromCSVData(headers, values);
                    if (character != null)
                    {
                        characters.Add(character);
                        Debug.Log($"✅ 캐릭터 임포트: {character.characterName}");
                    }
                }
                
                Debug.Log($"📊 총 {characters.Count}개 캐릭터 임포트 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CSV 임포트 중 오류 발생: {e.Message}");
            }
            
            return characters;
        }
        
        private CharacterData CreateCharacterFromCSVData(string[] headers, string[] values)
        {
            try
            {
                CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
                
                // CSV 데이터를 딕셔너리로 변환
                Dictionary<string, string> data = new Dictionary<string, string>();
                for (int i = 0; i < headers.Length; i++)
                {
                    data[headers[i]] = values[i];
                }
                
                // 기본 정보 설정
                character.id = GetValueOrDefault(data, "id", "");
                character.characterName = GetValueOrDefault(data, "name", "Unknown");
                character.description = GetValueOrDefault(data, "description", "");
                
                // 직업과 레어도
                character.jobClass = ParseJobClass(GetValueOrDefault(data, "jobClass", "Warrior"));
                character.rarity = ParseRarity(GetValueOrDefault(data, "rarity", "Common"));
                character.tacticalRarity = ParseCharacterRarity(GetValueOrDefault(data, "rarity", "Common"));
                
                // 스탯 설정
                character.level = ParseInt(GetValueOrDefault(data, "level", "1"));
                character.baseHP = ParseInt(GetValueOrDefault(data, "baseHP", "100"));
                character.maxHP = character.baseHP;
                character.hp = character.baseHP;
                character.health = character.baseHP;
                
                character.baseMP = ParseInt(GetValueOrDefault(data, "baseMP", "50"));
                character.baseAttack = ParseInt(GetValueOrDefault(data, "baseAttack", "10"));
                character.attackPower = character.baseAttack;
                character.baseDefense = ParseInt(GetValueOrDefault(data, "baseDefense", "5"));
                character.baseMagicPower = ParseInt(GetValueOrDefault(data, "baseMagicPower", "0"));
                
                // 전투 스탯
                character.critRate = ParseFloat(GetValueOrDefault(data, "critRate", "10.0")) / 100f;
                character.baseCritRate = character.critRate;
                character.critDamage = ParseFloat(GetValueOrDefault(data, "critDamage", "150.0"));
                character.accuracy = ParseFloat(GetValueOrDefault(data, "accuracy", "95.0"));
                character.evasion = ParseFloat(GetValueOrDefault(data, "evasion", "5.0"));
                
                // 스킬 설정
                character.skillId = GetValueOrDefault(data, "skill1Id", "");
                
                // 공격 패턴과 애니메이션 설정
                CSVCharacterDataProcessor.SetupCharacterDefaults(character);
                
                // Asset으로 저장
                string fileName = CSVNamingUtility.GenerateCharacterFileName(character);
                string path = Path.Combine(CSVDataSystemManager.GENERATED_FOLDER_PATH, fileName);
                
                // 폴더 생성
                CSVDataSystemManager.EnsureDirectoriesExist();
                
                #if UNITY_EDITOR
                UnityEditor.AssetDatabase.CreateAsset(character, path);
                #endif
                
                return character;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"캐릭터 데이터 생성 실패: {e.Message}");
                return null;
            }
        }
        
        #region Parsing Utilities
        
        private string[] ParseCSVLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string currentField = "";
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.Trim());
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }
            
            result.Add(currentField.Trim());
            return result.ToArray();
        }
        
        private string GetValueOrDefault(Dictionary<string, string> data, string key, string defaultValue)
        {
            return data.ContainsKey(key) ? data[key] : defaultValue;
        }
        
        private int ParseInt(string value)
        {
            return int.TryParse(value, out int result) ? result : 0;
        }
        
        private float ParseFloat(string value)
        {
            return float.TryParse(value, out float result) ? result : 0f;
        }
        
        private JobClass ParseJobClass(string jobClass)
        {
            switch (jobClass)
            {
                case "Warrior": return JobClass.Warrior;
                case "Knight": return JobClass.Knight;
                case "Wizard": return JobClass.Mage;
                case "Priest": return JobClass.Priest;
                case "Rogue": return JobClass.Rogue;
                case "Sage": return JobClass.Sage;
                case "Archer": return JobClass.Archer;
                case "Gunner": return JobClass.Archer; // Gunner를 Archer로 매핑
                default: return JobClass.None;
            }
        }
        
        private Rarity ParseRarity(string rarity)
        {
            switch (rarity)
            {
                case "Common": return Rarity.Common;
                case "Uncommon": return Rarity.Uncommon;
                case "Rare": return Rarity.Rare;
                case "Epic": return Rarity.Epic;
                case "Legendary": return Rarity.Legendary;
                default: return Rarity.Common;
            }
        }
        
        private CharacterRarity ParseCharacterRarity(string rarity)
        {
            switch (rarity)
            {
                case "Common": return CharacterRarity.Common;
                case "Uncommon": return CharacterRarity.Uncommon;
                case "Rare": return CharacterRarity.Rare;
                case "Epic": return CharacterRarity.Epic;
                case "Legendary": return CharacterRarity.Legendary;
                default: return CharacterRarity.Common;
            }
        }
        
        #endregion
    }
} 