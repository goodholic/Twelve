using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle; // Unit을 위해 추가

namespace GuildMaster.Guild
{
    public class AdventurerSkillManager : MonoBehaviour
    {
        // Skill learning configuration
        private const int SKILLS_PER_CLASS = 3;
        private const int SKILL_POINTS_PER_LEVEL = 1;
        private const int SKILL_UNLOCK_LEVELS = 5; // Every 5 levels, unlock a new skill slot
        
        // Adventurer skill data
        private Dictionary<string, AdventurerSkillData> adventurerSkills; // UnitId -> SkillData
        
        // Events
        public event Action<Unit, Battle.Skill> OnSkillLearned;
        public event Action<Unit, Battle.Skill> OnSkillUpgraded;
        public event Action<Unit, int> OnSkillPointsChanged;
        
        [System.Serializable]
        public class AdventurerSkillData
        {
            public string UnitId { get; set; }
            public int SkillPoints { get; set; }
            public List<string> LearnedSkillIds { get; set; }
            public Dictionary<string, int> SkillLevels { get; set; } // SkillId -> Level
            public int MaxSkillSlots { get; set; }
            
            public AdventurerSkillData(string unitId)
            {
                UnitId = unitId;
                SkillPoints = 0;
                LearnedSkillIds = new List<string>();
                SkillLevels = new Dictionary<string, int>();
                MaxSkillSlots = 1; // Start with 1 skill slot
            }
        }
        
        void Awake()
        {
            adventurerSkills = new Dictionary<string, AdventurerSkillData>();
        }
        
        public void InitializeAdventurer(Unit unit)
        {
            if (!adventurerSkills.ContainsKey(unit.UnitId))
            {
                var skillData = new AdventurerSkillData(unit.UnitId);
                skillData.MaxSkillSlots = 1 + (unit.Level / SKILL_UNLOCK_LEVELS);
                skillData.SkillPoints = unit.Level * SKILL_POINTS_PER_LEVEL;
                
                adventurerSkills[unit.UnitId] = skillData;
                
                // Auto-learn basic skill for the class
                AutoLearnBasicSkill(unit);
            }
        }
        
        void AutoLearnBasicSkill(Unit unit)
        {
            var skillManager = Core.GameManager.Instance?.SkillManager;
            if (skillManager == null) return;
            
            string basicSkillId = GetBasicSkillForClass(unit.JobClass);
            if (!string.IsNullOrEmpty(basicSkillId))
            {
                LearnSkill(unit, basicSkillId, true); // Free basic skill
            }
        }
        
        string GetBasicSkillForClass(JobClass jobClass)
        {
            switch (jobClass)
            {
                case JobClass.Warrior: return "skill_slash";
                case JobClass.Mage: return "skill_fireball";
                case JobClass.Priest: return "skill_heal";
                case JobClass.Ranger: return "skill_precise_shot";
                case JobClass.Assassin: return "skill_poison_blade";
                case JobClass.Knight: return "skill_shield_bash";
                case JobClass.Sage: return "skill_arcane_wisdom";
                default: return null;
            }
        }
        
        public bool LearnSkill(Unit unit, string skillId, bool isFree = false)
        {
            if (!adventurerSkills.ContainsKey(unit.UnitId))
            {
                InitializeAdventurer(unit);
            }
            
            var skillData = adventurerSkills[unit.UnitId];
            var skillManager = Core.GameManager.Instance?.SkillManager;
            
            if (skillManager == null) return false;
            
            var skill = skillManager.GetSkill(skillId);
            if (skill == null) return false;
            
            // Check if already learned
            if (skillData.LearnedSkillIds.Contains(skillId))
                return false;
            
            // Check skill slot limit
            if (skillData.LearnedSkillIds.Count >= skillData.MaxSkillSlots)
                return false;
            
            // Check class requirement
            if (skill.RequiredClass != JobClass.None && skill.RequiredClass != unit.JobClass)
                return false;
            
            // Check level requirement
            if (unit.Level < skill.RequiredLevel)
                return false;
            
            // Check skill points (unless it's free)
            if (!isFree && skillData.SkillPoints < 1)
                return false;
            
            // Learn the skill
            if (skillManager.LearnSkill(unit, skillId))
            {
                skillData.LearnedSkillIds.Add(skillId);
                skillData.SkillLevels[skillId] = 1;
                
                if (!isFree)
                {
                    skillData.SkillPoints--;
                    OnSkillPointsChanged?.Invoke(unit, skillData.SkillPoints);
                }
                
                OnSkillLearned?.Invoke(unit, skill);
                return true;
            }
            
            return false;
        }
        
