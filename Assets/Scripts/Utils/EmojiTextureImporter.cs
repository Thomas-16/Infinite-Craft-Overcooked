#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class EmojiTextureImporter : AssetPostprocessor
{
	void OnPreprocessTexture()
	{
		if (assetPath.Contains("Emojis"))
		{
			TextureImporter importer = assetImporter as TextureImporter;
			importer.textureType = TextureImporterType.Sprite;
			importer.spritePixelsPerUnit = 100;  // Adjust based on your needs
			importer.mipmapEnabled = true;
			importer.filterMode = FilterMode.Bilinear;
			// Size the emoji appropriately (you can adjust these values)
			TextureImporterSettings settings = new TextureImporterSettings();
			importer.ReadTextureSettings(settings);
			settings.spriteAlignment = (int)SpriteAlignment.Center;
			importer.SetTextureSettings(settings);
			// Set max texture size to 256 (or your preferred size)
			importer.maxTextureSize = 256;
		}
	}
}
#endif