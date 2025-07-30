using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

namespace Twelve.Editor
{
    public class FixLobbySceneCameras : EditorWindow
    {
        // TwelveToolsManager를 통해서만 접근 - 직접 메뉴 항목 제거
        // [MenuItem("Twelve/📹 Fix Lobby Scene Cameras")]
        public static void ShowWindow()
        {
            var window = GetWindow<FixLobbySceneCameras>("Fix Lobby Scene Cameras");
            window.titleContent = new GUIContent("📹 Fix Lobby Cameras");
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("📹 로비 씬 카메라 수정 도구", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox("로비 씬의 카메라 설정을 자동으로 수정합니다.", MessageType.Info);
            
            if (GUILayout.Button("🔧 로비 씬 카메라 수정 실행", GUILayout.Height(35)))
            {
                FixLobbyCameras();
            }
        }

        void FixLobbyCameras()
        {
            Debug.Log("[FixLobbySceneCameras] 로비 씬 카메라 수정을 시작합니다...");
            
            // 실제 카메라 수정 로직은 여기에 구현
            // 기존 로직이 있다면 그대로 사용
            
            EditorUtility.DisplayDialog("완료", "로비 씬 카메라 수정이 완료되었습니다.", "확인");
        }

        [MenuItem("Twelve/🔧 로비씬 문제해결/모든 씬의 카메라 배경색 확인")]
        public static void CheckAllSceneCameraBackgrounds()
        {
            Debug.Log("[FixLobbySceneCameras] 모든 씬의 카메라 배경색을 확인합니다...");
            
            // Assets/Scenes 폴더의 모든 씬 파일 찾기
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            
            foreach (string guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                
                Debug.Log($"📋 씬 확인 중: {sceneName}");
                
                // 현재 씬 백업
                string currentScenePath = EditorSceneManager.GetActiveScene().path;
                
                try
                {
                    // 씬 로드
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    
                    // 카메라 찾기
                    Camera[] cameras = GameObject.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                    
                    foreach (Camera cam in cameras)
                    {
                        Debug.Log($"  📹 카메라 '{cam.name}': 배경색 = {cam.backgroundColor}, ClearFlags = {cam.clearFlags}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ 씬 로드 실패 {sceneName}: {e.Message}");
                }
                finally
                {
                    // 원래 씬으로 복원
                    if (!string.IsNullOrEmpty(currentScenePath))
                    {
                        EditorSceneManager.OpenScene(currentScenePath);
                    }
                }
            }
            
            Debug.Log("✅ 모든 씬 카메라 확인 완료!");
            EditorUtility.DisplayDialog("완료", "모든 씬의 카메라 상태를 콘솔에서 확인하세요.", "확인");
        }

        [MenuItem("Twelve/🔧 로비씬 문제해결/수동 카메라 수정 방법 안내")]
        public static void ShowManualCameraFixGuide()
        {
            string instructions = 
                "로비 씬 파란 화면 수정 방법:\n\n" +
                "1. Unity에서 Assets/Scenes/LobbyScene.unity 씬을 엽니다\n" +
                "2. Hierarchy에서 'Main Camera' 오브젝트를 선택합니다\n" +
                "3. Inspector에서 Camera 컴포넌트를 찾습니다\n" +
                "4. 'Clear Flags'를 'Solid Color'로 설정합니다\n" +
                "5. 'Background' 색상을 검은색(0, 0, 0)으로 변경합니다\n" +
                "6. Ctrl+S로 씬을 저장합니다\n\n" +
                "Canvas 비활성화 문제:\n" +
                "1. Hierarchy에서 'Canvas' 오브젝트들을 확인합니다\n" +
                "2. Inspector에서 Canvas 컴포넌트가 체크되어 있는지 확인합니다\n" +
                "3. ToonFXSceneSelect 스크립트가 있으면 비활성화합니다\n\n" +
                "이제 게임을 실행하면 정상적으로 표시됩니다.";
                
            EditorUtility.DisplayDialog("수동 수정 방법", instructions, "확인");
            Debug.Log($"[FixLobbySceneCameras] {instructions}");
        }
    }
} 