using UnityEngine;
using System.Collections.Generic;

namespace pjy.Characters.Behaviors
{
    /// <summary>
    /// 서포트 행동
    /// - 아군 힐링
    /// - 버프 제공
    /// - 디버프 해제
    /// </summary>
    public class SupportBehavior : CharacterBehaviorBase
    {
        [Header("서포트 설정")]
        [SerializeField] private float supportRange = 4f;
        [SerializeField] private float healAmount = 30f;
        [SerializeField] private float supportSpeed = 1f;
        [SerializeField] private LayerMask allyLayerMask = -1;
        
        [Header("힐링 설정")]
        [SerializeField] private bool canHeal = true;
        [SerializeField] private float healThreshold = 0.7f; // 70% 이하일 때 힐링
        
        [Header("버프 설정")]
        [SerializeField] private bool canBuff = true;
        [SerializeField] private float buffAmount = 0.2f; // 20% 증가
        [SerializeField] private float buffDuration = 10f;
        
        [Header("효과")]
        [SerializeField] private GameObject healEffectPrefab;
        [SerializeField] private GameObject buffEffectPrefab;
        [SerializeField] private AudioClip healSound;
        [SerializeField] private AudioClip buffSound;
        
        private Character currentTarget;
        private float lastSupportTime = 0f;
        private bool isSupporting = false;
        
        public override string BehaviorName => "서포트";
        
        protected override void OnInitialize()
        {
            // 캐릭터 데이터에서 설정 가져오기
            if (character != null)
            {
                supportRange = character.attackRange;
                healAmount = character.attackPower * 0.6f; // 공격력의 60%를 힐량으로
                supportSpeed = character.attackSpeed;
            }
        }
        
        protected override bool CheckExecuteConditions()
        {
            // 서포트 중이면 실행하지 않음
            if (isSupporting) return false;
            
            // 서포트 쿨다운 확인
            if (Time.time - lastSupportTime < (1f / supportSpeed)) return false;
            
            // 서포트할 타겟 찾기
            currentTarget = FindSupportTarget();
            return currentTarget != null;
        }
        
        protected override void OnExecute()
        {
            if (currentTarget == null) return;
            
            StartCoroutine(PerformSupport());
        }
        
        /// <summary>
        /// 서포트 실행 코루틴
        /// </summary>
        private System.Collections.IEnumerator PerformSupport()
        {
            isSupporting = true;
            lastSupportTime = Time.time;
            
            // 타겟 방향 바라보기
            if (currentTarget != null)
            {
                Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
                if (direction.x > 0)
                    transform.localScale = new Vector3(1, 1, 1);
                else if (direction.x < 0)
                    transform.localScale = new Vector3(-1, 1, 1);
            }
            
            // 시전 시간
            yield return new WaitForSeconds(0.3f);
            
            if (currentTarget != null)
            {
                // 힐링이 필요한지 확인
                if (canHeal && NeedsHealing(currentTarget))
                {
                    PerformHealing(currentTarget);
                }
                // 버프가 필요한지 확인
                else if (canBuff && NeedsBuff(currentTarget))
                {
                    PerformBuffing(currentTarget);
                }
            }
            
            // 서포트 후 딜레이
            yield return new WaitForSeconds(0.2f);
            
            isSupporting = false;
            currentTarget = null;
        }
        
