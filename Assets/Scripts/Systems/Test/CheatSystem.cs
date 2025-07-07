using System;
using System.Collections.Generic;
using UnityEngine;
using GuildMaster.Core;
using GuildMaster.Systems;

namespace GuildMaster.Core
{
    /// <summary>
    /// 개발 및 테스트용 치트 시스템
    /// </summary>
    public class CheatSystem : MonoBehaviour
    {
        private static CheatSystem _instance;
        public static CheatSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("CheatSystem");
                    _instance = go.AddComponent<CheatSystem>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        [Header("Settings")]
        [SerializeField] private bool enableCheats = false;
        [SerializeField] private KeyCode cheatMenuKey = KeyCode.F12;
        [SerializeField] private string cheatPassword = "guildmaster123";
        
        // UI
        private bool showCheatMenu = false;
        private Vector2 scrollPosition;
        private string commandInput = "";
        private List<string> commandHistory = new List<string>();
        private GUIStyle windowStyle;
        private GUIStyle buttonStyle;
        private GUIStyle textFieldStyle;
        
        // Cheat Commands
        private Dictionary<string, CheatCommand> cheatCommands = new Dictionary<string, CheatCommand>();
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeCheatCommands();
            
            // 디버그 빌드에서만 활성화
            #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enableCheats = false;
            #endif
        }
        
        void InitializeCheatCommands()
        {
            // ResourceManager 타입이 제거되어 주석 처리
            // 자원 관련 치트
            // RegisterCheat("gold", "add_gold [amount]", "골드 추가", (args) => {
            //     int amount = args.Length > 0 ? int.Parse(args[0]) : 1000;
            //     ResourceManager.Instance?.AddGold(amount);
            //     return $"골드 {amount} 추가됨";
            // });
            // 
            // RegisterCheat("wood", "add_wood [amount]", "나무 추가", (args) => {
            //     int amount = args.Length > 0 ? int.Parse(args[0]) : 500;
            //     ResourceManager.Instance?.AddWood(amount);
            //     return $"나무 {amount} 추가됨";
            // });
            // 
            // RegisterCheat("stone", "add_stone [amount]", "돌 추가", (args) => {
            //     int amount = args.Length > 0 ? int.Parse(args[0]) : 300;
            //     ResourceManager.Instance?.AddStone(amount);
            //     return $"돌 {amount} 추가됨";
            // });
            // 
            // RegisterCheat("food", "add_food [amount]", "식량 추가", (args) => {
            //     int amount = args.Length > 0 ? int.Parse(args[0]) : 200;
            //     ResourceManager.Instance?.AddFood(amount);
            //     return $"식량 {amount} 추가됨";
            // });
            
            // 레벨 관련 치트
            RegisterCheat("levelup", "level_up [levels]", "길드 레벨업", (args) => {
                int levels = args.Length > 0 ? int.Parse(args[0]) : 1;
                for (int i = 0; i < levels; i++)
                {
                    GameManager.Instance?.GuildManager?.LevelUp();
                }
                return $"길드 레벨 {levels} 증가";
            });
            
            // 시간 관련 치트
            RegisterCheat("speed", "set_speed [multiplier]", "게임 속도 변경", (args) => {
                float speed = args.Length > 0 ? float.Parse(args[0]) : 1f;
                Time.timeScale = speed;
                return $"게임 속도: {speed}x";
            });
            
            RegisterCheat("pause", "pause_game", "게임 일시정지", (args) => {
                Time.timeScale = Time.timeScale > 0 ? 0 : 1;
                return Time.timeScale > 0 ? "게임 재개됨" : "게임 일시정지됨";
            });
            
            // 건물 관련 치트
            RegisterCheat("unlock", "unlock_all_buildings", "모든 건물 잠금 해제", (args) => {
                // 모든 건물 잠금 해제 로직
                return "모든 건물 잠금 해제됨";
            });
            
            // 완료 치트
            RegisterCheat("complete", "complete_all_quests", "모든 퀘스트 완료", (args) => {
                // 모든 활성 퀘스트 완료 로직
                return "모든 퀘스트 완료됨";
            });
            
            // 소환 치트
            RegisterCheat("spawn", "spawn_adventurer [class]", "모험가 소환", (args) => {
                string className = args.Length > 0 ? args[0] : "Warrior";
                // 모험가 소환 로직
                return $"{className} 모험가 소환됨";
            });
            
            // 시스템 관련 치트
            RegisterCheat("clear", "clear_console", "콘솔 정리", (args) => {
                commandHistory.Clear();
                return "콘솔이 정리되었습니다";
            });
            
            RegisterCheat("help", "help [command]", "도움말 표시", (args) => {
                if (args.Length > 0)
                {
                    string cmd = args[0];
                    if (cheatCommands.ContainsKey(cmd))
                    {
                        var cheat = cheatCommands[cmd];
                        return $"{cheat.usage} - {cheat.description}";
                    }
                    return $"명령어 '{cmd}'를 찾을 수 없습니다";
                }
                
                string helpText = "사용 가능한 치트 명령어:\n";
                foreach (var kvp in cheatCommands)
                {
                    helpText += $"{kvp.Value.usage} - {kvp.Value.description}\n";
                }
                return helpText;
            });
            
            // ResourceManager 타입이 제거되어 수정
            // 통계 치트
            RegisterCheat("stats", "show_stats", "게임 통계 표시", (args) => {
                var stats = "";
                // ResourceManager 타입이 제거되어 주석 처리
                // if (ResourceManager.Instance != null)
                // {
                //     // ResourceType removed - using string resource names
                //     stats += $"골드: {ResourceManager.Instance.GetGold()}\n";
                //     stats += $"나무: {ResourceManager.Instance.GetWood()}\n";
                //     stats += $"돌: {ResourceManager.Instance.GetStone()}\n";
                //     stats += $"식량: {ResourceManager.Instance.GetFood()}\n";
                // }
                if (GameManager.Instance?.GuildManager != null)
                {
                    stats += $"길드 레벨: {GameManager.Instance.GuildManager.GetGuildLevel()}\n";
                }
                return stats;
            });
            
            // 테스트 치트
            RegisterCheat("test", "test_feature [feature]", "기능 테스트", (args) => {
                string feature = args.Length > 0 ? args[0] : "notification";
                switch (feature.ToLower())
                {
                    case "notification":
                        NotificationManager.Instance?.ShowNotification("치트 테스트", "테스트 알림", GuildMaster.Data.NotificationType.Info);
                        return "알림 테스트 완료";
                    case "battle":
                        // 테스트 전투 시작
                        return "전투 테스트 시작";
                    default:
                        return $"알 수 없는 테스트 기능: {feature}";
                }
            });
        }
        
