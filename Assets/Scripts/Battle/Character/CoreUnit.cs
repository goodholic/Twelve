using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Equipment;

namespace GuildMaster.Core
{
    /// <summary>
    /// 핵심 유닛 클래스 (Battle.Unit과 구분)
    /// </summary>
    [System.Serializable]
    public class CoreUnit
    {
        [Header("기본 정보")]
        public string unitName;
        public int unitId;
        public int level = 1;
        public float experience = 0f;
        
        [Header("능력치")]
        public float maxHP = 100f;
        public float currentHP = 100f;
        public float attackPower = 20f;
        public float defense = 10f;
        public float speed = 5f;
        public float criticalRate = 0.05f;
        public float criticalDamage = 1.5f;
        
        [Header("상태")]
        public bool isAlive = true;
        public bool isSelected = false;
        public Vector3 position = Vector3.zero;
        
        [Header("장비")]
        public List<int> equippedItems = new List<int>();
        
        /// <summary>
        /// 유닛 초기화
        /// </summary>
        public void Initialize(string name, int id, int startLevel = 1)
        {
            unitName = name;
            unitId = id;
            level = startLevel;
            currentHP = maxHP;
            isAlive = true;
            
            // 레벨에 따른 능력치 조정
            AdjustStatsForLevel();
        }
        
        /// <summary>
        /// 레벨에 따른 능력치 조정
        /// </summary>
        private void AdjustStatsForLevel()
        {
            float levelMultiplier = 1f + (level - 1) * 0.1f;
            maxHP *= levelMultiplier;
            attackPower *= levelMultiplier;
            defense *= levelMultiplier;
            currentHP = maxHP;
        }
        
        /// <summary>
        /// 데미지 받기
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!isAlive) return;
            
            float actualDamage = Mathf.Max(1f, damage - defense);
            currentHP -= actualDamage;
            
            if (currentHP <= 0)
            {
                currentHP = 0;
                isAlive = false;
            }
        }
        
        /// <summary>
        /// 치료
        /// </summary>
        public void Heal(float healAmount)
        {
            if (!isAlive) return;
            
            currentHP = Mathf.Min(maxHP, currentHP + healAmount);
        }
        
        /// <summary>
        /// 레벨업
        /// </summary>
        public void LevelUp()
        {
            level++;
            AdjustStatsForLevel();
            
            Debug.Log($"{unitName}이(가) 레벨 {level}로 올랐습니다!");
        }
        
        /// <summary>
        /// 경험치 추가
        /// </summary>
        public void AddExperience(float exp)
        {
            experience += exp;
            
            // 레벨업 체크 (간단한 공식)
            float requiredExp = level * 100f;
            if (experience >= requiredExp)
            {
                experience -= requiredExp;
                LevelUp();
            }
        }
        
        /// <summary>
        /// 전투력 계산
        /// </summary>
        public float GetCombatPower()
        {
            return (maxHP * 0.5f + attackPower * 2f + defense + speed) * level;
        }
        
        /// <summary>
        /// 유닛 상태 초기화
        /// </summary>
        public void ResetBattleState()
        {
            currentHP = maxHP;
            isAlive = currentHP > 0;
        }
        
        /// <summary>
        /// 유닛 복사
        /// </summary>
        public CoreUnit Clone()
        {
            var clone = new CoreUnit();
            clone.unitName = unitName;
            clone.unitId = unitId;
            clone.level = level;
            clone.experience = experience;
            clone.maxHP = maxHP;
            clone.currentHP = currentHP;
            clone.attackPower = attackPower;
            clone.defense = defense;
            clone.speed = speed;
            clone.criticalRate = criticalRate;
            clone.criticalDamage = criticalDamage;
            clone.isAlive = isAlive;
            clone.position = position;
            clone.equippedItems = new List<int>(equippedItems);
            
            return clone;
        }
    }
} 