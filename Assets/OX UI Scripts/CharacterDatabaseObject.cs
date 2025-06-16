using UnityEngine;

/// <summary>
/// ScriptableObject 기반의 캐릭터 데이터베이스.
/// .asset 형태로 프로젝트에 저장하여 사용한다.
/// </summary>
[CreateAssetMenu(fileName = "NewCharacterDatabase", menuName = "MyGame/Character Database (ScriptableObject)")]
public class CharacterDatabaseObject : ScriptableObject
{
    [Header("Character Data List (.asset)")]
    [Tooltip("각 인덱스에 대응하는 캐릭터 설정 정보들")]
    public CharacterData[] characters;
    
    [Header("현재 등록된 캐릭터 (호환성)")]
    [Tooltip("CharacterDatabase와의 호환성을 위한 속성")]
    public CharacterData[] currentRegisteredCharacters;
}
