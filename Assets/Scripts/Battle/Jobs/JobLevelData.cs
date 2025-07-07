using UnityEngine;
using System;
using GuildMaster.Data;
using GuildMaster.Battle;

namespace Twelve.Data
{
    /// <summary>
    /// 직업별 레벨 데이터를 저장하는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "JobLevelData", menuName = "Twelve/Job Level Data")]
    public class JobLevelData : ScriptableObject
    {
        [Serializable]
        public class JobLevel
        {
            public GuildMaster.Battle.JobClass jobClass;
            public int currentLevel = 1;
            public int maxLevel = 100;
            public float levelUpCost = 50f; // 레벨업 비용
            public bool autoLevelUpEnabled = false; // 자동 레벨업 활성화 여부
            public float accumulatedGold = 0f; // 누적된 골드
            
            // 레벨업 비용 계산 (레벨이 오를수록 비용 증가)
            public float GetLevelUpCost()
            {
                return levelUpCost * Mathf.Pow(1.1f, currentLevel - 1);
            }
            
            // 레벨업 가능 여부 확인
            public bool CanLevelUp(float availableGold)
            {
                return currentLevel < maxLevel && availableGold >= GetLevelUpCost();
            }
        }
        
        [SerializeField]
        public JobLevel[] jobLevels = new JobLevel[8]; // 7개에서 8개로 변경 (Gunner 추가)
        
        private void OnEnable()
        {
            InitializeJobLevels();
        }
        
        private void InitializeJobLevels()
        {
            if (jobLevels == null || jobLevels.Length != 8)
            {
                jobLevels = new JobLevel[8];
            }
            
            // 각 직업별로 초기화 (Warrior, Knight, Mage, Priest, Rogue, Archer, Sage, Gunner)
            GuildMaster.Battle.JobClass[] mainJobs = new GuildMaster.Battle.JobClass[] 
            {
                GuildMaster.Battle.JobClass.Warrior,
                GuildMaster.Battle.JobClass.Knight,
                GuildMaster.Battle.JobClass.Mage,
                GuildMaster.Battle.JobClass.Priest,
                GuildMaster.Battle.JobClass.Rogue,
                GuildMaster.Battle.JobClass.Archer,
                GuildMaster.Battle.JobClass.Sage,
                GuildMaster.Battle.JobClass.Gunner
            };
            
            for (int i = 0; i < mainJobs.Length && i < 8; i++)
            {
                if (jobLevels[i] == null)
                {
                    jobLevels[i] = new JobLevel
                    {
                        jobClass = mainJobs[i],
                        currentLevel = 1,
                        maxLevel = 100,
                        levelUpCost = 50f,
                        autoLevelUpEnabled = false
                    };
                }
            }
        }
        
        // 특정 직업의 레벨 데이터 가져오기
        public JobLevel GetJobLevel(GuildMaster.Battle.JobClass jobClass)
        {
            foreach (var jobLevel in jobLevels)
            {
                if (jobLevel.jobClass == jobClass)
                {
                    return jobLevel;
                }
            }
            return null;
        }
        
        // 자동 레벨업이 활성화된 직업 수
        public int GetAutoLevelUpEnabledCount()
        {
            int count = 0;
            foreach (var jobLevel in jobLevels)
            {
                if (jobLevel.autoLevelUpEnabled)
                {
                    count++;
                }
            }
            return count;
        }
    }
}