using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle; // JobClass를 위해 추가

namespace GuildMaster.Systems
{
    public enum StoryChapterType
    {
        Main,           // 메인 스토리
        Side,           // 사이드 스토리
        Character,      // 캐릭터 스토리
        Hidden          // 숨겨진 스토리
    }
    
    public enum StoryEnding
    {
        None,
        HeroicVictory,      // 영웅적 승리
        PeacefulResolution, // 평화로운 해결
        BitterSweet,        // 씁쓸한 승리
        TragicDefeat,       // 비극적 패배
        TrueEnding,         // 진엔딩
        SecretEnding        // 시크릿 엔딩
    }
    
    [System.Serializable]
    public class StoryChoice
    {
        public string ChoiceId { get; set; }
        public string Text { get; set; }
        public string NextNodeId { get; set; }
        public Dictionary<string, int> Requirements { get; set; } // stat requirements
        public Dictionary<string, int> Effects { get; set; } // reputation, resources, etc.
        
        public StoryChoice(string id, string text, string nextNode)
        {
            ChoiceId = id;
            Text = text;
            NextNodeId = nextNode;
            Requirements = new Dictionary<string, int>();
            Effects = new Dictionary<string, int>();
        }
        
        public bool CanChoose(Core.GameManager gameManager)
        {
            if (gameManager == null) return true;
            
            foreach (var req in Requirements)
            {
                switch (req.Key)
                {
                    case "guild_level":
                        if (gameManager.GuildManager.GetGuildData().GuildLevel < req.Value)
                            return false;
                        break;
                    case "reputation":
                        if (gameManager.GuildManager.GetGuildData().GuildReputation < req.Value)
                            return false;
                        break;
                    case "gold":
                        if (gameManager.ResourceManager.GetGold() < req.Value)
                            return false;
                        break;
                }
            }
            
            return true;
        }
    }
    
    [System.Serializable]
    public class StoryNode
    {
        public string NodeId { get; set; }
        public string Speaker { get; set; }
        public string DialogueText { get; set; }
        public List<StoryChoice> Choices { get; set; }
        public string NextNodeId { get; set; } // For linear progression
        public bool IsEndNode { get; set; }
        public StoryEnding? EndingType { get; set; }
        
        // Special events
        public string BattleId { get; set; }
        public string RewardId { get; set; }
        public string UnlockId { get; set; }
        
        // Node effects
        public Dictionary<string, int> Effects { get; set; }
        
        public StoryNode(string id)
        {
            NodeId = id;
            Choices = new List<StoryChoice>();
            Effects = new Dictionary<string, int>();
            IsEndNode = false;
        }
    }
    
    [System.Serializable]
    public class StoryChapter
    {
        public string ChapterId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public StoryChapterType Type { get; set; }
        public int ChapterNumber { get; set; }
        public bool IsUnlocked { get; set; }
        public bool IsCompleted { get; set; }
        
        // Story flow
        public Dictionary<string, StoryNode> Nodes { get; set; }
        public string StartNodeId { get; set; }
        public List<string> CompletedNodes { get; set; }
        
        // Requirements
        public int RequiredGuildLevel { get; set; }
        public List<string> RequiredChapters { get; set; }
        
        // Tracking
        public Dictionary<string, int> ChoicesMade { get; set; }
        public int PlaythroughCount { get; set; }
        public StoryEnding AchievedEnding { get; set; }
        
        public StoryChapter(string id, string title, StoryChapterType type, int number)
        {
            ChapterId = id;
            Title = title;
            Type = type;
            ChapterNumber = number;
            Nodes = new Dictionary<string, StoryNode>();
            CompletedNodes = new List<string>();
            RequiredChapters = new List<string>();
            ChoicesMade = new Dictionary<string, int>();
            AchievedEnding = StoryEnding.None;
        }
    }
    
    public class StoryManager : MonoBehaviour
    {
        // Story data
        private Dictionary<string, StoryChapter> allChapters;
        private StoryChapter currentChapter;
        private StoryNode currentNode;
        
        // Progress tracking
        private Dictionary<string, int> globalChoiceTracking;
        private List<StoryEnding> unlockedEndings;
        private int totalChoicesMade;
        
        // Auto play settings
        private bool isAutoPlay = false;
        private float autoPlaySpeed = 2f;
        private float autoPlayTimer = 0f;
        
