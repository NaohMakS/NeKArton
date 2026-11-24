using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public static bool IsActive { get; private set; }

    [Header("UI")]
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text speakerText;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] Transform choicesContainer;
    [SerializeField] Button choiceButtonPrefab;

    [Header("Начальная последовательность")]
    [SerializeField] DialogueData openingDialogue;
    [SerializeField] DialogueData followupDialogue;

    [Header("Настройки")]
    [SerializeField, Min(1f)] float charsPerSecond = 40f;

    DialogueData current;
    int index;
    bool lineFinished;
    bool hasChoices;
    bool sequenceRunning;
    int startFrame;
    Coroutine typing;
    Button skipButton;
    Canvas dialogueCanvas;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        IsActive = false;

        if (panel != null) panel.SetActive(false);
        dialogueCanvas = panel != null ? panel.GetComponentInParent<Canvas>() : null;
        if (dialogueCanvas != null)
        {
            RectTransform canvasRect = dialogueCanvas.transform as RectTransform;
            if (canvasRect != null) canvasRect.localScale = Vector3.one;
        }
    }

    void Start()
    {
        if (openingDialogue != null)
            StartCoroutine(BeginOpeningSequence());
    }

    IEnumerator BeginOpeningSequence()
    {
        yield return null;
        StartOpeningSequence();
    }

    public void StartOpeningSequence()
    {
        if (openingDialogue == null)
        {
            Debug.LogWarning("DialogueManager: openingDialogue не назначен.");
            return;
        }

        sequenceRunning = true;
        StartDialogueInternal(openingDialogue);
    }

    public void StartDialogue(DialogueData data)
    {
        if (IsActive || data == null) return;
        sequenceRunning = false;
        StartDialogueInternal(data);
    }

    void StartDialogueInternal(DialogueData data)
    {
        if (data == null || data.nodes == null || data.nodes.Length == 0)
        {
            Debug.LogWarning("DialogueManager: диалог пуст или не назначен.", this);
            FinishSequence(false);
            return;
        }

        current = data;
        IsActive = true;
        startFrame = Time.frameCount;
        if (panel != null) panel.SetActive(true);
        CreateSkipButton();
        ShowNode(0);
    }

    void Update()
    {
        if (!IsActive || Time.frameCount == startFrame) return;

        bool keyboardPressed = Keyboard.current != null &&
            (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (!keyboardPressed && !mousePressed) return;

        if (!lineFinished) FinishTyping();
        else if (!hasChoices) Next();
    }

    void ShowNode(int i)
    {
        if (current == null || i < 0 || i >= current.nodes.Length)
        {
            FinishCurrentDialogue();
            return;
        }

        index = i;
        DialogueNode node = current.nodes[i];
        ClearChoices();
        if (speakerText != null) speakerText.text = node.speaker;
        hasChoices = node.choices != null && node.choices.Length > 0;

        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(TypeLine(node.text));
    }

    IEnumerator TypeLine(string text)
    {
        lineFinished = false;
        if (bodyText == null)
        {
            lineFinished = true;
            yield break;
        }

        bodyText.text = text;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();

        int total = bodyText.textInfo.characterCount;
        float shown = 0f;
        while (shown < total)
        {
            shown += charsPerSecond * Time.unscaledDeltaTime;
            bodyText.maxVisibleCharacters = Mathf.FloorToInt(shown);
            yield return null;
        }
        FinishTyping();
    }

    void FinishTyping()
    {
        if (typing != null) { StopCoroutine(typing); typing = null; }
        if (bodyText != null) bodyText.maxVisibleCharacters = int.MaxValue;
        lineFinished = true;
        if (hasChoices) ShowChoices(current.nodes[index].choices);
    }

    void Next()
    {
        DialogueNode node = current.nodes[index];
        if (node.endsHere) { FinishCurrentDialogue(); return; }
        ShowNode(node.next >= 0 ? node.next : index + 1);
    }

    void FinishCurrentDialogue()
    {
        ClearChoices();
        DestroySkipButton();

        if (sequenceRunning && current == openingDialogue && followupDialogue != null)
        {
            StartDialogueInternal(followupDialogue);
            return;
        }

        FinishSequence(false);
    }

    void FinishSequence(bool skipped)
    {
        if (typing != null) { StopCoroutine(typing); typing = null; }
        ClearChoices();
        DestroySkipButton();
        if (panel != null) panel.SetActive(false);
        current = null;
        IsActive = false;
        sequenceRunning = false;
    }

    public void SkipSequence()
    {
        if (!IsActive) return;
        FinishSequence(true);
    }

    void CreateSkipButton()
    {
        DestroySkipButton();
        if (choiceButtonPrefab == null || panel == null) return;

        skipButton = Instantiate(choiceButtonPrefab, panel.transform);
        skipButton.name = "SkipDialogueButton";
        TMP_Text label = skipButton.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = "Пропустить";

        RectTransform rect = skipButton.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -18f);
            rect.sizeDelta = new Vector2(190f, 54f);
        }
        skipButton.onClick.AddListener(SkipSequence);
    }

    void DestroySkipButton()
    {
        if (skipButton != null) Destroy(skipButton.gameObject);
        skipButton = null;
    }

    public Button CreateInteractionButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        if (choiceButtonPrefab == null || parent == null) return null;
        Button button = Instantiate(choiceButtonPrefab, parent);
        button.name = "TalkToNastyaButton";
        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null) text.text = label;

        RectTransform rect = button.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 42f);
            rect.sizeDelta = new Vector2(260f, 64f);
        }
        button.onClick.AddListener(action);
        return button;
    }

    void ShowChoices(DialogueChoice[] choices)
    {
        if (choiceButtonPrefab == null || choicesContainer == null) return;
        foreach (DialogueChoice choice in choices)
        {
            Button button = Instantiate(choiceButtonPrefab, choicesContainer);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = choice.text;
            int target = choice.next;
            button.onClick.AddListener(() =>
            {
                if (target < 0) FinishCurrentDialogue();
                else ShowNode(target);
            });
        }

        if (EventSystem.current != null && choicesContainer.childCount > 0)
            EventSystem.current.SetSelectedGameObject(choicesContainer.GetChild(0).gameObject);
    }

    void ClearChoices()
    {
        if (choicesContainer == null) return;
        foreach (Transform child in choicesContainer) Destroy(child.gameObject);
    }
}
