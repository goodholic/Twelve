using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace TileConquest.Data
{
    /// <summary>
    /// 스토리/대화 데이터를 관리하는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "StoryDialogueData", menuName = "TileConquest/Story Dialogue", order = 5)]
    public class StoryDialogueDataSO : ScriptableObject
    {
        [Header("스토리 정보")]
        public string storyId;
        public string storyTitle;
        public StoryType storyType;
        
        [Header("대화 노드")]
        public List<DialogueNode> dialogueNodes = new List<DialogueNode>();
        
        [Header("캐릭터 정보")]
        public List<DialogueCharacter> characters = new List<DialogueCharacter>();
        
        [Header("분기 설정")]
        public bool hasBranching = false;
        public List<StoryBranch> branches = new List<StoryBranch>();
        
        [Header("배경 및 효과")]
        public Sprite defaultBackground;
        public AudioClip defaultBGM;
        
        /// <summary>
        /// 시작 노드 가져오기
        /// </summary>
        public DialogueNode GetStartNode()
        {
            return dialogueNodes.Find(node => node.isStartNode);
        }
        
        /// <summary>
        /// ID로 노드 찾기
        /// </summary>
        public DialogueNode GetNodeById(string nodeId)
        {
            return dialogueNodes.Find(node => node.nodeId == nodeId);
        }
        
        /// <summary>
        /// 다음 노드 가져오기
        /// </summary>
        public DialogueNode GetNextNode(string currentNodeId, int choiceIndex = 0)
        {
            var currentNode = GetNodeById(currentNodeId);
            if (currentNode == null) return null;
            
            if (currentNode.choices.Count > 0 && choiceIndex < currentNode.choices.Count)
            {
                string nextId = currentNode.choices[choiceIndex].nextNodeId;
                return GetNodeById(nextId);
            }
            
            return GetNodeById(currentNode.nextNodeId);
        }
    }
    
    /// <summary>
    /// 대화 노드
    /// </summary>
    [System.Serializable]
    public class DialogueNode
    {
        public string nodeId;
        public bool isStartNode = false;
        
        [Header("대화 내용")]
        public string speakerName;
        public int speakerIndex = 0;  // characters 리스트의 인덱스
        
        [TextArea(3, 5)]
        public string dialogueText;
        
        [Header("다음 노드")]
        public string nextNodeId;
        
        [Header("선택지")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();
        
        [Header("표시 설정")]
        public CharacterPosition position = CharacterPosition.Left;
        public CharacterExpression expression = CharacterExpression.Normal;
        
        [Header("효과")]
        public DialogueEffect effect = DialogueEffect.None;
        public float effectDuration = 0f;
        
        [Header("배경/사운드 변경")]
        public bool changeBackground = false;
        public Sprite newBackground;
        
        public bool changeBGM = false;
        public AudioClip newBGM;
        
        public bool playSFX = false;
        public AudioClip sfx;
        
        [Header("전투 트리거")]
        public bool triggerBattle = false;
        public string battleConfigId;
    }
    
    /// <summary>
    /// 대화 선택지
    /// </summary>
    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public string nextNodeId;
        
        [Header("조건")]
        public bool hasCondition = false;
        public ChoiceCondition condition;
        
        [Header("결과")]
        public bool hasConsequence = false;
        public ChoiceConsequence consequence;
    }
    
    /// <summary>
    /// 선택지 조건
    /// </summary>
    [System.Serializable]
    public class ChoiceCondition
    {
        public ConditionType type;
        public string conditionValue;
        public int requiredAmount;
    }
    
    /// <summary>
    /// 선택지 결과
    /// </summary>
    [System.Serializable]
    public class ChoiceConsequence
    {
        public ConsequenceType type;
        public string consequenceValue;
        public int amount;
    }
    
    /// <summary>
    /// 대화 캐릭터
    /// </summary>
    [System.Serializable]
    public class DialogueCharacter
    {
        public string characterName;
        public Sprite characterSprite;
        public Color characterColor = Color.white;
    }
    
    /// <summary>
    /// 스토리 분기
    /// </summary>
    [System.Serializable]
    public class StoryBranch
    {
        public string branchName;
        public string startNodeId;
        public string endNodeId;
        public List<string> requiredFlags = new List<string>();
    }
    
    /// <summary>
    /// 스토리 타입
    /// </summary>
    public enum StoryType
    {
        Main,           // 메인 스토리
        Side,           // 사이드 스토리
        Character,      // 캐릭터 스토리
        Tutorial,       // 튜토리얼
        Event           // 이벤트
    }
    
    /// <summary>
    /// 캐릭터 위치
    /// </summary>
    public enum CharacterPosition
    {
        Left,
        Right,
        Center
    }
    
    /// <summary>
    /// 캐릭터 표정
    /// </summary>
    public enum CharacterExpression
    {
        Normal,
        Happy,
        Sad,
        Angry,
        Surprised,
        Thinking
    }
    
    /// <summary>
    /// 대화 효과
    /// </summary>
    public enum DialogueEffect
    {
        None,
        FadeIn,
        FadeOut,
        Shake,
        Flash,
        TypeWriter
    }
    
    /// <summary>
    /// 조건 타입
    /// </summary>
    public enum ConditionType
    {
        Flag,           // 플래그 확인
        Character,      // 캐릭터 보유
        Victory,        // 승리 횟수
        Level           // 레벨
    }
    
    /// <summary>
    /// 결과 타입
    /// </summary>
    public enum ConsequenceType
    {
        SetFlag,        // 플래그 설정
        GiveCharacter,  // 캐릭터 지급
        GiveGold,       // 골드 지급
        GiveExp         // 경험치 지급
    }
}