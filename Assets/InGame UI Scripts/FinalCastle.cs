using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 최종성 시스템 - 각 지역의 최종 방어선
/// </summary>
public class FinalCastle : MonoBehaviour, IDamageable
{
    [Header("최종성 설정")]
    public float maxHealth = 1000f;
    public float currentHealth;
    
    [Header("지역 정보")]
    [Tooltip("이 최종성이 속한 지역 (1 또는 2)")]
    public int areaIndex = 1;
    
    [Header("공격 설정")]
    [Tooltip("공격력")]
    public float attackPower = 30f;
    [Tooltip("공격 사거리")]
    public float attackRange = 6f;
    [Tooltip("공격 쿨다운")]
    public float attackCooldown = 1.2f;
    [Tooltip("공격 타입")]
    public AttackTargetType attackTargetType = AttackTargetType.All;
    [Tooltip("범위 공격 여부")]
    public bool isAreaAttack = false;
    [Tooltip("범위 공격 반경")]
    public float areaAttackRadius = 2f;
    
    [Header("총알 설정")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;
    public Transform[] firePoints; // 다중 발사 위치
    
    [Header("UI 요소")]
    [SerializeField] private Canvas hpBarCanvas;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TextMeshProUGUI castleNameText;
    [SerializeField] private TextMeshProUGUI healthText;
    
    [Header("파괴 시 이펙트")]
    [SerializeField] private GameObject destroyEffectPrefab;
    
    private bool isDestroyed = false;
    private float lastAttackTime = 0f;
    private IDamageable currentTarget;
    
    // 최종성 파괴 이벤트 (게임 종료)
    public System.Action<int> OnFinalCastleDestroyed;
    
    // 코루틴
    private Coroutine attackCoroutine;
    
    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
        UpdateCastleName();
        
        // 공격 시작
        StartAttacking();
        
        // 총알 발사 위치가 없으면 자동 생성
        if (firePoints == null || firePoints.Length == 0)
        {
            firePoints = new Transform[1];
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = new Vector3(0, 1f, 0);
            firePoints[0] = fp.transform;
        }
        
        // 기본 총알 프리팹 설정
        if (bulletPrefab == null)
        {
            Debug.LogWarning($"[FinalCastle] {gameObject.name}의 bulletPrefab이 설정되지 않았습니다!");
            // CoreDataManager에서 기본 총알 프리팹 가져오기
            if (CoreDataManager.Instance != null && CoreDataManager.Instance.defaultBulletPrefab != null)
            {
                bulletPrefab = CoreDataManager.Instance.defaultBulletPrefab;
            }
        }
    }
    
    private void UpdateCastleName()
    {
        if (castleNameText != null)
        {
            castleNameText.text = $"지역{areaIndex} 최종성";
        }
    }
    
    /// <summary>
    /// 공격 시작
    /// </summary>
    private void StartAttacking()
    {
        if (attackCoroutine == null && !isDestroyed)
        {
            attackCoroutine = StartCoroutine(AttackRoutine());
        }
    }
    
    /// <summary>
    /// 공격 루틴
    /// </summary>
    private IEnumerator AttackRoutine()
    {
        while (!isDestroyed)
        {
            // 타겟 찾기
            FindAndAttackTarget();
            
            // 쿨다운 대기
            yield return new WaitForSeconds(attackCooldown);
        }
    }
    
