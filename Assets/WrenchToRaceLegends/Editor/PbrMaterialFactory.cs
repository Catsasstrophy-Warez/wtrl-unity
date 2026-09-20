using UnityEditor;
using UnityEngine;

namespace WTRL.EditorTools
{
    /// <summary>
    /// Builds a real PBR-lit URP material from a set of texture maps
    /// named by convention (`{base}.png` albedo, `{base}_normal.png`,
    /// `{base}_ao.png`, `{base}_metallicsmoothness.png`) -- closes "no
    /// normal/roughness/AO maps" from the follow-up quality-pass
    /// request. Every material this project's scene builders create for
    /// track asphalt/barriers and the ground plane now goes through
    /// this instead of a bare `mainTexture` assignment, so the real
    /// maps generate_world_tracks.py/make_ground_texture.py now produce
    /// actually get used.
    ///
    /// Any of the 3 extra maps may be missing (e.g. the FBX-embedded
    /// vehicle materials this project also builds don't have them) --
    /// each is wired only if its file exists, exactly like the existing
    /// "if (tex != null)" convention throughout this Editor assembly.
    /// </summary>
    public static class PbrMaterialFactory
    {
        /// <param name="albedoPath">Project-relative path to the base
        /// color texture, e.g.
        /// "Assets/.../foundry-row-circuit_asphalt.png". The
        /// normal/AO/metallic-smoothness maps are looked up by
        /// inserting their suffix before ".png" at this same path.</param>
        public static Material Create(string albedoPath)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (string.IsNullOrEmpty(albedoPath) || !albedoPath.EndsWith(".png"))
            {
                return mat;
            }

            var basePath = albedoPath.Substring(0, albedoPath.Length - ".png".Length);

            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            if (albedo != null) mat.mainTexture = albedo;

            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "_normal.png");
            if (normal != null)
            {
                mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", 1f);
            }

            var ao = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "_ao.png");
            if (ao != null)
            {
                mat.SetTexture("_OcclusionMap", ao);
                mat.SetFloat("_OcclusionStrength", 1f);
            }

            // URP/Lit's Metallic Gloss Map convention: R = metallic,
            // A = smoothness, selected via _SmoothnessTextureChannel = 0
            // ("Metallic Alpha" -- as opposed to 1, "Albedo Alpha").
            var metallicSmoothness = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "_metallicsmoothness.png");
            if (metallicSmoothness != null)
            {
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.SetTexture("_MetallicGlossMap", metallicSmoothness);
                mat.SetFloat("_SmoothnessTextureChannel", 0f);
                mat.SetFloat("_Smoothness", 1f); // multiplier on the texture's alpha -- 1 lets the map drive it fully
            }

            return mat;
        }

        /// <summary>Applies the same tiling scale to every texture slot
        /// a material created by <see cref="Create"/> might have wired
        /// (base color, normal, AO, metallic-smoothness) -- setting
        /// tiling on just `_BaseMap` while leaving the PBR maps at 1x
        /// would make them visibly swim out of alignment with the
        /// albedo texture at any tiling scale other than 1.</summary>
        public static void SetTiling(Material mat, Vector2 scale)
        {
            foreach (var property in new[] { "_BaseMap", "_BumpMap", "_OcclusionMap", "_MetallicGlossMap" })
            {
                if (mat.HasProperty(property))
                {
                    mat.SetTextureScale(property, scale);
                }
            }
        }
    }
}
