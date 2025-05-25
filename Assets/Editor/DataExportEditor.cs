#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class DataExportEditor : EditorWindow
{
    [MenuItem("Tools/Data Export Manager")]
    public static void ShowWindow()
    {
        GetWindow<DataExportEditor>("Data Export Manager");
    }

    private Vector2 scrollPosition;
    private bool showItemSection = true;
    private bool showEnemySection = true;
    private bool showAllySection = true;
    private bool showOneStarSection = true;
    
    // 아이템 생성용 필드들
    private string newItemName = "새로운 아이템";
    private ItemEffectType newItemEffect = ItemEffectType.IncreaseAttack;
    private float newItemValue = 1f;
    private string newItemDescription = "아이템 설명";
    private float newItemAreaRadius = 3f;
    private int newItemStarMin = 2;
    private int newItemStarMax = 3;
    private float newItemDamageValue = 30f;

    // 적 캐릭터 생성용 필드들
    private string newEnemyName = "새로운 적";
    private CharacterStar newEnemyStar = CharacterStar.TwoStar;
    private CharacterRace newEnemyRace = CharacterRace.Human;
    private float newEnemyAttack = 25f;
    private float newEnemyAttackSpeed = 1.2f;
    private float newEnemyRange = 2f;
    private float newEnemyHP = 300f;
    private float newEnemyMoveSpeed = 3f;
    private RangeType newEnemyRangeType = RangeType.Melee;
    private bool newEnemyAreaAttack = false;
    private int newEnemyCost = 10;
    private float newEnemyWeight = 1f;

    // 아군 캐릭터 생성용 필드들
    private string newAllyName = "새로운 아군";
    private CharacterStar newAllyStar = CharacterStar.TwoStar;
    private CharacterRace newAllyRace = CharacterRace.Human;
    private float newAllyAttack = 25f;
    private float newAllyAttackSpeed = 1.2f;
    private float newAllyRange = 2f;
    private float newAllyHP = 300f;
    private float newAllyMoveSpeed = 3f;
    private RangeType newAllyRangeType = RangeType.Melee;
    private bool newAllyAreaAttack = false;
    private int newAllyCost = 10;
    private float newAllyWeight = 1f;

    // 1성 캐릭터 생성용 필드들
    private string newOneStarName = "새로운 1성";
    private bool isOneStarEnemy = false; // true면 적, false면 아군
    private CharacterRace newOneStarRace = CharacterRace.Human;
    private float newOneStarAttack = 15f;
    private float newOneStarAttackSpeed = 1.0f;
    private float newOneStarRange = 1.5f;
    private float newOneStarHP = 150f;
    private float newOneStarMoveSpeed = 3f;
    private RangeType newOneStarRangeType = RangeType.Melee;
    private bool newOneStarAreaAttack = false;
    private int newOneStarCost = 5;

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Unity 데이터 생성 및 CSV 내보내기", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 아이템 섹션
        showItemSection = EditorGUILayout.Foldout(showItemSection, "아이템 관리", true);
        if (showItemSection)
        {
            DrawItemSection();
        }

        EditorGUILayout.Space();

        // 적 캐릭터 섹션
        showEnemySection = EditorGUILayout.Foldout(showEnemySection, "적 캐릭터 관리", true);
        if (showEnemySection)
        {
            DrawEnemySection();
        }

        EditorGUILayout.Space();

        // 아군 캐릭터 섹션
        showAllySection = EditorGUILayout.Foldout(showAllySection, "아군 캐릭터 관리", true);
        if (showAllySection)
        {
            DrawAllySection();
        }

        EditorGUILayout.Space();

        // 1성 캐릭터 섹션
        showOneStarSection = EditorGUILayout.Foldout(showOneStarSection, "1성 캐릭터 관리", true);
        if (showOneStarSection)
        {
            DrawOneStarSection();
        }

        EditorGUILayout.Space();

        // CSV 내보내기 버튼들
        GUILayout.Label("CSV 내보내기", EditorStyles.boldLabel);
        
        if (GUILayout.Button("모든 데이터를 CSV로 내보내기"))
        {
            ExportAllDataToCSV();
        }

        if (GUILayout.Button("아이템만 CSV로 내보내기"))
        {
            ExportItemsToCSV();
        }

        if (GUILayout.Button("적 2성/3성만 CSV로 내보내기"))
        {
            ExportEnemyStarsToCSV();
        }

        if (GUILayout.Button("아군 2성/3성만 CSV로 내보내기"))
        {
            ExportAllyStarsToCSV();
        }

        if (GUILayout.Button("1성 캐릭터만 CSV로 내보내기"))
        {
            ExportOneStarsToCSV();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawItemSection()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("새 아이템 생성", EditorStyles.boldLabel);

        newItemName = EditorGUILayout.TextField("아이템명", newItemName);
        newItemEffect = (ItemEffectType)EditorGUILayout.EnumPopup("효과 타입", newItemEffect);
        newItemValue = EditorGUILayout.FloatField("효과 값", newItemValue);
        newItemDescription = EditorGUILayout.TextField("설명", newItemDescription);
        newItemAreaRadius = EditorGUILayout.FloatField("범위 반경", newItemAreaRadius);
        newItemStarMin = EditorGUILayout.IntField("최소 별", newItemStarMin);
        newItemStarMax = EditorGUILayout.IntField("최대 별", newItemStarMax);
        newItemDamageValue = EditorGUILayout.FloatField("데미지 값", newItemDamageValue);

        if (GUILayout.Button("아이템 추가"))
        {
            AddNewItem();
        }

        EditorGUILayout.Space();

        // 현재 아이템 목록 표시
        ItemDatabaseObject itemDB = AssetDatabase.LoadAssetAtPath<ItemDatabaseObject>("Assets/Prefabs/Data/NewItemDatabase.asset");
        if (itemDB != null && itemDB.items != null)
        {
            GUILayout.Label($"현재 아이템 개수: {itemDB.items.Length}", EditorStyles.helpBox);
            for (int i = 0; i < itemDB.items.Length; i++)
            {
                EditorGUILayout.LabelField($"{i + 1}. {itemDB.items[i].itemName} - {itemDB.items[i].description}");
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawEnemySection()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("새 적 캐릭터 생성", EditorStyles.boldLabel);

        newEnemyName = EditorGUILayout.TextField("캐릭터명", newEnemyName);
        newEnemyStar = (CharacterStar)EditorGUILayout.EnumPopup("별 등급", newEnemyStar);
        newEnemyRace = (CharacterRace)EditorGUILayout.EnumPopup("종족", newEnemyRace);
        newEnemyAttack = EditorGUILayout.FloatField("공격력", newEnemyAttack);
        newEnemyAttackSpeed = EditorGUILayout.FloatField("공격속도", newEnemyAttackSpeed);
        newEnemyRange = EditorGUILayout.FloatField("공격범위", newEnemyRange);
        newEnemyHP = EditorGUILayout.FloatField("최대 HP", newEnemyHP);
        newEnemyMoveSpeed = EditorGUILayout.FloatField("이동속도", newEnemyMoveSpeed);
        newEnemyRangeType = (RangeType)EditorGUILayout.EnumPopup("공격 타입", newEnemyRangeType);
        newEnemyAreaAttack = EditorGUILayout.Toggle("광역공격", newEnemyAreaAttack);
        newEnemyCost = EditorGUILayout.IntField("비용", newEnemyCost);
        newEnemyWeight = EditorGUILayout.FloatField("가중치", newEnemyWeight);

        if (GUILayout.Button("적 캐릭터 추가"))
        {
            AddNewEnemyCharacter();
        }

        EditorGUILayout.Space();

        // 현재 적 2성/3성 캐릭터 개수 표시
        StarMergeDatabaseObject starDB = AssetDatabase.LoadAssetAtPath<StarMergeDatabaseObject>("Assets/Prefabs/Data/OPStarMergeDatabase 1.asset");
        if (starDB != null)
        {
            int twoStarCount = 0;
            int threeStarCount = 0;
            
            if (starDB.twoStarPools != null)
            {
                foreach (var pool in starDB.twoStarPools)
                {
                    if (pool.possibleCharacters != null)
                        twoStarCount += pool.possibleCharacters.Length;
                }
            }
            
            if (starDB.threeStarPools != null)
            {
                foreach (var pool in starDB.threeStarPools)
                {
                    if (pool.possibleCharacters != null)
                        threeStarCount += pool.possibleCharacters.Length;
                }
            }

            GUILayout.Label($"현재 적 2성 캐릭터: {twoStarCount}개", EditorStyles.helpBox);
            GUILayout.Label($"현재 적 3성 캐릭터: {threeStarCount}개", EditorStyles.helpBox);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawAllySection()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("새 아군 캐릭터 생성", EditorStyles.boldLabel);

        newAllyName = EditorGUILayout.TextField("캐릭터명", newAllyName);
        newAllyStar = (CharacterStar)EditorGUILayout.EnumPopup("별 등급", newAllyStar);
        newAllyRace = (CharacterRace)EditorGUILayout.EnumPopup("종족", newAllyRace);
        newAllyAttack = EditorGUILayout.FloatField("공격력", newAllyAttack);
        newAllyAttackSpeed = EditorGUILayout.FloatField("공격속도", newAllyAttackSpeed);
        newAllyRange = EditorGUILayout.FloatField("공격범위", newAllyRange);
        newAllyHP = EditorGUILayout.FloatField("최대 HP", newAllyHP);
        newAllyMoveSpeed = EditorGUILayout.FloatField("이동속도", newAllyMoveSpeed);
        newAllyRangeType = (RangeType)EditorGUILayout.EnumPopup("공격 타입", newAllyRangeType);
        newAllyAreaAttack = EditorGUILayout.Toggle("광역공격", newAllyAreaAttack);
        newAllyCost = EditorGUILayout.IntField("비용", newAllyCost);
        newAllyWeight = EditorGUILayout.FloatField("가중치", newAllyWeight);

        if (GUILayout.Button("아군 캐릭터 추가"))
        {
            AddNewAllyCharacter();
        }

        EditorGUILayout.Space();

        // 현재 아군 2성/3성 캐릭터 개수 표시
        StarMergeDatabaseObject starDB = AssetDatabase.LoadAssetAtPath<StarMergeDatabaseObject>("Assets/Prefabs/Data/StarMergeDatabase.asset");
        if (starDB != null)
        {
            int twoStarCount = 0;
            int threeStarCount = 0;
            
            if (starDB.twoStarPools != null)
            {
                foreach (var pool in starDB.twoStarPools)
                {
                    if (pool.possibleCharacters != null)
                        twoStarCount += pool.possibleCharacters.Length;
                }
            }
            
            if (starDB.threeStarPools != null)
            {
                foreach (var pool in starDB.threeStarPools)
                {
                    if (pool.possibleCharacters != null)
                        threeStarCount += pool.possibleCharacters.Length;
                }
            }

            GUILayout.Label($"현재 아군 2성 캐릭터: {twoStarCount}개", EditorStyles.helpBox);
            GUILayout.Label($"현재 아군 3성 캐릭터: {threeStarCount}개", EditorStyles.helpBox);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawOneStarSection()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("새 1성 캐릭터 생성", EditorStyles.boldLabel);

        newOneStarName = EditorGUILayout.TextField("캐릭터명", newOneStarName);
        isOneStarEnemy = EditorGUILayout.Toggle("적 캐릭터", isOneStarEnemy);
        newOneStarRace = (CharacterRace)EditorGUILayout.EnumPopup("종족", newOneStarRace);
        newOneStarAttack = EditorGUILayout.FloatField("공격력", newOneStarAttack);
        newOneStarAttackSpeed = EditorGUILayout.FloatField("공격속도", newOneStarAttackSpeed);
        newOneStarRange = EditorGUILayout.FloatField("공격범위", newOneStarRange);
        newOneStarHP = EditorGUILayout.FloatField("최대 HP", newOneStarHP);
        newOneStarMoveSpeed = EditorGUILayout.FloatField("이동속도", newOneStarMoveSpeed);
        newOneStarRangeType = (RangeType)EditorGUILayout.EnumPopup("공격 타입", newOneStarRangeType);
        newOneStarAreaAttack = EditorGUILayout.Toggle("광역공격", newOneStarAreaAttack);
        newOneStarCost = EditorGUILayout.IntField("비용", newOneStarCost);

        if (GUILayout.Button("1성 캐릭터 추가"))
        {
            AddNewOneStarCharacter();
        }

        EditorGUILayout.Space();

        // 현재 1성 캐릭터 개수 표시
        CharacterDatabaseObject allyDB = AssetDatabase.LoadAssetAtPath<CharacterDatabaseObject>("Assets/Prefabs/Data/CharacterDatabase.asset");
        CharacterDatabaseObject enemyDB = AssetDatabase.LoadAssetAtPath<CharacterDatabaseObject>("Assets/Prefabs/Data/opponentCharacterDatabase.asset");
        
        int allyOneStarCount = 0;
        int enemyOneStarCount = 0;
        
        if (allyDB != null && allyDB.characters != null)
        {
            foreach (var character in allyDB.characters)
            {
                if (character.initialStar == CharacterStar.OneStar)
                    allyOneStarCount++;
            }
        }
        
        if (enemyDB != null && enemyDB.characters != null)
        {
            foreach (var character in enemyDB.characters)
            {
                if (character.initialStar == CharacterStar.OneStar)
                    enemyOneStarCount++;
            }
        }

        GUILayout.Label($"현재 아군 1성 캐릭터: {allyOneStarCount}개", EditorStyles.helpBox);
        GUILayout.Label($"현재 적 1성 캐릭터: {enemyOneStarCount}개", EditorStyles.helpBox);

        EditorGUILayout.EndVertical();
    }

    private void AddNewItem()
    {
        ItemDatabaseObject itemDB = AssetDatabase.LoadAssetAtPath<ItemDatabaseObject>("Assets/Prefabs/Data/NewItemDatabase.asset");
        if (itemDB == null)
        {
            Debug.LogError("NewItemDatabase.asset을 찾을 수 없습니다!");
            return;
        }

        // 새 아이템 데이터 생성
        ItemData newItem = new ItemData
        {
            itemName = newItemName,
            effectType = newItemEffect,
            effectValue = newItemValue,
            description = newItemDescription,
            areaRadius = newItemAreaRadius,
            starMin = newItemStarMin,
            starMax = newItemStarMax,
            damageValue = newItemDamageValue
        };

        // 배열에 추가
        List<ItemData> itemList = new List<ItemData>(itemDB.items);
        itemList.Add(newItem);
        itemDB.items = itemList.ToArray();

        // 저장
        EditorUtility.SetDirty(itemDB);
        AssetDatabase.SaveAssets();

        Debug.Log($"새 아이템 '{newItemName}' 추가 완료!");
    }

    private void AddNewEnemyCharacter()
    {
        StarMergeDatabaseObject starDB = AssetDatabase.LoadAssetAtPath<StarMergeDatabaseObject>("Assets/Prefabs/Data/OPStarMergeDatabase 1.asset");
        if (starDB == null)
        {
            Debug.LogError("OPStarMergeDatabase 1.asset을 찾을 수 없습니다!");
            return;
        }

        // 새 캐릭터 데이터 생성
        CharacterData newCharacter = new CharacterData
        {
            characterName = newEnemyName,
            initialStar = newEnemyStar,
            race = newEnemyRace,
            attackPower = newEnemyAttack,
            attackSpeed = newEnemyAttackSpeed,
            attackRange = newEnemyRange,
            maxHP = newEnemyHP,
            moveSpeed = newEnemyMoveSpeed,
            rangeType = newEnemyRangeType,
            isAreaAttack = newEnemyAreaAttack,
            cost = newEnemyCost
        };

        // WeightedCharacter로 래핑
        WeightedCharacter weightedChar = new WeightedCharacter
        {
            characterData = newCharacter,
            weight = newEnemyWeight
        };

        // 해당 종족과 별 등급에 맞는 풀 찾기 또는 생성
        RaceType raceType = (RaceType)newEnemyRace;
        
        if (newEnemyStar == CharacterStar.TwoStar)
        {
            AddToStarPool(ref starDB.twoStarPools, raceType, weightedChar);
        }
        else if (newEnemyStar == CharacterStar.ThreeStar)
        {
            AddToStarPool(ref starDB.threeStarPools, raceType, weightedChar);
        }

        // 저장
        EditorUtility.SetDirty(starDB);
        AssetDatabase.SaveAssets();

        Debug.Log($"새 적 캐릭터 '{newEnemyName}' ({newEnemyStar}, {newEnemyRace}) 추가 완료!");
    }

    private void AddToStarPool(ref RaceStarPool[] pools, RaceType raceType, WeightedCharacter weightedChar)
    {
        // 해당 종족 풀 찾기
        RaceStarPool targetPool = null;
        foreach (var pool in pools)
        {
            if (pool.race == raceType)
            {
                targetPool = pool;
                break;
            }
        }

        // 풀이 없으면 새로 생성
        if (targetPool == null)
        {
            List<RaceStarPool> poolList = new List<RaceStarPool>(pools);
            targetPool = new RaceStarPool
            {
                race = raceType,
                possibleCharacters = new WeightedCharacter[0]
            };
            poolList.Add(targetPool);
            pools = poolList.ToArray();
        }

        // 캐릭터 추가
        List<WeightedCharacter> charList = new List<WeightedCharacter>(targetPool.possibleCharacters);
        charList.Add(weightedChar);
        targetPool.possibleCharacters = charList.ToArray();
    }

    private void AddNewAllyCharacter()
    {
        StarMergeDatabaseObject starDB = AssetDatabase.LoadAssetAtPath<StarMergeDatabaseObject>("Assets/Prefabs/Data/StarMergeDatabase.asset");
        if (starDB == null)
        {
            Debug.LogError("StarMergeDatabase.asset을 찾을 수 없습니다!");
            return;
        }

        // 새 캐릭터 데이터 생성
        CharacterData newCharacter = new CharacterData
        {
            characterName = newAllyName,
            initialStar = newAllyStar,
            race = newAllyRace,
            attackPower = newAllyAttack,
            attackSpeed = newAllyAttackSpeed,
            attackRange = newAllyRange,
            maxHP = newAllyHP,
            moveSpeed = newAllyMoveSpeed,
            rangeType = newAllyRangeType,
            isAreaAttack = newAllyAreaAttack,
            cost = newAllyCost
        };

        // WeightedCharacter로 래핑
        WeightedCharacter weightedChar = new WeightedCharacter
        {
            characterData = newCharacter,
            weight = newAllyWeight
        };

        // 해당 종족과 별 등급에 맞는 풀 찾기 또는 생성
        RaceType raceType = (RaceType)newAllyRace;
        
        if (newAllyStar == CharacterStar.TwoStar)
        {
            AddToStarPool(ref starDB.twoStarPools, raceType, weightedChar);
        }
        else if (newAllyStar == CharacterStar.ThreeStar)
        {
            AddToStarPool(ref starDB.threeStarPools, raceType, weightedChar);
        }

        // 저장
        EditorUtility.SetDirty(starDB);
        AssetDatabase.SaveAssets();

        Debug.Log($"새 아군 캐릭터 '{newAllyName}' ({newAllyStar}, {newAllyRace}) 추가 완료!");
    }

    private void AddNewOneStarCharacter()
    {
        // 새 캐릭터 데이터 생성
        CharacterData newCharacter = new CharacterData
        {
            characterName = newOneStarName,
            initialStar = CharacterStar.OneStar,
            race = newOneStarRace,
            attackPower = newOneStarAttack,
            attackSpeed = newOneStarAttackSpeed,
            attackRange = newOneStarRange,
            maxHP = newOneStarHP,
            moveSpeed = newOneStarMoveSpeed,
            rangeType = newOneStarRangeType,
            isAreaAttack = newOneStarAreaAttack,
            cost = newOneStarCost
        };

        // 적인지 아군인지에 따라 다른 데이터베이스에 추가
        string dbPath = isOneStarEnemy ? 
            "Assets/Prefabs/Data/opponentCharacterDatabase.asset" : 
            "Assets/Prefabs/Data/CharacterDatabase.asset";
            
        CharacterDatabaseObject charDB = AssetDatabase.LoadAssetAtPath<CharacterDatabaseObject>(dbPath);
        if (charDB == null)
        {
            Debug.LogError($"{dbPath}을 찾을 수 없습니다!");
            return;
        }

        // 배열에 추가
        List<CharacterData> charList = new List<CharacterData>(charDB.characters);
        charList.Add(newCharacter);
        charDB.characters = charList.ToArray();

        // 저장
        EditorUtility.SetDirty(charDB);
        AssetDatabase.SaveAssets();

        string type = isOneStarEnemy ? "적" : "아군";
        Debug.Log($"새 {type} 1성 캐릭터 '{newOneStarName}' ({newOneStarRace}) 추가 완료!");
    }

    private void ExportAllDataToCSV()
    {
        ExportItemsToCSV();
        ExportEnemyStarsToCSV();
        ExportAllyStarsToCSV();
        ExportOneStarsToCSV();
        Debug.Log("모든 데이터 CSV 내보내기 완료!");
    }

    private void ExportItemsToCSV()
    {
        ItemDatabaseObject itemDB = AssetDatabase.LoadAssetAtPath<ItemDatabaseObject>("Assets/Prefabs/Data/NewItemDatabase.asset");
        if (itemDB == null || itemDB.items == null)
        {
            Debug.LogError("아이템 데이터베이스를 찾을 수 없습니다!");
            return;
        }

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("아이템명,효과 타입,효과 값,설명,아이콘,범위 반경,최소 별,최대 별,데미지 값");

        foreach (var item in itemDB.items)
        {
            string effectTypeName = GetEffectTypeName(item.effectType);
            csv.AppendLine($"{item.itemName},{effectTypeName},{item.effectValue},{item.description},있음,{item.areaRadius},{item.starMin},{item.starMax},{item.damageValue}");
        }

        string filePath = Path.Combine(Application.dataPath, "../items_new.csv");
        File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
        
        Debug.Log($"아이템 CSV 파일 생성 완료: {filePath}");
    }

    private void ExportEnemyStarsToCSV()
    {
        StarMergeDatabaseObject starDB = AssetDatabase.LoadAssetAtPath<StarMergeDatabaseObject>("Assets/Prefabs/Data/OPStarMergeDatabase 1.asset");
        if (starDB == null)
        {
            Debug.LogError("적 StarMergeDatabase를 찾을 수 없습니다!");
            return;
        }

        // 2성 적 캐릭터 내보내기
        StringBuilder twoStarCsv = new StringBuilder();
        twoStarCsv.AppendLine("이름,초기 별,종족,공격력,공격속도,공격범위,최대 HP,이동속도,공격 타입,광역공격,비용,가중치");

        if (starDB.twoStarPools != null)
        {
            foreach (var pool in starDB.twoStarPools)
            {
                if (pool.possibleCharacters != null)
                {
                    foreach (var weightedChar in pool.possibleCharacters)
                    {
                        var character = weightedChar.characterData;
                        string raceName = GetRaceName(character.race);
                        string rangeTypeName = GetRangeTypeName(character.rangeType);
                        string areaAttack = character.isAreaAttack ? "예" : "아니오";

                        twoStarCsv.AppendLine($"{character.characterName},2,{raceName},{character.attackPower},{character.attackSpeed},{character.attackRange},{character.maxHP},{character.moveSpeed},{rangeTypeName},{areaAttack},{character.cost},{weightedChar.weight}");
                    }
                }
            }
        }

        // 3성 적 캐릭터 내보내기
        StringBuilder threeStarCsv = new StringBuilder();
        threeStarCsv.AppendLine("이름,초기 별,종족,공격력,공격속도,공격범위,최대 HP,이동속도,공격 타입,광역공격,비용,가중치");

        if (starDB.threeStarPools != null)
        {
            foreach (var pool in starDB.threeStarPools)
            {
                if (pool.possibleCharacters != null)
                {
                    foreach (var weightedChar in pool.possibleCharacters)
                    {
                        var character = weightedChar.characterData;
                        string raceName = GetRaceName(character.race);
                        string rangeTypeName = GetRangeTypeName(character.rangeType);
                        string areaAttack = character.isAreaAttack ? "예" : "아니오";

                        threeStarCsv.AppendLine($"{character.characterName},3,{raceName},{character.attackPower},{character.attackSpeed},{character.attackRange},{character.maxHP},{character.moveSpeed},{rangeTypeName},{areaAttack},{character.cost},{weightedChar.weight}");
                    }
                }
            }
        }

        // 파일 저장
        string twoStarPath = Path.Combine(Application.dataPath, "../enemy_two_star_characters.csv");
        string threeStarPath = Path.Combine(Application.dataPath, "../enemy_three_star_characters.csv");

        File.WriteAllText(twoStarPath, twoStarCsv.ToString(), Encoding.UTF8);
        File.WriteAllText(threeStarPath, threeStarCsv.ToString(), Encoding.UTF8);

        Debug.Log($"적 2성 캐릭터 CSV 파일 생성 완료: {twoStarPath}");
        Debug.Log($"적 3성 캐릭터 CSV 파일 생성 완료: {threeStarPath}");
    }

    private void ExportAllyStarsToCSV()
    {
        StarMergeDatabaseObject starDB = AssetDatabase.LoadAssetAtPath<StarMergeDatabaseObject>("Assets/Prefabs/Data/StarMergeDatabase.asset");
        if (starDB == null)
        {
            Debug.LogError("아군 StarMergeDatabase를 찾을 수 없습니다!");
            return;
        }

        // 2성 아군 캐릭터 내보내기
        StringBuilder twoStarCsv = new StringBuilder();
        twoStarCsv.AppendLine("이름,초기 별,종족,공격력,공격속도,공격범위,최대 HP,이동속도,공격 타입,광역공격,비용,가중치");

        if (starDB.twoStarPools != null)
        {
            foreach (var pool in starDB.twoStarPools)
            {
                if (pool.possibleCharacters != null)
                {
                    foreach (var weightedChar in pool.possibleCharacters)
                    {
                        var character = weightedChar.characterData;
                        string raceName = GetRaceName(character.race);
                        string rangeTypeName = GetRangeTypeName(character.rangeType);
                        string areaAttack = character.isAreaAttack ? "예" : "아니오";

                        twoStarCsv.AppendLine($"{character.characterName},2,{raceName},{character.attackPower},{character.attackSpeed},{character.attackRange},{character.maxHP},{character.moveSpeed},{rangeTypeName},{areaAttack},{character.cost},{weightedChar.weight}");
                    }
                }
            }
        }

        // 3성 아군 캐릭터 내보내기
        StringBuilder threeStarCsv = new StringBuilder();
        threeStarCsv.AppendLine("이름,초기 별,종족,공격력,공격속도,공격범위,최대 HP,이동속도,공격 타입,광역공격,비용,가중치");

        if (starDB.threeStarPools != null)
        {
            foreach (var pool in starDB.threeStarPools)
            {
                if (pool.possibleCharacters != null)
                {
                    foreach (var weightedChar in pool.possibleCharacters)
                    {
                        var character = weightedChar.characterData;
                        string raceName = GetRaceName(character.race);
                        string rangeTypeName = GetRangeTypeName(character.rangeType);
                        string areaAttack = character.isAreaAttack ? "예" : "아니오";

                        threeStarCsv.AppendLine($"{character.characterName},3,{raceName},{character.attackPower},{character.attackSpeed},{character.attackRange},{character.maxHP},{character.moveSpeed},{rangeTypeName},{areaAttack},{character.cost},{weightedChar.weight}");
                    }
                }
            }
        }

        // 파일 저장
        string twoStarPath = Path.Combine(Application.dataPath, "../ally_two_star_characters.csv");
        string threeStarPath = Path.Combine(Application.dataPath, "../ally_three_star_characters.csv");

        File.WriteAllText(twoStarPath, twoStarCsv.ToString(), Encoding.UTF8);
        File.WriteAllText(threeStarPath, threeStarCsv.ToString(), Encoding.UTF8);

        Debug.Log($"아군 2성 캐릭터 CSV 파일 생성 완료: {twoStarPath}");
        Debug.Log($"아군 3성 캐릭터 CSV 파일 생성 완료: {threeStarPath}");
    }

    private void ExportOneStarsToCSV()
    {
        // 아군 1성 캐릭터 내보내기
        CharacterDatabaseObject allyDB = AssetDatabase.LoadAssetAtPath<CharacterDatabaseObject>("Assets/Prefabs/Data/CharacterDatabase.asset");
        StringBuilder allyOneStarCsv = new StringBuilder();
        allyOneStarCsv.AppendLine("이름,초기 별,종족,공격력,공격속도,공격범위,최대 HP,이동속도,공격 타입,광역공격,비용");

        if (allyDB != null && allyDB.characters != null)
        {
            foreach (var character in allyDB.characters)
            {
                if (character.initialStar == CharacterStar.OneStar)
                {
                    string raceName = GetRaceName(character.race);
                    string rangeTypeName = GetRangeTypeName(character.rangeType);
                    string areaAttack = character.isAreaAttack ? "예" : "아니오";

                    allyOneStarCsv.AppendLine($"{character.characterName},1,{raceName},{character.attackPower},{character.attackSpeed},{character.attackRange},{character.maxHP},{character.moveSpeed},{rangeTypeName},{areaAttack},{character.cost}");
                }
            }
        }

        // 적 1성 캐릭터 내보내기
        CharacterDatabaseObject enemyDB = AssetDatabase.LoadAssetAtPath<CharacterDatabaseObject>("Assets/Prefabs/Data/opponentCharacterDatabase.asset");
        StringBuilder enemyOneStarCsv = new StringBuilder();
        enemyOneStarCsv.AppendLine("이름,초기 별,종족,공격력,공격속도,공격범위,최대 HP,이동속도,공격 타입,광역공격,비용");

        if (enemyDB != null && enemyDB.characters != null)
        {
            foreach (var character in enemyDB.characters)
            {
                if (character.initialStar == CharacterStar.OneStar)
                {
                    string raceName = GetRaceName(character.race);
                    string rangeTypeName = GetRangeTypeName(character.rangeType);
                    string areaAttack = character.isAreaAttack ? "예" : "아니오";

                    enemyOneStarCsv.AppendLine($"{character.characterName},1,{raceName},{character.attackPower},{character.attackSpeed},{character.attackRange},{character.maxHP},{character.moveSpeed},{rangeTypeName},{areaAttack},{character.cost}");
                }
            }
        }

        // 파일 저장
        string allyOneStarPath = Path.Combine(Application.dataPath, "../ally_one_star_characters.csv");
        string enemyOneStarPath = Path.Combine(Application.dataPath, "../enemy_one_star_characters.csv");

        File.WriteAllText(allyOneStarPath, allyOneStarCsv.ToString(), Encoding.UTF8);
        File.WriteAllText(enemyOneStarPath, enemyOneStarCsv.ToString(), Encoding.UTF8);

        Debug.Log($"아군 1성 캐릭터 CSV 파일 생성 완료: {allyOneStarPath}");
        Debug.Log($"적 1성 캐릭터 CSV 파일 생성 완료: {enemyOneStarPath}");
    }

    private string GetEffectTypeName(ItemEffectType effectType)
    {
        switch (effectType)
        {
            case ItemEffectType.IncreaseAttack: return "공격력";
            case ItemEffectType.IncreaseHP: return "HP";
            case ItemEffectType.IncreaseRange: return "사거리";
            case ItemEffectType.TeleportJumpedEnemies: return "텔레포트";
            case ItemEffectType.DamageJumpedEnemies: return "데미지";
            case ItemEffectType.SummonRandom2Or3Star: return "소환";
            default: return "알 수 없음";
        }
    }

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