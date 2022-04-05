using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleScreenManager : MonoBehaviour
{
    public AudioSource startMusic;
    public AudioSource idleMusic;
    public AudioSource quitMusic;

    public GameObject logo;
    public PauseMenu pauseMenu;

    bool canQuit = false;
    bool quitted = false;

    void Start()
    {
        StartCoroutine("Begin");
    }

    IEnumerator Begin() {
        startMusic.Play();
        idleMusic.PlayDelayed(startMusic.clip.length - 0.01f);
        yield return new WaitForSeconds(0.6f);
        logo.GetComponent<Animator>().SetBool("Closing", false);
        yield return new WaitForSeconds(2.0f);
        canQuit = true;
    }

    IEnumerator FadeOutMusic() {
        while(startMusic.volume > 0) {
            var volume = Mathf.Max(0, startMusic.volume - Time.deltaTime * 5.0f);
            startMusic.volume = volume;
            idleMusic.volume = volume;
            yield return null;
        }
    }

    IEnumerator End() {
        yield return new WaitForSeconds(0.8f);
        pauseMenu.EnableMenu(false);
        yield return new WaitForSeconds(0.1f);
        GameObject.Find("Logo").SetActive(false);
        GameObject.Find("HidePlane").SetActive(false);
    }

    void Update()
    {
        if(canQuit && !quitted && Input.GetKeyDown(KeyCode.Return)) {
            logo.GetComponent<Animator>().SetBool("Closing", true);
            quitMusic.ignoreListenerPause = true;
            quitMusic.Play();
            StartCoroutine("FadeOutMusic");
            StartCoroutine("End");
            quitted = true;
        }
    }
}
