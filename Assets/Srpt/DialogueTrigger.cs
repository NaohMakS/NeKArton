using System.Collections;
using UnityEngine;

/// <summary>
/// Starts the assigned dialogue when the player enters a dedicated dialogue point.
/// Put this component and a trigger Collider2D on a separate, invisible scene object.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class DialogueTrigger : MonoBehaviour
{
    [Header("Диалог")]
    [SerializeField] private DialogueData dialogue;

    [Tooltip("Запускать диалог только один раз за текущую загрузку сцены.")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;
    private bool waitingToStart;

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered || waitingToStart || !IsPlayer(other))
            return;

        if (dialogue == null)
        {
            Debug.LogError("DialogueTrigger: не назначен DialogueData.", this);
            return;
        }

        hasTriggered = triggerOnce;
        waitingToStart = true;
        StartCoroutine(StartWhenDialogueManagerIsReady());
    }

    private static bool IsPlayer(Collider2D other)
    {
        if (other == null)
            return false;

        if (other.CompareTag("Player"))
            return true;

        return other.GetComponentInParent<PlayerMovement>() != null;
    }

    private IEnumerator StartWhenDialogueManagerIsReady()
    {
        // If the opening monologue is still playing, wait until it ends.
        while (DialogueManager.Instance == null || DialogueManager.IsActive)
            yield return null;

        DialogueManager.Instance.StartDialogue(dialogue);
        waitingToStart = false;
    }
}
