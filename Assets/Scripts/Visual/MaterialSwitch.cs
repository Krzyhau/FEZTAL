using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialSwitch : MonoBehaviour
{
    public bool switched = false;
    public Material switchMaterial;
    public int switchMaterialID = 0;

    bool hasMaterialItself = false;
    
    Material originalMaterial;
    MeshRenderer render;
    bool oldSwitched = false;

    void Start()
    {
        render = GetComponent<MeshRenderer>();
        if (render) {
            hasMaterialItself = true;
            originalMaterial = render.materials[switchMaterialID];
        }
    }

    void Update() {
        if (oldSwitched != switched) {
            SwitchMaterial(switched);
            oldSwitched = switched;
        }
    }

    public void SwitchMaterial(bool switched) {
        this.switched = switched;
        if (hasMaterialItself) {
            //switch own material
            var materials = render.materials;
            if (switched) {
                materials[switchMaterialID] = switchMaterial;
            } else {
                materials[switchMaterialID] = originalMaterial;
            }
            render.materials = materials;
        }

        //switch all materials of children
        foreach(Transform child in transform) {
            var matSwitch = child.GetComponent<MaterialSwitch>();
            if (matSwitch) {
                matSwitch.SwitchMaterial(switched);
            }
        }
    }
}
