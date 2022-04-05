using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour
{

    public LayerMask hitMask;

    const float MAX_DIST = 128.0f;
    
    float length = 0;
    GameObject hitObject;

    GameObject visual;
    GameObject particles;

    bool portalled = false;
    GameObject portalledLaser;

    void Start() {
        visual = transform.Find("Visual").gameObject;
        particles = transform.Find("Particle").gameObject;

        particles.SetActive(true);
        var angles = particles.transform.localEulerAngles;
        particles.transform.parent = null;
        particles.transform.rotation = Quaternion.Euler(angles);
    }

    void Update()
    {
        if (hitObject && hitObject.GetComponent<Portal>() && !portalledLaser) {
            hitObject = null;
        }


        RaycastHit hit;
        bool didLaserHit = Physics.Raycast(transform.position, transform.forward, out hit, MAX_DIST, hitMask);
        
        
        if(hitObject && (!didLaserHit || hit.collider.gameObject != hitObject)) {
            RemoveLaserTarget();

            particles.SetActive(true);
        }

        if (!didLaserHit) {
            length = MAX_DIST;
            hitObject = null;
        } else {
            length = hit.distance;

            if (!hitObject) {
                SetLaserTarget(hit.collider.gameObject);
            }
        }

        visual.transform.localScale = new Vector3(1,1, length);
        if (didLaserHit) {
            particles.transform.position = hit.point + hit.normal * 0.01f;
        }

        if (portalledLaser && didLaserHit) {
            var portal = hit.collider.gameObject.GetComponent<Portal>();
            portalledLaser.transform.position = portal.GetPortalledPosition(hit.point - hit.normal * 0.01f);
            portalledLaser.transform.rotation = portal.GetPortalledRotation(transform.rotation);
        }
        
    }

    void RemoveLaserTarget() {
        if (hitObject) {
            var laserEmitter = hitObject.GetComponent<LaserEmitter>();
            if (laserEmitter) {
                laserEmitter.RemoveRelaySource();
            }

            var portal = hitObject.GetComponent<Portal>();
            if (portal && portalledLaser) {
                portalledLaser.SetActive(false);
                Destroy(portalledLaser);
                portalledLaser = null;
            }

            var laserReceiver = hitObject.GetComponent<LaserReceiver>();
            if (laserReceiver) {
                laserReceiver.LaserUnpower();
            }
        }

        hitObject = null;
    }


    void SetLaserTarget(GameObject obj) {
        var emitter = obj.GetComponent<LaserEmitter>();
        if (emitter && emitter.isRelay) {
            emitter.AddRelaySource();
            particles.SetActive(false);
        }

        var portal = obj.GetComponent<Portal>();
        if (portal && portal.linkedPortal) {
            particles.SetActive(false);
            if (!portalled) {
                portalledLaser = Instantiate((GameObject)Resources.Load("Laser"));
                portalledLaser.GetComponent<Laser>().MarkPortalled();
            }
        }

        var laserReceiver = obj.GetComponent<LaserReceiver>();
        if (laserReceiver) {
            laserReceiver.LaserPower();
            particles.SetActive(false);
        }

        hitObject = obj;
    }


    void OnDestroy() {
        RemoveLaserTarget();
        if (particles) {
            particles.GetComponent<ParticleSystem>().Stop();
        }
        Destroy(particles, 1);
    }

    public void MarkPortalled() {
        portalled = true;
    }
}
