using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class PlaneToQuadFuckingFix
{
    private static Mesh quadMesh;

    [MenuItem("Tools/Selected planes to quads")]
    private static void PlanesToQuads() {

        if (!quadMesh) {
            quadMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        }

        foreach(GameObject go in Selection.objects){
            go.GetComponent<MeshCollider>().sharedMesh = quadMesh;
            go.GetComponent<MeshFilter>().sharedMesh = quadMesh;
        }
    }
}

#endif
