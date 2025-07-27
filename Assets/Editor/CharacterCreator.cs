using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using TwelveGame.Battle; // GameManager 참조를 위해 추가

#if UNITY_EDITOR
public class CharacterCreator : EditorWindow
{
    [MenuItem("Twelve/🛠️ Development Tools/Create Default Characters")]
    public static void CreateDefaultCharacters()
    {
        string folderPath = "Assets/Characters";
        
        // 폴더가 없으면 생성
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Characters");
        }
        
        // X팀 캐릭터 생성
        CreateXTeamCharacters(folderPath);
        
        // O팀 캐릭터 생성
        CreateOTeamCharacters(folderPath);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("기본 캐릭터들이 생성되었습니다!");
    }
    
    static void CreateXTeamCharacters(string folderPath)
    {
        // X팀 캐릭터 1: 십자 전사
        CharacterData xChar1 = ScriptableObject.CreateInstance<CharacterData>();
        xChar1.characterName = "X 십자 전사";
        xChar1.hp = 100;
        xChar1.attackPower = 50;
        xChar1.attackPattern = AttackPattern.Cross;
        AssetDatabase.CreateAsset(xChar1, $"{folderPath}/X_CrossWarrior.asset");
        
        // X팀 캐릭터 2: 대각선 마법사
        CharacterData xChar2 = ScriptableObject.CreateInstance<CharacterData>();
        xChar2.characterName = "X 대각선 마법사";
        xChar2.hp = 80;
        xChar2.attackPower = 60;
        xChar2.attackPattern = AttackPattern.Diagonal;
        AssetDatabase.CreateAsset(xChar2, $"{folderPath}/X_DiagonalMage.asset");
        
        // X팀 캐릭터 3: 직선 궁수
        CharacterData xChar3 = ScriptableObject.CreateInstance<CharacterData>();
        xChar3.characterName = "X 직선 궁수";
        xChar3.hp = 70;
        xChar3.attackPower = 70;
        xChar3.attackPattern = AttackPattern.Line;
        AssetDatabase.CreateAsset(xChar3, $"{folderPath}/X_LineArcher.asset");
        
        // X팀 캐릭터 4: 나이트
        CharacterData xChar4 = ScriptableObject.CreateInstance<CharacterData>();
        xChar4.characterName = "X 나이트";
        xChar4.hp = 120;
        xChar4.attackPower = 40;
        xChar4.attackPattern = AttackPattern.Knight;
        AssetDatabase.CreateAsset(xChar4, $"{folderPath}/X_Knight.asset");
        
        // X팀 캐릭터 5: 건너편 공격자
        CharacterData xChar5 = ScriptableObject.CreateInstance<CharacterData>();
        xChar5.characterName = "X 포격수";
        xChar5.hp = 60;
        xChar5.attackPower = 80;
        xChar5.attackPattern = AttackPattern.CrossBoard;
        AssetDatabase.CreateAsset(xChar5, $"{folderPath}/X_Artillery.asset");
        
        // X팀 캐릭터 6-10: 커스텀 패턴
        for (int i = 6; i <= 10; i++)
        {
            CharacterData xChar = ScriptableObject.CreateInstance<CharacterData>();
            xChar.characterName = $"X 특수병 {i-5}";
            xChar.hp = 90;
            xChar.attackPower = 55;
            xChar.attackPattern = AttackPattern.Custom;
            
            // 간단한 커스텀 패턴 생성
            GenerateSimpleCustomPattern(xChar, i);
            
            AssetDatabase.CreateAsset(xChar, $"{folderPath}/X_Special{i-5}.asset");
        }
    }
    
    static void CreateOTeamCharacters(string folderPath)
    {
        // O팀 캐릭터 1: 십자 방패병
        CharacterData oChar1 = ScriptableObject.CreateInstance<CharacterData>();
        oChar1.characterName = "O 십자 방패병";
        oChar1.hp = 110;
        oChar1.attackPower = 45;
        oChar1.attackPattern = AttackPattern.Cross;
        AssetDatabase.CreateAsset(oChar1, $"{folderPath}/O_CrossShield.asset");
        
        // O팀 캐릭터 2: 대각선 암살자
        CharacterData oChar2 = ScriptableObject.CreateInstance<CharacterData>();
        oChar2.characterName = "O 대각선 암살자";
        oChar2.hp = 75;
        oChar2.attackPower = 65;
        oChar2.attackPattern = AttackPattern.Diagonal;
        AssetDatabase.CreateAsset(oChar2, $"{folderPath}/O_DiagonalAssassin.asset");
        
        // O팀 캐릭터 3: 직선 창병
        CharacterData oChar3 = ScriptableObject.CreateInstance<CharacterData>();
        oChar3.characterName = "O 직선 창병";
        oChar3.hp = 85;
        oChar3.attackPower = 55;
        oChar3.attackPattern = AttackPattern.Line;
        AssetDatabase.CreateAsset(oChar3, $"{folderPath}/O_LineSpear.asset");
        
        // O팀 캐릭터 4: 기마병
        CharacterData oChar4 = ScriptableObject.CreateInstance<CharacterData>();
        oChar4.characterName = "O 기마병";
        oChar4.hp = 100;
        oChar4.attackPower = 50;
        oChar4.attackPattern = AttackPattern.Knight;
        AssetDatabase.CreateAsset(oChar4, $"{folderPath}/O_Cavalry.asset");
        
        // O팀 캐릭터 5: 대포병
        CharacterData oChar5 = ScriptableObject.CreateInstance<CharacterData>();
        oChar5.characterName = "O 대포병";
        oChar5.hp = 65;
        oChar5.attackPower = 75;
        oChar5.attackPattern = AttackPattern.CrossBoard;
        AssetDatabase.CreateAsset(oChar5, $"{folderPath}/O_Cannon.asset");
        
        // O팀 캐릭터 6-10: 커스텀 패턴
        for (int i = 6; i <= 10; i++)
        {
            CharacterData oChar = ScriptableObject.CreateInstance<CharacterData>();
            oChar.characterName = $"O 정예병 {i-5}";
            oChar.hp = 95;
            oChar.attackPower = 52;
            oChar.attackPattern = AttackPattern.Custom;
            
            // 간단한 커스텀 패턴 생성
            GenerateSimpleCustomPattern(oChar, i + 10);
            
            AssetDatabase.CreateAsset(oChar, $"{folderPath}/O_Elite{i-5}.asset");
        }
    }
    
    static void GenerateSimpleCustomPattern(CharacterData character, int seed)
    {
        Random.InitState(seed);
        character.customPattern.Clear();
        
        // 간단한 패턴 생성 (3-5개의 공격 위치)
        int patternCount = Random.Range(3, 6);
        
        for (int i = 0; i < patternCount; i++)
        {
            Vector2Int pos = new Vector2Int(
                Random.Range(-2, 3),
                Random.Range(-2, 3)
            );
            
            // 중복 제거
            if (!character.customPattern.Contains(pos) && pos != Vector2Int.zero)
            {
                character.customPattern.Add(pos);
            }
        }
    }
}

