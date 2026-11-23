using UnityEditor;
using UnityEngine;

public static class NeKArtonSpriteSheetSetup
{
    [MenuItem("NeKArton/Setup character sprite sheets")]
    public static void Setup()
    {
        SetupSheet("Assets/Sprite/движение_гг.png", 24, "GG");
        SetupSheet("Assets/Sprite/движение_насти.png", 22, "Nastya");
        AssetDatabase.Refresh();
        Debug.Log("NeKArton: sprite sheets sliced. Select each texture and verify the frames in Sprite Editor.");
    }

    private static void SetupSheet(string path, int columns, string prefix)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("Texture not found: " + path);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null) return;

        float frameWidth = (float)texture.width / columns;
        var metas = new SpriteMetaData[columns];
        for (int i = 0; i < columns; i++)
        {
            metas[i] = new SpriteMetaData
            {
                name = prefix + "_" + (i + 1).ToString("00"),
                rect = new Rect(i * frameWidth, 0, frameWidth, texture.height),
                alignment = (int)SpriteAlignment.BottomCenter,
                pivot = new Vector2(0.5f, 0f)
            };
        }

        importer.spritesheet = metas;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }
}