    /// <summary>
    /// 타겟 찾기 및 공격
    /// </summary>
    private void FindAndAttackTarget()
    {
        if (isDestroyed) return;
        
        if (isAreaAttack)
        {
            // 범위 공격
            PerformAreaAttack();
        }
        else
        {
            // 단일 타겟 공격
            IDamageable bestTarget = null;
            GameObject targetObject = null;
            float closestDistance = float.MaxValue;
            
            // attackTargetType에 따라 타겟 찾기
            switch (attackTargetType)
            {
                case AttackTargetType.Monster:
                case AttackTargetType.All:
                case AttackTargetType.Both:
                    // 몬스터 찾기
                    Monster[] monsters = FindObjectsByType<Monster>(FindObjectsSortMode.None);
                    foreach (var monster in monsters)
                    {
                        if (monster == null || !monster.IsAlive()) continue;
                        if (monster.areaIndex == areaIndex) continue; // 같은 지역 몬스터는 공격하지 않음
                        
                        float distance = Vector3.Distance(transform.position, monster.transform.position);
                        if (distance <= attackRange && distance < closestDistance)
                        {
                            closestDistance = distance;
                            bestTarget = monster;
                            targetObject = monster.gameObject;
                        }
                    }
                    
                    if (attackTargetType == AttackTargetType.Monster && bestTarget != null)
                    {
                        AttackTarget(bestTarget, targetObject);
                        return;
                    }
                    break;
            }
            
            // 캐릭터 찾기 (Character, Both 또는 All 타입일 때)
            if (attackTargetType == AttackTargetType.Character || 
                attackTargetType == AttackTargetType.All ||
                attackTargetType == AttackTargetType.Both)
            {
                Character[] characters = FindObjectsByType<Character>(FindObjectsSortMode.None);
                foreach (var character in characters)
                {
                    if (character == null || character.currentHP <= 0) continue;
                    if (character.areaIndex == areaIndex) continue; // 같은 지역 캐릭터는 공격하지 않음
                    if (character.isHero) continue; // 히어로는 공격하지 않음
                    
                    float distance = Vector3.Distance(transform.position, character.transform.position);
                    if (distance <= attackRange && distance < closestDistance)
                    {
                        closestDistance = distance;
                        bestTarget = character;
                        targetObject = character.gameObject;
                    }
                }
            }
            
            // 타겟이 있으면 공격
            if (bestTarget != null && targetObject != null)
            {
                AttackTarget(bestTarget, targetObject);
            }
        }
    }
    
    /// <summary>
    /// 단일 타겟 공격
    /// </summary>
    private void AttackTarget(IDamageable target, GameObject targetObject)
    {
        if (target == null || targetObject == null) return;
        
        currentTarget = target;
        lastAttackTime = Time.time;
        
        // 모든 발사 위치에서 총알 발사
        foreach (var firePoint in firePoints)
        {
            if (firePoint == null) continue;
            
            if (bulletPrefab != null)
            {
                GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
                
                // Bullet 컴포넌트 설정
                Bullet bulletComp = bullet.GetComponent<Bullet>();
                if (bulletComp == null)
                {
                    bulletComp = bullet.AddComponent<Bullet>();
                }
                
                // 총알 초기화
                bulletComp.damage = attackPower;
                bulletComp.speed = bulletSpeed;
                bulletComp.targetObject = targetObject;
                bulletComp.isFromCastle = true;
                bulletComp.ownerAreaIndex = areaIndex;
                
                // 방향 설정
                Vector3 direction = (targetObject.transform.position - firePoint.position).normalized;
                bulletComp.direction = direction;
                
                // 총알 회전
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                bullet.transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
            }
        }
        
        Debug.Log($"[FinalCastle] {gameObject.name}이(가) {targetObject.name}을(를) 공격!");
    }
    
