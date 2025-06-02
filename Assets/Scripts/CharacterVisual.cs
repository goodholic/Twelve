using UnityEngine;
using UnityEngine.UI;

public class CharacterVisual : MonoBehaviour
{
    private Character character;
    
    // 사거리 표시용
    [Header("Range Indicator Settings")]
    public GameObject rangeIndicatorPrefab;
    public bool showRangeIndicator = true;
    private GameObject rangeIndicatorInstance;
    
    // 2성/3성 아웃라인 및 이펙트
    [Header("2성 표시")]
    public Color twoStarOutlineColor = Color.yellow;
    [Range(0f, 10f)] public float twoStarOutlineWidth = 1f;
    public GameObject twoStarEffectPrefab;
    
    [Header("3성 표시")]
    public Color threeStarOutlineColor = Color.cyan;
    [Range(0f, 10f)] public float threeStarOutlineWidth = 1.5f;
    public GameObject threeStarEffectPrefab;
    
    // 별 모양(Star) 프리팹
    [Header("별 모양(Star) 프리팹")]
    public GameObject star2Prefab;
    public GameObject star3Prefab;
    private GameObject starVisualInstance;
    
    // 캐릭터 머리 위 HP 바 표시 관련
    [Header("Overhead HP Bar")]
    [Tooltip("캐릭터 머리 위에 표시할 HP Bar용 Canvas(또는 UI)")]
    public Canvas hpBarCanvas;
    [Tooltip("HP Bar Fill Image (체력 비율)")]
    public Image hpFillImage;
    [Tooltip("HP 텍스트 (옵션)")]
    public Text hpText;
    
    // 위/아래 방향 캐릭터 스프라이트 (2.5등신 SD 스타일)
    [Header("2.5등신 SD 캐릭터 스프라이트")]
    public Sprite characterUpDirectionSprite;
    public Sprite characterDownDirectionSprite;
    public Sprite characterLeftDirectionSprite;
    public Sprite characterRightDirectionSprite;
    
    // 종족별 시각 효과
    [Header("종족별 시각 효과")]
    public GameObject humanEffectPrefab;
    public GameObject orcEffectPrefab;
    public GameObject elfEffectPrefab;
    private GameObject raceEffectInstance;
    
    // 드래그 가능 여부 표시
    [Header("드래그 가능 표시")]
    public GameObject draggableIndicatorPrefab;
    private GameObject draggableIndicatorInstance;
    
    public void Initialize(Character character)
    {
        this.character = character;
        
        // 히어로면 HP바 비활성화, 아니면 표시
        if (character.isHero)
        {
            if (hpBarCanvas != null)
            {
                hpBarCanvas.gameObject.SetActive(false);
            }
        }
        else
        {
            if (hpBarCanvas != null)
            {
                hpBarCanvas.gameObject.SetActive(true);
            }
            UpdateHpBar();
        }
        
        // 사거리 표시
        CreateRangeIndicator();
        
        // 2성/3성 별 표시
        ApplyStarVisual();
        
        // 종족별 효과 적용
        ApplyRaceVisual();
        
        // 드래그 가능 표시 (게임 기획서: 드래그로 라인 변경 가능)
        if (character.isDraggable && draggableIndicatorPrefab != null)
        {
            draggableIndicatorInstance = Instantiate(draggableIndicatorPrefab, transform);
            draggableIndicatorInstance.transform.localPosition = new Vector3(0, 0.5f, 0);
        }
    }
    
    public void CreateRangeIndicator()
    {
        if (!showRangeIndicator) return;
        if (rangeIndicatorPrefab == null) return;
        
        if (rangeIndicatorInstance != null)
        {
            Destroy(rangeIndicatorInstance);
        }
        
        rangeIndicatorInstance = Instantiate(rangeIndicatorPrefab, transform);
        rangeIndicatorInstance.name = "RangeIndicator";
        rangeIndicatorInstance.transform.localPosition = Vector3.zero;
        
        float diameter = character.attackRange * 2f;
        rangeIndicatorInstance.transform.localScale = new Vector3(diameter, diameter, 1f);
        
        // 사거리에 따른 색상 변경 (선택적)
        Image rangeRenderer = rangeIndicatorInstance.GetComponent<Image>();
        if (rangeRenderer != null)
        {
            switch (character.rangeType)
            {
                case RangeType.Melee:
                    rangeRenderer.color = new Color(1f, 0.5f, 0.5f, 0.3f); // 빨간색
                    break;
                case RangeType.Ranged:
                    rangeRenderer.color = new Color(0.5f, 1f, 0.5f, 0.3f); // 초록색
                    break;
                case RangeType.LongRange:
                    rangeRenderer.color = new Color(0.5f, 0.5f, 1f, 0.3f); // 파란색
                    break;
            }
        }
    }
    
