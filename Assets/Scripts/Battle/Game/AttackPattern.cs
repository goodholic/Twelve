using UnityEngine;
using System.Collections.Generic;

namespace GuildMaster.Data
{
    [System.Serializable]
    public enum AttackPatternType
    {
        Single,
        Line,
        Cross,
        Square,
        Circle,
        Random,
        All,
        Front,
        Back,
        Self,
        None
    }
}

namespace GuildMaster.Battle
{
    /// <summary>
    /// 캐릭터의 공격 범위 패턴을 정의하는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "AttackPattern", menuName = "GuildMaster/Battle/AttackPattern", order = 1)]
    public class AttackPattern : ScriptableObject
    {
        [Header("패턴 정보")]
        [SerializeField] private string patternName;
        [SerializeField] private PatternType patternType;
        [SerializeField] private int range = 1;
        
        [Header("패턴 데이터")]
        [SerializeField] private List<Vector2Int> rangeOffsets = new List<Vector2Int>();
        
        [Header("시각화")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color gizmoColor = Color.red;
        
        /// <summary>
        /// 패턴 타입
        /// </summary>
        public enum PatternType
        {
            Single,     // 단일 타겟
            Adjacent,   // 인접 4방향
            Cross,      // 십자 모양
            Diagonal,   // 대각선
            Square,     // 정사각형 범위
            Line,       // 직선
            Circle,     // 원형
            Custom      // 커스텀 패턴
        }
        
        /// <summary>
        /// 패턴 초기화
        /// </summary>
        public void Initialize(PatternType type, int range)
        {
            this.patternType = type;
            this.range = range;
            this.patternName = $"{type} Range {range}";
            
            GeneratePattern();
        }
        
        /// <summary>
        /// 패턴 생성
        /// </summary>
        void GeneratePattern()
        {
            rangeOffsets.Clear();
            
            switch (patternType)
            {
                case PatternType.Single:
                    GenerateSinglePattern();
                    break;
                case PatternType.Adjacent:
                    GenerateAdjacentPattern();
                    break;
                case PatternType.Cross:
                    GenerateCrossPattern();
                    break;
                case PatternType.Diagonal:
                    GenerateDiagonalPattern();
                    break;
                case PatternType.Square:
                    GenerateSquarePattern();
                    break;
                case PatternType.Line:
                    GenerateLinePattern();
                    break;
                case PatternType.Circle:
                    GenerateCirclePattern();
                    break;
                case PatternType.Custom:
                    // 커스텀 패턴은 수동으로 설정
                    break;
            }
        }
        
        /// <summary>
        /// 단일 타겟 패턴
        /// </summary>
        void GenerateSinglePattern()
        {
            // 전방 1칸
            rangeOffsets.Add(new Vector2Int(1, 0));
        }
        
        /// <summary>
        /// 인접 4방향 패턴
        /// </summary>
        void GenerateAdjacentPattern()
        {
            rangeOffsets.Add(new Vector2Int(1, 0));   // 오른쪽
            rangeOffsets.Add(new Vector2Int(-1, 0));  // 왼쪽
            rangeOffsets.Add(new Vector2Int(0, 1));   // 위
            rangeOffsets.Add(new Vector2Int(0, -1));  // 아래
        }
        
        /// <summary>
        /// 십자 패턴
        /// </summary>
        void GenerateCrossPattern()
        {
            for (int i = 1; i <= range; i++)
            {
                rangeOffsets.Add(new Vector2Int(i, 0));   // 오른쪽
                rangeOffsets.Add(new Vector2Int(-i, 0));  // 왼쪽
                rangeOffsets.Add(new Vector2Int(0, i));   // 위
                rangeOffsets.Add(new Vector2Int(0, -i));  // 아래
            }
        }
        
        /// <summary>
        /// 대각선 패턴
        /// </summary>
        void GenerateDiagonalPattern()
        {
            for (int i = 1; i <= range; i++)
            {
                rangeOffsets.Add(new Vector2Int(i, i));    // 오른쪽 위
                rangeOffsets.Add(new Vector2Int(i, -i));   // 오른쪽 아래
                rangeOffsets.Add(new Vector2Int(-i, i));   // 왼쪽 위
                rangeOffsets.Add(new Vector2Int(-i, -i));  // 왼쪽 아래
            }
        }
        
        /// <summary>
        /// 정사각형 패턴
        /// </summary>
        void GenerateSquarePattern()
        {
            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    if (x == 0 && y == 0) continue; // 자기 자신 제외
                    rangeOffsets.Add(new Vector2Int(x, y));
                }
            }
        }
        
