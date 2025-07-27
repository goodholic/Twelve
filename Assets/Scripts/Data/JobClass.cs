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
        Sage = 6,       // 현자 - 만능형
        Archer = 7,     // 궁수 - 원거리 물리 공격
        Gunner = 8,     // 총사 - 원거리 화기 공격
        All = 999       // 모든 직업
    }
}

/// <summary>
/// 전술 게임용 캐릭터 타입들 (TacticalTileGame.Data 네임스페이스)
/// </summary>
namespace TacticalTileGame.Data
{
    /// <summary>
    /// 전술 게임용 캐릭터 클래스 (JobClass와 동일하지만 네임스페이스 호환성을 위해)
    /// </summary>
    public enum CharacterClass
    {
        None = 0,
        Warrior = 1,
        Knight = 2,
        Mage = 3,
        Priest = 4,
        Rogue = 5,
        Sage = 6,
        Archer = 7,
        Gunner = 8,
        All = 999
    }
    
    /// <summary>
    /// 전술 게임용 캐릭터 레어도
    /// </summary>
    public enum CharacterRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
} 