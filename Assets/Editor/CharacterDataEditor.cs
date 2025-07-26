using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
[CustomEditor(typeof(CharacterData))]
public class CharacterDataEditor : UnityEditor.Editor
{
    private bool showPatternEditor = false;
    private const int GRID_SIZE = 7; // 7x7 그리드 (중앙 기준으로 -3 ~ +3)
    private bool[,] patternGrid = new bool[GRID_SIZE, GRID_SIZE];
    
    public override void OnInspectorGUI()
    {
        CharacterData character = (CharacterData)target;
        
        // 기본 인스펙터 표시
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        
        // 커스텀 패턴인 경우에만 에디터 표시
        if (character.attackPattern == AttackPattern.Custom)
        {
            showPatternEditor = EditorGUILayout.Foldout(showPatternEditor, "공격 패턴 에디터", true);
            
            if (showPatternEditor)
            {
                DrawPatternEditor(character);
            }
        }
        else
        {
            // 프리셋 패턴 미리보기
            EditorGUILayout.LabelField("공격 패턴 미리보기:");
            DrawPatternPreview(character);
        }
    }
    
    void DrawPatternEditor(CharacterData character)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("클릭하여 공격 범위를 설정하세요 (중앙이 캐릭터 위치)");
        
        // 현재 패턴을 그리드에 로드
        LoadPatternToGrid(character);
        
