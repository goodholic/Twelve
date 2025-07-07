using UnityEngine;

namespace GuildMaster.Data
{
    [System.Serializable]
    public class Tile1
    {
        public int x;
        public int y;
        public bool isOccupied;
        public GameObject occupiedBy;
        
        public Tile1(int x, int y)
        {
            this.x = x;
            this.y = y;
            this.isOccupied = false;
            this.occupiedBy = null;
        }
        
        public void SetOccupied(GameObject obj)
        {
            isOccupied = true;
            occupiedBy = obj;
        }
        
        public void ClearOccupied()
        {
            isOccupied = false;
            occupiedBy = null;
        }
        
        public Vector2 GetPosition()
        {
            return new Vector2(x, y);
        }
    }
} 