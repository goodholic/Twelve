using UnityEngine;
using System.Collections.Generic;
using GuildMaster.Data;

[CreateAssetMenu(fileName = "Character Database Object", menuName = "Character Database Object")]
public class CharacterDatabaseObject : ScriptableObject
{
    [Header("데이터베이스 참조")]
    public CharacterDatabase database;
    
    // 호환성을 위한 characters 프로퍼티
    public List<GuildMaster.Data.CharacterData> characters
    {
        get
        {
            if (database != null)
                return database.GetCharacterDataList();
            return new List<GuildMaster.Data.CharacterData>();
        }
    }
    
    public CharacterDatabase GetDatabase()
    {
        return database;
    }
} 