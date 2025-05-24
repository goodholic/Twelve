#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 타일 참조 상태를 실시간으로 모니터링하고 문제를 진단하는 에디터 도구
/// </summary>
public class TileReferenceDebugTool : EditorWindow
{
    private Vector2 scrollPosition;
    private bool autoRefresh = true;
    private float lastUpdateTime = 0f;
    private const float UPDATE_INTERVAL = 1f;
    
    // 진단 결과
    private List<string> diagnosticResults = new List<string>();
    private List<TileReferenceProblem> problems = new List<TileReferenceProblem>();
    
    [System.Serializable]
    public class TileReferenceProblem
    {
        public enum ProblemType
        {
            DuplicateReference,     // 중복 참조
            OrphanedReference,      // 고아 참조
            MissingReference,       // 누락된 참조
            PositionMismatch,       // 위치 불일치
            EmptyPlacedTile         // 비어있는 placed 타일
        }
        
        public ProblemType type;
        public string tileName;
        public string characterName;
        public string description;
        public Vector3 tilePosition;
        public Vector3 characterPosition;
        public float distance;
        public Tile tileReference;
    }

    [MenuItem("Tools/Fusion/타일 참조 디버그 도구")]
    public static void ShowWindow()
    {
        var window = GetWindow<TileReferenceDebugTool>();
        window.titleContent = new GUIContent("타일 참조 디버그");
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("타일 참조 상태 디버그 도구", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 자동 새로고침 설정
        autoRefresh = EditorGUILayout.Toggle("자동 새로고침 (1초마다)", autoRefresh);
        EditorGUILayout.Space();

        // 컨트롤 버튼들
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("전체 진단", GUILayout.Height(30)))
        {
            RunFullDiagnostic();
        }
        if (GUILayout.Button("자동 수정", GUILayout.Height(30)))
        {
            AutoFixProblems();
        }
        if (GUILayout.Button("참조 정리", GUILayout.Height(30)))
        {
            CleanupReferences();
        }
        EditorGUILayout.EndHorizontal();
        
        // ▼▼ [추가] Placed Tile 전용 버튼 ▼▼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Placed Tile 진단", GUILayout.Height(30)))
        {
            DiagnosePlacedTiles();
        }
        if (GUILayout.Button("Placed Tile 수정", GUILayout.Height(30)))
        {
            FixPlacedTiles();
        }
        if (GUILayout.Button("빈 Placed Tile 재설정", GUILayout.Height(30)))
        {
            ResetEmptyPlacedTiles();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();

        // 실시간 상태 표시
        if (Application.isPlaying)
        {
            DisplayRealTimeStatus();
        }
        else
        {
            EditorGUILayout.HelpBox("플레이 모드에서만 사용 가능합니다.", MessageType.Info);
        }
    }
    
    private void Update()
    {
        if (!Application.isPlaying || !autoRefresh) return;
        
        if (Time.time - lastUpdateTime > UPDATE_INTERVAL)
        {
            RunFullDiagnostic();
            lastUpdateTime = Time.time;
            Repaint();
        }
    }

    private void DisplayRealTimeStatus()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // 요약 정보
        EditorGUILayout.LabelField("진단 요약", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"발견된 문제: {problems.Count}개");
        
        if (problems.Count > 0)
        {
            var duplicates = problems.Count(p => p.type == TileReferenceProblem.ProblemType.DuplicateReference);
            var orphaned = problems.Count(p => p.type == TileReferenceProblem.ProblemType.OrphanedReference);
            var missing = problems.Count(p => p.type == TileReferenceProblem.ProblemType.MissingReference);
            var mismatch = problems.Count(p => p.type == TileReferenceProblem.ProblemType.PositionMismatch);
            var emptyPlaced = problems.Count(p => p.type == TileReferenceProblem.ProblemType.EmptyPlacedTile);
            
            EditorGUILayout.LabelField($"- 중복 참조: {duplicates}개");
            EditorGUILayout.LabelField($"- 고아 참조: {orphaned}개");
            EditorGUILayout.LabelField($"- 누락된 참조: {missing}개");
            EditorGUILayout.LabelField($"- 위치 불일치: {mismatch}개");
            EditorGUILayout.LabelField($"- 비어있는 placed 타일: {emptyPlaced}개");
        }
        
        EditorGUILayout.Space();
        
        // 상세 문제 목록
        if (problems.Count > 0)
        {
            EditorGUILayout.LabelField("발견된 문제들", EditorStyles.boldLabel);
            
            foreach (var problem in problems)
            {
                Color originalColor = GUI.color;
                
                switch (problem.type)
                {
                    case TileReferenceProblem.ProblemType.DuplicateReference:
                        GUI.color = Color.yellow;
                        break;
                    case TileReferenceProblem.ProblemType.OrphanedReference:
                        GUI.color = Color.red;
                        break;
                    case TileReferenceProblem.ProblemType.MissingReference:
                        GUI.color = Color.cyan;
                        break;
                    case TileReferenceProblem.ProblemType.PositionMismatch:
                        GUI.color = Color.magenta;
                        break;
                    case TileReferenceProblem.ProblemType.EmptyPlacedTile:
                        GUI.color = new Color(1f, 0.5f, 0f); // Orange
                        break;
                }
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"[{problem.type}] {problem.description}");
                if (!string.IsNullOrEmpty(problem.tileName))
                    EditorGUILayout.LabelField($"타일: {problem.tileName}");
                if (!string.IsNullOrEmpty(problem.characterName))
                    EditorGUILayout.LabelField($"캐릭터: {problem.characterName}");
                if (problem.distance > 0)
                    EditorGUILayout.LabelField($"거리: {problem.distance:F2}");
                
                // 빈 placed 타일의 경우 즉시 수정 버튼
                if (problem.type == TileReferenceProblem.ProblemType.EmptyPlacedTile && problem.tileReference != null)
                {
                    if (GUILayout.Button("이 타일 수정"))
                    {
                        FixSingleEmptyPlacedTile(problem.tileReference);
                    }
                }
                
                EditorGUILayout.EndVertical();
                
                GUI.color = originalColor;
            }
        }
        