        // Events
        public event Action<StoryChapter> OnChapterStarted;
        public event Action<StoryChapter, StoryEnding> OnChapterCompleted;
        public event Action<StoryNode> OnNodeStarted;
        public event Action<StoryChoice> OnChoiceMade;
        public event Action<StoryEnding> OnEndingUnlocked;
        public event Action<string, string> OnDialogueDisplay;
        
        void Awake()
        {
            allChapters = new Dictionary<string, StoryChapter>();
            globalChoiceTracking = new Dictionary<string, int>();
            unlockedEndings = new List<StoryEnding>();
            
            InitializeStoryContent();
        }
        
        void InitializeStoryContent()
        {
            // Create main story chapters
            CreateMainStoryChapters();
            
            // Create side stories
            CreateSideStories();
            
            // Create character stories
            CreateCharacterStories();
            
            // Create hidden stories
            CreateHiddenStories();
        }
        
        void CreateMainStoryChapters()
        {
            // Chapter 1: The Beginning
            var chapter1 = new StoryChapter("main_1", "길드의 시작", StoryChapterType.Main, 1)
            {
                Description = "작은 길드를 물려받은 당신의 이야기가 시작됩니다.",
                RequiredGuildLevel = 1,
                IsUnlocked = true
            };
            
            // Create nodes for chapter 1
            var node1 = new StoryNode("main_1_start")
            {
                Speaker = "노련한 모험가",
                DialogueText = "드디어 길드를 물려받으셨군요. 이제부터가 진짜 시작입니다. 어떤 길드를 만들어 나가실 건가요?",
                NextNodeId = "main_1_choice"
            };
            
            var node2 = new StoryNode("main_1_choice")
            {
                Speaker = "시스템",
                DialogueText = "당신의 길드 운영 방침을 선택하세요."
            };
            
            // Add choices
            node2.Choices.Add(new StoryChoice("choice_military", "군사력 중심의 강력한 길드", "main_1_military"));
            node2.Choices.Add(new StoryChoice("choice_trade", "경제력 중심의 부유한 길드", "main_1_trade"));
            node2.Choices.Add(new StoryChoice("choice_balanced", "균형 잡힌 명예로운 길드", "main_1_balanced"));
            
            // Military path
            var nodeMilitary = new StoryNode("main_1_military")
            {
                Speaker = "노련한 모험가",
                DialogueText = "강력한 군사력이야말로 이 세계에서 살아남는 방법이죠. 현명한 선택입니다.",
                NextNodeId = "main_1_end"
            };
            
            // Trade path
            var nodeTrade = new StoryNode("main_1_trade")
            {
                Speaker = "노련한 모험가",
                DialogueText = "돈이 곧 힘입니다. 부유한 길드는 많은 것을 할 수 있죠.",
                NextNodeId = "main_1_end"
            };
            
            // Balanced path
            var nodeBalanced = new StoryNode("main_1_balanced")
            {
                Speaker = "노련한 모험가",
                DialogueText = "균형이야말로 진정한 힘입니다. 모든 면에서 성장하는 길드가 되겠군요.",
                NextNodeId = "main_1_end"
            };
            
            // End node
            var nodeEnd = new StoryNode("main_1_end")
            {
                Speaker = "노련한 모험가",
                DialogueText = "자, 이제 길드 운영을 시작해봅시다. 행운을 빕니다!",
                IsEndNode = true,
                RewardId = "chapter1_completion"
            };
            
            // Add nodes to chapter
            chapter1.Nodes["main_1_start"] = node1;
            chapter1.Nodes["main_1_choice"] = node2;
            chapter1.Nodes["main_1_military"] = nodeMilitary;
            chapter1.Nodes["main_1_trade"] = nodeTrade;
            chapter1.Nodes["main_1_balanced"] = nodeBalanced;
            chapter1.Nodes["main_1_end"] = nodeEnd;
            chapter1.StartNodeId = "main_1_start";
            
            allChapters[chapter1.ChapterId] = chapter1;
            
            // Chapter 2: Rising Threats
            var chapter2 = new StoryChapter("main_2", "다가오는 위협", StoryChapterType.Main, 2)
            {
                Description = "평화로운 대륙에 어둠의 그림자가 드리우기 시작합니다.",
                RequiredGuildLevel = 5
            };
            chapter2.RequiredChapters.Add("main_1");
            
            CreateChapter2Nodes(chapter2);
            allChapters[chapter2.ChapterId] = chapter2;
            
            // Chapter 3: Alliance or War
            var chapter3 = new StoryChapter("main_3", "동맹 혹은 전쟁", StoryChapterType.Main, 3)
            {
                Description = "다른 길드들과의 관계가 중요해지는 시기입니다.",
                RequiredGuildLevel = 10
            };
            chapter3.RequiredChapters.Add("main_2");
            
            CreateChapter3Nodes(chapter3);
            allChapters[chapter3.ChapterId] = chapter3;
            
            // Final Chapter: Destiny
            var chapterFinal = new StoryChapter("main_final", "운명의 갈림길", StoryChapterType.Main, 10)
            {
                Description = "길드의 운명이 결정되는 순간입니다.",
                RequiredGuildLevel = 20
            };
            chapterFinal.RequiredChapters.Add("main_3");
            
            CreateFinalChapterNodes(chapterFinal);
            allChapters[chapterFinal.ChapterId] = chapterFinal;
        }
        
