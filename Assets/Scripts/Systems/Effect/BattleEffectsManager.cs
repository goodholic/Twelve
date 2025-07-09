using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 전투 중 시각적 효과와 피드백을 관리하는 매니저
    /// </summary>
    public class BattleEffectsManager : MonoBehaviour
    {
        private static BattleEffectsManager _instance;
        public static BattleEffectsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<BattleEffectsManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("BattleEffectsManager");
                        _instance = go.AddComponent<BattleEffectsManager>();
                    }
                }
                return _instance;
            }
        }
        
        [Header("데미지 텍스트")]
        [SerializeField] private GameObject damageTextPrefab;
        [SerializeField] private float damageTextDuration = 1.5f;
        [SerializeField] private float damageTextFloatSpeed = 2f;
        [SerializeField] private AnimationCurve damageTextAlphaCurve;
        
        [Header("이펙트 프리팹")]
        [SerializeField] private GameObject attackEffectPrefab;
        [SerializeField] private GameObject healEffectPrefab;
        [SerializeField] private GameObject buffEffectPrefab;
        [SerializeField] private GameObject debuffEffectPrefab;
        [SerializeField] private GameObject criticalEffectPrefab;
        
        [Header("카메라 효과")]
        [SerializeField] private float cameraShakeIntensity = 0.3f;
        [SerializeField] private float cameraShakeDuration = 0.2f;
        
        [Header("오브젝트 풀")]
        private Queue<GameObject> damageTextPool = new Queue<GameObject>();
        private Dictionary<string, Queue<GameObject>> effectPools = new Dictionary<string, Queue<GameObject>>();
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializePools();
        }
        
        /// <summary>
        /// 오브젝트 풀 초기화
        /// </summary>
        void InitializePools()
        {
            // 데미지 텍스트 풀
            for (int i = 0; i < 20; i++)
            {
                if (damageTextPrefab != null)
                {
                    GameObject obj = Instantiate(damageTextPrefab);
                    obj.SetActive(false);
                    damageTextPool.Enqueue(obj);
                }
            }
            
            // 이펙트 풀 초기화
            InitializeEffectPool("attack", attackEffectPrefab, 10);
            InitializeEffectPool("heal", healEffectPrefab, 10);
            InitializeEffectPool("buff", buffEffectPrefab, 10);
            InitializeEffectPool("debuff", debuffEffectPrefab, 10);
            InitializeEffectPool("critical", criticalEffectPrefab, 5);
        }
        
        /// <summary>
        /// 이펙트 풀 초기화
        /// </summary>
        void InitializeEffectPool(string poolName, GameObject prefab, int count)
        {
            if (prefab == null) return;
            
            Queue<GameObject> pool = new Queue<GameObject>();
            
            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
            
            effectPools[poolName] = pool;
        }
        
        /// <summary>
        /// 데미지 텍스트 표시
        /// </summary>
        public void ShowDamageText(Vector3 position, int damage, bool isCritical = false)
        {
            GameObject textObj = GetFromPool(damageTextPool);
            
            if (textObj == null)
            {
                if (damageTextPrefab != null)
                {
                    textObj = Instantiate(damageTextPrefab);
                }
                else
                {
                    Debug.LogWarning("데미지 텍스트 프리팹이 설정되지 않았습니다!");
                    return;
                }
            }
            
            textObj.transform.position = position;
            textObj.SetActive(true);
            
            // 텍스트 설정
            TextMeshPro tmpText = textObj.GetComponent<TextMeshPro>();
            if (tmpText == null)
            {
                tmpText = textObj.GetComponentInChildren<TextMeshPro>();
            }
            
            if (tmpText != null)
            {
                tmpText.text = damage.ToString();
                
                if (isCritical)
                {
                    tmpText.text = "CRITICAL!\n" + damage;
                    tmpText.color = Color.yellow;
                    tmpText.fontSize *= 1.5f;
                }
                else
                {
                    tmpText.color = Color.white;
                }
            }
            
            // 애니메이션 시작
            StartCoroutine(AnimateDamageText(textObj));
            
            // 크리티컬 효과
            if (isCritical)
            {
                PlayEffect("critical", position, null, 1f);
                CameraShake(cameraShakeIntensity * 1.5f, cameraShakeDuration * 1.5f);
            }
        }
        
        /// <summary>
        /// 데미지 텍스트 애니메이션
        /// </summary>
        IEnumerator AnimateDamageText(GameObject textObj)
        {
            if (textObj == null) yield break;
            
            Vector3 startPos = textObj.transform.position;
            TextMeshPro tmpText = textObj.GetComponent<TextMeshPro>();
            if (tmpText == null)
            {
                tmpText = textObj.GetComponentInChildren<TextMeshPro>();
            }
            
            Color originalColor = tmpText != null ? tmpText.color : Color.white;
            
            float elapsed = 0f;
            
            while (elapsed < damageTextDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / damageTextDuration;
                
                // 위로 떠오르기
                textObj.transform.position = startPos + Vector3.up * (damageTextFloatSpeed * t);
                
                // 페이드 아웃
                if (tmpText != null)
                {
                    Color newColor = originalColor;
                    newColor.a = damageTextAlphaCurve.Evaluate(t);
                    tmpText.color = newColor;
                }
                
                yield return null;
            }
            
            // 풀로 반환
            textObj.SetActive(false);
            damageTextPool.Enqueue(textObj);
        }
        
        /// <summary>
        /// 이펙트 재생
        /// </summary>
        public void PlayEffect(string effectType, Vector3 position, Transform parent = null, float duration = 2f)
        {
            if (!effectPools.ContainsKey(effectType))
            {
                Debug.LogWarning($"이펙트 타입 '{effectType}'을 찾을 수 없습니다!");
                return;
            }
            
            GameObject effect = GetFromPool(effectPools[effectType]);
            
            if (effect == null)
            {
                Debug.LogWarning($"이펙트 풀에서 오브젝트를 가져올 수 없습니다: {effectType}");
                return;
            }
            
            effect.transform.position = position;
            if (parent != null)
            {
                effect.transform.SetParent(parent);
            }
            
            effect.SetActive(true);
            
            // 파티클 시스템 재생
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }
            
            // 일정 시간 후 풀로 반환
            StartCoroutine(ReturnEffectToPool(effect, effectType, duration));
        }
        
        /// <summary>
        /// 이펙트를 풀로 반환
        /// </summary>
        IEnumerator ReturnEffectToPool(GameObject effect, string poolName, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            effect.SetActive(false);
            effect.transform.SetParent(null);
            
            if (effectPools.ContainsKey(poolName))
            {
                effectPools[poolName].Enqueue(effect);
            }
        }
        
        /// <summary>
        /// 공격 효과 재생
        /// </summary>
        public void PlayAttackEffect(CharacterUnit attacker, CharacterUnit target)
        {
            if (attacker == null || target == null) return;
            
            // 공격자에서 대상으로 이동하는 효과
            StartCoroutine(PlayProjectileEffect(attacker.transform.position, target.transform.position));
            
            // 타격 효과
            PlayEffect("attack", target.transform.position, null, 1f);
            
            // 카메라 흔들림
            CameraShake(cameraShakeIntensity, cameraShakeDuration);
        }
        
        /// <summary>
        /// 투사체 효과
        /// </summary>
        IEnumerator PlayProjectileEffect(Vector3 start, Vector3 end)
        {
            // 간단한 투사체 효과 (추후 개선 가능)
            yield return new WaitForSeconds(0.1f);
        }
        
        /// <summary>
        /// 치유 효과 재생
        /// </summary>
        public void PlayHealEffect(CharacterUnit target)
        {
            if (target == null) return;
            
            PlayEffect("heal", target.transform.position, target.transform, 2f);
        }
        
        /// <summary>
        /// 버프 효과 재생
        /// </summary>
        public void PlayBuffEffect(CharacterUnit target)
        {
            if (target == null) return;
            
            PlayEffect("buff", target.transform.position, target.transform, 2f);
        }
        
        /// <summary>
        /// 디버프 효과 재생
        /// </summary>
        public void PlayDebuffEffect(CharacterUnit target)
        {
            if (target == null) return;
            
            PlayEffect("debuff", target.transform.position, target.transform, 2f);
        }
        
        /// <summary>
        /// 카메라 흔들림
        /// </summary>
        public void CameraShake(float intensity, float duration)
        {
            if (Camera.main != null)
            {
                StartCoroutine(ShakeCamera(intensity, duration));
            }
        }
        
        /// <summary>
        /// 카메라 흔들림 코루틴
        /// </summary>
        IEnumerator ShakeCamera(float intensity, float duration)
        {
            Camera cam = Camera.main;
            Vector3 originalPos = cam.transform.position;
            
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                
                float x = Random.Range(-1f, 1f) * intensity;
                float y = Random.Range(-1f, 1f) * intensity;
                
                cam.transform.position = originalPos + new Vector3(x, y, 0);
                
                yield return null;
            }
            
            cam.transform.position = originalPos;
        }
        
        /// <summary>
        /// 풀에서 오브젝트 가져오기
        /// </summary>
        GameObject GetFromPool(Queue<GameObject> pool)
        {
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }
            
            return null;
        }
        
        /// <summary>
        /// 승리 효과
        /// </summary>
        public void PlayVictoryEffect()
        {
            // 화면 전체에 승리 효과 재생
            Vector3 centerPos = Camera.main.transform.position + Camera.main.transform.forward * 10f;
            
            for (int i = 0; i < 5; i++)
            {
                Vector3 randomOffset = Random.insideUnitSphere * 3f;
                PlayEffect("buff", centerPos + randomOffset, null, 3f);
            }
        }
        
        /// <summary>
        /// 패배 효과
        /// </summary>
        public void PlayDefeatEffect()
        {
            // 화면 전체에 패배 효과 재생
            Vector3 centerPos = Camera.main.transform.position + Camera.main.transform.forward * 10f;
            PlayEffect("debuff", centerPos, null, 3f);
        }
    }
}