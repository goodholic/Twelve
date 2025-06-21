using UnityEngine;

namespace pjy.Characters.Behaviors
{
    /// <summary>
    /// 원거리 공격 행동
    /// - 원거리에서 투사체를 발사하여 공격
    /// - 총알 시스템과 연동
    /// </summary>
    public class RangedAttackBehavior : CharacterBehaviorBase
    {
        [Header("원거리 공격 설정")]
        [SerializeField] private float attackRange = 5f;
        [SerializeField] private float attackDamage = 40f;
        [SerializeField] private float attackSpeed = 2f;
        [SerializeField] private LayerMask targetLayerMask = -1;
        
        [Header("투사체 설정")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float bulletSpeed = 10f;
        [SerializeField] private float bulletLifetime = 3f;
        
        [Header("공격 효과")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private AudioClip shootSound;
        
        private GameObject currentTarget;
        private float lastAttackTime = 0f;
        private bool isAttacking = false;
        
        public override string BehaviorName => "원거리 공격";
        
        protected override void OnInitialize()
        {
            // 캐릭터 데이터에서 설정 가져오기
            if (character != null)
            {
                attackRange = character.attackRange;
                attackDamage = character.attackPower;
                attackSpeed = character.attackSpeed;
            }
            
            // Fire Point가 없으면 캐릭터 위치에 생성
            if (firePoint == null)
            {
                GameObject firePointObj = new GameObject("FirePoint");
                firePointObj.transform.SetParent(transform);
                firePointObj.transform.localPosition = new Vector3(0, 0.5f, 0);
                firePoint = firePointObj.transform;
            }
            
            // 기본 총알 프리팹이 없으면 생성
            if (bulletPrefab == null)
            {
                bulletPrefab = CreateDefaultBullet();
            }
        }
        
        protected override bool CheckExecuteConditions()
        {
            // 공격 중이면 실행하지 않음
            if (isAttacking) return false;
            
            // 공격 쿨다운 확인
            if (Time.time - lastAttackTime < (1f / attackSpeed)) return false;
            
            // 타겟 찾기
            currentTarget = FindBestTarget();
            return currentTarget != null;
        }
        
        protected override void OnExecute()
        {
            if (currentTarget == null) return;
            
            StartCoroutine(PerformRangedAttack());
        }
        
        /// <summary>
        /// 원거리 공격 실행 코루틴
        /// </summary>
        private System.Collections.IEnumerator PerformRangedAttack()
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
            
            // 조준 시간
            yield return new WaitForSeconds(0.1f);
            
            // 총알 발사
            if (currentTarget != null && bulletPrefab != null)
            {
                FireBullet();
            }
            
            // 발사 후 딜레이
            yield return new WaitForSeconds(0.2f);
            
            isAttacking = false;
        }
        
        /// <summary>
        /// 총알 발사
        /// </summary>
        private void FireBullet()
        {
            if (currentTarget == null || firePoint == null) return;
            
            // 총구 플래시 효과
            if (muzzleFlashPrefab != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
                Destroy(flash, 0.2f);
            }
            
            // 발사 사운드
            if (shootSound != null)
            {
                AudioSource.PlayClipAtPoint(shootSound, firePoint.position);
            }
            
            // 총알 생성
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            
            // 총알 설정
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                Vector3 direction = (currentTarget.transform.position - firePoint.position).normalized;
                bulletScript.Initialize(attackDamage, bulletSpeed, currentTarget, character.areaIndex, false);
                bulletScript.SetDirection(direction);
            }
            else
            {
                // Bullet 스크립트가 없으면 기본 이동
                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 direction = (currentTarget.transform.position - firePoint.position).normalized;
                    rb.linearVelocity = direction * bulletSpeed;
                }
                
                // 일정 시간 후 제거
                Destroy(bullet, bulletLifetime);
            }
            
            if (showDebugLogs)
                Debug.Log($"[{BehaviorName}] {character.characterName}이(가) {currentTarget.name}을 향해 총알을 발사했습니다.");
        }
        
        /// <summary>
        /// 최적의 타겟 찾기 (가장 가까운 적)
        /// </summary>
        private GameObject FindBestTarget()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, attackRange, targetLayerMask);
            GameObject bestTarget = null;
            float nearestDistance = float.MaxValue;
            
            foreach (Collider col in colliders)
            {
                // 자기 자신은 제외
                if (col.gameObject == gameObject) continue;
                
                // 같은 팀은 제외
                Character targetCharacter = col.GetComponent<Character>();
                if (targetCharacter != null && character != null)
                {
                    if (character.IsSameTeam(targetCharacter)) continue;
                }
                
                // IDamageable이 있는지 확인
                IDamageable damageable = col.GetComponent<IDamageable>();
                if (damageable == null) continue;
                
                // 시야 확인 (장애물에 가려지지 않았는지)
                if (!HasLineOfSight(col.transform.position)) continue;
                
                // 거리 계산
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    bestTarget = col.gameObject;
                }
            }
            
            return bestTarget;
        }
        
        /// <summary>
        /// 타겟까지의 시야 확인 (장애물 체크)
        /// </summary>
        private bool HasLineOfSight(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - firePoint.position).normalized;
            float distance = Vector3.Distance(firePoint.position, targetPosition);
            
            // 레이캐스트로 장애물 확인
            RaycastHit hit;
            if (Physics.Raycast(firePoint.position, direction, out hit, distance))
            {
                // 타겟에 직접 맞았으면 시야 확보
                if (hit.transform.position == targetPosition)
                    return true;
                
                // 장애물에 막혔으면 시야 차단
                return false;
            }
            
            return true; // 장애물이 없으면 시야 확보
        }
        
        /// <summary>
        /// 기본 총알 프리팹 생성
        /// </summary>
        private GameObject CreateDefaultBullet()
        {
            GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.transform.localScale = Vector3.one * 0.1f;
            bullet.name = "DefaultBullet";
            
            // 기본 머티리얼 설정
            Renderer renderer = bullet.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.yellow;
            }
            
            // 콜라이더를 트리거로 설정
            Collider collider = bullet.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
            
            // Rigidbody 추가
            Rigidbody rb = bullet.AddComponent<Rigidbody>();
            rb.useGravity = false;
            
            // Bullet 스크립트 추가 (있다면)
            if (System.Type.GetType("Bullet") != null)
            {
                bullet.AddComponent(System.Type.GetType("Bullet"));
            }
            
            return bullet;
        }
        
        protected override void OnUpdateBehavior()
        {
            // 현재 타겟이 사거리를 벗어났는지 확인
            if (currentTarget != null)
            {
                float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
                if (distance > attackRange || !HasLineOfSight(currentTarget.transform.position))
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
        
        // 기즈모로 공격 범위와 시야 표시 (에디터에서만)
        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!isEnabled) return;
            
            // 공격 범위
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 현재 타겟까지의 라인
            if (currentTarget != null && firePoint != null)
            {
                Gizmos.color = HasLineOfSight(currentTarget.transform.position) ? Color.green : Color.red;
                Gizmos.DrawLine(firePoint.position, currentTarget.transform.position);
            }
        }
        #endif
    }
}