using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using pjy.Managers;

namespace pjy.Gameplay
{
    /// <summary>
    /// 강화된 AI 두뇌 시스템 - 적응형 학습과 전략적 의사결정
    /// </summary>
    public class AIBrain : MonoBehaviour
    {
        [Header("AI Brain Settings")]
        [SerializeField] private float thinkingSpeed = 1f;
        [SerializeField] private float learningRate = 0.1f;
        [SerializeField] private float adaptationSpeed = 0.05f;
        
        [Header("Decision Weights (Dynamic)")]
        [SerializeField] private float summonWeight = 0.4f;
        [SerializeField] private float mergeWeight = 0.3f;
        [SerializeField] private float positioningWeight = 0.3f;
        [SerializeField] private float enhancementWeight = 0.2f;
        
        [Header("Advanced AI Features")]
        [SerializeField] private bool enableLearning = true;
        [SerializeField] private bool enableGachaDecisions = true;
        [SerializeField] private bool enableWaveRewardSelection = true;
        [SerializeField] private float riskTolerance = 0.5f;
        
        [Header("State Analysis")]
        [SerializeField] private int enemyCharacterCount;
        [SerializeField] private int allyCharacterCount;
        [SerializeField] private float threatLevel;
        [SerializeField] private float economicAdvantage;
        [SerializeField] private float currentWinRate;
        
        private AIPlayer aiPlayer;
        private Dictionary<string, float> characterEffectiveness = new Dictionary<string, float>();
        private List<DecisionRecord> decisionHistory = new List<DecisionRecord>();
        
        [System.Serializable]
        private class DecisionRecord
        {
            public float timestamp;
            public string decision;
            public float successScore;
        }
        
        public void Initialize(AIPlayer player)
        {
            aiPlayer = player;
            StartCoroutine(AnalyzeGameState());
        }
        
        private IEnumerator AnalyzeGameState()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f / thinkingSpeed);
                
