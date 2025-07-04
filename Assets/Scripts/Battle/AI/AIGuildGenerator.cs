using UnityEngine;
using System.Collections.Generic;
using GuildMaster.Battle;

namespace GuildMaster.Battle.AI
{
    public static class AIGuildGenerator
    {
        public enum Difficulty
        {
            Novice,
            Bronze,
            Silver,
            Gold,
            Platinum,
            Diamond
        }
        
        public static List<Squad> GenerateAIGuild(Difficulty difficulty, int unitCount)
        {
            var squads = new List<Squad>();
            
            // 난이도에 따른 기본 설정
            int baseLevel = GetBaseLevelForDifficulty(difficulty);
            Rarity minRarity = GetMinRarityForDifficulty(difficulty);
            
            // 9명씩 2개 부대 생성
            for (int squadIndex = 0; squadIndex < 2; squadIndex++)
            {
                var squad = new Squad($"AI Squad {squadIndex}", squadIndex, false);
                
                for (int i = 0; i < 9 && i + (squadIndex * 9) < unitCount; i++)
                {
                    var unit = GenerateAIUnit(baseLevel, minRarity, difficulty);
                    squad.AddUnit(unit);
                }
                
                squads.Add(squad);
            }
            
            return squads;
        }
        
        private static Unit GenerateAIUnit(int baseLevel, Rarity minRarity, Difficulty difficulty)
        {
            // 랜덤 직업 선택
            JobClass[] availableClasses = { 
                JobClass.Warrior, JobClass.Knight, JobClass.Mage, 
                JobClass.Priest, JobClass.Assassin, JobClass.Ranger, JobClass.Sage 
            };
            
            JobClass randomClass = availableClasses[Random.Range(0, availableClasses.Length)];
            
            // 레벨 변동
            int level = baseLevel + Random.Range(-2, 3);
            level = Mathf.Max(1, level);
            
            // 등급 결정
            Rarity rarity = DetermineRarity(minRarity, difficulty);
            
            // 유닛 생성
            var unit = new Unit($"AI_{randomClass}_{Random.Range(1000, 9999)}", level, randomClass, rarity);
            unit.isPlayerUnit = false;
            
            // 난이도별 추가 스탯 보너스
            ApplyDifficultyBonus(unit, difficulty);
            
            return unit;
        }
        
        private static int GetBaseLevelForDifficulty(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Novice => 5,
                Difficulty.Bronze => 10,
                Difficulty.Silver => 15,
                Difficulty.Gold => 20,
                Difficulty.Platinum => 25,
                Difficulty.Diamond => 30,
                _ => 5
            };
        }
        
        private static Rarity GetMinRarityForDifficulty(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Novice => Rarity.Common,
                Difficulty.Bronze => Rarity.Common,
                Difficulty.Silver => Rarity.Uncommon,
                Difficulty.Gold => Rarity.Rare,
                Difficulty.Platinum => Rarity.Epic,
                Difficulty.Diamond => Rarity.Legendary,
                _ => Rarity.Common
            };
        }
        
        private static Rarity DetermineRarity(Rarity minRarity, Difficulty difficulty)
        {
            float randomValue = Random.value;
            
            // 난이도별 등급 확률
            var probabilities = difficulty switch
            {
                Difficulty.Novice => new float[] { 0.7f, 0.25f, 0.05f, 0f, 0f },
                Difficulty.Bronze => new float[] { 0.5f, 0.35f, 0.15f, 0f, 0f },
                Difficulty.Silver => new float[] { 0.3f, 0.4f, 0.25f, 0.05f, 0f },
                Difficulty.Gold => new float[] { 0.2f, 0.3f, 0.35f, 0.15f, 0f },
                Difficulty.Platinum => new float[] { 0.1f, 0.2f, 0.35f, 0.25f, 0.1f },
                Difficulty.Diamond => new float[] { 0.05f, 0.15f, 0.3f, 0.35f, 0.15f },
                _ => new float[] { 0.7f, 0.25f, 0.05f, 0f, 0f }
            };
            
            float cumulative = 0f;
            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulative += probabilities[i];
                if (randomValue <= cumulative)
                {
                    Rarity selectedRarity = (Rarity)i;
                    return selectedRarity >= minRarity ? selectedRarity : minRarity;
                }
            }
            
            return minRarity;
        }
        
        private static void ApplyDifficultyBonus(Unit unit, Difficulty difficulty)
        {
            float multiplier = difficulty switch
            {
                Difficulty.Novice => 1.0f,
                Difficulty.Bronze => 1.1f,
                Difficulty.Silver => 1.2f,
                Difficulty.Gold => 1.35f,
                Difficulty.Platinum => 1.5f,
                Difficulty.Diamond => 1.75f,
                _ => 1.0f
            };
            
            unit.attackPower *= multiplier;
            unit.maxHP *= multiplier;
            unit.currentHP = unit.maxHP;
            unit.defense *= multiplier;
            unit.magicPower *= multiplier;
        }
    }
} 