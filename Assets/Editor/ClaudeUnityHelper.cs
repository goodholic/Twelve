using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

public static class ClaudeUnityHelper
{
    [MenuItem("Claude/Create Cube")]
    public static void CreateCube()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(cube, "Create Cube");
        Selection.activeGameObject = cube;
    }
    
    [MenuItem("Claude/Create TextMeshPro")]
    public static void CreateTextMeshPro()
    {
        GameObject textObj = new GameObject("TextMeshPro");
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = "Hello from Claude!";
        tmp.fontSize = 36;
        Undo.RegisterCreatedObjectUndo(textObj, "Create TextMeshPro");
        Selection.activeGameObject = textObj;
    }
    
    [MenuItem("Claude/Modify Selected Object")]
    public static void ModifySelectedObject()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected != null)
        {
            Undo.RecordObject(selected.transform, "Modify Transform");
            selected.transform.position += Vector3.up * 2;
            selected.transform.localScale = Vector3.one * 1.5f;
            EditorUtility.SetDirty(selected);
        }
    }
    
    [MenuItem("Claude/Save All Changes")]
    public static void SaveAllChanges()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorApplication.ExecuteMenuItem("File/Save");
        Debug.Log("모든 변경사항이 저장되었습니다!");
    }
}