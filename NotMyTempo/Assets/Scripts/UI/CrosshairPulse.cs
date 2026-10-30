using UnityEngine;

public class CrosshairPulse : MonoBehaviour
{
    [SerializeField] private Conductor conductor;
    [Tooltip("How big the crosshair gets on the beat")]
    [SerializeField] private float pulseScale;
    [Tooltip("How quickly it shrinks back. Higher = snappier")]
    [SerializeField] private float returnSpeed = 10f;

    private void Awake()
    {
        if (conductor == null)
            conductor = FindAnyObjectByType<Conductor>();
    }


}
