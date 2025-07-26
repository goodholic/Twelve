using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Data;

namespace TacticalTileGame.Data
{
    /// <summary>
    /// 타일 전략 게임의 모든 데이터를 관리하는 매니저
    /// </summary>
    public class TacticalDataManager : MonoBehaviour
    {
        private static TacticalDataManager instance;
        public static TacticalDataManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<TacticalDataManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("TacticalDataManager");
                        instance = go.AddComponent<TacticalDataManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        [Header("통합 데이터베이스")]
        [SerializeField] private CharacterDatabaseSO mainDatabase;
        [SerializeField] private string mainDatabasePath = "CharacterDatabase";
        
        [Header("추가 데이터 경로")]
        [SerializeField] private string skillDataPath = "ScriptableObjects/Skills";
        [SerializeField] private string dialogueDataPath = "ScriptableObjects/Dialogues";
        
        [Header("로드된 데이터")]
        private Dictionary<string, TacticalSkillDataSO> skillDatabase = new Dictionary<string, TacticalSkillDataSO>();
        // StoryDialogueDataSO 타입이 삭제되어 주석 처리
        // private Dictionary<string, StoryDialogueDataSO> dialogueDatabase = new Dictionary<string, StoryDialogueDataSO>();
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadAllData();
        }
        
        /// <summary>
        /// 모든 데이터 로드
        /// </summary>
        private void LoadAllData()
        {
            LoadCharacterData();
            LoadSkillData();
            // LoadDialogueData(); // StoryDialogueDataSO 타입이 삭제되어 주석 처리
            
            // 스킬 참조 연결
            LinkSkillsToCharacters();
            
            int characterCount = mainDatabase != null ? mainDatabase.tacticalCharacters.Count : 0;
            Debug.Log($"Data Loading Complete - Characters: {characterCount}, Skills: {skillDatabase.Count}");
        }
        
        /// <summary>
        /// 메인 데이터베이스 로드 (통합된 CharacterDatabaseSO 사용)
        /// </summary>
        private void LoadCharacterData()
        {
            if (mainDatabase == null)
            {
                mainDatabase = Resources.Load<CharacterDatabaseSO>(mainDatabasePath);
                if (mainDatabase == null)
                {
                    Debug.LogError($"CharacterDatabaseSO not found at path: {mainDatabasePath}");
                    return;
                }
            }
            
            mainDatabase.Initialize();
            Debug.Log($"Main database loaded with {mainDatabase.characters.Count} base characters and {mainDatabase.tacticalCharacters.Count} tactical characters");
        }
        
        /// <summary>
        /// 스킬 데이터 로드
        /// </summary>
        private void LoadSkillData()
        {
            TacticalSkillDataSO[] skills = Resources.LoadAll<TacticalSkillDataSO>(skillDataPath);
            
            skillDatabase.Clear();
            
            foreach (var skill in skills)
            {
                skillDatabase[skill.skillId] = skill;
            }
        }
        
        /// <summary>
        /// 대화 데이터 로드 - StoryDialogueDataSO 타입이 삭제되어 주석 처리
        /// </summary>
        /*
        private void LoadDialogueData()
        {
            StoryDialogueDataSO[] dialogues = Resources.LoadAll<StoryDialogueDataSO>(dialogueDataPath);
            
            dialogueDatabase.Clear();
            
            foreach (var dialogue in dialogues)
            {
                dialogueDatabase[dialogue.dialogueId] = dialogue;
            }
        }
        */
        
        /// <summary>
        /// 캐릭터에 스킬 참조 연결
        /// </summary>
        private void LinkSkillsToCharacters()
        {
            foreach (var character in mainDatabase.GetAllTacticalCharacters())
            {
                character.skills.Clear();
                
                foreach (string skillId in character.skillIds)
                {
                    if (skillDatabase.TryGetValue(skillId, out TacticalSkillDataSO skill))
                    {
                        character.skills.Add(skill);
                    }
                    else
                    {
                        Debug.LogWarning($"Skill {skillId} not found for character {character.characterId}");
                    }
                }
            }
        }
        
        #region 캐릭터 관련 메서드
        
