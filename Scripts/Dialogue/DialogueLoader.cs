using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GuildMaster.Dialogue
{
    public class DialogueLoader : MonoBehaviour
    {
        [Header("CSV File Settings")]
        [SerializeField] private string csvFileName = "dialogue.csv";
        [SerializeField] private bool loadFromResources = true;
        [SerializeField] private string customPath = "";
        
        [Header("References")]
        [SerializeField] private DialogueSystem dialogueSystem;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private int testDialogueStartID = 1;
        
        void Start()
        {
            if (dialogueSystem == null)
                dialogueSystem = GetComponent<DialogueSystem>();
                
            if (dialogueSystem == null)
            {
                Debug.LogError("DialogueSystem not found!");
                return;
            }
        }
        
        public void LoadDialogueFromFile()
        {
            string csvContent = "";
            
            if (loadFromResources)
            {
                // Load from Resources folder
                TextAsset csvFile = Resources.Load<TextAsset>($"Dialogues/{csvFileName.Replace(".csv", "")}");
                if (csvFile != null)
                {
                    csvContent = csvFile.text;
                }
                else
                {
                    Debug.LogError($"CSV file not found in Resources/Dialogues/{csvFileName}");
                    return;
                }
            }
            else
            {
                // Load from custom path
                string fullPath = string.IsNullOrEmpty(customPath) ? 
                    Path.Combine(Application.dataPath, "CSV", csvFileName) : 
                    Path.Combine(customPath, csvFileName);
                    
                if (File.Exists(fullPath))
                {
                    csvContent = File.ReadAllText(fullPath, Encoding.UTF8);
                }
                else
                {
                    Debug.LogError($"CSV file not found at: {fullPath}");
                    return;
                }
            }
            
            if (!string.IsNullOrEmpty(csvContent))
            {
                dialogueSystem.LoadDialogueFromCSV(csvContent);
                Debug.Log($"Dialogue loaded from {csvFileName}");
            }
        }
        
        public void LoadDialogueFromStreamingAssets(string fileName)
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, "Dialogues", fileName);
            
            if (filePath.Contains("://") || filePath.Contains(":///"))
            {
                // Web or mobile platform - use UnityWebRequest
                StartCoroutine(LoadDialogueFromWeb(filePath));
            }
            else
            {
                // Desktop platform - direct file access
                if (File.Exists(filePath))
                {
                    string csvContent = File.ReadAllText(filePath, Encoding.UTF8);
                    dialogueSystem.LoadDialogueFromCSV(csvContent);
                }
                else
                {
                    Debug.LogError($"File not found: {filePath}");
                }
            }
        }
        
        IEnumerator LoadDialogueFromWeb(string url)
        {
            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();
                
                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    dialogueSystem.LoadDialogueFromCSV(www.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"Failed to load dialogue: {www.error}");
                }
            }
        }
        
        public void LoadDialogueFromCSVContent(string csvContent)
        {
            if (dialogueSystem != null && !string.IsNullOrEmpty(csvContent))
            {
                dialogueSystem.LoadDialogueFromCSV(csvContent);
            }
        }
        
        // Editor helper method
        public void CreateSampleCSV()
        {
            string sampleCSV = @"ID,CharacterName,DialogueText,Position,Expression,Effect,Duration,NextID,Background,BGM,SFX
1,길드장,""드디어 우리만의 길드를 만들었어!"",Left,Happy,,0,2,GuildHall,Theme01,
2,비서,""축하드립니다, 길드장님! 이제 모험가들을 모집할 시간이에요."",Right,Normal,,0,3,GuildHall,,
3,길드장,""좋아! 최고의 길드를 만들어보자!"",Left,Happy,Shake,2,4,GuildHall,,Cheer
4,,""[튜토리얼 시작]"",,,FadeIn,3,5,GuildHall,,
5,비서,""먼저 훈련소를 건설해야 해요."",Right,Normal,,0,6,GuildHall,,
6,비서,""훈련소에서는 모험가들이 경험치를 더 빨리 얻을 수 있답니다."",Right,Happy,,0,7,GuildHall,,
7,길드장,""알겠어! 바로 건설하자!"",Left,Normal,,0,,GuildHall,,Build";
            
            string path = Path.Combine(Application.dataPath, "Resources", "Dialogues");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            File.WriteAllText(Path.Combine(path, "sample_dialogue.csv"), sampleCSV, Encoding.UTF8);
            Debug.Log($"Sample CSV created at: {path}/sample_dialogue.csv");
            
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        
        // Debug methods
        void OnGUI()
        {
            if (!debugMode) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            
            if (GUILayout.Button("Load Dialogue"))
            {
                LoadDialogueFromFile();
            }
            
            if (GUILayout.Button("Start Test Dialogue"))
            {
                dialogueSystem.StartDialogue(testDialogueStartID);
            }
            
            if (GUILayout.Button("Create Sample CSV"))
            {
                CreateSampleCSV();
            }
            
            GUILayout.EndArea();
        }
    }
    
    // CSV Format Helper
    public static class DialogueCSVFormat
    {
        public const string HEADER = "ID,CharacterName,DialogueText,Position,Expression,Effect,Duration,NextID,Background,BGM,SFX";
        
        public static string CreateCSVLine(
            int id,
            string characterName,
            string dialogueText,
            string position,
            string expression = "Normal",
            string effect = "",
            float duration = 0f,
            string nextID = "",
            string background = "",
            string bgm = "",
            string sfx = "")
        {
            // Escape quotes in dialogue text
            dialogueText = dialogueText.Replace("\"", "\"\"");
            if (dialogueText.Contains(",") || dialogueText.Contains("\"") || dialogueText.Contains("\n"))
            {
                dialogueText = $"\"{dialogueText}\"";
            }
            
            return $"{id},{characterName},{dialogueText},{position},{expression},{effect},{duration},{nextID},{background},{bgm},{sfx}";
        }
        
        public static string CreateDialogueScript(List<DialogueEntry> entries)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(HEADER);
            
            foreach (var entry in entries)
            {
                sb.AppendLine(CreateCSVLine(
                    entry.id,
                    entry.characterName,
                    entry.dialogueText,
                    entry.position,
                    entry.expression,
                    entry.effect,
                    entry.duration,
                    entry.nextID,
                    entry.background,
                    entry.bgm,
                    entry.sfx
                ));
            }
            
            return sb.ToString();
        }
    }
    
    [System.Serializable]
    public class DialogueEntry
    {
        public int id;
        public string characterName;
        public string dialogueText;
        public string position = "Left";
        public string expression = "Normal";
        public string effect = "";
        public float duration = 0f;
        public string nextID = "";
        public string background = "";
        public string bgm = "";
        public string sfx = "";
    }
}