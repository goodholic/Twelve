using System;
using System.Collections.Generic;
using UnityEngine;
using GuildMaster.Data;
using GuildMaster.Systems;
using Unit = GuildMaster.Battle.UnitStatus;

namespace GuildMaster.Battle
{
    /// <summary>
    /// AI 길드 생성기
    /// 난이도에 따라 AI 부대를 생성
    /// </summary>
    public static class AIGuildGenerator
    {
        public enum Difficulty
        {
            Novice,     // 초보자
            Apprentice, // 견습생
            Journeyman, // 숙련공
            Expert,     // 전문가
            Master,     // 마스터
            Legendary   // 전설
        }
        
        /// <summary>
        /// AI 길드의 부대들을 생성
        /// </summary>
        public static List<Squad> GenerateAIGuild(Difficulty difficulty, int squadCount)
        {
            var squads = new List<Squad>();
            
            for (int i = 0; i < squadCount; i++)
            {
                var squad = GenerateAISquad(difficulty, $"AI Squad {i + 1}", i);
                if (squad != null)
                {
                    squads.Add(squad);
                }
            }
            
            return squads;
        }
        
        /// <summary>
        /// 단일 AI 부대 생성
        /// </summary>
        public static Squad GenerateAISquad(Difficulty difficulty, string squadName, int squadIndex)
        {
            var squad = new Squad(squadName, squadIndex, false); // isPlayerSquad = false
            
            // 난이도에 따른 유닛 수와 레벨 결정
            int unitCount = GetUnitCount(difficulty);
            int averageLevel = GetAverageLevel(difficulty);
            Rarity[] rarityPool = GetRarityPool(difficulty);
            
            // 균형잡힌 직업 구성
            JobClass[] jobComposition = GetJobComposition(unitCount);
            
            for (int i = 0; i < unitCount; i++)
            {
                // 무작위 레어도 선택
                Rarity rarity = rarityPool[UnityEngine.Random.Range(0, rarityPool.Length)];
                
                // 직업별 캐릭터 생성
                var unit = CreateAIUnit(jobComposition[i], rarity, averageLevel, difficulty);
                if (unit != null)
                {
                    // 3x3 그리드에 배치
                    int row = i / 3;
                    int col = i % 3;
                    squad.AddUnit(unit, row, col);
                }
            }
            
            return squad;
        }
        
        static UnitStatus CreateAIUnit(JobClass jobClass, Rarity rarity, int baseLevel, Difficulty difficulty)
        {
            try
            {
                // CharacterManager를 통해 캐릭터 생성
                var characterManager = CharacterManager.Instance;
                if (characterManager == null)
                {
                    Debug.LogError("CharacterManager not available!");
                    return null;
                }
                
                // 직업과 레어도에 맞는 랜덤 유닛 생성
                var unit = characterManager.CreateRandomUnitByClass(jobClass);
                if (unit == null)
                {
                    // 폴백: 기본 유닛 생성
                    unit = new UnitStatus($"AI_{jobClass}", baseLevel, jobClass);
                }
                
                // 레벨 조정
                int levelVariation = UnityEngine.Random.Range(-2, 3);
                int targetLevel = Mathf.Max(1, baseLevel + levelVariation);
                
                while (unit.level < targetLevel)
                {
                    unit.AddExperience(unit.experienceToNextLevel);
                }
                
                // 난이도에 따른 스탯 보정
                ApplyDifficultyModifiers(unit, difficulty);
                
                // AI 유닛 표시
                unit.isAI = true;
                unit.teamId = 1; // AI 팀
                
                return unit;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create AI unit: {e.Message}");
                return null;
            }
        }
        
        static void ApplyDifficultyModifiers(UnitStatus unit, Difficulty difficulty)
        {
            float statMultiplier = difficulty switch
            {
                Difficulty.Novice => 0.8f,
                Difficulty.Apprentice => 0.9f,
                Difficulty.Journeyman => 1.0f,
                Difficulty.Expert => 1.2f,
                Difficulty.Master => 1.5f,
                Difficulty.Legendary => 2.0f,
                _ => 1.0f
            };
            
            unit.maxHP = Mathf.RoundToInt(unit.maxHP * statMultiplier);
            unit.currentHP = unit.maxHP;
            unit.attackPower = Mathf.RoundToInt(unit.attackPower * statMultiplier);
            unit.defense = Mathf.RoundToInt(unit.defense * statMultiplier);
            unit.magicPower = Mathf.RoundToInt(unit.magicPower * statMultiplier);
            
            // 높은 난이도일수록 AI 반응속도 증가
            if (difficulty >= Difficulty.Expert)
            {
                unit.speed = Mathf.RoundToInt(unit.speed * 1.2f);
            }
        }
        
        static int GetUnitCount(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Novice => 3,      // 3명
                Difficulty.Apprentice => 5,  // 5명
                Difficulty.Journeyman => 7,  // 7명
                Difficulty.Expert => 9,      // 9명 (풀 스쿼드)
                Difficulty.Master => 9,      // 9명
                Difficulty.Legendary => 9,   // 9명
                _ => 5
            };
        }
        
