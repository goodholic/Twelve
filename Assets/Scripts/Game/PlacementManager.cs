using UnityEngine;

namespace GuildMaster.Game
{
    public class PlacementManager : MonoBehaviour
    {
        private static PlacementManager instance;
        public static PlacementManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<PlacementManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("PlacementManager");
                        instance = go.AddComponent<PlacementManager>();
                    }
                }
                return instance;
            }
        }

        [Header("UI 참조")]
        public Transform bulletPanel;
        
        private int playerCharacterCount = 0;
        private int aiCharacterCount = 0;
        
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
        
        /// <summary>
        /// 캐릭터 수 가져오기
        /// </summary>
        /// <param name="isAI">AI 캐릭터 여부</param>
        /// <returns>캐릭터 수</returns>
        public int GetCharacterCount(bool isAI)
        {
            return isAI ? aiCharacterCount : playerCharacterCount;
        }
        
        /// <summary>
        /// 캐릭터 수 증가
        /// </summary>
        /// <param name="isAI">AI 캐릭터 여부</param>
        public void IncrementCharacterCount(bool isAI)
        {
            if (isAI)
                aiCharacterCount++;
            else
                playerCharacterCount++;
        }
        
        /// <summary>
        /// 캐릭터 수 감소
        /// </summary>
        /// <param name="isAI">AI 캐릭터 여부</param>
        public void DecrementCharacterCount(bool isAI)
        {
            if (isAI)
                aiCharacterCount = Mathf.Max(0, aiCharacterCount - 1);
            else
                playerCharacterCount = Mathf.Max(0, playerCharacterCount - 1);
        }
        
        /// <summary>
        /// 캐릭터 수 초기화
        /// </summary>
        public void ResetCharacterCounts()
        {
            playerCharacterCount = 0;
            aiCharacterCount = 0;
        }
    }
} 