using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Battle; // Unit을 위해 추가
using GuildMaster.Core;   // ResourceType을 위해 추가

namespace GuildMaster.Exploration
{
    public class DungeonSystem : MonoBehaviour
    {
        private static DungeonSystem _instance;
        public static DungeonSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DungeonSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("DungeonSystem");
                        _instance = go.AddComponent<DungeonSystem>();
                    }
                }
                return _instance;
            }
        }
        
        // 던전 타입
        public enum DungeonType
        {
            Normal,      // 일반 던전
            Elite,       // 엘리트 던전
            Raid,        // 레이드 던전
            Daily,       // 일일 던전
            Event,       // 이벤트 던전
            Infinite     // 무한 던전
        }
        
        // 던전 난이도
        public enum DungeonDifficulty
        {
            Easy,
            Normal,
            Hard,
            Extreme,
            Nightmare
        }
        
        // 던전 정보
        [System.Serializable]
        public class Dungeon
        {
            public int dungeonId;
            public string dungeonName;
            public string description;
            public DungeonType type;
            public DungeonDifficulty difficulty;
            public int recommendedLevel;
            public int recommendedPower;
            
            public int maxFloors; // 최대 층수
            public int entryCost; // 입장 비용
            public int dailyEntryLimit; // 일일 입장 제한
            public float explorationTime; // 탐험 소요 시간 (분)
            
            public List<DungeonFloor> floors;
            public DungeonReward firstClearReward; // 첫 클리어 보상
            public DungeonReward repeatReward; // 반복 보상
            
            public bool isUnlocked;
            public bool isCompleted;
            public int currentFloor;
            public int todayEntries;
            
            public Dungeon()
            {
                floors = new List<DungeonFloor>();
            }
        }
        
        // 던전 층
        [System.Serializable]
        public class DungeonFloor
        {
            public int floorNumber;
            public string floorName;
            public List<DungeonEncounter> encounters;
            public DungeonReward floorReward;
            public bool isCleared;
            
            public DungeonFloor()
            {
                encounters = new List<DungeonEncounter>();
            }
        }
        
        // 던전 조우
        [System.Serializable]
        public class DungeonEncounter
        {
            public enum EncounterType
            {
                Battle,      // 전투
                Treasure,    // 보물
                Trap,        // 함정
                Rest,        // 휴식
                Event,       // 이벤트
                Boss         // 보스
            }
            
            public EncounterType type;
            public string encounterName;
            public string description;
            public float successChance; // 성공 확률
            public DungeonReward reward;
            public int damageOnFail; // 실패 시 피해
        }
        
        // 던전 보상
        [System.Serializable]
        public class DungeonReward
        {
            public int goldReward;
            public int expReward;
            public Dictionary<ResourceType, int> resourceRewards;
            public List<string> itemRewards; // 아이템 ID 리스트
            public List<string> equipmentRewards; // 장비 ID 리스트
            public float unitRewardChance; // 유닛 획득 확률
            public string specialReward; // 특별 보상
            
            public DungeonReward()
            {
                resourceRewards = new Dictionary<ResourceType, int>();
                itemRewards = new List<string>();
                equipmentRewards = new List<string>();
            }
        }
        
        // 탐험 결과
        [System.Serializable]
        public class ExplorationResult
        {
            public bool isSuccess;
            public int floorsCleared;
            public DungeonReward totalRewards;
            public List<Battle.Unit> survivingUnits;
            public List<string> explorationLog;
            public float explorationTime;
            
            public ExplorationResult()
            {
                totalRewards = new DungeonReward();
                survivingUnits = new List<Battle.Unit>();
                explorationLog = new List<string>();
            }
        }
        
        // 던전 데이터
        private List<Dungeon> allDungeons;
        private Dungeon currentDungeon;
        private List<Battle.Squad> explorationSquads;
        private bool isExploring;
        
        // 무한 던전 관련
        private int infiniteDungeonHighestFloor;
        private int infiniteDungeonCurrentFloor;
        
        // 이벤트
        public event Action<Dungeon> OnDungeonUnlocked;
        public event Action<Dungeon> OnDungeonEntered;
        public event Action<DungeonFloor> OnFloorCleared;
        public event Action<ExplorationResult> OnExplorationComplete;
        public event Action<DungeonEncounter> OnEncounterStart;
        public event Action<DungeonReward> OnRewardReceived;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeDungeons();
        }
        
        void InitializeDungeons()
        {
            allDungeons = new List<Dungeon>();
            explorationSquads = new List<Battle.Squad>();
            
            // 던전 데이터 생성
            CreateNormalDungeons();
            CreateDailyDungeons();
            CreateSpecialDungeons();
            CreateInfiniteDungeon();
        }
        
        void CreateNormalDungeons()
        {
            // 초급 던전: 고블린 소굴
            Dungeon goblinDen = new Dungeon
            {
                dungeonId = 1,
                dungeonName = "고블린 소굴",
                description = "약한 고블린들이 모여있는 작은 동굴입니다.",
                type = DungeonType.Normal,
                difficulty = DungeonDifficulty.Easy,
                recommendedLevel = 5,
                recommendedPower = 1000,
                maxFloors = 3,
                entryCost = 100,
                dailyEntryLimit = 3,
                explorationTime = 5f,
                isUnlocked = true
            };
            
            // 층 생성
            for (int i = 1; i <= goblinDen.maxFloors; i++)
            {
                DungeonFloor floor = CreateGoblinFloor(i);
                goblinDen.floors.Add(floor);
            }
            
            // 보상 설정
            goblinDen.firstClearReward = new DungeonReward
            {
                goldReward = 1000,
                expReward = 500,
                unitRewardChance = 0.1f
            };
            goblinDen.repeatReward = new DungeonReward
            {
                goldReward = 300,
                expReward = 150
            };
            
            allDungeons.Add(goblinDen);
            
            // 중급 던전: 오크 요새
            Dungeon orcFortress = new Dungeon
            {
                dungeonId = 2,
                dungeonName = "오크 요새",
                description = "오크 전사들이 지키고 있는 견고한 요새입니다.",
                type = DungeonType.Normal,
                difficulty = DungeonDifficulty.Normal,
                recommendedLevel = 10,
                recommendedPower = 3000,
                maxFloors = 5,
                entryCost = 300,
                dailyEntryLimit = 3,
                explorationTime = 10f
            };
            
            allDungeons.Add(orcFortress);
            
            // 상급 던전: 용의 둥지
            Dungeon dragonNest = new Dungeon
            {
                dungeonId = 3,
                dungeonName = "용의 둥지",
                description = "고대 용이 잠들어 있다는 전설의 동굴입니다.",
                type = DungeonType.Normal,
                difficulty = DungeonDifficulty.Hard,
                recommendedLevel = 20,
                recommendedPower = 10000,
                maxFloors = 7,
                entryCost = 1000,
                dailyEntryLimit = 1,
                explorationTime = 20f
            };
            
            allDungeons.Add(dragonNest);
        }
        
        DungeonFloor CreateGoblinFloor(int floorNumber)
        {
            DungeonFloor floor = new DungeonFloor
            {
                floorNumber = floorNumber,
                floorName = $"고블린 소굴 {floorNumber}층"
            };
            
            // 전투 조우
            floor.encounters.Add(new DungeonEncounter
            {
                type = DungeonEncounter.EncounterType.Battle,
                encounterName = "고블린 무리",
                description = "작은 고블린들이 공격해옵니다!",
                successChance = 0.8f,
                reward = new DungeonReward { goldReward = 100, expReward = 50 }
            });
            
            // 보물 조우
            if (floorNumber > 1)
            {
                floor.encounters.Add(new DungeonEncounter
                {
                    type = DungeonEncounter.EncounterType.Treasure,
                    encounterName = "숨겨진 보물상자",
                    description = "먼지가 쌓인 보물상자를 발견했습니다.",
                    successChance = 0.6f,
                    reward = new DungeonReward { goldReward = 300 }
                });
            }
            
            // 보스 조우 (마지막 층)
            if (floorNumber == 3)
            {
                floor.encounters.Add(new DungeonEncounter
                {
                    type = DungeonEncounter.EncounterType.Boss,
                    encounterName = "고블린 족장",
                    description = "거대한 고블린 족장이 나타났습니다!",
                    successChance = 0.5f,
                    reward = new DungeonReward 
                    { 
                        goldReward = 500, 
                        expReward = 200,
                        unitRewardChance = 0.2f
                    }
                });
            }
            
            return floor;
        }
        
        void CreateDailyDungeons()
        {
            // 요일별 던전
            string[] dailyDungeonNames = 
            {
                "월요일 - 전사의 시련",
                "화요일 - 마법사의 탑",
                "수요일 - 도적의 미로",
                "목요일 - 성직자의 성소",
                "금요일 - 궁수의 사냥터",
                "토요일 - 황금의 동굴",
                "일요일 - 경험의 전당"
            };
            
            for (int i = 0; i < 7; i++)
            {
                Dungeon dailyDungeon = new Dungeon
                {
                    dungeonId = 100 + i,
                    dungeonName = dailyDungeonNames[i],
                    description = "특정 직업이나 자원을 위한 일일 던전입니다.",
                    type = DungeonType.Daily,
                    difficulty = DungeonDifficulty.Normal,
                    recommendedLevel = 10,
                    recommendedPower = 2000,
                    maxFloors = 1,
                    entryCost = 0,
                    dailyEntryLimit = 3,
                    explorationTime = 5f
                };
                
                // 요일별 특별 보상
                DungeonReward specialReward = new DungeonReward();
                switch (i)
                {
                    case 0: // 월요일 - 전사 장비
                        specialReward.equipmentRewards.Add("warrior_equipment");
                        break;
                    case 1: // 화요일 - 마법사 장비
                        specialReward.equipmentRewards.Add("wizard_equipment");
                        break;
                    case 5: // 토요일 - 골드
                        specialReward.goldReward = 5000;
                        break;
                    case 6: // 일요일 - 경험치
                        specialReward.expReward = 2000;
                        break;
                }
                
                dailyDungeon.repeatReward = specialReward;
                allDungeons.Add(dailyDungeon);
            }
        }
        
        void CreateSpecialDungeons()
        {
            // 레이드 던전: 마왕성
            Dungeon demonCastle = new Dungeon
            {
                dungeonId = 500,
                dungeonName = "마왕성",
                description = "4개 부대가 함께 도전하는 대규모 레이드 던전입니다.",
                type = DungeonType.Raid,
                difficulty = DungeonDifficulty.Extreme,
                recommendedLevel = 30,
                recommendedPower = 50000,
                maxFloors = 10,
                entryCost = 5000,
                dailyEntryLimit = 1,
                explorationTime = 60f
            };
            
            allDungeons.Add(demonCastle);
            
            // 이벤트 던전은 동적으로 추가
        }
        
        void CreateInfiniteDungeon()
        {
            Dungeon infiniteDungeon = new Dungeon
            {
                dungeonId = 999,
                dungeonName = "무한의 심연",
                description = "끝이 없는 지하 미궁. 얼마나 깊이 내려갈 수 있을까요?",
                type = DungeonType.Infinite,
                difficulty = DungeonDifficulty.Normal,
                recommendedLevel = 15,
                recommendedPower = 5000,
                maxFloors = int.MaxValue,
                entryCost = 1000,
                dailyEntryLimit = 1,
                explorationTime = 0f // 수동 종료
            };
            
            allDungeons.Add(infiniteDungeon);
        }
        
        // 던전 입장
        public bool EnterDungeon(int dungeonId, List<Battle.Squad> squads)
        {
            Dungeon dungeon = allDungeons.FirstOrDefault(d => d.dungeonId == dungeonId);
            if (dungeon == null || !dungeon.isUnlocked)
            {
                Debug.LogWarning($"Dungeon {dungeonId} is not available");
                return false;
            }
            
            // 입장 제한 체크
            if (dungeon.todayEntries >= dungeon.dailyEntryLimit)
            {
                Debug.LogWarning("Daily entry limit reached");
                return false;
            }
            
            // 입장 비용 체크
            var resourceManager = Core.GameManager.Instance?.ResourceManager;
            if (resourceManager != null && resourceManager.GetGold() < dungeon.entryCost)
            {
                Debug.LogWarning("Not enough gold for entry");
                return false;
            }
            
            // 입장 비용 차감
            if (resourceManager != null)
            {
                resourceManager.AddGold(-dungeon.entryCost);
            }
            
            currentDungeon = dungeon;
            explorationSquads = squads;
            isExploring = true;
            dungeon.todayEntries++;
            
            OnDungeonEntered?.Invoke(dungeon);
            
            // 자동 탐험 시작
            if (dungeon.type != DungeonType.Infinite)
            {
                StartCoroutine(AutoExploration(dungeon));
            }
            
            return true;
        }
        
        // 자동 탐험
        IEnumerator AutoExploration(Dungeon dungeon)
        {
            ExplorationResult result = new ExplorationResult();
            float startTime = Time.time;
            
            // 각 층 탐험
            for (int i = 0; i < dungeon.floors.Count; i++)
            {
                DungeonFloor floor = dungeon.floors[i];
                
                // 층 탐험
                bool floorSuccess = ExploreFloor(floor, result);
                
                if (floorSuccess)
                {
                    floor.isCleared = true;
                    result.floorsCleared++;
                    OnFloorCleared?.Invoke(floor);
                    
                    // 층 보상 추가
                    if (floor.floorReward != null)
                    {
                        AddReward(result.totalRewards, floor.floorReward);
                    }
                }
                else
                {
                    // 실패 시 탐험 종료
                    result.explorationLog.Add($"{floor.floorName} 탐험 실패!");
                    break;
                }
                
                // 탐험 시간 대기 (실제 시간의 1/10)
                yield return new WaitForSeconds(dungeon.explorationTime * 60f / dungeon.maxFloors / 10f);
            }
            
            // 탐험 완료
            result.explorationTime = Time.time - startTime;
            result.isSuccess = result.floorsCleared == dungeon.maxFloors;
            
            if (result.isSuccess && !dungeon.isCompleted)
            {
                dungeon.isCompleted = true;
                // 첫 클리어 보상
                AddReward(result.totalRewards, dungeon.firstClearReward);
            }
            else
            {
                // 반복 보상
                AddReward(result.totalRewards, dungeon.repeatReward);
            }
            
            // 보상 지급
            ApplyRewards(result.totalRewards);
            
            isExploring = false;
            currentDungeon = null;
            
            OnExplorationComplete?.Invoke(result);
        }
        
        // 층 탐험
        bool ExploreFloor(DungeonFloor floor, ExplorationResult result)
        {
            result.explorationLog.Add($"=== {floor.floorName} 진입 ===");
            
            foreach (var encounter in floor.encounters)
            {
                OnEncounterStart?.Invoke(encounter);
                
                bool encounterSuccess = ProcessEncounter(encounter, result);
                
                if (!encounterSuccess && encounter.type == DungeonEncounter.EncounterType.Boss)
                {
                    // 보스 실패 시 탐험 종료
                    return false;
                }
            }
            
            return true;
        }
        
        // 조우 처리
        bool ProcessEncounter(DungeonEncounter encounter, ExplorationResult result)
        {
            result.explorationLog.Add($"[{encounter.type}] {encounter.encounterName}");
            
            // 성공 확률 계산 (부대 전투력 기반)
            float squadPower = CalculateSquadPower();
            float successModifier = squadPower / (currentDungeon.recommendedPower * 1.5f);
            float finalSuccessChance = Mathf.Clamp(encounter.successChance * successModifier, 0.1f, 0.95f);
            
            bool success = UnityEngine.Random.value < finalSuccessChance;
            
            if (success)
            {
                result.explorationLog.Add($"✓ {encounter.encounterName} 성공!");
                
                if (encounter.reward != null)
                {
                    AddReward(result.totalRewards, encounter.reward);
                }
                
                // 특수 조우 처리
                switch (encounter.type)
                {
                    case DungeonEncounter.EncounterType.Rest:
                        // HP/MP 회복
                        HealSquads(0.3f);
                        result.explorationLog.Add("부대가 휴식을 취해 체력을 회복했습니다.");
                        break;
                        
                    case DungeonEncounter.EncounterType.Event:
                        // 특별 이벤트
                        ProcessSpecialEvent(encounter, result);
                        break;
                }
            }
            else
            {
                result.explorationLog.Add($"✗ {encounter.encounterName} 실패!");
                
                // 피해 적용
                if (encounter.damageOnFail > 0)
                {
                    DamageSquads(encounter.damageOnFail);
                    result.explorationLog.Add($"부대가 {encounter.damageOnFail}의 피해를 입었습니다.");
                }
            }
            
            return success;
        }
        
        // 부대 전투력 계산
        float CalculateSquadPower()
        {
            float totalPower = 0f;
            foreach (var squad in explorationSquads)
            {
                if (squad != null)
                {
                    totalPower += squad.totalPower;
                }
            }
            return totalPower;
        }
        
        // 부대 치유
        void HealSquads(float percentage)
        {
            foreach (var squad in explorationSquads)
            {
                if (squad != null)
                {
                    foreach (var unit in squad.GetAliveUnits())
                    {
                        unit.Heal(unit.maxHP * percentage);
                        unit.RestoreMana(unit.maxMP * percentage);
                    }
                }
            }
        }
        
        // 부대 피해
        void DamageSquads(int damage)
        {
            foreach (var squad in explorationSquads)
            {
                if (squad != null)
                {
                    foreach (var unit in squad.GetAliveUnits())
                    {
                        unit.TakeDamage(damage);
                    }
                }
            }
        }
        
        // 특별 이벤트 처리
        void ProcessSpecialEvent(DungeonEncounter encounter, ExplorationResult result)
        {
            // 랜덤 이벤트
            int eventType = UnityEngine.Random.Range(0, 3);
            switch (eventType)
            {
                case 0: // 숨겨진 보물
                    result.totalRewards.goldReward += 1000;
                    result.explorationLog.Add("숨겨진 보물을 발견했습니다! (+1000 골드)");
                    break;
                    
                case 1: // 신비한 샘
                    HealSquads(1f);
                    result.explorationLog.Add("신비한 샘에서 모든 부대가 완전히 회복되었습니다!");
                    break;
                    
                case 2: // 떠돌이 상인
                    result.explorationLog.Add("떠돌이 상인을 만났지만 아무것도 사지 않았습니다.");
                    break;
            }
        }
        
        // 보상 합산
        void AddReward(DungeonReward total, DungeonReward add)
        {
            if (add == null) return;
            
            total.goldReward += add.goldReward;
            total.expReward += add.expReward;
            
            foreach (var resource in add.resourceRewards)
            {
                if (total.resourceRewards.ContainsKey(resource.Key))
                {
                    total.resourceRewards[resource.Key] += resource.Value;
                }
                else
                {
                    total.resourceRewards[resource.Key] = resource.Value;
                }
            }
            
            total.itemRewards.AddRange(add.itemRewards);
            total.equipmentRewards.AddRange(add.equipmentRewards);
            
            // 유닛 보상 확률
            if (UnityEngine.Random.value < add.unitRewardChance)
            {
                total.unitRewardChance = 1f; // 획득 확정
            }
        }
        
        // 보상 지급
        void ApplyRewards(DungeonReward rewards)
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager == null) return;
            
            // 골드
            if (rewards.goldReward > 0 && gameManager.ResourceManager != null)
            {
                gameManager.ResourceManager.AddGold(rewards.goldReward);
            }
            
            // 경험치
            if (rewards.expReward > 0)
            {
                // 부대 경험치 분배
                foreach (var squad in explorationSquads)
                {
                    if (squad != null)
                    {
                        foreach (var unit in squad.GetAliveUnits())
                        {
                            unit.AddExperience(rewards.expReward / squad.GetAliveUnits().Count);
                        }
                    }
                }
            }
            
            // 자원
            if (gameManager.ResourceManager != null)
            {
                foreach (var resource in rewards.resourceRewards)
                {
                    switch (resource.Key)
                    {
                        case ResourceType.Wood:
                            gameManager.ResourceManager.AddWood(resource.Value);
                            break;
                        case ResourceType.Stone:
                            gameManager.ResourceManager.AddStone(resource.Value);
                            break;
                        case ResourceType.ManaStone:
                            gameManager.ResourceManager.AddManaStone(resource.Value);
                            break;
                    }
                }
            }
            
            // 아이템/장비 (TODO: 실제 아이템 시스템과 연동)
            foreach (string itemId in rewards.itemRewards)
            {
                Debug.Log($"Item reward: {itemId}");
            }
            
            foreach (string equipmentId in rewards.equipmentRewards)
            {
                Debug.Log($"Equipment reward: {equipmentId}");
            }
            
            // 유닛 보상
            if (rewards.unitRewardChance >= 1f)
            {
                // TODO: 랜덤 유닛 생성 및 추가
                Debug.Log("Unit reward earned!");
            }
            
            OnRewardReceived?.Invoke(rewards);
        }
        
        // 무한 던전 관련
        public void ProgressInfiniteDungeon()
        {
            if (currentDungeon == null || currentDungeon.type != DungeonType.Infinite)
                return;
            
            infiniteDungeonCurrentFloor++;
            
            // 동적으로 층 생성
            DungeonFloor newFloor = GenerateInfiniteFloor(infiniteDungeonCurrentFloor);
            currentDungeon.floors.Add(newFloor);
            
            // 최고 기록 갱신
            if (infiniteDungeonCurrentFloor > infiniteDungeonHighestFloor)
            {
                infiniteDungeonHighestFloor = infiniteDungeonCurrentFloor;
            }
        }
        
        DungeonFloor GenerateInfiniteFloor(int floorNumber)
        {
            DungeonFloor floor = new DungeonFloor
            {
                floorNumber = floorNumber,
                floorName = $"무한의 심연 {floorNumber}층"
            };
            
            // 층이 깊어질수록 난이도 증가
            int encounterCount = 3 + (floorNumber / 10);
            float difficultyMultiplier = 1f + (floorNumber * 0.1f);
            
            for (int i = 0; i < encounterCount; i++)
            {
                DungeonEncounter encounter = new DungeonEncounter
                {
                    type = (DungeonEncounter.EncounterType)UnityEngine.Random.Range(0, 5),
                    encounterName = $"조우 {i + 1}",
                    successChance = Mathf.Max(0.3f, 0.8f - (floorNumber * 0.01f)),
                    damageOnFail = Mathf.RoundToInt(10 * difficultyMultiplier)
                };
                
                // 보상도 증가
                encounter.reward = new DungeonReward
                {
                    goldReward = Mathf.RoundToInt(100 * difficultyMultiplier),
                    expReward = Mathf.RoundToInt(50 * difficultyMultiplier)
                };
                
                floor.encounters.Add(encounter);
            }
            
            // 10층마다 보스
            if (floorNumber % 10 == 0)
            {
                floor.encounters.Add(new DungeonEncounter
                {
                    type = DungeonEncounter.EncounterType.Boss,
                    encounterName = $"심연의 수호자 {floorNumber / 10}",
                    successChance = 0.5f,
                    damageOnFail = Mathf.RoundToInt(50 * difficultyMultiplier),
                    reward = new DungeonReward
                    {
                        goldReward = Mathf.RoundToInt(1000 * difficultyMultiplier),
                        expReward = Mathf.RoundToInt(500 * difficultyMultiplier),
                        unitRewardChance = 0.1f * (floorNumber / 10)
                    }
                });
            }
            
            return floor;
        }
        
        // 던전 해금
        public void UnlockDungeon(int dungeonId)
        {
            Dungeon dungeon = allDungeons.FirstOrDefault(d => d.dungeonId == dungeonId);
            if (dungeon != null && !dungeon.isUnlocked)
            {
                dungeon.isUnlocked = true;
                OnDungeonUnlocked?.Invoke(dungeon);
            }
        }
        
        // 일일 리셋
        public void DailyReset()
        {
            foreach (var dungeon in allDungeons)
            {
                dungeon.todayEntries = 0;
            }
            
            // 일일 던전 갱신
            UpdateDailyDungeons();
        }
        
        void UpdateDailyDungeons()
        {
            // 오늘 요일에 맞는 일일 던전만 활성화
            int dayOfWeek = (int)DateTime.Now.DayOfWeek;
            
            for (int i = 0; i < 7; i++)
            {
                var dailyDungeon = allDungeons.FirstOrDefault(d => d.dungeonId == 100 + i);
                if (dailyDungeon != null)
                {
                    dailyDungeon.isUnlocked = (i == dayOfWeek);
                }
            }
        }
        
        // 조회 메서드들
        public List<Dungeon> GetAvailableDungeons()
        {
            return allDungeons.Where(d => d.isUnlocked).ToList();
        }
        
        public List<Dungeon> GetDungeonsByType(DungeonType type)
        {
            return allDungeons.Where(d => d.type == type && d.isUnlocked).ToList();
        }
        
        public Dungeon GetDungeon(int dungeonId)
        {
            return allDungeons.FirstOrDefault(d => d.dungeonId == dungeonId);
        }
        
        public int GetInfiniteHighScore()
        {
            return infiniteDungeonHighestFloor;
        }
        
        public bool IsExploring()
        {
            return isExploring;
        }
        
        public float GetExplorationProgress()
        {
            if (currentDungeon == null || !isExploring)
                return 0f;
            
            int clearedFloors = currentDungeon.floors.Count(f => f.isCleared);
            return (float)clearedFloors / currentDungeon.maxFloors;
        }
    }
}