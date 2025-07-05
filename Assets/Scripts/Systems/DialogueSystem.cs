using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildMaster.Data;

namespace GuildMaster.Systems
{
    public class DialogueSystem : MonoBehaviour
    {
        private static DialogueSystem _instance;
        public static DialogueSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DialogueSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("DialogueSystem");
                        _instance = go.AddComponent<DialogueSystem>();
                    }
                }
                return _instance;
            }
        }
        
        // 대화 상태
        public enum DialogueState
        {
            Idle,
            Playing,
            WaitingForInput,
            Transitioning
        }
        
        // 현재 대화 정보
        private DialogueData currentDialogueData;
        private DialogueData currentEntry;
        private DialogueState state = DialogueState.Idle;
        private Coroutine dialogueCoroutine;
        
        // 대화 데이터베이스
        private Dictionary<string, DialogueData> dialogueDatabase;
        
        // 이벤트
        public event Action<DialogueData> OnDialogueStart;
        public event Action<DialogueData> OnDialogueLineStart;
        public event Action<DialogueData> OnDialogueLineEnd;
        public event Action OnDialogueEnd;
        public event Action<string> OnBackgroundChange;
        public event Action<string> OnBGMChange;
        public event Action<string> OnSFXPlay;
        public event Action<string, string> OnCharacterExpression; // character, expression
        public event Action<string> OnEffectPlay;
        
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
            dialogueDatabase = new Dictionary<string, DialogueData>();
            LoadDialogueFiles();
        }
        
        void LoadDialogueFiles()
        {
            // DataManager에서 대화 데이터 로드
            var dataManager = GuildMaster.Core.DataManager.Instance;
            if (dataManager != null)
            {
                // 모든 대화 데이터를 DataManager에서 가져오기
                Debug.Log("Loading dialogue data from DataManager");
            }

            // CSV 파일에서 직접 로드하는 방법
            LoadDialogueFromCSV();
        }

        void LoadDialogueFromCSV()
        {
            string[] csvFiles = {
                "dialogue_example",
                "dialogue_mainstory_chapter1",
                "dialogue_battle_victory",
                "arcadia_chronicle_dialogue"
            };

            foreach (string fileName in csvFiles)
            {
                TextAsset csvFile = Resources.Load<TextAsset>("CSV/" + fileName);
                if (csvFile != null)
                {
                    ParseDialogueCSV(csvFile.text, fileName);
                }
            }
        }

        void ParseDialogueCSV(string csvText, string fileName)
        {
            // CSV 파싱 로직
            string[] lines = csvText.Split('\n');
            
            for (int i = 1; i < lines.Length; i++) // 첫 번째 줄은 헤더
            {
                if (string.IsNullOrEmpty(lines[i])) continue;
                
                string[] values = lines[i].Split(',');
                if (values.Length < 4) continue;

                DialogueData dialogue = new DialogueData
                {
                    dialogueId = values[0].Trim(),
                    speaker = values[1].Trim(),
                    content = values[2].Trim(),
                    nextDialogueId = values[3].Trim(),
                    dialogueName = fileName,
                    background = "",
                    bgm = "",
                    sfx = "",
                    expression = "",
                    effect = "",
                    duration = 3.0f
                };

                dialogueDatabase[dialogue.dialogueId] = dialogue;
            }
        }
        
        // 대화 시작
        public void StartDialogue(string dialogueId)
        {
            if (string.IsNullOrEmpty(dialogueId))
            {
                Debug.LogWarning("Invalid dialogue ID");
                return;
            }

            if (dialogueDatabase.ContainsKey(dialogueId))
            {
                currentDialogueData = dialogueDatabase[dialogueId];
                currentEntry = currentDialogueData;
                state = DialogueState.Playing;
                dialogueCoroutine = StartCoroutine(PlayDialogue(currentEntry));
            }
            else
            {
                Debug.LogError($"Dialogue with ID '{dialogueId}' not found!");
            }
        }
        
        // 대화 재생 코루틴
        IEnumerator PlayDialogue(DialogueData startEntry)
        {
            state = DialogueState.Playing;
            currentEntry = startEntry;
            
            OnDialogueStart?.Invoke(currentEntry);
            
            while (currentEntry != null)
            {
                // 대화 라인 시작
                OnDialogueLineStart?.Invoke(currentEntry);
                
                // 배경 변경
                if (!string.IsNullOrEmpty(currentEntry.background))
                {
                    OnBackgroundChange?.Invoke(currentEntry.background);
                }
                
                // BGM 변경
                if (!string.IsNullOrEmpty(currentEntry.bgm))
                {
                    OnBGMChange?.Invoke(currentEntry.bgm);
                }
                
                // 효과음 재생
                if (!string.IsNullOrEmpty(currentEntry.sfx))
                {
                    OnSFXPlay?.Invoke(currentEntry.sfx);
                }
                
                // 캐릭터 표정
                if (!string.IsNullOrEmpty(currentEntry.expression))
                {
                    OnCharacterExpression?.Invoke(currentEntry.characterName, currentEntry.expression);
                }
                
                // 특수 효과
                if (!string.IsNullOrEmpty(currentEntry.effect))
                {
                    OnEffectPlay?.Invoke(currentEntry.effect);
                }
                
                // 대화 표시 대기
                state = DialogueState.WaitingForInput;
                float elapsedTime = 0f;
                bool skipRequested = false;
                
                while (elapsedTime < currentEntry.duration && !skipRequested)
                {
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                    {
                        skipRequested = true;
                    }
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                
                // 다음 대화 대기
                state = DialogueState.Transitioning;
                OnDialogueLineEnd?.Invoke(currentEntry);
                
                yield return new WaitForSeconds(0.2f); // 짧은 전환 시간
                
                // 다음 대화로 이동
                if (string.IsNullOrEmpty(currentEntry.nextDialogueId))
                {
                    currentEntry = null; // 대화 종료
                }
                else
                {
                    // 단순화된 다음 대화 검색
                    string nextId = currentEntry.nextDialogueId;
                    // 단순화: 다음 대화로 이동하지 않고 대화 종료
                    Debug.Log($"Dialogue ending, next ID would be: {nextId}");
                    currentEntry = null;
                }
                
                state = DialogueState.Playing;
            }
            
            // 대화 종료
            state = DialogueState.Idle;
            currentDialogueData = null;
            currentEntry = null;
            
            OnDialogueEnd?.Invoke();
        }
        
        // 대화 스킵
        public void SkipDialogue()
        {
            if (state != DialogueState.Playing) return;
            
            // 대화 스킵 로직
            if (!string.IsNullOrEmpty(currentEntry.nextDialogueId))
            {
                StartDialogue(currentEntry.nextDialogueId);
            }
            else
            {
                EndDialogue();
            }
        }
        
        // 대화 강제 종료
        public void StopDialogue()
        {
            if (dialogueCoroutine != null)
            {
                StopCoroutine(dialogueCoroutine);
                dialogueCoroutine = null;
            }
            
            state = DialogueState.Idle;
            currentDialogueData = null;
            currentEntry = null;
            
            OnDialogueEnd?.Invoke();
        }
        
        // 특정 대화 데이터 로드
        public void LoadDialogueData(DialogueData dialogueData)
        {
            if (dialogueData != null && !dialogueDatabase.ContainsKey(dialogueData.dialogueName))
            {
                dialogueData.Initialize();
                dialogueDatabase.Add(dialogueData.dialogueName, dialogueData);
            }
        }
        
        // 대화 데이터 언로드
        public void UnloadDialogueData(string dialogueName)
        {
            if (dialogueDatabase.ContainsKey(dialogueName))
            {
                dialogueDatabase.Remove(dialogueName);
            }
        }
        
        // 조회 메서드들
        public DialogueState GetState()
        {
            return state;
        }
        
        public bool IsPlaying()
        {
            return state != DialogueState.Idle;
        }
        
        public DialogueData GetCurrentEntry()
        {
            return currentEntry;
        }
        
        public string GetCurrentDialogueName()
        {
            return currentDialogueData?.dialogueName;
        }
        
        public bool HasDialogue(string dialogueName)
        {
            return dialogueDatabase.ContainsKey(dialogueName);
        }
        
        public List<string> GetAllDialogueNames()
        {
            return new List<string>(dialogueDatabase.Keys);
        }

        public void NextDialogue()
        {
            if (state != DialogueState.Playing) return;
            
            // 현재 대화의 다음 ID 확인
            string nextId = currentEntry.nextDialogueId;
            
            if (!string.IsNullOrEmpty(nextId))
            {
                StartDialogue(nextId);
            }
            else
            {
                EndDialogue();
            }
        }

        // 대화 종료
        public void EndDialogue()
        {
            if (dialogueCoroutine != null)
            {
                StopCoroutine(dialogueCoroutine);
                dialogueCoroutine = null;
            }
            
            state = DialogueState.Idle;
            currentDialogueData = null;
            currentEntry = null;
            
            OnDialogueEnd?.Invoke();
        }
    }
}