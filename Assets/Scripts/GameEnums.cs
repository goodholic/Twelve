using UnityEngine;

/// <summary>
/// 게임 내에서 사용하는 공통 열거형(enum) 모음
/// </summary>

// 인종 타입 - 캐릭터/몬스터 인종 구분
public enum RaceType
{
    Human,      // 인간족
    Orc,        // 오크족
    Elf,        // 엘프족
    Undead,     // 언데드족
    Etc         // 기타
}

// 다른 열거형들은 Character.cs에 이미 정의되어 있으므로 여기서는 제거합니다. 