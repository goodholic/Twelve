using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Canvas 비활성화 문제의 원인인 MMTouchControls 컴포넌트를 찾아 제거하는 전문 도구입니다.
/// Feel 에셋의 MMTouchControls가 Canvas를 비활성화시키는 문제를 해결합니다.
/// TwelveToolsManager를 통해서만 접근 가능합니다.
/// </summary>
public class MMTouchControlsRemover : EditorWindow
{
    private Vector2 scrollPosition;
    private List<GameObject> foundMMTouchControls = new List<GameObject>();
    private bool isScanning = false;

    // TwelveToolsManager를 통해서만 접근 - 직접 메뉴 항목 제거
    // [MenuItem("Twelve/🚨 MMTouchControls Remover")]
    public static void ShowWindow()
    {
        var window = GetWindow<MMTouchControlsRemover>("MMTouchControls Remover");
        window.titleContent = new GUIContent("🚨 MMTouchControls Remover");
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "MMTouchControls 컴포넌트가 Canvas를 비활성화시키고 있습니다.\n" +
            "이 도구로 모든 MMTouchControls를 안전하게 제거할 수 있습니다.\n\n" +
            "⚠️ 주의: Feel 에셋의 모바일 터치 컨트롤 기능이 제거됩니다.",
            MessageType.Warning);
            
        EditorGUILayout.Space(10);
        
        // 자동 스캔 및 제거 버튼 추가
        if (GUILayout.Button("🚀 자동 스캔 및 제거 (원클릭 해결!)", GUILayout.Height(40)))
        {
            AutoScanAndRemove();
        }
        
        EditorGUILayout.Space(5);

        if (GUILayout.Button("🔍 MMTouchControls 컴포넌트 스캔", GUILayout.Height(30)))
        {
            ScanForMMTouchControls();
        }
        
        if (isScanning)
        {
            EditorGUILayout.LabelField("스캔 중...", EditorStyles.centeredGreyMiniLabel);
            return;
        }
        
        EditorGUILayout.Space();
        
