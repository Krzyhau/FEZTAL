using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public Door entryDoors;
    public Door exitDoors;
    public string nextLevel;
    public bool isMainMenu = false;
    public AudioMixer audioMixer;
    public PauseMenu pauseMenu;
    public float startSleepingTime = 0.0f;

    [Header("Special Entry")]
    public bool specialEntry = false;
    public Vector3 specialEntryCamPos;
    public float specialEntryCamSize;
    public AnimationCurve specialEntryCurve;
    public float specialEntryLength;

    static LevelManager main;
    float fadeValue = 1;
    bool transitioning = false;
    float sceneTime = 0;
    bool closedEntryDoors = false;
    bool sleeping = false;

    bool speedrunHudActive = false;
    Image speedrunHud;
    Text speedrunHudValues;

    Image fadeImage;

    void Awake()
    {
        main = this;
        fadeImage = transform.Find("Fade").GetComponent<Image>();

        speedrunHud = transform.Find("SpeedrunHud").GetComponent<Image>();
        speedrunHudValues = speedrunHud.transform.Find("Values").GetComponent<Text>();

        if (isMainMenu) fadeValue = 0;
        fadeImage.color = new Color(0, 0, 0, fadeValue);

        UpdateSpeedrunTimer();
    }

    void Start() {
        RefreshAllSettings();
        if (startSleepingTime > 0) {
            StartCoroutine("InitiateSleeping");
        }
    }

    IEnumerator InitiateSleeping() {
        sleeping = true;
        yield return null;
        GomezController.main.BlockMovement(true);
        GomezController.main.GetComponent<Animator>().SetBool("Sleeping", true);
        yield return new WaitForSeconds(startSleepingTime);
        GomezController.main.GetComponent<Animator>().SetBool("Sleeping", false);
        yield return new WaitForSeconds(1.25f);
        GomezController.main.BlockMovement(false);
        StartSpeedrun();
        sleeping = false;
    }

    void Update()
    {
        if(fadeValue == 1 && transitioning) {
            if (SceneManager.GetSceneByName(nextLevel) != null) {
                Time.timeScale = 1;
                SceneManager.LoadScene(nextLevel);
            }
        }

        fadeValue = Mathf.MoveTowards(fadeValue, transitioning ? 1 : 0, Time.deltaTime);
        Color fc = fadeImage.color;
        fadeImage.color = new Color(fc.r, fc.g, fc.b, fadeValue);



        if (!isMainMenu) {
            if (sceneTime > 0.7f && !closedEntryDoors) {
                if (!entryDoors) {
                    closedEntryDoors = true;
                }
                else if (entryDoors.opened) {
                    entryDoors.Close();
                    closedEntryDoors = true;
                }
            }

            if (!IsPaused() && Input.GetKeyDown(KeyCode.Escape) && GomezController.CanControl()) {
                pauseMenu.EnableMenu(true);
            }

            if(specialEntry && sceneTime < specialEntryLength) {
                float t = specialEntryCurve.Evaluate(sceneTime / specialEntryLength);
                GomezController gomez = GomezController.main;
                Vector3 camPos = Vector3.Lerp(specialEntryCamPos, gomez.GetActualCameraFollowPoint(), t);
                float camSize = Mathf.Lerp(specialEntryCamSize, gomez.GetActualCameraSize(), t);
                gomez.SetCameraPositionThisFrame(camPos, camSize);
            }
        }

        if (IsPaused()) {
            Time.timeScale = 0;
            AudioListener.pause = true;
        } else {
            Time.timeScale = 1;
            AudioListener.pause = false;
        }

        sceneTime += Time.deltaTime;


        UpdateSpeedrunTimer();
    }


    public bool TransitionToNextLevel(GameObject hitDoors) {
        if(exitDoors && hitDoors == exitDoors.gameObject && exitDoors.opened) {
            transitioning = true;
            return true;
        }
        return false;
    }
    public static LevelManager GetInstance() {
        return main;
    }

    public void RefreshAllSettings() {
        OnSettingChanged("fullscreen");
        OnSettingChanged("bloom");
        OnSettingChanged("vignette");
        OnSettingChanged("pixelperfect");

        OnSettingChanged("volume");
        OnSettingChanged("music");

        OnSettingChanged("speedruntimer");
        OnSettingChanged("followmouse");
        OnSettingChanged("bluecolor");
        OnSettingChanged("orangecolor");
    }
    public void OnSettingChanged(string name) {
        string settingName = "setting_" + name;
        if(name == "volume" || name == "music") {
            int percent = PlayerPrefs.GetInt(settingName);
            if (!PlayerPrefs.HasKey(settingName)) percent = 100;
            float realVolume = Mathf.Log10(Mathf.Max(0.0001f, percent * 0.01f)) * 20.0f;
            audioMixer.SetFloat(name == "music" ? "MusicVolume" : "MasterVolume", realVolume);
        }
        if(name == "bluecolor" || name == "orangecolor") {
            if (GomezController.main) {
                GomezController.main.GetComponent<PortalShooter>().UpdateSettingsColors();
            }
        }
        if(name == "speedruntimer") {
            speedrunHudActive = PlayerPrefs.GetInt(settingName) > 0;
        }
    }

    public bool IsPaused() {
        return pauseMenu.IsMenuEnabled();
    }






    public void StartSpeedrun() {
        SpeedrunValues.timer = 0;
        SpeedrunValues.portalCount = 0;
        SpeedrunValues.shiftCount = 0;

        SpeedrunValues.timerActive = true;
    }

    public void StopSpeedrun() {
        SpeedrunValues.timerActive = false;
    }

    void UpdateSpeedrunTimer() {
        if (!IsPaused() && SpeedrunValues.timerActive) {
            SpeedrunValues.timer += Time.unscaledDeltaTime;
        }

        bool shouldShow = speedrunHudActive && !IsPaused() && !sleeping && !isMainMenu;
        if (shouldShow && !speedrunHud.gameObject.activeSelf) speedrunHud.gameObject.SetActive(true);
        if (!shouldShow && speedrunHud.gameObject.activeSelf) speedrunHud.gameObject.SetActive(false);

        if (SpeedrunValues.timerActive) {
            string values = "";
            float srtime = SpeedrunValues.timer;
            values += $"{Mathf.FloorToInt(srtime / 60)}:{Mathf.FloorToInt(srtime % 60).ToString("D2")}";
            if (srtime < 600) { // smaller than 10 minutes
                values += $".{Mathf.FloorToInt((srtime * 1000) % 1000).ToString("D3")}";
            }
            values += $" \n{SpeedrunValues.portalCount} \n{SpeedrunValues.shiftCount} ";

            speedrunHudValues.text = values;
        }
        
    }

}