        // 그리드 그리기
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        for (int y = GRID_SIZE - 1; y >= 0; y--) // 위에서 아래로
        {
            EditorGUILayout.BeginVertical();
            
            for (int x = 0; x < GRID_SIZE; x++)
            {
                EditorGUILayout.BeginHorizontal();
                
                // 중앙 표시
                bool isCenter = (x == GRID_SIZE / 2 && y == GRID_SIZE / 2);
                Color originalColor = GUI.backgroundColor;
                
                if (isCenter)
                    GUI.backgroundColor = Color.green;
                else if (patternGrid[x, y])
                    GUI.backgroundColor = Color.red;
                
                // 버튼
                if (GUILayout.Button(isCenter ? "●" : (patternGrid[x, y] ? "X" : ""), 
                    GUILayout.Width(25), GUILayout.Height(25)))
                {
                    if (!isCenter) // 중앙은 클릭 불가
                    {
                        patternGrid[x, y] = !patternGrid[x, y];
                        SavePatternFromGrid(character);
                    }
                }
                
                GUI.backgroundColor = originalColor;
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        // 패턴 클리어 버튼
        if (GUILayout.Button("패턴 초기화"))
        {
            ClearPattern(character);
        }
        
        // 프리셋 패턴 적용 버튼들
        EditorGUILayout.LabelField("프리셋 패턴 적용:");
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("십자"))
            ApplyPresetPattern(character, AttackPattern.Cross);
        if (GUILayout.Button("대각선"))
            ApplyPresetPattern(character, AttackPattern.Diagonal);
        if (GUILayout.Button("직선"))
            ApplyPresetPattern(character, AttackPattern.Line);
        if (GUILayout.Button("나이트"))
            ApplyPresetPattern(character, AttackPattern.Knight);
            
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawPatternPreview(CharacterData character)
    {
        List<Vector2Int> pattern = character.GetAttackPositions();
        
        if (pattern.Count == 0)
        {
            EditorGUILayout.LabelField("패턴 없음");
            return;
        }
        
        // 패턴 범위 계산
        int minX = 0, maxX = 0, minY = 0, maxY = 0;
        foreach (var pos in pattern)
        {
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }
        
        int width = maxX - minX + 1;
        int height = maxY - minY + 1;
        
        // 미리보기 그리드 그리기
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        for (int y = maxY; y >= minY; y--)
        {
            EditorGUILayout.BeginVertical();
            
            for (int x = minX; x <= maxX; x++)
            {
                EditorGUILayout.BeginHorizontal();
                
                bool isCenter = (x == 0 && y == 0);
                bool isAttack = pattern.Contains(new Vector2Int(x, y));
                
                Color originalColor = GUI.backgroundColor;
                if (isCenter)
                    GUI.backgroundColor = Color.green;
                else if (isAttack)
                    GUI.backgroundColor = Color.red;
                
                GUILayout.Button(isCenter ? "●" : (isAttack ? "X" : ""), 
                    GUILayout.Width(25), GUILayout.Height(25));
                
                GUI.backgroundColor = originalColor;
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }
    
    void LoadPatternToGrid(CharacterData character)
    {
        // 그리드 초기화
        for (int x = 0; x < GRID_SIZE; x++)
            for (int y = 0; y < GRID_SIZE; y++)
                patternGrid[x, y] = false;
        
        // 패턴 로드
        if (character.customPattern == null) return;
        
        foreach (var pos in character.customPattern)
        {
            int gridX = pos.x + GRID_SIZE / 2;
            int gridY = pos.y + GRID_SIZE / 2;
            
            if (gridX >= 0 && gridX < GRID_SIZE && gridY >= 0 && gridY < GRID_SIZE)
                patternGrid[gridX, gridY] = true;
        }
    }
    
    void SavePatternFromGrid(CharacterData character)
    {
        if (character.customPattern == null)
            character.customPattern = new List<Vector2Int>();
        else
            character.customPattern.Clear();
        
        for (int x = 0; x < GRID_SIZE; x++)
        {
            for (int y = 0; y < GRID_SIZE; y++)
            {
                if (patternGrid[x, y])
                {
                    int relativeX = x - GRID_SIZE / 2;
                    int relativeY = y - GRID_SIZE / 2;
                    character.customPattern.Add(new Vector2Int(relativeX, relativeY));
                }
            }
        }
        
        EditorUtility.SetDirty(character);
    }
    
    void ClearPattern(CharacterData character)
    {
        character.customPattern.Clear();
        LoadPatternToGrid(character);
        EditorUtility.SetDirty(character);
    }
    
    void ApplyPresetPattern(CharacterData character, AttackPattern preset)
    {
        character.customPattern.Clear();
        character.customPattern.AddRange(AttackPatternManager.GetPattern(preset));
        LoadPatternToGrid(character);
        EditorUtility.SetDirty(character);
    }
}

// 씬에서 공격 범위를 시각화하는 에디터 윈도우
public class AttackPatternVisualizerWindow : EditorWindow
{
    private CharacterData selectedCharacter;
    private GameObject previewObject;
    
    [MenuItem("Tools/OX Game/Attack Pattern Visualizer")]
    public static void ShowWindow()
    {
        GetWindow<AttackPatternVisualizerWindow>("공격 패턴 시각화");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("공격 패턴 시각화 도구", EditorStyles.boldLabel);
        
        selectedCharacter = (CharacterData)EditorGUILayout.ObjectField(
            "캐릭터 선택", selectedCharacter, typeof(CharacterData), false);
        
        if (selectedCharacter != null)
        {
            EditorGUILayout.LabelField($"캐릭터: {selectedCharacter.characterName}");
            EditorGUILayout.LabelField($"공격 패턴: {selectedCharacter.attackPattern}");
            
            if (GUILayout.Button("씬에서 미리보기"))
            {
                ShowPreviewInScene();
            }
            
            if (GUILayout.Button("미리보기 제거"))
            {
                ClearPreview();
            }
        }
    }
    
    void ShowPreviewInScene()
    {
        ClearPreview();
        
        previewObject = new GameObject("Attack Pattern Preview");
        List<Vector2Int> pattern = selectedCharacter.GetAttackPositions();
        
        // 중앙 표시
        CreatePreviewTile(Vector3.zero, Color.green, "Center");
        
        // 공격 범위 표시
        foreach (var pos in pattern)
        {
            Vector3 worldPos = new Vector3(pos.x, pos.y, 0);
            CreatePreviewTile(worldPos, Color.red, "Attack");
        }
    }
    
    void CreatePreviewTile(Vector3 position, Color color, string name)
    {
        GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
        tile.name = name;
        tile.transform.parent = previewObject.transform;
        tile.transform.position = position;
        tile.transform.localScale = Vector3.one * 0.9f;
        
        Renderer renderer = tile.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.material.color = color;
    }
    
    void ClearPreview()
    {
        if (previewObject != null)
            DestroyImmediate(previewObject);
    }
    
    void OnDestroy()
    {
        ClearPreview();
    }
}
#endif