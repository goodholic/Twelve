using UnityEngine;
using System;
using System.Collections.Generic;
using GuildMaster.Core;
using GuildMaster.Guild;
using GuildMaster.Data;


namespace GuildMaster.Game
{
    /// <summary>
    /// 건물 및 오브젝트 배치 관리 시스템
    /// </summary>
    public class PlacementManager : MonoBehaviour
    {
        private static PlacementManager _instance;
        public static PlacementManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PlacementManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("PlacementManager");
                        _instance = go.AddComponent<PlacementManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Placement Settings")]
        [SerializeField] private LayerMask groundLayer = 1;
        [SerializeField] private LayerMask obstacleLayer = 8;
        [SerializeField] private GameObject placementPreviewPrefab;
        [SerializeField] private float gridSize = 1f;
        [SerializeField] private bool snapToGrid = true;

        [Header("Building Prefabs")]
        [SerializeField] private List<BuildingPrefabData> buildingPrefabs = new List<BuildingPrefabData>();

        // Current placement state
        private GameObject currentPreview;
        private BuildingType currentBuildingType;
        private bool isPlacing = false;
        private Vector3 lastValidPosition;

        // Placement grid
        private Dictionary<Vector2Int, PlacedObject> placementGrid = new Dictionary<Vector2Int, PlacedObject>();

        // Events
        public event Action<BuildingType, Vector3> OnBuildingPlaced;
        public event Action<Vector3> OnBuildingRemoved;
        public event Action<bool> OnPlacementModeChanged;

        [System.Serializable]
        public class BuildingPrefabData
        {
            public BuildingType buildingType;
            public GameObject prefab;
            public Vector2Int size = Vector2Int.one;
            public string displayName;
            public Sprite icon;
        }

        [System.Serializable]
        public class PlacedObject
        {
            public GameObject gameObject;
            public BuildingType buildingType;
            public Vector2Int gridPosition;
            public Vector2Int size;
            public string id;

            public PlacedObject(GameObject obj, BuildingType type, Vector2Int pos, Vector2Int objSize)
            {
                gameObject = obj;
                buildingType = type;
                gridPosition = pos;
                size = objSize;
                id = Guid.NewGuid().ToString();
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            if (isPlacing)
            {
                HandlePlacementInput();
                UpdatePreviewPosition();
            }
        }

        public void StartPlacement(BuildingType buildingType)
        {
            if (isPlacing)
            {
                CancelPlacement();
            }

            currentBuildingType = buildingType;
            isPlacing = true;
            CreatePreview();
            OnPlacementModeChanged?.Invoke(true);
        }

        public void CancelPlacement()
        {
            if (currentPreview != null)
            {
                Destroy(currentPreview);
                currentPreview = null;
            }

            isPlacing = false;
            OnPlacementModeChanged?.Invoke(false);
        }

        void CreatePreview()
        {
            var buildingData = GetBuildingData(currentBuildingType);
            if (buildingData?.prefab != null)
            {
                currentPreview = Instantiate(buildingData.prefab);
                
                // Make it semi-transparent
                var renderers = currentPreview.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    foreach (var material in renderer.materials)
                    {
                        if (material.HasProperty("_Color"))
                        {
                            Color color = material.color;
                            color.a = 0.5f;
                            material.color = color;
                        }
                    }
                }

                // Disable colliders
                var colliders = currentPreview.GetComponentsInChildren<Collider>();
                foreach (var collider in colliders)
                {
                    collider.enabled = false;
                }
            }
        }

