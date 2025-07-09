using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace TileConquest.Data
{
    /// <summary>
    /// 타일 전투 설정을 관리하는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "TileBattleConfig", menuName = "TileConquest/Battle Config", order = 2)]
    public class TileBattleConfigSO : ScriptableObject
    {
        [Header("보드 설정")]
        [Tooltip("타일 A (위쪽)의 크기")]
        public Vector2Int tileASize = new Vector2Int(6, 3);
        
        [Tooltip("타일 B (아래쪽)의 크기")]
        public Vector2Int tileBSize = new Vector2Int(6, 3);
        
        [Tooltip("타일 간 간격")]
        public float tileGap = 1.0f;
        
        [Header("게임 규칙")]
        [Tooltip("선택 가능한 최대 캐릭터 수")]
        public int maxCharacterSelection = 10;
        
        [Tooltip("턴당 배치 가능한 캐릭터 수")]
        public int charactersPerTurn = 1;
        
        [Tooltip("최대 턴 수 (0 = 무제한)")]
        public int maxTurns = 0;
        
        [Header("승리 조건")]
        [Tooltip("양쪽 타일 점령 시 점수")]
        public int bothTilesScore = 2;
        
        [Tooltip("한쪽 타일 점령 시 점수")]
        public int oneTileScore = 1;
        
        [Tooltip("타일 점령 실패 시 점수")]
        public int noTileScore = 0;
        
        [Header("타이머 설정")]
        [Tooltip("턴당 시간 제한 (초, 0 = 무제한)")]
        public float turnTimeLimit = 30f;
        
        [Tooltip("전체 게임 시간 제한 (초, 0 = 무제한)")]
        public float gameTimeLimit = 0f;
        
        [Header("AI 설정")]
        [Tooltip("AI 난이도")]
        public AIDifficulty aiDifficulty = AIDifficulty.Normal;
        
        [Tooltip("AI 생각 시간 (초)")]
        [Range(0.5f, 5f)]
        public float aiThinkTime = 2f;
        
        [Header("시각 효과")]
        [Tooltip("캐릭터 배치 애니메이션 시간")]
        public float placementAnimTime = 0.5f;
        
        [Tooltip("공격 애니메이션 시간")]
        public float attackAnimTime = 1f;
        
        [Tooltip("타일 점령 효과 시간")]
        public float captureEffectTime = 1.5f;
        
        [Header("점수 계산")]
        [Tooltip("기본 점수")]
        public int baseScore = 100;
        
        [Tooltip("남은 캐릭터당 보너스 점수")]
        public int remainingCharacterBonus = 10;
        
        [Tooltip("빠른 승리 보너스 (턴 수 기준)")]
        public int quickVictoryTurns = 10;
        public int quickVictoryBonus = 50;
        
        /// <summary>
        /// 타일 점령 판정
        /// </summary>
        public TileOwnership CalculateTileOwnership(int playerCount, int enemyCount)
        {
            if (playerCount > enemyCount)
                return TileOwnership.Player;
            else if (enemyCount > playerCount)
                return TileOwnership.Enemy;
            else
                return TileOwnership.Neutral;
        }
        
        /// <summary>
        /// 게임 결과 계산
        /// </summary>
        public GameResult CalculateGameResult(TileOwnership tileA, TileOwnership tileB)
        {
            int playerTiles = 0;
            if (tileA == TileOwnership.Player) playerTiles++;
            if (tileB == TileOwnership.Player) playerTiles++;
            
            int enemyTiles = 0;
            if (tileA == TileOwnership.Enemy) enemyTiles++;
            if (tileB == TileOwnership.Enemy) enemyTiles++;
            
            if (playerTiles > enemyTiles)
            {
                if (playerTiles == 2)
                    return GameResult.Victory;
                else
                    return GameResult.Draw;
            }
            else if (enemyTiles > playerTiles)
            {
                return GameResult.Defeat;
            }
            else
            {
                return GameResult.Draw;
            }
        }
        
        /// <summary>
        /// 최종 점수 계산
        /// </summary>
        public int CalculateFinalScore(GameResult result, int turnsUsed, int charactersRemaining)
        {
            int score = 0;
            
            // 기본 점수
            switch (result)
            {
                case GameResult.Victory:
                    score = bothTilesScore * baseScore;
                    break;
                case GameResult.Draw:
                    score = oneTileScore * baseScore;
                    break;
                case GameResult.Defeat:
                    score = noTileScore;
                    break;
            }
            
            // 남은 캐릭터 보너스
            score += charactersRemaining * remainingCharacterBonus;
            
            // 빠른 승리 보너스
            if (result == GameResult.Victory && turnsUsed <= quickVictoryTurns)
            {
                score += quickVictoryBonus;
            }
            
            return score;
        }
    }
    
    /// <summary>
    /// AI 난이도
    /// </summary>
    public enum AIDifficulty
    {
        Easy,       // 랜덤 배치
        Normal,     // 기본 전략 사용
        Hard,       // 고급 전략 사용
        Expert      // 최적화된 전략 사용
    }
    
    /// <summary>
    /// 타일 소유권
    /// </summary>
    public enum TileOwnership
    {
        Neutral,
        Player,
        Enemy
    }
    
    /// <summary>
    /// 게임 결과
    /// </summary>
    public enum GameResult
    {
        Victory,    // 2점 - 승리
        Draw,       // 1점 - 무승부
        Defeat      // 0점 - 패배
    }
}