    /// <summary>
    /// 범위 공격
    /// </summary>
    private void PerformAreaAttack()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, attackRange);
        int targetsHit = 0;
        
        foreach (var collider in colliders)
        {
            if (collider == null) continue;
            
            // 몬스터 체크
            if (attackTargetType == AttackTargetType.Monster || 
                attackTargetType == AttackTargetType.All ||
                attackTargetType == AttackTargetType.Both)
            {
                Monster monster = collider.GetComponent<Monster>();
                if (monster != null && monster.IsAlive() && monster.areaIndex != areaIndex)
                {
                    // 범위 내 모든 몬스터에게 데미지
                    float distance = Vector3.Distance(transform.position, monster.transform.position);
                    if (distance <= areaAttackRadius)
                    {
                        monster.TakeDamage(attackPower);
                        targetsHit++;
                        
                        // 이펙트 생성
                        CreateAttackEffect(monster.transform.position);
                    }
                }
            }
            
            // 캐릭터 체크
            if (attackTargetType == AttackTargetType.Character || 
                attackTargetType == AttackTargetType.All ||
                attackTargetType == AttackTargetType.Both)
            {
                Character character = collider.GetComponent<Character>();
                if (character != null && character.currentHP > 0 && 
                    character.areaIndex != areaIndex && !character.isHero)
                {
                    float distance = Vector3.Distance(transform.position, character.transform.position);
                    if (distance <= areaAttackRadius)
                    {
                        character.TakeDamage(attackPower);
                        targetsHit++;
                        
                        // 이펙트 생성
                        CreateAttackEffect(character.transform.position);
                    }
                }
            }
        }
        
        if (targetsHit > 0)
        {
            Debug.Log($"[FinalCastle] {gameObject.name}의 범위 공격! {targetsHit}개 타겟 명중");
        }
    }
    
    /// <summary>
    /// 공격 이펙트 생성
    /// </summary>
    private void CreateAttackEffect(Vector3 position)
    {
        if (destroyEffectPrefab != null)
        {
            GameObject effect = Instantiate(destroyEffectPrefab, position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * 0.5f; // 작은 크기로
            Destroy(effect, 1f);
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;
        
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        
        UpdateHealthBar();
        
        Debug.Log($"[FinalCastle] 지역{areaIndex} 최종성 피격! 남은 체력: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            DestroyFinalCastle();
        }
    }
    
    private void UpdateHealthBar()
    {
        if (hpFillImage != null)
        {
            float ratio = currentHealth / maxHealth;
            hpFillImage.fillAmount = ratio;
            
            // 체력에 따른 색상 변경
            if (ratio > 0.6f)
                hpFillImage.color = Color.green;
            else if (ratio > 0.3f)
                hpFillImage.color = Color.yellow;
            else
                hpFillImage.color = Color.red;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
        }
    }
    
    private void DestroyFinalCastle()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        
        Debug.Log($"[FinalCastle] 지역{areaIndex} 최종성 파괴됨! 게임 종료!");
        
        // 공격 중지
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        
        // 파괴 이펙트 생성
        if (destroyEffectPrefab != null)
        {
            GameObject effect = Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        
        // 이벤트 호출
        OnFinalCastleDestroyed?.Invoke(areaIndex);
        
        // GameManager에 게임 종료 알림
        if (GameManager.Instance != null)
        {
            // 지역1 최종성 파괴 = 플레이어 패배
            // 지역2 최종성 파괴 = 플레이어 승리
            bool isVictory = (areaIndex == 2);
            GameManager.Instance.SetGameOver(isVictory);
        }
        
        // HP바 비활성화 (성 자체는 비활성화하지 않음)
        if (hpBarCanvas != null)
        {
            hpBarCanvas.gameObject.SetActive(false);
        }
        
        // 스프라이트 변경 또는 파괴된 모습 표시
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 0.5f); // 더 어둡게 표시
        }
    }
    
    /// <summary>
    /// 디버그용 공격 범위 표시
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 공격 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // 범위 공격 반경 표시
        if (isAreaAttack)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, areaAttackRadius);
        }
        
        // 현재 타겟이 있으면 선으로 연결
        if (currentTarget != null)
        {
            MonoBehaviour targetMono = currentTarget as MonoBehaviour;
            if (targetMono != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, targetMono.transform.position);
            }
        }
    }
    
    public bool IsDestroyed()
    {
        return isDestroyed;
    }
    
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public float GetMaxHealth()
    {
        return maxHealth;
    }
}