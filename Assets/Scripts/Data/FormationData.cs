using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class FormationData
    {
        [Header("기본 정보")]
        public string formationId;
        public string formationName;
        public string description;
        
        // CSVDataManager 호환성 속성들
        public int id { get => int.TryParse(formationId, out int result) ? result : 0; set => formationId = value.ToString(); }
        public string nameKey { get => formationName; set => formationName = value; }
        public List<float> bonusEffects { get => bonuses.Select(b => b.value).ToList(); }
        
        [Header("대형 설정")]
        public FormationType formationType;
        public int maxUnits = 5;
        public FormationShape shape = FormationShape.Line;
        
        [Header("위치 정보")]
        public List<FormationPosition> positions = new List<FormationPosition>();
        public Vector2 formationSize = new Vector2(3, 2);
        public float unitSpacing = 1.5f;
        
        [Header("보너스 효과")]
        public List<FormationBonus> bonuses = new List<FormationBonus>();
        public bool hasLeaderBonus = false;
        public FormationBonus leaderBonus;
        
        [Header("요구사항")]
        public int requiredLevel = 1;
        public int unlockedUnits = 3;
        public bool isLocked = false;
        
        public FormationData()
        {
            formationId = System.Guid.NewGuid().ToString();
            formationName = "New Formation";
        }
        
        public bool CanUse(int playerLevel, int availableUnits)
        {
            return playerLevel >= requiredLevel && availableUnits >= unlockedUnits && !isLocked;
        }
        
        public Vector3 GetWorldPosition(int index, Vector3 centerPosition)
        {
            if (index >= 0 && index < positions.Count)
            {
                var offset = positions[index].localPosition;
                return centerPosition + new Vector3(offset.x, 0, offset.y);
            }
            return centerPosition;
        }
        
        public List<FormationBonus> GetActiveBonuses(List<string> unitTypes)
        {
            List<FormationBonus> activeBonuses = new List<FormationBonus>();
            
            foreach (var bonus in bonuses)
            {
                if (bonus.CanApply(unitTypes))
                {
                    activeBonuses.Add(bonus);
                }
            }
            
            return activeBonuses;
        }
    }
    
    [System.Serializable]
    public class FormationPosition
    {
        public Vector2 localPosition;
        public FormationRole preferredRole = FormationRole.Any;
        public bool isLeaderPosition = false;
        public int priority = 0;
    }
    
    [System.Serializable]
    public class FormationBonus
    {
        public string bonusName;
        public FormationBonusType bonusType;
        public float value;
        public List<string> requiredUnitTypes = new List<string>();
        public FormationCondition condition;
        
        public bool CanApply(List<string> availableUnitTypes)
        {
            if (requiredUnitTypes.Count == 0) return true;
            
            foreach (string required in requiredUnitTypes)
            {
                if (!availableUnitTypes.Contains(required))
                    return false;
            }
            return true;
        }
    }
    
    public enum FormationType
    {
        Offensive,
        Defensive,
        Balanced,
        Support,
        Tactical
    }
    
    public enum FormationShape
    {
        Line,
        V_Shape,
        Wedge,
        Square,
        Circle,
        Diamond
    }
    
    public enum FormationRole
    {
        Any,
        Tank,
        DPS,
        Support,
        Leader,
        Flanker
    }
    
    public enum FormationBonusType
    {
        AttackBonus,
        DefenseBonus,
        SpeedBonus,
        CritRateBonus,
        HealingBonus,
        ExperienceBonus
    }
    
    public enum FormationCondition
    {
        Always,
        FullFormation,
        SameType,
        MixedTypes,
        HighLevel
    }
} 