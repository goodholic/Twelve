using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Reflection;

namespace Twelve.Editor
{
    /// <summary>
    /// Twelve 프로젝트 통합 도구 메뉴
    /// 모든 개발 도구들을 체계적으로 정리한 메인 허브
    /// </summary>
    public static class TwelveProjectTools
    {
        #region 메인 도구 허브
        
        /// <summary>
        /// Twelve 프로젝트 도구 허브 열기
        /// </summary>
        [MenuItem("Twelve/🎮 Twelve Project Hub", false, 0)]
        public static void OpenProjectHub()
        {
            TwelveProjectHubWindow.ShowWindow();
        }
        
        /// <summary>
        /// 프로젝트 정보 표시
        /// </summary>
        [MenuItem("Twelve/📖 About Twelve Project", false, 1)]
        public static void ShowAboutProject()
        {
            Debug.Log("=== 🎮 Twelve 프로젝트 정보 ===");
            Debug.Log("📅 버전: v1.0.2024");
            Debug.Log("🎯 타입: PNG 시퀀스 투명배경 애니메이션 RPG");
            Debug.Log("🖼️ 특징: PNG 시퀀스 투명배경 캐릭터 애니메이션");
            Debug.Log("📊 데이터: CSV 기반 캐릭터 시스템");
            Debug.Log("🛠️ 도구: 통합 개발 도구 모음");
            Debug.Log("=====================================");
        }
        
        #endregion
        
        #region 빠른 액세스 도구들
        
        // 중복 제거됨: Create Test Character는 Data Management/Import Character Data와 동일한 기능
        
        /// <summary>
        /// CharacterDatabaseSO 테스트 데이터 생성
        /// </summary>
        [MenuItem("Twelve/⚡ Quick Tools/🎮 배틀용 캐릭터 DB 생성", false, 102)]
        public static void CreateBattleCharacterDatabase()
        {
            // RuntimeCharacterGenerator를 통해 테스트 캐릭터 생성
            var generator = Object.FindObjectOfType<RuntimeCharacterGenerator>();
            if (generator == null)
            {
                // GameManager를 찾아서 RuntimeCharacterGenerator 추가
                var gameManager = Object.FindObjectOfType<TwelveGame.Battle.GameManager>();
                if (gameManager != null)
                {
                    generator = gameManager.gameObject.AddComponent<RuntimeCharacterGenerator>();
                }
                else
                {
                    Debug.LogError("❌ GameManager를 찾을 수 없습니다. BattleScene을 열어주세요.");
                    return;
                }
            }
            
            generator.GenerateTestCharacters();
            Debug.Log("✅ 배틀용 캐릭터 데이터베이스 생성 완료!");
        }

