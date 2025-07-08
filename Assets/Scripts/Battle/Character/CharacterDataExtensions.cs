using UnityEngine;

namespace GuildMaster.Battle
{
    /// <summary>
    /// CharacterData의 확장 메서드들
    /// </summary>
    public static class CharacterDataExtensions
    {
        /// <summary>
        /// 캐릭터의 직업에 따른 공격 패턴 반환
        /// </summary>
        public static AttackPattern GetAttackPattern(this CharacterData character)
        {
            switch (character.jobClass)
            {
                case JobClass.Warrior:
                    // 전사: 십자형 패턴
                    return AttackPattern.GetCrossPattern();
                    
                case JobClass.Knight:
                    // 기사: 사각형 패턴 (주변 8칸)
                    return AttackPattern.GetSquarePattern();
                    
                case JobClass.Wizard:
                    // 마법사: 대각선 패턴 + 확장
                    var wizardPattern = new AttackPattern("마법사 패턴");
                    wizardPattern.attackOffsets.Add(new Vector2Int(1, 1));
                    wizardPattern.attackOffsets.Add(new Vector2Int(1, -1));
                    wizardPattern.attackOffsets.Add(new Vector2Int(-1, 1));
                    wizardPattern.attackOffsets.Add(new Vector2Int(-1, -1));
                    wizardPattern.attackOffsets.Add(new Vector2Int(2, 0));
                    wizardPattern.attackOffsets.Add(new Vector2Int(-2, 0));
                    wizardPattern.attackOffsets.Add(new Vector2Int(0, 2));
                    wizardPattern.attackOffsets.Add(new Vector2Int(0, -2));
                    return wizardPattern;
                    
                case JobClass.Priest:
                    // 성직자: 십자형 패턴
                    return AttackPattern.GetCrossPattern();
                    
                case JobClass.Rogue:
                    // 도적: 대각선 패턴
                    return AttackPattern.GetDiagonalPattern();
                    
                case JobClass.Sage:
                    // 현자: 사각형 패턴
                    return AttackPattern.GetSquarePattern();
                    
                case JobClass.Archer:
                    // 궁수: 직선 3칸
                    return AttackPattern.GetLinePattern(3);
                    
                case JobClass.Gunner:
                    // 총사: 직선 4칸 + 관통
                    var gunnerPattern = new AttackPattern("총사 패턴");
                    for (int i = 1; i <= 4; i++)
                    {
                        gunnerPattern.attackOffsets.Add(new Vector2Int(i, 0));
                        gunnerPattern.attackOffsets.Add(new Vector2Int(-i, 0));
                        gunnerPattern.attackOffsets.Add(new Vector2Int(0, i));
                        gunnerPattern.attackOffsets.Add(new Vector2Int(0, -i));
                    }
                    return gunnerPattern;
                    
                default:
                    // 기본: 십자형
                    return AttackPattern.GetCrossPattern();
            }
        }
    }
    
    /// <summary>
    /// CharacterData 부분 클래스 - Battle 시스템용 추가 속성
    /// </summary>
    public partial class CharacterData
    {
        [Header("전투 비주얼")]
        public Sprite characterSprite;  // 캐릭터 스프라이트
        public Color characterColor = Color.white;  // 캐릭터 색상
        
        [Header("전투 패턴")]
        public bool hasCustomPattern = false;  // 커스텀 패턴 사용 여부
        public AttackPattern customAttackPattern;  // 커스텀 공격 패턴
        
        /// <summary>
        /// 공격 범위 반환 (타일 기준)
        /// </summary>
        public int GetAttackRange()
        {
            switch (jobClass)
            {
                case JobClass.Warrior:
                case JobClass.Knight:
                case JobClass.Priest:
                case JobClass.Rogue:
                    return 1;  // 근접
                    
                case JobClass.Wizard:
                case JobClass.Sage:
                    return 2;  // 중거리
                    
                case JobClass.Archer:
                    return 3;  // 원거리
                    
                case JobClass.Gunner:
                    return 4;  // 장거리
                    
                default:
                    return 1;
            }
        }
        
        /// <summary>
        /// 공격 타입 반환
        /// </summary>
        public string GetAttackType()
        {
            switch (jobClass)
            {
                case JobClass.Warrior:
                case JobClass.Knight:
                case JobClass.Rogue:
                case JobClass.Archer:
                case JobClass.Gunner:
                    return "물리";
                    
                case JobClass.Wizard:
                case JobClass.Priest:
                case JobClass.Sage:
                    return "마법";
                    
                default:
                    return "물리";
            }
        }
    }
}