        void CreateChapter2Nodes(StoryChapter chapter)
        {
            // Chapter 2 focuses on emerging threats
            var startNode = new StoryNode("main_2_start")
            {
                Speaker = "정찰병",
                DialogueText = "길드장님! 북쪽 국경에서 이상한 움직임이 포착되었습니다. 정체불명의 군대가 모이고 있다고 합니다.",
                NextNodeId = "main_2_investigate"
            };
            
            var investigateNode = new StoryNode("main_2_investigate")
            {
                Speaker = "시스템",
                DialogueText = "어떻게 대응하시겠습니까?"
            };
            
            investigateNode.Choices.Add(new StoryChoice("send_scouts", "정찰대를 파견한다", "main_2_scout_result")
            {
                Requirements = { { "gold", 500 } }
            });
            
            investigateNode.Choices.Add(new StoryChoice("prepare_defense", "방어 준비를 강화한다", "main_2_defense_result"));
            
            investigateNode.Choices.Add(new StoryChoice("seek_alliance", "다른 길드와 정보를 공유한다", "main_2_alliance_result")
            {
                Requirements = { { "reputation", 100 } }
            });
            
            // Different paths lead to different outcomes
            var scoutResult = new StoryNode("main_2_scout_result")
            {
                Speaker = "정찰병",
                DialogueText = "고대의 봉인이 풀린 것 같습니다. 어둠의 군단이 깨어나고 있습니다!",
                NextNodeId = "main_2_battle_prep",
                UnlockId = "ancient_threat_discovered"
            };
            
            var defenseResult = new StoryNode("main_2_defense_result")
            {
                Speaker = "방어대장",
                DialogueText = "방어선은 준비되었지만, 적의 정체를 모른 채 싸우는 것은 위험합니다.",
                NextNodeId = "main_2_battle_prep"
            };
            
            var allianceResult = new StoryNode("main_2_alliance_result")
            {
                Speaker = "동맹 길드 대표",
                DialogueText = "우리도 같은 위협을 감지했습니다. 함께 대응한다면 승산이 있을 것입니다.",
                NextNodeId = "main_2_battle_prep"
            };
            allianceResult.Effects = new Dictionary<string, int> { { "ally_guild", 1 } };
            
            var battlePrep = new StoryNode("main_2_battle_prep")
            {
                Speaker = "시스템",
                DialogueText = "첫 번째 대규모 전투가 다가옵니다. 준비하세요!",
                IsEndNode = true,
                BattleId = "story_battle_darkness_vanguard"
            };
            
            chapter.Nodes["main_2_start"] = startNode;
            chapter.Nodes["main_2_investigate"] = investigateNode;
            chapter.Nodes["main_2_scout_result"] = scoutResult;
            chapter.Nodes["main_2_defense_result"] = defenseResult;
            chapter.Nodes["main_2_alliance_result"] = allianceResult;
            chapter.Nodes["main_2_battle_prep"] = battlePrep;
            chapter.StartNodeId = "main_2_start";
        }
        
