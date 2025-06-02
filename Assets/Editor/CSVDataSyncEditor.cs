#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class CSVDataSyncEditor : EditorWindow
{
    [MenuItem("Tools/CSV Data Sync Manager")]
    public static void ShowWindow()
    {
        GetWindow<CSVDataSyncEditor>("CSV Data Sync Manager");
    }

    private Vector2 scrollPosition;
    private bool autoSyncEnabled = false;
    private float lastCheckTime = 0f;
    private float checkInterval = 2f; // 2초마다 체크

    // CSV 파일 경로들
    private string csvFolderPath = "Assets/CSV/";
    
    // 파일 변경 추적을 위한 딕셔너리
    private Dictionary<string, string> fileHashes = new Dictionary<string, string>();

    private void OnEnable()
    {
        // 초기 파일 해시 저장
        UpdateFileHashes();
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (autoSyncEnabled && Time.realtimeSinceStartup - lastCheckTime > checkInterval)
        {
            lastCheckTime = Time.realtimeSinceStartup;
            CheckForFileChanges();
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("CSV ↔ ScriptableObject 동기화 관리", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 자동 동기화 토글
        EditorGUILayout.BeginHorizontal();
        autoSyncEnabled = EditorGUILayout.Toggle("자동 동기화", autoSyncEnabled);
        if (autoSyncEnabled)
        {
            EditorGUILayout.LabelField("(2초마다 파일 변경 체크)", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // CSV → ScriptableObject 섹션
        GUILayout.Label("CSV → ScriptableObject 가져오기", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        if (GUILayout.Button("모든 CSV 파일 가져오기"))
        {
            ImportAllCSVFiles();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("아군 1성 캐릭터 CSV 가져오기"))
        {
            ImportAllyOneStarFromCSV();
        }

        if (GUILayout.Button("적 1성 캐릭터 CSV 가져오기"))
        {
            ImportEnemyOneStarFromCSV();
        }

        if (GUILayout.Button("아군 2성/3성 캐릭터 CSV 가져오기"))
        {
            ImportAllyStarsFromCSV();
        }

        if (GUILayout.Button("적 2성/3성 캐릭터 CSV 가져오기"))
        {
            ImportEnemyStarsFromCSV();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // ScriptableObject → CSV 섹션
        GUILayout.Label("ScriptableObject → CSV 내보내기", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        if (GUILayout.Button("모든 데이터를 CSV로 내보내기"))
        {
            ExportAllToCSV();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("변경된 파일만 내보내기"))
        {
            ExportModifiedOnly();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // 상태 표시
        GUILayout.Label("동기화 상태", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        CheckSyncStatus();

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
    }

    private void CheckForFileChanges()
    {
        var changedFiles = new List<string>();

        foreach (var csvFile in Directory.GetFiles(csvFolderPath, "*.csv"))
        {
            string fileName = Path.GetFileName(csvFile);
            string currentHash = GetFileHash(csvFile);

            if (!fileHashes.ContainsKey(fileName) || fileHashes[fileName] != currentHash)
            {
                changedFiles.Add(fileName);
            }
        }

        if (changedFiles.Count > 0)
        {
            if (EditorUtility.DisplayDialog("CSV 파일 변경 감지", 
                $"다음 파일들이 변경되었습니다:\n{string.Join("\n", changedFiles)}\n\n가져오시겠습니까?", 
                "예", "아니오"))
            {
                foreach (var file in changedFiles)
                {
                    ImportCSVFile(file);
                }
                UpdateFileHashes();
            }
        }
    }

    private void ImportCSVFile(string fileName)
    {
        switch (fileName)
        {
            case "ally_one_star_characters.csv":
                ImportAllyOneStarFromCSV();
                break;
            case "enemy_one_star_characters.csv":
                ImportEnemyOneStarFromCSV();
                break;
            case "ally_two_star_characters.csv":
            case "ally_three_star_characters.csv":
                ImportAllyStarsFromCSV();
                break;
            case "enemy_two_star_characters.csv":
            case "enemy_three_star_characters.csv":
                ImportEnemyStarsFromCSV();
                break;
        }
    }

    private void ImportAllCSVFiles()
    {
        ImportAllyOneStarFromCSV();
        ImportEnemyOneStarFromCSV();
        ImportAllyStarsFromCSV();
        ImportEnemyStarsFromCSV();
        
        UpdateFileHashes();
        Debug.Log("모든 CSV 파일 가져오기 완료!");
    }

    private void ImportAllyOneStarFromCSV()
    {
        string csvPath = Path.Combine(csvFolderPath, "ally_one_star_characters.csv");
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        CharacterDatabaseObject charDB = AssetDatabase.LoadAssetAtPath<CharacterDatabaseObject>("Assets/Prefabs/Data/CharacterDatabase.asset");
        if (charDB == null)
        {
            Debug.LogError("CharacterDatabase.asset을 찾을 수 없습니다!");
            return;
        }

        // 기존 1성이 아닌 캐릭터들 보존
        List<CharacterData> existingNonOneStar = new List<CharacterData>();
        if (charDB.characters != null)
        {
            existingNonOneStar = charDB.characters.Where(c => c.initialStar != CharacterStar.OneStar).ToList();
        }

        // CSV에서 1성 캐릭터 읽기
        List<CharacterData> oneStarCharacters = ParseCharacterCSV(csvPath);

        // 합치기
        List<CharacterData> allCharacters = new List<CharacterData>();
        allCharacters.AddRange(existingNonOneStar);
        allCharacters.AddRange(oneStarCharacters);

        charDB.characters = allCharacters.ToArray();
        EditorUtility.SetDirty(charDB);
        AssetDatabase.SaveAssets();

        Debug.Log($"아군 1성 캐릭터 {oneStarCharacters.Count}개 가져오기 완료!");
    }

    private void ImportEnemyOneStarFromCSV()
    {
        string csvPath = Path.Combine(csvFolderPath, "enemy_one_star_characters.csv");
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        CharacterDatabaseObject charDB = AssetDatabase.LoadAssetAtPath<CharacterDatabaseObject>("Assets/Prefabs/Data/opponentCharacterDatabase.asset");
        if (charDB == null)
        {
            Debug.LogError("opponentCharacterDatabase.asset을 찾을 수 없습니다!");
            return;
        }

        // 기존 1성이 아닌 캐릭터들 보존
        List<CharacterData> existingNonOneStar = new List<CharacterData>();
        if (charDB.characters != null)
        {
            existingNonOneStar = charDB.characters.Where(c => c.initialStar != CharacterStar.OneStar).ToList();
        }

        // CSV에서 1성 캐릭터 읽기
        List<CharacterData> oneStarCharacters = ParseCharacterCSV(csvPath);

        // 합치기
        List<CharacterData> allCharacters = new List<CharacterData>();
        allCharacters.AddRange(existingNonOneStar);
        allCharacters.AddRange(oneStarCharacters);

        charDB.characters = allCharacters.ToArray();
        EditorUtility.SetDirty(charDB);
        AssetDatabase.SaveAssets();

        Debug.Log($"적 1성 캐릭터 {oneStarCharacters.Count}개 가져오기 완료!");
    }

    private void ImportAllyStarsFromCSV()
    {
        StarMergeDatabaseObject starDB = AssetDatabase.LoadAssetAtPath<StarMergeDatabaseObject>("Assets/Prefabs/Data/StarMergeDatabase.asset");
        if (starDB == null)
        {
            Debug.LogError("StarMergeDatabase.asset을 찾을 수 없습니다!");
            return;
        }

        // 2성 캐릭터 가져오기
        string twoStarPath = Path.Combine(csvFolderPath, "ally_two_star_characters.csv");
        if (File.Exists(twoStarPath))
        {
            var twoStarChars = ParseWeightedCharacterCSV(twoStarPath);
            starDB.twoStarPools = ConvertToRaceStarPools(twoStarChars);
        }

        // 3성 캐릭터 가져오기
        string threeStarPath = Path.Combine(csvFolderPath, "ally_three_star_characters.csv");
        if (File.Exists(threeStarPath))
        {
            var threeStarChars = ParseWeightedCharacterCSV(threeStarPath);
            starDB.threeStarPools = ConvertToRaceStarPools(threeStarChars);
        }

        EditorUtility.SetDirty(starDB);
        AssetDatabase.SaveAssets();

        Debug.Log("아군 2성/3성 캐릭터 가져오기 완료!");
    }

    private void ImportEnemyStarsFromCSV()
    {
        StarMergeDatabaseObject starDB = AssetDatabase.LoadAssetAtPath<StarMergeDatabaseObject>("Assets/Prefabs/Data/OPStarMergeDatabase 1.asset");
        if (starDB == null)
        {
            Debug.LogError("OPStarMergeDatabase 1.asset을 찾을 수 없습니다!");
            return;
        }

        // 2성 캐릭터 가져오기
        string twoStarPath = Path.Combine(csvFolderPath, "enemy_two_star_characters.csv");
        if (File.Exists(twoStarPath))
        {
            var twoStarChars = ParseWeightedCharacterCSV(twoStarPath);
            starDB.twoStarPools = ConvertToRaceStarPools(twoStarChars);
        }

        // 3성 캐릭터 가져오기
        string threeStarPath = Path.Combine(csvFolderPath, "enemy_three_star_characters.csv");
        if (File.Exists(threeStarPath))
        {
            var threeStarChars = ParseWeightedCharacterCSV(threeStarPath);
            starDB.threeStarPools = ConvertToRaceStarPools(threeStarChars);
        }

        EditorUtility.SetDirty(starDB);
        AssetDatabase.SaveAssets();

        Debug.Log("적 2성/3성 캐릭터 가져오기 완료!");
    }

    private List<CharacterData> ParseCharacterCSV(string csvPath)
    {
        List<CharacterData> characters = new List<CharacterData>();
        string[] lines = File.ReadAllLines(csvPath);

        // 헤더 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCSVLine(lines[i]);
            if (values.Length < 10) continue;

            CharacterData character = new CharacterData
            {
                characterName = values[0],
                initialStar = ParseCharacterStar(values[1]),
                race = ParseRace(values[2]),
                attackPower = float.Parse(values[3]),
                attackSpeed = float.Parse(values[4]),
                attackRange = float.Parse(values[5]),
                maxHP = float.Parse(values[6]),
                moveSpeed = float.Parse(values[7]),
                rangeType = ParseRangeType(values[8]),
                isAreaAttack = values[9] == "예",
                cost = int.Parse(values[10])
            };

            characters.Add(character);
        }

        return characters;
    }

    private List<WeightedCharacter> ParseWeightedCharacterCSV(string csvPath)
    {
        List<WeightedCharacter> weightedCharacters = new List<WeightedCharacter>();
        string[] lines = File.ReadAllLines(csvPath);

        // 헤더 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCSVLine(lines[i]);
            if (values.Length < 11) continue; // 가중치 포함

            CharacterData character = new CharacterData
            {
                characterName = values[0],
                initialStar = ParseCharacterStar(values[1]),
                race = ParseRace(values[2]),
                attackPower = float.Parse(values[3]),
                attackSpeed = float.Parse(values[4]),
                attackRange = float.Parse(values[5]),
                maxHP = float.Parse(values[6]),
                moveSpeed = float.Parse(values[7]),
                rangeType = ParseRangeType(values[8]),
                isAreaAttack = values[9] == "예",
                cost = int.Parse(values[10])
            };

            WeightedCharacter weightedChar = new WeightedCharacter
            {
                characterData = character,
                weight = values.Length > 11 ? float.Parse(values[11]) : 1f
            };

            weightedCharacters.Add(weightedChar);
        }

        return weightedCharacters;
    }

    private RaceStarPool[] ConvertToRaceStarPools(List<WeightedCharacter> characters)
    {
        var pools = new Dictionary<RaceType, List<WeightedCharacter>>();

        foreach (var character in characters)
        {
            RaceType race = (RaceType)character.characterData.race;
            if (!pools.ContainsKey(race))
            {
                pools[race] = new List<WeightedCharacter>();
            }
            pools[race].Add(character);
        }

        return pools.Select(kvp => new RaceStarPool
        {
            race = kvp.Key,
            possibleCharacters = kvp.Value.ToArray()
        }).ToArray();
    }

    private void ExportAllToCSV()
    {
        // Unity 에디터의 기존 내보내기 기능 사용
        var exportEditor = EditorWindow.GetWindow<DataExportEditor>();
        if (exportEditor != null)
        {
            exportEditor.Close();
        }

        // DataExportEditor의 내보내기 메서드 직접 호출
        ExportOneStarsDirectly();
        ExportAllyStarsDirectly();
        ExportEnemyStarsDirectly();

        UpdateFileHashes();
        Debug.Log("모든 데이터 CSV 내보내기 완료!");
    }

    private void ExportOneStarsDirectly()
    {
        // 아군 1성
        CharacterDatabaseObject allyDB = AssetDatabase.LoadAssetAtPath<CharacterDatabaseObject>("Assets/Prefabs/Data/CharacterDatabase.asset");
        if (allyDB != null && allyDB.characters != null)
        {
            using (StreamWriter writer = new StreamWriter(Path.Combine(csvFolderPath, "ally_one_star_characters.csv")))
            {
                writer.WriteLine("이름,초기 별,종족,공격력,공격속도,공격범위,최대 HP,이동속도,공격 타입,광역공격,비용");

                foreach (var character in allyDB.characters.Where(c => c.initialStar == CharacterStar.OneStar))
                {
                    WriteCharacterLine(writer, character);
                }
            }
        }

        // 적 1성
        CharacterDatabaseObject enemyDB = AssetDatabase.LoadAssetAtPath<CharacterDatabaseObject>("Assets/Prefabs/Data/opponentCharacterDatabase.asset");
        if (enemyDB != null && enemyDB.characters != null)
        {
            using (StreamWriter writer = new StreamWriter(Path.Combine(csvFolderPath, "enemy_one_star_characters.csv")))
            {
                writer.WriteLine("이름,초기 별,종족,공격력,공격속도,공격범위,최대 HP,이동속도,공격 타입,광역공격,비용");

                foreach (var character in enemyDB.characters.Where(c => c.initialStar == CharacterStar.OneStar))
                {
                    WriteCharacterLine(writer, character);
                }
            }
        }
    }

    private void ExportAllyStarsDirectly()
    {
        StarMergeDatabaseObject starDB = AssetDatabase.LoadAssetAtPath<StarMergeDatabaseObject>("Assets/Prefabs/Data/StarMergeDatabase.asset");
        if (starDB == null) return;

        // 2성
        if (starDB.twoStarPools != null)
        {
            using (StreamWriter writer = new StreamWriter(Path.Combine(csvFolderPath, "ally_two_star_characters.csv")))
            {
                writer.WriteLine("이름,초기 별,종족,공격력,공격속도,공격범위,최대 HP,이동속도,공격 타입,광역공격,비용,가중치");

                foreach (var pool in starDB.twoStarPools)
                {
                    if (pool.possibleCharacters != null)
                    {
                        foreach (var weightedChar in pool.possibleCharacters)
                        {
                            WriteWeightedCharacterLine(writer, weightedChar, 2);
                        }
                    }
                }
            }
        }

        // 3성
        if (starDB.threeStarPools != null)
        {
            using (StreamWriter writer = new StreamWriter(Path.Combine(csvFolderPath, "ally_three_star_characters.csv")))
            {
                writer.WriteLine("이름,초기 별,종족,공격력,공격속도,공격범위,최대 HP,이동속도,공격 타입,광역공격,비용,가중치");

                foreach (var pool in starDB.threeStarPools)
                {
                    if (pool.possibleCharacters != null)
                    {
                        foreach (var weightedChar in pool.possibleCharacters)
                        {
                            WriteWeightedCharacterLine(writer, weightedChar, 3);
                        }
                    }
                }
            }
        }
    }

    private void ExportEnemyStarsDirectly()
    {
        StarMergeDatabaseObject starDB = AssetDatabase.LoadAssetAtPath<StarMergeDatabaseObject>("Assets/Prefabs/Data/OPStarMergeDatabase 1.asset");
        if (starDB == null) return;

        // 2성
        if (starDB.twoStarPools != null)
        {
            using (StreamWriter writer = new StreamWriter(Path.Combine(csvFolderPath, "enemy_two_star_characters.csv")))
            {
                writer.WriteLine("이름,초기 별,종족,공격력,공격속도,공격범위,최대 HP,이동속도,공격 타입,광역공격,비용,가중치");

                foreach (var pool in starDB.twoStarPools)
                {
                    if (pool.possibleCharacters != null)
                    {
                        foreach (var weightedChar in pool.possibleCharacters)
                        {
                            WriteWeightedCharacterLine(writer, weightedChar, 2);
                        }
                    }
                }
            }
        }

        // 3성
        if (starDB.threeStarPools != null)
        {
            using (StreamWriter writer = new StreamWriter(Path.Combine(csvFolderPath, "enemy_three_star_characters.csv")))
            {
                writer.WriteLine("이름,초기 별,종족,공격력,공격속도,공격범위,최대 HP,이동속도,공격 타입,광역공격,비용,가중치");

                foreach (var pool in starDB.threeStarPools)
                {
                    if (pool.possibleCharacters != null)
                    {
                        foreach (var weightedChar in pool.possibleCharacters)
                        {
                            WriteWeightedCharacterLine(writer, weightedChar, 3);
                        }
                    }
                }
            }
        }
    }

    private void WriteCharacterLine(StreamWriter writer, CharacterData character)
    {
        string raceName = GetRaceName(character.race);
        string rangeTypeName = GetRangeTypeName(character.rangeType);
        string areaAttack = character.isAreaAttack ? "예" : "아니오";

        writer.WriteLine($"{character.characterName},{(int)character.initialStar},{raceName},{character.attackPower},{character.attackSpeed},{character.attackRange},{character.maxHP},{character.moveSpeed},{rangeTypeName},{areaAttack},{character.cost}");
    }

    private void WriteWeightedCharacterLine(StreamWriter writer, WeightedCharacter weightedChar, int starLevel)
    {
        var character = weightedChar.characterData;
        string raceName = GetRaceName(character.race);
        string rangeTypeName = GetRangeTypeName(character.rangeType);
        string areaAttack = character.isAreaAttack ? "예" : "아니오";

        writer.WriteLine($"{character.characterName},{starLevel},{raceName},{character.attackPower},{character.attackSpeed},{character.attackRange},{character.maxHP},{character.moveSpeed},{rangeTypeName},{areaAttack},{character.cost},{weightedChar.weight}");
    }

    private void ExportModifiedOnly()
    {
        // 구현 예정: 변경된 데이터만 감지하여 내보내기
        Debug.Log("변경된 데이터 감지 기능은 추후 구현 예정입니다.");
    }

    private void CheckSyncStatus()
    {
        // CSV 파일들의 상태 확인
        string[] csvFiles = new string[]
        {
            "ally_one_star_characters.csv",
            "enemy_one_star_characters.csv",
            "ally_two_star_characters.csv",
            "ally_three_star_characters.csv",
            "enemy_two_star_characters.csv",
            "enemy_three_star_characters.csv"
        };

        foreach (var fileName in csvFiles)
        {
            string fullPath = Path.Combine(csvFolderPath, fileName);
            if (File.Exists(fullPath))
            {
                var lastModified = File.GetLastWriteTime(fullPath);
                EditorGUILayout.LabelField($"{fileName}: {lastModified:yyyy-MM-dd HH:mm:ss}");
            }
            else
            {
                EditorGUILayout.LabelField($"{fileName}: 파일 없음", EditorStyles.boldLabel);
            }
        }
    }

    private void UpdateFileHashes()
    {
        fileHashes.Clear();
        
        if (!Directory.Exists(csvFolderPath))
        {
            Directory.CreateDirectory(csvFolderPath);
        }

        foreach (var csvFile in Directory.GetFiles(csvFolderPath, "*.csv"))
        {
            string fileName = Path.GetFileName(csvFile);
            fileHashes[fileName] = GetFileHash(csvFile);
        }
    }

    private string GetFileHash(string filePath)
    {
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                var hash = md5.ComputeHash(stream);
                return System.BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
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
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        result.Add(currentField); // 마지막 필드 추가
        return result.ToArray();
    }

    // 파싱 헬퍼 메서드들
    private CharacterStar ParseCharacterStar(string star)
    {
        switch (star)
        {
            case "1": return CharacterStar.OneStar;
            case "2": return CharacterStar.TwoStar;
            case "3": return CharacterStar.ThreeStar;
            default: return CharacterStar.OneStar;
        }
    }

    private CharacterRace ParseRace(string race)
    {
        switch (race)
        {
            case "Human": return CharacterRace.Human;
            case "Orc": return CharacterRace.Orc;
            case "Elf": return CharacterRace.Elf;
            default: return CharacterRace.Human;
        }
    }

    private RangeType ParseRangeType(string rangeType)
    {
        switch (rangeType)
        {
            case "Melee": return RangeType.Melee;
            case "Ranged": return RangeType.Ranged;
            case "LongRange": return RangeType.LongRange;
            default: return RangeType.Melee;
        }
    }

    // 이름 변환 헬퍼 메서드들
    private string GetRaceName(CharacterRace race)
    {
        switch (race)
        {
            case CharacterRace.Human: return "Human";
            case CharacterRace.Orc: return "Orc";
            case CharacterRace.Elf: return "Elf";
            default: return "Unknown";
        }
    }

    private string GetRangeTypeName(RangeType rangeType)
    {
        switch (rangeType)
        {
            case RangeType.Melee: return "Melee";
            case RangeType.Ranged: return "Ranged";
            case RangeType.LongRange: return "LongRange";
            default: return "Unknown";
        }
    }
}
#endif 