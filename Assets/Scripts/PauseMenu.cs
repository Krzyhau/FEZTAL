using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public string startMenu;
    [Header("Inner Elements")]
    public Camera UICamera;
    public GameObject mainBorders;
    public AnimationCurve borderAnimationCurve;
    public AnimationCurve specialInAnimation;
    public AnimationCurve specialOutAnimation;
    public Text titleText;
    public GameObject menuOptions;
    public Text acceptText;
    public Text denyText;
    public GameObject cursor;
    public GameObject arrowCursor;
    public GameObject backgroundObject;
    public enum MenuFieldType
    {
        None,
        Boolean,
        Percentage,
        Color,
        InputKey
    }

    string[] settingsColorNames = {
        "blue", "orange", "aqua", "red", "yellow", "green", "lime", "pink", "purple", "brown"
    };

    [Serializable]
    public struct MenuField
    {
        public string name;
        [Multiline] public string displayName;
        public MenuFieldType inputType;
        public string defaultValue;
        public UnityEvent interactEvent;
    }

    [Serializable]
    public struct Menu
    {
        public string name;
        public string displayName;
        public int width;
        public List<MenuField> fields;
        public string acceptName;
        public UnityEvent acceptEvent;
        public string denyName;
        public UnityEvent denyEvent;
    }

    [Space(20)]

    public List<Menu> menus;

    [Header("Menu Sounds")]
    public AudioClip pauseSound;
    public AudioClip unpauseSound;
    public AudioClip goUpSound;
    public AudioClip goDownSound;
    public AudioClip changeUpSound;
    public AudioClip changeDownSound;
    public AudioClip acceptSound;
    public AudioClip denySound;
    public AudioClip startGameSound;
    public AudioClip exitGameSound;

    RectTransform mainBorderVertical;
    List<RectTransform> mainBorderHorizontals = new List<RectTransform>();

    GameObject acceptObj;
    GameObject denyObj;

    RectTransform cursorRect;
    Image cursorImage;
    RectTransform menuOptionsRect;
    RectTransform menuMaskRect;
    RectTransform menuParentRect;
    RectTransform menuParentMaskRect;
    RectTransform arrowCursorRect;

    GameObject templateField;
    RectTransform activeFieldRect = null;
    MenuField activeField;

    Menu currentMenu;

    float stateTimer = 0;
    bool active = false;
    bool menuEnabled = false;
    bool crosshairEnabled = false;
    bool closing = false;
    bool specialCameraAnim = false;
    string closeAction = "";

    AudioManager audioManager;

    void Start()
    {
        mainBorderVertical = mainBorders.GetComponent<RectTransform>();
        foreach (Transform border in mainBorders.transform) {
            mainBorderHorizontals.Add(border.GetComponent<RectTransform>());
        }
        cursorRect = cursor.GetComponent<RectTransform>();
        cursorImage = cursor.GetComponent<Image>();
        menuOptionsRect = menuOptions.GetComponent<RectTransform>();
        menuMaskRect = menuOptions.transform.parent.GetComponent<RectTransform>();
        menuParentRect = menuMaskRect.transform.parent.GetComponent<RectTransform>();
        menuParentMaskRect = menuParentRect.transform.parent.GetComponent<RectTransform>();
        arrowCursorRect = arrowCursor.GetComponent<RectTransform>();

        acceptObj = acceptText.transform.parent.gameObject;
        denyObj = denyText.transform.parent.gameObject;

        ShowElements(false);

        templateField = menuOptions.transform.GetChild(0).gameObject;
        ShowElement(templateField, false);
        templateField.transform.SetParent(menuMaskRect.transform);

        audioManager = GetComponent<AudioManager>();
        GetComponent<AudioSource>().ignoreListenerPause = true;
    }

    void Update() {

        const float borderAnimTime = 0.8f;
        const float zoomOutAnimTime = 0.3f;

        if (!menuEnabled) {
            if (menuParentRect.gameObject.activeSelf) {
                menuParentRect.gameObject.SetActive(false);
            }
            return;
        } else {
            if (!menuParentRect.gameObject.activeSelf) {
                menuParentRect.gameObject.SetActive(true);
            }
        }

        if (!active) {
            float rt = stateTimer / borderAnimTime;
            if (closing) rt = 1 - rt;
            float t = borderAnimationCurve.Evaluate(rt);
            mainBorderVertical.localScale = new Vector3(1, Mathf.Clamp(2.0f * t, 0, 1), 1);

            foreach(var border in mainBorderHorizontals) {
                border.localScale = new Vector3(Mathf.Clamp(t - 0.5f, 0, 0.5f), 1, 1);
            }


            // VERY JANKY MENU POSITIONING! IT IS CURSE TO BE CREATIVE BUT NOT SKILLED ENOUGH >=[
            Vector3 uiCamPos = backgroundObject.transform.position;
            if (specialCameraAnim) {
                float ot;
                if (closing) {
                    ot = 0.5f+specialOutAnimation.Evaluate(1 - rt)*0.5f;
                } else {
                    ot = specialInAnimation.Evaluate(rt)*0.5f;
                }
                uiCamPos.y = Mathf.Lerp(-12, 12, ot);

                float oot = Mathf.Clamp(ot * 1.2f - 0.1f, 0, 1);
                menuParentMaskRect.sizeDelta = new Vector2(800, Mathf.Lerp(500, 0, 2*Mathf.Abs(0.5f- oot)));
                menuParentMaskRect.anchoredPosition = new Vector2(0, Mathf.Lerp(-250, 250, oot));
                menuParentRect.anchoredPosition = new Vector2(0, Mathf.Lerp(250, -250, oot));
            }
            if (!specialCameraAnim || stateTimer > borderAnimTime) {
                uiCamPos.y = 0;
                menuParentMaskRect.sizeDelta = new Vector2(800, 500);
                menuParentMaskRect.anchoredPosition = Vector2.zero;
                menuParentRect.anchoredPosition = Vector2.zero;
            }
            backgroundObject.transform.position = uiCamPos;

            

            if (stateTimer > borderAnimTime || (specialCameraAnim && closing && stateTimer > borderAnimTime-0.2f)) {
                if (closing) {
                    menuEnabled = false;
                    stateTimer = 0;
                    closing = false;
                    OnMenuDisabled();
                } else {
                    active = true;
                    ShowElements(true);
                    ChangeMenu(startMenu);
                    stateTimer = zoomOutAnimTime - Time.unscaledDeltaTime * 0.1f;
                }
                
            }
        }
        
        if(active){
            // navigation handling
            int nav = 0;
            if (Input.GetKeyDown(KeyCode.UpArrow)) {
                nav -= 1;
                audioManager.PlayClip(goUpSound);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow)) {
                nav += 1;
                audioManager.PlayClip(goDownSound);
            }
            if (nav != 0) {
                int current = activeFieldRect.transform.GetSiblingIndex();
                int fieldCount = menuOptions.transform.childCount;
                int next = (current + nav + fieldCount) % fieldCount;
                activeFieldRect = menuOptions.transform.GetChild(next).GetComponent<RectTransform>();
                activeField = currentMenu.fields[next];
            }

            // key press handling
            if (Input.GetKeyDown(KeyCode.Return)) {
                AcceptPress();
            }
            if (Input.GetKeyDown(KeyCode.Escape)) {
                DenyPress();
            }

            int navX = 0;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                navX -= 1;
            }
            if (Input.GetKeyDown(KeyCode.RightArrow)) {
                navX += 1;
            }
            if (navX != 0 && activeField.inputType != MenuFieldType.None && activeField.inputType != MenuFieldType.InputKey) {
                ChangeCurrentSetting(navX>0);
            }

            // resize parent rect to desired positon
            Vector2 parentRectSize = new Vector2(currentMenu.width == 0 ? 500 : currentMenu.width, menuParentRect.sizeDelta.y);
            float prevWidth = menuParentRect.sizeDelta.x;
            menuParentRect.sizeDelta = Vector2.MoveTowards(menuParentRect.sizeDelta, parentRectSize, Time.unscaledDeltaTime * 1000);
            float widthDiff = menuParentRect.sizeDelta.x - prevWidth;

            // adjusting cursor position to accommodate the parent div resizing
            cursorRect.anchoredPosition = cursorRect.anchoredPosition + Vector2.right * widthDiff * 0.5f;

            // update cursor position and zooming out
            Vector2 cv = new Vector2(0.5f, -0.5f); // correctio vector
            if (stateTimer < zoomOutAnimTime || !activeFieldRect) {
                if (stateTimer == 0) {
                    menuMaskRect.sizeDelta = cursorRect.sizeDelta;
                    menuMaskRect.anchoredPosition = cursorRect.anchoredPosition;
                } else {
                    float t = Mathf.Min(stateTimer / (zoomOutAnimTime - 0.05f), 1);
                    menuMaskRect.sizeDelta = Vector2.Lerp(cursorRect.sizeDelta, menuParentRect.sizeDelta, t);
                    menuMaskRect.anchoredPosition = Vector2.Lerp(cursorRect.anchoredPosition, menuParentRect.sizeDelta * cv, t);
                }

                if(stateTimer + Time.unscaledDeltaTime >= zoomOutAnimTime) {
                    cursorRect.sizeDelta = menuMaskRect.sizeDelta;
                    cursorRect.anchoredPosition = menuMaskRect.sizeDelta * cv;
                }
                cursorImage.color = new Color(255, 255, 255, 0);
            } else {
                menuMaskRect.sizeDelta = menuParentRect.sizeDelta;

                float interpForce = Time.unscaledDeltaTime * Mathf.Min(0.4f + stateTimer - zoomOutAnimTime, 1) * 20;

                cursorRect.sizeDelta = Vector2.Lerp(cursorRect.sizeDelta, activeFieldRect.sizeDelta + new Vector2(40, 0), interpForce);
                cursorRect.anchoredPosition = Vector2.Lerp(cursorRect.anchoredPosition, activeFieldRect.anchoredPosition, interpForce);

                if (crosshairEnabled) {
                    cursorImage.color = Color.Lerp(cursorImage.color, Color.white, Time.unscaledDeltaTime * 2.0f);
                } else {
                    cursorImage.color = new Color(255, 255, 255, 0);
                }
            }
            Vector2 newMenuOptionsPos = menuParentRect.sizeDelta * cv - (menuMaskRect.anchoredPosition - menuMaskRect.sizeDelta * cv);
            menuOptionsRect.anchoredPosition = newMenuOptionsPos;
            menuOptionsRect.sizeDelta = menuParentRect.sizeDelta;

            if (activeFieldRect) {
                arrowCursorRect.sizeDelta = new Vector2(40 + activeFieldRect.sizeDelta.x, arrowCursorRect.sizeDelta.y);
                arrowCursorRect.anchoredPosition = activeFieldRect.anchoredPosition;

                if (activeField.inputType != MenuFieldType.None && activeField.inputType != MenuFieldType.InputKey) {
                    ShowElement(arrowCursor, true);
                } else {
                    ShowElement(arrowCursor, false);
                }
            }
        }

        stateTimer += Time.unscaledDeltaTime;
    }

    string GetFieldDisplayName(MenuField field) {
        string name = field.displayName;

        if(field.inputType != MenuFieldType.None) {
            name += " : ";
            string settingName = "setting_" + field.name;
            switch (field.inputType) {
                case MenuFieldType.Boolean:
                    name += PlayerPrefs.GetInt(settingName) > 0 ? "YES" : "NO";
                    break;
                case MenuFieldType.Percentage:
                    name += PlayerPrefs.GetInt(settingName) + "%";
                    break;
                case MenuFieldType.Color:
                    name += PlayerPrefs.GetString(settingName);
                    break;
                case MenuFieldType.InputKey:
                    name += PlayerPrefs.GetString(settingName);
                    break;
            }
        }

        return name.ToUpper();
    }

    public void ChangeCurrentSetting(bool wentRigth) {
        bool changed = false;
        string settingName = "setting_" + activeField.name;
        switch (activeField.inputType) {
            case MenuFieldType.Boolean:
                int boolValue = PlayerPrefs.GetInt(settingName) > 0 ? 0 : 1;
                PlayerPrefs.SetInt(settingName, boolValue);
                changed = true;
                break;
            case MenuFieldType.Percentage:
                int percent = PlayerPrefs.GetInt(settingName);
                percent += (wentRigth) ? 10 : -10;
                PlayerPrefs.SetInt(settingName, Mathf.Clamp(percent,0,100));
                changed = true;
                break;
            case MenuFieldType.Color:
                string color = PlayerPrefs.GetString(settingName);
                int id = Array.IndexOf(settingsColorNames, color);
                int newColorId = (id + (wentRigth ? 1 : -1) + settingsColorNames.Length) % settingsColorNames.Length;
                PlayerPrefs.SetString(settingName, settingsColorNames[newColorId]);
                changed = true;
                break;
        }

        if (changed) {
            audioManager.PlayClip(wentRigth ? changeUpSound : changeDownSound);
        }

        activeFieldRect.gameObject.GetComponent<Text>().text = GetFieldDisplayName(activeField);

        LevelManager.GetInstance().OnSettingChanged(activeField.name);
        activeField.interactEvent.Invoke();
    }


    public void ChangeMenu(string target) {
        //removing previous fields
        foreach(Transform field in menuOptions.transform){
            if(field.gameObject != templateField) {
                Destroy(field.gameObject);
            }
        }
        activeFieldRect = null;
        crosshairEnabled = false;

        //separating name for providing button in single variable
        string[] targetparts = target.Split(':');
        string name = targetparts[0];
        string buttonID = (targetparts.Length > 1) ? targetparts[1] : "";

        bool foundMenu = false;
        Menu wantedMenu = new Menu();
        foreach(Menu menu in menus) {
            if(menu.name == name) {
                foundMenu = true;
                wantedMenu = menu;
                break;
            }
        }
        if (!foundMenu) return;
        currentMenu = wantedMenu;

        foreach (MenuField field in wantedMenu.fields) {
            // check if value is assigned
            if(field.inputType != MenuFieldType.None) {
                string settingName = "setting_" + field.name;
                if (!PlayerPrefs.HasKey(settingName)) {
                    switch (field.inputType) {
                        case MenuFieldType.Boolean:
                            PlayerPrefs.SetInt(settingName, field.defaultValue != "false" ? 1 : 0);
                            break;
                        case MenuFieldType.Percentage:
                            PlayerPrefs.SetInt(settingName, Int32.Parse(field.defaultValue));
                            break;
                        case MenuFieldType.Color:
                        case MenuFieldType.InputKey:
                            PlayerPrefs.SetString(settingName, field.defaultValue);
                            break;
                    }
                }
            }

            //create menu field in menu
            var fieldObj = Instantiate(templateField, menuOptions.transform);
            ShowElement(fieldObj, true);
            fieldObj.name = field.name.ToUpper();
            fieldObj.GetComponent<Text>().text = GetFieldDisplayName(field);

            EventTrigger trigger = fieldObj.GetComponent<EventTrigger>();
            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((eventData) => { 
                field.interactEvent.Invoke();
                if (active && field.interactEvent.GetPersistentEventCount() > 0) {
                    audioManager.PlayClip(acceptSound);
                }
            });
            trigger.triggers.Add(clickEntry);
            EventTrigger.Entry hoverEntry = new EventTrigger.Entry();
            hoverEntry.eventID = EventTriggerType.PointerEnter;
            hoverEntry.callback.AddListener((eventData) => { 
                activeFieldRect = fieldObj.GetComponent<RectTransform>();
                activeField = field;
                if (stateTimer > 0.5f && (field.interactEvent.GetPersistentEventCount() > 0 || field.inputType != MenuFieldType.None)) {
                    audioManager.PlayClip(goUpSound);
                }
            });
            trigger.triggers.Add(hoverEntry);

            if (activeFieldRect == null || field.name == buttonID) hoverEntry.callback.Invoke(null);
            if (field.interactEvent.GetPersistentEventCount() > 0 || field.inputType != MenuFieldType.None) crosshairEnabled = true;
        }

        titleText.text = wantedMenu.displayName;

        ShowElement(acceptObj, wantedMenu.acceptEvent.GetPersistentEventCount() > 0);
        ShowElement(denyObj,wantedMenu.denyEvent.GetPersistentEventCount() > 0);

        acceptText.text = wantedMenu.acceptName;
        denyText.text = wantedMenu.denyName;

        stateTimer = 0;
    }

    public void AcceptPress() {
        ShowElement(acceptObj, false);
        if (currentMenu.acceptEvent.GetPersistentEventCount() > 0) {
            currentMenu.acceptEvent.Invoke();
        } else {
            if (activeFieldRect) {
                activeField.interactEvent.Invoke();
            }
        }
        if (active && currentMenu.denyEvent.GetPersistentEventCount() > 0) {
            audioManager.PlayClip(acceptSound);
        }
    }

    public void DenyPress() {
        if (active && currentMenu.denyEvent.GetPersistentEventCount() > 0) {
            audioManager.PlayClip(denySound);
        }
        currentMenu.denyEvent.Invoke();
    }

    public void EnableMenu(bool animOpen = false) {
        menuEnabled = true;
        active = false;
        stateTimer = 0;
        specialCameraAnim = animOpen;
        closing = false;
        audioManager.PlayClip(pauseSound);
    }

    public void DisableMenu(bool animClose = false) {
        active = false;
        stateTimer = 0;
        closing = true;
        ShowElements(false);
        specialCameraAnim = animClose;

        if(animClose == true) {
            audioManager.PlayClip(unpauseSound);
        } else {
            if(closeAction == "exit") {
                audioManager.PlayClip(exitGameSound);
            } else {
                audioManager.PlayClip(startGameSound);
            }
        }
        
    }

    public void SetCloseAction(string action) {
        closeAction = action;
    }

    void OnMenuDisabled() {
        if(closeAction.Length > 0) {
            if(closeAction == "exit") {
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
            } else if(SceneManager.GetSceneByName(closeAction) != null){
                Time.timeScale = 1; // Level manager should handle that smh
                AudioListener.pause = false;
                SceneManager.LoadScene(closeAction);
            }
        }
    }

    public void ShowElements(bool show) {
        titleText.enabled = show;
        ShowElement(acceptObj, show);
        ShowElement(denyObj, show);
        ShowElement(menuOptions, show);
        ShowElement(menuMaskRect.gameObject, show);
        ShowElement(cursor, show);
        if (!show) ShowElement(arrowCursor, show);
    }

    void ShowElement(GameObject element, bool show) {
        element.transform.localScale = show ? Vector3.one : Vector3.zero;
    }

    public bool IsMenuEnabled() {
        return menuEnabled;
    }
}
