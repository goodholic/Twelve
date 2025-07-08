using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 전투에 배치된 캐릭터 컴포넌트
    /// </summary>
    public class BattleCharacter : MonoBehaviour
    {
        [Header("캐릭터 정보")]
        [SerializeField] private CharacterData characterData;
        [SerializeField] private Tile.Team team;
        
        [Header("비주얼")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteRenderer teamIndicator;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private GameObject attackRangeIndicator;
        
        [Header("색상 설정")]
        [SerializeField] private Color allyColor = new Color(0.5f, 0.5f, 1f, 1f);
        [SerializeField] private Color enemyColor = new Color(1f, 0.5f, 0.5f, 1f);
        
        private AttackPattern attackPattern;
        
        /// <summary>
        /// 캐릭터 초기화
        /// </summary>
        public void Initialize(CharacterData data, Tile.Team characterTeam)
        {
            characterData = data;
            team = characterTeam;
            
            // 이름 설정
            if (nameText != null)
            {
                nameText.text = data.characterName;
            }
            
            // 팀 색상 설정
            if (teamIndicator != null)
            {
                teamIndicator.color = team == Tile.Team.Ally ? allyColor : enemyColor;
            }
            
            // 공격 패턴 설정 (직업별로 다르게)
            SetAttackPatternByJob();
            
            // 캐릭터 스프라이트 설정 (있다면)
            if (spriteRenderer != null && data.characterSprite != null)
            {
                spriteRenderer.sprite = data.characterSprite;
            }
        }
        
        /// <summary>
        /// 직업별 공격 패턴 설정
        /// </summary>
        private void SetAttackPatternByJob()
        {
            switch (characterData.jobClass)
            {
                case JobClass.Warrior:
                    // 전사: 십자형 패턴
                    attackPattern = AttackPattern.GetCrossPattern();
                    break;
                    
                case JobClass.Knight:
                    // 기사: 사각형 패턴 (주변 8칸)
                    attackPattern = AttackPattern.GetSquarePattern();
                    break;
                    
                case JobClass.Wizard:
                    // 마법사: 대각선 패턴
                    attackPattern = AttackPattern.GetDiagonalPattern();
                    break;
                    
                case JobClass.Priest:
                    // 성직자: 십자형 패턴
                    attackPattern = AttackPattern.GetCrossPattern();
                    break;
                    
                case JobClass.Rogue:
                    // 도적: 대각선 패턴
                    attackPattern = AttackPattern.GetDiagonalPattern();
                    break;
                    
                case JobClass.Sage:
                    // 현자: 사각형 패턴
                    attackPattern = AttackPattern.GetSquarePattern();
                    break;
                    
                case JobClass.Archer:
                    // 궁수: 직선 3칸
                    attackPattern = AttackPattern.GetLinePattern(3);
                    break;
                    
                case JobClass.Gunner:
                    // 총사: 직선 4칸
                    attackPattern = AttackPattern.GetLinePattern(4);
                    break;
                    
                default:
                    // 기본: 십자형
                    attackPattern = AttackPattern.GetCrossPattern();
                    break;
            }
        }
        
        /// <summary>
        /// 공격 패턴 반환
        /// </summary>
        public AttackPattern GetAttackPattern()
        {
            return attackPattern;
        }
        
        /// <summary>
        /// 공격 범위 표시
        /// </summary>
        public void ShowAttackRange(bool show)
        {
            if (attackRangeIndicator != null)
            {
                attackRangeIndicator.SetActive(show);
            }
        }
        
        /// <summary>
        /// 캐릭터 정보 반환
        /// </summary>
        public CharacterData GetCharacterData()
        {
            return characterData;
        }
        
        /// <summary>
        /// 팀 정보 반환
        /// </summary>
        public Tile.Team GetTeam()
        {
            return team;
        }
        
        /// <summary>
        /// 캐릭터 하이라이트
        /// </summary>
        public void SetHighlight(bool highlight)
        {
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = highlight ? 0.8f : 1f;
                spriteRenderer.color = color;
            }
        }
        
        /// <summary>
        /// 캐릭터가 클릭되었을 때
        /// </summary>
        private void OnMouseDown()
        {
            ShowAttackRange(true);
        }
        
        /// <summary>
        /// 마우스가 캐릭터를 벗어났을 때
        /// </summary>
        private void OnMouseExit()
        {
            ShowAttackRange(false);
        }
    }
}