        void CreateChapter3Nodes(StoryChapter chapter)
        {
            // Chapter 3 focuses on alliances and conflicts
            var startNode = new StoryNode("main_3_start")
            {
                Speaker = "길드 고문관",
                DialogueText = "어둠의 위협이 커지면서 대륙의 길드들이 두 진영으로 나뉘고 있습니다. 우리도 선택을 해야 합니다.",
                NextNodeId = "main_3_faction_choice"
            };
            
            var factionChoice = new StoryNode("main_3_faction_choice")
            {
                Speaker = "시스템",
                DialogueText = "어느 진영에 합류하시겠습니까?"
            };
            
            factionChoice.Choices.Add(new StoryChoice("join_alliance", "정의의 동맹에 가입", "main_3_alliance_path"));
            factionChoice.Choices[factionChoice.Choices.Count - 1].Effects = new Dictionary<string, int> { { "faction", 1 } };
            
            factionChoice.Choices.Add(new StoryChoice("join_empire", "철의 제국에 가입", "main_3_empire_path"));
            factionChoice.Choices[factionChoice.Choices.Count - 1].Effects = new Dictionary<string, int> { { "faction", 2 } };
            
            factionChoice.Choices.Add(new StoryChoice("remain_neutral", "중립을 유지", "main_3_neutral_path")
            {
                Requirements = { { "guild_level", 15 } }
            });
            
            // Alliance path
            var alliancePath = new StoryNode("main_3_alliance_path")
            {
                Speaker = "동맹 지도자",
                DialogueText = "환영합니다. 함께라면 어둠을 물리칠 수 있을 것입니다.",
                NextNodeId = "main_3_major_battle"
            };
            alliancePath.Effects = new Dictionary<string, int> { { "reputation", 200 } };
            
            // Empire path
            var empirePath = new StoryNode("main_3_empire_path")
            {
                Speaker = "제국 사령관",
                DialogueText = "현명한 선택이오. 제국의 힘으로 이 혼란을 종식시킬 것이오.",
                NextNodeId = "main_3_major_battle"
            };
            empirePath.Effects = new Dictionary<string, int> { { "gold", 5000 } };
            
            // Neutral path
            var neutralPath = new StoryNode("main_3_neutral_path")
            {
                Speaker = "길드 고문관",
                DialogueText = "위험한 선택이지만, 우리만의 길을 가는 것도 하나의 방법입니다.",
                NextNodeId = "main_3_major_battle"
            };
            neutralPath.Effects = new Dictionary<string, int> { { "reputation", -100 }, { "independence", 1 } };
            
            var majorBattle = new StoryNode("main_3_major_battle")
            {
                Speaker = "시스템",
                DialogueText = "대륙의 운명을 결정할 대전투가 시작됩니다!",
                IsEndNode = true,
                BattleId = "story_battle_faction_war"
            };
            
            chapter.Nodes["main_3_start"] = startNode;
            chapter.Nodes["main_3_faction_choice"] = factionChoice;
            chapter.Nodes["main_3_alliance_path"] = alliancePath;
            chapter.Nodes["main_3_empire_path"] = empirePath;
            chapter.Nodes["main_3_neutral_path"] = neutralPath;
            chapter.Nodes["main_3_major_battle"] = majorBattle;
            chapter.StartNodeId = "main_3_start";
        }
        
