#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;

public enum GridTileType
{
    None,
    Walkable,
    Walkable2,
    Placable,
    Placable2,
    // 원래 Occupied → PlaceTile
    PlaceTile,
    // 원래 Occupied2 → Placed2
    Placed2
}

[Serializable]
public class TileRow
{
    public GridTileType[] columns = new GridTileType[12];
}

[Serializable]
public class TileReferenceRow
{
    public Tile[] tiles = new Tile[12];
}

public class TileGridEditor : MonoBehaviour
{
    [Header("Grid State: 7 x 12 (None/Walkable/Walkable2/Placable/Placable2/PlaceTile/Placed2)")]
    public TileRow[] rows = new TileRow[7];

    [Header("Tile References: 7 x 12")]
    public TileReferenceRow[] tileReferences = new TileReferenceRow[7];

    private void Reset()
    {
        if (rows == null || rows.Length != 7)
        {
            rows = new TileRow[7];
        }
        for (int r = 0; r < 7; r++)
        {
            if (rows[r] == null)
            {
                rows[r] = new TileRow();
            }
            if (rows[r].columns == null || rows[r].columns.Length != 12)
            {
                rows[r].columns = new GridTileType[12];
            }
            for (int c = 0; c < 12; c++)
            {
                rows[r].columns[c] = GridTileType.None;
            }
        }

        if (tileReferences == null || tileReferences.Length != 7)
        {
            tileReferences = new TileReferenceRow[7];
        }
        for (int r = 0; r < 7; r++)
        {
            if (tileReferences[r] == null)
            {
                tileReferences[r] = new TileReferenceRow();
            }
            if (tileReferences[r].tiles == null || tileReferences[r].tiles.Length != 12)
            {
                tileReferences[r].tiles = new Tile[12];
            }
            for (int c = 0; c < 12; c++)
            {
                tileReferences[r].tiles[c] = null;
            }
        }
    }

    private void OnValidate()
    {
        if (rows == null || rows.Length != 7)
        {
            rows = new TileRow[7];
        }
        for (int r = 0; r < 7; r++)
        {
            if (rows[r] == null)
            {
                rows[r] = new TileRow();
            }
            if (rows[r].columns == null || rows[r].columns.Length != 12)
            {
                rows[r].columns = new GridTileType[12];
            }
        }

        if (tileReferences == null || tileReferences.Length != 7)
        {
            tileReferences = new TileReferenceRow[7];
        }
        for (int r = 0; r < 7; r++)
        {
            if (tileReferences[r] == null)
            {
                tileReferences[r] = new TileReferenceRow();
            }
            if (tileReferences[r].tiles == null || tileReferences[r].tiles.Length != 12)
            {
                tileReferences[r].tiles = new Tile[12];
            }
        }
    }

    public void ApplyGridToReferencedTiles()
    {
        OnValidate();

        for (int r = 0; r < 7; r++)
        {
            for (int c = 0; c < 12; c++)
            {
                Tile tile = tileReferences[r].tiles[c];
                if (tile == null) continue;

                tile.row = r;
                tile.column = c;
                tile.tileIndex = (r * 12) + c;

                GridTileType gridType = rows[r].columns[c];
                ApplyStateToTile(tile, gridType);
            }
        }

        Debug.Log("[TileGridEditor] 7×12 타일 상태 적용 완료!");
    }

    private void ApplyStateToTile(Tile tile, GridTileType state)
    {
        if (tile == null) return;

        // RemoveChildIfExists에서 "Occupied" → "PlaceTile", "Occupied2" → "Placed2"
        RemoveChildIfExists(tile.transform, "Walkable");
        RemoveChildIfExists(tile.transform, "Walkable2");
        RemoveChildIfExists(tile.transform, "Placable");
        RemoveChildIfExists(tile.transform, "Placable2");
        RemoveChildIfExists(tile.transform, "PlaceTile");
        RemoveChildIfExists(tile.transform, "Placed2");

        switch (state)
        {
            case GridTileType.None:
                break;

            case GridTileType.Walkable:
                CreateChild(tile.transform, "Walkable");
                break;

            case GridTileType.Walkable2:
                CreateChild(tile.transform, "Walkable2");
                break;

            case GridTileType.Placable:
                CreateChild(tile.transform, "Placable");
                break;

            case GridTileType.Placable2:
                CreateChild(tile.transform, "Placable2");
                break;

            // 원래 Occupied → PlaceTile
            case GridTileType.PlaceTile:
                CreateChild(tile.transform, "PlaceTile");
                break;

            // 원래 Occupied2 → Placed2
            case GridTileType.Placed2:
                CreateChild(tile.transform, "Placed2");
                break;
        }

        tile.RefreshTileVisual();
    }