        /// <summary>
        /// CharacterDatabaseSO 상태 확인
        /// </summary>
        [MenuItem("Twelve/⚡ Quick Tools/📊 캐릭터 DB 상태 확인", false, 103)]
        public static void CheckCharacterDatabaseStatus()
        {
            var database = UnityEditor.AssetDatabase.LoadAssetAtPath<GuildMaster.Data.CharacterDatabaseSO>("Assets/Prefabs/Data/Characters/CharacterDatabaseSO.asset");
            
            if (database == null)
            {
                Debug.LogError("❌ CharacterDatabaseSO를 찾을 수 없습니다!");
                return;
            }
            
            database.Initialize();
            
            Debug.Log("=== 📊 CharacterDatabaseSO 상태 ===");
            Debug.Log($"📁 경로: Assets/Prefabs/Data/Characters/CharacterDatabaseSO.asset");
            Debug.Log($"🎭 CharacterDataSO 개수: {database.characters.Count}");
            Debug.Log($"⚔️ TacticalCharacter 개수: {database.tacticalCharacters.Count}");
            
            if (database.tacticalCharacters.Count > 0)
            {
                Debug.Log("✅ 배틀에서 사용 가능한 캐릭터가 있습니다!");
                foreach (var character in database.tacticalCharacters.Take(5))
                {
                    Debug.Log($"  - {character.characterName} (ID: {character.characterId})");
                }
                if (database.tacticalCharacters.Count > 5)
                {
                    Debug.Log($"  ... 외 {database.tacticalCharacters.Count - 5}개");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ 배틀용 캐릭터가 없습니다. '🎮 배틀용 캐릭터 DB 생성'을 실행하세요.");
            }
            
            Debug.Log("=====================================");
        }
        
        /// <summary>
        /// PNG 시퀀스 빠른 설정 가이드
        /// </summary>
        [MenuItem("Twelve/⚡ Quick Tools/🖼️ PNG 시퀀스 빠른 가이드", false, 104)]
        public static void ShowPNGSequenceQuickGuide()
        {
            Debug.Log("=== 🖼️ PNG 시퀀스 투명배경 빠른 가이드 ===");
            Debug.Log("📁 1. PNG 시퀀스 파일들을 Assets/Video/ 폴더에 배치");
            Debug.Log("📝 2. 파일명에 'idle' 또는 'attack' 키워드 포함");
            Debug.Log("🛠️ 3. Unity 메뉴 → Twelve → 🖼️ PNG 시퀀스 도구 → 📁 Video 폴더에서 빠른 설정");
            Debug.Log("🎬 4. 새 CharacterData 생성 또는 기존 CharacterData 선택");
            Debug.Log("✨ 5. 자동으로 감지하여 할당 완료!");
            Debug.Log("==========================================");
            Debug.Log("🚫 비디오 코덱 문제 완전 해결!");
            Debug.Log("🖼️ PNG 시퀀스는 완벽한 투명배경을 지원합니다!");
            Debug.Log("💡 팁: 영문 파일명 사용 (character_idle_01.png)");
            Debug.Log("🛠️ 자동 설정: Twelve → 🖼️ PNG 시퀀스 도구");
            Debug.Log("==========================================");
        }
        
        /// <summary>
        /// CharacterDatabaseSO를 Resources 폴더로 복사
        /// </summary>
        [MenuItem("Twelve/⚡ Quick Tools/📁 DB를 Resources로 복사", false, 105)]
        public static void CopyDatabaseToResources()
        {
            string sourcePath = "Assets/Prefabs/Data/Characters/CharacterDatabaseSO.asset";
            string targetPath = "Assets/Resources/Data/CharacterDatabase.asset";
            
            // Resources/Data 폴더 생성
            string resourcesDir = "Assets/Resources";
            string dataDir = "Assets/Resources/Data";
            
            if (!System.IO.Directory.Exists(resourcesDir))
                System.IO.Directory.CreateDirectory(resourcesDir);
                
            if (!System.IO.Directory.Exists(dataDir))
                System.IO.Directory.CreateDirectory(dataDir);
            
            // 파일 복사
            if (UnityEditor.AssetDatabase.CopyAsset(sourcePath, targetPath))
            {
                UnityEditor.AssetDatabase.Refresh();
                Debug.Log($"✅ CharacterDatabaseSO가 Resources 폴더로 복사됨: {targetPath}");
            }
            else
            {
                Debug.LogError($"❌ 복사 실패: {sourcePath} → {targetPath}");
            }
        }
        
        /// <summary>
        /// 프로젝트 정리 도구
        /// </summary>
        [MenuItem("Twelve/⚡ Quick Tools/Clean Project", false, 102)]
        public static void CleanProject()
        {
            Debug.Log("🧹 프로젝트 정리 시작...");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("✅ 프로젝트 정리 완료!");
        }
        
        #endregion
        
        #region 도움말 및 지원
        
        /// <summary>
        /// 메뉴 구조 도움말
        /// </summary>
        [MenuItem("Twelve/❓ Help/Menu Structure", false, 200)]
        public static void ShowMenuStructure()
        {
            Debug.Log("=== 📋 Twelve 프로젝트 메뉴 구조 ===");
            Debug.Log("");
            Debug.Log("🎮 Twelve/");
            Debug.Log("├── 📖 About Twelve Project");
            Debug.Log("├── 🎮 Twelve Project Hub");
            Debug.Log("├── ⚡ Quick Tools/");
            Debug.Log("│   ├── 🖼️ PNG 시퀀스 빠른 가이드");
            Debug.Log("│   └── Clean Project");
            Debug.Log("├── 📊 Data Management/");
            Debug.Log("│   ├── Import Character Data");
            Debug.Log("│   ├── Export Character Data");
            Debug.Log("│   ├── CSV Data Converter");
            Debug.Log("│   ├── CSV 임포터");
            Debug.Log("│   ├── 빠른 캐릭터 임포트");
            Debug.Log("│   ├── 빠른 대화 임포트");
            Debug.Log("│   ├── 빠른 건물 임포트");
            Debug.Log("│   ├── 빠른 스킬 임포트");
            Debug.Log("│   ├── CSV → SO 변환기");
            Debug.Log("│   ├── 데이터 내보내기 관리자");
            Debug.Log("│   ├── 빠른 데이터 내보내기");
            Debug.Log("│   ├── CSV 데이터 동기화 관리자");
            Debug.Log("│   ├── 빠른 CSV 동기화");
            Debug.Log("│   ├── CSV 데이터 생성");
            Debug.Log("│   ├── Full System Sync");
            Debug.Log("│   └── Validate System");
            // Animation Tools 메뉴는 PNG 시퀀스 도구로 교체됨
            Debug.Log("├── 🖼️ PNG 시퀀스 도구/");
            Debug.Log("│   ├── PNG 시퀀스 자동 설정");
            Debug.Log("│   ├── 📁 Video 폴더에서 빠른 설정");
            Debug.Log("│   ├── 📂 폴더 선택하여 빠른 설정");
            Debug.Log("│   └── 🔍 PNG 시퀀스 검증");
            Debug.Log("├── 🛠️ Development Tools/");
            Debug.Log("│   ├── Attack Pattern Visualizer");
            Debug.Log("│   ├── Create Default Characters");
            Debug.Log("│   ├── Assign Character Pools");
            Debug.Log("│   ├── Missing Prefab Fixer");
            Debug.Log("│   ├── Fix Serialization Issues");
            Debug.Log("│   ├── Force Refresh Scene");
            Debug.Log("│   ├── Clear Selection and Reload Inspectors");
            Debug.Log("│   ├── 인스펙터 정리 실행");
            Debug.Log("│   ├── 자동 인스펙터 정리 활성화");
            Debug.Log("│   └── 시작 시 자동 수정 수동 실행");
            Debug.Log("└── ❓ Help/");
            Debug.Log("    ├── Menu Structure");
            Debug.Log("    └── Troubleshooting");
            Debug.Log("=====================================");
        }
        
        /// <summary>
        /// 문제 해결 가이드
        /// </summary>
        [MenuItem("Twelve/❓ Help/Troubleshooting", false, 201)]
        public static void ShowTroubleshooting()
        {
            Debug.Log("=== 🔧 Twelve 프로젝트 문제 해결 ===");
            Debug.Log("");
            Debug.Log("🖼️ PNG 시퀀스 애니메이션 안됨:");
            Debug.Log("  → PNGSequenceController 컴포넌트 확인");
            Debug.Log("  → PNG 시퀀스 파일 할당 확인");
            Debug.Log("  → Animation Type: PNGSequence 설정 확인");
            Debug.Log("  → Frame Rate가 0보다 큰지 확인");
            Debug.Log("");
            Debug.Log("📊 CSV 임포트 실패:");
            Debug.Log("  → Assets/CSV/ 폴더에 파일 있는지 확인");
            Debug.Log("  → CSV 파일 형식 확인");
            Debug.Log("  → Twelve/CSV System/Validate System 실행");
            Debug.Log("");
            Debug.Log("🛠️ PNG 시퀀스 설정 문제:");
            Debug.Log("  → Unity 메뉴 → Twelve → 🖼️ PNG 시퀀스 도구 사용");
            Debug.Log("  → 📁 Video 폴더에서 빠른 설정 시도");
            Debug.Log("  → 파일명에 'idle' 또는 'attack' 키워드 포함 확인");
            Debug.Log("");
            Debug.Log("💡 더 많은 도움이 필요하면:");
            Debug.Log("  → Unity Console 에러 메시지 확인");
            Debug.Log("  → Twelve/❓ Help/ 메뉴 활용");
            Debug.Log("=====================================");
        }
        
        #endregion

        /// <summary>
        /// CharacterDatabaseSO 상세 분석
        /// </summary>
        [MenuItem("Twelve/⚡ Quick Tools/🔍 캐릭터 DB 상세 분석", false, 106)]
        public static void AnalyzeCharacterDatabaseDetailed()
        {
            var database = UnityEditor.AssetDatabase.LoadAssetAtPath<GuildMaster.Data.CharacterDatabaseSO>("Assets/Prefabs/Data/Characters/CharacterDatabaseSO.asset");
            
            if (database == null)
            {
                Debug.LogError("❌ CharacterDatabaseSO를 찾을 수 없습니다!");
                return;
            }
            
            database.Initialize();
            
            Debug.Log("=== 🔍 CharacterDatabaseSO 상세 분석 ===");
            Debug.Log($"📁 경로: Assets/Prefabs/Data/Characters/CharacterDatabaseSO.asset");
            Debug.Log("");
            
            // CharacterDataSO 분석
            Debug.Log($"🎨 CharacterDataSO (UI/에디터용): {database.characters.Count}개");
            if (database.characters.Count > 0)
            {
                Debug.Log("  📋 CharacterDataSO 리스트:");
                foreach (var character in database.characters.Take(5))
                {
                    Debug.Log($"    - {character.characterName} (ID: {character.id}, Job: {character.jobClass}, Rarity: {character.rarity})");
                }
                if (database.characters.Count > 5)
                {
                    Debug.Log($"    ... 외 {database.characters.Count - 5}개");
                }
            }
            else
            {
                Debug.LogWarning("  ⚠️ CharacterDataSO 리스트가 비어있습니다!");
                Debug.Log("  💡 이는 정상입니다. 현재 배틀에서는 TacticalCharacter만 사용합니다.");
            }
            
            Debug.Log("");
            
            // CharacterData (Tactical) 분석
            Debug.Log($"⚔️ CharacterData (실제 게임용): {database.tacticalCharacters.Count}개");
            if (database.tacticalCharacters.Count > 0)
            {
                Debug.Log("  📋 TacticalCharacter 리스트:");
                foreach (var character in database.tacticalCharacters.Take(5))
                {
                    Debug.Log($"    - {character.characterName} (ID: {character.characterId}, HP: {character.hp}, ATK: {character.attackPower})");
                }
                if (database.tacticalCharacters.Count > 5)
                {
                    Debug.Log($"    ... 외 {database.tacticalCharacters.Count - 5}개");
                }
            }
            else
            {
                Debug.LogError("  ❌ TacticalCharacter 리스트가 비어있습니다!");
                Debug.Log("  🔧 '🎮 배틀용 캐릭터 DB 생성'을 먼저 실행하세요.");
            }
            
            Debug.Log("");
            Debug.Log("=== 📚 데이터 타입 설명 ===");
            Debug.Log("🎨 CharacterDataSO: CSV 에디터, UI 시스템용 (단순한 구조)");
            Debug.Log("⚔️ CharacterData: 실제 배틀, 게임플레이용 (완전한 구조)");
            Debug.Log("💡 현재 배틀에서는 CharacterData(tactical)만 사용됩니다.");
            Debug.Log("=====================================");
        }

        /// <summary>
        /// 배틀씬 완전 설정 가이드
        /// </summary>
        [MenuItem("Twelve/⚡ Quick Tools/📋 배틀씬 완전 설정 가이드", false, 108)]
        public static void ShowBattleSceneSetupGuide()
        {
            Debug.Log("=== 📋 배틀씬 완전 설정 가이드 ===");
            Debug.Log("");
            
            // 1. 기본 설정 확인
            Debug.Log("🎯 1단계: 기본 설정");
            Debug.Log("   ✅ BattleScene.unity 열기");
            Debug.Log("   ✅ Unity 메뉴 → Twelve → ⚡ Quick Tools → 🚑 배틀 씬 에러 해결");
            Debug.Log("");
            
            // 2. GameManager 설정
            Debug.Log("🎮 2단계: GameManager 설정");
            Debug.Log("   ✅ GameManager 오브젝트 존재 확인");
            Debug.Log("   ✅ Character Database 필드 할당");
            Debug.Log("   ✅ Auto Start Game 체크");
            Debug.Log("   ✅ Load All Characters At Start 체크");
            Debug.Log("");
            
            // 3. UI 설정
            Debug.Log("🖥️ 3단계: UI 설정");
            Debug.Log("   ✅ BattleUIManager 오브젝트 존재");
            Debug.Log("   ✅ Canvas와 UI 요소들 생성");
            Debug.Log("   ✅ 캐릭터 버튼 4개 설정");
            Debug.Log("   ✅ 턴 표시 텍스트 설정");
            Debug.Log("   ✅ 점수 표시 텍스트 설정");
            Debug.Log("");
            
            // 4. 보드 설정
            Debug.Log("🏁 4단계: 보드 설정");
            Debug.Log("   ✅ BoardManager 오브젝트 존재");
            Debug.Log("   ✅ A타일 18개 (6x3) 생성 및 할당");
            Debug.Log("   ✅ B타일 18개 (6x3) 생성 및 할당");
            Debug.Log("   ✅ 타일 클릭 이벤트 설정");
            Debug.Log("");
            
            // 5. 기타 시스템
            Debug.Log("⚔️ 5단계: 기타 시스템");
            Debug.Log("   ✅ BattleSystem 오브젝트 존재");
            Debug.Log("   ✅ AIPlayer (선택사항)");
            Debug.Log("   ✅ 카메라 설정");
            Debug.Log("");
            
            // 6. 테스트
            Debug.Log("🧪 6단계: 테스트");
            Debug.Log("   ✅ Play 버튼 클릭");
            Debug.Log("   ✅ Console 에러 없음 확인");
            Debug.Log("   ✅ 캐릭터 버튼 4개 표시 확인");
            Debug.Log("   ✅ 타일 클릭 반응 확인");
            Debug.Log("");
            
            Debug.Log("💡 자동 설정: Unity 메뉴 → Twelve → ⚡ Quick Tools → 🔧 배틀씬 자동 설정");
            Debug.Log("=====================================");
        }

        /// <summary>
        /// 배틀씬 자동 설정 (가능한 모든 것 자동화)
        /// </summary>
        [MenuItem("Twelve/⚡ Quick Tools/🔧 배틀씬 자동 설정", false, 109)]
        public static void AutoSetupBattleScene()
        {
            Debug.Log("🔧 배틀씬 자동 설정 시작...");
            
            // 1. BattleScene 확인
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (!activeScene.name.Contains("Battle"))
            {
                Debug.LogWarning("⚠️ BattleScene이 아닌 것 같습니다. BattleScene.unity를 열어주세요.");
            }
            
            // 2. 캐릭터 데이터베이스 설정
            Debug.Log("1️⃣ 캐릭터 데이터베이스 설정...");
            FixBattleSceneErrors();
            
            // 3. GameManager 설정
            Debug.Log("2️⃣ GameManager 설정...");
            SetupGameManager();
            
            // 4. UI 설정
            Debug.Log("3️⃣ UI 설정...");
            SetupBattleUI();
            
            // 5. 보드 설정
            Debug.Log("4️⃣ 보드 설정...");
            SetupBoard();
            
            // 6. 기타 시스템 설정
            Debug.Log("5️⃣ 기타 시스템 설정...");
            SetupOtherSystems();
            
            Debug.Log("✅ 배틀씬 자동 설정 완료!");
            Debug.Log("🎮 이제 Play 버튼을 눌러서 테스트해보세요!");
        }

        private static void SetupGameManager()
        {
            var gameManager = Object.FindObjectOfType<TwelveGame.Battle.GameManager>();
            
            if (gameManager == null)
            {
                // GameManager 생성
                var gameManagerObj = new GameObject("GameManager");
                gameManager = gameManagerObj.AddComponent<TwelveGame.Battle.GameManager>();
                Debug.Log("✅ GameManager 생성됨");
            }
            
            // CharacterDatabase 할당
            var database = UnityEditor.AssetDatabase.LoadAssetAtPath<GuildMaster.Data.CharacterDatabaseSO>("Assets/Prefabs/Data/Characters/CharacterDatabaseSO.asset");
            if (database != null)
            {
                var field = typeof(TwelveGame.Battle.GameManager).GetField("characterDatabase", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(gameManager, database);
                    Debug.Log("✅ CharacterDatabase 할당됨");
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(gameManager);
        }

        private static void SetupBattleUI()
        {
            var uiManager = Object.FindObjectOfType<TwelveGame.Battle.BattleUIManager>();
            
            if (uiManager == null)
            {
                // Canvas 생성
                var canvasObj = new GameObject("Battle Canvas");
                var canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                
                // BattleUIManager 생성
                var uiManagerObj = new GameObject("BattleUIManager");
                uiManagerObj.transform.SetParent(canvasObj.transform);
                uiManager = uiManagerObj.AddComponent<TwelveGame.Battle.BattleUIManager>();
                
                Debug.Log("✅ BattleUIManager와 Canvas 생성됨");
            }
            
            // 기본 UI 요소들 생성 (간단한 텍스트들)
            CreateBasicUIElements(uiManager);
        }

        private static void CreateBasicUIElements(TwelveGame.Battle.BattleUIManager uiManager)
        {
            var canvas = uiManager.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            
            // 턴 표시 텍스트
            var turnTextObj = new GameObject("Turn Text");
            turnTextObj.transform.SetParent(canvas.transform);
            var turnText = turnTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            turnText.text = "현재 턴: X팀";
            turnText.fontSize = 24;
            
            var turnRect = turnTextObj.GetComponent<RectTransform>();
            turnRect.anchorMin = new Vector2(0, 1);
            turnRect.anchorMax = new Vector2(0, 1);
            turnRect.anchoredPosition = new Vector2(100, -50);
            
            // 점수 표시 텍스트
            var scoreTextObj = new GameObject("Score Text");
            scoreTextObj.transform.SetParent(canvas.transform);
            var scoreText = scoreTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            scoreText.text = "X: 0 - O: 0";
            scoreText.fontSize = 20;
            
            var scoreRect = scoreTextObj.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(1, 1);
            scoreRect.anchorMax = new Vector2(1, 1);
            scoreRect.anchoredPosition = new Vector2(-100, -50);
            
            // UI 필드 할당
            var currentTurnField = typeof(TwelveGame.Battle.BattleUIManager).GetField("currentTurnText");
            var scoreField = typeof(TwelveGame.Battle.BattleUIManager).GetField("scoreText");
            
            if (currentTurnField != null) currentTurnField.SetValue(uiManager, turnText);
            if (scoreField != null) scoreField.SetValue(uiManager, scoreText);
            
            UnityEditor.EditorUtility.SetDirty(uiManager);
            Debug.Log("✅ 기본 UI 요소들 생성됨");
        }

        private static void SetupBoard()
        {
            var boardManager = Object.FindObjectOfType<TwelveGame.Battle.BoardManager>();
            
            if (boardManager == null)
            {
                var boardManagerObj = new GameObject("BoardManager");
                boardManager = boardManagerObj.AddComponent<TwelveGame.Battle.BoardManager>();
                Debug.Log("✅ BoardManager 생성됨");
            }
            
            // 간단한 타일들 생성 (실제 프로젝트에서는 더 복잡할 수 있음)
            CreateSimpleTiles(boardManager);
        }

        private static void CreateSimpleTiles(TwelveGame.Battle.BoardManager boardManager)
        {
            // A보드와 B보드 생성
            var aBoardObj = new GameObject("A Board");
            var bBoardObj = new GameObject("B Board");
            
            aBoardObj.transform.position = new Vector3(-4, 0, 0);
            bBoardObj.transform.position = new Vector3(4, 0, 0);
            
            // 간단한 평면 타일들 생성 (6x3)
            CreateTileGrid(aBoardObj, "A_Tile", 6, 3);
            CreateTileGrid(bBoardObj, "B_Tile", 6, 3);
            
            Debug.Log("✅ 기본 타일 그리드 생성됨 (수동으로 BoardManager에 할당 필요)");
        }

        private static void CreateTileGrid(GameObject parent, string tilePrefix, int width, int height)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tileObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    tileObj.name = $"{tilePrefix}_{x}_{y}";
                    tileObj.transform.SetParent(parent.transform);
                    tileObj.transform.localPosition = new Vector3(x * 1.1f, 0, y * 1.1f);
                    tileObj.transform.localScale = Vector3.one * 0.1f;
                    
                    // 클릭 가능하도록 콜라이더 추가
                    if (tileObj.GetComponent<Collider>() == null)
                    {
                        tileObj.AddComponent<BoxCollider>();
                    }
                }
            }
        }

