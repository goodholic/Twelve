using UnityEngine;
using System.Collections.Generic;
using TMPro;
using GuildMaster.Battle;
using GuildMaster.Data;

namespace TileConquest.Data
{
    /// <summary>
    /// 타일 전투용 캐릭터 데이터 ScriptableObject
    /// 기존 CharacterDataSO를 확장하여 공격 범위 정보 추가
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterTileData", menuName = "TileConquest/Character Tile Data", order = 3)]
    public class CharacterTileDataSO : ScriptableObject
    {
        [Header("기본 캐릭터 정보")]
        [Tooltip("기존 캐릭터 데이터 참조")]
        public CharacterDataSO baseCharacterData;
        
        [Header("공격 범위")]
        [Tooltip("캐릭터의 공격 범위 패턴")]
        public AttackRangeSO attackRange;
        
        [Header("타일 전투 특성")]
        [Tooltip("배치 우선순위 (AI용)")]
        [Range(0, 100)]
        public int placementPriority = 50;
        
        [Tooltip("공격력 보정치")]
        [Range(0.5f, 2.0f)]
        public float attackMultiplier = 1.0f;
        
        [Tooltip("특수 능력")]
        public List<TileAbility> specialAbilities = new List<TileAbility>();
        
        [Header("시너지 효과")]
        [Tooltip("같은 직업끼리 인접 시 보너스")]
        public bool hasJobSynergy = true;
        
        [Tooltip("시너지 보너스 비율")]
        [Range(0f, 0.5f)]
        public float synergyBonus = 0.1f;
        
        [Header("AI 힌트")]
        [Tooltip("AI가 이 캐릭터를 배치할 때 선호하는 위치")]
        public PreferredPosition aiPreferredPosition = PreferredPosition.Any;
        
        [Tooltip("AI가 이 캐릭터를 사용하는 전략")]
        public AIStrategy aiStrategy = AIStrategy.Balanced;
        
        /// <summary>
        /// 캐릭터의 실제 공격력 계산
        /// </summary>
        public int CalculateAttackPower(int adjacentAllies = 0)
        {
            if (baseCharacterData == null) return 0;
            
            float baseAttack = baseCharacterData.baseAttack;
            float finalAttack = baseAttack * attackMultiplier;
            
            // 시너지 보너스 적용
            if (hasJobSynergy && adjacentAllies > 0)
            {
                finalAttack *= (1 + synergyBonus * adjacentAllies);
            }
            
            return Mathf.RoundToInt(finalAttack);
        }
        
        /// <summary>
        /// 특정 위치에서의 공격 범위 계산
        /// </summary>
        public List<Vector2Int> GetAttackablePositions(Vector2Int position)
        {
            if (attackRange == null) return new List<Vector2Int>();
            return attackRange.GetAttackPositions(position);
        }
        
        /// <summary>
        /// 캐릭터 정보 문자열 반환
        /// </summary>
        public string GetCharacterInfo()
        {
            if (baseCharacterData == null) return "No character data";
            
            string info = $"{baseCharacterData.characterName}\n";
            info += $"직업: {baseCharacterData.jobClass}\n";
            info += $"공격 범위: {attackRange?.rangeName ?? "None"}\n";
            info += $"기본 공격력: {baseCharacterData.baseAttack}\n";
            
            if (specialAbilities.Count > 0)
            {
                info += "특수 능력:\n";
                foreach (var ability in specialAbilities)
                {
                    info += $"- {GetAbilityDescription(ability)}\n";
                }
            }
            
            return info;
        }
        
        private string GetAbilityDescription(TileAbility ability)
        {
            switch (ability)
            {
                case TileAbility.DoubleStrike:
                    return "이중 타격: 같은 위치를 두 번 공격";
                case TileAbility.Pierce:
                    return "관통: 적을 뚫고 지나감";
                case TileAbility.Splash:
                    return "스플래시: 주변 타일에도 피해";
                case TileAbility.Heal:
                    return "치유: 아군 회복";
                case TileAbility.Shield:
                    return "방어막: 아군 보호";
                case TileAbility.Boost:
                    return "강화: 아군 공격력 증가";
                default:
                    return "알 수 없는 능력";
            }
        }
    }
    
    /// <summary>
    /// 타일 특수 능력
    /// </summary>
    public enum TileAbility
    {
        None,
        DoubleStrike,   // 이중 타격
        Pierce,         // 관통
        Splash,         // 범위 피해
        Heal,           // 치유
        Shield,         // 방어막
        Boost           // 강화
    }
    
    /// <summary>
    /// AI 선호 위치
    /// </summary>
    public enum PreferredPosition
    {
        Any,            // 아무 곳
        Center,         // 중앙
        Edge,           // 가장자리
        Corner,         // 모서리
        Front,          // 전방
        Back            // 후방
    }
    
    /// <summary>
    /// AI 전략
    /// </summary>
    public enum AIStrategy
    {
        Aggressive,     // 공격적
        Defensive,      // 방어적
        Balanced,       // 균형
        Support,        // 지원
        Control         // 영역 제어
    }
}