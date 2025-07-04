using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Core;
using GuildMaster.Battle;
using GuildMaster.Guild;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 길드 시설 상호작용 시스템
    /// 캐릭터 배치, 자원 생산, 훈련 보너스, 시설별 이벤트 등을 관리
    /// </summary>
    public class FacilityInteractionSystem : MonoBehaviour
    {
        private static FacilityInteractionSystem _instance;
        public static FacilityInteractionSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<FacilityInteractionSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("FacilityInteractionSystem");
                        _instance = go.AddComponent<FacilityInteractionSystem>();
                    }
                }
                return _instance;
            }
        }

        [Header("System Settings")]
        [SerializeField] private float productionCheckInterval = 60f; // 1분마다 생산 체크
        [SerializeField] private float eventCheckInterval = 300f; // 5분마다 이벤트 체크
        [SerializeField] private float trainingCheckInterval = 30f; // 30초마다 훈련 체크
        [SerializeField] private float synergyBonusMultiplier = 1.5f; // 시너지 보너스 배율

        [Header("Training Settings")]
        [SerializeField] private float baseTrainingExpPerMinute = 10f;
        [SerializeField] private float trainingEfficiencyDecay = 0.9f; // 피로도에 따른 효율 감소
        [SerializeField] private int maxCharactersPerFacility = 3;

        [Header("Event Settings")]
        [SerializeField] private float baseEventChance = 0.1f; // 기본 이벤트 발생 확률 (10%)
        [SerializeField] private float relationshipEventBonus = 0.05f; // 관계도에 따른 보너스
        [SerializeField] private float trainingBreakthroughExpBonus = 100f; // 훈련 돌파 시 경험치 보너스

        // 시설 배치 데이터
        private Dictionary<string, FacilityAssignment> facilityAssignments;
        private Dictionary<string, WorkSchedule> characterSchedules;
        private Dictionary<string, FacilityProductionData> productionData;
        private Dictionary<string, List<FacilityEvent>> facilityEventHistory;

        // 타이머
        private float lastProductionCheck;
        private float lastEventCheck;
        private float lastTrainingCheck;

        // 이벤트
        public event Action<string, Unit, GuildManager.Building> OnCharacterAssigned;
        public event Action<string, Unit, GuildManager.Building> OnCharacterUnassigned;
        public event Action<GuildManager.Building, Dictionary<ResourceType, int>> OnResourceProduced;
        public event Action<Unit, float> OnTrainingCompleted;
        public event Action<FacilityEvent> OnFacilityEventTriggered;
        public event Action<WorkShift> OnShiftChanged;

        public enum WorkShift
        {
            Morning,    // 06:00 - 14:00
            Afternoon,  // 14:00 - 22:00
            Night,      // 22:00 - 06:00
            FullDay     // 24시간
        }

        public enum FacilityEventType
        {
            ProductionBonus,
            TrainingBreakthrough,
            AccidentMinor,
            AccidentMajor,
            SkillDiscovery,
            RelationshipEvent,
            VisitorEvent,
            EquipmentUpgrade,
            ResearchBreakthrough,
            MysteriousEvent
        }

        [System.Serializable]
        public class FacilityAssignment
        {
            public string facilityId;
            public GuildManager.BuildingType buildingType;
            public List<string> assignedCharacterIds;
            public DateTime assignmentTime;
            public float totalProductionTime;
            public float efficiencyModifier;
            public Dictionary<string, float> characterContributions;

            public FacilityAssignment(string id, GuildManager.BuildingType type)
            {
                facilityId = id;
                buildingType = type;
                assignedCharacterIds = new List<string>();
                assignmentTime = DateTime.Now;
                totalProductionTime = 0f;
                efficiencyModifier = 1f;
                characterContributions = new Dictionary<string, float>();
            }
        }

        [System.Serializable]
        public class WorkSchedule
        {
            public string characterId;
            public WorkShift currentShift;
            public string assignedFacilityId;
            public float workHoursToday;
            public float fatigue;
            public DateTime lastShiftChange;
            public bool isOnBreak;
            public Dictionary<DayOfWeek, WorkShift> weeklySchedule;

            public WorkSchedule(string id)
            {
                characterId = id;
                currentShift = WorkShift.Morning;
                workHoursToday = 0f;
                fatigue = 0f;
                lastShiftChange = DateTime.Now;
                isOnBreak = false;
                weeklySchedule = new Dictionary<DayOfWeek, WorkShift>();
                
                // 기본 주간 스케줄 설정
                for (int i = 0; i < 7; i++)
                {
                    weeklySchedule[(DayOfWeek)i] = WorkShift.Morning;
                }
            }
        }

        [System.Serializable]
        public class FacilityProductionData
        {
            public string facilityId;
            public Dictionary<ResourceType, float> resourcesPerMinute;
            public Dictionary<ResourceType, float> totalProduced;
            public float lastProductionTime;
            public float productionMultiplier;

            public FacilityProductionData(string id)
            {
                facilityId = id;
                resourcesPerMinute = new Dictionary<ResourceType, float>();
                totalProduced = new Dictionary<ResourceType, float>();
                lastProductionTime = Time.time;
                productionMultiplier = 1f;
            }
        }

        [System.Serializable]
        public class FacilityEvent
        {
            public string eventId;
            public FacilityEventType type;
            public string facilityId;
            public List<string> involvedCharacterIds;
            public string title;
            public string description;
            public DateTime timestamp;
            public Dictionary<string, object> outcomes;
            public bool isPositive;

            public FacilityEvent()
            {
                eventId = Guid.NewGuid().ToString();
                involvedCharacterIds = new List<string>();
                outcomes = new Dictionary<string, object>();
                timestamp = DateTime.Now;
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
            facilityAssignments = new Dictionary<string, FacilityAssignment>();
            characterSchedules = new Dictionary<string, WorkSchedule>();
            productionData = new Dictionary<string, FacilityProductionData>();
            facilityEventHistory = new Dictionary<string, List<FacilityEvent>>();

            lastProductionCheck = Time.time;
            lastEventCheck = Time.time;
            lastTrainingCheck = Time.time;
        }

        void Start()
        {
            StartCoroutine(SystemUpdateCoroutine());
            SubscribeToEvents();
        }

        void SubscribeToEvents()
        {
            var eventManager = EventManager.Instance;
            if (eventManager != null)
            {
                eventManager.Subscribe(GuildMaster.Core.EventType.BuildingConstructed, OnBuildingConstructed);
                eventManager.Subscribe(GuildMaster.Core.EventType.BuildingDestroyed, OnBuildingDestroyed);
                eventManager.Subscribe(GuildMaster.Core.EventType.DayChanged, OnDayChanged);
            }
        }

        IEnumerator SystemUpdateCoroutine()
        {
            while (true)
            {
                float currentTime = Time.time;

                // 생산 체크
                if (currentTime - lastProductionCheck >= productionCheckInterval)
                {
                    ProcessProduction();
                    lastProductionCheck = currentTime;
                }

                // 훈련 체크
                if (currentTime - lastTrainingCheck >= trainingCheckInterval)
                {
                    ProcessTraining();
                    lastTrainingCheck = currentTime;
                }

                // 이벤트 체크
                if (currentTime - lastEventCheck >= eventCheckInterval)
                {
                    CheckForFacilityEvents();
                    lastEventCheck = currentTime;
                }

                // 스케줄 업데이트
                UpdateWorkSchedules();

                yield return new WaitForSeconds(10f); // 10초마다 업데이트
            }
        }

        /// <summary>
        /// 캐릭터를 시설에 배치
        /// </summary>
        public bool AssignCharacterToFacility(string characterId, string facilityId)
        {
            // 캐릭터 확인
            var unit = GetUnitById(characterId);
            if (unit == null)
            {
                Debug.LogError($"Character {characterId} not found");
                return false;
            }

            // 시설 확인
            var building = GetBuildingById(facilityId);
            if (building == null)
            {
                Debug.LogError($"Facility {facilityId} not found");
                return false;
            }

            // 이미 다른 시설에 배치되어 있는지 확인
            UnassignCharacterFromCurrentFacility(characterId);

            // 시설 배치 데이터 생성 또는 가져오기
            if (!facilityAssignments.ContainsKey(facilityId))
            {
                facilityAssignments[facilityId] = new FacilityAssignment(facilityId, building.Type);
            }

            var assignment = facilityAssignments[facilityId];

            // 최대 인원 체크
            if (assignment.assignedCharacterIds.Count >= maxCharactersPerFacility)
            {
                Debug.LogWarning($"Facility {facilityId} is full");
                return false;
            }

            // 캐릭터 배치
            assignment.assignedCharacterIds.Add(characterId);
            assignment.characterContributions[characterId] = 0f;

            // 스케줄 생성
            if (!characterSchedules.ContainsKey(characterId))
            {
                characterSchedules[characterId] = new WorkSchedule(characterId);
            }
            characterSchedules[characterId].assignedFacilityId = facilityId;

            // 효율성 계산
            UpdateFacilityEfficiency(facilityId);

            // 이벤트 발생
            OnCharacterAssigned?.Invoke(characterId, unit, building);

            // EventManager에 알림
            var parameters = new Dictionary<string, object>
            {
                { "characterId", characterId },
                { "characterName", unit.unitName },
                { "facilityId", facilityId },
                { "facilityType", building.Type.ToString() }
            };
            EventManager.Instance.TriggerEvent(GuildMaster.Core.EventType.SpecialMission, 
                $"{unit.unitName}이(가) {building.Type.ToString()}에 배치됨", parameters);

            return true;
        }

        /// <summary>
        /// 캐릭터를 시설에서 해제
        /// </summary>
        public bool UnassignCharacterFromFacility(string characterId, string facilityId)
        {
            if (!facilityAssignments.ContainsKey(facilityId))
                return false;

            var assignment = facilityAssignments[facilityId];
            if (!assignment.assignedCharacterIds.Contains(characterId))
                return false;

            // 캐릭터 제거
            assignment.assignedCharacterIds.Remove(characterId);
            assignment.characterContributions.Remove(characterId);

            // 스케줄 업데이트
            if (characterSchedules.ContainsKey(characterId))
            {
                characterSchedules[characterId].assignedFacilityId = null;
            }

            // 효율성 재계산
            UpdateFacilityEfficiency(facilityId);

            // 이벤트 발생
            var unit = GetUnitById(characterId);
            var building = GetBuildingById(facilityId);
            if (unit != null && building != null)
            {
                OnCharacterUnassigned?.Invoke(characterId, unit, building);
            }

            return true;
        }

        void UnassignCharacterFromCurrentFacility(string characterId)
        {
            foreach (var kvp in facilityAssignments)
            {
                if (kvp.Value.assignedCharacterIds.Contains(characterId))
                {
                    UnassignCharacterFromFacility(characterId, kvp.Key);
                    break;
                }
            }
        }

        /// <summary>
        /// 시설 효율성 업데이트
        /// </summary>
        void UpdateFacilityEfficiency(string facilityId)
        {
            if (!facilityAssignments.ContainsKey(facilityId))
                return;

            var assignment = facilityAssignments[facilityId];
            var building = GetBuildingById(facilityId);
            if (building == null) return;

            float baseEfficiency = 1f;
            float skillBonus = 0f;
            float synergyBonus = 0f;
            float fatigueModifier = 1f;

            var assignedUnits = new List<Unit>();
            foreach (var characterId in assignment.assignedCharacterIds)
            {
                var unit = GetUnitById(characterId);
                if (unit != null)
                {
                    assignedUnits.Add(unit);

                    // 스킬 보너스 계산
                    skillBonus += CalculateSkillBonus(unit, building.Type);

                    // 피로도 계산
                    if (characterSchedules.ContainsKey(characterId))
                    {
                        var schedule = characterSchedules[characterId];
                        fatigueModifier *= (1f - schedule.fatigue * 0.5f); // 피로도가 50%일 때 효율 25% 감소
                    }
                }
            }

            // 시너지 보너스 계산
            if (assignedUnits.Count > 1)
            {
                var relationshipBonuses = RelationshipSystem.Instance.GetRelationshipBonuses(assignedUnits);
                foreach (var bonus in relationshipBonuses)
                {
                    synergyBonus += bonus.value * synergyBonusMultiplier;
                }
            }

            // 최종 효율성 계산
            assignment.efficiencyModifier = baseEfficiency * (1f + skillBonus) * (1f + synergyBonus) * fatigueModifier;
            assignment.efficiencyModifier = Mathf.Clamp(assignment.efficiencyModifier, 0.1f, 5f); // 10% ~ 500%
        }

        /// <summary>
        /// 스킬 보너스 계산
        /// </summary>
        float CalculateSkillBonus(Unit unit, GuildManager.BuildingType buildingType)
        {
            float bonus = 0f;

            // 직업별 시설 보너스
            switch (buildingType)
            {
                case GuildManager.BuildingType.TrainingGround:
                    if (unit.jobClass == JobClass.Knight || unit.jobClass == JobClass.Warrior)
                        bonus += 0.2f;
                    break;

                case GuildManager.BuildingType.ResearchLab:
                    if (unit.jobClass == JobClass.Mage || unit.jobClass == JobClass.Sage)
                        bonus += 0.3f;
                    break;

                case GuildManager.BuildingType.Temple:
                    if (unit.jobClass == JobClass.Priest || unit.jobClass == JobClass.Sage)
                        bonus += 0.25f;
                    break;

                case GuildManager.BuildingType.Armory:
                    if (unit.jobClass == JobClass.Knight || unit.jobClass == JobClass.Blacksmith)
                        bonus += 0.2f;
                    break;

                case GuildManager.BuildingType.Shop:
                    if (unit.jobClass == JobClass.Merchant)
                        bonus += 0.4f;
                    break;

                case GuildManager.BuildingType.ScoutPost:
                    if (unit.jobClass == JobClass.Ranger || unit.jobClass == JobClass.Assassin)
                        bonus += 0.3f;
                    break;
            }

            // 레벨 보너스
            bonus += unit.level * 0.01f; // 레벨당 1%

            // 스킬 보너스 (특정 스킬이 있을 경우)
            if (unit.skillIds != null && unit.skillIds.Count > 0)
            {
                // 스킬 ID 기반으로 시설 관련 보너스 계산
                foreach (var skillId in unit.skillIds)
                {
                    if (IsFacilityRelatedSkillId(skillId, buildingType))
                    {
                        bonus += 0.1f;
                    }
                }
            }

            return bonus;
        }

        bool IsFacilityRelatedSkill(Skill skill, GuildManager.BuildingType buildingType)
        {
            // 스킬과 시설의 관련성 체크
            // 실제 구현에서는 스킬의 태그나 타입을 체크
            return false;
        }

        bool IsFacilityRelatedSkillId(int skillId, GuildManager.BuildingType buildingType)
        {
            // 스킬 ID와 시설의 관련성 체크
            // 실제 구현에서는 스킬 데이터베이스에서 스킬 정보를 조회하여 판단
            // 임시로 false 반환
            return false;
        }

        bool IsProducerBuilding(GuildManager.BuildingType buildingType)
        {
            // 자원을 생산하는 건물 타입 체크
            return buildingType == GuildManager.BuildingType.Shop || 
                   buildingType == GuildManager.BuildingType.ResearchLab;
        }

        /// <summary>
        /// 자원 생산 처리
        /// </summary>
        void ProcessProduction()
        {
            var guildManager = GameManager.Instance?.GuildManager;
            var resourceManager = ResourceManager.Instance;
            if (guildManager == null || resourceManager == null) return;

            foreach (var kvp in facilityAssignments)
            {
                var facilityId = kvp.Key;
                var assignment = kvp.Value;

                if (assignment.assignedCharacterIds.Count == 0)
                    continue;

                var building = GetBuildingById(facilityId);
                if (building == null || !IsProducerBuilding(building.Type))
                    continue;

                // 생산 데이터 초기화
                if (!productionData.ContainsKey(facilityId))
                {
                    productionData[facilityId] = new FacilityProductionData(facilityId);
                }

                var production = productionData[facilityId];
                
                // 기본 생산량 계산
                float baseProduction = building.GetProductionAmount();
                float finalProduction = baseProduction * assignment.efficiencyModifier * production.productionMultiplier;

                // 자원 생산
                var producedResources = new Dictionary<ResourceType, int>();
                
                switch (building.Type)
                {
                    case GuildManager.BuildingType.Shop:
                        int goldProduced = Mathf.RoundToInt(finalProduction);
                        resourceManager.AddResource(ResourceType.Gold, goldProduced, $"{building.Type.ToString()} 생산");
                        producedResources[ResourceType.Gold] = goldProduced;
                        break;

                    case GuildManager.BuildingType.ResearchLab:
                        // 연구 포인트 생산 (마나스톤으로 대체)
                        int manaProduced = Mathf.RoundToInt(finalProduction * 0.5f);
                        resourceManager.AddResource(ResourceType.ManaStone, manaProduced, $"{building.Type.ToString()} 생산");
                        producedResources[ResourceType.ManaStone] = manaProduced;
                        break;
                }

                // 생산 기록
                foreach (var resource in producedResources)
                {
                    if (!production.totalProduced.ContainsKey(resource.Key))
                        production.totalProduced[resource.Key] = 0f;
                    production.totalProduced[resource.Key] += resource.Value;
                }

                // 캐릭터별 기여도 업데이트
                float contributionPerCharacter = 1f / assignment.assignedCharacterIds.Count;
                foreach (var characterId in assignment.assignedCharacterIds)
                {
                    if (!assignment.characterContributions.ContainsKey(characterId))
                        assignment.characterContributions[characterId] = 0f;
                    assignment.characterContributions[characterId] += finalProduction * contributionPerCharacter;
                }

                // 이벤트 발생
                if (producedResources.Count > 0)
                {
                    OnResourceProduced?.Invoke(building, producedResources);
                }
            }
        }

        /// <summary>
        /// 훈련 처리
        /// </summary>
        void ProcessTraining()
        {
            var trainingGrounds = GetFacilitiesByType(GuildManager.BuildingType.TrainingGround);
            
            foreach (var facility in trainingGrounds)
            {
                if (!facilityAssignments.ContainsKey(facility.Key))
                    continue;

                var assignment = facilityAssignments[facility.Key];
                var building = facility.Value;

                foreach (var characterId in assignment.assignedCharacterIds)
                {
                    var unit = GetUnitById(characterId);
                    if (unit == null) continue;

                    // 훈련 경험치 계산
                    float trainingEffect = building.GetCurrentEffect() / 100f; // 퍼센트를 배율로 변환
                    float expGained = baseTrainingExpPerMinute * (1f + trainingEffect) * assignment.efficiencyModifier;

                    // 피로도 적용
                    if (characterSchedules.ContainsKey(characterId))
                    {
                        var schedule = characterSchedules[characterId];
                        expGained *= (1f - schedule.fatigue * trainingEfficiencyDecay);
                    }

                    // 경험치 추가
                    unit.AddExperience(Mathf.RoundToInt(expGained));

                    // 훈련 완료 이벤트
                    OnTrainingCompleted?.Invoke(unit, expGained);
                }
            }
        }

        /// <summary>
        /// 시설 이벤트 체크
        /// </summary>
        void CheckForFacilityEvents()
        {
            foreach (var kvp in facilityAssignments)
            {
                var facilityId = kvp.Key;
                var assignment = kvp.Value;

                if (assignment.assignedCharacterIds.Count == 0)
                    continue;

                // 이벤트 발생 확률 계산
                float eventChance = baseEventChance;
                
                // 관계도에 따른 보너스
                if (assignment.assignedCharacterIds.Count > 1)
                {
                    var units = assignment.assignedCharacterIds.Select(id => GetUnitById(id)).Where(u => u != null).ToList();
                    var relationshipBonuses = RelationshipSystem.Instance.GetRelationshipBonuses(units);
                    eventChance += relationshipEventBonus * relationshipBonuses.Count;
                }

                // 이벤트 발생
                if (UnityEngine.Random.value < eventChance)
                {
                    var eventType = GetRandomEventType(assignment.buildingType);
                    TriggerFacilityEvent(facilityId, eventType);
                }
            }
        }

        FacilityEventType GetRandomEventType(GuildManager.BuildingType buildingType)
        {
            // 시설별 이벤트 가중치
            var eventWeights = new Dictionary<FacilityEventType, float>();

            switch (buildingType)
            {
                case GuildManager.BuildingType.TrainingGround:
                    eventWeights[FacilityEventType.TrainingBreakthrough] = 0.3f;
                    eventWeights[FacilityEventType.AccidentMinor] = 0.2f;
                    eventWeights[FacilityEventType.SkillDiscovery] = 0.2f;
                    eventWeights[FacilityEventType.RelationshipEvent] = 0.3f;
                    break;

                case GuildManager.BuildingType.ResearchLab:
                    eventWeights[FacilityEventType.ResearchBreakthrough] = 0.4f;
                    eventWeights[FacilityEventType.AccidentMinor] = 0.1f;
                    eventWeights[FacilityEventType.MysteriousEvent] = 0.2f;
                    eventWeights[FacilityEventType.SkillDiscovery] = 0.3f;
                    break;

                case GuildManager.BuildingType.Shop:
                    eventWeights[FacilityEventType.ProductionBonus] = 0.4f;
                    eventWeights[FacilityEventType.VisitorEvent] = 0.3f;
                    eventWeights[FacilityEventType.RelationshipEvent] = 0.3f;
                    break;

                case GuildManager.BuildingType.Armory:
                    eventWeights[FacilityEventType.EquipmentUpgrade] = 0.4f;
                    eventWeights[FacilityEventType.AccidentMinor] = 0.2f;
                    eventWeights[FacilityEventType.ProductionBonus] = 0.4f;
                    break;

                default:
                    eventWeights[FacilityEventType.RelationshipEvent] = 0.5f;
                    eventWeights[FacilityEventType.ProductionBonus] = 0.3f;
                    eventWeights[FacilityEventType.AccidentMinor] = 0.2f;
                    break;
            }

            // 가중치 기반 랜덤 선택
            float totalWeight = eventWeights.Values.Sum();
            float randomValue = UnityEngine.Random.value * totalWeight;
            float currentWeight = 0f;

            foreach (var kvp in eventWeights)
            {
                currentWeight += kvp.Value;
                if (randomValue <= currentWeight)
                {
                    return kvp.Key;
                }
            }

            return FacilityEventType.RelationshipEvent;
        }

        /// <summary>
        /// 시설 이벤트 발생
        /// </summary>
        void TriggerFacilityEvent(string facilityId, FacilityEventType eventType)
        {
            var assignment = facilityAssignments[facilityId];
            var building = GetBuildingById(facilityId);
            if (building == null) return;

            var facilityEvent = new FacilityEvent
            {
                type = eventType,
                facilityId = facilityId,
                involvedCharacterIds = new List<string>(assignment.assignedCharacterIds)
            };

            // 이벤트 타입별 처리
            switch (eventType)
            {
                case FacilityEventType.ProductionBonus:
                    HandleProductionBonusEvent(facilityEvent, building);
                    break;

                case FacilityEventType.TrainingBreakthrough:
                    HandleTrainingBreakthroughEvent(facilityEvent);
                    break;

                case FacilityEventType.AccidentMinor:
                    HandleAccidentEvent(facilityEvent, false);
                    break;

                case FacilityEventType.AccidentMajor:
                    HandleAccidentEvent(facilityEvent, true);
                    break;

                case FacilityEventType.SkillDiscovery:
                    HandleSkillDiscoveryEvent(facilityEvent);
                    break;

                case FacilityEventType.RelationshipEvent:
                    HandleRelationshipEvent(facilityEvent);
                    break;

                case FacilityEventType.VisitorEvent:
                    HandleVisitorEvent(facilityEvent, building);
                    break;

                case FacilityEventType.EquipmentUpgrade:
                    HandleEquipmentUpgradeEvent(facilityEvent);
                    break;

                case FacilityEventType.ResearchBreakthrough:
                    HandleResearchBreakthroughEvent(facilityEvent);
                    break;

                case FacilityEventType.MysteriousEvent:
                    HandleMysteriousEvent(facilityEvent);
                    break;
            }

            // 이벤트 기록
            if (!facilityEventHistory.ContainsKey(facilityId))
            {
                facilityEventHistory[facilityId] = new List<FacilityEvent>();
            }
            facilityEventHistory[facilityId].Add(facilityEvent);

            // 이벤트 발생 알림
            OnFacilityEventTriggered?.Invoke(facilityEvent);

            // EventManager에 알림
            var parameters = new Dictionary<string, object>
            {
                { "facilityId", facilityId },
                { "eventType", eventType.ToString() },
                { "facilityName", building.Type.ToString() }
            };
            EventManager.Instance.TriggerEvent(GuildMaster.Core.EventType.SpecialEventStarted, facilityEvent.title, parameters);
        }

        void HandleProductionBonusEvent(FacilityEvent evt, GuildManager.Building building)
        {
            var facilityId = evt.facilityId;
            
            if (productionData.ContainsKey(facilityId))
            {
                productionData[facilityId].productionMultiplier *= 1.5f; // 50% 보너스
                
                // 보너스 지속 시간 (30분)
                StartCoroutine(ResetProductionBonus(facilityId, 1800f));
                
                var parameters = new Dictionary<string, object>
                {
                    { "facilityId", facilityId },
                    { "bonusMultiplier", 1.5f }
                };
                
                EventManager.Instance.TriggerEvent(GuildMaster.Core.EventType.ResourceBonus,
                    $"{building.Type.ToString()} 생산 보너스 발생", parameters);
            }
        }

        IEnumerator ResetProductionBonus(string facilityId, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (productionData.ContainsKey(facilityId))
            {
                productionData[facilityId].productionMultiplier = 1f;
            }
        }

        void HandleTrainingBreakthroughEvent(FacilityEvent evt)
        {
            foreach (var characterId in evt.involvedCharacterIds)
            {
                var unit = GetUnitById(characterId);
                if (unit == null) continue;

                var parameters = new Dictionary<string, object>
                {
                    { "characterId", characterId },
                    { "characterName", unit.unitName },
                    { "expBonus", trainingBreakthroughExpBonus },
                    { "facilityId", evt.facilityId }
                };

                // 경험치 보너스 지급
                unit.AddExperience((int)trainingBreakthroughExpBonus);

                Debug.Log($"{unit.unitName}이(가) 훈련 돌파를 달성했습니다!");
            }
        }

        void HandleAccidentEvent(FacilityEvent evt, bool isMajor)
        {
            foreach (var characterId in evt.involvedCharacterIds)
            {
                var unit = GetUnitById(characterId);
                if (unit == null) continue;

                float damage = isMajor ? 0.3f : 0.1f; // 30% 또는 10%
                unit.TakeDamage(unit.maxHP * damage);

                var parameters = new Dictionary<string, object>
                {
                    { "characterId", characterId },
                    { "characterName", unit.unitName },
                    { "damage", (int)(unit.maxHP * damage) },
                    { "isMajor", isMajor }
                };

                Debug.LogWarning($"{unit.unitName}이(가) 사고를 당했습니다!");
            }
        }

        void HandleSkillDiscoveryEvent(FacilityEvent evt)
        {
            evt.title = "새로운 스킬 발견!";
            evt.description = "시설에서의 활동 중 새로운 기술을 깨달았습니다.";
            evt.isPositive = true;

            if (evt.involvedCharacterIds.Count > 0)
            {
                var characterId = evt.involvedCharacterIds[UnityEngine.Random.Range(0, evt.involvedCharacterIds.Count)];
                var unit = GetUnitById(characterId);
                if (unit != null)
                {
                    // 스킬 포인트 지급 또는 새 스킬 해금
                    evt.outcomes["characterId"] = characterId;
                    evt.outcomes["skillPoints"] = 1;
                }
            }
        }

        void HandleRelationshipEvent(FacilityEvent evt)
        {
            if (evt.involvedCharacterIds.Count < 2) return;

            var unit1 = GetUnitById(evt.involvedCharacterIds[0]);
            var unit2 = GetUnitById(evt.involvedCharacterIds[1]);
            if (unit1 == null || unit2 == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "character1Id", unit1.unitId },
                { "character1Name", unit1.unitName },
                { "character2Id", unit2.unitId },
                { "character2Name", unit2.unitName },
                { "relationshipBonus", relationshipEventBonus }
            };

            Debug.Log($"{unit1.unitName}과(와) {unit2.unitName}의 관계가 개선되었습니다!");
        }

        void HandleVisitorEvent(FacilityEvent evt, GuildManager.Building building)
        {
            // 방문자 이벤트 처리 - 자원이나 아이템 보상
            var resourceManager = ResourceManager.Instance;
            if (resourceManager != null)
            {
                // 랜덤 보상
                int goldReward = UnityEngine.Random.Range(100, 500);
                resourceManager.AddResource(ResourceType.Gold, goldReward, "방문자 선물");
            }
        }

        void HandleEquipmentUpgradeEvent(FacilityEvent evt)
        {
            evt.title = "장비 개선";
            evt.description = "무기고에서 장비를 개선하는 새로운 방법을 발견했습니다.";
            evt.isPositive = true;

            // 모든 배치된 캐릭터의 장비 보너스
            foreach (var characterId in evt.involvedCharacterIds)
            {
                var unit = GetUnitById(characterId);
                if (unit != null)
                {
                    unit.attackPower += 5;
                    unit.defense += 5;
                }
            }
            
            evt.outcomes["attackBonus"] = 5;
            evt.outcomes["defenseBonus"] = 5;
        }

        void HandleResearchBreakthroughEvent(FacilityEvent evt)
        {
            evt.title = "연구 돌파구";
            evt.description = "연구소에서 중요한 발견을 했습니다!";
            evt.isPositive = true;

            // 마나스톤 보너스
            int manaBonus = UnityEngine.Random.Range(10, 50);
            ResourceManager.Instance.AddResource(ResourceType.ManaStone, manaBonus, "연구 성과");
            
            evt.outcomes["manaBonus"] = manaBonus;
        }

        void HandleMysteriousEvent(FacilityEvent evt)
        {
            evt.title = "신비로운 현상";
            evt.description = "시설에서 설명할 수 없는 현상이 발생했습니다...";
            evt.isPositive = UnityEngine.Random.value > 0.5f;

            if (evt.isPositive)
            {
                // 랜덤 보너스
                var bonusType = UnityEngine.Random.Range(0, 3);
                switch (bonusType)
                {
                    case 0: // 모든 캐릭터 완전 회복
                        foreach (var characterId in evt.involvedCharacterIds)
                        {
                            var unit = GetUnitById(characterId);
                            if (unit != null)
                            {
                                unit.currentHP = unit.maxHP;
                                unit.currentMP = unit.maxMP;
                            }
                        }
                        evt.outcomes["effect"] = "fullRestore";
                        break;
                        
                    case 1: // 경험치 보너스
                        foreach (var characterId in evt.involvedCharacterIds)
                        {
                            var unit = GetUnitById(characterId);
                            if (unit != null)
                            {
                                unit.AddExperience(unit.experienceToNextLevel / 3);
                            }
                        }
                        evt.outcomes["effect"] = "expBonus";
                        break;
                        
                    case 2: // 임시 스탯 증가
                        foreach (var characterId in evt.involvedCharacterIds)
                        {
                            var unit = GetUnitById(characterId);
                            if (unit != null)
                            {
                                unit.speed += 10;
                            }
                        }
                        evt.outcomes["effect"] = "speedBonus";
                        break;
                }
            }
            else
            {
                // 부정적 효과
                foreach (var characterId in evt.involvedCharacterIds)
                {
                    if (characterSchedules.ContainsKey(characterId))
                    {
                        characterSchedules[characterId].fatigue = Mathf.Min(1f, characterSchedules[characterId].fatigue + 0.3f);
                    }
                }
                evt.outcomes["effect"] = "fatigue";
            }
        }

        /// <summary>
        /// 작업 스케줄 업데이트
        /// </summary>
        void UpdateWorkSchedules()
        {
            var currentHour = DateTime.Now.Hour;
            var currentShift = GetCurrentShift(currentHour);

            foreach (var kvp in characterSchedules)
            {
                var schedule = kvp.Value;
                
                // 교대 시간 체크
                if (schedule.currentShift != currentShift)
                {
                    schedule.lastShiftChange = DateTime.Now;
                    schedule.currentShift = currentShift;
                    OnShiftChanged?.Invoke(currentShift);
                }

                // 근무 시간 업데이트
                if (!schedule.isOnBreak && !string.IsNullOrEmpty(schedule.assignedFacilityId))
                {
                    schedule.workHoursToday += Time.deltaTime / 3600f; // 초를 시간으로 변환
                    
                    // 피로도 증가
                    schedule.fatigue = Mathf.Min(1f, schedule.fatigue + (Time.deltaTime / 3600f) * 0.1f); // 시간당 10% 증가
                }
                else
                {
                    // 휴식 중일 때 피로도 감소
                    schedule.fatigue = Mathf.Max(0f, schedule.fatigue - (Time.deltaTime / 3600f) * 0.2f); // 시간당 20% 감소
                }
            }
        }

        WorkShift GetCurrentShift(int hour)
        {
            if (hour >= 6 && hour < 14)
                return WorkShift.Morning;
            else if (hour >= 14 && hour < 22)
                return WorkShift.Afternoon;
            else
                return WorkShift.Night;
        }

        /// <summary>
        /// 캐릭터의 작업 스케줄 설정
        /// </summary>
        public void SetCharacterSchedule(string characterId, DayOfWeek day, WorkShift shift)
        {
            if (!characterSchedules.ContainsKey(characterId))
            {
                characterSchedules[characterId] = new WorkSchedule(characterId);
            }

            characterSchedules[characterId].weeklySchedule[day] = shift;
        }

        /// <summary>
        /// 캐릭터 휴식 설정
        /// </summary>
        public void SetCharacterOnBreak(string characterId, bool isOnBreak)
        {
            if (characterSchedules.ContainsKey(characterId))
            {
                characterSchedules[characterId].isOnBreak = isOnBreak;
            }
        }

        /// <summary>
        /// 시설별 캐릭터 목록 조회
        /// </summary>
        public List<Unit> GetCharactersInFacility(string facilityId)
        {
            if (!facilityAssignments.ContainsKey(facilityId))
                return new List<Unit>();

            var units = new List<Unit>();
            foreach (var characterId in facilityAssignments[facilityId].assignedCharacterIds)
            {
                var unit = GetUnitById(characterId);
                if (unit != null)
                {
                    units.Add(unit);
                }
            }

            return units;
        }

        /// <summary>
        /// 캐릭터의 현재 시설 조회
        /// </summary>
        public GuildManager.Building GetCharacterCurrentFacility(string characterId)
        {
            foreach (var kvp in facilityAssignments)
            {
                if (kvp.Value.assignedCharacterIds.Contains(characterId))
                {
                    return GetBuildingById(kvp.Key);
                }
            }
            return null;
        }

        /// <summary>
        /// 시설 효율성 조회
        /// </summary>
        public float GetFacilityEfficiency(string facilityId)
        {
            if (facilityAssignments.ContainsKey(facilityId))
            {
                return facilityAssignments[facilityId].efficiencyModifier;
            }
            return 1f;
        }

        /// <summary>
        /// 캐릭터 피로도 조회
        /// </summary>
        public float GetCharacterFatigue(string characterId)
        {
            if (characterSchedules.ContainsKey(characterId))
            {
                return characterSchedules[characterId].fatigue;
            }
            return 0f;
        }

        /// <summary>
        /// 시설 생산 데이터 조회
        /// </summary>
        public FacilityProductionData GetFacilityProductionData(string facilityId)
        {
            return productionData.ContainsKey(facilityId) ? productionData[facilityId] : null;
        }

        /// <summary>
        /// 시설 이벤트 기록 조회
        /// </summary>
        public List<FacilityEvent> GetFacilityEventHistory(string facilityId)
        {
            return facilityEventHistory.ContainsKey(facilityId) ? 
                new List<FacilityEvent>(facilityEventHistory[facilityId]) : 
                new List<FacilityEvent>();
        }

        // Helper 메서드들
        Unit GetUnitById(string characterId)
        {
            var guildManager = GameManager.Instance?.GuildManager;
            if (guildManager != null)
            {
                var adventurers = guildManager.GetAdventurers();
                return adventurers.FirstOrDefault(a => a.unitId == characterId);
            }
            return null;
        }

        GuildManager.Building GetBuildingById(string facilityId)
        {
            var guildManager = GameManager.Instance?.GuildManager;
            if (guildManager != null)
            {
                var buildings = guildManager.GetGuildData().Buildings;
                return buildings.FirstOrDefault(b => b.ToString() == facilityId || 
                    $"{b.Type}_{b.Position.x}_{b.Position.y}" == facilityId);
            }
            return null;
        }

        Dictionary<string, GuildManager.Building> GetFacilitiesByType(GuildManager.BuildingType type)
        {
            var facilities = new Dictionary<string, GuildManager.Building>();
            var guildManager = GameManager.Instance?.GuildManager;
            if (guildManager != null)
            {
                var buildings = guildManager.GetGuildData().Buildings;
                foreach (var building in buildings.Where(b => b.Type == type))
                {
                    string facilityId = $"{building.Type}_{building.Position.x}_{building.Position.y}";
                    facilities[facilityId] = building;
                }
            }
            return facilities;
        }

        // 이벤트 핸들러
        void OnBuildingConstructed(GameEvent gameEvent)
        {
            var buildingType = gameEvent.GetParameter<GuildManager.BuildingType>("buildingType");
            var position = gameEvent.GetParameter<Vector2Int>("position");
            string facilityId = $"{buildingType}_{position.x}_{position.y}";
            
            // 새 시설 초기화
            facilityAssignments[facilityId] = new FacilityAssignment(facilityId, buildingType);
            productionData[facilityId] = new FacilityProductionData(facilityId);
        }

        void OnBuildingDestroyed(GameEvent gameEvent)
        {
            var facilityId = gameEvent.GetParameter<string>("facilityId");
            
            // 배치된 캐릭터 해제
            if (facilityAssignments.ContainsKey(facilityId))
            {
                var assignment = facilityAssignments[facilityId];
                var characterIds = new List<string>(assignment.assignedCharacterIds);
                foreach (var characterId in characterIds)
                {
                    UnassignCharacterFromFacility(characterId, facilityId);
                }
                
                facilityAssignments.Remove(facilityId);
            }
            
            // 생산 데이터 제거
            productionData.Remove(facilityId);
            facilityEventHistory.Remove(facilityId);
        }

        void OnDayChanged(GameEvent gameEvent)
        {
            // 일일 초기화
            foreach (var schedule in characterSchedules.Values)
            {
                schedule.workHoursToday = 0f;
                schedule.fatigue = Mathf.Max(0f, schedule.fatigue - 0.3f); // 하루 휴식으로 30% 회복
            }
        }

        void OnDestroy()
        {
            var eventManager = EventManager.Instance;
            if (eventManager != null)
            {
                eventManager.Unsubscribe(GuildMaster.Core.EventType.BuildingConstructed, OnBuildingConstructed);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.BuildingDestroyed, OnBuildingDestroyed);
                eventManager.Unsubscribe(GuildMaster.Core.EventType.DayChanged, OnDayChanged);
            }
        }
    }
}