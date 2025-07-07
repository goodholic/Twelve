using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Data;

namespace GuildMaster.Battle
{
    public enum SkillType
    {
        Active,         // 능동 스킬
        Passive,        // 패시브 스킬
        Ultimate,       // 궁극기
        Damage,         // 피해 스킬 (BattleAnimationSystem 호환성)
        Heal,           // 치유 스킬 (BattleAnimationSystem 호환성)
        Buff,           // 버프 스킬 (BattleAnimationSystem 호환성)
        Debuff,         // 디버프 스킬 (BattleAnimationSystem 호환성)
        Physical,       // 물리 스킬
        Magical,        // 마법 스킬
        Healing,        // 치유
        Utility,        // 유틸리티
        Defensive,      // 방어형
        Summon          // 소환
    }
    
    public enum SkillTargetType
    {
        Self,           // 자신
        SingleAlly,     // 아군 단일
        SingleEnemy,    // 적 단일
        AllAllies,      // 모든 아군
        AllEnemies,     // 모든 적
        Squad,          // 분대 전체
        Row,            // 한 줄
        Column,         // 한 열
        Area            // 범위
    }
    
    public enum SkillEffectType
    {
        Damage,         // 피해
        Heal,           // 치유
        Buff,           // 버프
        Debuff,         // 디버프
        Summon,         // 소환
        Shield,         // 보호막
        StatusEffect,   // 상태 효과
        Special         // 특수 효과
    }
    
    public enum StatusEffectType
    {
        Stun,           // 기절
        Poison,         // 중독
        Burn,           // 화상
        Freeze,         // 빙결
        Silence,        // 침묵
        Blind,          // 실명
        Slow,           // 둔화
        Bleed,          // 출혈
        Regeneration,   // 재생
        Invulnerable    // 무적
    }
    
    public class Skill
    {
        public string SkillId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public SkillType Type { get; set; }
        public SkillType skillType => Type; // 호환성을 위한 속성
        public JobClass RequiredClass { get; set; }
        public int RequiredLevel { get; set; }
        
        // Targeting
        public SkillTargetType TargetType { get; set; }
        public int AreaRadius { get; set; } // For area effects
        
        // Cost
        public int ManaCost { get; set; }
        public int HealthCost { get; set; }
        public float CooldownTime { get; set; }
        public int UsesPerBattle { get; set; } // -1 for unlimited
        
        // Effects
        public List<SkillEffect> Effects { get; set; }
        
        // Animation/Visual
        public string AnimationId { get; set; }
        public string EffectPrefabId { get; set; }
        
        // Scaling
        public float PowerScaling { get; set; } // How much skill scales with unit's power
        public float LevelScaling { get; set; } // How much skill improves per skill level
        
        // Current state
        public int CurrentLevel { get; set; }
        public float CurrentCooldown { get; set; }
        public int UsesRemaining { get; set; }
        
        public Skill(string name, SkillType type, JobClass requiredClass)
        {
            SkillId = Guid.NewGuid().ToString();
            Name = name;
            Type = type;
            RequiredClass = requiredClass;
            Effects = new List<SkillEffect>();
            PowerScaling = 1.0f;
            LevelScaling = 0.1f;
            CurrentLevel = 1;
            UsesPerBattle = -1;
        }
        
        public bool CanUse(UnitStatus caster)
        {
            if (caster.JobClass != RequiredClass && RequiredClass != JobClass.None)
                return false;
                
            if (caster.Level < RequiredLevel)
                return false;
                
            if (caster.CurrentMana < ManaCost)
                return false;
                
            if (caster.CurrentHealth <= HealthCost)
                return false;
                
            if (CurrentCooldown > 0)
                return false;
                
            if (UsesPerBattle > 0 && UsesRemaining <= 0)
                return false;
                
            return true;
        }
        
        public float CalculateEffectValue(UnitStatus caster, SkillEffect effect)
        {
            float baseValue = effect.BaseValue;
            float scaling = 0f;
            
            switch (effect.ScalesWith)
            {
                case "attack":
                    scaling = caster.Attack * effect.ScalingRatio;
                    break;
                case "magic":
                    scaling = caster.MagicPower * effect.ScalingRatio;
                    break;
                case "defense":
                    scaling = caster.Defense * effect.ScalingRatio;
                    break;
                case "maxhealth":
                    scaling = caster.MaxHealth * effect.ScalingRatio;
                    break;
            }
            
            float levelBonus = CurrentLevel * LevelScaling;
            return (baseValue + scaling) * (1f + levelBonus) * PowerScaling;
        }
        
