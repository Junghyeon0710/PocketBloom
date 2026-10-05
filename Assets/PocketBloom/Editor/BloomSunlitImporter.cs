using System;
using UnityEditor;
using UnityEngine;

namespace PocketBloom.Editor
{
    public sealed class BloomSunlitImporter : AssetPostprocessor
    {
        const string Root = "Assets/PocketBloom/Resources/Sunlit/";
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root, StringComparison.Ordinal)) return;
            Configure((TextureImporter)assetImporter);
        }
        static void Configure(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear; importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
        [MenuItem("Pocket Bloom/Import Sunlit Garden Art")]
        public static void Import()
        {
            foreach (string path in System.IO.Directory.GetFiles(Root, "*.png"))
            {
                var importer = AssetImporter.GetAtPath(path.Replace('\\', '/')) as TextureImporter;
                if (!importer) throw new InvalidOperationException("Texture importer missing: " + path);
                Configure(importer); importer.SaveAndReimport();
            }
        }
        [MenuItem("Pocket Bloom/Bake Sunlit Garden Font")]
        public static void BakeFont()
        {
            const string path = "Assets/PocketBloom/Resources/SunlitFont.asset";
            if (AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(path))
                throw new InvalidOperationException("SunlitFont already exists; review before replacing the asset.");
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/PocketBloom/Art/PocketBloomSans-SemiBold.ttf");
            if (!source) throw new InvalidOperationException("Run Tools/BakeSunlitFont.py first.");
            var font = TMPro.TMP_FontAsset.CreateFontAsset(source, 48, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, TMPro.AtlasPopulationMode.Dynamic, false);
            font.name = "SunlitFont";
            string text = "";
            foreach (string file in System.IO.Directory.GetFiles("Assets/PocketBloom/Scripts", "*.cs")) text += System.IO.File.ReadAllText(file);
            for (int i = 32; i <= 126; i++) text += (char)i;
            text += "→·…";
            font.TryAddCharacters(text, out string missing);
            if (!string.IsNullOrEmpty(missing.Trim()))
                Debug.LogWarning("Font source lacks source-code characters: " + missing);
            font.atlasPopulationMode = TMPro.AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, path);
            font.material.name = "SunlitFont Material"; AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var texture in font.atlasTextures) { texture.name = "SunlitFont Atlas"; AssetDatabase.AddObjectToAsset(texture, font); }
            EditorUtility.SetDirty(font); AssetDatabase.SaveAssets();
        }
    }
}
