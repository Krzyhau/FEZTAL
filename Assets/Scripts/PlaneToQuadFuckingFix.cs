using System.Linq;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class PlaneToQuadFuckingFix
{
    private static Mesh quadMesh;

    [MenuItem("Tools/Selected planes to quads")]
    private static void PlanesToQuads() {

        Undo.SetCurrentGroupName("Convert planes to quads");
        int group = Undo.GetCurrentGroup();
        
        if (!quadMesh) {
            quadMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        }

        var allChildren = Selection.gameObjects.SelectMany(obj => obj.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject);

        foreach (var child in allChildren)
        {
            ParseObject(child);
        }
        
        Undo.CollapseUndoOperations(group);
    }

    static void ParseObject(GameObject go)
    {
        bool changed = false;
        
        if (go.TryGetComponent<MeshFilter>(out var meshFilter) && meshFilter.sharedMesh is {name:"Plane"})
        {
            Undo.RecordObject(meshFilter, "Convert plane to quad - mesh");
            meshFilter.sharedMesh = quadMesh;
            changed = true;
        }
        
        if (go.TryGetComponent<MeshCollider>(out var meshCollider) && meshCollider.sharedMesh is {name:"Plane"})
        {
            Undo.RecordObject(meshCollider, "Convert plane to quad - collider");
            meshCollider.sharedMesh = quadMesh;
            changed = true;
        }

        if (changed)
        {
            Undo.RecordObject(go.transform, "Convert plane to quad - transform");
            
            go.transform.rotation *= Quaternion.Euler(90,180,0);
            var scale = go.transform.localScale;
            go.transform.localScale = new Vector3(scale.x, scale.z, scale.y) * 10.0f;
        }
        
            
        EditorUtility.SetDirty(go);
    }
}

#endif
