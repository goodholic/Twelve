using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using pjy.Characters.Behaviors;

namespace pjy.Characters
{
    /// <summary>
    /// 캐릭터 행동 관리자
    /// - CSV에서 지정된 행동 컴포넌트들을 동적으로 추가
    /// - 행동 우선순위 관리
    /// - 인스펙터에서 체크박스로 개별 행동 활성화/비활성화
    /// </summary>
    public class CharacterBehaviorManager : MonoBehaviour
    {
        [Header("행동 관리")]
        [SerializeField] private bool isActive = true;
        [SerializeField] private float behaviorUpdateInterval = 0.1f;
        
        [Header("등록된 행동들")]
        [SerializeField] private List<BehaviorEntry> behaviors = new List<BehaviorEntry>();
        
        private Character character;
        private float lastUpdateTime = 0f;
        
        /// <summary>
        /// 행동 엔트리 (인스펙터에서 체크박스로 관리)
        /// </summary>
        [System.Serializable]
        public class BehaviorEntry
        {
            public bool isEnabled = true;
            public CharacterBehaviorBase behavior;
            public float priority = 1.0f;
            
            [Header("디버그 정보")]
            public string behaviorName;
            public string lastExecutionTime;
        }
        
        private void Awake()
        {
            character = GetComponent<Character>();
        }
        
        private void Start()
        {
            InitializeBehaviors();
        }
        
        private void Update()
        {
            if (!isActive) return;
            
            // 주기적으로 행동 업데이트
            if (Time.time - lastUpdateTime >= behaviorUpdateInterval)
            {
                UpdateBehaviors();
                lastUpdateTime = Time.time;
            }
        }
        
        /// <summary>
        /// 행동들 초기화
        /// </summary>
        private void InitializeBehaviors()
        {
            // 기존의 모든 행동 컴포넌트들을 찾아서 등록
            CharacterBehaviorBase[] existingBehaviors = GetComponents<CharacterBehaviorBase>();
            
            foreach (var behavior in existingBehaviors)
            {
                if (!behaviors.Any(b => b.behavior == behavior))
                {
                    AddBehavior(behavior);
                }
            }
            
            // 모든 행동 초기화
            foreach (var entry in behaviors)
            {
                if (entry.behavior != null)
                {
                    entry.behavior.Initialize(character);
                    entry.behaviorName = entry.behavior.BehaviorName;
                }
            }
            
            // 우선순위로 정렬
            SortBehaviorsByPriority();
            
            Debug.Log($"[CharacterBehaviorManager] {character.characterName}의 {behaviors.Count}개 행동이 초기화되었습니다.");
        }
        
        /// <summary>
        /// 행동들 업데이트
        /// </summary>
        private void UpdateBehaviors()
        {
            if (behaviors.Count == 0) return;
            
            // 우선순위가 높은 순서대로 실행 가능한 행동 찾기
            foreach (var entry in behaviors)
            {
                if (!entry.isEnabled || entry.behavior == null) continue;
                
                // 행동 실행 조건 확인
                if (entry.behavior.CanExecute())
                {
                    // 첫 번째로 실행 가능한 행동을 실행하고 종료
                    entry.behavior.Execute();
                    entry.lastExecutionTime = System.DateTime.Now.ToString("HH:mm:ss");
                    break; // 한 번에 하나의 행동만 실행
                }
            }
        }
        
        /// <summary>
        /// CSV에서 지정된 행동 컴포넌트들을 추가
        /// </summary>
        public void SetupBehaviorsFromCSV(string[] behaviorComponentNames)
        {
            if (behaviorComponentNames == null || behaviorComponentNames.Length == 0)
                return;
            
            foreach (string behaviorName in behaviorComponentNames)
            {
                if (string.IsNullOrEmpty(behaviorName)) continue;
                
                // 행동 컴포넌트 타입 찾기
                System.Type behaviorType = FindBehaviorType(behaviorName);
                if (behaviorType == null)
                {
                    Debug.LogWarning($"[CharacterBehaviorManager] 행동 타입을 찾을 수 없습니다: {behaviorName}");
                    continue;
                }
                
                // 이미 해당 컴포넌트가 있는지 확인
                if (GetComponent(behaviorType) != null)
                {
                    Debug.Log($"[CharacterBehaviorManager] {behaviorName} 컴포넌트가 이미 존재합니다.");
                    continue;
                }
                
                // 컴포넌트 추가
                CharacterBehaviorBase behavior = gameObject.AddComponent(behaviorType) as CharacterBehaviorBase;
                if (behavior != null)
                {
                    AddBehavior(behavior);
                    Debug.Log($"[CharacterBehaviorManager] {behaviorName} 행동이 추가되었습니다.");
                }
            }
            
            // 추가 완료 후 초기화
            InitializeBehaviors();
        }
        