        /// <summary>
        /// 직선 패턴 (전방)
        /// </summary>
        void GenerateLinePattern()
        {
            for (int i = 1; i <= range; i++)
            {
                rangeOffsets.Add(new Vector2Int(i, 0)); // 전방 직선
            }
        }
        
        /// <summary>
        /// 원형 패턴
        /// </summary>
        void GenerateCirclePattern()
        {
            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    if (x == 0 && y == 0) continue; // 자기 자신 제외
                    
                    // 원형 범위 체크 (맨하탄 거리)
                    if (Mathf.Abs(x) + Mathf.Abs(y) <= range)
                    {
                        rangeOffsets.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
        
        /// <summary>
        /// 범위 오프셋 가져오기
        /// </summary>
        public List<Vector2Int> GetRangeOffsets()
        {
            return new List<Vector2Int>(rangeOffsets);
        }
        
        /// <summary>
        /// 특정 방향으로 회전된 패턴 가져오기
        /// </summary>
        public List<Vector2Int> GetRotatedPattern(Direction direction)
        {
            List<Vector2Int> rotatedOffsets = new List<Vector2Int>();
            
            foreach (var offset in rangeOffsets)
            {
                Vector2Int rotated = offset;
                
                switch (direction)
                {
                    case Direction.Right:
                        // 기본 방향 (변경 없음)
                        break;
                    case Direction.Left:
                        rotated = new Vector2Int(-offset.x, offset.y);
                        break;
                    case Direction.Up:
                        rotated = new Vector2Int(offset.y, offset.x);
                        break;
                    case Direction.Down:
                        rotated = new Vector2Int(offset.y, -offset.x);
                        break;
                }
                
                rotatedOffsets.Add(rotated);
            }
            
            return rotatedOffsets;
        }
        
        /// <summary>
        /// 커스텀 패턴 추가
        /// </summary>
        public void AddCustomOffset(Vector2Int offset)
        {
            if (patternType != PatternType.Custom)
            {
                Debug.LogWarning("커스텀 오프셋은 Custom 패턴 타입에서만 추가 가능합니다.");
                return;
            }
            
            if (!rangeOffsets.Contains(offset))
            {
                rangeOffsets.Add(offset);
            }
        }
        
        /// <summary>
        /// 커스텀 패턴 제거
        /// </summary>
        public void RemoveCustomOffset(Vector2Int offset)
        {
            if (patternType != PatternType.Custom)
            {
                Debug.LogWarning("커스텀 오프셋은 Custom 패턴 타입에서만 제거 가능합니다.");
                return;
            }
            
            rangeOffsets.Remove(offset);
        }
        
        /// <summary>
        /// 패턴 시각화 (에디터용)
        /// </summary>
        public void DrawGizmos(Vector3 centerPosition, float tileSize)
        {
            if (!showGizmos) return;
            
            Gizmos.color = gizmoColor;
            
            foreach (var offset in rangeOffsets)
            {
                Vector3 tilePos = centerPosition + new Vector3(offset.x * tileSize, 0, offset.y * tileSize);
                Gizmos.DrawWireCube(tilePos, Vector3.one * tileSize * 0.9f);
            }
        }
    }
    
    /// <summary>
    /// 방향
    /// </summary>
    public enum Direction
    {
        Right,
        Left,
        Up,
        Down
    }
}