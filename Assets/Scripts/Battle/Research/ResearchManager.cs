using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Data;

namespace GuildMaster.Systems
{
    public class ResearchManager : MonoBehaviour
    {
        [Header("연구 설정")]
        public List<ResearchData> availableResearch = new List<ResearchData>();
        public List<string> completedResearch = new List<string>();
        public List<string> activeResearch = new List<string>();
        public int maxActiveResearch = 3;
        
        [Header("리소스")]
        public int researchPoints = 0;
        public int maxResearchPoints = 1000;
        public float researchPointsPerSecond = 1f;
        
        private static ResearchManager instance;
        public static ResearchManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ResearchManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("ResearchManager");
                        instance = go.AddComponent<ResearchManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            GenerateResearchPoints();
            UpdateActiveResearch();
        }
        
        private void GenerateResearchPoints()
        {
            if (researchPoints < maxResearchPoints)
            {
                researchPoints = Mathf.Min(maxResearchPoints, 
                    researchPoints + Mathf.RoundToInt(researchPointsPerSecond * Time.deltaTime));
            }
        }
        
        private void UpdateActiveResearch()
        {
            for (int i = activeResearch.Count - 1; i >= 0; i--)
            {
                var research = GetResearchData(activeResearch[i]);
                if (research != null && research.IsCompleted())
                {
                    CompleteResearch(research.researchId);
                }
            }
        }
        
        public bool CanStartResearch(string researchId)
        {
            var research = GetResearchData(researchId);
            if (research == null) return false;
            
            if (activeResearch.Count >= maxActiveResearch) return false;
            if (activeResearch.Contains(researchId)) return false;
            if (completedResearch.Contains(researchId)) return false;
            if (researchPoints < research.cost) return false;
            
            return research.ArePrerequisitesMet(completedResearch);
        }
        
        public bool StartResearch(string researchId)
        {
            if (!CanStartResearch(researchId)) return false;
            
            var research = GetResearchData(researchId);
            researchPoints -= research.cost;
            research.StartResearch();
            activeResearch.Add(researchId);
            
            Debug.Log($"연구 시작: {research.researchName}");
            return true;
        }
        
        public void CompleteResearch(string researchId)
        {
            var research = GetResearchData(researchId);
            if (research == null) return;
            
            activeResearch.Remove(researchId);
            completedResearch.Add(researchId);
            research.ApplyEffects();
            
            Debug.Log($"연구 완료: {research.researchName}");
        }
        
        public void CancelResearch(string researchId)
        {
            var research = GetResearchData(researchId);
            if (research == null || !activeResearch.Contains(researchId)) return;
            
            activeResearch.Remove(researchId);
            // 일부 리소스 반환 (50%)
            researchPoints += research.cost / 2;
            research.CancelResearch();
            
            Debug.Log($"연구 취소: {research.researchName}");
        }
        
        public ResearchData GetResearchData(string researchId)
        {
            return availableResearch.Find(r => r.researchId == researchId);
        }
        
        public List<ResearchData> GetAvailableResearch()
        {
            return availableResearch.Where(r => 
                !completedResearch.Contains(r.researchId) &&
                !activeResearch.Contains(r.researchId) &&
                r.ArePrerequisitesMet(completedResearch)
            ).ToList();
        }
        
        public List<ResearchData> GetActiveResearch()
        {
            return availableResearch.Where(r => activeResearch.Contains(r.researchId)).ToList();
        }
        
        public List<ResearchData> GetCompletedResearch()
        {
            return availableResearch.Where(r => completedResearch.Contains(r.researchId)).ToList();
        }
        
        public bool IsResearchCompleted(string researchId)
        {
            return completedResearch.Contains(researchId);
        }
        
        public float GetResearchProgress(string researchId)
        {
            var research = GetResearchData(researchId);
            if (research == null || !activeResearch.Contains(researchId)) return 0f;
            
            return research.GetProgress();
        }
        
        public void AddResearchPoints(int amount)
        {
            researchPoints = Mathf.Min(maxResearchPoints, researchPoints + amount);
        }
        
        public void IncreaseResearchSpeed(float multiplier)
        {
            researchPointsPerSecond *= multiplier;
        }
    }
    
    [System.Serializable]
    public class ResearchData
    {
        [Header("기본 정보")]
        public string researchId;
        public string researchName;
        public string description;
        public ResearchCategory category;
        
        [Header("비용 및 시간")]
        public int cost = 100;
        public float researchTime = 60f; // 초 단위
        public float startTime;
        
        [Header("요구사항")]
        public List<string> prerequisites = new List<string>();
        public int requiredLevel = 1;
        
        [Header("효과")]
        public List<ResearchEffect> effects = new List<ResearchEffect>();
        
        [Header("상태")]
        public bool isActive = false;
        
        public bool ArePrerequisitesMet(List<string> completedResearch)
        {
            return prerequisites.All(p => completedResearch.Contains(p));
        }
        
        public void StartResearch()
        {
            isActive = true;
            startTime = Time.time;
        }
        
        public void CancelResearch()
        {
            isActive = false;
            startTime = 0f;
        }
        
        public bool IsCompleted()
        {
            if (!isActive) return false;
            return Time.time >= startTime + researchTime;
        }
        
        public float GetProgress()
        {
            if (!isActive) return 0f;
            
            float elapsed = Time.time - startTime;
            return Mathf.Clamp01(elapsed / researchTime);
        }
        
        public void ApplyEffects()
        {
            foreach (var effect in effects)
            {
                effect.Apply();
            }
        }
    }
    
    [System.Serializable]
    public class ResearchEffect
    {
        public ResearchEffectType effectType;
        public string targetId;
        public float value;
        public string description;
        
        public void Apply()
        {
            // 실제 게임에서는 효과를 적용하는 로직 구현
            Debug.Log($"연구 효과 적용: {effectType} - {description}");
        }
    }
    
    public enum ResearchCategory
    {
        Military,
        Economic,
        Technology,
        Magic,
        Architecture,
        Agriculture,
        Medicine,
        Exploration
    }
    
    public enum ResearchEffectType
    {
        StatBonus,
        UnlockBuilding,
        UnlockUnit,
        UnlockSkill,
        ResourceBonus,
        ProductionBonus,
        CostReduction,
        TimeReduction
    }
} 