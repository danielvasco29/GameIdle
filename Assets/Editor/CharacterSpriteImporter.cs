using UnityEditor;
using UnityEngine;

namespace GameIdle
{
    // Automatically applies correct import settings for any texture placed in
    // Characters/Sprites/ or Backgrounds/ — no manual steps required.
    public class CharacterSpriteImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            bool isCharSprite  = assetPath.Contains("Characters/Sprites/");
            bool isBackground  = assetPath.Contains("Resources/Backgrounds/");
            if (!isCharSprite && !isBackground) return;

            var importer = (TextureImporter)assetImporter;

            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.npotScale           = TextureImporterNPOTScale.None; // keep original size
            importer.isReadable          = true; // needed for background flood-fill removal
            importer.mipmapEnabled       = false;
            importer.filterMode          = FilterMode.Bilinear;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterPlatformSettings
            {
                maxTextureSize    = 4096,
                format            = TextureImporterFormat.RGBA32,
                textureCompression = TextureImporterCompression.Uncompressed,
                overridden        = true,
            };
            importer.SetPlatformTextureSettings(settings);
        }
    }
}
