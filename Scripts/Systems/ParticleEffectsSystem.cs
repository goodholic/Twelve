using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuildMaster.Systems
{
    public class ParticleEffectsSystem : MonoBehaviour
    {
        private static ParticleEffectsSystem _instance;
        public static ParticleEffectsSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ParticleEffectsSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ParticleEffectsSystem");
                        _instance = go.AddComponent<ParticleEffectsSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        [System.Serializable]
        public class ParticleEffectData
        {
            public string effectId;
            public string effectName;
            public GameObject particlePrefab;
            public EffectCategory category;
            public float duration = 2f;
            public bool autoReturn = true;
            public float scale = 1f;
            public bool attachToTarget = false;
            public Vector3 offset = Vector3.zero;
        }
        
        public enum EffectCategory
        {
            Combat,         // 전투 효과
            Skill,          // 스킬 효과
            UI,             // UI 효과
            Building,       // 건설 효과
            Environment,    // 환경 효과
            Status,         // 상태 효과
            Celebration     // 축하 효과
        }
        
        [System.Serializable]
        public class CombatEffect
        {
            public string effectId;
            public GameObject prefab;
            public float lifeTime = 1f;
            public bool followTarget = false;
            public Vector3 localPosition = Vector3.zero;
            public Vector3 localRotation = Vector3.zero;
            public Vector3 localScale = Vector3.one;
        }
        
        [Header("Effect Libraries")]
        [SerializeField] private List<ParticleEffectData> effectLibrary;
        [SerializeField] private List<CombatEffect> combatEffects;
        
        [Header("Effect Pools")]
        [SerializeField] private Transform effectPoolParent;
        [SerializeField] private int poolSizePerEffect = 5;
        
        private Dictionary<string, ParticleEffectData> effectDictionary;
        private Dictionary<string, Queue<GameObject>> effectPools;
        private Dictionary<string, List<GameObject>> activeEffects;
        
        // 텍스트 메시 프로 풀 (데미지 텍스트용)
        private Queue<GameObject> damageTextPool;
        [SerializeField] private GameObject damageTextPrefab;
        
        [Header("Effect Prefabs")]
        [SerializeField] private GameObject[] effectPrefabs;
        
        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
            
            Initialize();
        }
        
        void Initialize()
        {
            effectDictionary = new Dictionary<string, ParticleEffectData>();
            effectPools = new Dictionary<string, Queue<GameObject>>();
            activeEffects = new Dictionary<string, List<GameObject>>();
            damageTextPool = new Queue<GameObject>();
            
            if (effectPoolParent == null)
            {
                GameObject poolParent = new GameObject("EffectPool");
                poolParent.transform.SetParent(transform);
                effectPoolParent = poolParent.transform;
            }
            
            // 기본 효과 추가
            AddDefaultEffects();
            
            // 효과 라이브러리 초기화
            InitializeEffectLibrary();
            
            // 오브젝트 풀 생성
            CreateEffectPools();
        }
        
        void AddDefaultEffects()
        {
            // 전투 효과
            AddEffect("hit_physical", "Physical Hit", EffectCategory.Combat, 0.5f);
            AddEffect("hit_magical", "Magical Hit", EffectCategory.Combat, 0.8f);
            AddEffect("hit_critical", "Critical Hit", EffectCategory.Combat, 1f);
            AddEffect("block_shield", "Shield Block", EffectCategory.Combat, 0.6f);
            AddEffect("dodge_effect", "Dodge Effect", EffectCategory.Combat, 0.4f);
            
            // 스킬 효과
            AddEffect("skill_heal", "Heal Effect", EffectCategory.Skill, 1.5f);
            AddEffect("skill_buff", "Buff Effect", EffectCategory.Skill, 2f, true);
            AddEffect("skill_debuff", "Debuff Effect", EffectCategory.Skill, 2f, true);
            AddEffect("skill_aoe", "AOE Effect", EffectCategory.Skill, 2f);
            AddEffect("skill_ultimate", "Ultimate Skill", EffectCategory.Skill, 3f);
            
            // UI 효과
            AddEffect("ui_sparkle", "UI Sparkle", EffectCategory.UI, 1f);
            AddEffect("ui_glow", "UI Glow", EffectCategory.UI, 2f);
            AddEffect("ui_levelup", "Level Up", EffectCategory.UI, 2f);
            AddEffect("ui_reward", "Reward Effect", EffectCategory.UI, 1.5f);
            
            // 건설 효과
            AddEffect("build_smoke", "Build Smoke", EffectCategory.Building, 2f);
            AddEffect("build_complete", "Build Complete", EffectCategory.Building, 3f);
            AddEffect("build_upgrade", "Upgrade Effect", EffectCategory.Building, 2f);
            
            // 환경 효과
            AddEffect("env_dust", "Dust Particles", EffectCategory.Environment, 0f, false);
            AddEffect("env_leaves", "Falling Leaves", EffectCategory.Environment, 0f, false);
            AddEffect("env_fireflies", "Fireflies", EffectCategory.Environment, 0f, false);
            
            // 축하 효과
            AddEffect("celebration_confetti", "Confetti", EffectCategory.Celebration, 3f);
            AddEffect("celebration_firework", "Firework", EffectCategory.Celebration, 4f);
        }
        
        void AddEffect(string id, string name, EffectCategory category, float duration, bool attachToTarget = false)
        {
            var effectData = new ParticleEffectData
            {
                effectId = id,
                effectName = name,
                category = category,
                duration = duration,
                autoReturn = duration > 0,
                attachToTarget = attachToTarget
            };
            
            if (effectLibrary == null)
                effectLibrary = new List<ParticleEffectData>();
                
            effectLibrary.Add(effectData);
        }
        
        void InitializeEffectLibrary()
        {
            foreach (var effect in effectLibrary)
            {
                if (!string.IsNullOrEmpty(effect.effectId))
                {
                    effectDictionary[effect.effectId] = effect;
                    activeEffects[effect.effectId] = new List<GameObject>();
                }
            }
        }
        
        void CreateEffectPools()
        {
            foreach (var effect in effectDictionary.Values)
            {
                if (effect.particlePrefab != null)
                {
                    CreatePool(effect.effectId, effect.particlePrefab, poolSizePerEffect);
                }
            }
            
            // 데미지 텍스트 풀 생성
            if (damageTextPrefab != null)
            {
                for (int i = 0; i < 20; i++)
                {
                    GameObject textObj = Instantiate(damageTextPrefab, effectPoolParent);
                    textObj.SetActive(false);
                    damageTextPool.Enqueue(textObj);
                }
            }
        }
        
        void CreatePool(string effectId, GameObject prefab, int poolSize)
        {
            if (!effectPools.ContainsKey(effectId))
            {
                effectPools[effectId] = new Queue<GameObject>();
            }
            
            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = Instantiate(prefab, effectPoolParent);
                obj.SetActive(false);
                obj.name = $"{effectId}_pooled_{i}";
                effectPools[effectId].Enqueue(obj);
            }
        }
        
        // 이펙트 재생
        public GameObject PlayEffect(string effectName, Vector3 position, Transform parent = null)
        {
            if (string.IsNullOrEmpty(effectName))
            {
                Debug.LogWarning("Effect name is null or empty");
                return null;
            }

            GameObject effectPrefab = GetEffectPrefab(effectName);
            if (effectPrefab == null)
            {
                Debug.LogWarning($"Effect prefab '{effectName}' not found");
                return null;
            }

            GameObject instance = Instantiate(effectPrefab, position, Quaternion.identity, parent);
            
            // 자동 파괴 설정
            ParticleSystem particleSystem = instance.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                float duration = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
                Destroy(instance, duration);
            }
            else
            {
                // ParticleSystem이 없으면 기본 시간 후 파괴
                Destroy(instance, 5f);
            }

            return instance;
        }

        GameObject GetEffectPrefab(string effectName)
        {
            if (effectPrefabs == null) return null;
            
            foreach (var effect in effectPrefabs)
            {
                if (effect != null && effect.name == effectName)
                {
                    return effect;
                }
            }
            return null;
        }
        
        // 특정 대상에 이펙트 재생
        public GameObject PlayEffectOnTarget(string effectId, Transform target, Vector3 offset = default)
        {
            if (target == null) 
            {
                Debug.LogWarning("Target is null for effect: " + effectId);
                return null;
            }
            
            return PlayEffect(effectId, target.position + offset, target);
        }
        
        // 데미지 텍스트 표시
        public void ShowDamageText(Vector3 position, int damage, DamageType type = DamageType.Normal)
        {
            GameObject textObj = GetDamageTextFromPool();
            if (textObj == null) return;
            
            textObj.transform.position = position;
            textObj.SetActive(true);
            
            // TMPro 텍스트 설정
            var tmp = textObj.GetComponent<TMPro.TextMeshPro>();
            if (tmp != null)
            {
                tmp.text = damage.ToString();
                
                // 타입별 색상 설정
                switch (type)
                {
                    case DamageType.Critical:
                        tmp.color = Color.red;
                        tmp.fontSize = 36;
                        break;
                    case DamageType.Heal:
                        tmp.color = Color.green;
                        tmp.text = "+" + damage;
                        break;
                    case DamageType.Magic:
                        tmp.color = new Color(0.5f, 0, 1f); // 보라색
                        break;
                    default:
                        tmp.color = Color.white;
                        tmp.fontSize = 24;
                        break;
                }
            }
            
            // 애니메이션
            StartCoroutine(AnimateDamageText(textObj));
        }
        
        public enum DamageType
        {
            Normal,
            Critical,
            Magic,
            Heal
        }
        
        IEnumerator AnimateDamageText(GameObject textObj)
        {
            float duration = 1.5f;
            float timer = 0;
            Vector3 startPos = textObj.transform.position;
            
            var tmp = textObj.GetComponent<TMPro.TextMeshPro>();
            Color startColor = tmp.color;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                
                // 위로 올라가면서 페이드 아웃
                textObj.transform.position = startPos + Vector3.up * (t * 2f);
                
                if (tmp != null)
                {
                    Color newColor = startColor;
                    newColor.a = 1f - t;
                    tmp.color = newColor;
                }
                
                yield return null;
            }
            
            textObj.SetActive(false);
            damageTextPool.Enqueue(textObj);
        }
        
        GameObject GetDamageTextFromPool()
        {
            if (damageTextPool.Count > 0)
            {
                return damageTextPool.Dequeue();
            }
            
            if (damageTextPrefab != null)
            {
                GameObject newText = Instantiate(damageTextPrefab, effectPoolParent);
                return newText;
            }
            
            return null;
        }
        
        // 스킬 이펙트 체인
        public void PlaySkillEffectChain(List<string> effectIds, Vector3 position, float delayBetween = 0.5f)
        {
            StartCoroutine(PlayEffectChain(effectIds, position, delayBetween));
        }
        
        IEnumerator PlayEffectChain(List<string> effectIds, Vector3 position, float delay)
        {
            foreach (string effectId in effectIds)
            {
                PlayEffect(effectId, position);
                yield return new WaitForSeconds(delay);
            }
        }
        
        // 범위 이펙트
        public void PlayAreaEffect(string effectId, Vector3 center, float radius, int count = 5)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i;
                Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
                PlayEffect(effectId, center + offset);
            }
        }
        
        // 궤적 이펙트
        public void PlayTrailEffect(string effectId, Vector3 start, Vector3 end, float duration = 0.5f)
        {
            GameObject effectObj = PlayEffect(effectId, start);
            if (effectObj != null)
            {
                StartCoroutine(MoveEffect(effectObj, start, end, duration));
            }
        }
        
        IEnumerator MoveEffect(GameObject effectObj, Vector3 start, Vector3 end, float duration)
        {
            float timer = 0;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                
                // 포물선 궤적
                Vector3 currentPos = Vector3.Lerp(start, end, t);
                currentPos.y += Mathf.Sin(t * Mathf.PI) * 2f;
                
                effectObj.transform.position = currentPos;
                yield return null;
            }
            
            effectObj.transform.position = end;
        }
        
        // 지속 이펙트 관리
        public GameObject StartPersistentEffect(string effectId, Transform target)
        {
            GameObject effect = PlayEffectOnTarget(effectId, target);
            return effect;
        }
        
        public void StopPersistentEffect(GameObject effectObj)
        {
            if (effectObj == null) return;
            
            var particleSystems = effectObj.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                ps.Stop();
            }
            
            StartCoroutine(DelayedReturn(effectObj, 2f));
        }
        
        IEnumerator DelayedReturn(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnEffectToPool(obj);
        }
        
        // 풀 관리
        GameObject GetFromPool(string effectId)
        {
            if (effectPools.ContainsKey(effectId) && effectPools[effectId].Count > 0)
            {
                return effectPools[effectId].Dequeue();
            }
            return null;
        }
        
        IEnumerator ReturnToPool(string effectId, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnEffectToPool(obj);
        }
        
        void ReturnEffectToPool(GameObject obj)
        {
            if (obj == null) return;
            
            obj.SetActive(false);
            obj.transform.SetParent(effectPoolParent);
            
            // 해당 이펙트 ID 찾기
            string effectId = null;
            foreach (var kvp in activeEffects)
            {
                if (kvp.Value.Contains(obj))
                {
                    kvp.Value.Remove(obj);
                    effectId = kvp.Key;
                    break;
                }
            }
            
            if (effectId != null && effectPools.ContainsKey(effectId))
            {
                effectPools[effectId].Enqueue(obj);
            }
        }
        
        // 모든 이펙트 정지
        public void StopAllEffects()
        {
            foreach (var effectList in activeEffects.Values)
            {
                foreach (var effect in effectList.ToArray())
                {
                    ReturnEffectToPool(effect);
                }
            }
        }
        
        // 특정 카테고리 이펙트 정지
        public void StopEffectsByCategory(EffectCategory category)
        {
            foreach (var kvp in effectDictionary)
            {
                if (kvp.Value.category == category)
                {
                    var activeList = activeEffects[kvp.Key];
                    foreach (var effect in activeList.ToArray())
                    {
                        ReturnEffectToPool(effect);
                    }
                }
            }
        }
        
        // 전투 이펙트 헬퍼
        public void PlayHitEffect(Vector3 position, bool isCritical = false)
        {
            PlayEffect(isCritical ? "hit_critical" : "hit_physical", position);
        }
        
        public void PlayHealEffect(Transform target)
        {
            PlayEffectOnTarget("skill_heal", target);
        }
        
        public void PlayLevelUpEffect(Transform target)
        {
            PlayEffectOnTarget("ui_levelup", target);
            // 추가 축하 이펙트
            PlayEffect("celebration_confetti", target.position + Vector3.up * 2f);
        }
        
        public void PlayBuildingCompleteEffect(Vector3 position)
        {
            PlayEffect("build_complete", position);
            PlayAreaEffect("ui_sparkle", position, 2f, 8);
        }

        public GameObject PlayEffect(string effectId, Vector3 position)
        {
            Debug.Log($"Playing effect: {effectId} at {position}");
            // 임시로 빈 GameObject 반환
            return new GameObject($"Effect_{effectId}");
        }

        public void StopEffect(string effectId)
        {
            Debug.Log($"Stopping effect: {effectId}");
        }
    }
}