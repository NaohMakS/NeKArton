using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public sealed class DialogueInteractionTrigger : MonoBehaviour
{
    [SerializeField] private DialogueData dialogue;
    [SerializeField, Min(0.5f)] private float interactionRange = 3f;
    [SerializeField] private GameObject hint;

    private Collider2D triggerCollider;
    private Transform player;
    private Button promptButton;
    private Transform uiRoot;
    private bool inRange;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
        FindPlayer();
        FindCanvas();
    }

    private void Update()
    {
        if (player == null) FindPlayer();
        if (uiRoot == null) FindCanvas();

        inRange = IsPlayerInRange();
        bool available = inRange && !DialogueManager.IsActive;
        if (hint != null && hint.activeSelf != available) hint.SetActive(available);

        if (available) EnsurePrompt();
        else RemovePrompt();

        if (available && Keyboard.current != null &&
            (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            StartConversation();
    }

    private bool IsPlayerInRange()
    {
        if (player == null) return false;
        Collider2D playerCollider = player.GetComponentInChildren<Collider2D>();
        if (triggerCollider != null && playerCollider != null)
        {
            ColliderDistance2D distance = triggerCollider.Distance(playerCollider);
            if (distance.isValid && distance.distance <= 0f) return true;
        }

        Vector2 closest = triggerCollider != null
            ? triggerCollider.ClosestPoint(player.position)
            : (Vector2)transform.position;
        return Vector2.Distance(closest, player.position) <= interactionRange;
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
        Canvas canvas = null;
        GameObject named = GameObject.Find("DialogueCanvas");
        if (named != null) canvas = named.GetComponent<Canvas>();
        if (canvas == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            foreach (Canvas candidate in canvases)
                if (candidate.name == "DialogueCanvas") { canvas = candidate; break; }
        }
        if (canvas != null) uiRoot = canvas.transform;
    }

    private void EnsurePrompt()
    {
        if (promptButton != null || uiRoot == null) return;

        if (DialogueManager.Instance != null)
            promptButton = DialogueManager.Instance.CreateInteractionButton(uiRoot, "Поговорить", StartConversation);

        if (promptButton == null)
        {
            GameObject go = new GameObject("TalkToNastyaButton",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(uiRoot, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 44f);
            rect.sizeDelta = new Vector2(260f, 64f);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.12f, 0.16f, 0.96f);
            promptButton = go.GetComponent<Button>();
            promptButton.targetGraphic = image;

            GameObject labelGo = new GameObject("Label",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);
            TMP_Text label = labelGo.GetComponent<TMP_Text>();
            label.text = "Поговорить";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 24f;
            label.raycastTarget = false;
        }

        promptButton.onClick.RemoveListener(StartConversation);
        promptButton.onClick.AddListener(StartConversation);
        promptButton.gameObject.SetActive(true);
        promptButton.transform.SetAsLastSibling();
    }

    private void RemovePrompt()
    {
        if (promptButton != null) Destroy(promptButton.gameObject);
        promptButton = null;
    }

    private void StartConversation()
    {
        if (!inRange || DialogueManager.IsActive || dialogue == null) return;
        RemovePrompt();
        if (DialogueManager.Instance != null) DialogueManager.Instance.StartDialogue(dialogue);
        else Debug.LogError("DialogueInteractionTrigger: DialogueManager is missing in this scene.", this);
    }

    private void OnDisable()
    {
        RemovePrompt();
        if (hint != null) hint.SetActive(false);
    }
}
