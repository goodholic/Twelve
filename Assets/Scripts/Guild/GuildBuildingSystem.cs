using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GuildMaster.Core;
using GuildMaster.Data;


namespace GuildMaster.Guild
{
    /// <summary>
    /// 길드 건물 시스템 - 건물 배치, 건설, 업그레이드 관리
    /// </summary>
    public class GuildBuildingSystem : MonoBehaviour
    {
        private static GuildBuildingSystem _instance;
        public static GuildBuildingSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GuildBuildingSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GuildBuildingSystem");
                        _instance = go.AddComponent<GuildBuildingSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Building System Settings")]
        public LayerMask groundLayer = 1;
        public Material previewMaterial;
        public Material validPlacementMaterial;
        public Material invalidPlacementMaterial;

        // 배치된 건물들
        public List<PlacedBuilding> placedBuildings = new List<PlacedBuilding>();
        public Dictionary<int, PlacedBuilding> buildingDict = new Dictionary<int, PlacedBuilding>();

        // 건물 데이터
        public List<BuildingDataSO> availableBuildings = new List<BuildingDataSO>();

        // 이벤트
        public event Action<PlacedBuilding> OnBuildingCompleted;
        public event Action<PlacedBuilding> OnBuildingUpgraded;
        public event Action<PlacedBuilding> OnBuildingRemoved;
        public event Action<PlacedBuilding> OnBuildingStarted;

        // 건설 상태
        private bool isBuildingMode = false;
        private BuildingDataSO selectedBuildingData;
        private GameObject previewObject;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSystem();
        }

        void InitializeSystem()
        {
            // 기본 건물 데이터 로드
            LoadBuildingData();
        }

        void LoadBuildingData()
        {
            // Resources 폴더에서 BuildingDataSO 파일들을 로드
            var buildingData = Resources.LoadAll<BuildingDataSO>("Buildings");
            availableBuildings.AddRange(buildingData);
        }

        public void StartBuildingMode(BuildingDataSO buildingData)
        {
            selectedBuildingData = buildingData;
            isBuildingMode = true;
            CreatePreviewObject();
        }

        public void ExitBuildingMode()
        {
            isBuildingMode = false;
            selectedBuildingData = null;
            DestroyPreviewObject();
        }

        void CreatePreviewObject()
        {
            if (selectedBuildingData != null && selectedBuildingData.prefab != null)
            {
                previewObject = Instantiate(selectedBuildingData.prefab);
                previewObject.name = "Preview_" + selectedBuildingData.buildingName;
                
                // 미리보기 오브젝트 설정
                SetupPreviewObject(previewObject);
            }
        }

