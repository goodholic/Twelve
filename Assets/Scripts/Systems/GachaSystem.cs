using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Data;

namespace GuildMaster.Systems
{
    [System.Serializable]
    public class GachaSystem : MonoBehaviour
    {
        [Header("가챠 설정")]
        public List<GachaPool> gachaPools = new List<GachaPool>();
        public int singleDrawCost = 300;
        public int tenDrawCost = 2700;
        public bool guaranteedRareIn10Draws = true;
        
        [Header("확률 설정")]
        public float legendaryRate = 0.01f;
        public float epicRate = 0.05f;
        public float rareRate = 0.15f;
        public float uncommonRate = 0.30f;
        public float commonRate = 0.49f;
        
        [Header("천장 시스템")]
        public int hardPityCount = 90; // 90회 보장
        public int softPityStart = 75; // 75회부터 확률 증가
        public float softPityRateIncrease = 0.06f; // 매 뽑기마다 6% 증가
        
        [Header("픽업 시스템")]
        public float pickupRate = 0.5f; // 최고 등급 획득 시 50% 확률로 픽업 캐릭터
        
        // 천장 카운터 (풀별로 관리)
        private Dictionary<string, int> pityCounters = new Dictionary<string, int>();
        private Dictionary<string, List<GachaHistory>> gachaHistories = new Dictionary<string, List<GachaHistory>>();
        
        private static GachaSystem instance;
        public static GachaSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<GachaSystem>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("GachaSystem");
                        instance = go.AddComponent<GachaSystem>();
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
        
        public List<CharacterData> DrawSingle(string poolId = "default")
        {
            var pool = GetGachaPool(poolId);
            if (pool == null) return new List<CharacterData>();
            
            var result = new List<CharacterData>();
            var character = DrawCharacter(pool);
            if (character != null)
            {
                result.Add(character);
            }
            
            return result;
        }
        
        public List<CharacterData> DrawTen(string poolId = "default")
        {
            var pool = GetGachaPool(poolId);
            if (pool == null) return new List<CharacterData>();
            
            var results = new List<CharacterData>();
            bool hasRareOrAbove = false;
            
            // 9번 일반 뽑기
            for (int i = 0; i < 9; i++)
            {
                var character = DrawCharacter(pool);
                if (character != null)
                {
                    results.Add(character);
                    if (character.rarity >= CharacterRarity.Rare)
                        hasRareOrAbove = true;
                }
            }
            
            // 10번째는 레어 이상 보장 (설정에 따라)
            if (guaranteedRareIn10Draws && !hasRareOrAbove)
            {
                var character = DrawCharacter(pool, CharacterRarity.Rare);
                if (character != null)
                    results.Add(character);
            }
            else
            {
                var character = DrawCharacter(pool);
                if (character != null)
                    results.Add(character);
            }
            
            return results;
        }
        
        private CharacterData DrawCharacter(GachaPool pool, CharacterRarity minRarity = CharacterRarity.Common)
        {
            // 천장 카운터 증가
            if (!pityCounters.ContainsKey(pool.poolId))
                pityCounters[pool.poolId] = 0;
            pityCounters[pool.poolId]++;
            
            // 천장 시스템 적용
            var rarityWeights = GetRarityWeightsWithPity(pool.poolId, minRarity);
            var selectedRarity = SelectRarity(rarityWeights);
            
            CharacterData selectedCharacter = null;
            
            // 최고 등급인 경우 픽업 확률 적용
            if (selectedRarity == CharacterRarity.Legendary && pool.pickupCharacters.Count > 0)
            {
                if (Random.Range(0f, 1f) < pickupRate)
                {
                    // 픽업 캐릭터 중에서 선택
                    int randomIndex = Random.Range(0, pool.pickupCharacters.Count);
                    selectedCharacter = pool.pickupCharacters[randomIndex];
                }
            }
            
            // 일반 캐릭터 선택
            if (selectedCharacter == null)
            {
                var availableCharacters = pool.characters
                    .Where(c => c.rarity == selectedRarity)
                    .ToList();
                    
                if (availableCharacters.Count == 0)
                    return null;
                    
                int randomIndex = Random.Range(0, availableCharacters.Count);
                selectedCharacter = availableCharacters[randomIndex];
            }
            
            // 최고 등급 획득 시 천장 카운터 리셋
            if (selectedRarity == CharacterRarity.Legendary)
            {
                pityCounters[pool.poolId] = 0;
            }
            
            // 히스토리 기록
            AddToHistory(pool.poolId, selectedCharacter);
            
            return selectedCharacter;
        }
        
        private Dictionary<CharacterRarity, float> GetRarityWeights(CharacterRarity minRarity = CharacterRarity.Common)
        {
            var weights = new Dictionary<CharacterRarity, float>();
            
            if (minRarity <= CharacterRarity.Common)
                weights[CharacterRarity.Common] = commonRate;
            if (minRarity <= CharacterRarity.Uncommon)
                weights[CharacterRarity.Uncommon] = uncommonRate;
            if (minRarity <= CharacterRarity.Rare)
                weights[CharacterRarity.Rare] = rareRate;
            if (minRarity <= CharacterRarity.Epic)
                weights[CharacterRarity.Epic] = epicRate;
            if (minRarity <= CharacterRarity.Legendary)
                weights[CharacterRarity.Legendary] = legendaryRate;
                
            return weights;
        }
        
