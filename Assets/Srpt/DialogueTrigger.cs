using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public sealed class DialogueTrigger : MonoBehaviour
{
    [Header("Диалог")]
    [SerializeField] private DialogueData dialogue;

    [Header("Взаимодействие")]
    [SerializeField, Min(0.5f)] private float interactionRange = 3f;
    [SerializeField] private GameObject hint;

    private Transform player;
    private Transform uiRoot;
    private Button interactionButton;
    private bool playerInRange;
    private bool missingReferencesReported;

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null) trigger.isTrigger = true;
        FindPlayer();
        FindCanvas();
    }

    private void Update()
    {
        if (player == null) FindPlayer();
        if (uiRoot == null) FindCanvas();

        playerInRange = player != null &&
            Vector2.Distance(player.position, transform.position) <= interactionRange;

        bool canInteract = playerInRange && !DialogueManager.IsActive;
        if (hint != null && hint.activeSelf != canInteract)
            hint.SetActive(canInteract);

        if (canInteract)
            ShowPrompt();
        else
            HidePrompt();

        if (canInteract && Keyboard.current != null &&
            (Keyboard.current.eKey.wasPressedThisFrame ||
             Keyboard.current.enterKey.wasPressedThisFrame))
            BeginDialogue();
    }

    private void FindPlayer()
    {
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found == null)
        {
            PlayerMovement movement = FindObjectOfType<PlayerMovement>();
            if (movement != null) found = movement.gameObject;
        }

        if (found != null) player = found.transform;
    }

    private void FindCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        Canvas selected = null;

        // Prefer the main screen-space Canvas; DialogueCanvas may only be a child panel.
        foreach (Canvas candidate in canvases)
        {
            if (candidate == null) continue;
            if (candidate.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                selected = candidate;
                break;
            }
            if (selected == null) selected = candidate;
        }

        if (selected != null)
        {
            uiRoot = selected.transform;
            return;
        }

        if (!missingReferencesReported)
        {
            Debug.LogError("DialogueTrigger: в сцене не найден Canvas. Кнопка взаимодействия не может быть создана.", this);
            missingReferencesReported = true;
        }
    }

    private void ShowPrompt()
    {
        if (interactionButton != null || uiRoot == null) return;

        GameObject root = new GameObject("TalkToNastyaButton",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        root.transform.SetParent(uiRoot, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 42f);
        rect.sizeDelta = new Vector2(260f, 62f);

        Image image = root.GetComponent<Image>();
        image.color = new Color(0.12f, 0.12f, 0.16f, 0.96f);

        interactionButton = root.GetComponent<Button>();
        interactionButton.targetGraphic = image;
        interactionButton.onClick.AddListener(BeginDialogue);

        GameObject labelObject = new GameObject("Label",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObject.transform.SetParent(root.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);

        Text label = labelObject.GetComponent<Text>();
        label.text = "Поговорить";
        label.alignment = TextAnchor.MiddleCenter;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = 24;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private void HidePrompt()
    {
        if (interactionButton != null) Destroy(interactionButton.gameObject);
        interactionButton = null;
    }

    private void BeginDialogue()
    {
        if (!playerInRange || DialogueManager.IsActive) return;
        if (dialogue == null)
        {
            Debug.LogError("DialogueTrigger: не назначен DialogueData.", this);
            return;
        }

        HidePrompt();
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(dialogue);
        else
            Debug.LogError("DialogueTrigger: в сцене не найден DialogueManager.", this);
    }

    private void OnDisable()
    {
        HidePrompt();
        if (hint != null) hint.SetActive(false);
    }
}
