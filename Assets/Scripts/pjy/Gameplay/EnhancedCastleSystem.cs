using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace pjy.Gameplay
{
    /// <summary>
    /// 향상된 성 시스템 - 게임 기획서 요구사항
    /// 중간성 3개(각 500HP), 최종성 1개(1000HP)
    /// 성이 공격 능력을 가지며, 체력 관리와 연동
    /// </summary>
    public class EnhancedCastleSystem : MonoBehaviour
    {
        [Header("성 기본 설정")]
        [SerializeField] private CastleType castleType = CastleType.Middle;
        [SerializeField] private RouteType routeType = RouteType.Center;
        [SerializeField] private int areaIndex = 1; // 1: Player, 2: AI
        [SerializeField] private bool isPlayerCastle = true;
        
        [Header("체력 설정")]
        [SerializeField] private float maxHealth = 500f;
        [SerializeField] private float currentHealth = 500f;
        
        [Header("공격 설정")]
        [SerializeField] private float attackPower = 30f;
        [SerializeField] private float attackRange = 6f;
        [SerializeField] private float attackSpeed = 1f;
        [SerializeField] private LayerMask targetLayer;
        
        [Header("특수 능력")]
        [SerializeField] private bool hasDefenseBoost = true;
        [SerializeField] private float defenseBoostRadius = 4f;
        [SerializeField] private float defenseBoostAmount = 0.2f; // 20% 방어력 증가
        
        [Header("범위 공격")]
        [SerializeField] private bool hasAreaAttack = false;
        [SerializeField] private float areaAttackRadius = 3f;
        [SerializeField] private float areaAttackCooldown = 10f;
        [SerializeField] private float areaAttackDamageMultiplier = 0.5f;
        
        [Header("UI 연동")]
        [SerializeField] private GameObject healthBarPrefab;
        [SerializeField] private Transform healthBarPosition;
        [SerializeField] private TextMeshProUGUI healthText;
        
        [Header("시각 효과")]
        [SerializeField] private GameObject normalAttackEffect;
        [SerializeField] private GameObject areaAttackEffect;
        [SerializeField] private GameObject defenseBoostEffect;
        [SerializeField] private GameObject destroyEffect;
        
        [Header("오디오")]
        [SerializeField] private AudioClip attackSound;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip destroySound;
        
        // 런타임 변수
        private float nextAttackTime;
        private float nextAreaAttackTime;
        private List<GameObject> currentTargets = new List<GameObject>();
        private bool isDestroyed = false;
        private AudioSource audioSource;
        private GameObject currentHealthBar;
        private CastleHealthManager castleHealthManager;
        
        public enum CastleType
        {
            Middle,
            Final
        }
        
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // CastleHealthManager 찾기
            castleHealthManager = CastleHealthManager.Instance;
        }
        
        private void Start()
        {
            InitializeCastle();
            CreateHealthBar();
            
            // 최종성은 범위 공격 가능
            if (castleType == CastleType.Final)
            {
                hasAreaAttack = true;
                hasDefenseBoost = true;
            }
            
            // 방어 버프 효과 시작
            if (hasDefenseBoost)
            {
                StartDefenseBoost();
            }
        }
        
        private void InitializeCastle()
        {
            // 성 타입에 따른 초기화
            if (castleType == CastleType.Final)
            {
                maxHealth = 1000f;
                currentHealth = maxHealth;
                attackPower = 50f;
                attackRange = 8f;
                areaAttackRadius = 4f;
                defenseBoostRadius = 5f;
            }
            else
            {
                maxHealth = 500f;
                currentHealth = maxHealth;
                attackPower = 30f;
                attackRange = 6f;
                areaAttackRadius = 3f;
                defenseBoostRadius = 4f;
            }
            
            // CastleHealthManager와 동기화
            SyncWithHealthManager();
        }
        
        private void Update()
        {
            if (isDestroyed) return;
            
            // 타겟 업데이트
            UpdateTargets();
            
            // 일반 공격
            if (Time.time >= nextAttackTime && currentTargets.Count > 0)
            {
                PerformAttack();
                nextAttackTime = Time.time + (1f / attackSpeed);
            }
            
            // 범위 공격
            if (hasAreaAttack && Time.time >= nextAreaAttackTime)
            {
                if (CheckAreaAttackCondition())
                {
                    PerformAreaAttack();
                    nextAreaAttackTime = Time.time + areaAttackCooldown;
                }
            }
        }
        
        private void UpdateTargets()
        {
            currentTargets.Clear();
            
            // 범위 내 적 찾기
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, attackRange);
            
            foreach (var collider in colliders)
            {
                if (collider == null) continue;
                
                // 몬스터 체크
                Monster monster = collider.GetComponent<Monster>();
                if (monster != null && monster.areaIndex != areaIndex)
                {
                    currentTargets.Add(monster.gameObject);
                    continue;
                }
                
                // 적 캐릭터 체크
                Character character = collider.GetComponent<Character>();
                if (character != null && character.areaIndex != areaIndex)
                {
                    currentTargets.Add(character.gameObject);
                }
            }
            
            // 거리순 정렬
            currentTargets = currentTargets.OrderBy(t => 
                Vector2.Distance(transform.position, t.transform.position)
            ).ToList();
        }
        
        private void PerformAttack()
        {
            if (currentTargets.Count == 0) return;
            
            GameObject target = currentTargets[0];
            if (target == null) return;
            
            // 데미지 적용
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackPower);
                
                // 효과 표시
                ShowAttackEffect(target.transform.position);
                PlaySound(attackSound);
                
                Debug.Log($"[EnhancedCastle] {name}이(가) {target.name}을(를) 공격! 데미지: {attackPower}");
            }
        }
        
        private bool CheckAreaAttackCondition()
        {
            // 3개 이상의 적이 가까이 있을 때
            int nearbyEnemies = 0;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, areaAttackRadius);
            
            foreach (var collider in colliders)
            {
                if (IsEnemy(collider.gameObject))
                {
                    nearbyEnemies++;
                }
            }
            
            return nearbyEnemies >= 3;
        }
        
        private void PerformAreaAttack()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, areaAttackRadius);
            float areaDamage = attackPower * areaAttackDamageMultiplier;
            
            foreach (var collider in colliders)
            {
                if (IsEnemy(collider.gameObject))
                {
                    IDamageable damageable = collider.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(areaDamage);
                    }
                }
            }
            
            // 범위 공격 효과
            ShowAreaAttackEffect();
            PlaySound(attackSound);
            
            Debug.Log($"[EnhancedCastle] {name} 범위 공격 발동! 데미지: {areaDamage}");
        }
        
        private bool IsEnemy(GameObject obj)
        {
            Monster monster = obj.GetComponent<Monster>();
            if (monster != null && monster.areaIndex != areaIndex)
                return true;
                
            Character character = obj.GetComponent<Character>();
            if (character != null && character.areaIndex != areaIndex)
                return true;
                
            return false;
        }
        
        private void StartDefenseBoost()
        {
            if (defenseBoostEffect != null)
            {
                GameObject effect = Instantiate(defenseBoostEffect, transform.position, Quaternion.identity);
                effect.transform.SetParent(transform);
                effect.transform.localScale = Vector3.one * defenseBoostRadius * 2f;
            }
            
            // 주기적으로 방어 버프 적용
            InvokeRepeating(nameof(ApplyDefenseBoost), 0f, 1f);
        }
        
        private void ApplyDefenseBoost()
        {
            if (isDestroyed) return;
            
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, defenseBoostRadius);
            
            foreach (var collider in colliders)
            {
                Character character = collider.GetComponent<Character>();
                if (character != null && character.areaIndex == areaIndex)
                {
                    // 방어력 버프 적용 (캐릭터에 버프 시스템이 있다고 가정)
                    character.ApplyDefenseBoost(defenseBoostAmount, 1.5f); // 1.5초 지속
                }
            }
        }
        
        public void TakeDamage(float damage)
        {
            if (isDestroyed) return;
            
            currentHealth -= damage;
            if (currentHealth < 0) currentHealth = 0;
            
            // UI 업데이트
            UpdateHealthBar();
            
            // CastleHealthManager와 동기화
            UpdateHealthManager(damage);
            
            // 효과
            PlaySound(hitSound);
            
            Debug.Log($"[EnhancedCastle] {name} 피해 받음: {damage}, 남은 체력: {currentHealth}/{maxHealth}");
            
            if (currentHealth <= 0)
            {
                OnCastleDestroyed();
            }
        }
        
        private void UpdateHealthManager(float damage)
        {
            if (castleHealthManager == null) return;
            
            if (castleType == CastleType.Final)
            {
                castleHealthManager.TakeDamageToFinalCastle((int)damage);
            }
            else
            {
                castleHealthManager.TakeDamageToMidCastle(routeType, (int)damage);
            }
        }
        
        private void SyncWithHealthManager()
        {
            if (castleHealthManager == null) return;
            
            if (castleType == CastleType.Final)
            {
                currentHealth = castleHealthManager.finalCastleCurrentHealth;
            }
            else
            {
                switch (routeType)
                {
                    case RouteType.Left:
                        currentHealth = castleHealthManager.leftMidCastleHealth;
                        break;
                    case RouteType.Center:
                        currentHealth = castleHealthManager.centerMidCastleHealth;
                        break;
                    case RouteType.Right:
                        currentHealth = castleHealthManager.rightMidCastleHealth;
                        break;
                }
            }
        }
        
        private void OnCastleDestroyed()
        {
            isDestroyed = true;
            
            // 파괴 효과
            if (destroyEffect != null)
            {
                Instantiate(destroyEffect, transform.position, Quaternion.identity);
            }
            
            PlaySound(destroySound);
            
            // 성 비활성화 (파괴 표시)
            GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            
            // 공격 중지
            CancelInvoke();
            
            Debug.Log($"[EnhancedCastle] {name} 파괴됨!");
            
            // 게임 오버 체크는 CastleHealthManager에서 처리
        }
        
        private void CreateHealthBar()
        {
            if (healthBarPrefab != null && healthBarPosition != null)
            {
                currentHealthBar = Instantiate(healthBarPrefab, healthBarPosition.position, Quaternion.identity);
                currentHealthBar.transform.SetParent(transform);
                UpdateHealthBar();
            }
        }
        
        private void UpdateHealthBar()
        {
            if (currentHealthBar != null)
            {
                var slider = currentHealthBar.GetComponentInChildren<UnityEngine.UI.Slider>();
                if (slider != null)
                {
                    slider.value = currentHealth / maxHealth;
                }
            }
            
            if (healthText != null)
            {
                healthText.text = $"{(int)currentHealth}/{(int)maxHealth}";
            }
        }
        
        private void ShowAttackEffect(Vector3 targetPos)
        {
            if (normalAttackEffect != null)
            {
                Vector3 effectPos = Vector3.Lerp(transform.position, targetPos, 0.7f);
                GameObject effect = Instantiate(normalAttackEffect, effectPos, Quaternion.identity);
                Destroy(effect, 1f);
            }
        }
        
        private void ShowAreaAttackEffect()
        {
            if (areaAttackEffect != null)
            {
                GameObject effect = Instantiate(areaAttackEffect, transform.position, Quaternion.identity);
                effect.transform.localScale = Vector3.one * areaAttackRadius * 2f;
                Destroy(effect, 2f);
            }
        }
        
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        // 공개 메서드
        public bool IsDestroyed() => isDestroyed;
        public float GetHealthPercentage() => currentHealth / maxHealth;
        public CastleType GetCastleType() => castleType;
        public RouteType GetRouteType() => routeType;
        
        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 공격 범위
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 범위 공격 범위
            if (hasAreaAttack)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, areaAttackRadius);
            }
            
            // 방어 버프 범위
            if (hasDefenseBoost)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, defenseBoostRadius);
            }
        }
        #endif
    }
}