        void HandlePlacementInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceBuilding();
            }
            else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
            }
        }

        void UpdatePreviewPosition()
        {
            if (currentPreview == null) return;

            Vector3 mousePosition = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                Vector3 targetPosition = hit.point;

                if (snapToGrid)
                {
                    targetPosition = SnapToGrid(targetPosition);
                }

                currentPreview.transform.position = targetPosition;
                lastValidPosition = targetPosition;

                // Check if position is valid
                bool isValid = IsPositionValid(targetPosition);
                SetPreviewColor(isValid);
            }
        }

        Vector3 SnapToGrid(Vector3 position)
        {
            float snappedX = Mathf.Round(position.x / gridSize) * gridSize;
            float snappedZ = Mathf.Round(position.z / gridSize) * gridSize;
            return new Vector3(snappedX, position.y, snappedZ);
        }

        bool IsPositionValid(Vector3 position)
        {
            Vector2Int gridPos = WorldToGridPosition(position);
            var buildingData = GetBuildingData(currentBuildingType);

            if (buildingData == null) return false;

            // Check if all grid cells are free
            for (int x = 0; x < buildingData.size.x; x++)
            {
                for (int y = 0; y < buildingData.size.y; y++)
                {
                    Vector2Int checkPos = gridPos + new Vector2Int(x, y);
                    if (placementGrid.ContainsKey(checkPos))
                    {
                        return false;
                    }
                }
            }

            // Check for obstacles
            Bounds bounds = GetBuildingBounds(position, buildingData.size);
            return !Physics.CheckBox(bounds.center, bounds.extents, Quaternion.identity, obstacleLayer);
        }

        void TryPlaceBuilding()
        {
            if (!IsPositionValid(lastValidPosition))
            {
                Debug.LogWarning("Cannot place building at this position!");
                return;
            }

            // Place the building
            var buildingData = GetBuildingData(currentBuildingType);
            if (buildingData?.prefab != null)
            {
                GameObject building = Instantiate(buildingData.prefab, lastValidPosition, Quaternion.identity);
                
                // Restore original materials
                RestoreOriginalMaterials(building);

                // Add to grid
                Vector2Int gridPos = WorldToGridPosition(lastValidPosition);
                var placedObject = new PlacedObject(building, currentBuildingType, gridPos, buildingData.size);
                
                for (int x = 0; x < buildingData.size.x; x++)
                {
                    for (int y = 0; y < buildingData.size.y; y++)
                    {
                        Vector2Int pos = gridPos + new Vector2Int(x, y);
                        placementGrid[pos] = placedObject;
                    }
                }

                OnBuildingPlaced?.Invoke(currentBuildingType, lastValidPosition);
                CancelPlacement();
            }
        }

        void SetPreviewColor(bool isValid)
        {
            if (currentPreview == null) return;

            Color color = isValid ? Color.green : Color.red;
            color.a = 0.5f;

            var renderers = currentPreview.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_Color"))
                    {
                        material.color = color;
                    }
                }
            }
        }

        void RestoreOriginalMaterials(GameObject building)
        {
            var buildingData = GetBuildingData(currentBuildingType);
            if (buildingData?.prefab == null) return;

            var originalRenderers = buildingData.prefab.GetComponentsInChildren<Renderer>();
            var newRenderers = building.GetComponentsInChildren<Renderer>();

            for (int i = 0; i < Mathf.Min(originalRenderers.Length, newRenderers.Length); i++)
            {
                newRenderers[i].materials = originalRenderers[i].materials;
            }
        }

        public bool RemoveBuilding(Vector3 position)
        {
            Vector2Int gridPos = WorldToGridPosition(position);
            
            if (placementGrid.TryGetValue(gridPos, out PlacedObject placedObject))
            {
                // Remove from grid
                for (int x = 0; x < placedObject.size.x; x++)
                {
                    for (int y = 0; y < placedObject.size.y; y++)
                    {
                        Vector2Int pos = placedObject.gridPosition + new Vector2Int(x, y);
                        placementGrid.Remove(pos);
                    }
                }

                // Destroy object
                if (placedObject.gameObject != null)
                {
                    Destroy(placedObject.gameObject);
                }

                OnBuildingRemoved?.Invoke(position);
                return true;
            }

            return false;
        }

        BuildingPrefabData GetBuildingData(BuildingType buildingType)
        {
            return buildingPrefabs.Find(data => data.buildingType == buildingType);
        }

        Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPosition.x / gridSize),
                Mathf.RoundToInt(worldPosition.z / gridSize)
            );
        }

        Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            return new Vector3(gridPosition.x * gridSize, 0, gridPosition.y * gridSize);
        }

        Bounds GetBuildingBounds(Vector3 position, Vector2Int size)
        {
            Vector3 center = position + new Vector3(size.x * gridSize * 0.5f, 0, size.y * gridSize * 0.5f);
            Vector3 sizeVector = new Vector3(size.x * gridSize, 2f, size.y * gridSize);
            return new Bounds(center, sizeVector);
        }

        public List<PlacedObject> GetAllPlacedObjects()
        {
            var uniqueObjects = new HashSet<PlacedObject>();
            foreach (var placedObject in placementGrid.Values)
            {
                uniqueObjects.Add(placedObject);
            }
            return new List<PlacedObject>(uniqueObjects);
        }

        public PlacedObject GetPlacedObjectAt(Vector3 position)
        {
            Vector2Int gridPos = WorldToGridPosition(position);
            placementGrid.TryGetValue(gridPos, out PlacedObject placedObject);
            return placedObject;
        }

        public bool IsPlacing => isPlacing;
        public BuildingType CurrentBuildingType => currentBuildingType;
    }
} 