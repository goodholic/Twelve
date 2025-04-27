using System.Collections;
using UnityEngine;
using UnityEngine.UI; // UI Image를 사용할 때 필요

/// <summary>
/// 2D 캐릭터(배치 가능 오브젝트) 예시
/// </summary>
public enum CharacterStar
{
    OneStar = 1,
    TwoStar = 2,
    ThreeStar = 3
}

public class Character : MonoBehaviour
{
    [Header("Character Stats")]
    public CharacterStar star = CharacterStar.OneStar;
    public float attackPower = 10f;

    [Tooltip("1초당 공격 횟수(AttackRoutine 쿨타임 결정)")]
    public float attackSpeed = 1f;

    [Tooltip("공격 사거리(원 범위)")]
    public float attackRange = 1.5f;

    [Tooltip("현재 배치된 타일")]
    public Tile currentTile;

    [Tooltip("현재 공격 중인 몬스터(2D)")]
    public Monster currentTarget;

    // ===================================
    // (추가) 광역 공격 관련 필드
    // ===================================
    [Tooltip("이 캐릭터가 광역 공격인지 여부")]
    public bool isAreaAttack = false;

    [Tooltip("광역 공격 범위(반경) - isAreaAttack=true일 때만 사용")]
    public float areaAttackRadius = 1f;

    [Header("Range Indicator Settings")]
    [Tooltip("원(서클) 형태로 시각화해줄 프리팹(예: 반투명 Circle Sprite 등)")]
    public GameObject rangeIndicatorPrefab;

    [Tooltip("사거리 원을 보여줄지 여부(체크 해제 시 숨길 수 있음)")]
    public bool showRangeIndicator = true;

    private GameObject rangeIndicatorInstance; // 사거리 표시용 런타임 오브젝트

    private float attackCooldown;

    // ===========================
    // 총알 발사 관련 설정
    // ===========================
    [Header("Bullet Settings")]
    [Tooltip("캐릭터가 발사할 총알(Projectile) 프리팹 (Bullet.cs가 붙은 오브젝트)")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 5f;

    private RectTransform bulletPanel;  // 내부적으로만 사용할 참조

    // ======================================================
    // (UI로 사용하는 경우) SpriteRenderer 아닌 Image 사용
    // ======================================================
    private SpriteRenderer spriteRenderer; 
    private Image uiImage;  // Canvas 상에서 Image 컴포넌트를 쓸 때 필요

    // ================================================
    // (추가) 합성시 별 등급별 테두리/라이팅 효과 재질
    // ================================================
    [Header("Material Settings For Star Effects")]
    [Tooltip("일반 SpriteRenderer용 (월드좌표 스프라이트)")]
    public Material baseMaterial;       
    public Material outlineMaterial;    
    public Material lightingMaterial;   

    [Tooltip("UI Image 전용 머티리얼 (All-in-1 Sprite Shader의 UI 버전)")]
    public Material baseMaterialUI;     
    public Material outlineMaterialUI;  
    public Material lightingMaterialUI; 

    /// <summary>
    /// 배치 시, PlacementManager가 bulletPanel을 할당해주는 용도
    /// </summary>
    public void SetBulletPanel(RectTransform panel)
    {
        bulletPanel = panel;
    }

    private void Awake()
    {
        // 1) SpriteRenderer 있는지 확인
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            // 2) 없으면 UI Image 컴포넌트 찾기
            uiImage = GetComponentInChildren<Image>();
        }
    }

