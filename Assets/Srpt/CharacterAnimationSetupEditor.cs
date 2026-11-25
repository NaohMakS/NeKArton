#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NeKArtonNastyaRestoreEditor
{
    private const string FramesFolder = "Assets/Sprite/ходьба_Н";
    private const string DialoguePath = "Assets/Dialogues/Nastya_NPC_Dialogue.asset";

    [MenuItem("NeKArton/Restore Nastya from ходьба_Н")]
    public static void RestoreNastya()
    {
        if (SceneManager.GetActiveScene().name != "Location-1")
        {
            EditorUtility.DisplayDialog("NeKArton",
                "Открой Assets/Scenes/Location-1.unity перед восстановлением Насти.", "OK");
            return;
        }

        Sprite[] frames = LoadFrames();
        if (frames.Length != 12)
        {
            EditorUtility.DisplayDialog("NeKArton",
                "Ожидалось 12 кадров в папке " + FramesFolder +
                ", найдено: " + frames.Length + ". Проверь импорт PNG в Unity.", "OK");
            return;
        }

        SpriteRenderer background = FindBackground();
        if (background == null)
        {
            EditorUtility.DisplayDialog("NeKArton",
                "Не найден SpriteRenderer объекта Background_FirstLocation.", "OK");
            return;
        }

        DialogueData dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(DialoguePath);
        if (dialogue == null)
        {
            EditorUtility.DisplayDialog("NeKArton",
                "Не найден файл диалога: " + DialoguePath, "OK");
            return;
        }

        GameObject npc = GameObject.Find("Nastya_NPC");
        bool created = npc == null;
        if (created)
        {
            npc = new GameObject("Nastya_NPC");
            Undo.RegisterCreatedObjectUndo(npc, "Restore Nastya");
        }

        npc.SetActive(true);
        if (created)
        {
            Bounds bounds = background.bounds;
            float floorY = bounds.min.y + bounds.size.y * 0.145f;
            float startX = bounds.min.x + bounds.size.x * 0.755f;
            npc.transform.position = new Vector3(startX, floorY, 0f);
        }

        npc.transform.localScale = new Vector3(0.4165f, 0.4828f, 1f);

        SpriteRenderer renderer = npc.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = Undo.AddComponent<SpriteRenderer>(npc);
        renderer.sprite = frames[0];
        renderer.color = Color.white;
        renderer.sortingLayerID = background.sortingLayerID;
        renderer.sortingOrder = Mathf.Max(background.sortingOrder + 1, 51);

        CircleCollider2D circleTrigger = npc.GetComponent<CircleCollider2D>();
        if (circleTrigger != null)
        {
            circleTrigger.isTrigger = true;
            circleTrigger.radius = 1.4f;
            circleTrigger.offset = new Vector2(0f, 0.4f);
        }
        else
        {
            BoxCollider2D triggerCollider = npc.GetComponent<BoxCollider2D>();
            if (triggerCollider == null) triggerCollider = Undo.AddComponent<BoxCollider2D>(npc);
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector2(1.8f, 3.2f);
            triggerCollider.offset = new Vector2(0f, 1.6f);
        }

        Transform stop = GameObject.Find("NastyaLampStopPoint")?.transform;
        if (stop == null)
        {
            GameObject stopObject = new GameObject("NastyaLampStopPoint");
            Undo.RegisterCreatedObjectUndo(stopObject, "Create Nastya stop point");
            stop = stopObject.transform;
        }

        Bounds backgroundBounds = background.bounds;
        float stopX = backgroundBounds.min.x + backgroundBounds.size.x * 0.57f;
        float stopY = backgroundBounds.min.y + backgroundBounds.size.y * 0.145f;
        stop.position = new Vector3(stopX, stopY, 0f);

        NastyaCameraCue cue = npc.GetComponent<NastyaCameraCue>();
        if (cue == null) cue = Undo.AddComponent<NastyaCameraCue>(npc);
        SerializedObject cueData = new SerializedObject(cue);
        cueData.FindProperty("idleSprite").objectReferenceValue = frames[0];
        SerializedProperty frameArray = cueData.FindProperty("walkFrames");
        frameArray.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            frameArray.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        cueData.FindProperty("framesPerSecond").floatValue = 9f;
        cueData.FindProperty("targetCamera").objectReferenceValue = Camera.main;
        cueData.FindProperty("lampStopPoint").objectReferenceValue = stop;
        cueData.FindProperty("moveSpeed").floatValue = 1.5f;
        cueData.FindProperty("stopDistance").floatValue = 0.08f;
        cueData.ApplyModifiedProperties();

        DialogueTrigger dialogueTrigger = npc.GetComponent<DialogueTrigger>();
        if (dialogueTrigger == null) dialogueTrigger = Undo.AddComponent<DialogueTrigger>(npc);
        SerializedObject triggerData = new SerializedObject(dialogueTrigger);
        triggerData.FindProperty("dialogue").objectReferenceValue = dialogue;
        triggerData.FindProperty("interactionRange").floatValue = 2.5f;
        triggerData.FindProperty("hint").objectReferenceValue = null;
        triggerData.ApplyModifiedProperties();

        EditorUtility.SetDirty(npc);
        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(cue);
        EditorUtility.SetDirty(dialogueTrigger);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = npc;

        EditorUtility.DisplayDialog("NeKArton",
            "Настя восстановлена: 12 кадров, движение к лампе и диалог назначены. " +
            "Сохрани сцену (Ctrl+S) и проверь Play Mode.", "OK");
    }

    private static Sprite[] LoadFrames()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        return Enumerable.Range(1, 12)
            .Select(index => LoadLargestSprite(
                FramesFolder + "/Nastya_Walk_" + index.ToString("00") + ".png"))
            .Where(sprite => sprite != null)
            .ToArray();
    }

    private static Sprite LoadLargestSprite(string path)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        return assets.OfType<Sprite>()
            .OrderByDescending(sprite => sprite.rect.width * sprite.rect.height)
            .FirstOrDefault();
    }

    private static SpriteRenderer FindBackground()
    {
        GameObject background = GameObject.Find("Background_FirstLocation");
        return background != null ? background.GetComponent<SpriteRenderer>() : null;
    }
}
#endif
