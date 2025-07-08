using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 타일 시스템 - 전투 그리드의 개별 타일을 관리
    /// A타일과 B타일로 구성되며, 각각 아군과 적군이 배치 가능
    /// </summary>
    public class Tile : MonoBehaviour
    {
        [Header("타일 정보")]
        public int x;                    // 타일의 X 좌표 (0~5)
        public int y;                    // 타일의 Y 좌표 (0~2)
        public TileType tileType;        // A 타일 또는 B 타일
        public bool isOccupied;          // 타일 점유 여부
        public GameObject occupiedUnit;  // 점유한 유닛
        public Team occupiedTeam;        // 점유한 팀 (아군/적군)
        
        [Header("비주얼")]
        public SpriteRenderer spriteRenderer;
        public Color normalColor = Color.white;
        public Color highlightColor = Color.yellow;
        public Color allyOccupiedColor = new Color(0.5f, 0.5f, 1f, 1f);    // 파란색
        public Color enemyOccupiedColor = new Color(1f, 0.5f, 0.5f, 1f);   // 빨간색
        public Color validPlacementColor = new Color(0.5f, 1f, 0.5f, 0.5f); // 연한 녹색
        public Color invalidPlacementColor = new Color(1f, 0.5f, 0.5f, 0.5f); // 연한 빨간색
        
        [Header("타일 효과")]
        public GameObject selectionEffect;
        public GameObject placementIndicator;
        
        // 타일 타입 (A 또는 B)
        public enum TileType
        {
            A,  // 위쪽 타일 그룹
            B   // 아래쪽 타일 그룹
        }
        
        // 팀 타입
        public enum Team
        {
            None,   // 비어있음
            Ally,   // 아군
            Enemy   // 적군
        }
        
        private bool isHighlighted = false;
        private bool isValidPlacement = false;
        
        private void Start()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            UpdateVisual();
        }
        
        /// <summary>
        /// 타일 초기화
        /// </summary>
        public void Initialize(int x, int y, TileType type)
        {
            this.x = x;
            this.y = y;
            this.tileType = type;
            gameObject.name = $"Tile_{type}_{x}_{y}";
            UpdateVisual();
        }
        
        /// <summary>
        /// 유닛 배치
        /// </summary>
        public void PlaceUnit(GameObject unit, Team team)
        {
            if (isOccupied)
            {
                Debug.LogWarning($"타일 [{x},{y}]는 이미 점유되어 있습니다!");
                return;
            }
            
            isOccupied = true;
            occupiedUnit = unit;
            occupiedTeam = team;
            
            // 유닛 위치 설정
            unit.transform.position = transform.position;
            unit.transform.parent = transform;
            
            UpdateVisual();
        }
        
        /// <summary>
        /// 유닛 제거
        /// </summary>
        public void RemoveUnit()
        {
            if (!isOccupied)
                return;
                
            isOccupied = false;
            occupiedUnit = null;
            occupiedTeam = Team.None;
            UpdateVisual();
        }
        
        /// <summary>
        /// 타일 하이라이트
        /// </summary>
        public void SetHighlight(bool highlight)
        {
            isHighlighted = highlight;
            
            if (selectionEffect != null)
                selectionEffect.SetActive(highlight);
                
            UpdateVisual();
        }
        
        /// <summary>
        /// 배치 가능 표시
        /// </summary>
        public void ShowPlacementIndicator(bool show, bool isValid = true)
        {
            isValidPlacement = isValid;
            
            if (placementIndicator != null)
            {
                placementIndicator.SetActive(show);
                
                // 유효성에 따라 색상 변경
                SpriteRenderer indicatorRenderer = placementIndicator.GetComponent<SpriteRenderer>();
                if (indicatorRenderer != null)
                {
                    indicatorRenderer.color = isValid ? validPlacementColor : invalidPlacementColor;
                }
            }
        }
        
        /// <summary>
        /// 타일 시각 업데이트
        /// </summary>
        private void UpdateVisual()
        {
            if (spriteRenderer == null)
                return;
                
            // 우선순위: 하이라이트 > 점유 > 일반
            if (isHighlighted)
            {
                spriteRenderer.color = highlightColor;
            }
            else if (isOccupied)
            {
                spriteRenderer.color = occupiedTeam == Team.Ally ? allyOccupiedColor : enemyOccupiedColor;
            }
            else
            {
                spriteRenderer.color = normalColor;
            }
        }
        
        /// <summary>
        /// 타일 위치 반환
        /// </summary>
        public Vector2Int GetPosition()
        {
            return new Vector2Int(x, y);
        }
        
        /// <summary>
        /// 월드 좌표 반환
        /// </summary>
        public Vector3 GetWorldPosition()
        {
            return transform.position;
        }
        
        /// <summary>
        /// 유닛 배치 가능 여부
        /// </summary>
        public bool CanPlaceUnit()
        {
            return !isOccupied;
        }
        
        /// <summary>
        /// 타일 정보 문자열
        /// </summary>
        public string GetTileInfo()
        {
            string occupiedInfo = isOccupied ? $"Occupied by {occupiedTeam}" : "Empty";
            return $"Tile {tileType} [{x},{y}] - {occupiedInfo}";
        }
        
        /// <summary>
        /// 타일 리셋
        /// </summary>
        public void ResetTile()
        {
            RemoveUnit();
            SetHighlight(false);
            ShowPlacementIndicator(false);
        }
        
        // 마우스 이벤트 처리
        private void OnMouseEnter()
        {
            if (Placement.Instance != null && Placement.Instance.IsPlacementMode())
            {
                SetHighlight(true);
                
                // 배치 가능 여부에 따라 다른 표시
                bool canPlace = CanPlaceUnit() && Placement.Instance.CanPlaceOnTile(this);
                ShowPlacementIndicator(true, canPlace);
            }
        }
        
        private void OnMouseExit()
        {
            SetHighlight(false);
            ShowPlacementIndicator(false);
        }
        
        private void OnMouseDown()
        {
            if (Placement.Instance != null && Placement.Instance.IsPlacementMode())
            {
                Placement.Instance.TryPlaceUnit(this);
            }
        }
    }
}
