using UnityEngine;
using UnityEditor;

namespace GuildMaster.Editor
{
    /// <summary>
    /// Central hub for all data tools as mentioned in CLAUDE.md
    /// </summary>
    public class GuildMasterToolbar : EditorWindow
    {
        private Vector2 scrollPosition;
        
        [MenuItem("Tools/GuildMaster/Show Toolbar")]
        public static void ShowWindow()
        {
            var window = GetWindow<GuildMasterToolbar>("GuildMaster Toolbar");
            window.minSize = new Vector2(300, 400);
        }
        
        void OnGUI()
        {
            EditorGUILayout.LabelField("GuildMaster Data Tools", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // CSV Management Section
            EditorGUILayout.LabelField("CSV Management", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            if (GUILayout.Button("CSV Data Sync Manager", GUILayout.Height(30)))
            {
                CSVDataSyncManager.ShowWindow();
            }
            EditorGUILayout.LabelField("Bidirectional sync between CSV and ScriptableObjects", EditorStyles.miniLabel);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Quick CSV Sync (Ctrl+Shift+S)", GUILayout.Height(25)))
            {
                CSVDataSyncManager.QuickSync();
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Data Management Section
            EditorGUILayout.LabelField("Data Management", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            if (GUILayout.Button("Data Export Manager", GUILayout.Height(30)))
            {
                DataExportManager.ShowWindow();
            }
            EditorGUILayout.LabelField("Visual character editor with validation", EditorStyles.miniLabel);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Quick Data Export (Ctrl+Shift+E)", GUILayout.Height(25)))
            {
                DataExportManager.QuickExport();
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Sample Data Section
            EditorGUILayout.LabelField("Sample Data Generation", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            if (GUILayout.Button("Generate Sample Data", GUILayout.Height(25)))
            {
                SampleDataGenerator.GenerateSampleCharacterData();
            }
            
            if (GUILayout.Button("Generate Ally Sample Data", GUILayout.Height(25)))
            {
                SampleDataGenerator.GenerateAllySampleData();
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Quick Actions Section
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            if (GUILayout.Button("Open CSV Folder"))
            {
                EditorUtility.RevealInFinder("Assets/CSV");
            }
            
            if (GUILayout.Button("Open Data Folder"))
            {
                EditorUtility.RevealInFinder("Assets/Prefabs/Data");
            }
            
            if (GUILayout.Button("Refresh Asset Database"))
            {
                AssetDatabase.Refresh();
                Debug.Log("Asset database refreshed");
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Info Section
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Keyboard Shortcuts:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• Ctrl+Shift+S - Quick CSV Sync", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• Ctrl+Shift+E - Quick Data Export", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
        }
    }
}