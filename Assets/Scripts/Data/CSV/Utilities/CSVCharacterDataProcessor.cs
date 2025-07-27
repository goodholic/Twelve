using UnityEngine;
using System.Collections.Generic;
using GuildMaster.Data;
using TacticalTileGame.Data;

namespace GuildMaster.CSV
{
    /// <summary>
    /// CSV에서 가져온 캐릭터 데이터의 기본 설정을 처리하는 유틸리티
    /// </summary>
    public static class CSVCharacterDataProcessor
    {
        /// <summary>
        /// 캐릭터 데이터의 기본 설정을 적용
        /// </summary>
        public static void SetupCharacterDefaults(CharacterData character)
        {
            if (character == null) return;
            
            // 공격 패턴과 범위 타입 설정
            SetAttackPatternByJob(character);
            
            // 동영상 애니메이션 설정
                            SetupPNGSequenceAnimation(character);
            
            // 레어도별 기본값 설정
            SetRarityBasedValues(character);
            
            // 기타 기본값 설정
            SetOtherDefaults(character);
        }
        
        /// <summary>
        /// 직업별 공격 패턴 설정
        /// </summary>
        private static void SetAttackPatternByJob(CharacterData character)
        {
            switch (character.jobClass)
            {
                case JobClass.Warrior:
                    character.attackPattern = AttackPattern.Cross; // 십자 공격
                    character.rangeType = RangeType.Melee;
                    character.attackRange = 1.0f;
                    break;
                    
                case JobClass.Knight:
                    character.attackPattern = AttackPattern.Cross; // 방어형 십자 공격
                    character.rangeType = RangeType.Melee;
                    character.attackRange = 1.0f;
                    break;
                    
                case JobClass.Mage: // Wizard
                    character.attackPattern = AttackPattern.Line; // 직선 마법 공격
                    character.rangeType = RangeType.Magic;
                    character.attackRange = 2.0f;
                    break;
                    
                case JobClass.Priest:
                    character.attackPattern = AttackPattern.Cross; // 치유용 십자
                    character.rangeType = RangeType.Magic;
                    character.attackRange = 2.0f;
                    break;
                    
                case JobClass.Rogue:
                    character.attackPattern = AttackPattern.Diagonal; // 대각선 기습
                    character.rangeType = RangeType.Melee;
                    character.attackRange = 1.0f;
                    break;
                    
                case JobClass.Sage:
                    character.attackPattern = AttackPattern.Knight; // 복잡한 나이트 패턴
                    character.rangeType = RangeType.Magic;
                    character.attackRange = 2.0f;
                    break;
                    
                case JobClass.Archer:
                    character.attackPattern = AttackPattern.Line; // 직선 원거리
                    character.rangeType = RangeType.Ranged;
                    character.attackRange = 3.0f;
                    break;
                    
                default:
                    character.attackPattern = AttackPattern.Cross;
                    character.rangeType = RangeType.Melee;
                    character.attackRange = 1.0f;
                    break;
            }
        }
        
        /// <summary>
        /// PNG 시퀀스 애니메이션 기본 설정
        /// </summary>
        private static void SetupPNGSequenceAnimation(CharacterData character)
        {
            character.animationType = AnimationType.PNGSequence;
            character.pngSequenceScale = 1.0f;
            character.loopPNGSequences = true;
            character.pngSequenceFrameRate = 12; // 기본 프레임레이트 설정
        }
        
        /// <summary>
        /// 레어도별 기본값 설정
        /// </summary>
        private static void SetRarityBasedValues(CharacterData character)
        {
            // 별 등급 설정
            character.star = GetStarByRarity(character.rarity);
            character.starLevel = character.star;
            character.initialStar = character.star;
        }
        
        /// <summary>
        /// 기타 기본값 설정
        /// </summary>
        private static void SetOtherDefaults(CharacterData character)
        {
            character.attackSpeed = 1.0f;
            character.currentExp = 0;
            character.expToNextLevel = CalculateExpToNextLevel(character.level);
            character.race = GetRaceByJob(character.jobClass);
            character.maxLevel = 50;
        }
        
        #region Helper Methods
        
        /// <summary>
        /// 레어도별 별 등급 계산
        /// </summary>
        private static int GetStarByRarity(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common: return 1;
                case Rarity.Uncommon: return 2;
                case Rarity.Rare: return 3;
                case Rarity.Epic: return 4;
                case Rarity.Legendary: return 5;
                case Rarity.Mythic: return 6;
                default: return 1;
            }
        }
        
        /// <summary>
        /// 레벨별 다음 레벨까지 필요 경험치 계산
        /// </summary>
        private static int CalculateExpToNextLevel(int currentLevel)
        {
            // 기본 공식: 레벨 * 100 + (레벨 - 1) * 50
            return currentLevel * 100 + (currentLevel - 1) * 50;
        }
        
        /// <summary>
        /// 직업별 종족 설정
        /// </summary>
        private static string GetRaceByJob(JobClass jobClass)
        {
            switch (jobClass)
            {
                case JobClass.Warrior:
                case JobClass.Knight:
                    return "Human";
                    
                case JobClass.Mage:
                case JobClass.Sage:
                    return "Human";
                    
                case JobClass.Priest:
                    return "Human";
                    
                case JobClass.Rogue:
                    return "Human";
                    
                case JobClass.Archer:
                case JobClass.Gunner:
                    return "Elf";
                    
                default:
                    return "Human";
            }
        }
        
        #endregion
        
        #region Validation Methods
        
        /// <summary>
        /// 캐릭터 데이터 유효성 검사
        /// </summary>
        public static bool ValidateCharacterData(CharacterData character, out string errorMessage)
        {
            errorMessage = "";
            
            if (character == null)
            {
                errorMessage = "캐릭터 데이터가 null입니다.";
                return false;
            }
            
            if (string.IsNullOrEmpty(character.id))
            {
                errorMessage = "캐릭터 ID가 비어있습니다.";
                return false;
            }
            
            if (string.IsNullOrEmpty(character.characterName))
            {
                errorMessage = "캐릭터 이름이 비어있습니다.";
                return false;
            }
            
            if (character.baseHP <= 0)
            {
                errorMessage = "기본 HP가 0 이하입니다.";
                return false;
            }
            
            if (character.baseAttack < 0)
            {
                errorMessage = "기본 공격력이 0 미만입니다.";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 캐릭터 밸런스 경고 확인
        /// </summary>
        public static List<string> GetBalanceWarnings(CharacterData character)
        {
            List<string> warnings = new List<string>();
            
            if (character == null) return warnings;
            
            // HP 밸런스 체크
            if (character.baseHP > 2000)
            {
                warnings.Add($"HP가 너무 높습니다: {character.baseHP}");
            }
            else if (character.baseHP < 100)
            {
                warnings.Add($"HP가 너무 낮습니다: {character.baseHP}");
            }
            
            // 공격력 밸런스 체크
            if (character.baseAttack > 300)
            {
                warnings.Add($"공격력이 너무 높습니다: {character.baseAttack}");
            }
            
            // 크리티컬 확률 체크
            if (character.critRate > 0.5f)
            {
                warnings.Add($"크리티컬 확률이 너무 높습니다: {character.critRate * 100}%");
            }
            
            // 명중률 체크
            if (character.accuracy > 100f)
            {
                warnings.Add($"명중률이 100%를 초과합니다: {character.accuracy}%");
            }
            
            return warnings;
        }
        
        #endregion
    }
} 