using UnityEngine;

namespace pjy.Characters.Behaviors
{
    /// <summary>
    /// 근접 공격 행동
    /// - 가까운 적을 찾아 근접 공격
    /// - 사거리 내 타겟 탐지 및 공격
    /// </summary>
    public class MeleeAttackBehavior : CharacterBehaviorBase
    {
        [Header("근접 공격 설정")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackDamage = 50f;
        [SerializeField] private float attackSpeed = 1.5f;
        [SerializeField] private LayerMask targetLayerMask = -1;
        
        [Header("공격 효과")]
        [SerializeField] private GameObject attackEffectPrefab;
        [SerializeField] private AudioClip attackSound;
        
        private GameObject currentTarget;
        private float lastAttackTime = 0f;
        private bool isAttacking = false;
        
        public override string BehaviorName => "근접 공격";
        
        protected override void OnInitialize()
        {
            // 캐릭터 데이터에서 설정 가져오기
            if (character != null)
            {
                attackRange = character.attackRange;
                attackDamage = character.attackPower;
                attackSpeed = character.attackSpeed;
            }
        }
        
        protected override bool CheckExecuteConditions()
        {
            // 공격 중이면 실행하지 않음
            if (isAttacking) return false;
            
            // 공격 쿨다운 확인
            if (Time.time - lastAttackTime < (1f / attackSpeed)) return false;
            
            // 타겟 찾기
            currentTarget = FindNearestTarget();
            return currentTarget != null;
        }
        
        protected override void OnExecute()
        {
            if (currentTarget == null) return;
            
            StartCoroutine(PerformAttack());
        }
        
        /// <summary>
        /// 공격 실행 코루틴
        /// </summary>
        private System.Collections.IEnumerator PerformAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            
            // 타겟 방향 바라보기
            if (currentTarget != null)
            {
                Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
                if (direction.x > 0)
                    transform.localScale = new Vector3(1, 1, 1);
                else if (direction.x < 0)
                    transform.localScale = new Vector3(-1, 1, 1);
            }
            
            // 공격 애니메이션 시간
            float attackAnimationTime = 0.3f;
            
            // 공격 효과 생성
            if (attackEffectPrefab != null && currentTarget != null)
            {
                GameObject effect = Instantiate(attackEffectPrefab, currentTarget.transform.position, Quaternion.identity);
                Destroy(effect, 1f);
            }
            
            // 공격 사운드 재생
            if (attackSound != null)
            {
                AudioSource.PlayClipAtPoint(attackSound, transform.position);
            }
            
            // 데미지 적용
            yield return new WaitForSeconds(attackAnimationTime * 0.5f); // 공격 중간 지점에서 데미지 적용
            
            if (currentTarget != null)
            {
                IDamageable damageable = currentTarget.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(attackDamage);
                    
                    if (showDebugLogs)
                        Debug.Log($"[{BehaviorName}] {character.characterName}이(가) {currentTarget.name}에게 {attackDamage} 데미지를 입혔습니다.");
                }
            }
            
            // 공격 완료까지 대기
            yield return new WaitForSeconds(attackAnimationTime * 0.5f);
            
            isAttacking = false;
            currentTarget = null;
        }
        
        /// <summary>
        /// 가장 가까운 타겟 찾기
        /// </summary>
        private GameObject FindNearestTarget()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, attackRange, targetLayerMask);
            GameObject nearestTarget = null;
            float nearestDistance = float.MaxValue;
            
            foreach (Collider col in colliders)
            {
                // 자기 자신은 제외
                if (col.gameObject == gameObject) continue;
                
                // 같은 팀은 제외 (캐릭터끼리의 경우)
                Character targetCharacter = col.GetComponent<Character>();
                if (targetCharacter != null && character != null)
                {
                    if (character.IsSameTeam(targetCharacter)) continue;
                }
                
                // IDamageable이 있는지 확인
                IDamageable damageable = col.GetComponent<IDamageable>();
                if (damageable == null) continue;
                
                // 거리 계산
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = col.gameObject;
                }
            }
            
            return nearestTarget;
        }
        
        protected override void OnUpdateBehavior()
        {
            // 현재 타겟이 사거리를 벗어났는지 확인
            if (currentTarget != null)
            {
                float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
                if (distance > attackRange)
                {
                    currentTarget = null;
                }
            }
        }
        
        protected override void OnStopBehavior()
        {
            if (isAttacking)
            {
                StopAllCoroutines();
                isAttacking = false;
            }
            currentTarget = null;
        }
        
        // 기즈모로 공격 범위 표시 (에디터에서만)
        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!isEnabled) return;
            
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            }
        }
        #endif
    }
}