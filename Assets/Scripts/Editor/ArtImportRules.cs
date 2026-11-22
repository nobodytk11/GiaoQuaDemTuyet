using UnityEditor;
using UnityEngine;

namespace GiaoQua.EditorTools
{
    /// <summary>
    /// Import rules for every texture under Assets/Art.
    /// Kenney tiles are 70 px, so 70 pixels = 1 world unit (1 tile = 1 m).
    /// Characters and enemies get their pivot at the feet, so frames of different heights stand on the same line.
    /// </summary>
    public class ArtImportRules : AssetPostprocessor
    {
        const float PixelsPerUnit = 70f;

        public override uint GetVersion() { return 1; }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool isTile = assetPath.Contains("/Tiles/");
            bool standsOnFeet = assetPath.Contains("/Player/") || assetPath.Contains("/Enemies/");
            settings.spriteMeshType = isTile ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            settings.spriteAlignment = (int)(standsOnFeet ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
            importer.SetTextureSettings(settings);
        }
    }
}
