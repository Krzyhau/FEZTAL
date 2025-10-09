using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroSkip : MonoBehaviour
{

    public GameObject cubes;

    CanvasGroup skipCG;
    Image blackout;
    AudioSource mainMusic;

    bool canSkip = false;
    bool skipping = false;

    float skipPopupTime = 0;
    float skippingState = 0;
    int annoyanceCounter = 0;

    void Start()
    {
        skipCG = transform.GetChild(0).GetComponent<CanvasGroup>();
        blackout = transform.GetChild(1).GetComponent<Image>();
        mainMusic = cubes.GetComponent<AudioSource>();

        canSkip = PlayerPrefs.GetInt("seenintro") > 0;
    }

    void Update()
    {
        if (!canSkip)
        {
            CheckSkipFromAnnoyance();
        }
        else
        {
            HandleSkipPopup();
            HandleSkipping();
        }
    }

    void CheckSkipFromAnnoyance()
    {
        if (Input.anyKeyDown)
        {
            annoyanceCounter++;
            if (Input.GetKeyDown(KeyCode.Return))
            {
                annoyanceCounter += 2;
            }
            
            if (annoyanceCounter > 2)
            {
                PlayerPrefs.SetInt("seenintro", 1);
                canSkip = true;
                skipPopupTime = 0.5f;
            }
        }
    }
    
    void HandleSkipPopup()
    {
        skipPopupTime += Time.deltaTime;

        float skipAlpha = Mathf.Max(Mathf.Min(skipPopupTime-0.5f, 1, 2-skipPopupTime*0.2f),0);
        skipCG.alpha = skipAlpha;
    }

    void HandleSkipping()
    {
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
