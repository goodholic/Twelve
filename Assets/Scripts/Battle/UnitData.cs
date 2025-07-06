using System;
using UnityEngine;
using GuildMaster.Data;

namespace GuildMaster.Battle
{
    /// <summary>
    /// Unit의 순수 데이터만을 포함하는 직렬화 가능한 클래스
    /// </summary>
    [System.Serializable]
    public class UnitData
    {
        // Basic Info
        public string unitId;
        public string characterId;
        public string unitName;
        public int level;
        public JobClass jobClass;
        public Rarity rarity;
        public bool isPlayerUnit;
        public int experience;
        public int experienceToNextLevel;
        
        // Job Mastery
        public float jobMastery;
        public int awakeningLevel;

        // Position in Battle
        public int currentSquad;
        public Vector2Int gridPosition;
        public float formationBuff = 1f;

        // Base Stats
        public float maxHP;
        public float currentHP;
        public float maxMP;
        public float currentMP;
        public float attackPower;
        public float defense;
        public float magicPower;
        public float magicResistance;
        public float speed;
        public float criticalRate;
        public float criticalDamage;
        public float accuracy;
        public float evasion;
        
        // Shield system
        public float currentShield;
        public float maxShield;

        // Skills
        public int[] skillIds;
        
        // AI related
        public bool isAI = false;
        public int teamId = 0;
        
        // Stat multipliers
        public float attackMultiplier = 1f;
        public float defenseMultiplier = 1f;
        public float speedMultiplier = 1f;
        
        public UnitData()
        {
            unitId = Guid.NewGuid().ToString();
            skillIds = new int[0];
        }
        
        /// <summary>
        /// Unit 객체로부터 데이터 복사
        /// </summary>
        public void CopyFromUnit(Unit unit)
        {
            if (unit == null) return;
            
            unitId = unit.unitId;
            characterId = unit.characterId;
            unitName = unit.unitName;
            level = unit.level;
            jobClass = unit.jobClass;
            rarity = unit.rarity;
            isPlayerUnit = unit.isPlayerUnit;
            experience = unit.experience;
            experienceToNextLevel = unit.experienceToNextLevel;
            
            jobMastery = unit.jobMastery;
            awakeningLevel = unit.awakeningLevel;
            
            currentSquad = unit.currentSquad;
            gridPosition = unit.gridPosition;
            formationBuff = unit.formationBuff;
            
            maxHP = unit.maxHP;
            currentHP = unit.currentHP;
            maxMP = unit.maxMP;
            currentMP = unit.currentMP;
            attackPower = unit.attackPower;
            defense = unit.defense;
            magicPower = unit.magicPower;
            magicResistance = unit.magicResistance;
            speed = unit.speed;
            criticalRate = unit.criticalRate;
            criticalDamage = unit.criticalDamage;
            accuracy = unit.accuracy;
            evasion = unit.evasion;
            
            currentShield = unit.currentShield;
            maxShield = unit.maxShield;
            
            skillIds = unit.skillIds.ToArray();
            
            isAI = unit.isAI;
            teamId = unit.teamId;
            
            attackMultiplier = unit.attackMultiplier;
            defenseMultiplier = unit.defenseMultiplier;
            speedMultiplier = unit.speedMultiplier;
        }
    }
}