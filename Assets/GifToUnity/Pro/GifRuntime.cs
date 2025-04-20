using GifImporter;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GifImporter
{
    public class GifRuntime
    {
        public static Gif GetGif(byte[] data, string name, bool pixelated = false, bool compress = true)
        {
            try
            {
                var gif = ScriptableObject.CreateInstance<Gif>();
                var frames = GifRuntimeConvert.RuntimeCore.GetFrames(data);
                gif.name = name;
                gif.Frames = new List<GifFrame>();

                var atlas = new Texture2D(1, 1, TextureFormat.RGBA32, true);
                atlas.name = name;

                if (pixelated) atlas.filterMode = FilterMode.Point;

                var textures = new Texture2D[frames.Count];

                for (int i = 0; i < frames.Count; i++)
                {
                    var frame = frames[i];
                    Texture2D texture = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false);

                    texture.name = name + "_" + i;

                    var bytes = new byte[frame.Data.Length];

                    for (int j = 0; j < frame.Height; j++)
                    {
                        Array.Copy(frame.Data, j * frame.Width * 4,
                                   bytes, (frame.Height - j - 1) * frame.Width * 4,
                                   frame.Width * 4);
                    }

                    texture.SetPixelData(bytes, 0);
                    texture.Apply();

                    textures[i] = texture;

                    //-----------
                    //gif.Frames.Add(new GifFrame()
                    //{
                    //    DelayInMs = frame.DelayInMs,
                    //    Sprite = Sprite.Create(texture, new Rect(0,0,texture.width, texture.height), Vector2.zero)
                    //});

                    //return gif;
                }

                var rects = atlas.PackTextures(textures, 2);

                if(compress) atlas.Compress(false);

                for (int i = 0; i < frames.Count; i++)
                {
                    var frame = frames[i];

                    UnityEngine.Object.Destroy(textures[i]);

                    var atlasRest = rects[i];
                    var spriteRect = new Rect(
                        atlasRest.x * atlas.width,
                        atlasRest.y * atlas.height,
                        atlasRest.width * atlas.width,
                        atlasRest.height * atlas.height);

                    var sprite = Sprite.Create(atlas, spriteRect, Vector2.zero);
                    sprite.name = i+" "+atlas.name;

                    gif.Frames.Add(new GifFrame()
                    {
                        DelayInMs = frame.DelayInMs,
                        Sprite = sprite
                    });
                }

                return gif;
            }
            catch(Exception exception)
            {
                Debug.LogError("Can't load gif:" + name);
                Debug.LogException(exception);
                return null;
            }
        }
    }
}