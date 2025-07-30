using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// PNG 시퀀스 애니메이션을 쉽게 설정할 수 있는 자동화 도구
/// </summary>
public class PNGSequenceTools : EditorWindow
{
    private string sourceFolder = "Assets/Video";
    private CharacterData targetCharacterData;
    private Vector2 scrollPosition;
    private List<PNGSequenceGroup> detectedSequences = new List<PNGSequenceGroup>();
    
    [System.Serializable]
    public class PNGSequenceGroup
    {
        public string sequenceName;
        public string animationType; // idle, attack, walk, skill 등
        public List<Texture2D> frames = new List<Texture2D>();
        public bool isSelected = true;
        public int estimatedFrameRate = 12;
    }
    
    [MenuItem("Twelve/🖼️ PNG 도구/PNG 시퀀스 자동 설정")]
    public static void ShowWindow()
    {
        PNGSequenceTools window = GetWindow<PNGSequenceTools>("🖼️ PNG 시퀀스 자동 설정");
        window.minSize = new Vector2(500, 600);
        window.Show();
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("🖼️ PNG 시퀀스 자동 설정 도구", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        // 소스 폴더 설정
        EditorGUILayout.LabelField("📁 PNG 시퀀스 폴더:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        sourceFolder = EditorGUILayout.TextField("폴더 경로:", sourceFolder);
        if (GUILayout.Button("📂 폴더 선택", GUILayout.Width(100)))
        {
            string selectedFolder = EditorUtility.OpenFolderPanel("PNG 시퀀스 폴더 선택", "Assets", "");
            if (!string.IsNullOrEmpty(selectedFolder))
            {
                sourceFolder = FileUtil.GetProjectRelativePath(selectedFolder);
                DetectPNGSequences();
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // CharacterData 대상 설정
        EditorGUILayout.LabelField("🎭 대상 캐릭터 데이터:", EditorStyles.boldLabel);
        targetCharacterData = (CharacterData)EditorGUILayout.ObjectField("CharacterData:", targetCharacterData, typeof(CharacterData), false);
        
        EditorGUILayout.Space(10);
        
        // 스캔 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔍 PNG 시퀀스 스캔", GUILayout.Height(30)))
        {
            DetectPNGSequences();
        }
        if (GUILayout.Button("🎬 새 CharacterData 생성", GUILayout.Height(30)))
        {
            CreateNewCharacterData();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(15);
        
        // 감지된 시퀀스 표시
        if (detectedSequences.Count > 0)
        {
            EditorGUILayout.LabelField($"✅ 감지된 PNG 시퀀스: {detectedSequences.Count}개", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            foreach (var sequence in detectedSequences)
            {
                DrawSequenceGroup(sequence);
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            
            // 적용 버튼
            EditorGUI.BeginDisabledGroup(targetCharacterData == null);
            if (GUILayout.Button("✨ PNG 시퀀스 자동 할당 적용", GUILayout.Height(40)))
            {
                ApplyPNGSequences();
            }
            EditorGUI.EndDisabledGroup();
            
            if (targetCharacterData == null)
            {
                EditorGUILayout.HelpBox("⚠️ CharacterData를 선택해주세요!", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("�� PNG 시퀀스를 스캔하려면 '🔍 PNG 시퀀스 스캔' 버튼을 클릭하세요.", MessageType.Info);
        }
    }
    
    private void DrawSequenceGroup(PNGSequenceGroup sequence)
    {
        EditorGUILayout.BeginVertical("HelpBox");
        
        // 시퀀스 헤더
        EditorGUILayout.BeginHorizontal();
        sequence.isSelected = EditorGUILayout.Toggle(sequence.isSelected, GUILayout.Width(20));
        EditorGUILayout.LabelField($"🎬 {sequence.sequenceName}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"({sequence.frames.Count} frames)", EditorStyles.miniLabel, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        
        // 애니메이션 타입 및 프레임레이트
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("타입:", GUILayout.Width(40));
        sequence.animationType = EditorGUILayout.TextField(sequence.animationType, GUILayout.Width(100));
        EditorGUILayout.LabelField("FPS:", GUILayout.Width(30));
        sequence.estimatedFrameRate = EditorGUILayout.IntSlider(sequence.estimatedFrameRate, 6, 60, GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();
        
        // 프레임 미리보기 (처음 몇 개만)
        if (sequence.frames.Count > 0)
        {
            EditorGUILayout.LabelField("📼 프레임 미리보기:");
            EditorGUILayout.BeginHorizontal();
            int previewCount = Mathf.Min(sequence.frames.Count, 5);
            for (int i = 0; i < previewCount; i++)
            {
                if (sequence.frames[i] != null)
                {
                    GUILayout.Box(sequence.frames[i], GUILayout.Width(40), GUILayout.Height(40));
                }
            }
            if (sequence.frames.Count > 5)
            {
                EditorGUILayout.LabelField($"... +{sequence.frames.Count - 5}개", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }
    
    private void DetectPNGSequences()
    {
        detectedSequences.Clear();
        
        if (!Directory.Exists(sourceFolder))
        {
            Debug.LogWarning($"[PNG Tools] 폴더를 찾을 수 없습니다: {sourceFolder}");
            return;
        }
        
        // PNG 파일들 찾기
        string[] pngFiles = Directory.GetFiles(sourceFolder, "*.png", SearchOption.AllDirectories);
        
        if (pngFiles.Length == 0)
        {
            Debug.LogWarning($"[PNG Tools] PNG 파일을 찾을 수 없습니다: {sourceFolder}");
            return;
        }
        
        // 파일명 패턴으로 그룹화
        Dictionary<string, List<string>> sequenceGroups = new Dictionary<string, List<string>>();
        
        foreach (string filePath in pngFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string groupKey = ExtractSequenceGroup(fileName);
            
            if (!sequenceGroups.ContainsKey(groupKey))
            {
                sequenceGroups[groupKey] = new List<string>();
            }
            sequenceGroups[groupKey].Add(filePath);
        }
        
        // 시퀀스 그룹 생성
        foreach (var group in sequenceGroups)
        {
            if (group.Value.Count > 1) // 2개 이상의 파일이 있는 시퀀스만
            {
                PNGSequenceGroup sequence = new PNGSequenceGroup();
                sequence.sequenceName = group.Key;
                sequence.animationType = DetermineAnimationType(group.Key);
                
                // 파일 정렬 (숫자 순서대로)
                group.Value.Sort(new NaturalStringComparer());
                
                // Texture2D 로드
                foreach (string filePath in group.Value)
                {
                    string assetPath = filePath.Replace('\\', '/');
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    if (texture != null)
                    {
                        sequence.frames.Add(texture);
                    }
                }
                
                if (sequence.frames.Count > 0)
                {
                    detectedSequences.Add(sequence);
                }
            }
        }
        
        Debug.Log($"[PNG Tools] ✅ {detectedSequences.Count}개의 PNG 시퀀스를 감지했습니다!");
        
        foreach (var seq in detectedSequences)
        {
            Debug.Log($"[PNG Tools] 📼 {seq.sequenceName} ({seq.animationType}): {seq.frames.Count} frames");
        }
    }
    
    private string ExtractSequenceGroup(string fileName)
    {
        // 파일명에서 숫자와 언더스코어를 제거하여 그룹명 추출
        // 예: "archer_idle_01" -> "archer_idle"
        //     "Knight_Attack_001" -> "Knight_Attack"
        
        string pattern = @"[_\-\s]*\d+$"; // 끝의 숫자와 구분자 제거
        string groupName = Regex.Replace(fileName, pattern, "", RegexOptions.IgnoreCase);
        
        // 추가적인 정리
        groupName = groupName.Trim('_', '-', ' ');
        
        return string.IsNullOrEmpty(groupName) ? fileName : groupName;
    }
    
    private string DetermineAnimationType(string sequenceName)
    {
        string lowerName = sequenceName.ToLower();
        
        if (lowerName.Contains("idle") || lowerName.Contains("stand") || lowerName.Contains("wait"))
            return "idle";
        else if (lowerName.Contains("attack") || lowerName.Contains("hit") || lowerName.Contains("strike"))
            return "attack";
        else if (lowerName.Contains("walk") || lowerName.Contains("move") || lowerName.Contains("run"))
            return "walk";
        else if (lowerName.Contains("skill") || lowerName.Contains("magic") || lowerName.Contains("cast"))
            return "skill";
        else if (lowerName.Contains("death") || lowerName.Contains("die") || lowerName.Contains("dead"))
            return "death";
        else
            return "unknown";
    }
    
    private void ApplyPNGSequences()
    {
        if (targetCharacterData == null)
        {
            Debug.LogError("[PNG Tools] CharacterData가 선택되지 않았습니다!");
            return;
        }
        
        // AnimationType을 PNGSequence로 설정
        targetCharacterData.animationType = AnimationType.PNGSequence;
        
        int appliedCount = 0;
        
        foreach (var sequence in detectedSequences)
        {
            if (!sequence.isSelected || sequence.frames.Count == 0) continue;
            
            switch (sequence.animationType.ToLower())
            {
                case "idle":
                    targetCharacterData.idlePNGSequence = sequence.frames.ToArray();
                    appliedCount++;
                    Debug.Log($"[PNG Tools] ✅ Idle 시퀀스 적용: {sequence.frames.Count} frames");
                    break;
                    
                case "attack":
                    targetCharacterData.attackPNGSequence = sequence.frames.ToArray();
                    appliedCount++;
                    Debug.Log($"[PNG Tools] ✅ Attack 시퀀스 적용: {sequence.frames.Count} frames");
                    break;
                    
                default:
                    Debug.LogWarning($"[PNG Tools] ⚠️ 지원되지 않는 애니메이션 타입: {sequence.animationType}");
                    break;
            }
        }
        
        // 프레임레이트 설정 (첫 번째 선택된 시퀀스의 값 사용)
        var firstSelected = detectedSequences.FirstOrDefault(s => s.isSelected);
        if (firstSelected != null)
        {
            targetCharacterData.pngSequenceFrameRate = firstSelected.estimatedFrameRate;
        }
        
        // 에셋 저장
        EditorUtility.SetDirty(targetCharacterData);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"[PNG Tools] 🎉 완료! {appliedCount}개의 PNG 시퀀스가 적용되었습니다!");
        EditorUtility.DisplayDialog("PNG 시퀀스 적용 완료", 
            $"✅ {appliedCount}개의 PNG 시퀀스가 성공적으로 적용되었습니다!\n\n" +
            $"📋 적용된 CharacterData: {targetCharacterData.name}\n" +
            $"🎬 Animation Type: PNGSequence\n" +
            $"⚡ Frame Rate: {targetCharacterData.pngSequenceFrameRate} FPS", "확인");
    }
    
    private void CreateNewCharacterData()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "새 CharacterData 생성", 
            "NewPNGCharacter", 
            "asset", 
            "PNG 시퀀스용 CharacterData를 저장할 위치를 선택하세요");
            
        if (!string.IsNullOrEmpty(path))
        {
            CharacterData newCharacterData = ScriptableObject.CreateInstance<CharacterData>();
            newCharacterData.characterName = "PNG Character";
            newCharacterData.animationType = AnimationType.PNGSequence;
            newCharacterData.pngSequenceFrameRate = 12;
            
            AssetDatabase.CreateAsset(newCharacterData, path);
            AssetDatabase.SaveAssets();
            
            targetCharacterData = newCharacterData;
            
            Debug.Log($"[PNG Tools] ✅ 새 CharacterData 생성됨: {path}");
            EditorGUIUtility.PingObject(newCharacterData);
        }
    }
}

/// <summary>
/// 자연스러운 문자열 정렬을 위한 Comparer (숫자 순서 고려)
/// </summary>
public class NaturalStringComparer : IComparer<string>
{
    public int Compare(string x, string y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        
        // 숫자가 포함된 문자열의 자연스러운 정렬
        // 예: file1.png, file2.png, file10.png, file11.png
        
        var regex = new Regex(@"(\d+)");
        var xMatch = regex.Match(x);
        var yMatch = regex.Match(y);
        
        if (xMatch.Success && yMatch.Success)
        {
            string xPrefix = x.Substring(0, xMatch.Index);
            string yPrefix = y.Substring(0, yMatch.Index);
            
            if (xPrefix == yPrefix)
            {
                int xNum = int.Parse(xMatch.Value);
                int yNum = int.Parse(yMatch.Value);
                return xNum.CompareTo(yNum);
            }
        }
        
        return string.Compare(x, y, System.StringComparison.OrdinalIgnoreCase);
    }
} 