using UnityEngine;
using TMPro;
using UnityEngine.UI;
using TMPro;

public class Conductor : MonoBehaviour
{
    public float beatMarker;
    [SerializeField] private float deltaTimeNumber;
    [SerializeField] private int bpm;

    [SerializeField] private float buffer;
    public bool isWithinBuffer;

    [SerializeField] private Image reticle;
    [SerializeField] private Vector3 baseReticleSize;
    [SerializeField] private Vector3 timedReticleSize;


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
        else if (beatMarker >= deltaTimeNumber +- buffer)
        {
            reticle.gameObject.transform.localScale = timedReticleSize;
            isWithinBuffer = true;
        }
        else
        {
            reticle.gameObject.transform.localScale = baseReticleSize;
            isWithinBuffer = false;
        }
    }

    void DoBeat()
    {
        Debug.Log("Beat");

        beatMarker = 0;

        reticle.gameObject.transform.localScale = timedReticleSize;
    }
}
