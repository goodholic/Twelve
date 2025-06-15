///////////////////////////////
// Assets\Scripts\Bullet.cs
///////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 10종의 탄환 타입 정의
/// </summary>
public enum BulletType
{
    Normal,
    Piercing,
    Explosive,
    Slow,
    Bleed,
    Chain,
    Stun,
    ArmorPenetration,
    Split,
    Energy
}

/// <summary>
/// 타워(또는 유닛)에서 발사되는 탄환.
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Bullet Type (10종)")]
    public BulletType bulletType = BulletType.Normal;

    [Header("공통: 공격력 / 속도 / 최대 생존시간")]
    public float damage = 10f;
    public float speed = 3f;
    public float maxLifeTime = 3f;

    [Header("광역 공격 여부(폭발탄 등)")]
    public bool isAreaAttack = false;
    public float areaRadius = 1.5f;

    [Header("타겟 / 기타")]
    public GameObject target;        // 몬스터나 캐릭터 모두 사용 가능하도록 GameObject로 변경
    private float aliveTime = 0f;    // 사용 중인 시간 체크
    private IDamageable damageableTarget;  // 인터페이스 사용 (Monster/Character/Castle 등)

    // -------------------------------------------------------
    // [탄환별 추가 옵션들]
    // -------------------------------------------------------
    [Header("관통탄 옵션")]
    public int maxPierceCount = 3;

    [Header("슬로우탄 옵션")]
    public float slowDuration = 2f;
    public float slowAmount = 0.5f;

    [Header("출혈탄 옵션(DoT)")]
    public float bleedDuration = 3f;
    public float bleedDamagePerSec = 2f;

    [Header("연쇄탄 옵션")]
    public int chainMaxBounces = 3;
    public float chainBounceRange = 2f;

    [Header("마비탄 옵션")]
    public float stunDuration = 1f;

    [Header("파괴탄 옵션(방어 무시)")]
    public bool ignoreDefense = true;

    [Header("분열탄 옵션")]
    public int splitCount = 3;
    public float splitBulletAngleSpread = 30f;
    public GameObject subBulletPrefab;

    [Header("에너지탄 옵션")]
    public float extraRange = 5f;
    public float energySpeedFactor = 1.2f;

    [Header("Impact / 폭발 이펙트 (옵션)")]
    public GameObject impactEffectPrefab;
    public GameObject explosionEffectPrefab;

    // 포물선 이동을 위한 변수들
    [Header("포물선 설정")]
    public float arcHeight = 0.5f;  // 포물선의 최대 높이 (값이 클수록 더 높게 날아감)
    private Vector3 startPosition;   // 총알 시작 위치
    private float totalDistance;     // 총알이 날아갈 총 거리
    private float currentDistance;   // 현재까지 날아간 거리
    private float speedMultiplier = 0.7f;  // 전체적인 속도 감소 인자 (조정용)

    // ================================
    // [수정] 아래 두 스프라이트는
    // "소환된 캐릭터가 (위/아래로) 표시될 때" 쓰이는 것이며,
    // 실제 '탄환' 그래픽으로 사용되지 않음!
    // ================================
    [Header("소환된 캐릭터가 위/아래로 공격할 때 보여줄 스프라이트 (총알 스프라이트 X)")]
    public Sprite bulletUpDirectionSprite;    // (기존) 위쪽 방향 탄환 스프라이트 -> 지금은 "소환된 캐릭터(위로 쏠 때)"
    public Sprite bulletDownDirectionSprite;  // (기존) 아래쪽 방향 탄환 스프라이트 -> 지금은 "소환된 캐릭터(아래로 쏠 때)"

    private SpriteRenderer spriteRenderer;
    private UnityEngine.UI.Image uiImage;

    private int piercedCount = 0;
    private List<Monster> chainAttackedMonsters = new List<Monster>();
    private int currentBounceCount = 0;

    // VFX 패널 (정적 참조)
    private static RectTransform vfxPanel;

    // === [수정 부분] ===
    [Header("Area 구분 (1 or 2)")]
    public int areaIndex = 1;
    // =================

    /// <summary>
    /// VFX 이펙트들이 생성될 부모 패널 설정 (정적 메서드)
    /// </summary>
    public static void SetVfxPanel(RectTransform panel)
    {
        vfxPanel = panel;
        Debug.Log("[Bullet] VFX 패널 설정됨: " + (panel != null ? panel.name : "null"));
    }

    /// <summary>
    /// 탄환 초기화 (몬스터/캐릭터/성 등 IDamageable 대상)
    /// </summary>
    public void Init(IDamageable targetObject, float baseDamage, float baseSpeed,
                     bool areaAtk, float areaAtkRadius, int areaIndex)
    {
        if (targetObject != null)
        {
            if (targetObject is Monster targetMonster)
            {
                this.target = targetMonster.gameObject;
                this.damageableTarget = targetMonster;
            }
            else if (targetObject is Character targetChar)
            {
                this.target = targetChar.gameObject;
                this.damageableTarget = targetChar;
            }
            else if (targetObject is MiddleCastle targetMiddleCastle)
            {
                this.target = targetMiddleCastle.gameObject;
                this.damageableTarget = targetMiddleCastle;
            }
            else if (targetObject is FinalCastle targetFinalCastle)
            {
                this.target = targetFinalCastle.gameObject;
                this.damageableTarget = targetFinalCastle;
            }

            this.damage = baseDamage;
            this.speed = baseSpeed;
            this.isAreaAttack = areaAtk;
            this.areaRadius = areaAtkRadius;
            this.areaIndex = areaIndex;

            // 포물선 이동 초기화
            this.startPosition = transform.position;
            if (target != null)
            {
                this.totalDistance = Vector2.Distance(startPosition, target.transform.position);
            }
            this.currentDistance = 0f;
        }
    }

    /// <summary>
    /// 몬스터를 타겟으로 하는 탄환 초기화 (이전 호환)
    /// </summary>
    public void Init(Monster targetMonster, float baseDamage, float baseSpeed,
                     bool areaAtk, float areaAtkRadius, int areaIndex)
    {
        Init((IDamageable)targetMonster, baseDamage, baseSpeed, areaAtk, areaAtkRadius, areaIndex);
    }

    /// <summary>
    /// 캐릭터를 타겟으로 하는 탄환 초기화 (이전 호환)
    /// </summary>
    public void InitForCharacter(Character targetChar, float baseDamage, float baseSpeed,
                                bool areaAtk, float areaAtkRadius, int areaIndex)
    {
        Init((IDamageable)targetChar, baseDamage, baseSpeed, areaAtk, areaAtkRadius, areaIndex);
    }

    private void Start()
    {
        piercedCount = 0;
        currentBounceCount = 0;
        chainAttackedMonsters.Clear();

        // 포물선 이동 초기화
        startPosition = transform.position;
        if (target != null)
        {
            totalDistance = Vector2.Distance(startPosition, target.transform.position);
        }
        currentDistance = 0f;

        // 스프라이트 렌더러 or UI 이미지
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<UnityEngine.UI.Image>();

        // UI 상에서 총알 안 보이는 문제 해결 - RectTransform 초기화 개선
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            // 앵커와 피벗 설정
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            
            // 크기 설정 (총알이 보이도록 적절한 크기 설정)
            rt.sizeDelta = new Vector2(30f, 30f); // 기본 크기 30x30
            
            // 스케일 확인
            if (rt.localScale.x < 0.1f || rt.localScale.y < 0.1f)
            {
                rt.localScale = Vector3.one;
            }
            
            // 맨 앞으로 이동
            rt.SetAsLastSibling();
            
            // UI 이미지가 있다면 활성화 확인
            if (uiImage != null)
            {
                uiImage.enabled = true;
                
                // 이미지 색상이 투명하지 않도록 확인
                if (uiImage.color.a < 0.1f)
                {
                    Color c = uiImage.color;
                    c.a = 1f;
                    uiImage.color = c;
                }
                
                // 스프라이트가 설정되어 있는지 확인
                if (uiImage.sprite == null)
                {
                    Debug.LogWarning($"[Bullet] UI Image에 스프라이트가 설정되지 않았습니다!");
                }
            }
        }
        else if (spriteRenderer != null)
        {
            // SpriteRenderer 사용 시
            spriteRenderer.enabled = true;
            spriteRenderer.sortingOrder = 100; // 높은 값으로 설정하여 앞쪽에 표시
        }
    }

    private void Update()
    {
        aliveTime += Time.deltaTime;

        if (aliveTime >= maxLifeTime)
        {
            Destroy(gameObject);
            return;
        }

        MoveBullet();
    }

    private void MoveBullet()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        switch (bulletType)
        {
            case BulletType.Energy:
            case BulletType.Normal:
            case BulletType.Piercing:
            case BulletType.Explosive:
            case BulletType.Slow:
            case BulletType.Bleed:
            case BulletType.Stun:
            case BulletType.ArmorPenetration:
            case BulletType.Split:
            {
                Vector3 dir = (target.transform.position - transform.position).normalized;
                float moveDistance = speed * speedMultiplier * Time.deltaTime;

                currentDistance += moveDistance;
                float progressRatio = Mathf.Clamp01(currentDistance / totalDistance);

                Vector3 newPosition = Vector3.Lerp(startPosition, target.transform.position, progressRatio);
                float arcHeightAtPoint = arcHeight * Mathf.Sin(progressRatio * Mathf.PI);
                newPosition.y += arcHeightAtPoint;
                transform.position = newPosition;

                float dist = Vector2.Distance(transform.position, target.transform.position);
                if (dist < 0.2f || progressRatio >= 0.98f)
                {
                    OnHitTarget();
                }
                break;
            }

            case BulletType.Chain:
            {
                Vector3 dirC = (target.transform.position - transform.position).normalized;
                float moveDistanceC = speed * speedMultiplier * Time.deltaTime;

                currentDistance += moveDistanceC;
                float progressRatioC = Mathf.Clamp01(currentDistance / totalDistance);

                Vector3 newPositionC = Vector3.Lerp(startPosition, target.transform.position, progressRatioC);
                float arcHeightAtPointC = arcHeight * Mathf.Sin(progressRatioC * Mathf.PI);
                newPositionC.y += arcHeightAtPointC;
                transform.position = newPositionC;

                float distC = Vector2.Distance(transform.position, target.transform.position);
                if (distC < 0.2f || progressRatioC >= 0.98f)
                {
                    OnHitTarget();
                }
                break;
            }
        }
    }

    private void OnHitTarget()
    {
        switch (bulletType)
        {
            case BulletType.Normal:
                ApplyDamageToTarget(damage);
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Piercing:
                ApplyDamageToTarget(damage);
                piercedCount++;
                SpawnImpactEffect(target.transform.position);

                if (piercedCount >= maxPierceCount)
                {
                    Destroy(gameObject);
                }
                else
                {
                    FindNextUnifiedTarget(target.transform.position);
                }
                break;

            case BulletType.Explosive:
                ApplyDamageToTarget(damage);
                ApplyUnifiedAreaDamage(target.transform.position);
                SpawnExplosionEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Slow:
                ApplyDamageToTarget(damage * 0.8f);
                if (damageableTarget is Monster monsterSlow)
                {
                    monsterSlow.ApplySlow(slowDuration, slowAmount);
                }
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Bleed:
                ApplyDamageToTarget(damage * 0.7f);
                if (damageableTarget is Monster monsterBleed)
                {
                    monsterBleed.ApplyBleed(bleedDuration, bleedDamagePerSec);
                }
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Chain:
                ApplyDamageToTarget(damage);
                currentBounceCount++;
                SpawnImpactEffect(target.transform.position);

                if (currentBounceCount >= chainMaxBounces)
                {
                    Destroy(gameObject);
                }
                else
                {
                    FindNextUnifiedTarget(target.transform.position);
                }
                break;

            case BulletType.Stun:
                ApplyDamageToTarget(damage * 0.8f);
                if (damageableTarget is Monster monsterStun)
                {
                    monsterStun.ApplyStun(stunDuration);
                }
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.ArmorPenetration:
                ApplyDamageToTarget(damage);
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Split:
                ApplyDamageToTarget(damage);
                if (subBulletPrefab != null && splitCount > 0)
                {
                    DoSplit(target.transform.position);
                }
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;

            case BulletType.Energy:
                ApplyDamageToTarget(damage * 0.8f);
                SpawnImpactEffect(target.transform.position);
                Destroy(gameObject);
                break;
        }
    }

    private void ApplyDamageToTarget(float dmg)
    {
        if (damageableTarget != null)
        {
            damageableTarget.TakeDamage(dmg);
        }
    }

    private void ApplyUnifiedAreaDamage(Vector3 center)
    {
        if (areaRadius <= 0f) return;

        // 몬스터 광역 데미지
        Monster[] allMonsters = Object.FindObjectsByType<Monster>(FindObjectsSortMode.None);
        foreach (var m in allMonsters)
        {
            if (m == null) continue;
            if (m.gameObject == target) continue;
            float dist = Vector2.Distance(center, m.transform.position);
            if (dist <= areaRadius)
            {
                m.TakeDamage(damage);
            }
        }

        // 캐릭터 광역 데미지
        Character[] allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var c in allCharacters)
        {
            if (c == null) continue;
            if (c.gameObject == target) continue;
            if (c.areaIndex == this.areaIndex) continue;
            
            float dist = Vector2.Distance(center, c.transform.position);
            if (dist <= areaRadius)
            {
                c.TakeDamage(damage);
            }
        }
    }

    private void FindNextUnifiedTarget(Vector3 position)
    {
        GameObject bestTarget = null;
        IDamageable bestDamageable = null;
        float minDist = float.MaxValue;

        // 몬스터 체크
        Monster[] allMonsters = Object.FindObjectsByType<Monster>(FindObjectsSortMode.None);
        foreach (var m in allMonsters)
        {
            if (m == null) continue;
            if (m.gameObject == target) continue;
            
            float dist = Vector2.Distance(position, m.transform.position);
            if (dist < chainBounceRange && dist < minDist)
            {
                minDist = dist;
                bestTarget = m.gameObject;
                bestDamageable = m;
            }
        }
        
        // 캐릭터 체크
        Character[] allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var c in allCharacters)
        {
            if (c == null) continue;
            if (c.gameObject == target) continue;
            if (c.areaIndex == this.areaIndex) continue;
            
            float dist = Vector2.Distance(position, c.transform.position);
            if (dist < chainBounceRange && dist < minDist)
            {
                minDist = dist;
                bestTarget = c.gameObject;
                bestDamageable = c;
            }
        }
        
        // 중간성 체크
        MiddleCastle[] middleCastles = Object.FindObjectsByType<MiddleCastle>(FindObjectsSortMode.None);
        foreach (var castle in middleCastles)
        {
            if (castle == null) continue;
            if (castle.gameObject == target) continue;
            if (castle.areaIndex == this.areaIndex) continue;
            
            float dist = Vector2.Distance(position, castle.transform.position);
            if (dist < chainBounceRange && dist < minDist)
            {
                minDist = dist;
                bestTarget = castle.gameObject;
                bestDamageable = castle;
            }
        }
        
        // 최종성 체크
        FinalCastle[] finalCastles = Object.FindObjectsByType<FinalCastle>(FindObjectsSortMode.None);
        foreach (var castle in finalCastles)
        {
            if (castle == null) continue;
            if (castle.gameObject == target) continue;
            if (castle.areaIndex == this.areaIndex) continue;
            
            float dist = Vector2.Distance(position, castle.transform.position);
            if (dist < chainBounceRange && dist < minDist)
            {
                minDist = dist;
                bestTarget = castle.gameObject;
                bestDamageable = castle;
            }
        }

        if (bestTarget != null && bestDamageable != null)
        {
            target = bestTarget;
            damageableTarget = bestDamageable;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void DoSplit(Vector3 splitCenter)
    {
        float halfAngle = (splitBulletAngleSpread * (splitCount - 1)) / 2f;
        for (int i = 0; i < splitCount; i++)
        {
            float angle = -halfAngle + i * splitBulletAngleSpread;

            GameObject sub = Instantiate(subBulletPrefab, splitCenter, Quaternion.identity);
            Bullet subB = sub.GetComponent<Bullet>();
            if (subB != null)
            {
                subB.bulletType = BulletType.Normal;
                subB.damage = this.damage * 0.6f;
                subB.speed = this.speed * 0.6f;
                subB.maxLifeTime = 2f;
                subB.speedMultiplier = this.speedMultiplier;
                subB.areaIndex = this.areaIndex;

                subB.isAreaAttack = false;
                subB.areaRadius = 0f;

                Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                Rigidbody2D rb = sub.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = dir * subB.speed * subB.speedMultiplier;
                }
            }
        }
    }

    private void SpawnImpactEffect(Vector3 pos)
    {
        if (impactEffectPrefab != null)
        {
            GameObject effect;
            if (vfxPanel != null)
            {
                effect = Instantiate(impactEffectPrefab, vfxPanel);
                RectTransform rectTransform = effect.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Vector2 localPos = vfxPanel.InverseTransformPoint(pos);
                    rectTransform.anchoredPosition = localPos;
                }
                else
                {
                    effect.transform.position = pos;
                }
            }
            else
            {
                effect = Instantiate(impactEffectPrefab, pos, Quaternion.identity);
            }
            Destroy(effect, 1f);
        }
    }

    private void SpawnExplosionEffect(Vector3 pos)
    {
        if (explosionEffectPrefab != null)
        {
            GameObject effect;
            if (vfxPanel != null)
            {
                effect = Instantiate(explosionEffectPrefab, vfxPanel);
                RectTransform rectTransform = effect.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Vector2 localPos = vfxPanel.InverseTransformPoint(pos);
                    rectTransform.anchoredPosition = localPos;
                }
                else
                {
                    effect.transform.position = pos;
                }
            }
            else
            {
                effect = Instantiate(explosionEffectPrefab, pos, Quaternion.identity);
            }
            Destroy(effect, 1f);
        }
    }

    // =====================================================================
    // CharacterCombat에서 호출하는 메서드들 (호환성을 위해 추가)
    // =====================================================================
    
    /// <summary>
    /// CharacterCombat에서 호출하는 Initialize 메서드 (Init 메서드의 래퍼)
    /// </summary>
    public void Initialize(GameObject targetGameObject, float baseDamage, int areaIndex, 
                          bool areaAtk, float baseSpeed, Character sourceCharacter)
    {
        // targetGameObject에서 IDamageable 컴포넌트 찾기
        IDamageable targetDamageable = null;
        
        if (targetGameObject != null)
        {
            // Monster 컴포넌트 확인
            Monster monster = targetGameObject.GetComponent<Monster>();
            if (monster != null)
            {
                targetDamageable = monster;
            }
            else
            {
                // Character 컴포넌트 확인
                Character character = targetGameObject.GetComponent<Character>();
                if (character != null)
                {
                    targetDamageable = character;
                }
                else
                {
                    // Castle 컴포넌트들 확인
                    MiddleCastle middleCastle = targetGameObject.GetComponent<MiddleCastle>();
                    if (middleCastle != null)
                    {
                        targetDamageable = middleCastle;
                    }
                    else
                    {
                        FinalCastle finalCastle = targetGameObject.GetComponent<FinalCastle>();
                        if (finalCastle != null)
                        {
                            targetDamageable = finalCastle;
                        }
                    }
                }
            }
        }
        
        if (targetDamageable != null)
        {
            // 기본 광역 반지름 설정 (sourceCharacter가 있는 경우)
            float areaAtkRadius = 1.5f;
            if (sourceCharacter != null && sourceCharacter.isAreaAttack)
            {
                areaAtkRadius = sourceCharacter.areaAttackRadius;
            }
            
            // 기존 Init 메서드 호출
            Init(targetDamageable, baseDamage, baseSpeed, areaAtk, areaAtkRadius, areaIndex);
        }
        else
        {
            Debug.LogWarning($"[Bullet] Initialize: 타겟 오브젝트에서 IDamageable 컴포넌트를 찾을 수 없습니다: {targetGameObject?.name}");
        }
    }
    
    /// <summary>
    /// 광역 공격 설정
    /// </summary>
    public void SetAreaAttack(float radius)
    {
        this.isAreaAttack = true;
        this.areaRadius = radius;
    }
    
    /// <summary>
    /// 근접 공격으로 설정 (즉시 타겟에 도달하도록)
    /// </summary>
    public void SetAsMeleeAttack()
    {
        // 근접 공격의 경우 속도를 매우 빠르게 설정하여 즉시 도달하도록 함
        this.speed = 50f;
        this.maxLifeTime = 0.5f; // 짧은 생존 시간
        this.speedMultiplier = 2f; // 속도 배율 증가
    }
}