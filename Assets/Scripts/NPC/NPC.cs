using UnityEngine;
using System;
using System.Collections.Generic;
using GuildMaster.Core;

namespace GuildMaster.NPC
{
    /// <summary>
    /// NPC 기본 클래스
    /// </summary>
    public class NPC : MonoBehaviour
    {
        [Header("NPC Info")]
        public string npcId;
        public string npcName;
        public string description;
        public NPCType npcType;
        public Sprite portrait;

        [Header("Interaction")]
        public bool canInteract = true;
        public float interactionDistance = 2f;
        public List<string> dialogueLines = new List<string>();
        public bool hasQuest = false;
        public bool isVendor = false;

        [Header("Movement")]
        public bool canMove = false;
        public float moveSpeed = 2f;
        public Transform[] patrolPoints;
        public PatrolType patrolType = PatrolType.None;

        [Header("Schedule")]
        public bool hasSchedule = false;
        public List<NPCSchedule> dailySchedule = new List<NPCSchedule>();

        // State
        private int currentPatrolIndex = 0;
        private bool isMoving = false;
        private Transform player;
        private NPCSchedule currentSchedule;

        public enum NPCType
        {
            Civilian,
            Merchant,
            Guard,
            QuestGiver,
            Trainer,
            Informant,
            Guild_Member,
            Noble
        }

        public enum PatrolType
        {
            None,
            Linear,
            Loop,
            Random
        }

        [System.Serializable]
        public class NPCSchedule
        {
            public int hour;
            public int minute;
            public string activity;
            public Vector3 location;
            public string description;
        }

        // Events
        public event Action<NPC> OnInteraction;
        public event Action<NPC, string> OnDialogueStart;
        public event Action<NPC> OnQuestAvailable;

        void Start()
        {
            Initialize();
        }

        void Initialize()
        {
            if (string.IsNullOrEmpty(npcId))
            {
                npcId = Guid.NewGuid().ToString();
            }

            // 플레이어 찾기
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }

            // 스케줄 시작
            if (hasSchedule && dailySchedule.Count > 0)
            {
                StartSchedule();
            }

            // 패트롤 시작
            if (canMove && patrolPoints != null && patrolPoints.Length > 0)
            {
                StartPatrol();
            }
        }

        void Update()
        {
            // 플레이어와의 거리 체크
            if (player != null && canInteract)
            {
                float distance = Vector3.Distance(transform.position, player.position);
                if (distance <= interactionDistance)
                {
                    ShowInteractionPrompt(true);
                    
                    // 상호작용 입력 체크
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        Interact();
                    }
                }
                else
                {
                    ShowInteractionPrompt(false);
                }
            }

            // 패트롤 업데이트
            if (canMove && isMoving)
            {
                UpdatePatrol();
            }

            // 스케줄 업데이트
            if (hasSchedule)
            {
                UpdateSchedule();
            }
        }

        void ShowInteractionPrompt(bool show)
        {
            // UI에 상호작용 프롬프트 표시/숨김
            // 실제로는 UI 매니저를 통해 처리
        }

        public void Interact()
        {
            if (!canInteract) return;

            OnInteraction?.Invoke(this);

            // NPC 타입에 따른 상호작용
            switch (npcType)
            {
                case NPCType.Merchant:
                    OpenMerchantShop();
                    break;
                case NPCType.QuestGiver:
                    if (hasQuest)
                    {
                        OnQuestAvailable?.Invoke(this);
                    }
                    else
                    {
                        StartDialogue();
                    }
                    break;
                case NPCType.Trainer:
                    OpenTrainingMenu();
                    break;
                default:
                    StartDialogue();
                    break;
            }
        }

        void StartDialogue()
        {
            if (dialogueLines.Count > 0)
            {
                string randomLine = dialogueLines[UnityEngine.Random.Range(0, dialogueLines.Count)];
                OnDialogueStart?.Invoke(this, randomLine);
            }
        }

        void OpenMerchantShop()
        {
            if (isVendor)
            {
                var merchantManager = FindObjectOfType<MerchantManager>();
                if (merchantManager != null)
                {
                    merchantManager.OpenShop(npcId);
                }
            }
        }

        void OpenTrainingMenu()
        {
            // 훈련 메뉴 열기
            Debug.Log($"{npcName} offers training services!");
        }

        void StartPatrol()
        {
            if (patrolPoints.Length > 0)
            {
                isMoving = true;
            }
        }

        void UpdatePatrol()
        {
            if (patrolPoints.Length == 0) return;

            Transform targetPoint = patrolPoints[currentPatrolIndex];
            Vector3 direction = (targetPoint.position - transform.position).normalized;
            
            transform.position += direction * moveSpeed * Time.deltaTime;

            // 목표 지점에 도달했는지 확인
            if (Vector3.Distance(transform.position, targetPoint.position) < 0.1f)
            {
                switch (patrolType)
                {
                    case PatrolType.Linear:
                        currentPatrolIndex++;
                        if (currentPatrolIndex >= patrolPoints.Length)
                        {
                            isMoving = false;
                        }
                        break;
                    case PatrolType.Loop:
                        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                        break;
                    case PatrolType.Random:
                        currentPatrolIndex = UnityEngine.Random.Range(0, patrolPoints.Length);
                        break;
                }
            }
        }

        void StartSchedule()
        {
            InvokeRepeating(nameof(CheckSchedule), 0f, 60f); // 1분마다 스케줄 체크
        }

        void CheckSchedule()
        {
            DateTime now = DateTime.Now;
            
            foreach (var schedule in dailySchedule)
            {
                if (now.Hour == schedule.hour && now.Minute == schedule.minute)
                {
                    ExecuteSchedule(schedule);
                    break;
                }
            }
        }

        void ExecuteSchedule(NPCSchedule schedule)
        {
            currentSchedule = schedule;
            
            // 스케줄된 위치로 이동
            if (schedule.location != Vector3.zero)
            {
                StartCoroutine(MoveToLocation(schedule.location));
            }

            // 스케줄 활동 실행
            Debug.Log($"{npcName} is now {schedule.activity}: {schedule.description}");
        }

        System.Collections.IEnumerator MoveToLocation(Vector3 targetLocation)
        {
            isMoving = true;
            
            while (Vector3.Distance(transform.position, targetLocation) > 0.1f)
            {
                Vector3 direction = (targetLocation - transform.position).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;
                yield return null;
            }
            
            transform.position = targetLocation;
            isMoving = false;
        }

        void UpdateSchedule()
        {
            // 현재 스케줄 상태 업데이트
            if (currentSchedule != null)
            {
                // 스케줄 기반 행동 업데이트
            }
        }

        public void AddDialogueLine(string line)
        {
            dialogueLines.Add(line);
        }

        public void SetQuest(bool hasQuest)
        {
            this.hasQuest = hasQuest;
        }

        public void SetVendor(bool isVendor)
        {
            this.isVendor = isVendor;
        }

        public NPCSchedule GetCurrentSchedule()
        {
            return currentSchedule;
        }

        public bool IsPlayerInRange()
        {
            if (player == null) return false;
            return Vector3.Distance(transform.position, player.position) <= interactionDistance;
        }
    }
} 