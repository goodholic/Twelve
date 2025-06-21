using UnityEngine;
using System.Collections.Generic;

namespace pjy.Data
{
    /// <summary>
    /// 캐릭터 스킬 데이터베이스
    /// - 스킬 정보 관리
    /// - CSV 연동 지원
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterSkillDatabase", menuName = "pjy/Data/Character Skill Database")]
    public class CharacterSkillDatabase : ScriptableObject
    {
        [Header("스킬 목록")]
        [SerializeField] private List<SkillData> skills = new List<SkillData>();
        
        /// <summary>
        /// 스킬 ID로 스킬 데이터 가져오기
        /// </summary>
        public SkillData GetSkill(string skillId)
        {
            return skills.Find(s => s.skillId == skillId);
        }
        
        /// <summary>
        /// 모든 스킬 가져오기
        /// </summary>
        public List<SkillData> GetAllSkills()
        {
            return new List<SkillData>(skills);
        }
        
        /// <summary>
        /// 스킬 추가
        /// </summary>
        public void AddSkill(SkillData skill)
        {
            if (skill != null && !skills.Contains(skill))
            {
                skills.Add(skill);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
        
        /// <summary>
        /// 스킬 제거
        /// </summary>
        public void RemoveSkill(SkillData skill)
        {
            if (skills.Contains(skill))
            {
                skills.Remove(skill);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
    }

    /// <summary>
    /// 스킬 데이터
    /// </summary>
    [System.Serializable]
    public class SkillData
    {
        [Header("기본 정보")]
        public string skillId;
        public string skillName;
        public string description;
        public SkillType skillType;
        
        [Header("효과")]
        public float damage;
        public float healAmount;
        public float buffAmount;
        public float duration;
        public float cooldown;
        public float range;
        
        [Header("대상")]
        public SkillTargetType targetType;
        public int maxTargets = 1;
        
        [Header("시각 효과")]
        public GameObject effectPrefab;
        public string animationTrigger;
        public AudioClip soundEffect;
    }

    /// <summary>
    /// 스킬 타입
    /// </summary>
    public enum SkillType
    {
        Attack,         // 공격 스킬
        Heal,           // 치유 스킬
        Buff,           // 버프 스킬
        Debuff,         // 디버프 스킬
        Utility,        // 유틸리티 스킬
        Passive         // 패시브 스킬
    }

    /// <summary>
    /// 스킬 대상 타입
    /// </summary>
    public enum SkillTargetType
    {
        Self,           // 자신
        Enemy,          // 적
        Ally,           // 아군
        Area,           // 범위
        All             // 전체
    }
}