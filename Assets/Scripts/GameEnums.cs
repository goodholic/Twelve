using UnityEngine;

/// <summary>
/// 게임 내에서 사용하는 공통 열거형(enum) 모음
/// </summary>

/// <summary>
/// 라우트 타입 - 3라인 시스템 (게임 기획서 참조)
/// </summary>
[System.Serializable]
public enum RouteType
{
    Left = 0,    // 왼쪽 라인
    Center = 1,  // 중앙 라인
    Right = 2    // 오른쪽 라인
}

/// <summary>
/// 인종 타입 - 캐릭터/몬스터 인종 구분
/// </summary>
[System.Serializable]
public enum RaceType
{
    Human,      // 인간족
    Orc,        // 오크족
    Elf,        // 엘프족
    Undead,     // 언데드족
    Etc         // 기타
}

/// <summary>
/// 공격 대상 타입
/// </summary>
[System.Serializable]
public enum AttackTargetType
{
    Character,      // 캐릭터만 (CharacterOnly와 호환)
    Monster,        // 몬스터만 (MonsterOnly와 호환)
    Both,           // 캐릭터와 몬스터 둘 다
    CastleOnly,     // 성만
    All             // 모든 타겟
}