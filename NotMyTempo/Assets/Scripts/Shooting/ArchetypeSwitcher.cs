using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PROTOTYPE / DEBUG ONLY. Press 1, 2, 3... to swap which archetype the gun uses at runtime.
/// Remove once guns get their archetype from drops or loadouts or whateverrrrr
/// </summary>
public class ArchetypeSwitcher : MonoBehaviour
{
    [Tooltip("Every archetype on this gun. Number key 1 picks the first, 2 the second, and so on (up to 9)")]
    [SerializeField] private FireArchetype[] archetypes;
    [Tooltip("Which archetype is active when the game starts (0 = first in the list)")]
    [SerializeField] private int startingIndex;

    private const int MaxNumberKeys = 9;

    private void Start()
    {
        ActivateArchetype(startingIndex);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        int keyCount = Mathf.Min(archetypes.Length, MaxNumberKeys);
        for (int i = 0; i < keyCount; i++)
        {
            Key numberKey = Key.Digit1 + i;
            if (keyboard[numberKey].wasPressedThisFrame)
            {
                ActivateArchetype(i);
            }
        }
    }

    private void ActivateArchetype(int index)
    {
        bool isValidIndex = index >= 0 && index < archetypes.Length;
        if (!isValidIndex)
        {
            Debug.LogError(name + ": no archetype at index " + index + ".", this);
            return;
        }

        // Turn everything off first so the old crosshair hides before the new one shows
        for (int i = 0; i < archetypes.Length; i++)
        {
            if (i != index)
            {
                archetypes[i].enabled = false;
            }
        }
        archetypes[index].enabled = true;
        Debug.Log("Gun archetype: " + archetypes[index].GetType().Name);
    }
}
