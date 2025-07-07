using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;

#if UNITY_EDITOR
namespace GuildMaster.Dialogue.Editor
{
    public class DialogueEditorWindow : EditorWindow
    {
        private List<DialogueEntry> dialogueEntries = new List<DialogueEntry>();
        private Vector2 scrollPosition;
        private string csvFileName = "new_dialogue.csv";
        private int nextID = 1;
        
        [MenuItem("Tools/Guild Master/Dialogue Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<DialogueEditorWindow>("Dialogue Editor");
            window.minSize = new Vector2(800, 400);
        }
        
        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Left panel - Entry list
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            DrawEntryList();
            EditorGUILayout.EndVertical();
            
            // Right panel - Entry editor
            EditorGUILayout.BeginVertical();
            DrawEntryEditor();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            // Bottom toolbar
            DrawToolbar();
        }
        
        void DrawEntryList()
        {
            EditorGUILayout.LabelField("Dialogue Entries", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Add New Entry"))
            {
                dialogueEntries.Add(new DialogueEntry { id = nextID++ });
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            for (int i = 0; i < dialogueEntries.Count; i++)
            {
                var entry = dialogueEntries[i];
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button($"#{entry.id} {entry.characterName}", GUILayout.Width(150)))
                {
                    GUI.FocusControl(null);
                }
                
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    dialogueEntries.RemoveAt(i);
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        void DrawEntryEditor()
        {
            EditorGUILayout.LabelField("Entry Editor", EditorStyles.boldLabel);
            
            if (dialogueEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("No entries. Click 'Add New Entry' to start.", MessageType.Info);
                return;
            }
            
            // For simplicity, edit the first entry. In a real editor, you'd track the selected entry
            var entry = dialogueEntries[0];
            
            entry.id = EditorGUILayout.IntField("ID", entry.id);
            entry.characterName = EditorGUILayout.TextField("Character Name", entry.characterName);
            
            EditorGUILayout.LabelField("Dialogue Text");
            entry.dialogueText = EditorGUILayout.TextArea(entry.dialogueText, GUILayout.Height(60));
            
            entry.position = EditorGUILayout.TextField("Position (Left/Right)", entry.position);
            entry.expression = EditorGUILayout.TextField("Expression", entry.expression);
            entry.effect = EditorGUILayout.TextField("Effect", entry.effect);
            entry.duration = EditorGUILayout.FloatField("Duration", entry.duration);
            entry.nextID = EditorGUILayout.TextField("Next ID", entry.nextID);
            entry.background = EditorGUILayout.TextField("Background", entry.background);
            entry.bgm = EditorGUILayout.TextField("BGM", entry.bgm);
            entry.sfx = EditorGUILayout.TextField("SFX", entry.sfx);
        }
        
        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            csvFileName = EditorGUILayout.TextField(csvFileName, GUILayout.Width(200));
            
            if (GUILayout.Button("Save CSV", EditorStyles.toolbarButton))
            {
                SaveToCSV();
            }
            
            if (GUILayout.Button("Load CSV", EditorStyles.toolbarButton))
            {
                LoadFromCSV();
            }
            
            if (GUILayout.Button("Clear All", EditorStyles.toolbarButton))
            {
                if (EditorUtility.DisplayDialog("Clear All", "Are you sure you want to clear all entries?", "Yes", "No"))
                {
                    dialogueEntries.Clear();
                    nextID = 1;
                }
            }
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
        }
        
        void SaveToCSV()
        {
            string path = EditorUtility.SaveFilePanel("Save Dialogue CSV", Application.dataPath, csvFileName, "csv");
            
            if (!string.IsNullOrEmpty(path))
            {
                string csvContent = DialogueCSVFormat.CreateDialogueScript(dialogueEntries);
                File.WriteAllText(path, csvContent, Encoding.UTF8);
                
                Debug.Log($"Dialogue saved to: {path}");
                AssetDatabase.Refresh();
            }
        }
        
        void LoadFromCSV()
        {
            string path = EditorUtility.OpenFilePanel("Load Dialogue CSV", Application.dataPath, "csv");
            
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                string csvContent = File.ReadAllText(path, Encoding.UTF8);
                LoadDialogueFromCSV(csvContent);
                
                Debug.Log($"Dialogue loaded from: {path}");
            }
        }
        
        void LoadDialogueFromCSV(string csvContent)
        {
            dialogueEntries.Clear();
            string[] lines = csvContent.Split('\n');
            
            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var entry = ParseCSVLine(lines[i]);
                if (entry != null)
                {
                    dialogueEntries.Add(entry);
                    if (entry.id >= nextID)
                        nextID = entry.id + 1;
                }
            }
        }
        
        DialogueEntry ParseCSVLine(string line)
        {
            try
            {
                string[] values = SplitCSVLine(line);
                if (values.Length < 5) return null;
                
                return new DialogueEntry
                {
                    id = int.Parse(values[0]),
                    characterName = values[1],
                    dialogueText = values[2],
                    position = values[3],
                    expression = values.Length > 4 ? values[4] : "Normal",
                    effect = values.Length > 5 ? values[5] : "",
                    duration = values.Length > 6 ? float.Parse(values[6]) : 0f,
                    nextID = values.Length > 7 ? values[7] : "",
                    background = values.Length > 8 ? values[8] : "",
                    bgm = values.Length > 9 ? values[9] : "",
                    sfx = values.Length > 10 ? values[10] : ""
                };
            }
            catch
            {
                return null;
            }
        }
        
        string[] SplitCSVLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string currentField = "";
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField += '"';
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField);
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }
            
            result.Add(currentField);
            return result.ToArray();
        }
    }
    
    [CustomEditor(typeof(DialogueLoader))]
    public class DialogueLoaderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            DialogueLoader loader = (DialogueLoader)target;
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Load Dialogue"))
            {
                loader.LoadDialogueFromFile();
            }
            
            if (GUILayout.Button("Create Sample CSV"))
            {
                loader.CreateSampleCSV();
            }
            
            if (GUILayout.Button("Open Dialogue Editor"))
            {
                DialogueEditorWindow.ShowWindow();
            }
        }
    }
}
#endif