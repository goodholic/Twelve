using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 타일 전투 게임의 메인 매니저
    /// </summary>
    public class TileBattleGameManager : MonoBehaviour
    {
        [Header("Game Systems")]
        [SerializeField] private TileBoardSystem tileBoardSystem;
        [SerializeField] private TurnBasedBattleSystem battleSystem;
        [SerializeField] private CharacterSelectionUI characterSelectionUI;
        [SerializeField] private BattleUIManager battleUIManager;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private GameObject unitPrefab;
        [SerializeField] private GameObject characterCardPrefab;
        [SerializeField] private GameObject damageTextPrefab;
        
        private static TileBattleGameManager instance;
        public static TileBattleGameManager Instance => instance;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            
            InitializeSystems();
        }
        
        private void InitializeSystems()
        {
            // 시스템 컴포넌트 찾기 또는 생성
            if (tileBoardSystem == null)
            {
                GameObject boardObj = new GameObject("TileBoardSystem");
                tileBoardSystem = boardObj.AddComponent<TileBoardSystem>();
            }
            
            if (battleSystem == null)
            {
                GameObject battleObj = new GameObject("TurnBasedBattleSystem");
                battleSystem = battleObj.AddComponent<TurnBasedBattleSystem>();
            }
            
            if (characterSelectionUI == null)
            {
                GameObject uiObj = new GameObject("CharacterSelectionUI");
                characterSelectionUI = uiObj.AddComponent<CharacterSelectionUI>();
            }
            
            if (battleUIManager == null)
            {
                GameObject battleUIObj = new GameObject("BattleUIManager");
                battleUIManager = battleUIObj.AddComponent<BattleUIManager>();
            }
            
            // 프리팹 설정
            SetupPrefabs();
        }
        
        private void SetupPrefabs()
        {
            // 타일 프리팹이 없으면 기본 생성
            if (tilePrefab == null)
            {
                tilePrefab = CreateDefaultTilePrefab();
            }
            
            // 유닛 프리팹이 없으면 기본 생성
            if (unitPrefab == null)
            {
                unitPrefab = CreateDefaultUnitPrefab();
            }
            
            // 캐릭터 카드 프리팹이 없으면 기본 생성
            if (characterCardPrefab == null)
            {
                characterCardPrefab = CreateDefaultCharacterCardPrefab();
            }
            
            // 데미지 텍스트 프리팹이 없으면 기본 생성
            if (damageTextPrefab == null)
            {
                damageTextPrefab = CreateDefaultDamageTextPrefab();
            }
        }
        
        private GameObject CreateDefaultTilePrefab()
        {
            GameObject tile = new GameObject("TilePrefab");
            
            // 스프라이트 렌더러 추가
            SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = new Color(0.8f, 0.8f, 0.8f);
            
            // 콜라이더 추가
            BoxCollider2D collider = tile.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            
            return tile;
        }
        
        private GameObject CreateDefaultUnitPrefab()
        {
            GameObject unit = new GameObject("UnitPrefab");
            
            // 스프라이트 렌더러
            SpriteRenderer sr = unit.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.sortingOrder = 10;
            
            // Unit 컴포넌트
            unit.AddComponent<Unit>();
            
            return unit;
        }
        
        private GameObject CreateDefaultCharacterCardPrefab()
        {
            GameObject card = new GameObject("CharacterCardPrefab");
            
            // RectTransform 설정
            RectTransform rt = card.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 250);
            
            // 배경 이미지
            Image bg = card.AddComponent<Image>();
            bg.color = Color.white;
            
            // 버튼
            Button button = card.AddComponent<Button>();
            
            // CharacterCard 컴포넌트
            card.AddComponent<CharacterCard>();
            
            return card;
        }
        
        private GameObject CreateDefaultDamageTextPrefab()
        {
            GameObject damageText = new GameObject("DamageTextPrefab");
            
            // TextMeshPro 컴포넌트
            TextMeshProUGUI tmp = damageText.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 24;
            tmp.color = Color.red;
            tmp.alignment = TextAlignmentOptions.Center;
            
            return damageText;
        }
        
        private Sprite CreateSquareSprite()
        {
            // 1x1 정사각형 스프라이트 생성
            Texture2D texture = new Texture2D(100, 100);
            Color[] pixels = new Color[100 * 100];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 100, 100), new Vector2(0.5f, 0.5f), 100);
        }
        
        private Sprite CreateCircleSprite()
        {
            // 원형 스프라이트 생성
            Texture2D texture = new Texture2D(100, 100);
            Color[] pixels = new Color[100 * 100];
            
            Vector2 center = new Vector2(50, 50);
            float radius = 45;
            
            for (int y = 0; y < 100; y++)
            {
                for (int x = 0; x < 100; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * 100 + x] = distance <= radius ? Color.white : Color.clear;
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 100, 100), new Vector2(0.5f, 0.5f), 100);
        }
        
        public void StartNewBattle()
        {
            // 새 전투 시작
            if (battleSystem != null)
            {
                battleSystem.enabled = true;
            }
        }
        
        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
        
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}