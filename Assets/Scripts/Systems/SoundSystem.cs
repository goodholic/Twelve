using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GuildMaster.Systems
{
    public class SoundSystem : MonoBehaviour
    {
        private static SoundSystem instance;
        public static SoundSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SoundSystem>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("SoundSystem");
                        instance = go.AddComponent<SoundSystem>();
                    }
                }
                return instance;
            }
        }
        
        [Header("사운드 설정")]
        public AudioSource musicSource;
        public AudioSource sfxSource;
        public AudioSource voiceSource;
        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public float voiceVolume = 1f;
        public float ambientVolume = 1f;
        
        [Header("사운드 클립")]
        public AudioClip[] musicClips;
        public AudioClip[] sfxClips;
        
        [Header("환경음 소스들")]
        public AudioSource[] ambientSources = new AudioSource[4];
        
        private Dictionary<string, AudioClip> soundLibrary = new Dictionary<string, AudioClip>();
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSoundSystem();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeSoundSystem()
        {
            // AudioSource 컴포넌트 초기화
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
            
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
            
            if (voiceSource == null)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
                voiceSource.loop = false;
                voiceSource.playOnAwake = false;
            }
            
            // 기본 사운드 라이브러리 구축
            BuildSoundLibrary();
        }
        
        private void BuildSoundLibrary()
        {
            // 기본 UI 사운드들 추가
            soundLibrary["ui_click"] = Resources.Load<AudioClip>("Sounds/UI/ui_click");
            soundLibrary["ui_success"] = Resources.Load<AudioClip>("Sounds/UI/ui_success");
            soundLibrary["ui_error"] = Resources.Load<AudioClip>("Sounds/UI/ui_error");
            soundLibrary["ui_sparkle"] = Resources.Load<AudioClip>("Sounds/UI/ui_sparkle");
            
            // 배틀 사운드들 추가 (예시)
            soundLibrary["battle_start"] = Resources.Load<AudioClip>("Sounds/Battle/battle_start");
            soundLibrary["character_spawn"] = Resources.Load<AudioClip>("Sounds/Battle/character_spawn");
        }
        
        /// <summary>
        /// 사운드 재생
        /// </summary>
        public void PlaySound(string soundName)
        {
            if (soundLibrary.ContainsKey(soundName) && soundLibrary[soundName] != null)
            {
                sfxSource.PlayOneShot(soundLibrary[soundName], sfxVolume * masterVolume);
            }
            else
            {
                Debug.LogWarning($"Sound '{soundName}' not found in library!");
            }
        }
        
        /// <summary>
        /// 음악 재생
        /// </summary>
        public void PlayMusic(string musicName)
        {
            if (soundLibrary.ContainsKey(musicName) && soundLibrary[musicName] != null)
            {
                musicSource.clip = soundLibrary[musicName];
                musicSource.volume = musicVolume * masterVolume;
                musicSource.Play();
            }
        }
        
        /// <summary>
        /// 음악 정지
        /// </summary>
        public void StopMusic()
        {
            musicSource.Stop();
        }
        
        /// <summary>
        /// 볼륨 설정
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }
        
        private void UpdateVolumes()
        {
            if (musicSource != null)
                musicSource.volume = musicVolume * masterVolume;
            
            if (sfxSource != null)
                sfxSource.volume = sfxVolume * masterVolume;
        }
        
        /// <summary>
        /// 음성 볼륨 설정
        /// </summary>
        public void SetVoiceVolume(float volume)
        {
            voiceVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("VoiceVolume", voiceVolume);
            
            // 현재 재생 중인 음성 오디오에 적용
            if (voiceSource != null)
            {
                voiceSource.volume = voiceVolume * masterVolume;
            }
        }
        
        /// <summary>
        /// 환경음 볼륨 설정
        /// </summary>
        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("AmbientVolume", ambientVolume);
            
            // 현재 재생 중인 환경음에 적용
            foreach (var source in ambientSources)
            {
                if (source != null)
                {
                    source.volume = ambientVolume * masterVolume;
                }
            }
        }
        
        /// <summary>
        /// 마스터 볼륨 가져오기
        /// </summary>
        public float GetMasterVolume()
        {
            return masterVolume;
        }
        
        /// <summary>
        /// 음악 볼륨 가져오기
        /// </summary>
        public float GetMusicVolume()
        {
            return musicVolume;
        }
        
        /// <summary>
        /// SFX 볼륨 가져오기
        /// </summary>
        public float GetSFXVolume()
        {
            return sfxVolume;
        }
        
        /// <summary>
        /// 음성 볼륨 가져오기
        /// </summary>
        public float GetVoiceVolume()
        {
            return voiceVolume;
        }
        
        /// <summary>
        /// 환경음 볼륨 가져오기
        /// </summary>
        public float GetAmbientVolume()
        {
            return ambientVolume;
        }
    }
}