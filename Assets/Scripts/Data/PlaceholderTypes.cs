using UnityEngine;
using System.Collections.Generic;
using GuildMaster.Data;
using TacticalTileGame.Data;

/// <summary>
/// CSV Editor에서 사용하는 삭제된 타입들의 플레이스홀더 클래스들
/// </summary>

[System.Serializable]
public class CSVCharacter
{
    public string characterID;
    public string id; // CSV에서 사용하는 id 필드
    public string characterName;
    public string name; // CSV에서 사용하는 name 필드
    public JobClass jobClass;
    public int level;
    public CharacterRarity rarity;
    public int baseHP;
    public int baseMP;
    public int baseAttack;
    public int baseDefense;
    public int baseMagicPower;
    public int baseSpeed;
    public float baseCritRate;
    public float baseCritDamage;
    public float baseAccuracy;
    public float baseEvasion;
    
    // CSV Editor에서 사용하는 추가 필드들
    public float critRate;
    public float critDamage;
    public float accuracy;
    public float evasion;
    public string skill1Id;
    public string skill2Id;
    public string skill3Id;
    public List<int> skillIDs;
    public string description;
}

// CharacterDatabase 클래스 제거됨 - CharacterDatabase 생성 기능만 제거, TacticalCharacterData는 유지

public static class JobClassSystem
{
    public static string GetJobClassName(JobClass jobClass)
    {
        return jobClass.ToString();
    }
}

/// <summary>
/// CharacterRarity와 Rarity 간 변환 유틸리티
/// </summary>
public static class RarityConverter
{
    public static Rarity ToRarity(CharacterRarity characterRarity)
    {
        return (Rarity)(int)characterRarity;
    }
    
    public static CharacterRarity ToCharacterRarity(Rarity rarity)
    {
        return (CharacterRarity)(int)rarity;
    }
}

/// <summary>
/// CharacterDataSO와 CSVCharacter 간 변환 유틸리티
/// </summary>
public static class CharacterConverter
{
    public static CSVCharacter FromCharacterDataSO(CharacterDataSO so)
    {
        if (so == null) return null;
        
        return new CSVCharacter
        {
            id = so.id,
            characterID = so.id,
            name = so.characterName,
            characterName = so.characterName,
            jobClass = so.jobClass,
            level = so.level,
            rarity = RarityConverter.ToCharacterRarity(so.rarity),
            baseHP = so.baseHP,
            baseMP = so.baseMP,
            baseAttack = so.baseAttack,
            baseDefense = so.baseDefense,
            baseMagicPower = so.baseMagicPower,
            baseSpeed = so.baseSpeed,
            critRate = so.critRate,
            critDamage = so.critDamage,
            accuracy = so.accuracy,
            evasion = so.evasion,
            description = so.description ?? ""
        };
    }
    
    public static CharacterData ToCharacterData(CSVCharacter csvChar)
    {
        if (csvChar == null) return null;
        
        var characterData = ScriptableObject.CreateInstance<CharacterData>();
        characterData.id = csvChar.id;
        characterData.characterName = csvChar.characterName;
        characterData.jobClass = csvChar.jobClass;
        characterData.level = csvChar.level;
        characterData.rarity = RarityConverter.ToRarity(csvChar.rarity);
        characterData.baseHP = csvChar.baseHP;
        characterData.baseMP = csvChar.baseMP;
        characterData.baseAttack = csvChar.baseAttack;
        characterData.baseDefense = csvChar.baseDefense;
        characterData.baseMagicPower = csvChar.baseMagicPower;
        characterData.baseSpeed = csvChar.baseSpeed;
        characterData.critRate = csvChar.critRate;
        characterData.critDamage = csvChar.critDamage;
        characterData.accuracy = csvChar.accuracy;
        characterData.evasion = csvChar.evasion;
        characterData.skill1Id = csvChar.skill1Id;
        characterData.skill2Id = csvChar.skill2Id;
        characterData.skill3Id = csvChar.skill3Id;
        characterData.description = csvChar.description;
        
        return characterData;
    }
    
