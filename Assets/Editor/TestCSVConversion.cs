#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// CSV 변환 도구 테스트용 스크립트
/// </summary>
public class TestCSVConversion
{
    [MenuItem("Tools/Test CSV Conversion")]
    public static void TestConversion()
    {
        string testCSVPath = "Assets/CSV/ally_one_star_characters.csv";
        string outputDir = "Assets/Prefabs/Data/Characters";
        
        if (!File.Exists(testCSVPath))
        {
            Debug.LogError($"Test CSV file not found: {testCSVPath}");
            return;
        }

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        try
        {
            string[] lines = File.ReadAllLines(testCSVPath);
            if (lines.Length <= 1)
            {
                Debug.LogError("CSV file has no data rows");
                return;
            }

            // 첫 번째 데이터 라인만 테스트
            string testLine = lines[1];
            Debug.Log($"Testing line: {testLine}");

            string[] values = testLine.Split(',');
            
            if (values.Length < 11)
            {
                Debug.LogError($"Not enough columns. Expected 11, got {values.Length}");
                return;
            }

            // CharacterData 생성 테스트
            CharacterData testCharacter = ScriptableObject.CreateInstance<CharacterData>();
            
            testCharacter.characterName = values[0].Trim();
            testCharacter.initialStar = int.Parse(values[1].Trim()) switch
            {
                1 => CharacterStar.OneStar,
                2 => CharacterStar.TwoStar,
                3 => CharacterStar.ThreeStar,
                _ => CharacterStar.OneStar
            };
            
            testCharacter.race = values[2].Trim() switch
            {
                "Human" => CharacterRace.Human,
                "Orc" => CharacterRace.Orc,
                "Elf" => CharacterRace.Elf,
                _ => CharacterRace.Human
            };

            testCharacter.attackPower = float.Parse(values[3].Trim());
            testCharacter.attackSpeed = float.Parse(values[4].Trim());
            testCharacter.attackRange = float.Parse(values[5].Trim());
            testCharacter.maxHP = float.Parse(values[6].Trim());
            testCharacter.moveSpeed = float.Parse(values[7].Trim());
            
            testCharacter.rangeType = values[8].Trim() switch
            {
                "Melee" => RangeType.Melee,
                "Ranged" => RangeType.Ranged,
                "LongRange" => RangeType.LongRange,
                _ => RangeType.Ranged
            };

            testCharacter.isAreaAttack = values[9].Trim() == "예";
            testCharacter.cost = int.Parse(values[10].Trim());

            // 테스트 에셋 저장
            string outputPath = Path.Combine(outputDir, $"TEST_{testCharacter.characterName}.asset");
            outputPath = outputPath.Replace('\\', '/');
            
            AssetDatabase.CreateAsset(testCharacter, outputPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Successfully created test CharacterData: {outputPath}");
            Debug.Log($"Character: {testCharacter.characterName}, Race: {testCharacter.race}, Attack: {testCharacter.attackPower}");
            
            EditorUtility.DisplayDialog("Test Successful", 
                $"테스트 성공! {testCharacter.characterName} 캐릭터 데이터가 생성되었습니다.", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}");
            EditorUtility.DisplayDialog("Test Failed", 
                $"테스트 실패: {e.Message}", "OK");
        }
    }
}
#endif