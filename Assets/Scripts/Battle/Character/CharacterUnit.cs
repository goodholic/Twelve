using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using GuildMaster.Data;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 전투에 참여하는 캐릭터 유닛
    /// </summary>
    public class CharacterUnit : MonoBehaviour
    {
        [Header("캐릭터 정보")]
        [SerializeField] private string characterId;
        [SerializeField] private string characterName;
        [SerializeField] private JobClass jobClass;
        [SerializeField] private int level = 1;
        [SerializeField] private Rarity rarity = Rarity.Common;
        
        [Header("소속")]
        [SerializeField] private Tile.Team team = Tile.Team.Ally;
        public Tile.Team Team => team;
        
        [Header("스탯")]
        [SerializeField] private int maxHP = 100;
        [SerializeField] private int currentHP = 100;
        [SerializeField] private int attackPower = 10;
        [SerializeField] private int defense = 5;
        [SerializeField] private float criticalRate = 0.1f;
        [SerializeField] private float criticalDamage = 1.5f;
        
        [Header("공격 패턴")]
        [SerializeField] private AttackPattern attackPattern;
        public AttackPattern AttackPattern => attackPattern;
        
        [Header("현재 상태")]
        private Tile currentTile;
        public Tile CurrentTile 
        { 
            get => currentTile; 
            set => currentTile = value; 
        }
        
        private bool isPlaced = false;
        public bool IsPlaced => isPlaced;
        
        [Header("UI")]
        [SerializeField] private GameObject healthBarPrefab;
        private GameObject healthBar;
        private Slider healthSlider;
        
        [Header("비주얼")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        
        // 이벤트
        public System.Action<CharacterUnit, float> OnDamageTaken;
        public System.Action<CharacterUnit> OnDeath;
        public System.Action<CharacterUnit, CharacterUnit, float> OnAttack;
        
        void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
                
            if (animator == null)
                animator = GetComponent<Animator>();
                
            InitializeAttackPattern();
        }
        
        void Start()
        {
            CreateHealthBar();
            UpdateHealthBar();
        }
        
        /// <summary>
        /// 공격 패턴 초기화
        /// </summary>
        void InitializeAttackPattern()
        {
            if (attackPattern == null)
            {
                attackPattern = ScriptableObject.CreateInstance<AttackPattern>();
                
                // 직업별 기본 공격 패턴 설정
                switch (jobClass)
                {
                    case JobClass.Warrior:
                        attackPattern.Initialize(AttackPattern.PatternType.Cross, 1);
                        break;
                    case JobClass.Knight:
                        attackPattern.Initialize(AttackPattern.PatternType.Adjacent, 1);
                        break;
                    case JobClass.Wizard:
                        attackPattern.Initialize(AttackPattern.PatternType.Line, 3);
                        break;
                    case JobClass.Priest:
                        attackPattern.Initialize(AttackPattern.PatternType.Square, 2);
                        break;
                    case JobClass.Rogue:
                        attackPattern.Initialize(AttackPattern.PatternType.Diagonal, 2);
                        break;
                    case JobClass.Archer:
                        attackPattern.Initialize(AttackPattern.PatternType.Line, 4);
                        break;
                    case JobClass.Sage:
                        attackPattern.Initialize(AttackPattern.PatternType.Circle, 2);
                        break;
                    default:
                        attackPattern.Initialize(AttackPattern.PatternType.Single, 1);
                        break;
                }
            }
        }
        
        /// <summary>
        /// 캐릭터 데이터로 초기화
        /// </summary>
        public void Initialize(CharacterData data, Tile.Team team)
        {
            characterId = data.ID;
            characterName = data.Name;
            jobClass = data.jobClass;
            level = data.Level;
            rarity = data.rarity;
            
            maxHP = data.HP;
            currentHP = maxHP;
            attackPower = data.Attack;
            defense = data.Defense;
            criticalRate = data.CritRate;
            criticalDamage = data.CritDamage;
            
            this.team = team;
            
            gameObject.name = $"{team}_{characterName}";
            
            // 스프라이트 설정
            if (spriteRenderer != null && data.sprite != null)
            {
                spriteRenderer.sprite = data.sprite;
                
                // 적군인 경우 스프라이트 반전
                if (team == Tile.Team.Enemy)
                {
                    spriteRenderer.flipX = true;
                }
            }
            
            UpdateHealthBar();
        }
        
        /// <summary>
        /// 체력바 생성
        /// </summary>
        void CreateHealthBar()
        {
            if (healthBarPrefab != null)
            {
                healthBar = Instantiate(healthBarPrefab, transform);
                healthBar.transform.localPosition = Vector3.up * 1.5f;
                
                healthSlider = healthBar.GetComponentInChildren<Slider>();
            }
        }
        
        /// <summary>
        /// 체력바 업데이트
        /// </summary>
        void UpdateHealthBar()
        {
            if (healthSlider != null)
            {
                healthSlider.value = (float)currentHP / maxHP;
            }
        }
        
        /// <summary>
        /// 데미지 받기
        /// </summary>
        public void TakeDamage(float damage, CharacterUnit attacker = null)
        {
            // 방어력 계산
            float finalDamage = Mathf.Max(1, damage - defense);
            
            currentHP -= Mathf.RoundToInt(finalDamage);
            currentHP = Mathf.Max(0, currentHP);
            
            UpdateHealthBar();
            
            // 데미지 텍스트 표시
            ShowDamageText(finalDamage);
            
            // 피격 애니메이션
            if (animator != null)
            {
                animator.SetTrigger("Hit");
            }
            
            OnDamageTaken?.Invoke(this, finalDamage);
            
            if (currentHP <= 0)
            {
                Die();
            }
        }
        
        /// <summary>
        /// 데미지 텍스트 표시
        /// </summary>
        void ShowDamageText(float damage)
        {
            if (ParticleEffectsSystem.Instance != null)
            {
                Vector3 textPos = transform.position + Vector3.up * 2f;
                ParticleEffectsSystem.Instance.ShowDamageText(textPos, (int)damage, false);
            }
        }
        
        /// <summary>
        /// 공격하기
        /// </summary>
        public void Attack(CharacterUnit target)
        {
            if (target == null || !target.IsAlive()) return;
            
            // 크리티컬 판정
            bool isCritical = Random.value < criticalRate;
            float damage = attackPower;
            
            if (isCritical)
            {
                damage *= criticalDamage;
            }
            
            // 공격 애니메이션
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
            
            // 이펙트 재생
            if (ParticleEffectsSystem.Instance != null)
            {
                ParticleEffectsSystem.Instance.PlayEffectOnTarget("attack_hit", target.transform);
            }
            
            // 데미지 적용
            target.TakeDamage(damage, this);
            
            OnAttack?.Invoke(this, target, damage);
        }
        
        /// <summary>
        /// 사망 처리
        /// </summary>
        void Die()
        {
            isPlaced = false;
            
            // 타일에서 제거
            if (currentTile != null)
            {
                currentTile.RemoveUnit();
                currentTile = null;
            }
            
            // 사망 애니메이션
            if (animator != null)
            {
                animator.SetTrigger("Death");
            }
            
            OnDeath?.Invoke(this);
            
            // 일정 시간 후 제거
            Destroy(gameObject, 2f);
        }
        
        /// <summary>
        /// 생존 여부
        /// </summary>
        public bool IsAlive()
        {
            return currentHP > 0;
        }
        
        /// <summary>
        /// 타일에 배치
        /// </summary>
        public void PlaceOnTile(Tile tile)
        {
            if (tile == null || tile.isOccupied) return;
            
            currentTile = tile;
            isPlaced = true;
            transform.position = tile.GetWorldPosition();
        }
        
        /// <summary>
        /// 공격 가능 여부 확인
        /// </summary>
        public bool CanAttack(CharacterUnit target)
        {
            if (target == null || !target.IsAlive() || target.Team == team)
                return false;
            
            // 공격 범위 내에 있는지 확인
            List<Tile> attackRange = TileGridManager.Instance.GetAttackRangeTiles(this);
            return attackRange.Contains(target.CurrentTile);
        }
        
        /// <summary>
        /// 캐릭터 정보 문자열
        /// </summary>
        public string GetInfo()
        {
            return $"{characterName} Lv.{level} ({jobClass})\nHP: {currentHP}/{maxHP}\nATK: {attackPower} DEF: {defense}";
        }
    }
    
    
    /// <summary>
    /// 희귀도
    /// </summary>
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}