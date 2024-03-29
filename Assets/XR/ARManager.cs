using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;

namespace AR
{
    public class ARManager : MonoBehaviour
    {
        public bool placementConfirmed = false;

        public GameObject ARCursor;
        public GameObject objectToMove;
        public ARRaycastManager raycastManager;

        void Update()
        {
            if (DebugManager.AugmentedReality && !placementConfirmed)
            {
                UpdateCursor();

                if (Input.touchCount > 0 && !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId) && Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    PlaceObject();
                }

                var DecisionPanel = GameObject.Find("UI/DecisionPanelHolder").transform.Find("DecisionsPanel");
                if (DecisionPanel)
                {
                    DecisionPanel.gameObject.SetActive(false);
                }
            }
        }

        void UpdateCursor()
        {
            Vector2 screenPosition = Camera.main.ViewportToScreenPoint(new Vector2(0.5f, 0.5f));
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            raycastManager.Raycast(screenPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.Planes);

            if (hits.Count > 0)
            {
                ARCursor.SetActive(true);
                ARCursor.transform.position = hits[0].pose.position;
                ARCursor.transform.rotation = hits[0].pose.rotation;
            }
            else
            {
                ARCursor.SetActive(false);
            }
        }

        void PlaceObject()
        {
            // Move object
            objectToMove.SetActive(true);
            GameObject.Find("ARSessionOrigin").GetComponent<ARSessionOrigin>().MakeContentAppearAt(objectToMove.transform, ARCursor.transform.position, ARCursor.transform.rotation);

            //Enable Confirm Button
            GameObject.Find("UI").transform.Find("ARPanel").gameObject.SetActive(true);
        }

        public void SetPlacementConfirmed(bool placementSet)
        {
            placementConfirmed = placementSet;
            if (placementConfirmed)
            {
                ARCursor.SetActive(false);
            }
        }
    }
}