        // 진단 로그
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("진단 로그", EditorStyles.boldLabel);
        
        foreach (var result in diagnosticResults)
        {
            EditorGUILayout.LabelField(result, EditorStyles.wordWrappedLabel);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void RunFullDiagnostic()
    {
        if (!Application.isPlaying)
        {
            diagnosticResults.Add("플레이 모드가 아닙니다.");
            return;
        }
        
        problems.Clear();
        diagnosticResults.Clear();
        diagnosticResults.Add($"[{System.DateTime.Now:HH:mm:ss}] 전체 진단 시작");
        
        var tiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        var characters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        diagnosticResults.Add($"타일 {tiles.Length}개, 캐릭터 {characters.Length}개 발견");
        
        // 1. 중복 참조 검사
        CheckDuplicateReferences(tiles, characters);
        
        // 2. 고아 참조 검사
        CheckOrphanedReferences(characters);
        
        // 3. 위치 불일치 검사
        CheckPositionMismatch(characters);
        
        // 4. 누락된 참조 검사
        CheckMissingReferences(tiles, characters);
        
        // 5. 비어있는 placed 타일 검사
        CheckEmptyPlacedTiles(tiles, characters);
        
        diagnosticResults.Add($"진단 완료 - 총 {problems.Count}개 문제 발견");
    }
    
    private void CheckDuplicateReferences(Tile[] tiles, Character[] characters)
    {
        foreach (var tile in tiles)
        {
            if (tile == null) continue;
            
            var occupants = new List<Character>();
            foreach (var character in characters)
            {
                if (character != null && character.currentTile == tile)
                {
                    occupants.Add(character);
                }
            }
            
            if (occupants.Count > 1)
            {
                var problem = new TileReferenceProblem
                {
                    type = TileReferenceProblem.ProblemType.DuplicateReference,
                    tileName = tile.name,
                    description = $"{tile.name}에 {occupants.Count}개의 중복 참조",
                    tilePosition = tile.transform.position,
                    tileReference = tile
                };
                problems.Add(problem);
            }
        }
    }
    
    private void CheckOrphanedReferences(Character[] characters)
    {
        foreach (var character in characters)
        {
            if (character == null || character.currentTile == null) continue;
            
            // 참조하는 타일이 실제로 존재하는지 확인
            var tile = character.currentTile;
            if (tile == null || !tile.gameObject.activeInHierarchy)
            {
                var problem = new TileReferenceProblem
                {
                    type = TileReferenceProblem.ProblemType.OrphanedReference,
                    characterName = character.characterName,
                    description = $"{character.characterName}이 존재하지 않는 타일을 참조",
                    characterPosition = character.transform.position
                };
                problems.Add(problem);
            }
        }
    }
    
    private void CheckPositionMismatch(Character[] characters)
    {
        foreach (var character in characters)
        {
            if (character == null || character.currentTile == null) continue;
            
            float distance = Vector3.Distance(character.transform.position, character.currentTile.transform.position);
            if (distance > 2.0f) // 임계값
            {
                var problem = new TileReferenceProblem
                {
                    type = TileReferenceProblem.ProblemType.PositionMismatch,
                    tileName = character.currentTile.name,
                    characterName = character.characterName,
                    description = $"{character.characterName}과 {character.currentTile.name}의 위치가 불일치",
                    tilePosition = character.currentTile.transform.position,
                    characterPosition = character.transform.position,
                    distance = distance,
                    tileReference = character.currentTile
                };
                problems.Add(problem);
            }
        }
    }
    
    private void CheckMissingReferences(Tile[] tiles, Character[] characters)
    {
        foreach (var tile in tiles)
        {
            if (tile == null) continue;
            
            // 타일 위치에 캐릭터가 있지만 참조가 없는 경우
            foreach (var character in characters)
            {
                if (character == null) continue;
                
                float distance = Vector3.Distance(character.transform.position, tile.transform.position);
                if (distance < 0.5f && character.currentTile != tile)
                {
                    var problem = new TileReferenceProblem
                    {
                        type = TileReferenceProblem.ProblemType.MissingReference,
                        tileName = tile.name,
                        characterName = character.characterName,
                        description = $"{character.characterName}이 {tile.name} 위에 있지만 참조가 없음",
                        distance = distance,
                        tileReference = tile
                    };
                    problems.Add(problem);
                }
            }
        }
    }
    
    private void CheckEmptyPlacedTiles(Tile[] tiles, Character[] characters)
    {
        foreach (var tile in tiles)
        {
            if (tile == null) continue;
            
            // PlaceTile 또는 Placed2인 타일만 검사
            if (tile.IsPlaceTile() || tile.IsPlaced2())
            {
                // 해당 타일을 참조하는 캐릭터가 있는지 확인
                bool hasCharacter = false;
                foreach (var c in characters)
                {
                    if (c != null && c.currentTile == tile)
                    {
                        hasCharacter = true;
                        break;
                    }
                }
                
                // 캐릭터가 없는 placed 타일은 문제
                if (!hasCharacter)
                {
                    var problem = new TileReferenceProblem
                    {
                        type = TileReferenceProblem.ProblemType.EmptyPlacedTile,
                        tileName = tile.name,
                        description = $"{tile.name}은 placed 타일이지만 캐릭터가 없음",
                        tilePosition = tile.transform.position,
                        tileReference = tile
                    };
                    problems.Add(problem);
                }
            }
        }
    }
    
    private void AutoFixProblems()
    {
        if (!Application.isPlaying)
        {
            diagnosticResults.Add("플레이 모드가 아닙니다.");
            return;
        }
        
        var placementManager = PlacementManager.Instance;
        if (placementManager == null)
        {
            diagnosticResults.Add("PlacementManager를 찾을 수 없습니다.");
            return;
        }
        
        int fixedCount = 0;
        
        foreach (var problem in problems)
        {
            switch (problem.type)
            {
                case TileReferenceProblem.ProblemType.DuplicateReference:
                case TileReferenceProblem.ProblemType.OrphanedReference:
                case TileReferenceProblem.ProblemType.PositionMismatch:
                    placementManager.CleanupDestroyedCharacterReferences();
                    fixedCount++;
                    break;
                case TileReferenceProblem.ProblemType.EmptyPlacedTile:
                    if (problem.tileReference != null)
                    {
                        FixSingleEmptyPlacedTile(problem.tileReference);
                        fixedCount++;
                    }
                    break;
            }
        }
        
        diagnosticResults.Add($"자동 수정 완료 - {fixedCount}개 문제 해결 시도");
        RunFullDiagnostic(); // 재진단
    }
    
    private void CleanupReferences()
    {
        if (!Application.isPlaying)
        {
            diagnosticResults.Add("플레이 모드가 아닙니다.");
            return;
        }
        
        var placementManager = PlacementManager.Instance;
        if (placementManager == null)
        {
            diagnosticResults.Add("PlacementManager를 찾을 수 없습니다.");
            return;
        }
        
        placementManager.CleanupAllTileReferences();
        diagnosticResults.Add("전체 참조 정리 완료");
        
        RunFullDiagnostic(); // 재진단
    }
    
    // ▼▼ [추가] Placed Tile 전용 진단 메서드 ▼▼
    private void DiagnosePlacedTiles()
    {
        if (!Application.isPlaying)
        {
            diagnosticResults.Add("플레이 모드가 아닙니다.");
            return;
        }
        
        diagnosticResults.Clear();
        problems.Clear();
        diagnosticResults.Add($"[{System.DateTime.Now:HH:mm:ss}] Placed Tile 진단 시작");
        
        var tiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        var characters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        int placedTileCount = 0;
        int problemCount = 0;
        
        foreach (var tile in tiles)
        {
            if (tile == null) continue;
            
            // PlaceTile 또는 Placed2인 타일만 검사
            if (tile.IsPlaceTile() || tile.IsPlaced2())
            {
                placedTileCount++;
                
                // 해당 타일을 참조하는 캐릭터 찾기
                Character occupant = null;
                foreach (var c in characters)
                {
                    if (c != null && c.currentTile == tile)
                    {
                        occupant = c;
                        break;
                    }
                }
                
                // PlaceTile/Placed2 자식 존재 여부 확인
                bool hasPlaceTileChild = (tile.transform.Find("PlaceTile") != null || tile.transform.Find("Placed2") != null);
                
                // 문제 감지
                if (occupant == null && hasPlaceTileChild)
                {
                    problemCount++;
                    var problem = new TileReferenceProblem
                    {
                        type = TileReferenceProblem.ProblemType.OrphanedReference,
                        tileName = tile.name,
                        description = $"{tile.name}에 PlaceTile 자식이 있지만 캐릭터가 없음",
                        tileReference = tile
                    };
                    problems.Add(problem);
                }
                else if (occupant != null && !hasPlaceTileChild)
                {
                    problemCount++;
                    var problem = new TileReferenceProblem
                    {
                        type = TileReferenceProblem.ProblemType.MissingReference,
                        tileName = tile.name,
                        characterName = occupant.characterName,
                        description = $"{tile.name}에 캐릭터가 있지만 PlaceTile 자식이 없음",
                        tileReference = tile
                    };
                    problems.Add(problem);
                }
                else if (occupant == null)
                {
                    // 빈 placed 타일
                    problemCount++;
                    var problem = new TileReferenceProblem
                    {
                        type = TileReferenceProblem.ProblemType.EmptyPlacedTile,
                        tileName = tile.name,
                        description = $"{tile.name}은 placed 타일이지만 비어있음",
                        tileReference = tile
                    };
                    problems.Add(problem);
                }
            }
        }
        
        diagnosticResults.Add($"Placed Tile 진단 완료: 총 {placedTileCount}개 타일 중 {problemCount}개 문제 발견");
    }
    
    private void FixPlacedTiles()
    {
        if (!Application.isPlaying)
        {
            diagnosticResults.Add("플레이 모드가 아닙니다.");
            return;
        }
        
        var placementManager = PlacementManager.Instance;
        if (placementManager == null)
        {
            diagnosticResults.Add("PlacementManager를 찾을 수 없습니다.");
            return;
        }
        
        diagnosticResults.Add($"[{System.DateTime.Now:HH:mm:ss}] Placed Tile 수정 시작");
        
        var tiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        var characters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        int fixedCount = 0;
        
        foreach (var tile in tiles)
        {
            if (tile == null) continue;
            
            if (tile.IsPlaceTile() || tile.IsPlaced2())
            {
                bool hasCharacter = false;
                foreach (var c in characters)
                {
                    if (c != null && c.currentTile == tile)
                    {
                        hasCharacter = true;
                        break;
                    }
                }
                
                bool hasPlaceTileChild = (tile.transform.Find("PlaceTile") != null || tile.transform.Find("Placed2") != null);
                
                // 상태 불일치 수정
                if (hasCharacter && !hasPlaceTileChild)
                {
                    // placed tile은 이미 PlaceTile/Placed2 타입이므로 비주얼 업데이트만
                    tile.RefreshTileVisual();
                    fixedCount++;
                    diagnosticResults.Add($"✓ {tile.name} placed tile 비주얼 업데이트");
                }
                else if (!hasCharacter && hasPlaceTileChild)
                {
                    // placed tile에서 자식이 있는데 캐릭터가 없는 경우는 비정상
                    // 하지만 placed tile 자체가 PlaceTile/Placed2 이름을 가지므로 무시
                    tile.RefreshTileVisual();
                    fixedCount++;
                    diagnosticResults.Add($"✓ {tile.name} placed tile 비주얼 업데이트");
                }
            }
        }
        
        diagnosticResults.Add($"Placed Tile 수정 완료: {fixedCount}개 타일 수정됨");
        
        // 재진단
        DiagnosePlacedTiles();
    }
    
    private void ResetEmptyPlacedTiles()
    {
        if (!Application.isPlaying)
        {
            diagnosticResults.Add("플레이 모드가 아닙니다.");
            return;
        }
        
        diagnosticResults.Add($"[{System.DateTime.Now:HH:mm:ss}] 빈 Placed Tile 재설정 시작");
        
        var tiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
        var characters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        int resetCount = 0;
        
        foreach (var tile in tiles)
        {
            if (tile == null) continue;
            
            if (tile.IsPlaceTile() || tile.IsPlaced2())
            {
                bool hasCharacter = false;
                foreach (var c in characters)
                {
                    if (c != null && c.currentTile == tile)
                    {
                        hasCharacter = true;
                        break;
                    }
                }
                
                if (!hasCharacter)
                {
                    // 빈 placed 타일 강제 재설정
                    FixSingleEmptyPlacedTile(tile);
                    resetCount++;
                }
            }
        }
        
        diagnosticResults.Add($"빈 Placed Tile 재설정 완료: {resetCount}개 타일 재설정됨");
        
        // 재진단
        RunFullDiagnostic();
    }
    
    private void FixSingleEmptyPlacedTile(Tile tile)
    {
        if (tile == null) return;
        
        // 타일의 비주얼을 업데이트
        tile.RefreshTileVisual();
        
        // PlaceTile/Placed2 자식이 있으면 제거
        Transform placeTileChild = tile.transform.Find("PlaceTile");
        if (placeTileChild != null)
        {
            DestroyImmediate(placeTileChild.gameObject);
        }
        
        Transform placed2Child = tile.transform.Find("Placed2");
        if (placed2Child != null)
        {
            DestroyImmediate(placed2Child.gameObject);
        }
        
        diagnosticResults.Add($"✓ {tile.name} placed tile을 빈 상태로 재설정");
    }
}
#endif 