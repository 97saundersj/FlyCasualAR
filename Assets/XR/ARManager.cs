using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

namespace AR
{
    public class ARManager : MonoBehaviour
    {
        public bool placementConfirmed = false;
        public GameObject objectToMove;
        public ARRaycastManager raycastManager;
        private GameObject ARCursorPrefab;
        private GameObject ARCursorObject;

        void Start()
        {
            ARCursorPrefab = ARCursorPrefab = Resources.Load<GameObject>("Prefabs/XR/ARCursor");
        }

        void Update()
        {
            if (DebugManager.AugmentedReality && !placementConfirmed)
            {
                UpdateCursor();

                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId) && Input.GetTouch(0).phase == TouchPhase.Began))
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
                    ARCursorObject.transform.rotation = hits[0].pose.rotation;
                }
            }
        }

        void PlaceObject()
        {
            // Move object
            objectToMove.SetActive(true);
            //objectToMove.transform.parent = ARCursorObject.transform.parent;
            objectToMove.transform.position = ARCursorObject.transform.position;
            objectToMove.transform.rotation = ARCursorObject.transform.rotation;
            //GameObject.Find("XR Origin (Mobile AR)").GetComponent<XROrigin>().MakeContentAppearAt(objectToMove.transform, ARCursorObject.transform.position, ARCursorObject.transform.rotation);

            //Enable Confirm Button
            GameObject.Find("UI")?.transform.Find("ARPanel")?.gameObject.SetActive(true);
        }

        public void SetPlacementConfirmed(bool placementSet)
        {
            placementConfirmed = placementSet;
            if (placementConfirmed)
            {
                ARCursorPrefab.SetActive(false);
            }
        }
    }
}
