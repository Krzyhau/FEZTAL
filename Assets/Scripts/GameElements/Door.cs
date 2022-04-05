using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    public bool opened;
    public bool exitDoor;
    public AnimationCurve openingCurve;
    public float openingSpeed = 1;

    public AudioClip openingSound;
    public AudioClip closingSound;

    float openingState = 0;
    Material doorMaterial;
    // Start is called before the first frame update
    void Start()
    {
        if (opened) openingState = 1;
        doorMaterial = transform.GetChild(0).GetComponent<MeshRenderer>().materials[0];
    }

    // Update is called once per frame
    void Update()
    {
        openingState = Mathf.MoveTowards(openingState, opened ? 1 : 0, Time.deltaTime * openingSpeed);

        doorMaterial.SetInt("_MirrorLeftSide", exitDoor ? 0 : 1);
        doorMaterial.SetFloat("_OpeningState", openingCurve.Evaluate(openingState));
    }

    public void Open() {
        opened = true;
        GetComponent<AudioManager>().PlayClip(openingSound);
    }

    public void Close() {
        opened = false;
        GetComponent<AudioManager>().PlayClip(closingSound);
    }
}
