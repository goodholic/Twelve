using UnityEngine;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 유닛 배치를 관리하는 매니저
    /// </summary>
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
        
        [Header("배치 설정")]
        public bool isPlacementMode = false;
        public GameObject selectedUnit;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }
        
        public void EnterPlacementMode()
        {
            isPlacementMode = true;
            Debug.Log("Placement mode activated");
        }
        
        public void ExitPlacementMode()
        {
            isPlacementMode = false;
            selectedUnit = null;
            Debug.Log("Placement mode deactivated");
        }
        
        public bool IsInPlacementMode()
        {
            return isPlacementMode;
        }
        
        public void SelectUnit(GameObject unit)
        {
            selectedUnit = unit;
        }
        
        public void PlaceUnit(Vector3 position)
        {
            if (selectedUnit != null)
            {
                selectedUnit.transform.position = position;
                Debug.Log($"Unit placed at {position}");
            }
        }
    }
} 