using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Twelve.Editor
{
    public class FixLobbySceneCameras
    {
        [MenuItem("Twelve/🔧 로비 씬 카메라 수정 (파란 화면 해결)")]
        public static void FixLobbySceneCameraBackground()
        {
            Debug.Log("[FixLobbySceneCameras] 로비 씬 카메라 배경색 수정을 시작합니다...");

            string currentScenePath = EditorSceneManager.GetActiveScene().path;

            try
            {
                // 로비 씬 로드
                var lobbyScene = EditorSceneManager.OpenScene("Assets/Scenes/LobbyScene.unity");

                // Main Camera 찾기
                Camera mainCamera = null;
                GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

                foreach (GameObject obj in allObjects)
                {
                    if (obj.name == "Main Camera" && obj.CompareTag("MainCamera"))
                    {
                        mainCamera = obj.GetComponent<Camera>();
                        break;
                    }
                }

                if (mainCamera == null)
                {
                    // 태그로 다시 시도
                    GameObject cameraObj = GameObject.FindWithTag("MainCamera");
                    if (cameraObj != null)
                    {
                        mainCamera = cameraObj.GetComponent<Camera>();
                    }
                }

                // 모든 카메라 찾기 (Main Camera를 못 찾은 경우)
                if (mainCamera == null)
                {
                    Camera[] allCameras = GameObject.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                    if (allCameras.Length > 0)
                    {
                        mainCamera = allCameras[0]; // 첫 번째 카메라 사용
                        Debug.LogWarning($"[FixLobbySceneCameras] Main Camera를 찾지 못해 첫 번째 카메라 '{mainCamera.gameObject.name}'를 사용합니다.");
                    }
                }

                if (mainCamera != null)
                {
                    Debug.Log($"[FixLobbySceneCameras] Main Camera 발견: {mainCamera.gameObject.name}");
                    Debug.Log($"[FixLobbySceneCameras] 현재 설정 - Clear Flags: {mainCamera.clearFlags}, Background Color: {mainCamera.backgroundColor}");

                    // 카메라 설정을 검은 배경으로 변경
                    mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    mainCamera.backgroundColor = Color.black;

                    Debug.Log($"[FixLobbySceneCameras] 변경된 설정 - Clear Flags: {mainCamera.clearFlags}, Background Color: {mainCamera.backgroundColor}");

                    // 변경사항 저장
                    EditorSceneManager.MarkSceneDirty(lobbyScene);
                    EditorSceneManager.SaveScene(lobbyScene);

                    Debug.Log("[FixLobbySceneCameras] ✅ 로비 씬 카메라 배경색이 검은색으로 변경되었습니다!");
                    
                    EditorUtility.DisplayDialog("성공", 
                        "로비 씬의 카메라 배경색이 검은색으로 변경되었습니다!\n" +
                        "이제 게임을 실행하면 파란 화면 대신 검은 화면이 표시됩니다.", 
                        "확인");
                }
                else
                {
                    Debug.LogError("[FixLobbySceneCameras] ❌ Main Camera를 찾을 수 없습니다!");
                    EditorUtility.DisplayDialog("오류", 
                        "로비 씬에서 Main Camera를 찾을 수 없습니다.\n" +
                        "수동으로 카메라 설정을 확인해주세요.", 
                        "확인");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FixLobbySceneCameras] ❌ 오류 발생: {e.Message}");
                EditorUtility.DisplayDialog("오류", 
                    $"카메라 수정 중 오류가 발생했습니다:\n{e.Message}", 
                    "확인");
            }
            finally
            {
                // 원래 씬으로 복원
                if (!string.IsNullOrEmpty(currentScenePath) && currentScenePath != "Assets/Scenes/LobbyScene.unity")
                {
                    EditorSceneManager.OpenScene(currentScenePath);
                }
            }
        }

        [MenuItem("Twelve/🔧 로비 씬 Canvas 활성화 문제 해결")]
        public static void FixLobbyCanvasActivation()
        {
            Debug.Log("[FixLobbySceneCameras] 로비 씬 Canvas 활성화 문제 해결을 시작합니다...");

            string currentScenePath = EditorSceneManager.GetActiveScene().path;

            try
            {
                // 로비 씬 로드
                var lobbyScene = EditorSceneManager.OpenScene("Assets/Scenes/LobbyScene.unity");

                // 모든 Canvas 찾기
                Canvas[] allCanvases = GameObject.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                int fixedCount = 0;

                Debug.Log($"[FixLobbySceneCameras] 총 {allCanvases.Length}개의 Canvas 발견");

                foreach (Canvas canvas in allCanvases)
                {
                    if (!canvas.enabled)
                    {
                        Debug.LogWarning($"[FixLobbySceneCameras] 비활성화된 Canvas 발견: {canvas.gameObject.name}");
                        canvas.enabled = true;
                        fixedCount++;
                        Debug.Log($"[FixLobbySceneCameras] ✅ Canvas '{canvas.gameObject.name}' 활성화됨");
                    }
                    else
                    {
                        Debug.Log($"[FixLobbySceneCameras] Canvas '{canvas.gameObject.name}' 이미 활성화됨");
                    }
                }

                // ToonFXSceneSelect 등 Canvas를 비활성화할 수 있는 스크립트들 찾기 및 비활성화
                GameObject[] allGameObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (GameObject go in allGameObjects)
                {
                    MonoBehaviour[] scripts = go.GetComponents<MonoBehaviour>();
                    foreach (MonoBehaviour script in scripts)
                    {
                        if (script != null && script.GetType().Name == "ToonFXSceneSelect")
                        {
                            if (script.enabled)
                            {
                                script.enabled = false;
                                Debug.LogWarning($"[FixLobbySceneCameras] ToonFXSceneSelect 스크립트를 비활성화했습니다: {script.gameObject.name}");
                                fixedCount++;
                            }
                        }
                    }
                }

                if (fixedCount > 0)
                {
                    // 변경사항 저장
                    EditorSceneManager.MarkSceneDirty(lobbyScene);
                    EditorSceneManager.SaveScene(lobbyScene);

                    Debug.Log($"[FixLobbySceneCameras] ✅ {fixedCount}개의 Canvas/Script 문제를 수정했습니다!");
                    
                    EditorUtility.DisplayDialog("성공", 
                        $"{fixedCount}개의 Canvas 활성화 문제를 해결했습니다!\n" +
                        "이제 로비 씬에서 UI가 정상적으로 표시될 것입니다.", 
                        "확인");
                }
                else
                {
                    Debug.Log("[FixLobbySceneCameras] 모든 Canvas가 이미 활성화되어 있습니다.");
                    EditorUtility.DisplayDialog("정보", 
                        "모든 Canvas가 이미 활성화되어 있습니다.\n" +
                        "다른 원인이 있을 수 있으니 콘솔 로그를 확인해주세요.", 
                        "확인");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FixLobbySceneCameras] ❌ 오류 발생: {e.Message}");
                EditorUtility.DisplayDialog("오류", 
                    $"Canvas 수정 중 오류가 발생했습니다:\n{e.Message}", 
                    "확인");
            }
            finally
            {
                // 원래 씬으로 복원
                if (!string.IsNullOrEmpty(currentScenePath) && currentScenePath != "Assets/Scenes/LobbyScene.unity")
                {
                    EditorSceneManager.OpenScene(currentScenePath);
                }
            }
        }

        [MenuItem("Twelve/🔧 모든 씬의 카메라 배경색 확인")]
        public static void CheckAllSceneCameras()
        {
            Debug.Log("[FixLobbySceneCameras] 모든 씬의 카메라 설정을 확인합니다...");

            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");

            foreach (string guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(scenePath);

                Camera[] cameras = GameObject.FindObjectsByType<Camera>(FindObjectsSortMode.None);

                Debug.Log($"[씬: {scene.name}] 카메라 {cameras.Length}개 발견");

                foreach (Camera camera in cameras)
                {
                    Debug.Log($"  - {camera.gameObject.name}: Clear Flags = {camera.clearFlags}, Background = {camera.backgroundColor}");
                }
            }
        }
        
        [MenuItem("Twelve/🔧 로비 씬에서 수동 카메라 수정 방법 안내")]
        public static void ShowManualFixInstructions()
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