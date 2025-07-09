using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 타일 클릭 및 마우스 입력을 처리하는 컴포넌트
    /// </summary>
    public class TileInputHandler : MonoBehaviour
    {
        [Header("입력 설정")]
        [SerializeField] private LayerMask tileLayerMask = -1;
        [SerializeField] private float raycastDistance = 100f;
        [SerializeField] private bool enableInput = true;
        
        [Header("하이라이트 설정")]
        [SerializeField] private bool showHoverHighlight = true;
        [SerializeField] private bool showValidPlacement = true;
        
        [Header("현재 상태")]
        private Tile hoveredTile;
        private Tile selectedTile;
        private CharacterUnit selectedCharacter;
        
        // 캐시
        private Camera mainCamera;
        private TurnBasedBattleManager battleManager;
        private TileGridManager tileGridManager;
        
        // 이벤트
        public System.Action<Tile> OnTileClicked;
        public System.Action<Tile> OnTileHovered;
        public System.Action<Tile> OnTileUnhovered;
        
        void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera를 찾을 수 없습니다!");
            }
            
            battleManager = TurnBasedBattleManager.Instance;
            tileGridManager = TileGridManager.Instance;
        }
        
        void Update()
        {
            if (!enableInput || !battleManager.IsPlayerTurn() || !battleManager.IsDeploymentPhase())
                return;
            
            HandleMouseInput();
        }
        
        /// <summary>
        /// 마우스 입력 처리
        /// </summary>
        void HandleMouseInput()
        {
            // UI 위에 마우스가 있으면 무시
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                ClearHoveredTile();
                return;
            }
            
            // 레이캐스트로 타일 감지
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, raycastDistance, tileLayerMask))
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                
                if (tile != null)
                {
                    // 호버 처리
                    if (tile != hoveredTile)
                    {
                        ClearHoveredTile();
                        SetHoveredTile(tile);
                    }
                    
                    // 클릭 처리
                    if (Input.GetMouseButtonDown(0))
                    {
                        HandleTileClick(tile);
                    }
                }
            }
            else
            {
                ClearHoveredTile();
            }
            
            // 우클릭으로 선택 취소
            if (Input.GetMouseButtonDown(1))
            {
                ClearSelection();
            }
        }
        
        /// <summary>
        /// 타일 호버 설정
        /// </summary>
        void SetHoveredTile(Tile tile)
        {
            hoveredTile = tile;
            
            if (showHoverHighlight)
            {
                tile.SetHighlight(true);
            }
            
            // 배치 가능 여부 표시
            if (showValidPlacement && battleManager.IsDeploymentPhase())
            {
                bool canPlace = tile.CanPlaceUnit();
                tile.ShowPlacementIndicator(true, canPlace);
            }
            
            OnTileHovered?.Invoke(tile);
        }
        
        /// <summary>
        /// 호버 타일 초기화
        /// </summary>
        void ClearHoveredTile()
        {
            if (hoveredTile != null)
            {
                hoveredTile.SetHighlight(false);
                hoveredTile.ShowPlacementIndicator(false);
                OnTileUnhovered?.Invoke(hoveredTile);
                hoveredTile = null;
            }
        }
        
        /// <summary>
        /// 타일 클릭 처리
        /// </summary>
        void HandleTileClick(Tile tile)
        {
            // 이전 선택 해제
            if (selectedTile != null && selectedTile != tile)
            {
                selectedTile.SetHighlight(false);
            }
            
            selectedTile = tile;
            OnTileClicked?.Invoke(tile);
            
            // 배치 모드일 때
            if (battleManager.IsDeploymentPhase() && battleManager.IsPlayerTurn())
            {
                // TurnBasedBattleManager에 타일 선택 알림
                battleManager.OnPlayerSelectTile(tile);
            }
        }
        
        /// <summary>
        /// 선택 초기화
        /// </summary>
        void ClearSelection()
        {
            if (selectedTile != null)
            {
                selectedTile.SetHighlight(false);
                selectedTile = null;
            }
            
            selectedCharacter = null;
        }
        
        /// <summary>
        /// 입력 활성화/비활성화
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            enableInput = enabled;
            
            if (!enabled)
            {
                ClearHoveredTile();
                ClearSelection();
            }
        }
        
        /// <summary>
        /// 캐릭터 선택 (추후 기능용)
        /// </summary>
        public void SelectCharacter(CharacterUnit character)
        {
            selectedCharacter = character;
            
            // 캐릭터의 공격 범위 표시
            if (character != null && tileGridManager != null)
            {
                tileGridManager.ShowAttackRange(character, true);
            }
        }
        
        /// <summary>
        /// 디버그 표시
        /// </summary>
        void OnGUI()
        {
            if (!Application.isEditor) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"입력 활성화: {enableInput}");
            GUILayout.Label($"플레이어 턴: {battleManager?.IsPlayerTurn() ?? false}");
            GUILayout.Label($"배치 페이즈: {battleManager?.IsDeploymentPhase() ?? false}");
            
            if (hoveredTile != null)
            {
                GUILayout.Label($"호버 타일: {hoveredTile.GetTileInfo()}");
            }
            
            if (selectedTile != null)
            {
                GUILayout.Label($"선택 타일: {selectedTile.GetTileInfo()}");
            }
            
            GUILayout.EndArea();
        }
    }
}