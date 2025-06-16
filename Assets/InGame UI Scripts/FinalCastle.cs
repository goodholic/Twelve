using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 최종성 시스템 - 모든 중간성이 파괴된 후 최후의 방어선
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
    public float attackPower = 50f;
    [Tooltip("공격 사거리")]
    public float attackRange = 8f;
    [Tooltip("공격 쿨다운")]
    public float attackCooldown = 0.8f;
    [Tooltip("공격 타입")]
    public AttackTargetType attackTargetType = AttackTargetType.All;
    
    [Header("특수 능력 - 범위 공격")]
    [Tooltip("범위 공격 활성화")]
    public bool useAreaAttack = true;
    [Tooltip("범위 공격 반경")]
    public float areaAttackRadius = 3f;
    [Tooltip("범위 공격 쿨다운")]
    public float areaAttackCooldown = 5f;
    private float lastAreaAttackTime = 0f;
    
    [Header("총알 설정")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;
    public Transform firePoint;
    
    [Header("UI 요소")]
    [SerializeField] private Canvas hpBarCanvas;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TextMeshProUGUI castleNameText;
    [SerializeField] private TextMeshProUGUI healthText;
    
    [Header("파괴 시 이펙트")]
    [SerializeField] private GameObject destroyEffectPrefab;
    [SerializeField] private GameObject areaAttackEffectPrefab;
    
    private bool isDestroyed = false;
    private float lastAttackTime = 0f;
    private IDamageable currentTarget;
    
    // 최종성 파괴 이벤트
    public System.Action<int> OnFinalCastleDestroyed; // areaIndex 전달
    
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
            fp.transform.localPosition = new Vector3(0, 1.5f, 0);
            firePoint = fp.transform;
        }
        
        // 기본 총알 프리팹 설정
        if (bulletPrefab == null)
        {
            Debug.LogWarning($"[FinalCastle] {gameObject.name}의 bulletPrefab이 설정되지 않았습니다!");
        }
    }
    
    private void Update()
    {
        if (isDestroyed) return;
        
        // 범위 공격 체크
        if (useAreaAttack && Time.time - lastAreaAttackTime >= areaAttackCooldown)
        {
            PerformAreaAttack();
        }
    }
    
    /// <summary>
    /// 성 이름 업데이트
    /// </summary>
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
        if (attackCoroutine == null)
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
            yield return new WaitForSeconds(attackCooldown);
            
            if (!isDestroyed)
            {
                FindAndAttackTarget();
            }
        }
    }
    
    /// <summary>
    /// 타겟 찾아서 공격
    /// </summary>
    private void FindAndAttackTarget()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        
        IDamageable bestTarget = null;
        GameObject targetObject = null;
        float closestDistance = float.MaxValue;
        
        // 몬스터 찾기
        if (attackTargetType == AttackTargetType.Monster || attackTargetType == AttackTargetType.All)
        {
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
        }
        
        // 캐릭터 찾기
        if (attackTargetType == AttackTargetType.Character || attackTargetType == AttackTargetType.All)
        {
            Character[] characters = FindObjectsByType<Character>(FindObjectsSortMode.None);
            foreach (var character in characters)
            {
                if (character == null || character.currentHP <= 0) continue;
                if (character.areaIndex == areaIndex) continue; // 같은 지역 캐릭터는 공격하지 않음
                
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
            
            Debug.Log($"[FinalCastle] 지역{areaIndex} 최종성이 {targetObject.name}을(를) 공격!");
        }
        else
        {
            // 총알 없이 직접 데미지
            target.TakeDamage(attackPower);
            Debug.Log($"[FinalCastle] 지역{areaIndex} 최종성이 직접 공격! 데미지: {attackPower}");
        }
    }
    
    /// <summary>
    /// 범위 공격 수행
    /// </summary>
    private void PerformAreaAttack()
    {
        lastAreaAttackTime = Time.time;
        
        // 범위 공격 이펙트
        if (areaAttackEffectPrefab != null)
        {
            GameObject effect = Instantiate(areaAttackEffectPrefab, transform.position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * areaAttackRadius;
            Destroy(effect, 2f);
        }
        
        // 범위 내 적들에게 데미지
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, areaAttackRadius);
        int targetsHit = 0;
        
        foreach (var collider in colliders)
        {
            if (collider == null) continue;
            
            // 몬스터 체크
            Monster monster = collider.GetComponent<Monster>();
            if (monster != null && monster.IsAlive() && monster.areaIndex != areaIndex)
            {
                monster.TakeDamage(attackPower * 0.5f); // 범위 공격은 데미지 50%
                CreateAttackEffect(monster.transform.position);
                targetsHit++;
                continue;
            }
            
            // 캐릭터 체크
            Character character = collider.GetComponent<Character>();
            if (character != null && character.currentHP > 0 && character.areaIndex != areaIndex)
            {
                character.TakeDamage(attackPower * 0.5f); // 범위 공격은 데미지 50%
                CreateAttackEffect(character.transform.position);
                targetsHit++;
            }
        }
        
        if (targetsHit > 0)
        {
            Debug.Log($"[FinalCastle] 지역{areaIndex} 최종성 범위 공격! {targetsHit}개 타겟 명중");
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
        
        // 파괴 이펙트
        if (destroyEffectPrefab != null)
        {
            GameObject effect = Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * 2f; // 큰 크기로
            Destroy(effect, 3f);
        }
        
        // 이벤트 호출
        OnFinalCastleDestroyed?.Invoke(areaIndex);
        
        // GameManager에 게임 종료 알림
        if (GameManager.Instance != null)
        {
            // 지역1 최종성이 파괴되면 플레이어 패배
            // 지역2 최종성이 파괴되면 플레이어 승리
            bool playerWin = (areaIndex == 2);
            GameManager.Instance.SetGameOver(playerWin);
        }
        
        // 스프라이트 변경 또는 비활성화
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.2f, 0.2f, 0.2f, 0.3f);
        }
        
        // Collider 비활성화
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
    }
    
    /// <summary>
    /// 파괴 상태 확인
    /// </summary>
    public bool IsDestroyed()
    {
        return isDestroyed;
    }
    
    /// <summary>
    /// 디버그용 - 성 정보 출력
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 일반 공격 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // 범위 공격 반경 표시
        if (useAreaAttack)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, areaAttackRadius);
        }
    }
}