        private static void SetupOtherSystems()
        {
            // BattleSystem
            var battleSystem = Object.FindObjectOfType<TwelveGame.Battle.BattleSystem>();
            if (battleSystem == null)
            {
                var battleSystemObj = new GameObject("BattleSystem");
                battleSystem = battleSystemObj.AddComponent<TwelveGame.Battle.BattleSystem>();
                Debug.Log("✅ BattleSystem 생성됨");
            }
            
            // 카메라 설정
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0, 10, -8);
                mainCamera.transform.rotation = Quaternion.Euler(45, 0, 0);
                Debug.Log("✅ 카메라 위치 조정됨");
            }
        }

        /// <summary>
        /// 배틀 씬 에러 한번에 해결
        /// </summary>
        [MenuItem("Twelve/⚡ Quick Tools/🚑 배틀 씬 에러 해결", false, 107)]
        public static void FixBattleSceneErrors()
        {
            Debug.Log("🚑 배틀 씬 에러 해결 시작...");
            
            // 1. CharacterDatabaseSO 생성
            Debug.Log("1️⃣ CharacterDatabaseSO 생성 중...");
            CreateBattleCharacterDatabase();
            
            // 2. GameManager 설정 확인 및 자동 할당
            Debug.Log("2️⃣ GameManager 설정 확인 중...");
            var gameManager = Object.FindObjectOfType<TwelveGame.Battle.GameManager>();
            
            if (gameManager == null)
            {
                Debug.LogWarning("⚠️ GameManager를 찾을 수 없습니다. BattleScene을 열어주세요.");
            }
            else
            {
                // CharacterDatabase 자동 할당
                var database = UnityEditor.AssetDatabase.LoadAssetAtPath<GuildMaster.Data.CharacterDatabaseSO>("Assets/Prefabs/Data/Characters/CharacterDatabaseSO.asset");
                
                if (database != null)
                {
                    // Reflection을 사용해서 private 필드에 할당
                    var field = typeof(TwelveGame.Battle.GameManager).GetField("characterDatabase", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        
                    if (field != null)
                    {
                        field.SetValue(gameManager, database);
                        UnityEditor.EditorUtility.SetDirty(gameManager);
                        Debug.Log("✅ GameManager에 CharacterDatabase 자동 할당 완료");
                    }
                }
            }
            
            // 3. Resources 폴더로 복사
            Debug.Log("3️⃣ Resources 폴더로 복사 중...");
            CopyDatabaseToResources();
            
            // 4. 최종 확인
            Debug.Log("4️⃣ 최종 상태 확인 중...");
            CheckCharacterDatabaseStatus();
            
            Debug.Log("✅ 배틀 씬 에러 해결 완료!");
            Debug.Log("💡 이제 BattleScene에서 Play 버튼을 눌러보세요!");
        }

        /// <summary>
        /// Inspector 에러 진단 및 수정
        /// </summary>
        [MenuItem("Twelve/⚡ Quick Tools/🔍 Inspector 에러 진단", false, 110)]
        public static void DiagnoseInspectorErrors()
        {
            Debug.Log("🔍 Inspector 에러 진단 시작...");
            
            // 1. Missing Components 검사
            Debug.Log("1️⃣ Missing Components 검사 중...");
            FindAndReportMissingComponents();
            
            // 2. Null References 검사
            Debug.Log("2️⃣ Null References 검사 중...");
            CheckNullReferences();
            
            // 3. Inspector 선택 초기화
            Debug.Log("3️⃣ Inspector 선택 초기화...");
            Selection.activeGameObject = null;
            
            // 4. 에디터 새로고침
            Debug.Log("4️⃣ 에디터 새로고침...");
            UnityEditor.AssetDatabase.Refresh();
            
            Debug.Log("✅ Inspector 에러 진단 완료!");
        }

        /// <summary>
        /// 안전한 배틀씬 설정 (에러 방지)
        /// </summary>
        [MenuItem("Twelve/⚡ Quick Tools/🛡️ 안전한 배틀씬 설정", false, 111)]
        public static void SafeSetupBattleScene()
        {
            Debug.Log("🛡️ 안전한 배틀씬 설정 시작...");
            
            try
            {
                // 1. 기존 선택 해제
                Selection.activeGameObject = null;
                
                // 2. Missing Components 정리
                CleanupMissingComponents();
                
                // 3. 안전한 GameManager 설정
                SafeSetupGameManager();
                
                // 4. 최소한의 UI만 생성
                SafeSetupMinimalUI();
                
                // 5. 간단한 시스템들만 생성
                SafeSetupCoreSystems();
                
                Debug.Log("✅ 안전한 배틀씬 설정 완료!");
                Debug.Log("💡 이제 Play 버튼을 눌러보세요. Inspector 에러가 없어야 합니다.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 설정 중 에러 발생: {e.Message}");
                Debug.LogError("🔧 '🔍 Inspector 에러 진단'을 먼저 실행해보세요.");
            }
        }

        private static void FindAndReportMissingComponents()
        {
            var allGameObjects = Object.FindObjectsOfType<GameObject>();
            int missingCount = 0;
            
            foreach (var go in allGameObjects)
            {
                var components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        Debug.LogWarning($"⚠️ Missing Component 발견: {go.name} (인덱스 {i})");
                        missingCount++;
                    }
                }
            }
            
            if (missingCount == 0)
            {
                Debug.Log("✅ Missing Components 없음");
            }
            else
            {
                Debug.LogWarning($"⚠️ 총 {missingCount}개의 Missing Components 발견됨");
            }
        }

        private static void CheckNullReferences()
        {
            // GameManager 체크
            var gameManager = Object.FindObjectOfType<TwelveGame.Battle.GameManager>();
            if (gameManager != null)
            {
                Debug.Log("✅ GameManager 존재함");
                
                // CharacterDatabase 체크
                var dbField = typeof(TwelveGame.Battle.GameManager).GetField("characterDatabase", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dbField != null)
                {
                    var db = dbField.GetValue(gameManager);
                    if (db == null)
                    {
                        Debug.LogWarning("⚠️ GameManager.characterDatabase가 null입니다.");
                    }
                    else
                    {
                        Debug.Log("✅ CharacterDatabase 할당됨");
                    }
                }
            }
            else
            {
                Debug.LogWarning("⚠️ GameManager를 찾을 수 없습니다.");
            }
            
            // UI Manager 체크
            var uiManager = Object.FindObjectOfType<TwelveGame.Battle.BattleUIManager>();
            if (uiManager != null)
            {
                Debug.Log("✅ BattleUIManager 존재함");
            }
            else
            {
                Debug.LogWarning("⚠️ BattleUIManager를 찾을 수 없습니다.");
            }
        }

        private static void CleanupMissingComponents()
        {
            var allGameObjects = Object.FindObjectsOfType<GameObject>();
            int cleanedCount = 0;
            
            foreach (var go in allGameObjects)
            {
                // Missing Components 제거는 위험할 수 있으므로 로그만 남김
                var components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        Debug.LogWarning($"🧹 Missing Component 감지됨: {go.name}");
                        cleanedCount++;
                    }
                }
            }
            
            if (cleanedCount > 0)
            {
                Debug.LogWarning($"⚠️ {cleanedCount}개의 Missing Components가 감지되었습니다.");
                Debug.LogWarning("💡 수동으로 해당 GameObject들을 확인하고 Missing Components를 제거해주세요.");
            }
        }

        private static void SafeSetupGameManager()
        {
            var gameManager = Object.FindObjectOfType<TwelveGame.Battle.GameManager>();
            
            if (gameManager == null)
            {
                Debug.Log("📝 GameManager 생성 중...");
                var gameManagerObj = new GameObject("GameManager");
                
                // 안전하게 컴포넌트 추가
                try
                {
                    gameManager = gameManagerObj.AddComponent<TwelveGame.Battle.GameManager>();
                    Debug.Log("✅ GameManager 생성됨");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ GameManager 생성 실패: {e.Message}");
                    if (gameManagerObj != null)
                        Object.DestroyImmediate(gameManagerObj);
                    return;
                }
            }
            
            // CharacterDatabase 안전하게 할당
            try
            {
                var database = UnityEditor.AssetDatabase.LoadAssetAtPath<GuildMaster.Data.CharacterDatabaseSO>("Assets/Prefabs/Data/Characters/CharacterDatabaseSO.asset");
                if (database != null)
                {
                    var field = typeof(TwelveGame.Battle.GameManager).GetField("characterDatabase", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(gameManager, database);
                        UnityEditor.EditorUtility.SetDirty(gameManager);
                        Debug.Log("✅ CharacterDatabase 안전하게 할당됨");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ CharacterDatabase 할당 실패: {e.Message}");
            }
        }

        private static void SafeSetupMinimalUI()
        {
            var uiManager = Object.FindObjectOfType<TwelveGame.Battle.BattleUIManager>();
            
            if (uiManager == null)
            {
                Debug.Log("📝 최소한의 UI 생성 중...");
                
                try
                {
                    // EventSystem 먼저 확인/생성
                    if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                    {
                        var eventSystemObj = new GameObject("EventSystem");
                        eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                        eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                        Debug.Log("✅ EventSystem 생성됨");
                    }
                    
                    // Canvas 생성
                    var canvasObj = new GameObject("Battle Canvas");
                    var canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                    canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                    
                    // BattleUIManager 생성 (Canvas의 자식으로)
                    var uiManagerObj = new GameObject("BattleUIManager");
                    uiManagerObj.transform.SetParent(canvasObj.transform);
                    uiManager = uiManagerObj.AddComponent<TwelveGame.Battle.BattleUIManager>();
                    
                    Debug.Log("✅ 최소한의 UI 생성됨");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ UI 생성 실패: {e.Message}");
                }
            }
        }

        private static void SafeSetupCoreSystems()
        {
            // BattleSystem 안전하게 생성
            var battleSystem = Object.FindObjectOfType<TwelveGame.Battle.BattleSystem>();
            if (battleSystem == null)
            {
                try
                {
                    var battleSystemObj = new GameObject("BattleSystem");
                    battleSystem = battleSystemObj.AddComponent<TwelveGame.Battle.BattleSystem>();
                    Debug.Log("✅ BattleSystem 생성됨");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ BattleSystem 생성 실패: {e.Message}");
                }
            }
            
            // 카메라 안전하게 설정
            try
            {
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    mainCamera.transform.position = new Vector3(0, 10, -8);
                    mainCamera.transform.rotation = Quaternion.Euler(45, 0, 0);
                    Debug.Log("✅ 카메라 위치 조정됨");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ 카메라 설정 실패: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Twelve 프로젝트 허브 창
    /// </summary>
    public class TwelveProjectHubWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<TwelveProjectHubWindow>("Twelve Project Hub");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("🎮 Twelve Project Hub", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("프로젝트 개요:", EditorStyles.boldLabel);
            GUILayout.Label("• 🖼️ PNG 시퀀스 투명배경 캐릭터 애니메이션 시스템");
            GUILayout.Label("• 📊 CSV 기반 캐릭터 데이터 관리");
            GUILayout.Label("• 🛠️ 통합 개발 도구 모음");
            
            GUILayout.Space(20);
            
            GUILayout.Label("빠른 도구:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("📊 CSV 데이터 임포트"))
            {
                GuildMaster.CSV.CSVDataSystemManager.ImportCharacterData();
            }
            
            if (GUILayout.Button("🖼️ PNG 시퀀스 빠른 가이드"))
            {
                TwelveProjectTools.ShowPNGSequenceQuickGuide();
            }
            
            if (GUILayout.Button("🧹 프로젝트 정리"))
            {
                TwelveProjectTools.CleanProject();
            }
            
            GUILayout.Space(20);
            
            GUILayout.Label("문서 및 도움말:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("📋 메뉴 구조 보기"))
            {
                TwelveProjectTools.ShowMenuStructure();
            }
            
            if (GUILayout.Button("🔧 문제 해결 가이드"))
            {
                TwelveProjectTools.ShowTroubleshooting();
            }
        }
    }
} 