using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SecretCodeCaller : MonoBehaviour
{

    [Serializable]
    public enum GameInput
    {
        Left,
        Right,
        Jump,
        BluePortal,
        OrangePortal,
        ShiftLeft,
        ShiftRight,
        Grab
    }

    static string[] inputNames = new string[] {
        "a",
        "d",
        "space",
        "mouse 0",
        "mouse 1",
        "q",
        "e",
        "left ctrl"
    };

    List<GameInput> lastInputs = new List<GameInput>();


    public List<GameInput> secretCode;
    public UnityEvent onCodeInput;

    bool alreadyUnlocked = false;

    // Update is called once per frame
    void Update()
    {
        if (LevelManager.IsPaused() || alreadyUnlocked) return;

        // fetching inputs
        for (int i = 0; i < inputNames.Length; i++) {
            string input = inputNames[i];
            if (input.StartsWith("Mouse")) {
                int mouseID = Int32.Parse(input.Substring(6));
                if (Input.GetMouseButtonDown(mouseID)) {
                    lastInputs.Add((GameInput)i);
                }
            } else if (Input.GetKeyDown(input)) {
                lastInputs.Add((GameInput)i);
            }
        }


        if(lastInputs.Count >= secretCode.Count) {

            // adjusting the size of the input buffer to match the secret code
            if(lastInputs.Count > secretCode.Count) {
                lastInputs.RemoveRange(0, lastInputs.Count - secretCode.Count);
            }

            // checking if the code is correct
            bool wrongCode = false;
            for (int i = 0; i < secretCode.Count; i++) {
                if(lastInputs[i] != secretCode[i]) {
                    wrongCode = true;
                    break;
                }
            }

            // code is correct
            if (!wrongCode) {
                onCodeInput.Invoke();
                alreadyUnlocked = true;
            }
        }
        
    }
}
