using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] DialogueData dialogue;
    [SerializeField] GameObject hint;
    [SerializeField] Button interactionButtonPrefab;

    bool playerInRange;
    Button interactionButton;
    Transform uiRoot;

    void Reset()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    void Start()
    {
        Canvas canvas = null;
        foreach (Canvas candidate in FindObjectsOfType<Canvas>())
            if (candidate.name == "DialogueCanvas") { canvas = candidate; break; }

        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            uiRoot = canvas.transform;
            RectTransform rootRect = uiRoot as RectTransform;
            if (rootRect != null) rootRect.localScale = Vector3.one;
        }
    }

    void Update()
    {
        bool visible = playerInRange && !DialogueManager.IsActive;
        if (hint != null) hint.SetActive(visible);

        if (visible && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            BeginDialogue();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        ShowPrompt();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        HidePrompt();
    }

    void ShowPrompt()
    {
        if (interactionButton != null || DialogueManager.Instance == null || uiRoot == null) return;
        interactionButton = DialogueManager.Instance.CreateInteractionButton(
            uiRoot, "Поговорить", BeginDialogue);
    }

    void HidePrompt()
    {
        if (interactionButton != null) Destroy(interactionButton.gameObject);
        interactionButton = null;
    }

    void BeginDialogue()
    {
        if (!playerInRange || DialogueManager.IsActive || DialogueManager.Instance == null) return;
        HidePrompt();
        DialogueManager.Instance.StartDialogue(dialogue);
    }

    void OnDisable()
    {
        HidePrompt();
    }
}
