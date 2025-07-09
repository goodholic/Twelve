using UnityEngine;
using System.Collections.Generic;
using GuildMaster.Battle; // JobClass를 위해 추가
using GuildMaster.Data;

namespace GuildMaster.Battle
{
    [CreateAssetMenu(fileName = "AIBehavior", menuName = "GuildMaster/AI/Behavior")]
    public class AIBehaviorSO : ScriptableObject
    {
        [Header("AI Identity")]
        public string behaviorName;
        public AIType aiType;
        
        [Header("Combat Preferences")]
        [Range(0f, 1f)] public float aggressiveness = 0.5f;
        [Range(0f, 1f)] public float defensiveness = 0.5f;
        [Range(0f, 1f)] public float supportiveness = 0.5f;
        
        [Header("Target Priority")]
        public List<TargetPriority> targetPriorities = new List<TargetPriority>();
        
        [Header("Skill Usage")]
        public List<SkillUsageRule> skillRules = new List<SkillUsageRule>();
        
        [Header("Movement Behavior")]
        public MovementStrategy movementStrategy;
        [Range(0f, 1f)] public float retreatThreshold = 0.3f;
        
        [Header("Team Behavior")]
        public bool prioritizeHealing = true;
        public bool protectAllies = true;
        public float allyProtectionRange = 3f;
        
        public enum AIType
        {
            Aggressive,
            Defensive,
            Balanced,
            Support,
            Strategic
        }
        
        public enum MovementStrategy
        {
            RushToTarget,
            MaintainDistance,
            FlankTarget,
            ProtectAllies,
            Strategic
        }
        
        [System.Serializable]
        public class TargetPriority
        {
            public TargetCondition condition;
            public float weight = 1f;
            
            public enum TargetCondition
            {
                LowestHP,
                HighestHP,
                LowestDefense,
                HighestAttack,
                IsHealer,
                IsDamageDealer,
                ClosestTarget,
                FarthestTarget,
                MostBuffed,
                MostDebuffed
            }
        }
        
        [System.Serializable]
        public class SkillUsageRule
        {
            public string skillId;
            public SkillCondition condition;
            public float threshold = 0.5f;
            public int priority = 1;
            
            public enum SkillCondition
            {
                AlwaysUse,
                TargetHPBelow,
                SelfHPBelow,
                AllyHPBelow,
                EnemyCount,
                AllyCount,
                HasDebuff,
                HasBuff,
                ManaCostEfficient
            }
        }
        
        public float EvaluateTarget(CharacterUnit unit, CharacterUnit target, BattleContext context)
        {
            float score = 0f;
            
            foreach (var priority in targetPriorities)
            {
                float conditionScore = EvaluateTargetCondition(unit, target, priority.condition, context);
                score += conditionScore * priority.weight;
            }
            
            return score;
        }
        
        float EvaluateTargetCondition(CharacterUnit unit, CharacterUnit target, TargetPriority.TargetCondition condition, BattleContext context)
        {
            switch (condition)
            {
                case TargetPriority.TargetCondition.LowestHP:
                    return 1f - (target.currentHP / (float)target.maxHP);
                    
                case TargetPriority.TargetCondition.HighestHP:
                    return target.currentHP / (float)target.maxHP;
                    
                case TargetPriority.TargetCondition.LowestDefense:
                    return 1f - (target.defense / 100f);
                    
                case TargetPriority.TargetCondition.HighestAttack:
                    return target.attackPower / 100f;
                    
                case TargetPriority.TargetCondition.IsHealer:
                    return IsHealer(target.jobClass) ? 1f : 0f;
                    
                case TargetPriority.TargetCondition.IsDamageDealer:
                    return IsDamageDealer(target.jobClass) ? 1f : 0f;
                    
                case TargetPriority.TargetCondition.ClosestTarget:
                    float maxDistance = 10f;
                    float distance = Vector3.Distance(unit.transform.position, target.transform.position);
                    return 1f - (distance / maxDistance);
                    
                case TargetPriority.TargetCondition.FarthestTarget:
                    float maxDist = 10f;
                    float dist = Vector3.Distance(unit.transform.position, target.transform.position);
                    return dist / maxDist;
                    
                case TargetPriority.TargetCondition.MostBuffed:
                    return target.activeBuffs.Count / 5f;
                    
                case TargetPriority.TargetCondition.MostDebuffed:
                    return target.activeDebuffs.Count / 5f;
                    
                default:
                    return 0.5f;
            }
        }
        
        public bool ShouldUseSkill(string skillId, CharacterUnit unit, BattleContext context)
        {
            var rule = skillRules.Find(r => r.skillId == skillId);
            if (rule == null) return false;
            
            return EvaluateSkillCondition(rule.condition, rule.threshold, unit, context);
        }
        
