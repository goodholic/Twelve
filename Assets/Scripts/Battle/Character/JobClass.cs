using UnityEngine;
using System.Collections.Generic;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 직업 종류 열거형
    /// </summary>
    public enum JobClass
    {
        Warrior,    // 전사
        Knight,     // 기사
        Wizard,     // 마법사
        Priest,     // 성직자
        Rogue,      // 도적
        Sage,       // 현자
        Archer,     // 궁수
        Gunner      // 총사
    }
    
    /// <summary>
    /// 직업별 스탯 배율
    /// </summary>
    [System.Serializable]
    public class JobStatMultipliers
    {
        public float hpMultiplier = 1.0f;
        public float attackMultiplier = 1.0f;
        public float defenseMultiplier = 1.0f;
        public float magicPowerMultiplier = 1.0f;
        public float speedMultiplier = 1.0f;
        public float criticalRateBonus = 0.0f;
        public string description;
    }
    
    /// <summary>
    /// 직업 시스템 관리
    /// </summary>
    public static class JobClassSystem
    {
        private static Dictionary<JobClass, JobStatMultipliers> jobStatMultipliers = new Dictionary<JobClass, JobStatMultipliers>
        {
            [JobClass.Warrior] = new JobStatMultipliers
            {
                hpMultiplier = 1.2f,        // 높은 체력
                attackMultiplier = 1.2f,    // 높은 공격력
                defenseMultiplier = 1.0f,   // 보통 방어력
                magicPowerMultiplier = 0.7f, // 낮은 마법력
                speedMultiplier = 1.0f,     // 보통 속도
                criticalRateBonus = 0.05f,  // 5% 크리티컬 보너스
                description = "높은 HP와 물리 공격력을 가진 전방 탱커"
            },
            
            [JobClass.Knight] = new JobStatMultipliers
            {
                hpMultiplier = 1.3f,        // 매우 높은 체력
                attackMultiplier = 0.9f,    // 약간 낮은 공격력
                defenseMultiplier = 1.5f,   // 매우 높은 방어력
                magicPowerMultiplier = 0.6f, // 낮은 마법력
                speedMultiplier = 0.8f,     // 느린 속도
                criticalRateBonus = 0.02f,  // 낮은 크리티컬
                description = "최고의 방어력과 아군 보호 능력"
            },
            
            [JobClass.Wizard] = new JobStatMultipliers
            {
                hpMultiplier = 0.7f,        // 낮은 체력
                attackMultiplier = 0.5f,    // 매우 낮은 물리 공격력
                defenseMultiplier = 0.7f,   // 낮은 방어력
                magicPowerMultiplier = 1.5f, // 매우 높은 마법력
                speedMultiplier = 1.1f,     // 약간 빠른 속도
                criticalRateBonus = 0.08f,  // 높은 마법 크리티컬
                description = "강력한 마법 공격력과 광역 스킬"
            },
            
            [JobClass.Priest] = new JobStatMultipliers
            {
                hpMultiplier = 0.85f,       // 보통 체력
                attackMultiplier = 0.5f,    // 낮은 공격력
                defenseMultiplier = 0.8f,   // 보통 방어력
                magicPowerMultiplier = 1.5f, // 높은 마법력 (힐링 특화)
                speedMultiplier = 1.0f,     // 보통 속도
                criticalRateBonus = 0.03f,  // 낮은 크리티컬
                description = "힐링과 부활 마법의 전문가"
            },
            
            [JobClass.Rogue] = new JobStatMultipliers
            {
                hpMultiplier = 0.85f,       // 낮은 체력
                attackMultiplier = 1.4f,    // 높은 공격력
                defenseMultiplier = 0.6f,   // 낮은 방어력
                magicPowerMultiplier = 0.8f, // 낮은 마법력
                speedMultiplier = 1.5f,     // 매우 빠른 속도
                criticalRateBonus = 0.10f,  // 매우 높은 크리티컬
                description = "빠른 속도와 크리티컬 특화"
            },
            
            [JobClass.Sage] = new JobStatMultipliers
            {
                hpMultiplier = 1.0f,        // 보통 체력
                attackMultiplier = 1.0f,    // 보통 공격력
                defenseMultiplier = 0.9f,   // 약간 낮은 방어력
                magicPowerMultiplier = 1.2f, // 높은 마법력
                speedMultiplier = 1.2f,     // 빠른 속도
                criticalRateBonus = 0.07f,  // 높은 크리티컬
                description = "마법과 물리를 아우르는 만능형"
            },
            
            [JobClass.Archer] = new JobStatMultipliers
            {
                hpMultiplier = 0.9f,        // 보통 체력
                attackMultiplier = 1.0f,    // 보통 공격력 (범위 공격 특화)
                defenseMultiplier = 0.8f,   // 낮은 방어력
                magicPowerMultiplier = 0.8f, // 낮은 마법력
                speedMultiplier = 1.3f,     // 빠른 속도
                criticalRateBonus = 0.06f,  // 보통 크리티컬
                description = "원거리 범위 물리 공격의 달인"
            },
            
            [JobClass.Gunner] = new JobStatMultipliers
            {
                hpMultiplier = 0.75f,       // 낮은 체력
                attackMultiplier = 1.5f,    // 매우 높은 단일 공격력
                defenseMultiplier = 0.6f,   // 매우 낮은 방어력
                magicPowerMultiplier = 0.5f, // 낮은 마법력
                speedMultiplier = 1.0f,     // 보통 속도
                criticalRateBonus = 0.12f,  // 매우 높은 크리티컬
                description = "장거리 단일 물리 공격의 달인"
            }
        };
        
        /// <summary>
        /// 직업별 스탯 배율 가져오기
        /// </summary>
        public static JobStatMultipliers GetJobStatMultipliers(JobClass jobClass)
        {
            if (jobStatMultipliers.ContainsKey(jobClass))
            {
                return jobStatMultipliers[jobClass];
            }
            
            Debug.LogWarning($"직업 {jobClass}에 대한 스탯 배율이 정의되지 않았습니다. 기본값을 반환합니다.");
            return new JobStatMultipliers();
        }
        
        /// <summary>
        /// 직업별 공격 범위 패턴 가져오기
        /// </summary>
        public static List<Vector2Int> GetAttackPattern(JobClass jobClass)
        {
            List<Vector2Int> pattern = new List<Vector2Int>();
            
            switch (jobClass)
            {
                case JobClass.Warrior: // 주변 1칸
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx != 0 || dy != 0)
                                pattern.Add(new Vector2Int(dx, dy));
                        }
                    }
                    break;
                    
                case JobClass.Knight: // 전방 2칸 직선
                    pattern.Add(new Vector2Int(1, 0));
                    pattern.Add(new Vector2Int(2, 0));
                    break;
                    
                case JobClass.Wizard: // 3x3 광역
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            pattern.Add(new Vector2Int(dx, dy));
                        }
                    }
                    break;
                    
                case JobClass.Priest: // 2x2 범위
                    pattern.Add(new Vector2Int(0, 0));
                    pattern.Add(new Vector2Int(1, 0));
                    pattern.Add(new Vector2Int(0, 1));
                    pattern.Add(new Vector2Int(1, 1));
                    break;
                    
                case JobClass.Rogue: // 대각선
                    pattern.Add(new Vector2Int(1, 1));
                    pattern.Add(new Vector2Int(1, -1));
                    pattern.Add(new Vector2Int(-1, 1));
                    pattern.Add(new Vector2Int(-1, -1));
                    break;
                    
                case JobClass.Sage: // 십자 모양
                    for (int i = -2; i <= 2; i++)
                    {
                        if (i != 0)
                        {
                            pattern.Add(new Vector2Int(i, 0));
                            pattern.Add(new Vector2Int(0, i));
                        }
                    }
                    break;
                    
                case JobClass.Archer: // 직선 관통
                    for (int i = 1; i <= 5; i++)
                    {
                        pattern.Add(new Vector2Int(i, 0));
                    }
                    break;
                    
                case JobClass.Gunner: // 장거리 단일 (특수 처리 필요)
                    // 실제 게임에서는 거리 3-4 타일의 모든 위치
                    pattern.Add(new Vector2Int(3, 0));
                    pattern.Add(new Vector2Int(4, 0));
                    pattern.Add(new Vector2Int(3, 1));
                    pattern.Add(new Vector2Int(3, -1));
                    break;
            }
            
            return pattern;
        }
        
        /// <summary>
        /// 직업 이름 가져오기 (한글)
        /// </summary>
        public static string GetJobClassName(JobClass jobClass)
        {
            switch (jobClass)
            {
                case JobClass.Warrior: return "전사";
                case JobClass.Knight: return "기사";
                case JobClass.Wizard: return "마법사";
                case JobClass.Priest: return "성직자";
                case JobClass.Rogue: return "도적";
                case JobClass.Sage: return "현자";
                case JobClass.Archer: return "궁수";
                case JobClass.Gunner: return "총사";
                default: return "알 수 없음";
            }
        }
        
        /// <summary>
        /// 직업 아이콘 색상 가져오기
        /// </summary>
        public static Color GetJobColor(JobClass jobClass)
        {
            switch (jobClass)
            {
                case JobClass.Warrior: return new Color(0.8f, 0.2f, 0.2f); // 빨강
                case JobClass.Knight: return new Color(0.6f, 0.6f, 0.6f); // 회색
                case JobClass.Wizard: return new Color(0.2f, 0.2f, 0.8f); // 파랑
                case JobClass.Priest: return new Color(1f, 1f, 0.6f); // 노랑
                case JobClass.Rogue: return new Color(0.5f, 0.2f, 0.5f); // 보라
                case JobClass.Sage: return new Color(0.2f, 0.8f, 0.8f); // 청록
                case JobClass.Archer: return new Color(0.2f, 0.6f, 0.2f); // 초록
                case JobClass.Gunner: return new Color(0.4f, 0.4f, 0.4f); // 진회색
                default: return Color.white;
            }
        }
    }
}