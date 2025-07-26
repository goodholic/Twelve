// 통합 공격 패턴 에디터 - 모든 캐릭터 타입과 공격 패턴 기능의 중앙 허브
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Data;
using TacticalTileGame.Data;

/// <summary>
/// 통합 공격 패턴 에디터 시스템
/// CharacterData 타입 지원
/// </summary>
[CustomEditor(typeof(CharacterData))]
public class CharacterAttackPatternEditor : UnityEditor.Editor
{
    private const int GRID_SIZE = 7; // 확장된 그리드 크기
    private bool showPatternEditor = true;
    private bool showPresetButtons = true;
    private bool[,] patternGrid = new bool[GRID_SIZE, GRID_SIZE];
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🎯 통합 공격 패턴 에디터", EditorStyles.boldLabel);
        
        // CharacterData 타입 처리
        if (target is CharacterData characterData)
        {
            DrawCharacterDataPatternEditor(characterData);
        }
        else
        {
            EditorGUILayout.HelpBox("CharacterData 타입만 지원합니다.", MessageType.Warning);
        }
    }
    
    #region CharacterData 패턴 에디터
    
    void DrawCharacterDataPatternEditor(CharacterData character)
    {
        EditorGUILayout.Space();
        
        // 패턴 타입 선택
        character.attackPattern = (AttackPattern)EditorGUILayout.EnumPopup("공격 패턴 타입:", character.attackPattern);
        
        if (character.attackPattern == AttackPattern.Custom)
        {
            showPatternEditor = EditorGUILayout.Foldout(showPatternEditor, "🛠️ 커스텀 패턴 에디터", true);
            
            if (showPatternEditor)
            {
                DrawCustomPatternEditor(character);
            }
        }
        else
        {
            // 프리셋 패턴 미리보기
            EditorGUILayout.LabelField("📋 패턴 미리보기:", EditorStyles.boldLabel);
            DrawPatternPreview(AttackPatternManager.GetPattern(character.attackPattern));
        }
        
        // 프리셋 적용 버튼들
        DrawPresetButtons(character);
    }
    
    void DrawCustomPatternEditor(CharacterData character)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("클릭하여 공격 범위를 설정하세요 (🟢가 캐릭터 위치)", EditorStyles.helpBox);
        
        // 현재 패턴을 그리드에 로드
        LoadCharacterDataPatternToGrid(character);
        
        // 그리드 그리기
        DrawPatternGrid();
        
        // 그리드에서 패턴 저장
        SaveCharacterDataPatternFromGrid(character);
        
        // 유틸리티 버튼들
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🗑️ 패턴 초기화"))
        {
            ClearCharacterDataPattern(character);
        }
        if (GUILayout.Button("📊 패턴 분석"))
        {
            AnalyzePattern(character.GetAttackPositions());
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    #endregion
    
    #region 공통 패턴 관리 메서드들
    
    void DrawPatternGrid()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        EditorGUILayout.BeginVertical();
        for (int y = GRID_SIZE - 1; y >= 0; y--) // 위에서 아래로
        {
            EditorGUILayout.BeginHorizontal();
            
            for (int x = 0; x < GRID_SIZE; x++)
            {
                bool isCenter = (x == GRID_SIZE / 2 && y == GRID_SIZE / 2);
                string buttonText = isCenter ? "🟢" : (patternGrid[x, y] ? "🔴" : "⬜");
                
                GUI.enabled = !isCenter; // 중앙은 클릭 불가
                if (GUILayout.Button(buttonText, GUILayout.Width(30), GUILayout.Height(30)))
                {
                    patternGrid[x, y] = !patternGrid[x, y];
                }
                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }
    
    void DrawPatternPreview(List<Vector2Int> pattern)
    {
        if (pattern == null || pattern.Count == 0)
        {
            EditorGUILayout.LabelField("패턴 없음", EditorStyles.helpBox);
            return;
        }
        
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"공격 범위: {pattern.Count}개 타일", EditorStyles.boldLabel);
        
        // 패턴 그리드 미리보기
        int previewSize = 5;
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        EditorGUILayout.BeginVertical();
        for (int y = previewSize - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < previewSize; x++)
            {
                Vector2Int pos = new Vector2Int(x - previewSize/2, y - previewSize/2);
                bool isCenter = (pos == Vector2Int.zero);
                bool isAttack = pattern.Contains(pos);
                
                string icon = isCenter ? "🟢" : (isAttack ? "🔴" : "⬜");
                GUILayout.Label(icon, GUILayout.Width(25), GUILayout.Height(25));
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawPresetButtons(CharacterData character)
    {
        showPresetButtons = EditorGUILayout.Foldout(showPresetButtons, "🎮 프리셋 패턴", true);
        
        if (showPresetButtons)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("빠른 적용:", EditorStyles.miniBoldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("✚ 십자"))
            {
                ApplyPresetPattern(character, AttackPattern.Cross);
            }
            if (GUILayout.Button("✖️ 대각선"))
            {
                ApplyPresetPattern(character, AttackPattern.Diagonal);
            }
            if (GUILayout.Button("➖ 직선"))
            {
                ApplyPresetPattern(character, AttackPattern.Line);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🐴 나이트"))
            {
                ApplyPresetPattern(character, AttackPattern.Knight);
            }
            if (GUILayout.Button("🌊 건너편"))
            {
                ApplyPresetPattern(character, AttackPattern.CrossBoard);
            }
            if (GUILayout.Button("🎯 커스텀"))
            {
                character.attackPattern = AttackPattern.Custom;
                EditorUtility.SetDirty(character);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
    }
    
    void LoadCharacterDataPatternToGrid(CharacterData character)
    {
        // 그리드 초기화
        for (int x = 0; x < GRID_SIZE; x++)
        {
            for (int y = 0; y < GRID_SIZE; y++)
            {
                patternGrid[x, y] = false;
            }
        }
        
        // 커스텀 패턴 로드
        if (character.attackPattern == AttackPattern.Custom && character.customPattern != null)
        {
            foreach (var pos in character.customPattern)
            {
                int gridX = pos.x + GRID_SIZE / 2;
                int gridY = pos.y + GRID_SIZE / 2;
                
                if (gridX >= 0 && gridX < GRID_SIZE && gridY >= 0 && gridY < GRID_SIZE)
                {
                    patternGrid[gridX, gridY] = true;
                }
            }
        }
    }
    
    void SaveCharacterDataPatternFromGrid(CharacterData character)
    {
        if (character.attackPattern == AttackPattern.Custom)
        {
            character.customPattern.Clear();
            
            for (int x = 0; x < GRID_SIZE; x++)
            {
                for (int y = 0; y < GRID_SIZE; y++)
                {
                    if (patternGrid[x, y])
                    {
                        Vector2Int worldPos = new Vector2Int(x - GRID_SIZE / 2, y - GRID_SIZE / 2);
                        character.customPattern.Add(worldPos);
                    }
                }
            }
            
            EditorUtility.SetDirty(character);
        }
    }
    
    void ClearCharacterDataPattern(CharacterData character)
    {
        if (character.attackPattern == AttackPattern.Custom)
        {
            character.customPattern.Clear();
            
            // 그리드도 클리어
            for (int x = 0; x < GRID_SIZE; x++)
            {
                for (int y = 0; y < GRID_SIZE; y++)
                {
                    patternGrid[x, y] = false;
                }
            }
            
            EditorUtility.SetDirty(character);
        }
    }
    
    void ApplyPresetPattern(CharacterData character, AttackPattern pattern)
    {
        character.attackPattern = pattern;
        if (pattern == AttackPattern.Custom)
        {
            character.customPattern.Clear();
            character.customPattern.AddRange(AttackPatternManager.GetPattern(AttackPattern.Cross));
        }
        EditorUtility.SetDirty(character);
    }
    
    string AnalyzePattern(List<Vector2Int> pattern)
    {
        if (pattern == null || pattern.Count == 0)
            return "패턴 없음";
        
        int minRange = pattern.Min(p => Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)));
        int maxRange = pattern.Max(p => Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)));
        float avgRange = (float)pattern.Average(p => Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)));
        
        bool horizontalSymmetry = pattern.All(p => pattern.Contains(new Vector2Int(-p.x, p.y)));
        bool verticalSymmetry = pattern.All(p => pattern.Contains(new Vector2Int(p.x, -p.y)));
        bool diagonalSymmetry = pattern.All(p => pattern.Contains(new Vector2Int(p.y, p.x)));
        
        List<string> symmetries = new List<string>();
        if (horizontalSymmetry) symmetries.Add("좌우");
        if (verticalSymmetry) symmetries.Add("상하");
        if (diagonalSymmetry) symmetries.Add("대각선");
        
        string result = $"타일 수: {pattern.Count}\n";
        result += $"사거리: {minRange}-{maxRange} (평균 {avgRange:F1})\n";
        result += $"대칭성: {(symmetries.Count > 0 ? string.Join(", ", symmetries) : "없음")}";
        
        EditorUtility.DisplayDialog("패턴 분석 결과", result, "확인");
        return symmetries.Count > 0 ? string.Join(", ", symmetries) : "없음";
    }
    
    #endregion
        
        #region TacticalCharacterDataSO 패턴 에디터 (사용안함 - CharacterData만 지원)

    // TacticalCharacterDataSO 지원 제거됨 - CharacterData만 지원
    /*
    void DrawTacticalDataPatternEditor(TacticalCharacterDataSO tacticalData)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🎯 전술 캐릭터 공격 패턴", EditorStyles.boldLabel);
        
        // CSV 패턴 입력
        EditorGUILayout.LabelField("CSV 패턴 문자열:", EditorStyles.miniBoldLabel);
        string newPatternCSV = EditorGUILayout.TextArea(tacticalData.attackPatternCSV ?? "", GUILayout.Height(60));
        
        if (newPatternCSV != tacticalData.attackPatternCSV)
        {
            tacticalData.attackPatternCSV = newPatternCSV;
            tacticalData.attackPattern = ParseAttackPatternFromString(newPatternCSV);
            EditorUtility.SetDirty(tacticalData);
        }
        
        // 비주얼 에디터
        showPatternEditor = EditorGUILayout.Foldout(showPatternEditor, "🛠️ 비주얼 패턴 에디터", true);
        
        if (showPatternEditor)
        {
            DrawTacticalPatternEditor(tacticalData);
        }
        
        // 현재 패턴 정보
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"📊 현재 패턴: {tacticalData.attackPattern.Count}개 타일", EditorStyles.helpBox);
        DrawPatternPreview(tacticalData.attackPattern);
    }
    
    void DrawTacticalPatternEditor(TacticalCharacterDataSO tacticalData)
    {
        EditorGUILayout.BeginVertical("box");
        
        // 현재 패턴을 그리드에 로드
        LoadTacticalDataPatternToGrid(tacticalData);
        
        // 그리드 그리기
        DrawPatternGrid();
        
        // 그리드에서 패턴 저장
        SaveTacticalDataPatternFromGrid(tacticalData);
        
        // 유틸리티 버튼들
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🗑️ 패턴 초기화"))
        {
            ClearTacticalDataPattern(tacticalData);
        }
        if (GUILayout.Button("📝 CSV 생성"))
        {
            GenerateCSVFromPattern(tacticalData);
        }
        if (GUILayout.Button("📊 패턴 분석"))
        {
            AnalyzePattern(tacticalData.attackPattern);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    #endregion
    
    #region 공통 그리드 및 패턴 관리
    
    void DrawPatternGrid()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        EditorGUILayout.BeginVertical();
        for (int y = GRID_SIZE - 1; y >= 0; y--) // 위에서 아래로
        {
            EditorGUILayout.BeginHorizontal();
            
            for (int x = 0; x < GRID_SIZE; x++)
            {
                // 중앙 표시
                bool isCenter = (x == GRID_SIZE / 2 && y == GRID_SIZE / 2);
                Color originalColor = GUI.backgroundColor;
                
                if (isCenter)
                    GUI.backgroundColor = Color.green;
                else if (patternGrid[x, y])
                    GUI.backgroundColor = Color.red;
                
                // 버튼
                string buttonText = isCenter ? "🟢" : (patternGrid[x, y] ? "🔴" : "⚪");
                if (GUILayout.Button(buttonText, GUILayout.Width(30), GUILayout.Height(30)))
                {
                    if (!isCenter) // 중앙은 클릭 불가
                    {
                        patternGrid[x, y] = !patternGrid[x, y];
                    }
                }
                
                GUI.backgroundColor = originalColor;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }
    
    void DrawPatternPreview(List<Vector2Int> pattern)
    {
        if (pattern == null || pattern.Count == 0)
        {
            EditorGUILayout.LabelField("패턴 없음", EditorStyles.helpBox);
            return;
        }
        
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"📊 패턴 타일 수: {pattern.Count}", EditorStyles.miniBoldLabel);
        
        // 미니 그리드로 패턴 표시
        int previewSize = 5;
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        EditorGUILayout.BeginVertical();
        for (int y = previewSize - 1; y >= -(previewSize - 1); y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = -(previewSize - 1); x < previewSize; x++)
            {
                bool isCenter = (x == 0 && y == 0);
                bool isPattern = pattern.Contains(new Vector2Int(x, y));
                
                string symbol = isCenter ? "🟢" : (isPattern ? "🔴" : "⚫");
                GUILayout.Label(symbol, GUILayout.Width(20), GUILayout.Height(20));
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawPresetButtons(CharacterData character)
    {
        showPresetButtons = EditorGUILayout.Foldout(showPresetButtons, "🎮 프리셋 패턴", true);
        
        if (showPresetButtons)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("빠른 적용:", EditorStyles.miniBoldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("✚ 십자"))
                ApplyPresetPattern(character, AttackPattern.Cross);
            if (GUILayout.Button("✖️ 대각선"))
                ApplyPresetPattern(character, AttackPattern.Diagonal);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("➡️ 직선"))
                ApplyPresetPattern(character, AttackPattern.Line);
            if (GUILayout.Button("🏇 나이트"))
                ApplyPresetPattern(character, AttackPattern.Knight);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🌐 크로스보드"))
                ApplyPresetPattern(character, AttackPattern.CrossBoard);
            if (GUILayout.Button("🎨 커스텀"))
            {
                character.attackPattern = AttackPattern.Custom;
                EditorUtility.SetDirty(character);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
    }
    
    #endregion
    
    #region CharacterData 전용 메서드들
    
    void LoadCharacterDataPatternToGrid(CharacterData character)
    {
        // 그리드 초기화
        for (int x = 0; x < GRID_SIZE; x++)
        {
            for (int y = 0; y < GRID_SIZE; y++)
            {
                patternGrid[x, y] = false;
            }
        }
        
        // 현재 패턴 로드
        List<Vector2Int> pattern = character.GetAttackPositions();
        int center = GRID_SIZE / 2;
        
        foreach (var pos in pattern)
        {
            int gridX = center + pos.x;
            int gridY = center + pos.y;
            
            if (gridX >= 0 && gridX < GRID_SIZE && gridY >= 0 && gridY < GRID_SIZE)
            {
                patternGrid[gridX, gridY] = true;
            }
        }
    }
    
    void SaveCharacterDataPatternFromGrid(CharacterData character)
    {
        if (character.attackPattern != AttackPattern.Custom) return;
        
        character.customPattern.Clear();
        int center = GRID_SIZE / 2;
        
        for (int x = 0; x < GRID_SIZE; x++)
        {
            for (int y = 0; y < GRID_SIZE; y++)
            {
                if (patternGrid[x, y])
                {
                    Vector2Int offset = new Vector2Int(x - center, y - center);
                    character.customPattern.Add(offset);
                }
            }
        }
        
        EditorUtility.SetDirty(character);
    }
    
    void ClearCharacterDataPattern(CharacterData character)
    {
        character.customPattern.Clear();
        
        // 그리드도 초기화
        for (int x = 0; x < GRID_SIZE; x++)
        {
            for (int y = 0; y < GRID_SIZE; y++)
            {
                patternGrid[x, y] = false;
            }
        }
        
        EditorUtility.SetDirty(character);
    }
    
    void ApplyPresetPattern(CharacterData character, AttackPattern preset)
    {
        character.attackPattern = preset;
        if (preset == AttackPattern.Custom)
        {
            character.customPattern.Clear();
            character.customPattern.AddRange(AttackPatternManager.GetPattern(AttackPattern.Cross));
        }
        EditorUtility.SetDirty(character);
    }
    
    #endregion
    
    #region TacticalCharacterDataSO 전용 메서드들 (사용안함)
    
    void LoadTacticalDataPatternToGrid(TacticalCharacterDataSO tacticalData)
    {
        // 그리드 초기화
        for (int x = 0; x < GRID_SIZE; x++)
        {
            for (int y = 0; y < GRID_SIZE; y++)
            {
                patternGrid[x, y] = false;
            }
        }
        
        // 현재 패턴 로드
        int center = GRID_SIZE / 2;
        
        foreach (var pos in tacticalData.attackPattern)
        {
            int gridX = center + pos.x;
            int gridY = center + pos.y;
            
            if (gridX >= 0 && gridX < GRID_SIZE && gridY >= 0 && gridY < GRID_SIZE)
            {
                patternGrid[gridX, gridY] = true;
            }
        }
    }
    
    void SaveTacticalDataPatternFromGrid(TacticalCharacterDataSO tacticalData)
    {
        tacticalData.attackPattern.Clear();
        int center = GRID_SIZE / 2;
        
        for (int x = 0; x < GRID_SIZE; x++)
        {
            for (int y = 0; y < GRID_SIZE; y++)
            {
                if (patternGrid[x, y])
                {
                    Vector2Int offset = new Vector2Int(x - center, y - center);
                    tacticalData.attackPattern.Add(offset);
                }
            }
        }
        
        // CSV 문자열도 업데이트
        GenerateCSVFromPattern(tacticalData);
        EditorUtility.SetDirty(tacticalData);
    }
    
    void ClearTacticalDataPattern(TacticalCharacterDataSO tacticalData)
    {
        tacticalData.attackPattern.Clear();
        tacticalData.attackPatternCSV = "";
        
        // 그리드도 초기화
        for (int x = 0; x < GRID_SIZE; x++)
        {
            for (int y = 0; y < GRID_SIZE; y++)
            {
                patternGrid[x, y] = false;
            }
        }
        
        EditorUtility.SetDirty(tacticalData);
    }
    
    void GenerateCSVFromPattern(TacticalCharacterDataSO tacticalData)
    {
        List<string> positions = new List<string>();
        foreach (var pos in tacticalData.attackPattern)
        {
            positions.Add($"{pos.x},{pos.y}");
        }
        tacticalData.attackPatternCSV = string.Join(";", positions);
    }
    
    List<Vector2Int> ParseAttackPatternFromString(string patternString)
    {
        List<Vector2Int> pattern = new List<Vector2Int>();
        if (string.IsNullOrEmpty(patternString)) return pattern;
        
        string[] positions = patternString.Split(';');
        foreach (string pos in positions)
        {
            string[] coords = pos.Split(',');
            if (coords.Length == 2)
            {
                if (int.TryParse(coords[0], out int x) && int.TryParse(coords[1], out int y))
                {
                    pattern.Add(new Vector2Int(x, y));
                }
            }
        }
        
        return pattern;
    }
    
    #endregion
    
    #region 유틸리티 메서드들
    
    void AnalyzePattern(List<Vector2Int> pattern)
    {
        if (pattern == null || pattern.Count == 0)
        {
            EditorUtility.DisplayDialog("패턴 분석", "분석할 패턴이 없습니다.", "확인");
            return;
        }
        
        // 패턴 분석
        int minX = pattern.Min(p => p.x);
        int maxX = pattern.Max(p => p.x);
        int minY = pattern.Min(p => p.y);
        int maxY = pattern.Max(p => p.y);
        
        float avgDistance = pattern.Average(p => Mathf.Sqrt(p.x * p.x + p.y * p.y));
        
        string analysis = $"📊 패턴 분석 결과\n\n" +
                         $"🎯 타일 수: {pattern.Count}\n" +
                         $"📏 X 범위: {minX} ~ {maxX}\n" +
                         $"📏 Y 범위: {minY} ~ {maxY}\n" +
                         $"📐 평균 거리: {avgDistance:F2}\n" +
                         $"🔄 대칭성: {CheckSymmetry(pattern)}";
        
        EditorUtility.DisplayDialog("패턴 분석", analysis, "확인");
    }
    
    string CheckSymmetry(List<Vector2Int> pattern)
    {
        bool horizontalSymmetry = pattern.All(p => pattern.Contains(new Vector2Int(-p.x, p.y)));
        bool verticalSymmetry = pattern.All(p => pattern.Contains(new Vector2Int(p.x, -p.y)));
        bool diagonalSymmetry = pattern.All(p => pattern.Contains(new Vector2Int(p.y, p.x)));
        
        List<string> symmetries = new List<string>();
        if (horizontalSymmetry) symmetries.Add("좌우");
        if (verticalSymmetry) symmetries.Add("상하");
        if (diagonalSymmetry) symmetries.Add("대각선");
        
        return symmetries.Count > 0 ? string.Join(", ", symmetries) : "없음";
    }
    
    */
    #endregion
}

