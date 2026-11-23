#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NeKArtonCharacterAnimationSetup
{
    private const string HeroSheet = "Assets/Sprite/движение_гг.png";
    private const string NastyaSheet = "Assets/Sprite/движение_насти.png";
    private const int HeroCount = 24;
    private const int NastyaCount = 22;

    [MenuItem("NeKArton/Setup character animations in Location-1")]
    public static void Setup()
    {
        if (SceneManager.GetActiveScene().name != "Location-1")
        {
            EditorUtility.DisplayDialog("NeKArton", "Сначала открой Assets/Scenes/Location-1.unity.", "OK");
            return;
        }

        if (!Slice(HeroSheet, HeroCount, "GG") || !Slice(NastyaSheet, NastyaCount, "Nastya"))
            return;

        Sprite[] hero = LoadFrames(HeroSheet, "GG_", HeroCount);
        Sprite[] girl = LoadFrames(NastyaSheet, "Nastya_", NastyaCount);
        if (hero.Length != HeroCount || girl.Length != NastyaCount)
        {
            EditorUtility.DisplayDialog("NeKArton", "Не все кадры импортированы. Проверь PNG и повтори команду.", "OK");
            return;
        }

        PlayerMovement player = UnityEngine.Object.FindObjectOfType<PlayerMovement>();
        if (player == null)
        {
            EditorUtility.DisplayDialog("NeKArton", "В сцене не найден PlayerMovement.", "OK");
            return;
        }

        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
        SerializedObject playerData = new SerializedObject(player);
        playerData.FindProperty("spriteRenderer").objectReferenceValue = playerRenderer;
        AssignFrames(playerData.FindProperty("walkFrames"), hero);
        playerData.ApplyModifiedProperties();
        if (playerRenderer != null) playerRenderer.sprite = hero[0];

        GameObject backgroundObject = GameObject.Find("Background_FirstLocation");
        SpriteRenderer background = backgroundObject != null ? backgroundObject.GetComponent<SpriteRenderer>() : null;
        if (background == null)
        {
            EditorUtility.DisplayDialog("NeKArton", "Не найден фон Background_FirstLocation.", "OK");
            return;
        }

        GameObject npc = GameObject.Find("Nastya_NPC");
        if (npc == null) npc = new GameObject("Nastya_NPC");
        SpriteRenderer renderer = npc.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = npc.AddComponent<SpriteRenderer>();
        renderer.sprite = girl[0];
        renderer.sortingLayerID = background.sortingLayerID;
        renderer.sortingOrder = background.sortingOrder + 1;

        Bounds b = background.bounds;
        float floorY = b.min.y + b.size.y * 0.145f;
        float doorX = b.min.x + b.size.x * 0.755f;
        float lampX = b.min.x + b.size.x * 0.57f;
        npc.transform.position = new Vector3(doorX, floorY, 0f);
        npc.transform.localScale = new Vector3(0.4165f, 0.4828f, 1f);

        GameObject stop = GameObject.Find("NastyaLampStopPoint");
        if (stop == null) stop = new GameObject("NastyaLampStopPoint");
        stop.transform.position = new Vector3(lampX, floorY, 0f);

        NastyaCameraCue cue = npc.GetComponent<NastyaCameraCue>();
        if (cue == null) cue = npc.AddComponent<NastyaCameraCue>();
        SerializedObject cueData = new SerializedObject(cue);
        AssignFrames(cueData.FindProperty("walkFrames"), girl);
        cueData.FindProperty("targetCamera").objectReferenceValue = Camera.main;
        cueData.FindProperty("lampStopPoint").objectReferenceValue = stop.transform;
        cueData.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = npc;
        EditorUtility.DisplayDialog("NeKArton", "Готово! Анимация назначена, Настя создана у третьей двери справа. Сохрани сцену сочетанием Ctrl+S и проверь расположение в Game View.", "OK");
    }

    private static bool Slice(string path, int count, string prefix)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            EditorUtility.DisplayDialog("NeKArton", "Не найден файл: " + path, "OK");
            return false;
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null) return false;

        float cellWidth = (float)texture.width / count;
        SpriteMetaData[] data = new SpriteMetaData[count];
        for (int i = 0; i < count; i++)
        {
            data[i] = new SpriteMetaData
            {
                name = prefix + "_" + (i + 1).ToString("00"),
                rect = new Rect(i * cellWidth, 0f, cellWidth, texture.height),
                alignment = (int)SpriteAlignment.BottomCenter,
                pivot = new Vector2(0.5f, 0f)
            };
        }
#pragma warning disable 618
        importer.spritesheet = data;
#pragma warning restore 618
        importer.SaveAndReimport();
        return true;
    }

    private static Sprite[] LoadFrames(string path, string prefix, int count)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .Where(s => s.name.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(s => s.name, StringComparer.Ordinal).Take(count).ToArray();
    }

    private static void AssignFrames(SerializedProperty array, Sprite[] frames)
    {
        array.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
    }
}
#endif
