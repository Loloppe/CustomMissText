using System;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomMissText.Services
{
    // Yoinked from HitScoreVisualizer, credit goes to Eris and qqrz997
    internal class BloomFontProvider : IDisposable
    {
        public enum HsvFontType
        {
            Default = 0,
            Bloom
        }

        private readonly Lazy<TMP_FontAsset> cachedTekoFont;
        private readonly Lazy<TMP_FontAsset> bloomTekoFont;

        public BloomFontProvider()
        {
            var tekoFontAsset = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(x => x.name == "Teko-Medium SDF");
            var bloomFontShader = Resources.FindObjectsOfTypeAll<Shader>().FirstOrDefault(x => x.name == "TextMeshPro/Distance Field");

            if (tekoFontAsset == null)
            {
                throw new("Teko-Medium SDF not found, unable to create HSV fonts. This is likely because of a game update.");
            }
            if (bloomFontShader == null)
            {
                throw new("Bloom font shader not found, unable to create HSV fonts. This is likely because of a game update.");
            }

            cachedTekoFont = new(() => tekoFontAsset.CopyFontAsset(), LazyThreadSafetyMode.ExecutionAndPublication);
            bloomTekoFont = new(() =>
            {
                var bloomTekoFont = tekoFontAsset.CopyFontAsset("Teko-Medium SDF (Bloom)");
                bloomTekoFont.material.shader = bloomFontShader;
                return bloomTekoFont;
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public TMP_FontAsset GetFontForType(HsvFontType hsvFontType)
        {
            return hsvFontType switch
            {
                HsvFontType.Default => cachedTekoFont.Value,
                HsvFontType.Bloom => bloomTekoFont.Value,
                _ => throw new ArgumentOutOfRangeException(nameof(hsvFontType))
            };
        }

        public void Dispose()
        {
            if (cachedTekoFont.IsValueCreated && cachedTekoFont.Value != null)
            {
                Object.Destroy(cachedTekoFont.Value);
            }

            if (bloomTekoFont.IsValueCreated && bloomTekoFont.Value != null)
            {
                Object.Destroy(bloomTekoFont.Value);
            }
        }
    }

    internal class MaterialProperties
    {
        public static int MainTex { get; } = Shader.PropertyToID("_MainTex");
    }

    internal static class TextMeshExtensions
    {
        public static TMP_FontAsset CopyFontAsset(this TMP_FontAsset original, string newName = "")
        {
            if (string.IsNullOrEmpty(newName))
            {
                newName = original.name;
            }

            var newFontAsset = Object.Instantiate(original);

            var texture = original.atlasTexture;

            var newTexture = new Texture2D(texture.width, texture.height, texture.format, texture.mipmapCount, true) { name = $"{newName} Atlas" };
            Graphics.CopyTexture(texture, newTexture);

            var material = new Material(original.material) { name = $"{newName} Atlas Material" };
            material.SetTexture(MaterialProperties.MainTex, newTexture);

            newFontAsset.m_AtlasTexture = newTexture;
            newFontAsset.name = newName;
            newFontAsset.atlasTextures = [newTexture];
            newFontAsset.material = material;

            return newFontAsset;
        }
    }
}
