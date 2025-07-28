using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Battle;
using TacticalTileGame.Data;
// using Unit = GuildMaster.Battle.Unit; // Not needed - Unit is already in the namespace

namespace GuildMaster.Data
{
    /// <summary>
    /// 통합 캐릭터 데이터베이스 - 모든 캐릭터 데이터의 중앙 관리소
    /// UI, 전술게임, CSV 시스템이 모두 이를 참조합니다
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDatabaseSO", menuName = "Twelve Game/Data/Character Database")]
    public class CharacterDatabaseSO : ScriptableObject
    {
            [SerializeField] public List<CharacterDataSO> characters = new List<CharacterDataSO>();
    [SerializeField] public List<CharacterData> tacticalCharacters = new List<CharacterData>(); // TacticalCharacterDataSO → CharacterData 통합
        
        private Dictionary<string, CharacterDataSO> characterLookup;
        private Dictionary<string, CharacterData> tacticalLookup; // TacticalCharacterDataSO → CharacterData 통합
        
        public void Initialize()
        {
            // CSV 데이터가 없으면 자동으로 로드 시도
            if (tacticalCharacters.Count == 0)
            {
                AutoLoadFromCSV();
            }

            // 룩업 테이블 생성
            BuildLookupTables();
            
            Debug.Log($"✅ CharacterDatabase 초기화 완료 - Characters: {characters.Count}, Tactical: {tacticalCharacters.Count}");
        }

