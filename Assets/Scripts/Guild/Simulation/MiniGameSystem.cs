using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Core;
using GuildMaster.Data;
using GuildMaster.Battle;
using Random = UnityEngine.Random;
// using ResourceType = GuildMaster.Data.ResourceType; // ResourceType removed

namespace GuildMaster.Systems
{
    public class MiniGameSystem : MonoBehaviour
    {
        private static MiniGameSystem _instance;
        public static MiniGameSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MiniGameSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("MiniGameSystem");
                        _instance = go.AddComponent<MiniGameSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #region Enums and Classes

        public enum MiniGameType
        {
            ResourceGathering,
            CombatTraining,
            Negotiation,
            PuzzleSolving,
            TimingBased
        }

        public enum EventType
        {
            Seasonal,
            Random,
            GuildMilestone,
            CharacterSpecific
        }

        public enum SeasonalEventType
        {
            SpringFestival,
            SummerTournament,
            HarvestFestival,
            WinterCelebration
        }

        public enum RandomEventType
        {
            MerchantVisit,
            MonsterRaid,
            TravelingCircus,
            MysteriousStranger,
            BanditsAttack,
            NoblemenVisit,
            DisasterRelief,
            TreasureDiscovery
        }

        public enum EventDifficulty
        {
            Easy,
            Normal,
            Hard,
            Expert,
            Legendary
        }

        [Serializable]
        public class MiniGame
        {
            public string id;
            public string name;
            public string description;
            public MiniGameType type;
            public EventDifficulty difficulty;
            public float duration;
            public bool isActive;
            public float startTime;
            public Dictionary<string, object> gameData;
            public MiniGameReward rewards;
            public int maxAttempts = 3;
            public int currentAttempts = 0;
            public float successRate = 0f;
            
            public MiniGame()
            {
                gameData = new Dictionary<string, object>();
                rewards = new MiniGameReward();
            }
        }

        [Serializable]
        public class SpecialEvent
        {
            public string id;
            public string name;
            public string description;
            public EventType type;
            public EventDifficulty difficulty;
            public float startTime;
            public float duration;
            public bool isActive;
            public bool isCompleted;
            public List<MiniGame> associatedMiniGames;
            public EventRewards rewards;
            public EventRequirements requirements;
            public Dictionary<string, object> eventData;
            public int participantCount;
            public float progress;
            
            public SpecialEvent()
            {
                associatedMiniGames = new List<MiniGame>();
                rewards = new EventRewards();
                requirements = new EventRequirements();
                eventData = new Dictionary<string, object>();
            }
        }

        [Serializable]
        public class MiniGameReward
        {
            public int goldReward;
            public int woodReward;
            public int stoneReward;
            public int manaStoneReward;
            public int reputationReward;
            public int experienceReward;
            public List<string> itemRewards;
            public float successMultiplier = 1f;
            
            public MiniGameReward()
            {
                itemRewards = new List<string>();
            }
        }

        [Serializable]
        public class EventRewards
        {
            public MiniGameReward baseRewards;
            public MiniGameReward completionBonus;
            public MiniGameReward participationReward;
            public Dictionary<string, int> milestoneRewards;
            
            public EventRewards()
            {
                baseRewards = new MiniGameReward();
                completionBonus = new MiniGameReward();
                participationReward = new MiniGameReward();
                milestoneRewards = new Dictionary<string, int>();
            }
        }

        [Serializable]
        public class EventRequirements
        {
            public int minGuildLevel = 1;
            public int minAdventurerCount = 0;
            public int minReputation = 0;
            public List<string> requiredBuildings;
            public List<string> requiredCharacters;
            
            public EventRequirements()
            {
                requiredBuildings = new List<string>();
                requiredCharacters = new List<string>();
            }
        }

        [Serializable]
        public class MiniGameResult
        {
            public string miniGameId;
            public bool success;
            public float score;
            public float completionTime;
            public MiniGameReward earnedRewards;
            public Dictionary<string, object> resultData;
            
            public MiniGameResult()
            {
                resultData = new Dictionary<string, object>();
                earnedRewards = new MiniGameReward();
            }
        }

        [Serializable]
        public class EventSchedule
        {
            public string eventId;
            public float scheduledTime;
            public EventType eventType;
            public bool isRecurring;
            public float recurringInterval;
            public int maxOccurrences;
            public int currentOccurrences;
        }

        #endregion

        #region Properties and Fields

        [Header("Settings")]
        [SerializeField] private float eventCheckInterval = 60f; // Check for events every minute
        [SerializeField] private int maxConcurrentEvents = 3;
        [SerializeField] private int maxConcurrentMiniGames = 5;
        [SerializeField] private float difficultyScalingFactor = 0.1f;
        
        [Header("Event Configuration")]
        [SerializeField] private float seasonalEventDuration = 604800f; // 7 days
        [SerializeField] private float randomEventDuration = 86400f; // 1 day
        [SerializeField] private float milestoneEventDuration = 172800f; // 2 days
        
        // Active content
        private List<SpecialEvent> activeEvents = new List<SpecialEvent>();
        private List<MiniGame> activeMiniGames = new List<MiniGame>();
        private Queue<EventSchedule> scheduledEvents = new Queue<EventSchedule>();
        
        // Event history
        private List<SpecialEvent> completedEvents = new List<SpecialEvent>();
        private Dictionary<string, MiniGameResult> miniGameHistory = new Dictionary<string, MiniGameResult>();
        
        // Seasonal tracking
        private SeasonalEventType currentSeason;
        private float lastSeasonChange;
        private float seasonDuration = 2592000f; // 30 days per season
        
        // Player progress tracking
        private int playerGuildLevel = 1;
        private float totalEventParticipation = 0f;
        private Dictionary<MiniGameType, int> miniGameCompletions = new Dictionary<MiniGameType, int>();
        
        // Events
        public event Action<SpecialEvent> OnEventStarted;
        public event Action<SpecialEvent> OnEventCompleted;
        public event Action<MiniGame> OnMiniGameStarted;
        public event Action<MiniGameResult> OnMiniGameCompleted;
        public event Action<SeasonalEventType> OnSeasonChanged;
        
        #endregion

        #region Unity Lifecycle

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            Initialize();
        }

        void Start()
        {
            StartCoroutine(EventManagementCoroutine());
            StartCoroutine(SeasonalUpdateCoroutine());
        }

        void Initialize()
        {
            // Initialize mini-game completion tracking
            foreach (MiniGameType type in Enum.GetValues(typeof(MiniGameType)))
            {
                miniGameCompletions[type] = 0;
            }
            
            // Set initial season
            currentSeason = GetCurrentSeason();
            lastSeasonChange = Time.time;
            
            // Schedule initial events
            ScheduleInitialEvents();
        }

        #endregion

        #region Event Management

        IEnumerator EventManagementCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(eventCheckInterval);
                
                // Check scheduled events
                ProcessScheduledEvents();
                
                // Update active events
                UpdateActiveEvents();
                
                // Generate random events
                if (ShouldGenerateRandomEvent())
                {
                    GenerateRandomEvent();
                }
                