        /// <summary>
        /// 힐링 실행
        /// </summary>
        private void PerformHealing(Character target)
        {
            if (target == null) return;
            
            // 힐링 적용
            float currentHP = target.currentHP;
            float maxHP = target.maxHP;
            float newHP = Mathf.Min(maxHP, currentHP + healAmount);
            
            target.currentHP = newHP;
            target.health = newHP;
            target.UpdateHPBar();
            
            // 힐링 효과
            if (healEffectPrefab != null)
            {
                GameObject effect = Instantiate(healEffectPrefab, target.transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            
            // 힐링 사운드
            if (healSound != null)
            {
                AudioSource.PlayClipAtPoint(healSound, target.transform.position);
            }
            
            if (showDebugLogs)
                Debug.Log($"[{BehaviorName}] {character.characterName}이(가) {target.characterName}을 {healAmount}만큼 힐링했습니다.");
        }
        
        /// <summary>
        /// 버프 적용
        /// </summary>
        private void PerformBuffing(Character target)
        {
            if (target == null) return;
            
            // 공격력 버프 적용
            target.ApplyDefenseBoost(buffAmount, buffDuration);
            
            // 버프 효과
            if (buffEffectPrefab != null)
            {
                GameObject effect = Instantiate(buffEffectPrefab, target.transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            
            // 버프 사운드
            if (buffSound != null)
            {
                AudioSource.PlayClipAtPoint(buffSound, target.transform.position);
            }
            
            if (showDebugLogs)
                Debug.Log($"[{BehaviorName}] {character.characterName}이(가) {target.characterName}에게 버프를 적용했습니다.");
        }
        
        /// <summary>
        /// 서포트할 타겟 찾기
        /// </summary>
        private Character FindSupportTarget()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, supportRange, allyLayerMask);
            List<Character> potentialTargets = new List<Character>();
            
            foreach (Collider col in colliders)
            {
                // 자기 자신은 제외
                if (col.gameObject == gameObject) continue;
                
                Character targetCharacter = col.GetComponent<Character>();
                if (targetCharacter == null) continue;
                
                // 같은 팀만 서포트
                if (character != null && !character.IsSameTeam(targetCharacter)) continue;
                
                potentialTargets.Add(targetCharacter);
            }
            
            // 우선순위에 따라 타겟 선택
            Character bestTarget = null;
            float bestPriority = float.MinValue;
            
            foreach (Character target in potentialTargets)
            {
                float priority = CalculateSupportPriority(target);
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestTarget = target;
                }
            }
            
            return bestTarget;
        }
        
        /// <summary>
        /// 서포트 우선순위 계산
        /// </summary>
        private float CalculateSupportPriority(Character target)
        {
            float priority = 0f;
            
            // HP가 낮을수록 높은 우선순위 (힐링)
            if (canHeal && NeedsHealing(target))
            {
                float hpRatio = target.currentHP / target.maxHP;
                priority += (1f - hpRatio) * 100f; // HP가 낮을수록 높은 점수
            }
            
            // 버프가 필요하면 추가 점수
            if (canBuff && NeedsBuff(target))
            {
                priority += 50f;
            }
            
            // 거리에 따른 보정 (가까울수록 높은 우선순위)
            float distance = Vector3.Distance(transform.position, target.transform.position);
            priority += (supportRange - distance) * 10f;
            
            return priority;
        }
        
        /// <summary>
        /// 힐링이 필요한지 확인
        /// </summary>
        private bool NeedsHealing(Character target)
        {
            if (!canHeal || target == null) return false;
            
            float hpRatio = target.currentHP / target.maxHP;
            return hpRatio < healThreshold;
        }
        
        /// <summary>
        /// 버프가 필요한지 확인
        /// </summary>
        private bool NeedsBuff(Character target)
        {
            if (!canBuff || target == null) return false;
            
            // 간단한 버프 필요성 검사 (실제로는 더 복잡한 로직 필요)
            // 현재는 HP가 만땅이고 전투 중인 캐릭터에게 버프 적용
            float hpRatio = target.currentHP / target.maxHP;
            return hpRatio > 0.8f; // HP가 80% 이상일 때 버프 적용
        }
        
        protected override void OnUpdateBehavior()
        {
            // 현재 타겟이 사거리를 벗어났는지 확인
            if (currentTarget != null)
            {
                float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
                if (distance > supportRange)
                {
                    currentTarget = null;
                }
            }
        }
        
        protected override void OnStopBehavior()
        {
            if (isSupporting)
            {
                StopAllCoroutines();
                isSupporting = false;
            }
            currentTarget = null;
        }
        
        // 기즈모로 서포트 범위 표시 (에디터에서만)
        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!isEnabled) return;
            
            Gizmos.color = new Color(0, 0, 1, 0.3f);
            Gizmos.DrawWireSphere(transform.position, supportRange);
            
            if (currentTarget != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            }
        }
        #endif
    }
}