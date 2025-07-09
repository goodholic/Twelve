using UnityEngine;
using System.Collections.Generic;

namespace TacticalTileGame.Data
{
    /// <summary>
    /// 캐릭터의 공격 범위 패턴을 정의하는 클래스
    /// </summary>
    [System.Serializable]
    public class AttackPattern
    {
        public string patternName;
        public List<Vector2Int> attackTiles; // 공격 가능한 타일 위치 (상대 좌표)
        
        public AttackPattern()
        {
            attackTiles = new List<Vector2Int>();
        }
        
        public AttackPattern(string name)
        {
            patternName = name;
            attackTiles = new List<Vector2Int>();
        }
        
        /// <summary>
        /// CSV 문자열에서 공격 패턴 파싱
        /// 예: "1,0;-1,0;0,1;0,-1" (십자 패턴)
        /// </summary>
        public static AttackPattern ParseFromString(string patternString)
        {
            var pattern = new AttackPattern();
            if (string.IsNullOrEmpty(patternString)) return pattern;
            
            string[] positions = patternString.Split(';');
            foreach (string pos in positions)
            {
                string[] coords = pos.Split(',');
                if (coords.Length == 2)
                {
                    if (int.TryParse(coords[0], out int x) && int.TryParse(coords[1], out int y))
                    {
                        pattern.attackTiles.Add(new Vector2Int(x, y));
                    }
                }
            }
            
            return pattern;
        }
        
        /// <summary>
        /// 공격 패턴을 CSV 문자열로 변환
        /// </summary>
        public string ToCSVString()
        {
            List<string> positions = new List<string>();
            foreach (var tile in attackTiles)
            {
                positions.Add($"{tile.x},{tile.y}");
            }
            return string.Join(";", positions);
        }
    }
    
    /// <summary>
    /// 미리 정의된 공격 패턴들
    /// </summary>
    public static class AttackPatternPresets
    {
        // 십자 패턴 (상하좌우)
        public static AttackPattern CrossPattern()
        {
            var pattern = new AttackPattern("Cross");
            pattern.attackTiles.Add(new Vector2Int(0, 1));   // 위
            pattern.attackTiles.Add(new Vector2Int(0, -1));  // 아래
            pattern.attackTiles.Add(new Vector2Int(1, 0));   // 오른쪽
            pattern.attackTiles.Add(new Vector2Int(-1, 0));  // 왼쪽
            return pattern;
        }
        
        // 대각선 패턴
        public static AttackPattern DiagonalPattern()
        {
            var pattern = new AttackPattern("Diagonal");
            pattern.attackTiles.Add(new Vector2Int(1, 1));    // 우상
            pattern.attackTiles.Add(new Vector2Int(1, -1));   // 우하
            pattern.attackTiles.Add(new Vector2Int(-1, 1));   // 좌상
            pattern.attackTiles.Add(new Vector2Int(-1, -1));  // 좌하
            return pattern;
        }
        
        // 주변 8칸 패턴
        public static AttackPattern SurroundPattern()
        {
            var pattern = new AttackPattern("Surround");
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x != 0 || y != 0) // 중심 제외
                    {
                        pattern.attackTiles.Add(new Vector2Int(x, y));
                    }
                }
            }
            return pattern;
        }
        
        // 직선 패턴 (앞으로 3칸)
        public static AttackPattern LinePattern(int range = 3)
        {
            var pattern = new AttackPattern($"Line{range}");
            for (int i = 1; i <= range; i++)
            {
                pattern.attackTiles.Add(new Vector2Int(0, i));
            }
            return pattern;
        }
        
        // L자 패턴 (체스 나이트)
        public static AttackPattern KnightPattern()
        {
            var pattern = new AttackPattern("Knight");
            pattern.attackTiles.Add(new Vector2Int(2, 1));
            pattern.attackTiles.Add(new Vector2Int(2, -1));
            pattern.attackTiles.Add(new Vector2Int(-2, 1));
            pattern.attackTiles.Add(new Vector2Int(-2, -1));
            pattern.attackTiles.Add(new Vector2Int(1, 2));
            pattern.attackTiles.Add(new Vector2Int(1, -2));
            pattern.attackTiles.Add(new Vector2Int(-1, 2));
            pattern.attackTiles.Add(new Vector2Int(-1, -2));
            return pattern;
        }
        
        // 원형 범위 패턴
        public static AttackPattern CirclePattern(int radius = 2)
        {
            var pattern = new AttackPattern($"Circle{radius}");
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x * x + y * y <= radius * radius && (x != 0 || y != 0))
                    {
                        pattern.attackTiles.Add(new Vector2Int(x, y));
                    }
                }
            }
            return pattern;
        }
    }
}