    public void ApplyStarVisual()
    {
        if (starVisualInstance != null)
        {
            Destroy(starVisualInstance);
            starVisualInstance = null;
        }
        
        // 합성 시스템: 1성×3 → 2성, 2성×3 → 3성
        if (character.star == CharacterStar.OneStar)
        {
            // 1성은 특별한 효과 없음
            return;
        }
        else if (character.star == CharacterStar.TwoStar)
        {
            if (star2Prefab != null)
            {
                starVisualInstance = Instantiate(star2Prefab, transform);
                starVisualInstance.transform.localPosition = new Vector3(0, 0.8f, 0);
            }
            
            // 2성 이펙트 추가
            if (twoStarEffectPrefab != null)
            {
                GameObject effect = Instantiate(twoStarEffectPrefab, transform);
                effect.transform.localPosition = Vector3.zero;
            }
        }
        else if (character.star == CharacterStar.ThreeStar)
        {
            if (star3Prefab != null)
            {
                starVisualInstance = Instantiate(star3Prefab, transform);
                starVisualInstance.transform.localPosition = new Vector3(0, 0.8f, 0);
            }
            
            // 3성 이펙트 추가
            if (threeStarEffectPrefab != null)
            {
                GameObject effect = Instantiate(threeStarEffectPrefab, transform);
                effect.transform.localPosition = Vector3.zero;
            }
        }
    }
    
    public void ApplyRaceVisual()
    {
        if (raceEffectInstance != null)
        {
            Destroy(raceEffectInstance);
            raceEffectInstance = null;
        }
        
        // 종족별 시각 효과 적용
        GameObject effectPrefab = null;
        
        // characterData 대신 character.race 직접 사용
        switch (character.race)
        {
            case CharacterRace.Human:
                effectPrefab = humanEffectPrefab;
                break;
            case CharacterRace.Orc:
                effectPrefab = orcEffectPrefab;
                break;
            case CharacterRace.Elf:
                effectPrefab = elfEffectPrefab;
                break;
        }
        
        if (effectPrefab != null)
        {
            raceEffectInstance = Instantiate(effectPrefab, transform);
            raceEffectInstance.transform.localPosition = Vector3.zero;
        }
    }
    
    public void UpdateHpBar()
    {
        if (hpFillImage == null) return;
        float maxHP = character.GetMaxHP();
        float ratio = (maxHP <= 0f) ? 0f : (character.currentHP / maxHP);
        if (ratio < 0f) ratio = 0f;
        hpFillImage.fillAmount = ratio;
        
        // HP 텍스트 업데이트 (옵션)
        if (hpText != null)
        {
            hpText.text = $"{(int)character.currentHP}/{(int)maxHP}";
        }
    }
    
