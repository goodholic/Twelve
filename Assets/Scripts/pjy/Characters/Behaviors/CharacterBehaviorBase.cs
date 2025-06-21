using UnityEngine;

namespace pjy.Characters.Behaviors
{
    /// <summary>
    /// 캐릭터 행동 컴포넌트 기본 클래스
    /// - 모든 캐릭터 행동의 베이스
    /// - 인스펙터에서 체크박스로 활성화/비활성화
    /// - 상속을 통한 다형성 구현
    /// </summary>
    public abstract class CharacterBehaviorBase : MonoBehaviour
    {
        [Header("행동 설정")]
        [SerializeField] protected bool isEnabled = true;
        [SerializeField] protected float behaviorPriority = 1.0f;
        [SerializeField] protected float behaviorCooldown = 0f;
        
        [Header("디버그")]
        [SerializeField] protected bool showDebugLogs = false;
        
        protected Character character;
        protected float lastBehaviorTime = 0f;
        protected bool isInitialized = false;
        
        /// <summary>
        /// 행동이 활성화되었는지 확인
        /// </summary>
        public bool IsEnabled => isEnabled && isInitialized;
        
        /// <summary>
        /// 행동 우선도
        /// </summary>
        public float Priority => behaviorPriority;
        
        /// <summary>
        /// 행동 이름 (자식 클래스에서 오버라이드)
        /// </summary>
        public virtual string BehaviorName => GetType().Name;
        
        /// <summary>
        /// 초기화
        /// </summary>
        public virtual void Initialize(Character targetCharacter)
        {
            character = targetCharacter;
            isInitialized = true;
            
            if (showDebugLogs)
                Debug.Log($"[{BehaviorName}] {character.characterName}에 초기화됨");
                
            OnInitialize();
        }
        
        /// <summary>
        /// 자식 클래스별 초기화 (오버라이드 용)
        /// </summary>
        protected virtual void OnInitialize() { }
        
        /// <summary>
        /// 행동 실행 조건 확인
        /// </summary>
        public virtual bool CanExecute()
        {
            if (!IsEnabled) return false;
            if (character == null) return false;
            if (Time.time - lastBehaviorTime < behaviorCooldown) return false;
            
            return CheckExecuteConditions();
        }
        
        /// <summary>
        /// 자식 클래스별 실행 조건 (오버라이드 필수)
        /// </summary>
        protected abstract bool CheckExecuteConditions();
        
        /// <summary>
        /// 행동 실행
        /// </summary>
        public virtual void Execute()
        {
            if (!CanExecute()) return;
            
            lastBehaviorTime = Time.time;
            
            if (showDebugLogs)
                Debug.Log($"[{BehaviorName}] {character.characterName}이(가) 행동 실행");
                
            OnExecute();
        }
        
        /// <summary>
        /// 자식 클래스별 행동 실행 (오버라이드 필수)
        /// </summary>
        protected abstract void OnExecute();
        
        /// <summary>
        /// 행동 업데이트 (매 프레임 호출)
        /// </summary>
        public virtual void UpdateBehavior()
        {
            if (!IsEnabled) return;
            
            OnUpdateBehavior();
        }
        
        /// <summary>
        /// 자식 클래스별 업데이트 (오버라이드 용)
        /// </summary>
        protected virtual void OnUpdateBehavior() { }
        
        /// <summary>
        /// 행동 중지
        /// </summary>
        public virtual void StopBehavior()
        {
            if (showDebugLogs)
                Debug.Log($"[{BehaviorName}] {character.characterName}의 행동 중지");
                
            OnStopBehavior();
        }
        
        /// <summary>
        /// 자식 클래스별 중지 처리 (오버라이드 용)
        /// </summary>
        protected virtual void OnStopBehavior() { }
        
        /// <summary>
        /// 행동 활성화/비활성화
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            bool wasEnabled = isEnabled;
            isEnabled = enabled;
            
            if (wasEnabled && !enabled)
            {
                StopBehavior();
            }
            
            if (showDebugLogs)
                Debug.Log($"[{BehaviorName}] {character.characterName}의 행동 {(enabled ? "활성화" : "비활성화")}");
        }
        
        /// <summary>
        /// 쿨다운 설정
        /// </summary>
        public void SetCooldown(float cooldown)
        {
            behaviorCooldown = cooldown;
        }
        
        /// <summary>
        /// 우선도 설정
        /// </summary>
        public void SetPriority(float priority)
        {
            behaviorPriority = priority;
        }
        
        protected virtual void Update()
        {
            if (IsEnabled)
            {
                UpdateBehavior();
            }
        }
        
        protected virtual void OnDisable()
        {
            StopBehavior();
        }
        
        protected virtual void OnDestroy()
        {
            StopBehavior();
        }
    }
}