    private void RemoveChildIfExists(Transform parent, string childName)
    {
        if (parent == null) return;
        Transform child = parent.Find(childName);
        if (child != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(child.gameObject, false);
#else
            Destroy(child.gameObject);
#endif
        }
    }

    private void CreateChild(Transform parent, string childName)
    {
        if (parent == null) return;
        Transform existing = parent.Find(childName);
        if (existing != null) return;

        GameObject go = new GameObject(childName);
#if UNITY_EDITOR
        go.hideFlags = HideFlags.None;
#endif
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TileGridEditor))]
public class TileGridEditorInspector : Editor
{
    private const float CellWidth = 45f;
    private const float CellHeight = 24f;

    public override void OnInspectorGUI()
    {
        if (target == null)
        {
            EditorGUILayout.HelpBox("Target is null or destroyed.", MessageType.Error);
            return;
        }

        serializedObject.Update();

        TileGridEditor gridEditor = (TileGridEditor)target;

        EditorGUILayout.LabelField("[Grid State] 7×12", EditorStyles.boldLabel);
        SerializedProperty rowsProp = serializedObject.FindProperty("rows");
        if (rowsProp != null && rowsProp.arraySize == 7)
        {
            for (int r = 0; r < 7; r++)
            {
                SerializedProperty rowProp = rowsProp.GetArrayElementAtIndex(r);
                if (rowProp == null) continue;

                SerializedProperty colsProp = rowProp.FindPropertyRelative("columns");
                if (colsProp == null || colsProp.arraySize != 12) continue;

                EditorGUILayout.BeginHorizontal();

                for (int c = 0; c < 12; c++)
                {
                    SerializedProperty cellProp = colsProp.GetArrayElementAtIndex(c);
                    if (cellProp == null) continue;

                    GridTileType currentVal = (GridTileType)cellProp.enumValueIndex;
                    string label = MakeLabelForCell(currentVal);

                    if (GUILayout.Button(label, GUILayout.Width(CellWidth), GUILayout.Height(CellHeight)))
                    {
                        cellProp.enumValueIndex = (int)GetNextGridTileType(currentVal);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.LabelField("[Tile References] 7×12", EditorStyles.boldLabel);
        SerializedProperty tileRefsProp = serializedObject.FindProperty("tileReferences");
        if (tileRefsProp != null && tileRefsProp.arraySize == 7)
        {
            for (int r = 0; r < 7; r++)
            {
                SerializedProperty rowRefProp = tileRefsProp.GetArrayElementAtIndex(r);
                if (rowRefProp == null) continue;

                SerializedProperty tilesProp = rowRefProp.FindPropertyRelative("tiles");
                if (tilesProp == null || tilesProp.arraySize != 12) continue;

                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < 12; c++)
                {
                    SerializedProperty tileProp = tilesProp.GetArrayElementAtIndex(c);
                    if (tileProp == null) continue;

                    EditorGUILayout.PropertyField(tileProp, GUIContent.none, GUILayout.Width(120));
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (GUILayout.Button("Apply to 7×12 Tile References"))
        {
            gridEditor.ApplyGridToReferencedTiles();
        }

        EditorGUILayout.HelpBox(
            @"[사용 방법]
1) [Grid State] 영역 각 칸 클릭 -> (None -> Walkable -> Walkable2 -> Placable -> Placable2 -> PlaceTile -> Placed2 -> None) 순환.
2) [Tile References] 7×12에 씬상의 Tile을 연결.
3) [Apply] 버튼으로 Tile에 자식 오브젝트 생성/삭제하여 적용.",
            MessageType.Info
        );

        serializedObject.ApplyModifiedProperties();
    }

    private string MakeLabelForCell(GridTileType t)
    {
        switch (t)
        {
            case GridTileType.None:      return "X";
            case GridTileType.Walkable:  return "W1";
            case GridTileType.Walkable2: return "W2";
            case GridTileType.Placable:  return "P1";
            case GridTileType.Placable2: return "P2";
            case GridTileType.PlaceTile: return "O1"; // 시각 구분 위해 "O1"이라 표기(실제는 PlaceTile)
            case GridTileType.Placed2:   return "O2"; // 시각 구분 위해 "O2"이라 표기(실제는 Placed2)
        }
        return "?";
    }

    private GridTileType GetNextGridTileType(GridTileType current)
    {
        switch (current)
        {
            case GridTileType.None:      return GridTileType.Walkable;
            case GridTileType.Walkable:  return GridTileType.Walkable2;
            case GridTileType.Walkable2: return GridTileType.Placable;
            case GridTileType.Placable:  return GridTileType.Placable2;
            case GridTileType.Placable2: return GridTileType.PlaceTile;
            case GridTileType.PlaceTile: return GridTileType.Placed2;
            case GridTileType.Placed2:   return GridTileType.None;
        }
        return GridTileType.None;
    }
}
#endif
