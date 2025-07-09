using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using GuildMaster.Data;

/// <summary>
/// 뽑기 결과의 각 캐릭터 아이콘을 표시하는 UI 요소입니다.
/// </summary>
public class DrawResultIconUI : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private Image characterIcon;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI characterLevelText;
    [SerializeField] private TextMeshProUGUI characterStatsText;
    
    [Header("레어도 표시")]
    [SerializeField] private Image rarityBorder;
    [SerializeField] private GameObject[] starIcons; // 성급 표시용 별 아이콘들
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = Color.magenta;
    [SerializeField] private Color legendaryColor = Color.yellow;
    
    [Header("범위 공격 표시")]
    [SerializeField] private GameObject areaAttackIndicator;
    [SerializeField] private TextMeshProUGUI attackRangeText;
    
    /// <summary>
    /// UI 정보를 설정합니다.
    /// </summary>
    /// <param name="character">표시할 캐릭터 데이터</param>
    public void SetCharacterInfo(GuildMaster.Data.CharacterData character)
    {
        if (character == null) return;
        
        // 캐릭터 아이콘 설정
        if (characterIcon != null && character.buttonIcon != null)
        {
            characterIcon.sprite = character.buttonIcon;
        }
        
        // 이름 설정
        if (characterNameText != null)
        {
            characterNameText.text = character.characterName;
        }
        
        // 레벨 설정
        if (characterLevelText != null)
        {
            characterLevelText.text = $"Lv.{character.level}";
        }
        
        // 스탯 표시
        if (characterStatsText != null)
        {
            characterStatsText.text = $"공격력: {character.attackPower}\nHP: {character.maxHP}";
        }
        
        // 성급 표시
        if (starIcons != null && starIcons.Length > 0)
        {
            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] != null)
                {
                    starIcons[i].SetActive(i < character.starLevel);
                }
            }
        }
        
        // 레어도 테두리 색상 설정
        if (rarityBorder != null)
        {
            switch (character.starLevel)
            {
                case 4:
                    rarityBorder.color = legendaryColor;
                    break;
                case 3:
                    rarityBorder.color = epicColor;
                    break;
                case 2:
                    rarityBorder.color = rareColor;
                    break;
                default:
                    rarityBorder.color = normalColor;
                    break;
            }
        }
        
        // 범위 공격 표시
        if (areaAttackIndicator != null)
        {
            areaAttackIndicator.SetActive(character.isAreaAttack);
        }
        
        if (attackRangeText != null && character.isAreaAttack)
        {
            attackRangeText.text = $"범위: {character.areaAttackRadius:F1}";
        }
    }
    
    /// <summary>
    /// 획득 애니메이션 재생
    /// </summary>
    public void PlayAcquireAnimation()
    {
        StartCoroutine(AcquireAnimationCoroutine());
    }
    
    private IEnumerator AcquireAnimationCoroutine()
    {
        // 크기 애니메이션
        Vector3 originalScale = transform.localScale;
        transform.localScale = Vector3.zero;
        
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(0f, 1f, t);
            transform.localScale = originalScale * scale;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
}