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
        public JobLevel[] jobLevels = new JobLevel[7];
        
        private void OnEnable()
        {
            InitializeJobLevels();
        }
        
        private void InitializeJobLevels()
        {
            if (jobLevels == null || jobLevels.Length != 7)
            {
                jobLevels = new JobLevel[7];
            }
            
            // 각 직업별로 초기화
            GuildMaster.Battle.JobClass[] allJobs = (GuildMaster.Battle.JobClass[])Enum.GetValues(typeof(GuildMaster.Battle.JobClass));
            for (int i = 0; i < allJobs.Length && i < 7; i++)
            {
                if (jobLevels[i] == null)
                {
                    jobLevels[i] = new JobLevel
                    {
                        jobClass = allJobs[i],
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