    private void Start()
    {
        // 별 등급에 따른 능력치 보정
        switch (star)
        {
            case CharacterStar.OneStar:
                // 그대로
                break;
            case CharacterStar.TwoStar:
                attackPower *= 1.3f;
                attackRange *= 1.1f;
                attackSpeed *= 1.1f;
                break;
            case CharacterStar.ThreeStar:
                attackPower *= 1.6f;
                attackRange *= 1.2f;
                attackSpeed *= 1.2f;
                break;
        }

        // 공격속도 -> 쿨타임 계산
        attackCooldown = 1f / attackSpeed;

        // 사거리 표시용 원 생성
        CreateRangeIndicator();

        // 공격 루틴 시작
        StartCoroutine(AttackRoutine());

        // (추가) 현재 별 등급에 맞춰 머티리얼 적용
        ApplyStarVisual();
    }

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackCooldown);

            currentTarget = FindTargetInRange();
            if (currentTarget != null)
            {
                Attack(currentTarget);
            }
        }
    }

    private Monster FindTargetInRange()
    {
        GameObject[] monsterObjs = GameObject.FindGameObjectsWithTag("Monster");
        Monster nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (GameObject mo in monsterObjs)
        {
            Monster m = mo.GetComponent<Monster>();
            if (m == null) continue;

            float dist = Vector2.Distance(transform.position, m.transform.position);
            if (dist <= attackRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = m;
            }
        }
        return nearest;
    }

    private void Attack(Monster target)
    {
        if (target == null) return;

        // ---------------------------
        //  (1) 총알 프리팹 사용
        // ---------------------------
        if (bulletPrefab != null)
        {
            // bulletPrefab(프로젝트 자산) -> 런타임 인스턴스화
            GameObject bulletObj = Instantiate(bulletPrefab);

            // bulletPanel이 존재한다면, bulletObj를 그 자식으로 붙임
            if (bulletPanel != null && bulletPanel.gameObject.scene.IsValid())
            {
                bulletObj.transform.SetParent(bulletPanel, false);
            }
            else
            {
                Debug.LogWarning($"[Character] bulletPanel이 유효하지 않음. (bulletObj 단독 생성)");
            }

            // UI(RectTransform)로 좌표 잡기 (캔버스 상 총알)
            RectTransform bulletRect = bulletObj.GetComponent<RectTransform>();
            if (bulletRect != null && bulletPanel != null)
            {
                Vector2 localPos = bulletPanel.InverseTransformPoint(transform.position);
                bulletRect.anchoredPosition = localPos;
                bulletRect.localRotation = Quaternion.identity;
            }
            else
            {
                // 3D Transform(월드좌표) 경우
                bulletObj.transform.position = transform.position;
                bulletObj.transform.localRotation = Quaternion.identity;
            }

            // 초기값 세팅 (광역 공격 여부 + 범위 포함)
            Bullet bulletComp = bulletObj.GetComponent<Bullet>();
            if (bulletComp != null)
            {
                bulletComp.Init(target, attackPower, bulletSpeed, isAreaAttack, areaAttackRadius);
            }
        }
        else
        {
            // ---------------------------
            //  (2) 총알 프리팹이 없으면 즉시 공격
            // ---------------------------
            if (isAreaAttack)
            {
                // 광역 공격: 해당 target 위치를 기준으로 범위 내 모든 몬스터에 데미지
                DoAreaDamage(target.transform.position);
            }
            else
            {
                // 단일 공격
                target.TakeDamage(attackPower);
            }
        }
    }

    /// <summary>
    /// 광역 공격(즉시형)을 처리하는 메서드(총알 프리팹 없이 직접 타격하는 경우)
    /// </summary>
    private void DoAreaDamage(Vector3 centerPos)
    {
        // 모든 몬스터 검색
        GameObject[] monsterObjs = GameObject.FindGameObjectsWithTag("Monster");
        foreach (GameObject mo in monsterObjs)
        {
            Monster m = mo.GetComponent<Monster>();
            if (m == null) continue;

            float dist = Vector2.Distance(centerPos, m.transform.position);
            if (dist <= areaAttackRadius)
            {
                m.TakeDamage(attackPower);
            }
        }
        Debug.Log($"[Character] 광역 공격 발생! 범위={areaAttackRadius}, Damage={attackPower}");
    }

    private void CreateRangeIndicator()
    {
        if (!showRangeIndicator) return;
        if (rangeIndicatorPrefab == null) return;

        if (rangeIndicatorInstance != null)
        {
            Destroy(rangeIndicatorInstance);
            rangeIndicatorInstance = null;
        }

        rangeIndicatorInstance = Instantiate(rangeIndicatorPrefab, transform);
        rangeIndicatorInstance.name = "RangeIndicator";
        rangeIndicatorInstance.transform.localPosition = Vector3.zero;

        float diameter = attackRange * 2f;
        rangeIndicatorInstance.transform.localScale = new Vector3(diameter, diameter, 1f);
    }

    private void OnValidate()
    {
        if (rangeIndicatorInstance != null)
        {
            float diameter = attackRange * 2f;
            rangeIndicatorInstance.transform.localScale = new Vector3(diameter, diameter, 1f);
        }
    }

    /// <summary>
    /// (추가) 합성 결과로 1성→2성 / 2성→3성이 되었을 때,
    /// 각 등급에 맞는 머티리얼(테두리/라이팅) 적용
    /// + (UI 전용) Image 컴포넌트면 UI용 머티리얼을 적용.
    /// </summary>
    public void ApplyStarVisual()
    {
        // 1) SpriteRenderer를 사용하는 경우
        if (spriteRenderer != null)
        {
            switch (star)
            {
                case CharacterStar.OneStar:
                    if (baseMaterial != null)
                        spriteRenderer.material = baseMaterial;
                    break;

                case CharacterStar.TwoStar:
                    if (outlineMaterial != null)
                        spriteRenderer.material = outlineMaterial;
                    break;

                case CharacterStar.ThreeStar:
                    if (lightingMaterial != null)
                        spriteRenderer.material = lightingMaterial;
                    break;
            }
        }
        // 2) UI Image를 사용하는 경우
        else if (uiImage != null)
        {
            switch (star)
            {
                case CharacterStar.OneStar:
                    if (baseMaterialUI != null)
                        uiImage.material = baseMaterialUI;
                    break;

                case CharacterStar.TwoStar:
                    if (outlineMaterialUI != null)
                        uiImage.material = outlineMaterialUI;
                    break;

                case CharacterStar.ThreeStar:
                    if (lightingMaterialUI != null)
                        uiImage.material = lightingMaterialUI;
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"[Character] SpriteRenderer나 Image 컴포넌트를 찾지 못했습니다. 재질 적용 불가.");
        }
    }
}
