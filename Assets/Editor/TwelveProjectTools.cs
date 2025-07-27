using UnityEngine;
using UnityEditor;

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
        /// PNG 시퀀스 빠른 설정 가이드
        /// </summary>
        [MenuItem("Twelve/⚡ Quick Tools/🖼️ PNG 시퀀스 빠른 가이드", false, 101)]
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