        void CreateFinalChapterNodes(StoryChapter chapter)
        {
            // Final chapter with multiple endings
            var startNode = new StoryNode("main_final_start")
            {
                Speaker = "운명의 목소리",
                DialogueText = "모든 것이 이 순간으로 수렴합니다. 당신의 선택이 세계의 운명을 결정할 것입니다.",
                NextNodeId = "main_final_revelation"
            };
            
            var revelationNode = new StoryNode("main_final_revelation")
            {
                Speaker = "고대의 현자",
                DialogueText = "어둠의 근원은 사실 고대의 저주였습니다. 이를 해결할 방법은 세 가지가 있습니다.",
                NextNodeId = "main_final_choice"
            };
            
            var finalChoice = new StoryNode("main_final_choice")
            {
                Speaker = "시스템",
                DialogueText = "최종 선택의 시간입니다."
            };
            
            // Different ending paths based on previous choices and current state
            finalChoice.Choices.Add(new StoryChoice("heroic_sacrifice", "희생을 통한 봉인", "ending_heroic")
            {
                Requirements = { { "reputation", 1000 } }
            });
            
            finalChoice.Choices.Add(new StoryChoice("diplomatic_solution", "화합을 통한 정화", "ending_peaceful")
            {
                Requirements = { { "ally_guilds", 3 } }
            });
            
            finalChoice.Choices.Add(new StoryChoice("power_absorption", "어둠의 힘을 흡수", "ending_dark")
            {
                Requirements = { { "guild_level", 25 } }
            });
            
            finalChoice.Choices.Add(new StoryChoice("true_understanding", "진실을 깨달음", "ending_true")
            {
                Requirements = { { "all_chapters_completed", 1 }, { "hidden_knowledge", 1 } }
            });
            
            // Heroic ending
            var heroicEnding = new StoryNode("ending_heroic")
            {
                Speaker = "역사의 기록자",
                DialogueText = "당신의 희생으로 세계는 구원받았습니다. 영웅의 이름은 영원히 기억될 것입니다.",
                IsEndNode = true,
                EndingType = StoryEnding.HeroicVictory
            };
            
            // Peaceful ending
            var peacefulEnding = new StoryNode("ending_peaceful")
            {
                Speaker = "평화의 사자",
                DialogueText = "모든 길드가 하나 되어 어둠을 정화했습니다. 새로운 시대가 열렸습니다.",
                IsEndNode = true,
                EndingType = StoryEnding.PeacefulResolution
            };
            
            // Dark ending
            var darkEnding = new StoryNode("ending_dark")
            {
                Speaker = "어둠의 목소리",
                DialogueText = "당신은 어둠을 받아들였고, 새로운 암흑시대의 지배자가 되었습니다.",
                IsEndNode = true,
                EndingType = StoryEnding.BitterSweet
            };
            
            // True ending
            var trueEnding = new StoryNode("ending_true")
            {
                Speaker = "진실의 수호자",
                DialogueText = "모든 것은 순환이었습니다. 당신은 진정한 균형을 이루어냈습니다.",
                IsEndNode = true,
                EndingType = StoryEnding.TrueEnding
            };
            
            chapter.Nodes["main_final_start"] = startNode;
            chapter.Nodes["main_final_revelation"] = revelationNode;
            chapter.Nodes["main_final_choice"] = finalChoice;
            chapter.Nodes["ending_heroic"] = heroicEnding;
            chapter.Nodes["ending_peaceful"] = peacefulEnding;
            chapter.Nodes["ending_dark"] = darkEnding;
            chapter.Nodes["ending_true"] = trueEnding;
            chapter.StartNodeId = "main_final_start";
        }
        
        void CreateSideStories()
        {
            // Side story: The Lost Artifact
            var sideStory1 = new StoryChapter("side_artifact", "잃어버린 유물", StoryChapterType.Side, 1)
            {
                Description = "고대의 유물을 찾는 특별한 임무",
                RequiredGuildLevel = 7
            };
            
            // Add nodes for side story
            // ... (similar structure to main story but shorter)
            
            allChapters[sideStory1.ChapterId] = sideStory1;
        }
        
        void CreateCharacterStories()
        {
            // Character story for each job class
            foreach (JobClass jobClass in Enum.GetValues(typeof(JobClass)))
            {
                var charStory = new StoryChapter($"char_{jobClass}", $"{jobClass}의 이야기", StoryChapterType.Character, (int)jobClass)
                {
                    Description = $"{jobClass} 클래스의 특별한 이야기",
                    RequiredGuildLevel = 5
                };
                
                // Add character-specific nodes
                // ... (focusing on that class's background and special missions)
                
                allChapters[charStory.ChapterId] = charStory;
            }
        }
        
        void CreateHiddenStories()
        {
            // Hidden story that unlocks secret ending
            var hiddenStory = new StoryChapter("hidden_truth", "숨겨진 진실", StoryChapterType.Hidden, 1)
            {
                Description = "모든 비밀이 밝혀지는 이야기",
                RequiredGuildLevel = 20,
                IsUnlocked = false // Requires special conditions
            };
            
            // This story reveals the true nature of the darkness and unlocks the true ending
            // ... (add nodes that reveal hidden lore)
            
            allChapters[hiddenStory.ChapterId] = hiddenStory;
        }
        
        void Update()
        {
            if (isAutoPlay && currentNode != null)
            {
                autoPlayTimer += Time.deltaTime;
                if (autoPlayTimer >= autoPlaySpeed)
                {
                    autoPlayTimer = 0f;
                    AdvanceStory();
                }
            }
        }
        
