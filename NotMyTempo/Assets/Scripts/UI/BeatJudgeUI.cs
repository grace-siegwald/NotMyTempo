using TMPro;
using UnityEngine;

/// <summary>
/// Shows ON BEAT / EARLY / LATE under the crosshair after each judged action,
/// then fades it out.
/// </summary>
public class BeatJudgeUI : MonoBehaviour
{
    [Tooltip("Found automatically if left empty")]
    [SerializeField] private Conductor conductor;
    [Tooltip("Found automatically if left empty")]
    [SerializeField] private TMP_Text label;

    [Tooltip("How long the text stays up, in seconds")]
    [SerializeField] private float showTime = 0.4f;
    [SerializeField] private Color onBeatColor;
    [SerializeField] private Color offBeatColor;

    private float hideAt;

    private void Awake()
    {
        if (conductor == null)
            conductor = FindAnyObjectByType<Conductor>();
        if (label == null)
            label = GetComponentInChildren<TMP_Text>();
        label.alpha = 0f; // set label to be invisible at the start
    }

    private void OnEnable()
    {
        conductor.OnActionJudged += ShowJudgement;
    }
    private void OnDisable()
    {
        conductor.OnActionJudged -= ShowJudgement;
    }


    private void Update()
    {
        // fade froom visible to invisible over showTime seconds
        float remaining = hideAt - Time.time;
        label.alpha = Mathf.Clamp01(remaining / showTime);
    }

    private void ShowJudgement(bool isOnBeat, float offset)
    {
        ActionJudgement judgement = conductor.Judgement;
        switch (judgement)
        {
            case ActionJudgement.OnBeat:
                label.text = "ON BEAT";
                label.color = onBeatColor;
                break;
            case ActionJudgement.Early:
                int earlyTime = Mathf.RoundToInt(Mathf.Abs(offset) * 1000f);
                label.text = $"EARLY ({earlyTime} ms)";
                label.color = offBeatColor;
                break;
            case ActionJudgement.Late:
                int lateTime = Mathf.RoundToInt(Mathf.Abs(offset) * 1000f);
                label.text = $"LATE ({lateTime} ms)";
                label.color = offBeatColor;
                break;
        }
        
        hideAt = Time.time + showTime;
    }
}
