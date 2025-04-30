#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;

/// <summary>
/// GridTileType 열거형에 Walkable2, Picable2, Occupied2 추가.
///   - None      : 아무 상태 아님
///   - Walkable  : 1인용 몬스터 경로
///   - Walkable2 : 2인용 몬스터 경로
///   - Placable  : 1인용 플레이어 배치
///   - Picable2  : 2인용(이미지 다르게 할) 플레이어 배치
///   - Occupied  : (기존) 점유 상태
///   - Occupied2 : (추가) 또 다른 점유 상태(이미지 다르게 할 용)
/// </summary>
public enum GridTileType
{
    None,
    Walkable,
    Walkable2,
    Placable,
    Picable2,
    Occupied,
    Occupied2
}

[Serializable]
public class TileRow
{
    // 세로 12열
    public GridTileType[] columns = new GridTileType[12];
}

[Serializable]
public class TileReferenceRow
{
    // 실제 Tile 오브젝트 12개
    public Tile[] tiles = new Tile[12];
}

/// <summary>
/// 7×12 크기의 타일 격자를 구성하는 에디터 전용 스크립트.
/// Inspector에서 rows[r].columns[c] = GridTileType 설정 → Apply 버튼으로
/// tileReferences[r].tiles[c]에 있는 Tile 자식("Walkable"/"Walkable2"/"Placable"/"Picable2"/"Occupied"/"Occupied2")를 생성/삭제함.
/// </summary>
public class TileGridEditor : MonoBehaviour
{
    [Header("Grid State: 7 x 12 (None/Walkable/Walkable2/Placable/Picable2/Occupied/Occupied2)")]
    public TileRow[] rows = new TileRow[7];

    [Header("Tile References: 7 x 12")]
    [Tooltip("씬 상에 배치된 7×12 Tile 오브젝트를 순서대로 연결하세요.")]
    public TileReferenceRow[] tileReferences = new TileReferenceRow[7];

    private void Reset()
    {
        // rows (7행) 초기화
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

        // tileReferences (7행) 초기화
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
        // rows 길이 7 보정
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

        // tileReferences 길이 7 보정
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

    /// <summary>
    /// [Apply] 버튼 -> rows[r].columns[c]값을 참고하여
    /// tileReferences[r].tiles[c]에 있는 Tile에 상태를 반영.
    /// => Tile.cs 안의 "Walkable"/"Walkable2"/"Placable"/"Picable2"/"Occupied"/"Occupied2" 자식 생성/삭제
    /// </summary>
    public void ApplyGridToReferencedTiles()
    {
        OnValidate(); // 배열 구조 재확인

        for (int r = 0; r < 7; r++)
        {
            for (int c = 0; c < 12; c++)
            {
                Tile tile = tileReferences[r].tiles[c];
                if (tile == null) continue;

                // row/column/index 설정(디버그용)
                tile.row = r;
                tile.column = c;
                tile.tileIndex = (r * 12) + c;

                // enum 값 확인
                GridTileType gridType = rows[r].columns[c];

                // 실제로 자식 오브젝트 붙였다 떼기
                ApplyStateToTile(tile, gridType);
            }
        }

        Debug.Log("[TileGridEditor] 7×12 타일 상태를 모두 적용했습니다!");
    }

    private void ApplyStateToTile(Tile tile, GridTileType state)
    {
        if (tile == null) return;

        // 먼저 기존 자식 오브젝트들 제거
        RemoveChildIfExists(tile.transform, "Walkable");
        RemoveChildIfExists(tile.transform, "Walkable2");
        RemoveChildIfExists(tile.transform, "Placable");
        RemoveChildIfExists(tile.transform, "Picable2");
        RemoveChildIfExists(tile.transform, "Occupied");
        RemoveChildIfExists(tile.transform, "Occupied2");

        // 새로 필요한 자식 생성
        switch (state)
        {
            case GridTileType.None:
                // 아무것도 안 함
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

            case GridTileType.Picable2:
                CreateChild(tile.transform, "Picable2");
                break;

            case GridTileType.Occupied:
                CreateChild(tile.transform, "Occupied");
                break;

            case GridTileType.Occupied2:
                CreateChild(tile.transform, "Occupied2");
                break;
        }

        // Tile.cs 쪽 비주얼 다시 갱신(프리팹 반영)
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
        // 이미 있으면 중복 방지
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
    private const float CellWidth = 45f;   // 버튼 가로 크기
    private const float CellHeight = 24f;  // 버튼 세로 크기

    public override void OnInspectorGUI()
    {
        if (target == null)
        {
            EditorGUILayout.HelpBox("Target is null or destroyed.", MessageType.Error);
            return;
        }

        serializedObject.Update();

        TileGridEditor gridEditor = (TileGridEditor)target;

        // 1) rows(7x12) 표시
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

                    // 버튼 클릭 시 순환
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

        // 2) tileReferences(7x12)
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

        // 3) Apply 버튼
        if (GUILayout.Button("Apply to 7×12 Tile References"))
        {
            gridEditor.ApplyGridToReferencedTiles();
        }

        EditorGUILayout.HelpBox(
            @"[사용 방법]
1) [Grid State] 7×12 영역의 각 칸을 클릭하면 (None -> Walkable -> Walkable2 -> Placable -> Picable2 -> Occupied -> Occupied2 -> None) 순으로 바뀝니다.
2) [Tile References] 7×12에 씬 상의 Tile들을 연결해둡니다.
3) [Apply] 버튼을 누르면 Tile 자식오브젝트 생성/삭제로 상태를 적용합니다.

picable2, occupied2 등을 쓰면 프리팹도 따로 배치해둘 수 있습니다.
",
            MessageType.Info
        );

        serializedObject.ApplyModifiedProperties();
    }

    private string MakeLabelForCell(GridTileType t)
    {
        // 버튼에 표시할 간단한 텍스트
        switch (t)
        {
            case GridTileType.None:      return "X";
            case GridTileType.Walkable:  return "W1";
            case GridTileType.Walkable2: return "W2";
            case GridTileType.Placable:  return "P1";
            case GridTileType.Picable2:  return "P2";
            case GridTileType.Occupied:  return "O1";
            case GridTileType.Occupied2: return "O2";
        }
        return "?";
    }

    private GridTileType GetNextGridTileType(GridTileType current)
    {
        // 순환 순서: None -> Walkable -> Walkable2 -> Placable -> Picable2 -> Occupied -> Occupied2 -> None
        switch (current)
        {
            case GridTileType.None:      return GridTileType.Walkable;
            case GridTileType.Walkable:  return GridTileType.Walkable2;
            case GridTileType.Walkable2: return GridTileType.Placable;
            case GridTileType.Placable:  return GridTileType.Picable2;
            case GridTileType.Picable2:  return GridTileType.Occupied;
            case GridTileType.Occupied:  return GridTileType.Occupied2;
            case GridTileType.Occupied2: return GridTileType.None;
        }
        return GridTileType.None;
    }
}
#endif
