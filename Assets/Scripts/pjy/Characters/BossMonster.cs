using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace pjy.Characters
{
    /// <summary>
    /// 보스 몬스터 시스템 - 특별한 패턴과 페이즈를 가진 강력한 몬스터
    /// </summary>
    public class BossMonster : Monster
    {
        [Header("보스 설정")]
        [SerializeField] private BossType bossType = BossType.ChapterBoss;
        [SerializeField] private int bossId = 1;
        [SerializeField] private string bossName = "챕터 보스";
        
        [Header("페이즈 시스템")]
        [SerializeField] private int currentPhase = 1;
        [SerializeField] private int maxPhases = 3;
        [SerializeField] private float[] phaseHealthThresholds = { 0.7f, 0.4f, 0f };
        [SerializeField] private bool isTransitioning = false;
        
        [Header("보스 스탯 배율")]
        [SerializeField] private float healthMultiplier = 5f;
        [SerializeField] private float damageMultiplier = 2f;
        [SerializeField] private float speedMultiplier = 0.8f;
        [SerializeField] private float rewardMultiplier = 3f;
        
        [Header("특수 패턴")]
        [SerializeField] private List<BossPattern> availablePatterns = new List<BossPattern>();
        [SerializeField] private float patternCooldown = 5f;
        [SerializeField] private float nextPatternTime = 0f;
        private BossPattern currentPattern;
        
        [Header("광역 공격")]
        [SerializeField] private bool hasAreaAttack = true;
        [SerializeField] private float areaAttackRadius = 3f;
        [SerializeField] private float areaAttackDamage = 50f;
        [SerializeField] private GameObject areaAttackEffect;
        
        [Header("소환 능력")]
        [SerializeField] private bool canSummonMinions = true;
        [SerializeField] private GameObject minionPrefab;
        [SerializeField] private int minionsPerSummon = 3;
        [SerializeField] private float summonCooldown = 10f;
        private float nextSummonTime = 0f;
        
        [Header("버프/디버프")]
        [SerializeField] private bool canBuffSelf = true;
        [SerializeField] private bool canDebuffEnemies = true;
        [SerializeField] private float buffDuration = 5f;
        [SerializeField] private float debuffRadius = 4f;
        
        [Header("UI")]
        [SerializeField] private GameObject bossHealthBarPrefab;
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private TextMeshProUGUI phaseText;
        private GameObject bossHealthBar;
        
        [Header("보상")]
        [SerializeField] private int baseGoldReward = 100;
        [SerializeField] private int baseDiamondReward = 10;
        [SerializeField] private List<RewardItem> specialRewards = new List<RewardItem>();
        
        // 보스 상태
        private bool isDead = false;
        private float originalSpeed;
        private float buffEndTime = 0f;
        private bool isBuffed = false;
        private List<Character> debuffedCharacters = new List<Character>();
        
        // 보스 전용 속성
        private float bossMaxHealth;
        private float bossAttackPower;
        private int bossGoldReward;
        public int chapterIndex = 1;
        
        // 페이즈별 능력 활성화
        private Dictionary<int, List<BossAbility>> phaseAbilities = new Dictionary<int, List<BossAbility>>();
        
        protected new void Awake()
        {
            // Monster.Awake()가 private이므로 직접 초기화
            InitializeBossStats();
            InitializePatterns();
            InitializePhaseAbilities();
        }
        
        protected new void Start()
        {
            // Monster.Start()가 private이므로 필요한 초기화만 수행
            CreateBossHealthBar();
            originalSpeed = moveSpeed;
        }
        
        /// <summary>
        /// 보스 스탯 초기화
        /// </summary>
        private void InitializeBossStats()
        {
            // 기본 몬스터 대비 강화된 스탯
            health *= healthMultiplier;
            moveSpeed *= speedMultiplier;
            
            // 보스 전용 속성들
            bossMaxHealth = health;
            bossAttackPower = damageToCastle * damageMultiplier;
            bossGoldReward = Mathf.RoundToInt(baseGoldReward * rewardMultiplier);
        }
        
        /// <summary>
        /// 보스 패턴 초기화
        /// </summary>
        private void InitializePatterns()
        {
            // 기본 패턴 추가
            availablePatterns.Add(new BossPattern
            {
                patternName = "광역 충격파",
                patternType = PatternType.AreaAttack,
                damage = areaAttackDamage,
                radius = areaAttackRadius,
                cooldown = 8f
            });
            
            availablePatterns.Add(new BossPattern
            {
                patternName = "미니언 소환",
                patternType = PatternType.SummonMinions,
                summonCount = minionsPerSummon,
                cooldown = summonCooldown
            });
            
            availablePatterns.Add(new BossPattern
            {
                patternName = "광폭화",
                patternType = PatternType.SelfBuff,
                buffMultiplier = 1.5f,
                duration = buffDuration,
                cooldown = 15f
            });
            
            availablePatterns.Add(new BossPattern
            {
                patternName = "약화의 포효",
                patternType = PatternType.DebuffArea,
                debuffMultiplier = 0.7f,
                radius = debuffRadius,
                duration = 5f,
                cooldown = 12f
            });
        }
        
        /// <summary>
        /// 페이즈별 능력 설정
        /// </summary>
        private void InitializePhaseAbilities()
        {
            // 페이즈 1: 기본 공격만
            phaseAbilities[1] = new List<BossAbility> { BossAbility.BasicAttack };
            
            // 페이즈 2: 광역 공격, 미니언 소환 추가
            phaseAbilities[2] = new List<BossAbility> 
            { 
                BossAbility.BasicAttack, 
                BossAbility.AreaAttack, 
                BossAbility.SummonMinions 
            };
            
            // 페이즈 3: 모든 능력 활성화
            phaseAbilities[3] = new List<BossAbility> 
            { 
                BossAbility.BasicAttack, 
                BossAbility.AreaAttack, 
                BossAbility.SummonMinions,
                BossAbility.SelfBuff,
                BossAbility.DebuffEnemies
            };
        }
        
        protected new void Update()
        {
            // Monster.Update()가 private이므로 필요한 업데이트만 수행
            
            // 페이즈 체크
            CheckPhaseTransition();
            
            // 패턴 실행
            if (Time.time >= nextPatternTime && !isTransitioning)
            {
                ExecutePattern();
            }
            
            // 버프 종료 체크
            if (isBuffed && Time.time >= buffEndTime)
            {
                RemoveBuff();
            }
            
            // 보스 UI 업데이트
            UpdateBossUI();
        }
        
        /// <summary>
        /// 페이즈 전환 체크
        /// </summary>
        private void CheckPhaseTransition()
        {
            float healthRatio = health / bossMaxHealth;
            
            for (int i = 0; i < phaseHealthThresholds.Length; i++)
            {
                if (healthRatio <= phaseHealthThresholds[i] && currentPhase == i + 1 && currentPhase < maxPhases)
                {
                    StartCoroutine(TransitionToPhase(currentPhase + 1));
                    break;
                }
            }
        }
        
        /// <summary>
        /// 페이즈 전환
        /// </summary>
        private IEnumerator TransitionToPhase(int newPhase)
        {
            isTransitioning = true;
            
            // 전환 이펙트
            Debug.Log($"[BossMonster] {bossName} 페이즈 {newPhase} 전환!");
            
            // 무적 상태
            float originalHealth = health;
            
            // 전환 애니메이션 (1초)
            yield return new WaitForSeconds(1f);
            
            currentPhase = newPhase;
            
            // 페이즈별 강화
            switch (newPhase)
            {
                case 2:
                    bossAttackPower *= 1.2f;
                    moveSpeed *= 1.1f;
                    break;
                case 3:
                    bossAttackPower *= 1.5f;
                    moveSpeed *= 1.2f;
                    patternCooldown *= 0.8f;
                    break;
            }
            
            // 페이즈 전환 시 체력 회복 (10%)
            health = Mathf.Min(health + bossMaxHealth * 0.1f, bossMaxHealth);
            
            // 특수 패턴 즉시 실행
            ExecutePhaseTransitionPattern();
            
            isTransitioning = false;
        }
        
        /// <summary>
        /// 페이즈 전환 특수 패턴
        /// </summary>
        private void ExecutePhaseTransitionPattern()
        {
            switch (currentPhase)
            {
                case 2:
                    // 페이즈 2 진입: 광역 충격파
                    ExecuteAreaAttack(areaAttackRadius * 1.5f, areaAttackDamage * 1.5f);
                    break;
                case 3:
                    // 페이즈 3 진입: 대량 미니언 소환
                    SummonMinions(minionsPerSummon * 2);
                    ApplySelfBuff(2f, buffDuration * 1.5f);
                    break;
            }
        }
        
        /// <summary>
        /// 패턴 실행
        /// </summary>
        private void ExecutePattern()
        {
            // 현재 페이즈에서 사용 가능한 패턴 필터링
            var validPatterns = availablePatterns.FindAll(p => 
                IsPatternAvailableInPhase(p.patternType, currentPhase));
            
            if (validPatterns.Count == 0) return;
            
            // 랜덤 패턴 선택
            currentPattern = validPatterns[Random.Range(0, validPatterns.Count)];
            
            Debug.Log($"[BossMonster] {bossName} 패턴 실행: {currentPattern.patternName}");
            
            switch (currentPattern.patternType)
            {
                case PatternType.AreaAttack:
                    ExecuteAreaAttack(currentPattern.radius, currentPattern.damage);
                    break;
                    
                case PatternType.SummonMinions:
                    SummonMinions(currentPattern.summonCount);
                    break;
                    
                case PatternType.SelfBuff:
                    ApplySelfBuff(currentPattern.buffMultiplier, currentPattern.duration);
                    break;
                    
                case PatternType.DebuffArea:
                    ApplyAreaDebuff(currentPattern.radius, currentPattern.debuffMultiplier, currentPattern.duration);
                    break;
            }
            
            nextPatternTime = Time.time + currentPattern.cooldown;
        }
        
        /// <summary>
        /// 패턴이 현재 페이즈에서 사용 가능한지 확인
        /// </summary>
        private bool IsPatternAvailableInPhase(PatternType patternType, int phase)
        {
            switch (patternType)
            {
                case PatternType.AreaAttack:
                    return phase >= 2 && hasAreaAttack;
                case PatternType.SummonMinions:
                    return phase >= 2 && canSummonMinions;
                case PatternType.SelfBuff:
                    return phase >= 3 && canBuffSelf;
                case PatternType.DebuffArea:
                    return phase >= 3 && canDebuffEnemies;
                default:
                    return true;
            }
        }
        
        /// <summary>
        /// 광역 공격 실행
        /// </summary>
        private void ExecuteAreaAttack(float radius, float damage)
        {
            // 이펙트 생성
            if (areaAttackEffect != null)
            {
                GameObject effect = Instantiate(areaAttackEffect, transform.position, Quaternion.identity);
                effect.transform.localScale = Vector3.one * radius * 2f;
                Destroy(effect, 2f);
            }
            
            // 범위 내 적 탐색
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius);
            
            foreach (var collider in colliders)
            {
                Character character = collider.GetComponent<Character>();
                if (character != null && character.areaIndex != areaIndex)
                {
                    character.TakeDamage(damage);
                    
                    // 넉백 효과
                    Vector2 knockbackDir = (collider.transform.position - transform.position).normalized;
                    collider.GetComponent<Rigidbody2D>()?.AddForce(knockbackDir * 500f);
                }
            }
        }
        
        /// <summary>
        /// 미니언 소환
        /// </summary>
        private void SummonMinions(int count)
        {
            if (minionPrefab == null || Time.time < nextSummonTime) return;
            
            for (int i = 0; i < count; i++)
            {
                // 보스 주변 랜덤 위치
                Vector2 randomOffset = Random.insideUnitCircle * 2f;
                Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);
                
                GameObject minion = Instantiate(minionPrefab, spawnPos, Quaternion.identity);
                
                // 미니언 설정
                Monster minionMonster = minion.GetComponent<Monster>();
                if (minionMonster != null)
                {
                    minionMonster.currentChapter = chapterIndex;
                    minionMonster.areaIndex = areaIndex;
                    minionMonster.SetWaypoints(pathWaypoints);
                    
                    // 미니언은 보스보다 약함
                    minionMonster.health *= 0.3f;
                    minionMonster.damageToCastle = Mathf.RoundToInt(bossAttackPower * 0.5f);
                }
            }
            
            nextSummonTime = Time.time + summonCooldown;
        }
        
        /// <summary>
        /// 자가 버프 적용
        /// </summary>
        private void ApplySelfBuff(float multiplier, float duration)
        {
            if (isBuffed) return;
            
            isBuffed = true;
            buffEndTime = Time.time + duration;
            
            // 스탯 증가
            bossAttackPower *= multiplier;
            moveSpeed *= multiplier;
            
            // 시각 효과
            SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.color = new Color(1f, 0.7f, 0.7f); // 붉은색
            }
            
            Debug.Log($"[BossMonster] {bossName} 광폭화! 공격력 {multiplier}배 증가");
        }
        
        /// <summary>
        /// 버프 제거
        /// </summary>
        private void RemoveBuff()
        {
            isBuffed = false;
            
            // 스탯 원상복구
            InitializeBossStats(); // 재계산
            
            // 시각 효과 제거
            SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.color = Color.white;
            }
        }
        
        /// <summary>
        /// 범위 디버프 적용
        /// </summary>
        private void ApplyAreaDebuff(float radius, float debuffMultiplier, float duration)
        {
            // 범위 내 적 캐릭터 탐색
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius);
            
            foreach (var collider in colliders)
            {
                Character character = collider.GetComponent<Character>();
                if (character != null && character.areaIndex != areaIndex)
                {
                    // 디버프 적용
                    StartCoroutine(ApplyDebuffToCharacter(character, debuffMultiplier, duration));
                    debuffedCharacters.Add(character);
                }
            }
        }
        
        /// <summary>
        /// 캐릭터에 디버프 적용
        /// </summary>
        private IEnumerator ApplyDebuffToCharacter(Character character, float multiplier, float duration)
        {
            if (character == null) yield break;
            
            // 원래 스탯 저장
            float originalAttack = character.attackPower;
            float originalSpeed = character.moveSpeed;
            
            // 디버프 적용
            character.attackPower *= multiplier;
            character.moveSpeed *= multiplier;
            
            // 시각 효과
            SpriteRenderer charSprite = character.GetComponentInChildren<SpriteRenderer>();
            if (charSprite != null)
            {
                charSprite.color = new Color(0.7f, 0.7f, 1f); // 파란색
            }
            
            yield return new WaitForSeconds(duration);
            
            // 디버프 제거
            if (character != null)
            {
                character.attackPower = originalAttack;
                character.moveSpeed = originalSpeed;
                
                if (charSprite != null)
                {
                    charSprite.color = Color.white;
                }
                
                debuffedCharacters.Remove(character);
            }
        }
        
        /// <summary>
        /// 보스 체력바 생성
        /// </summary>
        private void CreateBossHealthBar()
        {
            if (bossHealthBarPrefab != null)
            {
                // 화면 상단에 보스 체력바 생성
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    bossHealthBar = Instantiate(bossHealthBarPrefab, canvas.transform);
                    
                    // 보스 이름 설정
                    bossNameText = bossHealthBar.GetComponentInChildren<TextMeshProUGUI>();
                    if (bossNameText != null)
                    {
                        bossNameText.text = bossName;
                    }
                }
            }
        }
        
        /// <summary>
        /// 보스 UI 업데이트
        /// </summary>
        private void UpdateBossUI()
        {
            if (bossHealthBar == null) return;
            
            // 체력바 업데이트
            UnityEngine.UI.Slider healthSlider = bossHealthBar.GetComponentInChildren<UnityEngine.UI.Slider>();
            if (healthSlider != null)
            {
                healthSlider.value = health / bossMaxHealth;
            }
            
            // 페이즈 텍스트 업데이트
            if (phaseText != null)
            {
                phaseText.text = $"페이즈 {currentPhase}";
            }
        }
        
        /// <summary>
        /// 데미지 받기 오버라이드
        /// </summary>
        public new void TakeDamage(float damage)
        {
            if (isTransitioning) return; // 페이즈 전환 중에는 무적
            
            base.TakeDamage(damage);
            
            // 보스 피격 이펙트
            StartCoroutine(HitEffect());
        }
        
        /// <summary>
        /// 피격 이펙트
        /// </summary>
        private IEnumerator HitEffect()
        {
            SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null)
            {
                Color originalColor = sprite.color;
                sprite.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                sprite.color = originalColor;
            }
        }
        
        /// <summary>
        /// 보스 사망 처리
        /// </summary>
        protected new void Die()
        {
            if (isDead) return;
            isDead = true;
            
            Debug.Log($"[BossMonster] {bossName} 처치!");
            
            // 보상 지급
            GiveRewards();
            
            // 사망 이펙트
            StartCoroutine(DeathSequence());
        }
        
        /// <summary>
        /// 보상 지급
        /// </summary>
        private void GiveRewards()
        {
            // 골드 보상
            PlayerController player = GetTargetPlayer();
            if (player != null)
            {
                player.AddMinerals(baseGoldReward);
                
                // 다이아몬드 보상
                // player.AddDiamonds(baseDiamondReward); // 다이아몬드 시스템 구현 시 활성화
                
                // 특별 보상
                foreach (var reward in specialRewards)
                {
                    Debug.Log($"[BossMonster] 특별 보상 획득: {reward.itemName} x{reward.quantity}");
                    // 실제 보상 지급 로직 추가
                }
            }
        }
        
        /// <summary>
        /// 대상 플레이어 찾기
        /// </summary>
        private PlayerController GetTargetPlayer()
        {
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if ((areaIndex == 1 && player.PlayerID == 1) ||
                    (areaIndex == 2 && player.PlayerID == 2))
                {
                    return player;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 사망 시퀀스
        /// </summary>
        private IEnumerator DeathSequence()
        {
            // 사망 애니메이션
            float deathTime = 2f;
            float elapsed = 0f;
            
            while (elapsed < deathTime)
            {
                elapsed += Time.deltaTime;
                
                // 페이드 아웃
                SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
                if (sprite != null)
                {
                    Color color = sprite.color;
                    color.a = 1f - (elapsed / deathTime);
                    sprite.color = color;
                }
                
                // 크기 감소
                transform.localScale = Vector3.one * (1f - (elapsed / deathTime) * 0.5f);
                
                yield return null;
            }
            
            // UI 제거
            if (bossHealthBar != null)
            {
                Destroy(bossHealthBar);
            }
            
            // 오브젝트 제거
            Destroy(gameObject);
        }
        
        // 디버그용
        private void OnDrawGizmosSelected()
        {
            // 광역 공격 범위
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, areaAttackRadius);
            
            // 디버프 범위
            Gizmos.color = new Color(0f, 0f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, debuffRadius);
        }
    }
    
    // 데이터 구조체들
    public enum BossType
    {
        ChapterBoss,    // 챕터 보스
        EventBoss,      // 이벤트 보스
        RaidBoss        // 레이드 보스
    }
    
    public enum PatternType
    {
        AreaAttack,     // 광역 공격
        SummonMinions,  // 미니언 소환
        SelfBuff,       // 자가 버프
        DebuffArea,     // 범위 디버프
        Teleport,       // 순간이동
        Charge          // 돌진
    }
    
    public enum BossAbility
    {
        BasicAttack,
        AreaAttack,
        SummonMinions,
        SelfBuff,
        DebuffEnemies
    }
    
    [System.Serializable]
    public class BossPattern
    {
        public string patternName;
        public PatternType patternType;
        public float cooldown = 5f;
        public float damage;
        public float radius;
        public int summonCount;
        public float buffMultiplier;
        public float debuffMultiplier;
        public float duration;
    }
    
    [System.Serializable]
    public class RewardItem
    {
        public string itemName;
        public int itemId;
        public int quantity;
        public float dropChance = 1f;
    }
}