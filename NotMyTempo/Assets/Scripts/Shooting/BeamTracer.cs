using UnityEngine;

/// <summary>
/// A single visible beam from one "bullet".
/// The Gun spawns one per direction, and it destroys itself after the shot's BeamDuration.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class BeamTracer : MonoBehaviour
{
    public void Show(Vector3 start, Vector3 end, float duration)
    {
        LineRenderer line = GetComponent<LineRenderer>();
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        Destroy(gameObject, duration);
    }

}
