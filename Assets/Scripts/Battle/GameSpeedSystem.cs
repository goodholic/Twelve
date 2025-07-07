using UnityEngine;
using System;

namespace GuildMaster.Systems
{
    public class GameSpeedSystem : MonoBehaviour
    {
        private static GameSpeedSystem _instance;
        public static GameSpeedSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameSpeedSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameSpeedSystem");
                        _instance = go.AddComponent<GameSpeedSystem>();
                    }
                }
                return _instance;
            }
        }
        
        // 게임 속도 설정
        public enum SpeedSetting
        {
            Normal = 1,
            Fast = 2,
            VeryFast = 4
        }
        
        private SpeedSetting currentSpeed = SpeedSetting.Normal;
        private float baseTimeScale = 1f;
        
        // 이벤트
        public event Action<SpeedSetting> OnSpeedChanged;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        // 게임 속도 설정
        public void SetGameSpeed(SpeedSetting speed)
        {
            currentSpeed = speed;
            Time.timeScale = baseTimeScale * (int)speed;
            OnSpeedChanged?.Invoke(speed);
            
            Debug.Log($"Game speed set to {speed}x");
        }
        
        // 다음 속도로 순환
        public void CycleSpeed()
        {
            switch (currentSpeed)
            {
                case SpeedSetting.Normal:
                    SetGameSpeed(SpeedSetting.Fast);
                    break;
                case SpeedSetting.Fast:
                    SetGameSpeed(SpeedSetting.VeryFast);
                    break;
                case SpeedSetting.VeryFast:
                    SetGameSpeed(SpeedSetting.Normal);
                    break;
            }
        }
        
        // 일시정지
        public void Pause()
        {
            Time.timeScale = 0f;
        }
        
        // 재개
        public void Resume()
        {
            Time.timeScale = baseTimeScale * (int)currentSpeed;
        }
        
        // 현재 속도 가져오기
        public SpeedSetting GetCurrentSpeed()
        {
            return currentSpeed;
        }
        
        // 전투 속도 배율 가져오기 (애니메이션 등에 사용)
        public float GetSpeedMultiplier()
        {
            return (int)currentSpeed;
        }
        
        void OnDestroy()
        {
            // 게임 종료 시 타임스케일 복원
            Time.timeScale = 1f;
        }
    }
}