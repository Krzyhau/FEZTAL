using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroCubes : MonoBehaviour
{
    GameObject cubeOriginal;

    public Camera[] camerasToControl;
    public AnimationCurve cameraZoomCurve;
    public string nextScene;

    List<Animation> cubeAnims = new List<Animation>();
    List<Transform> cubes = new List<Transform>();
    List<Vector3> cubePoses1 = new List<Vector3>();
    List<Vector3> cubePoses2 = new List<Vector3>();
    List<MeshRenderer> cubeMaterials = new List<MeshRenderer>();

    AudioSource introAudio;

    const int sizeX = 10;
    const int sizeY = 8;

    int sequenceNumber = 0;

    int[][] reds = new int[][]{
        new int[]{},
        new int[]{},
        new int[]{76, 93},
        new int[]{77, 94, 75, 92},
        new int[]{78, 110},
        new int[]{57, 93, 109, 129},
        new int[]{40, 44, 125, 129},
    };

    void Start()
    {
        introAudio = GetComponent<AudioSource>();

        cubeOriginal = transform.GetChild(0).gameObject;

        int id = 0;

        for(int x = -sizeX; x <= sizeX; x+=2) {
            for(int y = -sizeY; y <= sizeY; y++) {
                GameObject obj;
                if (x == 0 && y == 0) {
                    obj = cubeOriginal;
                } else {
                    var offset = 1.5f * (y % 2 == 0 ? x : x + 1);
                    var pos = transform.position + new Vector3(-offset, 3 * y, offset);
                    obj = Instantiate(cubeOriginal, pos, Quaternion.identity, transform);
                }
                obj.name = id.ToString();
                id++;
            }
        }
        

        // saving all stuff into arrays for quick access
        foreach (Transform child in transform) {
            cubeAnims.Add(child.GetComponent<Animation>());
            cubePoses1.Add(child.GetChild(0).position + Vector3.one);
            cubePoses2.Add(child.GetChild(1).position + Vector3.one);
            cubes.Add(child.GetChild(2));
            cubeMaterials.Add(child.GetChild(2).GetComponent<MeshRenderer>());
        }

        StartCoroutine("Sequence");
    }

    
    void Update()
    {
        float f = cameraZoomCurve.Evaluate(introAudio.time);
        foreach(Camera cam in camerasToControl) {
            cam.orthographicSize = f;
        }
    }

    IEnumerator Sequence() {
        sequenceNumber = 0;
        yield return new WaitForSeconds(10.20f);
        for (int i = 0; i < 7; i++) {
            StartCoroutine("Switch");
            yield return new WaitForSeconds(3.584f);
        }
        yield return new WaitForSeconds(2.0f);
        PlayerPrefs.SetInt("seenintro", 1);
        SceneManager.LoadScene(nextScene);
    }

    IEnumerator Switch() {
        for (int i = 0; i < cubeAnims.Count; i++) {
            cubeAnims[i].Stop();
            cubeAnims[i].Play();
        }
        yield return new WaitForSeconds(0.41f);
        for (int i = 0; i < cubes.Count; i++) {
            cubes[i].position = (cubes[i].position == cubePoses1[i]) ? cubePoses2[i] : cubePoses1[i];

            Color c = Random.ColorHSV(0, 1, 0, 1, 0.1f, 0.5f);

            foreach (int id in reds[sequenceNumber]) {
                if(id.ToString().Equals(cubes[i].parent.name)) {
                    c = Color.red;
                    break;
                }
            }

            cubeMaterials[i].material.color = c;
        }

        sequenceNumber++;
    }
}