        public int GetManaCost()
        {
            return ManaCost;
        }
        
        public SkillData GetSkillData()
        {
            return new SkillData
            {
                id = SkillId,
                name = Name,
                description = Description,
                skillType = (GuildMaster.Data.SkillType)Type,
                manaCost = ManaCost
            };
        }
        
        public void StartCooldown()
        {
            CurrentCooldown = CooldownTime;
            if (UsesPerBattle > 0)
            {
                UsesRemaining--;
            }
        }
    }
    
    public class SkillEffect
    {
        public SkillEffectType Type { get; set; }
        public float BaseValue { get; set; }
        public string ScalesWith { get; set; } // "attack", "magic", "defense", etc.
        public float ScalingRatio { get; set; }
        public float Duration { get; set; } // For buffs/debuffs
        public StatusEffectType? StatusEffect { get; set; }
        public float ProcChance { get; set; } = 1f; // Chance for effect to apply
        
        public SkillEffect(SkillEffectType type, float baseValue)
        {
            Type = type;
            BaseValue = baseValue;
            ProcChance = 1f;
        }
    }
    
    public class StatusEffect
    {
        public string EffectId { get; set; }
        public StatusEffectType Type { get; set; }
        public float Duration { get; set; }
        public float RemainingDuration { get; set; }
        public float TickInterval { get; set; } // For DoT effects
        public float Value { get; set; } // Damage/heal per tick or stat modifier
        public UnitStatus Source { get; set; }
        public Skill SourceSkill { get; set; }
        
        public StatusEffect(StatusEffectType type, float duration, float value)
        {
            EffectId = Guid.NewGuid().ToString();
            Type = type;
            Duration = duration;
            RemainingDuration = duration;
            Value = value;
            TickInterval = 1f; // Default 1 second
        }
        
        public bool IsExpired => RemainingDuration <= 0;
        
        public void UpdateDuration(float deltaTime)
        {
            RemainingDuration -= deltaTime;
        }
    }
    
    public class SkillManager : MonoBehaviour
    {
        // Skill database
        private Dictionary<string, Skill> allSkills;
        private Dictionary<string, List<string>> classSkills; // JobClass -> Skill IDs
        private Dictionary<string, List<string>> unitSkills; // Unit ID -> Skill IDs
        
        // Active status effects
        private Dictionary<string, List<StatusEffect>> activeStatusEffects; // Unit ID -> Effects
        
        // Skill templates
        private Dictionary<string, SkillTemplate> skillTemplates;
        
        // Events
        public event Action<UnitStatus, Skill, List<UnitStatus>> OnSkillUsed;
        public event Action<UnitStatus, StatusEffect> OnStatusEffectApplied;
        public event Action<UnitStatus, StatusEffect> OnStatusEffectRemoved;
        public event Action<UnitStatus, Skill> OnSkillLearned;
        public event Action<UnitStatus, Skill> OnSkillUpgraded;
        
        void Awake()
        {
            allSkills = new Dictionary<string, Skill>();
            classSkills = new Dictionary<string, List<string>>();
            unitSkills = new Dictionary<string, List<string>>();
            activeStatusEffects = new Dictionary<string, List<StatusEffect>>();
            skillTemplates = new Dictionary<string, SkillTemplate>();
            
            InitializeSkills();
        }
        
        void InitializeSkills()
        {
            // Warrior Skills
            CreateSkill("skill_slash", "검격", SkillType.Active, JobClass.Warrior, skill =>
            {
                skill.Description = "강력한 검격으로 적 단일에게 피해를 입힙니다.";
                skill.TargetType = SkillTargetType.SingleEnemy;
                skill.ManaCost = 20;
                skill.CooldownTime = 3f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Damage, 50f)
                {
                    ScalesWith = "attack",
                    ScalingRatio = 1.5f
                });
            });
            
