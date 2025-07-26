using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Data;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 게임 초기화 및 테스트를 위한 스크립트
    /// 삭제된 타입들로 인해 대부분의 기능이 주석 처리됨
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [Header("테스트 설정")]
        [SerializeField] private bool autoStartBattle = true;
        // AITurnController 타입이 삭제되어 주석 처리
        // [SerializeField] private AITurnController.AIDifficulty aiDifficulty = AITurnController.AIDifficulty.Normal;
        
        [Header("테스트용 캐릭터 데이터")]
        [SerializeField] private List<CharacterData> testPlayerCharacters = new List<CharacterData>();
        [SerializeField] private List<CharacterData> testEnemyCharacters = new List<CharacterData>();
        
        // [Header("매니저 참조")] - 삭제된 타입들 주석 처리
        // private TurnBasedBattleManager battleManager;
        // private TileGridManager tileGridManager;
        // private AITurnController aiController;
        
        void Start()
        {
            // 삭제된 타입들로 인해 주석 처리
            // InitializeManagers();
            
            if (autoStartBattle)
            {
                // StartTestBattle();
                Debug.Log("GameInitializer: 삭제된 타입들로 인해 테스트 전투를 시작할 수 없습니다.");
            }
        }
        
        /*
         * 아래의 모든 메서드들은 삭제된 타입들(TurnBasedBattleManager, TileGridManager, 
         * AITurnController, JobClass 등)을 사용하므로 주석 처리되었습니다.
         * 
         * 포함된 메서드들:
         * - InitializeManagers()
         * - StartTestBattle()
         * - CreateTestCharacters()
         * - CreateTestCharacter()
         * - ApplyJobClassModifiers()
         * - ApplyRarityModifiers()
         * - CreateDebugCharacters()
         */
    }
}