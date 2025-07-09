using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace TileConquest.Data
{
    /// <summary>
    /// 캐릭터별 공격 범위 패턴을 정의하는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "AttackRange", menuName = "TileConquest/Attack Range", order = 1)]
    public class AttackRangeSO : ScriptableObject
    {
        [Header("범위 정보")]
        public string rangeId;
        public string rangeName;
        
        [Header("범위 패턴")]
        [Tooltip("공격 범위를 2D 배열로 표현 (중심은 [1,1])")]
        public bool[,] rangePattern = new bool[3, 3];
        
        [Header("범위 타입")]
        public RangeType rangeType;
        
        [Header("범위 크기")]
        [Range(1, 6)]
        public int rangeSize = 1;
        
        [Header("설명")]
        [TextArea(2, 4)]
        public string description;
        
        /// <summary>
        /// 범위 패턴을 시각적으로 표현하기 위한 헬퍼 메서드
        /// </summary>
        public List<Vector2Int> GetAttackPositions(Vector2Int centerPos)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            
            switch (rangeType)
            {
                case RangeType.Cross:
                    // 십자 패턴
                    for (int i = 1; i <= rangeSize; i++)
                    {
                        positions.Add(centerPos + new Vector2Int(i, 0));
                        positions.Add(centerPos + new Vector2Int(-i, 0));
                        positions.Add(centerPos + new Vector2Int(0, i));
                        positions.Add(centerPos + new Vector2Int(0, -i));
                    }
                    break;
                    
                case RangeType.Diagonal:
                    // 대각선 패턴
                    for (int i = 1; i <= rangeSize; i++)
                    {
                        positions.Add(centerPos + new Vector2Int(i, i));
                        positions.Add(centerPos + new Vector2Int(-i, i));
                        positions.Add(centerPos + new Vector2Int(i, -i));
                        positions.Add(centerPos + new Vector2Int(-i, -i));
                    }
                    break;
                    
                case RangeType.Circle:
                    // 원형 패턴
                    for (int x = -rangeSize; x <= rangeSize; x++)
                    {
                        for (int y = -rangeSize; y <= rangeSize; y++)
                        {
                            if (x * x + y * y <= rangeSize * rangeSize)
                            {
                                positions.Add(centerPos + new Vector2Int(x, y));
                            }
                        }
                    }
                    break;
                    
                case RangeType.Square:
                    // 사각형 패턴
                    for (int x = -rangeSize; x <= rangeSize; x++)
                    {
                        for (int y = -rangeSize; y <= rangeSize; y++)
                        {
                            positions.Add(centerPos + new Vector2Int(x, y));
                        }
                    }
                    break;
                    
                case RangeType.Line:
                    // 직선 패턴 (가로 또는 세로)
                    for (int i = 1; i <= rangeSize; i++)
                    {
                        positions.Add(centerPos + new Vector2Int(i, 0));
                        positions.Add(centerPos + new Vector2Int(-i, 0));
                    }
                    break;
                    
                case RangeType.Custom:
                    // 커스텀 패턴 (rangePattern 사용)
                    int centerX = rangePattern.GetLength(0) / 2;
                    int centerY = rangePattern.GetLength(1) / 2;
                    
                    for (int x = 0; x < rangePattern.GetLength(0); x++)
                    {
                        for (int y = 0; y < rangePattern.GetLength(1); y++)
                        {
                            if (rangePattern[x, y])
                            {
                                positions.Add(centerPos + new Vector2Int(x - centerX, y - centerY));
                            }
                        }
                    }
                    break;
            }
            
            // 중심점 제거 (자기 자신 위치)
            positions.Remove(centerPos);
            
            return positions;
        }
        
        /// <summary>
        /// 타일 범위 내에 있는지 확인
        /// </summary>
        public bool IsInBounds(Vector2Int pos, int width, int height)
        {
            return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
        }
    }
    
    /// <summary>
    /// 범위 타입
    /// </summary>
    public enum RangeType
    {
        Cross,      // 십자 패턴
        Diagonal,   // 대각선 패턴
        Circle,     // 원형 패턴
        Square,     // 사각형 패턴
        Line,       // 직선 패턴
        Custom      // 커스텀 패턴
    }
}