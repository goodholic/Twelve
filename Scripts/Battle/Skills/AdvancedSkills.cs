using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuildMaster.Battle
{
    public class AdvancedSkills : MonoBehaviour
    {
        private SkillManager skillManager;
        
        void Start()
        {
            skillManager = GetComponent<SkillManager>();
            if (skillManager != null)
            {
                InitializeAdvancedSkills();
            }
        }
        
        void InitializeAdvancedSkills()
        {
            // Warrior Advanced Skills
            CreateAdvancedSkill("skill_berserker_rage", "광전사의 분노", SkillType.Ultimate, JobClass.Warrior, skill =>
            {
                skill.Description = "체력이 낮을수록 강해지며, 모든 공격이 치명타로 변환됩니다.";
                skill.TargetType = SkillTargetType.Self;
                skill.RequiredLevel = 30;
                skill.ManaCost = 80;
                skill.CooldownTime = 60f;
                skill.UsesPerBattle = 1;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Buff, 1.0f) // 100% crit rate
                {
                    Duration = 10f
                });
                skill.Effects.Add(new SkillEffect(SkillEffectType.Special, 2.0f) // Berserker mode
                {
                    Duration = 10f
                });
            });
            
            CreateAdvancedSkill("skill_sword_dance", "검무", SkillType.Active, JobClass.Warrior, skill =>
            {
                skill.Description = "연속으로 5회 공격하며 각 타격마다 피해가 증가합니다.";
                skill.TargetType = SkillTargetType.SingleEnemy;
                skill.RequiredLevel = 25;
                skill.ManaCost = 60;
                skill.CooldownTime = 20f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Special, 5f) // 5 hits
                {
                    ScalesWith = "attack",
                    ScalingRatio = 0.8f
                });
            });
            
            // Knight Advanced Skills
            CreateAdvancedSkill("skill_fortress", "불굴의 요새", SkillType.Ultimate, JobClass.Knight, skill =>
            {
                skill.Description = "10초간 모든 아군의 피해를 대신 받으며, 받는 피해가 50% 감소합니다.";
                skill.TargetType = SkillTargetType.Self;
                skill.RequiredLevel = 30;
                skill.ManaCost = 100;
                skill.CooldownTime = 90f;
                skill.UsesPerBattle = 1;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Special, 0.5f) // Damage reduction
                {
                    Duration = 10f
                });
            });
            
            CreateAdvancedSkill("skill_holy_ground", "신성한 땅", SkillType.Active, JobClass.Knight, skill =>
            {
                skill.Description = "주변 지역을 신성화하여 아군은 지속 치유, 적은 지속 피해를 받습니다.";
                skill.TargetType = SkillTargetType.Area;
                skill.AreaRadius = 3;
                skill.RequiredLevel = 20;
                skill.ManaCost = 70;
                skill.CooldownTime = 30f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Special, 20f) // Holy ground effect
                {
                    Duration = 15f,
                    ScalesWith = "defense",
                    ScalingRatio = 0.5f
                });
            });
            
            // Mage Advanced Skills
            CreateAdvancedSkill("skill_arcane_orb", "비전 구체", SkillType.Active, JobClass.Mage, skill =>
            {
                skill.Description = "추적하는 구체를 발사하여 적중 시 주변에 폭발을 일으킵니다.";
                skill.TargetType = SkillTargetType.SingleEnemy;
                skill.RequiredLevel = 15;
                skill.ManaCost = 50;
                skill.CooldownTime = 8f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Damage, 80f)
                {
                    ScalesWith = "magic",
                    ScalingRatio = 2.0f
                });
                skill.Effects.Add(new SkillEffect(SkillEffectType.Special, 3f) // Explosion radius
                {
                    ScalesWith = "magic",
                    ScalingRatio = 0.5f
                });
            });
            
            CreateAdvancedSkill("skill_elemental_convergence", "원소의 수렴", SkillType.Ultimate, JobClass.Mage, skill =>
            {
                skill.Description = "모든 원소의 힘을 모아 거대한 폭발을 일으킵니다. 각 원소별 상태이상 부여.";
                skill.TargetType = SkillTargetType.AllEnemies;
                skill.RequiredLevel = 35;
                skill.ManaCost = 150;
                skill.CooldownTime = 120f;
                skill.UsesPerBattle = 1;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Damage, 150f)
                {
                    ScalesWith = "magic",
                    ScalingRatio = 3.0f
                });
                // Fire - Burn
                skill.Effects.Add(new SkillEffect(SkillEffectType.StatusEffect, 10f)
                {
                    StatusEffect = StatusEffectType.Burn,
                    Duration = 8f,
                    ProcChance = 0.9f
                });
                // Ice - Freeze
                skill.Effects.Add(new SkillEffect(SkillEffectType.StatusEffect, 0f)
                {
                    StatusEffect = StatusEffectType.Freeze,
                    Duration = 3f,
                    ProcChance = 0.7f
                });
                // Lightning - Stun
                skill.Effects.Add(new SkillEffect(SkillEffectType.StatusEffect, 0f)
                {
                    StatusEffect = StatusEffectType.Stun,
                    Duration = 2f,
                    ProcChance = 0.5f
                });
            });
            
            // Priest Advanced Skills
            CreateAdvancedSkill("skill_resurrection", "부활", SkillType.Ultimate, JobClass.Priest, skill =>
            {
                skill.Description = "죽은 아군을 모두 부활시키고 체력을 50% 회복시킵니다.";
                skill.TargetType = SkillTargetType.AllAllies;
                skill.RequiredLevel = 40;
                skill.ManaCost = 200;
                skill.CooldownTime = 180f;
                skill.UsesPerBattle = 1;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Special, 0.5f)); // Resurrection with 50% HP
            });
            
            CreateAdvancedSkill("skill_sanctuary", "성역", SkillType.Active, JobClass.Priest, skill =>
            {
                skill.Description = "지정 지역에 성역을 생성하여 아군은 무적, 적은 침묵 상태가 됩니다.";
                skill.TargetType = SkillTargetType.Area;
                skill.AreaRadius = 2;
                skill.RequiredLevel = 25;
                skill.ManaCost = 80;
                skill.CooldownTime = 45f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.StatusEffect, 0f)
                {
                    StatusEffect = StatusEffectType.Invulnerable,
                    Duration = 3f,
                    ProcChance = 1f
                });
                skill.Effects.Add(new SkillEffect(SkillEffectType.StatusEffect, 0f)
                {
                    StatusEffect = StatusEffectType.Silence,
                    Duration = 5f,
                    ProcChance = 1f
                });
            });
            
            // Assassin Advanced Skills
            CreateAdvancedSkill("skill_shadow_clone", "그림자 분신", SkillType.Ultimate, JobClass.Assassin, skill =>
            {
                skill.Description = "자신의 분신을 3개 생성하여 함께 공격합니다.";
                skill.TargetType = SkillTargetType.Self;
                skill.RequiredLevel = 35;
                skill.ManaCost = 100;
                skill.CooldownTime = 90f;
                skill.UsesPerBattle = 1;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Summon, 3f) // 3 clones
                {
                    Duration = 20f,
                    ScalesWith = "attack",
                    ScalingRatio = 0.7f
                });
            });
            
            CreateAdvancedSkill("skill_assassination", "암살", SkillType.Active, JobClass.Assassin, skill =>
            {
                skill.Description = "적의 약점을 노려 즉사 확률이 있는 치명적인 일격을 가합니다.";
                skill.TargetType = SkillTargetType.SingleEnemy;
                skill.RequiredLevel = 30;
                skill.ManaCost = 80;
                skill.CooldownTime = 60f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Damage, 200f)
                {
                    ScalesWith = "attack",
                    ScalingRatio = 3.0f
                });
                skill.Effects.Add(new SkillEffect(SkillEffectType.Special, 0.15f) // 15% instant kill chance
                {
                    ProcChance = 0.15f
                });
            });
            
            // Ranger Advanced Skills
            CreateAdvancedSkill("skill_rain_of_arrows", "화살비", SkillType.Ultimate, JobClass.Ranger, skill =>
            {
                skill.Description = "하늘에서 무수한 화살이 쏟아져 전장을 뒤덮습니다.";
                skill.TargetType = SkillTargetType.AllEnemies;
                skill.RequiredLevel = 35;
                skill.ManaCost = 120;
                skill.CooldownTime = 100f;
                skill.UsesPerBattle = 1;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Special, 10f) // 10 waves of arrows
                {
                    ScalesWith = "attack",
                    ScalingRatio = 0.6f
                });
            });
            
            CreateAdvancedSkill("skill_hunters_mark", "사냥꾼의 표식", SkillType.Active, JobClass.Ranger, skill =>
            {
                skill.Description = "적에게 표식을 남겨 모든 아군의 해당 적에 대한 피해가 증가합니다.";
                skill.TargetType = SkillTargetType.SingleEnemy;
                skill.RequiredLevel = 20;
                skill.ManaCost = 40;
                skill.CooldownTime = 15f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Debuff, 0.5f) // 50% damage increase
                {
                    Duration = 10f
                });
            });
            
            // Sage Advanced Skills
            CreateAdvancedSkill("skill_enlightenment", "깨달음", SkillType.Ultimate, JobClass.Sage, skill =>
            {
                skill.Description = "모든 아군의 스킬 쿨다운을 초기화하고 마나를 완전 회복시킵니다.";
                skill.TargetType = SkillTargetType.AllAllies;
                skill.RequiredLevel = 40;
                skill.ManaCost = 0; // No mana cost
                skill.CooldownTime = 180f;
                skill.UsesPerBattle = 1;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Special, 1f)); // Full cooldown reset
                skill.Effects.Add(new SkillEffect(SkillEffectType.Special, 999f)); // Full mana restore
            });
            
            CreateAdvancedSkill("skill_polymorph", "변이술", SkillType.Active, JobClass.Sage, skill =>
            {
                skill.Description = "적을 무해한 생물로 변환시켜 모든 능력을 봉인합니다.";
                skill.TargetType = SkillTargetType.SingleEnemy;
                skill.RequiredLevel = 25;
                skill.ManaCost = 60;
                skill.CooldownTime = 30f;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Special, 0f) // Polymorph
                {
                    Duration = 5f,
                    ProcChance = 0.8f
                });
            });
            
            // Awakening Skills (Level 50+ with Awakening)
            CreateAwakeningSkill("skill_god_slash", "신검", JobClass.Warrior, skill =>
            {
                skill.Description = "[각성] 신의 힘을 담은 일격으로 전방의 모든 것을 베어냅니다.";
                skill.TargetType = SkillTargetType.Column;
                skill.RequiredLevel = 50;
                skill.ManaCost = 150;
                skill.CooldownTime = 300f;
                skill.UsesPerBattle = 1;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Damage, 500f)
                {
                    ScalesWith = "attack",
                    ScalingRatio = 5.0f
                });
            });
            
            CreateAwakeningSkill("skill_eternal_guardian", "영원의 수호자", JobClass.Knight, skill =>
            {
                skill.Description = "[각성] 불멸의 수호자가 되어 아군을 지킵니다.";
                skill.TargetType = SkillTargetType.Self;
                skill.RequiredLevel = 50;
                skill.ManaCost = 200;
                skill.CooldownTime = 300f;
                skill.UsesPerBattle = 1;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Special, 0.9f) // 90% damage reduction
                {
                    Duration = 30f
                });
            });
            
            CreateAwakeningSkill("skill_apocalypse", "아포칼립스", JobClass.Mage, skill =>
            {
                skill.Description = "[각성] 세계를 멸망시킬 수 있는 궁극의 마법을 시전합니다.";
                skill.TargetType = SkillTargetType.AllEnemies;
                skill.RequiredLevel = 50;
                skill.ManaCost = 300;
                skill.CooldownTime = 300f;
                skill.UsesPerBattle = 1;
                skill.Effects.Add(new SkillEffect(SkillEffectType.Damage, 1000f)
                {
                    ScalesWith = "magic",
                    ScalingRatio = 6.0f
                });
            });
        }
        
        void CreateAdvancedSkill(string id, string name, SkillType type, JobClass jobClass, Action<Skill> configure)
        {
            // This method would interface with the SkillManager to add skills
            // Implementation depends on how SkillManager exposes its creation methods
        }
        
        void CreateAwakeningSkill(string id, string name, JobClass jobClass, Action<Skill> configure)
        {
            // Awakening skills require awakening level > 0
            CreateAdvancedSkill(id, name, SkillType.Ultimate, jobClass, skill =>
            {
                configure(skill);
                // Add awakening requirement
                skill.RequiredLevel = 50;
            });
        }
    }
    
    // Skill Combo System
    public class SkillComboSystem
    {
        public class SkillCombo
        {
            public string ComboId { get; set; }
            public string Name { get; set; }
            public List<string> RequiredSkills { get; set; } // Skills that must be used in sequence
            public float TimeWindow { get; set; } // Time to complete combo
            public SkillEffect BonusEffect { get; set; } // Bonus when combo is completed
        }
        
        private Dictionary<string, SkillCombo> combos = new Dictionary<string, SkillCombo>();
        private Dictionary<string, List<string>> activeComboProgress = new Dictionary<string, List<string>>();
        
        public void InitializeCombos()
        {
            // Warrior combos
            combos["combo_blade_dance"] = new SkillCombo
            {
                ComboId = "combo_blade_dance",
                Name = "검무 연계",
                RequiredSkills = new List<string> { "skill_slash", "skill_whirlwind", "skill_sword_dance" },
                TimeWindow = 10f,
                BonusEffect = new SkillEffect(SkillEffectType.Damage, 200f)
                {
                    ScalesWith = "attack",
                    ScalingRatio = 3.0f
                }
            };
            
            // Mage combos
            combos["combo_elemental_chain"] = new SkillCombo
            {
                ComboId = "combo_elemental_chain",
                Name = "원소 연쇄",
                RequiredSkills = new List<string> { "skill_fireball", "skill_blizzard", "skill_arcane_orb" },
                TimeWindow = 8f,
                BonusEffect = new SkillEffect(SkillEffectType.Special, 1f) // Elemental explosion
            };
        }
        
        public bool CheckCombo(string unitId, string skillId)
        {
            if (!activeComboProgress.ContainsKey(unitId))
            {
                activeComboProgress[unitId] = new List<string>();
            }
            
            activeComboProgress[unitId].Add(skillId);
            
            // Check if any combo is completed
            foreach (var combo in combos.Values)
            {
                if (IsComboCompleted(activeComboProgress[unitId], combo.RequiredSkills))
                {
                    // Combo completed!
                    activeComboProgress[unitId].Clear();
                    return true;
                }
            }
            
            // Clean up old skills outside time window
            // TODO: Implement time-based cleanup
            
            return false;
        }
        
        bool IsComboCompleted(List<string> progress, List<string> required)
        {
            if (progress.Count < required.Count) return false;
            
            // Check if the last N skills match the required combo
            var recentSkills = progress.Skip(progress.Count - required.Count).ToList();
            return recentSkills.SequenceEqual(required);
        }
    }
}