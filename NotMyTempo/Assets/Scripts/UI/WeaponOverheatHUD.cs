using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class WeaponOverheatHUD : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("The WeaponOverheat component on the active gun. Assign again when equipping a different gun.")]
    [SerializeField] private WeaponOverheat source;

    [Header("Canvas references")]
    [Tooltip("Optional horizontal Filled Image showing current heat, from empty to full.")]
    [SerializeField] private Image heatFill;
    [Tooltip("Child containing the vent dial visuals. Do not assign this script's own GameObject.")]
    [SerializeField] private GameObject dialRoot;
    [Tooltip("Centered pivot containing a needle graphic pointing upward at zero rotation.")]
    [SerializeField] private RectTransform needlePivot;
    [Tooltip("Centered Filled Image using a ring sprite, Radial 360, Top origin and Clockwise.")]
    [SerializeField] private Image targetArc;
    [Tooltip("Optional TextMeshPro UI label for heat, vent prompts and jam status.")]
    [SerializeField] private TMP_Text statusLabel;
    [Tooltip("Optional TextMeshPro UI label for vent hits and misses.")]
    [SerializeField] private TMP_Text progressLabel;

    public void SetSource(WeaponOverheat weapon)
    {
        source = weapon;
    }

    private void LateUpdate()
    {
        bool available = source != null && source.isActiveAndEnabled;
        bool showDial = available && source.IsOverheated;

        if (dialRoot != null)
            dialRoot.SetActive(showDial);

        if (heatFill != null)
            heatFill.fillAmount = available ? source.HeatFraction : 0f;

        if (!available)
        {
            if (statusLabel != null) statusLabel.text = "";
            if (progressLabel != null) progressLabel.text = "";
            return;
        }

        // Clockwise gameplay angles map to negative Canvas Z rotations.
        if (needlePivot != null)
            needlePivot.localRotation = Quaternion.Euler(0f, 0f, -source.NeedleAngle);

        if (targetArc != null)
        {
            targetArc.enabled = source.IsVenting;
            targetArc.fillAmount = source.TargetWidth / 360f;
            targetArc.rectTransform.localRotation = Quaternion.Euler(
                0f, 0f, -(source.TargetAngle - source.TargetWidth * 0.5f));
        }

        if (statusLabel != null)
        {
            if (source.IsJammed)
                statusLabel.text = "VENT JAMMED - WAIT";
            else if (source.IsVenting)
                statusLabel.text = "VENT: PRESS AGAIN IN THE GREEN ZONE";
            else if (source.IsOverheated)
                statusLabel.text = "OVERHEATED - PRESS VENT";
            else
                statusLabel.text = $"HEAT {source.HeatFraction:P0}";
        }

        if (progressLabel != null)
            progressLabel.text = source.IsOverheated
                ? $"HITS {source.Hits}/{source.HitsRequired}   MISSES {source.Misses}/{source.MissLimit}"
                : "";
    }

    private void OnDisable()
    {
        if (dialRoot != null) dialRoot.SetActive(false);
        if (heatFill != null) heatFill.fillAmount = 0f;
        if (statusLabel != null) statusLabel.text = "";
        if (progressLabel != null) progressLabel.text = "";
    }
}
