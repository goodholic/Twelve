using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GuildMaster.Battle;
using GuildMaster.Systems;
using UnitStatus = GuildMaster.Battle.UnitStatus;
// using DG.Tweening; // DOTween 패키지가 없으므로 주석 처리

namespace GuildMaster.Battle
{
    public class BattleAnimationSystem : MonoBehaviour
    {
        private static BattleAnimationSystem _instance;
        public static BattleAnimationSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<BattleAnimationSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("BattleAnimationSystem");
                        _instance = go.AddComponent<BattleAnimationSystem>();
                    }
                }
                return _instance;
            }
        }
        
        [System.Serializable]
        public class AnimationData
        {
            public string animationId;
            public AnimationType type;
            public float duration = 1f;
            public AnimationCurve movementCurve;
            public AnimationCurve scaleCurve;
            public bool useParticleEffect = true;
            public string particleEffectId;
            public string soundEffectId;
        }
        
        public enum AnimationType
        {
            Attack,
            Skill,
            Movement,
            Damage,
            Death,
            Victory,
            Buff,
            Debuff,
            Summon
        }
        
        [System.Serializable]
        public class CameraShakeProfile
        {
            public string profileId;
            public float duration = 0.5f;
            public float strength = 0.3f;
            public int vibrato = 10;
            public float randomness = 90f;
        }
        
        [Header("Animation Settings")]
        [SerializeField] private float defaultAnimationSpeed = 1f;
        [SerializeField] private bool useScreenShake = true;
        [SerializeField] private bool useSlowMotion = true;
        
        [Header("Camera Shake Profiles")]
        [SerializeField] private List<CameraShakeProfile> shakeProfiles;
        
        [Header("Time Scale Effects")]
        [SerializeField] private float criticalHitTimeScale = 0.3f;
        [SerializeField] private float criticalHitDuration = 0.5f;
        
        private Dictionary<string, AnimationData> animationDatabase;
        private Queue<BattleAnimation> animationQueue;
        private Coroutine currentAnimationCoroutine;
        private Camera battleCamera;
        
        // 애니메이션 속도 배율
        private float speedMultiplier = 1f;
        
        // 이벤트
        public event Action<UnitStatus, AnimationType> OnAnimationStart;
        public event Action<UnitStatus, AnimationType> OnAnimationEnd;
        public event Action<UnitStatus, UnitStatus, int> OnDamageDealt;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            Initialize();
        }
        
        void Initialize()
        {
            animationDatabase = new Dictionary<string, AnimationData>();
            animationQueue = new Queue<BattleAnimation>();
            
            // 기본 애니메이션 데이터 설정
            SetupDefaultAnimations();
            
            // 카메라 찾기
            battleCamera = Camera.main;
            
            // DOTween 초기화 (패키지가 없으므로 주석 처리)
            // DOTween.Init();
            // DOTween.defaultAutoPlay = AutoPlay.None;
        }
        
        void SetupDefaultAnimations()
        {
            // 기본 공격 애니메이션
            AddAnimation(new AnimationData
            {
                animationId = "basic_attack",
                type = AnimationType.Attack,
                duration = 0.5f,
                particleEffectId = "hit_physical",
                soundEffectId = "sword_swing"
            });
            
            // 마법 공격
            AddAnimation(new AnimationData
            {
                animationId = "magic_attack",
                type = AnimationType.Skill,
                duration = 1f,
                particleEffectId = "hit_magical",
                soundEffectId = "magic_cast"
            });
            
            // 크리티컬 공격
            AddAnimation(new AnimationData
            {
                animationId = "critical_attack",
                type = AnimationType.Attack,
                duration = 0.8f,
                particleEffectId = "hit_critical",
                soundEffectId = "critical_hit"
            });
            
            // 힐 스킬
            AddAnimation(new AnimationData
            {
                animationId = "heal_skill",
                type = AnimationType.Skill,
                duration = 1.5f,
                particleEffectId = "skill_heal",
                soundEffectId = "heal_cast"
            });
            
            // 버프/디버프
            AddAnimation(new AnimationData
            {
                animationId = "buff_apply",
                type = AnimationType.Buff,
                duration = 1f,
                particleEffectId = "skill_buff",
                soundEffectId = "buff_apply"
            });
            
            AddAnimation(new AnimationData
            {
                animationId = "debuff_apply",
                type = AnimationType.Debuff,
                duration = 1f,
                particleEffectId = "skill_debuff",
                soundEffectId = "debuff_apply"
            });
            
            // 피격
            AddAnimation(new AnimationData
            {
                animationId = "take_damage",
                type = AnimationType.Damage,
                duration = 0.3f,
                soundEffectId = "damage_taken"
            });
            
            // 사망
            AddAnimation(new AnimationData
            {
                animationId = "death",
                type = AnimationType.Death,
                duration = 1f,
                soundEffectId = "unit_death"
            });
            
            // 승리
            AddAnimation(new AnimationData
            {
                animationId = "victory",
                type = AnimationType.Victory,
                duration = 2f,
                particleEffectId = "celebration_confetti"
            });
            
            // 이동
            AddAnimation(new AnimationData
            {
                animationId = "move",
                type = AnimationType.Movement,
                duration = 0.5f
            });
        }
        
        void AddAnimation(AnimationData data)
        {
            animationDatabase[data.animationId] = data;
        }
        
        // ===== 공격 애니메이션 =====
        
        public void PlayAttackAnimation(UnitStatus attacker, UnitStatus target, bool isCritical = false)
        {
            string animId = isCritical ? "critical_attack" : "basic_attack";
            var animation = new BattleAnimation
            {
                animationId = animId,
                source = attacker,
                target = target,
                isCritical = isCritical
            };
            
            EnqueueAnimation(animation);
        }
        
        public void PlaySkillAnimation(UnitStatus caster, List<UnitStatus> targets, Skill skill)
        {
            var animation = new BattleAnimation
            {
                animationId = GetSkillAnimationId(skill),
                source = caster,
                targets = targets,
                skill = skill
            };
            
            EnqueueAnimation(animation);
        }
        
        string GetSkillAnimationId(Skill skill)
        {
            // 스킬 타입에 따라 애니메이션 ID 반환
            switch (skill.skillType)
            {
                case SkillType.Damage:
                    return "magic_attack";
                case SkillType.Heal:
                    return "heal_skill";
                case SkillType.Buff:
                    return "buff_apply";
                case SkillType.Debuff:
                    return "debuff_apply";
                default:
                    return "basic_attack";
            }
        }
        
        // ===== 애니메이션 큐 관리 =====
        
        void EnqueueAnimation(BattleAnimation animation)
        {
            animationQueue.Enqueue(animation);
            
            if (currentAnimationCoroutine == null)
            {
                currentAnimationCoroutine = StartCoroutine(ProcessAnimationQueue());
            }
        }
        
        IEnumerator ProcessAnimationQueue()
        {
            while (animationQueue.Count > 0)
            {
                var animation = animationQueue.Dequeue();
                yield return PlayAnimation(animation);
            }
            
            currentAnimationCoroutine = null;
        }
        
        IEnumerator PlayAnimation(BattleAnimation battleAnim)
        {
            if (!animationDatabase.ContainsKey(battleAnim.animationId))
            {
                Debug.LogWarning($"Animation {battleAnim.animationId} not found!");
                yield break;
            }
            
            var animData = animationDatabase[battleAnim.animationId];
            
            // 애니메이션 시작 이벤트
            OnAnimationStart?.Invoke(battleAnim.source, animData.type);
            
            // 사운드 재생
            if (!string.IsNullOrEmpty(animData.soundEffectId))
            {
                SoundSystem.Instance?.PlaySound(animData.soundEffectId);
            }
            
            // 애니메이션 타입별 처리
            switch (animData.type)
            {
                case AnimationType.Attack:
                    yield return PlayMeleeAttackAnimation(battleAnim, animData);
                    break;
                    
                case AnimationType.Skill:
                    yield return PlaySkillCastAnimation(battleAnim, animData);
                    break;
                    
                case AnimationType.Damage:
                    yield return PlayDamageAnimation(battleAnim.source, animData);
                    break;
                    
                case AnimationType.Death:
                    yield return PlayDeathAnimation(battleAnim.source, animData);
                    break;
                    
                case AnimationType.Victory:
                    yield return PlayVictoryAnimation(battleAnim.source, animData);
                    break;
                    
                case AnimationType.Buff:
                case AnimationType.Debuff:
                    yield return PlayStatusEffectAnimation(battleAnim, animData);
                    break;
            }
            
            // 애니메이션 종료 이벤트
            OnAnimationEnd?.Invoke(battleAnim.source, animData.type);
        }
        
        // ===== 개별 애니메이션 구현 =====
        
        IEnumerator PlayMeleeAttackAnimation(BattleAnimation anim, AnimationData data)
        {
            if (anim.source == null || anim.target == null) yield break;
            
            Transform sourceTransform = anim.source.transform;
            Transform targetTransform = anim.target.transform;
            
            Vector3 originalPos = sourceTransform.position;
            Vector3 attackPos = targetTransform.position - (targetTransform.position - originalPos).normalized * 1f;
            
            // 공격자 이동 (DOTween 없이 간단한 이동)
            // Sequence attackSequence = DOTween.Sequence(); // DOTween 주석 처리
            
            // 전진
            // attackSequence.Append(sourceTransform.DOMove(attackPos, data.duration * 0.3f)
            //     .SetEase(Ease.OutQuad)); // DOTween 주석 처리
            
            // 공격 모션
            // attackSequence.Append(sourceTransform.DOPunchRotation(new Vector3(0, 0, 15), data.duration * 0.2f)); // DOTween 주석 처리
            
            // 타격 이펙트
            // attackSequence.AppendCallback(() => // DOTween 주석 처리
            {
                if (data.useParticleEffect && !string.IsNullOrEmpty(data.particleEffectId))
                {
                    ParticleEffectsSystem.Instance?.PlayEffect(data.particleEffectId, targetTransform.position);
                }
                
                // 크리티컬 시 특수 효과
                if (anim.isCritical)
                {
                    PlayCriticalHitEffect(targetTransform.position);
                }
                
                // 피격 애니메이션
                PlayHitReaction(anim.target);
            }
            // ); // DOTween 주석 처리
            
            // 후퇴
            // attackSequence.Append(sourceTransform.DOMove(originalPos, data.duration * 0.3f)
            //     .SetEase(Ease.InQuad)); // DOTween 주석 처리
            
            // attackSequence.Play(); // DOTween 주석 처리
            
            yield return new WaitForSeconds(data.duration * speedMultiplier);
        }
        
        IEnumerator PlaySkillCastAnimation(BattleAnimation anim, AnimationData data)
        {
            if (anim.source == null) yield break;
            
            Transform casterTransform = anim.source.transform;
            
            // 캐스팅 모션
            // Sequence castSequence = DOTween.Sequence();
            
            // 캐스터 빛나기
            // if (anim.source.GetComponent<Renderer>() != null)
            // {
            //     Material mat = anim.source.GetComponent<Renderer>().material;
            //     castSequence.Append(mat.DOColor(Color.white * 2f, "_EmissionColor", data.duration * 0.3f)
            //         .SetEase(Ease.InOutQuad));
            // }
            
            // 캐스팅 이펙트
            GameObject castEffect = null;
            if (data.useParticleEffect)
            {
                castEffect = ParticleEffectsSystem.Instance?.PlayEffectOnTarget("skill_cast", casterTransform);
            }
            
            // castSequence.AppendInterval(data.duration * 0.4f);
            
            // 스킬 발동
            // castSequence.AppendCallback(() =>
            // {
                // 타겟들에게 이펙트 적용
                if (anim.targets != null)
                {
                    foreach (var target in anim.targets)
                    {
                        if (target != null && data.useParticleEffect && !string.IsNullOrEmpty(data.particleEffectId))
                        {
                            ParticleEffectsSystem.Instance?.PlayEffectOnTarget(data.particleEffectId, target.transform);
                        }
                    }
                }
                else if (anim.target != null && data.useParticleEffect && !string.IsNullOrEmpty(data.particleEffectId))
                {
                    ParticleEffectsSystem.Instance?.PlayEffectOnTarget(data.particleEffectId, anim.target.transform);
                }
                
                // 캐스팅 이펙트 종료
                if (castEffect != null)
                {
                    ParticleEffectsSystem.Instance?.StopPersistentEffect(castEffect);
                }
            // });
            
            // 원래 색상으로 복귀
            // if (anim.source.GetComponent<Renderer>() != null)
            // {
            //     Material mat = anim.source.GetComponent<Renderer>().material;
            //     castSequence.Append(mat.DOColor(Color.black, "_EmissionColor", data.duration * 0.3f));
            // }
            
            // castSequence.Play();
            
            yield return new WaitForSeconds(data.duration * speedMultiplier);
        }
        
        IEnumerator PlayDamageAnimation(UnitStatus unit, AnimationData data)
        {
            if (unit == null || unit.transform == null) yield break;
            
            Transform unitTransform = unit.transform;
            
            // 피격 반응
            // Sequence damageSequence = DOTween.Sequence();
            
            // 붉은색 플래시
            // if (unit.GetComponent<Renderer>() != null)
            // {
            //     Material mat = unit.GetComponent<Renderer>().material;
            //     Color originalColor = mat.color;
            //     
            //     damageSequence.Append(mat.DOColor(Color.red, data.duration * 0.5f));
            //     damageSequence.Append(mat.DOColor(originalColor, data.duration * 0.5f));
            // }
            
            // 흔들림
            // damageSequence.Join(unitTransform.DOShakePosition(data.duration, 0.2f, 10, 90f, false, true));
            
            // damageSequence.Play();
            
            yield return new WaitForSeconds(data.duration * speedMultiplier);
        }
        
        IEnumerator PlayDeathAnimation(UnitStatus unit, AnimationData data)
        {
            if (unit == null || unit.transform == null) yield break;
            
            Transform unitTransform = unit.transform;
            
            // 사망 애니메이션
            // Sequence deathSequence = DOTween.Sequence();
            
            // 쓰러짐
            // deathSequence.Append(unitTransform.DORotate(new Vector3(0, 0, -90), data.duration * 0.5f));
            
            // 페이드 아웃
            // if (unit.GetComponent<Renderer>() != null)
            // {
            //     Material mat = unit.GetComponent<Renderer>().material;
            //     deathSequence.Join(mat.DOFade(0, data.duration));
            // }
            
            // 크기 축소
            // deathSequence.Join(unitTransform.DOScale(0, data.duration).SetEase(Ease.InBack));
            
            // 사망 이펙트
            if (data.useParticleEffect)
            {
                ParticleEffectsSystem.Instance?.PlayEffect("death_effect", unitTransform.position);
            }
            
            // deathSequence.Play();
            
            yield return new WaitForSeconds(data.duration * speedMultiplier);
            
            // 유닛 비활성화
            // unit.gameObject.SetActive(false);
        }
        
        IEnumerator PlayVictoryAnimation(UnitStatus unit, AnimationData data)
        {
            if (unit == null || unit.transform == null) yield break;
            
            Transform unitTransform = unit.transform;
            
            // 승리 애니메이션
            // Sequence victorySequence = DOTween.Sequence();
            
            // 점프
            // victorySequence.Append(unitTransform.DOJump(unitTransform.position, 1f, 1, data.duration * 0.5f));
            
            // 회전
            // victorySequence.Join(unitTransform.DORotate(new Vector3(0, 360, 0), data.duration * 0.5f, RotateMode.FastBeyond360));
            
            // 축하 이펙트
            if (data.useParticleEffect && !string.IsNullOrEmpty(data.particleEffectId))
            {
                ParticleEffectsSystem.Instance?.PlayEffect(data.particleEffectId, unitTransform.position + Vector3.up * 2f);
            }
            
            // victorySequence.Play();
            
            yield return new WaitForSeconds(data.duration * speedMultiplier);
        }
        
        IEnumerator PlayStatusEffectAnimation(BattleAnimation anim, AnimationData data)
        {
            if (anim.target == null) yield break;
            
            Transform targetTransform = anim.target.transform;
            
            // 상태 이펙트 적용
            if (data.useParticleEffect && !string.IsNullOrEmpty(data.particleEffectId))
            {
                GameObject effect = ParticleEffectsSystem.Instance?.PlayEffectOnTarget(data.particleEffectId, targetTransform);
                
                // 지속 효과인 경우 유닛에 연결
                if (data.type == AnimationType.Buff || data.type == AnimationType.Debuff)
                {
                    // TODO: 버프/디버프 지속시간 관리
                }
            }
            
            // 시각적 피드백
            // Sequence statusSequence = DOTween.Sequence();
            
            // if (data.type == AnimationType.Buff)
            // {
            //     // 버프: 크기 증가와 빛나기
            //     statusSequence.Append(targetTransform.DOScale(1.2f, data.duration * 0.3f));
            //     statusSequence.Append(targetTransform.DOScale(1f, data.duration * 0.3f));
            // }
            // else
            // {
            //     // 디버프: 크기 감소와 어두워지기
            //     statusSequence.Append(targetTransform.DOScale(0.8f, data.duration * 0.3f));
            //     statusSequence.Append(targetTransform.DOScale(1f, data.duration * 0.3f));
            // }
            
            // statusSequence.Play();
            
            yield return new WaitForSeconds(data.duration * speedMultiplier);
        }
        
        // ===== 특수 효과 =====
        
        void PlayCriticalHitEffect(Vector3 position)
        {
            // 슬로우 모션
            if (useSlowMotion)
            {
                StartCoroutine(ApplySlowMotion());
            }
            
            // 카메라 흔들림
            if (useScreenShake && battleCamera != null)
            {
                ShakeCamera("critical_hit");
            }
            
            // 크리티컬 텍스트
            ParticleEffectsSystem.Instance?.ShowDamageText(position + Vector3.up, 999, ParticleEffectsSystem.DamageType.Critical);
        }
        
        IEnumerator ApplySlowMotion()
        {
            Time.timeScale = criticalHitTimeScale;
            yield return new WaitForSecondsRealtime(criticalHitDuration);
            Time.timeScale = 1f;
        }
        
        public void ShakeCamera(string profileId)
        {
            if (battleCamera == null) return;
            
            var profile = shakeProfiles.Find(p => p.profileId == profileId);
            if (profile == null)
            {
                // 기본 흔들림
                // battleCamera.DOShakePosition(0.5f, 0.3f, 10, 90f, false, true);
            }
            else
            {
                // battleCamera.DOShakePosition(profile.duration, profile.strength, profile.vibrato, profile.randomness, false, true);
            }
        }
        
        void PlayHitReaction(UnitStatus target)
        {
            if (target == null || target.transform == null) return;
            
            // 간단한 피격 반응
            // target.transform.DOPunchPosition(-target.transform.forward * 0.3f, 0.2f, 0, 0);
        }
        
        // ===== 유틸리티 =====
        
        public void SetAnimationSpeed(float speed)
        {
            speedMultiplier = Mathf.Clamp(speed, 0.1f, 4f);
            // DOTween.timeScale = speedMultiplier;
        }
        
        public void ToggleScreenShake(bool enabled)
        {
            useScreenShake = enabled;
        }
        
        public void ToggleSlowMotion(bool enabled)
        {
            useSlowMotion = enabled;
        }
        
        public void PlayDamageNumber(Vector3 position, int damage, bool isCritical = false, bool isHeal = false)
        {
            var damageType = isHeal ? ParticleEffectsSystem.DamageType.Heal :
                           isCritical ? ParticleEffectsSystem.DamageType.Critical :
                           ParticleEffectsSystem.DamageType.Normal;
                           
            ParticleEffectsSystem.Instance?.ShowDamageText(position + Vector3.up, damage, damageType);
        }
        
        // 전투 시작/종료 연출
        public IEnumerator PlayBattleStartAnimation()
        {
            // 전투 시작 연출
            if (battleCamera != null)
            {
                // 카메라 줌인
                float originalSize = battleCamera.orthographicSize;
                // battleCamera.DOOrthoSize(originalSize * 0.8f, 0.5f).SetEase(Ease.OutQuad);
                
                yield return new WaitForSeconds(0.5f);
                
                // VS 텍스트 표시
                // TODO: UI 표시
                
                yield return new WaitForSeconds(1f);
                
                // 카메라 원위치
                // battleCamera.DOOrthoSize(originalSize, 0.5f).SetEase(Ease.InQuad);
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        public IEnumerator PlayBattleEndAnimation(bool victory)
        {
            if (victory)
            {
                // 승리 연출
                ParticleEffectsSystem.Instance?.PlayEffect("celebration_firework", Vector3.up * 5f);
                SoundSystem.Instance?.PlayMusic("victory");
                
                // 모든 살아있는 유닛 승리 모션
                // var units = FindObjectsOfType<Unit>();
                // foreach (var unit in units)
                // {
                //     if (unit.IsAlive && unit.IsPlayerUnit)
                //     {
                //         StartCoroutine(PlayVictoryAnimation(unit, animationDatabase["victory"]));
                //     }
                // }
            }
            else
            {
                // 패배 연출
                SoundSystem.Instance?.PlayMusic("defeat");
            }
            
            yield return new WaitForSeconds(3f);
        }
        
        // 내부 클래스
        class BattleAnimation
        {
            public string animationId;
            public UnitStatus source;
            public UnitStatus target;
            public List<UnitStatus> targets;
            public Skill skill;
            public bool isCritical;
            public int damage;
        }
    }
}