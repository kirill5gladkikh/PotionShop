
using UnityEngine;
using System.Collections.Generic;

public class IngredientManager : MonoBehaviour
{
    [Header("Settings")]
    public int maxIngredients = 2;

    private List<string> selectedIngredients =
        new List<string>();

    private List<string> addedToCauldron =
        new List<string>();

    public string PotionName { get; private set; } = "";

    public bool TrySelectIngredient(string ingredientName)
    {
        if (selectedIngredients.Contains(ingredientName))
            return false;

        if (selectedIngredients.Count >= maxIngredients)
        {
            Debug.Log("Maximum 2 ingredients allowed!");
            return false;
        }

        selectedIngredients.Add(ingredientName);

        Debug.Log(
            "Selected: " + ingredientName +
            " (" + selectedIngredients.Count + "/2)"
        );

        return true;
    }

    public Vector3 GetIngredientPosition(
        Vector3 mortarPosition,
        string ingredientName)
    {
        int index = selectedIngredients.IndexOf(ingredientName);

        float offsetX = index == 0 ? -0.35f : 0.35f;

        return new Vector3(
            mortarPosition.x + offsetX,
            mortarPosition.y,
            mortarPosition.z
        );
    }

    public void AddIngredientToCauldron(string ingredientName)
    {
        if (!selectedIngredients.Contains(ingredientName))
            return;

        if (addedToCauldron.Contains(ingredientName))
            return;

        addedToCauldron.Add(ingredientName);

        Debug.Log(
            "Cauldron: " + addedToCauldron.Count +
            "/" + maxIngredients
        );

        if (IsReadyToStir())
        {
            IdentifyPotion();
        }
    }

    void IdentifyPotion()
    {
        if (HasIngredients("Herb", "RedBerry"))
        {
            PotionName = "Healing Potion";
        }
        else if (HasIngredients("GoldenRoot", "RedBerry"))
        {
            PotionName = "Energy Potion";
        }
        else if (HasIngredients("BlueFlower", "PurpleMushroom"))
        {
            PotionName = "Sleep Potion";
        }
        else
        {
            PotionName = "Unknown Potion";
        }

        Debug.Log("Recipe identified: " + PotionName);
    }

    bool HasIngredients(string first, string second)
    {
        return addedToCauldron.Contains(first) &&
               addedToCauldron.Contains(second);
    }

    public bool IsReadyToStir()
    {
        return addedToCauldron.Count == maxIngredients;
    }

    public bool HasTwoIngredients()
    {
        return selectedIngredients.Count == maxIngredients;
    }

    public List<string> GetSelectedIngredients()
    {
        return new List<string>(selectedIngredients);
    }

    public List<string> GetCauldronIngredients()
    {
        return new List<string>(addedToCauldron);
    }
}
