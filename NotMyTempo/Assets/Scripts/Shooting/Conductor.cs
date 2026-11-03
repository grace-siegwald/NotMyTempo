using UnityEngine;
using System;
using FMODUnity;
using FMOD.Studio;

/// <summary>
/// Plays the music through FMOD and keeps time from the music itself,
/// so beats always line up with what the player hears.
/// Listen to OnBeat for things that happen every beat, or check
/// isWithinBuffer when the player acts.
/// 
/// Listen to OnActionJudged to see if the player acted on the beat or not, and how far off they were.
/// </summary>
public class Conductor : MonoBehaviour
{
    [Header("Music")]
    [Tooltip("The FMOD event to play")]
    [SerializeField] private EventReference musicEvent; // FMOD's version of a prefab
    [Tooltip("Beats per minute. Must be set to match the tempo in FMOD studio")]
    [SerializeField] private float bpm = 120f; // beats per minute

    [Header("Timing")]
    [Tooltip("How close to a beat counts as on beat (on either side). Smaller = tigther timing, larger = more forgiving. 0.1 = 100ms")]
    [SerializeField] private float buffer = 0.1f;
    [Tooltip("How much to compensate for latency in the audio system. 0.1 = 100ms")]
    [SerializeField] private float latencyCompensation = 0f;    

    // Properties
    public event Action OnBeat; // fires once on every beat
    public event Action<bool, float> OnActionJudged; // fires when the player does something that is "judged" against the beat, passing (isOnBeat, offsetFromNearestBeat)
    public float SecondsPerBeat { get { return 60f / bpm; } } // how many seconds each beat lasts
    public int BeatNumber { get; private set; } = -1; // how many beats have passed since the start of the song
    public bool isWithinBuffer { get { return Math.Abs(OffsetFromNearestBeat()) <= buffer; } } // true if the player is within the buffer of a beat

    private EventInstance music; // just a reference to the event instance (for getting SongTime)
    public float SongTime // calculates how many seconds have passed since the song started, according to FMOD
    { 
        get
        {
            music.getTimelinePosition(out int milliseconds);
            return milliseconds / 1000f - latencyCompensation;
        }
    }

    
    private void Start() // starts the music
    {
        music = RuntimeManager.CreateInstance(musicEvent); // an event is the recipe. An instance is one copy of it actually playing
        music.start(); // now GO !
    }

    private void Update() // beat detection every frame, checks if we've passed a beat, and fires OnBeat if we have
    {
        int beat = Mathf.FloorToInt(SongTime / SecondsPerBeat); // divide the song time by the beat length and round down to get the current beat number
        if (beat >= 0 && beat != BeatNumber) // then, if the current beat number is greater than the last one we recorded, we have passed a beat
        {
            // so we update the beat number and fire the event! very cool
            BeatNumber = beat; 
            OnBeat?.Invoke(); 
        }
    }


    // Seconds from the nearest beat. Negative = early, positive = late
    public float OffsetFromNearestBeat()
    {
        float distanceIntoBeat = SongTime % SecondsPerBeat; // the % operator gives the remainder after division, so this is how far into the current beat we are 

        // Past halfway through a beat, we're closer to the NEXT beat, so we're early
        if (distanceIntoBeat > SecondsPerBeat / 2f) // if distanceIntoBeat is more than the length of half a beat...
        {
            return distanceIntoBeat - SecondsPerBeat; // ...return the distance to the NEXT beat, which is negative (because we're early)
        }

        return distanceIntoBeat;
    }

    // Judges an action that's happening right now, returns true if it was on beat, and tells any listeners of OnActionJudged :)
    public bool JudgeAction() 
    {
        float offset = OffsetFromNearestBeat();
        bool isOnBeat = Math.Abs(offset) <= buffer; // is the offset within the buffer? if so, it's on beat
        OnActionJudged?.Invoke(isOnBeat, offset); // tell any listeners what happened
        return isOnBeat;
    }

    private void OnDestroy()
    {
        music.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        music.release();
    }

    //// Update is called once per frame
    //void Update()
    //{
    //    beatMarker += Time.deltaTime;
    //    if(beatMarker >= deltaTimeNumber)
    //    {
    //        DoBeat();
    //    }
    //    else if (beatMarker >= deltaTimeNumber - buffer || beatMarker <= buffer)
    //    {
    //        isWithinBuffer = true;
    //    }
    //    else
    //    {
    //        isWithinBuffer = false;
    //    }
    //}

    //void DoBeat()
    //{
    //    Debug.Log("Beat");

    //    beatMarker -= deltaTimeNumber;

    //    OnBeat?.Invoke();
    //}
}