    public void UpdateCharacterDirectionSprite(CharacterMovement movement)
    {
        // 현재 이동(또는 타겟) 방향 계산
        Vector2 velocity = Vector2.zero;
        bool hasDirection = false;
        
        // 1. 웨이포인트 기반 이동 중이면
        if (movement.pathWaypoints != null && movement.currentWaypointIndex >= 0 && movement.currentWaypointIndex < movement.pathWaypoints.Length)
        {
            Transform target = movement.pathWaypoints[movement.currentWaypointIndex];
            if (target != null)
            {
                velocity = (target.position - transform.position);
                hasDirection = true;
            }
        }
        
        // 2. 공격 타겟이 있으면
        if (!hasDirection)
        {
            if (character.currentCharTarget != null)
            {
                velocity = (character.currentCharTarget.transform.position - transform.position);
                hasDirection = true;
            }
            else if (character.currentTarget != null)
            {
                velocity = (character.currentTarget.transform.position - transform.position);
                hasDirection = true;
            }
        }
        
        // 3. 방향에 따른 스프라이트 변경 (2.5등신 SD 스타일)
        if (!hasDirection) return;
        
        SpriteRenderer spriteRenderer = character.GetSpriteRenderer();
        Image uiImage = character.GetUIImage();
        
        if (spriteRenderer == null && uiImage == null) return;
        
        // 2성/3성 캐릭터의 특별 처리
        if (character.star == CharacterStar.TwoStar || character.star == CharacterStar.ThreeStar)
        {
            Transform frontImageObj = transform.Find("FrontImage");
            Transform backImageObj = transform.Find("BackImage");
            
            if (frontImageObj != null || backImageObj != null)
            {
                bool isMovingUp = velocity.y > 0f;
                if (frontImageObj != null)
                {
                    frontImageObj.gameObject.SetActive(isMovingUp);
                }
                if (backImageObj != null)
                {
                    backImageObj.gameObject.SetActive(!isMovingUp);
                }
                return;
            }
        }
        
        // 방향별 스프라이트 선택
        Sprite spriteToUse = null;
        float absX = Mathf.Abs(velocity.x);
        float absY = Mathf.Abs(velocity.y);
        
        if (absX > absY)
        {
            // 좌우 이동
            spriteToUse = velocity.x > 0 ? characterRightDirectionSprite : characterLeftDirectionSprite;
        }
        else
        {
            // 상하 이동
            spriteToUse = velocity.y > 0 ? characterUpDirectionSprite : characterDownDirectionSprite;
        }
        
        // 스프라이트 적용
        if (spriteToUse != null)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = spriteToUse;
            }
            else if (uiImage != null)
            {
                uiImage.sprite = spriteToUse;
            }
        }
    }
    
    private void OnValidate()
    {
        if (rangeIndicatorInstance != null && character != null)
        {
            float diameter = character.attackRange * 2f;
            rangeIndicatorInstance.transform.localScale = new Vector3(diameter, diameter, 1f);
            
            Image rangeRenderer = rangeIndicatorInstance.GetComponent<Image>();
            if (rangeRenderer != null)
            {
                switch (character.rangeType)
                {
                    case RangeType.Melee:
                        rangeRenderer.color = new Color(1f, 0.5f, 0.5f, 0.3f); // 빨간색
                        break;
                    case RangeType.Ranged:
                        rangeRenderer.color = new Color(0.5f, 1f, 0.5f, 0.3f); // 초록색
                        break;
                    case RangeType.LongRange:
                        rangeRenderer.color = new Color(0.5f, 0.5f, 1f, 0.3f); // 파란색
                        break;
                }
            }
        }
    }
    
    private void LateUpdate()
    {
        if (this == null || gameObject == null || character == null) return;
        
        // HP 바 위치 보정(머리 위에 표시)
        if (hpBarCanvas != null)
        {
            if (!character.isHero)
            {
                hpBarCanvas.gameObject.SetActive(true);
                
                if (hpBarCanvas.transform.parent == null)
                {
                    Vector3 offset = new Vector3(0f, 1.2f, 0f);
                    hpBarCanvas.transform.position = transform.position + offset;
                }
            }
        }
        
        // 드래그 가능 표시 애니메이션 (선택적)
        if (draggableIndicatorInstance != null)
        {
            float bobHeight = Mathf.Sin(Time.time * 2f) * 0.1f;
            draggableIndicatorInstance.transform.localPosition = new Vector3(0, 0.5f + bobHeight, 0);
        }
    }
    
    /// <summary>
    /// 캐릭터가 합성될 때 호출되는 효과
    /// </summary>
    public void PlayMergeEffect()
    {
        // 합성 효과 재생 (파티클, 사운드 등)
        Debug.Log($"[CharacterVisual] {character.characterName} 합성 효과 재생!");
    }
    
    private void OnDestroy()
    {
        if (rangeIndicatorInstance != null)
        {
            Destroy(rangeIndicatorInstance);
        }
        
        if (starVisualInstance != null)
        {
            Destroy(starVisualInstance);
        }
        
        if (raceEffectInstance != null)
        {
            Destroy(raceEffectInstance);
        }
        
        if (draggableIndicatorInstance != null)
        {
            Destroy(draggableIndicatorInstance);
        }
        
        rangeIndicatorInstance = null;
        starVisualInstance = null;
        raceEffectInstance = null;
        draggableIndicatorInstance = null;
        hpBarCanvas = null;
        hpFillImage = null;
        hpText = null;
    }
}