using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GifExtract;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GifImporter.Editors
{
    public class GifImporter : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
                                           string[] movedFromAssetPaths)
        {
            foreach (string file in importedAssets)
            {
                if (file.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase))
                {
                    Debug.Log("Reimported Asset: " + file);
                    GenerateSprites(file);
                }
            }
        }

        class Frame
        {
            public Texture2D             Texture2D;
            public string                Path;
            public GifExtract.Core.Frame FrameData;
            public string                LocalPath;
        }

        private static void GenerateSprites(string file)
        {
            var filePath = Application.dataPath + file.Substring("Assets".Length);

            FileInfo fileInfo       = new FileInfo(filePath);
            var      name           = fileInfo.Name.Replace(fileInfo.Extension, "");
            var      dir            = fileInfo.Directory;
            var      spritesDirPath = Path.Combine(dir.FullName, name);

            var framesData = Core.GetFrames(filePath);
            int frameCount = framesData.Count;

            List<Frame> frames = new List<Frame>();

            for (int i = 0; i < frameCount; i++)
            {
                var data = framesData[i];

                Texture2D texture = new Texture2D(data.Width, data.Height, TextureFormat.RGBA32, false);

                texture.name                = name + "_" + i;
                texture.alphaIsTransparency = true;

                var bytes = new byte[data.Data.Length];

                for (int j = 0; j < data.Height; j++)
                {
                    Array.Copy(data.Data, j * data.Width * 4,
                               bytes, (data.Height - j - 1) * data.Width * 4,
                               data.Width * 4);
                }


                texture.SetPixelData(bytes, 0);
                texture.Apply();

                frames.Add(new Frame
                {
                    Texture2D = texture,
                    FrameData = data
                });
            }


            if (Directory.Exists(spritesDirPath))
            {
                try
                {
                    // 읽기 전용 속성 제거 시도
                    var directoryInfo = new DirectoryInfo(spritesDirPath);
                    if (directoryInfo.Exists && (directoryInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        directoryInfo.Attributes &= ~FileAttributes.ReadOnly;
                    }
                    
                    // 하위 파일들의 읽기 전용 속성도 제거
                    foreach (var subFile in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
                    {
                        if ((subFile.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            subFile.Attributes &= ~FileAttributes.ReadOnly;
                        }
                    }
                    
                    Directory.Delete(spritesDirPath, true);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Debug.LogWarning($"[GifImporter] 폴더 삭제 권한이 없습니다: {spritesDirPath}. 경고: {ex.Message}");
                    // 삭제에 실패해도 계속 진행
                }
                catch (IOException ex)
                {
                    Debug.LogWarning($"[GifImporter] 폴더 삭제 중 IO 오류 발생: {spritesDirPath}. 경고: {ex.Message}");
                    // 삭제에 실패해도 계속 진행
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GifImporter] 폴더 삭제 중 예상치 못한 오류: {spritesDirPath}. 경고: {ex.Message}");
                    // 삭제에 실패해도 계속 진행
                }
            }

            Directory.CreateDirectory(spritesDirPath);

            for (int i = 0; i < frameCount; i++)
            {
                var pngBytes  = frames[i].Texture2D.EncodeToPNG();
                var frameName = $"{name}_{i + 1}.png";
                var framePath = Path.Combine(spritesDirPath, frameName);
                File.WriteAllBytes(framePath, pngBytes);

                frames[i].Path      = framePath;
                frames[i].LocalPath = framePath.Replace('\\', '/').Replace(Application.dataPath, "Assets");

                AssetDatabase.ImportAsset(frames[i].LocalPath,
                                          ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
                var importer = AssetImporter.GetAtPath(frames[i].LocalPath) as TextureImporter;

                if (importer != null)
                {
                    importer.textureType         = TextureImporterType.Sprite;
                    importer.spriteImportMode    = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 32;

                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.filterMode         = FilterMode.Point;
                    importer.npotScale          = TextureImporterNPOTScale.None;

                    importer.SaveAndReimport();
                }
            }

            var localDir = spritesDirPath.Replace('\\', '/').Replace(Application.dataPath, "Assets");
            SessionState.SetString("LastGifExport", localDir);

            AssetDatabase.ImportAsset(localDir, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            var localPath = localDir + ".asset";

            var gif = AssetDatabase.LoadAssetAtPath<Gif>(localPath);

            if (gif == null)
            {
                gif = ScriptableObject.CreateInstance<Gif>();
                AssetDatabase.CreateAsset(gif, localDir + ".asset");
            }

            gif.name   = name;
            gif.Frames = new List<GifFrame>();

            foreach (var frame in frames)
            {
                gif.Frames.Add(new GifFrame()
                {
                    DelayInMs = frame.FrameData.DelayInMs,
                    Sprite    = AssetDatabase.LoadAssetAtPath<Sprite>(frame.LocalPath)
                });
            }

            EditorUtility.SetDirty(gif);
            AssetDatabase.SaveAssetIfDirty(gif);
        }
    }
}