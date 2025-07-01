using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Core;
using GuildMaster.Battle;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 캐릭터 관계도 및 호감도 시스템
    /// 캐릭터 간의 관계, 호감도, 시너지 등을 관리
    /// </summary>
    public class RelationshipSystem : MonoBehaviour
    {
        private static RelationshipSystem _instance;
        public static RelationshipSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<RelationshipSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("RelationshipSystem");
                        _instance = go.AddComponent<RelationshipSystem>();
                    }
                }
                return _instance;
            }
        }

        [Header("Relationship Settings")]
        [SerializeField] private int maxRelationshipLevel = 10;
        [SerializeField] private int baseExpRequired = 100;
        [SerializeField] private float expMultiplier = 1.5f;
        [SerializeField] private float synergyBonusPerLevel = 0.02f; // 레벨당 2% 보너스
        
        [Header("Affinity Settings")]
        [SerializeField] private int maxAffinityLevel = 5;
        [SerializeField] private int affinityExpRequired = 500;
        
        // 관계 데이터
        private Dictionary<string, CharacterRelationship> relationships;
        private Dictionary<string, AffinityData> affinities;
        private Dictionary<string, List<string>> relationshipNetwork;
        
        // 이벤트
        public event Action<string, string, int> OnRelationshipLevelUp; // char1, char2, newLevel
        public event Action<string, int> OnAffinityLevelUp; // charId, newLevel
        public event Action<string, string, RelationshipEvent> OnRelationshipEvent;
        
        public enum RelationshipType
        {
            Neutral,      // 중립
            Friendly,     // 우호적
            Close,        // 친밀
            Rival,        // 라이벌
            Romantic,     // 연인
            Family,       // 가족
            Mentor,       // 스승-제자
            Comrade       // 전우
        }
        
        public enum RelationshipEvent
        {
            FirstMeeting,        // 첫 만남
            BattleTogether,      // 함께 전투
            Victory,             // 승리
            Defeat,              // 패배
            Gift,                // 선물
            Conversation,        // 대화
            SpecialEvent,        // 특별 이벤트
            CriticalSave,        // 위기 구출
            CriticalHit,         // 치명타 달성
            Betrayal,            // 배신
            Reconciliation       // 화해
        }
        
        [System.Serializable]
        public class CharacterRelationship
        {
            public string characterId1;
            public string characterId2;
            public RelationshipType type;
            public int level;
            public int experience;
            public float synergy;
            public List<RelationshipEvent> eventHistory;
            public DateTime firstMet;
            public DateTime lastInteraction;
            public Dictionary<string, object> metadata;
            
            public CharacterRelationship(string id1, string id2)
            {
                characterId1 = id1;
                characterId2 = id2;
                type = RelationshipType.Neutral;
                level = 0;
                experience = 0;
                synergy = 0f;
                eventHistory = new List<RelationshipEvent>();
                firstMet = DateTime.Now;
                lastInteraction = DateTime.Now;
                metadata = new Dictionary<string, object>();
            }
            
            public string GetRelationshipKey()
            {
                // 정렬된 키로 생성 (순서 무관)
                return string.Compare(characterId1, characterId2) < 0 ? 
                    $"{characterId1}_{characterId2}" : 
                    $"{characterId2}_{characterId1}";
            }
        }
        
        [System.Serializable]
        public class AffinityData
        {
            public string characterId;
            public string playerGuildId;
            public int level;
            public int experience;
            public List<string> unlockedDialogues;
            public List<string> receivedGifts;
            public bool isRomanceable;
            public bool romanceUnlocked;
            public Dictionary<string, int> preferences; // 선호도 (아이템, 활동 등)
            
            public AffinityData(string id)
            {
                characterId = id;
                level = 0;
                experience = 0;
                unlockedDialogues = new List<string>();
                receivedGifts = new List<string>();
                preferences = new Dictionary<string, int>();
            }
        }
        
        [System.Serializable]
        public class RelationshipBonus
        {
            public BonusType type;
            public float value;
            public string description;
            
            public enum BonusType
            {
                AttackPower,
                Defense,
                CriticalRate,
                HealingPower,
                Speed,
                SkillDamage,
                ExpGain,
                GoldGain
            }
        }
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeSystem();
        }
        
        void InitializeSystem()
        {
            relationships = new Dictionary<string, CharacterRelationship>();
            affinities = new Dictionary<string, AffinityData>();
            relationshipNetwork = new Dictionary<string, List<string>>();
            
            LoadRelationshipData();
        }
        
        void Start()
        {
            SubscribeToEvents();
        }
        
        void SubscribeToEvents()
        {
            var eventManager = EventManager.Instance;
            if (eventManager != null)
            {
                eventManager.Subscribe(GuildMaster.Core.EventType.BattleVictory, OnBattleVictory);
                eventManager.Subscribe(GuildMaster.Core.EventType.BattleDefeat, OnBattleDefeat);
                eventManager.Subscribe(GuildMaster.Core.EventType.UnitKilled, OnUnitKilled);
                eventManager.Subscribe(GuildMaster.Core.EventType.CriticalHit, OnCriticalHit);
            }
        }
        
        /// <summary>
        /// 새로운 관계 생성
        /// </summary>
        public void CreateRelationship(string characterId1, string characterId2, RelationshipType type = RelationshipType.Neutral)
        {
            var relationship = new CharacterRelationship(characterId1, characterId2)
            {
                type = type
            };
            
            string key = relationship.GetRelationshipKey();
            relationships[key] = relationship;
            
            // 네트워크 업데이트
            UpdateRelationshipNetwork(characterId1, characterId2);
            
            // 첫 만남 이벤트
            TriggerRelationshipEvent(characterId1, characterId2, RelationshipEvent.FirstMeeting);
        }
        
        /// <summary>
        /// 관계 경험치 추가
        /// </summary>
        public void AddRelationshipExp(string characterId1, string characterId2, int exp, RelationshipEvent eventType)
        {
            var relationship = GetRelationship(characterId1, characterId2);
            if (relationship == null)
            {
                CreateRelationship(characterId1, characterId2);
                relationship = GetRelationship(characterId1, characterId2);
            }
            
            relationship.experience += exp;
            relationship.lastInteraction = DateTime.Now;
            
            // 이벤트 기록
            relationship.eventHistory.Add(eventType);
            if (relationship.eventHistory.Count > 100)
            {
                relationship.eventHistory.RemoveAt(0);
            }
            
            // 레벨업 체크
            int requiredExp = GetRequiredExpForLevel(relationship.level + 1);
            while (relationship.experience >= requiredExp && relationship.level < maxRelationshipLevel)
            {
                relationship.experience -= requiredExp;
                relationship.level++;
                
                // 시너지 업데이트
                UpdateSynergy(relationship);
                
                // 관계 타입 자동 업그레이드
                UpdateRelationshipType(relationship);
                
                OnRelationshipLevelUp?.Invoke(characterId1, characterId2, relationship.level);
                
                requiredExp = GetRequiredExpForLevel(relationship.level + 1);
            }
            
            OnRelationshipEvent?.Invoke(characterId1, characterId2, eventType);
        }
        
        /// <summary>
        /// 호감도 경험치 추가
        /// </summary>
        public void AddAffinityExp(string characterId, int exp)
        {
            if (!affinities.ContainsKey(characterId))
            {
                affinities[characterId] = new AffinityData(characterId);
            }
            
            var affinity = affinities[characterId];
            affinity.experience += exp;
            
            // 레벨업 체크
            while (affinity.experience >= affinityExpRequired && affinity.level < maxAffinityLevel)
            {
                affinity.experience -= affinityExpRequired;
                affinity.level++;
                
                // 레벨업 보상
                UnlockAffinityRewards(characterId, affinity.level);
                
                OnAffinityLevelUp?.Invoke(characterId, affinity.level);
            }
        }
        
        /// <summary>
        /// 선물 주기
        /// </summary>
        public void GiveGift(string characterId, string itemId, int value)
        {
            var affinity = GetOrCreateAffinity(characterId);
            
            // 선물 기록
            affinity.receivedGifts.Add(itemId);
            
            // 선호도에 따른 보너스
            int bonusExp = value;
            if (affinity.preferences.ContainsKey(itemId))
            {
                bonusExp = value * (1 + affinity.preferences[itemId]);
            }
            
            AddAffinityExp(characterId, bonusExp);
            
            // 관계 이벤트 트리거
            var guildManager = GameManager.Instance?.GuildManager;
            if (guildManager != null)
            {
                var adventurers = guildManager.GetAdventurers();
                foreach (var adventurer in adventurers)
                {
                    if (adventurer.unitId == characterId)
                    {
                        // 같은 부대원들과의 관계도 약간 상승
                        var squadMembers = GetSquadMembers(adventurer);
                        foreach (var member in squadMembers)
                        {
                            if (member.unitId != characterId)
                            {
                                AddRelationshipExp(characterId, member.unitId, value / 2, RelationshipEvent.Gift);
                            }
                        }
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// 대화하기
        /// </summary>
        public void HaveConversation(string characterId1, string characterId2, string dialogueId)
        {
            AddRelationshipExp(characterId1, characterId2, 20, RelationshipEvent.Conversation);
            
            // 호감도도 상승
            AddAffinityExp(characterId1, 10);
            AddAffinityExp(characterId2, 10);
            
            // 대화 잠금 해제
            var affinity1 = GetOrCreateAffinity(characterId1);
            var affinity2 = GetOrCreateAffinity(characterId2);
            
            if (!affinity1.unlockedDialogues.Contains(dialogueId))
                affinity1.unlockedDialogues.Add(dialogueId);
            
            if (!affinity2.unlockedDialogues.Contains(dialogueId))
                affinity2.unlockedDialogues.Add(dialogueId);
        }
        
        /// <summary>
        /// 관계 조회
        /// </summary>
        public CharacterRelationship GetRelationship(string characterId1, string characterId2)
        {
            string key = GetRelationshipKey(characterId1, characterId2);
            return relationships.ContainsKey(key) ? relationships[key] : null;
        }
        
        /// <summary>
        /// 호감도 조회
        /// </summary>
        public AffinityData GetAffinity(string characterId)
        {
            return affinities.ContainsKey(characterId) ? affinities[characterId] : null;
        }
        
        /// <summary>
        /// 시너지 보너스 계산
        /// </summary>
        public List<RelationshipBonus> GetRelationshipBonuses(List<Unit> squad)
        {
            var bonuses = new List<RelationshipBonus>();
            
            // 모든 페어 체크
            for (int i = 0; i < squad.Count; i++)
            {
                for (int j = i + 1; j < squad.Count; j++)
                {
                    var unit1 = squad[i];
                    var unit2 = squad[j];
                    
                    if (unit1 == null || unit2 == null) continue;
                    
                    var relationship = GetRelationship(unit1.unitId, unit2.unitId);
                    if (relationship != null && relationship.level > 0)
                    {
                        // 관계 타입에 따른 보너스
                        var typeBonus = GetRelationshipTypeBonus(relationship.type);
                        foreach (var bonus in typeBonus)
                        {
                            bonus.value *= relationship.synergy;
                            bonuses.Add(bonus);
                        }
                    }
                }
            }
            
            return bonuses;
        }
        
        List<RelationshipBonus> GetRelationshipTypeBonus(RelationshipType type)
        {
            var bonuses = new List<RelationshipBonus>();
            
            switch (type)
            {
                case RelationshipType.Friendly:
                    bonuses.Add(new RelationshipBonus
                    {
                        type = RelationshipBonus.BonusType.ExpGain,
                        value = 0.1f,
                        description = "우호적 관계 - 경험치 +10%"
                    });
                    break;
                    
                case RelationshipType.Close:
                    bonuses.Add(new RelationshipBonus
                    {
                        type = RelationshipBonus.BonusType.Defense,
                        value = 0.15f,
                        description = "친밀한 관계 - 방어력 +15%"
                    });
                    bonuses.Add(new RelationshipBonus
                    {
                        type = RelationshipBonus.BonusType.HealingPower,
                        value = 0.2f,
                        description = "친밀한 관계 - 치유력 +20%"
                    });
                    break;
                    
                case RelationshipType.Rival:
                    bonuses.Add(new RelationshipBonus
                    {
                        type = RelationshipBonus.BonusType.AttackPower,
                        value = 0.2f,
                        description = "라이벌 관계 - 공격력 +20%"
                    });
                    bonuses.Add(new RelationshipBonus
                    {
                        type = RelationshipBonus.BonusType.CriticalRate,
                        value = 0.1f,
                        description = "라이벌 관계 - 치명타율 +10%"
                    });
                    break;
                    
                case RelationshipType.Romantic:
                    bonuses.Add(new RelationshipBonus
                    {
                        type = RelationshipBonus.BonusType.AttackPower,
                        value = 0.15f,
                        description = "연인 관계 - 공격력 +15%"
                    });
                    bonuses.Add(new RelationshipBonus
                    {
                        type = RelationshipBonus.BonusType.Defense,
                        value = 0.15f,
                        description = "연인 관계 - 방어력 +15%"
                    });
                    bonuses.Add(new RelationshipBonus
                    {
                        type = RelationshipBonus.BonusType.Speed,
                        value = 0.1f,
                        description = "연인 관계 - 속도 +10%"
                    });
                    break;
                    
                case RelationshipType.Family:
                    bonuses.Add(new RelationshipBonus
                    {
                        type = RelationshipBonus.BonusType.Defense,
                        value = 0.25f,
                        description = "가족 관계 - 방어력 +25%"
                    });
                    bonuses.Add(new RelationshipBonus
                    {
                        type = RelationshipBonus.BonusType.HealingPower,
                        value = 0.3f,
                        description = "가족 관계 - 치유력 +30%"
                    });
                    break;
                    
                case RelationshipType.Mentor:
                    bonuses.Add(new RelationshipBonus
                    {
                        type = RelationshipBonus.BonusType.ExpGain,
                        value = 0.25f,
                        description = "스승-제자 관계 - 경험치 +25%"
                    });
                    bonuses.Add(new RelationshipBonus
                    {
                        type = RelationshipBonus.BonusType.SkillDamage,
                        value = 0.15f,
                        description = "스승-제자 관계 - 스킬 데미지 +15%"
                    });
                    break;
                    
                case RelationshipType.Comrade:
                    bonuses.Add(new RelationshipBonus
                    {
                        type = RelationshipBonus.BonusType.AttackPower,
                        value = 0.1f,
                        description = "전우 관계 - 공격력 +10%"
                    });
                    bonuses.Add(new RelationshipBonus
                    {
                        type = RelationshipBonus.BonusType.Defense,
                        value = 0.1f,
                        description = "전우 관계 - 방어력 +10%"
                    });
                    bonuses.Add(new RelationshipBonus
                    {
                        type = RelationshipBonus.BonusType.CriticalRate,
                        value = 0.05f,
                        description = "전우 관계 - 치명타율 +5%"
                    });
                    break;
            }
            
            return bonuses;
        }
        
        /// <summary>
        /// 관계 네트워크 조회
        /// </summary>
        public List<string> GetRelatedCharacters(string characterId)
        {
            return relationshipNetwork.ContainsKey(characterId) ? 
                new List<string>(relationshipNetwork[characterId]) : 
                new List<string>();
        }
        
        /// <summary>
        /// 가장 친한 캐릭터 조회
        /// </summary>
        public string GetBestFriend(string characterId)
        {
            var related = GetRelatedCharacters(characterId);
            CharacterRelationship bestRelationship = null;
            
            foreach (var otherId in related)
            {
                var relationship = GetRelationship(characterId, otherId);
                if (relationship != null && 
                    (relationship.type == RelationshipType.Close || 
                     relationship.type == RelationshipType.Romantic ||
                     relationship.type == RelationshipType.Family))
                {
                    if (bestRelationship == null || relationship.level > bestRelationship.level)
                    {
                        bestRelationship = relationship;
                    }
                }
            }
            
            if (bestRelationship != null)
            {
                return bestRelationship.characterId1 == characterId ? 
                    bestRelationship.characterId2 : 
                    bestRelationship.characterId1;
            }
            
            return null;
        }
        
        /// <summary>
        /// 라이벌 조회
        /// </summary>
        public string GetRival(string characterId)
        {
            var related = GetRelatedCharacters(characterId);
            
            foreach (var otherId in related)
            {
                var relationship = GetRelationship(characterId, otherId);
                if (relationship != null && relationship.type == RelationshipType.Rival)
                {
                    return otherId;
                }
            }
            
            return null;
        }
        
        // Private 메서드들
        void UpdateRelationshipNetwork(string characterId1, string characterId2)
        {
            if (!relationshipNetwork.ContainsKey(characterId1))
                relationshipNetwork[characterId1] = new List<string>();
            
            if (!relationshipNetwork.ContainsKey(characterId2))
                relationshipNetwork[characterId2] = new List<string>();
            
            if (!relationshipNetwork[characterId1].Contains(characterId2))
                relationshipNetwork[characterId1].Add(characterId2);
            
            if (!relationshipNetwork[characterId2].Contains(characterId1))
                relationshipNetwork[characterId2].Add(characterId1);
        }
        
        void UpdateSynergy(CharacterRelationship relationship)
        {
            relationship.synergy = 1f + (relationship.level * synergyBonusPerLevel);
        }
        
        void UpdateRelationshipType(CharacterRelationship relationship)
        {
            // 레벨에 따른 자동 관계 업그레이드
            if (relationship.type == RelationshipType.Neutral && relationship.level >= 3)
            {
                relationship.type = RelationshipType.Friendly;
            }
            else if (relationship.type == RelationshipType.Friendly && relationship.level >= 6)
            {
                // 이벤트 히스토리 분석하여 관계 타입 결정
                int battleCount = relationship.eventHistory.Count(e => e == RelationshipEvent.BattleTogether);
                int rivalryCount = relationship.eventHistory.Count(e => e == RelationshipEvent.Victory || e == RelationshipEvent.CriticalHit);
                
                if (rivalryCount > battleCount * 2)
                {
                    relationship.type = RelationshipType.Rival;
                }
                else
                {
                    relationship.type = RelationshipType.Close;
                }
            }
        }
        
        void TriggerRelationshipEvent(string characterId1, string characterId2, RelationshipEvent eventType)
        {
            OnRelationshipEvent?.Invoke(characterId1, characterId2, eventType);
        }
        
        AffinityData GetOrCreateAffinity(string characterId)
        {
            if (!affinities.ContainsKey(characterId))
            {
                affinities[characterId] = new AffinityData(characterId);
            }
            return affinities[characterId];
        }
        
        void UnlockAffinityRewards(string characterId, int level)
        {
            // 레벨별 보상 잠금 해제
            switch (level)
            {
                case 1:
                    // 기본 대화 잠금 해제
                    break;
                case 2:
                    // 특별 대화 잠금 해제
                    break;
                case 3:
                    // 개인 퀘스트 잠금 해제
                    break;
                case 4:
                    // 특별 스킬 잠금 해제
                    break;
                case 5:
                    // 로맨스 옵션 잠금 해제 (해당하는 경우)
                    var affinity = GetAffinity(characterId);
                    if (affinity != null && affinity.isRomanceable)
                    {
                        affinity.romanceUnlocked = true;
                    }
                    break;
            }
        }
        
        string GetRelationshipKey(string characterId1, string characterId2)
        {
            return string.Compare(characterId1, characterId2) < 0 ? 
                $"{characterId1}_{characterId2}" : 
                $"{characterId2}_{characterId1}";
        }
        
        int GetRequiredExpForLevel(int level)
        {
            return Mathf.RoundToInt(baseExpRequired * Mathf.Pow(expMultiplier, level - 1));
        }
        
        List<Unit> GetSquadMembers(Unit unit)
        {
            // 같은 부대의 멤버들 찾기
            var battleManager = GameManager.Instance?.BattleManager;
            if (battleManager != null)
            {
                // 구현 필요
            }
            
            return new List<Unit>();
        }
        
        // 이벤트 핸들러
        void OnBattleVictory(GameEvent gameEvent)
        {
            // 전투에 참여한 유닛들의 관계 경험치 증가
            var participants = gameEvent.GetParameter<List<string>>("participants");
            if (participants != null)
            {
                for (int i = 0; i < participants.Count; i++)
                {
                    for (int j = i + 1; j < participants.Count; j++)
                    {
                        AddRelationshipExp(participants[i], participants[j], 50, RelationshipEvent.Victory);
                    }
                }
            }
        }
        
        void OnBattleDefeat(GameEvent gameEvent)
        {
            // 패배 시에도 관계는 발전
            var participants = gameEvent.GetParameter<List<string>>("participants");
            if (participants != null)
            {
                for (int i = 0; i < participants.Count; i++)
                {
                    for (int j = i + 1; j < participants.Count; j++)
                    {
                        AddRelationshipExp(participants[i], participants[j], 30, RelationshipEvent.Defeat);
                    }
                }
            }
        }
        
        void OnUnitKilled(GameEvent gameEvent)
        {
            var killedUnitId = gameEvent.GetParameter<string>("unitId");
            var killerUnitId = gameEvent.GetParameter<string>("killerId");
            
            if (!string.IsNullOrEmpty(killedUnitId) && !string.IsNullOrEmpty(killerUnitId))
            {
                // 같은 편이 죽었을 때
                var isAlly = gameEvent.GetParameter<bool>("isAlly");
                if (isAlly)
                {
                    // 복수심 또는 슬픔으로 관계 강화
                    var allies = gameEvent.GetParameter<List<string>>("allies");
                    if (allies != null)
                    {
                        foreach (var allyId in allies)
                        {
                            if (allyId != killedUnitId)
                            {
                                AddRelationshipExp(allyId, killedUnitId, 100, RelationshipEvent.SpecialEvent);
                            }
                        }
                    }
                }
            }
        }
        
        void OnCriticalHit(GameEvent gameEvent)
        {
            var attackerId = gameEvent.GetParameter<string>("attackerId");
            var targetId = gameEvent.GetParameter<string>("targetId");
            
            // 크리티컬 히트 시 목격한 아군들과의 관계 상승
            var witnesses = gameEvent.GetParameter<List<string>>("witnesses");
            if (witnesses != null && !string.IsNullOrEmpty(attackerId))
            {
                foreach (var witnessId in witnesses)
                {
                    if (witnessId != attackerId)
                    {
                        AddRelationshipExp(attackerId, witnessId, 10, RelationshipEvent.BattleTogether);
                    }
                }
            }
        }
        
        // 저장/로드
        void SaveRelationshipData()
        {
            // 관계 데이터 저장
            var relationshipList = relationships.Values.ToList();
            string relationshipJson = JsonUtility.ToJson(new SerializableList<CharacterRelationship>(relationshipList));
            PlayerPrefs.SetString("RelationshipData", relationshipJson);
            
            // 호감도 데이터 저장
            var affinityList = affinities.Values.ToList();
            string affinityJson = JsonUtility.ToJson(new SerializableList<AffinityData>(affinityList));
            PlayerPrefs.SetString("AffinityData", affinityJson);
            
            PlayerPrefs.Save();
        }
        
        void LoadRelationshipData()
        {
            // 관계 데이터 로드
            if (PlayerPrefs.HasKey("RelationshipData"))
            {
                string json = PlayerPrefs.GetString("RelationshipData");
                var data = JsonUtility.FromJson<SerializableList<CharacterRelationship>>(json);
                if (data != null && data.items != null)
                {
                    foreach (var relationship in data.items)
                    {
                        relationships[relationship.GetRelationshipKey()] = relationship;
                        UpdateRelationshipNetwork(relationship.characterId1, relationship.characterId2);
                    }
                }
            }
            
            // 호감도 데이터 로드
            if (PlayerPrefs.HasKey("AffinityData"))
            {
                string json = PlayerPrefs.GetString("AffinityData");
                var data = JsonUtility.FromJson<SerializableList<AffinityData>>(json);
                if (data != null && data.items != null)
                {
                    foreach (var affinity in data.items)
                    {
                        affinities[affinity.characterId] = affinity;
                    }
                }
            }
        }
        
        [System.Serializable]
        class SerializableList<T>
        {
            public List<T> items;
            
            public SerializableList(List<T> list)
            {
                items = list;
            }
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveRelationshipData();
            }
        }
        
        void OnDestroy()
        {
            SaveRelationshipData();
            
            // 이벤트 구독 해제
            var eventManager = EventManager.Instance;
            if (eventManager != null)
            {
                eventManager.Unsubscribe(GuildMaster.Core.EventType.BattleVictory, OnBattleVictory);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.BattleDefeat, OnBattleDefeat);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.UnitKilled, OnUnitKilled);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.CriticalHit, OnCriticalHit);
            }
        }
    }
}