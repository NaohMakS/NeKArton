#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NeKArtonAnimationAndDoorSetup
{
    private const int Padding = 32;
    private const int EmptyGapToSplit = 8;

    [MenuItem("NeKArton/Generate padded character frames")]
    public static void GenerateFrames()
    {
        GenerateOne("Assets/Sprite/движение_гг.png", "Assets/Generated/Player", "Player", 24);
        GenerateOne("Assets/Sprite/движение_насти.png", "Assets/Generated/Nastya", "Nastya", 22);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("NeKArton",
            "Кадры сохранены в Assets/Generated. Первый кадр каждого персонажа — отдельный Idle.png; остальные — Walk_XX.png с прозрачным полем 32 px.",
            "OK");
    }

    private static void GenerateOne(string sourcePath, string folder, string prefix, int expected)
    {
        var importer = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        if (importer == null) throw new FileNotFoundException("Не найден спрайт-лист: " + sourcePath);
        importer.textureType = TextureImporterType.Default;
        importer.isReadable = true;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        if (source == null) throw new Exception("Не удалось прочитать " + sourcePath);
        Color32[] pixels = source.GetPixels32();
        int w = source.width, h = source.height;
        bool[] occupied = new bool[w];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (pixels[y * w + x].a > 8) { occupied[x] = true; break; }

        var spans = new List<Vector2Int>();
        int start = -1, last = -1;
        for (int x = 0; x < w; x++)
        {
            if (occupied[x]) { if (start < 0) start = x; last = x; }
            else if (start >= 0 && x - last >= EmptyGapToSplit)
            { spans.Add(new Vector2Int(start, last + 1)); start = -1; }
        }
        if (start >= 0) spans.Add(new Vector2Int(start, last + 1));

        if (spans.Count != expected)
        {
            spans.Clear();
            for (int i = 0; i < expected; i++)
                spans.Add(new Vector2Int(Mathf.RoundToInt(i * w / (float)expected),
                                         Mathf.RoundToInt((i + 1) * w / (float)expected)));
        }

        Directory.CreateDirectory(folder);
        for (int i = 0; i < spans.Count; i++)
        {
            int x0 = spans[i].x, x1 = spans[i].y, y0 = h, y1 = 0;
            for (int x = x0; x < x1; x++)
                for (int y = 0; y < h; y++)
                    if (pixels[y * w + x].a > 8) { y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y + 1); }
            if (y1 <= y0) throw new Exception(prefix + ": пустой кадр " + i);

            int cw = x1 - x0, ch = y1 - y0;
            var frame = new Texture2D(cw + Padding * 2, ch + Padding * 2, TextureFormat.RGBA32, false);
            var output = new Color32[frame.width * frame.height];
            for (int y = 0; y < ch; y++)
                Array.Copy(pixels, (y0 + y) * w + x0, output, (y + Padding) * frame.width + Padding, cw);
            frame.SetPixels32(output);
            frame.Apply();

            string fileName = i == 0 ? "Idle.png" : "Walk_" + (i - 1).ToString("00") + ".png";
            string path = Path.Combine(folder, fileName).Replace("\\", "/");
            File.WriteAllBytes(path, frame.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(frame);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = importer.spritePixelsPerUnit;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }
    }

    [MenuItem("NeKArton/Create door trigger at selected object")]
    public static void AddDoorToSelection()
    {
        GameObject target = Selection.activeGameObject;
        if (target == null)
        {
            EditorUtility.DisplayDialog("NeKArton", "Выбери объект двери в Hierarchy.", "OK");
            return;
        }
        var col = target.GetComponent<BoxCollider2D>();
        if (col == null) col = target.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.6f, 2.4f);
        if (target.GetComponent<DoorTransitionTrigger>() == null)
            target.AddComponent<DoorTransitionTrigger>();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("NeKArton",
            "Триггер добавлен. В Inspector укажи Destination Scene — точное имя следующей сцены из Build Settings.", "OK");
    }
}
#endif
