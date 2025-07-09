using UnityEngine;
using System.Collections.Generic;
using AttackPattern = GuildMaster.Data.AttackPatternType;

namespace TacticalTileGame.Data
{
    /// <summary>
    /// 타일 전략 게임용 캐릭터 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "TacticalCharacterData", menuName = "TacticalTileGame/Data/CharacterData", order = 1)]
    public class TacticalCharacterDataSO : ScriptableObject
    {
        [Header("캐릭터 기본 정보")]
        public string characterId;
        public string characterName;
        public string description;
        public Sprite characterIcon;
        public GameObject characterPrefab;
        
        [Header("캐릭터 스탯")]
        public int baseHP = 100;
        public int baseAttack = 10;
        public int baseDefense = 5;
        public int baseMagicPower = 0;
        public int baseSpeed = 5;
        public float critRate = 0.1f;
        
        [Header("공격 범위")]
        public AttackPattern attackPattern;
        public string attackPatternCSV; // CSV에서 읽어온 패턴 문자열
        
        [Header("스킬")]
        public List<string> skillIds = new List<string>();
        public List<TacticalSkillDataSO> skills = new List<TacticalSkillDataSO>();
        
        [Header("캐릭터 특성")]
        public CharacterClass characterClass;
        public CharacterRarity rarity = CharacterRarity.Common;
        public int maxLevel = 50;
        
        /// <summary>
        /// CSV 데이터로부터 캐릭터 데이터 생성
        /// </summary>
        public void InitializeFromCSV(Dictionary<string, string> csvData)
        {
            if (csvData.ContainsKey("id")) characterId = csvData["id"];
            if (csvData.ContainsKey("name")) characterName = csvData["name"];
            if (csvData.ContainsKey("description")) description = csvData["description"];
            
            // 스탯 파싱
            if (csvData.ContainsKey("hp") && int.TryParse(csvData["hp"], out int hp))
                baseHP = hp;
            if (csvData.ContainsKey("attack") && int.TryParse(csvData["attack"], out int atk))
                baseAttack = atk;
            if (csvData.ContainsKey("defense") && int.TryParse(csvData["defense"], out int def))
                baseDefense = def;
            if (csvData.ContainsKey("magic") && int.TryParse(csvData["magic"], out int mag))
                baseMagicPower = mag;
            if (csvData.ContainsKey("speed") && int.TryParse(csvData["speed"], out int spd))
                baseSpeed = spd;
            if (csvData.ContainsKey("critRate") && float.TryParse(csvData["critRate"], out float crit))
                critRate = crit;
            
            // 공격 패턴 파싱
            if (csvData.ContainsKey("attackPattern"))
            {
                attackPatternCSV = csvData["attackPattern"];
                attackPattern = AttackPattern.ParseFromString(attackPatternCSV);
            }
            
            // 스킬 ID 파싱
            if (csvData.ContainsKey("skills"))
            {
                string[] skills = csvData["skills"].Split(',');
                skillIds.Clear();
                foreach (string skillId in skills)
                {
                    if (!string.IsNullOrEmpty(skillId.Trim()))
                    {
                        skillIds.Add(skillId.Trim());
                    }
                }
            }
            
            // 클래스와 레어도 파싱
            if (csvData.ContainsKey("class") && System.Enum.TryParse<CharacterClass>(csvData["class"], out CharacterClass charClass))
                characterClass = charClass;
            if (csvData.ContainsKey("rarity") && System.Enum.TryParse<CharacterRarity>(csvData["rarity"], out CharacterRarity charRarity))
                rarity = charRarity;
        }
        
        /// <summary>
        /// 실제 공격 가능한 타일 위치 계산
        /// </summary>
        public List<Vector2Int> GetAttackableTiles(Vector2Int characterPosition)
        {
            List<Vector2Int> attackableTiles = new List<Vector2Int>();
            
            if (attackPattern != null)
            {
                foreach (var offset in attackPattern.attackTiles)
                {
                    Vector2Int targetTile = characterPosition + offset;
                    attackableTiles.Add(targetTile);
                }
            }
            
            return attackableTiles;
        }
    }
    
    /// <summary>
    /// 캐릭터 클래스
    /// </summary>
    public enum CharacterClass
    {
        Warrior,    // 전사 - 근접 물리 공격
        Knight,     // 기사 - 방어 특화
        Wizard,     // 마법사 - 원거리 마법 공격
        Priest,     // 성직자 - 힐링과 버프
        Rogue,      // 도적 - 빠른 속도와 크리티컬
        Sage,       // 현자 - 만능형
        Archer,     // 궁수 - 원거리 물리 공격
        Gunner      // 총사 - 장거리 단일 공격
    }
    
    /// <summary>
    /// 캐릭터 레어도
    /// </summary>
    public enum CharacterRarity
    {
        Common,     // 일반
        Uncommon,   // 고급
        Rare,       // 희귀
        Epic,       // 영웅
        Legendary   // 전설
    }
}