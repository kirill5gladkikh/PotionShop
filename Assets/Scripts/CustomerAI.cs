
using UnityEngine;

public class CustomerAI : MonoBehaviour
{
    public enum CustomerMood
    {
        Neutral,
        Happy,
        Angry,
        Sad
    }

    [Header("Character")]
    public string customerName = "Customer";

    [TextArea(2, 4)]
    public string personality;

    [TextArea(2, 4)]
    public string backstory;

    public string speakingStyle;
    public string requestedPotion;

    [Header("Relationship")]
    [Range(0, 100)]
    public int relationship = 50;

    public int tolerance = 2;
    public CustomerMood mood = CustomerMood.Neutral;

    private int mistakes;

    private static int savedCustomerIndex = -1;
    private static int savedRelationship = 50;
    private static int savedMistakes = 0;
    private static CustomerMood savedMood =
        CustomerMood.Neutral;

    void Start()
    {
        LoadCustomerState();
    }

    public void SetupCharacter(int index)
    {
        switch (index)
        {
            case 0:
                customerName = "Elian";
                personality =
                    "Kind, shy, caring, easily worried.";

                backstory =
                    "A young traveler seeking medicine " +
                    "for his sick younger brother.";

                speakingStyle =
                    "Soft-spoken, polite, sometimes nervous.";

                requestedPotion = "Healing Potion";
                break;

            case 1:
                customerName = "Mira";
                personality =
                    "Energetic, confident, impatient, adventurous.";

                backstory =
                    "An explorer preparing for a dangerous " +
                    "journey through the frozen mountains.";

                speakingStyle =
                    "Fast, enthusiastic, occasionally sarcastic.";

                requestedPotion = "Energy Potion";
                break;

            case 2:
                customerName = "Rowan";
                personality =
                    "Quiet, mysterious, thoughtful, melancholic.";

                backstory =
                    "A scholar troubled by strange dreams " +
                    "who has not slept peacefully for weeks.";

                speakingStyle =
                    "Calm, poetic, speaks in short sentences.";

                requestedPotion = "Sleep Potion";
                break;
        }

        LoadCustomerState();
    }

    public void LoadCustomerState()
    {
        int index = SceneNavigation.CurrentCustomer;

        if (savedCustomerIndex != index)
        {
            savedCustomerIndex = index;
            savedRelationship = 50;
            savedMistakes = 0;
            savedMood = CustomerMood.Neutral;
        }

        relationship = savedRelationship;
        mistakes = savedMistakes;
        mood = savedMood;
    }

    public void ReceiveCorrectPotion()
    {
        mood = CustomerMood.Happy;
        relationship = Mathf.Clamp(
            relationship + 10, 0, 100
        );

        SaveCustomerState();
    }

    public void ReceiveWrongPotion()
    {
        mistakes++;
        mood = CustomerMood.Angry;

        relationship = Mathf.Clamp(
            relationship - 10, 0, 100
        );

        SaveCustomerState();
    }

    public void ReceiveFriendlyResponse()
    {
        relationship = Mathf.Clamp(
            relationship + 5, 0, 100
        );

        mood = CustomerMood.Happy;
        SaveCustomerState();
    }

    public void ReceiveRudeResponse()
    {
        relationship = Mathf.Clamp(
            relationship - 5, 0, 100
        );

        mood = CustomerMood.Sad;
        SaveCustomerState();
    }

    public bool WillLeave()
    {
        return mistakes > tolerance;
    }

    private void SaveCustomerState()
    {
        savedCustomerIndex =
            SceneNavigation.CurrentCustomer;

        savedRelationship = relationship;
        savedMistakes = mistakes;
        savedMood = mood;
    }
}
