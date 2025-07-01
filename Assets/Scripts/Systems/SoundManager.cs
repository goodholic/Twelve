using UnityEngine;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 사운드를 관리하는 매니저
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        private static SoundManager instance;
        public static SoundManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SoundManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("SoundManager");
                        instance = go.AddComponent<SoundManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        [Header("오디오 소스")]
        public AudioSource musicSource;
        public AudioSource sfxSource;
        
        [Header("볼륨 설정")]
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.8f;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeAudioSources();
        }
        
        private void InitializeAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.volume = musicVolume;
            }
            
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.volume = sfxVolume;
            }
        }
        
        public void PlayMusic(AudioClip clip)
        {
            if (musicSource != null && clip != null)
            {
                musicSource.clip = clip;
                musicSource.Play();
            }
        }
        
        public void PlaySFX(AudioClip clip)
        {
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }
        
        public void PlaySFX(string clipName)
        {
            // 클립 이름으로 사운드 재생 (Resources에서 로드)
            AudioClip clip = Resources.Load<AudioClip>($"Audio/SFX/{clipName}");
            PlaySFX(clip);
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
                musicSource.volume = musicVolume;
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null)
                sfxSource.volume = sfxVolume;
        }
        
        public void StopMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }
        
        public void PauseMusic()
        {
            if (musicSource != null)
                musicSource.Pause();
        }
        
        public void ResumeMusic()
        {
            if (musicSource != null)
                musicSource.UnPause();
        }
    }
} 