using UnityEngine;
using System.Collections;

namespace TwelveGame.Battle
{
    /// <summary>
    /// 캐릭터 애니메이션을 관리하는 컴포넌트
    /// idle 모션 (기본 상태)와 attack 모션 (배치 시 한번)을 처리
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        public float attackAnimationDuration = 1.0f;
        public float idleAnimationSpeed = 0.5f;
        
        [Header("Attack Animation")]
        public AnimationCurve attackScaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1.3f);
        public AnimationCurve attackRotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 360);
        
        [Header("Idle Animation")]
        public AnimationCurve idleBounceCurve = AnimationCurve.EaseInOut(0, 1, 1, 1.1f);
        
        private Vector3 originalScale;
        private Vector3 originalRotation;
        private Vector3 originalPosition;
        private bool isPlayingAttack = false;
        private Coroutine currentIdleCoroutine;
        private Coroutine currentAttackCoroutine;
        
        void Start()
        {
            // 원본 트랜스폼 저장
            originalScale = transform.localScale;
            originalRotation = transform.localEulerAngles;
            originalPosition = transform.localPosition;
            
            Debug.Log($"🎭 CharacterAnimator 초기화: {name}");
        }
        
        /// <summary>
        /// Attack 모션 시작 (배치 시 한번 실행)
        /// </summary>
        public void PlayAttackAnimation()
        {
            if (isPlayingAttack) return;
            
            Debug.Log($"⚔️ Attack 애니메이션 시작: {name}");
            
            // 현재 진행 중인 idle 애니메이션 중지
            StopIdleAnimation();
            
            // Attack 애니메이션 시작
            if (currentAttackCoroutine != null)
                StopCoroutine(currentAttackCoroutine);
                
            currentAttackCoroutine = StartCoroutine(AttackAnimationCoroutine());
        }
        
        /// <summary>
        /// Idle 모션 시작 (반복 재생)
        /// </summary>
        public void PlayIdleAnimation()
        {
            if (isPlayingAttack) return;
            
            Debug.Log($"😴 Idle 애니메이션 시작: {name}");
            
            if (currentIdleCoroutine != null)
                StopCoroutine(currentIdleCoroutine);
                
            currentIdleCoroutine = StartCoroutine(IdleAnimationCoroutine());
        }
        
        /// <summary>
        /// Idle 애니메이션 중지
        /// </summary>
        public void StopIdleAnimation()
        {
            if (currentIdleCoroutine != null)
            {
                StopCoroutine(currentIdleCoroutine);
                currentIdleCoroutine = null;
            }
        }
        
        /// <summary>
        /// Attack 애니메이션 코루틴
        /// </summary>
        private IEnumerator AttackAnimationCoroutine()
        {
            isPlayingAttack = true;
            float elapsed = 0f;
            
            while (elapsed < attackAnimationDuration)
            {
                float progress = elapsed / attackAnimationDuration;
                
                // 스케일 애니메이션 (확대/축소)
                float scaleMultiplier = attackScaleCurve.Evaluate(progress);
                transform.localScale = originalScale * scaleMultiplier;
                
                // 회전 애니메이션
                float rotation = attackRotationCurve.Evaluate(progress);
                transform.localEulerAngles = originalRotation + Vector3.forward * rotation;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 원상 복구
            transform.localScale = originalScale;
            transform.localEulerAngles = originalRotation;
            transform.localPosition = originalPosition;
            
            isPlayingAttack = false;
            
            Debug.Log($"✅ Attack 애니메이션 완료: {name}");
            
            // Attack 완료 후 idle 애니메이션 시작
            PlayIdleAnimation();
        }
        
        /// <summary>
        /// Idle 애니메이션 코루틴 (반복)
        /// </summary>
        private IEnumerator IdleAnimationCoroutine()
        {
            while (!isPlayingAttack)
            {
                float elapsed = 0f;
                float cycleDuration = 2f / idleAnimationSpeed;
                
                while (elapsed < cycleDuration && !isPlayingAttack)
                {
                    float progress = elapsed / cycleDuration;
                    
                    // 부드러운 상하 움직임
                    float bounce = idleBounceCurve.Evaluate(Mathf.PingPong(progress * 2, 1));
                    Vector3 bounceOffset = Vector3.up * (bounce - 1) * 0.1f;
                    transform.localPosition = originalPosition + bounceOffset;
                    
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                
                // 한 사이클 완료 후 잠시 대기
                yield return new WaitForSeconds(0.5f);
            }
            
            // 위치 원상 복구
            transform.localPosition = originalPosition;
        }
        
        void OnDestroy()
        {
            // 코루틴 정리
            StopIdleAnimation();
            if (currentAttackCoroutine != null)
                StopCoroutine(currentAttackCoroutine);
        }
    }
}