                UpdateGameStateAnalysis();
                EvaluateThreats();
                UpdateDecisionWeights();
            }
        }
        
        private void UpdateGameStateAnalysis()
        {
            Character[] allCharacters = FindObjectsByType<Character>(FindObjectsSortMode.None);
            
            enemyCharacterCount = 0;
            allyCharacterCount = 0;
            
            foreach (Character character in allCharacters)
            {
                if (character.areaIndex == aiPlayer.AreaIndex)
                {
                    allyCharacterCount++;
                }
                else
                {
                    enemyCharacterCount++;
                }
            }
            
            economicAdvantage = (float)aiPlayer.Gold / 100f;
        }
        
        private void EvaluateThreats()
        {
            threatLevel = 0f;
            
            if (enemyCharacterCount > allyCharacterCount)
            {
                threatLevel += (enemyCharacterCount - allyCharacterCount) * 0.1f;
            }
            
            Monster[] monsters = FindObjectsByType<Monster>(FindObjectsSortMode.None);
            int nearbyMonsters = monsters.Count(m => m.areaIndex != aiPlayer.AreaIndex);
            threatLevel += nearbyMonsters * 0.05f;
            
            FinalCastle castle = FindObjectsByType<FinalCastle>(FindObjectsSortMode.None)
                .FirstOrDefault(c => c.areaIndex == aiPlayer.AreaIndex);
                
            if (castle != null)
            {
                float healthPercent = castle.currentHealth / castle.maxHealth;
                if (healthPercent < 0.5f)
                {
                    threatLevel += (0.5f - healthPercent) * 2f;
                }
            }
            
            threatLevel = Mathf.Clamp01(threatLevel);
        }
        
        private void UpdateDecisionWeights()
        {
            if (threatLevel > 0.7f)
            {
                // 위험 상황 - 즉시 방어력 강화
                summonWeight = 0.6f;
                mergeWeight = 0.1f;
                positioningWeight = 0.3f;
                enhancementWeight = 0.0f;
            }
            else if (threatLevel < 0.3f)
            {
                // 안전 상황 - 장기 전략 중심
                summonWeight = 0.2f;
                mergeWeight = 0.4f;
                positioningWeight = 0.2f;
                enhancementWeight = 0.2f;
            }
            else
            {
                // 균형 상황 - 기본 전략
                summonWeight = 0.4f;
                mergeWeight = 0.3f;
                positioningWeight = 0.2f;
                enhancementWeight = 0.1f;
            }
            
            // 학습 기반 가중치 조정
            if (enableLearning)
            {
                AdjustWeightsBasedOnHistory();
            }
        }

        /// <summary>
        /// 가챠 의사결정 (AI가 가챠를 뽑을지 판단)
        /// </summary>
        public bool ShouldDrawGacha()
        {
            if (!enableGachaDecisions || GachaManager.Instance == null)
                return false;
            
            // 경제 상황 분석
            int currentGold = GachaManager.Instance.GetCurrentGold();
            bool canAffordSingle = currentGold >= 100;
            bool canAffordTen = currentGold >= 900;
            
            // 캐릭터 부족 상황 분석
            bool needMoreCharacters = allyCharacterCount < 20;
            bool hasExcessGold = currentGold > 2000;
            
            // 위험 상황에서는 보수적 접근
            if (threatLevel > 0.6f)
            {
                return hasExcessGold && canAffordTen;
            }
            
            // 일반 상황에서의 가챠 결정
            float gachaDesire = 0f;
            
            if (needMoreCharacters) gachaDesire += 0.4f;
            if (hasExcessGold) gachaDesire += 0.3f;
            if (Random.value < riskTolerance) gachaDesire += 0.2f;
            
            return gachaDesire > 0.5f && (canAffordSingle || canAffordTen);
        }

        /// <summary>
        /// 웨이브 보상 캐릭터 선택 (AI가 3개 중 1개 선택)
        /// </summary>
        public int SelectWaveRewardCharacter(CharacterData[] options)
        {
            if (!enableWaveRewardSelection || options == null || options.Length == 0)
                return 0;
            
            float[] scores = new float[options.Length];
            
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] == null) continue;
                
                scores[i] = EvaluateCharacterValue(options[i]);
            }
            
            // 가장 높은 점수의 캐릭터 선택
            int bestIndex = 0;
            float bestScore = scores[0];
            
            for (int i = 1; i < scores.Length; i++)
            {
                if (scores[i] > bestScore)
                {
                    bestScore = scores[i];
                    bestIndex = i;
                }
            }
            
            Debug.Log($"[AIBrain] 웨이브 보상 선택: {options[bestIndex].characterName} (점수: {bestScore:F2})");
            return bestIndex;
        }

        /// <summary>
        /// 캐릭터 가치 평가
        /// </summary>
        private float EvaluateCharacterValue(CharacterData character)
        {
            float value = 0f;
            
            // 기본 스탯 가치
            value += character.attack * 0.1f;
            value += character.health * 0.05f;
            value += character.attackSpeed * 10f;
            
            // 등급 보너스
            value += character.starLevel * 20f;
            
            // 종족 시너지 고려
            value += EvaluateRaceSynergy(character.race);
            
            // 현재 보유 캐릭터와의 중복도 고려
            if (IsCharacterDuplicate(character))
            {
                value *= 0.7f; // 중복 시 가치 감소
            }
            
            // 학습된 효과도 반영
            if (characterEffectiveness.ContainsKey(character.characterName))
            {
                value *= (1f + characterEffectiveness[character.characterName]);
            }
            
            return value;
        }

        /// <summary>
        /// 종족 시너지 평가
        /// </summary>
        private float EvaluateRaceSynergy(CharacterRace race)
        {
            if (RaceSynergyManager.Instance == null) return 0f;
            
            // 현재 해당 종족 캐릭터 수 확인
            var currentRaceCounts = RaceSynergyManager.Instance.GetRaceCountsForPlayer(aiPlayer.PlayerID);
            
            int currentCount = 0;
            switch (race)
            {
                case CharacterRace.Human:
                    currentCount = currentRaceCounts.humanCount;
                    break;
                case CharacterRace.Orc:
                    currentCount = currentRaceCounts.orcCount;
                    break;
                case CharacterRace.Elf:
                    currentCount = currentRaceCounts.elfCount;
                    break;
            }
            
            // 시너지 임계점에 가까울수록 높은 점수
            float synergyValue = 0f;
            if (currentCount == 2 || currentCount == 4 || currentCount == 6 || currentCount == 8)
            {
                synergyValue = 15f; // 시너지 임계점 직전
            }
            else if (currentCount >= 3)
            {
                synergyValue = 5f; // 기본 시너지 활성
            }
            
            return synergyValue;
        }

        /// <summary>
        /// 캐릭터 중복 체크
        /// </summary>
        private bool IsCharacterDuplicate(CharacterData character)
        {
            if (CharacterInventoryManager.Instance == null) return false;
            
            return CharacterInventoryManager.Instance.IsCharacterOwned(character.characterID);
        }

        /// <summary>
        /// 역사 기반 가중치 조정
        /// </summary>
        private void AdjustWeightsBasedOnHistory()
        {
            if (decisionHistory.Count < 10) return;
            
            // 최근 10개 결정의 성공률 분석
            var recentDecisions = decisionHistory.TakeLast(10);
            
            foreach (var decision in recentDecisions)
            {
                if (decision.successScore > 0.7f)
                {
                    // 성공한 결정의 가중치 증가
                    AdjustWeightForDecision(decision.decision, adaptationSpeed);
                }
                else if (decision.successScore < 0.3f)
                {
                    // 실패한 결정의 가중치 감소
                    AdjustWeightForDecision(decision.decision, -adaptationSpeed);
                }
            }
        }

        /// <summary>
        /// 특정 결정의 가중치 조정
        /// </summary>
        private void AdjustWeightForDecision(string decision, float adjustment)
        {
            switch (decision)
            {
                case "summon":
                    summonWeight = Mathf.Clamp01(summonWeight + adjustment);
                    break;
                case "merge":
                    mergeWeight = Mathf.Clamp01(mergeWeight + adjustment);
                    break;
                case "position":
                    positioningWeight = Mathf.Clamp01(positioningWeight + adjustment);
                    break;
                case "enhance":
                    enhancementWeight = Mathf.Clamp01(enhancementWeight + adjustment);
                    break;
            }
            
            // 가중치 정규화
            NormalizeWeights();
        }

        /// <summary>
        /// 가중치 정규화
        /// </summary>
        private void NormalizeWeights()
        {
            float total = summonWeight + mergeWeight + positioningWeight + enhancementWeight;
            if (total > 0f)
            {
                summonWeight /= total;
                mergeWeight /= total;
                positioningWeight /= total;
                enhancementWeight /= total;
            }
        }

        /// <summary>
        /// 결정 기록 추가
        /// </summary>
        public void RecordDecision(string decision, float successScore)
        {
            decisionHistory.Add(new DecisionRecord
            {
                timestamp = Time.time,
                decision = decision,
                successScore = successScore
            });
            
            // 기록 크기 제한 (최대 100개)
            if (decisionHistory.Count > 100)
            {
                decisionHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// AI 성능 지표 업데이트
        /// </summary>
        public void UpdatePerformanceMetrics(bool gameWon)
        {
            if (enableLearning)
            {
                float newWinRate = gameWon ? 1f : 0f;
                currentWinRate = Mathf.Lerp(currentWinRate, newWinRate, learningRate);
                
                // 게임 결과에 따른 전체 전략 조정
                if (gameWon)
                {
                    // 승리 시 현재 전략 강화
                    ReinforceCurrentStrategy();
                }
                else
                {
                    // 패배 시 전략 다변화
                    DiversifyStrategy();
                }
            }
        }

        /// <summary>
        /// 현재 전략 강화
        /// </summary>
        private void ReinforceCurrentStrategy()
        {
            // 최근 성공적인 결정들의 가중치 증가
            var successfulDecisions = decisionHistory
                .Where(d => d.successScore > 0.6f)
                .TakeLast(5);
            
            foreach (var decision in successfulDecisions)
            {
                AdjustWeightForDecision(decision.decision, adaptationSpeed * 0.5f);
            }
        }

        /// <summary>
        /// 전략 다변화
        /// </summary>
        private void DiversifyStrategy()
        {
            // 가중치를 더 균등하게 조정
            float target = 0.25f;
            float adjustmentRate = adaptationSpeed * 0.3f;
            
            summonWeight = Mathf.Lerp(summonWeight, target, adjustmentRate);
            mergeWeight = Mathf.Lerp(mergeWeight, target, adjustmentRate);
            positioningWeight = Mathf.Lerp(positioningWeight, target, adjustmentRate);
            enhancementWeight = Mathf.Lerp(enhancementWeight, target, adjustmentRate);
            
            // 위험 감수성 조정
            riskTolerance = Mathf.Clamp01(riskTolerance + Random.Range(-0.1f, 0.1f));
        }

        /// <summary>
        /// AI 상태 정보 반환 (디버깅용)
        /// </summary>
        public string GetAIStatus()
        {
            return $"위협도: {threatLevel:F2}, 승률: {currentWinRate:F2}, " +
                   $"가중치(소환:{summonWeight:F2}, 합성:{mergeWeight:F2}, 배치:{positioningWeight:F2}, 강화:{enhancementWeight:F2})";
        }
        
        public void CheckAndPerformMerges()
        {
            List<Tile> occupiedTiles = aiPlayer.GetOccupiedTiles();
            
            foreach (Tile tile in occupiedTiles)
            {
                List<Character> characters = tile.GetOccupyingCharacters();
                if (characters.Count >= 3)
                {
                    Character first = characters[0];
                    bool canMerge = characters.All(c => 
                        c.characterName == first.characterName && 
                        c.star == first.star &&
                        first.star != CharacterStar.ThreeStar);
                        
                    if (canMerge)
                    {
                        AutoMergeManager mergeManager = FindFirstObjectByType<AutoMergeManager>();
                        if (mergeManager != null)
                        {
                            Character[] mergeTargets = characters.Take(3).ToArray();
                            mergeManager.MergeCharacters(mergeTargets);
                            
                            RecordDecision("Merge", 1f);
                            Debug.Log($"[AIBrain] Successfully merged 3x {first.characterName} {first.star}");
                        }
                    }
                }
            }
        }
        
        public CharacterData SelectBestCharacterToSummon(List<CharacterData> availableCharacters)
        {
            if (availableCharacters.Count == 0) return null;
            
            CharacterData bestChoice = null;
            float bestScore = -1f;
            
            foreach (CharacterData character in availableCharacters)
            {
                if (character.cost > aiPlayer.Gold) continue;
                
                float score = EvaluateCharacter(character);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestChoice = character;
                }
            }
            
            return bestChoice;
        }
        
        private float EvaluateCharacter(CharacterData character)
        {
            float score = 0f;
            
            float damageScore = character.attackPower / 100f;
            float healthScore = character.health / 1000f;
            float costEfficiency = (damageScore + healthScore) / (character.cost / 10f);
            score += costEfficiency * 0.4f;
            
            if (characterEffectiveness.ContainsKey(character.characterName))
            {
                score += characterEffectiveness[character.characterName] * 0.3f;
            }
            
            if (threatLevel > 0.5f)
            {
                score += damageScore * 0.3f;
            }
            else
            {
                score += healthScore * 0.3f;
            }
            
            return score;
        }
        
        public Tile SelectBestTile(List<Tile> availableTiles, CharacterData characterToPlace)
        {
            if (availableTiles.Count == 0) return null;
            
            Tile bestTile = null;
            float bestScore = -1f;
            
            foreach (Tile tile in availableTiles)
            {
                float score = EvaluateTile(tile, characterToPlace);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTile = tile;
                }
            }
            
            return bestTile;
        }
        
        private float EvaluateTile(Tile tile, CharacterData character)
        {
            float score = 0f;
            
            List<Character> occupyingChars = tile.GetOccupyingCharacters();
            if (occupyingChars.Count > 0 && occupyingChars.Count < 3)
            {
                Character first = occupyingChars[0];
                if (first.characterName == character.characterName && first.star == character.star)
                {
                    score += 0.5f;
                }
            }
            
            float xPos = tile.transform.position.x;
            if (aiPlayer.AreaIndex == 1)
            {
                if (threatLevel > 0.5f)
                {
                    score += (10f - xPos) / 20f;
                }
                else
                {
                    score += xPos / 20f;
                }
            }
            else
            {
                if (threatLevel > 0.5f)
                {
                    score += (xPos + 10f) / 20f;
                }
                else
                {
                    score += (10f - xPos) / 20f;
                }
            }
            
            return score;
        }
        
        public void OnCharacterSummoned(Character character)
        {
            string key = character.characterName;
            if (!characterEffectiveness.ContainsKey(key))
            {
                characterEffectiveness[key] = 0.5f;
            }
        }
        
        public void OnCharacterDeath(Character character)
        {
            string key = character.characterName;
            if (characterEffectiveness.ContainsKey(key))
            {
                float lifespan = character.gameObject ? Time.time : 0f;
                float effectiveness = Mathf.Clamp01(lifespan / 60f);
                
                characterEffectiveness[key] = Mathf.Lerp(
                    characterEffectiveness[key],
                    effectiveness,
                    learningRate
                );
            }
        }
        

        
        public float GetThreatLevel() => threatLevel;
        public float GetEconomicAdvantage() => economicAdvantage;
        public int GetEnemyCount() => enemyCharacterCount;
        public int GetAllyCount() => allyCharacterCount;
    }
}