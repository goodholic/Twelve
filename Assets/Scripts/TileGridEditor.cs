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

// [추가] 실제 씬에 존재하는 Tile 레퍼런스 10×10을 Inspector에서 넣기 위한 구조
[Serializable]
public class TileReferenceRow
{
    public Tile[] tiles = new Tile[10];
}

/// <summary>
/// 10×10 그리드를 에디터에서 설정하고,
/// 1) tileReferences[r].tiles[c]로 Inspector에서 직접 연결된 Tile을 가져온 뒤
/// 2) rows[r].columns[c] 상태(Placable/Walkable/Occupied 등)를 적용하여
///    Tile 자식 오브젝트를 생성/삭제한다.
/// </summary>
public class TileGridEditor : MonoBehaviour
{
    [Header("Grid State: 10 x 10 (Placable/Walkable/Occupied)")]
    public TileRow[] rows = new TileRow[10];

    [Header("Tile References: 10 x 10")]
    [Tooltip("씬에 배치된 Tile 오브젝트를 10×10 배열에 수동으로 넣으세요.")]
    public TileReferenceRow[] tileReferences = new TileReferenceRow[10];

    private void Reset()
    {
        // 1) rows 배열(상태) 초기화
        for (int r = 0; r < rows.Length; r++)
        {
            rows[r] = new TileRow();
            for (int c = 0; c < rows[r].columns.Length; c++)
            {
                rows[r].columns[c] = GridTileType.None;
            }
        }

        // 2) tileReferences 배열(실제 Tile 참조) 초기화
        for (int r = 0; r < tileReferences.Length; r++)
        {
            tileReferences[r] = new TileReferenceRow();
            for (int c = 0; c < tileReferences[r].tiles.Length; c++)
            {
                tileReferences[r].tiles[c] = null;
            }
        }
    }

    /// <summary>
    /// 10×10 각각의 Tile 레퍼런스(tileReferences[r].tiles[c])에 대해
    /// rows[r].columns[c] 상태를 적용(Placable/Walkable/Occupied 자식 생성/삭제).
    /// </summary>
    public void ApplyGridToReferencedTiles()
    {
        // 10×10 반복
        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 10; c++)
            {
                Tile tile = tileReferences[r].tiles[c];
                if (tile == null)
                {
                    // 해당 칸이 null이면 넘어감
                    continue;
                }

                // 행/열, 인덱스 설정
                tile.row = r;
                tile.column = c;
                tile.tileIndex = (r * 10) + c;

                // rows[r].columns[c]로부터 상태 가져옴
                GridTileType gridType = rows[r].columns[c];

                // 타일에 상태 적용
                ApplyStateToTile(tile, gridType);
            }
        }

        Debug.Log("TileGridEditor: 모든 Referenced Tile에 Grid 상태를 적용 완료.");
    }

    /// <summary>
    /// Tile에 대해 자식 오브젝트("Placable", "Walkable", "Occupied")를 생성/삭제
    /// </summary>
    private void ApplyStateToTile(Tile tile, GridTileType state)
    {
        if (tile == null) return;

        // 기존 "Placable"/"Occupied"/"Walkable" 자식 전부 제거
        RemoveChildIfExists(tile.transform, "Placable");
        RemoveChildIfExists(tile.transform, "Occupied");
        RemoveChildIfExists(tile.transform, "Walkable");

        // 필요한 상태만 새로 생성
        switch (state)
        {
            case GridTileType.None:
                // 아무것도 안 만듦
                break;
            case GridTileType.Walkable:
                CreateChildObject(tile.transform, "Walkable");
                break;
            case GridTileType.Placable:
                CreateChildObject(tile.transform, "Placable");
                break;
            case GridTileType.Occupied:
                CreateChildObject(tile.transform, "Occupied");
                break;
        }

        // 타일 비주얼 갱신
        tile.RefreshTileVisual();
    }

    private void RemoveChildIfExists(Transform parent, string childName)
    {
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

    private void CreateChildObject(Transform parent, string childName)
    {
        // 이미 있으면 중복 생성하지 않음
        if (parent.Find(childName) != null) return;

        // 이름만 있는 빈 오브젝트 생성
        GameObject go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TileGridEditor))]
