using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Диалог")]
    [SerializeField] private DialogueData dialogue;

    [Header("Взаимодействие")]
    [SerializeField, Min(0.5f)] private float interactionRange = 2.5f;
    [SerializeField] private GameObject hint;

    private Transform player;
    private Collider2D interactionCollider;
    private Collider2D playerCollider;
    private Button interactionButton;
    private Transform uiRoot;
    private bool playerInRange;

    private void Reset()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void Start()
    {
        interactionCollider = GetComponent<Collider2D>();
        FindPlayer();
        FindDialogueCanvas();

        if (uiRoot == null)
            Debug.LogError("DialogueTrigger: не найден DialogueCanvas. Кнопка взаимодействия не появится.", this);
        if (player == null)
            Debug.LogError("DialogueTrigger: не найден игрок. Проверь Player tag или компонент PlayerMovement.", this);
        if (dialogue == null)
            Debug.LogError("DialogueTrigger: не назначен DialogueData для Насти.", this);
    }

    private void Update()
    {
        if (player == null) FindPlayer();
        if (uiRoot == null) FindDialogueCanvas();

        if (player != null && playerCollider == null)
            playerCollider = player.GetComponentInChildren<Collider2D>();

        bool collidersTouching = interactionCollider != null && playerCollider != null &&
            interactionCollider.Distance(playerCollider).distance <= 0f;
        bool withinConfiguredRange = player != null &&
            Vector2.Distance(player.position, transform.position) <= interactionRange;
        // Prefer existing collider overlap; keep the configured distance as a fallback.
        playerInRange = collidersTouching || withinConfiguredRange;
        bool canInteract = playerInRange && !DialogueManager.IsActive;

        if (hint != null) hint.SetActive(canInteract);

        if (canInteract)
            ShowPrompt();
        else
            HidePrompt();

        if (canInteract && Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
            BeginDialogue();
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            PlayerMovement movement = FindObjectOfType<PlayerMovement>();
            if (movement != null) playerObject = movement.gameObject;
        }

        if (playerObject != null) player = playerObject.transform;
    }

    private void FindDialogueCanvas()
    {
        GameObject canvasObject = GameObject.Find("DialogueCanvas");
        Canvas canvas = canvasObject != null ? canvasObject.GetComponent<Canvas>() : null;

        if (canvas == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            foreach (Canvas candidate in canvases)
            {
                if (candidate.name == "DialogueCanvas")
                {
                    canvas = candidate;
                    break;
                }
            }
        }

        if (canvas != null)
        {
            uiRoot = canvas.transform;
            RectTransform rect = uiRoot as RectTransform;
            if (rect != null) rect.localScale = Vector3.one;
        }
    }

    private void ShowPrompt()
    {
        if (interactionButton != null || DialogueManager.IsActive ||
            DialogueManager.Instance == null || uiRoot == null)
            return;

        if (DialogueManager.Instance != null)
            interactionButton = DialogueManager.Instance.CreateInteractionButton(
                uiRoot, "Поговорить", BeginDialogue);

        // A missing dialogue-button prefab must not block interaction.
        if (interactionButton == null)
            interactionButton = CreateFallbackButton();

        if (interactionButton != null)
        {
            interactionButton.gameObject.SetActive(true);
            interactionButton.transform.SetAsLastSibling();
        }
        else
            Debug.LogError("DialogueTrigger: не удалось создать кнопку. Проверьте Canvas и GraphicRaycaster.", this);
    }

    private Button CreateFallbackButton()
    {
        if (uiRoot == null) return null;

        GameObject root = new GameObject("TalkToNastyaButton", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        root.transform.SetParent(uiRoot, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 48f);
        rect.sizeDelta = new Vector2(260f, 64f);

        Image image = root.GetComponent<Image>();
        image.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);

        Button button = root.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(BeginDialogue);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Text));
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
        return button;
    }

    private void HidePrompt()
    {
        if (interactionButton != null)
            Destroy(interactionButton.gameObject);
        interactionButton = null;
    }

    private void BeginDialogue()
    {
        if (!playerInRange || DialogueManager.IsActive ||
            DialogueManager.Instance == null || dialogue == null)
            return;

        HidePrompt();
        DialogueManager.Instance.StartDialogue(dialogue);
    }

    private void OnDisable()
    {
        HidePrompt();
    }
}
