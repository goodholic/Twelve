using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace pjy.Managers
{
    /// <summary>
    /// 게임 사운드 관리 시스템
    /// - BGM 및 효과음 재생
    /// - 볼륨 조절 및 음소거
    /// - 사운드 풀링 시스템
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        [Header("오디오 소스")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private List<AudioSource> sfxPoolSources = new List<AudioSource>();
        
        [Header("볼륨 설정")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float bgmVolume = 0.7f;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 0.8f;
        
        [Header("사운드 클립")]
        [SerializeField] private AudioClip mainMenuBGM;
        [SerializeField] private AudioClip gameBGM;
        [SerializeField] private AudioClip victoryBGM;
        [SerializeField] private AudioClip defeatBGM;
        
        [Header("효과음 클립")]
        [SerializeField] private AudioClip buttonClickSFX;
        [SerializeField] private AudioClip characterSummonSFX;
        [SerializeField] private AudioClip characterMergeSFX;
        [SerializeField] private AudioClip attackSFX;
        [SerializeField] private AudioClip enemyDeathSFX;
        [SerializeField] private AudioClip waveStartSFX;
        [SerializeField] private AudioClip waveCompleteSFX;
        [SerializeField] private AudioClip coinGetSFX;
        [SerializeField] private AudioClip errorSFX;
        
        [Header("사운드 풀 설정")]
        [SerializeField] private int sfxPoolSize = 10;
        
        private static SoundManager instance;
        public static SoundManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SoundManager>();
                }
                return instance;
            }
        }
        
        private Dictionary<string, AudioClip> soundClips = new Dictionary<string, AudioClip>();
        private bool isMuted = false;
        private int currentSFXIndex = 0;
        
        private void Awake()
        {
            // 싱글톤 초기화
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeSoundManager();
        }
        
        private void Start()
        {
            LoadVolumeSettings();
            InitializeSoundClips();
            
            // 메인 메뉴 BGM 시작
            PlayBGM("MainMenu");
        }
        
        /// <summary>
        /// 사운드 매니저 초기화
        /// </summary>
        private void InitializeSoundManager()
        {
            // BGM 소스가 없으면 생성
            if (bgmSource == null)
            {
                GameObject bgmObj = new GameObject("BGM_Source");
                bgmObj.transform.SetParent(transform);
                bgmSource = bgmObj.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }
            
            // SFX 소스가 없으면 생성
            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFX_Source");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
            
            // SFX 풀 생성
            CreateSFXPool();
        }
        
        /// <summary>
        /// SFX 오디오 소스 풀 생성
        /// </summary>
        private void CreateSFXPool()
        {
            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject poolObj = new GameObject($"SFX_Pool_{i}");
                poolObj.transform.SetParent(transform);
                AudioSource poolSource = poolObj.AddComponent<AudioSource>();
                poolSource.playOnAwake = false;
                sfxPoolSources.Add(poolSource);
            }
        }
        
        /// <summary>
        /// 사운드 클립 딕셔너리 초기화
        /// </summary>
        private void InitializeSoundClips()
        {
            // BGM 클립 등록
            if (mainMenuBGM != null) soundClips["MainMenu"] = mainMenuBGM;
            if (gameBGM != null) soundClips["Game"] = gameBGM;
            if (victoryBGM != null) soundClips["Victory"] = victoryBGM;
            if (defeatBGM != null) soundClips["Defeat"] = defeatBGM;
            
            // SFX 클립 등록
            if (buttonClickSFX != null) soundClips["ButtonClick"] = buttonClickSFX;
            if (characterSummonSFX != null) soundClips["CharacterSummon"] = characterSummonSFX;
            if (characterMergeSFX != null) soundClips["CharacterMerge"] = characterMergeSFX;
            if (attackSFX != null) soundClips["Attack"] = attackSFX;
            if (enemyDeathSFX != null) soundClips["EnemyDeath"] = enemyDeathSFX;
            if (waveStartSFX != null) soundClips["WaveStart"] = waveStartSFX;
            if (waveCompleteSFX != null) soundClips["WaveComplete"] = waveCompleteSFX;
            if (coinGetSFX != null) soundClips["CoinGet"] = coinGetSFX;
            if (errorSFX != null) soundClips["Error"] = errorSFX;
        }
        
        /// <summary>
        /// 볼륨 설정 로드
        /// </summary>
        private void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.7f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
            isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
            
            ApplyVolumeSettings();
        }
        
        /// <summary>
        /// 볼륨 설정 저장
        /// </summary>
        private void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// 볼륨 설정 적용
        /// </summary>
        private void ApplyVolumeSettings()
        {
            if (bgmSource != null)
                bgmSource.volume = isMuted ? 0f : masterVolume * bgmVolume;
                
            if (sfxSource != null)
                sfxSource.volume = isMuted ? 0f : masterVolume * sfxVolume;
                
            foreach (var poolSource in sfxPoolSources)
            {
                if (poolSource != null)
                    poolSource.volume = isMuted ? 0f : masterVolume * sfxVolume;
            }
        }
        
        /// <summary>
        /// BGM 재생
        /// </summary>
        public void PlayBGM(string clipName)
        {
            if (!soundClips.ContainsKey(clipName))
            {
                Debug.LogWarning($"[SoundManager] BGM 클립을 찾을 수 없습니다: {clipName}");
                return;
            }
            
            if (bgmSource != null)
            {
                bgmSource.clip = soundClips[clipName];
                bgmSource.Play();
            }
        }
        
        /// <summary>
        /// BGM 중지
        /// </summary>
        public void StopBGM()
        {
            if (bgmSource != null)
                bgmSource.Stop();
        }
        
        /// <summary>
        /// BGM 일시정지
        /// </summary>
        public void PauseBGM()
        {
            if (bgmSource != null)
                bgmSource.Pause();
        }
        
        /// <summary>
        /// BGM 재개
        /// </summary>
        public void ResumeBGM()
        {
            if (bgmSource != null)
                bgmSource.UnPause();
        }
        
        /// <summary>
        /// 효과음 재생
        /// </summary>
        public void PlaySFX(string clipName)
        {
            if (!soundClips.ContainsKey(clipName))
            {
                Debug.LogWarning($"[SoundManager] SFX 클립을 찾을 수 없습니다: {clipName}");
                return;
            }
            
            // 풀에서 사용 가능한 오디오 소스 찾기
            AudioSource availableSource = GetAvailableSFXSource();
            if (availableSource != null)
            {
                availableSource.clip = soundClips[clipName];
                availableSource.Play();
            }
        }
        
        /// <summary>
        /// 효과음 재생 (AudioClip 직접 전달)
        /// </summary>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            
            AudioSource availableSource = GetAvailableSFXSource();
            if (availableSource != null)
            {
                availableSource.clip = clip;
                availableSource.Play();
            }
        }
        
        /// <summary>
        /// 효과음 재생 (위치 지정)
        /// </summary>
        public void PlaySFXAtPosition(string clipName, Vector3 position)
        {
            if (!soundClips.ContainsKey(clipName)) return;
            
            AudioSource.PlayClipAtPoint(soundClips[clipName], position, masterVolume * sfxVolume);
        }
        
        /// <summary>
        /// 사용 가능한 SFX 소스 가져오기
        /// </summary>
        private AudioSource GetAvailableSFXSource()
        {
            // 재생 중이지 않은 소스 찾기
            foreach (var source in sfxPoolSources)
            {
                if (!source.isPlaying)
                    return source;
            }
            
            // 모든 소스가 사용 중이면 순환적으로 사용
            AudioSource currentSource = sfxPoolSources[currentSFXIndex];
            currentSFXIndex = (currentSFXIndex + 1) % sfxPoolSources.Count;
            return currentSource;
        }
        
        /// <summary>
        /// 마스터 볼륨 설정
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
            SaveVolumeSettings();
        }
        
        /// <summary>
        /// BGM 볼륨 설정
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
            SaveVolumeSettings();
        }
        
        /// <summary>
        /// SFX 볼륨 설정
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
            SaveVolumeSettings();
        }
        
        /// <summary>
        /// 음소거 토글
        /// </summary>
        public void ToggleMute()
        {
            isMuted = !isMuted;
            ApplyVolumeSettings();
            SaveVolumeSettings();
        }
        
        /// <summary>
        /// 음소거 설정
        /// </summary>
        public void SetMute(bool mute)
        {
            isMuted = mute;
            ApplyVolumeSettings();
            SaveVolumeSettings();
        }
        
        /// <summary>
        /// 게임 상황별 BGM 변경
        /// </summary>
        public void PlayGameStateBGM(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    PlayBGM("MainMenu");
                    break;
                case GameState.InGame:
                    PlayBGM("Game");
                    break;
                case GameState.Victory:
                    PlayBGM("Victory");
                    break;
                case GameState.Defeat:
                    PlayBGM("Defeat");
                    break;
            }
        }
        
        /// <summary>
        /// 게임 이벤트별 효과음 재생
        /// </summary>
        public void PlayGameEventSFX(GameEvent gameEvent)
        {
            switch (gameEvent)
            {
                case GameEvent.ButtonClick:
                    PlaySFX("ButtonClick");
                    break;
                case GameEvent.CharacterSummon:
                    PlaySFX("CharacterSummon");
                    break;
                case GameEvent.CharacterMerge:
                    PlaySFX("CharacterMerge");
                    break;
                case GameEvent.Attack:
                    PlaySFX("Attack");
                    break;
                case GameEvent.EnemyDeath:
                    PlaySFX("EnemyDeath");
                    break;
                case GameEvent.WaveStart:
                    PlaySFX("WaveStart");
                    break;
                case GameEvent.WaveComplete:
                    PlaySFX("WaveComplete");
                    break;
                case GameEvent.CoinGet:
                    PlaySFX("CoinGet");
                    break;
                case GameEvent.Error:
                    PlaySFX("Error");
                    break;
            }
        }
        
        /// <summary>
        /// 볼륨 페이드 인
        /// </summary>
        public void FadeInBGM(float duration)
        {
            StartCoroutine(FadeVolume(bgmSource, 0f, masterVolume * bgmVolume, duration));
        }
        
        /// <summary>
        /// 볼륨 페이드 아웃
        /// </summary>
        public void FadeOutBGM(float duration)
        {
            StartCoroutine(FadeVolume(bgmSource, bgmSource.volume, 0f, duration));
        }
        
        /// <summary>
        /// 볼륨 페이드 코루틴
        /// </summary>
        private IEnumerator FadeVolume(AudioSource source, float startVolume, float endVolume, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                source.volume = Mathf.Lerp(startVolume, endVolume, t);
                yield return null;
            }
            
            source.volume = endVolume;
        }
        
        /// <summary>
        /// 현재 볼륨 설정 정보 가져오기
        /// </summary>
        public SoundSettings GetSoundSettings()
        {
            return new SoundSettings
            {
                masterVolume = this.masterVolume,
                bgmVolume = this.bgmVolume,
                sfxVolume = this.sfxVolume,
                isMuted = this.isMuted
            };
        }
        
        private void OnDestroy()
        {
            SaveVolumeSettings();
            if (instance == this)
                instance = null;
        }
    }
    
    /// <summary>
    /// 게임 상태 열거형
    /// </summary>
    public enum GameState
    {
        MainMenu,
        InGame,
        Victory,
        Defeat
    }
    
    /// <summary>
    /// 게임 이벤트 열거형
    /// </summary>
    public enum GameEvent
    {
        ButtonClick,
        CharacterSummon,
        CharacterMerge,
        Attack,
        EnemyDeath,
        WaveStart,
        WaveComplete,
        CoinGet,
        Error
    }
    
    /// <summary>
    /// 사운드 설정 데이터
    /// </summary>
    [System.Serializable]
    public class SoundSettings
    {
        public float masterVolume;
        public float bgmVolume;
        public float sfxVolume;
        public bool isMuted;
    }
}