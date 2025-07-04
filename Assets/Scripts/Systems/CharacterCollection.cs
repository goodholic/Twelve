using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Battle;
using GuildMaster.Core;
using GuildMaster.Data;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 36명의 캐릭터 수집 및 관리 시스템
    /// 플레이어는 최대 36명의 캐릭터를 수집하고 2부대로 편성할 수 있음
    /// </summary>
    public class CharacterCollection : MonoBehaviour
    {
        private static CharacterCollection _instance;
        public static CharacterCollection Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CharacterCollection>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CharacterCollection");
                        _instance = go.AddComponent<CharacterCollection>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("수집 설정")]
        public const int MAX_CHARACTERS = 36;  // 최대 36명 수집 가능
        public const int SQUADS_COUNT = 2;     // 2개 부대
        public const int UNITS_PER_SQUAD = 18;  // 부대당 18명

        [Header("현재 컬렉션")]
        [SerializeField] private List<GuildMaster.Battle.Unit> collectedCharacters = new List<GuildMaster.Battle.Unit>();
        [SerializeField] private Squad[] battleSquads = new Squad[SQUADS_COUNT];

        // 이벤트
        public event Action<GuildMaster.Battle.Unit> OnCharacterAdded;
        public event Action<GuildMaster.Battle.Unit> OnCharacterRemoved;
        public event Action<int> OnSquadUpdated;  // squadIndex
        public event Action OnCollectionFull;

        // 프로퍼티
        public List<GuildMaster.Battle.Unit> CollectedCharacters => new List<GuildMaster.Battle.Unit>(collectedCharacters);
        public int CurrentCharacterCount => collectedCharacters.Count;
        public bool IsCollectionFull => collectedCharacters.Count >= MAX_CHARACTERS;
        public float CollectionProgress => (float)collectedCharacters.Count / MAX_CHARACTERS;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeSquads();
        }

        void InitializeSquads()
        {
            for (int i = 0; i < SQUADS_COUNT; i++)
            {
                battleSquads[i] = new Squad($"부대 {i + 1}", i, true);
            }
        }

        /// <summary>
        /// 새로운 캐릭터 추가
        /// </summary>
        public bool AddCharacter(GuildMaster.Battle.Unit newCharacter)
        {
            if (newCharacter == null)
            {
                Debug.LogError("추가하려는 캐릭터가 null입니다!");
                return false;
            }

            if (IsCollectionFull)
            {
                Debug.LogWarning($"캐릭터 컬렉션이 가득 찼습니다! (최대 {MAX_CHARACTERS}명)");
                OnCollectionFull?.Invoke();
                return false;
            }

            // 중복 체크
            if (collectedCharacters.Contains(newCharacter))
            {
                Debug.LogWarning($"{newCharacter.Name}은(는) 이미 컬렉션에 있습니다!");
                return false;
            }

            collectedCharacters.Add(newCharacter);
            OnCharacterAdded?.Invoke(newCharacter);

            Debug.Log($"{newCharacter.Name}을(를) 컬렉션에 추가했습니다! ({CurrentCharacterCount}/{MAX_CHARACTERS})");
            
            if (IsCollectionFull)
            {
                Debug.Log("🎉 캐릭터 컬렉션을 모두 모았습니다!");
                OnCollectionFull?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// 캐릭터 제거
        /// </summary>
        public bool RemoveCharacter(GuildMaster.Battle.Unit character)
        {
            if (character == null) return false;

            // 부대에서 먼저 제거
            for (int i = 0; i < SQUADS_COUNT; i++)
            {
                if (battleSquads[i].Units.Contains(character))
                {
                    battleSquads[i].RemoveUnit(character);
                    OnSquadUpdated?.Invoke(i);
                }
            }

            if (collectedCharacters.Remove(character))
            {
                OnCharacterRemoved?.Invoke(character);
                Debug.Log($"{character.Name}을(를) 컬렉션에서 제거했습니다. ({CurrentCharacterCount}/{MAX_CHARACTERS})");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 캐릭터를 부대에 배치
        /// </summary>
        public bool AssignCharacterToSquad(GuildMaster.Battle.Unit character, int squadIndex, int row, int col)
        {
            if (character == null || squadIndex < 0 || squadIndex >= SQUADS_COUNT)
                return false;

            if (!collectedCharacters.Contains(character))
            {
                Debug.LogError($"{character.Name}은(는) 컬렉션에 없는 캐릭터입니다!");
                return false;
            }

            // 다른 부대에서 제거
            for (int i = 0; i < SQUADS_COUNT; i++)
            {
                if (i != squadIndex && battleSquads[i].Units.Contains(character))
                {
                    battleSquads[i].RemoveUnit(character);
                    OnSquadUpdated?.Invoke(i);
                }
            }

            // 선택한 부대에 배치
            bool success = battleSquads[squadIndex].AddUnit(character, row, col);
            if (success)
            {
                OnSquadUpdated?.Invoke(squadIndex);
                Debug.Log($"{character.Name}을(를) 부대 {squadIndex + 1}에 배치했습니다.");
            }

            return success;
        }

        /// <summary>
        /// 부대에서 캐릭터 제거
        /// </summary>
        public bool RemoveCharacterFromSquad(GuildMaster.Battle.Unit character, int squadIndex)
        {
            if (squadIndex < 0 || squadIndex >= SQUADS_COUNT)
                return false;

            bool success = battleSquads[squadIndex].RemoveUnit(character);
            if (success)
            {
                OnSquadUpdated?.Invoke(squadIndex);
            }

            return success;
        }

        /// <summary>
        /// 특정 부대 가져오기
        /// </summary>
        public Squad GetSquad(int squadIndex)
        {
            if (squadIndex < 0 || squadIndex >= SQUADS_COUNT)
                return null;

            return battleSquads[squadIndex];
        }

        /// <summary>
        /// 전투용 부대 리스트 가져오기
        /// </summary>
        public List<Squad> GetBattleSquads()
        {
            return battleSquads.Where(s => s.AliveUnitsCount > 0).ToList();
        }

        /// <summary>
        /// 캐릭터가 속한 부대 인덱스 찾기
        /// </summary>
        public int GetCharacterSquadIndex(GuildMaster.Battle.Unit character)
        {
            for (int i = 0; i < SQUADS_COUNT; i++)
            {
                if (battleSquads[i].Units.Contains(character))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 컬렉션 통계 가져오기
        /// </summary>
        public CollectionStats GetCollectionStats()
        {
            var stats = new CollectionStats
            {
                TotalCharacters = CurrentCharacterCount,
                MaxCharacters = MAX_CHARACTERS,
                CollectionProgress = CollectionProgress,
                CharactersByJob = new Dictionary<GuildMaster.Battle.JobClass, int>(),
                CharactersByRarity = new Dictionary<CharacterRarity, int>(),
                AverageLevel = 0f,
                TotalCombatPower = 0f
            };

            // 직업별 통계
            foreach (var character in collectedCharacters)
            {
                if (!stats.CharactersByJob.ContainsKey(character.JobClass))
                    stats.CharactersByJob[character.JobClass] = 0;
                stats.CharactersByJob[character.JobClass]++;

                if (!stats.CharactersByRarity.ContainsKey((CharacterRarity)character.rarity))
                    stats.CharactersByRarity[(CharacterRarity)character.rarity] = 0;
                stats.CharactersByRarity[(CharacterRarity)character.rarity]++;

                stats.AverageLevel += character.level;
                stats.TotalCombatPower += character.GetCombatPower();
            }

            if (CurrentCharacterCount > 0)
            {
                stats.AverageLevel /= CurrentCharacterCount;
            }

            return stats;
        }

        /// <summary>
        /// 자동 부대 편성 (최적화)
        /// </summary>
        public void AutoAssignSquads()
        {
            // 모든 부대 초기화
            foreach (var squad in battleSquads)
            {
                squad.ClearSquad();
            }

            // 캐릭터를 전투력 순으로 정렬
            var sortedCharacters = collectedCharacters
                .OrderByDescending(c => c.GetCombatPower())
                .ToList();

            int squadIndex = 0;
            foreach (var character in sortedCharacters)
            {
                // 각 부대에 순차적으로 배치
                battleSquads[squadIndex].AddUnit(character);
                squadIndex = (squadIndex + 1) % SQUADS_COUNT;
            }

            // 부대 업데이트 이벤트 발생
            for (int i = 0; i < SQUADS_COUNT; i++)
            {
                OnSquadUpdated?.Invoke(i);
            }

            Debug.Log("자동 부대 편성이 완료되었습니다!");
        }

        /// <summary>
        /// 세이브 데이터 가져오기
        /// </summary>
        public CollectionSaveData GetSaveData()
        {
            var saveData = new CollectionSaveData();
            
            // 수집된 캐릭터 ID들 저장 (string을 int로 변환)
            foreach (var character in collectedCharacters)
            {
                if (int.TryParse(character.unitId, out int id))
                {
                    saveData.CollectedCharacterIds.Add(id);
                }
            }

            // 부대 데이터 저장
            for (int i = 0; i < SQUADS_COUNT; i++)
            {
                var squadData = new SquadSaveData
                {
                    SquadIndex = i
                };

                var squadUnits = battleSquads[i].GetAllUnits();
                foreach (var unit in squadUnits)
                {
                    if (int.TryParse(unit.unitId, out int id))
                    {
                        squadData.CharacterIds.Add(id);
                    }
                }

                saveData.SquadData.Add(squadData);
            }

            return saveData;
        }

        /// <summary>
        /// 세이브 데이터 로드
        /// </summary>
        public void LoadSaveData(CollectionSaveData saveData)
        {
            if (saveData == null) return;

            // 컬렉션 초기화
            collectedCharacters.Clear();
            
            // 저장된 캐릭터들 복원 (int ID를 string으로 변환하여 검색)
            foreach (int characterId in saveData.CollectedCharacterIds)
            {
                var unit = CharacterManager.Instance.CreateCharacter(characterId.ToString(), 1);
                if (unit != null)
                {
                    collectedCharacters.Add(unit);
                }
            }

            // 부대 데이터 복원
            foreach (var squadData in saveData.SquadData)
            {
                if (squadData.SquadIndex >= 0 && squadData.SquadIndex < SQUADS_COUNT)
                {
                    battleSquads[squadData.SquadIndex].ClearSquad();
                    
                    foreach (int characterId in squadData.CharacterIds)
                    {
                        var character = collectedCharacters.FirstOrDefault(c => c.unitId == characterId.ToString());
                        if (character != null)
                        {
                            battleSquads[squadData.SquadIndex].AddUnit(character);
                        }
                    }
                }
            }

            Debug.Log($"컬렉션 로드 완료: {CurrentCharacterCount}명의 캐릭터");
        }
    }

    /// <summary>
    /// 컬렉션 통계 데이터
    /// </summary>
    [System.Serializable]
    public class CollectionStats
    {
        public int TotalCharacters;
        public int MaxCharacters;
        public float CollectionProgress;
        public Dictionary<GuildMaster.Battle.JobClass, int> CharactersByJob;
        public Dictionary<CharacterRarity, int> CharactersByRarity;
        public float AverageLevel;
        public float TotalCombatPower;
    }

    /// <summary>
    /// 컬렉션 저장 데이터
    /// </summary>
    [System.Serializable]
    public class CollectionSaveData
    {
        public List<int> CollectedCharacterIds = new List<int>();
        public List<SquadSaveData> SquadData = new List<SquadSaveData>();
    }

    /// <summary>
    /// 부대 저장 데이터
    /// </summary>
    [System.Serializable]
    public class SquadSaveData
    {
        public int SquadIndex;
        public List<int> CharacterIds = new List<int>();
    }
}