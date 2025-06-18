#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// CSV 파일을 CharacterData ScriptableObject로 변환하는 도구
/// </summary>
public class CSVToScriptableObjectTool : EditorWindow
{
    [MenuItem("Tools/CSV to ScriptableObject Converter")]
    public static void ShowWindow()
    {
        GetWindow<CSVToScriptableObjectTool>("CSV to ScriptableObject Converter");
    }

    private Vector2 scrollPosition;
    private string csvDirectory = "Assets/CSV";
    private string outputDirectory = "Assets/Prefabs/Data/Characters";
    private List<string> csvFiles = new List<string>();
    private Dictionary<string, bool> selectedFiles = new Dictionary<string, bool>();

    private void OnEnable()
    {
        RefreshCSVFiles();
    }

    private void OnGUI()
    {
        GUILayout.Label("CSV to CharacterData ScriptableObject Converter", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 디렉토리 설정
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        csvDirectory = EditorGUILayout.TextField("CSV Directory:", csvDirectory);
        outputDirectory = EditorGUILayout.TextField("Output Directory:", outputDirectory);

        EditorGUILayout.Space();

        // 새로고침 버튼
        if (GUILayout.Button("Refresh CSV Files"))
        {
            RefreshCSVFiles();
        }

        EditorGUILayout.Space();

        // CSV 파일 목록
        EditorGUILayout.LabelField("Available CSV Files:", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (string csvFile in csvFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(csvFile);
            
            if (!selectedFiles.ContainsKey(csvFile))
                selectedFiles[csvFile] = false;

            EditorGUILayout.BeginHorizontal();
            selectedFiles[csvFile] = EditorGUILayout.Toggle(selectedFiles[csvFile], GUILayout.Width(20));
            EditorGUILayout.LabelField(fileName);
            
            if (GUILayout.Button("Convert This File", GUILayout.Width(120)))
            {
                ConvertSingleFile(csvFile);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // 변환 버튼들
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Convert Selected Files"))
        {
            ConvertSelectedFiles();
        }
        
        if (GUILayout.Button("Convert All Files"))
        {
            ConvertAllFiles();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 도움말
        EditorGUILayout.HelpBox(
            "이 도구는 CSV 파일을 CharacterData ScriptableObject로 변환합니다.\n" +
            "CSV 형식: 이름, 초기별, 종족, 공격력, 공격속도, 공격범위, 최대HP, 이동속도, 공격타입, 광역공격, 비용",
            MessageType.Info);
    }

    private void RefreshCSVFiles()
    {
        csvFiles.Clear();
        selectedFiles.Clear();

        if (Directory.Exists(csvDirectory))
        {
            string[] files = Directory.GetFiles(csvDirectory, "*.csv", SearchOption.AllDirectories);
            csvFiles.AddRange(files);
        }
        else
        {
            Debug.LogWarning($"CSV Directory not found: {csvDirectory}");
        }
    }

    private void ConvertSelectedFiles()
    {
        var filesToConvert = selectedFiles.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
        
        if (filesToConvert.Count == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "선택된 파일이 없습니다.", "OK");
            return;
        }

        foreach (string file in filesToConvert)
        {
            ConvertSingleFile(file);
        }
    }

    private void ConvertAllFiles()
    {
        foreach (string file in csvFiles)
        {
            ConvertSingleFile(file);
        }
    }

    private void ConvertSingleFile(string csvFilePath)
    {
        if (!File.Exists(csvFilePath))
        {
            Debug.LogError($"CSV file not found: {csvFilePath}");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(csvFilePath);
            if (lines.Length <= 1)
            {
                Debug.LogWarning($"CSV file is empty or has no data rows: {csvFilePath}");
                return;
            }

            // 출력 디렉토리 생성
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string fileName = Path.GetFileNameWithoutExtension(csvFilePath);
            int convertedCount = 0;

            // 헤더 스킵하고 데이터 처리
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                CharacterData characterData = ParseCSVLineToCharacterData(line, i);
                if (characterData != null)
                {
                    string outputPath = Path.Combine(outputDirectory, $"{fileName}_{characterData.characterName}.asset");
                    outputPath = outputPath.Replace('\\', '/');
                    
                    // 기존 파일이 있는지 확인
                    CharacterData existingAsset = AssetDatabase.LoadAssetAtPath<CharacterData>(outputPath);
                    if (existingAsset != null)
                    {
                        // 기존 에셋 업데이트
                        UpdateExistingCharacterData(existingAsset, characterData);
                        EditorUtility.SetDirty(existingAsset);
                    }
                    else
                    {
                        // 새 에셋 생성
                        AssetDatabase.CreateAsset(characterData, outputPath);
                    }
                    
                    convertedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Converted {convertedCount} characters from {fileName} to ScriptableObjects");
            EditorUtility.DisplayDialog("Conversion Complete", 
                $"{fileName}에서 {convertedCount}개의 캐릭터를 변환했습니다.", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error converting {csvFilePath}: {e.Message}");
            EditorUtility.DisplayDialog("Conversion Error", 
                $"변환 중 오류가 발생했습니다: {e.Message}", "OK");
        }
    }

    private CharacterData ParseCSVLineToCharacterData(string csvLine, int lineNumber)
    {
        try
        {
            string[] values = SplitCSVLine(csvLine);
            
            if (values.Length < 11) // 최소 필요한 컬럼 수
            {
                Debug.LogWarning($"Line {lineNumber}: Not enough columns in CSV line");
                return null;
            }

            CharacterData characterData = ScriptableObject.CreateInstance<CharacterData>();
            
            // CSV 컬럼 매핑 (CSV 헤더: 이름,초기 별,종족,공격력,공격속도,공격범위,최대 HP,이동속도,공격 타입,광역공격,비용)
            characterData.characterName = values[0].Trim();
            
            // 초기 별 (1, 2, 3 -> OneStar, TwoStar, ThreeStar)
            if (int.TryParse(values[1].Trim(), out int starValue))
            {
                characterData.initialStar = starValue switch
                {
                    1 => CharacterStar.OneStar,
                    2 => CharacterStar.TwoStar,
                    3 => CharacterStar.ThreeStar,
                    _ => CharacterStar.OneStar
                };
                characterData.star = characterData.initialStar;
            }

            // 종족
            string raceStr = values[2].Trim();
            characterData.race = raceStr.ToLower() switch
            {
                "human" => CharacterRace.Human,
                "orc" => CharacterRace.Orc,
                "elf" => CharacterRace.Elf,
                _ => CharacterRace.Human
            };
            characterData.tribe = characterData.race switch
            {
                CharacterRace.Human => RaceType.Human,
                CharacterRace.Orc => RaceType.Orc,
                CharacterRace.Elf => RaceType.Elf,
                _ => RaceType.Human
            };

            // 공격력
            if (float.TryParse(values[3].Trim(), out float attackPower))
                characterData.attackPower = attackPower;

            // 공격속도
            if (float.TryParse(values[4].Trim(), out float attackSpeed))
                characterData.attackSpeed = attackSpeed;

            // 공격범위
            if (float.TryParse(values[5].Trim(), out float attackRange))
            {
                characterData.attackRange = attackRange;
                characterData.range = attackRange;
            }

            // 최대 HP
            if (float.TryParse(values[6].Trim(), out float maxHP))
            {
                characterData.maxHP = maxHP;
                characterData.maxHealth = maxHP;
                characterData.health = maxHP;
            }

            // 이동속도
            if (float.TryParse(values[7].Trim(), out float moveSpeed))
                characterData.moveSpeed = moveSpeed;

            // 공격 타입 (Melee, Ranged, LongRange 등)
            string attackTypeStr = values[8].Trim();
            characterData.rangeType = attackTypeStr.ToLower() switch
            {
                "melee" => RangeType.Melee,
                "ranged" => RangeType.Ranged,
                "longrange" => RangeType.LongRange,
                _ => RangeType.Ranged
            };

            // 광역공격 (예/아니오)
            string areaAttackStr = values[9].Trim();
            characterData.isAreaAttack = areaAttackStr == "예" || areaAttackStr.ToLower() == "yes";

            // 비용
            if (int.TryParse(values[10].Trim(), out int cost))
                characterData.cost = cost;

            // 기본값 설정
            characterData.level = 1;
            characterData.attackTargetType = AttackTargetType.Both;
            characterData.attackShapeType = AttackShapeType.Single;
            characterData.currentExp = 0f;
            characterData.expToNextLevel = 100f;

            return characterData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing line {lineNumber}: {e.Message}");
            return null;
        }
    }

    private void UpdateExistingCharacterData(CharacterData existing, CharacterData newData)
    {
        existing.characterName = newData.characterName;
        existing.initialStar = newData.initialStar;
        existing.star = newData.star;
        existing.race = newData.race;
        existing.tribe = newData.tribe;
        existing.attackPower = newData.attackPower;
        existing.attackSpeed = newData.attackSpeed;
        existing.attackRange = newData.attackRange;
        existing.range = newData.range;
        existing.maxHP = newData.maxHP;
        existing.maxHealth = newData.maxHealth;
        existing.health = newData.health;
        existing.moveSpeed = newData.moveSpeed;
        existing.rangeType = newData.rangeType;
        existing.isAreaAttack = newData.isAreaAttack;
        existing.cost = newData.cost;
    }

    private string[] SplitCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        result.Add(currentField);
        return result.ToArray();
    }
}
#endif