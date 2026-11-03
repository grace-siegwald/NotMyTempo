using UnityEngine;
public class CrosshairPulse : MonoBehaviour
{
    [SerializeField] private Conductor conductor;
    [Tooltip("How big the crosshair gets on the beat")]
    [SerializeField] private float pulseScale = 1.4f;
    [Tooltip("How quickly it shrinks back. Higher = snappier")]
    [SerializeField] private float returnSpeed = 10f;

    private Vector3 defaultScale;

    private void Awake()
    {
        defaultScale = transform.localScale; // for remembering whatever size you set in the Inspector :)

        if (conductor == null)
        {
            conductor = FindAnyObjectByType<Conductor>();
        }
    }
    private void Update()
    {
        // Every frame, ease back toward the normal size
        transform.localScale = Vector3.Lerp(transform.localScale, defaultScale, returnSpeed * Time.deltaTime); // moves from a toward b by a fraction t
    }
    private void OnEnable() // on enable, sign Pulse up to the OnBeat event
    {
        if (conductor != null)
            conductor.OnBeat += Pulse;
    }
    private void OnDisable() // and on disable, un-sign from the event
    {
        if (conductor != null)
            conductor.OnBeat -= Pulse;
    }
    private void Pulse()
    {
        // On the beat, jump straight to the big size
        transform.localScale = defaultScale * pulseScale;
    }
}

