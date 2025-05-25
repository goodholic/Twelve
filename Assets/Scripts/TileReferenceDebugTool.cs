using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TileReferenceDebugTool : MonoBehaviour
{
    [Header("디버그 설정")]
    [SerializeField] private bool enableRealtimeCheck = true;
    [SerializeField] private float checkInterval = 0.5f; // 0.5초마다 체크
    
    [Header("실시간 타일 상태")]
    [SerializeField] private List<TileStatus> tileStatuses = new List<TileStatus>();
    
    [System.Serializable]
    public class TileStatus
    {
        public string tileName;
        public string tileType;
        public bool hasCharacter;
        public string characterName;
        public bool hasPlaceTileChild;
        public bool isProblematic;
        public string problemDescription;
    }
    
    private void Start()
    {
        if (enableRealtimeCheck)
        {
            InvokeRepeating(nameof(CheckAllTileReferences), 0f, checkInterval);
        }
    }
    
    [ContextMenu("타일 참조 상태 즉시 체크")]
    public void CheckAllTileReferences()
    {
        tileStatuses.Clear();
        
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None)
            .Where(t => t != null)
            .ToArray();
            
        Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None)
            .Where(c => c != null)
            .ToArray();
        
        foreach (var tile in allTiles)
        {
            if (tile == null) continue;
            
            TileStatus status = new TileStatus();
            status.tileName = tile.name;
            
            // 타일 타입 확인
            if (tile.IsPlaceTile()) status.tileType = "PlaceTile";
            else if (tile.IsPlaced2()) status.tileType = "Placed2";
            else if (tile.IsPlacable()) status.tileType = "Placable";
            else if (tile.IsPlacable2()) status.tileType = "Placable2";
            else if (tile.IsWalkable()) status.tileType = "Walkable";
            else if (tile.IsWalkable2()) status.tileType = "Walkable2";
            else status.tileType = "Unknown";
            
            // 캐릭터 존재 확인
            Character occupant = null;
            foreach (var c in allChars)
            {
                if (c != null && c.currentTile == tile)
                {
                    occupant = c;
                    break;
                }
            }
            
            status.hasCharacter = (occupant != null);
            status.characterName = occupant != null ? occupant.characterName : "없음";
            
            // PlaceTile 자식 존재 확인
            bool hasPlaceTileChild = tile.transform.Find("PlaceTile") != null || 
                                   tile.transform.Find("Placed2") != null;
            status.hasPlaceTileChild = hasPlaceTileChild;
            
            // 문제 상황 체크
            CheckProblematicSituation(tile, status, occupant, hasPlaceTileChild);
            
            tileStatuses.Add(status);
        }
        
        // 문제가 있는 타일만 로그 출력
        foreach (var status in tileStatuses)
        {
            if (status.isProblematic)
            {
                Debug.LogWarning($"[TileDebug] 문제 감지: {status.tileName} ({status.tileType}) - {status.problemDescription}");
            }
        }
    }
    
    private void CheckProblematicSituation(Tile tile, TileStatus status, Character occupant, bool hasPlaceTileChild)
    {
        // placed tile 특별 처리
        if (tile.IsPlaceTile() || tile.IsPlaced2())
        {
            // placed tile은 자식 관리가 아닌 캐릭터 존재만 체크
            if (!status.hasCharacter)
            {
                // placed tile이 비어있는 것은 정상이지만, 별도로 표시
                status.isProblematic = false;
                status.problemDescription = "빈 Placed Tile (정상)";
            }
            else
            {
                status.isProblematic = false;
                status.problemDescription = "캐릭터 배치됨";
            }
            return;
        }
        
        // placable tile의 경우
        if (tile.IsPlacable() || tile.IsPlacable2())
        {
            // 캐릭터가 있는데 PlaceTile 자식이 없음
            if (status.hasCharacter && !hasPlaceTileChild)
            {
                status.isProblematic = true;
                status.problemDescription = "캐릭터가 있지만 PlaceTile 자식이 없음";
            }
            // 캐릭터가 없는데 PlaceTile 자식이 있음
            else if (!status.hasCharacter && hasPlaceTileChild)
            {
                status.isProblematic = true;
                status.problemDescription = "캐릭터가 없지만 PlaceTile 자식이 있음";
            }
            else if (status.hasCharacter && hasPlaceTileChild)
            {
                status.isProblematic = false;
                status.problemDescription = "정상 (캐릭터 배치됨)";
            }
            else
            {
                status.isProblematic = false;
                status.problemDescription = "정상 (비어있음)";
            }
        }
        
        // walkable tile의 경우
        if (tile.IsWalkable() || tile.IsWalkable2())
        {
            status.isProblematic = false;
            status.problemDescription = status.hasCharacter ? "캐릭터 이동 중" : "비어있음";
        }
    }
    
    [ContextMenu("문제 있는 타일 자동 수정")]
    public void FixProblematicTiles()
    {
        int fixedCount = 0;
        
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None)
            .Where(t => t != null)
            .ToArray();
            
        Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None)
            .Where(c => c != null)
            .ToArray();
        
        foreach (var tile in allTiles)
        {
            if (tile == null) continue;
            
            // placed tile은 자동 수정 대상에서 제외
            if (tile.IsPlaceTile() || tile.IsPlaced2())
            {
                // placed tile 비주얼만 업데이트
                tile.RefreshTileVisual();
                continue;
            }
            
            // placable tile 체크
            if (tile.IsPlacable() || tile.IsPlacable2())
            {
                Character occupant = null;
                foreach (var c in allChars)
                {
                    if (c != null && c.currentTile == tile)
                    {
                        occupant = c;
                        break;
                    }
                }
                
                bool hasPlaceTileChild = tile.transform.Find("PlaceTile") != null || 
                                       tile.transform.Find("Placed2") != null;
                
                // 문제 상황 수정
                if (occupant != null && !hasPlaceTileChild)
                {
                    // 캐릭터가 있는데 자식이 없으면 생성
                    PlacementManager.Instance.CreatePlaceTileChild(tile);
                    fixedCount++;
                    Debug.Log($"[TileDebug] {tile.name}에 PlaceTile 자식 생성");
                }
                else if (occupant == null && hasPlaceTileChild)
                {
                    // 캐릭터가 없는데 자식이 있으면 제거
                    PlacementManager.Instance.RemovePlaceTileChild(tile);
                    fixedCount++;
                    Debug.Log($"[TileDebug] {tile.name}의 PlaceTile 자식 제거");
                }
            }
        }
        
        Debug.Log($"[TileDebug] 총 {fixedCount}개의 타일 문제를 수정했습니다.");
        
        // 수정 후 다시 체크
        CheckAllTileReferences();
    }
    
    [ContextMenu("빈 Placed Tile 재설정")]
    public void ResetEmptyPlacedTiles()
    {
        Tile[] allTiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None)
            .Where(t => t != null)
            .ToArray();
            
        Character[] allChars = Object.FindObjectsByType<Character>(FindObjectsSortMode.None)
            .Where(c => c != null)
            .ToArray();
            
        int resetCount = 0;
        
        foreach (var tile in allTiles)
        {
            if (tile == null) continue;
            
            // placed tile만 처리
            if (tile.IsPlaceTile() || tile.IsPlaced2())
            {
                // 캐릭터가 있는지 확인
                bool hasCharacter = false;
                foreach (var c in allChars)
                {
                    if (c != null && c.currentTile == tile)
                    {
                        hasCharacter = true;
                        break;
                    }
                }
                
                // 캐릭터가 없으면 비주얼 업데이트
                if (!hasCharacter)
                {
                    tile.RefreshTileVisual();
                    resetCount++;
                    Debug.Log($"[TileDebug] 빈 placed tile {tile.name} 재설정");
                }
            }
        }
        
        Debug.Log($"[TileDebug] 총 {resetCount}개의 빈 placed tile을 재설정했습니다.");
    }
    
    private void OnGUI()
    {
        if (!enableRealtimeCheck) return;
        
        // 화면 우측 상단에 문제 있는 타일 수 표시
        int problematicCount = tileStatuses.Count(s => s.isProblematic);
        if (problematicCount > 0)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(Screen.width - 200, 10, 190, 30), 
                $"문제 있는 타일: {problematicCount}개", 
                new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
        }
        else
        {
            GUI.color = Color.green;
            GUI.Label(new Rect(Screen.width - 200, 10, 190, 30), 
                "모든 타일 정상", 
                new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
        }
        
        // ▼▼ [추가] 현재 선택된 캐릭터 및 미네랄 상태 표시 ▼▼
        if (PlacementManager.Instance != null)
        {
            GUI.color = Color.white;
            int currentIndex = PlacementManager.Instance.GetCurrentCharacterIndex();
            string selectedText = currentIndex >= 0 ? $"선택된 캐릭터: {currentIndex}번" : "캐릭터 미선택";
            GUI.Label(new Rect(Screen.width - 200, 40, 190, 30), 
                selectedText, 
                new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Normal });
            
            // 미네랄 상태 표시
            if (PlacementManager.Instance.region1MineralBar != null)
            {
                int minerals = PlacementManager.Instance.region1MineralBar.GetCurrentMinerals();
                GUI.Label(new Rect(Screen.width - 200, 70, 190, 30), 
                    $"지역1 미네랄: {minerals}/10", 
                    new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Normal });
            }
        }
    }
} 