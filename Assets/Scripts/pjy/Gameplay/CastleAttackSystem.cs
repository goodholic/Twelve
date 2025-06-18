using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace pjy.Gameplay
{
    public class CastleAttackSystem : MonoBehaviour
    {
        [Header("Castle Settings")]
        [SerializeField] private CastleType castleType = CastleType.Final;
        [SerializeField] private int areaIndex = 1;
        
        [Header("Attack Settings")]
        [SerializeField] private float attackPower = 50f;
        [SerializeField] private float attackRange = 8f;
        [SerializeField] private float attackSpeed = 1f;
        [SerializeField] private bool canAttackMonsters = true;
        [SerializeField] private bool canAttackCharacters = true;
        
        [Header("Area Attack Settings")]
        [SerializeField] private bool hasAreaAttack = true;
        [SerializeField] private float areaAttackRadius = 3f;
        [SerializeField] private float areaAttackCooldown = 5f;
        [SerializeField] private float areaAttackDamageMultiplier = 1.5f;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject attackEffectPrefab;
        [SerializeField] private GameObject areaAttackEffectPrefab;
        [SerializeField] private LineRenderer attackLineRenderer;
        
        [Header("Runtime Info")]
        [SerializeField] private List<GameObject> currentTargets = new List<GameObject>();
        [SerializeField] private float nextAttackTime;
        [SerializeField] private float nextAreaAttackTime;
        
        private float currentHealth;
        private float maxHealth;
        private bool isActive = true;
        
        public enum CastleType
        {
            Middle,
            Final
        }
        
        private void Start()
        {
            InitializeCastle();
            
            if (attackLineRenderer == null)
            {
                attackLineRenderer = gameObject.AddComponent<LineRenderer>();
                attackLineRenderer.startWidth = 0.1f;
                attackLineRenderer.endWidth = 0.05f;
                attackLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                attackLineRenderer.startColor = Color.red;
                attackLineRenderer.endColor = Color.yellow;
                attackLineRenderer.enabled = false;
            }
        }
        
        private void InitializeCastle()
        {
            if (castleType == CastleType.Final)
            {
                maxHealth = 1000f;
                attackPower = 50f;
                attackRange = 8f;
                areaAttackRadius = 3f;
            }
            else
            {
                maxHealth = 500f;
                attackPower = 20f;
                attackRange = 5f;
                areaAttackRadius = 2f;
            }
            
            currentHealth = maxHealth;
        }
        
        private void Update()
        {
            if (!isActive || currentHealth <= 0) return;
            
            UpdateTargets();
            
            if (Time.time >= nextAttackTime && currentTargets.Count > 0)
            {
                PerformNormalAttack();
                nextAttackTime = Time.time + (1f / attackSpeed);
            }
            
            if (hasAreaAttack && Time.time >= nextAreaAttackTime)
            {
                CheckAndPerformAreaAttack();
            }
        }
        
        private void UpdateTargets()
        {
            currentTargets.Clear();
            
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, attackRange);
            
            foreach (var collider in colliders)
            {
                if (collider == null || collider.gameObject == gameObject) continue;
                
                if (canAttackMonsters)
                {
                    Monster monster = collider.GetComponent<Monster>();
                    if (monster != null && monster.areaIndex != areaIndex)
                    {
                        currentTargets.Add(monster.gameObject);
                        continue;
                    }
                }
                
                if (canAttackCharacters)
                {
                    Character character = collider.GetComponent<Character>();
                    if (character != null && character.areaIndex != areaIndex)
                    {
                        currentTargets.Add(character.gameObject);
                        continue;
                    }
                }
            }
            
            currentTargets = currentTargets.OrderBy(t => Vector3.Distance(transform.position, t.transform.position)).ToList();
        }
        
        private void PerformNormalAttack()
        {
            if (currentTargets.Count == 0) return;
            
            GameObject target = currentTargets[0];
            if (target == null) return;
            
            Monster monster = target.GetComponent<Monster>();
            if (monster != null)
            {
                monster.TakeDamage(attackPower);
                ShowAttackEffect(target.transform.position);
                Debug.Log($"[CastleAttack] {name} attacked Monster for {attackPower} damage");
            }
            else
            {
                Character character = target.GetComponent<Character>();
                if (character != null)
                {
                    character.TakeDamage(attackPower);
                    ShowAttackEffect(target.transform.position);
                    Debug.Log($"[CastleAttack] {name} attacked Character {character.characterName} for {attackPower} damage");
                }
            }
            
            if (attackLineRenderer != null)
            {
                StartCoroutine(ShowAttackLine(target.transform.position));
            }
        }
        
        private void CheckAndPerformAreaAttack()
        {
            List<GameObject> nearbyTargets = new List<GameObject>();
            float checkRadius = areaAttackRadius * 1.5f;
            
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, checkRadius);
            
            foreach (var collider in colliders)
            {
                if (collider == null || collider.gameObject == gameObject) continue;
                
                if (canAttackMonsters)
                {
                    Monster monster = collider.GetComponent<Monster>();
                    if (monster != null && monster.areaIndex != areaIndex)
                    {
                        nearbyTargets.Add(monster.gameObject);
                        continue;
                    }
                }
                
                if (canAttackCharacters)
                {
                    Character character = collider.GetComponent<Character>();
                    if (character != null && character.areaIndex != areaIndex)
                    {
                        nearbyTargets.Add(character.gameObject);
                        continue;
                    }
                }
            }
            
            if (nearbyTargets.Count >= 3)
            {
                PerformAreaAttack();
                nextAreaAttackTime = Time.time + areaAttackCooldown;
            }
        }
        
        private void PerformAreaAttack()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, areaAttackRadius);
            float areaDamage = attackPower * areaAttackDamageMultiplier;
            
            foreach (var collider in colliders)
            {
                if (collider == null || collider.gameObject == gameObject) continue;
                
                Monster monster = collider.GetComponent<Monster>();
                if (monster != null && monster.areaIndex != areaIndex && canAttackMonsters)
                {
                    monster.TakeDamage(areaDamage);
                    continue;
                }
                
                Character character = collider.GetComponent<Character>();
                if (character != null && character.areaIndex != areaIndex && canAttackCharacters)
                {
                    character.TakeDamage(areaDamage);
                }
            }
            
            ShowAreaAttackEffect();
            Debug.Log($"[CastleAttack] {name} performed area attack! Damage: {areaDamage}");
        }
        
        private void ShowAttackEffect(Vector3 targetPosition)
        {
            if (attackEffectPrefab != null)
            {
                Vector3 effectPos = Vector3.Lerp(transform.position, targetPosition, 0.8f);
                GameObject effect = Instantiate(attackEffectPrefab, effectPos, Quaternion.identity);
                Destroy(effect, 1f);
            }
        }
        
        private void ShowAreaAttackEffect()
        {
            if (areaAttackEffectPrefab != null)
            {
                GameObject effect = Instantiate(areaAttackEffectPrefab, transform.position, Quaternion.identity);
                effect.transform.localScale = Vector3.one * areaAttackRadius * 2f;
                Destroy(effect, 1.5f);
            }
        }
        
        private IEnumerator ShowAttackLine(Vector3 targetPosition)
        {
            if (attackLineRenderer == null) yield break;
            
            attackLineRenderer.enabled = true;
            attackLineRenderer.SetPosition(0, transform.position);
            attackLineRenderer.SetPosition(1, targetPosition);
            
            yield return new WaitForSeconds(0.2f);
            
            attackLineRenderer.enabled = false;
        }
        
        public void TakeDamage(float damage)
        {
            if (!isActive || currentHealth <= 0) return;
            
            currentHealth -= damage;
            if (currentHealth < 0) currentHealth = 0;
            
            Debug.Log($"[CastleAttack] {name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
            
            if (currentHealth <= 0)
            {
                OnCastleDestroyed();
            }
        }
        
        private void OnCastleDestroyed()
        {
            isActive = false;
            Debug.Log($"[CastleAttack] {name} has been destroyed!");
            
            if (castleType == CastleType.Final)
            {
                GameManager gameManager = FindFirstObjectByType<GameManager>();
                if (gameManager != null)
                {
                    if (areaIndex == 1)
                    {
                        gameManager.SetGameOver(false);
                    }
                    else
                    {
                        gameManager.SetGameOver(true);
                    }
                }
            }
            
            Destroy(gameObject, 0.5f);
        }
        
        public float GetCurrentHealth() => currentHealth;
        public float GetMaxHealth() => maxHealth;
        public bool IsActive() => isActive;
        
        public void SetAttackTargets(bool monsters, bool characters)
        {
            canAttackMonsters = monsters;
            canAttackCharacters = characters;
        }
        
        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            if (hasAreaAttack)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, areaAttackRadius);
            }
            
            if (currentTargets != null)
            {
                Gizmos.color = Color.green;
                foreach (var target in currentTargets)
                {
                    if (target != null)
                    {
                        Gizmos.DrawLine(transform.position, target.transform.position);
                    }
                }
            }
        }
        #endif
    }
}