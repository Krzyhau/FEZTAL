using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Portal : MonoBehaviour
{

    public int portalID = 0;
    public GameObject linkedPortal;
    public Color portalColor;

    GameObject portalCamera;
    GameObject portalModel;
    RenderTexture texture;
    Color prevColor = new Color(128,128,128);
    float openingState = 0;

    void Awake() {

        if (!portalCamera) portalCamera = transform.Find("Camera").gameObject;
        if (!portalModel) portalModel = transform.Find("Model").gameObject;

        if (linkedPortal != null) {
            SetLinkedPortal(linkedPortal);
        }
        Place(transform.position,transform.rotation);
    }

    void Update() {
        // updating the size
        portalModel.transform.localScale = Vector3.Lerp(portalModel.transform.localScale, Vector3.one, Time.deltaTime*7.0f);

        // refreshing the color
        if (prevColor != portalColor) {
            portalModel.GetComponent<MeshRenderer>().materials[0].SetColor("PortalColor", portalColor);
            portalModel.GetComponent<MeshRenderer>().materials[1].SetColor("PortalColor", portalColor);
            portalModel.GetComponent<MeshRenderer>().materials[2].SetColor("PortalColor", portalColor);
            prevColor = portalColor;
        }

        // updating the opening state
        if (linkedPortal) {
            openingState = Mathf.Lerp(openingState, 1, Time.deltaTime * 10.0f);
        } else {
            openingState = 0;
        }
        portalModel.GetComponent<MeshRenderer>().materials[2].SetFloat("OpeningState", openingState);

        if (linkedPortal != null) {
            // setting the position of portal-rendering camera correctly
            portalCamera.transform.position = GetPortalledPosition(Camera.main.transform.position);
            //portalCamera.transform.RotateAround(linkedPortal.transform.position, linkedPortal.transform.up, 180.0f);
            portalCamera.transform.rotation = GetPortalledRotation(Camera.main.transform.rotation);
            

            // clipping camera to avoid the banana juice effect
            Camera portalCamCam = portalCamera.GetComponent<Camera>();
            Plane p = new Plane(-linkedPortal.transform.forward, linkedPortal.transform.position);
            Vector4 clipPlaneWorldSpace = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);
            Vector4 clipPlaneCameraSpace = Matrix4x4.Transpose(Matrix4x4.Inverse(portalCamCam.worldToCameraMatrix)) * clipPlaneWorldSpace;
            portalCamCam.projectionMatrix = Camera.main.CalculateObliqueMatrix(clipPlaneCameraSpace);
        }
    }

    public Vector3 GetPortalledPosition(Vector3 pos) {
        Vector3 relativePos = transform.InverseTransformPoint(pos);
        Vector3 telePos = linkedPortal.transform.TransformPoint(relativePos);
        return Quaternion.AngleAxis(180.0f, linkedPortal.transform.up) * (telePos - linkedPortal.transform.position) + linkedPortal.transform.position;
    }

    public Quaternion GetPortalledRotation(Quaternion rot) {
        return Quaternion.LookRotation(
            linkedPortal.transform.TransformDirection(transform.InverseTransformDirection(rot * Vector3.back)),
            linkedPortal.transform.TransformDirection(transform.InverseTransformDirection(rot * Vector3.up))
        );
    }

    public void SetLinkedPortal(GameObject portal) {
        linkedPortal = portal;
        portalCamera.SetActive(true);
        if(!texture) texture = new RenderTexture(Screen.width, Screen.height, 32);
        portalCamera.GetComponent<Camera>().targetTexture = texture;
        portalModel.GetComponent<MeshRenderer>().materials[2].SetTexture("CameraTexture", texture);
        openingState = Mathf.Min(openingState,0.75f);
    }

    public void Place(Vector3 position, Quaternion angles) {
        transform.position = position;
        transform.rotation = angles;
        portalModel.transform.localScale = Vector3.zero;
    }
}
