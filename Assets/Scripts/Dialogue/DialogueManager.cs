using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GuildMaster.Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        private static DialogueManager instance;
        public static DialogueManager Instance => instance;
        
        [Header("Dialogue System")]
        [SerializeField] private DialogueSystem dialogueSystem;
        [SerializeField] private DialogueLoader dialogueLoader;
        
        [Header("Story Dialogues")]
        [SerializeField] private List<StoryDialogue> storyDialogues = new List<StoryDialogue>();
        
        [Header("Tutorial Dialogues")]
        [SerializeField] private List<TutorialDialogue> tutorialDialogues = new List<TutorialDialogue>();
        
        private HashSet<string> completedDialogues = new HashSet<string>();
        private Queue<int> pendingDialogues = new Queue<int>();
        
        [System.Serializable]
        public class StoryDialogue
        {
            public string dialogueName;
            public string csvFileName;
            public int startID;
            public bool isMainStory;
            public List<string> prerequisites; // Other dialogues that must be completed first
            public bool autoPlay;
            
            public bool CanPlay(HashSet<string> completed)
            {
                return prerequisites.All(p => completed.Contains(p));
            }
        }
        
        [System.Serializable]
        public class TutorialDialogue
        {
            public string tutorialName;
            public string triggerEvent; // Event that triggers this tutorial
            public int dialogueStartID;
            public bool playOnce = true;
            public bool hasPlayed = false;
        }
        
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            if (dialogueSystem == null)
                dialogueSystem = GetComponentInChildren<DialogueSystem>();
            if (dialogueLoader == null)
                dialogueLoader = GetComponentInChildren<DialogueLoader>();
        }
        
        void Start()
        {
            // Subscribe to dialogue events
            if (dialogueSystem != null)
            {
                dialogueSystem.OnAllDialoguesComplete += OnDialogueComplete;
            }
            
            // Subscribe to game events for tutorials
            SubscribeToGameEvents();
        }
        
        void SubscribeToGameEvents()
        {
            // Subscribe to various game events that might trigger tutorials
            var gameManager = Core.GameManager.Instance;
            if (gameManager != null)
            {
                // Example events
                var guildManager = gameManager.GuildManager;
                if (guildManager != null)
                {
                    guildManager.OnBuildingConstructed += (building) => CheckTutorial("FirstBuilding");
                    guildManager.OnAdventurerRecruited += (unit) => CheckTutorial("FirstRecruit");
                    guildManager.OnGuildLevelUp += () => CheckTutorial($"GuildLevel{guildManager.guildLevel}");
                }
                
                var battleManager = gameManager.BattleManager;
                if (battleManager != null)
                {
                    battleManager.OnBattleEnd += (victory) => 
                    {
                        if (victory)
                            CheckTutorial("FirstVictory");
                        else
                            CheckTutorial("FirstDefeat");
                    };
                }
            }
        }
        
        public void PlayStoryDialogue(string dialogueName)
        {
            var story = storyDialogues.FirstOrDefault(s => s.dialogueName == dialogueName);
            if (story == null)
            {
                Debug.LogError($"Story dialogue '{dialogueName}' not found!");
                return;
            }
            
            if (!story.CanPlay(completedDialogues))
            {
                Debug.LogWarning($"Prerequisites not met for dialogue '{dialogueName}'");
                return;
            }
            
            // Load the CSV if specified
            if (!string.IsNullOrEmpty(story.csvFileName))
            {
                dialogueLoader.LoadDialogueFromStreamingAssets(story.csvFileName);
            }
            
            // Set auto-play
            dialogueSystem.SetAutoPlay(story.autoPlay);
            
            // Start the dialogue
            dialogueSystem.StartDialogue(story.startID);
        }
        
        public void LoadAndPlayDialogue(string csvContent, int startID)
        {
            if (dialogueSystem == null)
            {
                Debug.LogError("DialogueSystem not initialized!");
                return;
            }
            
            dialogueSystem.LoadDialogueFromCSV(csvContent);
            dialogueSystem.StartDialogue(startID);
        }
        
        public void PlayDialogueSequence(List<int> dialogueIDs)
        {
            pendingDialogues.Clear();
            foreach (var id in dialogueIDs)
            {
                pendingDialogues.Enqueue(id);
            }
            
            PlayNextInSequence();
        }
        
        void PlayNextInSequence()
        {
            if (pendingDialogues.Count > 0)
            {
                int nextID = pendingDialogues.Dequeue();
                dialogueSystem.StartDialogue(nextID);
            }
        }
        
        void OnDialogueComplete()
        {
            // Check if there are more dialogues in the sequence
            if (pendingDialogues.Count > 0)
            {
                StartCoroutine(DelayedNextDialogue());
            }
        }
        
        IEnumerator DelayedNextDialogue()
        {
            yield return new WaitForSeconds(0.5f); // Brief pause between dialogues
            PlayNextInSequence();
        }
        
        void CheckTutorial(string eventName)
        {
            var tutorial = tutorialDialogues.FirstOrDefault(t => t.triggerEvent == eventName);
            if (tutorial != null && (!tutorial.playOnce || !tutorial.hasPlayed))
            {
                tutorial.hasPlayed = true;
                dialogueSystem.StartDialogue(tutorial.dialogueStartID);
            }
        }
        
        public void MarkDialogueCompleted(string dialogueName)
        {
            completedDialogues.Add(dialogueName);
            SaveDialogueProgress();
        }
        
        public bool IsDialogueCompleted(string dialogueName)
        {
            return completedDialogues.Contains(dialogueName);
        }
        
        public void ResetAllProgress()
        {
            completedDialogues.Clear();
            foreach (var tutorial in tutorialDialogues)
            {
                tutorial.hasPlayed = false;
            }
            SaveDialogueProgress();
        }
        
        void SaveDialogueProgress()
        {
            // Save completed dialogues to PlayerPrefs or save file
            string completed = string.Join(",", completedDialogues);
            PlayerPrefs.SetString("CompletedDialogues", completed);
            
            // Save tutorial states
            for (int i = 0; i < tutorialDialogues.Count; i++)
            {
                PlayerPrefs.SetInt($"Tutorial_{i}_Played", tutorialDialogues[i].hasPlayed ? 1 : 0);
            }
            
            PlayerPrefs.Save();
        }
        
        void LoadDialogueProgress()
        {
            // Load completed dialogues
            string completed = PlayerPrefs.GetString("CompletedDialogues", "");
            if (!string.IsNullOrEmpty(completed))
            {
                completedDialogues = new HashSet<string>(completed.Split(','));
            }
            
            // Load tutorial states
            for (int i = 0; i < tutorialDialogues.Count; i++)
            {
                tutorialDialogues[i].hasPlayed = PlayerPrefs.GetInt($"Tutorial_{i}_Played", 0) == 1;
            }
        }
        
        // Quick dialogue creation for runtime
        public void CreateQuickDialogue(List<QuickDialogueEntry> entries)
        {
            var csvLines = new List<string>();
            csvLines.Add(DialogueCSVFormat.HEADER);
            
            int id = 1000; // Start from 1000 to avoid conflicts
            foreach (var entry in entries)
            {
                csvLines.Add(DialogueCSVFormat.CreateCSVLine(
                    id,
                    entry.speaker,
                    entry.text,
                    entry.position,
                    entry.expression,
                    entry.effect,
                    entry.duration,
                    entry.isLast ? "" : (id + 1).ToString()
                ));
                id++;
            }
            
            string csvContent = string.Join("\n", csvLines);
            LoadAndPlayDialogue(csvContent, 1000);
        }
        
        [System.Serializable]
        public class QuickDialogueEntry
        {
            public string speaker;
            public string text;
            public string position = "Left";
            public string expression = "Normal";
            public string effect = "";
            public float duration = 0f;
            public bool isLast = false;
        }
        
        // Example usage methods
        public void ShowGuildCreationDialogue()
        {
            var entries = new List<QuickDialogueEntry>
            {
                new QuickDialogueEntry 
                { 
                    speaker = "System", 
                    text = "새로운 길드가 창설되었습니다!", 
                    position = "Left", 
                    effect = "FadeIn" 
                },
                new QuickDialogueEntry 
                { 
                    speaker = "길드장", 
                    text = "드디어 우리만의 길드를 만들었구나!", 
                    position = "Left", 
                    expression = "Happy" 
                },
                new QuickDialogueEntry 
                { 
                    speaker = "비서", 
                    text = "축하드립니다! 이제 모험을 시작할 시간입니다.", 
                    position = "Right", 
                    expression = "Normal",
                    isLast = true 
                }
            };
            
            CreateQuickDialogue(entries);
        }
        
        public void ShowBattleVictoryDialogue(string enemyName)
        {
            var entries = new List<QuickDialogueEntry>
            {
                new QuickDialogueEntry 
                { 
                    speaker = "전투 결과", 
                    text = $"{enemyName}과의 전투에서 승리했습니다!", 
                    position = "Left", 
                    effect = "Flash" 
                },
                new QuickDialogueEntry 
                { 
                    speaker = "길드장", 
                    text = "잘했어! 우리 길드의 명성이 높아지고 있어!", 
                    position = "Left", 
                    expression = "Happy",
                    isLast = true 
                }
            };
            
            CreateQuickDialogue(entries);
        }
    }
}