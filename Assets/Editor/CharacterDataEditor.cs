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
        
        // 통합 공격 패턴 에디터 안내
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("🎯 공격 패턴 편집", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("더 강력한 패턴 에디터를 사용하려면 Inspector에서", EditorStyles.helpBox);
        EditorGUILayout.LabelField("'CharacterAttackPatternEditor'를 확인하세요!", EditorStyles.helpBox);
        
        // 간단한 패턴 정보만 표시
        EditorGUILayout.LabelField($"현재 패턴: {character.attackPattern}");
        if (character.attackPattern == AttackPattern.Custom)
        {
            EditorGUILayout.LabelField($"커스텀 타일 수: {character.customPattern.Count}");
        }
        EditorGUILayout.EndVertical();
    }
    
    // 구 패턴 에디터 기능 - CharacterAttackPatternEditor.cs로 완전 이동됨
    // 이 메서드는 더 이상 사용되지 않음
    
    // 패턴 미리보기 기능을 CharacterAttackPatternEditor.cs로 이동
    void DrawPatternPreview_Legacy(CharacterData character)
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
    
    // 중복 패턴 관리 메서드들을 CharacterAttackPatternEditor.cs로 완전 통합
    // LoadPatternToGrid, SavePatternFromGrid, ClearPattern, ApplyPresetPattern
    // 이제 더 강력한 통합 에디터에서 모든 기능을 제공합니다
}

// 씬에서 공격 범위를 시각화하는 에디터 윈도우
// AttackPatternVisualizerWindow 기능이 CharacterAttackPatternEditor.cs로 완전 통합됨
// 더 강력한 기능을 제공하는 통합 에디터를 사용하세요
[System.Obsolete("Use CharacterAttackPatternEditor instead")]
public class AttackPatternVisualizerWindow_Legacy : EditorWindow
{
    private CharacterData selectedCharacter;
    private GameObject previewObject;
    
    // Attack Pattern Visualizer는 더 이상 사용하지 않음 - 메뉴 항목 제거
    // [MenuItem("Twelve/🛠️ Development Tools/Attack Pattern Visualizer")]
    public static void ShowWindow()
    {
        // AttackPatternVisualizerWindow 제거됨 - CharacterAttackPatternEditor 사용
        EditorUtility.DisplayDialog("알림", "패턴 시각화는 CharacterAttackPatternEditor에서 제공됩니다.", "확인");
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