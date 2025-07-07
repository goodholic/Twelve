using UnityEngine;
using System;
using GuildMaster.Battle;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class SkillData
    {
        [Header("기본 정보")]
        public string skillId;
        public string skillName;
        public string description;
        
        // 호환성을 위한 속성들
        public string id { get => skillId; set => skillId = value; }
        public string name { get => skillName; set => skillName = value; }
        public string nameKey = ""; // 로컬라이제이션 키
        public string descriptionKey = ""; // 로컬라이제이션 키
        
        [Header("스킬 타입")]
        public SkillType skillType;
        public TargetType targetType;
        
        [Header("데미지 및 효과")]
        public float damageMultiplier = 1.0f;
        public float healAmount = 0f;
        public int damage = 0;
        public int baseDamage { get => damage; set => damage = value; } // 호환성
        public int healing = 0;
        public float[] effectValues = new float[0]; // 효과 값들 배열
        
        [Header("상태 효과")]
        public BuffType buffType = BuffType.None;
        public DebuffType debuffType = DebuffType.None;
        public float buffAmount = 0f;
        public float buffDuration = 0f;
        public float duration = 0f;
        
        [Header("비용 및 쿨다운")]
        public int manaCost = 0;
        public float cooldown = 0f;
        public float castTime = 0f;
        
        [Header("범위 및 대상")]
        public float range = 1f;
        public float areaOfEffect = 0f;
        public bool areaEffect = false;
        public int maxTargets = 1;
        public bool piercing = false;
        
        [Header("요구사항")]
        public int requiredLevel = 1;
        public JobClass jobClass = JobClass.Warrior;
        public JobClass requiredJobClass { get => jobClass; set => jobClass = value; } // 호환성
        public string requirements = "";
        
        [Header("추가 속성")]
        public float criticalMultiplier = 1.5f;
        public float statusChance = 1.0f;
        public bool canCrit = true;
        public bool ignoreDefense = false;
        public float lifesteal = 0f;
        
        [Header("비주얼/오디오")]
        public string animationName = "";
        public string animationTrigger = "";
        public string soundEffectName = "";
        public string particleEffectName = "";
        public GameObject effectPrefab;
        public Sprite skillIcon;
        public Sprite iconSprite { get => skillIcon; set => skillIcon = value; } // 호환성
        public string iconPath = ""; // 아이콘 경로
        public AudioClip castSound;
        public AudioClip hitSound;
        
        public SkillData()
        {
            skillId = "";
            skillName = "Unknown Skill";
            description = "";
            nameKey = "skill_unknown";
            descriptionKey = "skill_unknown_desc";
        }
        
        public SkillData(string id, string name, SkillType type, TargetType target)
        {
            skillId = id;
            skillName = name;
            skillType = type;
            targetType = target;
            nameKey = $"skill_{name.ToLower()}";
            descriptionKey = $"skill_{name.ToLower()}_desc";
        }
        
        public bool CanUse(int casterLevel, JobClass casterClass, int currentMana)
        {
            if (casterLevel < requiredLevel)
                return false;
                
            if (jobClass != JobClass.Warrior && casterClass != jobClass)
                return false;
                
            if (currentMana < manaCost)
                return false;
                
            return true;
        }
        
        public int CalculateDamage(int attackerPower)
        {
            return Mathf.RoundToInt((damage + attackerPower) * damageMultiplier);
        }
        
        public int CalculateHealing(int healerPower)
        {
            return Mathf.RoundToInt(healing + healAmount + (healerPower * 0.5f));
        }
        
        public string GetSkillInfo()
        {
            string info = $"<b>{skillName}</b>\n";
            info += $"{description}\n\n";
            info += $"타입: {GetSkillTypeName()}\n";
            info += $"대상: {GetTargetTypeName()}\n";
            
            if (damage > 0 || damageMultiplier > 1.0f)
                info += $"데미지: {damage} (x{damageMultiplier})\n";
                
            if (healing > 0 || healAmount > 0)
                info += $"회복: {healing + healAmount}\n";
                
            if (manaCost > 0)
                info += $"마나 소모: {manaCost}\n";
                
            if (cooldown > 0)
                info += $"쿨다운: {cooldown}초\n";
                
            if (requiredLevel > 1)
                info += $"요구 레벨: {requiredLevel}\n";
                
            return info;
        }
        
        private string GetSkillTypeName()
        {
            return skillType switch
            {
                SkillType.Physical => "물리",
                SkillType.Magical => "마법",
                SkillType.Healing => "치유",
                SkillType.Buff => "버프",
                SkillType.Debuff => "디버프",
                SkillType.Utility => "유틸리티",
                SkillType.Defensive => "방어",
                SkillType.Summon => "소환",
                SkillType.Attack => "공격",
                SkillType.Heal => "치유",
                _ => "알 수 없음"
            };
        }
        
        private string GetTargetTypeName()
        {
            return targetType switch
            {
                TargetType.Self => "자신",
                TargetType.Single => "단일 대상",
                TargetType.AOE => "광역",
                TargetType.Line => "직선",
                TargetType.AllAllies => "모든 아군",
                TargetType.AllEnemies => "모든 적",
                TargetType.Area => "지역",
                TargetType.Enemy => "적",
                TargetType.Ally => "아군",
                _ => "알 수 없음"
            };
        }
    }
    
    public enum SkillType
    {
        Physical,
        Magical,
        Healing,
        Buff,
        Debuff,
        Utility,
        Defensive,
        Summon,
        Attack,
        Heal
    }
    
    public enum TargetType
    {
        Self,
        Single,
        AOE,
        Line,
        AllAllies,
        AllEnemies,
        Area,
        Enemy,
        Ally
    }
    
    public enum BuffType
    {
        None,
        Defense,
        HolyShield,
        Fortress,
        Rage,
        Berserk,
        Immunity,
        Accuracy,
        Blessing,
        DivineProtection,
        Sanctuary,
        Morale,
        Courage,
        Heroic,
        MagicBoost,
        Victory,
        Destiny,
        Speed,
        Regeneration,
        ManaRestore,
        Focus,
        Resistance,
        Alert,
        Counter,
        Survival,
        ElementalShield,
        ManaAbsorb,
        Chivalry,
        LastStand,
        Berserker,
        Unbreakable,
        MagicShield,
        BeastForm,
        EarthBlessing,
        ShadowWalk,
        BattleSong,
        Clone,
        Stealth
    }
    
    public enum DebuffType
    {
        None,
        Taunt,
        Stun,
        Knockdown,
        Poison,
        Freeze,
        Paralyze,
        Burn,
        Slow,
        Blind,
        Fear,
        Sleep,
        Debuff,
        Silence,
        Root,
        Entangle,
        DeadlyPoison,
        Confusion,
        Charm,
        TimeStop,
        Knockback,
        Net,
        Weakness,
        Pacify,
        Despair,
        ElementalDamage,
        Void,
        Mark,
        Multiple
    }
} 