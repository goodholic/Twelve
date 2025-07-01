using UnityEngine;
using System.Collections.Generic;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class Unit
    {
        [Header("기본 정보")]
        public int unitId;
        public int characterId;
        public string unitName = "Unit";
        
        [Header("레벨 및 경험치")]
        public int level = 1;
        public int experience = 0;
        
        [Header("체력 및 마나")]
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public float maxMP = 50f;
        public float currentMP = 50f;
        public int maxHP = 100;
        public int currentHP = 100;
        
        [Header("스탯")]
        public int attackPower = 10;
        public int defense = 5;
        public int experienceToNextLevel = 100;
        
        [Header("직업")]
        public JobClass jobClass = JobClass.Warrior;
        
        [Header("아이콘")]
        public Sprite icon;
        
        // 속성들
        public string Name 
        { 
            get { return unitName; } 
            set { unitName = value; }
        }
        
        public Sprite Icon 
        { 
            get { return icon; } 
            set { icon = value; }
        }
        
        public float CurrentHealth 
        { 
            get { return currentHealth; } 
            set { currentHealth = Mathf.Clamp(value, 0, maxHealth); }
        }
        
        public float CurrentMP 
        { 
            get { return currentMP; } 
            set { currentMP = Mathf.Clamp(value, 0, maxMP); }
        }
        
        /// <summary>
        /// 전투력 계산
        /// </summary>
        public float GetCombatPower()
        {
            float basePower = level * 10f;
            float healthRatio = currentHealth / maxHealth;
            return basePower * (0.5f + healthRatio * 0.5f);
        }
        
        /// <summary>
        /// 경험치 추가
        /// </summary>
        public void AddExperience(int exp)
        {
            experience += exp;
            while (experience >= experienceToNextLevel)
            {
                experience -= experienceToNextLevel;
                level++;
                experienceToNextLevel = Mathf.RoundToInt(experienceToNextLevel * 1.1f);
            }
        }
        
        /// <summary>
        /// 레벨 프로퍼티 (호환성)
        /// </summary>
        public int Level 
        { 
            get { return level; } 
            set { level = value; }
        }
        
        /// <summary>
        /// 상태 효과 목록 가져오기
        /// </summary>
        public List<string> GetStatusEffects()
        {
            // TODO: 실제 상태 효과 시스템 구현 시 확장
            return new List<string>();
        }
        
        /// <summary>
        /// 기본 생성자
        /// </summary>
        public Unit()
        {
            unitId = 0;
            characterId = 0;
            unitName = "Unit";
            level = 1;
            experience = 0;
            maxHealth = 100f;
            currentHealth = 100f;
            maxHP = 100;
            currentHP = 100;
            maxMP = 50f;
            currentMP = 50f;
            attackPower = 10;
            defense = 5;
            experienceToNextLevel = 100;
            jobClass = JobClass.Warrior;
        }
        
        /// <summary>
        /// 매개변수가 있는 생성자
        /// </summary>
        public Unit(int id, int charId, string name)
        {
            unitId = id;
            characterId = charId;
            unitName = name;
            level = 1;
            experience = 0;
            maxHealth = 100f;
            currentHealth = 100f;
            maxHP = 100;
            currentHP = 100;
            maxMP = 50f;
            currentMP = 50f;
            attackPower = 10;
            defense = 5;
            experienceToNextLevel = 100;
            jobClass = JobClass.Warrior;
        }
        
        /// <summary>
        /// 문자열 기반 생성자 (CharacterManager 호환성)
        /// </summary>
        public Unit(string name, int lvl, JobClass job)
        {
            unitId = 0;
            characterId = 0;
            unitName = name;
            level = lvl;
            experience = 0;
            maxHealth = 100f;
            currentHealth = 100f;
            maxHP = 100;
            currentHP = 100;
            maxMP = 50f;
            currentMP = 50f;
            attackPower = 10;
            defense = 5;
            experienceToNextLevel = 100;
            jobClass = job;
        }
    }
} 