using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Portal : Passage
{
    [Header("Portal-related stuff")]
    [SerializeField] private Camera _portalCamera;
    [SerializeField] private MeshRenderer _portalModel;

    public int portalID = 0;
    public Color portalColor;

    private RenderTexture _renderTexture;

    private Color _prevColor = new Color(128,128,128);
    private float _openingState = 0;

    private void Awake() {
        if (HasLinkedPortal()) {
            SetLinkedPortal((Portal)TargetPassage);
        }
        Place(transform.position,transform.rotation);
    }

    private void Update() {
        // updating the size
        _portalModel.transform.localScale = Vector3.Lerp(_portalModel.transform.localScale, Vector3.one, Time.deltaTime*7.0f);

        // refreshing the color if needed
        if (_prevColor != portalColor) {
            _portalModel.materials[0].SetColor("PortalColor", portalColor);
            _portalModel.materials[1].SetColor("PortalColor", portalColor);
            _portalModel.materials[2].SetColor("PortalColor", portalColor);
            _prevColor = portalColor;
        }

        // updating the opening state
        if (TargetPassage) {
            _openingState = Mathf.Lerp(_openingState, 1, Time.deltaTime * 10.0f);
        } else {
            _openingState = 0;
        }
        _portalModel.materials[2].SetFloat("OpeningState", _openingState);

        // translating the camera
        if (TargetPassage) {
            // setting the position of portal-rendering camera correctly
            _portalCamera.transform.position = GetPortalledPosition(Camera.main.transform.position);
            //portalCamera.transform.RotateAround(linkedPortal.transform.position, linkedPortal.transform.up, 180.0f);
            _portalCamera.transform.rotation = GetPortalledRotation(Camera.main.transform.rotation);
            

            // clipping camera to avoid the banana juice effect
            Camera portalCamCam = _portalCamera.GetComponent<Camera>();
            Plane p = new Plane(-TargetPassage.transform.forward, TargetPassage.transform.position);
            Vector4 clipPlaneWorldSpace = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);
            Vector4 clipPlaneCameraSpace = Matrix4x4.Transpose(Matrix4x4.Inverse(portalCamCam.worldToCameraMatrix)) * clipPlaneWorldSpace;
            portalCamCam.projectionMatrix = Camera.main.CalculateObliqueMatrix(clipPlaneCameraSpace);
        }
    }

    public bool HasLinkedPortal()
    {
        return TargetPassage != null && TargetPassage is Portal;
    }

    public Vector3 GetPortalledPosition(Vector3 pos) {
        if (!HasLinkedPortal()) return pos;

        Vector3 relativePos = transform.InverseTransformPoint(pos);
        Vector3 telePos = TargetPassage.transform.TransformPoint(relativePos);
        return Quaternion.AngleAxis(180.0f, TargetPassage.transform.up) * (telePos - TargetPassage.transform.position) + TargetPassage.transform.position;
    }

    public Quaternion GetPortalledRotation(Quaternion rot) {
        return Quaternion.LookRotation(
            TargetPassage.transform.TransformDirection(transform.InverseTransformDirection(rot * Vector3.back)),
            TargetPassage.transform.TransformDirection(transform.InverseTransformDirection(rot * Vector3.up))
        );
    }

    public void SetLinkedPortal(Portal portal) {
        TargetPassage = portal;
        _portalCamera.gameObject.SetActive(true);
        if(!_renderTexture) _renderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGBFloat);
        _portalCamera.targetTexture = _renderTexture;
        _portalModel.materials[2].SetTexture("CameraTexture", _renderTexture);
        _openingState = Mathf.Min(_openingState,0.5f);
    }

    public void Place(Vector3 position, Quaternion angles) {
        Open();
        transform.position = position;
        transform.rotation = angles;
        _portalModel.transform.localScale = Vector3.zero;
    }
}
