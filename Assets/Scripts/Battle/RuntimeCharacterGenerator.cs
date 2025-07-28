using UnityEngine;
using System.Collections.Generic;
using GuildMaster.Data;
using TacticalTileGame.Data;

public class RuntimeCharacterGenerator : MonoBehaviour
{
    [Header("캐릭터 생성")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool generateNow = false;
    
    private void Start()
    {
        if (generateOnStart)
        {
            GenerateAllCharacters();
        }
    }
    
    private void OnValidate()
    {
        if (generateNow)
        {
            generateNow = false;
            GenerateAllCharacters();
        }
    }
    
    [ContextMenu("Generate Characters")]
    public void GenerateAllCharacters()
    {
        GenerateTestCharacters();
    }
    
    /// <summary>
    /// 테스트용 캐릭터 생성 (외부에서 호출 가능)
    /// </summary>
    public void GenerateTestCharacters()
    {
        Debug.Log("🚀 테스트 캐릭터 생성 시작...");
        
        // CSV 데이터
        var characterData = new (string id, string name, string job, string rarity, int hp, int mp, int atk, int def, int mag, float crit, float critDmg, float acc, float eva, string skill, string desc)[]
        {
            ("CHAR101", "철혈의 로젤리아", "Warrior", "Epic", 1440, 120, 180, 120, 36, 10.0f, 165.0f, 95.0f, 5.0f, "SKILL_W001", "강인한 체력과 힘으로 전장을 지배하는 여전사"),
            ("CHAR102", "폭풍의 카트리나", "Warrior", "Rare", 1320, 110, 165, 110, 33, 8.0f, 160.0f, 94.0f, 6.0f, "SKILL_W004", "폭풍처럼 거센 전투를 즐기는 광전사"),
            ("CHAR103", "성기사 세라피나", "Knight", "Epic", 1560, 180, 108, 225, 72, 7.0f, 152.0f, 97.0f, 4.0f, "SKILL_K001", "신성한 빛으로 아군을 수호하는 여기사"),
            ("CHAR104", "수호자 발레리아", "Knight", "Rare", 1430, 165, 99, 210, 66, 5.0f, 145.0f, 96.0f, 3.0f, "SKILL_K004", "철벽같은 방어로 동료를 지키는 수호기사"),
            ("CHAR105", "원소술사 엘레노라", "Wizard", "Epic", 560, 600, 40, 56, 300, 13.0f, 175.0f, 98.0f, 7.0f, "SKILL_M001", "4원소를 자유자재로 다루는 대마법사"),
            ("CHAR106", "시공술사 크리스티나", "Wizard", "Legendary", 630, 650, 45, 63, 330, 15.0f, 180.0f, 99.0f, 8.0f, "SKILL_M004", "시간과 공간을 조작하는 전설의 마법사"),
            ("CHAR107", "대사제 안젤리카", "Priest", "Epic", 850, 525, 50, 80, 300, 8.0f, 135.0f, 99.0f, 8.0f, "SKILL_P001", "생명의 기적을 행하는 성스러운 사제"),
            ("CHAR108", "치유사 루시아나", "Priest", "Rare", 765, 480, 45, 72, 270, 6.0f, 130.0f, 98.0f, 7.0f, "SKILL_P004", "빛의 축복으로 상처를 치유하는 성직자"),
            ("CHAR109", "그림자 나타샤", "Rogue", "Epic", 680, 160, 196, 72, 96, 20.0f, 185.0f, 96.0f, 22.0f, "SKILL_R001", "어둠 속을 누비는 은밀한 암살자"),
            ("CHAR110", "질풍의 실비아", "Rogue", "Rare", 595, 140, 182, 66, 88, 18.0f, 180.0f, 95.0f, 20.0f, "SKILL_R004", "바람처럼 빠르고 날렵한 도적"),
            ("CHAR111", "대현자 미네르바", "Sage", "Legendary", 1200, 360, 150, 108, 240, 17.0f, 165.0f, 98.0f, 15.0f, "SKILL_S001", "지혜와 힘을 겸비한 전설의 여현자"),
            ("CHAR112", "지혜의 소피아", "Sage", "Epic", 1000, 300, 130, 99, 216, 14.0f, 160.0f, 97.0f, 13.0f, "SKILL_S004", "모든 지식을 탐구하는 현명한 현자"),
            ("CHAR113", "신궁 아르테미시아", "Archer", "Epic", 810, 200, 130, 96, 96, 16.0f, 165.0f, 99.9f, 18.0f, "SKILL_A001", "절대 빗나가지 않는 전설의 여궁수"),
            ("CHAR114", "바람궁수 실바나", "Archer", "Rare", 720, 180, 120, 88, 88, 14.0f, 160.0f, 98.0f, 16.0f, "SKILL_A004", "자연의 가호를 받은 엘프 궁수"),
            ("CHAR115", "명사수 빅토리아", "Gunner", "Legendary", 750, 100, 225, 90, 50, 22.0f, 200.0f, 99.9f, 10.0f, "SKILL_G001", "한 발로 적을 제압하는 전설의 저격수"),
            ("CHAR116", "쌍권총 카밀라", "Gunner", "Epic", 675, 90, 210, 81, 45, 20.0f, 190.0f, 98.0f, 9.0f, "SKILL_G004", "양손의 권총으로 화려한 사격술을 선보이는 여총잡이")
        };
        
        List<CharacterData> characters = new List<CharacterData>();
        
        foreach (var data in characterData)
        {
            CharacterData character = CreateCharacter(data);
            if (character != null)
            {
                characters.Add(character);
                Debug.Log($"✅ 캐릭터 생성: {character.characterName} ({character.jobClass}, {character.rarity})");
            }
        }
        
        // 데이터베이스 생성 (에디터에서만)
        #if UNITY_EDITOR
        CreateDatabase(characters);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        #endif
        
        Debug.Log($"🎉 총 {characters.Count}개 캐릭터 생성 완료!");
    }
    
    private CharacterData CreateCharacter((string id, string name, string job, string rarity, int hp, int mp, int atk, int def, int mag, float crit, float critDmg, float acc, float eva, string skill, string desc) data)
    {
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
        
        // 기본 정보
        character.id = data.id;
        character.characterName = data.name;
        character.jobClass = ParseJobClass(data.job);
        character.rarity = ParseRarity(data.rarity);
        character.tacticalRarity = ParseCharacterRarity(data.rarity);
        character.description = data.desc;
        character.skillId = data.skill;
        
        // 스탯
        character.baseHP = data.hp;
        character.maxHP = data.hp;
        character.hp = data.hp;
        character.health = data.hp;
        character.baseMP = data.mp;
        character.baseAttack = data.atk;
        character.attackPower = data.atk;
        character.baseDefense = data.def;
        character.baseMagicPower = data.mag;
        
        // 전투 스탯
        character.critRate = data.crit / 100f;
        character.baseCritRate = character.critRate;
        character.critDamage = data.critDmg;
        character.accuracy = data.acc;
        character.evasion = data.eva;
        
        // 공격 패턴 설정
        SetAttackPattern(character);
        
        // 동영상 애니메이션 설정
                    character.animationType = AnimationType.PNGSequence;
                    character.pngSequenceScale = 1.0f;
            character.loopPNGSequences = true;
        
        // 기타 설정
        character.star = GetStarByRarity(character.rarity);
        character.starLevel = character.star;
        character.initialStar = character.star;
        character.level = 1;
        character.attackSpeed = 1.0f;
        character.attackRange = GetRangeByJob(character.jobClass);
        
        #if UNITY_EDITOR
        // 에디터에서만 Asset으로 저장
        string fileName = $"{character.id}_{character.characterName.Replace(" ", "").Replace("의", "").Replace("자", "")}.asset";
        string path = $"Assets/Characters/Generated/{fileName}";
        
        if (!System.IO.Directory.Exists("Assets/Characters/Generated"))
        {
            System.IO.Directory.CreateDirectory("Assets/Characters/Generated");
        }
        
        UnityEditor.AssetDatabase.CreateAsset(character, path);
        #endif
        
        return character;
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
            case "Gunner": return JobClass.Archer;
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
    
    private void SetAttackPattern(CharacterData character)
    {
        switch (character.jobClass)
        {
            case JobClass.Warrior:
                character.attackPattern = AttackPattern.Cross; // 십자 공격
                character.rangeType = RangeType.Melee;
                break;
            case JobClass.Knight:
                character.attackPattern = AttackPattern.Cross; // 방어형 십자 공격
                character.rangeType = RangeType.Melee;
                break;
            case JobClass.Mage:
                character.attackPattern = AttackPattern.Line; // 직선 마법 공격
                character.rangeType = RangeType.Magic;
                break;
            case JobClass.Priest:
                character.attackPattern = AttackPattern.Cross; // 치유용 십자
                character.rangeType = RangeType.Magic;
                break;
            case JobClass.Rogue:
                character.attackPattern = AttackPattern.Diagonal; // 대각선 기습
                character.rangeType = RangeType.Melee;
                break;
            case JobClass.Sage:
                character.attackPattern = AttackPattern.Knight; // 복잡한 나이트 패턴
                character.rangeType = RangeType.Magic;
                break;
            case JobClass.Archer:
                character.attackPattern = AttackPattern.Line; // 직선 원거리
                character.rangeType = RangeType.Ranged;
                break;
            case JobClass.Gunner:
                character.attackPattern = AttackPattern.CrossBoard; // 건너편 공격 (A↔B 타일)
                character.rangeType = RangeType.Ranged;
                break;
            default:
                character.attackPattern = AttackPattern.Cross;
                character.rangeType = RangeType.Melee;
                break;
        }
    }
    
    private int GetStarByRarity(Rarity rarity)
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
    
    private float GetRangeByJob(JobClass jobClass)
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
            case JobClass.Gunner:
                return 3.0f; // 원거리
            default:
                return 1.0f;
        }
    }
    
