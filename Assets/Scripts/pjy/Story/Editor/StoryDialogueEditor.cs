#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace pjy.Story.Editor
{
    /// <summary>
    /// 스토리 대화 에디터 툴
    /// - 스토리 생성 및 편집
    /// - CSV 가져오기/내보내기
    /// - 실시간 미리보기
    /// </summary>
    public class StoryDialogueEditor : EditorWindow
    {
        // 데이터
        private StoryDatabase storyDatabase;
        private CharacterImageDatabase characterImageDatabase;
        private StoryData currentStory;
        private List<StoryData> stories = new List<StoryData>();
        
        // UI 상태
        private Vector2 storyListScrollPos;
        private Vector2 dialogueScrollPos;
        private int selectedStoryIndex = -1;
        private int selectedDialogueIndex = -1;
        
        // 편집 상태
        private string newStoryId = "";
        private string newStoryTitle = "";
        private string csvContent = "";
        private bool showCSVImport = false;
        private bool showPreview = false;
        
        // 스타일
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;
        private GUIStyle buttonStyle;
        
        [MenuItem("Tools/pjy/Story Dialogue Editor")]
        public static void ShowWindow()
        {
            StoryDialogueEditor window = GetWindow<StoryDialogueEditor>("Story Dialogue Editor");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            LoadData();
            InitializeStyles();
        }
        
        private void InitializeStyles()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };
            
            boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
            
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }
        
        private void LoadData()
        {
            // StoryDatabase 찾기
            string[] guids = AssetDatabase.FindAssets("t:StoryDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                storyDatabase = AssetDatabase.LoadAssetAtPath<StoryDatabase>(path);
                if (storyDatabase != null)
                {
                    stories = storyDatabase.GetAllStories();
                }
            }
            
            // CharacterImageDatabase 찾기
            guids = AssetDatabase.FindAssets("t:CharacterImageDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                characterImageDatabase = AssetDatabase.LoadAssetAtPath<CharacterImageDatabase>(path);
            }
        }
        
        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            
            // 왼쪽 패널 - 스토리 목록
            DrawStoryListPanel();
            
            // 구분선
            GUILayout.Box("", GUILayout.Width(2), GUILayout.ExpandHeight(true));
            
            // 오른쪽 패널 - 스토리 편집
            DrawStoryEditPanel();
            
            EditorGUILayout.EndHorizontal();
            
            // 하단 툴바
            DrawToolbar();
        }
        
        private void DrawStoryListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            
            // 헤더
            EditorGUILayout.LabelField("스토리 목록", headerStyle);
            EditorGUILayout.Space();
            
            // 새 스토리 생성
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("새 스토리 생성", EditorStyles.boldLabel);
            newStoryId = EditorGUILayout.TextField("스토리 ID", newStoryId);
            newStoryTitle = EditorGUILayout.TextField("스토리 제목", newStoryTitle);
            
            if (GUILayout.Button("스토리 생성", buttonStyle))
            {
                CreateNewStory();
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // 스토리 목록
            if (stories != null && stories.Count > 0)
            {
                storyListScrollPos = EditorGUILayout.BeginScrollView(storyListScrollPos);
                
                for (int i = 0; i < stories.Count; i++)
                {
                    var story = stories[i];
                    if (story == null) continue;
                    
                    Color originalColor = GUI.backgroundColor;
                    if (i == selectedStoryIndex)
                    {
                        GUI.backgroundColor = Color.cyan;
                    }
                    
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    
                    if (GUILayout.Button($"{story.storyTitle}\\n({story.storyId})", GUILayout.Height(40)))
                    {
                        SelectStory(i);
                    }
                    
                    if (GUILayout.Button("삭제", GUILayout.Width(50), GUILayout.Height(40)))
                    {
                        if (EditorUtility.DisplayDialog("스토리 삭제", $"'{story.storyTitle}' 스토리를 삭제하시겠습니까?", "삭제", "취소"))
                        {
                            DeleteStory(i);
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    GUI.backgroundColor = originalColor;
                }
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("스토리가 없습니다. 새 스토리를 생성하거나 CSV를 가져오세요.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawStoryEditPanel()
        {
            EditorGUILayout.BeginVertical();
            
            if (currentStory != null)
            {
                // 스토리 정보 편집
                EditorGUILayout.LabelField($"스토리 편집: {currentStory.storyTitle}", headerStyle);
                EditorGUILayout.Space();
                
                EditorGUILayout.BeginVertical(boxStyle);
                currentStory.storyTitle = EditorGUILayout.TextField("제목", currentStory.storyTitle);
                currentStory.storyDescription = EditorGUILayout.TextField("설명", currentStory.storyDescription);
                currentStory.storyType = (StoryType)EditorGUILayout.EnumPopup("타입", currentStory.storyType);
                currentStory.storyOrder = EditorGUILayout.IntField("순서", currentStory.storyOrder);
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space();
                
                // 대화 목록
                EditorGUILayout.LabelField("대화 목록", EditorStyles.boldLabel);
                
                if (GUILayout.Button("새 대화 추가", buttonStyle))
                {
                    AddNewDialogue();
                }
                
                EditorGUILayout.Space();
                
                if (currentStory.dialogueLines != null && currentStory.dialogueLines.Count > 0)
                {
                    dialogueScrollPos = EditorGUILayout.BeginScrollView(dialogueScrollPos);
                    
                    for (int i = 0; i < currentStory.dialogueLines.Count; i++)
                    {
                        DrawDialogueLine(i);
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.HelpBox("대화가 없습니다. 새 대화를 추가하세요.", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("스토리를 선택하세요.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawDialogueLine(int index)
        {
            var dialogue = currentStory.dialogueLines[index];
            
            Color originalColor = GUI.backgroundColor;
            if (index == selectedDialogueIndex)
            {
                GUI.backgroundColor = Color.yellow;
            }
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"대화 {index + 1}", EditorStyles.boldLabel, GUILayout.Width(80));
            
            if (GUILayout.Button("위로", GUILayout.Width(50)))
            {
                MoveDialogue(index, -1);
            }
            if (GUILayout.Button("아래로", GUILayout.Width(50)))
            {
                MoveDialogue(index, 1);
            }
            if (GUILayout.Button("삭제", GUILayout.Width(50)))
            {
                DeleteDialogue(index);
                GUI.backgroundColor = originalColor;
                return;
            }
            EditorGUILayout.EndHorizontal();
            
            // 캐릭터 이름
            dialogue.characterName = EditorGUILayout.TextField("캐릭터", dialogue.characterName);
            
            // 감정
            dialogue.emotion = (CharacterEmotion)EditorGUILayout.EnumPopup("감정", dialogue.emotion);
            
            // 위치
            dialogue.position = (CharacterPosition)EditorGUILayout.EnumPopup("위치", dialogue.position);
            
            // 대화 텍스트
            EditorGUILayout.LabelField("대화 내용:");
            dialogue.dialogueText = EditorGUILayout.TextArea(dialogue.dialogueText, GUILayout.Height(60));
            
            // 선택지
            if (dialogue.choices == null)
                dialogue.choices = new List<DialogueChoice>();
            
            EditorGUILayout.LabelField("선택지:");
            for (int j = 0; j < dialogue.choices.Count; j++)
            {
                EditorGUILayout.BeginHorizontal();
                dialogue.choices[j].choiceText = EditorGUILayout.TextField(dialogue.choices[j].choiceText);
                dialogue.choices[j].nextStoryId = EditorGUILayout.TextField(dialogue.choices[j].nextStoryId);
                if (GUILayout.Button("삭제", GUILayout.Width(50)))
                {
                    dialogue.choices.RemoveAt(j);
                    j--;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("선택지 추가"))
            {
                dialogue.choices.Add(new DialogueChoice());
            }
            
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = originalColor;
            
            EditorGUILayout.Space();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("데이터베이스 새로고침", EditorStyles.toolbarButton))
            {
                LoadData();
            }
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("CSV 가져오기", EditorStyles.toolbarButton))
            {
                showCSVImport = !showCSVImport;
            }
            
            if (GUILayout.Button("CSV 내보내기", EditorStyles.toolbarButton))
            {
                ExportToCSV();
            }
            
            if (GUILayout.Button("미리보기", EditorStyles.toolbarButton))
            {
                PreviewStory();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // CSV 가져오기 패널
            if (showCSVImport)
            {
                EditorGUILayout.BeginVertical(boxStyle);
                EditorGUILayout.LabelField("CSV 가져오기", EditorStyles.boldLabel);
                csvContent = EditorGUILayout.TextArea(csvContent, GUILayout.Height(100));
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("CSV 파일 선택"))
                {
                    string path = EditorUtility.OpenFilePanel("CSV 파일 선택", "", "csv");
                    if (!string.IsNullOrEmpty(path))
                    {
                        csvContent = File.ReadAllText(path);
                    }
                }
                
                if (GUILayout.Button("가져오기"))
                {
                    ImportFromCSV();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void CreateNewStory()
        {
            if (string.IsNullOrEmpty(newStoryId) || string.IsNullOrEmpty(newStoryTitle))
            {
                EditorUtility.DisplayDialog("오류", "스토리 ID와 제목을 입력하세요.", "확인");
                return;
            }
            
            if (storyDatabase == null)
            {
                CreateStoryDatabase();
            }
            
            StoryData newStory = ScriptableObject.CreateInstance<StoryData>();
            newStory.storyId = newStoryId;
            newStory.storyTitle = newStoryTitle;
            newStory.dialogueLines = new List<DialogueLine>();
            
            storyDatabase.AddStory(newStory);
            stories = storyDatabase.GetAllStories();
            
            newStoryId = "";
            newStoryTitle = "";
            
            EditorUtility.SetDirty(storyDatabase);
            AssetDatabase.SaveAssets();
        }
        
        private void CreateStoryDatabase()
        {
            storyDatabase = ScriptableObject.CreateInstance<StoryDatabase>();
            AssetDatabase.CreateAsset(storyDatabase, "Assets/Data/StoryDatabase.asset");
            AssetDatabase.SaveAssets();
        }
        
        private void SelectStory(int index)
        {
            selectedStoryIndex = index;
            if (index >= 0 && index < stories.Count)
            {
                currentStory = stories[index];
                selectedDialogueIndex = -1;
            }
        }
        
        private void DeleteStory(int index)
        {
            if (index >= 0 && index < stories.Count)
            {
                storyDatabase.RemoveStory(stories[index].storyId);
                stories = storyDatabase.GetAllStories();
                
                if (selectedStoryIndex == index)
                {
                    currentStory = null;
                    selectedStoryIndex = -1;
                }
                
                EditorUtility.SetDirty(storyDatabase);
                AssetDatabase.SaveAssets();
            }
        }
        
        private void AddNewDialogue()
        {
            if (currentStory != null)
            {
                if (currentStory.dialogueLines == null)
                    currentStory.dialogueLines = new List<DialogueLine>();
                
                DialogueLine newDialogue = new DialogueLine
                {
                    storyId = currentStory.storyId,
                    characterName = "캐릭터",
                    dialogueText = "새 대화 내용",
                    emotion = CharacterEmotion.Normal,
                    position = CharacterPosition.Center,
                    choices = new List<DialogueChoice>()
                };
                
                currentStory.dialogueLines.Add(newDialogue);
                
                EditorUtility.SetDirty(storyDatabase);
            }
        }
        
        private void DeleteDialogue(int index)
        {
            if (currentStory != null && index >= 0 && index < currentStory.dialogueLines.Count)
            {
                currentStory.dialogueLines.RemoveAt(index);
                
                if (selectedDialogueIndex == index)
                    selectedDialogueIndex = -1;
                else if (selectedDialogueIndex > index)
                    selectedDialogueIndex--;
                
                EditorUtility.SetDirty(storyDatabase);
            }
        }
        
        private void MoveDialogue(int index, int direction)
        {
            if (currentStory == null || currentStory.dialogueLines == null) return;
            
            int newIndex = index + direction;
            if (newIndex < 0 || newIndex >= currentStory.dialogueLines.Count) return;
            
            var temp = currentStory.dialogueLines[index];
            currentStory.dialogueLines[index] = currentStory.dialogueLines[newIndex];
            currentStory.dialogueLines[newIndex] = temp;
            
            if (selectedDialogueIndex == index)
                selectedDialogueIndex = newIndex;
            else if (selectedDialogueIndex == newIndex)
                selectedDialogueIndex = index;
            
            EditorUtility.SetDirty(storyDatabase);
        }
        
        private void ImportFromCSV()
        {
            if (string.IsNullOrEmpty(csvContent))
            {
                EditorUtility.DisplayDialog("오류", "CSV 내용을 입력하세요.", "확인");
                return;
            }
            
            if (storyDatabase == null)
            {
                CreateStoryDatabase();
            }
            
            storyDatabase.ImportFromCSV(csvContent);
            stories = storyDatabase.GetAllStories();
            
            EditorUtility.SetDirty(storyDatabase);
            AssetDatabase.SaveAssets();
            
            csvContent = "";
            showCSVImport = false;
            
            EditorUtility.DisplayDialog("완료", "CSV 가져오기가 완료되었습니다.", "확인");
        }
        
        private void ExportToCSV()
        {
            if (storyDatabase == null)
            {
                EditorUtility.DisplayDialog("오류", "스토리 데이터베이스가 없습니다.", "확인");
                return;
            }
            
            string csv = storyDatabase.ExportToCSV();
            string path = EditorUtility.SaveFilePanel("CSV 내보내기", "", "stories.csv", "csv");
            
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, csv);
                EditorUtility.DisplayDialog("완료", $"CSV 파일이 저장되었습니다:\\n{path}", "확인");
            }
        }
        
        private void PreviewStory()
        {
            if (currentStory == null)
            {
                EditorUtility.DisplayDialog("오류", "미리보기할 스토리를 선택하세요.", "확인");
                return;
            }
            
            // 스토리 대화 시스템이 씬에 있는지 확인
            StoryDialogueSystem dialogueSystem = FindObjectOfType<StoryDialogueSystem>();
            if (dialogueSystem == null)
            {
                EditorUtility.DisplayDialog("오류", "씬에 StoryDialogueSystem이 없습니다.", "확인");
                return;
            }
            
            // 플레이 모드에서만 미리보기 가능
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("알림", "플레이 모드에서만 미리보기가 가능합니다.", "확인");
                return;
            }
            
            dialogueSystem.StartStory(currentStory.storyId);
        }
    }
}
#endif