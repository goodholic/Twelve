using UnityEngine;

namespace GuildMaster.Data
{
    /// <summary>
    /// 캐릭터 직업 클래스
    /// </summary>
    public enum JobClass
    {
        None = 0,       // 없음
        Warrior = 1,    // 전사 - 근접 물리 공격
        Knight = 2,     // 기사 - 방어 특화
        Mage = 3,       // 마법사 - 원거리 마법 공격
        Priest = 4,     // 성직자 - 힐링과 버프
        Rogue = 5,      // 도적 - 빠른 속도와 크리티컬
        Assassin = 6,   // 암살자 - 치명타 특화
        Archer = 7,     // 궁수 - 원거리 물리 공격
        Ranger = 8,     // 레인저 - 원거리 + 자연 마법
        Sage = 9,       // 현자 - 만능형
        Bard = 10,      // 바드 - 지원형
        All = 999       // 모든 직업
    }
} 