using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class Conductor : MonoBehaviour
{
    // the current time since last beat
    public float beatMarker; 
    [Tooltip("The maximum time since last beat, at 120bpm its 0.5")]
    [SerializeField] private float deltaTimeNumber;
    [Tooltip("The beats per minute")]
    [SerializeField] private int bpm;
    [Tooltip("Input buffer window, smaller = tigther timing, larger = more forgiving")]
    [SerializeField] private float buffer;
    // is your input within buffer
    public bool isWithinBuffer;

    public event Action OnBeat;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        deltaTimeNumber = 60f / bpm;
        Debug.Log(deltaTimeNumber);
    }

    // Update is called once per frame
    void Update()
    {
        beatMarker += Time.deltaTime;
        if(beatMarker >= deltaTimeNumber)
        {
            DoBeat();
        }
        else if (beatMarker >= deltaTimeNumber - buffer | beatMarker <= buffer)
        {
            isWithinBuffer = true;
        }
        else
        {
            isWithinBuffer = false;
        }
    }

    void DoBeat()
    {
        Debug.Log("Beat");

        beatMarker -= deltaTimeNumber;

        OnBeat?.Invoke();
    }
}
