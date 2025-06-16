using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 중간성 시스템 - 3라인의 중간 지점에 위치한 성
/// </summary>
public class MiddleCastle : MonoBehaviour, IDamageable
{
    [Header("중간성 설정")]
    public float maxHealth = 500f;
    public float currentHealth;
    
    [Header("라인 정보")]
    [Tooltip("이 중간성이 속한 라인 (Left/Center/Right)")]
    public RouteType routeType = RouteType.Center;
    
    [Header("지역 정보")]
    [Tooltip("이 중간성이 속한 지역 (1 또는 2)")]
    public int areaIndex = 1;
    
    [Header("공격 설정")]
    [Tooltip("공격력")]
    public float attackPower = 20f;
    [Tooltip("공격 사거리")]
    public float attackRange = 5f;
    [Tooltip("공격 쿨다운")]
    public float attackCooldown = 1.5f;
    [Tooltip("공격 타입")]
    public AttackTargetType attackTargetType = AttackTargetType.All;
    
    [Header("총알 설정")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 8f;
    public Transform firePoint;
    
    [Header("UI 요소")]
    [SerializeField] private Canvas hpBarCanvas;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TextMeshProUGUI castleNameText;
    
    [Header("파괴 시 이펙트")]
    [SerializeField] private GameObject destroyEffectPrefab;
    
    private bool isDestroyed = false;
    private float lastAttackTime = 0f;
    private IDamageable currentTarget;
    
    // 중간성 파괴 이벤트
    public System.Action<RouteType, int> OnMiddleCastleDestroyed;
    
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
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = new Vector3(0, 0.5f, 0);
            firePoint = fp.transform;
        }
        
        // 기본 총알 프리팹 설정
        if (bulletPrefab == null)
        {
            Debug.LogWarning($"[MiddleCastle] {gameObject.name}의 bulletPrefab이 설정되지 않았습니다!");
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
            string routeName = "";
            switch (routeType)
            {
                case RouteType.Left:
                    routeName = "좌측";
                    break;
                case RouteType.Center:
                    routeName = "중앙";
                    break;
                case RouteType.Right:
                    routeName = "우측";
                    break;
            }
            castleNameText.text = $"지역{areaIndex} {routeName} 중간성";
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
        
        IDamageable bestTarget = null;
        GameObject targetObject = null;
        float closestDistance = float.MaxValue;
        
        // attackTargetType에 따라 타겟 찾기
        switch (attackTargetType)
        {
            case AttackTargetType.Monster:
            case AttackTargetType.All:
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
                
            case AttackTargetType.Character:
                break; // Character 타겟은 아래에서 처리
        }
        
        // 캐릭터 찾기 (Character 또는 All 타입일 때)
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
    
    /// <summary>
    /// 타겟 공격
    /// </summary>
    private void AttackTarget(IDamageable target, GameObject targetObject)
    {
        if (target == null || targetObject == null) return;
        
        currentTarget = target;
        lastAttackTime = Time.time;
        
        // 총알 발사
        if (bulletPrefab != null && firePoint != null)
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
            
            Debug.Log($"[MiddleCastle] {gameObject.name}이(가) {targetObject.name}을(를) 공격!");
        }
        else
        {
            // 총알 없이 직접 데미지
            target.TakeDamage(attackPower);
            Debug.Log($"[MiddleCastle] {gameObject.name}이(가) 직접 공격! 데미지: {attackPower}");
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;
        
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        
        UpdateHealthBar();
        
        Debug.Log($"[MiddleCastle] {gameObject.name} 피격! 남은 체력: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            DestroyMiddleCastle();
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
    }
    
    private void DestroyMiddleCastle()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        
        Debug.Log($"[MiddleCastle] {gameObject.name} 파괴됨!");
        
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
        OnMiddleCastleDestroyed?.Invoke(routeType, areaIndex);
        
        // CastleHealthManager에 알림
        CastleHealthManager castleManager = FindFirstObjectByType<CastleHealthManager>();
        if (castleManager != null)
        {
            Debug.Log($"[MiddleCastle] CastleHealthManager에 파괴 알림");
            // CastleHealthManager의 중간성 파괴 처리는 TakeDamage를 통해 자동으로 됨
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
            spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 어둡게 표시
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
        
        // 현재 타겟이 있으면 선으로 연결
        if (currentTarget != null)
        {
            MonoBehaviour targetMono = currentTarget as MonoBehaviour;
            if (targetMono != null)
            {
                Gizmos.color = Color.yellow;
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