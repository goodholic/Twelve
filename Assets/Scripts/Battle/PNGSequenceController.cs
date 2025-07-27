using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GuildMaster.Battle
{
    /// <summary>
/// PNG 시퀀스 투명배경 애니메이션 컨트롤러
/// Texture2D 배열을 사용하여 PNG 시퀀스로 투명배경 캐릭터 애니메이션을 재생합니다.
/// 완벽한 투명배경 지원, 코덱 문제 없음, 모든 플랫폼 호환.
/// </summary>
public class PNGSequenceController : MonoBehaviour
    {
        [Header("컴포넌트 참조")]
        [Space(10)]
        [SerializeField, Tooltip("PNG 시퀀스를 표시할 RawImage 컴포넌트")]
        public RawImage displayImage;
        
        [Header("캐릭터 데이터")]
        [Space(10)]
        [SerializeField, Tooltip("PNG 시퀀스 정보가 포함된 캐릭터 데이터")]
        public CharacterData characterData;
        
        [Header("🖼️ PNG 시퀀스 애니메이션 (권장!)")]
        [Space(10)]
        [SerializeField, Tooltip("대기 상태 PNG 시퀀스 (투명배경 완벽 지원)")]
        public Texture2D[] idlePNGFrames;
        
        [Space(5)]
        [SerializeField, Tooltip("공격 상태 PNG 시퀀스 (투명배경 완벽 지원)")]
        public Texture2D[] attackPNGFrames;
        
        [Space(5)]
        [SerializeField, Tooltip("PNG 시퀀스 재생 속도 (FPS)")]
        [Range(1, 60)]
        public int frameRate = 12;
        
        [Header("현재 상태")]
        [Space(10)]
        [SerializeField, Tooltip("현재 재생 중인 애니메이션 상태")]
        public CharacterAnimationState currentState = CharacterAnimationState.Idle;
        
        // PNG 시퀀스 재생 관련
        private bool isInitialized = false;
        private Coroutine pngSequenceCoroutine;
        private Texture2D[] currentPNGSequence;
        private int currentFrameIndex = 0;
        private bool isPNGSequencePlaying = false;
        
        public enum CharacterAnimationState
        {
            Idle,    // 대기 (기본값)
            Attack,  // 공격
            Death    // 죽음 (연기 효과)
        }
        
        private void Awake()
        {
            InitializePNGSequenceController();
        }
        
        private void Start()
        {
            if (characterData != null && characterData.animationType == AnimationType.PNGSequence)
            {
                if (ValidatePNGSequences())
                {
                    SetupPNGSequenceAnimation();
                    PlayAnimation(CharacterAnimationState.Idle);
                }
                else
                {
                    Debug.LogWarning($"[PNG Controller] {gameObject.name}: 필수 PNG 시퀀스가 없습니다. idle과 attack PNG 시퀀스가 모두 필요합니다.");
                }
            }
        }
        
        /// <summary>
        /// PNG 시퀀스 컨트롤러 초기화
        /// </summary>
        private void InitializePNGSequenceController()
        {
            Debug.Log($"[PNG Controller] {gameObject.name}: PNG 시퀀스 애니메이션 시스템 초기화");
            
            // UI 표시 컴포넌트 자동 설정
            if (displayImage == null)
            {
                displayImage = GetComponent<RawImage>();
                if (displayImage != null)
                {
                    Debug.Log($"[PNG Controller] {gameObject.name}: RawImage 컴포넌트 자동 연결됨");
                }
            }
            
            isInitialized = true;
            Debug.Log($"[PNG Controller] {gameObject.name}: 초기화 완료");
        }
        

        
        /// <summary>
        /// 캐릭터 상태에 따른 PNG 시퀀스 애니메이션 재생
        /// </summary>
        public void PlayAnimation(CharacterAnimationState state)
        {
            if (!isInitialized || characterData == null || characterData.animationType != AnimationType.PNGSequence)
                return;
            
            currentState = state;
            PlayPNGSequenceAnimation(state);
        }
        

        
        /// <summary>
        /// PNG 시퀀스 애니메이션 재생
        /// </summary>
        private void PlayPNGSequenceAnimation(CharacterAnimationState state)
        {
            Texture2D[] targetSequence = GetPNGSequenceForState(state);
            
            if (targetSequence != null && targetSequence.Length > 0)
            {
                // 기존 PNG 시퀀스 재생 중지
                if (pngSequenceCoroutine != null)
                {
                    StopCoroutine(pngSequenceCoroutine);
                }
                
                currentPNGSequence = targetSequence;
                currentFrameIndex = 0;
                
                // PNG 시퀀스 재생 시작
                pngSequenceCoroutine = StartCoroutine(PlayPNGSequenceCoroutine());
                
                Debug.Log($"[PNG Controller] Playing {state} animation: {targetSequence.Length} frames at {frameRate} FPS");
            }
            else
            {
                Debug.LogWarning($"[PNG Controller] No PNG sequence found for state: {state}");
            }
        }
        
        /// <summary>
        /// 상태에 해당하는 PNG 시퀀스 반환
        /// </summary>
        private Texture2D[] GetPNGSequenceForState(CharacterAnimationState state)
        {
            switch (state)
            {
                case CharacterAnimationState.Attack:
                    // 직접 할당된 PNG 시퀀스 우선
                    if (attackPNGFrames != null && attackPNGFrames.Length > 0) 
                        return attackPNGFrames;
                    // CharacterData에서 가져오기
                    if (characterData?.attackPNGSequence != null && characterData.attackPNGSequence.Length > 0)
                        return characterData.attackPNGSequence;
                    return null;
                    
                case CharacterAnimationState.Death:
                    return null; // Death는 연기 효과
                    
                default: // Idle 및 기타 모든 상태
                    // 직접 할당된 PNG 시퀀스 우선
                    if (idlePNGFrames != null && idlePNGFrames.Length > 0) 
                        return idlePNGFrames;
                    // CharacterData에서 가져오기
                    if (characterData?.idlePNGSequence != null && characterData.idlePNGSequence.Length > 0)
                        return characterData.idlePNGSequence;
                    return null;
            }
        }
        

        
        /// <summary>
        /// PNG 시퀀스 재생 코루틴
        /// </summary>
        private IEnumerator PlayPNGSequenceCoroutine()
        {
            if (currentPNGSequence == null || currentPNGSequence.Length == 0)
                yield break;
                
            isPNGSequencePlaying = true;
            currentFrameIndex = 0;
            
            // 프레임레이트 계산 (CharacterData 설정 우선)
            int actualFrameRate = frameRate;
            if (characterData != null && characterData.pngSequenceFrameRate > 0)
            {
                actualFrameRate = characterData.pngSequenceFrameRate;
            }
            
            float frameDuration = 1f / actualFrameRate;
            
            while (isPNGSequencePlaying)
            {
                // 현재 프레임 표시
                if (currentFrameIndex < currentPNGSequence.Length)
                {
                    Texture2D currentFrame = currentPNGSequence[currentFrameIndex];
                    DisplayPNGFrame(currentFrame);
                }
                
                // 다음 프레임으로 이동
                currentFrameIndex++;
                
                // 루프 처리
                if (currentFrameIndex >= currentPNGSequence.Length)
                {
                    if (characterData != null && characterData.loopPNGSequences)
                    {
                        currentFrameIndex = 0; // 루프
                    }
                    else
                    {
                        isPNGSequencePlaying = false; // 한 번만 재생
                        break;
                    }
                }
                
                yield return new WaitForSeconds(frameDuration);
            }
        }
        
        /// <summary>
        /// PNG 프레임을 UI 또는 Renderer에 표시
        /// </summary>
        private void DisplayPNGFrame(Texture2D frame)
        {
            if (frame == null) return;
            
            // UI Image에 표시 (우선순위)
            if (displayImage != null)
            {
                displayImage.texture = frame;
            }
            
            // 3D Renderer에 표시
            var renderer = GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.mainTexture = frame;
            }
        }
        
        /// <summary>
        /// 공격 애니메이션 재생 (PNG 시퀀스, 일정 시간 후 대기 상태로 복귀)
        /// </summary>
        public void PlayAttackAnimation()
        {
            PlayAnimation(CharacterAnimationState.Attack);
            
            // PNG 시퀀스 프레임 수 기반으로 복귀 시간 계산
            if (characterData != null && characterData.attackPNGSequence != null && characterData.attackPNGSequence.Length > 0)
            {
                float frameRate = characterData.pngSequenceFrameRate > 0 ? characterData.pngSequenceFrameRate : this.frameRate;
                float animationDuration = characterData.attackPNGSequence.Length / frameRate;
                StartCoroutine(ReturnToIdleAfterDelay(animationDuration));
            }
        }
        
        /// <summary>
        /// 스킬 애니메이션 재생 (attack 동영상 사용)
        /// </summary>
        public void PlaySkillAnimation()
        {
            // 스킬은 attack 애니메이션과 동일하게 처리
            PlayAttackAnimation();
        }
        
        /// <summary>
        /// 죽음 효과 재생 (연기 효과로 사라짐)
        /// </summary>
        public void PlayDeathEffect()
        {
            Debug.Log($"[PNG Controller] {gameObject.name}: Playing death smoke effect");
            
            // 동영상 재생 중지
            StopAnimation();
            
            // 연기 효과 시작 (파티클 시스템이나 페이드 아웃 효과)
            StartCoroutine(PlaySmokeDisappearEffect());
        }
        
        /// <summary>
        /// 연기로 사라지는 효과
        /// </summary>
        private IEnumerator PlaySmokeDisappearEffect()
        {
            // 연기 파티클 효과 재생 (ParticleSystem이 있다면)
            ParticleSystem smokeParticle = GetComponentInChildren<ParticleSystem>();
            if (smokeParticle != null)
            {
                smokeParticle.Play();
                yield return new WaitForSeconds(1.0f); // 파티클 지속 시간
            }
            
            // 캐릭터 페이드 아웃
            float fadeTime = 1.0f;
            float elapsed = 0;
            
            // UI 캐릭터 페이드 (RawImage)
            if (displayImage != null)
            {
                Color originalColor = displayImage.color;
                while (elapsed < fadeTime)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(1.0f, 0.0f, elapsed / fadeTime);
                    displayImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                    yield return null;
                }
            }
            
            // 3D 캐릭터 페이드 (Renderer)
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = renderer.material;
                Color originalColor = material.color;
                elapsed = 0;
                
                while (elapsed < fadeTime)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(1.0f, 0.0f, elapsed / fadeTime);
                    material.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                    yield return null;
                }
            }
            
            // 완전히 사라짐
            gameObject.SetActive(false);
            Debug.Log($"[PNG Controller] {gameObject.name}: Death effect completed - character disappeared");
        }
        
        /// <summary>
        /// 지정된 시간 후 대기 상태로 복귀
        /// </summary>
        private IEnumerator ReturnToIdleAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            PlayAnimation(CharacterAnimationState.Idle);
        }
        
        /// <summary>
        /// PNG 시퀀스 애니메이션 중지
        /// </summary>
        public void StopAnimation()
        {
            if (pngSequenceCoroutine != null)
            {
                StopCoroutine(pngSequenceCoroutine);
                pngSequenceCoroutine = null;
            }
            isPNGSequencePlaying = false;
            Debug.Log($"[PNG Controller] {gameObject.name}: PNG 시퀀스 애니메이션 중지됨");
        }
        
        /// <summary>
        /// PNG 시퀀스 애니메이션 일시정지
        /// </summary>
        public void PauseAnimation()
        {
            isPNGSequencePlaying = false;
            Debug.Log($"[PNG Controller] {gameObject.name}: PNG 시퀀스 애니메이션 일시정지됨");
        }
        
        /// <summary>
        /// PNG 시퀀스 애니메이션 재개
        /// </summary>
        public void ResumeAnimation()
        {
            if (currentPNGSequence != null && currentPNGSequence.Length > 0)
            {
                isPNGSequencePlaying = true;
                if (pngSequenceCoroutine == null)
                {
                    pngSequenceCoroutine = StartCoroutine(PlayPNGSequenceCoroutine());
                }
                Debug.Log($"[PNG Controller] {gameObject.name}: PNG 시퀀스 애니메이션 재개됨");
            }
        }
        

        
        /// <summary>
        /// PNG 시퀀스 유효성 검사 (idle, attack 필수)
        /// </summary>
        private bool ValidatePNGSequences()
        {
            // 직접 할당된 PNG 시퀀스 체크
            bool hasDirectSequences = (idlePNGFrames != null && idlePNGFrames.Length > 0) && 
                                     (attackPNGFrames != null && attackPNGFrames.Length > 0);
            
            // CharacterData에서 PNG 시퀀스 체크
            bool hasCharacterDataSequences = false;
            if (characterData != null)
            {
                hasCharacterDataSequences = (characterData.idlePNGSequence != null && characterData.idlePNGSequence.Length > 0) &&
                                           (characterData.attackPNGSequence != null && characterData.attackPNGSequence.Length > 0);
            }
            
            // 둘 중 하나라도 완전하면 OK
            bool hasRequiredSequences = hasDirectSequences || hasCharacterDataSequences;
            
            if (!hasRequiredSequences)
            {
                Debug.LogError($"[PNG Controller] {gameObject.name}: 필수 PNG 시퀀스가 누락되었습니다.");
                Debug.LogError($"PNG 직접할당 - idle: {(idlePNGFrames?.Length ?? 0)} frames, attack: {(attackPNGFrames?.Length ?? 0)} frames");
                if (characterData != null)
                {
                    Debug.LogError($"CharacterData - idle: {(characterData.idlePNGSequence?.Length ?? 0)} frames, attack: {(characterData.attackPNGSequence?.Length ?? 0)} frames");
                }
                Debug.LogError("💡 해결: Inspector에서 Idle PNG Frames와 Attack PNG Frames에 PNG 시퀀스를 할당하세요.");
                return false;
            }
            
            // PNG 시퀀스 품질 검증
            ValidatePNGSequence(idlePNGFrames ?? characterData?.idlePNGSequence, "Idle PNG Sequence");
            ValidatePNGSequence(attackPNGFrames ?? characterData?.attackPNGSequence, "Attack PNG Sequence");
            
            Debug.Log($"[PNG Controller] {gameObject.name}: PNG 시퀀스 애니메이션 준비 완료");
            Debug.Log($"✅ Idle: {(idlePNGFrames?.Length ?? characterData?.idlePNGSequence?.Length ?? 0)} frames");
            Debug.Log($"✅ Attack: {(attackPNGFrames?.Length ?? characterData?.attackPNGSequence?.Length ?? 0)} frames");
            Debug.Log($"🎬 Frame Rate: {(characterData?.pngSequenceFrameRate ?? frameRate)} FPS");
            
            return true;
        }
        
        /// <summary>
        /// PNG 시퀀스 품질 검증
        /// </summary>
        private void ValidatePNGSequence(Texture2D[] sequence, string sequenceName)
        {
            if (sequence == null || sequence.Length == 0) return;
            
            Debug.Log($"[PNG Controller] ✅ {sequenceName}: {sequence.Length} frames 로드됨");
            
            // 투명배경 확인
            bool hasAlpha = false;
            foreach (var frame in sequence)
            {
                if (frame != null && (frame.format == TextureFormat.RGBA32 || frame.format == TextureFormat.ARGB32))
                {
                    hasAlpha = true;
                    break;
                }
            }
            
            if (hasAlpha)
            {
                Debug.Log($"🎭 {sequenceName}: 투명배경 지원 확인됨");
            }
            else
            {
                Debug.LogWarning($"⚠️ {sequenceName}: 투명배경이 감지되지 않았습니다. RGBA 포맷인지 확인하세요.");
            }
            
            // 해상도 일관성 체크
            if (sequence.Length > 1)
            {
                int width = sequence[0]?.width ?? 0;
                int height = sequence[0]?.height ?? 0;
                bool consistentResolution = true;
                
                foreach (var frame in sequence)
                {
                    if (frame != null && (frame.width != width || frame.height != height))
                    {
                        consistentResolution = false;
                        break;
                    }
                }
                
                if (consistentResolution)
                {
                    Debug.Log($"📐 {sequenceName}: 해상도 일관성 확인됨 ({width}x{height})");
                }
                else
                {
                    Debug.LogWarning($"📐 {sequenceName}: 프레임 해상도가 일치하지 않습니다. 동일한 해상도 사용을 권장합니다.");
                }
            }
        }
        
        /// <summary>
        /// PNG 시퀀스 애니메이션 설정
        /// </summary>
        private void SetupPNGSequenceAnimation()
        {
            Debug.Log($"[PNG Controller] {gameObject.name}: PNG 시퀀스 애니메이션 시스템 초기화");
            
            // CharacterData에서 설정 가져오기
            if (characterData != null)
            {
                if (idlePNGFrames == null || idlePNGFrames.Length == 0)
                {
                    idlePNGFrames = characterData.idlePNGSequence;
                }
                
                if (attackPNGFrames == null || attackPNGFrames.Length == 0)
                {
                    attackPNGFrames = characterData.attackPNGSequence;
                }
                
                // 프레임레이트 설정
                if (characterData.pngSequenceFrameRate > 0)
                {
                    frameRate = characterData.pngSequenceFrameRate;
                }
            }
            
            // UI Image 설정 (있다면)
            if (displayImage != null)
            {
                Debug.Log($"[PNG Controller] UI 표시용 RawImage 연결됨: {displayImage.name}");
            }
            
            // 3D Renderer 설정 (있다면)
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Debug.Log($"[PNG Controller] 3D Renderer 연결됨: {renderer.name}");
                
                // 비디오 스케일 적용
                if (characterData != null)
                {
                    Vector3 scale = transform.localScale;
                    scale *= characterData.pngSequenceScale;
                    transform.localScale = scale;
                }
            }
            
            Debug.Log($"[PNG Controller] PNG 시퀀스 시스템 준비 완료!");
        }
        

        
        /// <summary>
        /// 캐릭터 데이터 설정
        /// </summary>
        public void SetCharacterData(CharacterData data)
        {
            characterData = data;
            
            if (characterData != null && characterData.animationType == AnimationType.PNGSequence)
            {
                if (ValidatePNGSequences())
                {
                    SetupPNGSequenceAnimation();
                    PlayAnimation(CharacterAnimationState.Idle);
                }
            }
        }
        
        /// <summary>
        /// 현재 PNG 시퀀스 애니메이션이 끝났는지 확인
        /// </summary>
        public bool IsPNGSequenceFinished()
        {
            if (currentPNGSequence == null || currentPNGSequence.Length == 0)
                return true;
                
            return !isPNGSequencePlaying;
        }
        
        private void OnDestroy()
        {
            // PNG 시퀀스 코루틴 정리
            if (pngSequenceCoroutine != null)
            {
                StopCoroutine(pngSequenceCoroutine);
                pngSequenceCoroutine = null;
            }
        }
        
        [ContextMenu("🖼️ PNG 시퀀스 애니메이션 가이드 (권장!)")]
        public void ShowPNGSequenceGuide()
        {
            Debug.Log("=== 🖼️ PNG 시퀀스 투명배경 애니메이션 시스템 (권장!) ===");
            Debug.Log("📁 1. 투명배경 PNG 시퀀스 파일들을 Project 창에 드래그");
            Debug.Log("📝 2. 파일명 규칙: idle_01.png, idle_02.png, ... attack_01.png, attack_02.png, ...");
            Debug.Log("🎭 3. PNG 파일들을 Inspector에서 확인 → 투명배경(RGBA) 자동 인식");
            Debug.Log("📊 4. PNG Import Settings: Texture Type → Sprite (2D and UI)");
            Debug.Log("🎯 5. Alpha Source: Input Texture Alpha");
            Debug.Log("📐 6. Inspector의 Idle PNG Frames 배열에 idle 시퀀스 드래그");
            Debug.Log("⚔️ 7. Attack PNG Frames 배열에 attack 시퀀스 드래그");
            Debug.Log("🎬 8. Frame Rate: 12-24 FPS (권장)");
            Debug.Log("✔️ 9. Animation Type을 'PNGSequence'로 설정");
            Debug.Log("==========================================");
            Debug.Log("✨ PNG 시퀀스의 장점:");
            Debug.Log("🚫 코덱 문제 없음 (Apple ProRes 걱정 NO!)");
            Debug.Log("🎭 완벽한 투명배경 지원 (알파채널 100%)");
            Debug.Log("⚡ 빠른 로딩 및 재생");
            Debug.Log("🔧 편집 용이 (개별 프레임 수정 가능)");
            Debug.Log("🌍 완벽한 플랫폼 호환성");
            Debug.Log("📏 권장 해상도: 512x512 또는 1024x1024");
            Debug.Log("📝 파일명: 영문 + 숫자 사용 (character_idle_01.png)");
            Debug.Log("🎯 권장 사용법: MOV 대신 PNG 시퀀스 사용!");
            Debug.Log("==========================================");
            Debug.Log("🛠️ 빠른 설정 도구:");
            Debug.Log("Unity 메뉴 → Twelve → 🖼️ PNG 시퀀스 도구 → 📁 Video 폴더에서 빠른 설정");
            Debug.Log("==========================================");
        }
        
        [ContextMenu("🛠️ PNG 시퀀스 자동 설정 도구 열기")]
        public void OpenPNGSequenceTools()
        {
            #if UNITY_EDITOR
            Debug.Log("🛠️ PNG 시퀀스 자동 설정 도구 열기");
            Debug.Log("Unity 메뉴 → Twelve → 🖼️ PNG 시퀀스 도구 → PNG 시퀀스 자동 설정");
            #endif
        }
        
        [ContextMenu("📁 Video 폴더에서 빠른 설정")]
        public void QuickSetupFromVideoFolder()
        {
            #if UNITY_EDITOR
            Debug.Log("📁 Video 폴더에서 빠른 설정");
            Debug.Log("Unity 메뉴 → Twelve → 🖼️ PNG 시퀀스 도구 → 📁 Video 폴더에서 빠른 설정");
            #endif
        }
        
        #if UNITY_EDITOR
        [Header("에디터 테스트")]
        [Space]
        public bool testInEditor = false;
        public CharacterAnimationState testState = CharacterAnimationState.Idle;
        
        private void OnValidate()
        {
            if (testInEditor && Application.isPlaying)
            {
                PlayAnimation(testState);
                testInEditor = false;
            }
        }
        #endif
    }
} 