        public bool StartChapter(string chapterId)
        {
            if (!allChapters.ContainsKey(chapterId)) return false;
            
            var chapter = allChapters[chapterId];
            if (!CanStartChapter(chapter)) return false;
            
            currentChapter = chapter;
            currentNode = chapter.Nodes[chapter.StartNodeId];
            
            OnChapterStarted?.Invoke(chapter);
            DisplayCurrentNode();
            
            return true;
        }
        
        bool CanStartChapter(StoryChapter chapter)
        {
            if (!chapter.IsUnlocked) return false;
            
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return false;
            
            if (gameManager.GuildManager.GetGuildData().GuildLevel < chapter.RequiredGuildLevel)
                return false;
            
            foreach (var requiredChapter in chapter.RequiredChapters)
            {
                if (!allChapters.ContainsKey(requiredChapter) || !allChapters[requiredChapter].IsCompleted)
                    return false;
            }
            
            return true;
        }
        
        void DisplayCurrentNode()
        {
            if (currentNode == null) return;
            
            OnNodeStarted?.Invoke(currentNode);
            OnDialogueDisplay?.Invoke(currentNode.Speaker, currentNode.DialogueText);
            
            // Apply node effects
            ApplyNodeEffects(currentNode);
            
            // Handle special events
            if (!string.IsNullOrEmpty(currentNode.BattleId))
            {
                StartStoryBattle(currentNode.BattleId);
            }
            
            if (!string.IsNullOrEmpty(currentNode.RewardId))
            {
                GiveStoryReward(currentNode.RewardId);
            }
            
            if (!string.IsNullOrEmpty(currentNode.UnlockId))
            {
                UnlockContent(currentNode.UnlockId);
            }
        }
        
        void ApplyNodeEffects(StoryNode node)
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null || node.Effects == null) return;
            
