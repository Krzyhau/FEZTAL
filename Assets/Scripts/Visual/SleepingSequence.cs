using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Visual
{
    public class SleepingSequence : MonoBehaviour
    {
        [SerializeField] private float _sleepingTime;
        [SerializeField] private Vector3 _entryCamPos;
        [SerializeField] private float _entryCamSize;
        [SerializeField] private AnimationCurve _entryCurve;
        [SerializeField] private float _entryLength;

        private void Start()
        {
            if (_sleepingTime > 0)
            {
                StartCoroutine("InitiateSleeping");
            }
        }

        IEnumerator InitiateSleeping()
        {
            LevelManager.Player.BlockMovement(true);
            LevelManager.Camera.ControlEnabled = false;
            LevelManager.Player.GetComponent<Animator>().SetBool("Sleeping", true);
            yield return new WaitForSeconds(_sleepingTime);
            LevelManager.Player.GetComponent<Animator>().SetBool("Sleeping", false);
            yield return new WaitForSeconds(1.25f);
            LevelManager.Player.BlockMovement(false);
            LevelManager.Camera.ControlEnabled = true;
            LevelManager.StartSpeedrun();
        }

        private void Update()
        {
            if (Time.timeSinceLevelLoad < _entryLength)
            {
                float t = _entryCurve.Evaluate(Time.timeSinceLevelLoad / _entryLength);
                FEZCameraController cam = LevelManager.Player.CameraController;
                Vector3 camPos = Vector3.Lerp(_entryCamPos, cam.GetActualFollowPoint(), t);
                float camSize = Mathf.Lerp(_entryCamSize, cam.GetActualSize(), t);
                cam.SetPositionThisFrame(camPos, camSize);
            }
        }
    }
}