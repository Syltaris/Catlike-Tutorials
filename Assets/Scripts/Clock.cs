using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Clock : MonoBehaviour
{

    [SerializeField]
    Transform hoursPivot,  minutesPivot, secondsPivot;

    const float hoursToDegrees = -30.0f, minutesToDegrees = -6f, secondsToDegrees = -6f;


    void UpdateTime() {
        TimeSpan time = DateTime.Now.TimeOfDay;

        hoursPivot.localRotation = Quaternion.Euler(0,0,hoursToDegrees * (float) time.TotalHours);
        minutesPivot.localRotation = Quaternion.Euler(0,0,minutesToDegrees * (float)  time.TotalMinutes);
        secondsPivot.localRotation = Quaternion.Euler(0,0,secondsToDegrees * (float) time.TotalSeconds);
    }



    void Awake() {
        UpdateTime();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTime();
    }
}
