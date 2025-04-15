using UnityEngine;

/// <summary>
/// 여러 캐릭터 인덱스별 데이터를 한번에 관리하기 위한 Database 스크립트.
/// 인스펙터에서 CharacterData[]를 편집할 수 있음.
/// </summary>
public class CharacterDatabase : MonoBehaviour
{
    [Header("Character Data List")]
    [Tooltip("각 인덱스에 대응하는 캐릭터 설정 정보들")]
    public CharacterData[] characters;

    // 필요하다면, 배열 크기 제한(예: 4개나 10개) 등을 둘 수 있으나,
    // 여기서는 자유롭게 배열 크기를 조절 가능하게 두었습니다.
}
