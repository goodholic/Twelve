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

/// <summary>
/// 공격 형태 타입
/// </summary>
[System.Serializable]
public enum AttackShapeType
{
    Single,         // 단일 타겟
    Circle,         // 원형 범위
    Rectangle,      // 사각형 범위
    Sector          // 부채꼴 범위
}

/// <summary>
/// 캐릭터 종족 (CharacterData용)
/// </summary>
[System.Serializable]
public enum CharacterRace
{
    Human,          // 인간족
    Orc,            // 오크족
    Elf,            // 엘프족
    Undead          // 언데드족
}

/// <summary>
/// 캐릭터 별 등급
/// </summary>
[System.Serializable]
public enum CharacterStar
{
    OneStar = 1,    // 1성
    TwoStar = 2,    // 2성
    ThreeStar = 3   // 3성
}

/// <summary>
/// 공격 범위 타입
/// </summary>
[System.Serializable]
public enum RangeType
{
    Melee,          // 근접 공격
    Ranged,         // 원거리 공격
    LongRange       // 장거리 공격
}