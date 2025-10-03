using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private GomezController _player;
    [SerializeField] private FEZCameraController _camera;
    [SerializeField] private PauseMenu _pauseMenu;
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private SpeedrunTimer _speedrunTimer;


    private bool _puzzleFinished;


    private static LevelManager _main;
    public static LevelManager main => _main;
    public static GomezController Player => main._player;
    public static FEZCameraController Camera => main._camera;
    public static PauseMenu PauseMenu => main._pauseMenu;
    public static bool PuzzleFinished => main._puzzleFinished;

    void Awake() => _main = this;
    

    void Start() {
        RefreshAllSettings();
    }

    public static void RefreshAllSettings() {
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

    public static void OnSettingChanged(string name)
    {
        string settingName = "setting_" + name;
        if (name == "volume" || name == "music")
        {
            int percent = PlayerPrefs.GetInt(settingName);
            if (!PlayerPrefs.HasKey(settingName)) percent = 100;
            float realVolume = Mathf.Log10(Mathf.Max(0.0001f, percent * 0.01f)) * 20.0f;
            main._audioMixer.SetFloat(name == "music" ? "MusicVolume" : "MasterVolume", realVolume);
        }
        if (name == "bluecolor" || name == "orangecolor")
        {
            Player?.GetComponent<PortalShooter>().UpdateSettingsColors();
        }
        if (name == "speedruntimer")
        {
            main._speedrunTimer.Active = PlayerPrefs.GetInt(settingName) > 0;
        }
    }

    public static bool IsPaused() {
        return main._pauseMenu.IsMenuEnabled();
    }

    public static bool IsPauseMenuActive()
    {
        return main._pauseMenu.IsActive();
    }

    public static void StartSpeedrun() {
        ResetSpeedrun();
        SpeedrunValues.timerActive = true;
    }

    public static void StopSpeedrun() {
        SpeedrunValues.timerActive = false;
    }

    public static void ResetSpeedrun()
    {
        SpeedrunValues.timerActive = false;
        SpeedrunValues.timer = 0;
        SpeedrunValues.portalCount = 0;
        SpeedrunValues.shiftCount = 0;
    }

    public static void CompletePuzzle()
    {
        main._puzzleFinished = true;
    }

}
