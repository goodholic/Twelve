using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// CSV 파일로부터 CharacterData ScriptableObject를 자동 생성하는 에디터 도구
/// Tools > Character Tools > CSV to ScriptableObject Converter 메뉴로 접근
/// </summary>
public class CSVToCharacterDataTool : EditorWindow
{
    [MenuItem("Tools/Character Tools/CSV to ScriptableObject Converter")]
    public static void ShowWindow()
    {
        GetWindow<CSVToCharacterDataTool>("CSV to CharacterData");
    }

    private string csvFolderPath = "Assets/CSV";
    private string outputFolderPath = "Assets/Prefabs/Data/Characters";
    private bool createFolderStructure = true;
    private bool overwriteExisting = false;
    
    private Vector2 scrollPosition;
    private List<string> csvFiles = new List<string>();
    private List<bool> selectedFiles = new List<bool>();

    private void OnEnable()
    {
        RefreshCSVFilesList();
    }

    private void OnGUI()
    {
        GUILayout.Label("CSV to CharacterData ScriptableObject Converter", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 경로 설정
        EditorGUILayout.LabelField("설정", EditorStyles.boldLabel);
        csvFolderPath = EditorGUILayout.TextField("CSV 폴더 경로:", csvFolderPath);
        outputFolderPath = EditorGUILayout.TextField("출력 폴더 경로:", outputFolderPath);
        
        GUILayout.Space(5);
        createFolderStructure = EditorGUILayout.Toggle("폴더 구조 생성", createFolderStructure);
        overwriteExisting = EditorGUILayout.Toggle("기존 파일 덮어쓰기", overwriteExisting);

        GUILayout.Space(10);

        // 새로고침 버튼
        if (GUILayout.Button("CSV 파일 목록 새로고침"))
        {
            RefreshCSVFilesList();
        }

        GUILayout.Space(10);

        // CSV 파일 목록
        EditorGUILayout.LabelField("변환할 CSV 파일 선택", EditorStyles.boldLabel);
        
        if (csvFiles.Count == 0)
        {
            EditorGUILayout.HelpBox("CSV 폴더에서 캐릭터 관련 CSV 파일을 찾을 수 없습니다.", MessageType.Warning);
        }
        else
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            
            for (int i = 0; i < csvFiles.Count; i++)
            {
                selectedFiles[i] = EditorGUILayout.Toggle(Path.GetFileName(csvFiles[i]), selectedFiles[i]);
            }
            
            EditorGUILayout.EndScrollView();
            
            GUILayout.Space(10);
            
            // 전체 선택/해제 버튼
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("전체 선택"))
            {
                for (int i = 0; i < selectedFiles.Count; i++)
                {
                    selectedFiles[i] = true;
                }
            }
            if (GUILayout.Button("전체 해제"))
            {
                for (int i = 0; i < selectedFiles.Count; i++)
                {
                    selectedFiles[i] = false;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(20);

        // 변환 실행 버튼
        GUI.enabled = HasSelectedFiles();
        if (GUILayout.Button("선택된 CSV 파일들을 ScriptableObject로 변환", GUILayout.Height(30)))
        {
            ConvertSelectedCSVFiles();
        }
        GUI.enabled = true;

        GUILayout.Space(10);

        // 도움말
        EditorGUILayout.HelpBox(
            "이 도구는 CSV 파일의 캐릭터 데이터를 CharacterData ScriptableObject로 변환합니다.\n\n" +
            "지원되는 CSV 형식:\n" +
            "- ally_one_star_characters.csv\n" +
            "- ally_two_star_characters.csv\n" +
            "- ally_three_star_characters.csv\n" +
            "- enemy_one_star_characters.csv (등)\n\n" +
            "CSV 컬럼: 이름, 초기 별, 종족, 공격력, 공격속도, 공격범위, 최대 HP, 이동속도, 공격 타입, 광역공격, 비용",
            MessageType.Info);
    }

    private void RefreshCSVFilesList()
    {
        csvFiles.Clear();
        selectedFiles.Clear();

        if (!Directory.Exists(csvFolderPath))
        {
            Debug.LogWarning($"CSV 폴더가 존재하지 않습니다: {csvFolderPath}");
            return;
        }

        string[] allCsvFiles = Directory.GetFiles(csvFolderPath, "*.csv");
        
        foreach (string file in allCsvFiles)
        {
            string fileName = Path.GetFileName(file).ToLower();
            // 캐릭터 관련 CSV 파일만 필터링
            if (fileName.Contains("character") || fileName.Contains("ally") || fileName.Contains("enemy"))
            {
                csvFiles.Add(file);
                selectedFiles.Add(false);
            }
        }

        Debug.Log($"발견된 캐릭터 CSV 파일: {csvFiles.Count}개");
    }

    private bool HasSelectedFiles()
    {
        foreach (bool selected in selectedFiles)
        {
            if (selected) return true;
        }
        return false;
    }

    private void ConvertSelectedCSVFiles()
    {
        int convertedCount = 0;
        int skippedCount = 0;
        int errorCount = 0;

        for (int i = 0; i < csvFiles.Count; i++)
        {
            if (!selectedFiles[i]) continue;

            try
            {
                string csvPath = csvFiles[i];
                string fileName = Path.GetFileNameWithoutExtension(csvPath);
                
                Debug.Log($"CSV 변환 시작: {fileName}");
                
                var characters = ParseCSVFile(csvPath);
                
                if (characters.Count == 0)
                {
                    Debug.LogWarning($"CSV 파일에서 유효한 캐릭터 데이터를 찾을 수 없습니다: {fileName}");
                    skippedCount++;
                    continue;
                }

                CreateCharacterDataAssets(characters, fileName);
                convertedCount++;
                
                Debug.Log($"변환 완료: {fileName} ({characters.Count}개 캐릭터)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CSV 변환 중 오류 발생: {csvFiles[i]}\n{e.Message}");
                errorCount++;
            }
        }

        AssetDatabase.Refresh();
        
        string message = $"변환 완료!\n성공: {convertedCount}개\n건너뜀: {skippedCount}개\n오류: {errorCount}개";
        EditorUtility.DisplayDialog("변환 완료", message, "확인");
    }

    private List<CharacterCSVData> ParseCSVFile(string csvPath)
    {
        List<CharacterCSVData> characters = new List<CharacterCSVData>();
        
        string[] lines = File.ReadAllLines(csvPath);
        
        if (lines.Length < 2)
        {
            Debug.LogWarning($"CSV 파일이 비어있거나 헤더만 있습니다: {csvPath}");
            return characters;
        }

        // 헤더 라인 분석 (첫 번째 라인)
        string[] headers = ParseCSVLine(lines[0]);
        
        // 데이터 라인들 처리 (두 번째 라인부터)
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = ParseCSVLine(line);
            
            if (values.Length < headers.Length)
            {
                Debug.LogWarning($"CSV 라인 {i+1}: 컬럼 수가 부족합니다. 건너뜁니다.");
                continue;
            }

            try
            {
                CharacterCSVData character = ParseCharacterData(headers, values);
                if (character != null)
                {
                    characters.Add(character);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CSV 라인 {i+1} 파싱 오류: {e.Message}");
            }
        }

        return characters;
    }

    private string[] ParseCSVLine(string line)
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
                result.Add(currentField.Trim());
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        result.Add(currentField.Trim());
        return result.ToArray();
    }

    private CharacterCSVData ParseCharacterData(string[] headers, string[] values)
    {
        CharacterCSVData character = new CharacterCSVData();

        for (int i = 0; i < headers.Length && i < values.Length; i++)
        {
            string header = headers[i].Trim().ToLower();
            string value = values[i].Trim();

            if (string.IsNullOrEmpty(value)) continue;

            try
            {
                switch (header)
                {
                    case "이름":
                    case "name":
                        character.name = value;
                        break;
                    case "초기 별":
                    case "star":
                    case "초기별":
                        if (int.TryParse(value, out int star))
                        {
                            character.star = star;
                        }
                        break;
                    case "종족":
                    case "race":
                        character.race = value;
                        break;
                    case "공격력":
                    case "attack":
                    case "attackpower":
                        if (float.TryParse(value, out float attack))
                        {
                            character.attackPower = attack;
                        }
                        break;
                    case "공격속도":
                    case "attackspeed":
                    case "공격 속도":
                        if (float.TryParse(value, out float attackSpeed))
                        {
                            character.attackSpeed = attackSpeed;
                        }
                        break;
                    case "공격범위":
                    case "range":
                    case "attackrange":
                    case "공격 범위":
                        if (float.TryParse(value, out float range))
                        {
                            character.attackRange = range;
                        }
                        break;
                    case "최대 hp":
                    case "maxhp":
                    case "hp":
                    case "health":
                        if (float.TryParse(value, out float hp))
                        {
                            character.maxHP = hp;
                        }
                        break;
                    case "이동속도":
                    case "movespeed":
                    case "이동 속도":
                        if (float.TryParse(value, out float moveSpeed))
                        {
                            character.moveSpeed = moveSpeed;
                        }
                        break;
                    case "공격 타입":
                    case "attacktype":
                    case "공격타입":
                        character.attackType = value;
                        break;
                    case "광역공격":
                    case "areaattack":
                    case "광역 공격":
                        character.isAreaAttack = (value == "예" || value == "true" || value == "1");
                        break;
                    case "비용":
                    case "cost":
                        if (int.TryParse(value, out int cost))
                        {
                            character.cost = cost;
                        }
                        break;
                    case "가중치":
                    case "weight":
                        if (float.TryParse(value, out float weight))
                        {
                            character.weight = weight;
                        }
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"컬럼 '{header}' 파싱 오류: {e.Message}");
            }
        }

        // 필수 데이터 검증
        if (string.IsNullOrEmpty(character.name))
        {
            Debug.LogWarning("캐릭터 이름이 비어있습니다. 건너뜁니다.");
            return null;
        }

        return character;
    }

    private void CreateCharacterDataAssets(List<CharacterCSVData> characters, string baseName)
    {
        // 출력 폴더 생성
        if (createFolderStructure && !Directory.Exists(outputFolderPath))
        {
            Directory.CreateDirectory(outputFolderPath);
        }

        foreach (var csvData in characters)
        {
            CharacterData characterData = ScriptableObject.CreateInstance<CharacterData>();
            
            // CSV 데이터를 CharacterData로 변환
            characterData.characterName = csvData.name;
            characterData.attackPower = csvData.attackPower;
            characterData.attackSpeed = csvData.attackSpeed;
            characterData.attackRange = csvData.attackRange;
            characterData.range = csvData.attackRange; // 동일한 값
            characterData.health = csvData.maxHP;
            characterData.maxHealth = csvData.maxHP;
            characterData.maxHP = csvData.maxHP; // 동일한 값
            characterData.moveSpeed = csvData.moveSpeed;
            characterData.cost = csvData.cost;
            characterData.isAreaAttack = csvData.isAreaAttack;
            characterData.areaAttackRadius = csvData.isAreaAttack ? 1.5f : 0f;

            // 별 등급 설정
            switch (csvData.star)
            {
                case 1:
                    characterData.star = CharacterStar.OneStar;
                    characterData.initialStar = CharacterStar.OneStar;
                    break;
                case 2:
                    characterData.star = CharacterStar.TwoStar;
                    characterData.initialStar = CharacterStar.TwoStar;
                    break;
                case 3:
                    characterData.star = CharacterStar.ThreeStar;
                    characterData.initialStar = CharacterStar.ThreeStar;
                    break;
            }

            // 종족 설정
            switch (csvData.race.ToLower())
            {
                case "human":
                case "인간":
                    characterData.race = CharacterRace.Human;
                    characterData.tribe = RaceType.Human;
                    break;
                case "orc":
                case "오크":
                    characterData.race = CharacterRace.Orc;
                    characterData.tribe = RaceType.Orc;
                    break;
                case "elf":
                case "엘프":
                    characterData.race = CharacterRace.Elf;
                    characterData.tribe = RaceType.Elf;
                    break;
                case "undead":
                case "언데드":
                    characterData.race = CharacterRace.Undead;
                    characterData.tribe = RaceType.Undead;
                    break;
                default:
                    characterData.race = CharacterRace.Human;
                    characterData.tribe = RaceType.Human;
                    break;
            }

            // 공격 타입 설정
            switch (csvData.attackType.ToLower())
            {
                case "melee":
                case "근접":
                    characterData.rangeType = RangeType.Melee;
                    break;
                case "ranged":
                case "원거리":
                    characterData.rangeType = RangeType.Ranged;
                    break;
                case "longrange":
                case "장거리":
                    characterData.rangeType = RangeType.LongRange;
                    break;
                default:
                    characterData.rangeType = RangeType.Ranged;
                    break;
            }

            // 공격 대상 타입 설정 (기본값)
            characterData.attackTargetType = AttackTargetType.Both;
            characterData.attackShapeType = csvData.isAreaAttack ? AttackShapeType.Circle : AttackShapeType.Single;

            // 프리팹 이름 설정
            characterData.prefabName = csvData.name;

            // 파일명 생성 (안전한 파일명으로 변환)
            string safeFileName = MakeSafeFileName(csvData.name);
            string assetPath = Path.Combine(outputFolderPath, $"{safeFileName}.asset");

            // 기존 파일 덮어쓰기 확인
            if (!overwriteExisting && File.Exists(assetPath))
            {
                Debug.LogWarning($"파일이 이미 존재합니다. 건너뜀: {assetPath}");
                continue;
            }

            // ScriptableObject 저장
            AssetDatabase.CreateAsset(characterData, assetPath);
            Debug.Log($"CharacterData 생성됨: {assetPath}");
        }
    }

    private string MakeSafeFileName(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string safeName = fileName;
        
        foreach (char c in invalidChars)
        {
            safeName = safeName.Replace(c, '_');
        }
        
        return safeName;
    }

    [System.Serializable]
    private class CharacterCSVData
    {
        public string name = "";
        public int star = 1;
        public string race = "Human";
        public float attackPower = 10f;
        public float attackSpeed = 1f;
        public float attackRange = 3f;
        public float maxHP = 100f;
        public float moveSpeed = 1f;
        public string attackType = "Ranged";
        public bool isAreaAttack = false;
        public int cost = 10;
        public float weight = 1f;
    }
}