using UnityEngine;
using UnityEditor;
// using UnityEngine.Video; // PNG 시퀀스로 교체됨
using GuildMaster.Data;

[CustomEditor(typeof(CharacterData))]
public class CharacterDataInspector : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        CharacterData characterData = (CharacterData)target;
        
        // 기본 Inspector 그리기
        DrawDefaultInspector();
        
        // Animation Type이 PNGSequence일 때 추가 안내
        if (characterData.animationType == AnimationType.PNGSequence)
        {
            EditorGUILayout.Space(20);
            
            // 안내 박스
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("🖼️ PNG 시퀀스 투명배경 애니메이션 설정", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 필드 상태 체크
            bool hasIdlePNG = characterData.idlePNGSequence != null && characterData.idlePNGSequence.Length > 0;
            bool hasAttackPNG = characterData.attackPNGSequence != null && characterData.attackPNGSequence.Length > 0;
            
            // Idle PNG 시퀀스 상태
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🧘 Idle PNG:", GUILayout.Width(100));
            if (hasIdlePNG)
            {
                EditorGUILayout.LabelField($"✅ {characterData.idlePNGSequence.Length} frames", EditorStyles.miniLabel);
            }
            else
            {
                GUI.color = Color.red;
                EditorGUILayout.LabelField("❌ 필요함", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();
            
            // Attack PNG 시퀀스 상태
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("⚔️ Attack PNG:", GUILayout.Width(100));
            if (hasAttackPNG)
            {
                EditorGUILayout.LabelField($"✅ {characterData.attackPNGSequence.Length} frames", EditorStyles.miniLabel);
            }
            else
            {
                GUI.color = Color.red;
                EditorGUILayout.LabelField("❌ 필요함", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();
            
            // 프레임레이트 표시
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("⚡ Frame Rate:", GUILayout.Width(100));
            EditorGUILayout.LabelField($"{characterData.pngSequenceFrameRate} FPS", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // 사용법 안내
            EditorGUILayout.LabelField("💡 PNG 시퀀스 자동 설정 방법:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 중요한 설정 안내를 박스로 강조
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField("🖼️ PNG 시퀀스는 완벽한 투명배경을 지원합니다!", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("1. PNG 파일들을 Assets/Video/ 폴더에 배치", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("2. 파일명에 'idle' 또는 'attack' 키워드 포함", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("3. Unity 메뉴 → Twelve → 🖼️ PNG 시퀀스 도구 사용", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("4. 자동으로 감지하여 할당됩니다!", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // 추가 도움말
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🖼️ PNG 시퀀스 자동 설정 도구"))
            {
                ShowPNGSequenceSetupGuide();
            }
            if (GUILayout.Button("📁 Video 폴더에서 빠른 설정"))
            {
                ShowQuickSetupGuide();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // 빠른 액션 버튼들
            EditorGUILayout.LabelField("🚀 빠른 액션:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🔍 PNG 시퀀스 검증"))
            {
                ValidatePNGSequences(characterData);
            }
            
            if (GUILayout.Button("🎬 테스트 캐릭터 생성"))
            {
                CreateTestCharacter(characterData);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 상태 요약
            if (hasIdlePNG && hasAttackPNG)
            {
                EditorGUILayout.BeginVertical("HelpBox");
                GUI.color = Color.green;
                EditorGUILayout.LabelField("✅ PNG 시퀀스 설정 완료!", EditorStyles.boldLabel);
                GUI.color = Color.white;
                EditorGUILayout.LabelField(
                    $"🎬 PNGSequenceController에서 사용 가능합니다.\n" +
                    $"📊 총 {characterData.idlePNGSequence.Length + characterData.attackPNGSequence.Length} frames\n" +
                    $"⚡ {characterData.pngSequenceFrameRate} FPS로 재생됩니다.",
                    EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical("HelpBox");
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("⚠️ PNG 시퀀스 설정이 필요합니다", EditorStyles.boldLabel);
                GUI.color = Color.white;
                string missingInfo = "";
                if (!hasIdlePNG) missingInfo += "• Idle PNG 시퀀스 누락\n";
                if (!hasAttackPNG) missingInfo += "• Attack PNG 시퀀스 누락\n";
                EditorGUILayout.LabelField(missingInfo + "🛠️ 위의 자동 설정 도구를 사용하세요!", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField("🖼️ PNG 시퀀스를 사용하려면 Animation Type을 PNGSequence로 변경하세요", EditorStyles.helpBox);
            EditorGUILayout.EndVertical();
        }
    }
    
    private void ShowPNGSequenceSetupGuide()
    {
        Debug.Log("=== 🖼️ PNG 시퀀스 자동 설정 가이드 ===");
        Debug.Log("Unity 메뉴 → Twelve → 🖼️ PNG 시퀀스 도구 → PNG 시퀀스 자동 설정");
        Debug.Log("📁 폴더를 선택하고 PNG 시퀀스를 자동으로 감지하여 설정할 수 있습니다.");
        Debug.Log("🔍 파일명에 'idle', 'attack' 키워드가 포함된 PNG들을 자동 분류합니다.");
        Debug.Log("✨ 프레임레이트, 루프 설정 등을 한 번에 처리할 수 있습니다.");
    }
    
    private void ShowQuickSetupGuide()
    {
        Debug.Log("=== 📁 Video 폴더 빠른 설정 가이드 ===");
        Debug.Log("Unity 메뉴 → Twelve → 🖼️ PNG 시퀀스 도구 → 📁 Video 폴더에서 빠른 설정");
        Debug.Log("Assets/Video/ 폴더의 PNG 파일들을 자동으로 스캔합니다.");
        Debug.Log("🎬 새 CharacterData 생성 또는 📋 기존 CharacterData 선택 가능");
        Debug.Log("⚡ 가장 빠르고 쉬운 방법입니다!");
    }
    
    private void ValidatePNGSequences(CharacterData characterData)
    {
        Debug.Log($"=== 🔍 '{characterData.name}' PNG 시퀀스 검증 ===");
        
        bool isValid = true;
        
        // Idle 시퀀스 검증
        if (characterData.idlePNGSequence == null || characterData.idlePNGSequence.Length == 0)
        {
            Debug.LogWarning("❌ Idle PNG 시퀀스가 없습니다.");
            isValid = false;
        }
        else
        {
            int nullFrames = 0;
            for (int i = 0; i < characterData.idlePNGSequence.Length; i++)
            {
                if (characterData.idlePNGSequence[i] == null) nullFrames++;
            }
            
            if (nullFrames > 0)
            {
                Debug.LogWarning($"❌ Idle 시퀀스에서 {nullFrames}개 프레임이 누락되었습니다.");
                isValid = false;
            }
            else
            {
                Debug.Log($"✅ Idle 시퀀스: {characterData.idlePNGSequence.Length} frames");
            }
        }
        
        // Attack 시퀀스 검증
        if (characterData.attackPNGSequence == null || characterData.attackPNGSequence.Length == 0)
        {
            Debug.LogWarning("❌ Attack PNG 시퀀스가 없습니다.");
            isValid = false;
        }
        else
        {
            int nullFrames = 0;
            for (int i = 0; i < characterData.attackPNGSequence.Length; i++)
            {
                if (characterData.attackPNGSequence[i] == null) nullFrames++;
            }
            
            if (nullFrames > 0)
            {
                Debug.LogWarning($"❌ Attack 시퀀스에서 {nullFrames}개 프레임이 누락되었습니다.");
                isValid = false;
            }
            else
            {
                Debug.Log($"✅ Attack 시퀀스: {characterData.attackPNGSequence.Length} frames");
            }
        }
        
        // 프레임레이트 검증
        if (characterData.pngSequenceFrameRate <= 0)
        {
            Debug.LogWarning("❌ PNG 시퀀스 프레임레이트가 0 이하입니다. 12 FPS로 설정하는 것을 권장합니다.");
            isValid = false;
        }
        else
        {
            Debug.Log($"✅ 프레임레이트: {characterData.pngSequenceFrameRate} FPS");
        }
        
        if (isValid)
        {
            Debug.Log("🎉 PNG 시퀀스 설정이 완벽합니다!");
        }
        else
        {
            Debug.LogWarning("🛠️ PNG 시퀀스 자동 설정 도구를 사용하여 문제를 해결하세요.");
        }
    }
    
    private void CreateTestCharacter(CharacterData characterData)
    {
        Debug.Log($"🎬 '{characterData.name}' 테스트 캐릭터 생성 중...");
        
        // 테스트용 GameObject 생성
        GameObject testCharacter = new GameObject($"Test PNG Character - {characterData.characterName}");
        
        // Canvas 찾기 또는 생성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Test Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        testCharacter.transform.SetParent(canvas.transform, false);
        
        // RawImage 컴포넌트 추가
        var rawImage = testCharacter.AddComponent<UnityEngine.UI.RawImage>();
        
        // PNGSequenceController 추가
        var pngController = testCharacter.AddComponent<GuildMaster.Battle.PNGSequenceController>();
        pngController.characterData = characterData;
        pngController.displayImage = rawImage;
        
        // 위치 설정
        var rectTransform = testCharacter.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 200);
        rectTransform.anchoredPosition = Vector2.zero;
        
        // 선택
        Selection.activeGameObject = testCharacter;
        
        Debug.Log("✅ 테스트 캐릭터가 생성되었습니다!");
        Debug.Log("🎮 Play 모드에서 PNG 시퀀스 애니메이션을 확인할 수 있습니다.");
        EditorGUIUtility.PingObject(testCharacter);
    }
} 