        public bool UpgradeSkill(Unit unit, string skillId)
        {
            if (!adventurerSkills.ContainsKey(unit.UnitId))
                return false;
            
            var skillData = adventurerSkills[unit.UnitId];
            
            if (!skillData.LearnedSkillIds.Contains(skillId))
                return false;
            
            if (skillData.SkillPoints < 1)
                return false;
            
            var skillManager = Core.GameManager.Instance?.SkillManager;
            if (skillManager == null) return false;
            
            var skill = skillManager.GetSkill(skillId);
            if (skill == null) return false;
            
            int currentLevel = skillData.SkillLevels[skillId];
            int maxLevel = 5; // Max skill level
            
            if (currentLevel >= maxLevel)
                return false;
            
            // Upgrade the skill
            if (skillManager.UpgradeSkill(unit, skillId))
            {
                skillData.SkillLevels[skillId]++;
                skillData.SkillPoints--;
                
                OnSkillPointsChanged?.Invoke(unit, skillData.SkillPoints);
                OnSkillUpgraded?.Invoke(unit, skill);
                
                return true;
            }
            
            return false;
        }
        
        public void OnAdventurerLevelUp(Unit unit)
        {
            if (!adventurerSkills.ContainsKey(unit.UnitId))
            {
                InitializeAdventurer(unit);
                return;
            }
            
            var skillData = adventurerSkills[unit.UnitId];
            
            // Grant skill points
            skillData.SkillPoints += SKILL_POINTS_PER_LEVEL;
            OnSkillPointsChanged?.Invoke(unit, skillData.SkillPoints);
            
            // Check for new skill slot
            int newMaxSlots = 1 + (unit.Level / SKILL_UNLOCK_LEVELS);
            if (newMaxSlots > skillData.MaxSkillSlots)
            {
                skillData.MaxSkillSlots = newMaxSlots;
                Debug.Log($"{unit.Name} unlocked a new skill slot! Total slots: {skillData.MaxSkillSlots}");
            }
            
            // Auto-learn class skills at certain levels
            AutoLearnMilestoneSkills(unit);
        }
        
        void AutoLearnMilestoneSkills(Unit unit)
        {
            var skillManager = Core.GameManager.Instance?.SkillManager;
            if (skillManager == null) return;
            
            // Learn ultimate skill at level 15
            if (unit.Level == 15)
            {
                string ultimateSkillId = GetUltimateSkillForClass(unit.JobClass);
                if (!string.IsNullOrEmpty(ultimateSkillId))
                {
                    LearnSkill(unit, ultimateSkillId, true); // Free ultimate skill
                }
            }
        }
        
        string GetUltimateSkillForClass(JobClass jobClass)
        {
            switch (jobClass)
            {
                case JobClass.Warrior: return "skill_battlecry";
                case JobClass.Mage: return "skill_meteor";
                case JobClass.Sage: return "skill_time_warp";
                default: return null;
            }
        }
        
        public List<Battle.Skill> GetAvailableSkillsToLearn(Unit unit)
        {
            var skillManager = Core.GameManager.Instance?.SkillManager;
            if (skillManager == null) return new List<Battle.Skill>();
            
            if (!adventurerSkills.ContainsKey(unit.UnitId))
            {
                InitializeAdventurer(unit);
            }
            
            var skillData = adventurerSkills[unit.UnitId];
            var classSkills = skillManager.GetClassSkills(unit.JobClass);
            
            return classSkills
                .Where(s => !skillData.LearnedSkillIds.Contains(s.SkillId))
                .Where(s => unit.Level >= s.RequiredLevel)
                .ToList();
        }
        
        public AdventurerSkillData GetAdventurerSkillData(Unit unit)
        {
            if (!adventurerSkills.ContainsKey(unit.UnitId))
            {
                InitializeAdventurer(unit);
            }
            
            return adventurerSkills[unit.UnitId];
        }
        
        public int GetSkillPoints(Unit unit)
        {
            if (!adventurerSkills.ContainsKey(unit.UnitId))
            {
                InitializeAdventurer(unit);
            }
            
            return adventurerSkills[unit.UnitId].SkillPoints;
        }
        
        public void ResetSkills(Unit unit)
        {
            if (!adventurerSkills.ContainsKey(unit.UnitId))
                return;
            
            var skillData = adventurerSkills[unit.UnitId];
            var skillManager = Core.GameManager.Instance?.SkillManager;
            
            if (skillManager == null) return;
            
            // Calculate total skill points to refund
            int totalRefund = 0;
            foreach (var kvp in skillData.SkillLevels)
            {
                totalRefund += kvp.Value;
            }
            
            // Reset all skills
            skillData.LearnedSkillIds.Clear();
            skillData.SkillLevels.Clear();
            skillData.SkillPoints += totalRefund;
            
            // Re-learn basic skill
            AutoLearnBasicSkill(unit);
            
            OnSkillPointsChanged?.Invoke(unit, skillData.SkillPoints);
        }
        
        public int GetResetCost(Unit unit)
        {
            // Reset cost increases with level
            return unit.Level * 100;
        }
    }
}