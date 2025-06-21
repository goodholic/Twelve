using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace pjy.Story
{
    /// <summary>
    /// 스토리 데이터베이스
    /// - 모든 스토리 데이터 관리
    /// - 조건 기반 스토리 검색
    /// - CSV 연동 지원
    /// </summary>
    [CreateAssetMenu(fileName = "StoryDatabase", menuName = "pjy/Story/StoryDatabase")]
    public class StoryDatabase : ScriptableObject
    {
        [Header("스토리 목록")]
        [SerializeField] private List<StoryData> stories = new List<StoryData>();
        
        [Header("메인 스토리")]
        [SerializeField] private List<StoryData> mainStories = new List<StoryData>();
        
        [Header("이벤트 스토리")]
        [SerializeField] private List<StoryData> eventStories = new List<StoryData>();
        
        [Header("캐릭터 스토리")]
        [SerializeField] private List<StoryData> characterStories = new List<StoryData>();

        /// <summary>
        /// 스토리 ID로 스토리 가져오기
        /// </summary>
        public StoryData GetStory(string storyId)
        {
            return stories.FirstOrDefault(s => s.storyId == storyId);
        }

        /// <summary>
        /// 모든 스토리 가져오기
        /// </summary>
        public List<StoryData> GetAllStories()
        {
            return new List<StoryData>(stories);
        }

        /// <summary>
        /// 이벤트 스토리 가져오기
        /// </summary>
        public List<StoryData> GetEventStories(string eventType, int eventValue)
        {
            return eventStories.Where(s => 
                s.triggerConditions.Any(c => 
                    c.eventType == eventType && 
                    c.eventValue == eventValue
                )
            ).ToList();
        }
        
        /// <summary>
        /// 메인 스토리 가져오기
        /// </summary>
        public List<StoryData> GetMainStories()
        {
            return mainStories.OrderBy(s => s.storyOrder).ToList();
        }
        
        /// <summary>
        /// 캐릭터 스토리 가져오기
        /// </summary>
        public List<StoryData> GetCharacterStories(string characterId)
        {
            return characterStories.Where(s => 
                s.triggerConditions.Any(c => 
                    c.eventType == "character" && 
                    c.eventTarget == characterId
                )
            ).ToList();
        }

        /// <summary>
        /// 스토리 추가
        /// </summary>
        public void AddStory(StoryData story)
        {
            if (story == null || string.IsNullOrEmpty(story.storyId))
                return;
                
            // 중복 체크
            if (stories.Any(s => s.storyId == story.storyId))
            {
                Debug.LogWarning($"[StoryDatabase] 중복된 스토리 ID: {story.storyId}");
                return;
            }
            
            stories.Add(story);
            
            // 타입별 분류
            switch (story.storyType)
            {
                case StoryType.Main:
                    if (!mainStories.Contains(story))
                        mainStories.Add(story);
                    break;
                case StoryType.Event:
                    if (!eventStories.Contains(story))
                        eventStories.Add(story);
                    break;
                case StoryType.Character:
                    if (!characterStories.Contains(story))
                        characterStories.Add(story);
                    break;
            }
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            Debug.Log($"[StoryDatabase] 스토리 추가됨: {story.storyId}");
        }

        /// <summary>
        /// 스토리 제거
        /// </summary>
        public void RemoveStory(string storyId)
        {
            StoryData story = GetStory(storyId);
            if (story == null) return;
            
            stories.Remove(story);
            mainStories.Remove(story);
            eventStories.Remove(story);
            characterStories.Remove(story);
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            Debug.Log($"[StoryDatabase] 스토리 제거됨: {storyId}");
        }

        /// <summary>
        /// CSV 형식으로 스토리 데이터 내보내기
        /// </summary>
        public string ExportToCSV()
        {
            System.Text.StringBuilder csv = new System.Text.StringBuilder();
            
            // 헤더
            csv.AppendLine("StoryID,StoryType,StoryTitle,Order,CharacterName,DialogueText,Emotion,Position,Choices,NextStoryID,Conditions");
            
            foreach (var story in stories)
            {
                foreach (var line in story.dialogueLines)
                {
                    string choices = string.Join("|", line.choices?.Select(c => $"{c.choiceText}:{c.nextStoryId}") ?? new string[0]);
                    string conditions = string.Join("|", story.conditions?.Select(c => $"{c.conditionType}:{c.targetValue}") ?? new string[0]);
                    
                    csv.AppendLine($"{story.storyId},{story.storyType},{story.storyTitle},{story.storyOrder},{line.characterName},{EscapeCSV(line.dialogueText)},{line.emotion},{line.position},{choices},{line.nextStoryId},{conditions}");
                }
            }
            
            return csv.ToString();
        }
        
        /// <summary>
        /// CSV 문자열 이스케이프
        /// </summary>
        private string EscapeCSV(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            
            // 쉼표나 따옴표가 포함된 경우 따옴표로 감싸기
            if (text.Contains(",") || text.Contains("\"") || text.Contains("\n"))
            {
                return $"\"{text.Replace("\"", "\"\"\"\"\"\"\"")}\"";
            }
            return text;
        }
        
        /// <summary>
        /// CSV에서 스토리 데이터 가져오기
        /// </summary>
        public void ImportFromCSV(string csvContent)
        {
            if (string.IsNullOrEmpty(csvContent)) return;
            
            string[] lines = csvContent.Split('\n');
            if (lines.Length <= 1) return; // 헤더만 있는 경우
            
            Dictionary<string, StoryData> storyDict = new Dictionary<string, StoryData>();
            
            // 첫 번째 줄은 헤더이므로 건너뛰기
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                string[] values = ParseCSVLine(line);
                if (values.Length < 7) continue;
                
                string storyId = values[0];
                if (string.IsNullOrEmpty(storyId)) continue;
                
                // 스토리 데이터 생성 또는 가져오기
                if (!storyDict.ContainsKey(storyId))
                {
                    StoryData newStory = CreateInstance<StoryData>();
                    newStory.storyId = storyId;
                    
                    if (System.Enum.TryParse(values[1], out StoryType storyType))
                        newStory.storyType = storyType;
                    
                    newStory.storyTitle = values[2];
                    
                    if (int.TryParse(values[3], out int order))
                        newStory.storyOrder = order;
                    
                    newStory.dialogueLines = new List<DialogueLine>();
                    storyDict[storyId] = newStory;
                }
                
                // 대화 라인 추가
                DialogueLine dialogueLine = new DialogueLine
                {
                    storyId = storyId,
                    characterName = values[4],
                    dialogueText = values[5]
                };
                
                if (System.Enum.TryParse(values[6], out CharacterEmotion emotion))
                    dialogueLine.emotion = emotion;
                
                if (values.Length > 7 && System.Enum.TryParse(values[7], out CharacterPosition position))
                    dialogueLine.position = position;
                
                // 선택지 파싱
                if (values.Length > 8 && !string.IsNullOrEmpty(values[8]))
                {
                    dialogueLine.choices = ParseChoices(values[8]);
                }
                
                storyDict[storyId].dialogueLines.Add(dialogueLine);
            }
            
            // 데이터베이스에 추가
            foreach (var story in storyDict.Values)
            {
                AddStory(story);
            }
            
            Debug.Log($"[StoryDatabase] CSV에서 {storyDict.Count}개 스토리 가져오기 완료");
        }
        
        /// <summary>
        /// CSV 라인 파싱
        /// </summary>
        private string[] ParseCSVLine(string line)
        {
            List<string> result = new List<string>();
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
        
        /// <summary>
        /// 선택지 파싱
        /// </summary>
        private List<DialogueChoice> ParseChoices(string choicesText)
        {
            List<DialogueChoice> choices = new List<DialogueChoice>();
            
            if (string.IsNullOrEmpty(choicesText)) return choices;
            
            string[] choiceArray = choicesText.Split('|');
            foreach (string choiceStr in choiceArray)
            {
                string[] parts = choiceStr.Split(':');
                if (parts.Length >= 2)
                {
                    DialogueChoice choice = new DialogueChoice
                    {
                        choiceText = parts[0],
                        nextStoryId = parts[1]
                    };
                    choices.Add(choice);
                }
            }
            
            return choices;
        }

        /// <summary>
        /// 스토리 존재 여부 확인
        /// </summary>
        public bool HasStory(string storyId)
        {
            return stories.Any(s => s.storyId == storyId);
        }

        /// <summary>
        /// 조건에 맞는 스토리들 검색
        /// </summary>
        public List<StoryData> SearchStories(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return GetAllStories();

            return stories.Where(s => 
                s.storyTitle.Contains(keyword, System.StringComparison.OrdinalIgnoreCase) ||
                s.storyDescription.Contains(keyword, System.StringComparison.OrdinalIgnoreCase) ||
                s.storyId.Contains(keyword, System.StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        /// <summary>
        /// 스토리 개수 가져오기
        /// </summary>
        public int GetStoryCount()
        {
            return stories.Count;
        }

        /// <summary>
        /// 이벤트 스토리 개수 가져오기
        /// </summary>
        public int GetEventStoryCount()
        {
            return eventStories.Count;
        }
        
        /// <summary>
        /// 데이터베이스 정리
        /// </summary>
        [ContextMenu("Clean Database")]
        public void CleanDatabase()
        {
            stories.RemoveAll(s => s == null || string.IsNullOrEmpty(s.storyId));
            mainStories.RemoveAll(s => s == null || !stories.Contains(s));
            eventStories.RemoveAll(s => s == null || !stories.Contains(s));
            characterStories.RemoveAll(s => s == null || !stories.Contains(s));
            
            Debug.Log("[StoryDatabase] 데이터베이스 정리 완료");
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 스토리 데이터 검증
        /// </summary>
        [UnityEditor.MenuItem("Tools/Story/Validate Story Database")]
        public static void ValidateStoryDatabase()
        {
            StoryDatabase[] databases = Resources.FindObjectsOfTypeAll<StoryDatabase>();
            
            foreach (var db in databases)
            {
                Debug.Log($"[StoryDatabase] 검증 시작: {db.name}");
                
                // 중복 ID 체크
                var duplicateIds = db.stories
                    .GroupBy(s => s.storyId)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);
                
                foreach (var duplicateId in duplicateIds)
                {
                    Debug.LogError($"[StoryDatabase] 중복된 스토리 ID: {duplicateId}");
                }
                
                // 빈 ID 체크
                var emptyIds = db.stories.Where(s => string.IsNullOrEmpty(s.storyId));
                foreach (var story in emptyIds)
                {
                    Debug.LogError($"[StoryDatabase] 빈 스토리 ID 발견: {story.storyTitle}");
                }
                
                Debug.Log($"[StoryDatabase] 검증 완료. 총 스토리: {db.GetStoryCount()}, 이벤트 스토리: {db.GetEventStoryCount()}");
            }
        }
#endif
    }
} 