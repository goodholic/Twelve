using UnityEngine;
using System.Collections.Generic;
using System;

namespace pjy.Story
{
    /// <summary>
    /// 스토리 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "NewStory", menuName = "pjy/Story/StoryData")]
    public class StoryData : ScriptableObject
    {
        [Header("스토리 기본 정보")]
        public string storyId;
        public string storyTitle;
        public string storyDescription;
        public StoryType storyType = StoryType.Main;
        public int storyOrder = 0;
        
        [Header("조건")]
        public List<StoryCondition> conditions = new List<StoryCondition>();
        
        [Header("트리거 조건")]
        public List<StoryTriggerCondition> triggerConditions = new List<StoryTriggerCondition>();
        
        [Header("대화")]
        public List<DialogueLine> dialogueLines = new List<DialogueLine>();
        
        [Header("이벤트 설정")]
        public string eventType;
        public int eventValue;
        public bool isEventStory = false;
    }

    /// <summary>
    /// 대화 라인
    /// </summary>
    [System.Serializable]
    public class DialogueLine
    {
        [Header("기본 정보")]
        public string storyId;
        public string nextStoryId;
        
        [Header("캐릭터 정보")]
        public string characterName;
        public CharacterEmotion emotion = CharacterEmotion.Normal;
        public CharacterPosition position = CharacterPosition.Center;
        
        [Header("대화 내용")]
        [TextArea(3, 5)]
        public string dialogueText;
        
        [Header("선택지")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();
        
        [Header("효과")]
        public bool hasChoices => choices != null && choices.Count > 0;
        public float displayDelay = 0f;
        public string backgroundMusic;
        public string soundEffect;
    }

    /// <summary>
    /// 대화 선택지
    /// </summary>
    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public string nextStoryId;
        public string setVariable;
        public int setValue;
        public List<StoryCondition> conditions = new List<StoryCondition>();
        public List<StoryEffect> effects = new List<StoryEffect>();
    }

    /// <summary>
    /// 스토리 조건
    /// </summary>
    [System.Serializable]
    public class StoryCondition
    {
        public ConditionType conditionType;
        public string targetKey;
        public int targetValue;
        public ComparisonOperator comparisonOperator = ComparisonOperator.Equal;
        public string stringValue;
        public bool boolValue;
    }

    /// <summary>
    /// 스토리 효과
    /// </summary>
    [System.Serializable]
    public class StoryEffect
    {
        public EffectType effectType;
        public string targetKey;
        public int intValue;
        public string stringValue;
        public bool boolValue;
    }

    /// <summary>
    /// 조건 타입
    /// </summary>
    public enum ConditionType
    {
        Stage,      // 스테이지 조건
        Character,  // 캐릭터 조건
        Item,       // 아이템 조건
        Variable,   // 변수 조건
        Date,       // 날짜 조건
        Gold,       // 골드 조건
        Diamond,    // 다이아몬드 조건
        Level       // 레벨 조건
    }

    /// <summary>
    /// 비교 연산자
    /// </summary>
    public enum ComparisonOperator
    {
        Equal,              // ==
        NotEqual,           // !=
        Greater,            // >
        GreaterOrEqual,     // >=
        Less,               // <
        LessOrEqual         // <=
    }

    /// <summary>
    /// 효과 타입
    /// </summary>
    public enum EffectType
    {
        SetVariable,    // 변수 설정
        AddGold,        // 골드 추가
        AddDiamond,     // 다이아몬드 추가
        AddItem,        // 아이템 추가
        UnlockStage,    // 스테이지 해금
        PlaySound,      // 사운드 재생
        ShowMessage     // 메시지 표시
    }

    /// <summary>
    /// 캐릭터 위치
    /// </summary>
    public enum CharacterPosition
    {
        Left,
        Center,
        Right,
        Hidden
    }

    /// <summary>
    /// 캐릭터 감정
    /// </summary>
    public enum CharacterEmotion
    {
        Normal,
        Happy,
        Sad,
        Angry,
        Surprised,
        Worried,
        Excited
    }

    /// <summary>
    /// 스토리 타입
    /// </summary>
    public enum StoryType
    {
        Main,       // 메인 스토리
        Event,      // 이벤트 스토리
        Character,  // 캐릭터 스토리
        Tutorial,   // 튜토리얼
        Side        // 사이드 스토리
    }

    /// <summary>
    /// 스토리 트리거 조건
    /// </summary>
    [System.Serializable]
    public class StoryTriggerCondition
    {
        public string eventType;        // 이벤트 타입 (stage, character, item 등)
        public string eventTarget;      // 이벤트 대상 (캐릭터 ID, 아이템 ID 등)
        public int eventValue;          // 이벤트 값 (스테이지 번호, 개수 등)
        public ComparisonOperator comparisonOperator = ComparisonOperator.Equal;
    }
} 