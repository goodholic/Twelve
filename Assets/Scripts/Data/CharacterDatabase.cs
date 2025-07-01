using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle;
using GuildMaster.Data;

[CreateAssetMenu(fileName = "Character Database", menuName = "Character Database")]
public class CharacterDatabase : ScriptableObject
{
    [Header("캐릭터 목록")]
    public List<CharacterData> characters = new List<CharacterData>();
    
    [Header("등록된 캐릭터들")]
    public CharacterData[] currentRegisteredCharacters = new CharacterData[10];
    
    public CharacterData GetCharacterByName(string name)
    {
        return characters.Find(c => c.characterName == name);
    }
    
    public CharacterData GetCharacterByIndex(int index)
    {
        if (index >= 0 && index < characters.Count)
            return characters[index];
        return null;
    }
    
    public List<CharacterData> GetCharactersByRace(CharacterRace race)
    {
        return characters.FindAll(c => c.race == race);
    }
    
    public List<CharacterData> GetCharactersByStar(int star)
    {
        return characters.FindAll(c => c.star == star);
    }
    
    public List<CharacterData> GetCharacterDataList()
    {
        return characters;
    }
    
    public void Initialize()
    {
        // 데이터베이스 초기화 로직
        Debug.Log($"Character Database initialized with {characters.Count} characters");
    }
    
    public CharacterData GetCharacter(string characterId)
    {
        return characters.Find(c => c.id == characterId || c.characterID == characterId);
    }
    
    public List<CharacterData> GetCharactersByClass(GuildMaster.Data.JobClass jobClass)
    {
        return characters.FindAll(c => c.jobClass == jobClass);
    }
    
    public List<CharacterData> GetCharactersByJobClass(GuildMaster.Data.JobClass jobClass)
    {
        return characters.Where(c => c.jobClass == jobClass).ToList();
    }
} 