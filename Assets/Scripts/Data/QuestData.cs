using UnityEngine;
using System;
using System.Collections.Generic;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class QuestData
    {
        [Header("기본 정보")]
        public string questId;
        public string questName;
        [TextArea(3, 5)]
        public string description;
        [TextArea(2, 4)]
        public string summary;
        
        // 호환성을 위한 속성들 (읽기/쓰기 가능)
        public string id { get => questId; set => questId = value; }
        public string name { get => questName; set => questName = value; }
        public string nameKey { get => questName; set => questName = value; }
        public int targetQuantity = 1; // 목표 수량
        public int targetValue { get => targetQuantity; set => targetQuantity = value; }
        public string titleKey = ""; // 로컬라이제이션 키
        public string descriptionKey = ""; // 로컬라이제이션 키
        
        [Header("퀘스트 타입")]
        public QuestType questType;
        public QuestCategory category;
        public QuestPriority priority = QuestPriority.Normal;
        
        [Header("진행 조건")]
        public int requiredLevel = 1;
        public List<string> prerequisiteQuests = new List<string>();
        public List<QuestRequirement> requirements = new List<QuestRequirement>();
        
        [Header("목표")]
        public List<QuestObjective> objectives = new List<QuestObjective>();
        public bool autoComplete = false;
        public bool trackProgress = true;
        
        [Header("보상")]
        public int experienceReward = 100;
        public int goldReward = 50;
        public List<QuestReward> rewards = new List<QuestReward>();
        public List<string> itemRewards = new List<string>();
        
        // 보상 호환성 속성들
        public int expReward { get => experienceReward; set => experienceReward = value; }
        public int rewardExp { get => experienceReward; set => experienceReward = value; }
        public int rewardGold { get => goldReward; set => goldReward = value; }
        public List<string> rewardItems { get => itemRewards; set => itemRewards = value; }
        
        [Header("시간 제한")]
        public bool hasTimeLimit = false;
        public float timeLimit = 3600f; // 초 단위
        public DateTime startTime;
        public DateTime deadline;
        
        [Header("위치")]
        public bool hasLocation = false;
        public Vector3 questLocation;
        public string locationName = "";
        public float locationRadius = 10f;
        
        [Header("NPC")]
        public string questGiverNPC = "";
        public string questCompleteNPC = "";
        public bool returnToGiver = true;
        
        [Header("상태")]
        public QuestState state = QuestState.Available;
        public float progress = 0f;
        public bool isRepeatable = false;
        public int repeatCount = 0;
        public int maxRepeats = 1;
        
        [Header("대화")]
        public string startDialogue = "";
        public string progressDialogue = "";
        public string completeDialogue = "";
        public string failDialogue = "";
        
        [Header("기타")]
        public bool isMainQuest = false;
        public bool isHidden = false;
        public bool showInJournal = true;
        public int sortOrder = 0;
        public string questChain = "";
        
        public QuestData()
        {
            questId = System.Guid.NewGuid().ToString();
            questName = "Unknown Quest";
            description = "";
            startTime = DateTime.Now;
        }
        
        public QuestData(string id, string name, QuestType type, QuestCategory cat)
        {
            questId = id;
            questName = name;
            questType = type;
            category = cat;
            startTime = DateTime.Now;
        }
        
        public bool CanStart(int playerLevel, List<string> completedQuests)
        {
            if (playerLevel < requiredLevel)
                return false;
                
            if (state != QuestState.Available)
                return false;
                
            foreach (string prerequisite in prerequisiteQuests)
            {
                if (!completedQuests.Contains(prerequisite))
                    return false;
            }
            
            foreach (var requirement in requirements)
            {
                if (!requirement.IsMet())
                    return false;
            }
            
            return true;
        }
        
        public void StartQuest()
        {
            state = QuestState.Active;
            startTime = DateTime.Now;
            
            if (hasTimeLimit)
            {
                deadline = startTime.AddSeconds(timeLimit);
            }
            
            foreach (var objective in objectives)
            {
                objective.currentProgress = 0;
                objective.isCompleted = false;
            }
        }
        
        public void UpdateProgress(string objectiveId, int amount = 1)
        {
            if (state != QuestState.Active)
                return;
                
            var objective = objectives.Find(o => o.objectiveId == objectiveId);
            if (objective != null)
            {
                objective.currentProgress = Mathf.Min(objective.targetAmount, objective.currentProgress + amount);
                objective.isCompleted = objective.currentProgress >= objective.targetAmount;
                
                CheckCompletion();
            }
        }
        
        public void CheckCompletion()
        {
            if (state != QuestState.Active)
                return;
                
            bool allCompleted = true;
            int completedObjectives = 0;
            
            foreach (var objective in objectives)
            {
                if (objective.isCompleted)
                    completedObjectives++;
                else
                    allCompleted = false;
            }
            
            progress = objectives.Count > 0 ? (float)completedObjectives / objectives.Count : 1f;
            
            if (allCompleted)
            {
                if (autoComplete)
                {
                    CompleteQuest();
                }
                else
                {
                    state = QuestState.ReadyToComplete;
                }
            }
            
            // 시간 제한 확인
            if (hasTimeLimit && DateTime.Now > deadline)
            {
                FailQuest();
            }
        }
        
        public void CompleteQuest()
        {
            if (state != QuestState.Active && state != QuestState.ReadyToComplete)
                return;
                
            state = QuestState.Completed;
            
            if (isRepeatable && repeatCount < maxRepeats)
            {
                repeatCount++;
                // 반복 퀘스트면 일정 시간 후 다시 사용 가능하게 설정 가능
            }
        }
        
        public void FailQuest()
        {
            state = QuestState.Failed;
        }
        
        public void AbandonQuest()
        {
            state = QuestState.Abandoned;
        }
        
        public List<QuestReward> GetRewards()
        {
            List<QuestReward> allRewards = new List<QuestReward>(rewards);
            
            if (experienceReward > 0)
            {
                allRewards.Add(new QuestReward
                {
                    rewardType = QuestRewardType.Experience,
                    amount = experienceReward
                });
            }
            
            if (goldReward > 0)
            {
                allRewards.Add(new QuestReward
                {
                    rewardType = QuestRewardType.Gold,
                    amount = goldReward
                });
            }
            
            foreach (string itemId in itemRewards)
            {
                allRewards.Add(new QuestReward
                {
                    rewardType = QuestRewardType.Item,
                    itemId = itemId,
                    amount = 1
                });
            }
            
            return allRewards;
        }
        
        public string GetQuestInfo()
        {
            string info = $"<b>{questName}</b>\n";
            info += $"타입: {GetQuestTypeName()}\n";
            info += $"우선순위: {GetPriorityName()}\n";
            
            if (!string.IsNullOrEmpty(description))
                info += $"\n{description}\n";
            
            info += $"\n<b>목표:</b>\n";
            foreach (var objective in objectives)
            {
                string status = objective.isCompleted ? "✓" : "○";
                info += $"{status} {objective.description} ({objective.currentProgress}/{objective.targetAmount})\n";
            }
            
            if (hasTimeLimit)
            {
                var timeLeft = deadline - DateTime.Now;
                if (timeLeft.TotalSeconds > 0)
                {
                    info += $"\n남은 시간: {timeLeft.TotalMinutes:F0}분\n";
                }
                else
                {
                    info += "\n<color=red>시간 초과</color>\n";
                }
            }
            
            info += $"\n<b>보상:</b>\n";
            if (experienceReward > 0) info += $"경험치: {experienceReward}\n";
            if (goldReward > 0) info += $"골드: {goldReward}\n";
            if (itemRewards.Count > 0) info += $"아이템: {itemRewards.Count}개\n";
            
            return info;
        }
        
        private string GetQuestTypeName()
        {
            return questType switch
            {
                QuestType.Main => "메인",
                QuestType.Side => "사이드",
                QuestType.Daily => "일일",
                QuestType.Weekly => "주간",
                QuestType.Event => "이벤트",
                QuestType.Guild => "길드",
                _ => "일반"
            };
        }
        
        private string GetPriorityName()
        {
            return priority switch
            {
                QuestPriority.Low => "낮음",
                QuestPriority.Normal => "보통",
                QuestPriority.High => "높음",
                QuestPriority.Urgent => "긴급",
                _ => "보통"
            };
        }
    }
    
    [System.Serializable]
    public class QuestObjective
    {
        public string objectiveId;
        public string description;
        public QuestObjectiveType type;
        public string targetId;
        public int targetAmount = 1;
        public int currentProgress = 0;
        public bool isCompleted = false;
        public bool isOptional = false;
        
        public float GetProgressPercentage()
        {
            return targetAmount > 0 ? (float)currentProgress / targetAmount : 0f;
        }
    }
    
    [System.Serializable]
    public class QuestRequirement
    {
        public QuestRequirementType type;
        public string targetId;
        public int amount;
        public string description;
        
        public bool IsMet()
        {
            // 실제 게임에서는 플레이어 상태를 확인하여 구현
            return true;
        }
    }
    
    [System.Serializable]
    public class QuestReward
    {
        public QuestRewardType rewardType;
        public string itemId;
        public int amount;
        public string description;
    }
    
    public enum QuestType
    {
        Main,
        Side,
        Daily,
        Weekly,
        Event,
        Guild,
        Tutorial
    }
    
    public enum QuestCategory
    {
        Combat,
        Collection,
        Delivery,
        Exploration,
        Social,
        Crafting,
        Building,
        Story
    }
    
    public enum QuestPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }
    
    public enum QuestState
    {
        Available,
        Locked,
        Active,
        ReadyToComplete,
        Completed,
        Failed,
        Abandoned
    }
    
    public enum QuestObjectiveType
    {
        Kill,
        Collect,
        Deliver,
        Talk,
        Reach,
        Use,
        Craft,
        Build,
        Survive,
        Protect
    }
    
    public enum QuestRequirementType
    {
        Level,
        Item,
        Skill,
        Quest,
        Building,
        Achievement
    }
    
    public enum QuestRewardType
    {
        Experience,
        Gold,
        Item,
        Skill,
        Building,
        Title
    }
} 