using UnityEngine;
using System.Collections.Generic;
using AttackPattern = GuildMaster.Data.AttackPatternType;

namespace TacticalTileGame.Data
{
    /// <summary>
    /// 타일 전략 게임용 스킬 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "TacticalSkillData", menuName = "TacticalTileGame/Data/SkillData", order = 2)]
    public class TacticalSkillDataSO : ScriptableObject
    {
        [Header("스킬 기본 정보")]
        public string skillId;
        public string skillName;
        public string description;
        public Sprite skillIcon;
        
        [Header("스킬 속성")]
        public SkillType skillType = SkillType.Damage;
        public TargetType targetType = TargetType.Enemy;
        public int damage = 0;
        public int healing = 0;
        public int manaCost = 0;
        public float cooldown = 0f;
        
        [Header("스킬 범위")]
        public AttackPattern skillPattern;
        public string skillPatternCSV; // CSV에서 읽어온 패턴 문자열
        public bool areaEffect = false; // 범위 공격 여부
        
        [Header("상태 효과")]
        public List<StatusEffect> statusEffects = new List<StatusEffect>();
        
        [Header("스킬 요구사항")]
        public int requiredLevel = 1;
        public CharacterClass requiredClass = CharacterClass.Warrior;
        
        [Header("시각 효과")]
        public string animationName;
        public string soundEffectName;
        public GameObject particleEffectPrefab;
        
        /// <summary>
        /// CSV 데이터로부터 스킬 데이터 생성
        /// </summary>
        public void InitializeFromCSV(Dictionary<string, string> csvData)
        {
            if (csvData.ContainsKey("id")) skillId = csvData["id"];
            if (csvData.ContainsKey("name")) skillName = csvData["name"];
            if (csvData.ContainsKey("description")) description = csvData["description"];
            
            // 스킬 속성 파싱
            if (csvData.ContainsKey("type") && System.Enum.TryParse<SkillType>(csvData["type"], out SkillType type))
                skillType = type;
            if (csvData.ContainsKey("targetType") && System.Enum.TryParse<TargetType>(csvData["targetType"], out TargetType target))
                targetType = target;
            
            if (csvData.ContainsKey("damage") && int.TryParse(csvData["damage"], out int dmg))
                damage = dmg;
            if (csvData.ContainsKey("healing") && int.TryParse(csvData["healing"], out int heal))
                healing = heal;
            if (csvData.ContainsKey("manaCost") && int.TryParse(csvData["manaCost"], out int mana))
                manaCost = mana;
            if (csvData.ContainsKey("cooldown") && float.TryParse(csvData["cooldown"], out float cd))
                cooldown = cd;
            
            // 스킬 범위 파싱
            if (csvData.ContainsKey("skillPattern"))
            {
                skillPatternCSV = csvData["skillPattern"];
                skillPattern = AttackPattern.ParseFromString(skillPatternCSV);
            }
            
            if (csvData.ContainsKey("areaEffect") && bool.TryParse(csvData["areaEffect"], out bool area))
                areaEffect = area;
            
            // 요구사항 파싱
            if (csvData.ContainsKey("requiredLevel") && int.TryParse(csvData["requiredLevel"], out int level))
                requiredLevel = level;
            if (csvData.ContainsKey("requiredClass") && System.Enum.TryParse<CharacterClass>(csvData["requiredClass"], out CharacterClass reqClass))
                requiredClass = reqClass;
            
            // 효과 이름 파싱
            if (csvData.ContainsKey("animation")) animationName = csvData["animation"];
            if (csvData.ContainsKey("sound")) soundEffectName = csvData["sound"];
        }
        
        /// <summary>
        /// 스킬 사용 가능한 타일 위치 계산
        /// </summary>
        public List<Vector2Int> GetTargetableTiles(Vector2Int casterPosition)
        {
            List<Vector2Int> targetableTiles = new List<Vector2Int>();
            
            if (skillPattern != null)
            {
                foreach (var offset in skillPattern.attackTiles)
                {
                    Vector2Int targetTile = casterPosition + offset;
                    targetableTiles.Add(targetTile);
                }
            }
            
            return targetableTiles;
        }
    }
    
    /// <summary>
    /// 스킬 타입
    /// </summary>
    public enum SkillType
    {
        Damage,     // 데미지
        Heal,       // 힐링
        Buff,       // 버프
        Debuff,     // 디버프
        Summon,     // 소환
        Special     // 특수
    }
    
    /// <summary>
    /// 타겟 타입
    /// </summary>
    public enum TargetType
    {
        Self,       // 자신
        Ally,       // 아군
        Enemy,      // 적군
        All,        // 모두
        Empty       // 빈 타일
    }
    
    /// <summary>
    /// 상태 효과
    /// </summary>
    [System.Serializable]
    public class StatusEffect
    {
        public StatusEffectType effectType;
        public float duration;
        public int value;
        
        public StatusEffect(StatusEffectType type, float dur, int val)
        {
            effectType = type;
            duration = dur;
            value = val;
        }
    }
    
    /// <summary>
    /// 상태 효과 타입
    /// </summary>
    public enum StatusEffectType
    {
        Poison,     // 독
        Burn,       // 화상
        Freeze,     // 빙결
        Stun,       // 기절
        Silence,    // 침묵
        Slow,       // 둔화
        AttackUp,   // 공격력 증가
        DefenseUp,  // 방어력 증가
        SpeedUp,    // 속도 증가
        Regeneration // 재생
    }
}