        void RegisterCheat(string command, string usage, string description, Func<string[], string> action)
        {
            cheatCommands[command] = new CheatCommand
            {
                command = command,
                usage = usage,
                description = description,
                action = action
            };
        }
        
        void Update()
        {
            if (!CanUseCheats()) return;
            
            // 치트 메뉴 토글
            if (Input.GetKeyDown(cheatMenuKey))
            {
                showCheatMenu = !showCheatMenu;
            }
            
            // 엔터 키로 명령어 실행
            if (showCheatMenu && Input.GetKeyDown(KeyCode.Return))
            {
                ExecuteCommand(commandInput);
                commandInput = "";
            }
        }
        
        void OnGUI()
        {
            if (!showCheatMenu || !CanUseCheats()) return;
            
            InitializeGUIStyles();
            
            // 치트 메뉴 창
            GUILayout.BeginArea(new Rect(10, 10, 400, Screen.height - 20));
            GUILayout.BeginVertical(windowStyle);
            
            GUILayout.Label("=== 치트 시스템 ===", buttonStyle);
            GUILayout.Space(5);
            
            // 명령어 입력
            GUILayout.BeginHorizontal();
            GUILayout.Label("명령어:", GUILayout.Width(60));
            GUI.SetNextControlName("CommandInput");
            commandInput = GUILayout.TextField(commandInput, textFieldStyle);
            if (GUILayout.Button("실행", GUILayout.Width(50)))
            {
                ExecuteCommand(commandInput);
                commandInput = "";
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // 명령어 히스토리
            GUILayout.Label("명령어 히스토리:");
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            
            for (int i = commandHistory.Count - 1; i >= 0; i--)
            {
                GUILayout.Label(commandHistory[i]);
            }
            
            GUILayout.EndScrollView();
            
            GUILayout.Space(5);
            
            // 퀴크 버튼들
            GUILayout.Label("퀵 치트:");
            GUILayout.BeginHorizontal();
            // ResourceManager 타입이 제거되어 주석 처리
            // if (GUILayout.Button("골드 +1000"))
            //     ExecuteCommand("gold 1000");
            // if (GUILayout.Button("모든 자원 +500"))
            // {
            //     ExecuteCommand("gold 500");
            //     ExecuteCommand("wood 500");
            //     ExecuteCommand("stone 500");
            //     ExecuteCommand("food 500");
            // }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("레벨업"))
                ExecuteCommand("levelup 1");
            if (GUILayout.Button("속도 2x"))
                ExecuteCommand("speed 2");
            if (GUILayout.Button("속도 리셋"))
                ExecuteCommand("speed 1");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("도움말"))
                ExecuteCommand("help");
            if (GUILayout.Button("통계"))
                ExecuteCommand("stats");
            if (GUILayout.Button("콘솔 정리"))
                ExecuteCommand("clear");
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            if (GUILayout.Button("치트 메뉴 닫기"))
            {
                showCheatMenu = false;
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
            
            // 포커스 설정
            if (showCheatMenu && UnityEngine.Event.current.type == UnityEngine.EventType.Repaint)
            {
                GUI.FocusControl("CommandInput");
            }
        }
        
        void InitializeGUIStyles()
        {
            if (windowStyle == null)
            {
                windowStyle = new GUIStyle(GUI.skin.box);
                windowStyle.padding = new RectOffset(10, 10, 10, 10);
                
                buttonStyle = new GUIStyle(GUI.skin.label);
                buttonStyle.fontSize = 14;
                buttonStyle.fontStyle = FontStyle.Bold;
                buttonStyle.alignment = TextAnchor.MiddleCenter;
                
                textFieldStyle = new GUIStyle(GUI.skin.textField);
                textFieldStyle.fontSize = 12;
            }
        }
        
        void ExecuteCommand(string input)
        {
            if (string.IsNullOrEmpty(input.Trim())) return;
            
            string[] parts = input.Trim().Split(' ');
            string command = parts[0].ToLower();
            string[] args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);
            
            string result;
            if (cheatCommands.ContainsKey(command))
            {
                try
                {
                    result = cheatCommands[command].action(args);
                }
                catch (Exception e)
                {
                    result = $"오류: {e.Message}";
                }
            }
            else
            {
                result = $"알 수 없는 명령어: {command}. 'help'를 입력하여 도움말을 확인하세요.";
            }
            
            commandHistory.Add($"> {input}");
            commandHistory.Add(result);
            
            // 히스토리 제한
            while (commandHistory.Count > 100)
            {
                commandHistory.RemoveAt(0);
            }
            
            Debug.Log($"[Cheat] {input} -> {result}");
        }
        
        bool CanUseCheats()
        {
            if (!enableCheats) return false;
            
            #if UNITY_EDITOR
            return true;
            #elif DEVELOPMENT_BUILD
            return true;
            #else
            return GameConfig.Instance?.CanUseCheats() ?? false;
            #endif
        }
        
        /// <summary>
        /// 치트 활성화 (비밀번호 필요)
        /// </summary>
        public bool EnableCheats(string password)
        {
            if (password == cheatPassword)
            {
                enableCheats = true;
                Debug.Log("[Cheat] 치트 시스템이 활성화되었습니다.");
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 치트 비활성화
        /// </summary>
        public void DisableCheats()
        {
            enableCheats = false;
            showCheatMenu = false;
            Debug.Log("[Cheat] 치트 시스템이 비활성화되었습니다.");
        }
        
        /// <summary>
        /// 치트 명령어 클래스
        /// </summary>
        private class CheatCommand
        {
            public string command;
            public string usage;
            public string description;
            public Func<string[], string> action;
        }
    }
} 