        static int GetAverageLevel(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Novice => 5,
                Difficulty.Apprentice => 10,
                Difficulty.Journeyman => 20,
                Difficulty.Expert => 35,
                Difficulty.Master => 50,
                Difficulty.Legendary => 70,
                _ => 10
            };
        }
        
        static Rarity[] GetRarityPool(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Novice => new[] { Rarity.Common, Rarity.Common, Rarity.Uncommon },
                Difficulty.Apprentice => new[] { Rarity.Common, Rarity.Uncommon, Rarity.Uncommon },
                Difficulty.Journeyman => new[] { Rarity.Uncommon, Rarity.Uncommon, Rarity.Rare },
                Difficulty.Expert => new[] { Rarity.Uncommon, Rarity.Rare, Rarity.Rare },
                Difficulty.Master => new[] { Rarity.Rare, Rarity.Rare, Rarity.Epic },
                Difficulty.Legendary => new[] { Rarity.Rare, Rarity.Epic, Rarity.Legendary },
                _ => new[] { Rarity.Common, Rarity.Uncommon }
            };
        }
        
        static JobClass[] GetJobComposition(int unitCount)
        {
            // 균형잡힌 팀 구성을 위한 직업 배분
            var composition = new List<JobClass>();
            
            // 기본 구성: 탱커, 힐러, 딜러
            if (unitCount >= 3)
            {
                composition.Add(JobClass.Knight);    // 탱커
                composition.Add(JobClass.Priest);    // 힐러
                composition.Add(JobClass.Warrior);   // 근거리 딜러
            }
            
            // 추가 유닛
            if (unitCount >= 4) composition.Add(JobClass.Mage);      // 마법 딜러
            if (unitCount >= 5) composition.Add(JobClass.Ranger);    // 원거리 딜러
            if (unitCount >= 6) composition.Add(JobClass.Assassin);  // 암살자
            if (unitCount >= 7) composition.Add(JobClass.Knight);    // 추가 탱커
            if (unitCount >= 8) composition.Add(JobClass.Warrior);   // 추가 딜러
            if (unitCount >= 9) composition.Add(JobClass.Mage);      // 추가 마법사
            
            // 배열로 변환하고 섞기
            var result = composition.ToArray();
            for (int i = 0; i < result.Length; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, result.Length);
                var temp = result[i];
                result[i] = result[randomIndex];
                result[randomIndex] = temp;
            }
            
            return result;
        }
        
        /// <summary>
        /// 특수 테마 AI 길드 생성
        /// </summary>
        public static Squad GenerateThemedSquad(string theme, Difficulty difficulty)
        {
            var squad = new Squad($"{theme} Squad", 0, false);
            
            switch (theme.ToLower())
            {
                case "knights":
                    // 기사단 테마
                    GenerateKnightSquad(squad, difficulty);
                    break;
                    
                case "mages":
                    // 마법사 길드 테마
                    GenerateMageSquad(squad, difficulty);
                    break;
                    
                case "assassins":
                    // 암살자 길드 테마
                    GenerateAssassinSquad(squad, difficulty);
                    break;
                    
                default:
                    // 기본 균형 부대
                    return GenerateAISquad(difficulty, squad.squadName, 0);
            }
            
            return squad;
        }
        
        static void GenerateKnightSquad(Squad squad, Difficulty difficulty)
        {
            int unitCount = GetUnitCount(difficulty);
            int level = GetAverageLevel(difficulty);
            
            // 기사와 성직자 위주의 구성
            JobClass[] knightComposition = { 
                JobClass.Knight, JobClass.Knight, JobClass.Priest,
                JobClass.Warrior, JobClass.Knight, JobClass.Priest,
                JobClass.Knight, JobClass.Warrior, JobClass.Knight
            };
            
            for (int i = 0; i < unitCount && i < knightComposition.Length; i++)
            {
                var unit = CreateAIUnit(knightComposition[i], Rarity.Rare, level, difficulty);
                if (unit != null)
                {
                    squad.AddUnit(unit, i / 3, i % 3);
                }
            }
        }
        
        static void GenerateMageSquad(Squad squad, Difficulty difficulty)
        {
            int unitCount = GetUnitCount(difficulty);
            int level = GetAverageLevel(difficulty);
            
            // 마법사 위주의 구성
            JobClass[] mageComposition = { 
                JobClass.Mage, JobClass.Mage, JobClass.Wizard,
                JobClass.Priest, JobClass.Mage, JobClass.Wizard,
                JobClass.Knight, JobClass.Mage, JobClass.Wizard
            };
            
            for (int i = 0; i < unitCount && i < mageComposition.Length; i++)
            {
                var unit = CreateAIUnit(mageComposition[i], Rarity.Rare, level, difficulty);
                if (unit != null)
                {
                    squad.AddUnit(unit, i / 3, i % 3);
                }
            }
        }
        
        static void GenerateAssassinSquad(Squad squad, Difficulty difficulty)
        {
            int unitCount = GetUnitCount(difficulty);
            int level = GetAverageLevel(difficulty);
            
            // 암살자와 레인저 위주의 구성
            JobClass[] assassinComposition = { 
                JobClass.Assassin, JobClass.Assassin, JobClass.Ranger,
                JobClass.Rogue, JobClass.Assassin, JobClass.Ranger,
                JobClass.Assassin, JobClass.Rogue, JobClass.Priest
            };
            
            for (int i = 0; i < unitCount && i < assassinComposition.Length; i++)
            {
                var unit = CreateAIUnit(assassinComposition[i], Rarity.Rare, level, difficulty);
                if (unit != null)
                {
                    squad.AddUnit(unit, i / 3, i % 3);
                }
            }
        }
    }
}