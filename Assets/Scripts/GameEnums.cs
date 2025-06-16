using UnityEngine;

/// <summary>
/// 게임 내에서 사용하는 공통 인터페이스와 열거형(enum) 모음
/// </summary>

/// <summary>
/// 데미지를 받을 수 있는 오브젝트들이 구현해야 하는 인터페이스
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage);
}

/// <summary>
/// 라우트 타입 - 3라인 시스템 (게임 기획서 참조)
/// </summary>
public enum RouteType
{
    Left = 0,    // 왼쪽 라인
    Center = 1,  // 중앙 라인
    Right = 2    // 오른쪽 라인
}

/// <summary>
/// 공격 범위 타입
/// </summary>
public enum RangeType
{
    Melee,      // 근거리
    Ranged,     // 원거리
    LongRange   // 장거리
}

/// <summary>
/// 인종 타입 - 캐릭터/몬스터 인종 구분
/// </summary>
public enum RaceType
{
    Human,      // 인간족
    Orc,        // 오크족
    Elf,        // 엘프족
    Undead,     // 언데드족
    Etc         // 기타
}

using UnityEngine;

/// <summary>
/// 라인/경로 타입 정의
/// 게임 기획서: 3라인 시스템 (왼쪽/중앙/오른쪽)
/// </summary>
[System.Serializable]
public enum RouteType
{
    Left,    // 왼쪽 라인
    Center,  // 중앙 라인
    Right    // 오른쪽 라인
}

/// <summary>
/// 종족 타입 정의
/// </summary>
[System.Serializable]
public enum RaceType
{
    Human,
    Orc,
    Elf,
}