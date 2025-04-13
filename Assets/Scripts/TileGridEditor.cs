// Assets\Scripts\TileGridEditor.cs

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
    public GridTileType[] columns = new GridTileType[10];
}

public class TileGridEditor : MonoBehaviour
{
    [Header("Grid Size: 10 x 15")]
    public TileRow[] rows = new TileRow[15];

    private void Reset()
    {
        for (int r = 0; r < rows.Length; r++)
        {
            rows[r] = new TileRow();
            for (int c = 0; c < rows[r].columns.Length; c++)
            {
                rows[r].columns[c] = GridTileType.None;
            }
        }
    }

    public void ApplyGridToChildTiles()
    {
        Tile[] childTiles = GetComponentsInChildren<Tile>(true);
        int expectedCount = 15 * 10;
        if (childTiles.Length != expectedCount)
        {
            Debug.LogError($"TileGridEditor: 자식 Tile 개수가 {childTiles.Length}개 입니다. (예상: {expectedCount}개).");
            return;
        }

        childTiles = SortTilesByHierarchy(childTiles);

        for (int i = 0; i < childTiles.Length; i++)
        {
            int r = i / 10;
            int c = i % 10;
            GridTileType gridType = rows[r].columns[c];
            ApplyStateToTile(childTiles[i], gridType);
        }

        Debug.Log("TileGridEditor: 자식 Tile들에 Grid 상태를 적용했습니다.");
    }

    private void ApplyStateToTile(Tile tile, GridTileType state)
    {
        tile.isWalkable = false;
        tile.isPlacable = false;
        tile.isOccupied = false;

        switch (state)
        {
            case GridTileType.None:
                // 아무것도 안 함
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

        tile.RefreshTileVisual();
    }

    private Tile[] SortTilesByHierarchy(Tile[] tiles)
    {
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
    private const float CellWidth  = 35f;
    private const float CellHeight = 22f;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        TileGridEditor gridEditor = (TileGridEditor)target;
        SerializedProperty rowsProp = serializedObject.FindProperty("rows");

        if (rowsProp.arraySize != 15)
        {
            rowsProp.arraySize = 15;
        }

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

                string shape;
                switch (currentVal)
                {
                    case GridTileType.None:     shape = "X"; break;
                    case GridTileType.Walkable: shape = "△"; break;
                    case GridTileType.Placable: shape = "□"; break;
                    case GridTileType.Occupied: shape = "○"; break;
                    default: shape = "?"; break;
                }

                GUILayoutOption wOpt = GUILayout.Width(CellWidth);
                GUILayoutOption hOpt = GUILayout.Height(CellHeight);

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
            gridEditor.ApplyGridToChildTiles();
        }

        EditorGUILayout.HelpBox(
            "가로 10 × 세로 15의 Grid.\n" +
            "X, △(Walkable), □(Placable), ○(Occupied)\n" +
            "[Apply to Tiles] 버튼을 누르면 자식 Tile에 적용됩니다.\n" +
            "자식 Tile 개수는 150개여야 합니다.",
            MessageType.Info
        );

        serializedObject.ApplyModifiedProperties();
    }

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
