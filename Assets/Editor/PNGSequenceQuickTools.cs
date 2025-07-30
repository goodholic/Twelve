using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// Video 폴더 PNG 파일들의 임포트 설정을 자동으로 적용하는 도구
/// </summary>
public static class PNGImportSettingsTool
{
    [MenuItem("Twelve/🖼️ PNG 도구/📁 Video 폴더 PNG 설정 적용")]
    public static void ApplyVideoFolderPNGSettings()
    {
        ApplyPNGSettings("Assets/Video");
    }
    
    [MenuItem("Twelve/🖼️ PNG 도구/📂 폴더 선택하여 PNG 설정 적용")]
    public static void ApplySelectedFolderPNGSettings()
    {
        string selectedFolder = EditorUtility.OpenFolderPanel("PNG 설정을 적용할 폴더 선택", "Assets", "");
        if (!string.IsNullOrEmpty(selectedFolder))
        {
            string relativePath = FileUtil.GetProjectRelativePath(selectedFolder);
            if (!string.IsNullOrEmpty(relativePath))
            {
                ApplyPNGSettings(relativePath);
            }
            else
            {
                Debug.LogError("[PNG Import Tool] 선택한 폴더가 프로젝트 외부에 있습니다!");
            }
        }
    }
    
    [MenuItem("Assets/🖼️ 선택된 PNG 파일들에 스프라이트 설정 적용", false, 20)]
    public static void ApplyPNGSettingsToSelectedFiles()
    {
        var selectedTextures = Selection.objects.OfType<Texture2D>().ToArray();
        
        if (selectedTextures.Length == 0)
        {
            EditorUtility.DisplayDialog("PNG 선택 필요", 
                "PNG 파일들을 선택한 후 다시 시도해주세요.", "확인");
            return;
        }
        
        ApplySettingsToTextures(selectedTextures);
    }
    
    [MenuItem("Assets/🖼️ 선택된 PNG 파일들에 스프라이트 설정 적용", true)]
    public static bool ValidateApplyPNGSettingsToSelectedFiles()
    {
        return Selection.objects.OfType<Texture2D>().Any();
    }
    
    private static void ApplyPNGSettings(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            EditorUtility.DisplayDialog("폴더 없음", 
                $"폴더를 찾을 수 없습니다: {folderPath}", "확인");
            return;
        }
        
        // PNG 파일 찾기
        string[] pngFiles = Directory.GetFiles(folderPath, "*.png", SearchOption.AllDirectories);
        
        if (pngFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("PNG 파일 없음", 
                $"선택한 폴더에 PNG 파일이 없습니다:\n{folderPath}", "확인");
            return;
        }
        
        Debug.Log($"[PNG Import Tool] 📁 폴더 스캔: {folderPath}");
        Debug.Log($"[PNG Import Tool] 🖼️ 발견된 PNG 파일: {pngFiles.Length}개");
        
        // 사용자에게 확인 요청
        bool confirmed = EditorUtility.DisplayDialog(
            "PNG 임포트 설정 적용",
            $"📁 폴더: {folderPath}\n🖼️ PNG 파일: {pngFiles.Length}개 발견\n\n" +
            "다음 설정을 모든 PNG 파일에 적용하시겠습니까?\n\n" +
            "• Texture Type: Sprite (2D and UI)\n" +
            "• Alpha Source: Input Texture Alpha\n" +
            "• Alpha Is Transparency: ✅",
            "적용",
            "취소");
            
        if (!confirmed) return;
        
        // PNG 파일들을 Texture2D로 로드하고 설정 적용
        var textures = pngFiles.Select(f => 
        {
            string assetPath = f.Replace('\\', '/');
            if (assetPath.StartsWith(Application.dataPath))
            {
                assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }).Where(t => t != null).ToArray();
        
        if (textures.Length > 0)
        {
            ApplySettingsToTextures(textures);
        }
        else
        {
            Debug.LogError("[PNG Import Tool] PNG 파일들을 로드할 수 없습니다!");
        }
    }
    