/// <summary>
/// 통합 공격 패턴 매니저 - 모든 공격 패턴 로직의 중앙 관리
/// CharacterData.cs에서 이동됨
/// </summary>
public static class AttackPatternManager
{
    public static List<Vector2Int> GetPattern(AttackPattern pattern)
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        switch (pattern)
        {
            case AttackPattern.Cross:
                // 십자 패턴
                positions.Add(new Vector2Int(0, 1));   // 위
                positions.Add(new Vector2Int(0, -1));  // 아래
                positions.Add(new Vector2Int(1, 0));   // 오른쪽
                positions.Add(new Vector2Int(-1, 0));  // 왼쪽
                break;

            case AttackPattern.Diagonal:
                // 대각선 패턴
                positions.Add(new Vector2Int(1, 1));    // 우상
                positions.Add(new Vector2Int(1, -1));   // 우하
                positions.Add(new Vector2Int(-1, 1));   // 좌상
                positions.Add(new Vector2Int(-1, -1));  // 좌하
                break;

            case AttackPattern.Line:
                // 직선 패턴 (전방 3칸)
                positions.Add(new Vector2Int(1, 0));
                positions.Add(new Vector2Int(2, 0));
                positions.Add(new Vector2Int(3, 0));
                break;

            case AttackPattern.Knight:
                // ㄹ자 패턴 (체스 나이트)
                positions.Add(new Vector2Int(2, 1));
                positions.Add(new Vector2Int(2, -1));
                positions.Add(new Vector2Int(-2, 1));
                positions.Add(new Vector2Int(-2, -1));
                positions.Add(new Vector2Int(1, 2));
                positions.Add(new Vector2Int(1, -2));
                positions.Add(new Vector2Int(-1, 2));
                positions.Add(new Vector2Int(-1, -2));
                break;

            case AttackPattern.CrossBoard:
                // 건너편 타일 공격은 특별 처리
                // 실제 게임에서는 별도 로직으로 처리
                positions.Add(new Vector2Int(0, 0)); // 플래그용
                break;

            case AttackPattern.Custom:
                // 커스텀 패턴은 각 캐릭터 데이터에서 직접 정의
                break;
        }

