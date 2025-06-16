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