            CreateSkill("skill_whirlwind", "회전베기", SkillType.Active, JobClass.Warrior, skill =>
            {
                skill.Description = "주변 모든 적에게 피해를 입힙니다.";
                skill.TargetType = SkillTargetType.Area;
                skill.AreaRadius = 2;
                skill.ManaCost = 40;
                skill.CooldownTime = 10f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Damage, 30f)
                {
                    ScalesWith = "attack",
                    ScalingRatio = 1.0f
                });
            });
            
            CreateSkill("skill_battlecry", "전투의 함성", SkillType.Ultimate, JobClass.Warrior, skill =>
            {
                skill.Description = "아군 분대의 공격력과 방어력을 증가시킵니다.";
                skill.TargetType = SkillTargetType.Squad;
                skill.ManaCost = 60;
                skill.CooldownTime = 30f;
                skill.UsesPerBattle = 1;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Buff, 0.25f)
                {
                    Duration = 15f
                });
            });
            
            // Mage Skills
            CreateSkill("skill_fireball", "화염구", SkillType.Active, JobClass.Mage, skill =>
            {
                skill.Description = "적 단일에게 화염 피해를 입힙니다.";
                skill.TargetType = SkillTargetType.SingleEnemy;
                skill.ManaCost = 30;
                skill.CooldownTime = 2f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Damage, 40f)
                {
                    ScalesWith = "magic",
                    ScalingRatio = 1.8f
                });
                skill.Effects.Add(new SkillEffect(SkillEffectType.StatusEffect, 5f)
                {
                    StatusEffect = StatusEffectType.Burn,
                    Duration = 5f,
                    ProcChance = 0.3f
                });
            });
            
            CreateSkill("skill_blizzard", "눈보라", SkillType.Active, JobClass.Mage, skill =>
            {
                skill.Description = "적 전체에게 얼음 피해를 입히고 둔화시킵니다.";
                skill.TargetType = SkillTargetType.AllEnemies;
                skill.ManaCost = 80;
                skill.CooldownTime = 15f;
                skill.RequiredLevel = 10;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Damage, 25f)
                {
                    ScalesWith = "magic",
                    ScalingRatio = 1.2f
                });
                skill.Effects.Add(new SkillEffect(SkillEffectType.StatusEffect, 0.3f)
                {
                    StatusEffect = StatusEffectType.Slow,
                    Duration = 8f,
                    ProcChance = 0.5f
                });
            });
            
            CreateSkill("skill_meteor", "메테오", SkillType.Ultimate, JobClass.Mage, skill =>
            {
                skill.Description = "거대한 운석을 소환하여 범위 피해를 입힙니다.";
                skill.TargetType = SkillTargetType.Area;
                skill.AreaRadius = 3;
                skill.ManaCost = 120;
                skill.CooldownTime = 45f;
                skill.UsesPerBattle = 1;
                skill.RequiredLevel = 15;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Damage, 100f)
                {
                    ScalesWith = "magic",
                    ScalingRatio = 2.5f
                });
            });
            
            // Priest Skills
            CreateSkill("skill_heal", "치유", SkillType.Active, JobClass.Priest, skill =>
            {
                skill.Description = "아군 단일의 체력을 회복시킵니다.";
                skill.TargetType = SkillTargetType.SingleAlly;
                skill.ManaCost = 25;
                skill.CooldownTime = 3f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Heal, 50f)
                {
                    ScalesWith = "magic",
                    ScalingRatio = 1.5f
                });
            });
            
            CreateSkill("skill_mass_heal", "대치유", SkillType.Active, JobClass.Priest, skill =>
            {
                skill.Description = "모든 아군의 체력을 회복시킵니다.";
                skill.TargetType = SkillTargetType.AllAllies;
                skill.ManaCost = 60;
                skill.CooldownTime = 12f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Heal, 30f)
                {
                    ScalesWith = "magic",
                    ScalingRatio = 1.0f
                });
            });
            
            CreateSkill("skill_blessing", "축복", SkillType.Active, JobClass.Priest, skill =>
            {
                skill.Description = "아군에게 재생 효과를 부여합니다.";
                skill.TargetType = SkillTargetType.SingleAlly;
                skill.ManaCost = 40;
                skill.CooldownTime = 8f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.StatusEffect, 10f)
                {
                    StatusEffect = StatusEffectType.Regeneration,
                    Duration = 10f,
                    ScalesWith = "magic",
                    ScalingRatio = 0.5f
                });
            });
            
            // Ranger Skills
            CreateSkill("skill_precise_shot", "정밀 사격", SkillType.Active, JobClass.Ranger, skill =>
            {
                skill.Description = "높은 정확도로 치명타 피해를 입힙니다.";
                skill.TargetType = SkillTargetType.SingleEnemy;
                skill.ManaCost = 15;
                skill.CooldownTime = 4f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Damage, 60f)
                {
                    ScalesWith = "attack",
                    ScalingRatio = 2.0f
                });
            });
            
            CreateSkill("skill_trap", "함정 설치", SkillType.Active, JobClass.Ranger, skill =>
            {
                skill.Description = "적을 기절시키는 함정을 설치합니다.";
                skill.TargetType = SkillTargetType.Area;
                skill.AreaRadius = 1;
                skill.ManaCost = 30;
                skill.CooldownTime = 10f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.StatusEffect, 0f)
                {
                    StatusEffect = StatusEffectType.Stun,
                    Duration = 3f,
                    ProcChance = 0.7f
                });
            });
            
            // Assassin Skills
            CreateSkill("skill_stealth", "은신", SkillType.Active, JobClass.Assassin, skill =>
            {
                skill.Description = "잠시 무적 상태가 되며 다음 공격의 피해가 증가합니다.";
                skill.TargetType = SkillTargetType.Self;
                skill.ManaCost = 20;
                skill.CooldownTime = 15f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.StatusEffect, 0f)
                {
                    StatusEffect = StatusEffectType.Invulnerable,
                    Duration = 2f
                });
                skill.Effects.Add(new SkillEffect(SkillEffectType.Buff, 0.5f)
                {
                    Duration = 5f
                });
            });
            
            CreateSkill("skill_poison_blade", "독칼", SkillType.Active, JobClass.Assassin, skill =>
            {
                skill.Description = "적에게 피해를 입히고 중독시킵니다.";
                skill.TargetType = SkillTargetType.SingleEnemy;
                skill.ManaCost = 25;
                skill.CooldownTime = 6f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Damage, 40f)
                {
                    ScalesWith = "attack",
                    ScalingRatio = 1.3f
                });
                skill.Effects.Add(new SkillEffect(SkillEffectType.StatusEffect, 8f)
                {
                    StatusEffect = StatusEffectType.Poison,
                    Duration = 8f,
                    ProcChance = 0.8f
                });
            });
            
            // Knight Skills
            CreateSkill("skill_shield_bash", "방패 강타", SkillType.Active, JobClass.Knight, skill =>
            {
                skill.Description = "적을 기절시키고 피해를 입힙니다.";
                skill.TargetType = SkillTargetType.SingleEnemy;
                skill.ManaCost = 30;
                skill.CooldownTime = 8f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Damage, 30f)
                {
                    ScalesWith = "defense",
                    ScalingRatio = 1.5f
                });
                skill.Effects.Add(new SkillEffect(SkillEffectType.StatusEffect, 0f)
                {
                    StatusEffect = StatusEffectType.Stun,
                    Duration = 2f,
                    ProcChance = 0.6f
                });
            });
            
            CreateSkill("skill_divine_shield", "신성한 방패", SkillType.Active, JobClass.Knight, skill =>
            {
                skill.Description = "아군 전체에게 보호막을 부여합니다.";
                skill.TargetType = SkillTargetType.AllAllies;
                skill.ManaCost = 50;
                skill.CooldownTime = 20f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Shield, 100f)
                {
                    ScalesWith = "defense",
                    ScalingRatio = 2.0f
                });
            });
            
            // Sage Skills
            CreateSkill("skill_arcane_wisdom", "비전의 지혜", SkillType.Passive, JobClass.Sage, skill =>
            {
                skill.Description = "마나 재생과 스킬 피해가 증가합니다.";
                skill.TargetType = SkillTargetType.Self;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Buff, 0.2f));
            });
            
            CreateSkill("skill_time_warp", "시간 왜곡", SkillType.Ultimate, JobClass.Sage, skill =>
            {
                skill.Description = "적 전체를 침묵시키고 아군의 쿨다운을 감소시킵니다.";
                skill.TargetType = SkillTargetType.AllEnemies;
                skill.ManaCost = 100;
                skill.CooldownTime = 60f;
                skill.UsesPerBattle = 1;
                skill.RequiredLevel = 20;
                skill.Effects.Add(new SkillEffect(SkillEffectType.StatusEffect, 0f)
                {
                    StatusEffect = StatusEffectType.Silence,
                    Duration = 5f
                });
                skill.Effects.Add(new SkillEffect(SkillEffectType.Special, 0.5f)); // Cooldown reduction
            });
        }
        
        void CreateSkill(string id, string name, SkillType type, JobClass jobClass, Action<Skill> configure)
        {
            var skill = new Skill(name, type, jobClass)
            {
                SkillId = id
            };
            
            configure(skill);
            
            allSkills[id] = skill;
            
            string classKey = jobClass.ToString();
            if (!classSkills.ContainsKey(classKey))
            {
                classSkills[classKey] = new List<string>();
            }
            classSkills[classKey].Add(id);
        }
        
        public bool LearnSkill(UnitStatus unit, string skillId)
        {
            if (!allSkills.ContainsKey(skillId)) return false;
            
            var skill = allSkills[skillId];
            
            // Check if unit can learn the skill
            if (skill.RequiredClass != JobClass.None && unit.JobClass != skill.RequiredClass)
                return false;
                
            if (unit.Level < skill.RequiredLevel)
                return false;
            
            string unitId = unit.UnitId;
            if (!unitSkills.ContainsKey(unitId))
            {
                unitSkills[unitId] = new List<string>();
            }
            
            if (!unitSkills[unitId].Contains(skillId))
            {
                unitSkills[unitId].Add(skillId);
                OnSkillLearned?.Invoke(unit, skill);
                return true;
            }
            
            return false;
        }
        
        public bool UseSkill(UnitStatus caster, string skillId, List<UnitStatus> targets)
        {
            if (!allSkills.ContainsKey(skillId)) return false;
            
            var skill = allSkills[skillId];
            
            if (!skill.CanUse(caster)) return false;
            
            // Consume resources
            caster.currentMP -= skill.ManaCost;
            caster.currentHP -= skill.HealthCost;
            
            // Set cooldown
            skill.CurrentCooldown = skill.CooldownTime;
            
            // Use charge if limited
            if (skill.UsesPerBattle > 0)
            {
                skill.UsesRemaining--;
            }
            
            // Apply effects
            foreach (var effect in skill.Effects)
            {
                if (UnityEngine.Random.value <= effect.ProcChance)
                {
                    ApplySkillEffect(caster, skill, effect, targets);
                }
            }
            
            OnSkillUsed?.Invoke(caster, skill, targets);
            
            return true;
        }
        
        void ApplySkillEffect(UnitStatus caster, Skill skill, SkillEffect effect, List<UnitStatus> targets)
        {
            foreach (var target in targets)
            {
                float value = skill.CalculateEffectValue(caster, effect);
                
                switch (effect.Type)
                {
                    case SkillEffectType.Damage:
                        target.TakeDamage(value);
                        break;
                        
                    case SkillEffectType.Heal:
                        target.Heal(value);
                        break;
                        
                    case SkillEffectType.Buff:
                        ApplyBuff(target, effect, value);
                        break;
                        
                    case SkillEffectType.Debuff:
                        ApplyDebuff(target, effect, value);
                        break;
                        
                    case SkillEffectType.Shield:
                        target.AddShield(value);
                        break;
                        
                    case SkillEffectType.StatusEffect:
                        if (effect.StatusEffect.HasValue)
                        {
                            ApplyStatusEffect(target, effect.StatusEffect.Value, effect.Duration, value, caster, skill);
                        }
                        break;
                        
                    case SkillEffectType.Special:
                        HandleSpecialEffect(caster, target, skill, value);
                        break;
                }
            }
        }
        
        void ApplyBuff(UnitStatus target, SkillEffect effect, float value)
        {
            // TODO: Implement buff system
            // For now, directly modify stats
            target.attackPower *= (1f + value);
            target.defense *= (1f + value);
        }
        
        void ApplyDebuff(UnitStatus target, SkillEffect effect, float value)
        {
            // TODO: Implement debuff system
            // For now, directly modify stats
            target.attackPower *= (1f - value);
            target.defense *= (1f - value);
        }
        
        void ApplyStatusEffect(UnitStatus target, StatusEffectType type, float duration, float value, UnitStatus source, Skill sourceSkill)
        {
            var statusEffect = new StatusEffect(type, duration, value)
            {
                Source = source,
                SourceSkill = sourceSkill
            };
            
            string targetId = target.UnitId;
            if (!activeStatusEffects.ContainsKey(targetId))
            {
                activeStatusEffects[targetId] = new List<StatusEffect>();
            }
            
            activeStatusEffects[targetId].Add(statusEffect);
            OnStatusEffectApplied?.Invoke(target, statusEffect);
        }
        
        void HandleSpecialEffect(UnitStatus caster, UnitStatus target, Skill skill, float value)
        {
            // Handle special effects like cooldown reduction
            if (skill.SkillId == "skill_time_warp")
            {
                // Reduce all cooldowns for allies
                var alliedUnits = GetAlliedUnits(caster);
                foreach (var ally in alliedUnits)
                {
                    var allySkills = GetUnitSkills(ally);
                    foreach (var allySkill in allySkills)
                    {
                        allySkill.CurrentCooldown *= (1f - value);
                    }
                }
            }
        }
        
        public void UpdateSkillCooldowns(float deltaTime)
        {
            foreach (var skill in allSkills.Values)
            {
                if (skill.CurrentCooldown > 0)
                {
                    skill.CurrentCooldown -= deltaTime;
                    if (skill.CurrentCooldown < 0)
                        skill.CurrentCooldown = 0;
                }
            }
        }
        
        public void UpdateStatusEffects(float deltaTime)
        {
            foreach (var kvp in activeStatusEffects.ToList())
            {
                var unitId = kvp.Key;
                var effects = kvp.Value;
                var unit = GetUnitById(unitId);
                
                if (unit == null) continue;
                
                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    var effect = effects[i];
                    effect.UpdateDuration(deltaTime);
                    
                    // Apply tick effects
                    if (effect.TickInterval > 0)
                    {
                        ApplyTickEffect(unit, effect);
                    }
                    
                    if (effect.IsExpired)
                    {
                        effects.RemoveAt(i);
                        OnStatusEffectRemoved?.Invoke(unit, effect);
                    }
                }
            }
        }
        
        void ApplyTickEffect(UnitStatus unit, StatusEffect effect)
        {
            switch (effect.Type)
            {
                case StatusEffectType.Poison:
                case StatusEffectType.Burn:
                case StatusEffectType.Bleed:
                    unit.TakeDamage(effect.Value);
                    break;
                    
                case StatusEffectType.Regeneration:
                    unit.Heal(effect.Value);
                    break;
            }
        }
        
        public void ResetBattleSkills()
        {
            // Reset cooldowns and uses for new battle
            foreach (var skill in allSkills.Values)
            {
                skill.CurrentCooldown = 0;
                skill.UsesRemaining = skill.UsesPerBattle;
            }
            
            // Clear all status effects
            activeStatusEffects.Clear();
        }
        
        public bool UpgradeSkill(UnitStatus unit, string skillId)
        {
            if (!allSkills.ContainsKey(skillId)) return false;
            if (!HasSkill(unit, skillId)) return false;
            
            var skill = allSkills[skillId];
            skill.CurrentLevel++;
            
            OnSkillUpgraded?.Invoke(unit, skill);
            
            return true;
        }
        
        // Helper methods
        public List<Skill> GetUnitSkills(UnitStatus unit)
        {
            string unitId = unit.UnitId;
            if (!unitSkills.ContainsKey(unitId))
                return new List<Skill>();
                
            return unitSkills[unitId]
                .Select(id => allSkills[id])
                .ToList();
        }
        
        public List<Skill> GetClassSkills(JobClass jobClass)
        {
            string classKey = jobClass.ToString();
            if (!classSkills.ContainsKey(classKey))
                return new List<Skill>();
                
            return classSkills[classKey]
                .Select(id => allSkills[id])
                .ToList();
        }
        
        public bool HasSkill(UnitStatus unit, string skillId)
        {
            string unitId = unit.UnitId;
            return unitSkills.ContainsKey(unitId) && unitSkills[unitId].Contains(skillId);
        }
        
        public List<StatusEffect> GetUnitStatusEffects(UnitStatus unit)
        {
            string unitId = unit.UnitId;
            if (!activeStatusEffects.ContainsKey(unitId))
                return new List<StatusEffect>();
                
            return new List<StatusEffect>(activeStatusEffects[unitId]);
        }
        
        public bool HasStatusEffect(UnitStatus unit, StatusEffectType type)
        {
            return GetUnitStatusEffects(unit).Any(e => e.Type == type);
        }
        
        UnitStatus GetUnitById(string unitId)
        {
            // TODO: Implement unit lookup
            var guildManager = Core.GameManager.Instance?.GuildManager;
            if (guildManager != null)
            {
                return guildManager.GetAdventurers().FirstOrDefault(u => u.UnitId == unitId);
            }
            return null;
        }
        
        List<UnitStatus> GetAlliedUnits(UnitStatus unit)
        {
            // TODO: Get all allied units in battle
            return new List<UnitStatus>();
        }
        
        public Skill GetSkill(string skillId)
        {
            return allSkills.ContainsKey(skillId) ? allSkills[skillId] : null;
        }
        
        // Skill template for data-driven approach
        class SkillTemplate
        {
            public string TemplateId { get; set; }
            public string BaseName { get; set; }
            public SkillType Type { get; set; }
            public List<SkillEffect> BaseEffects { get; set; }
        }
    }
}