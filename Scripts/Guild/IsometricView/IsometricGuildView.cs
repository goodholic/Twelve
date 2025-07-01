using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuildMaster.Guild
{
    public class IsometricGuildView : MonoBehaviour
    {
        // Camera configuration
        [Header("Camera Settings")]
        [SerializeField] private Camera guildCamera;
        [SerializeField] private float cameraHeight = 15f;
        [SerializeField] private float cameraAngle = 45f;
        [SerializeField] private float cameraDistance = 20f;
        [SerializeField] private float minZoom = 10f;
        [SerializeField] private float maxZoom = 30f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float panSpeed = 0.5f;
        
        // Grid configuration
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 20;
        [SerializeField] private int gridHeight = 20;
        [SerializeField] private float cellSize = 2f;
        [SerializeField] private GameObject gridCellPrefab;
        [SerializeField] private Transform gridContainer;
        
        // Building placement
        [Header("Building System")]
        [SerializeField] private GameObject[] buildingPrefabs;
        [SerializeField] private Material validPlacementMaterial;
        [SerializeField] private Material invalidPlacementMaterial;
        [SerializeField] private LayerMask placementLayerMask;
        
        // Visual effects
        [Header("Visual Effects")]
        [SerializeField] private GameObject selectionHighlight;
        [SerializeField] private GameObject buildModeGrid;
        [SerializeField] private ParticleSystem constructionEffect;
        [SerializeField] private ParticleSystem upgradeEffect;
        
        // Grid data
        private GridCell[,] gridCells;
        private Dictionary<Vector2Int, Building> placedBuildings;
        private Building selectedBuilding;
        private GameObject previewBuilding;
        private bool isInBuildMode = false;
        
        // Camera control
        private Vector3 cameraTarget;
        private float currentZoom;
        private Vector3 lastMousePosition;
        private bool isDragging = false;
        
        // Events
        public event Action<Building> OnBuildingPlaced;
        public event Action<Building> OnBuildingRemoved;
        public event Action<Building> OnBuildingSelected;
        public event Action<Vector2Int> OnCellHovered;
        
        [System.Serializable]
        public class GridCell
        {
            public Vector2Int GridPosition { get; set; }
            public Vector3 WorldPosition { get; set; }
            public bool IsOccupied { get; set; }
            public Building OccupyingBuilding { get; set; }
            public GameObject CellObject { get; set; }
            public bool IsValidForPlacement { get; set; } = true;
            
            public GridCell(Vector2Int gridPos, Vector3 worldPos)
            {
                GridPosition = gridPos;
                WorldPosition = worldPos;
                IsOccupied = false;
            }
        }
        
        void Awake()
        {
            placedBuildings = new Dictionary<Vector2Int, Building>();
            InitializeCamera();
            GenerateGrid();
        }
        
        void InitializeCamera()
        {
            if (guildCamera == null)
            {
                guildCamera = Camera.main;
            }
            
            currentZoom = cameraDistance;
            cameraTarget = new Vector3(gridWidth * cellSize / 2f, 0, gridHeight * cellSize / 2f);
            UpdateCameraPosition();
        }
        
        void GenerateGrid()
        {
            gridCells = new GridCell[gridWidth, gridHeight];
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    Vector3 worldPos = GridToWorldPosition(new Vector2Int(x, z));
                    gridCells[x, z] = new GridCell(new Vector2Int(x, z), worldPos);
                    
                    if (gridCellPrefab != null && gridContainer != null)
                    {
                        GameObject cell = Instantiate(gridCellPrefab, worldPos, Quaternion.identity, gridContainer);
                        cell.name = $"GridCell_{x}_{z}";
                        gridCells[x, z].CellObject = cell;
                        
                        // Set up cell visuals
                        var renderer = cell.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.enabled = false; // Hidden by default
                        }
                    }
                }
            }
            
            // Mark some cells as invalid (for obstacles, terrain, etc.)
            GenerateTerrainObstacles();
        }
        
        void GenerateTerrainObstacles()
        {
            // Create some random obstacles for visual variety
            System.Random random = new System.Random();
            
            // Central plaza area (always clear)
            for (int x = 8; x < 12; x++)
            {
                for (int z = 8; z < 12; z++)
                {
                    gridCells[x, z].IsValidForPlacement = true;
                }
            }
            
            // Random decorative elements
            int obstacleCount = random.Next(10, 20);
            for (int i = 0; i < obstacleCount; i++)
            {
                int x = random.Next(0, gridWidth);
                int z = random.Next(0, gridHeight);
                
                // Don't place obstacles in central area
                if (x >= 8 && x < 12 && z >= 8 && z < 12) continue;
                
                gridCells[x, z].IsValidForPlacement = false;
            }
        }
        
        void Update()
        {
            HandleCameraControls();
            HandleBuildingPlacement();
            HandleBuildingSelection();
        }
        
        void HandleCameraControls()
        {
            // Zoom
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            if (scrollDelta != 0)
            {
                currentZoom -= scrollDelta * zoomSpeed;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
                UpdateCameraPosition();
            }
            
            // Pan
            if (Input.GetMouseButtonDown(2)) // Middle mouse button
            {
                isDragging = true;
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(2))
            {
                isDragging = false;
            }
            
            if (isDragging)
            {
                Vector3 delta = Input.mousePosition - lastMousePosition;
                cameraTarget += new Vector3(-delta.x, 0, -delta.y) * panSpeed;
                
                // Clamp camera target to grid bounds
                float halfWidth = gridWidth * cellSize / 2f;
                float halfHeight = gridHeight * cellSize / 2f;
                cameraTarget.x = Mathf.Clamp(cameraTarget.x, -halfWidth, halfWidth * 3);
                cameraTarget.z = Mathf.Clamp(cameraTarget.z, -halfHeight, halfHeight * 3);
                
                lastMousePosition = Input.mousePosition;
                UpdateCameraPosition();
            }
            
            // Edge scrolling
            float edgeScrollSpeed = panSpeed * 10f;
            Vector3 mousePos = Input.mousePosition;
            
            if (mousePos.x <= 10) cameraTarget.x -= edgeScrollSpeed * Time.deltaTime;
            if (mousePos.x >= Screen.width - 10) cameraTarget.x += edgeScrollSpeed * Time.deltaTime;
            if (mousePos.y <= 10) cameraTarget.z -= edgeScrollSpeed * Time.deltaTime;
            if (mousePos.y >= Screen.height - 10) cameraTarget.z += edgeScrollSpeed * Time.deltaTime;
            
            UpdateCameraPosition();
        }
        
        void UpdateCameraPosition()
        {
            if (guildCamera == null) return;
            
            // Calculate isometric camera position
            float angleRad = cameraAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(0, Mathf.Sin(angleRad), -Mathf.Cos(angleRad)) * currentZoom;
            
            guildCamera.transform.position = cameraTarget + offset;
            guildCamera.transform.LookAt(cameraTarget);
        }
        
        void HandleBuildingPlacement()
        {
            if (!isInBuildMode || previewBuilding == null) return;
            
            Ray ray = guildCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f, placementLayerMask))
            {
                Vector2Int gridPos = WorldToGridPosition(hit.point);
                
                if (IsValidGridPosition(gridPos))
                {
                    Vector3 snapPos = GridToWorldPosition(gridPos);
                    previewBuilding.transform.position = snapPos;
                    
                    Building buildingComponent = previewBuilding.GetComponent<Building>();
                    bool canPlace = CanPlaceBuilding(gridPos, buildingComponent);
                    
                    UpdatePreviewMaterial(canPlace);
                    
                    if (Input.GetMouseButtonDown(0) && canPlace)
                    {
                        PlaceBuilding(gridPos, buildingComponent);
                    }
                }
            }
            
            if (Input.GetMouseButtonDown(1)) // Right click to cancel
            {
                ExitBuildMode();
            }
        }
        
        void HandleBuildingSelection()
        {
            if (isInBuildMode) return;
            
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = guildCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit, 100f))
                {
                    Building building = hit.collider.GetComponentInParent<Building>();
                    
                    if (building != null)
                    {
                        SelectBuilding(building);
                    }
                    else
                    {
                        DeselectBuilding();
                    }
                }
            }
        }
        
        public void EnterBuildMode(Core.GuildManager.BuildingType buildingType)
        {
            if (isInBuildMode) ExitBuildMode();
            
            isInBuildMode = true;
            
            // Show grid
            if (buildModeGrid != null)
            {
                buildModeGrid.SetActive(true);
            }
            
            // Show grid cells
            foreach (var cell in gridCells)
            {
                if (cell.CellObject != null)
                {
                    var renderer = cell.CellObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                        renderer.material.color = cell.IsValidForPlacement && !cell.IsOccupied ? 
                            Color.green : Color.red;
                    }
                }
            }
            
            // Create preview building
            GameObject prefab = GetBuildingPrefab(buildingType);
            if (prefab != null)
            {
                previewBuilding = Instantiate(prefab);
                previewBuilding.name = "Preview_" + buildingType;
                
                // Disable colliders during preview
                foreach (var collider in previewBuilding.GetComponentsInChildren<Collider>())
                {
                    collider.enabled = false;
                }
            }
        }
        
        public void ExitBuildMode()
        {
            isInBuildMode = false;
            
            if (previewBuilding != null)
            {
                Destroy(previewBuilding);
                previewBuilding = null;
            }
            
            if (buildModeGrid != null)
            {
                buildModeGrid.SetActive(false);
            }
            
            // Hide grid cells
            foreach (var cell in gridCells)
            {
                if (cell.CellObject != null)
                {
                    var renderer = cell.CellObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                    }
                }
            }
        }
        
        bool CanPlaceBuilding(Vector2Int gridPos, Building building)
        {
            if (building == null) return false;
            
            Vector2Int size = building.GridSize;
            
            // Check all cells the building would occupy
            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    Vector2Int checkPos = gridPos + new Vector2Int(x, z);
                    
                    if (!IsValidGridPosition(checkPos)) return false;
                    if (gridCells[checkPos.x, checkPos.y].IsOccupied) return false;
                    if (!gridCells[checkPos.x, checkPos.y].IsValidForPlacement) return false;
                }
            }
            
            return true;
        }
        
        void PlaceBuilding(Vector2Int gridPos, Building building)
        {
            // Mark cells as occupied
            Vector2Int size = building.GridSize;
            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    Vector2Int occupyPos = gridPos + new Vector2Int(x, z);
                    gridCells[occupyPos.x, occupyPos.y].IsOccupied = true;
                    gridCells[occupyPos.x, occupyPos.y].OccupyingBuilding = building;
                }
            }
            
            // Set building data
            building.GridPosition = gridPos;
            building.IsPlaced = true;
            placedBuildings[gridPos] = building;
            
            // Enable colliders
            foreach (var collider in building.GetComponentsInChildren<Collider>())
            {
                collider.enabled = true;
            }
            
            // Play construction effect
            if (constructionEffect != null)
            {
                Instantiate(constructionEffect, building.transform.position, Quaternion.identity);
            }
            
            OnBuildingPlaced?.Invoke(building);
            
            // Clear preview
            previewBuilding = null;
            ExitBuildMode();
        }
        
        void SelectBuilding(Building building)
        {
            DeselectBuilding();
            
            selectedBuilding = building;
            
            if (selectionHighlight != null)
            {
                selectionHighlight.SetActive(true);
                selectionHighlight.transform.position = building.transform.position;
                selectionHighlight.transform.localScale = new Vector3(
                    building.GridSize.x * cellSize,
                    1f,
                    building.GridSize.y * cellSize
                );
            }
            
            OnBuildingSelected?.Invoke(building);
        }
        
        void DeselectBuilding()
        {
            selectedBuilding = null;
            
            if (selectionHighlight != null)
            {
                selectionHighlight.SetActive(false);
            }
        }
        
        void UpdatePreviewMaterial(bool canPlace)
        {
            if (previewBuilding == null) return;
            
            Material mat = canPlace ? validPlacementMaterial : invalidPlacementMaterial;
            
            foreach (var renderer in previewBuilding.GetComponentsInChildren<Renderer>())
            {
                renderer.material = mat;
            }
        }
        
        Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * cellSize, 0, gridPos.y * cellSize);
        }
        
        Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            int x = Mathf.RoundToInt(worldPos.x / cellSize);
            int z = Mathf.RoundToInt(worldPos.z / cellSize);
            return new Vector2Int(x, z);
        }
        
        bool IsValidGridPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
        }
        
        GameObject GetBuildingPrefab(Core.GuildManager.BuildingType type)
        {
            // Map building types to prefab indices
            int index = (int)type;
            if (index >= 0 && index < buildingPrefabs.Length)
            {
                return buildingPrefabs[index];
            }
            return null;
        }
        
        public void UpgradeBuilding(Building building)
        {
            if (building == null || !building.CanUpgrade()) return;
            
            // Play upgrade effect
            if (upgradeEffect != null)
            {
                Instantiate(upgradeEffect, building.transform.position, Quaternion.identity);
            }
            
            building.Upgrade();
        }
        
        public void RemoveBuilding(Building building)
        {
            if (building == null || !building.IsPlaced) return;
            
            // Free up grid cells
            Vector2Int size = building.GridSize;
            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    Vector2Int freePos = building.GridPosition + new Vector2Int(x, z);
                    if (IsValidGridPosition(freePos))
                    {
                        gridCells[freePos.x, freePos.y].IsOccupied = false;
                        gridCells[freePos.x, freePos.y].OccupyingBuilding = null;
                    }
                }
            }
            
            placedBuildings.Remove(building.GridPosition);
            OnBuildingRemoved?.Invoke(building);
            
            Destroy(building.gameObject);
        }
        
        public List<Building> GetAllBuildings()
        {
            return placedBuildings.Values.ToList();
        }
        
        public Building GetBuildingAt(Vector2Int gridPos)
        {
            if (IsValidGridPosition(gridPos))
            {
                return gridCells[gridPos.x, gridPos.y].OccupyingBuilding;
            }
            return null;
        }
        
        public bool IsCellOccupied(Vector2Int gridPos)
        {
            if (IsValidGridPosition(gridPos))
            {
                return gridCells[gridPos.x, gridPos.y].IsOccupied;
            }
            return true; // Out of bounds is considered occupied
        }
        
        public void SetCameraTarget(Vector3 position)
        {
            cameraTarget = position;
            UpdateCameraPosition();
        }
        
        public void FocusOnBuilding(Building building)
        {
            if (building != null)
            {
                SetCameraTarget(building.transform.position);
            }
        }
    }
}