        bool EvaluateSkillCondition(SkillUsageRule.SkillCondition condition, float threshold, CharacterUnit unit, BattleContext context)
        {
            switch (condition)
            {
                case SkillUsageRule.SkillCondition.AlwaysUse:
                    return true;
                    
                case SkillUsageRule.SkillCondition.TargetHPBelow:
                    if (context.currentTarget != null)
                        return (context.currentTarget.currentHP / (float)context.currentTarget.maxHP) < threshold;
                    return false;
                    
                case SkillUsageRule.SkillCondition.SelfHPBelow:
                    return (unit.currentHP / (float)unit.maxHP) < threshold;
                    
                case SkillUsageRule.SkillCondition.AllyHPBelow:
                    return context.allies.Exists(ally => (ally.currentHP / (float)ally.maxHP) < threshold);
                    
                case SkillUsageRule.SkillCondition.EnemyCount:
                    return context.enemies.Count >= threshold;
                    
                case SkillUsageRule.SkillCondition.AllyCount:
                    return context.allies.Count >= threshold;
                    
                case SkillUsageRule.SkillCondition.HasDebuff:
                    return unit.activeDebuffs.Count > 0;
                    
                case SkillUsageRule.SkillCondition.HasBuff:
                    return unit.activeBuffs.Count > 0;
                    
                case SkillUsageRule.SkillCondition.ManaCostEfficient:
                    // Implement mana cost efficiency logic
                    return true;
                    
                default:
                    return false;
            }
        }
        
        public Vector3 GetMovementPosition(CharacterUnit unit, BattleContext context)
        {
            switch (movementStrategy)
            {
                case MovementStrategy.RushToTarget:
                    if (context.currentTarget != null)
                        return context.currentTarget.transform.position;
                    break;
                    
                case MovementStrategy.MaintainDistance:
                    if (context.currentTarget != null)
                    {
                        Vector3 direction = unit.transform.position - context.currentTarget.transform.position;
                        return unit.transform.position + direction.normalized * 2f;
                    }
                    break;
                    
                case MovementStrategy.FlankTarget:
                    if (context.currentTarget != null)
                    {
                        Vector3 toTarget = context.currentTarget.transform.position - unit.transform.position;
                        Vector3 flank = Quaternion.Euler(0, 90, 0) * toTarget.normalized * 3f;
                        return context.currentTarget.transform.position + flank;
                    }
                    break;
                    
                case MovementStrategy.ProtectAllies:
                    CharacterUnit weakestAlly = GetWeakestAlly(context.allies);
                    if (weakestAlly != null)
                        return weakestAlly.transform.position;
                    break;
                    
                case MovementStrategy.Strategic:
                    return GetStrategicPosition(unit, context);
            }
            
            return unit.transform.position;
        }
        
        CharacterUnit GetWeakestAlly(List<CharacterUnit> allies)
        {
            CharacterUnit weakest = null;
            float lowestHPRatio = 1f;
            
            foreach (var ally in allies)
            {
                float hpRatio = ally.currentHP / (float)ally.maxHP;
                if (hpRatio < lowestHPRatio)
                {
                    lowestHPRatio = hpRatio;
                    weakest = ally;
                }
            }
            
            return weakest;
        }
        
        Vector3 GetStrategicPosition(CharacterUnit unit, BattleContext context)
        {
            // Complex strategic positioning logic
            // For now, return a position between allies and enemies
            Vector3 allyCenter = GetTeamCenter(context.allies);
            Vector3 enemyCenter = GetTeamCenter(context.enemies);
            
            return Vector3.Lerp(allyCenter, enemyCenter, 0.6f);
        }
        
        Vector3 GetTeamCenter(List<CharacterUnit> units)
        {
            if (units.Count == 0) return Vector3.zero;
            
            Vector3 sum = Vector3.zero;
            foreach (var unit in units)
            {
                sum += unit.transform.position;
            }
            
            return sum / units.Count;
        }
        
        bool IsHealer(JobClass jobClass)
        {
            return jobClass == JobClass.Priest || jobClass == JobClass.Sage;
        }
        
        bool IsDamageDealer(JobClass jobClass)
        {
            return jobClass == JobClass.Warrior || 
                   jobClass == JobClass.Assassin || 
                   jobClass == JobClass.Mage ||
                   jobClass == JobClass.Ranger;
        }
        
        public string GetBehaviorDescription()
        {
            string description = $"AI Behavior: {behaviorName}\n";
            description += $"Type: {aiType}\n";
            description += $"Aggressiveness: {aggressiveness:P0}\n";
            description += $"Defensiveness: {defensiveness:P0}\n";
            description += $"Supportiveness: {supportiveness:P0}\n";
            description += $"Movement Strategy: {movementStrategy}\n";
            
            return description;
        }
    }
    
    [System.Serializable]
    public class BattleContext
    {
        public List<CharacterUnit> allies = new List<CharacterUnit>();
        public List<CharacterUnit> enemies = new List<CharacterUnit>();
        public CharacterUnit currentTarget;
        public float battleTime;
        public int turnNumber;
    }
}