public class TileGridEditorInspector : Editor
{
    private const float CellWidth = 35f;
    private const float CellHeight = 22f;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 대상 스크립트
        TileGridEditor gridEditor = (TileGridEditor)target;

        // -----------------------------
        // (1) rows: GridTileType 10×10
        // -----------------------------
        EditorGUILayout.LabelField("[Grid State] 10×10 (None / Walkable / Placable / Occupied)", EditorStyles.boldLabel);
        SerializedProperty rowsProp = serializedObject.FindProperty("rows");
        if (rowsProp.arraySize != 10) rowsProp.arraySize = 10;

        for (int r = 0; r < 10; r++)
        {
            SerializedProperty rowProp = rowsProp.GetArrayElementAtIndex(r);
            SerializedProperty colsProp = rowProp.FindPropertyRelative("columns");
            if (colsProp.arraySize != 10) colsProp.arraySize = 10;

            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < 10; c++)
            {
                SerializedProperty cellProp = colsProp.GetArrayElementAtIndex(c);
                GridTileType currentVal = (GridTileType)cellProp.enumValueIndex;

                // 버튼으로 표시
                string shape = "?";
                switch (currentVal)
                {
                    case GridTileType.None:     shape = "X"; break;
                    case GridTileType.Walkable: shape = "△"; break;
                    case GridTileType.Placable: shape = "□"; break;
                    case GridTileType.Occupied: shape = "○"; break;
                }

                if (GUILayout.Button(shape, GUILayout.Width(CellWidth), GUILayout.Height(CellHeight)))
                {
                    cellProp.enumValueIndex = (int)GetNextType(currentVal);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // -----------------------------
        // (2) tileReferences: Tile 10×10
        // -----------------------------
        EditorGUILayout.LabelField("[Tile References] 10×10 (씬에 존재하는 Tile 오브젝트)", EditorStyles.boldLabel);
        SerializedProperty tileRefsProp = serializedObject.FindProperty("tileReferences");
        if (tileRefsProp.arraySize != 10) tileRefsProp.arraySize = 10;

        for (int r = 0; r < 10; r++)
        {
            SerializedProperty refRowProp = tileRefsProp.GetArrayElementAtIndex(r);
            SerializedProperty tilesProp = refRowProp.FindPropertyRelative("tiles");
            if (tilesProp.arraySize != 10) tilesProp.arraySize = 10;

            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < 10; c++)
            {
                SerializedProperty tileProp = tilesProp.GetArrayElementAtIndex(c);
                EditorGUILayout.PropertyField(tileProp, GUIContent.none, GUILayout.Width(100));
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // -----------------------------
        // (3) "Apply to Referenced Tiles" 버튼
        // -----------------------------
        if (GUILayout.Button("Apply to Referenced Tiles (10×10)"))
        {
            gridEditor.ApplyGridToReferencedTiles();
        }

        EditorGUILayout.HelpBox(
            "[사용 방법]\n" +
            "1) [Grid State] 10×10에서 각 셀의 상태(None/Walkable/Placable/Occupied)를 설정\n" +
            "2) [Tile References] 10×10에 씬의 Tile 오브젝트를 연결(Drag&Drop)\n" +
            "3) 'Apply to Referenced Tiles' 버튼 클릭 -> Tile.cs에 (row, col, tileIndex) 지정 + 자식 오브젝트 생성/삭제\n" +
            "   -> Tile이 Placable/Walkable/Occupied 상태를 인식하게 됨",
            MessageType.Info
        );

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 버튼 클릭 시 GridTileType을 순환( None -> Walkable -> Placable -> Occupied -> None )
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
