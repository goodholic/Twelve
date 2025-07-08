using UnityEngine;
using UnityEditor;
using GuildMaster.Battle;
using GuildMaster.Game;
using GuildMaster.Data;
using System.IO;

namespace GuildMaster.Editor
{
    /// <summary>
    /// JobLevelData 생성 및 관리를 위한 에디터 도구
    /// </summary>
    public class JobLevelDataEditor : EditorWindow
    {
        private JobLevelData currentData;
        private Vector2 scrollPosition;
        
        [MenuItem("Tools/Twelve/Job Level Data Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<JobLevelDataEditor>("Job Level Data Manager");
            window.minSize = new Vector2(400, 300);
        }
        
        [MenuItem("Assets/Create/Twelve/Job Level Data", priority = 0)]
        public static void CreateJobLevelData()
        {
            string path = "Assets/Resources";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(path + "/JobLevelData.asset");
            JobLevelData asset = ScriptableObject.CreateInstance<JobLevelData>();
            
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            
            Debug.Log($"Created JobLevelData at: {assetPath}");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Job Level Data Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 현재 데이터 선택
            currentData = (JobLevelData)EditorGUILayout.ObjectField("Job Level Data", currentData, typeof(JobLevelData), false);
            
            if (currentData == null)
            {
                EditorGUILayout.HelpBox("Please select or create a JobLevelData asset.", MessageType.Info);
                
                if (GUILayout.Button("Create New JobLevelData"))
                {
                    CreateJobLevelData();
                }
                return;
            }
            
            EditorGUILayout.Space();
            
            // 직업별 레벨 데이터 표시
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField("Job Levels", EditorStyles.boldLabel);
            
            if (currentData.jobLevels != null)
            {
                for (int i = 0; i < currentData.jobLevels.Length; i++)
                {
                    var jobLevel = currentData.jobLevels[i];
                    if (jobLevel == null) continue;
                    
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    
                    EditorGUILayout.LabelField($"{jobLevel.jobClass}", EditorStyles.boldLabel);
                    
                    EditorGUI.BeginChangeCheck();
                    
                    jobLevel.currentLevel = EditorGUILayout.IntSlider("Current Level", jobLevel.currentLevel, 1, jobLevel.maxLevel);
                    jobLevel.maxLevel = EditorGUILayout.IntField("Max Level", jobLevel.maxLevel);
                    jobLevel.levelUpCost = EditorGUILayout.FloatField("Base Level Up Cost", jobLevel.levelUpCost);
                    jobLevel.autoLevelUpEnabled = EditorGUILayout.Toggle("Auto Level Up", jobLevel.autoLevelUpEnabled);
                    jobLevel.accumulatedGold = EditorGUILayout.FloatField("Accumulated Gold", jobLevel.accumulatedGold);
                    
                    float nextCost = jobLevel.GetLevelUpCost();
                    EditorGUILayout.LabelField($"Next Level Cost: {nextCost:F2}");
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(currentData);
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            
            // 유틸리티 버튼들
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Reset All Levels"))
            {
                if (EditorUtility.DisplayDialog("Reset All Levels", "Are you sure you want to reset all job levels to 1?", "Yes", "No"))
                {
                    foreach (var jobLevel in currentData.jobLevels)
                    {
                        if (jobLevel != null)
                        {
                            jobLevel.currentLevel = 1;
                            jobLevel.accumulatedGold = 0f;
                            jobLevel.autoLevelUpEnabled = false;
                        }
                    }
                    EditorUtility.SetDirty(currentData);
                }
            }
            
            if (GUILayout.Button("Set All Max Level"))
            {
                if (EditorUtility.DisplayDialog("Set All Max Level", "Set all jobs to max level?", "Yes", "No"))
                {
                    foreach (var jobLevel in currentData.jobLevels)
                    {
                        if (jobLevel != null)
                        {
                            jobLevel.currentLevel = jobLevel.maxLevel;
                        }
                    }
                    EditorUtility.SetDirty(currentData);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Save"))
            {
                AssetDatabase.SaveAssets();
                Debug.Log("JobLevelData saved!");
            }
        }
    }
    
    // Custom Property Drawer for JobLevel
    [CustomPropertyDrawer(typeof(JobLevelData.JobLevel))]
    public class JobLevelPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Calculate rects
            var jobClassRect = new Rect(position.x, position.y, position.width * 0.3f, position.height);
            var levelRect = new Rect(position.x + position.width * 0.35f, position.y, position.width * 0.2f, position.height);
            var autoRect = new Rect(position.x + position.width * 0.6f, position.y, position.width * 0.4f, position.height);
            
            // Draw fields
            var jobClassProp = property.FindPropertyRelative("jobClass");
            var levelProp = property.FindPropertyRelative("currentLevel");
            var autoProp = property.FindPropertyRelative("autoLevelUpEnabled");
            
            EditorGUI.PropertyField(jobClassRect, jobClassProp, GUIContent.none);
            EditorGUI.PropertyField(levelRect, levelProp, GUIContent.none);
            
            var autoLabel = new GUIContent("Auto");
            EditorGUI.PropertyField(autoRect, autoProp, autoLabel);
            
            EditorGUI.EndProperty();
        }
    }
}