        /// <summary>
        /// CSV에서 자동으로 데이터 로드
        /// </summary>
        private void AutoLoadFromCSV()
        {
            #if UNITY_EDITOR
            try
            {
                Debug.Log("🔄 CSV에서 캐릭터 데이터 자동 로드 중...");
                
                // RuntimeCharacterGenerator를 통해 테스트 캐릭터 생성
                var generator = FindObjectOfType<RuntimeCharacterGenerator>();
                if (generator != null)
                {
                    generator.GenerateTestCharacters();
                }
                else
                {
                    // CSV 매니저를 통해 데이터 로드 시도
                    GuildMaster.CSV.CSVDataSystemManager.ImportCharacterData();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ CSV 자동 로드 실패: {e.Message}");
            }
            #endif
        }

        /// <summary>
        /// 룩업 테이블 구성
        /// </summary>
        private void BuildLookupTables()
        {
            // CharacterDataSO 룩업 테이블 생성
            characterLookup = new Dictionary<string, CharacterDataSO>();
            foreach (var character in characters)
            {
                if (!string.IsNullOrEmpty(character.id) && !characterLookup.ContainsKey(character.id))
                {
                    characterLookup.Add(character.id, character);
                }
            }
            
            // CharacterData 룩업 테이블 생성 (통합된 전술 캐릭터)
            tacticalLookup = new Dictionary<string, CharacterData>();
            foreach (var tactical in tacticalCharacters)
            {
                if (!string.IsNullOrEmpty(tactical.characterId) && !tacticalLookup.ContainsKey(tactical.characterId))
                {
                    tacticalLookup.Add(tactical.characterId, tactical);
                }
            }
        }
        
        public CharacterDataSO GetCharacter(string id)
        {
            return characters.FirstOrDefault(c => c.id == id);
        }
        
        public CharacterDataSO GetCharacterByName(string name)
        {
            return characters.FirstOrDefault(c => c.characterName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        
        public CharacterDataSO GetRandomCharacter()
        {
            if (characters.Count == 0) return null;
            int randomIndex = UnityEngine.Random.Range(0, characters.Count);
            return characters[randomIndex];
        }
        
        public CharacterDataSO GetRandomCharacterByRarity(GuildMaster.Data.Rarity rarity)
        {
            var filtered = characters.Where(c => c.rarity == rarity).ToList();
            if (filtered.Count == 0) return null;
            int randomIndex = UnityEngine.Random.Range(0, filtered.Count);
            return filtered[randomIndex];
        }
        
        public List<CharacterDataSO> GetCharactersByClass(JobClass jobClass)
        {
            return characters.Where(c => c.jobClass == jobClass).ToList();
        }
        
        public List<CharacterDataSO> GetCharactersByRarity(GuildMaster.Data.Rarity rarity)
        {
            return characters.Where(c => c.rarity == rarity).ToList();
        }
        
        // === 전술 캐릭터 관련 메서드들 ===
        public CharacterData GetTacticalCharacter(string id)
        {
            if (tacticalLookup != null && tacticalLookup.TryGetValue(id, out CharacterData tactical))
                return tactical;
            return tacticalCharacters.FirstOrDefault(c => c.characterId == id);
        }
        
        public List<CharacterData> GetTacticalCharactersByClass(TacticalTileGame.Data.CharacterClass characterClass)
        {
            return tacticalCharacters.Where(c => c.jobClass == (JobClass)characterClass).ToList();
        }
        
        public List<CharacterData> GetTacticalCharactersByRarity(TacticalTileGame.Data.CharacterRarity rarity)
        {
            return tacticalCharacters.Where(c => c.tacticalRarity == rarity).ToList();
        }
        
        public List<CharacterData> GetAllTacticalCharacters()
        {
            return new List<CharacterData>(tacticalCharacters);
        }

        /// <summary>
        /// 데이터베이스에 캐릭터 추가 (에디터 전용)
        /// </summary>
        #if UNITY_EDITOR
        public void AddCharacters(List<CharacterData> newCharacters)
        {
            if (newCharacters == null) return;

            tacticalCharacters.Clear();
            tacticalCharacters.AddRange(newCharacters);
            
            // CharacterData를 CharacterDataSO로 변환하여 characters 리스트에도 추가
            SyncCharacterDataSOFromTactical();
            
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            
            Debug.Log($"✅ CharacterDatabaseSO에 {newCharacters.Count}개 캐릭터 추가됨 (양쪽 리스트 동기화)");
            
            // 룩업 테이블 재구성
            BuildLookupTables();
        }

        /// <summary>
        /// tacticalCharacters에서 characters로 데이터 동기화
        /// </summary>
        private void SyncCharacterDataSOFromTactical()
        {
            characters.Clear();
            
            foreach (var tacticalChar in tacticalCharacters)
            {
                var characterDataSO = ScriptableObject.CreateInstance<CharacterDataSO>();
                
                // 기본 정보 복사
                characterDataSO.id = tacticalChar.characterId;
                characterDataSO.characterName = tacticalChar.characterName;
                characterDataSO.jobClass = tacticalChar.jobClass;
                characterDataSO.level = tacticalChar.level;
                characterDataSO.rarity = tacticalChar.rarity;
                characterDataSO.description = tacticalChar.description;
                characterDataSO.skillId = ""; // tacticalChar에는 skillId가 없으므로 빈 값
                
                // 기본 스탯 복사
                characterDataSO.baseHP = tacticalChar.baseHP;
                characterDataSO.baseMP = tacticalChar.baseMP;
                characterDataSO.baseAttack = tacticalChar.baseAttack;
                characterDataSO.baseDefense = tacticalChar.baseDefense;
                characterDataSO.baseMagicPower = tacticalChar.baseMagicPower;
                
                // 전투 스탯 복사
                characterDataSO.critRate = tacticalChar.critRate;
                characterDataSO.critDamage = tacticalChar.critDamage;
                characterDataSO.accuracy = tacticalChar.accuracy;
                characterDataSO.evasion = tacticalChar.evasion;
                
                characters.Add(characterDataSO);
            }
            
            Debug.Log($"🔄 {characters.Count}개 CharacterDataSO 생성 및 동기화 완료");
        }

        public void ClearAllCharacters()
        {
            characters.Clear();
            tacticalCharacters.Clear();
            
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            
            Debug.Log("🧹 CharacterDatabaseSO 모든 캐릭터 삭제됨");
        }
        #endif
        
        // === CSV 데이터와의 호환성을 위한 메서드들 ===
        public CSVCharacter GetCSVCharacter(string id)
        {
            var charData = GetCharacter(id);
            if (charData != null)
            {
                return CharacterConverter.FromCharacterDataSO(charData);
            }
            return null;
        }
        
        public List<CSVCharacter> GetAllCSVCharacters()
        {
            return characters.Select(c => CharacterConverter.FromCharacterDataSO(c)).ToList();
        }
    }

    [System.Serializable]
    public class CharacterDataSO : ScriptableObject
    {
        public string id;
        public string characterName;
        public JobClass jobClass;
        public int baseLevel;
        public int level = 1; // CSV Editor에서 사용하는 level 속성
        public Rarity rarity;
        public int baseHP;
        public int baseMP;
        public int baseAttack;
        public int baseDefense;
        public int baseMagicPower;
        // baseSpeed 제거됨
        public float critRate;
        public float critDamage;
        public float accuracy;
        public float evasion;
        public string skillId; // 스킬 하나로 통합
        public string description;
        public Sprite portrait;
        public GameObject modelPrefab;

        // NOTE: CreateUnit method removed as Unit class does not exist.
        // Use CharacterUnit component instead when creating units in battle.
    }
}