    private static void ApplySettingsToTextures(Texture2D[] textures)
    {
        if (textures.Length == 0) return;
        
        int processedCount = 0;
        int totalCount = textures.Length;
        
        AssetDatabase.StartAssetEditing();
        
        try
        {
            foreach (var texture in textures)
            {
                string assetPath = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrEmpty(assetPath)) continue;
                
                // 현재 진행상황 표시
                if (EditorUtility.DisplayCancelableProgressBar(
                    "PNG 임포트 설정 적용 중...", 
                    $"처리 중: {texture.name} ({processedCount + 1}/{totalCount})", 
                    (float)processedCount / totalCount))
                {
                    break; // 사용자가 취소함
                }
                
                // TextureImporter 가져오기
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    bool needsReimport = false;
                    
                    // Texture Type 설정
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        needsReimport = true;
                    }
                    
                    // Alpha Source 설정
                    if (importer.alphaSource != TextureImporterAlphaSource.FromInput)
                    {
                        importer.alphaSource = TextureImporterAlphaSource.FromInput;
                        needsReimport = true;
                    }
                    
                    // Alpha Is Transparency 설정
                    if (!importer.alphaIsTransparency)
                    {
                        importer.alphaIsTransparency = true;
                        needsReimport = true;
                    }
                    
                    // 변경사항이 있으면 재임포트
                    if (needsReimport)
                    {
                        importer.SaveAndReimport();
                        Debug.Log($"[PNG Import Tool] ✅ 설정 적용: {texture.name}");
                    }
                    else
                    {
                        Debug.Log($"[PNG Import Tool] ⏭️ 이미 올바른 설정: {texture.name}");
                    }
                }
                
                processedCount++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }
        
        AssetDatabase.Refresh();
        
        // 결과 메시지
        string resultMessage = $"✅ PNG 임포트 설정 적용이 완료되었습니다!\n\n" +
                              $"📊 처리된 파일: {processedCount}/{totalCount}개\n\n" +
                              "적용된 설정:\n" +
                              "• Texture Type: Sprite (2D and UI)\n" +
                              "• Alpha Source: Input Texture Alpha\n" +
                              "• Alpha Is Transparency: ✅";
                              
        EditorUtility.DisplayDialog("PNG 설정 적용 완료", resultMessage, "확인");
        
        Debug.Log($"[PNG Import Tool] 🎉 PNG 임포트 설정 적용 완료 - {processedCount}개 파일 처리됨");
    }
    
    [MenuItem("Twelve/🖼️ PNG 도구/🔍 Video 폴더 PNG 설정 확인")]
    public static void CheckVideoFolderPNGSettings()
    {
        CheckPNGSettingsInFolder("Assets/Video");
    }
    
    private static void CheckPNGSettingsInFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            EditorUtility.DisplayDialog("폴더 없음", 
                $"폴더를 찾을 수 없습니다: {folderPath}", "확인");
            return;
        }
        
        string[] pngFiles = Directory.GetFiles(folderPath, "*.png", SearchOption.AllDirectories);
        
        if (pngFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("PNG 파일 없음", 
                $"선택한 폴더에 PNG 파일이 없습니다:\n{folderPath}", "확인");
            return;
        }
        
        int correctlyConfigured = 0;
        int needsConfiguration = 0;
        string report = "📋 PNG 파일 설정 확인 결과:\n\n";
        
        foreach (string pngFile in pngFiles)
        {
            string assetPath = pngFile.Replace('\\', '/');
            if (assetPath.StartsWith(Application.dataPath))
            {
                assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
            }
            
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                bool isCorrect = importer.textureType == TextureImporterType.Sprite &&
                               importer.alphaSource == TextureImporterAlphaSource.FromInput &&
                               importer.alphaIsTransparency;
                
                string fileName = Path.GetFileName(assetPath);
                
                if (isCorrect)
                {
                    correctlyConfigured++;
                    report += $"✅ {fileName}\n";
                }
                else
                {
                    needsConfiguration++;
                    report += $"❌ {fileName}";
                    
                    if (importer.textureType != TextureImporterType.Sprite)
                        report += " (Texture Type)";
                    if (importer.alphaSource != TextureImporterAlphaSource.FromInput)
                        report += " (Alpha Source)";
                    if (!importer.alphaIsTransparency)
                        report += " (Alpha Transparency)";
                    
                    report += "\n";
                }
            }
        }
        
        report += $"\n📊 총 {pngFiles.Length}개 중 ✅ {correctlyConfigured}개 올바름, ❌ {needsConfiguration}개 수정 필요";
        
        if (needsConfiguration > 0)
        {
            report += "\n\n💡 '📁 Video 폴더 PNG 설정 적용' 메뉴를 사용하여 자동으로 설정하세요!";
        }
        
        EditorUtility.DisplayDialog("PNG 설정 확인 완료", report, "확인");
        Debug.Log($"[PNG Import Tool] 설정 확인 완료 - 올바름: {correctlyConfigured}, 수정 필요: {needsConfiguration}");
    }
} 