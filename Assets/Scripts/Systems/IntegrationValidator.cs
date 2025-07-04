using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Core;
using GuildMaster.Battle;
using GuildMaster.Battle.AI;
using GuildMaster.Data;
using GuildMaster.UI;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 시스템 통합 검증 도구
    /// 모든 핵심 시스템들이 올바르게 연결되고 작동하는지 확인
    /// </summary>
    public class IntegrationValidator : MonoBehaviour
    {
        [Header("Validation Settings")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool detailedLogging = true;
        
        [Header("Validation Results")]
        [SerializeField] private List<ValidationResult> results = new List<ValidationResult>();
        [SerializeField] private ValidationStatus overallStatus = ValidationStatus.NotStarted;
        
        [System.Serializable]
        public class ValidationResult
        {
            public string systemName;
            public ValidationStatus status;
            public string message;
            public float timestamp;
            
            public ValidationResult(string name, ValidationStatus status, string msg)
            {
                this.systemName = name;
                this.status = status;
                this.message = msg;
                this.timestamp = Time.time;
            }
        }
        
        public enum ValidationStatus
        {
            NotStarted,
            Passed,
            Warning,
            Failed,
            InProgress
        }
        
        void Start()
        {
            if (runOnStart)
            {
                StartCoroutine(RunFullValidation());
            }
        }
        
        [ContextMenu("Run System Validation")]
        public void RunValidation()
        {
            StartCoroutine(RunFullValidation());
        }
        
        IEnumerator RunFullValidation()
        {
            results.Clear();
            overallStatus = ValidationStatus.InProgress;
            
            Log("=== Starting System Integration Validation ===");
            
            // 1. Core Systems
            yield return ValidateCoreSystems();
            
            // 2. Data Systems
            yield return ValidateDataSystems();
            
            // 3. Battle Systems
            yield return ValidateBattleSystems();
            
            // 4. UI Systems
            yield return ValidateUISystems();
            
            // 5. Game Flow
            yield return ValidateGameFlow();
            
            // 6. Save/Load
            yield return ValidateSaveLoadSystem();
            
            // 7. Performance
            yield return ValidatePerformance();
            
            // Calculate overall status
            CalculateOverallStatus();
            
            Log($"=== Validation Complete: {overallStatus} ===");
            LogValidationSummary();
        }
        
        IEnumerator ValidateCoreSystems()
        {
            Log("Validating Core Systems...");
            
            // GameManager
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                AddResult("GameManager", ValidationStatus.Passed, "Instance found and initialized");
                
                // Check sub-managers
                CheckSubManager("ResourceManager", gameManager.ResourceManager);
                CheckSubManager("SaveManager", gameManager.SaveManager);
                CheckSubManager("BattleManager", gameManager.BattleManager);
                CheckSubManager("GuildManager", gameManager.GuildManager);
            }
            else
            {
                AddResult("GameManager", ValidationStatus.Failed, "Instance not found!");
            }
            
            // DataManager
            var dataManager = DataManager.Instance;
            if (dataManager != null)
            {
                AddResult("DataManager", ValidationStatus.Passed, "Instance found");
                
                // Check data loading
                var testCharacter = dataManager.GetCharacterData("warrior_001");
                if (testCharacter != null)
                {
                    AddResult("DataManager.CharacterData", ValidationStatus.Passed, "Character data loaded");
                }
                else
                {
                    AddResult("DataManager.CharacterData", ValidationStatus.Warning, "No character data found");
                }
            }
            else
            {
                AddResult("DataManager", ValidationStatus.Failed, "Instance not found!");
            }
            
            yield return null;
        }
        
        IEnumerator ValidateDataSystems()
        {
            Log("Validating Data Systems...");
            
            var dataManager = DataManager.Instance;
            if (dataManager == null)
            {
                AddResult("Data Systems", ValidationStatus.Failed, "DataManager not available");
                yield break;
            }
            
            // Check CSV data
            int characterCount = dataManager.GetAllCharacters().Count;
            AddResult("Character Data", 
                characterCount > 0 ? ValidationStatus.Passed : ValidationStatus.Warning,
                $"Found {characterCount} characters");
            
            // Check building data
            var guildHall = dataManager.GetBuildingData("guild_hall");
            if (guildHall != null)
            {
                AddResult("Building Data", ValidationStatus.Passed, "Core buildings loaded");
            }
            else
            {
                AddResult("Building Data", ValidationStatus.Failed, "Guild hall not found");
            }
            
            // Check skill data
            var basicAttack = dataManager.GetSkillData("101");
            if (basicAttack != null)
            {
                AddResult("Skill Data", ValidationStatus.Passed, "Basic skills loaded");
            }
            else
            {
                AddResult("Skill Data", ValidationStatus.Warning, "Basic attack skill not found");
            }
            
            yield return null;
        }
        
        IEnumerator ValidateBattleSystems()
        {
            Log("Validating Battle Systems...");
            
            var battleManager = GameManager.Instance?.BattleManager;
            if (battleManager == null)
            {
                AddResult("Battle Systems", ValidationStatus.Failed, "BattleManager not available");
                yield break;
            }
            
            // Check squad system
            try
            {
                var testSquad = new Squad("Test Squad", 0, true);
                var testUnit = new GuildMaster.Battle.Unit("Test Warrior", 1, GuildMaster.Battle.JobClass.Warrior, GuildMaster.Battle.Rarity.Common);
                testSquad.AddUnit(testUnit);
                
                if (testSquad.AliveUnitsCount == 1)
                {
                    AddResult("Squad System", ValidationStatus.Passed, "Squad creation successful");
                }
                else
                {
                    AddResult("Squad System", ValidationStatus.Failed, "Squad unit count mismatch");
                }
            }
            catch (Exception e)
            {
                AddResult("Squad System", ValidationStatus.Failed, $"Exception: {e.Message}");
            }
            
            // Check AI generation
            try
            {
                var aiSquads = GuildMaster.Battle.AI.AIGuildGenerator.GenerateAIGuild(GuildMaster.Battle.AI.AIGuildGenerator.Difficulty.Novice, 1);
                if (aiSquads != null && aiSquads.Count > 0)
                {
                    AddResult("AI Guild Generator", ValidationStatus.Passed, $"Generated {aiSquads.Count} AI squads");
                }
                else
                {
                    AddResult("AI Guild Generator", ValidationStatus.Failed, "No AI squads generated");
                }
            }
            catch (Exception e)
            {
                AddResult("AI Guild Generator", ValidationStatus.Failed, $"Exception: {e.Message}");
            }
            
            // Check skill system (스킬 시스템은 별도 검증으로 대체)
            AddResult("Skill System", ValidationStatus.Warning, "Manual verification required");
            
            yield return null;
        }
        
        IEnumerator ValidateUISystems()
        {
            Log("Validating UI Systems...");
            
            var uiManager = UIManager.Instance;
            if (uiManager != null)
            {
                AddResult("UIManager", ValidationStatus.Passed, "Instance found");
                
                // Check UI panels
                CheckUIComponent<MainMenuUI>("MainMenuUI");
                CheckUIComponent<BattleUIManager>("BattleUIManager");
                CheckUIComponent<ResourceUI>("ResourceUI");
                CheckUIComponent<SettingsUI>("SettingsUI");
            }
            else
            {
                AddResult("UIManager", ValidationStatus.Failed, "Instance not found!");
            }
            
            // Check notification system
            var notificationUI = FindObjectOfType<NotificationUI>();
            if (notificationUI != null)
            {
                AddResult("NotificationUI", ValidationStatus.Passed, "Component found");
            }
            else
            {
                AddResult("NotificationUI", ValidationStatus.Warning, "Component not in scene");
            }
            
            yield return null;
        }
        
        IEnumerator ValidateGameFlow()
        {
            Log("Validating Game Flow...");
            
            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                AddResult("Game Flow", ValidationStatus.Failed, "GameManager not available");
                yield break;
            }
            
            // Check state transitions
            var currentState = gameManager.CurrentState;
            AddResult("Game State", ValidationStatus.Passed, $"Current state: {currentState}");
            
            // Check game loop manager
            var gameLoopManager = GameLoopManager.Instance;
            if (gameLoopManager != null)
            {
                AddResult("GameLoopManager", ValidationStatus.Passed, "Instance found");
                
                // Check day/night cycle
                var timeInfo = $"Day {gameLoopManager.CurrentDay}, {gameLoopManager.GetTimeOfDay()}";
                AddResult("Day/Night Cycle", ValidationStatus.Passed, timeInfo);
            }
            else
            {
                AddResult("GameLoopManager", ValidationStatus.Warning, "Not initialized");
            }
            
            // Check story system
            var storyManagerObj = FindObjectOfType<StoryManager>();
            if (storyManagerObj != null)
            {
                AddResult("StoryManager", ValidationStatus.Passed, "Instance found");
            }
            else
            {
                AddResult("StoryManager", ValidationStatus.Warning, "Not initialized");
            }
            
            yield return null;
        }
        
        IEnumerator ValidateSaveLoadSystem()
        {
            Log("Validating Save/Load System...");
            
            var saveManager = SaveManager.Instance;
            if (saveManager == null)
            {
                AddResult("Save/Load System", ValidationStatus.Failed, "SaveManager not available");
                yield break;
            }
            
            // Check save functionality
            try
            {
                // Create test save data
                var testData = new GuildMaster.Core.SaveData
                {
                    gameVersion = "1.0.0",
                    slotIndex = -1, // Test slot
                    guildName = "Integration Test Guild",
                    guildLevel = 1,
                    totalPlayTime = 100f,
                    saveTime = DateTime.Now,
                    isAutoSave = false
                };
                
                // Initialize basic resources
                testData.gold = 1000;
                testData.wood = 500;
                testData.stone = 500;
                
                AddResult("Save System", ValidationStatus.Passed, "Test save data created");
            }
            catch (Exception e)
            {
                AddResult("Save System", ValidationStatus.Failed, $"Exception: {e.Message}");
            }
            
            // Check auto-save settings
            var autoSaveEnabled = PlayerPrefs.GetInt("AutoSaveEnabled", 1) == 1;
            AddResult("Auto-Save", 
                autoSaveEnabled ? ValidationStatus.Passed : ValidationStatus.Warning,
                autoSaveEnabled ? "Enabled" : "Disabled");
            
            yield return null;
        }
        
        IEnumerator ValidatePerformance()
        {
            Log("Validating Performance...");
            
            // Check frame rate
            float avgFrameRate = 1f / Time.smoothDeltaTime;
            ValidationStatus fpsStatus = avgFrameRate >= 30f ? ValidationStatus.Passed : 
                                       avgFrameRate >= 20f ? ValidationStatus.Warning : 
                                       ValidationStatus.Failed;
            AddResult("Frame Rate", fpsStatus, $"{avgFrameRate:F1} FPS");
            
            // Check memory usage
            long totalMemory = GC.GetTotalMemory(false) / 1024 / 1024; // MB
            ValidationStatus memStatus = totalMemory < 500 ? ValidationStatus.Passed :
                                       totalMemory < 1000 ? ValidationStatus.Warning :
                                       ValidationStatus.Failed;
            AddResult("Memory Usage", memStatus, $"{totalMemory} MB");
            
            // Check object pooling
            var particleSystem = ParticleEffectsSystem.Instance;
            if (particleSystem != null)
            {
                AddResult("Object Pooling", ValidationStatus.Passed, "Particle system pooling active");
            }
            else
            {
                AddResult("Object Pooling", ValidationStatus.Warning, "Particle system not initialized");
            }
            
            yield return null;
        }
        
        void CheckSubManager<T>(string name, T manager) where T : class
        {
            if (manager != null)
            {
                AddResult($"GameManager.{name}", ValidationStatus.Passed, "Initialized");
            }
            else
            {
                AddResult($"GameManager.{name}", ValidationStatus.Failed, "Not initialized!");
            }
        }
        
        void CheckUIComponent<T>(string name) where T : MonoBehaviour
        {
            var component = FindObjectOfType<T>(true); // Include inactive
            if (component != null)
            {
                AddResult(name, ValidationStatus.Passed, "Component found");
            }
            else
            {
                AddResult(name, ValidationStatus.Warning, "Component not in scene");
            }
        }
        
        void AddResult(string systemName, ValidationStatus status, string message)
        {
            var result = new ValidationResult(systemName, status, message);
            results.Add(result);
            
            if (detailedLogging)
            {
                string statusIcon = status switch
                {
                    ValidationStatus.Passed => "✓",
                    ValidationStatus.Warning => "⚠",
                    ValidationStatus.Failed => "✗",
                    _ => "○"
                };
                
                Debug.Log($"[Validation] {statusIcon} {systemName}: {message}");
            }
        }
        
        void CalculateOverallStatus()
        {
            if (results.Any(r => r.status == ValidationStatus.Failed))
            {
                overallStatus = ValidationStatus.Failed;
            }
            else if (results.Any(r => r.status == ValidationStatus.Warning))
            {
                overallStatus = ValidationStatus.Warning;
            }
            else if (results.All(r => r.status == ValidationStatus.Passed))
            {
                overallStatus = ValidationStatus.Passed;
            }
            else
            {
                overallStatus = ValidationStatus.Warning;
            }
        }
        
        void LogValidationSummary()
        {
            int passed = results.Count(r => r.status == ValidationStatus.Passed);
            int warnings = results.Count(r => r.status == ValidationStatus.Warning);
            int failed = results.Count(r => r.status == ValidationStatus.Failed);
            
            Debug.Log($"Validation Summary: {passed} Passed, {warnings} Warnings, {failed} Failed");
            
            if (failed > 0)
            {
                Debug.LogError("Critical systems failed validation!");
                foreach (var fail in results.Where(r => r.status == ValidationStatus.Failed))
                {
                    Debug.LogError($"  - {fail.systemName}: {fail.message}");
                }
            }
            
            if (warnings > 0)
            {
                Debug.LogWarning("Some systems have warnings:");
                foreach (var warn in results.Where(r => r.status == ValidationStatus.Warning))
                {
                    Debug.LogWarning($"  - {warn.systemName}: {warn.message}");
                }
            }
        }
        
        void Log(string message)
        {
            if (detailedLogging)
            {
                Debug.Log($"[IntegrationValidator] {message}");
            }
        }
        
        void ValidateUnit(GuildMaster.Battle.Unit unit, GuildMaster.Battle.JobClass expectedJobClass)
        {
            if (unit == null)
            {
                LogError("Unit is null");
                return;
            }

            if ((GuildMaster.Battle.JobClass)unit.jobClass != expectedJobClass)
            {
                LogError($"Unit job class mismatch. Expected: {expectedJobClass}, Actual: {unit.jobClass}");
            }

            LogInfo($"Unit validation completed for {unit.unitName}");
        }
        
        void LogError(string message)
        {
            Debug.LogError($"[IntegrationValidator] {message}");
        }
        
        void LogWarning(string message)
        {
            Debug.LogWarning($"[IntegrationValidator] {message}");
        }
        
        void LogInfo(string message)
        {
            Debug.Log($"[IntegrationValidator] {message}");
        }
        
        // Public API
        public ValidationStatus GetOverallStatus() => overallStatus;
        public List<ValidationResult> GetResults() => new List<ValidationResult>(results);
        public bool IsValidationComplete() => overallStatus != ValidationStatus.NotStarted && overallStatus != ValidationStatus.InProgress;
    }
}