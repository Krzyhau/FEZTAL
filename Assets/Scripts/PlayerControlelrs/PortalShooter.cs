using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalShooter : MonoBehaviour
{
    public GomezController gomez;
    public GameObject portalPrefab;
    public LayerMask portalBlockMask;
    public LayerMask portalableMask;
    public float gunReloadTime = 0.2f;
    public bool DEBUG = false;

    [Header("Sounds")]
    public AudioClip bluePortalShootSound;
    public AudioClip orangePortalShootSound;

    [Header("Visual")]
    public GameObject shotParticlePrefab;
    public GameObject shotMissedParticlePrefab;
    public GameObject shotMuzzle;
    public Transform shotParticleSpawnPoint;


    private Portal _portal1;
    private Portal _portal2;

    Color portal1Color;
    Color portal2Color;

    const float PORTAL_WIDTH = 0.97f;
    const float PORTAL_HEIGHT = 1.97f;

    const int MAX_BUMP_ITERATIONS = 4;
    const float BUMP_DISTANCE = 0.01f;
    const float SURFACE_CHECK_STEP = 0.01f;
    const float MAX_SURFACE_DIST = 0.1f;

    float reloading = 0;

    
    struct PortalHitInfo
    {
        public bool portalable;
        public Vector3 origin;
    }

    void Start() {
        UpdateSettingsColors();
    }

    void Update()
    {
        if (LevelManager.IsPaused()) return;

        if (gomez.CanControl() && reloading == 0) {
            AttemptPortalShots();
        }

        // preventing the player from shooting a portal while camera is still significantly angled.
        // this exists because the player regains control before the camera is fully rotated, the same
        // way as in the original game.
        if (!gomez.CanControl()) {
            reloading = gunReloadTime;
        }

        reloading = Mathf.Max(reloading - Time.deltaTime, 0);
    }

    void AttemptPortalShots() 
    {
        int shot = 0;
        if (Input.GetMouseButtonDown(0)) {
            shot = 1;
        }
        if (Input.GetMouseButtonDown(1)) {
            shot = 2;
        }

        if (shot == 0) return;

        var camera = gomez.CameraController.Camera;

        var portalColor = (shot == 1) ? portal1Color : portal2Color;

        var muzzleRenderer = shotMuzzle.GetComponent<SpriteRenderer>();
        muzzleRenderer.color = portalColor;

        reloading = gunReloadTime;

        GetComponent<AudioManager>().PlayClip(shot == 1 ? bluePortalShootSound : orangePortalShootSound);

        if (DEBUG) Debug.Log("Trying to place a portal");

        Vector3 clickPoint = camera.ScreenToWorldPoint(Input.mousePosition);   

        // while we're at it, setting the animation and flipping the sprite properly.
        GetComponent<Animator>().Play("gomez_shoot",0,0.0f);
        GetComponent<GomezController>().FlipSprite(Vector3.Dot(camera.transform.right, clickPoint - transform.position) < 0);

        RaycastHit hit;
        bool foundSurface = Physics.Raycast(clickPoint, camera.transform.forward, out hit, 128.0f, portalBlockMask);
        
        Vector3 shotPoint;
        if(foundSurface) shotPoint = hit.point + camera.transform.forward * (MAX_SURFACE_DIST * -0.1f);
        else {
            Vector3 clickDir = (clickPoint - shotParticleSpawnPoint.position);
            clickDir -= camera.transform.forward * Vector3.Dot(clickDir - shotParticleSpawnPoint.position, camera.transform.forward);
            shotPoint = shotParticleSpawnPoint.position + clickDir.normalized * 16f;
        }

        // create shot particle
        var shotParticleObj = Instantiate(shotParticlePrefab);
        var shotParticle = shotParticleObj.GetComponent<PortalShotParticle>();
        shotParticle.startPosition = shotParticleSpawnPoint.position;
        shotParticle.targetPosition = shotPoint;
        shotParticle.color = portalColor;
        shotParticle.timespan = 0.5f;

        if (!foundSurface) {
            if (DEBUG) Debug.Log("No surface was hit");
            return;
        }
        Vector3 placePoint = shotPoint;

        PortalHitInfo phitInfo = CheckPortalPlacement(placePoint, -hit.normal, shot);
        if (!phitInfo.portalable) {
            if (DEBUG) Debug.Log("No portalable surface was hit");

            var missedParticle = Instantiate(shotMissedParticlePrefab, placePoint, Quaternion.LookRotation(hit.normal));
            var missedParticleRenderer = missedParticle.GetComponent<ParticleSystemRenderer>();
            missedParticleRenderer.material.SetColor("_EmissionColor", portalColor * portalColor);
            missedParticleRenderer.material.color = portalColor;

            return;
        }

        if (DEBUG) Debug.Log("Portal placement successful, placing a portal.");

        SpeedrunValues.portalCount++;

        Vector3 portalPos = phitInfo.origin;
        if (GetPortalID(hit.collider.gameObject) == shot)
        {
            // we're shooting on top of the same portal - snap new position view-based z to the old one
            float viewZDelta = Vector3.Dot( portalPos - hit.point, hit.normal);
            portalPos -= hit.normal * viewZDelta;
        }
        
        Quaternion portalRot = Quaternion.LookRotation(hit.normal, Vector3.up);

        if (shot == 1) {
            if (_portal1 == null) {
                _portal1 = CreatePortal(portalPos, portalRot, portal1Color);
            } else {
                _portal1.Place(portalPos, portalRot);
            }
            _portal1.portalID = shot;
        } else if (shot == 2) {
            if (_portal2 == null) {
                _portal2 = CreatePortal(portalPos, portalRot, portal2Color);
            } else {
                _portal2.Place(portalPos, portalRot);
            }
            _portal2.portalID = shot;
        }

        // fix portal linkage
        if(_portal1 && _portal2) {
            _portal1.SetLinkedPortal(_portal2);
            _portal2.SetLinkedPortal(_portal1);
        }
    }

    PortalHitInfo CheckPortalPlacement(Vector3 origin, Vector3 shootDir, int portalID, int iteration = 0) {
        PortalHitInfo hitInfo;
        hitInfo.portalable = false;
        hitInfo.origin = origin;

        if (iteration > MAX_BUMP_ITERATIONS) return hitInfo;

        if (DEBUG) Debug.Log($"Checking surface on iteration {iteration}");

        RaycastHit hit;

        Vector3 checkOrigin = origin - shootDir * MAX_SURFACE_DIST;
        
        // checking if there is a portalable surface behind
        bool isSurface = Physics.Raycast(checkOrigin, shootDir, out hit, MAX_SURFACE_DIST * 2f, portalBlockMask);
        if (!isSurface || !IsPortalable(hit.collider.gameObject, portalID)) {
            if (DEBUG) Debug.Log($"Origin point wouldn't hit the surface!");
            return hitInfo;
        }

        // checking for edges
        int[] hD = { -1, 1, 0, 0, -1, -1, 1, 1 };
        int[] vD = { 0, 0, -1, 1, -1, 1, -1, 1 };
        for (int d = 0; d < 8; d++) {
            Vector3 checkDir = Vector3.up * (PORTAL_HEIGHT * 0.5f * vD[d]);
            checkDir += Vector3.Cross(Vector3.up, shootDir) * (PORTAL_WIDTH * 0.5f * hD[d]);

            // solid walls
            bool blockedByWall = Physics.Raycast(checkOrigin, checkDir.normalized, out hit, checkDir.magnitude, portalBlockMask);
            if (blockedByWall && GetPortalID(hit.collider.gameObject)!=portalID) {
                if (DEBUG) Debug.Log($"Blocked by wall {d}!");
                // check portal again, away from the wall
                Vector3 bumpedOrigin = origin + checkDir.normalized * (hit.distance - (checkDir.magnitude + BUMP_DISTANCE));
                return CheckPortalPlacement(bumpedOrigin, shootDir, portalID, iteration + 1);
            }

            // empty gaps
            float edgeDist = checkDir.magnitude;
            while(edgeDist > 0) {
                Vector3 edgeOrigin = checkOrigin + checkDir.normalized * edgeDist;
                bool hitSurface = Physics.Raycast(edgeOrigin, shootDir, out hit, MAX_SURFACE_DIST * 2f, portalBlockMask);
                if (hitSurface && IsPortalable(hit.collider.gameObject, portalID)) {
                    break;
                }
                edgeDist -= SURFACE_CHECK_STEP;
            }
            if (edgeDist < checkDir.magnitude) {
                if (DEBUG) Debug.Log($"Blocked by edge {d}!");
                // check portal again, away from the edge
                Vector3 bumpedOrigin = origin + checkDir.normalized * (edgeDist - (checkDir.magnitude + SURFACE_CHECK_STEP));
                return CheckPortalPlacement(bumpedOrigin, shootDir, portalID, iteration + 1);
            }
        }

        hitInfo.portalable = true;
        return hitInfo;
    }

    int GetPortalID(GameObject go) {
        var portal = go.GetComponent<Portal>();
        if (portal) return portal.portalID;
        else return 0;
    }

    bool IsPortalable(GameObject go, int portalID) {
        if (GetPortalID(go) == portalID) return true;
        var meshRend = go.GetComponent<MeshRenderer>();
        if (meshRend && !meshRend.materials[0].name.Contains("white")) return false;
        return ((portalableMask & (1 << go.layer)) != 0);
    }

    Portal CreatePortal(Vector3 position, Quaternion angles, Color c) {
        GameObject portalGo = Instantiate(portalPrefab, position, angles);
        Portal portal = portalGo.GetComponent<Portal>();
        portal.gameObject.name = "Shot Portal";
        portal.portalColor = c;
        return portal;
    }
    
    public Portal GetPortal(int portalID) {
        if (portalID == 1) return _portal1;
        if (portalID == 2) return _portal2;
        return null;
    }

    //"blue", "orange", "aqua", "red", "yellow", "green", "lime", "pink", "purple", "brown"
    Dictionary<string, Color> PortalColorsDict = new Dictionary<string, Color>() {
        {"blue",    new Color(0.0f,0.5f,1.0f)},
        {"orange",  new Color(0.7f,0.5f,0.2f)},
        {"aqua",    new Color(0.0f,0.6f,0.6f)},
        {"red",     new Color(0.8f,0.4f,0.3f)},
        {"yellow",  new Color(0.7f,0.6f,0.0f)},
        {"green",   new Color(0.3f,0.6f,0.3f)},
        {"lime",    new Color(0.4f,0.6f,0.2f)},
        {"pink",    new Color(0.6f,0.5f,0.6f)},
        {"purple",  new Color(0.5f,0.4f,0.7f)},
        {"brown",   new Color(0.5f,0.45f,0.4f)},
    };
    public void UpdateSettingsColors() {
        string p1str = PlayerPrefs.GetString("setting_bluecolor");
        string p2str = PlayerPrefs.GetString("setting_orangecolor");

        if (!PortalColorsDict.ContainsKey(p1str)) {
            p1str = "blue";
        }
        if (!PortalColorsDict.ContainsKey(p2str)) {
            p2str = "orange";
        }

        portal1Color = PortalColorsDict[p1str];
        portal2Color = PortalColorsDict[p2str];

        if (_portal1) {
            _portal1.GetComponent<Portal>().portalColor = portal1Color;
        }
        if (_portal2) {
            _portal2.GetComponent<Portal>().portalColor = portal2Color;
        }
    }
    
    public Color GetPortalColor(int portalID) {
        if (portalID == 1) return portal1Color;
        if (portalID == 2) return portal2Color;
        return Color.white;
    }
}
