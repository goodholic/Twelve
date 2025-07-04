// Assets\Scripts\GameSceneManager.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GuildMaster.Systems;
using GuildMaster.Core;
using GuildMaster.Data;
using GuildMaster.Game;
using GuildMaster.Battle;
using GuildMaster.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using GuildMaster.Guild;

namespace GuildMaster.UI
{
    /// <summary>
    /// 게임 씬 관리자
    /// </summary>
    public class GameSceneManager : MonoBehaviour
    {
        public static GameSceneManager Instance { get; private set; }
        
        [Header("씬 참조")]
        public string mainMenuSceneName = "MainMenu";
        public string gameplaySceneName = "Gameplay";
        public string battleSceneName = "Battle";
        
        [Header("씬 전환 설정")]
        public bool useLoadingScreen = true;
        public float minLoadingTime = 1f;
        
        // 현재 씬 상태
        public string CurrentSceneName { get; private set; }
        public bool IsSceneTransitioning { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                CurrentSceneName = SceneManager.GetActiveScene().name;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 씬 로드
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (IsSceneTransitioning)
            {
                Debug.LogWarning("씬 전환 중입니다. 잠시 후 다시 시도해주세요.");
                return;
            }
            
            StartCoroutine(LoadSceneCoroutine(sceneName));
        }
        
        /// <summary>
        /// 씬 로드 코루틴
        /// </summary>
        private IEnumerator LoadSceneCoroutine(string sceneName)
        {
            IsSceneTransitioning = true;
            
            if (useLoadingScreen)
            {
                // 로딩 화면 표시
                ShowLoadingScreen();
                
                // 최소 로딩 시간 대기
                yield return new WaitForSeconds(minLoadingTime);
            }
            
            // 씬 비동기 로드
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;
            
            // 로딩 진행률 표시
            while (asyncLoad.progress < 0.9f)
            {
                UpdateLoadingProgress(asyncLoad.progress);
                yield return null;
            }
            
            UpdateLoadingProgress(1f);
            
            // 씬 활성화
            asyncLoad.allowSceneActivation = true;
            yield return asyncLoad;
            
            // 씬 전환 완료 처리
            CurrentSceneName = sceneName;
            OnSceneLoaded(sceneName);
            
            if (useLoadingScreen)
            {
                HideLoadingScreen();
            }
            
            IsSceneTransitioning = false;
        }
        
        /// <summary>
        /// 씬 로드 완료 처리
        /// </summary>
        private void OnSceneLoaded(string sceneName)
        {
            Debug.Log($"씬 로드 완료: {sceneName}");
            
            // 씬별 초기화 처리
            switch (sceneName)
            {
                case "MainMenu":
                    InitializeMainMenuScene();
                    break;
                case "Gameplay":
                    InitializeGameplayScene();
                    break;
                case "Battle":
                    InitializeBattleScene();
                    break;
            }
        }
        
        /// <summary>
        /// 메인 메뉴 씬 초기화
        /// </summary>
        private void InitializeMainMenuScene()
        {
            // 메인 메뉴 UI 초기화
        }
        
        /// <summary>
        /// 게임플레이 씬 초기화
        /// </summary>
        private void InitializeGameplayScene()
        {
            // 길드 관리 시스템 활성화
            if (GuildSimulationCore.Instance != null)
            {
                // GuildSimulationCore 초기화
                Debug.Log("길드 시뮬레이션 모드 활성화");
            }
            
            // 자동화 시스템 활성화
            if (IdleGameCore.Instance != null)
            {
                // IdleGameCore 초기화
                Debug.Log("아이들 게임 모드 활성화");
            }
        }
        
        /// <summary>
        /// 전투 씬 초기화
        /// </summary>
        private void InitializeBattleScene()
        {
            // 전투 시스템 활성화
            if (GuildBattleCore.Instance != null)
            {
                // GuildBattleCore 초기화
                Debug.Log("길드 전투 모드 활성화");
            }
        }
        
        /// <summary>
        /// 로딩 화면 표시
        /// </summary>
        private void ShowLoadingScreen()
        {
            // 로딩 화면 UI 표시
            Debug.Log("로딩 화면 표시");
        }
        
        /// <summary>
        /// 로딩 화면 숨기기
        /// </summary>
        private void HideLoadingScreen()
        {
            // 로딩 화면 UI 숨기기
            Debug.Log("로딩 화면 숨기기");
        }
        
        /// <summary>
        /// 로딩 진행률 업데이트
        /// </summary>
        private void UpdateLoadingProgress(float progress)
        {
            // 로딩 진행률 UI 업데이트
            Debug.Log($"로딩 진행률: {progress * 100:F1}%");
        }
        
        // 편의 메서드들
        public void LoadMainMenu() => LoadScene(mainMenuSceneName);
        public void LoadGameplay() => LoadScene(gameplaySceneName);
        public void LoadBattle() => LoadScene(battleSceneName);
        
        /// <summary>
        /// 게임 종료
        /// </summary>
        public void QuitGame()
        {
            Application.Quit();
        }
    }
}