using UnityEngine;
using UnityEditor;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.Net.Http.Headers;

// 요청 클래스들
[System.Serializable]
public class ClaudeRequest
{
    public string model;
    public int max_tokens;
    public List<ClaudeMessage> messages;
}

[System.Serializable]
public class ClaudeMessage
{
    public string role;
    public List<ContentItem> content;
}

[System.Serializable]
public class ContentItem
{
    public string type;
    public string text;
    public ImageSource source;
}

[System.Serializable]
public class ImageSource
{
    public string type;
    public string media_type;
    public string data;
}

// 응답 클래스들
[System.Serializable]
public class ClaudeResponse
{
    public string id;
    public string type;
    public string role;
    public List<ContentBlock> content;
    public string model;
    public string stop_reason;
    public int? stop_sequence;
    public Usage usage;
}

[System.Serializable]
public class ContentBlock
{
    public string type;
    public string text;
}

[System.Serializable]
public class Usage
{
    public int input_tokens;
    public int output_tokens;
}

public class ClaudeUnityEditor : EditorWindow
{
    private string claudeApiKey = "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";
    private string userPrompt = "";
    private string claudeResponse = "";
    private Vector2 scrollPosition;
    private static readonly HttpClient client = new HttpClient();
    
    // 스크립트 편집 관련 변수들
    private List<string> scriptFiles = new List<string>();
    private string selectedScriptPath = "";
    private string scriptContent = "";
    private Vector2 scriptListScrollPosition;
    private Vector2 scriptContentScrollPosition;
    private string searchFilter = "";
    
    // 오브젝트 검색 관련 변수들
    private List<GameObject> foundObjects = new List<GameObject>();
    private string objectSearchFilter = "";
    private GameObject selectedObject = null;
    private Vector2 objectListScrollPosition;
    
    // 이미지 인식 관련 변수들
    private Texture2D uploadedImage = null;
    private string imagePath = "";

    [MenuItem("Window/Claude Unity Editor")]
    public static void ShowWindow()
    {
        GetWindow<ClaudeUnityEditor>("Claude Unity Editor");
    }

