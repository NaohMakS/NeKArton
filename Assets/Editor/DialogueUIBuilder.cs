#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Положите файл в Assets/Editor. Меню: NeKArton -> ...
public static class DialogueUIBuilder
{
    const string PrefabFolder = "Assets/Prefabs/UI";
    const string PrefabPath = PrefabFolder + "/ChoiceButton.prefab";
    const string DialogueFolder = "Assets/Dialogues";

    // ------------------------------------------------------------------
    // 1. Создаёт весь интерфейс диалога и настраивает DialogueManager
    // ------------------------------------------------------------------
    [MenuItem("NeKArton/1. Create Dialogue UI")]
    static void CreateDialogueUI()
    {
        // --- Canvas ---
        var canvasGO = new GameObject("DialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Dialogue UI");

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // --- Панель внизу экрана (bottom-stretch, высота 300) ---
        var panel = NewUI("DialoguePanel", canvasGO.transform);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.75f);
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 0f);
        panelRT.anchorMax = new Vector2(1f, 0f);
        panelRT.pivot = new Vector2(0.5f, 0f);
        panelRT.offsetMin = new Vector2(100f, 20f);    // left, bottom
        panelRT.offsetMax = new Vector2(-100f, 320f);  // -right, top (высота 300)

        // --- Имя говорящего (top-stretch) ---
        var speaker = NewTMP("SpeakerText", panel.transform, 38, FontStyles.Bold, new Color(1f, 0.85f, 0.4f));
        var speakerRT = speaker.rectTransform;
        speakerRT.anchorMin = new Vector2(0f, 1f);
        speakerRT.anchorMax = new Vector2(1f, 1f);
        speakerRT.pivot = new Vector2(0.5f, 1f);
        speakerRT.offsetMin = new Vector2(30f, -65f);
        speakerRT.offsetMax = new Vector2(-30f, -15f);
        speaker.alignment = TextAlignmentOptions.TopLeft;

        // --- Текст реплики (stretch, занимает остальную панель) ---
        var body = NewTMP("BodyText", panel.transform, 34, FontStyles.Normal, Color.white);
        var bodyRT = body.rectTransform;
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = new Vector2(30f, 25f);
        bodyRT.offsetMax = new Vector2(-30f, -75f);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.textWrappingMode = TextWrappingModes.Normal;

        // --- Контейнер вариантов ответа (над панелью, растёт вверх) ---
        var choices = NewUI("ChoicesContainer", canvasGO.transform);
        var choicesRT = choices.GetComponent<RectTransform>();
        choicesRT.anchorMin = new Vector2(0.5f, 0f);
        choicesRT.anchorMax = new Vector2(0.5f, 0f);
        choicesRT.pivot = new Vector2(0.5f, 0f);
        choicesRT.anchoredPosition = new Vector2(0f, 340f);
        choicesRT.sizeDelta = new Vector2(900f, 0f);

        var vlg = choices.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.childAlignment = TextAnchor.LowerCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var fitter = choices.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // --- Префаб кнопки ---
        Button buttonPrefab = GetOrCreateButtonPrefab();

        // --- EventSystem (если нет) ---
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        // --- DialogueManager + заполнение полей ---
        var manager = Object.FindFirstObjectByType<DialogueManager>();
        if (manager == null)
        {
            var mgrGO = new GameObject("DialogueManager");
            Undo.RegisterCreatedObjectUndo(mgrGO, "Create DialogueManager");
            manager = mgrGO.AddComponent<DialogueManager>();
        }

        var so = new SerializedObject(manager);
        so.FindProperty("panel").objectReferenceValue = panel;
        so.FindProperty("speakerText").objectReferenceValue = speaker;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("choicesContainer").objectReferenceValue = choices.transform;
        so.FindProperty("choiceButtonPrefab").objectReferenceValue = buttonPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = panel;
        Debug.Log("[NeKArton] Интерфейс диалога создан. Старый Canvas с тестовыми Text можно удалить.");
    }

    // ------------------------------------------------------------------
    // 2. Делает выбранный объект NPC: триггер + DialogueTrigger + пример диалога
    // ------------------------------------------------------------------
    [MenuItem("NeKArton/2. Make Selected an NPC With Dialogue")]
    static void MakeNpc()
    {
        GameObject npc = Selection.activeGameObject;
        if (npc == null) { Debug.LogWarning("[NeKArton] Выберите NPC в Hierarchy."); return; }

        var col = npc.GetComponent<Collider2D>();
        if (col == null)
        {
            var circle = Undo.AddComponent<CircleCollider2D>(npc);
            circle.radius = 2f;
            col = circle;
        }
        Undo.RecordObject(col, "Set trigger");
        col.isTrigger = true;

        var trigger = npc.GetComponent<DialogueTrigger>();
        if (trigger == null) trigger = Undo.AddComponent<DialogueTrigger>(npc);

        EnsureFolder(DialogueFolder);
        string path = AssetDatabase.GenerateUniqueAssetPath(DialogueFolder + "/" + npc.name + "_Dialogue.asset");
        var data = ScriptableObject.CreateInstance<DialogueData>();
        data.nodes = new[]
        {
            new DialogueNode
            {
                speaker = npc.name,
                text = "Привет! Ты мне поможешь?",
                choices = new[]
                {
                    new DialogueChoice { text = "Конечно!", next = 1 },
                    new DialogueChoice { text = "Не сейчас.", next = 2 }
                }
            },
            new DialogueNode { speaker = npc.name, text = "Спасибо! Знала, что на тебя можно положиться.", endsHere = true },
            new DialogueNode { speaker = npc.name, text = "Ладно, заходи потом.", endsHere = true }
        };
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();

        var so = new SerializedObject(trigger);
        so.FindProperty("dialogue").objectReferenceValue = data;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorGUIUtility.PingObject(data);
        Debug.Log("[NeKArton] NPC настроен. Диалог создан: " + path);
    }

    // ------------------------------------------------------------------
    // 3. Ставит выбранному объекту тег Player (+ Rigidbody2D для триггеров)
    // ------------------------------------------------------------------
    [MenuItem("NeKArton/3. Mark Selected As Player")]
    static void MarkPlayer()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null) { Debug.LogWarning("[NeKArton] Выберите игрока в Hierarchy."); return; }

        Undo.RecordObject(go, "Set Player tag");
        go.tag = "Player";

        if (go.GetComponent<Rigidbody2D>() == null)
        {
            var rb = Undo.AddComponent<Rigidbody2D>(go);
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            Debug.Log("[NeKArton] Добавлен Rigidbody2D (без гравитации). Он нужен, чтобы срабатывали триггеры.");
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    // ------------------------------------------------------------------
    // Вспомогательные методы
    // ------------------------------------------------------------------
    static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static TextMeshProUGUI NewTMP(string name, Transform parent, float size, FontStyles style, Color color)
    {
        var go = NewUI(name, parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.text = name;
        return t;
    }

    static Button GetOrCreateButtonPrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Button>(PrefabPath);
        if (existing != null) return existing;

        EnsureFolder(PrefabFolder);

        var go = new GameObject("ChoiceButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        var img = go.GetComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.22f, 0.95f);
        go.GetComponent<Button>().targetGraphic = img;

        var le = go.GetComponent<LayoutElement>();
        le.minHeight = 70f;
        le.preferredHeight = 70f;

        var label = NewTMP("Text (TMP)", go.transform, 30, FontStyles.Normal, Color.white);
        label.alignment = TextAlignmentOptions.Center;
        var rt = label.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(15f, 5f);
        rt.offsetMax = new Vector2(-15f, -5f);

        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        return AssetDatabase.LoadAssetAtPath<Button>(PrefabPath);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
#endif
