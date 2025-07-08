using UnityEngine;
using TMPro;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 전투에 참여하는 개별 유닛
    /// </summary>
    public class Unit : MonoBehaviour
    {
        [Header("Unit Info")]
        public string unitName;
        public JobClass jobClass;
        public bool isPlayerUnit;
        
        [Header("Current Position")]
        public string currentBoard; // "A" or "B"
        public Vector2Int currentTile;
        
        [Header("Stats")]
        public float maxHP;
        public float currentHP;
        public float attack;
        public float defense;
        public float magicPower;
        public float speed;
        public float critRate;
        public float critDamage;
        
        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private GameObject healthBar;
        [SerializeField] private Transform healthBarFill;
        
        private Character sourceCharacter;
        
        public void Initialize(Character character, bool isPlayer)
        {
            sourceCharacter = character;
            isPlayerUnit = isPlayer;
            
            // 기본 스탯 설정
            unitName = character.characterName;
            jobClass = character.jobClass;
            
            // 직업별 스탯 배율 적용
            var jobStats = JobClassSystem.GetJobStatMultipliers(jobClass);
            
            maxHP = character.baseHP * jobStats.hpMultiplier;
            currentHP = maxHP;
            attack = character.baseAttack * jobStats.attackMultiplier;
            defense = character.baseDefense * jobStats.defenseMultiplier;
            magicPower = character.baseMagicPower * jobStats.magicPowerMultiplier;
            speed = character.baseSpeed * jobStats.speedMultiplier;
            critRate = character.baseCritRate + jobStats.criticalRateBonus;
            critDamage = character.baseCritDamage;
            
            // 비주얼 설정
            SetupVisuals();
        }
        
        private void SetupVisuals()
        {
            // 스프라이트 설정
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (spriteRenderer == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            
            // 직업별 기본 색상 (실제로는 스프라이트 사용)
            Color unitColor = GetJobColor();
            spriteRenderer.color = unitColor;
            
            // 플레이어/적 구분
            if (!isPlayerUnit)
            {
                spriteRenderer.color = new Color(
                    spriteRenderer.color.r * 0.8f,
                    spriteRenderer.color.g * 0.8f,
                    spriteRenderer.color.b * 0.8f
                );
            }
            
            // 이름 텍스트 설정
            if (nameText == null)
            {
                GameObject textObj = new GameObject("NameText");
                textObj.transform.parent = transform;
                textObj.transform.localPosition = new Vector3(0, 0.8f, 0);
                nameText = textObj.AddComponent<TextMeshPro>();
            }
            
            nameText.text = unitName;
            nameText.fontSize = 2;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = isPlayerUnit ? Color.blue : Color.red;
            
            // 체력바 설정
            CreateHealthBar();
        }
        
        private Color GetJobColor()
        {
            switch (jobClass)
            {
                case JobClass.Warrior: return new Color(0.8f, 0.2f, 0.2f); // 빨강
                case JobClass.Knight: return new Color(0.6f, 0.6f, 0.6f); // 회색
                case JobClass.Wizard: return new Color(0.2f, 0.2f, 0.8f); // 파랑
                case JobClass.Priest: return new Color(1f, 1f, 0.6f); // 노랑
                case JobClass.Rogue: return new Color(0.5f, 0.2f, 0.5f); // 보라
                case JobClass.Sage: return new Color(0.2f, 0.8f, 0.8f); // 청록
                case JobClass.Archer: return new Color(0.2f, 0.6f, 0.2f); // 초록
                case JobClass.Gunner: return new Color(0.4f, 0.4f, 0.4f); // 진회색
                default: return Color.white;
            }
        }
        
        private void CreateHealthBar()
        {
            // 체력바 배경
            if (healthBar == null)
            {
                healthBar = new GameObject("HealthBar");
                healthBar.transform.parent = transform;
                healthBar.transform.localPosition = new Vector3(0, -0.6f, 0);
                
                SpriteRenderer bgRenderer = healthBar.AddComponent<SpriteRenderer>();
                bgRenderer.color = Color.black;
                bgRenderer.sortingOrder = 1;
                
                // 체력바 채움
                GameObject fillObj = new GameObject("HealthBarFill");
                fillObj.transform.parent = healthBar.transform;
                fillObj.transform.localPosition = Vector3.zero;
                healthBarFill = fillObj.transform;
                
                SpriteRenderer fillRenderer = fillObj.AddComponent<SpriteRenderer>();
                fillRenderer.color = isPlayerUnit ? Color.green : Color.red;
                fillRenderer.sortingOrder = 2;
            }
            
            UpdateHealthBar();
        }
        
        public void TakeDamage(float damage)
        {
            currentHP = Mathf.Max(0, currentHP - damage);
            UpdateHealthBar();
            
            // 데미지 애니메이션
            StartCoroutine(DamageAnimation());
        }
        
        private void UpdateHealthBar()
        {
            if (healthBarFill != null)
            {
                float healthPercent = currentHP / maxHP;
                healthBarFill.localScale = new Vector3(healthPercent, 1, 1);
            }
        }
        
        private System.Collections.IEnumerator DamageAnimation()
        {
            // 간단한 흔들림 효과
            Vector3 originalPos = transform.position;
            float shakeDuration = 0.2f;
            float shakeAmount = 0.1f;
            float elapsed = 0;
            
            while (elapsed < shakeDuration)
            {
                transform.position = originalPos + Random.insideUnitSphere * shakeAmount;
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.position = originalPos;
        }
        
        public bool IsAlive()
        {
            return currentHP > 0;
        }
        
        public float GetAttackPower()
        {
            // 직업에 따라 물리/마법 공격력 반환
            switch (jobClass)
            {
                case JobClass.Wizard:
                case JobClass.Priest:
                case JobClass.Sage:
                    return magicPower;
                default:
                    return attack;
            }
        }
        
        public void ShowAttackRange()
        {
            // 공격 범위 시각화 (필요시 구현)
        }
        
        public void HideAttackRange()
        {
            // 공격 범위 숨기기 (필요시 구현)
        }
    }
}