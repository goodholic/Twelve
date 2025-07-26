using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle;
using TacticalTileGame.Data;
using GuildMaster.Data;
using System;
using System.Text;
using System.Threading.Tasks;

namespace GuildMaster.Editor
{
    /// <summary>
    /// CSV Data Sync Manager for bidirectional synchronization between CSV files and ScriptableObjects
    /// As described in CLAUDE.md
    /// </summary>
    public class CSVDataSyncManager : EditorWindow
    {
        private const string CSV_FOLDER_PATH = "Assets/CSV";
        private const string SO_FOLDER_PATH = "Assets/Prefabs/Data";
        private const string BACKUP_FOLDER_PATH = "Assets/CSV/Backups";
        
        private Vector2 scrollPosition;
        private bool autoSyncEnabled = false;
        private FileSystemWatcher csvWatcher;
        private bool isProcessing = false;
        private string currentOperation = "";
        private List<string> operationLog = new List<string>();
        
        // Search and filter
        private string searchFilter = "";
        private CharacterRarity? rarityFilter = null;
        private JobClass? jobClassFilter = null;
        
        // Preview mode
        private bool showPreview = false;
        private List<CSVCharacter> previewCharacters = new List<CSVCharacter>();
        
        // Data validation
        private bool validateBeforeSync = true;
        private List<string> validationErrors = new List<string>();
        
        // Backup
        private bool createBackupBeforeSync = true;
        private string lastBackupPath = "";