    public static CSVCharacter FromCharacterData(CharacterData charData)
    {
        if (charData == null) return null;
        
        return new CSVCharacter
        {
            id = charData.id,
            characterID = charData.id,
            name = charData.characterName,
            characterName = charData.characterName,
            jobClass = charData.jobClass,
            level = charData.level,
            rarity = RarityConverter.ToCharacterRarity(charData.rarity),
            baseHP = charData.baseHP,
            baseMP = charData.baseMP,
            baseAttack = charData.baseAttack,
            baseDefense = charData.baseDefense,
            baseMagicPower = charData.baseMagicPower,
            baseSpeed = charData.baseSpeed,
            critRate = charData.critRate,
            critDamage = charData.critDamage,
            accuracy = charData.accuracy,
            evasion = charData.evasion,
            skill1Id = charData.skill1Id,
            skill2Id = charData.skill2Id,
            skill3Id = charData.skill3Id,
            description = charData.description ?? ""
        };
    }
    
    public static CharacterDataSO ToCharacterDataSO(CSVCharacter csvChar)
    {
        if (csvChar == null) return null;
        
        var so = ScriptableObject.CreateInstance<CharacterDataSO>();
        so.id = csvChar.id;
        so.characterName = csvChar.characterName;
        so.jobClass = csvChar.jobClass;
        so.level = csvChar.level;
        so.rarity = RarityConverter.ToRarity(csvChar.rarity);
        so.baseHP = csvChar.baseHP;
        so.baseMP = csvChar.baseMP;
        so.baseAttack = csvChar.baseAttack;
        so.baseDefense = csvChar.baseDefense;
        so.baseMagicPower = csvChar.baseMagicPower;
        so.baseSpeed = csvChar.baseSpeed;
        so.critRate = csvChar.critRate;
        so.critDamage = csvChar.critDamage;
        so.accuracy = csvChar.accuracy;
        so.evasion = csvChar.evasion;
        so.description = csvChar.description ?? "";
        
        return so;
    }
}

// StoryDialogueDataSO 플레이스홀더
[CreateAssetMenu(fileName = "StoryDialogue", menuName = "Twelve Game/Story/Dialogue (Legacy)")]
public class StoryDialogueDataSO : ScriptableObject
{
    public string dialogueId;
    public string chapterId;
    public string sceneId;
    
    public void InitializeFromCSV(string csvLine)
    {
        // CSV 파싱 로직 플레이스홀더
        var values = csvLine.Split(',');
        if (values.Length >= 3)
        {
            dialogueId = values[0];
            chapterId = values[1];
            sceneId = values[2];
        }
    }
}

// StoryCharacterSO 플레이스홀더
[CreateAssetMenu(fileName = "StoryCharacter", menuName = "Twelve Game/Story/Character (Legacy)")]
public class StoryCharacterSO : ScriptableObject
{
    public string characterId;
    public string characterName;
    
    public void InitializeFromCSV(string csvLine)
    {
        // CSV 파싱 로직 플레이스홀더
        var values = csvLine.Split(',');
        if (values.Length >= 2)
        {
            characterId = values[0];
            characterName = values[1];
        }
    }
}

// StoryDialogueSO 플레이스홀더
[CreateAssetMenu(fileName = "StoryDialogue", menuName = "Twelve Game/Story/Simple Dialogue (Legacy)")]
public class StoryDialogueSO : ScriptableObject
{
    public string dialogueId;
    public string chapterId;
    
    public void InitializeFromCSV(string csvLine)
    {
        // CSV 파싱 로직 플레이스홀더
        var values = csvLine.Split(',');
        if (values.Length >= 2)
        {
            dialogueId = values[0];
            chapterId = values[1];
        }
    }
} 