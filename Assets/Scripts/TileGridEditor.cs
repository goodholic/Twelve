#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;

public enum GridTileType
{
    None,
    Walkable,
    Placable,
    Occupied
}

[Serializable]
public class TileRow
{
    // 열 크기를 10으로 고정
    public GridTileType[] columns = new GridTileType[10];
}

/// <summary>
/// 10×10 그리드를 에디터에서 설정하고, 자식 Tile들에게 적용하는 스크립트
/// </summary>
public class TileGridEditor : MonoBehaviour
{
    [Header("Grid Size: 10 x 10")]
    // 이전에는 15줄(rows)였지만, 이제 10줄로 변경
    public TileRow[] rows = new TileRow[10];

    private void Reset()
    {
        // rows 배열(10줄)을 초기화
        for (int r = 0; r < rows.Length; r++)
        {
            rows[r] = new TileRow();
            // 각 줄의 columns도 10칸
            for (int c = 0; c < rows[r].columns.Length; c++)
            {
                rows[r].columns[c] = GridTileType.None;
            }
        }
    }

    public void ApplyGridToChildTiles()
    {
        // 자식 타일들을 전부 가져옴
        Tile[] childTiles = GetComponentsInChildren<Tile>(true);

        // 10×10이면 타일은 총 100개
        int expectedCount = 10 * 10;
        if (childTiles.Length != expectedCount)
        {
            Debug.LogError($"TileGridEditor: 자식 Tile 개수가 {childTiles.Length}개 입니다. (예상: {expectedCount}개).");
            return;
        }

        // 히에라르키 순서에 맞춰 정렬 (위~아래, 왼쪽~오른쪽 순으로 정렬되도록)
        childTiles = SortTilesByHierarchy(childTiles);

        // 정렬된 타일에 rows 정보를 적용
        for (int i = 0; i < childTiles.Length; i++)
        {
            // 1차원 인덱스 i → (행, 열) 변환
            int r = i / 10;
            int c = i % 10;

            GridTileType gridType = rows[r].columns[c];
            ApplyStateToTile(childTiles[i], gridType);
        }

        Debug.Log("TileGridEditor: 자식 Tile들에 Grid 상태를 적용했습니다.");
    }

    private void ApplyStateToTile(Tile tile, GridTileType state)
    {
        // 기본값
        tile.isWalkable = false;
        tile.isPlacable = false;
        tile.isOccupied = false;

        // 설정된 enum 값에 따라 타일 속성 부여
        switch (state)
        {
            case GridTileType.None:
                // 아무것도 안함
                break;
            case GridTileType.Walkable:
                tile.isWalkable = true;
                break;
            case GridTileType.Placable:
                tile.isPlacable = true;
                break;
            case GridTileType.Occupied:
                tile.isOccupied = true;
                break;
        }

        // 타일 비주얼 갱신
        tile.RefreshTileVisual();
    }

    public Tile[] SortTilesByHierarchy(Tile[] tiles)
    {
        // transform의 SiblingIndex(계층 순서) 기준으로 정렬
        System.Array.Sort(tiles, (t1, t2) =>
        {
            return t1.transform.GetSiblingIndex().CompareTo(t2.transform.GetSiblingIndex());
        });
        return tiles;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TileGridEditor))]
public class TileGridEditorInspector : Editor
{
    // 셀 버튼 UI 크기
    private const float CellWidth = 35f;
    private const float CellHeight = 22f;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        TileGridEditor gridEditor = (TileGridEditor)target;
        SerializedProperty rowsProp = serializedObject.FindProperty("rows");

        // rows 배열 크기를 10으로
        if (rowsProp.arraySize != 10)
        {
            rowsProp.arraySize = 10;
        }

        // 각 row(줄)은 10개
        for (int r = 0; r < 10; r++)
        {
            SerializedProperty rowProp = rowsProp.GetArrayElementAtIndex(r);
            SerializedProperty colsProp = rowProp.FindPropertyRelative("columns");
            // columns 배열 크기도 10으로
            if (colsProp.arraySize != 10)
            {
                colsProp.arraySize = 10;
            }

            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < 10; c++)
            {
                SerializedProperty cellProp = colsProp.GetArrayElementAtIndex(c);
                GridTileType currentVal = (GridTileType)cellProp.enumValueIndex;

                // 현재 상태에 따라 버튼 표시 문자
                string shape;
                switch (currentVal)
                {
                    case GridTileType.None:     shape = "X"; break;
                    case GridTileType.Walkable: shape = "△"; break;
                    case GridTileType.Placable: shape = "□"; break;
                    case GridTileType.Occupied: shape = "○"; break;
                    default:                    shape = "?"; break;
                }

                GUILayoutOption wOpt = GUILayout.Width(CellWidth);
                GUILayoutOption hOpt = GUILayout.Height(CellHeight);

                // 클릭 시 순환하여 다음 상태로 전환
                if (GUILayout.Button(shape, wOpt, hOpt))
                {
                    cellProp.enumValueIndex = (int)GetNextType(currentVal);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Apply to Tiles"))
        {
            // "Apply to Tiles" 버튼 누르면 자식 타일에 적용
            gridEditor.ApplyGridToChildTiles();
        }

        // 안내 문구
        EditorGUILayout.HelpBox(
            "가로 10 × 세로 10의 Grid.\n" +
            "X(None), △(Walkable), □(Placable), ○(Occupied)\n" +
            "[Apply to Tiles] 버튼을 누르면 자식 Tile에 적용됩니다.\n" +
            "자식 Tile 개수는 100개여야 합니다.",
            MessageType.Info
        );

        serializedObject.ApplyModifiedProperties();
    }

    // 버튼 클릭 시 순환 규칙
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
