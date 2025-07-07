using UnityEngine;
using GuildMaster.Data;
using GuildMaster.Battle;
using Twelve.Data;
using System.Collections;
using System.Collections.Generic;
using System;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 직업별 자동 레벨업을 관리하는 시스템
    /// </summary>
    public class AutoLevelUpManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float goldAccumulationRate = 10f; // 초당 골드 축적량
        [SerializeField] private float updateInterval = 0.1f; // 업데이트 간격
        
        [Header("Data")]
        [SerializeField] private JobLevelData jobLevelData;
        
        // Events
        public static event Action<JobClass, int> OnJobLevelUp;
        public static event Action<JobClass, float> OnGoldAccumulated;
        public static event Action<JobClass, bool> OnAutoLevelUpToggled;
        
        private Coroutine autoLevelUpCoroutine;
        private float totalGold = 0f;
        
        private static AutoLevelUpManager instance;
        public static AutoLevelUpManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<AutoLevelUpManager>();
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            
            // JobLevelData가 없으면 Resources에서 로드
            if (jobLevelData == null)
            {
                jobLevelData = Resources.Load<JobLevelData>("JobLevelData");
                if (jobLevelData == null)
                {
                    Debug.LogError("JobLevelData not found! Please create one in Resources folder.");
                }
            }
        }
        
        private void Start()
        {
            StartAutoLevelUp();
        }
        
        private void OnDestroy()
        {
            if (autoLevelUpCoroutine != null)
            {
                StopCoroutine(autoLevelUpCoroutine);
            }
        }
        
        /// <summary>
        /// 자동 레벨업 프로세스 시작
        /// </summary>
        public void StartAutoLevelUp()
        {
            if (autoLevelUpCoroutine != null)
            {
                StopCoroutine(autoLevelUpCoroutine);
            }
            autoLevelUpCoroutine = StartCoroutine(AutoLevelUpProcess());
        }
        
        /// <summary>
        /// 자동 레벨업 프로세스
        /// </summary>
        private IEnumerator AutoLevelUpProcess()
        {
            while (true)
            {
                yield return new WaitForSeconds(updateInterval);
                
                // 골드 축적
                float goldToAdd = goldAccumulationRate * updateInterval;
                totalGold += goldToAdd;
                
                // 자동 레벨업이 활성화된 직업들 처리
                int enabledCount = jobLevelData.GetAutoLevelUpEnabledCount();
                if (enabledCount > 0)
                {
                    // 활성화된 직업들에게 골드를 균등 분배
                    float goldPerJob = goldToAdd / enabledCount;
                    
                    foreach (var jobLevel in jobLevelData.jobLevels)
                    {
                        if (jobLevel.autoLevelUpEnabled)
                        {
                            jobLevel.accumulatedGold += goldPerJob;
                            OnGoldAccumulated?.Invoke(jobLevel.jobClass, jobLevel.accumulatedGold);
                            
                            // 레벨업 가능한지 확인
                            while (jobLevel.CanLevelUp(jobLevel.accumulatedGold))
                            {
                                float cost = jobLevel.GetLevelUpCost();
                                if (jobLevel.accumulatedGold >= cost)
                                {
                                    // 레벨업 실행
                                    jobLevel.accumulatedGold -= cost;
                                    jobLevel.currentLevel++;
                                    
                                    OnJobLevelUp?.Invoke(jobLevel.jobClass, jobLevel.currentLevel);
                                    
                                    Debug.Log($"{jobLevel.jobClass} leveled up to {jobLevel.currentLevel}! (Cost: {cost:F2})");
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 특정 직업의 자동 레벨업 토글
        /// </summary>
        public void ToggleAutoLevelUp(JobClass jobClass)
        {
            var jobLevel = jobLevelData.GetJobLevel(jobClass);
            if (jobLevel != null)
            {
                jobLevel.autoLevelUpEnabled = !jobLevel.autoLevelUpEnabled;
                OnAutoLevelUpToggled?.Invoke(jobClass, jobLevel.autoLevelUpEnabled);
                
                Debug.Log($"{jobClass} auto level up: {jobLevel.autoLevelUpEnabled}");
            }
        }
        
        /// <summary>
        /// 특정 직업의 자동 레벨업 설정
        /// </summary>
        public void SetAutoLevelUp(JobClass jobClass, bool enabled)
        {
            var jobLevel = jobLevelData.GetJobLevel(jobClass);
            if (jobLevel != null)
            {
                jobLevel.autoLevelUpEnabled = enabled;
                OnAutoLevelUpToggled?.Invoke(jobClass, enabled);
            }
        }
        
        /// <summary>
        /// 모든 자동 레벨업 비활성화
        /// </summary>
        public void DisableAllAutoLevelUp()
        {
            foreach (var jobLevel in jobLevelData.jobLevels)
            {
                if (jobLevel.autoLevelUpEnabled)
                {
                    jobLevel.autoLevelUpEnabled = false;
                    OnAutoLevelUpToggled?.Invoke(jobLevel.jobClass, false);
                }
            }
        }
        
        /// <summary>
        /// 특정 직업의 현재 레벨 가져오기
        /// </summary>
        public int GetJobLevel(JobClass jobClass)
        {
            var jobLevel = jobLevelData.GetJobLevel(jobClass);
            return jobLevel != null ? jobLevel.currentLevel : 0;
        }
        
        /// <summary>
        /// 특정 직업의 누적 골드 가져오기
        /// </summary>
        public float GetAccumulatedGold(JobClass jobClass)
        {
            var jobLevel = jobLevelData.GetJobLevel(jobClass);
            return jobLevel != null ? jobLevel.accumulatedGold : 0f;
        }
        
        /// <summary>
        /// 특정 직업의 다음 레벨업 비용 가져오기
        /// </summary>
        public float GetNextLevelUpCost(JobClass jobClass)
        {
            var jobLevel = jobLevelData.GetJobLevel(jobClass);
            return jobLevel != null ? jobLevel.GetLevelUpCost() : 0f;
        }
        
        /// <summary>
        /// 총 골드 가져오기
        /// </summary>
        public float GetTotalGold()
        {
            return totalGold;
        }
        
        /// <summary>
        /// 골드 축적 속도 설정
        /// </summary>
        public void SetGoldAccumulationRate(float rate)
        {
            goldAccumulationRate = Mathf.Max(0f, rate);
        }
    }
}