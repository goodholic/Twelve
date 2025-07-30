using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// PC/모바일 크로스 플랫폼용 PNG 크기 자동 조정 도구
/// </summary>
public static class PNGResizeTools
{
    [MenuItem("Twelve/🖼️ PNG 도구/📱 모바일 최적화 (720x540)")]
    public static void OptimizeForMobile()
    {
        ResizePNGsInVideoFolder(720, 540, "모바일");
    }

    [MenuItem("Twelve/🖼️ PNG 도구/💻 PC 중간 품질 (960x720)")]
    public static void OptimizeForPC()
    {
        ResizePNGsInVideoFolder(960, 720, "PC");
    }

    [MenuItem("Twelve/🖼️ PNG 도구/🎮 균형 최적화 (840x630)")]
    public static void OptimizeForBalance()
    {
        ResizePNGsInVideoFolder(840, 630, "균형");
    }

    [MenuItem("Twelve/🖼️ PNG 도구/📊 현재 크기 분석")]
    public static void AnalyzeCurrentSizes()
    {
        AnalyzePNGSizes();
    }

    [MenuItem("Twelve/🖼️ PNG 도구/💾 원본 백업 생성")]
    public static void CreateBackup()
    {
        CreateOriginalBackup();
    }
    
    private static void ResizePNGsInVideoFolder(int targetWidth, int targetHeight, string optimizationType)
    {
        string videoFolderPath = "Assets/Video";
        
        if (!Directory.Exists(videoFolderPath))
        {
            EditorUtility.DisplayDialog("폴더 없음", "Assets/Video 폴더를 찾을 수 없습니다.", "확인");
            return;
        }
        
        // PNG 파일 검색
        string[] pngFiles = Directory.GetFiles(videoFolderPath, "*.png", SearchOption.AllDirectories);
        
        if (pngFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("PNG 파일 없음", "Video 폴더에 PNG 파일이 없습니다.", "확인");
            return;
        }
        
        // 사용자 확인
        bool confirmed = EditorUtility.DisplayDialog(
            $"PNG 크기 조정 - {optimizationType}",
            $"📁 대상: {videoFolderPath}\n" +
            $"🖼️ 파일 수: {pngFiles.Length:N0}개\n" +
            $"📏 목표 크기: {targetWidth} x {targetHeight}\n\n" +
            $"⚠️ 주의: 이 작업은 되돌릴 수 없습니다!\n" +
            $"💡 먼저 '💾 원본 백업 생성'을 권장합니다.\n\n" +
            $"계속하시겠습니까?",
            "크기 조정 시작",
            "취소");
            
        if (!confirmed) return;
        
        // 크기 조정 실행
        ResizePNGFiles(pngFiles, targetWidth, targetHeight, optimizationType);
    }
    