        [MenuItem("Tools/GuildMaster/CSV Data Sync Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<CSVDataSyncManager>("CSV Data Sync Manager");
            window.minSize = new Vector2(600, 400);
            window.InitializeFileWatcher();
        }

        // Keyboard shortcut implementation
        [MenuItem("Tools/GuildMaster/Quick CSV Sync %#s")] // Ctrl+Shift+S
        public static void QuickSync()
        {
            var window = GetWindow<CSVDataSyncManager>();
            window.PerformBidirectionalSync();
        }

        void OnEnable()
        {
            InitializeFileWatcher();
        }

        void OnDisable()
        {
            csvWatcher?.Dispose();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("CSV Data Sync Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Auto Sync Mode
            EditorGUILayout.BeginHorizontal();
            autoSyncEnabled = EditorGUILayout.Toggle("Auto Sync Mode", autoSyncEnabled);
            EditorGUILayout.EndHorizontal();
            
            // Options
            EditorGUILayout.BeginHorizontal();
            validateBeforeSync = EditorGUILayout.Toggle("Validate Data", validateBeforeSync);
            createBackupBeforeSync = EditorGUILayout.Toggle("Create Backup", createBackupBeforeSync);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Main Actions
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import from CSV", GUILayout.Height(30)))
            {
                ImportFromCSV();
            }
            if (GUILayout.Button("Export to CSV", GUILayout.Height(30)))
            {
                ExportToCSV();
            }
            if (GUILayout.Button("Bidirectional Sync", GUILayout.Height(30)))
            {
                PerformBidirectionalSync();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            
            EditorGUILayout.Space();
            
            // Search and Filter
            EditorGUILayout.LabelField("Search & Filter", EditorStyles.boldLabel);
            searchFilter = EditorGUILayout.TextField("Search", searchFilter);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Rarity Filter:", GUILayout.Width(80));
            if (GUILayout.Button(rarityFilter?.ToString() ?? "All"))
            {
                ShowRarityFilterMenu();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Job Class:", GUILayout.Width(80));
            if (GUILayout.Button(jobClassFilter?.ToString() ?? "All"))
            {
                ShowJobClassFilterMenu();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Preview Mode
            showPreview = EditorGUILayout.Toggle("Preview Mode", showPreview);
            if (showPreview)
            {
                DrawPreview();
            }
            
            // Operation Log
            if (operationLog.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Operation Log", EditorStyles.boldLabel);
                foreach (var log in operationLog.TakeLast(10))
                {
                    EditorGUILayout.LabelField(log, EditorStyles.miniLabel);
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            // Progress
            if (isProcessing)
            {
                EditorGUI.ProgressBar(GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true)), 0.5f, currentOperation);
            }
        }

        private void InitializeFileWatcher()
        {
            if (!Directory.Exists(CSV_FOLDER_PATH))
            {
                Directory.CreateDirectory(CSV_FOLDER_PATH);
            }
            
            csvWatcher = new FileSystemWatcher(CSV_FOLDER_PATH)
            {
                Filter = "*.csv",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
            
            csvWatcher.Changed += OnFileChanged;
            csvWatcher.Created += OnFileChanged;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (autoSyncEnabled)
            {
                EditorApplication.delayCall += () => {
                    LogOperation($"Auto-sync triggered for: {e.Name}");
                    ImportFromCSV();
                };
            }
        }

        private async void ImportFromCSV()
        {
            if (createBackupBeforeSync)
            {
                CreateBackup();
            }
            
            isProcessing = true;
            currentOperation = "Importing from CSV...";
            operationLog.Clear();
            validationErrors.Clear();
            
            try
            {
                // Import character data
                string csvPath = Path.Combine(CSV_FOLDER_PATH, "character_data.csv");
                if (File.Exists(csvPath))
                {
                    await Task.Run(() => ImportCharacterData(csvPath));
                }
                
                AssetDatabase.Refresh();
                LogOperation("Import completed successfully!");
                
                if (validationErrors.Count > 0)
                {
                    EditorUtility.DisplayDialog("Import Complete with Warnings", 
                        $"Import completed but found {validationErrors.Count} validation issues.\nCheck the console for details.", "OK");
                }
            }
            catch (Exception e)
            {
                LogOperation($"Error during import: {e.Message}");
                Debug.LogError(e);
            }
            finally
            {
                isProcessing = false;
                currentOperation = "";
            }
        }

        private void ExportToCSV()
        {
            isProcessing = true;
            currentOperation = "Exporting to CSV...";
            operationLog.Clear();
            
            try
            {
                // CharacterDatabase 내보내기 기능 제거됨 (TacticalCharacterData는 유지)
                LogOperation("Character Database export feature has been removed");
            }
            catch (Exception e)
            {
                LogOperation($"Error during export: {e.Message}");
                Debug.LogError(e);
            }
            finally
            {
                isProcessing = false;
                currentOperation = "";
            }
        }

        private void PerformBidirectionalSync()
        {
            // Smart sync based on file modification times
            LogOperation("Starting bidirectional sync...");
            
            string csvPath = Path.Combine(CSV_FOLDER_PATH, "character_data.csv");
            // CharacterDatabase 생성 기능 제거됨 - 통합 데이터베이스 사용
            string soPath = ""; // 사용되지 않음
            
            if (File.Exists(csvPath) && File.Exists(soPath))
            {
                DateTime csvTime = File.GetLastWriteTime(csvPath);
                DateTime soTime = File.GetLastWriteTime(soPath);
                
                if (csvTime > soTime)
                {
                    LogOperation("CSV is newer, importing...");
                    ImportFromCSV();
                }
                else if (soTime > csvTime)
                {
                    LogOperation("ScriptableObject is newer, exporting...");
                    ExportToCSV();
                }
                else
                {
                    LogOperation("Already in sync");
                }
            }
        }

        private void ImportCharacterData(string csvPath)
        {
            // CharacterDatabase 가져오기 기능 제거됨 (TacticalCharacterData는 유지)
            LogOperation("Character Database import feature has been removed");
            return;
        }

        private void ExportCharacterData()
        {
            // CharacterDatabase 내보내기 기능 제거됨 (TacticalCharacterData는 유지)
            LogOperation("ExportCharacterData method is no longer supported");
            return;
        }

        private bool ApplyFilters(CSVCharacter character)
        {
            if (!string.IsNullOrEmpty(searchFilter) && 
                !character.name.ToLower().Contains(searchFilter.ToLower()))
                return false;
                
            if (rarityFilter.HasValue && character.rarity != rarityFilter.Value)
                return false;
                
            if (jobClassFilter.HasValue && character.jobClass != jobClassFilter.Value)
                return false;
                
            return true;
        }

        private void DrawPreview()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Preview (First 10 characters)", EditorStyles.boldLabel);
            
            if (previewCharacters.Count == 0)
            {
                LoadPreviewData();
            }
            
            foreach (var character in previewCharacters.Take(10))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(character.name, GUILayout.Width(150));
                EditorGUILayout.LabelField($"Lv.{character.level}", GUILayout.Width(50));
                EditorGUILayout.LabelField(character.jobClass.ToString(), GUILayout.Width(80));
                EditorGUILayout.LabelField(character.rarity.ToString(), GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void LoadPreviewData()
        {
            previewCharacters.Clear();
            string csvPath = Path.Combine(CSV_FOLDER_PATH, "character_data.csv");
            
            if (File.Exists(csvPath))
            {
                var lines = File.ReadAllLines(csvPath);
                for (int i = 1; i < lines.Length && i <= 10; i++)
                {
                    var values = ParseCSVLine(lines[i]);
                    if (values.Length >= 19)
                    {
                        previewCharacters.Add(new CSVCharacter
                        {
                            id = values[0],
                            characterID = values[0],
                            name = values[1],
                            characterName = values[1],
                            level = int.Parse(values[3]),
                            jobClass = ParseJobClass(values[2]),
                            rarity = ParseCharacterRarity(values[4])
                        });
                    }
                }
            }
        }

        private void ShowRarityFilterMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("All"), rarityFilter == null, () => rarityFilter = null);
            foreach (CharacterRarity rarity in Enum.GetValues(typeof(CharacterRarity)))
            {
                CharacterRarity r = rarity;
                menu.AddItem(new GUIContent(r.ToString()), rarityFilter == r, () => rarityFilter = r);
            }
            menu.ShowAsContext();
        }

        private void ShowJobClassFilterMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("All"), jobClassFilter == null, () => jobClassFilter = null);
            foreach (JobClass job in Enum.GetValues(typeof(JobClass)))
            {
                JobClass j = job;
                menu.AddItem(new GUIContent(j.ToString()), jobClassFilter == j, () => jobClassFilter = j);
            }
            menu.ShowAsContext();
        }

        private string[] ParseCSVLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string currentField = "";
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
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

        private JobClass ParseJobClass(string className)
        {
            return className switch
            {
                "Warrior" => JobClass.Warrior,
                "Knight" or "Paladin" => JobClass.Knight,
                "Wizard" or "Mage" => JobClass.Mage,
                "Priest" => JobClass.Priest,
                "Assassin" or "Rogue" => JobClass.Rogue,
                "Ranger" or "Archer" => JobClass.Archer,
                _ => JobClass.Warrior
            };
        }

        private CharacterRarity ParseCharacterRarity(string rarity)
        {
            return rarity switch
            {
                "Common" => CharacterRarity.Common,
                "Uncommon" => CharacterRarity.Uncommon,
                "Rare" => CharacterRarity.Rare,
                "Epic" => CharacterRarity.Epic,
                "Legendary" => CharacterRarity.Legendary,
                _ => CharacterRarity.Common
            };
        }

        private void LogOperation(string message)
        {
            operationLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            Debug.Log($"[CSV Sync] {message}");
        }
        
        private void ValidateCSVCharacterData(CSVCharacter character)
        {
            if (string.IsNullOrEmpty(character.id))
            {
                validationErrors.Add($"Character '{character.name}' has no ID");
            }
            
            if (string.IsNullOrEmpty(character.name))
            {
                validationErrors.Add($"Character with ID '{character.id}' has no name");
            }
            
            if (character.baseHP <= 0)
            {
                validationErrors.Add($"Character '{character.name}' has invalid HP: {character.baseHP}");
            }
            
            if (character.baseAttack < 0)
            {
                validationErrors.Add($"Character '{character.name}' has negative attack: {character.baseAttack}");
            }
            
            if (character.critRate < 0 || character.critRate > 1)
            {
                validationErrors.Add($"Character '{character.name}' has invalid crit rate: {character.critRate}");
            }
            
            if (character.accuracy < 0 || character.accuracy > 1)
            {
                validationErrors.Add($"Character '{character.name}' has invalid accuracy: {character.accuracy}");
            }
        }
        
        private void ValidateAllData()
        {
            validationErrors.Clear();
            LogOperation("Starting data validation...");
            
            // CharacterDatabase 검증 기능 제거됨 (TacticalCharacterData는 유지)
            LogOperation("Character Database validation feature has been removed");
            EditorUtility.DisplayDialog("Validation", "Character Database validation feature has been removed", "OK");
            // 주석 처리된 검증 코드 제거됨
        }
        
        private void CreateBackup()
        {
            LogOperation("Creating backup...");
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFolder = Path.Combine(BACKUP_FOLDER_PATH, timestamp);
            
            try
            {
                // Create backup directory
                if (!Directory.Exists(BACKUP_FOLDER_PATH))
                {
                    Directory.CreateDirectory(BACKUP_FOLDER_PATH);
                }
                Directory.CreateDirectory(backupFolder);
                
                // Backup CSV files
                string[] csvFiles = Directory.GetFiles(CSV_FOLDER_PATH, "*.csv");
                foreach (string file in csvFiles)
                {
                    string fileName = Path.GetFileName(file);
                    string destPath = Path.Combine(backupFolder, fileName);
                    File.Copy(file, destPath);
                }
                
                lastBackupPath = backupFolder;
                LogOperation($"Backup created at: {backupFolder}");
                
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                LogOperation($"Backup failed: {e.Message}");
                Debug.LogError(e);
            }
        }
        
        private void RestoreFromBackup()
        {
            string backupPath = EditorUtility.OpenFolderPanel("Select Backup Folder", BACKUP_FOLDER_PATH, "");
            
            if (string.IsNullOrEmpty(backupPath))
                return;
                
            if (EditorUtility.DisplayDialog("Restore Backup", 
                $"This will replace current CSV files with backup.\nBackup path: {backupPath}\nContinue?", "Yes", "No"))
            {
                try
                {
                    string[] backupFiles = Directory.GetFiles(backupPath, "*.csv");
                    foreach (string file in backupFiles)
                    {
                        string fileName = Path.GetFileName(file);
                        string destPath = Path.Combine(CSV_FOLDER_PATH, fileName);
                        File.Copy(file, destPath, true);
                    }
                    
                    AssetDatabase.Refresh();
                    LogOperation($"Restored from backup: {backupPath}");
                    EditorUtility.DisplayDialog("Restore Complete", "Backup restored successfully!", "OK");
                }
                catch (Exception e)
                {
                    LogOperation($"Restore failed: {e.Message}");
                    Debug.LogError(e);
                    EditorUtility.DisplayDialog("Restore Failed", $"Failed to restore backup: {e.Message}", "OK");
                }
            }
        }
    }
}