        return positions;
    }

    /// <summary>
    /// 건너편 보드 공격 여부 확인
    /// </summary>
    public static bool IsCrossBoardAttack(AttackPattern pattern)
    {
        return pattern == AttackPattern.CrossBoard;
    }
    
    /// <summary>
    /// 패턴 타입 이름 가져오기
    /// </summary>
    public static string GetPatternName(AttackPattern pattern)
    {
        switch (pattern)
        {
            case AttackPattern.Cross: return "십자형";
            case AttackPattern.Diagonal: return "대각선";
            case AttackPattern.Line: return "직선형";
            case AttackPattern.Knight: return "나이트";
            case AttackPattern.CrossBoard: return "크로스보드";
            case AttackPattern.Custom: return "커스텀";
            default: return "알 수 없음";
        }
    }
    
    /// <summary>
    /// 패턴 설명 가져오기
    /// </summary>
    public static string GetPatternDescription(AttackPattern pattern)
    {
        switch (pattern)
        {
            case AttackPattern.Cross: return "상하좌우 4방향 공격";
            case AttackPattern.Diagonal: return "대각선 4방향 공격";
            case AttackPattern.Line: return "전방 직선 3칸 공격";
            case AttackPattern.Knight: return "체스 나이트 패턴 공격";
            case AttackPattern.CrossBoard: return "건너편 보드 전체 공격";
            case AttackPattern.Custom: return "사용자 정의 패턴";
            default: return "";
        }
    }
    
    /// <summary>
    /// 패턴의 최대 사거리 계산
    /// </summary>
    public static int GetMaxRange(AttackPattern pattern)
    {
        var positions = GetPattern(pattern);
        if (positions.Count == 0) return 0;
        
        return Mathf.RoundToInt(positions.Max(p => Mathf.Sqrt(p.x * p.x + p.y * p.y)));
    }
    
    /// <summary>
    /// 두 패턴이 동일한지 비교
    /// </summary>
    public static bool ComparePatterns(List<Vector2Int> pattern1, List<Vector2Int> pattern2)
    {
        if (pattern1 == null || pattern2 == null) return false;
        if (pattern1.Count != pattern2.Count) return false;
        
        var sorted1 = pattern1.OrderBy(p => p.x).ThenBy(p => p.y).ToList();
        var sorted2 = pattern2.OrderBy(p => p.x).ThenBy(p => p.y).ToList();
        
        for (int i = 0; i < sorted1.Count; i++)
        {
            if (sorted1[i] != sorted2[i]) return false;
        }
        
        return true;
    }
}

#endif
