using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using GuildMaster.Data;
using TacticalTileGame.Data;

public static class QuickCharacterCreator
{
    public static void CreateAllCharacters()
    {
        Debug.Log("🚀 CSV에서 캐릭터 생성을 시작합니다...");
        
        // CSV 데이터 (character_csv_data.txt에서 복사)
        string[] characterLines = new string[]
        {
            "CHAR101,철혈의 로젤리아,Warrior,1,Epic,1440,120,180,120,36,10.0,165.0,95.0,5.0,SKILL_W001,강인한 체력과 힘으로 전장을 지배하는 여전사",
            "CHAR102,폭풍의 카트리나,Warrior,1,Rare,1320,110,165,110,33,8.0,160.0,94.0,6.0,SKILL_W004,폭풍처럼 거센 전투를 즐기는 광전사",
            "CHAR103,성기사 세라피나,Knight,1,Epic,1560,180,108,225,72,7.0,152.0,97.0,4.0,SKILL_K001,신성한 빛으로 아군을 수호하는 여기사",
            "CHAR104,수호자 발레리아,Knight,1,Rare,1430,165,99,210,66,5.0,145.0,96.0,3.0,SKILL_K004,철벽같은 방어로 동료를 지키는 수호기사",
            "CHAR105,원소술사 엘레노라,Wizard,1,Epic,560,600,40,56,300,13.0,175.0,98.0,7.0,SKILL_M001,4원소를 자유자재로 다루는 대마법사",
            "CHAR106,시공술사 크리스티나,Wizard,1,Legendary,630,650,45,63,330,15.0,180.0,99.0,8.0,SKILL_M004,시간과 공간을 조작하는 전설의 마법사",
            "CHAR107,대사제 안젤리카,Priest,1,Epic,850,525,50,80,300,8.0,135.0,99.0,8.0,SKILL_P001,생명의 기적을 행하는 성스러운 사제",
            "CHAR108,치유사 루시아나,Priest,1,Rare,765,480,45,72,270,6.0,130.0,98.0,7.0,SKILL_P004,빛의 축복으로 상처를 치유하는 성직자",
            "CHAR109,그림자 나타샤,Rogue,1,Epic,680,160,196,72,96,20.0,185.0,96.0,22.0,SKILL_R001,어둠 속을 누비는 은밀한 암살자",
            "CHAR110,질풍의 실비아,Rogue,1,Rare,595,140,182,66,88,18.0,180.0,95.0,20.0,SKILL_R004,바람처럼 빠르고 날렵한 도적",
            "CHAR111,대현자 미네르바,Sage,1,Legendary,1200,360,150,108,240,17.0,165.0,98.0,15.0,SKILL_S001,지혜와 힘을 겸비한 전설의 여현자",
            "CHAR112,지혜의 소피아,Sage,1,Epic,1000,300,130,99,216,14.0,160.0,97.0,13.0,SKILL_S004,모든 지식을 탐구하는 현명한 현자",
            "CHAR113,신궁 아르테미시아,Archer,1,Epic,810,200,130,96,96,16.0,165.0,99.9,18.0,SKILL_A001,절대 빗나가지 않는 전설의 여궁수",
            "CHAR114,바람궁수 실바나,Archer,1,Rare,720,180,120,88,88,14.0,160.0,98.0,16.0,SKILL_A004,자연의 가호를 받은 엘프 궁수",
            "CHAR115,명사수 빅토리아,Gunner,1,Legendary,750,100,225,90,50,22.0,200.0,99.9,10.0,SKILL_G001,한 발로 적을 제압하는 전설의 저격수",
            "CHAR116,쌍권총 카밀라,Gunner,1,Epic,675,90,210,81,45,20.0,190.0,98.0,9.0,SKILL_G004,양손의 권총으로 화려한 사격술을 선보이는 여총잡이"
        };
        
        List<CharacterData> characterList = new List<CharacterData>();
        
        // 각 캐릭터 데이터 생성
        foreach (string line in characterLines)
        {
            CharacterData character = CreateCharacterFromLine(line);
            if (character != null)
            {
                characterList.Add(character);
                Debug.Log($"✅ 캐릭터 생성: {character.characterName} ({character.jobClass}, {character.rarity})");
            }
        }
        
        // 캐릭터 데이터베이스 생성
        CreateDatabase(characterList);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"🎉 캐릭터 생성 완료! 총 {characterList.Count}개 캐릭터와 데이터베이스가 생성되었습니다.");
    }
    
    private static CharacterData CreateCharacterFromLine(string csvLine)
    {
        string[] values = csvLine.Split(',');
        if (values.Length < 16) return null;
        
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
        
        // CSV 데이터 파싱
        character.id = values[0];
        character.characterName = values[1];
        character.jobClass = ParseJobClass(values[2]);
        character.level = int.Parse(values[3]);
        character.rarity = ParseRarity(values[4]);
        character.tacticalRarity = ParseCharacterRarity(values[4]);
        
        // 스탯 설정
        character.baseHP = int.Parse(values[5]);
        character.maxHP = character.baseHP;
        character.hp = character.baseHP;
        character.health = character.baseHP;
        
        character.baseMP = int.Parse(values[6]);
        character.baseAttack = int.Parse(values[7]);
        character.attackPower = character.baseAttack;
        character.baseDefense = int.Parse(values[8]);
        character.baseMagicPower = int.Parse(values[9]);
        
        // 전투 스탯
        character.critRate = float.Parse(values[10]) / 100f;
        character.baseCritRate = character.critRate;
        character.critDamage = float.Parse(values[11]);
        character.accuracy = float.Parse(values[12]);
        character.evasion = float.Parse(values[13]);
        
        character.skillId = values[14];
        character.description = values[15];
        
        // 공격 패턴 설정
        SetAttackPattern(character);
        
        // PNG 시퀀스 애니메이션 설정
        character.animationType = AnimationType.PNGSequence;
        character.pngSequenceScale = 1.0f;
        character.loopPNGSequences = true;
        
        // 기타 설정
        character.star = GetStarByRarity(character.rarity);
        character.starLevel = character.star;
        character.initialStar = character.star;
        character.attackSpeed = 1.0f;
        character.attackRange = GetRangeByJob(character.jobClass);
        
        // 저장
        string fileName = $"{character.id}_{character.characterName.Replace(" ", "").Replace("의", "").Replace("자", "")}.asset";
        string path = $"Assets/Characters/Generated/{fileName}";
        
        // 폴더 생성
        if (!System.IO.Directory.Exists("Assets/Characters/Generated"))
        {
            System.IO.Directory.CreateDirectory("Assets/Characters/Generated");
        }
        
        AssetDatabase.CreateAsset(character, path);
        
        return character;
    }
    
    private static void SetAttackPattern(CharacterData character)
    {
        switch (character.jobClass)
        {
            case JobClass.Warrior:
                character.attackPattern = AttackPattern.Cross;
                character.rangeType = RangeType.Melee;
                break;
            case JobClass.Knight:
                character.attackPattern = AttackPattern.Cross;
                character.rangeType = RangeType.Melee;
                break;
            case JobClass.Mage:
                character.attackPattern = AttackPattern.Line;
                character.rangeType = RangeType.Magic;
                break;
            case JobClass.Priest:
                character.attackPattern = AttackPattern.Cross;
                character.rangeType = RangeType.Magic;
                break;
            case JobClass.Rogue:
                character.attackPattern = AttackPattern.Diagonal;
                character.rangeType = RangeType.Melee;
                break;
            case JobClass.Sage:
                character.attackPattern = AttackPattern.Knight;
                character.rangeType = RangeType.Magic;
                break;
            case JobClass.Archer:
                character.attackPattern = AttackPattern.Line;
                character.rangeType = RangeType.Ranged;
                break;
            default:
                character.attackPattern = AttackPattern.Cross;
                character.rangeType = RangeType.Melee;
                break;
        }
    }
    
    private static JobClass ParseJobClass(string jobClass)
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
            case "Gunner": return JobClass.Gunner;
            default: return JobClass.None;
        }
    }
    
    private static Rarity ParseRarity(string rarity)
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
    
    private static CharacterRarity ParseCharacterRarity(string rarity)
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
                return 1.0f;
            case JobClass.Mage:
            case JobClass.Priest:
            case JobClass.Sage:
                return 2.0f;
            case JobClass.Archer:
                return 3.0f;
            default:
                return 1.0f;
        }
    }
    
    private static void CreateDatabase(List<CharacterData> characters)
    {
        CharacterDatabaseSO database = ScriptableObject.CreateInstance<CharacterDatabaseSO>();
        database.tacticalCharacters = characters;
        database.characters = new List<CharacterDataSO>();
        
        string path = "Assets/Characters/Generated/CharacterDatabase.asset";
        AssetDatabase.CreateAsset(database, path);
        
        database.Initialize();
        
        Debug.Log($"📚 캐릭터 데이터베이스 생성: {path}");
        
        Selection.activeObject = database;
        EditorGUIUtility.PingObject(database);
    }
}

public class QuickCharacterCreatorMenu
{
    [MenuItem("Tools/Quick Create Characters")]
    public static void CreateCharacters()
    {
        QuickCharacterCreator.CreateAllCharacters();
    }
} 