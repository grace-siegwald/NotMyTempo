using System.Collections;
using UnityEngine;

/// <summary>
/// Swaps every renderer on this object to a flash material for a split second.
/// Call Flash() whenever the object gets hit.
/// </summary>
public class HitFlash : MonoBehaviour
{
    [Tooltip("A bright Unlit material")]
    [SerializeField] private Material flashMaterial;

    [Tooltip("How long the flash lasts, in seconds")]
    [SerializeField] private float flashDuration = 0.08f;

    private Renderer[] renderers;
    private Material[][] originalMaterials; // what each renderer normally wears
    private Material[][] flashMaterials;    // the same slots, all set to the flash material
    private Coroutine flashRoutine;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length][];
        flashMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterials;

            flashMaterials[i] = new Material[originalMaterials[i].Length];
            for (int m = 0; m < flashMaterials[i].Length; m++)
            {
                flashMaterials[i][m] = flashMaterial;
            }
        }
    }

    public void Flash()
    {
        if (flashMaterial == null)
            return;

        // If we're already flashing, restart it instead of stacking two
        if (flashRoutine != null) { StopCoroutine(flashRoutine); }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        SetMaterials(flashMaterials);
        yield return new WaitForSeconds(flashDuration); // means "come back to this line in 0.08 seconds”
        SetMaterials(originalMaterials);
        flashRoutine = null;
    }

    private void SetMaterials(Material[][] sets)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sharedMaterials = sets[i];
        }
    }
}