        /// <summary>
        /// ID로 캐릭터 데이터 가져오기 (통합 데이터베이스 사용)
        /// </summary>
        public CharacterData GetCharacter(string characterId)
        {
            if (mainDatabase == null)
            {
                Debug.LogError("Main database is null");
                return null;
            }
            
            var character = mainDatabase.GetTacticalCharacter(characterId);
            if (character == null)
            {
                Debug.LogWarning($"Character with ID '{characterId}' not found in main database");
            }
            return character;
        }
        
        /// <summary>
        /// 클래스별 캐릭터 목록 가져오기 (통합 데이터베이스 사용)
        /// </summary>
        public List<CharacterData> GetCharactersByClass(CharacterClass characterClass)
        {
            if (mainDatabase == null) return new List<CharacterData>();
            return mainDatabase.GetTacticalCharactersByClass(characterClass);
        }
        
        /// <summary>
        /// 레어도별 캐릭터 목록 가져오기 (통합 데이터베이스 사용)
        /// </summary>
        public List<CharacterData> GetCharactersByRarity(CharacterRarity rarity)
        {
            if (mainDatabase == null) return new List<CharacterData>();
            return mainDatabase.GetTacticalCharactersByRarity(rarity);
        }
        
        /// <summary>
        /// 모든 캐릭터 목록 가져오기 (통합 데이터베이스 사용)
        /// </summary>
        public List<CharacterData> GetAllCharacters()
        {
            if (mainDatabase == null) return new List<CharacterData>();
            return mainDatabase.GetAllTacticalCharacters();
        }
        
        #endregion
        
        #region 스킬 관련 메서드
        
        /// <summary>
        /// ID로 스킬 데이터 가져오기
        /// </summary>
        public TacticalSkillDataSO GetSkill(string skillId)
        {
            if (skillDatabase.TryGetValue(skillId, out TacticalSkillDataSO skill))
            {
                return skill;
            }
            return null;
        }
        
        /// <summary>
        /// 클래스별 사용 가능한 스킬 목록 가져오기
        /// </summary>
        public List<TacticalSkillDataSO> GetSkillsByClass(CharacterClass characterClass)
        {
            return skillDatabase.Values
                .Where(skill => skill.requiredClass == characterClass)
                .ToList();
        }
        
        /// <summary>
        /// 모든 스킬 목록 가져오기
        /// </summary>
        public List<TacticalSkillDataSO> GetAllSkills()
        {
            return skillDatabase.Values.ToList();
        }
        
        #endregion
        
        #region 대화 관련 메서드 - StoryDialogueDataSO 타입이 삭제되어 주석 처리
        
        /*
        /// <summary>
        /// ID로 대화 데이터 가져오기
        /// </summary>
        public StoryDialogueDataSO GetDialogue(string dialogueId)
        {
            if (dialogueDatabase.TryGetValue(dialogueId, out StoryDialogueDataSO dialogue))
            {
                return dialogue;
            }
            return null;
        }
        
        /// <summary>
        /// 챕터별 대화 목록 가져오기
        /// </summary>
        public List<StoryDialogueDataSO> GetDialoguesByChapter(string chapterId)
        {
            return dialogueDatabase.Values
                .Where(dialogue => dialogue.chapterId == chapterId)
                .OrderBy(dialogue => dialogue.dialogueId)
                .ToList();
        }
        
        /// <summary>
        /// 씬별 대화 목록 가져오기
        /// </summary>
        public List<StoryDialogueDataSO> GetDialoguesByScene(string sceneId)
        {
            return dialogueDatabase.Values
                .Where(dialogue => dialogue.sceneId == sceneId)
                .OrderBy(dialogue => dialogue.dialogueId)
                .ToList();
        }
        */
        
        #endregion
        
        #region 유틸리티 메서드
        
        /// <summary>
        /// 데이터 리로드
        /// </summary>
        public void ReloadAllData()
        {
            LoadAllData();
            Debug.Log("All data reloaded successfully");
        }
        
        /// <summary>
        /// 특정 타입의 데이터만 리로드
        /// </summary>
        public void ReloadData(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.Character:
                    LoadCharacterData();
                    LinkSkillsToCharacters();
                    break;
                case DataType.Skill:
                    LoadSkillData();
                    LinkSkillsToCharacters();
                    break;
                case DataType.Dialogue:
                    // LoadDialogueData(); // 이미 주석 처리됨
                    break;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// 데이터 타입
    /// </summary>
    public enum DataType
    {
        Character,
        Skill,
        Dialogue
    }
}