        void SetupPreviewObject(GameObject obj)
        {
            // 콜라이더 비활성화
            var colliders = obj.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            // 반투명 재질 적용
            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = previewMaterial;
                }
                renderer.materials = materials;
            }
        }

        void DestroyPreviewObject()
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }
        }

        void Update()
        {
            if (isBuildingMode && previewObject != null)
            {
                UpdatePreviewPosition();
                HandleBuildingInput();
            }
        }

        void UpdatePreviewPosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                Vector3 position = hit.point;
                position.y = 0; // 지면에 고정
                previewObject.transform.position = position;

                // 배치 가능 여부에 따라 색상 변경
                bool canPlace = CanPlaceBuilding(position, selectedBuildingData);
                UpdatePreviewMaterial(canPlace);
            }
        }

        void UpdatePreviewMaterial(bool canPlace)
        {
            Material targetMaterial = canPlace ? validPlacementMaterial : invalidPlacementMaterial;
            var renderers = previewObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = targetMaterial;
                }
                renderer.materials = materials;
            }
        }

        void HandleBuildingInput()
        {
            if (Input.GetMouseButtonDown(0)) // 좌클릭으로 건물 배치
            {
                TryPlaceBuilding();
            }
            else if (Input.GetKeyDown(KeyCode.Escape)) // ESC로 취소
            {
                ExitBuildingMode();
            }
        }

        void TryPlaceBuilding()
        {
            Vector3 position = previewObject.transform.position;
            if (CanPlaceBuilding(position, selectedBuildingData))
            {
                PlaceBuilding(selectedBuildingData, position);
                ExitBuildingMode();
            }
        }

        public bool CanPlaceBuilding(Vector3 position, BuildingDataSO buildingData)
        {
            // 다른 건물과 겹치는지 확인
            foreach (var building in placedBuildings)
            {
                float distance = Vector3.Distance(position, building.position);
                if (distance < 2f) // 최소 2유닛 거리 유지
                {
                    return false;
                }
            }

            // 자원 확인
            if (ResourceManager.Instance != null)
            {
                foreach (var cost in buildingData.constructionCost)
                {
                    if (ResourceManager.Instance.GetResource(cost.Key) < cost.Value)
                    {
                        return false;
                    }
                }
            }

            // 길드 레벨 및 전제 조건 확인
            if (GuildManager.Instance != null)
            {
                var existingBuildingTypes = placedBuildings.Select(b => b.buildingType).ToList();
                if (!buildingData.CanConstruct(GuildManager.Instance.guildLevel, existingBuildingTypes))
                {
                    return false;
                }
            }

            return true;
        }

        public PlacedBuilding PlaceBuilding(BuildingDataSO buildingData, Vector3 position)
        {
            // 자원 소모
            if (ResourceManager.Instance != null)
            {
                foreach (var cost in buildingData.constructionCost)
                {
                    ResourceManager.Instance.SpendResource(cost.Key, cost.Value);
                }
            }

            // 건물 생성
            PlacedBuilding newBuilding = new PlacedBuilding(buildingData, position);
            placedBuildings.Add(newBuilding);
            buildingDict[newBuilding.buildingId] = newBuilding;

            // 실제 게임 오브젝트 생성
            if (buildingData.prefab != null)
            {
                GameObject buildingObj = Instantiate(buildingData.prefab, position, Quaternion.identity);
                buildingObj.name = buildingData.buildingName + "_" + newBuilding.buildingId;
            }

            OnBuildingStarted?.Invoke(newBuilding);

            // 건설 시작
            StartCoroutine(ConstructBuildingCoroutine(newBuilding));

            return newBuilding;
        }

        System.Collections.IEnumerator ConstructBuildingCoroutine(PlacedBuilding building)
        {
            float constructionTime = building.buildingData.constructionTime;
            float elapsed = 0f;

            while (elapsed < constructionTime)
            {
                elapsed += Time.deltaTime;
                building.constructionProgress = elapsed / constructionTime;
                yield return null;
            }

            building.isConstructed = true;
            building.constructionProgress = 1f;
            OnBuildingCompleted?.Invoke(building);
        }

        public void UpgradeBuilding(int buildingId)
        {
            if (buildingDict.TryGetValue(buildingId, out PlacedBuilding building))
            {
                if (building.CanUpgrade())
                {
                    // 업그레이드 비용 확인 및 소모
                    var upgradeCost = building.buildingData.GetUpgradeCost(building.level);
                    bool canAfford = true;

                    if (ResourceManager.Instance != null)
                    {
                        foreach (var cost in upgradeCost)
                        {
                            if (ResourceManager.Instance.GetResource(cost.Key) < cost.Value)
                            {
                                canAfford = false;
                                break;
                            }
                        }

                        if (canAfford)
                        {
                            foreach (var cost in upgradeCost)
                            {
                                ResourceManager.Instance.SpendResource(cost.Key, cost.Value);
                            }

                            building.Upgrade();
                            OnBuildingUpgraded?.Invoke(building);
                        }
                    }
                }
            }
        }

        public void RemoveBuilding(int buildingId)
        {
            if (buildingDict.TryGetValue(buildingId, out PlacedBuilding building))
            {
                placedBuildings.Remove(building);
                buildingDict.Remove(buildingId);
                OnBuildingRemoved?.Invoke(building);
            }
        }

        public PlacedBuilding GetBuilding(int buildingId)
        {
            buildingDict.TryGetValue(buildingId, out PlacedBuilding building);
            return building;
        }

        public List<PlacedBuilding> GetBuildingsByType(BuildingType type)
        {
            return placedBuildings.Where(b => b.buildingType == type).ToList();
        }

        public List<BuildingDataSO> GetAvailableBuildings()
        {
            return availableBuildings.Where(b => b.CanConstruct(
                GuildManager.Instance?.guildLevel ?? 1,
                placedBuildings.Select(pb => pb.buildingType).ToList()
            )).ToList();
        }
    }
} 