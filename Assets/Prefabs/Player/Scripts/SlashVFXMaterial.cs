// SlashVFXMaterial.cs
// Helper to create the additive material at runtime if not assigned.
// Not required if you create the material manually in Unity Editor.

using UnityEngine;

public static class SlashVFXMaterial
{
    private static Material _additiveMat;

    public static Material GetAdditive()
    {
        if (_additiveMat != null) return _additiveMat;

        // URP 2D Sprite-Unlit with Additive blending
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        _additiveMat = new Material(shader);
        _additiveMat.SetFloat("_Blend", 1); // Additive
        _additiveMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _additiveMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        _additiveMat.SetInt("_ZWrite", 0);
        _additiveMat.renderQueue = 3000;
        return _additiveMat;
    }
}
