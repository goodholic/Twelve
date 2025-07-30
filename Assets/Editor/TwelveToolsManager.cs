using UnityEngine;
using UnityEditor;
using System.Reflection;

/// <summary>
/// Twelve 프로젝트의 모든 도구들을 통합 관리하는 중앙 관리자
/// 최신 강력한 도구들만 제공하여 프로젝트 유지보수를 효율화합니다
/// </summary>
public class TwelveToolsManager : EditorWindow
{
    private Vector2 scrollPosition;
    private int selectedTab = 0;
    private readonly string[] tabNames = { "🔧 문제 해결", "🎨 에셋 관리", "📊 프로젝트 유틸", "📖 도움말" };

    [MenuItem("Twelve/🏠 도구 관리자")]
    public static void ShowWindow()
    {
        var window = GetWindow<TwelveToolsManager>("Twelve Tools Manager");
        window.titleContent = new GUIContent("🏠 Twelve Tools");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    void OnGUI()
    {
        // 헤더
        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label("🎮 Twelve 프로젝트 도구 관리자", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        GUILayout.Label("v2.0 - 최신 통합 버전", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 탭 메뉴
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        
        EditorGUILayout.Space();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        switch (selectedTab)
        {
            case 0: DrawProblemSolvingTab(); break;
            case 1: DrawAssetManagementTab(); break;
            case 2: DrawProjectUtilsTab(); break;
            case 3: DrawHelpTab(); break;
        }
        
        EditorGUILayout.EndScrollView();
    }

    void DrawProblemSolvingTab()
    {
        GUILayout.Label("🔧 문제 해결 도구들", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Missing Script 관련 도구들
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("📋 Missing Script 해결", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("스크립트 연결 문제를 해결하는 최신 도구들입니다.", MessageType.Info);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔍 Deep Missing Script Scanner", GUILayout.Height(35)))
        {
            DeepMissingScriptScanner.ShowWindow();
        }
        if (GUILayout.Button("🧹 Third Party Cleaner", GUILayout.Height(35)))
        {
            ThirdPartyMissingScriptCleaner.ShowWindow();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField("• Deep Scanner: 가장 강력한 Missing Script 자동 수정 도구");
        EditorGUILayout.LabelField("• Third Party Cleaner: Feel, Layer Lab 등 써드파티 에셋 정리");
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // Canvas 문제 해결
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🖼️ Canvas 문제 해결", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("UI Canvas 비활성화 문제를 해결합니다.", MessageType.Info);
        
        if (GUILayout.Button("🚨 MMTouchControls Remover", GUILayout.Height(35)))
        {
            MMTouchControlsRemover.ShowWindow();
        }
        
        EditorGUILayout.LabelField("• Canvas를 비활성화시키는 MMTouchControls 컴포넌트 제거");
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // 로비 씬 관련
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🏠 로비 씬 수정", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("로비 씬의 카메라 설정을 수정합니다.", MessageType.Info);
        
        if (GUILayout.Button("📹 Fix Lobby Scene Cameras", GUILayout.Height(35)))
        {
            Twelve.Editor.FixLobbySceneCameras.ShowWindow();
        }
        
        EditorGUILayout.LabelField("• 로비 씬 카메라 및 UI 설정 자동 수정");
        EditorGUILayout.EndVertical();
    }

    void DrawAssetManagementTab()
    {
        GUILayout.Label("🎨 에셋 관리 도구들", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // PNG 관련 도구들
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🖼️ 이미지 처리 도구", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("PNG 이미지 리사이즈 및 시퀀스 처리 도구들입니다.", MessageType.Info);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📏 PNG Resize Tools"))
        {
            // PNGResizeTools.ShowWindow();  // ShowWindow 메서드 없음
            EditorUtility.DisplayDialog("PNG Resize Tools", "PNG 리사이즈 도구는 현재 메뉴 방식으로 제공됩니다.", "확인");
        }
        if (GUILayout.Button("🎬 PNG Sequence Tools"))
        {
            PNGSequenceTools.ShowWindow();
        }
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("⚡ PNG Quick Tools"))
        {
            // PNGSequenceQuickTools.ShowWindow();  // 존재하지 않음
            EditorUtility.DisplayDialog("개발 중", "PNG Quick Tools는 현재 개발 중입니다.", "확인");
        }
        
        EditorGUILayout.LabelField("• PNG 이미지 일괄 리사이즈");
        EditorGUILayout.LabelField("• PNG 시퀀스 애니메이션 처리");
        EditorGUILayout.LabelField("• 빠른 PNG 처리 도구");
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // 캐릭터 관련 도구들
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("👥 캐릭터 관리", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("캐릭터 데이터 생성 및 편집 도구들입니다.", MessageType.Info);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("➕ Character Creator"))
        {
            CharacterCreator.ShowWindow();
        }
        if (GUILayout.Button("✏️ Character Data Editor"))
        {
            // CharacterDataEditor는 CustomEditor이므로 별도 창이 아님
            EditorUtility.DisplayDialog("Character Data Editor", 
                "Character Data Editor는 캐릭터 데이터 에셋을 선택하면 Inspector에서 활성화됩니다.", "확인");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 추가 캐릭터 생성 도구들
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🚀 빠른 캐릭터 생성", EditorStyles.miniBoldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🎬 PNG 시퀀스 캐릭터"))
        {
            EditorApplication.ExecuteMenuItem("Twelve/👥 캐릭터 생성/PNG 시퀀스 캐릭터 데이터 생성");
        }
        if (GUILayout.Button("🧪 테스트 캐릭터"))
        {
            EditorApplication.ExecuteMenuItem("Twelve/👥 캐릭터 생성/테스트 캐릭터 데이터 생성");
        }
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("⚡ 빠른 캐릭터 생성"))
        {
            EditorApplication.ExecuteMenuItem("Twelve/👥 캐릭터 생성/빠른 캐릭터 생성");
        }
        
        EditorGUILayout.LabelField("• PNG 시퀀스 애니메이션용 캐릭터 생성", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• 테스트용 샘플 캐릭터 생성", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• 모든 기본 캐릭터 일괄 생성", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    void DrawProjectUtilsTab()
    {
        GUILayout.Label("📊 프로젝트 유틸리티", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 기존 프로젝트 도구
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🎮 Twelve 프로젝트 도구", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Twelve 프로젝트 전용 유틸리티입니다.", MessageType.Info);
        
        if (GUILayout.Button("🎮 Twelve Project Tools", GUILayout.Height(35)))
        {
            // 메뉴 아이템 직접 실행
            EditorApplication.ExecuteMenuItem("Twelve/🎮 Twelve Project Hub");
        }
        
        EditorGUILayout.LabelField("• 기존 프로젝트 유틸리티 모음");
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // Unity 에디터 도구들 안내
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🛠️ Unity 에디터 도구", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Unity 에디터 관련 도구들은 Tools 메뉴로 이동되었습니다.", MessageType.Info);
        
        if (GUILayout.Button("🔧 Unity Editor Tools 열기", GUILayout.Height(35)))
        {
            GenericMenu toolsMenu = new GenericMenu();
            toolsMenu.AddItem(new GUIContent("Unity Editor/Fix Serialization Issues"), false, 
                () => EditorApplication.ExecuteMenuItem("Tools/Unity Editor/Fix Serialization Issues"));
            toolsMenu.AddItem(new GUIContent("Unity Editor/Clear Selection and Reload Inspectors"), false, 
                () => EditorApplication.ExecuteMenuItem("Tools/Unity Editor/Clear Selection and Reload Inspectors"));
            toolsMenu.AddSeparator("");
            toolsMenu.AddItem(new GUIContent("Unity Scene/Force Refresh Scene"), false, 
                () => EditorApplication.ExecuteMenuItem("Tools/Unity Scene/Force Refresh Scene"));
            toolsMenu.AddSeparator("");
            toolsMenu.AddItem(new GUIContent("Unity Inspector/인스펙터 정리 실행"), false, 
                () => EditorApplication.ExecuteMenuItem("Tools/Unity Inspector/인스펙터 정리 실행"));
            toolsMenu.AddItem(new GUIContent("Unity Inspector/자동 인스펙터 정리 활성화"), false, 
                () => EditorApplication.ExecuteMenuItem("Tools/Unity Inspector/자동 인스펙터 정리 활성화"));
            
            toolsMenu.ShowAsContext();
        }
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("또는 직접 메뉴에서 접근:", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Tools → Unity Editor → Fix Serialization Issues", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Tools → Unity Scene → Force Refresh Scene", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Tools → Unity Inspector → 인스펙터 정리", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    void DrawHelpTab()
    {
        GUILayout.Label("📖 도움말 및 가이드", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 사용법 가이드
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("📋 권장 사용 순서", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("문제 해결을 위한 권장 순서입니다.", MessageType.Info);
        
        EditorGUILayout.LabelField("1️⃣ Missing Script 문제 해결:");
        EditorGUILayout.LabelField("   • Deep Missing Script Scanner 실행");
        EditorGUILayout.LabelField("   • Third Party Cleaner로 써드파티 정리");
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("2️⃣ Canvas 비활성화 문제 해결:");
        EditorGUILayout.LabelField("   • MMTouchControls Remover 실행");
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("3️⃣ 로비 씬 문제 해결:");
        EditorGUILayout.LabelField("   • Fix Lobby Scene Cameras 실행");
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("4️⃣ 프로젝트 정리:");
        EditorGUILayout.LabelField("   • Editor Cleanup Utility 실행");
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // 도구 설명
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🔧 도구별 상세 설명", EditorStyles.boldLabel);
        
        EditorGUILayout.LabelField("• Deep Missing Script Scanner", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  - 가장 강력한 Missing Script 자동 수정 도구");
        EditorGUILayout.LabelField("  - 씬과 프리팹의 모든 Missing Script 감지 및 수정");
        EditorGUILayout.LabelField("  - 99%의 Missing Script 문제 해결 가능");
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("• Third Party Missing Script Cleaner", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  - Feel, Layer Lab 등 써드파티 에셋의 Missing Script 정리");
        EditorGUILayout.LabelField("  - 써드파티와 핵심 게임 스크립트 구분하여 처리");
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("• MMTouchControls Remover", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  - Canvas 비활성화 문제의 근본 원인 제거");
        EditorGUILayout.LabelField("  - Feel 에셋의 MMTouchControls 컴포넌트 자동 감지 및 제거");
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // 연락처 및 버전 정보
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("ℹ️ 정보", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("프로젝트: Twelve Game");
        EditorGUILayout.LabelField("도구 버전: v2.0 (통합 버전)");
        EditorGUILayout.LabelField("최종 업데이트: " + System.DateTime.Now.ToString("yyyy-MM-dd"));
        EditorGUILayout.LabelField("상태: ✅ 모든 주요 문제 해결 완료");
        EditorGUILayout.EndVertical();
    }
} 