    void OnGUI()
    {
        GUILayout.Label("Claude Unity Editor", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        GUILayout.Label("API Key:");
        claudeApiKey = EditorGUILayout.PasswordField(claudeApiKey);
        
        GUILayout.Space(10);
        
        // 탭 선택
        string[] tabs = { "스크립트 편집", "오브젝트 제어", "이미지 인식" };
        int selectedTab = GUILayout.Toolbar(selectedTabIndex, tabs);
        selectedTabIndex = selectedTab;
        
        GUILayout.Space(10);
        
        switch (selectedTab)
        {
            case 0:
                DrawScriptEditTab();
                break;
            case 1:
                DrawObjectControlTab();
                break;
            case 2:
                DrawImageRecognitionTab();
                break;
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("명령어 입력:");
        userPrompt = EditorGUILayout.TextArea(userPrompt, GUILayout.Height(100));
        
        if (GUILayout.Button("Claude에게 전송"))
        {
            SendToClaudeAsync();
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("Claude 응답:");
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.TextArea(claudeResponse, GUILayout.Height(200));
        EditorGUILayout.EndScrollView();
        
        if (GUILayout.Button("응답 실행"))
        {
            ExecuteClaudeCommand();
        }
    }
    
    private int selectedTabIndex = 0;
    
    void DrawScriptEditTab()
    {
        // 스크립트 검색 섹션
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("스크립트 검색:", GUILayout.Width(100));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        if (GUILayout.Button("검색", GUILayout.Width(60)))
        {
            SearchScripts();
        }
        EditorGUILayout.EndHorizontal();
        
        // 스크립트 목록
        if (scriptFiles.Count > 0)
        {
            GUILayout.Label($"스크립트 목록 ({scriptFiles.Count}개):");
            scriptListScrollPosition = EditorGUILayout.BeginScrollView(scriptListScrollPosition, GUILayout.Height(100));
            foreach (string scriptPath in scriptFiles)
            {
                string fileName = Path.GetFileName(scriptPath);
                if (GUILayout.Button(fileName, selectedScriptPath == scriptPath ? EditorStyles.boldLabel : EditorStyles.label))
                {
                    SelectScript(scriptPath);
                }
            }
            EditorGUILayout.EndScrollView();
        }
        
        // 선택된 스크립트 내용
        if (!string.IsNullOrEmpty(selectedScriptPath))
        {
            GUILayout.Label($"선택된 스크립트: {Path.GetFileName(selectedScriptPath)}");
            scriptContentScrollPosition = EditorGUILayout.BeginScrollView(scriptContentScrollPosition, GUILayout.Height(150));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(scriptContent);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndScrollView();
        }
    }
    
    void DrawObjectControlTab()
    {
        // 오브젝트 검색 섹션
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("오브젝트 검색:", GUILayout.Width(100));
        objectSearchFilter = EditorGUILayout.TextField(objectSearchFilter);
        if (GUILayout.Button("검색", GUILayout.Width(60)))
        {
            SearchGameObjects();
        }
        EditorGUILayout.EndHorizontal();
        
        // 오브젝트 목록
        if (foundObjects.Count > 0)
        {
            GUILayout.Label($"오브젝트 목록 ({foundObjects.Count}개):");
            objectListScrollPosition = EditorGUILayout.BeginScrollView(objectListScrollPosition, GUILayout.Height(150));
            foreach (GameObject obj in foundObjects)
            {
                if (obj != null)
                {
                    if (GUILayout.Button(obj.name, selectedObject == obj ? EditorStyles.boldLabel : EditorStyles.label))
                    {
                        selectedObject = obj;
                        Selection.activeGameObject = obj;
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
        
        // 선택된 오브젝트 정보
        if (selectedObject != null)
        {
            GUILayout.Label($"선택된 오브젝트: {selectedObject.name}");
            EditorGUILayout.ObjectField("GameObject", selectedObject, typeof(GameObject), true);
        }
    }
    
    void DrawImageRecognitionTab()
    {
        GUILayout.Label("이미지 업로드:");
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("이미지 선택", GUILayout.Width(100)))
        {
            imagePath = EditorUtility.OpenFilePanel("이미지 선택", "", "png,jpg,jpeg");
            if (!string.IsNullOrEmpty(imagePath))
            {
                LoadImage(imagePath);
            }
        }
        
        if (!string.IsNullOrEmpty(imagePath))
        {
            GUILayout.Label($"선택된 파일: {Path.GetFileName(imagePath)}");
        }
        EditorGUILayout.EndHorizontal();
        
        if (uploadedImage != null)
        {
            GUILayout.Label("미리보기:");
            float maxWidth = 300;
            float scale = maxWidth / uploadedImage.width;
            GUILayout.Box(uploadedImage, GUILayout.Width(maxWidth), GUILayout.Height(uploadedImage.height * scale));
        }
    }
    
    void LoadImage(string path)
    {
        byte[] imageData = File.ReadAllBytes(path);
        uploadedImage = new Texture2D(2, 2);
        uploadedImage.LoadImage(imageData);
    }
    
    void SearchScripts()
    {
        scriptFiles.Clear();
        string[] allScripts = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
        
        if (string.IsNullOrEmpty(searchFilter))
        {
            scriptFiles.AddRange(allScripts);
        }
        else
        {
            foreach (string script in allScripts)
            {
                string fileName = Path.GetFileName(script);
                if (fileName.ToLower().Contains(searchFilter.ToLower()))
                {
                    scriptFiles.Add(script);
                }
            }
        }
        
        Debug.Log($"{scriptFiles.Count}개의 스크립트를 찾았습니다.");
    }
    
    void SelectScript(string scriptPath)
    {
        selectedScriptPath = scriptPath;
        try
        {
            scriptContent = File.ReadAllText(scriptPath);
            Debug.Log($"스크립트 로드됨: {Path.GetFileName(scriptPath)}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"스크립트 읽기 실패: {e.Message}");
            scriptContent = "스크립트를 읽을 수 없습니다.";
        }
    }
    
    void SearchGameObjects()
    {
        foundObjects.Clear();
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        if (string.IsNullOrEmpty(objectSearchFilter))
        {
            foundObjects.AddRange(allObjects);
        }
        else
        {
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.ToLower().Contains(objectSearchFilter.ToLower()))
                {
                    foundObjects.Add(obj);
                }
            }
        }
        
        Debug.Log($"{foundObjects.Count}개의 오브젝트를 찾았습니다.");
    }

    async void SendToClaudeAsync()
    {
        if (string.IsNullOrEmpty(claudeApiKey))
        {
            Debug.LogError("API Key를 입력해주세요!");
            return;
        }

        string fullPrompt = userPrompt;
        
        // 선택된 스크립트가 있으면 내용 포함
        if (!string.IsNullOrEmpty(selectedScriptPath) && !string.IsNullOrEmpty(scriptContent))
        {
            fullPrompt = $"다음 Unity 스크립트를 참고해서 작업해줘:\n\n파일명: {Path.GetFileName(selectedScriptPath)}\n\n```csharp\n{scriptContent}\n```\n\n요청사항: {userPrompt}";
        }
        
        // 선택된 오브젝트가 있으면 정보 포함
        if (selectedObject != null)
        {
            string objInfo = GetObjectInfo(selectedObject);
            fullPrompt = $"다음 Unity 오브젝트를 참고해서 작업해줘:\n\n{objInfo}\n\n요청사항: {userPrompt}";
        }

        // 요청 객체 생성
        List<ContentItem> contentItems = new List<ContentItem>();
        contentItems.Add(new ContentItem
        {
            type = "text",
            text = $"Unity Editor 스크립트 명령어로 변환해줘. 다음 작업을 수행하는 C# 코드만 작성해줘: {fullPrompt}"
        });
        
        // 이미지가 있으면 추가
        if (uploadedImage != null && !string.IsNullOrEmpty(imagePath))
        {
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            string base64Image = Convert.ToBase64String(imageBytes);
            
            contentItems.Add(new ContentItem
            {
                type = "image",
                source = new ImageSource
                {
                    type = "base64",
                    media_type = GetMediaType(imagePath),
                    data = base64Image
                }
            });
        }

        var requestBody = new ClaudeRequest
        {
            model = "claude-opus-4-20250514",
            max_tokens = 4096,
            messages = new List<ClaudeMessage>
            {
                new ClaudeMessage
                {
                    role = "user",
                    content = contentItems
                }
            }
        };

        try
        {
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-api-key", claudeApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);
            var responseString = await response.Content.ReadAsStringAsync();
            
            Debug.Log($"API 응답 상태: {response.StatusCode}");
            Debug.Log($"API 원본 응답: {responseString}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<ClaudeResponse>(responseString);
                if (result != null && result.content != null && result.content.Count > 0)
                {
                    claudeResponse = result.content[0].text;
                    Debug.Log($"파싱된 응답: {claudeResponse}");
                }
                else
                {
                    Debug.LogError("응답 형식이 올바르지 않습니다.");
                    claudeResponse = "오류: 응답을 파싱할 수 없습니다.";
                }
            }
            else
            {
                Debug.LogError($"API 오류: {response.StatusCode} - {responseString}");
                claudeResponse = $"API 오류: {response.StatusCode}";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"예외 발생: {e.GetType().Name} - {e.Message}");
            Debug.LogError($"스택 트레이스: {e.StackTrace}");
            claudeResponse = $"오류: {e.Message}";
        }
    }
    
    string GetMediaType(string path)
    {
        string extension = Path.GetExtension(path).ToLower();
        switch (extension)
        {
            case ".jpg":
            case ".jpeg":
                return "image/jpeg";
            case ".png":
                return "image/png";
            default:
                return "image/jpeg";
        }
    }
    
    string GetObjectInfo(GameObject obj)
    {
        string info = $"오브젝트 이름: {obj.name}\n";
        info += $"위치: {obj.transform.position}\n";
        info += $"회전: {obj.transform.rotation.eulerAngles}\n";
        info += $"스케일: {obj.transform.localScale}\n";
        info += $"활성화 상태: {obj.activeSelf}\n";
        info += $"태그: {obj.tag}\n";
        info += $"레이어: {LayerMask.LayerToName(obj.layer)}\n";
        
        info += "\n컴포넌트 목록:\n";
        Component[] components = obj.GetComponents<Component>();
        foreach (Component comp in components)
        {
            if (comp != null)
            {
                info += $"- {comp.GetType().Name}\n";
            }
        }
        
        return info;
    }

    void ExecuteClaudeCommand()
    {
        if (string.IsNullOrEmpty(claudeResponse))
        {
            Debug.LogWarning("실행할 응답이 없습니다.");
            return;
        }

        // 스크립트 수정 명령 처리
        if (claudeResponse.Contains("File.WriteAllText") && !string.IsNullOrEmpty(selectedScriptPath))
        {
            // Claude 응답에서 코드 블록 추출
            string pattern = @"```(?:csharp|cs)?\s*([\s\S]*?)```";
            var match = System.Text.RegularExpressions.Regex.Match(claudeResponse, pattern);
            
            if (match.Success)
            {
                string newCode = match.Groups[1].Value.Trim();
                try
                {
                    File.WriteAllText(selectedScriptPath, newCode);
                    AssetDatabase.Refresh();
                    Debug.Log($"스크립트가 수정되었습니다: {Path.GetFileName(selectedScriptPath)}");
                    
                    // 수정된 내용 다시 로드
                    SelectScript(selectedScriptPath);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"스크립트 저장 실패: {e.Message}");
                }
            }
        }

        // GameObject 생성 예제
        if (claudeResponse.Contains("GameObject.CreatePrimitive"))
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Claude가 만든 큐브";
            Undo.RegisterCreatedObjectUndo(cube, "Create Cube");
            EditorUtility.SetDirty(cube);
            Selection.activeGameObject = cube;
            Debug.Log("큐브가 생성되었습니다!");
        }
        
        // TextMeshPro 생성 예제
        if (claudeResponse.Contains("TextMeshPro"))
        {
            GameObject textObj = new GameObject("Claude Text");
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = "Claude가 생성한 텍스트";
            Undo.RegisterCreatedObjectUndo(textObj, "Create Text");
            EditorUtility.SetDirty(textObj);
            Selection.activeGameObject = textObj;
            Debug.Log("TextMeshPro가 생성되었습니다!");
        }
        
        // Scene 저장
        if (claudeResponse.Contains("SaveScene"))
        {
            EditorApplication.ExecuteMenuItem("File/Save");
            Debug.Log("Scene이 저장되었습니다!");
        }
        
        // 스크립트 생성
        if (claudeResponse.Contains("CreateScript") && claudeResponse.Contains("```"))
        {
            string pattern = @"CreateScript\s*\(\s*""([^""]+)""\s*\)";
            var match = System.Text.RegularExpressions.Regex.Match(claudeResponse, pattern);
            
            if (match.Success)
            {
                string scriptName = match.Groups[1].Value;
                string codePattern = @"```(?:csharp|cs)?\s*([\s\S]*?)```";
                var codeMatch = System.Text.RegularExpressions.Regex.Match(claudeResponse, codePattern);
                
                if (codeMatch.Success)
                {
                    string scriptPath = $"Assets/{scriptName}.cs";
                    File.WriteAllText(scriptPath, codeMatch.Groups[1].Value.Trim());
                    AssetDatabase.Refresh();
                    Debug.Log($"새 스크립트가 생성되었습니다: {scriptName}.cs");
                }
            }
        }
        
        // 오브젝트 제어 명령 처리
        if (selectedObject != null)
        {
            // 위치 변경
            if (claudeResponse.Contains("transform.position"))
            {
                string pattern = @"transform\.position\s*=\s*new\s+Vector3\s*\(\s*(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\s*\)";
                var match = System.Text.RegularExpressions.Regex.Match(claudeResponse, pattern);
                if (match.Success)
                {
                    float x = float.Parse(match.Groups[1].Value);
                    float y = float.Parse(match.Groups[2].Value);
                    float z = float.Parse(match.Groups[3].Value);
                    Undo.RecordObject(selectedObject.transform, "Move Object");
                    selectedObject.transform.position = new Vector3(x, y, z);
                    EditorUtility.SetDirty(selectedObject);
                    Debug.Log($"{selectedObject.name}의 위치가 변경되었습니다: ({x}, {y}, {z})");
                }
            }
            
            // 회전 변경
            if (claudeResponse.Contains("transform.rotation"))
            {
                string pattern = @"transform\.rotation\s*=\s*Quaternion\.Euler\s*\(\s*(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\s*\)";
                var match = System.Text.RegularExpressions.Regex.Match(claudeResponse, pattern);
                if (match.Success)
                {
                    float x = float.Parse(match.Groups[1].Value);
                    float y = float.Parse(match.Groups[2].Value);
                    float z = float.Parse(match.Groups[3].Value);
                    Undo.RecordObject(selectedObject.transform, "Rotate Object");
                    selectedObject.transform.rotation = Quaternion.Euler(x, y, z);
                    EditorUtility.SetDirty(selectedObject);
                    Debug.Log($"{selectedObject.name}의 회전이 변경되었습니다: ({x}, {y}, {z})");
                }
            }
            
            // 스케일 변경
            if (claudeResponse.Contains("transform.localScale"))
            {
                string pattern = @"transform\.localScale\s*=\s*new\s+Vector3\s*\(\s*(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\s*\)";
                var match = System.Text.RegularExpressions.Regex.Match(claudeResponse, pattern);
                if (match.Success)
                {
                    float x = float.Parse(match.Groups[1].Value);
                    float y = float.Parse(match.Groups[2].Value);
                    float z = float.Parse(match.Groups[3].Value);
                    Undo.RecordObject(selectedObject.transform, "Scale Object");
                    selectedObject.transform.localScale = new Vector3(x, y, z);
                    EditorUtility.SetDirty(selectedObject);
                    Debug.Log($"{selectedObject.name}의 스케일이 변경되었습니다: ({x}, {y}, {z})");
                }
            }
            
            // 활성화/비활성화
            if (claudeResponse.Contains("SetActive"))
            {
                string pattern = @"SetActive\s*\(\s*(true|false)\s*\)";
                var match = System.Text.RegularExpressions.Regex.Match(claudeResponse, pattern);
                if (match.Success)
                {
                    bool active = bool.Parse(match.Groups[1].Value);
                    Undo.RecordObject(selectedObject, "Toggle Active");
                    selectedObject.SetActive(active);
                    EditorUtility.SetDirty(selectedObject);
                    Debug.Log($"{selectedObject.name}가 {(active ? "활성화" : "비활성화")}되었습니다.");
                }
            }
        }
        
        // 컴포넌트 추가 명령
        if (selectedObject != null && claudeResponse.Contains("AddComponent"))
        {
            // Rigidbody 추가
            if (claudeResponse.Contains("AddComponent<Rigidbody>"))
            {
                Undo.AddComponent<Rigidbody>(selectedObject);
                Debug.Log($"{selectedObject.name}에 Rigidbody가 추가되었습니다.");
            }
            
            // BoxCollider 추가
            if (claudeResponse.Contains("AddComponent<BoxCollider>"))
            {
                Undo.AddComponent<BoxCollider>(selectedObject);
                Debug.Log($"{selectedObject.name}에 BoxCollider가 추가되었습니다.");
            }
            
            // TextMeshPro 추가
            if (claudeResponse.Contains("AddComponent<TextMeshPro>"))
            {
                Undo.AddComponent<TextMeshPro>(selectedObject);
                Debug.Log($"{selectedObject.name}에 TextMeshPro가 추가되었습니다.");
            }
        }
        
        // 아무 작업도 수행되지 않았을 때
        if (!claudeResponse.Contains("GameObject.CreatePrimitive") && 
            !claudeResponse.Contains("TextMeshPro") && 
            !claudeResponse.Contains("SaveScene") &&
            !claudeResponse.Contains("File.WriteAllText") &&
            !claudeResponse.Contains("CreateScript") &&
            !claudeResponse.Contains("transform.") &&
            !claudeResponse.Contains("SetActive") &&
            !claudeResponse.Contains("AddComponent"))
        {
            Debug.LogWarning("Claude의 응답에서 실행 가능한 명령을 찾을 수 없습니다.");
        }
    }
}