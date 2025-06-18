using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace pjy.AI
{
    /// <summary>
    /// AI 학습 데이터 저장 및 관리
    /// ScriptableObject로 학습 데이터를 영구 저장
    /// </summary>
    [CreateAssetMenu(fileName = "AILearningData", menuName = "Twelve/AI/Learning Data")]
    public class AILearningData : ScriptableObject
    {
        [Header("학습 통계")]
        [SerializeField] private int totalGamesPlayed = 0;
        [SerializeField] private int totalWins = 0;
        [SerializeField] private int totalLosses = 0;
        [SerializeField] private float averageWinRate = 0f;
        
        [Header("전략 성공률")]
        [SerializeField] private List<StrategySuccessRate> strategySuccessRates = new List<StrategySuccessRate>();
        
        [Header("행동 패턴")]
        [SerializeField] private List<ActionPattern> learnedPatterns = new List<ActionPattern>();
        
        [Header("상대 프로필")]
        [SerializeField] private List<OpponentProfile> opponentProfiles = new List<OpponentProfile>();
        
        [Header("최적화된 파라미터")]
        [SerializeField] private OptimizedParameters optimizedParams = new OptimizedParameters();
        
        /// <summary>
        /// 게임 결과 기록
        /// </summary>
        public void RecordGameResult(bool won, float gameDuration, string strategyUsed)
        {
            totalGamesPlayed++;
            
            if (won)
            {
                totalWins++;
            }
            else
            {
                totalLosses++;
            }
            
            // 승률 업데이트
            averageWinRate = (float)totalWins / totalGamesPlayed;
            
            // 전략별 성공률 업데이트
            UpdateStrategySuccessRate(strategyUsed, won);
            
            // 데이터 저장
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
        
        /// <summary>
        /// 전략 성공률 업데이트
        /// </summary>
        private void UpdateStrategySuccessRate(string strategy, bool success)
        {
            var existingRate = strategySuccessRates.FirstOrDefault(s => s.strategyName == strategy);
            
            if (existingRate == null)
            {
                existingRate = new StrategySuccessRate { strategyName = strategy };
                strategySuccessRates.Add(existingRate);
            }
            
            existingRate.totalAttempts++;
            if (success)
            {
                existingRate.successCount++;
            }
            
            existingRate.successRate = (float)existingRate.successCount / existingRate.totalAttempts;
        }
        
        /// <summary>
        /// 새로운 패턴 학습
        /// </summary>
        public void LearnPattern(string pattern, float effectiveness)
        {
            var existingPattern = learnedPatterns.FirstOrDefault(p => p.pattern == pattern);
            
            if (existingPattern == null)
            {
                existingPattern = new ActionPattern 
                { 
                    pattern = pattern,
                    effectiveness = effectiveness,
                    occurrenceCount = 1
                };
                learnedPatterns.Add(existingPattern);
            }
            else
            {
                // 이동 평균으로 효과성 업데이트
                existingPattern.occurrenceCount++;
                existingPattern.effectiveness = 
                    (existingPattern.effectiveness * (existingPattern.occurrenceCount - 1) + effectiveness) 
                    / existingPattern.occurrenceCount;
            }
            
            // 효과성 기준으로 정렬
            learnedPatterns = learnedPatterns.OrderByDescending(p => p.effectiveness).ToList();
            
            // 상위 50개 패턴만 유지
            if (learnedPatterns.Count > 50)
            {
                learnedPatterns = learnedPatterns.Take(50).ToList();
            }
        }
        
        /// <summary>
        /// 상대 프로필 업데이트
        /// </summary>
        public void UpdateOpponentProfile(string opponentId, PlayerStrategy strategy, bool wonAgainst)
        {
            var profile = opponentProfiles.FirstOrDefault(p => p.playerId == opponentId);
            
            if (profile == null)
            {
                profile = new OpponentProfile 
                { 
                    playerId = opponentId,
                    preferredStrategy = strategy
                };
                opponentProfiles.Add(profile);
            }
            
            profile.gamesPlayed++;
            if (wonAgainst)
            {
                profile.winsAgainst++;
            }
            
            profile.winRateAgainst = (float)profile.winsAgainst / profile.gamesPlayed;
            
            // 전략 카운터 업데이트
            if (!profile.strategyCounters.ContainsKey(strategy.ToString()))
            {
                profile.strategyCounters[strategy.ToString()] = 0;
            }
            profile.strategyCounters[strategy.ToString()]++;
        }
        
        /// <summary>
        /// 최적 전략 가져오기
        /// </summary>
        public string GetBestStrategyAgainst(PlayerStrategy opponentStrategy)
        {
            var relevantRates = strategySuccessRates
                .Where(s => s.strategyName.Contains($"vs_{opponentStrategy}"))
                .OrderByDescending(s => s.successRate)
                .FirstOrDefault();
                
            return relevantRates?.strategyName ?? "Balanced";
        }
        
        /// <summary>
        /// 학습된 패턴 중 가장 효과적인 것 가져오기
        /// </summary>
        public List<ActionPattern> GetTopPatterns(int count = 5)
        {
            return learnedPatterns.Take(count).ToList();
        }
        
        /// <summary>
        /// 파라미터 최적화
        /// </summary>
        public void OptimizeParameters(float aggressiveness, float defensiveness, float economicFocus)
        {
            // 지수 이동 평균으로 파라미터 업데이트
            float alpha = 0.1f; // 학습률
            
            optimizedParams.optimalAggressiveness = 
                optimizedParams.optimalAggressiveness * (1 - alpha) + aggressiveness * alpha;
                
            optimizedParams.optimalDefensiveness = 
                optimizedParams.optimalDefensiveness * (1 - alpha) + defensiveness * alpha;
                
            optimizedParams.optimalEconomicFocus = 
                optimizedParams.optimalEconomicFocus * (1 - alpha) + economicFocus * alpha;
                
            optimizedParams.lastUpdateTime = System.DateTime.Now.ToString();
        }
        
        /// <summary>
        /// 학습 데이터 초기화
        /// </summary>
        public void ResetLearningData()
        {
            totalGamesPlayed = 0;
            totalWins = 0;
            totalLosses = 0;
            averageWinRate = 0f;
            
            strategySuccessRates.Clear();
            learnedPatterns.Clear();
            opponentProfiles.Clear();
            
            optimizedParams = new OptimizedParameters();
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
        
        /// <summary>
        /// 통계 요약 가져오기
        /// </summary>
        public string GetStatsSummary()
        {
            return $"총 게임: {totalGamesPlayed} | 승률: {averageWinRate:P} | " +
                   $"학습된 패턴: {learnedPatterns.Count} | 상대 프로필: {opponentProfiles.Count}";
        }
    }
    
    // 데이터 구조체들
    [System.Serializable]
    public class StrategySuccessRate
    {
        public string strategyName;
        public int totalAttempts;
        public int successCount;
        public float successRate;
    }
    
    [System.Serializable]
    public class ActionPattern
    {
        public string pattern;
        public float effectiveness;
        public int occurrenceCount;
        public string description;
    }
    
    [System.Serializable]
    public class OpponentProfile
    {
        public string playerId;
        public int gamesPlayed;
        public int winsAgainst;
        public float winRateAgainst;
        public PlayerStrategy preferredStrategy;
        public Dictionary<string, int> strategyCounters = new Dictionary<string, int>();
    }
    
    [System.Serializable]
    public class OptimizedParameters
    {
        public float optimalAggressiveness = 0.5f;
        public float optimalDefensiveness = 0.5f;
        public float optimalEconomicFocus = 0.5f;
        public float optimalDecisionInterval = 2f;
        public string lastUpdateTime;
    }
}