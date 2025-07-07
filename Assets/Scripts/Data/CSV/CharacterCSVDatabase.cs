using UnityEngine;
using System.Collections.Generic;
using System.IO;
using GuildMaster.Data;

public class CharacterCSVDatabase : MonoBehaviour
{
    [Header("CSV 파일 경로")]
    public string csvFileName = "character_data.csv";
    
    [Header("캐릭터 데이터")]
    public List<GuildMaster.Data.CharacterData> characters = new List<GuildMaster.Data.CharacterData>();
    
    private void Start()
    {
        LoadCharacterDataFromCSV();
    }
    
    public void LoadCharacterDataFromCSV()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, csvFileName);
        
        if (!File.Exists(filePath))
        {
            Debug.LogError($"CSV 파일을 찾을 수 없습니다: {filePath}");
            return;
        }
        
        string[] lines = File.ReadAllLines(filePath);
        
        // 첫 번째 줄은 헤더이므로 건너뜀
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            
            if (values.Length < 10) continue; // 최소 필드 수 확인
            
            GuildMaster.Data.CharacterData character = new GuildMaster.Data.CharacterData();
            
            // CSV 데이터 파싱
            character.characterName = values[0];
            if (int.TryParse(values[1], out int index)) character.characterIndex = index;
            if (int.TryParse(values[2], out int race)) character.race = race == 0 ? "Human" : race == 1 ? "Elf" : "Dwarf";
            if (int.TryParse(values[3], out int star)) character.star = star;
            if (float.TryParse(values[4], out float attackPower)) character.attackPower = (int)attackPower;
            if (float.TryParse(values[5], out float attackSpeed)) character.attackSpeed = (int)attackSpeed;
            if (float.TryParse(values[6], out float health)) character.health = (int)health;
            if (int.TryParse(values[7], out int cost)) character.cost = cost;
            
            characters.Add(character);
        }
        
        Debug.Log($"CSV에서 {characters.Count}개의 캐릭터 데이터를 로드했습니다.");
    }
    
    public GuildMaster.Data.CharacterData GetCharacterByName(string name)
    {
        return characters.Find(c => c.characterName == name);
    }
    
    public GuildMaster.Data.CharacterData GetCharacterByIndex(int index)
    {
        return characters.Find(c => c.characterIndex == index);
    }
    
    public List<GuildMaster.Data.CharacterData> GetCharacterDataList()
    {
        return characters;
    }
    
    public List<GuildMaster.Data.CharacterData> GetCharactersByStar(int star)
    {
        return characters.FindAll(c => c.star == star);
    }
    
    public GuildMaster.Data.CharacterData GetRandomCharacterByStar(int star)
    {
        var charactersWithStar = GetCharactersByStar(star);
        if (charactersWithStar.Count > 0)
        {
            return charactersWithStar[Random.Range(0, charactersWithStar.Count)];
        }
        return null;
    }
} 