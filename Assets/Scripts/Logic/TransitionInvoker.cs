using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionInvoker : MonoBehaviour
{
    public void TransitionToLevel(string level)
    {
        if (SceneManager.GetSceneByName(level) != null)
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(level);
        }
    }
}