    #if UNITY_EDITOR
    private void CreateDatabase(List<CharacterData> characters)
    {
        string mainPath = "Assets/Prefabs/Data/Characters/CharacterDatabaseSO.asset";
        CharacterDatabaseSO database;
        
        // 기존 데이터베이스가 있으면 업데이트, 없으면 새로 생성
        database = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>(mainPath);
        
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<CharacterDatabaseSO>();
            
            // 폴더 생성
            string directory = System.IO.Path.GetDirectoryName(mainPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            UnityEditor.AssetDatabase.CreateAsset(database, mainPath);
            Debug.Log($"📚 새 CharacterDatabaseSO 생성: {mainPath}");
        }
        else
        {
            Debug.Log($"📚 기존 CharacterDatabaseSO 업데이트: {mainPath}");
        }
        
        // 캐릭터 데이터 설정
        database.tacticalCharacters = new List<CharacterData>(characters);
        if (database.characters == null)
            database.characters = new List<CharacterDataSO>();
        
        // 변경사항 저장
        UnityEditor.EditorUtility.SetDirty(database);
        UnityEditor.AssetDatabase.SaveAssets();
        
        database.Initialize();
        
        Debug.Log($"✅ CharacterDatabaseSO 업데이트 완료: {characters.Count}개 캐릭터");
        
        // Resources 폴더에도 복사
        CopyToResources(database, mainPath);
        
        UnityEditor.Selection.activeObject = database;
        UnityEditor.EditorGUIUtility.PingObject(database);
    }
    
    private void CopyToResources(CharacterDatabaseSO database, string sourcePath)
    {
        try
        {
            string resourcesDir = "Assets/Resources";
            string dataDir = "Assets/Resources/Data";
            string targetPath = "Assets/Resources/Data/CharacterDatabase.asset";
            
            // 폴더 생성
            if (!System.IO.Directory.Exists(resourcesDir))
                System.IO.Directory.CreateDirectory(resourcesDir);
                
            if (!System.IO.Directory.Exists(dataDir))
                System.IO.Directory.CreateDirectory(dataDir);
            
            // 기존 파일 삭제 후 복사
            if (System.IO.File.Exists(targetPath))
                UnityEditor.AssetDatabase.DeleteAsset(targetPath);
                
            if (UnityEditor.AssetDatabase.CopyAsset(sourcePath, targetPath))
            {
                UnityEditor.AssetDatabase.Refresh();
                Debug.Log($"📁 CharacterDatabaseSO를 Resources 폴더로 복사: {targetPath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Resources 폴더 복사 실패: {e.Message}");
        }
    }
    #endif
} 