            foreach (var effect in node.Effects)
            {
                switch (effect.Key)
                {
                    case "reputation":
                        gameManager.GuildManager.AddReputation(effect.Value);
                        break;
                    case "gold":
                        gameManager.ResourceManager.AddGold(effect.Value);
                        break;
                    case "faction":
                        PlayerPrefs.SetInt("story_faction", effect.Value);
                        break;
                    case "ally_guild":
                        PlayerPrefs.SetInt("ally_guilds", PlayerPrefs.GetInt("ally_guilds", 0) + effect.Value);
                        break;
                    case "independence":
                        PlayerPrefs.SetInt("independence", effect.Value);
                        break;
                }
            }
        }
        
        public void MakeChoice(int choiceIndex)
        {
            if (currentNode == null || choiceIndex >= currentNode.Choices.Count) return;
            
            var choice = currentNode.Choices[choiceIndex];
            var gameManager = Core.GameManager.Instance;
            
            if (!choice.CanChoose(gameManager)) return;
            
            // Apply effects
            ApplyChoiceEffects(choice);
            
            // Track choice
            string choiceKey = $"{currentChapter.ChapterId}_{currentNode.NodeId}_{choice.ChoiceId}";
            if (!globalChoiceTracking.ContainsKey(choiceKey))
                globalChoiceTracking[choiceKey] = 0;
            globalChoiceTracking[choiceKey]++;
            totalChoicesMade++;
            
            currentChapter.ChoicesMade[choice.ChoiceId] = currentChapter.ChoicesMade.GetValueOrDefault(choice.ChoiceId, 0) + 1;
            
            OnChoiceMade?.Invoke(choice);
            
            // Move to next node
            if (currentChapter.Nodes.ContainsKey(choice.NextNodeId))
            {
                currentNode = currentChapter.Nodes[choice.NextNodeId];
                currentChapter.CompletedNodes.Add(currentNode.NodeId);
                DisplayCurrentNode();
            }
        }
        
        void ApplyChoiceEffects(StoryChoice choice)
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return;
            
            foreach (var effect in choice.Effects)
            {
                switch (effect.Key)
                {
                    case "reputation":
                        gameManager.GuildManager.AddReputation(effect.Value);
                        break;
                    case "gold":
                        gameManager.ResourceManager.AddGold(effect.Value);
                        break;
                    case "faction":
                        PlayerPrefs.SetInt("story_faction", effect.Value);
                        break;
                    case "ally_guild":
                        PlayerPrefs.SetInt("ally_guilds", PlayerPrefs.GetInt("ally_guilds", 0) + effect.Value);
                        break;
                }
            }
        }
        
        public void AdvanceStory()
        {
            if (currentNode == null) return;
            
            // If node has choices, can't auto-advance
            if (currentNode.Choices.Count > 0) return;
            
            // If it's an end node, complete the chapter
            if (currentNode.IsEndNode)
            {
                CompleteChapter();
                return;
            }
            
            // Move to next node
            if (!string.IsNullOrEmpty(currentNode.NextNodeId) && currentChapter.Nodes.ContainsKey(currentNode.NextNodeId))
            {
                currentNode = currentChapter.Nodes[currentNode.NextNodeId];
                currentChapter.CompletedNodes.Add(currentNode.NodeId);
                DisplayCurrentNode();
            }
        }
        
        void CompleteChapter()
        {
            if (currentChapter == null) return;
            
            currentChapter.IsCompleted = true;
            currentChapter.PlaythroughCount++;
            
            if (currentNode != null && currentNode.EndingType.HasValue)
            {
                currentChapter.AchievedEnding = currentNode.EndingType.Value;
                
                if (!unlockedEndings.Contains(currentNode.EndingType.Value))
                {
                    unlockedEndings.Add(currentNode.EndingType.Value);
                    OnEndingUnlocked?.Invoke(currentNode.EndingType.Value);
                }
            }
            
            OnChapterCompleted?.Invoke(currentChapter, currentChapter.AchievedEnding);
            
            // Check for unlocks
            CheckForUnlocks();
            
            currentChapter = null;
            currentNode = null;
        }
        
        void CheckForUnlocks()
        {
            // Check if hidden stories should be unlocked
            if (GetCompletedMainChapterCount() >= 3 && globalChoiceTracking.Count > 20)
            {
                if (allChapters.ContainsKey("hidden_truth"))
                {
                    allChapters["hidden_truth"].IsUnlocked = true;
                }
            }
            
            // Check for true ending requirements
            if (unlockedEndings.Count >= 3)
            {
                PlayerPrefs.SetInt("hidden_knowledge", 1);
            }
            
            if (GetCompletedChapterCount() == allChapters.Count - 1) // All but final
            {
                PlayerPrefs.SetInt("all_chapters_completed", 1);
            }
        }
        
        void StartStoryBattle(string battleId)
        {
            // TODO: Integrate with battle system
            Debug.Log($"Starting story battle: {battleId}");
        }
        
        void GiveStoryReward(string rewardId)
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return;
            
            switch (rewardId)
            {
                case "chapter1_completion":
                    gameManager.ResourceManager.AddGold(1000);
                    gameManager.GuildManager.AddReputation(100);
                    break;
                // Add more rewards
            }
        }
        
        void UnlockContent(string unlockId)
        {
            switch (unlockId)
            {
                case "ancient_threat_discovered":
                    // Unlock new dungeon or enemy type
                    break;
                // Add more unlocks
            }
        }
        
        public void SetAutoPlay(bool enabled, float speed = 2f)
        {
            isAutoPlay = enabled;
            autoPlaySpeed = speed;
            autoPlayTimer = 0f;
        }
        
        public void SkipCurrentDialogue()
        {
            if (currentNode != null && !currentNode.IsEndNode && currentNode.Choices.Count == 0)
            {
                AdvanceStory();
            }
        }
        
        // Getters
        public List<StoryChapter> GetAvailableChapters()
        {
            return allChapters.Values.Where(c => c.IsUnlocked && !c.IsCompleted).ToList();
        }
        
        public List<StoryChapter> GetCompletedChapters()
        {
            return allChapters.Values.Where(c => c.IsCompleted).ToList();
        }
        
        public int GetCompletedChapterCount()
        {
            return allChapters.Values.Count(c => c.IsCompleted);
        }
        
        public int GetCompletedMainChapterCount()
        {
            return allChapters.Values.Count(c => c.IsCompleted && c.Type == StoryChapterType.Main);
        }
        
        public List<StoryEnding> GetUnlockedEndings()
        {
            return new List<StoryEnding>(unlockedEndings);
        }
        
        public StoryChapter GetCurrentChapter()
        {
            return currentChapter;
        }
        
        public StoryNode GetCurrentNode()
        {
            return currentNode;
        }
        
        public float GetStoryCompletionPercentage()
        {
            int totalNodes = allChapters.Values.Sum(c => c.Nodes.Count);
            int completedNodes = allChapters.Values.Sum(c => c.CompletedNodes.Count);
            
            return totalNodes > 0 ? (float)completedNodes / totalNodes * 100f : 0f;
        }
    }
}