using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using GuildMaster.Systems;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 타일 시스템 - 전투 그리드의 개별 타일을 관리
    /// A타일과 B타일로 구성되며, 각각 아군과 적군 영역을 가짐
    /// </summary>
    public class Tile : MonoBehaviour
    {
        [Header("타일 정보")]
        public int x;                    // 타일의 X 좌표
        public int y;                    // 타일의 Y 좌표
        public TileType tileType;        // A 타일 또는 B 타일
        public SideType sideType;        // 아군 또는 적군 영역
        public bool isOccupied;          // 타일 점유 여부
        public GameObject occupiedBy;     // 점유한 유닛
        public bool isWalkable = true;   // 이동 가능 여부
        
        [Header("비주얼")]
        public SpriteRenderer spriteRenderer;
        public Color normalColor = Color.white;
        public Color highlightColor = Color.yellow;
        public Color occupiedColor = Color.red;
        public Color playerSideColor = new Color(0.5f, 0.5f, 1f, 0.5f);  // 파란색 계열
        public Color enemySideColor = new Color(1f, 0.5f, 0.5f, 0.5f);   // 빨간색 계열
        
        [Header("타일 효과")]
        public GameObject selectionEffect;
        public GameObject placementIndicator;
        
        // 타일 타입 (A 또는 B)
        public enum TileType
        {
            A,  // A 타일
            B   // B 타일
        }
        
        // 진영 타입
        public enum SideType
        {
            Player,  // 아군 영역 (왼쪽)
            Enemy    // 적군 영역 (오른쪽)
        }
        
        private void Start()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            UpdateVisual();
            SetSideColor();
        }
        
        /// <summary>
        /// 타일 위치 설정
        /// </summary>
        public void SetPosition(int x, int y)
        {
            this.x = x;
            this.y = y;
            gameObject.name = $"Tile_{tileType}_{sideType}_{x}_{y}";
        }
        
        /// <summary>
        /// 타일 초기화
        /// </summary>
        public void Initialize(int x, int y, TileType type, SideType side)
        {
            SetPosition(x, y);
            this.tileType = type;
            this.sideType = side;
            UpdateVisual();
            SetSideColor();
        }
        
        /// <summary>
        /// 유닛 배치
        /// </summary>
        public void SetOccupied(GameObject unit)
        {
            isOccupied = true;
            occupiedBy = unit;
            UpdateVisual();
        }
        
        /// <summary>
        /// 유닛 제거
        /// </summary>
        public void ClearOccupied()
        {
            isOccupied = false;
            occupiedBy = null;
            UpdateVisual();
        }
        
        /// <summary>
        /// 타일 하이라이트
        /// </summary>
        public void SetHighlight(bool highlight)
        {
            if (spriteRenderer != null)
            {
                if (highlight)
                {
                    spriteRenderer.color = highlightColor;
                    if (selectionEffect != null)
                        selectionEffect.SetActive(true);
                }
                else
                {
                    UpdateVisual();
                    if (selectionEffect != null)
                        selectionEffect.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// 배치 가능 표시
        /// </summary>
        public void ShowPlacementIndicator(bool show)
        {
            if (placementIndicator != null)
            {
                placementIndicator.SetActive(show && CanPlaceUnit());
            }
        }
        
        /// <summary>
        /// 타일 시각 업데이트
        /// </summary>
        private void UpdateVisual()
        {
            if (spriteRenderer != null)
            {
                if (isOccupied)
                {
                    spriteRenderer.color = occupiedColor;
                }
                else
                {
                    SetSideColor();
                }
            }
        }
        
        /// <summary>
        /// 진영별 색상 설정
        /// </summary>
        private void SetSideColor()
        {
            if (spriteRenderer != null)
            {
                if (sideType == SideType.Player)
                {
                    spriteRenderer.color = playerSideColor;
                }
                else
                {
                    spriteRenderer.color = enemySideColor;
                }
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
            return isWalkable && !isOccupied;
        }
        
        /// <summary>
        /// 타일 정보 문자열
        /// </summary>
        public string GetTileInfo()
        {
            return $"Tile {tileType}-{sideType} [{x},{y}] - {(isOccupied ? "Occupied" : "Empty")}";
        }
        
        /// <summary>
        /// 타일 리셋
        /// </summary>
        public void ResetTile()
        {
            ClearOccupied();
            SetHighlight(false);
            ShowPlacementIndicator(false);
        }
        
        // 마우스 이벤트 처리
        private void OnMouseEnter()
        {
            if (PlacementManager.Instance != null && PlacementManager.Instance.IsInPlacementMode())
            {
                SetHighlight(true);
                ShowPlacementIndicator(true);
            }
        }
        
        private void OnMouseExit()
        {
            SetHighlight(false);
            ShowPlacementIndicator(false);
        }
        
        private void OnMouseDown()
        {
            if (PlacementManager.Instance != null && PlacementManager.Instance.IsInPlacementMode())
            {
                PlacementManager.Instance.TryPlaceUnit(this);
            }
        }
    }
}