// 게임 매니저에 캐릭터를 자동으로 할당하는 도구
public class CharacterPoolAssigner : EditorWindow
{
    private GameManager gameManager;
    
    [MenuItem("Twelve/🛠️ Development Tools/Assign Character Pools")]
    public static void ShowWindow()
    {
        GetWindow<CharacterPoolAssigner>("캐릭터 풀 할당");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("캐릭터 풀 자동 할당", EditorStyles.boldLabel);
        
        gameManager = (GameManager)EditorGUILayout.ObjectField(
            "Game Manager", gameManager, typeof(GameManager), true);
        
        if (gameManager == null)
        {
            EditorGUILayout.HelpBox("씬에서 GameManager를 선택하세요.", MessageType.Info);
            
            if (GUILayout.Button("씬에서 GameManager 찾기"))
            {
                gameManager = FindObjectOfType<GameManager>();
            }
            return;
        }
        
        if (GUILayout.Button("X팀 캐릭터 자동 할당"))
        {
            AssignXTeamCharacters();
        }
        
        if (GUILayout.Button("O팀 캐릭터 자동 할당"))
        {
            AssignOTeamCharacters();
        }
        
        if (GUILayout.Button("모든 캐릭터 자동 할당"))
        {
            AssignXTeamCharacters();
            AssignOTeamCharacters();
        }
    }
    
    void AssignXTeamCharacters()
    {
        string[] xCharacterPaths = {
            "Assets/Characters/X_CrossWarrior.asset",
            "Assets/Characters/X_DiagonalMage.asset",
            "Assets/Characters/X_LineArcher.asset",
            "Assets/Characters/X_Knight.asset",
            "Assets/Characters/X_Artillery.asset",
            "Assets/Characters/X_Special1.asset",
            "Assets/Characters/X_Special2.asset",
            "Assets/Characters/X_Special3.asset",
            "Assets/Characters/X_Special4.asset",
            "Assets/Characters/X_Special5.asset"
        };
        
        gameManager.xTeamPool.Clear();
        
        foreach (string path in xCharacterPaths)
        {
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (character != null)
            {
                gameManager.xTeamPool.Add(character);
            }
        }
        
        EditorUtility.SetDirty(gameManager);
        Debug.Log($"X팀 캐릭터 {gameManager.xTeamPool.Count}개 할당 완료!");
    }
    
    void AssignOTeamCharacters()
    {
        string[] oCharacterPaths = {
            "Assets/Characters/O_CrossShield.asset",
            "Assets/Characters/O_DiagonalAssassin.asset",
            "Assets/Characters/O_LineSpear.asset",
            "Assets/Characters/O_Cavalry.asset",
            "Assets/Characters/O_Cannon.asset",
            "Assets/Characters/O_Elite1.asset",
            "Assets/Characters/O_Elite2.asset",
            "Assets/Characters/O_Elite3.asset",
            "Assets/Characters/O_Elite4.asset",
            "Assets/Characters/O_Elite5.asset"
        };
        
        gameManager.oTeamPool.Clear();
        
        foreach (string path in oCharacterPaths)
        {
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (character != null)
            {
                gameManager.oTeamPool.Add(character);
            }
        }
        
        EditorUtility.SetDirty(gameManager);
        Debug.Log($"O팀 캐릭터 {gameManager.oTeamPool.Count}개 할당 완료!");
    }
}
#endif