        // 결과 표시
        if (foundMMTouchControls.Count > 0)
        {
            EditorGUILayout.LabelField($"🎯 발견된 MMTouchControls: {foundMMTouchControls.Count}개", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(300));
            
            for (int i = 0; i < foundMMTouchControls.Count; i++)
            {
                if (foundMMTouchControls[i] == null) continue;
                
                EditorGUILayout.BeginHorizontal();
                
                // 객체 이름과 경로
                string path = GetGameObjectPath(foundMMTouchControls[i]);
                EditorGUILayout.LabelField($"📍 {path}", GUILayout.ExpandWidth(true));
                
                // 선택 버튼
                if (GUILayout.Button("선택", GUILayout.Width(50)))
                {
                    Selection.activeGameObject = foundMMTouchControls[i];
                    EditorGUIUtility.PingObject(foundMMTouchControls[i]);
                }
                
                // 제거 버튼
                if (GUILayout.Button("🗑️ 제거", GUILayout.Width(50)))
                {
                    RemoveMMTouchControlsFromObject(foundMMTouchControls[i]);
                    foundMMTouchControls.RemoveAt(i);
                    i--;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            
            // 모두 제거 버튼
            if (GUILayout.Button("🚨 모든 MMTouchControls 제거", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("확인", 
                    $"정말로 {foundMMTouchControls.Count}개의 MMTouchControls 컴포넌트를 모두 제거하시겠습니까?", 
                    "제거", "취소"))
                {
                    RemoveAllMMTouchControls();
                }
            }
        }
        else if (foundMMTouchControls.Count == 0 && !isScanning)
        {
            EditorGUILayout.HelpBox("🎉 MMTouchControls 컴포넌트가 발견되지 않았습니다!", MessageType.Info);
        }
        
        EditorGUILayout.Space();
        
        // 정보 표시
        EditorGUILayout.HelpBox(
            "💡 팁:\n" +
            "• MMTouchControls는 Feel 에셋의 모바일 터치 컨트롤 컴포넌트입니다\n" +
            "• 이 컴포넌트가 데스크톱에서 Canvas를 비활성화시킵니다\n" +
            "• 제거 후 게임을 재실행하여 Canvas 문제가 해결되었는지 확인하세요", 
            MessageType.Info);
    }
    
    void AutoScanAndRemove()
    {
        Debug.Log("[MMTouchControlsRemover] 🚀 자동 스캔 및 제거 시작...");
        
        ScanForMMTouchControls();
        
        if (foundMMTouchControls.Count > 0)
        {
            Debug.Log($"[MMTouchControlsRemover] 🎯 {foundMMTouchControls.Count}개의 MMTouchControls 발견, 자동 제거 중...");
            RemoveAllMMTouchControls();
            
            EditorUtility.DisplayDialog("✅ 제거 완료!", 
                $"🎉 {foundMMTouchControls.Count}개의 MMTouchControls 컴포넌트가 제거되었습니다!\n\n" +
                "이제 Canvas 비활성화 문제가 해결되었습니다.\n" +
                "Unity에서 Play를 눌러 확인해보세요!", "확인");
        }
        else
        {
            EditorUtility.DisplayDialog("🔍 결과", 
                "MMTouchControls 컴포넌트가 발견되지 않았습니다.\n\n" +
                "다른 원인이 있을 수 있으니 Canvas 모니터링을 확인해보세요.", "확인");
        }
    }

    void ScanForMMTouchControls()
    {
        isScanning = true;
        foundMMTouchControls.Clear();
        
        try
        {
            // 씬의 모든 GameObject 검색
            GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
            
            foreach (var obj in allObjects)
            {
                // MMTouchControls 컴포넌트 확인 (안전한 방법)
                Component mmTouchControls = null;
                try 
                {
                    // 동적으로 MMTouchControls 타입 검색
                    var mmTouchControlsType = System.Type.GetType("MoreMountains.Tools.MMTouchControls, Assembly-CSharp");
                    if (mmTouchControlsType != null)
                    {
                        mmTouchControls = obj.GetComponent(mmTouchControlsType);
                    }
                }
                catch (System.Exception)
                {
                    // MoreMountains 네임스페이스가 없는 경우 무시
                    mmTouchControls = null;
                }
                
                if (mmTouchControls != null)
                {
                    foundMMTouchControls.Add(obj);
                    Debug.Log($"[MMTouchControlsRemover] MMTouchControls 발견: {GetGameObjectPath(obj)}");
                }
            }
            
            Debug.Log($"[MMTouchControlsRemover] 스캔 완료: {foundMMTouchControls.Count}개 발견");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MMTouchControlsRemover] 스캔 중 오류: {e.Message}");
        }
        finally
        {
            isScanning = false;
        }
    }

    void RemoveMMTouchControlsFromObject(GameObject obj)
    {
        if (obj == null) return;
        
        Component mmTouchControls = null;
        try 
        {
            // 동적으로 MMTouchControls 타입 검색
            var mmTouchControlsType = System.Type.GetType("MoreMountains.Tools.MMTouchControls, Assembly-CSharp");
            if (mmTouchControlsType != null)
            {
                mmTouchControls = obj.GetComponent(mmTouchControlsType);
            }
        }
        catch (System.Exception)
        {
            // MoreMountains 네임스페이스가 없는 경우 무시
            mmTouchControls = null;
        }
        
        if (mmTouchControls != null)
        {
            string objPath = GetGameObjectPath(obj);
            
            Undo.RecordObject(obj, "Remove MMTouchControls");
            DestroyImmediate(mmTouchControls);
            
            Debug.Log($"[MMTouchControlsRemover] ✅ MMTouchControls 제거됨: {objPath}");
            
            EditorUtility.SetDirty(obj);
        }
    }

    void RemoveAllMMTouchControls()
    {
        int removedCount = 0;
        
        for (int i = foundMMTouchControls.Count - 1; i >= 0; i--)
        {
            if (foundMMTouchControls[i] != null)
            {
                RemoveMMTouchControlsFromObject(foundMMTouchControls[i]);
                removedCount++;
            }
        }
        
        foundMMTouchControls.Clear();
        
        Debug.Log($"[MMTouchControlsRemover] 🎉 {removedCount}개의 MMTouchControls 컴포넌트를 제거했습니다!");
        
        // 씬 저장 권장
        if (removedCount > 0)
        {
            EditorUtility.DisplayDialog("완료", 
                $"{removedCount}개의 MMTouchControls 컴포넌트를 제거했습니다!\n\n" +
                "이제 게임을 실행해서 Canvas 비활성화 문제가 해결되었는지 확인하세요.", 
                "확인");
        }
    }

    string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return "null";
        
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
} 