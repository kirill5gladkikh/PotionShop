
using UnityEngine;

public class NPCDialogueTest : MonoBehaviour
{
    private void Start()
    {
        NPCDialogueAI.Instance.OnDialogueGenerated += ShowDialogue;
        NPCDialogueAI.Instance.OnDialogueError += ShowError;

        NPCDialogueAI.Instance.StartConversation(
            "Oliver",
            "A shy young traveler who loves winter and magic",
            "Oliver enters a cozy potion shop during a snowstorm. " +
            "He needs a warming potion for his journey."
        );
    }

    private void ShowDialogue(DialogueResult result)
    {
        Debug.Log("=== NPC DIALOGUE ===");
        Debug.Log("NPC: " + result.npcLine);

        for (int i = 0; i < result.choices.Length; i++)
        {
            Debug.Log(
                "Choice " + (i + 1) + ": " + result.choices[i]
            );
        }
    }

    private void ShowError(string error)
    {
        Debug.LogError("Dialogue test failed: " + error);
    }

    private void OnDestroy()
    {
        if (NPCDialogueAI.Instance == null)
            return;

        NPCDialogueAI.Instance.OnDialogueGenerated -= ShowDialogue;
        NPCDialogueAI.Instance.OnDialogueError -= ShowError;
    }
}