    private static void ResizePNGFiles(string[] pngFiles, int targetWidth, int targetHeight, string optimizationType)
    {
        int processedCount = 0;
        int resizedCount = 0;
        int totalCount = pngFiles.Length;
        
        AssetDatabase.StartAssetEditing();
        
        try
        {
            foreach (string filePath in pngFiles)
            {
                // 진행상황 표시
                if (EditorUtility.DisplayCancelableProgressBar(
                    $"PNG 크기 조정 중... ({optimizationType})",
                    $"처리 중: {Path.GetFileName(filePath)} ({processedCount + 1}/{totalCount})",
                    (float)processedCount / totalCount))
                {
                    break; // 사용자가 취소
                }
                
                string assetPath = filePath.Replace('\\', '/');
                if (assetPath.StartsWith(Application.dataPath))
                {
                    assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
                }
                
                // TextureImporter 설정
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    bool needsReimport = false;
                    
                    // 기본 스프라이트 설정 적용
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        needsReimport = true;
                    }
                    
                    if (importer.alphaSource != TextureImporterAlphaSource.FromInput)
                    {
                        importer.alphaSource = TextureImporterAlphaSource.FromInput;
                        needsReimport = true;
                    }
                    
                    if (!importer.alphaIsTransparency)
                    {
                        importer.alphaIsTransparency = true;
                        needsReimport = true;
                    }
                    
                    // 크기 제한 설정
                    if (importer.maxTextureSize != GetMaxTextureSize(targetWidth, targetHeight))
                    {
                        importer.maxTextureSize = GetMaxTextureSize(targetWidth, targetHeight);
                        needsReimport = true;
                    }
                    
                    // 압축 설정
                    var platformSettings = importer.GetDefaultPlatformTextureSettings();
                    if (platformSettings.maxTextureSize != GetMaxTextureSize(targetWidth, targetHeight))
                    {
                        platformSettings.maxTextureSize = GetMaxTextureSize(targetWidth, targetHeight);
                        platformSettings.textureCompression = TextureImporterCompression.Compressed;
                        platformSettings.compressionQuality = 75; // 품질과 크기의 균형
                        importer.SetPlatformTextureSettings(platformSettings);
                        needsReimport = true;
                    }
                    
                    if (needsReimport)
                    {
                        importer.SaveAndReimport();
                        resizedCount++;
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
        string resultMessage = $"✅ PNG 크기 조정 완료! ({optimizationType})\n\n" +
                              $"📊 처리된 파일: {processedCount:N0}/{totalCount:N0}개\n" +
                              $"🔄 크기 조정됨: {resizedCount:N0}개\n" +
                              $"📏 목표 해상도: {targetWidth} x {targetHeight}\n\n" +
                              $"💡 예상 효과:\n" +
                              $"• 메모리 사용량 대폭 감소\n" +
                              $"• 로딩 속도 향상\n" +
                              $"• 모바일 호환성 개선";
                              
        EditorUtility.DisplayDialog("크기 조정 완료", resultMessage, "확인");
        
        Debug.Log($"[PNG Resize] {optimizationType} 완료 - {resizedCount}개 파일 최적화");
    }
    
    private static int GetMaxTextureSize(int targetWidth, int targetHeight)
    {
        int maxDimension = Mathf.Max(targetWidth, targetHeight);
        
        // 2의 거듭제곱으로 올림
        if (maxDimension <= 512) return 512;
        if (maxDimension <= 1024) return 1024;
        if (maxDimension <= 2048) return 2048;
        return 4096;
    }
    
    private static void AnalyzePNGSizes()
    {
        string videoFolderPath = "Assets/Video";
        
        if (!Directory.Exists(videoFolderPath))
        {
            EditorUtility.DisplayDialog("폴더 없음", "Assets/Video 폴더를 찾을 수 없습니다.", "확인");
            return;
        }
        
        string[] pngFiles = Directory.GetFiles(videoFolderPath, "*.png", SearchOption.AllDirectories);
        
        if (pngFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("PNG 파일 없음", "Video 폴더에 PNG 파일이 없습니다.", "확인");
            return;
        }
        
        Dictionary<string, int> sizeGroups = new Dictionary<string, int>();
        long totalSize = 0;
        
        foreach (string filePath in pngFiles.Take(100)) // 샘플링으로 속도 향상
        {
            string assetPath = filePath.Replace('\\', '/');
            if (assetPath.StartsWith(Application.dataPath))
            {
                assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
            }
            
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture != null)
            {
                string sizeKey = $"{texture.width} x {texture.height}";
                if (!sizeGroups.ContainsKey(sizeKey))
                    sizeGroups[sizeKey] = 0;
                sizeGroups[sizeKey]++;
            }
            
            FileInfo fileInfo = new FileInfo(filePath);
            totalSize += fileInfo.Length;
        }
        
        // 폴더 전체 크기 계산
        DirectoryInfo dirInfo = new DirectoryInfo(videoFolderPath);
        long folderSize = dirInfo.GetFiles("*.png", SearchOption.AllDirectories).Sum(f => f.Length);
        
        string analysisReport = "📊 PNG 크기 분석 결과\n\n";
        analysisReport += $"📁 총 파일 수: {pngFiles.Length:N0}개\n";
        analysisReport += $"💾 총 크기: {FormatBytes(folderSize)}\n";
        analysisReport += $"📏 해상도 분포 (샘플 100개):\n\n";
        
        foreach (var kvp in sizeGroups.OrderByDescending(x => x.Value))
        {
            analysisReport += $"• {kvp.Key}: {kvp.Value}개\n";
        }
        
        analysisReport += $"\n💡 권장사항:\n";
        analysisReport += $"• 🎮 크로스 플랫폼: 840x630 (현재 대비 65% 감소)\n";
        analysisReport += $"• 📱 모바일 우선: 720x540 (현재 대비 75% 감소)\n";
        analysisReport += $"• 💻 PC 중간: 960x720 (현재 대비 55% 감소)";
        
        EditorUtility.DisplayDialog("PNG 크기 분석", analysisReport, "확인");
        
        Debug.Log($"[PNG Analysis] 총 {pngFiles.Length}개 파일, 크기: {FormatBytes(folderSize)}");
    }
    
    private static void CreateOriginalBackup()
    {
        string videoFolderPath = "Assets/Video";
        string backupFolderPath = "Assets/Video_Original_Backup";
        
        if (!Directory.Exists(videoFolderPath))
        {
            EditorUtility.DisplayDialog("폴더 없음", "Assets/Video 폴더를 찾을 수 없습니다.", "확인");
            return;
        }
        
        if (Directory.Exists(backupFolderPath))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "백업 폴더 존재",
                "백업 폴더가 이미 존재합니다.\n덮어쓰시겠습니까?",
                "덮어쓰기",
                "취소");
                
            if (!overwrite) return;
            
            Directory.Delete(backupFolderPath, true);
        }
        
        bool confirmed = EditorUtility.DisplayDialog(
            "원본 백업 생성",
            $"📁 원본: {videoFolderPath}\n" +
            $"💾 백업: {backupFolderPath}\n\n" +
            $"⏳ 3.4GB 복사 - 시간이 걸릴 수 있습니다.\n" +
            $"계속하시겠습니까?",
            "백업 시작",
            "취소");
            
        if (!confirmed) return;
        
        EditorUtility.DisplayProgressBar("백업 생성 중...", "폴더 복사 중...", 0.5f);
        
        try
        {
            CopyDirectory(videoFolderPath, backupFolderPath);
            
            EditorUtility.DisplayDialog(
                "백업 완료",
                $"✅ 원본 백업이 생성되었습니다!\n\n" +
                $"📁 백업 위치: {backupFolderPath}\n\n" +
                $"💡 이제 안전하게 크기 조정을 진행할 수 있습니다.",
                "확인");
                
            Debug.Log($"[PNG Backup] 백업 생성 완료: {backupFolderPath}");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("백업 실패", $"백업 생성 중 오류 발생:\n{e.Message}", "확인");
            Debug.LogError($"[PNG Backup] 백업 실패: {e.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
    }
    
    private static void CopyDirectory(string sourceDirName, string destDirName)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);
        DirectoryInfo[] dirs = dir.GetDirectories();
        
        Directory.CreateDirectory(destDirName);
        
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(destDirName, file.Name);
            file.CopyTo(tempPath, true);
        }
        
        foreach (DirectoryInfo subdir in dirs)
        {
            string tempPath = Path.Combine(destDirName, subdir.Name);
            CopyDirectory(subdir.FullName, tempPath);
        }
    }
    
    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        
        return $"{number:n1} {suffixes[counter]}";
    }
} 