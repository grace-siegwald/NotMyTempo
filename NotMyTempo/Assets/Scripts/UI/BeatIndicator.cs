using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The beat track on either side of the crosshair.
/// Small markers slide in from both sides and land on the big target triangles exactly on each beat.
/// Shared by every archetype, so it lives beside the crosshairs, not inside one.
/// </summary>
public class BeatIndicator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Found automatically if left empty")]
    [SerializeField] private Conductor conductor;
    [Tooltip("Incoming beat markers on the left side. More markers = more beats visible ahead")]
    [SerializeField] private Image[] leftMarkers;
    [Tooltip("Incoming beat markers on the right side")]
    [SerializeField] private Image[] rightMarkers;

    [Header("Layout")]
    [Tooltip("Distance from center, in pixels, of the big target triangles. Markers land here on the beat")]
    [SerializeField] private float targetDistance = 40f;
    [Tooltip("Pixels between incoming markers. Also how far a marker travels in one beat")]
    [SerializeField] private float beatSpacing = 22f;

    [Header("Colors")]
    [SerializeField] private Color markerColor = new Color(1f, 1f, 1f, 0.6f);

    private const float LeftSide = -1f;
    private const float RightSide = 1f;

    private void Awake()
    {
        if (conductor == null)
        {
            conductor = FindAnyObjectByType<Conductor>();
        }
    }

    private void Update()
    {
        float songBeats = conductor.SongTime / conductor.SecondsPerBeat;
        PlaceMarkers(leftMarkers, LeftSide, songBeats);
        PlaceMarkers(rightMarkers, RightSide, songBeats);
    }

    private void PlaceMarkers(Image[] markers, float side, float songBeats)
    {
        int nextBeat = Mathf.FloorToInt(songBeats) + 1;

        for (int i = 0; i < markers.Length; i++)
        {
            // Marker 0 is heading for the next beat, marker 1 for the one after, and so on
            float beatsAway = nextBeat + i - songBeats;
            float distanceFromCenter = targetDistance + beatsAway * beatSpacing;

            RectTransform markerTransform = markers[i].rectTransform;
            markerTransform.anchoredPosition = new Vector2(side * distanceFromCenter, markerTransform.anchoredPosition.y);

            // The farthest marker fades in as it enters the track
            Color color = markerColor;
            color.a *= Mathf.Clamp01(markers.Length - beatsAway);
            markers[i].color = color;
        }
    }
}
