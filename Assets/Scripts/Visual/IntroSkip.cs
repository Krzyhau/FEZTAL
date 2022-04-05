using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroSkip : MonoBehaviour
{

    public GameObject cubes;

    CanvasGroup skipCG;
    Image blackout;
    AudioSource mainMusic;

    bool seen = false;
    bool skipping = false;

    float skippingState = 0;

    void Start()
    {
        skipCG = transform.GetChild(0).GetComponent<CanvasGroup>();
        blackout = transform.GetChild(1).GetComponent<Image>();
        mainMusic = cubes.GetComponent<AudioSource>();

        seen = PlayerPrefs.GetInt("seenintro") > 0;

        // i didnt know where to put that or even how to do it properly, so im removing music managers from restart here lol
        GameObject musicManager = GameObject.Find("MusicManager");
        if (musicManager) Destroy(musicManager);
    }

    // Update is called once per frame
    void Update()
    {
        if (seen) {
            float skipAlpha = Mathf.Max(Mathf.Min(Time.timeSinceLevelLoad-0.5f, 1, 2-Time.timeSinceLevelLoad*0.2f),0);
            skipCG.alpha = skipAlpha;

            if (Input.GetKeyDown(KeyCode.Return)) {
                skipping = true;
            }

            if (skipping) {

                mainMusic.volume = 1 - skippingState;
                blackout.color = new Color(0, 0, 0, skippingState);
                skippingState += Time.deltaTime * 0.8f;
                if (skippingState > 1) {
                    SceneManager.LoadScene(cubes.GetComponent<IntroCubes>().nextScene);
                }
            }
        }
    }
}