        private Dictionary<CharacterRarity, float> GetRarityWeightsWithPity(string poolId, CharacterRarity minRarity = CharacterRarity.Common)
        {
            var weights = GetRarityWeights(minRarity);
            
            if (!pityCounters.ContainsKey(poolId))
                pityCounters[poolId] = 0;
            
            int currentPity = pityCounters[poolId];
            
            // 하드 천장: 90회째는 무조건 최고 등급
            if (currentPity >= hardPityCount)
            {
                weights.Clear();
                weights[CharacterRarity.Legendary] = 1f;
                return weights;
            }
            
            // 소프트 천장: 75회부터 확률 증가
            if (currentPity >= softPityStart)
            {
                int pityBonus = currentPity - softPityStart + 1;
                float bonusRate = pityBonus * softPityRateIncrease;
                
                // 레전더리 확률 증가
                if (weights.ContainsKey(CharacterRarity.Legendary))
                {
                    weights[CharacterRarity.Legendary] += bonusRate;
                    
                    // 다른 등급 확률 감소
                    float reduction = bonusRate / (weights.Count - 1);
                    foreach (var rarity in weights.Keys.ToList())
                    {
                        if (rarity != CharacterRarity.Legendary)
                        {
                            weights[rarity] = Mathf.Max(0, weights[rarity] - reduction);
                        }
                    }
                }
            }
            
            return weights;
        }
        
        private CharacterRarity SelectRarity(Dictionary<CharacterRarity, float> weights)
        {
            float totalWeight = weights.Values.Sum();
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            
            foreach (var kvp in weights.OrderByDescending(x => x.Key))
            {
                currentWeight += kvp.Value;
                if (randomValue <= currentWeight)
                    return kvp.Key;
            }
            
            return CharacterRarity.Common;
        }
        
        private GachaPool GetGachaPool(string poolId)
        {
            return gachaPools.Find(p => p.poolId == poolId);
        }
        
        public bool CanDraw(int cost)
        {
            // 실제 게임에서는 플레이어의 재화를 확인
            return true;
        }
        
        public void AddGachaPool(GachaPool pool)
        {
            if (!gachaPools.Any(p => p.poolId == pool.poolId))
            {
                gachaPools.Add(pool);
            }
        }
        
        public void RemoveGachaPool(string poolId)
        {
            gachaPools.RemoveAll(p => p.poolId == poolId);
        }
        
        // 천장 카운터 조회
        public int GetPityCounter(string poolId)
        {
            return pityCounters.ContainsKey(poolId) ? pityCounters[poolId] : 0;
        }
        
        // 천장 카운터 리셋
        public void ResetPityCounter(string poolId)
        {
            if (pityCounters.ContainsKey(poolId))
                pityCounters[poolId] = 0;
        }
        
        // 가챠 히스토리 추가
        private void AddToHistory(string poolId, CharacterData character)
        {
            if (!gachaHistories.ContainsKey(poolId))
                gachaHistories[poolId] = new List<GachaHistory>();
            
            gachaHistories[poolId].Add(new GachaHistory
            {
                character = character,
                drawTime = System.DateTime.Now,
                pityCount = GetPityCounter(poolId)
            });
            
            // 최대 100개까지만 유지
            if (gachaHistories[poolId].Count > 100)
                gachaHistories[poolId].RemoveAt(0);
        }
        
        // 가챠 히스토리 조회
        public List<GachaHistory> GetHistory(string poolId)
        {
            return gachaHistories.ContainsKey(poolId) ? new List<GachaHistory>(gachaHistories[poolId]) : new List<GachaHistory>();
        }
        
        // 무료 뽑기 관리
        private Dictionary<string, System.DateTime> lastFreeDrawTimes = new Dictionary<string, System.DateTime>();
        
        public bool CanDrawFree(string poolId)
        {
            var pool = GetGachaPool(poolId);
            if (pool == null || !pool.hasDailyFree) return false;
            
            if (!lastFreeDrawTimes.ContainsKey(poolId))
                return true;
            
            var lastTime = lastFreeDrawTimes[poolId];
            var now = System.DateTime.Now;
            
            // 매일 오전 5시 리셋
            var resetTime = now.Date.AddHours(5);
            if (now.Hour < 5)
                resetTime = resetTime.AddDays(-1);
            
            return lastTime < resetTime;
        }
        
        public CharacterData DrawFree(string poolId)
        {
            if (!CanDrawFree(poolId))
                return null;
            
            lastFreeDrawTimes[poolId] = System.DateTime.Now;
            var results = DrawSingle(poolId);
            return results.Count > 0 ? results[0] : null;
        }
    }
    
    // 가챠 히스토리 클래스
    [System.Serializable]
    public class GachaHistory
    {
        public CharacterData character;
        public System.DateTime drawTime;
        public int pityCount;
    }
    
    [System.Serializable]
    public class GachaPool
    {
        public string poolId;
        public string poolName;
        public GachaPoolType poolType = GachaPoolType.Normal;
        public List<CharacterData> characters = new List<CharacterData>();
        public List<CharacterData> pickupCharacters = new List<CharacterData>(); // 픽업 캐릭터
        public bool isActive = true;
        public System.DateTime startTime;
        public System.DateTime endTime;
        public bool hasDailyFree = false; // 일일 무료 뽑기 여부
        
        public bool IsAvailable()
        {
            if (!isActive) return false;
            
            var now = System.DateTime.Now;
            return now >= startTime && now <= endTime;
        }
    }
    
    public enum GachaPoolType
    {
        Normal,     // 일반 배너
        Premium,    // 프리미엄 배너
        Limited,    // 한정 배너
        Free        // 무료 배너
    }
} 