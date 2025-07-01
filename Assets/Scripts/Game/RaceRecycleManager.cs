using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Data;

public class RaceRecycleManager : MonoBehaviour
{
    [Header("종족 재활용 설정")]
    public List<CharacterRace> recyclableRaces = new List<CharacterRace>();
    public float recycleBonus = 0.1f; // 10% 보너스
    
    [Header("재활용 규칙")]
    public int minCharactersForRecycle = 3;
    public int maxRecyclePerTurn = 5;
    
    private Dictionary<CharacterRace, List<CharacterData>> recyclePool = new Dictionary<CharacterRace, List<CharacterData>>();
    
    private void Start()
    {
        InitializeRecyclePool();
    }
    
    private void InitializeRecyclePool()
    {
        foreach (CharacterRace race in System.Enum.GetValues(typeof(CharacterRace)))
        {
            recyclePool[race] = new List<CharacterData>();
        }
    }
    
    public bool CanRecycleRace(CharacterRace race)
    {
        return recyclableRaces.Contains(race) && 
               recyclePool[race].Count >= minCharactersForRecycle;
    }
    
    public void AddToRecyclePool(CharacterData character)
    {
        if (character != null && recyclableRaces.Contains(character.race))
        {
            recyclePool[character.race].Add(character);
            Debug.Log($"{character.race} 종족 캐릭터를 재활용 풀에 추가했습니다.");
        }
    }
    
    public void RemoveFromRecyclePool(CharacterData character)
    {
        if (character != null && recyclePool.ContainsKey(character.race))
        {
            recyclePool[character.race].Remove(character);
        }
    }
    
    public List<CharacterData> GetRecyclableCharacters(CharacterRace race)
    {
        if (recyclePool.ContainsKey(race))
        {
            return new List<CharacterData>(recyclePool[race]);
        }
        return new List<CharacterData>();
    }
    
    public CharacterData RecycleCharacter(CharacterRace race)
    {
        if (!CanRecycleRace(race)) return null;
        
        var availableCharacters = recyclePool[race];
        if (availableCharacters.Count > 0)
        {
            CharacterData recycledCharacter = availableCharacters[0];
            availableCharacters.RemoveAt(0);
            
            // 재활용 보너스 적용
            ApplyRecycleBonus(recycledCharacter);
            
            return recycledCharacter;
        }
        
        return null;
    }
    
    private void ApplyRecycleBonus(CharacterData character)
    {
        character.attackPower *= (1f + recycleBonus);
        character.health *= (1f + recycleBonus);
        character.maxHealth = character.health;
        character.maxHP = character.health;
        
        Debug.Log($"재활용 보너스가 적용되었습니다: {character.characterName}");
    }
    
    public int GetRecyclePoolCount(CharacterRace race)
    {
        return recyclePool.ContainsKey(race) ? recyclePool[race].Count : 0;
    }
    
    public void ClearRecyclePool(CharacterRace race)
    {
        if (recyclePool.ContainsKey(race))
        {
            recyclePool[race].Clear();
        }
    }
    
    public void ClearAllRecyclePools()
    {
        foreach (var pool in recyclePool.Values)
        {
            pool.Clear();
        }
    }
} 