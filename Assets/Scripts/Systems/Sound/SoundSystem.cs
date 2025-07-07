using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GuildMaster.Systems
{
    public class SoundSystem : MonoBehaviour
    {
        private static SoundSystem _instance;
        public static SoundSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SoundSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SoundSystem");
                        _instance = go.AddComponent<SoundSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        [System.Serializable]
        public class AudioClipData
        {
            public string clipId;
            public AudioClip audioClip;
            public float volume = 1f;
            public float pitch = 1f;
            public bool loop = false;
            public AudioMixerGroup mixerGroup;
        }
        
        [System.Serializable]
        public class MusicTrack
        {
            public string trackId;
            public string trackName;
            public AudioClip audioClip;
            public MusicCategory category;
            public float fadeInTime = 1f;
            public float fadeOutTime = 1f;
            public bool loop = true;
        }
        
        public enum MusicCategory
        {
            MainMenu,
            Guild,
            Battle,
            Victory,
            Defeat,
            Boss,
            Exploration,
            Event
        }
        
        public enum SoundCategory
        {
            UI,
            Battle,
            Skill,
            Building,
            Ambient,
            Notification,
            Voice
        }
        
        [Header("Audio Mixers")]
        [SerializeField] private AudioMixer mainMixer;
        [SerializeField] private AudioMixerGroup masterGroup;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup voiceGroup;
        [SerializeField] private AudioMixerGroup ambientGroup;
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambientSource;
        private List<AudioSource> sfxSources;
        private const int MAX_SFX_SOURCES = 10;
        
        [Header("Audio Libraries")]
        [SerializeField] private List<AudioClipData> soundEffects;
        [SerializeField] private List<MusicTrack> musicTracks;
        
        private Dictionary<string, AudioClipData> soundLibrary;
        private Dictionary<string, MusicTrack> musicLibrary;
        private MusicTrack currentMusic;
        private Coroutine musicFadeCoroutine;
        
        // Volume settings
        private float masterVolume = 1f;
        private float musicVolume = 0.7f;
        private float sfxVolume = 1f;
        private float voiceVolume = 1f;
        private float ambientVolume = 0.5f;
        
        // Events
        public event Action<MusicTrack> OnMusicChanged;
        public event Action<string> OnSoundPlayed;
        
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
            // 오디오 소스 초기화
            SetupAudioSources();
            
            // 라이브러리 초기화
            InitializeLibraries();
            
            // 저장된 볼륨 설정 로드
            LoadVolumeSettings();
            
            // 기본 음악 재생
            PlayMusic("guild_theme");
        }
        
        void SetupAudioSources()
        {
            // 음악 소스
            if (musicSource == null)
            {
                GameObject musicGO = new GameObject("MusicSource");
                musicGO.transform.SetParent(transform);
                musicSource = musicGO.AddComponent<AudioSource>();
            }
            musicSource.outputAudioMixerGroup = musicGroup;
            musicSource.loop = true;
            musicSource.priority = 0;
            
            // 환경음 소스
            if (ambientSource == null)
            {
                GameObject ambientGO = new GameObject("AmbientSource");
                ambientGO.transform.SetParent(transform);
                ambientSource = ambientGO.AddComponent<AudioSource>();
            }
            ambientSource.outputAudioMixerGroup = ambientGroup;
            ambientSource.loop = true;
            ambientSource.priority = 10;
            
            // 효과음 소스 풀
            sfxSources = new List<AudioSource>();
            for (int i = 0; i < MAX_SFX_SOURCES; i++)
            {
                GameObject sfxGO = new GameObject($"SFXSource_{i}");
                sfxGO.transform.SetParent(transform);
                AudioSource source = sfxGO.AddComponent<AudioSource>();
                source.outputAudioMixerGroup = sfxGroup;
                source.playOnAwake = false;
                sfxSources.Add(source);
            }
        }
        
        void InitializeLibraries()
        {
            // 사운드 라이브러리 초기화
            soundLibrary = new Dictionary<string, AudioClipData>();
            if (soundEffects == null) soundEffects = new List<AudioClipData>();
            
            // 기본 사운드 효과 추가
            AddDefaultSoundEffects();
            
            foreach (var sound in soundEffects)
            {
                if (!string.IsNullOrEmpty(sound.clipId) && sound.audioClip != null)
                {
                    soundLibrary[sound.clipId] = sound;
                }
            }
            
            // 음악 라이브러리 초기화
            musicLibrary = new Dictionary<string, MusicTrack>();
            if (musicTracks == null) musicTracks = new List<MusicTrack>();
            
            // 기본 음악 트랙 추가
            AddDefaultMusicTracks();
            
            foreach (var track in musicTracks)
            {
                if (!string.IsNullOrEmpty(track.trackId) && track.audioClip != null)
                {
                    musicLibrary[track.trackId] = track;
                }
            }
        }
        
        void AddDefaultSoundEffects()
        {
            // UI 사운드
            AddSoundEffect("ui_click", SoundCategory.UI, 0.7f);
            AddSoundEffect("ui_hover", SoundCategory.UI, 0.5f);
            AddSoundEffect("ui_success", SoundCategory.UI, 0.8f);
            AddSoundEffect("ui_error", SoundCategory.UI, 0.8f);
            AddSoundEffect("ui_open", SoundCategory.UI, 0.6f);
            AddSoundEffect("ui_close", SoundCategory.UI, 0.6f);
            
            // 전투 사운드
            AddSoundEffect("sword_swing", SoundCategory.Battle, 0.8f);
            AddSoundEffect("arrow_shot", SoundCategory.Battle, 0.7f);
            AddSoundEffect("magic_cast", SoundCategory.Battle, 0.9f);
            AddSoundEffect("shield_block", SoundCategory.Battle, 0.8f);
            AddSoundEffect("damage_taken", SoundCategory.Battle, 0.7f);
            AddSoundEffect("critical_hit", SoundCategory.Battle, 1f);
            
            // 스킬 사운드
            AddSoundEffect("heal_cast", SoundCategory.Skill, 0.8f);
            AddSoundEffect("buff_apply", SoundCategory.Skill, 0.7f);
            AddSoundEffect("debuff_apply", SoundCategory.Skill, 0.7f);
            AddSoundEffect("ultimate_skill", SoundCategory.Skill, 1f);
            
            // 건설 사운드
            AddSoundEffect("building_place", SoundCategory.Building, 0.9f);
            AddSoundEffect("building_complete", SoundCategory.Building, 1f);
            AddSoundEffect("building_upgrade", SoundCategory.Building, 0.9f);
            AddSoundEffect("building_demolish", SoundCategory.Building, 0.8f);
            
            // 알림 사운드
            AddSoundEffect("notification_quest", SoundCategory.Notification, 0.8f);
            AddSoundEffect("notification_achievement", SoundCategory.Notification, 1f);
            AddSoundEffect("notification_levelup", SoundCategory.Notification, 1f);
            AddSoundEffect("notification_reward", SoundCategory.Notification, 0.9f);
        }
        
        void AddSoundEffect(string id, SoundCategory category, float volume)
        {
            // 실제 AudioClip은 Unity Editor에서 할당
            soundEffects.Add(new AudioClipData
            {
                clipId = id,
                volume = volume,
                pitch = 1f,
                loop = false
            });
        }
        
        void AddDefaultMusicTracks()
        {
            // 메인 메뉴
            musicTracks.Add(new MusicTrack
            {
                trackId = "main_menu",
                trackName = "Main Menu Theme",
                category = MusicCategory.MainMenu,
                fadeInTime = 2f,
                fadeOutTime = 1f
            });
            
            // 길드 테마
            musicTracks.Add(new MusicTrack
            {
                trackId = "guild_theme",
                trackName = "Guild Hall",
                category = MusicCategory.Guild,
                fadeInTime = 1.5f,
                fadeOutTime = 1f
            });
            
            // 전투 음악
            musicTracks.Add(new MusicTrack
            {
                trackId = "battle_normal",
                trackName = "Battle Theme",
                category = MusicCategory.Battle,
                fadeInTime = 0.5f,
                fadeOutTime = 0.5f
            });
            
            musicTracks.Add(new MusicTrack
            {
                trackId = "battle_boss",
                trackName = "Boss Battle",
                category = MusicCategory.Boss,
                fadeInTime = 0.3f,
                fadeOutTime = 0.5f
            });
            
            // 승리/패배
            musicTracks.Add(new MusicTrack
            {
                trackId = "victory",
                trackName = "Victory Fanfare",
                category = MusicCategory.Victory,
                fadeInTime = 0f,
                fadeOutTime = 1f,
                loop = false
            });
            
            musicTracks.Add(new MusicTrack
            {
                trackId = "defeat",
                trackName = "Defeat Theme",
                category = MusicCategory.Defeat,
                fadeInTime = 0.5f,
                fadeOutTime = 1f,
                loop = false
            });
            
            // 탐험
            musicTracks.Add(new MusicTrack
            {
                trackId = "exploration",
                trackName = "Exploration Theme",
                category = MusicCategory.Exploration,
                fadeInTime = 2f,
                fadeOutTime = 1.5f
            });
        }
        
        // 음악 재생
        public void PlayMusic(string trackId, bool immediate = false)
        {
            if (!musicLibrary.ContainsKey(trackId))
            {
                Debug.LogWarning($"Music track {trackId} not found!");
                return;
            }
            
            var track = musicLibrary[trackId];
            
            if (currentMusic != null && currentMusic.trackId == trackId)
                return;
            
            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
            }
            
            if (immediate)
            {
                musicSource.Stop();
                musicSource.clip = track.audioClip;
                musicSource.volume = musicVolume;
                musicSource.loop = track.loop;
                musicSource.Play();
                currentMusic = track;
                OnMusicChanged?.Invoke(track);
            }
            else
            {
                musicFadeCoroutine = StartCoroutine(CrossfadeMusic(track));
            }
        }
        
        IEnumerator CrossfadeMusic(MusicTrack newTrack)
        {
            float fadeOutTime = currentMusic?.fadeOutTime ?? 0f;
            float fadeInTime = newTrack.fadeInTime;
            
            // Fade out current music
            if (musicSource.isPlaying && fadeOutTime > 0)
            {
                float startVolume = musicSource.volume;
                float timer = 0;
                
                while (timer < fadeOutTime)
                {
                    timer += Time.deltaTime;
                    musicSource.volume = Mathf.Lerp(startVolume, 0, timer / fadeOutTime);
                    yield return null;
                }
            }
            
            // Switch track
            musicSource.Stop();
            musicSource.clip = newTrack.audioClip;
            musicSource.loop = newTrack.loop;
            currentMusic = newTrack;
            
            // Fade in new music
            musicSource.Play();
            float targetVolume = musicVolume;
            float fadeTimer = 0;
            
            while (fadeTimer < fadeInTime)
            {
                fadeTimer += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0, targetVolume, fadeTimer / fadeInTime);
                yield return null;
            }
            
            musicSource.volume = targetVolume;
            OnMusicChanged?.Invoke(newTrack);
        }
        
        // 사운드 효과 재생
        public void PlaySound(string soundId, float volumeMultiplier = 1f, float pitchVariation = 0f)
        {
            if (!soundLibrary.ContainsKey(soundId))
            {
                Debug.LogWarning($"Sound effect {soundId} not found!");
                return;
            }
            
            var soundData = soundLibrary[soundId];
            AudioSource source = GetAvailableSFXSource();
            
            if (source != null && soundData.audioClip != null)
            {
                source.clip = soundData.audioClip;
                source.volume = soundData.volume * volumeMultiplier * sfxVolume;
                source.pitch = soundData.pitch + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
                source.loop = soundData.loop;
                source.Play();
                
                OnSoundPlayed?.Invoke(soundId);
                
                if (!soundData.loop)
                {
                    StartCoroutine(ReturnSourceToPool(source, soundData.audioClip.length));
                }
            }
        }
        
        AudioSource GetAvailableSFXSource()
        {
            foreach (var source in sfxSources)
            {
                if (!source.isPlaying)
                    return source;
            }
            
            // 모두 사용 중이면 가장 오래 재생된 것을 반환
            return sfxSources[0];
        }
        
        IEnumerator ReturnSourceToPool(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            source.Stop();
            source.clip = null;
        }
        
        // 3D 사운드 재생
        public void PlaySoundAtPosition(string soundId, Vector3 position, float volumeMultiplier = 1f)
        {
            if (!soundLibrary.ContainsKey(soundId))
                return;
            
            var soundData = soundLibrary[soundId];
            GameObject tempGO = new GameObject("TempAudio");
            tempGO.transform.position = position;
            
            AudioSource tempSource = tempGO.AddComponent<AudioSource>();
            tempSource.clip = soundData.audioClip;
            tempSource.volume = soundData.volume * volumeMultiplier * sfxVolume;
            tempSource.pitch = soundData.pitch;
            tempSource.spatialBlend = 1f; // 3D sound
            tempSource.minDistance = 5f;
            tempSource.maxDistance = 50f;
            tempSource.rolloffMode = AudioRolloffMode.Linear;
            tempSource.outputAudioMixerGroup = sfxGroup;
            
            tempSource.Play();
            
            Destroy(tempGO, soundData.audioClip.length);
        }
        
        // 환경음 재생
        public void PlayAmbient(string soundId, float volume = 1f)
        {
            if (!soundLibrary.ContainsKey(soundId))
                return;
            
            var soundData = soundLibrary[soundId];
            ambientSource.clip = soundData.audioClip;
            ambientSource.volume = volume * ambientVolume;
            ambientSource.Play();
        }
        
        public void StopAmbient()
        {
            ambientSource.Stop();
        }
        
        // 볼륨 설정
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            mainMixer?.SetFloat("MasterVolume", Mathf.Log10(masterVolume) * 20);
            SaveVolumeSettings();
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            mainMixer?.SetFloat("MusicVolume", Mathf.Log10(musicVolume) * 20);
            
            if (musicSource != null && currentMusic != null)
            {
                musicSource.volume = musicVolume;
            }
            
            SaveVolumeSettings();
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            mainMixer?.SetFloat("SFXVolume", Mathf.Log10(sfxVolume) * 20);
            SaveVolumeSettings();
        }
        
        public void SetVoiceVolume(float volume)
        {
            voiceVolume = Mathf.Clamp01(volume);
            mainMixer?.SetFloat("VoiceVolume", Mathf.Log10(voiceVolume) * 20);
            SaveVolumeSettings();
        }
        
        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            mainMixer?.SetFloat("AmbientVolume", Mathf.Log10(ambientVolume) * 20);
            
            if (ambientSource != null)
            {
                ambientSource.volume = ambientVolume;
            }
            
            SaveVolumeSettings();
        }
        
        // 음소거
        public void ToggleMute()
        {
            float currentVolume;
            mainMixer.GetFloat("MasterVolume", out currentVolume);
            
            if (currentVolume < -40f)
            {
                SetMasterVolume(masterVolume);
            }
            else
            {
                mainMixer.SetFloat("MasterVolume", -80f);
            }
        }
        
        // 설정 저장/로드
        void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetFloat("VoiceVolume", voiceVolume);
            PlayerPrefs.SetFloat("AmbientVolume", ambientVolume);
            PlayerPrefs.Save();
        }
        
        void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            voiceVolume = PlayerPrefs.GetFloat("VoiceVolume", 1f);
            ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 0.5f);
            
            SetMasterVolume(masterVolume);
            SetMusicVolume(musicVolume);
            SetSFXVolume(sfxVolume);
            SetVoiceVolume(voiceVolume);
            SetAmbientVolume(ambientVolume);
        }
        
        // 상황별 음악 자동 재생
        public void OnBattleStart(bool isBoss = false)
        {
            PlayMusic(isBoss ? "battle_boss" : "battle_normal");
        }
        
        public void OnBattleEnd(bool victory)
        {
            PlayMusic(victory ? "victory" : "defeat");
            
            // 일정 시간 후 길드 테마로 복귀
            StartCoroutine(ReturnToGuildMusic(victory ? 5f : 8f));
        }
        
        IEnumerator ReturnToGuildMusic(float delay)
        {
            yield return new WaitForSeconds(delay);
            PlayMusic("guild_theme");
        }
        
        // public void OnEnterDungeon() // Dungeon 기능 제거됨
        // {
        //     PlayMusic("exploration");
        //     PlayAmbient("dungeon_ambient");
        // }
        // 
        // public void OnExitDungeon() // Dungeon 기능 제거됨
        // {
        //     PlayMusic("guild_theme");
        //     StopAmbient();
        // }
        
        // 현재 볼륨 반환
        public float GetMasterVolume() => masterVolume;
        public float GetMusicVolume() => musicVolume;
        public float GetSFXVolume() => sfxVolume;
        public float GetVoiceVolume() => voiceVolume;
        public float GetAmbientVolume() => ambientVolume;
    }
}