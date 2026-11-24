using UnityEngine;

// Повесьте на NPC. Collider2D должен быть Is Trigger, у игрока тег "Player".
[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] DialogueData dialogue;
    [SerializeField] GameObject hint;   // необязательно: значок «Нажмите E»

    bool playerInRange;

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void Update()
    {
        if (hint != null) hint.SetActive(playerInRange && !DialogueManager.IsActive);

        if (playerInRange && !DialogueManager.IsActive && Input.GetKeyDown(KeyCode.E))
            DialogueManager.Instance.StartDialogue(dialogue);
    }

    void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag("Player")) playerInRange = true; }
    void OnTriggerExit2D(Collider2D other)  { if (other.CompareTag("Player")) playerInRange = false; }
}
