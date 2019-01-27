using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace Assembly_CSharp
{
    public static class Utils
    {
        public static SpriteAnimationClip LoadNewSpriteAnimationClip(string[] FilePaths, float PixelsPerUnit = 100.0f, float KeyFrameLength = 0.02f)
        {
            Debug.Log("Hi, welcome to LoadNewSpriteAnimationClip!");
            List<Sprite> sprites = new List<Sprite>();
            List<float> numbers = new List<float>();
            for (int i = 0; i < FilePaths.Length; i++)
            {
                numbers.Add((float)i);
                Debug.Log(String.Format("FRAME {0} IMAGE {1}", i, FilePaths[i]));
                sprites.Add(LoadNewSprite(FilePaths[i]));
            }
            SpriteAnimationClip newClip = new SpriteAnimationClip(KeyFrameLength, numbers.ToArray(), sprites.ToArray());
            return newClip;
        }

        public static Sprite LoadNewSprite(string FilePath, float PixelsPerUnit = 1.0f)
        {
            // Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference

            Sprite NewSprite = new Sprite();
            Texture2D SpriteTexture = LoadTexture(FilePath);
            NewSprite = Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0.5f, 0.5f), PixelsPerUnit, 1);

            return NewSprite;
        }

        public static Texture2D LoadTexture(string FilePath)
        {
            // Load a PNG or JPG file from disk to a Texture2D
            // Returns null if load fails

            Texture2D Tex2D;
            byte[] FileData;

            if (File.Exists(FilePath))
            {
                FileData = File.ReadAllBytes(FilePath);
                Tex2D = new Texture2D(40, 40, TextureFormat.RGBA32, false); // Create new "empty" texture
                if (Tex2D.LoadImage(FileData))
                {   // Load the imagedata into the texture (size is set automatically)

                    // various fixes to stop blurry sprites
                    Tex2D.filterMode = FilterMode.Point;
                    Tex2D.wrapMode = TextureWrapMode.Clamp;
                    Tex2D.anisoLevel = 1;
                    return Tex2D; // If data = readable -> return texture
                }
            }
            return null; // Return null if load failed
        }

        public static void Test()
        {
            Debug.Log("TESTING EMBEDDED RESOURCES");
            Debug.Log(PathMan.GetModInternalFile(PathMan.ANIM_CLIPS_PATH, "AnimClips.xml", "AssemblyCSharp.LadybugTestMod.mm.InitialAnimClipXml.xml"));
        }
    }

    public class AnimClipFrame : IComparable
    {
        public string fullPath { get; set; }
        public string key { get; set; }
        public int number { get; set; }

        public AnimClipFrame(string fullPath, string key, int number)
        {
            this.fullPath = fullPath;
            this.key = key;
            this.number = number;
        }

        int IComparable.CompareTo(object obj)
        {
            AnimClipFrame other = (AnimClipFrame)obj;
            return number.CompareTo(other.number);
        }
    }

    // Only stores keyFrameLength at the moment but maybe they'll be more
    // properties with anim clips in the future?
    public class AnimClipInfo
    {
        public float keyFrameLength = 0.05f;
        public AnimClipInfo(float keyFrameLength = 0.05f)
        {
            this.keyFrameLength = keyFrameLength;
        }
    }
}
