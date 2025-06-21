using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace pjy.Data
{
    /// <summary>
    /// CSV 형태로 캐릭터 데이터를 관리하는 데이터베이스
    /// CSV ↔ ScriptableObject 양방향 변환 지원
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterCSVDatabase", menuName = "pjy/Data/Character CSV Database")]
    public class CharacterCSVDatabase : ScriptableObject
    {
        [Header("CSV 데이터")]
        [TextArea(20, 50)]
        [SerializeField] private string csvData = "";
        
        [Header("캐릭터 목록")]
        [SerializeField] private List<CharacterCSVEntry> _characters = new List<CharacterCSVEntry>();
        
        public List<CharacterCSVEntry> characters => _characters;
        
        [Header("스킬 데이터베이스 참조")]
        [SerializeField] private CharacterSkillDatabase skillDatabase;
        
        /// <summary>
        /// CSV 헤더 정의
        /// </summary>
        private const string CSV_HEADER = "ID,Name,Race,Star,Level,AttackPower,AttackRange,AttackSpeed,MaxHP,MoveSpeed,Cost,RangeType,AttackTargetType,AttackShapeType,IsAreaAttack,AreaAttackRadius,IsBuffSupport,Skills,Behaviors,SpriteResourcePath,PrefabResourcePath,Description";
        
        /// <summary>
        /// CSV에서 데이터 로드
        /// </summary>
        public void LoadFromCSV()
        {
            if (string.IsNullOrEmpty(csvData))
            {
                Debug.LogWarning("[CharacterCSVDatabase] CSV 데이터가 비어있습니다.");
                return;
            }
            
            _characters.Clear();
            string[] lines = csvData.Split('\n');
            
            // 헤더 라인 스킵
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                try
                {
                    CharacterCSVEntry entry = ParseCSVLine(line);
                    if (entry != null)
                    {
                        _characters.Add(entry);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[CharacterCSVDatabase] CSV 라인 파싱 오류 (라인 {i+1}): {e.Message}\n라인: {line}");
                }
            }
            
            Debug.Log($"[CharacterCSVDatabase] CSV에서 {_characters.Count}개의 캐릭터 데이터를 로드했습니다.");
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        /// <summary>
        /// CSV 라인 파싱 (안전한 파싱으로 개선)
        /// </summary>
        private CharacterCSVEntry ParseCSVLine(string line)
        {
            string[] values = ParseCSVValues(line);
            if (values.Length < 22)
            {
                Debug.LogWarning($"[CharacterCSVDatabase] CSV 라인의 컬럼 수가 부족합니다: {values.Length}/22");
                return null;
            }
            
            CharacterCSVEntry entry = new CharacterCSVEntry();
            
            try
            {
                // 안전한 파싱 메서드 사용
                entry.id = SafeParseInt(values[0], 0);
                entry.characterName = values[1].Trim();
                entry.race = SafeParseEnum<CharacterRace>(values[2], CharacterRace.Human);
                entry.star = (CharacterStar)SafeParseInt(values[3], 1);
                entry.level = SafeParseInt(values[4], 1);
                entry.attackPower = SafeParseFloat(values[5], 10f);
                entry.attackRange = SafeParseFloat(values[6], 3f);
                entry.attackSpeed = SafeParseFloat(values[7], 1f);
                entry.maxHP = SafeParseFloat(values[8], 100f);
                entry.moveSpeed = SafeParseFloat(values[9], 1f);
                entry.cost = SafeParseInt(values[10], 10);
                entry.rangeType = SafeParseEnum<RangeType>(values[11], RangeType.Melee);
                entry.attackTargetType = SafeParseEnum<AttackTargetType>(values[12], AttackTargetType.Monster);
                entry.attackShapeType = SafeParseEnum<AttackShapeType>(values[13], AttackShapeType.Single);
                entry.isAreaAttack = SafeParseBool(values[14], false);
                entry.areaAttackRadius = float.Parse(values[15]);
                entry.isBuffSupport = bool.Parse(values[16]);
                entry.skillIds = ParseStringArray(values[17]);
                entry.behaviorComponents = ParseStringArray(values[18]);
                entry.spriteResourcePath = values[19];
                entry.prefabResourcePath = values[20];
                entry.description = values[21];
                
                return entry;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CharacterCSVDatabase] CSV 데이터 파싱 오류: {e.Message}\n라인: {line}");
                return null;
            }
        }
        
        /// <summary>
        /// CSV 값들을 파싱 (쉼표로 구분, 따옴표 처리)
        /// </summary>
        private string[] ParseCSVValues(string line)
        {
            List<string> values = new List<string>();
            bool inQuotes = false;
            string currentValue = "";
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.Trim());
                    currentValue = "";
                }
                else
                {
                    currentValue += c;
                }
            }
            
            // 마지막 값 추가
            values.Add(currentValue.Trim());
            
            return values.ToArray();
        }
        
        /// <summary>
        /// 세미콜론으로 구분된 문자열 배열 파싱
        /// </summary>
        private string[] ParseStringArray(string input)
        {
            if (string.IsNullOrEmpty(input))
                return new string[0];
                
            return input.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }
        
        /// <summary>
        /// 데이터를 CSV로 변환
        /// </summary>
        public void ExportToCSV()
        {
            List<string> lines = new List<string>();
            lines.Add(CSV_HEADER);
            
            foreach (var character in _characters)
            {
                string line = $"{character.id}," +
                             $"{character.characterName}," +
                             $"{character.race}," +
                             $"{(int)character.star}," +
                             $"{character.level}," +
                             $"{character.attackPower}," +
                             $"{character.attackRange}," +
                             $"{character.attackSpeed}," +
                             $"{character.maxHP}," +
                             $"{character.moveSpeed}," +
                             $"{character.cost}," +
                             $"{character.rangeType}," +
                             $"{character.attackTargetType}," +
                             $"{character.attackShapeType}," +
                             $"{character.isAreaAttack}," +
                             $"{character.areaAttackRadius}," +
                             $"{character.isBuffSupport}," +
                             $"\"{string.Join(";", character.skillIds)}\"," +
                             $"\"{string.Join(";", character.behaviorComponents)}\"," +
                             $"{character.spriteResourcePath}," +
                             $"{character.prefabResourcePath}," +
                             $"\"{character.description}\"";
                             
                lines.Add(line);
            }
            
            csvData = string.Join("\n", lines);
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            
            Debug.Log($"[CharacterCSVDatabase] {_characters.Count}개의 캐릭터 데이터를 CSV로 내보냈습니다.");
        }
        
        /// <summary>
        /// CharacterData ScriptableObject 생성
        /// </summary>
        public List<CharacterData> GenerateCharacterData()
        {
            List<CharacterData> characterDataList = new List<CharacterData>();
            
            foreach (var entry in _characters)
            {
                CharacterData data = CreateInstance<CharacterData>();
                
                // 기본 정보
                data.characterName = entry.characterName;
                data.characterIndex = entry.id;
                data.race = entry.race;
                data.star = entry.star;
                data.level = entry.level;
                
                // 전투 스탯
                data.attackPower = entry.attackPower;
                data.attackRange = entry.attackRange;
                data.range = entry.attackRange;
                data.attackSpeed = entry.attackSpeed;
                data.maxHP = entry.maxHP;
                data.maxHealth = entry.maxHP;
                data.health = entry.maxHP;
                data.moveSpeed = entry.moveSpeed;
                data.cost = entry.cost;
                data.rangeType = entry.rangeType;
                data.attackTargetType = entry.attackTargetType;
                data.attackShapeType = entry.attackShapeType;
                data.isAreaAttack = entry.isAreaAttack;
                data.areaAttackRadius = entry.areaAttackRadius;
                data.isBuffSupport = entry.isBuffSupport;
                data.tribe = (RaceType)entry.race;
                data.initialStar = entry.star;
                
                // 리소스 로드
                if (!string.IsNullOrEmpty(entry.spriteResourcePath))
                {
                    data.characterSprite = Resources.Load<Sprite>(entry.spriteResourcePath);
                    data.frontSprite = data.characterSprite;
                    data.backSprite = data.characterSprite;
                    data.buttonIcon = data.characterSprite;
                }
                
                if (!string.IsNullOrEmpty(entry.prefabResourcePath))
                {
                    data.spawnPrefab = Resources.Load<GameObject>(entry.prefabResourcePath);
                    data.motionPrefab = data.spawnPrefab;
                    data.prefabName = entry.prefabResourcePath;
                }
                
                characterDataList.Add(data);
            }
            
            return characterDataList;
        }
        
        /// <summary>
        /// 캐릭터 추가
        /// </summary>
        public void AddCharacter(CharacterCSVEntry character)
        {
            if (character != null)
            {
                _characters.Add(character);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
        
        /// <summary>
        /// 캐릭터 제거
        /// </summary>
        public void RemoveCharacter(int id)
        {
            _characters.RemoveAll(c => c.id == id);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        /// <summary>
        /// 캐릭터 찾기
        /// </summary>
        public CharacterCSVEntry GetCharacter(int id)
        {
            return _characters.Find(c => c.id == id);
        }
        
        /// <summary>
        /// 모든 캐릭터 가져오기
        /// </summary>
        public List<CharacterCSVEntry> GetAllCharacters()
        {
            return new List<CharacterCSVEntry>(characters);
        }
        
        /// <summary>
        /// CharacterData 리스트 가져오기 (호환성을 위한 메서드)
        /// </summary>
        public List<CharacterData> GetCharacterDataList()
        {
            return GenerateCharacterData();
        }
        
        /// <summary>
        /// 스킬 데이터베이스 설정
        /// </summary>
        public void SetSkillDatabase(CharacterSkillDatabase database)
        {
            skillDatabase = database;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        /// <summary>
        /// 캐릭터가 가진 스킬들 가져오기
        /// </summary>
        public List<SkillData> GetCharacterSkills(int characterId)
        {
            CharacterCSVEntry character = GetCharacter(characterId);
            if (character == null || skillDatabase == null)
                return new List<SkillData>();
                
            List<SkillData> skills = new List<SkillData>();
            foreach (string skillId in character.skillIds)
            {
                SkillData skill = skillDatabase.GetSkill(skillId);
                if (skill != null)
                {
                    skills.Add(skill);
                }
            }
            
            return skills;
        }
        
        /// <summary>
        /// 기본 캐릭터 데이터 생성 (샘플)
        /// </summary>
        public void GenerateSampleData()
        {
            _characters.Clear();
            
            // 휴먼 캐릭터들
            _characters.Add(CreateSampleCharacter(1, "인간 전사", CharacterRace.Human, CharacterStar.OneStar, 50, 3, 1.5f, 120, 2.0f, 15, RangeType.Melee, new string[] { "basic_attack", "warrior_strike" }, new string[] { "MeleeAttackBehavior", "TankBehavior" }));
            _characters.Add(CreateSampleCharacter(2, "인간 궁수", CharacterRace.Human, CharacterStar.OneStar, 40, 5, 2.0f, 80, 2.5f, 12, RangeType.Ranged, new string[] { "basic_attack", "power_shot" }, new string[] { "RangedAttackBehavior", "KitingBehavior" }));
            _characters.Add(CreateSampleCharacter(3, "인간 마법사", CharacterRace.Human, CharacterStar.TwoStar, 60, 4, 2.5f, 70, 1.8f, 18, RangeType.LongRange, new string[] { "fireball", "heal", "magic_shield" }, new string[] { "MagicAttackBehavior", "SupportBehavior" }));
            
            // 오크 캐릭터들
            _characters.Add(CreateSampleCharacter(4, "오크 전사", CharacterRace.Orc, CharacterStar.OneStar, 70, 2.5f, 1.2f, 150, 1.5f, 20, RangeType.Melee, new string[] { "basic_attack", "berserker_rage" }, new string[] { "MeleeAttackBehavior", "BerserkerBehavior" }));
            _characters.Add(CreateSampleCharacter(5, "오크 투척병", CharacterRace.Orc, CharacterStar.OneStar, 45, 4, 1.8f, 100, 2.0f, 14, RangeType.Ranged, new string[] { "basic_attack", "axe_throw" }, new string[] { "RangedAttackBehavior", "AggressiveBehavior" }));
            _characters.Add(CreateSampleCharacter(6, "오크 족장", CharacterRace.Orc, CharacterStar.ThreeStar, 90, 3, 1.0f, 200, 1.8f, 30, RangeType.Melee, new string[] { "basic_attack", "war_cry", "intimidate" }, new string[] { "MeleeAttackBehavior", "LeaderBehavior", "BuffBehavior" }));
            
            // 엘프 캐릭터들
            _characters.Add(CreateSampleCharacter(7, "엘프 레인저", CharacterRace.Elf, CharacterStar.OneStar, 45, 6, 2.2f, 75, 3.0f, 16, RangeType.LongRange, new string[] { "basic_attack", "multi_shot" }, new string[] { "RangedAttackBehavior", "MobilityBehavior" }));
            _characters.Add(CreateSampleCharacter(8, "엘프 힐러", CharacterRace.Elf, CharacterStar.TwoStar, 30, 4, 3.0f, 60, 2.2f, 22, RangeType.Ranged, new string[] { "heal", "group_heal", "regeneration" }, new string[] { "SupportBehavior", "HealerBehavior" }));
            _characters.Add(CreateSampleCharacter(9, "엘프 현자", CharacterRace.Elf, CharacterStar.ThreeStar, 80, 5, 2.8f, 90, 2.5f, 28, RangeType.LongRange, new string[] { "lightning_bolt", "teleport", "mana_shield", "wisdom_aura" }, new string[] { "MagicAttackBehavior", "TeleportBehavior", "WisdomBehavior" }));
            
            ExportToCSV();
            
            Debug.Log("[CharacterCSVDatabase] 샘플 캐릭터 데이터 9개를 생성했습니다.");
        }
        
        /// <summary>
        /// 샘플 캐릭터 생성 도우미
        /// </summary>
        private CharacterCSVEntry CreateSampleCharacter(int id, string name, CharacterRace race, CharacterStar star, 
            float attackPower, float attackRange, float attackSpeed, float maxHP, float moveSpeed, int cost, 
            RangeType rangeType, string[] skills, string[] behaviors)
        {
            CharacterCSVEntry entry = new CharacterCSVEntry();
            entry.id = id;
            entry.characterName = name;
            entry.race = race;
            entry.star = star;
            entry.level = 1;
            entry.attackPower = attackPower;
            entry.attackRange = attackRange;
            entry.attackSpeed = attackSpeed;
            entry.maxHP = maxHP;
            entry.moveSpeed = moveSpeed;
            entry.cost = cost;
            entry.rangeType = rangeType;
            entry.attackTargetType = AttackTargetType.Monster;
            entry.attackShapeType = AttackShapeType.Single;
            entry.isAreaAttack = false;
            entry.areaAttackRadius = 1.5f;
            entry.isBuffSupport = false;
            entry.skillIds = skills;
            entry.behaviorComponents = behaviors;
            entry.spriteResourcePath = $"Characters/{race}/{name.Replace(" ", "_")}";
            entry.prefabResourcePath = $"Prefabs/Characters/{race}/{name.Replace(" ", "_")}";
            entry.description = $"{race} 종족의 {name}입니다.";
            
            return entry;
        }
        
        // =======================================================================
        // 안전한 파싱 메서드들
        // =======================================================================
        
        /// <summary>
        /// 안전한 정수 파싱
        /// </summary>
        private int SafeParseInt(string value, int defaultValue)
        {
            if (string.IsNullOrEmpty(value)) return defaultValue;
            return int.TryParse(value.Trim(), out int result) ? result : defaultValue;
        }
        
        /// <summary>
        /// 안전한 실수 파싱
        /// </summary>
        private float SafeParseFloat(string value, float defaultValue)
        {
            if (string.IsNullOrEmpty(value)) return defaultValue;
            return float.TryParse(value.Trim(), out float result) ? result : defaultValue;
        }
        
        /// <summary>
        /// 안전한 불린 파싱
        /// </summary>
        private bool SafeParseBool(string value, bool defaultValue)
        {
            if (string.IsNullOrEmpty(value)) return defaultValue;
            return bool.TryParse(value.Trim(), out bool result) ? result : defaultValue;
        }
        
        /// <summary>
        /// 안전한 열거형 파싱
        /// </summary>
        private T SafeParseEnum<T>(string value, T defaultValue) where T : struct, System.Enum
        {
            if (string.IsNullOrEmpty(value)) return defaultValue;
            return System.Enum.TryParse<T>(value.Trim(), true, out T result) ? result : defaultValue;
        }
        
    }

    /// <summary>
    /// CSV 형태의 캐릭터 데이터 엔트리
    /// </summary>
    [System.Serializable]
    public class CharacterCSVEntry
    {
        [Header("기본 정보")]
        public int id;
        public string characterName;
        public CharacterRace race;
        public CharacterStar star;
        public int level = 1;
        
        [Header("전투 스탯")]
        public float attackPower;
        public float attackRange;
        public float attackSpeed;
        public float maxHP;
        public float moveSpeed;
        public int cost;
        
        [Header("공격 설정")]
        public RangeType rangeType;
        public AttackTargetType attackTargetType;
        public AttackShapeType attackShapeType;
        public bool isAreaAttack;
        public float areaAttackRadius;
        public bool isBuffSupport;
        
        [Header("스킬 & 행동")]
        public string[] skillIds = new string[0];
        public string[] behaviorComponents = new string[0];
        
        [Header("리소스")]
        public string spriteResourcePath;
        public string prefabResourcePath;
        public string description;
    }
}