        /// <summary>
        /// 행동 타입 찾기
        /// </summary>
        private System.Type FindBehaviorType(string behaviorName)
        {
            // 네임스페이스 포함한 전체 타입명 시도
            string fullTypeName = $"pjy.Characters.Behaviors.{behaviorName}";
            System.Type type = System.Type.GetType(fullTypeName);
            
            if (type != null) return type;
            
            // 전체 어셈블리에서 타입 검색
            System.Type[] allTypes = System.Reflection.Assembly.GetExecutingAssembly().GetTypes();
            return allTypes.FirstOrDefault(t => t.Name == behaviorName && 
                                               typeof(CharacterBehaviorBase).IsAssignableFrom(t));
        }
        
        /// <summary>
        /// 행동 추가
        /// </summary>
        public void AddBehavior(CharacterBehaviorBase behavior)
        {
            if (behavior == null) return;
            
            // 이미 등록된 행동인지 확인
            if (behaviors.Any(b => b.behavior == behavior)) return;
            
            BehaviorEntry entry = new BehaviorEntry
            {
                isEnabled = true,
                behavior = behavior,
                priority = behavior.Priority,
                behaviorName = behavior.BehaviorName,
                lastExecutionTime = "없음"
            };
            
            behaviors.Add(entry);
            SortBehaviorsByPriority();
        }
        
        /// <summary>
        /// 행동 제거
        /// </summary>
        public void RemoveBehavior(CharacterBehaviorBase behavior)
        {
            behaviors.RemoveAll(b => b.behavior == behavior);
            
            if (behavior != null)
            {
                Destroy(behavior);
            }
        }
        
        /// <summary>
        /// 행동 활성화/비활성화
        /// </summary>
        public void SetBehaviorEnabled(string behaviorName, bool enabled)
        {
            var entry = behaviors.FirstOrDefault(b => b.behaviorName == behaviorName);
            if (entry != null)
            {
                entry.isEnabled = enabled;
                if (entry.behavior != null)
                {
                    entry.behavior.SetEnabled(enabled);
                }
            }
        }
        
        /// <summary>
        /// 특정 행동 활성화/비활성화 (타입으로)
        /// </summary>
        public void SetBehaviorEnabled<T>(bool enabled) where T : CharacterBehaviorBase
        {
            var entry = behaviors.FirstOrDefault(b => b.behavior is T);
            if (entry != null)
            {
                entry.isEnabled = enabled;
                if (entry.behavior != null)
                {
                    entry.behavior.SetEnabled(enabled);
                }
            }
        }
        
        /// <summary>
        /// 우선순위로 행동 정렬
        /// </summary>
        private void SortBehaviorsByPriority()
        {
            behaviors = behaviors.OrderByDescending(b => b.priority).ToList();
        }
        
        /// <summary>
        /// 모든 행동 중지
        /// </summary>
        public void StopAllBehaviors()
        {
            foreach (var entry in behaviors)
            {
                if (entry.behavior != null)
                {
                    entry.behavior.StopBehavior();
                }
            }
        }
        
        /// <summary>
        /// 행동 관리자 활성화/비활성화
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
            
            if (!active)
            {
                StopAllBehaviors();
            }
        }
        
        /// <summary>
        /// 행동 정보 가져오기 (디버그용)
        /// </summary>
        public List<string> GetBehaviorInfo()
        {
            List<string> info = new List<string>();
            
            foreach (var entry in behaviors)
            {
                string status = entry.isEnabled ? "활성" : "비활성";
                info.Add($"{entry.behaviorName} - {status} (우선순위: {entry.priority})");
            }
            
            return info;
        }
        
        private void OnDisable()
        {
            StopAllBehaviors();
        }
        
        private void OnDestroy()
        {
            StopAllBehaviors();
        }
        
        // 에디터에서 행동 상태 확인용
        #if UNITY_EDITOR
        [ContextMenu("Refresh Behaviors")]
        private void RefreshBehaviors()
        {
            InitializeBehaviors();
            Debug.Log("[CharacterBehaviorManager] 행동들이 새로고침되었습니다.");
        }
        
        [ContextMenu("Show Behavior Info")]
        private void ShowBehaviorInfo()
        {
            List<string> info = GetBehaviorInfo();
            string infoText = string.Join("\n", info);
            Debug.Log($"[CharacterBehaviorManager] {character.characterName}의 행동 정보:\n{infoText}");
        }
        #endif
    }
}