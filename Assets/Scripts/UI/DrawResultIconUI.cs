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
    
    [Header("레어도 표시")]
    [SerializeField] private Image rarityBorder;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = Color.magenta;
    [SerializeField] private Color legendaryColor = Color.yellow;
    
    /// <summary>
    /// UI 정보를 설정합니다.
    /// </summary>
    /// <param name="character">표시할 캐릭터 데이터</param>
    public void SetCharacterInfo(GuildMaster.Data.CharacterData character)
    {
        if (character == null) return;
        
        // 캐릭터 아이콘 설정
        if (character != null && character.buttonIcon != null)
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
        
        // 레어도 테두리 색상 설정
        if (rarityBorder != null)
        {
            // 예시: 공격력이 높을수록 레어함
            if (character.attackPower >= 50)
                rarityBorder.color = legendaryColor;
            else if (character.attackPower >= 30)
                rarityBorder.color = epicColor;
            else if (character.attackPower >= 20)
                rarityBorder.color = rareColor;
            else
                rarityBorder.color = normalColor;
        }
    }
} 