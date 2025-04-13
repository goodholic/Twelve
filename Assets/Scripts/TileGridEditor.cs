#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;

/// <summary>
/// 부모 오브젝트에 붙여서,
/// 10×15 형태의 GridTileType 배열을 인스펙터에서 편집하고,
/// [Apply to Tiles] 버튼을 통해 자식 Tile 오브젝트들을 일괄 변경해주는 스크립트.
/// </summary>
public enum GridTileType
{
    None,       // X
    Walkable,   // △
    Placable,   // □
    Occupied    // ○
}

[Serializable]
public class TileRow
{
    public GridTileType[] columns = new GridTileType[10];
}

public class TileGridEditor : MonoBehaviour
{
    [Header("Grid Size: 10 x 15")]
    public TileRow[] rows = new TileRow[15];

    /// <summary>
    /// 스크립트를 추가했을 때, rows 배열 초기화 (기본값: None)
    /// </summary>
    private void Reset()
    {
        // 15줄
        for (int r = 0; r < rows.Length; r++)
        {
            rows[r] = new TileRow();
            // 각 Row에 10칸, 기본값은 None
            for (int c = 0; c < rows[r].columns.Length; c++)
            {
                rows[r].columns[c] = GridTileType.None;
            }
        }
    }

    /// <summary>
    /// rows 배열 상태를 자식 Tile들에 적용한다.
    /// </summary>
    public void ApplyGridToChildTiles()
    {
        // 자식 오브젝트에서 Tile 컴포넌트를 전부 찾음
        Tile[] childTiles = GetComponentsInChildren<Tile>(true); // 비활성화 포함

        // 15*10 = 150개가 필요한데, 개수가 맞는지 확인
        int expectedCount = 15 * 10;
        if (childTiles.Length != expectedCount)
        {
            Debug.LogError($"TileGridEditor: 자식 Tile 개수가 {childTiles.Length}개 입니다. " +
                           $"(예상: {expectedCount}개). 10×15 구성을 맞춰주세요.");
            return;
        }

        // row-major 순서대로 정렬(씬 순서가 뒤죽박죽일 수 있으니)
        // 만약 계층 순서대로 완벽히 만들었다면 생략 가능하지만,
        // 혹시 모를 상황 대비, transform 계층 순서를 기준으로 정렬.
        childTiles = SortTilesByHierarchy(childTiles);

        // 이제 0번 ~ 149번까지, (행 = i/10, 열 = i%10)에 매핑
        for (int i = 0; i < childTiles.Length; i++)
        {
            int r = i / 10;  // 0~14
            int c = i % 10;  // 0~9

            GridTileType gridType = rows[r].columns[c];
            ApplyStateToTile(childTiles[i], gridType);
        }

        Debug.Log("TileGridEditor: 자식 Tile들에 Grid 상태를 적용했습니다.");
    }

    /// <summary>
    /// 주어진 GridTileType( None / Walkable / Placable / Occupied )을 Tile 스크립트에 반영.
    /// </summary>
    private void ApplyStateToTile(Tile tile, GridTileType state)
    {
        // 우선 모든 상태를 false로 초기화
        tile.isWalkable = false;
        tile.isPlacable = false;
        tile.isOccupied = false;

        switch (state)
        {
            case GridTileType.None:
                // X: 아무 속성도 true가 아님
                break;
            case GridTileType.Walkable:
                tile.isWalkable = true;
                tile.isPlacable = false;
                tile.isOccupied = false;
                break;
            case GridTileType.Placable:
                tile.isWalkable = false;
                tile.isPlacable = true;
                tile.isOccupied = false;
                break;
            case GridTileType.Occupied:
                tile.isWalkable = false;
                tile.isPlacable = false;
                tile.isOccupied = true;
                break;
        }

        // Tile.cs 내부의 프리팹 교체 로직 반영
        tile.RefreshTileVisual();
    }

    /// <summary>
    /// 계층(Hierarchy) 순서대로 Tile들을 정렬.
    /// (row-major 순서로 배치되어 있다고 가정)
    /// </summary>
    private Tile[] SortTilesByHierarchy(Tile[] tiles)
    {
        // transform.GetSiblingIndex()로 정렬
        System.Array.Sort(tiles, (t1, t2) =>
        {
            // 조상 transform 중에서 this.gameObject(부모)의 자식 순서 찾기
            // 단순히 t1.transform.GetSiblingIndex() 비교하면
            // 하위 자식 구조가 복잡할 때 문제가 생길 수도 있음
            // 여기서는 가정: Tile들이 전부 동일한 부모 아래 직속(혹은 순서가 맞는 계층)이라고 가정
            // 필요하다면 transform.GetSiblingIndex()를 재귀적으로 비교하는 로직 작성
            return t1.transform.GetSiblingIndex().CompareTo(t2.transform.GetSiblingIndex());
        });
        return tiles;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TileGridEditor))]
public class TileGridEditorInspector : Editor
{
    // 셀 하나의 버튼 크기
    private const float CellWidth  = 35f;
    private const float CellHeight = 22f;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        TileGridEditor gridEditor = (TileGridEditor)target;

        // rows 프로퍼티 가져오기
        SerializedProperty rowsProp = serializedObject.FindProperty("rows");
        if (rowsProp.arraySize != 15)
        {
            rowsProp.arraySize = 15;
        }

        // 15줄 루프
        for (int r = 0; r < 15; r++)
        {
            SerializedProperty rowProp = rowsProp.GetArrayElementAtIndex(r);
            SerializedProperty colsProp = rowProp.FindPropertyRelative("columns");
            if (colsProp.arraySize != 10)
            {
                colsProp.arraySize = 10;
            }

            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < 10; c++)
            {
                SerializedProperty cellProp = colsProp.GetArrayElementAtIndex(c);
                GridTileType currentVal = (GridTileType)cellProp.enumValueIndex;

                // 표시 문자
                string shape;
                switch (currentVal)
                {
                    case GridTileType.None:     shape = "X"; break;   // 기본값
                    case GridTileType.Walkable: shape = "△"; break;
                    case GridTileType.Placable: shape = "□"; break;
                    case GridTileType.Occupied: shape = "○"; break;
                    default: shape = "?"; break;
                }

                GUILayoutOption wOpt = GUILayout.Width(CellWidth);
                GUILayoutOption hOpt = GUILayout.Height(CellHeight);

                // 버튼 그리기
                if (GUILayout.Button(shape, wOpt, hOpt))
                {
                    // 순환 전환
                    GridTileType next = GetNextType(currentVal);
                    cellProp.enumValueIndex = (int)next;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Apply to Tiles"))
        {
            // 버튼 누르면 자식 Tile에 적용
            gridEditor.ApplyGridToChildTiles();
        }

        EditorGUILayout.HelpBox(
            "가로 10 × 세로 15의 Grid.\n" +
            "X(기본), △(Walkable), □(Placable), ○(Occupied)\n" +
            "[Apply to Tiles] 버튼을 누르면 자식 Tile들에게 적용됩니다.\n" +
            "자식 Tile 개수는 반드시 150개여야 합니다.",
            MessageType.Info
        );

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// X -> △ -> □ -> ○ -> X ... 순환
    /// </summary>
    private GridTileType GetNextType(GridTileType current)
    {
        switch (current)
        {
            case GridTileType.None:     return GridTileType.Walkable;
            case GridTileType.Walkable: return GridTileType.Placable;
            case GridTileType.Placable: return GridTileType.Occupied;
            case GridTileType.Occupied: return GridTileType.None;
        }
        return GridTileType.None;
    }
}
#endif
