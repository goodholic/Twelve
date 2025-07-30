using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using GuildMaster.Data;
using TacticalTileGame.Data;

public class CSVCharacterGenerator
{
    // 기존 Data Management 카테고리와 중복되므로 제거하고 통합
    // 대신 기존 "Twelve/📊 Data Management/CSV 임포터"를 사용하세요
    /*
    [MenuItem("Tools/Generate Characters from CSV")]
    public static void GenerateCharactersFromCSV()
    {
        string csvPath = "Assets/CSV/character_csv_data.txt";
        
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }
    */
    
    // 대신 Data Management와 통합된 메서드 제공
    [MenuItem("Twelve/📊 Data Management/CSV에서 캐릭터 생성")]
    public static void GenerateCharactersFromCSVIntegrated()
    {
        string csvPath = "Assets/CSV/character_csv_data.txt";
        
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }
        
        // CSV 파일 읽기
        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length < 2)
        {
            Debug.LogError("CSV 파일에 데이터가 없습니다.");
            return;
        }
        
        // 헤더 파싱
        string[] headers = lines[0].Split(',');
        
        // 캐릭터 데이터들을 저장할 리스트
        List<CharacterData> characterList = new List<CharacterData>();
        
        // 각 라인을 파싱하여 CharacterData 생성
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            if (values.Length != headers.Length)
            {
                Debug.LogWarning($"라인 {i + 1}: 헤더와 값의 개수가 맞지 않습니다.");
                continue;
            }
            
            CharacterData characterData = CreateCharacterDataFromCSV(headers, values);
            if (characterData != null)
            {
                characterList.Add(characterData);
                Debug.Log($"✅ 캐릭터 생성 완료: {characterData.characterName}");
            }
        }
        
        // CharacterDatabaseSO 생성
        CreateCharacterDatabase(characterList);
        
        // Asset 데이터베이스 새로고침
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"🎉 총 {characterList.Count}개의 캐릭터와 데이터베이스가 생성되었습니다!");
    }
    
    private static CharacterData CreateCharacterDataFromCSV(string[] headers, string[] values)
    {
        try
        {
            // CharacterData ScriptableObject 생성
            CharacterData characterData = ScriptableObject.CreateInstance<CharacterData>();
            
            // CSV 데이터 파싱
            Dictionary<string, string> csvData = new Dictionary<string, string>();
            for (int i = 0; i < headers.Length; i++)
            {
                csvData[headers[i]] = values[i];
            }
            
            // 기본 정보 설정
            characterData.id = csvData["id"];
            characterData.characterName = csvData["name"];
            characterData.description = csvData["description"];
            
            // 직업 설정 (CSV의 jobClass를 JobClass enum으로 변환)
            characterData.jobClass = ParseJobClass(csvData["jobClass"]);
            
            // 레어도 설정
            characterData.rarity = ParseRarity(csvData["rarity"]);
            characterData.tacticalRarity = ParseCharacterRarity(csvData["rarity"]);
            
            // 스탯 설정
            characterData.level = int.Parse(csvData["level"]);
            characterData.baseHP = int.Parse(csvData["baseHP"]);
            characterData.maxHP = characterData.baseHP;
            characterData.hp = characterData.baseHP;
            characterData.health = characterData.baseHP;
            
            characterData.baseMP = int.Parse(csvData["baseMP"]);
            characterData.baseAttack = int.Parse(csvData["baseAttack"]);
            characterData.attackPower = characterData.baseAttack;
            characterData.baseDefense = int.Parse(csvData["baseDefense"]);
            characterData.baseMagicPower = int.Parse(csvData["baseMagicPower"]);
            
            // 전투 관련 스탯
            characterData.critRate = float.Parse(csvData["critRate"]) / 100f; // 퍼센트를 소수로 변환
            characterData.baseCritRate = characterData.critRate;
            characterData.critDamage = float.Parse(csvData["critDamage"]);
            characterData.accuracy = float.Parse(csvData["accuracy"]);
            characterData.evasion = float.Parse(csvData["evasion"]);
            
            // 스킬 설정
            characterData.skillId = csvData["skill1Id"];
            
            // 공격 패턴과 범위 타입 설정 (직업에 따라)
            SetAttackPatternByJob(characterData);
            
            // PNG 시퀀스 애니메이션 설정
            characterData.animationType = AnimationType.PNGSequence;
            characterData.pngSequenceScale = 1.0f;
            characterData.loopPNGSequences = true;
            
            // 기타 기본값
            characterData.star = GetStarByRarity(characterData.rarity);
            characterData.starLevel = characterData.star;
            characterData.initialStar = characterData.star;
            characterData.attackSpeed = 1.0f;
            characterData.attackRange = GetRangeByJob(characterData.jobClass);
            
            // Asset으로 저장
            string fileName = $"{characterData.id}_{characterData.characterName.Replace(" ", "")}.asset";
            string path = $"Assets/Characters/Generated/{fileName}";
            
            // 폴더가 없으면 생성
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            AssetDatabase.CreateAsset(characterData, path);
            
            return characterData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"캐릭터 데이터 생성 실패: {e.Message}");
            return null;
        }
    }
    
    private static JobClass ParseJobClass(string jobClassString)
    {
        switch (jobClassString)
        {
            case "Warrior": return JobClass.Warrior;
            case "Knight": return JobClass.Knight;
            case "Wizard": return JobClass.Mage; // CSV에서는 Wizard이지만 enum에서는 Mage
            case "Priest": return JobClass.Priest;
            case "Rogue": return JobClass.Rogue;
            case "Sage": return JobClass.Sage;
            case "Archer": return JobClass.Archer;
            case "Gunner": return JobClass.Gunner;
            default: return JobClass.None;
        }
    }
    
    private static Rarity ParseRarity(string rarityString)
    {
        switch (rarityString)
        {
            case "Common": return Rarity.Common;
            case "Uncommon": return Rarity.Uncommon;
            case "Rare": return Rarity.Rare;
            case "Epic": return Rarity.Epic;
            case "Legendary": return Rarity.Legendary;
            default: return Rarity.Common;
        }
    }
    
    private static CharacterRarity ParseCharacterRarity(string rarityString)
    {
        switch (rarityString)
        {
            case "Common": return CharacterRarity.Common;
            case "Uncommon": return CharacterRarity.Uncommon;
            case "Rare": return CharacterRarity.Rare;
            case "Epic": return CharacterRarity.Epic;
            case "Legendary": return CharacterRarity.Legendary;
            default: return CharacterRarity.Common;
        }
    }
    
    private static void SetAttackPatternByJob(CharacterData characterData)
    {
        // 직업에 따른 공격 패턴과 범위 타입 설정
        switch (characterData.jobClass)
        {
            case JobClass.Warrior:
                characterData.attackPattern = AttackPattern.Cross; // 십자 공격
                characterData.rangeType = RangeType.Melee;
                break;
                
            case JobClass.Knight:
                characterData.attackPattern = AttackPattern.Cross; // 방어형 십자 공격
                characterData.rangeType = RangeType.Melee;
                break;
                
            case JobClass.Mage: // Wizard
                characterData.attackPattern = AttackPattern.Line; // 직선 마법 공격
                characterData.rangeType = RangeType.Magic;
                break;
                
            case JobClass.Priest:
                characterData.attackPattern = AttackPattern.Cross; // 치유와 지원용 십자
                characterData.rangeType = RangeType.Magic;
                break;
                
            case JobClass.Rogue:
                characterData.attackPattern = AttackPattern.Diagonal; // 대각선 기습
                characterData.rangeType = RangeType.Melee;
                break;
                
            case JobClass.Sage:
                characterData.attackPattern = AttackPattern.Knight; // 복잡한 나이트 패턴
                characterData.rangeType = RangeType.Magic;
                break;
                
            case JobClass.Archer:
                characterData.attackPattern = AttackPattern.Line; // 직선 원거리 공격
                characterData.rangeType = RangeType.Ranged;
                break;
                
            case JobClass.Gunner:
                characterData.attackPattern = AttackPattern.CrossBoard; // 건너편 공격 (A↔B 타일)
                characterData.rangeType = RangeType.Ranged;
                break;
                
            default:
                characterData.attackPattern = AttackPattern.Cross;
                characterData.rangeType = RangeType.Melee;
                break;
        }
    }
    
    private static int GetCostByRarity(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return 1;
            case Rarity.Uncommon: return 2;
            case Rarity.Rare: return 3;
            case Rarity.Epic: return 4;
            case Rarity.Legendary: return 5;
            default: return 1;
        }
    }
    
    private static int GetStarByRarity(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return 1;
            case Rarity.Uncommon: return 2;
            case Rarity.Rare: return 3;
            case Rarity.Epic: return 4;
            case Rarity.Legendary: return 5;
            default: return 1;
        }
    }
    
    private static float GetRangeByJob(JobClass jobClass)
    {
        switch (jobClass)
        {
            case JobClass.Warrior:
            case JobClass.Knight:
            case JobClass.Rogue:
                return 1.0f; // 근접
                
            case JobClass.Mage:
            case JobClass.Priest:
            case JobClass.Sage:
                return 2.0f; // 마법 중거리
                
            case JobClass.Archer:
                return 3.0f; // 원거리
                
            case JobClass.Gunner:
                return 4.0f; // 건너편 공격용 긴 사거리
                
            default:
                return 1.0f;
        }
    }
    
    private static void CreateCharacterDatabase(List<CharacterData> characterList)
    {
        // CharacterDatabaseSO 생성
        CharacterDatabaseSO database = ScriptableObject.CreateInstance<CharacterDatabaseSO>();
        
        // 캐릭터들을 데이터베이스에 추가
        database.tacticalCharacters = characterList;
        
        // CharacterDataSO 형태로도 변환하여 추가 (호환성을 위해)
        database.characters = new List<CharacterDataSO>();
        
        // 데이터베이스 저장
        string databasePath = "Assets/Characters/Generated/CharacterDatabase.asset";
        string directory = Path.GetDirectoryName(databasePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        AssetDatabase.CreateAsset(database, databasePath);
        
        // 초기화
        database.Initialize();
        
        Debug.Log($"📚 캐릭터 데이터베이스 생성 완료: {databasePath}");
        Debug.Log($"📊 등록된 캐릭터 수: {characterList.Count}");
        
        // 생성된 데이터베이스 선택
        Selection.activeObject = database;
        EditorGUIUtility.PingObject(database);
    }
} 