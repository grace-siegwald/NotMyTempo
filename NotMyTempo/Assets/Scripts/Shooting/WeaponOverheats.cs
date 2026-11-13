using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class WeaponOverheats : MonoBehaviour
{
    [Header("Heat")]
    [Tooltip("Heat capacity in off-beat shots fired without a break.")]
    [SerializeField, Min(1)] private int shotsBeforeOverheat = 10;
    [SerializeField, Range(0f, 1f)] private float onBeatHeatMultiplier = 0.5f;
    [SerializeField, Min(0f)] private float coolingDelay = 0.5f;
    [SerializeField, Min(0f)] private float coolingPerSecond = 4f;

    [Header("Venting")]
    [Tooltip("Optional button action from an enabled action map. Defaults to R when unassigned.")]
    [SerializeField] private InputActionReference ventAction;
    [SerializeField, Min(1)] private int hitsToVent = 3;
    [SerializeField, Min(1)] private int missesBeforeJam = 3;
    [SerializeField, Min(1f)] private float needleSpeed = 220f;
    [SerializeField, Range(10f, 120f)] private float targetWidth = 46f;
    [SerializeField, Min(0.1f)] private float missDelay = 0.35f;
    [SerializeField, Min(0.1f)] private float jamDelay = 1f;

    private enum State { Ready, Overheated, Venting, Jammed }

    private State state;
    private int hits, misses;
    private float heat, lastShotTime, lastCoolingUpdate;
    private float needleAngle, targetAngle, retryTime;

    public bool IsOverheated => state != State.Ready;
    public bool CanFire => isActiveAndEnabled && !IsOverheated && Time.timeScale > 0f;
    public float HeatFraction => Mathf.Clamp01(heat / Mathf.Max(1, shotsBeforeOverheat));

    public bool TryFire(bool onBeat = false)
    {
        if (!CanFire)
            return false;

        CoolHeat();
        lastShotTime = Time.time;
        heat += onBeat ? onBeatHeatMultiplier : 1f;
        if (heat >= Mathf.Max(1, shotsBeforeOverheat))
            state = State.Overheated;

        return true;
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        if (!IsOverheated)
        {
            CoolHeat();
            return;
        }

        if (Time.time < retryTime)
            return;

        if (state == State.Jammed)
            state = State.Overheated;

        InputAction action = ventAction != null ? ventAction.action : null;
        bool pressed = action?.WasPressedThisFrame() ?? (Keyboard.current?.rKey.wasPressedThisFrame == true);

        if (state == State.Overheated)
        {
            if (pressed)
            {
                hits = misses = 0;
                state = State.Venting;
                NextTarget();
            }
        }
        else if (pressed)
        {
            JudgeTiming();
        }
        else
        {
            needleAngle = Mathf.Repeat(needleAngle + needleSpeed * Time.deltaTime, 360f);
        }
    }

    private void CoolHeat()
    {
        float now = Time.time;
        float coolingStart = Mathf.Max(lastCoolingUpdate, lastShotTime + coolingDelay);
        float coolingTime = Mathf.Max(0f, now - coolingStart);
        heat = Mathf.Max(0f, heat - coolingPerSecond * coolingTime);
        lastCoolingUpdate = now;
    }

    private void NextTarget()
    {
        targetAngle = UnityEngine.Random.Range(0f, 360f);
        needleAngle = Mathf.Repeat(targetAngle + 180f, 360f);
    }

    private void JudgeTiming()
    {
        if (Mathf.Abs(Mathf.DeltaAngle(needleAngle, targetAngle)) <= targetWidth * 0.5f)
        {
            if (++hits >= hitsToVent)
            {
                heat = 0f;
                state = State.Ready;
            }
            else
            {
                NextTarget();
            }
        }
        else if (++misses >= missesBeforeJam)
        {
            state = State.Jammed;
            retryTime = Time.time + jamDelay;
        }
        else
        {
            needleAngle = Mathf.Repeat(targetAngle + 180f, 360f);
            retryTime = Time.time + missDelay;
        }
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint)
            return;

        Matrix4x4 originalMatrix = GUI.matrix;
        Color originalColor = GUI.color;
        float scale = Mathf.Min(1.4f, Mathf.Min(Screen.width / 280f, Screen.height / 460f));
        GUI.matrix = originalMatrix * Matrix4x4.TRS(
            new Vector3(Screen.width * 0.5f, Screen.height - 10f, 0f),
            Quaternion.identity, new Vector3(scale, scale, 1f));

        GUI.Box(new Rect(-110f, -32f, 220f, 32f),
            IsOverheated ? "OVERHEATED" : $"HEAT {HeatFraction:P0}");
        GUI.color = Color.Lerp(Color.cyan, new Color(1f, 0.3f, 0.15f), HeatFraction);
        GUI.DrawTexture(new Rect(-103f, -9f, 206f * HeatFraction, 3f), Texture2D.whiteTexture);
        GUI.color = originalColor;

        string ventKey = ventAction != null ? "VENT" : "R";
        if (state == State.Venting)
            DrawDial(ventKey);
        else if (IsOverheated)
            GUI.Box(new Rect(-110f, -66f, 220f, 30f),
                state == State.Jammed ? "VENT JAMMED / WAIT" : $"PRESS {ventKey} TO VENT");

        GUI.matrix = originalMatrix;
        GUI.color = originalColor;
    }

    private void DrawDial(string ventKey)
    {
        GUI.Box(new Rect(-130f, -217f, 260f, 181f), Time.time < retryTime
            ? "MISSED / GET READY"
            : $"PRESS {ventKey} IN THE GREEN ZONE");
        GUI.Box(new Rect(-105f, -68f, 210f, 26f),
            $"HITS {hits}/{hitsToVent}     MISSES {misses}/{missesBeforeJam}");

        Matrix4x4 panel = GUI.matrix;
        GUI.matrix = panel * Matrix4x4.Translate(new Vector3(0f, -129f, 0f));
        Matrix4x4 dial = GUI.matrix;
        GUI.color = Color.gray;
        for (int angle = 0; angle < 360; angle += 10)
            DrawAtAngle(dial, angle, new Rect(-1.5f, -52f, 3f, 5f));

        GUI.color = Color.green;
        int segments = Mathf.CeilToInt(targetWidth / 2f);
        for (int i = 0; i <= segments; i++)
        {
            float angle = targetAngle + Mathf.Lerp(-targetWidth * 0.5f, targetWidth * 0.5f, (float)i / segments);
            DrawAtAngle(dial, angle, new Rect(-1f, -52f, 2f, 7f));
        }

        GUI.color = Color.white;
        DrawAtAngle(dial, needleAngle, new Rect(-1.5f, -51f, 3f, 51f));
        GUI.matrix = panel;
    }

    private static void DrawAtAngle(Matrix4x4 dial, float angle, Rect rect)
    {
        GUI.matrix = dial * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, angle));
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
    }
}
