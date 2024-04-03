using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

namespace AR
{
    public class XRBoardPlacer : MonoBehaviour
    {
        public bool placementConfirmed = false;
        public GameObject objectToMove;
        public ARRaycastManager raycastManager;
        private GameObject ARCursorPrefab;
        private GameObject ARCursorObject;

        void Start()
        {
            ARCursorPrefab = Resources.Load<GameObject>("Prefabs/XR/ARCursor");

            // Move the object to the new position far away
            objectToMove.transform.position = new Vector3(1000f, 1000f, 1000f);
        }

        void Update()
        {
            if (DebugManager.AugmentedReality && !placementConfirmed)
            {
                UpdateCursor();

                if (ARCursorObject != null && (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId) && Input.GetTouch(0).phase == TouchPhase.Began)))
                {
                    PlaceObject();
                }

                var decisionPanel = GameObject.Find("UI/DecisionPanelHolder")?.transform.Find("DecisionsPanel");
                decisionPanel?.gameObject.SetActive(false);
            }
        }

        void UpdateCursor()
        {
            Vector2 screenPosition = Camera.main.ViewportToScreenPoint(new Vector2(0.5f, 0.5f));
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            raycastManager.Raycast(screenPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.AllTypes);


            if (hits.Count > 0)
            {
                if (ARCursorObject == null)
                {
                    ARCursorObject = Instantiate(ARCursorPrefab, hits[0].pose.position, hits[0].pose.rotation, hits[0].trackable.transform.parent);
                }
                else
                {
                    ARCursorObject.transform.position = hits[0].pose.position;

                    // Calculate rotation based on player's orientation
                    Vector3 playerForward = Camera.main.transform.forward;
                    playerForward.y = 0f; // ensure the rotation is only in the horizontal plane

                    ARCursorObject.transform.rotation = Quaternion.LookRotation(playerForward);
                }
            }
        }

        void PlaceObject()
        {
            // Move object
            objectToMove.transform.position = new Vector3(0f, 0f, 0f);
            
            // Moving the object doesn't work, so move the world around the object
            var xrOrigin = GameObject.Find("XR Origin (Mobile AR)");

            xrOrigin.transform.localPosition = Vector3.zero;
            xrOrigin.transform.localRotation = Quaternion.identity;
            xrOrigin.GetComponent<XROrigin>().MakeContentAppearAt(objectToMove.transform, ARCursorObject.transform.position, ARCursorObject.transform.rotation);

            //Enable Confirm Button
            GameObject.Find("UI")?.transform.Find("ARPanel")?.gameObject.SetActive(true);
        }

        public void SetPlacementConfirmed(bool placementSet)
        {
            placementConfirmed = placementSet;
            if (placementConfirmed)
            {
                ARCursorObject.SetActive(false);
            }
        }
    }
}
