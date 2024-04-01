using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

namespace AR
{
    public class ARCursor : MonoBehaviour
    {
        public GameObject objectToMove;
        public ARRaycastManager raycastManager;
    
        // Update is called once per frame
        void Update()
        {
            if(!DebugManager.AugmentedReality)
            {
                return;
            }
    
            UpdateCursor();
            
            if(Input.touchCount > 0 && !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId) && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                PlaceObject();
            }
        }
    
        void UpdateCursor()
        {
            Vector2 screenPosition = Camera.main.ViewportToScreenPoint(new Vector2(0.5f, 0.5f));
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            raycastManager.Raycast(screenPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.Planes);
    
            if(hits.Count > 0)
            {
                transform.position = hits[0].pose.position;
                transform.rotation = hits[0].pose.rotation;
            }
        }
    
        void PlaceObject()
        {
            objectToMove.SetActive(true);
            //GameObject.Find("CameraHolder/ARSessionOrigin").GetComponent<ARSessionOrigin>().MakeContentAppearAt(objectToMove.transform, transform.position, transform.rotation);
            GameObject.Find("XR Origin").GetComponent<XROrigin>().MakeContentAppearAt(objectToMove.transform, transform.position, transform.rotation);
            GameObject.Find("UI").transform.Find("ARPanel").gameObject.SetActive(true);
        }
    }
}