using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreditsMusicManager : MonoBehaviour
{

    public float interpolation = 5.0f;

    public float volumeMultiplier = 0.8f;
    public float pauseVolumeMultiplier = 0.2f;

    AudioSource music;
    void Start() {
        music = GetComponent<AudioSource>();
        music.ignoreListenerPause = true;
        music.volume = 0;
    }


    void Update() {
        bool paused = LevelManager.GetInstance().IsPaused();
        float mult = (paused ? pauseVolumeMultiplier : volumeMultiplier);

        float t = Time.unscaledDeltaTime / (paused ? 1 : interpolation);

        music.volume = Mathf.MoveTowards(music.volume, mult, t);
    }
}
