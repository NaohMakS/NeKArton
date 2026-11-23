#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
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
            EditorUtility.DisplayDialog("NeKArton",
                "Сначала открой Assets/Scenes/Location-1.unity.", "OK");
            return;
        }

        if (!SliceWithUnity6Api(HeroSheet, HeroCount, "GG") ||
            !SliceWithUnity6Api(NastyaSheet, NastyaCount, "Nastya"))
            return;

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Sprite[] hero = LoadFrames(HeroSheet, "GG_");
        Sprite[] girl = LoadFrames(NastyaSheet, "Nastya_");

        if (hero.Length != HeroCount || girl.Length != NastyaCount)
        {
            string details = "Главный герой: " + hero.Length + "/" + HeroCount +
                " кадров; Настя: " + girl.Length + "/" + NastyaCount + " кадров.\n" +
                "Проверь Console: там выведены имена найденных спрайтов.";
            Debug.LogError("[NeKArton] " + details);
            EditorUtility.DisplayDialog("NeKArton — ошибка импорта", details, "OK");
            return;
        }

        PlayerMovement player = UnityEngine.Object.FindObjectOfType<PlayerMovement>();
        if (player == null)
        {
            EditorUtility.DisplayDialog("NeKArton", "В сцене не найден PlayerMovement.", "OK");
            return;
        }

        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
        if (playerRenderer == null)
        {
            EditorUtility.DisplayDialog("NeKArton", "У объекта игрока нет SpriteRenderer.", "OK");
            return;
        }

        SerializedObject playerData = new SerializedObject(player);
        playerData.FindProperty("spriteRenderer").objectReferenceValue = playerRenderer;
        AssignFrames(playerData.FindProperty("walkFrames"), hero);
        playerData.ApplyModifiedProperties();
        playerRenderer.sprite = hero[0];

        GameObject backgroundObject = GameObject.Find("Background_FirstLocation");
        SpriteRenderer background = backgroundObject != null
            ? backgroundObject.GetComponent<SpriteRenderer>() : null;
        if (background == null)
        {
            EditorUtility.DisplayDialog("NeKArton", "Не найден фон Background_FirstLocation.", "OK");
            return;
        }

        GameObject npc = GameObject.Find("Nastya_NPC");
        if (npc == null) npc = new GameObject("Nastya_NPC");

        SpriteRenderer npcRenderer = npc.GetComponent<SpriteRenderer>();
        if (npcRenderer == null) npcRenderer = npc.AddComponent<SpriteRenderer>();
        npcRenderer.sprite = girl[0];
        npcRenderer.sortingLayerID = background.sortingLayerID;
        npcRenderer.sortingOrder = background.sortingOrder + 1;

        Bounds bounds = background.bounds;
        float floorY = bounds.min.y + bounds.size.y * 0.145f;
        float doorX = bounds.min.x + bounds.size.x * 0.755f;
        float lampX = bounds.min.x + bounds.size.x * 0.57f;
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
        EditorUtility.DisplayDialog("NeKArton",
            "Готово! Назначены 24 кадра героя и 22 кадра Насти. Настя создана у третьей двери справа. Сохрани сцену (Ctrl+S) и проверь Game View.",
            "OK");
    }

    private static bool SliceWithUnity6Api(string path, int frameCount, string prefix)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[NeKArton] Не найден файл: " + path);
            EditorUtility.DisplayDialog("NeKArton", "Не найден файл: " + path, "OK");
            return false;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            EditorUtility.DisplayDialog("NeKArton", "Не удалось прочитать PNG: " + path, "OK");
            return false;
        }

        // Both supplied sheets are horizontal strips. Divide the full width into
        // equal frame cells; each cell retains the full transparent canvas height.
        float cellWidth = (float)texture.width / frameCount;
        SpriteRect[] rects = new SpriteRect[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            rects[i] = new SpriteRect
            {
                name = prefix + "_" + (i + 1).ToString("00"),
                spriteID = GUID.Generate(),
                rect = new Rect(i * cellWidth, 0f, cellWidth, texture.height),
                alignment = SpriteAlignment.BottomCenter,
                pivot = new Vector2(0.5f, 0f)
            };
        }

        // Unity 6 uses the Sprite Editor data-provider API. The legacy
        // TextureImporter.spritesheet setter may not persist slice changes.
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider =
            factories.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null)
        {
            EditorUtility.DisplayDialog("NeKArton",
                "Unity не предоставила Sprite Editor data provider для " + path +
                ". Проверь, установлен ли пакет 2D Sprite.", "OK");
            return false;
        }

        provider.InitSpriteEditorDataProvider();
        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();
        Debug.Log("[NeKArton] Нарезан " + path + ": " + texture.width + "x" +
                  texture.height + ", " + frameCount + " кадров.");
        return true;
    }

    private static Sprite[] LoadFrames(string path, string prefix)
    {
        Sprite[] all = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>().ToArray();

        Debug.Log("[NeKArton] Спрайты в " + path + ": " +
                  string.Join(", ", all.Select(s => s.name)));

        return all.Where(s => s.name.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
    }

    private static void AssignFrames(SerializedProperty array, Sprite[] frames)
    {
        array.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
    }
}
#endif
