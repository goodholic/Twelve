using UnityEngine;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 타일 클래스 - 게임 보드의 개별 타일을 나타냅니다.
    /// </summary>
    public class Tile : MonoBehaviour
    {
        [Header("타일 정보")]
        public int x;
        public int y;
        public bool isOccupied;
        public bool isWalkable = true;
        
        [Header("비주얼")]
        public SpriteRenderer spriteRenderer;
        public Color normalColor = Color.white;
        public Color highlightColor = Color.yellow;
        public Color occupiedColor = Color.red;
        
        private void Start()
        {
            UpdateVisual();
        }
        
        public void SetPosition(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        
        public void SetOccupied(bool occupied)
        {
            isOccupied = occupied;
            UpdateVisual();
        }
        
        public void SetHighlight(bool highlight)
        {
            if (spriteRenderer != null)
            {
                if (highlight)
                    spriteRenderer.color = highlightColor;
                else
                    UpdateVisual();
            }
        }
        
        private void UpdateVisual()
        {
            if (spriteRenderer != null)
            {
                if (isOccupied)
                    spriteRenderer.color = occupiedColor;
                else
                    spriteRenderer.color = normalColor;
            }
        }
        
        public Vector2Int GetPosition()
        {
            return new Vector2Int(x, y);
        }
        
        public bool CanPlaceUnit()
        {
            return isWalkable && !isOccupied;
        }
    }
} 