                // Check for milestone events
                CheckMilestoneEvents();
            }
        }

        IEnumerator SeasonalUpdateCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(3600f); // Check every hour
                
                if (Time.time - lastSeasonChange >= seasonDuration)
                {
                    ChangeSeason();
                }
            }
        }

        void ProcessScheduledEvents()
        {
            while (scheduledEvents.Count > 0 && scheduledEvents.Peek().scheduledTime <= Time.time)
            {
                var schedule = scheduledEvents.Dequeue();
                StartScheduledEvent(schedule);
                
                // Re-schedule if recurring
                if (schedule.isRecurring && schedule.currentOccurrences < schedule.maxOccurrences)
                {
                    schedule.scheduledTime = Time.time + schedule.recurringInterval;
                    schedule.currentOccurrences++;
                    scheduledEvents.Enqueue(schedule);
                }
            }
        }

        void UpdateActiveEvents()
        {
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                var activeEvent = activeEvents[i];
                
                if (Time.time - activeEvent.startTime >= activeEvent.duration)
                {
                    CompleteEvent(activeEvent);
                    activeEvents.RemoveAt(i);
                }
                else
                {
                    // Update event progress
                    UpdateEventProgress(activeEvent);
                }
            }
        }

        bool ShouldGenerateRandomEvent()
        {
            if (activeEvents.Count >= maxConcurrentEvents) return false;
            
            // Base chance influenced by guild level and recent activity
            float baseChance = 0.1f;
            float levelBonus = playerGuildLevel * 0.02f;
            float activityBonus = Mathf.Min(totalEventParticipation * 0.001f, 0.2f);
            
            float totalChance = baseChance + levelBonus + activityBonus;
            return Random.value < totalChance;
        }

        void GenerateRandomEvent()
        {
            var randomType = (RandomEventType)Random.Range(0, Enum.GetValues(typeof(RandomEventType)).Length);
            var newEvent = CreateRandomEvent(randomType);
            
            if (newEvent != null && CanStartEvent(newEvent))
            {
                StartEvent(newEvent);
            }
        }

        void CheckMilestoneEvents()
        {
            // Check guild level milestones
            if (playerGuildLevel % 5 == 0 && !HasRecentMilestoneEvent())
            {
                var milestoneEvent = CreateMilestoneEvent($"Guild Level {playerGuildLevel} Celebration");
                if (milestoneEvent != null && CanStartEvent(milestoneEvent))
                {
                    StartEvent(milestoneEvent);
                }
            }
            
            // Check other milestones (adventurer count, total wealth, etc.)
            CheckAdventurerMilestones();
            CheckWealthMilestones();
            CheckReputationMilestones();
        }

        #endregion

        #region Event Creation

        SpecialEvent CreateSeasonalEvent(SeasonalEventType seasonType)
        {
            var newEvent = new SpecialEvent
            {
                id = Guid.NewGuid().ToString(),
                type = EventType.Seasonal,
                startTime = Time.time,
                duration = seasonalEventDuration,
                isActive = true
            };

            switch (seasonType)
            {
                case SeasonalEventType.SpringFestival:
                    newEvent.name = "Spring Festival";
                    newEvent.description = "Celebrate the arrival of spring with special activities and bonuses!";
                    newEvent.difficulty = EventDifficulty.Normal;
                    AddSpringFestivalMiniGames(newEvent);
                    break;
                    
                case SeasonalEventType.SummerTournament:
                    newEvent.name = "Summer Tournament";
                    newEvent.description = "Test your guild's strength in the grand summer tournament!";
                    newEvent.difficulty = EventDifficulty.Hard;
                    AddSummerTournamentMiniGames(newEvent);
                    break;
                    
                case SeasonalEventType.HarvestFestival:
                    newEvent.name = "Harvest Festival";
                    newEvent.description = "Gather resources and celebrate the bountiful harvest!";
                    newEvent.difficulty = EventDifficulty.Normal;
                    AddHarvestFestivalMiniGames(newEvent);
                    break;
                    
                case SeasonalEventType.WinterCelebration:
                    newEvent.name = "Winter Celebration";
                    newEvent.description = "Warm hearts during the cold winter with festivities and gifts!";
                    newEvent.difficulty = EventDifficulty.Easy;
                    AddWinterCelebrationMiniGames(newEvent);
                    break;
            }

            SetEventRewards(newEvent);
            return newEvent;
        }

        SpecialEvent CreateRandomEvent(RandomEventType randomType)
        {
            var newEvent = new SpecialEvent
            {
                id = Guid.NewGuid().ToString(),
                type = EventType.Random,
                startTime = Time.time,
                duration = randomEventDuration,
                isActive = true
            };

            switch (randomType)
            {
                case RandomEventType.MerchantVisit:
                    newEvent.name = "Traveling Merchant";
                    newEvent.description = "A mysterious merchant arrives with rare goods. Negotiate for the best deals!";
                    newEvent.difficulty = EventDifficulty.Normal;
                    AddMerchantVisitMiniGames(newEvent);
                    break;
                    
                case RandomEventType.MonsterRaid:
                    newEvent.name = "Monster Raid";
                    newEvent.description = "Monsters are attacking the town! Defend against the invasion!";
                    newEvent.difficulty = EventDifficulty.Hard;
                    AddMonsterRaidMiniGames(newEvent);
                    break;
                    
                case RandomEventType.TravelingCircus:
                    newEvent.name = "Traveling Circus";
                    newEvent.description = "The circus is in town! Participate in fun games and earn prizes!";
                    newEvent.difficulty = EventDifficulty.Easy;
                    AddCircusMiniGames(newEvent);
                    break;
                    
                case RandomEventType.MysteriousStranger:
                    newEvent.name = "Mysterious Stranger";
                    newEvent.description = "A hooded figure offers cryptic challenges with valuable rewards.";
                    newEvent.difficulty = EventDifficulty.Expert;
                    AddMysteriousStrangerMiniGames(newEvent);
                    break;
                    
                case RandomEventType.BanditsAttack:
                    newEvent.name = "Bandits Attack";
                    newEvent.description = "Bandits are terrorizing travelers! Hunt them down!";
                    newEvent.difficulty = EventDifficulty.Normal;
                    AddBanditMiniGames(newEvent);
                    break;
                    
                case RandomEventType.NoblemenVisit:
                    newEvent.name = "Noble's Request";
                    newEvent.description = "A nobleman requests your guild's assistance with delicate matters.";
                    newEvent.difficulty = EventDifficulty.Normal;
                    AddNoblemenMiniGames(newEvent);
                    break;
                    
                case RandomEventType.DisasterRelief:
                    newEvent.name = "Disaster Relief";
                    newEvent.description = "A nearby village needs help after a natural disaster!";
                    newEvent.difficulty = EventDifficulty.Normal;
                    AddDisasterReliefMiniGames(newEvent);
                    break;
                    
                case RandomEventType.TreasureDiscovery:
                    newEvent.name = "Ancient Treasure Map";
                    newEvent.description = "An old map reveals the location of hidden treasure!";
                    newEvent.difficulty = EventDifficulty.Hard;
                    AddTreasureHuntMiniGames(newEvent);
                    break;
            }

            SetEventRewards(newEvent);
            return newEvent;
        }

        SpecialEvent CreateMilestoneEvent(string milestoneName)
        {
            var newEvent = new SpecialEvent
            {
                id = Guid.NewGuid().ToString(),
                name = milestoneName,
                description = $"Celebrate reaching {milestoneName}!",
                type = EventType.GuildMilestone,
                startTime = Time.time,
                duration = milestoneEventDuration,
                difficulty = EventDifficulty.Normal,
                isActive = true
            };

            // Add appropriate mini-games based on milestone type
            AddMilestoneMiniGames(newEvent, milestoneName);
            SetEventRewards(newEvent);
            
            return newEvent;
        }

        SpecialEvent CreateCharacterEvent(string characterId)
        {
            var character = CharacterManager.Instance.GetCharacterData(characterId);
            if (character == null) return null;

            var newEvent = new SpecialEvent
            {
                id = Guid.NewGuid().ToString(),
                name = $"{character.name}'s Special Quest",
                description = $"Help {character.name} with a personal matter.",
                type = EventType.CharacterSpecific,
                startTime = Time.time,
                duration = randomEventDuration,
                difficulty = GetDifficultyForCharacter(character),
                isActive = true
            };

            newEvent.requirements.requiredCharacters.Add(characterId);
            AddCharacterSpecificMiniGames(newEvent, character);
            SetEventRewards(newEvent);
            
            return newEvent;
        }

        #endregion

        #region Mini-Game Creation

        MiniGame CreateResourceGatheringGame(string name, EventDifficulty difficulty)
        {
            var miniGame = new MiniGame
            {
                id = Guid.NewGuid().ToString(),
                name = name,
                type = MiniGameType.ResourceGathering,
                difficulty = difficulty,
                duration = 180f, // 3 minutes
                description = "Gather resources efficiently within the time limit!"
            };

            // Set game-specific data
            miniGame.gameData["targetResource"] = GetRandomResourceType(); // Now returns string instead of ResourceType
            miniGame.gameData["targetAmount"] = GetTargetAmount(difficulty, 50, 500);
            miniGame.gameData["bonusMultiplier"] = GetBonusMultiplier(difficulty);
            
            SetMiniGameRewards(miniGame, difficulty);
            return miniGame;
        }

        MiniGame CreateCombatTrainingGame(string name, EventDifficulty difficulty)
        {
            var miniGame = new MiniGame
            {
                id = Guid.NewGuid().ToString(),
                name = name,
                type = MiniGameType.CombatTraining,
                difficulty = difficulty,
                duration = 300f, // 5 minutes
                description = "Train your adventurers in combat techniques!"
            };

            // Set combat training parameters
            miniGame.gameData["enemyCount"] = GetEnemyCount(difficulty);
            miniGame.gameData["enemyLevel"] = GetEnemyLevel(difficulty);
            miniGame.gameData["victoryCondition"] = "defeat_all";
            miniGame.gameData["allowedDefeats"] = GetAllowedDefeats(difficulty);
            
            SetMiniGameRewards(miniGame, difficulty);
            return miniGame;
        }

        MiniGame CreateNegotiationGame(string name, EventDifficulty difficulty)
        {
            var miniGame = new MiniGame
            {
                id = Guid.NewGuid().ToString(),
                name = name,
                type = MiniGameType.Negotiation,
                difficulty = difficulty,
                duration = 240f, // 4 minutes
                description = "Use diplomacy to achieve the best outcome!"
            };

            // Set negotiation parameters
            miniGame.gameData["targetAgreement"] = GetTargetAgreement(difficulty);
            miniGame.gameData["initialOffer"] = GetInitialOffer(difficulty);
            miniGame.gameData["negotiationRounds"] = GetNegotiationRounds(difficulty);
            miniGame.gameData["oppositionMood"] = GetOppositionMood(difficulty);
            
            SetMiniGameRewards(miniGame, difficulty);
            return miniGame;
        }

        MiniGame CreatePuzzleGame(string name, EventDifficulty difficulty)
        {
            var miniGame = new MiniGame
            {
                id = Guid.NewGuid().ToString(),
                name = name,
                type = MiniGameType.PuzzleSolving,
                difficulty = difficulty,
                duration = 360f, // 6 minutes
                description = "Solve challenging puzzles to unlock rewards!"
            };

            // Set puzzle parameters
            miniGame.gameData["puzzleType"] = GetRandomPuzzleType();
            miniGame.gameData["puzzleSize"] = GetPuzzleSize(difficulty);
            miniGame.gameData["moveLimit"] = GetMoveLimit(difficulty);
            miniGame.gameData["hintsAvailable"] = GetHintCount(difficulty);
            
            SetMiniGameRewards(miniGame, difficulty);
            return miniGame;
        }

        MiniGame CreateTimingGame(string name, EventDifficulty difficulty)
        {
            var miniGame = new MiniGame
            {
                id = Guid.NewGuid().ToString(),
                name = name,
                type = MiniGameType.TimingBased,
                difficulty = difficulty,
                duration = 120f, // 2 minutes
                description = "Perfect timing is key to success!"
            };

            // Set timing game parameters
            miniGame.gameData["sequenceLength"] = GetSequenceLength(difficulty);
            miniGame.gameData["timingWindow"] = GetTimingWindow(difficulty);
            miniGame.gameData["speedMultiplier"] = GetSpeedMultiplier(difficulty);
            miniGame.gameData["perfectBonus"] = GetPerfectBonus(difficulty);
            
            SetMiniGameRewards(miniGame, difficulty);
            return miniGame;
        }

        #endregion

        #region Mini-Game Logic

        public void StartMiniGame(MiniGame miniGame)
        {
            if (activeMiniGames.Count >= maxConcurrentMiniGames)
            {
                Debug.LogWarning("Maximum concurrent mini-games reached!");
                return;
            }

            miniGame.isActive = true;
            miniGame.startTime = Time.time;
            miniGame.currentAttempts = 0;
            activeMiniGames.Add(miniGame);
            
            OnMiniGameStarted?.Invoke(miniGame);
            
            // Start mini-game coroutine based on type
            StartCoroutine(RunMiniGame(miniGame));
        }

        IEnumerator RunMiniGame(MiniGame miniGame)
        {
            float elapsedTime = 0f;
            
            while (miniGame.isActive && elapsedTime < miniGame.duration)
            {
                elapsedTime = Time.time - miniGame.startTime;
                
                // Update mini-game based on type
                switch (miniGame.type)
                {
                    case MiniGameType.ResourceGathering:
                        UpdateResourceGatheringGame(miniGame, elapsedTime);
                        break;
                    case MiniGameType.CombatTraining:
                        UpdateCombatTrainingGame(miniGame, elapsedTime);
                        break;
                    case MiniGameType.Negotiation:
                        UpdateNegotiationGame(miniGame, elapsedTime);
                        break;
                    case MiniGameType.PuzzleSolving:
                        UpdatePuzzleGame(miniGame, elapsedTime);
                        break;
                    case MiniGameType.TimingBased:
                        UpdateTimingGame(miniGame, elapsedTime);
                        break;
                }
                
                yield return new WaitForSeconds(0.5f);
            }
            
            // Complete mini-game
            CompleteMiniGame(miniGame, elapsedTime >= miniGame.duration);
        }

        void UpdateResourceGatheringGame(MiniGame game, float elapsedTime)
        {
            // Simulate resource gathering progress
            if (!game.gameData.ContainsKey("currentAmount"))
                game.gameData["currentAmount"] = 0;
                
            float gatherRate = GetGatheringRate(game.difficulty);
            int gathered = Mathf.FloorToInt(gatherRate * Time.deltaTime);
            game.gameData["currentAmount"] = (int)game.gameData["currentAmount"] + gathered;
            
            // Check completion
            int target = (int)game.gameData["targetAmount"];
            int current = (int)game.gameData["currentAmount"];
            
            if (current >= target)
            {
                game.successRate = 1f;
                game.isActive = false;
            }
            else
            {
                game.successRate = (float)current / target;
            }
        }

        void UpdateCombatTrainingGame(MiniGame game, float elapsedTime)
        {
            // Simulate combat progress
            if (!game.gameData.ContainsKey("enemiesDefeated"))
                game.gameData["enemiesDefeated"] = 0;
                
            // Check if a battle simulation should occur
            if (Random.value < 0.1f) // 10% chance per update
            {
                bool victorious = SimulateBattle(game.difficulty);
                if (victorious)
                {
                    game.gameData["enemiesDefeated"] = (int)game.gameData["enemiesDefeated"] + 1;
                }
                else
                {
                    if (!game.gameData.ContainsKey("defeats"))
                        game.gameData["defeats"] = 0;
                    game.gameData["defeats"] = (int)game.gameData["defeats"] + 1;
                }
            }
            
            // Check completion
            int enemyCount = (int)game.gameData["enemyCount"];
            int defeated = (int)game.gameData["enemiesDefeated"];
            int defeats = game.gameData.ContainsKey("defeats") ? (int)game.gameData["defeats"] : 0;
            int allowedDefeats = (int)game.gameData["allowedDefeats"];
            
            if (defeated >= enemyCount)
            {
                game.successRate = 1f;
                game.isActive = false;
            }
            else if (defeats > allowedDefeats)
            {
                game.successRate = 0f;
                game.isActive = false;
            }
            else
            {
                game.successRate = (float)defeated / enemyCount;
            }
        }

        void UpdateNegotiationGame(MiniGame game, float elapsedTime)
        {
            // Simulate negotiation rounds
            if (!game.gameData.ContainsKey("currentRound"))
                game.gameData["currentRound"] = 0;
                
            float roundInterval = game.duration / (int)game.gameData["negotiationRounds"];
            int expectedRound = Mathf.FloorToInt(elapsedTime / roundInterval);
            
            if (expectedRound > (int)game.gameData["currentRound"])
            {
                game.gameData["currentRound"] = expectedRound;
                
                // Simulate negotiation progress
                float mood = (float)game.gameData["oppositionMood"];
                float negotiationSkill = GetNegotiationSkill(game.difficulty);
                
                if (Random.value < negotiationSkill * mood)
                {
                    if (!game.gameData.ContainsKey("agreementLevel"))
                        game.gameData["agreementLevel"] = 0f;
                    game.gameData["agreementLevel"] = (float)game.gameData["agreementLevel"] + 0.2f;
                }
            }
            
            // Check completion
            float targetAgreement = (float)game.gameData["targetAgreement"];
            float currentAgreement = game.gameData.ContainsKey("agreementLevel") ? 
                (float)game.gameData["agreementLevel"] : 0f;
            
            game.successRate = currentAgreement / targetAgreement;
            
            if (currentAgreement >= targetAgreement)
            {
                game.isActive = false;
            }
        }

        void UpdatePuzzleGame(MiniGame game, float elapsedTime)
        {
            // Puzzle games are typically turn-based, so we simulate progress
            if (!game.gameData.ContainsKey("puzzleProgress"))
                game.gameData["puzzleProgress"] = 0f;
                
            // Simulate puzzle solving progress
            float solveRate = GetPuzzleSolveRate(game.difficulty);
            game.gameData["puzzleProgress"] = Mathf.Min((float)game.gameData["puzzleProgress"] + 
                solveRate * Time.deltaTime, 1f);
            
            game.successRate = (float)game.gameData["puzzleProgress"];
            
            if (game.successRate >= 1f)
            {
                game.isActive = false;
            }
        }

        void UpdateTimingGame(MiniGame game, float elapsedTime)
        {
            // Simulate timing-based challenges
            if (!game.gameData.ContainsKey("sequenceIndex"))
                game.gameData["sequenceIndex"] = 0;
                
            int sequenceLength = (int)game.gameData["sequenceLength"];
            int currentIndex = (int)game.gameData["sequenceIndex"];
            
            // Check for timing window
            float timingWindow = (float)game.gameData["timingWindow"];
            float sequenceInterval = game.duration / sequenceLength;
            
            if (elapsedTime >= currentIndex * sequenceInterval)
            {
                // Simulate player input timing
                bool success = Random.value < GetTimingSuccessChance(game.difficulty);
                
                if (success)
                {
                    if (!game.gameData.ContainsKey("successfulHits"))
                        game.gameData["successfulHits"] = 0;
                    game.gameData["successfulHits"] = (int)game.gameData["successfulHits"] + 1;
                }
                
                game.gameData["sequenceIndex"] = currentIndex + 1;
            }
            
            // Check completion
            if (currentIndex >= sequenceLength)
            {
                int successfulHits = game.gameData.ContainsKey("successfulHits") ? 
                    (int)game.gameData["successfulHits"] : 0;
                game.successRate = (float)successfulHits / sequenceLength;
                game.isActive = false;
            }
        }

        void CompleteMiniGame(MiniGame miniGame, bool timeExpired)
        {
            miniGame.isActive = false;
            activeMiniGames.Remove(miniGame);
            
            // Calculate final success
            bool success = miniGame.successRate >= GetSuccessThreshold(miniGame.difficulty);
            
            // Create result
            var result = new MiniGameResult
            {
                miniGameId = miniGame.id,
                success = success,
                score = miniGame.successRate * 100f,
                completionTime = Time.time - miniGame.startTime
            };
            
            // Calculate rewards
            if (success)
            {
                result.earnedRewards = CalculateMiniGameRewards(miniGame, miniGame.successRate);
                GrantMiniGameRewards(result.earnedRewards);
            }
            else
            {
                // Partial rewards for effort
                result.earnedRewards = CalculatePartialRewards(miniGame, miniGame.successRate);
                GrantMiniGameRewards(result.earnedRewards);
            }
            
            // Track completion
            miniGameCompletions[miniGame.type]++;
            miniGameHistory[miniGame.id] = result;
            
            OnMiniGameCompleted?.Invoke(result);
        }

        #endregion

        #region Event Management Methods

        public void StartEvent(SpecialEvent specialEvent)
        {
            if (!CanStartEvent(specialEvent)) return;
            
            specialEvent.isActive = true;
            specialEvent.startTime = Time.time;
            activeEvents.Add(specialEvent);
            
            // Start associated mini-games
            foreach (var miniGame in specialEvent.associatedMiniGames)
            {
                StartMiniGame(miniGame);
            }
            
            OnEventStarted?.Invoke(specialEvent);
            
            // Send notification
            SendEventNotification(specialEvent, true);
        }

        void CompleteEvent(SpecialEvent specialEvent)
        {
            specialEvent.isActive = false;
            specialEvent.isCompleted = true;
            completedEvents.Add(specialEvent);
            
            // Calculate and grant rewards
            var finalRewards = CalculateEventRewards(specialEvent);
            GrantEventRewards(finalRewards);
            
            OnEventCompleted?.Invoke(specialEvent);
            
            // Send completion notification
            SendEventNotification(specialEvent, false);
        }

        bool CanStartEvent(SpecialEvent specialEvent)
        {
            // Check concurrent event limit
            if (activeEvents.Count >= maxConcurrentEvents) return false;
            
            // Check requirements
            var requirements = specialEvent.requirements;
            
            if (playerGuildLevel < requirements.minGuildLevel) return false;
            
            // Check building requirements
            foreach (var building in requirements.requiredBuildings)
            {
                // TODO: Check if building exists
            }
            
            // Check character requirements
            foreach (var character in requirements.requiredCharacters)
            {
                // TODO: Check if character is available
            }
            
            return true;
        }

        void UpdateEventProgress(SpecialEvent activeEvent)
        {
            // Calculate progress based on completed mini-games
            int totalMiniGames = activeEvent.associatedMiniGames.Count;
            int completedMiniGames = 0;
            float totalScore = 0f;
            
            foreach (var miniGame in activeEvent.associatedMiniGames)
            {
                if (!miniGame.isActive)
                {
                    completedMiniGames++;
                    if (miniGameHistory.ContainsKey(miniGame.id))
                    {
                        totalScore += miniGameHistory[miniGame.id].score;
                    }
                }
            }
            
            activeEvent.progress = totalMiniGames > 0 ? 
                (float)completedMiniGames / totalMiniGames : 0f;
            
            // Update participant count based on activity
            if (Random.value < 0.1f) // Simulate participation
            {
                activeEvent.participantCount++;
            }
        }

        #endregion

        #region Reward Calculation

        void SetMiniGameRewards(MiniGame miniGame, EventDifficulty difficulty)
        {
            // Base rewards based on difficulty
            float difficultyMultiplier = GetDifficultyMultiplier(difficulty);
            
            miniGame.rewards.goldReward = Mathf.RoundToInt(100 * difficultyMultiplier);
            miniGame.rewards.experienceReward = Mathf.RoundToInt(50 * difficultyMultiplier);
            miniGame.rewards.reputationReward = Mathf.RoundToInt(10 * difficultyMultiplier);
            
            // Type-specific rewards
            switch (miniGame.type)
            {
                case MiniGameType.ResourceGathering:
                    miniGame.rewards.woodReward = Mathf.RoundToInt(50 * difficultyMultiplier);
                    miniGame.rewards.stoneReward = Mathf.RoundToInt(30 * difficultyMultiplier);
                    break;
                    
                case MiniGameType.CombatTraining:
                    miniGame.rewards.experienceReward *= 2;
                    miniGame.rewards.manaStoneReward = Mathf.RoundToInt(5 * difficultyMultiplier);
                    break;
                    
                case MiniGameType.Negotiation:
                    miniGame.rewards.goldReward *= 2;
                    miniGame.rewards.reputationReward *= 2;
                    break;
                    
                case MiniGameType.PuzzleSolving:
                    miniGame.rewards.manaStoneReward = Mathf.RoundToInt(10 * difficultyMultiplier);
                    break;
                    
                case MiniGameType.TimingBased:
                    miniGame.rewards.goldReward = Mathf.RoundToInt(150 * difficultyMultiplier);
                    break;
            }
        }

        void SetEventRewards(SpecialEvent specialEvent)
        {
            float difficultyMultiplier = GetDifficultyMultiplier(specialEvent.difficulty);
            float eventTypeMultiplier = GetEventTypeMultiplier(specialEvent.type);
            
            // Base rewards
            specialEvent.rewards.baseRewards.goldReward = 
                Mathf.RoundToInt(500 * difficultyMultiplier * eventTypeMultiplier);
            specialEvent.rewards.baseRewards.experienceReward = 
                Mathf.RoundToInt(200 * difficultyMultiplier * eventTypeMultiplier);
            specialEvent.rewards.baseRewards.reputationReward = 
                Mathf.RoundToInt(50 * difficultyMultiplier * eventTypeMultiplier);
            
            // Completion bonus
            specialEvent.rewards.completionBonus.goldReward = 
                Mathf.RoundToInt(1000 * difficultyMultiplier * eventTypeMultiplier);
            specialEvent.rewards.completionBonus.manaStoneReward = 
                Mathf.RoundToInt(20 * difficultyMultiplier);
            
            // Participation rewards
            specialEvent.rewards.participationReward.goldReward = 100;
            specialEvent.rewards.participationReward.experienceReward = 25;
            
            // Milestone rewards
            specialEvent.rewards.milestoneRewards["25_percent"] = 250;
            specialEvent.rewards.milestoneRewards["50_percent"] = 500;
            specialEvent.rewards.milestoneRewards["75_percent"] = 750;
            specialEvent.rewards.milestoneRewards["100_percent"] = 1000;
        }

        MiniGameReward CalculateMiniGameRewards(MiniGame miniGame, float successRate)
        {
            var rewards = new MiniGameReward();
            
            // Apply success rate multiplier
            float multiplier = successRate * miniGame.rewards.successMultiplier;
            
            rewards.goldReward = Mathf.RoundToInt(miniGame.rewards.goldReward * multiplier);
            rewards.woodReward = Mathf.RoundToInt(miniGame.rewards.woodReward * multiplier);
            rewards.stoneReward = Mathf.RoundToInt(miniGame.rewards.stoneReward * multiplier);
            rewards.manaStoneReward = Mathf.RoundToInt(miniGame.rewards.manaStoneReward * multiplier);
            rewards.experienceReward = Mathf.RoundToInt(miniGame.rewards.experienceReward * multiplier);
            rewards.reputationReward = Mathf.RoundToInt(miniGame.rewards.reputationReward * multiplier);
            
            // Apply player progression bonuses
            float progressionBonus = 1f + (playerGuildLevel * 0.05f);
            rewards.goldReward = Mathf.RoundToInt(rewards.goldReward * progressionBonus);
            
            return rewards;
        }

        MiniGameReward CalculatePartialRewards(MiniGame miniGame, float successRate)
        {
            // Give 25% of rewards for failed attempts
            var partialRewards = CalculateMiniGameRewards(miniGame, successRate * 0.25f);
            return partialRewards;
        }

        EventRewards CalculateEventRewards(SpecialEvent specialEvent)
        {
            var finalRewards = new EventRewards();
            
            // Base rewards for participation
            finalRewards.participationReward = specialEvent.rewards.participationReward;
            
            // Calculate completion percentage
            float completionPercentage = specialEvent.progress;
            
            // Base rewards scaled by completion
            finalRewards.baseRewards.goldReward = 
                Mathf.RoundToInt(specialEvent.rewards.baseRewards.goldReward * completionPercentage);
            finalRewards.baseRewards.experienceReward = 
                Mathf.RoundToInt(specialEvent.rewards.baseRewards.experienceReward * completionPercentage);
            finalRewards.baseRewards.reputationReward = 
                Mathf.RoundToInt(specialEvent.rewards.baseRewards.reputationReward * completionPercentage);
            
            // Completion bonus if 100%
            if (completionPercentage >= 1f)
            {
                finalRewards.completionBonus = specialEvent.rewards.completionBonus;
            }
            
            // Milestone rewards
            foreach (var milestone in specialEvent.rewards.milestoneRewards)
            {
                float threshold = float.Parse(milestone.Key.Split('_')[0]) / 100f;
                if (completionPercentage >= threshold)
                {
                    finalRewards.milestoneRewards[milestone.Key] = milestone.Value;
                }
            }
            
            return finalRewards;
        }

        void GrantMiniGameRewards(MiniGameReward rewards)
        {
            // if (ResourceManager.Instance != null) // ResourceManager commented out
            // {
            //     if (rewards.goldReward > 0)
            //         ResourceManager.Instance.AddGold(rewards.goldReward);
            //     if (rewards.woodReward > 0)
            //         ResourceManager.Instance.AddWood(rewards.woodReward);
            //     if (rewards.stoneReward > 0)
            //         ResourceManager.Instance.AddStone(rewards.stoneReward);
            //     if (rewards.manaStoneReward > 0)
            //         ResourceManager.Instance.AddManaStone(rewards.manaStoneReward);
            //     if (rewards.reputationReward > 0)
            //         ResourceManager.Instance.AddReputation(rewards.reputationReward);
            // }
            
            // TODO: Grant experience to participating adventurers
            // TODO: Grant item rewards
        }

        void GrantEventRewards(EventRewards rewards)
        {
            // Grant all reward types
            GrantMiniGameRewards(rewards.participationReward);
            GrantMiniGameRewards(rewards.baseRewards);
            GrantMiniGameRewards(rewards.completionBonus);
            
            // Grant milestone rewards
            // foreach (var milestone in rewards.milestoneRewards) // ResourceManager commented out
            // {
            //     if (ResourceManager.Instance != null)
            //     {
            //         ResourceManager.Instance.AddGold(milestone.Value);
            //     }
            // }
        }

        #endregion

        #region Helper Methods

        void ScheduleInitialEvents()
        {
            // Schedule first seasonal event
            var seasonalSchedule = new EventSchedule
            {
                eventId = "seasonal_" + currentSeason.ToString(),
                scheduledTime = Time.time + 3600f, // 1 hour from start
                eventType = EventType.Seasonal,
                isRecurring = true,
                recurringInterval = seasonDuration,
                maxOccurrences = 99
            };
            scheduledEvents.Enqueue(seasonalSchedule);
            
            // Schedule random events
            for (int i = 0; i < 3; i++)
            {
                var randomSchedule = new EventSchedule
                {
                    eventId = "random_" + i,
                    scheduledTime = Time.time + Random.Range(3600f, 7200f),
                    eventType = EventType.Random,
                    isRecurring = false
                };
                scheduledEvents.Enqueue(randomSchedule);
            }
        }

        void StartScheduledEvent(EventSchedule schedule)
        {
            SpecialEvent newEvent = null;
            
            switch (schedule.eventType)
            {
                case EventType.Seasonal:
                    newEvent = CreateSeasonalEvent(currentSeason);
                    break;
                case EventType.Random:
                    var randomType = (RandomEventType)Random.Range(0, 
                        Enum.GetValues(typeof(RandomEventType)).Length);
                    newEvent = CreateRandomEvent(randomType);
                    break;
                case EventType.GuildMilestone:
                    newEvent = CreateMilestoneEvent("Scheduled Milestone");
                    break;
            }
            
            if (newEvent != null && CanStartEvent(newEvent))
            {
                StartEvent(newEvent);
            }
        }

        void ChangeSeason()
        {
            int nextSeasonIndex = ((int)currentSeason + 1) % 
                Enum.GetValues(typeof(SeasonalEventType)).Length;
            currentSeason = (SeasonalEventType)nextSeasonIndex;
            lastSeasonChange = Time.time;
            
            OnSeasonChanged?.Invoke(currentSeason);
            
            // Start seasonal event
            var seasonalEvent = CreateSeasonalEvent(currentSeason);
            if (seasonalEvent != null && CanStartEvent(seasonalEvent))
            {
                StartEvent(seasonalEvent);
            }
        }

        SeasonalEventType GetCurrentSeason()
        {
            // Simple season calculation based on time
            int monthIndex = (int)(Time.time / seasonDuration) % 4;
            return (SeasonalEventType)monthIndex;
        }

        void SendEventNotification(SpecialEvent specialEvent, bool isStart)
        {
            string message = isStart ? 
                $"Event Started: {specialEvent.name}!\n{specialEvent.description}" :
                $"Event Completed: {specialEvent.name}!\nCheck your rewards!";
                
            Debug.Log(message);
            // TODO: Integrate with actual notification system
        }

        bool HasRecentMilestoneEvent()
        {
            // Check if a milestone event occurred recently
            foreach (var completedEvent in completedEvents)
            {
                if (completedEvent.type == EventType.GuildMilestone &&
                    Time.time - completedEvent.startTime < 86400f) // Within last day
                {
                    return true;
                }
            }
            return false;
        }

        void CheckAdventurerMilestones()
        {
            // TODO: Implement adventurer count milestone checks
        }

        void CheckWealthMilestones()
        {
            // TODO: Implement wealth milestone checks
        }

        void CheckReputationMilestones()
        {
            // TODO: Implement reputation milestone checks
        }

        #endregion

        #region Difficulty and Scaling

        float GetDifficultyMultiplier(EventDifficulty difficulty)
        {
            return difficulty switch
            {
                EventDifficulty.Easy => 0.75f,
                EventDifficulty.Normal => 1f,
                EventDifficulty.Hard => 1.5f,
                EventDifficulty.Expert => 2f,
                EventDifficulty.Legendary => 3f,
                _ => 1f
            };
        }

        float GetEventTypeMultiplier(EventType type)
        {
            return type switch
            {
                EventType.Seasonal => 1.5f,
                EventType.Random => 1f,
                EventType.GuildMilestone => 2f,
                EventType.CharacterSpecific => 1.25f,
                _ => 1f
            };
        }

        EventDifficulty GetDifficultyForCharacter(CharacterData character)
        {
            // Base difficulty on character rarity
            return character.rarity switch
            {
                CharacterRarity.Common => EventDifficulty.Easy,
                CharacterRarity.Uncommon => EventDifficulty.Normal,
                CharacterRarity.Rare => EventDifficulty.Hard,
                CharacterRarity.Epic => EventDifficulty.Expert,
                CharacterRarity.Legendary => EventDifficulty.Legendary,
                _ => EventDifficulty.Normal
            };
        }

        public void UpdatePlayerProgression(int guildLevel)
        {
            playerGuildLevel = guildLevel;
            
            // Scale existing events
            foreach (var activeEvent in activeEvents)
            {
                ScaleEventDifficulty(activeEvent);
            }
        }

        void ScaleEventDifficulty(SpecialEvent specialEvent)
        {
            // Increase rewards based on guild level
            float scalingFactor = 1f + (playerGuildLevel * difficultyScalingFactor);
            
            specialEvent.rewards.baseRewards.goldReward = 
                Mathf.RoundToInt(specialEvent.rewards.baseRewards.goldReward * scalingFactor);
            specialEvent.rewards.baseRewards.experienceReward = 
                Mathf.RoundToInt(specialEvent.rewards.baseRewards.experienceReward * scalingFactor);
        }

        #endregion

        #region Game-Specific Helper Methods

        // Resource Gathering Helpers
        // ResourceType GetRandomResourceType() // ResourceType removed
        // {
        //     return (ResourceType)Random.Range(0, Enum.GetValues(typeof(ResourceType)).Length);
        // }
        string GetRandomResourceType()
        {
            string[] resourceTypes = { "Gold", "Wood", "Stone", "ManaStone" };
            return resourceTypes[Random.Range(0, resourceTypes.Length)];
        }

        int GetTargetAmount(EventDifficulty difficulty, int min, int max)
        {
            float difficultyFactor = GetDifficultyMultiplier(difficulty);
            return Mathf.RoundToInt(Random.Range(min, max) * difficultyFactor);
        }

        float GetBonusMultiplier(EventDifficulty difficulty)
        {
            return 1f + (GetDifficultyMultiplier(difficulty) - 1f) * 0.5f;
        }

        float GetGatheringRate(EventDifficulty difficulty)
        {
            return 10f / GetDifficultyMultiplier(difficulty);
        }

        // Combat Training Helpers
        int GetEnemyCount(EventDifficulty difficulty)
        {
            return difficulty switch
            {
                EventDifficulty.Easy => Random.Range(3, 5),
                EventDifficulty.Normal => Random.Range(5, 8),
                EventDifficulty.Hard => Random.Range(8, 12),
                EventDifficulty.Expert => Random.Range(12, 15),
                EventDifficulty.Legendary => Random.Range(15, 20),
                _ => 5
            };
        }

        int GetEnemyLevel(EventDifficulty difficulty)
        {
            int baseLevel = playerGuildLevel;
            return difficulty switch
            {
                EventDifficulty.Easy => Mathf.Max(1, baseLevel - 2),
                EventDifficulty.Normal => baseLevel,
                EventDifficulty.Hard => baseLevel + 2,
                EventDifficulty.Expert => baseLevel + 4,
                EventDifficulty.Legendary => baseLevel + 6,
                _ => baseLevel
            };
        }

        int GetAllowedDefeats(EventDifficulty difficulty)
        {
            return difficulty switch
            {
                EventDifficulty.Easy => 3,
                EventDifficulty.Normal => 2,
                EventDifficulty.Hard => 1,
                EventDifficulty.Expert => 0,
                EventDifficulty.Legendary => 0,
                _ => 2
            };
        }

        bool SimulateBattle(EventDifficulty difficulty)
        {
            float winChance = 0.7f / GetDifficultyMultiplier(difficulty);
            return Random.value < winChance;
        }

        // Negotiation Helpers
        float GetTargetAgreement(EventDifficulty difficulty)
        {
            return GetDifficultyMultiplier(difficulty);
        }

        float GetInitialOffer(EventDifficulty difficulty)
        {
            return 0.5f / GetDifficultyMultiplier(difficulty);
        }

        int GetNegotiationRounds(EventDifficulty difficulty)
        {
            return Mathf.RoundToInt(3 * GetDifficultyMultiplier(difficulty));
        }

        float GetOppositionMood(EventDifficulty difficulty)
        {
            return 1f / GetDifficultyMultiplier(difficulty);
        }

        float GetNegotiationSkill(EventDifficulty difficulty)
        {
            return 0.8f / GetDifficultyMultiplier(difficulty);
        }

        // Puzzle Helpers
        string GetRandomPuzzleType()
        {
            string[] puzzleTypes = { "sliding", "matching", "sequence", "logic", "riddle" };
            return puzzleTypes[Random.Range(0, puzzleTypes.Length)];
        }

        int GetPuzzleSize(EventDifficulty difficulty)
        {
            return difficulty switch
            {
                EventDifficulty.Easy => 3,
                EventDifficulty.Normal => 4,
                EventDifficulty.Hard => 5,
                EventDifficulty.Expert => 6,
                EventDifficulty.Legendary => 8,
                _ => 4
            };
        }

        int GetMoveLimit(EventDifficulty difficulty)
        {
            return Mathf.RoundToInt(50 / GetDifficultyMultiplier(difficulty));
        }

        int GetHintCount(EventDifficulty difficulty)
        {
            return Mathf.Max(1, 5 - (int)difficulty);
        }

        float GetPuzzleSolveRate(EventDifficulty difficulty)
        {
            return 0.01f / GetDifficultyMultiplier(difficulty);
        }

        // Timing Game Helpers
        int GetSequenceLength(EventDifficulty difficulty)
        {
            return Mathf.RoundToInt(5 * GetDifficultyMultiplier(difficulty));
        }

        float GetTimingWindow(EventDifficulty difficulty)
        {
            return 1f / GetDifficultyMultiplier(difficulty);
        }

        float GetSpeedMultiplier(EventDifficulty difficulty)
        {
            return GetDifficultyMultiplier(difficulty);
        }

        float GetPerfectBonus(EventDifficulty difficulty)
        {
            return 2f * GetDifficultyMultiplier(difficulty);
        }

        float GetTimingSuccessChance(EventDifficulty difficulty)
        {
            return 0.8f / GetDifficultyMultiplier(difficulty);
        }

        float GetSuccessThreshold(EventDifficulty difficulty)
        {
            return difficulty switch
            {
                EventDifficulty.Easy => 0.5f,
                EventDifficulty.Normal => 0.6f,
                EventDifficulty.Hard => 0.7f,
                EventDifficulty.Expert => 0.8f,
                EventDifficulty.Legendary => 0.9f,
                _ => 0.6f
            };
        }

        #endregion

        #region Event-Specific Mini-Game Setup

        void AddSpringFestivalMiniGames(SpecialEvent springEvent)
        {
            springEvent.associatedMiniGames.Add(
                CreateResourceGatheringGame("Flower Picking Contest", EventDifficulty.Easy));
            springEvent.associatedMiniGames.Add(
                CreateTimingGame("Spring Dance", EventDifficulty.Normal));
            springEvent.associatedMiniGames.Add(
                CreatePuzzleGame("Garden Arrangement", EventDifficulty.Normal));
        }

        void AddSummerTournamentMiniGames(SpecialEvent summerEvent)
        {
            summerEvent.associatedMiniGames.Add(
                CreateCombatTrainingGame("Arena Battles", EventDifficulty.Hard));
            summerEvent.associatedMiniGames.Add(
                CreateTimingGame("Archery Contest", EventDifficulty.Hard));
            summerEvent.associatedMiniGames.Add(
                CreateCombatTrainingGame("Team Tournament", EventDifficulty.Expert));
        }

        void AddHarvestFestivalMiniGames(SpecialEvent harvestEvent)
        {
            harvestEvent.associatedMiniGames.Add(
                CreateResourceGatheringGame("Harvest Competition", EventDifficulty.Normal));
            harvestEvent.associatedMiniGames.Add(
                CreateNegotiationGame("Market Haggling", EventDifficulty.Normal));
            harvestEvent.associatedMiniGames.Add(
                CreatePuzzleGame("Crop Rotation Planning", EventDifficulty.Normal));
        }

        void AddWinterCelebrationMiniGames(SpecialEvent winterEvent)
        {
            winterEvent.associatedMiniGames.Add(
                CreatePuzzleGame("Ice Sculpture Contest", EventDifficulty.Easy));
            winterEvent.associatedMiniGames.Add(
                CreateTimingGame("Snowball Fight", EventDifficulty.Easy));
            winterEvent.associatedMiniGames.Add(
                CreateNegotiationGame("Gift Exchange", EventDifficulty.Easy));
        }

        void AddMerchantVisitMiniGames(SpecialEvent merchantEvent)
        {
            merchantEvent.associatedMiniGames.Add(
                CreateNegotiationGame("Trade Negotiations", EventDifficulty.Normal));
            merchantEvent.associatedMiniGames.Add(
                CreatePuzzleGame("Inventory Management", EventDifficulty.Normal));
        }

        void AddMonsterRaidMiniGames(SpecialEvent raidEvent)
        {
            raidEvent.associatedMiniGames.Add(
                CreateCombatTrainingGame("Defend the Town", EventDifficulty.Hard));
            raidEvent.associatedMiniGames.Add(
                CreateTimingGame("Emergency Response", EventDifficulty.Hard));
        }

        void AddCircusMiniGames(SpecialEvent circusEvent)
        {
            circusEvent.associatedMiniGames.Add(
                CreateTimingGame("Juggling Performance", EventDifficulty.Easy));
            circusEvent.associatedMiniGames.Add(
                CreatePuzzleGame("Magic Tricks", EventDifficulty.Easy));
            circusEvent.associatedMiniGames.Add(
                CreateTimingGame("Tightrope Walking", EventDifficulty.Normal));
        }

        void AddMysteriousStrangerMiniGames(SpecialEvent strangerEvent)
        {
            strangerEvent.associatedMiniGames.Add(
                CreatePuzzleGame("Cryptic Riddles", EventDifficulty.Expert));
            strangerEvent.associatedMiniGames.Add(
                CreateNegotiationGame("Mysterious Bargain", EventDifficulty.Expert));
        }

        void AddBanditMiniGames(SpecialEvent banditEvent)
        {
            banditEvent.associatedMiniGames.Add(
                CreateCombatTrainingGame("Bandit Hunt", EventDifficulty.Normal));
            banditEvent.associatedMiniGames.Add(
                CreatePuzzleGame("Track the Bandits", EventDifficulty.Normal));
        }

        void AddNoblemenMiniGames(SpecialEvent nobleEvent)
        {
            nobleEvent.associatedMiniGames.Add(
                CreateNegotiationGame("Noble's Request", EventDifficulty.Normal));
            nobleEvent.associatedMiniGames.Add(
                CreatePuzzleGame("Court Etiquette", EventDifficulty.Normal));
        }

        void AddDisasterReliefMiniGames(SpecialEvent disasterEvent)
        {
            disasterEvent.associatedMiniGames.Add(
                CreateResourceGatheringGame("Emergency Supplies", EventDifficulty.Normal));
            disasterEvent.associatedMiniGames.Add(
                CreateTimingGame("Rescue Operations", EventDifficulty.Normal));
        }

        void AddTreasureHuntMiniGames(SpecialEvent treasureEvent)
        {
            treasureEvent.associatedMiniGames.Add(
                CreatePuzzleGame("Decipher Ancient Map", EventDifficulty.Hard));
            treasureEvent.associatedMiniGames.Add(
                CreateCombatTrainingGame("Guardian Monsters", EventDifficulty.Hard));
            treasureEvent.associatedMiniGames.Add(
                CreatePuzzleGame("Unlock Treasure Vault", EventDifficulty.Expert));
        }

        void AddMilestoneMiniGames(SpecialEvent milestoneEvent, string milestoneName)
        {
            // Add mini-games based on milestone type
            if (milestoneName.Contains("Level"))
            {
                milestoneEvent.associatedMiniGames.Add(
                    CreateCombatTrainingGame("Celebration Tournament", EventDifficulty.Normal));
            }
            
            milestoneEvent.associatedMiniGames.Add(
                CreateResourceGatheringGame("Celebration Preparations", EventDifficulty.Easy));
            milestoneEvent.associatedMiniGames.Add(
                CreateTimingGame("Fireworks Display", EventDifficulty.Easy));
        }

        void AddCharacterSpecificMiniGames(SpecialEvent charEvent, CharacterData character)
        {
            // Add mini-games based on character class
            switch (character.jobClass)
            {
                case JobClass.Warrior:
                case JobClass.Knight:
                    charEvent.associatedMiniGames.Add(
                        CreateCombatTrainingGame($"{character.name}'s Training", charEvent.difficulty));
                    break;
                    
                case JobClass.Mage:
                case JobClass.Priest:
                    charEvent.associatedMiniGames.Add(
                        CreatePuzzleGame($"{character.name}'s Research", charEvent.difficulty));
                    break;
                    
                case JobClass.Ranger:
                case JobClass.Assassin:
                    charEvent.associatedMiniGames.Add(
                        CreateTimingGame($"{character.name}'s Challenge", charEvent.difficulty));
                    break;
                    
                default:
                    charEvent.associatedMiniGames.Add(
                        CreateNegotiationGame($"{character.name}'s Request", charEvent.difficulty));
                    break;
            }
        }

        #endregion

        #region Public API

        public List<SpecialEvent> GetActiveEvents()
        {
            return new List<SpecialEvent>(activeEvents);
        }

        public List<MiniGame> GetActiveMiniGames()
        {
            return new List<MiniGame>(activeMiniGames);
        }

        public SpecialEvent GetEventById(string eventId)
        {
            return activeEvents.FirstOrDefault(e => e.id == eventId) ??
                   completedEvents.FirstOrDefault(e => e.id == eventId);
        }

        public MiniGame GetMiniGameById(string miniGameId)
        {
            return activeMiniGames.FirstOrDefault(m => m.id == miniGameId);
        }

        public void StartSpecificMiniGame(MiniGameType type, EventDifficulty difficulty)
        {
            MiniGame newMiniGame = null;
            
            switch (type)
            {
                case MiniGameType.ResourceGathering:
                    newMiniGame = CreateResourceGatheringGame("Quick Gathering", difficulty);
                    break;
                case MiniGameType.CombatTraining:
                    newMiniGame = CreateCombatTrainingGame("Combat Practice", difficulty);
                    break;
                case MiniGameType.Negotiation:
                    newMiniGame = CreateNegotiationGame("Quick Negotiation", difficulty);
                    break;
                case MiniGameType.PuzzleSolving:
                    newMiniGame = CreatePuzzleGame("Brain Teaser", difficulty);
                    break;
                case MiniGameType.TimingBased:
                    newMiniGame = CreateTimingGame("Reflex Test", difficulty);
                    break;
            }
            
            if (newMiniGame != null)
            {
                StartMiniGame(newMiniGame);
            }
        }

        public void ForceCompleteEvent(string eventId)
        {
            var activeEvent = activeEvents.FirstOrDefault(e => e.id == eventId);
            if (activeEvent != null)
            {
                CompleteEvent(activeEvent);
                activeEvents.Remove(activeEvent);
            }
        }

        public void ForceCompleteMiniGame(string miniGameId, bool success)
        {
            var miniGame = activeMiniGames.FirstOrDefault(m => m.id == miniGameId);
            if (miniGame != null)
            {
                miniGame.successRate = success ? 1f : 0f;
                CompleteMiniGame(miniGame, false);
            }
        }

        public float GetEventProgress(string eventId)
        {
            var activeEvent = activeEvents.FirstOrDefault(e => e.id == eventId);
            return activeEvent?.progress ?? 0f;
        }

        public int GetTotalEventParticipation()
        {
            return Mathf.RoundToInt(totalEventParticipation);
        }

        public Dictionary<MiniGameType, int> GetMiniGameStatistics()
        {
            return new Dictionary<MiniGameType, int>(miniGameCompletions);
        }

        public SeasonalEventType GetCurrentSeasonType()
        {
            return currentSeason;
        }

        public void ScheduleCustomEvent(SpecialEvent customEvent, float delay)
        {
            var schedule = new EventSchedule
            {
                eventId = customEvent.id,
                scheduledTime = Time.time + delay,
                eventType = customEvent.type,
                isRecurring = false
            };
            
            scheduledEvents.Enqueue(schedule);
        }

        #endregion
    }
}