using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
        
        [Header("데이터 경로")]
        [SerializeField] private string characterDataPath = "ScriptableObjects/Characters";
        [SerializeField] private string skillDataPath = "ScriptableObjects/Skills";
        [SerializeField] private string dialogueDataPath = "ScriptableObjects/Dialogues";
        
        [Header("로드된 데이터")]
        private Dictionary<string, TacticalCharacterDataSO> characterDatabase = new Dictionary<string, TacticalCharacterDataSO>();
        private Dictionary<string, TacticalSkillDataSO> skillDatabase = new Dictionary<string, TacticalSkillDataSO>();
        private Dictionary<string, StoryDialogueDataSO> dialogueDatabase = new Dictionary<string, StoryDialogueDataSO>();
        
        [Header("캐시된 데이터")]
        private Dictionary<CharacterClass, List<TacticalCharacterDataSO>> charactersByClass = new Dictionary<CharacterClass, List<TacticalCharacterDataSO>>();
        private Dictionary<CharacterRarity, List<TacticalCharacterDataSO>> charactersByRarity = new Dictionary<CharacterRarity, List<TacticalCharacterDataSO>>();
        
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
            LoadDialogueData();
            
            // 스킬 참조 연결
            LinkSkillsToCharacters();
            
            Debug.Log($"Data Loading Complete - Characters: {characterDatabase.Count}, Skills: {skillDatabase.Count}, Dialogues: {dialogueDatabase.Count}");
        }
        
        /// <summary>
        /// 캐릭터 데이터 로드
        /// </summary>
        private void LoadCharacterData()
        {
            TacticalCharacterDataSO[] characters = Resources.LoadAll<TacticalCharacterDataSO>(characterDataPath);
            
            characterDatabase.Clear();
            charactersByClass.Clear();
            charactersByRarity.Clear();
            
            foreach (var character in characters)
            {
                characterDatabase[character.characterId] = character;
                
                // 클래스별 분류
                if (!charactersByClass.ContainsKey(character.characterClass))
                {
                    charactersByClass[character.characterClass] = new List<TacticalCharacterDataSO>();
                }
                charactersByClass[character.characterClass].Add(character);
                
                // 레어도별 분류
                if (!charactersByRarity.ContainsKey(character.rarity))
                {
                    charactersByRarity[character.rarity] = new List<TacticalCharacterDataSO>();
                }
                charactersByRarity[character.rarity].Add(character);
            }
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
        /// 대화 데이터 로드
        /// </summary>
        private void LoadDialogueData()
        {
            StoryDialogueDataSO[] dialogues = Resources.LoadAll<StoryDialogueDataSO>(dialogueDataPath);
            
            dialogueDatabase.Clear();
            
            foreach (var dialogue in dialogues)
            {
                dialogueDatabase[dialogue.dialogueId] = dialogue;
            }
        }
        
        /// <summary>
        /// 캐릭터에 스킬 참조 연결
        /// </summary>
        private void LinkSkillsToCharacters()
        {
            foreach (var character in characterDatabase.Values)
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
        /// ID로 캐릭터 데이터 가져오기
        /// </summary>
        public TacticalCharacterDataSO GetCharacter(string characterId)
        {
            if (characterDatabase.TryGetValue(characterId, out TacticalCharacterDataSO character))
            {
                return character;
            }
            return null;
        }
        
        /// <summary>
        /// 클래스별 캐릭터 목록 가져오기
        /// </summary>
        public List<TacticalCharacterDataSO> GetCharactersByClass(CharacterClass characterClass)
        {
            if (charactersByClass.TryGetValue(characterClass, out List<TacticalCharacterDataSO> characters))
            {
                return new List<TacticalCharacterDataSO>(characters);
            }
            return new List<TacticalCharacterDataSO>();
        }
        
        /// <summary>
        /// 레어도별 캐릭터 목록 가져오기
        /// </summary>
        public List<TacticalCharacterDataSO> GetCharactersByRarity(CharacterRarity rarity)
        {
            if (charactersByRarity.TryGetValue(rarity, out List<TacticalCharacterDataSO> characters))
            {
                return new List<TacticalCharacterDataSO>(characters);
            }
            return new List<TacticalCharacterDataSO>();
        }
        
        /// <summary>
        /// 모든 캐릭터 목록 가져오기
        /// </summary>
        public List<TacticalCharacterDataSO> GetAllCharacters()
        {
            return characterDatabase.Values.ToList();
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
        
        #region 대화 관련 메서드
        
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
                    LoadDialogueData();
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