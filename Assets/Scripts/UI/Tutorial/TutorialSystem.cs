using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuildMaster.Systems
{
    public class TutorialSystem : MonoBehaviour
    {
        private static TutorialSystem _instance;
        public static TutorialSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<TutorialSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("TutorialSystem");
                        _instance = go.AddComponent<TutorialSystem>();
                    }
                }
                return _instance;
            }
        }
        
        [System.Serializable]
        public class TutorialStep
        {
            public string stepId;
            public string title;
            public string description;
            public string targetUIElement;
            public Vector2 highlightOffset;
            public bool requiresAction;
            public string actionType;
            public float delayBeforeNext;
            public bool skippable = true;
        }
        
        [System.Serializable]
        public class TutorialSequence
        {
            public string sequenceId;
            public string sequenceName;
            public List<TutorialStep> steps;
            public bool isCompleted;
            public string unlockCondition;
            public int rewardGold;
            public int rewardGems;
        }
        
        private Dictionary<string, TutorialSequence> tutorialSequences;
        private TutorialSequence currentSequence;
        private int currentStepIndex;
        private bool isTutorialActive;
        
        [Header("UI Elements")]
        [SerializeField] private GameObject tutorialOverlay;
        [SerializeField] private GameObject highlightMask;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private GameObject fingerPointer;
        [SerializeField] private GameObject darkBackground;
        
        // Events
        public event Action<string> OnTutorialStarted;
        public event Action<string> OnTutorialCompleted;
        public event Action<TutorialStep> OnStepChanged;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            Initialize();
        }
        
        void Initialize()
        {
            tutorialSequences = new Dictionary<string, TutorialSequence>();
            SetupTutorials();
            LoadTutorialProgress();
        }
        
        void SetupTutorials()
        {
            // 첫 시작 튜토리얼
            var firstStartTutorial = new TutorialSequence
            {
                sequenceId = "first_start",
                sequenceName = "길드 설립",
                steps = new List<TutorialStep>
                {
                    new TutorialStep
                    {
                        stepId = "welcome",
                        title = "길드 마스터가 되신 것을 환영합니다!",
                        description = "이제부터 당신만의 길드를 운영하게 됩니다. 먼저 길드 홀을 건설해봅시다.",
                        delayBeforeNext = 2f,
                        skippable = false
                    },
                    new TutorialStep
                    {
                        stepId = "build_guild_hall",
                        title = "길드 홀 건설",
                        description = "왼쪽 화면에서 빈 공간을 탭하여 길드 홀을 건설하세요.",
                        targetUIElement = "GuildViewport",
                        requiresAction = true,
                        actionType = "build_guildhall"
                    },
                    new TutorialStep
                    {
                        stepId = "recruit_first",
                        title = "첫 모험가 영입",
                        description = "무료 모집을 통해 첫 모험가를 영입해봅시다!",
                        targetUIElement = "RecruitButton",
                        requiresAction = true,
                        actionType = "recruit_adventurer"
                    },
                    new TutorialStep
                    {
                        stepId = "form_squad",
                        title = "부대 편성",
                        description = "오른쪽 부대 편성에서 모험가를 배치해보세요.",
                        targetUIElement = "FormationContainer",
                        requiresAction = true,
                        actionType = "place_unit"
                    },
                    new TutorialStep
                    {
                        stepId = "first_battle",
                        title = "첫 전투",
                        description = "이제 첫 모험을 떠나볼 시간입니다!",
                        targetUIElement = "AdventureButton",
                        requiresAction = true,
                        actionType = "start_adventure"
                    }
                },
                rewardGold = 1000,
                rewardGems = 100
            };
            
            tutorialSequences["first_start"] = firstStartTutorial;
            
            // 건설 튜토리얼
            var buildingTutorial = new TutorialSequence
            {
                sequenceId = "building_basics",
                sequenceName = "건설의 기초",
                steps = new List<TutorialStep>
                {
                    new TutorialStep
                    {
                        stepId = "building_intro",
                        title = "시설 건설",
                        description = "길드 시설을 건설하여 길드를 성장시킬 수 있습니다.",
                        delayBeforeNext = 1.5f
                    },
                    new TutorialStep
                    {
                        stepId = "build_training",
                        title = "훈련소 건설",
                        description = "훈련소를 건설하면 모험가들의 경험치 획득량이 증가합니다.",
                        targetUIElement = "BuildMenuButton",
                        requiresAction = true,
                        actionType = "open_build_menu"
                    }
                },
                unlockCondition = "guild_level_2",
                rewardGold = 500
            };
            
            tutorialSequences["building_basics"] = buildingTutorial;
            
            // 전투 심화 튜토리얼
            var advancedBattleTutorial = new TutorialSequence
            {
                sequenceId = "battle_advanced",
                sequenceName = "전투의 고급 전략",
                steps = new List<TutorialStep>
                {
                    new TutorialStep
                    {
                        stepId = "formation_intro",
                        title = "진형의 중요성",
                        description = "부대의 배치에 따라 전투 효율이 크게 달라집니다. 탱커는 앞줄에, 딜러와 힐러는 뒷줄에 배치하세요.",
                        delayBeforeNext = 3f
                    },
                    new TutorialStep
                    {
                        stepId = "speed_control",
                        title = "전투 속도 조절",
                        description = "상단의 속도 버튼을 눌러 전투 속도를 2배, 4배로 가속할 수 있습니다.",
                        targetUIElement = "SpeedButton",
                        highlightOffset = new Vector2(0, -20)
                    }
                },
                unlockCondition = "complete_10_battles",
                rewardGold = 800
            };
            
            tutorialSequences["battle_advanced"] = advancedBattleTutorial;
        }
        
        public void StartTutorial(string sequenceId)
        {
            if (!tutorialSequences.ContainsKey(sequenceId))
            {
                Debug.LogError($"Tutorial sequence {sequenceId} not found!");
                return;
            }
            
            var sequence = tutorialSequences[sequenceId];
            if (sequence.isCompleted)
            {
                Debug.Log($"Tutorial {sequenceId} already completed.");
                return;
            }
            
            currentSequence = sequence;
            currentStepIndex = 0;
            isTutorialActive = true;
            
            if (tutorialOverlay != null)
                tutorialOverlay.SetActive(true);
                
            OnTutorialStarted?.Invoke(sequenceId);
            
            ShowCurrentStep();
        }
        
        void ShowCurrentStep()
        {
            if (currentSequence == null || currentStepIndex >= currentSequence.steps.Count)
            {
                CompleteTutorial();
                return;
            }
            
            var step = currentSequence.steps[currentStepIndex];
            OnStepChanged?.Invoke(step);
            
            // UI 업데이트
            if (titleText != null)
                titleText.text = step.title;
                
            if (descriptionText != null)
                descriptionText.text = step.description;
            
            // 하이라이트 설정
            if (!string.IsNullOrEmpty(step.targetUIElement))
            {
                HighlightUIElement(step.targetUIElement, step.highlightOffset);
            }
            else
            {
                HideHighlight();
            }
            
            // 버튼 상태 설정
            if (skipButton != null)
                skipButton.gameObject.SetActive(step.skippable);
                
            if (nextButton != null)
                nextButton.gameObject.SetActive(!step.requiresAction);
            
            // 자동 진행
            if (!step.requiresAction && step.delayBeforeNext > 0)
            {
                StartCoroutine(AutoProgressStep(step.delayBeforeNext));
            }
        }
        
        void HighlightUIElement(string elementName, Vector2 offset)
        {
            GameObject targetElement = GameObject.Find(elementName);
            if (targetElement == null)
            {
                Debug.LogWarning($"Tutorial target element {elementName} not found!");
                return;
            }
            
            if (highlightMask != null)
            {
                highlightMask.SetActive(true);
                
                // 하이라이트 위치 설정
                RectTransform targetRect = targetElement.GetComponent<RectTransform>();
                RectTransform highlightRect = highlightMask.GetComponent<RectTransform>();
                
                if (targetRect != null && highlightRect != null)
                {
                    highlightRect.position = targetRect.position + (Vector3)offset;
                    highlightRect.sizeDelta = targetRect.sizeDelta * 1.2f;
                }
            }
            
            // 손가락 포인터 표시
            if (fingerPointer != null)
            {
                fingerPointer.SetActive(true);
                fingerPointer.transform.position = targetElement.transform.position + (Vector3)offset;
                
                // 손가락 애니메이션
                StartCoroutine(AnimateFinger());
            }
        }
        
        void HideHighlight()
        {
            if (highlightMask != null)
                highlightMask.SetActive(false);
                
            if (fingerPointer != null)
                fingerPointer.SetActive(false);
        }
        
        IEnumerator AutoProgressStep(float delay)
        {
            yield return new WaitForSeconds(delay);
            NextStep();
        }
        
        IEnumerator AnimateFinger()
        {
            if (fingerPointer == null) yield break;
            
            Vector3 originalPos = fingerPointer.transform.position;
            float animTime = 0;
            
            while (fingerPointer.activeSelf)
            {
                animTime += Time.deltaTime;
                float offset = Mathf.Sin(animTime * 3f) * 10f;
                fingerPointer.transform.position = originalPos + new Vector3(0, offset, 0);
                yield return null;
            }
        }
        
        public void NextStep()
        {
            currentStepIndex++;
            ShowCurrentStep();
        }
        
        public void SkipTutorial()
        {
            if (currentSequence != null && currentSequence.steps[currentStepIndex].skippable)
            {
                CompleteTutorial();
            }
        }
        
        void CompleteTutorial()
        {
            if (currentSequence == null) return;
            
            currentSequence.isCompleted = true;
            SaveTutorialProgress();
            
            // 보상 지급
            if (currentSequence.rewardGold > 0 || currentSequence.rewardGems > 0)
            {
                GiveTutorialRewards();
            }
            
            OnTutorialCompleted?.Invoke(currentSequence.sequenceId);
            
            // UI 정리
            if (tutorialOverlay != null)
                tutorialOverlay.SetActive(false);
                
            HideHighlight();
            
            isTutorialActive = false;
            currentSequence = null;
        }
        
        void GiveTutorialRewards()
        {
            if (currentSequence == null) return;
            
            // ResourceManager 타입이 제거되어 주석 처리
            // var gameManager = Core.GameManager.Instance;
            // if (gameManager?.ResourceManager != null)
            // {
            //     if (currentSequence.rewardGold > 0)
            //     {
            //         gameManager.ResourceManager.AddGold(currentSequence.rewardGold);
            //     }
            // }
            
            // TODO: 젬 보상 처리
            
            // 보상 팝업 표시
            ShowRewardPopup();
        }
        
        void ShowRewardPopup()
        {
            // TODO: 보상 획득 팝업 표시
            Debug.Log($"Tutorial completed! Rewards: {currentSequence.rewardGold} Gold");
        }
        
        public void OnActionCompleted(string actionType)
        {
            if (!isTutorialActive || currentSequence == null) return;
            
            var currentStep = currentSequence.steps[currentStepIndex];
            if (currentStep.requiresAction && currentStep.actionType == actionType)
            {
                NextStep();
            }
        }
        
        void SaveTutorialProgress()
        {
            // 튜토리얼 진행 상황 저장
            foreach (var tutorial in tutorialSequences)
            {
                PlayerPrefs.SetInt($"Tutorial_{tutorial.Key}", tutorial.Value.isCompleted ? 1 : 0);
            }
            PlayerPrefs.Save();
        }
        
        void LoadTutorialProgress()
        {
            // 튜토리얼 진행 상황 로드
            foreach (var tutorial in tutorialSequences)
            {
                bool isCompleted = PlayerPrefs.GetInt($"Tutorial_{tutorial.Key}", 0) == 1;
                tutorial.Value.isCompleted = isCompleted;
            }
        }
        
        public bool IsTutorialActive()
        {
            return isTutorialActive;
        }
        
        public bool IsTutorialCompleted(string sequenceId)
        {
            return tutorialSequences.ContainsKey(sequenceId) && 
                   tutorialSequences[sequenceId].isCompleted;
        }
        
        public void CheckAndStartTutorial(string condition)
        {
            // 조건에 맞는 튜토리얼 자동 시작
            foreach (var tutorial in tutorialSequences.Values)
            {
                if (!tutorial.isCompleted && tutorial.unlockCondition == condition)
                {
                    StartTutorial(tutorial.sequenceId);
                    break;
                }
            }
        }
    }
}