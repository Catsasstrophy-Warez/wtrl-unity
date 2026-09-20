using UnityEditor;
using UnityEngine;

namespace WTRL.EditorTools
{
    /// <summary>
    /// Forces correct TextureImporter settings on every generated PBR
    /// map this project ships -- necessary, not cosmetic. Unity's
    /// shaders decode normal maps with a special per-channel remap that
    /// only kicks in when the asset's Texture Type is "Normal Map"; a
    /// real tangent-space normal map imported as a plain sRGB color
    /// texture (Unity's default for any new PNG) reads back wrong
    /// (flattened/incorrect lighting response) even though the pixel
    /// data itself is correct. Likewise, AO and metallic/smoothness
    /// maps are DATA, not color, and should be imported linear
    /// (sRGB off) -- importing them as sRGB double-gamma-corrects the
    /// values the shader reads as a 0..1 scalar.
    ///
    /// Run via `Assets/WTRL/Configure PBR Texture Import Settings`, or
    /// in batchmode with `-executeMethod
    /// WTRL.EditorTools.PbrTextureImportSettings.ConfigureAll`, BEFORE
    /// building any scene that uses `PbrMaterialFactory` -- the scene
    /// builders read texture data at material-creation time, so a
    /// texture imported with the wrong settings needs a real
    /// reimport, not just a later fix-up.
    /// </summary>
    public static class PbrTextureImportSettings
    {
        private static readonly string[] SearchFolders =
        {
            "Assets/WrenchToRaceLegends/Art/Tracks/Textures",
            "Assets/WrenchToRaceLegends/Art/Environment/Textures",
        };

        [MenuItem("Assets/WTRL/Configure PBR Texture Import Settings")]
        public static void ConfigureAll()
        {
            var normalCount = 0;
            var linearDataCount = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", SearchFolders))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                if (path.EndsWith("_normal.png"))
                {
                    if (importer.textureType != TextureImporterType.NormalMap || importer.convertToNormalmap)
                    {
                        importer.textureType = TextureImporterType.NormalMap;
                        importer.convertToNormalmap = false; // we already author a real normal map, not a heightmap to convert
                        importer.SaveAndReimport();
                    }
                    normalCount++;
                }
                else if (path.EndsWith("_ao.png") || path.EndsWith("_metallicsmoothness.png"))
                {
                    if (importer.textureType != TextureImporterType.Default || importer.sRGBTexture)
                    {
                        importer.textureType = TextureImporterType.Default;
                        importer.sRGBTexture = false; // data, not color -- avoid double gamma-correction
                        importer.SaveAndReimport();
                    }
                    linearDataCount++;
                }
            }

            Debug.Log($"PbrTextureImportSettings: configured {normalCount} normal maps, {linearDataCount} linear-data (AO/metallic-smoothness) maps.");
        }
    }
}
