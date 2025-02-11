﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/*
 * DESCRIPTION
 * Movement, vertical rotation and zoom of main camera.
 * Movement, vertical rotation and zoom have MIN and MAX values.
 * Event when view is changed.
*/

public enum CameraModes
{
    Free,
    TopDown
}

public class CameraState
{
    public Vector3 CameraPosition { get; private set; }
    public Quaternion CameraAngles { get; private set; }
    public Vector3 CameraHolderPosition { get; private set; }
    public Quaternion CameraHolderAngles { get; private set; }
    public Vector3 LookAtPosition { get; private set; }
    public CameraModes CameraMode { get; private set; }

    public CameraState(Vector3 cameraPosition, Quaternion cameraAngles, Vector3 cameraHolderPosition, Quaternion cameraHolderAngles, CameraModes cameraMode, Vector3 lookAtPosition = default)
    {
        CameraPosition = cameraPosition;
        CameraAngles = cameraAngles;
        CameraHolderPosition = cameraHolderPosition;
        CameraHolderAngles = cameraHolderAngles;
        CameraMode = cameraMode;
        LookAtPosition = (lookAtPosition == default) ? Vector3.zero : lookAtPosition;
    }
}

public class CameraScript : MonoBehaviour {

    private static Transform Camera;
    private static Transform CameraHolder;
    private static Transform VirtualCamera;
    private static Transform VirtualCameraHolder;

    public static bool IsCinematic { get; private set; }
    private static CameraState SavedCameraState; // Position of camera before all dynamic movement
    private static CameraState OldCameraState; // Position of camera before current dynamic movement
    private static CameraState NewCameraState;
    private static float TransitionTimeCounter;
    private static readonly float TRANSITION_TIME_SPEED = 0.5f;

    private const float SENSITIVITY_MOVE = 0.125f;
    private const float SENSITIVITY_TURN = 5;
    private const float SENSITIVITY_ZOOM = 5;
    private const float MOUSE_MOVE_START_OFFSET = 2f;
    private const float BORDER_SQUARE = 18f;
    private const float MAX_HEIGHT = 8f;
    private const float MIN_HEIGHT = 1.5f;
    private const float MAX_ROTATION = 89.99f;
    private const float MIN_ROTATION = 314.99f;

    // Constants for touch controls
    private const float SENSITIVITY_TOUCH_MOVE = 0.010f;
    private const float SENSITIVITY_TOUCH_MOVE_ZOOMED_IN = SENSITIVITY_TOUCH_MOVE / 25f;
    private const float SENSITIVITY_TOUCH_TURN = 0.125f;
    private const float SENSITIVITY_TOUCH_ZOOM = 0.0375f;
    //TODO: need to scale any of the thresholds by DPI? (zoom may already account for that, but the rest?)
    private const float THRESHOLD_TOUCH_MOVE_MOMENTUM = 10f;
    private const float THRESHOLD_TOUCH_TURN = 0.05f;
    private const float THRESHOLD_TOUCH_TURN_SWITCH = 40f;
    private const float THRESHOLD_TOUCH_TURN_START = 20f;
    private const float THRESHOLD_TOUCH_ZOOM = 0.06f;
    private const float THRESHOLD_TOUCH_ZOOM_SWITCH = 30f;
    private const float THRESHOLD_TOUCH_ZOOM_START = 20f;
    private const float FRICTION_TOUCH_MOVE_MOMENTUM = 0.05f;
    private const float MOMENTUM_THRESHOLD = 1600f; 
    private const float MOMENTUM_MINIMUM = 0.25f;

    // State for touch controls
    private float initialPinchMagnitude = 0f; // Magnitude of the pinch when 2 fingers are first put on the screen
    private float lastProcessedPinchMagnitude = 0f; // Magnitude of the pinch when we last actually zoomed
    private Vector2 initialRotateCenter = new Vector2(0.0f, 0.0f);
    private Vector2 lastProcessedRotateCenter = new Vector2(0.0f, 0.0f);
    private Vector2 panningMomentum = new Vector2(0.0f, 0.0f);
    private float totalTouchMoveDuration = 0f;
    private Vector2 totalTouchMove = new Vector2(0.0f, 0.0f);

    public static bool InputAxisAreEnabled = true;
    static bool _inputMouseIsEnabled = true;
    public static bool InputMouseIsEnabled
    {
        get { return _inputMouseIsEnabled; }
        set {
            _inputMouseIsEnabled = value;

            // Mouse and touch are exclusive
            if (_inputMouseIsEnabled) {
                _inputTouchIsEnabled = false;
            }
        }
    }
    static bool _inputTouchIsEnabled = false;
    public static bool InputTouchIsEnabled
    {
        get { return _inputTouchIsEnabled; }
        set
        {
            _inputTouchIsEnabled = value;

            // Mouse and touch are exclusive
            if (_inputTouchIsEnabled)
            {
                _inputMouseIsEnabled = false;
            }
        }
    }
    public static bool TouchInputsPaused = false;

    private static CameraModes cameraMode = CameraModes.Free;

    // Use this for initialization
    void Start()
    {
        Camera = transform.Find("Main Camera");
        CameraHolder = transform;

        VirtualCameraHolder = GameObject.Find("VirtualCameraHolder").transform;
        VirtualCamera = VirtualCameraHolder.Find("VirtualCamera");

        ChangeMode(CameraModes.Free);
        IsCinematic = false;
        SetDefaultCameraPosition();

        InputTouchIsEnabled = Input.touchSupported && !Input.mousePresent;
        UI.UpdateControlsButtonName();
    }

    private static void SetDefaultCameraPosition()
    {
        bool isSecondPlayer = (Network.IsNetworkGame && !Network.IsServer);

        Camera camera = Camera.GetComponent<Camera>();
        camera.orthographicSize = 6;

        Camera.localEulerAngles = (cameraMode == CameraModes.Free) ? new Vector3(-50, 0, 0) : new Vector3(0, 0, 0);
        CameraHolder.localEulerAngles = new Vector3(90, 0, (!isSecondPlayer) ? 0 : 180);
        CameraHolder.localPosition = (cameraMode == CameraModes.Free) ? new Vector3(0, 6, (!isSecondPlayer) ? -9 : 9) : new Vector3(0, 0, (!isSecondPlayer) ? 0.85f: -0.85f);
    }

    // Update is called once per frame
    void Update()
    {
        //Don't update Main Camera if Augmented Reality is enabled
        if (DebugManager.AugmentedReality)
        {
            return;
        }

        if (IsCinematic)
        {
            DoCameraTransition(OldCameraState, NewCameraState);
            return;
        }

        //TODO: Call hide context menu only once
        CheckChangeMode();

        //Don't move camera while "Select a maneuver" window is shown - fixes problem with touch input
        if (!DirectionsMenu.IsVisible)
        {
            if (InputTouchIsEnabled)
            {
                CamRotateZoomByTouch();
                CamMoveByTouch();
            }

            if (InputMouseIsEnabled)
            {
                CamMoveByMouse();
                CamZoomByMouseScroll();
                CamRotateByMouse();
            }

            if (InputAxisAreEnabled)
            {
                CamMoveByAxis();
            }
        }

        CamClampPosition();
    }

    // CAMERA MODES

    private void CheckChangeMode()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && !Console.IsActive) ToggleMode();
    }

    public static void ToggleMode()
    {
        if (IsCinematic) return;

        ChangeMode((cameraMode == CameraModes.Free) ? CameraModes.TopDown : CameraModes.Free);
        SetDefaultCameraPosition();
    }

    private static void ChangeMode(CameraModes mode)
    {
        cameraMode = mode;

        Camera camera = Camera.GetComponent<Camera>();
        camera.orthographic = (mode == CameraModes.Free) ? false : true;
    }

    // Movement, Rotation, Zoom

    private void CamMoveByAxis()
    {
        if (Console.IsActive || Input.GetKey(KeyCode.LeftControl)) return;
        float runScale = (Input.GetKey(KeyCode.LeftShift)) ? 3 : 1;

        float x = GetXMovement();
        if (x != 0) x = x * SENSITIVITY_MOVE * runScale;

        float y = GetYMovement();
        if (y != 0) y = y * SENSITIVITY_MOVE * runScale;

        if ((x != 0) || (y != 0)) WhenViewChanged();
        transform.Translate (x, y, 0);
	}

    private float GetXMovement()
    {
        float result = 0f;
        if (DebugManager.AlternativeCameraControls)
        {
            result += Mathf.Clamp(Input.GetAxis("Horizontal"), -1f, 1f);
        }
        else
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) result += -1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) result += 1f;
        }
        return result;
    }

    private float GetYMovement()
    {
        float result = 0f;
        if (DebugManager.AlternativeCameraControls)
        {
            result += Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 1f);
        }
        else
        {
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) result += -1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) result += 1f;
        }
        return result;
    }


    private void CamMoveByMouse()
    {
        float x = 0;
        if (Input.mousePosition.x < MOUSE_MOVE_START_OFFSET && Input.mousePosition.x >= 0) x = -1f * SENSITIVITY_MOVE;
        else if (Input.mousePosition.x > Screen.width - MOUSE_MOVE_START_OFFSET && Input.mousePosition.x <= Screen.width) x = 1f * SENSITIVITY_MOVE;

        float y = 0;
        if (Input.mousePosition.y < MOUSE_MOVE_START_OFFSET && Input.mousePosition.y >= 0) y = -1f * SENSITIVITY_MOVE;
        else if (Input.mousePosition.y > Screen.height - MOUSE_MOVE_START_OFFSET && Input.mousePosition.y <= Screen.height) y = 1f * SENSITIVITY_MOVE;

        if ((x != 0) || (y != 0)) WhenViewChanged();
        transform.Translate(x, y, 0);
    }

    private void CamZoomByMouseScroll()
    {
		float zoom = Input.GetAxis ("Mouse ScrollWheel") * SENSITIVITY_ZOOM;
		if (zoom != 0)
        {
            ZoomByFactor(zoom);
        }	
	}

    private void ZoomByFactor(float zoom) {
        if (cameraMode == CameraModes.Free)
        {
            Vector3 newPosition = transform.position + (Camera.TransformDirection(0, 0, zoom));
            float zoomClampRate = 1;
            if (newPosition.y <= MIN_HEIGHT)
            {
                zoomClampRate = (transform.position.y - MIN_HEIGHT) / zoom;
            }
            if (newPosition.y >= MAX_HEIGHT)
            {
                zoomClampRate = (transform.position.y - MAX_HEIGHT) / zoom;
            }
            transform.Translate(transform.InverseTransformDirection(Camera.TransformDirection(0, 0, zoom * zoomClampRate)));
        }
        else
        {
            Camera camera = Camera.GetComponent<Camera>();
            camera.orthographicSize -= zoom;
            camera.orthographicSize = Mathf.Clamp(camera.orthographicSize, 1, 6);
        }

        WhenViewChanged();
    }

	private void CamRotateByMouse()
    {
        if (cameraMode == CameraModes.Free)
        {
            if (Input.GetKey(KeyCode.Mouse1) || Input.GetKey(KeyCode.Mouse2))
            {

                float turnX = Input.GetAxis("Mouse Y") * -SENSITIVITY_TURN;
                turnX = CamClampRotation(turnX);
                Camera.Rotate(turnX, 0, 0);

                float turnY = Input.GetAxis("Mouse X") * -SENSITIVITY_TURN;
                transform.Rotate(0, 0, turnY);

                if ((turnX != 0) || (turnY != 0)) WhenViewChanged();
            }
        }
	}

    // Pinch zoom, two finger rotate for touch controls

    void CamRotateZoomByTouch()
    {
        if (Input.touchCount > 0 && (Input.GetTouch(0).position.x > Screen.width ||
                                     Input.GetTouch(0).position.y > Screen.height ||
                                     TouchInputsPaused))
        {
            // Don't listen to touches that are off-screen, or being handled elsewhere
            return;
        }

        // If there are two touches on the device
        if (Input.touchCount == 2 && 
            (Input.GetTouch(0).phase == TouchPhase.Moved ||
             Input.GetTouch(1).phase == TouchPhase.Moved))
        {
            // Store both touches
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Find the magnitude of the vector (the distance) between the touches in each frame.
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // Normalize for DPI
            touchDeltaMag = touchDeltaMag / Screen.dpi * 265;

            // Initialize values when 2 fingers are first touched to the screen
            if (initialPinchMagnitude == 0f)
            {
                initialPinchMagnitude = touchDeltaMag;
                lastProcessedPinchMagnitude = touchDeltaMag;
            }

            float startThreshold = 0;

            if (initialRotateCenter != lastProcessedRotateCenter)
            {
                // A pinch is in progress
                startThreshold = THRESHOLD_TOUCH_ZOOM_SWITCH;
            }
            else if (initialPinchMagnitude == lastProcessedPinchMagnitude)
            {
                // A zoom is not yet in progress
                startThreshold = THRESHOLD_TOUCH_ZOOM_START;
            }

            // Try to pinch zoom if we pass a start threshold
            if (Mathf.Abs(initialPinchMagnitude - touchDeltaMag) > startThreshold)
            {
                // Find the difference in the distances between each frame.
                float deltaMagnitudeDiff = lastProcessedPinchMagnitude - touchDeltaMag;

                if (startThreshold != 0)
                {
                    deltaMagnitudeDiff = (Mathf.Abs(deltaMagnitudeDiff) - startThreshold) * Mathf.Sign(deltaMagnitudeDiff);
                }

                if (Mathf.Abs(deltaMagnitudeDiff) > THRESHOLD_TOUCH_ZOOM)
                {
                    float zoom = deltaMagnitudeDiff * -SENSITIVITY_TOUCH_ZOOM;
                    ZoomByFactor(zoom);

                    lastProcessedPinchMagnitude = touchDeltaMag;

                    // Turn off rotate for now
                    initialRotateCenter = lastProcessedRotateCenter;
                }
            }

            // Try to rotate by dragging two fingers
            if (cameraMode == CameraModes.Free)
            {
                // Find the difference between the average of the positions
                Vector2 centerPos = Vector2.Lerp(touchZero.position, touchOne.position, 0.5f);

                if (initialRotateCenter.magnitude == 0)
                {
                    initialRotateCenter = centerPos;
                    lastProcessedRotateCenter = centerPos;
                }

                startThreshold = 0f;
                if (initialPinchMagnitude != lastProcessedPinchMagnitude)
                {
                    // A pinch is in progress
                    startThreshold = THRESHOLD_TOUCH_TURN_SWITCH;
                }
                else if (initialRotateCenter == lastProcessedRotateCenter)
                {
                    // A zoom is not yet in progress
                    startThreshold = THRESHOLD_TOUCH_TURN_START;
                }

                // If we pass a start threshold, try the rotation
                if (Mathf.Abs((initialRotateCenter - centerPos).magnitude) > startThreshold)
                {
                    Vector2 deltaCenterPos = centerPos - lastProcessedRotateCenter;

                    if (startThreshold != 0)
                    {
                        deltaCenterPos = deltaCenterPos - Vector2.ClampMagnitude(deltaCenterPos, startThreshold);
                    }

                    if (Mathf.Abs(deltaCenterPos.magnitude) > THRESHOLD_TOUCH_TURN)
                    {
                        // Rotate!
                        float turnX = deltaCenterPos.y * -SENSITIVITY_TOUCH_TURN;
                        turnX = CamClampRotation(turnX);
                        Camera.Rotate(turnX, 0, 0);

                        float turnY = deltaCenterPos.x * -SENSITIVITY_TOUCH_TURN;
                        transform.Rotate(0, 0, turnY);

                        if ((turnX != 0) || (turnY != 0)) WhenViewChanged();

                        lastProcessedRotateCenter = centerPos;

                        // Turn off zooming until it passes it's start threshold again
                        initialPinchMagnitude = lastProcessedPinchMagnitude;
                    }
                }
            }
        }

        // When gestures aren't in progress, reset values used to track them
        if (Input.touchCount < 2)
        {
            initialPinchMagnitude = 0f;
            lastProcessedPinchMagnitude = 0f;

            initialRotateCenter = Vector2.zero;
            lastProcessedRotateCenter = initialRotateCenter;
        }
    }

    // One finger pan for touch controls

    void CamMoveByTouch()
    {
        if (Input.touchCount > 0 && (Input.GetTouch(0).position.x > Screen.width ||
                                     Input.GetTouch(0).position.y > Screen.height ||
                                     TouchInputsPaused ||
                                     (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId) && totalTouchMoveDuration == 0f)))
        {
            // Don't listen to touches that are off-screen, or being handled elsewhere
            return;
        }

        if (Input.touchCount > 1) {
            // Stop momentum as soon as a second finger is touched to the screen
            panningMomentum = Vector2.zero;
        }
        if (Input.touchCount == 1) 
        {
            // Note: in 2D mode we could also do this when 2 fingers are down (and thus a zoom is happening), since rotates can't also happen in 2D mode

            if (Input.GetTouch(0).phase == TouchPhase.Began)
            {
                // Stop momentum as soon as one finger is touched to the screen
                panningMomentum = Vector2.zero;

                // Setup to start a new move
                totalTouchMoveDuration = 0f;
                totalTouchMove = Vector2.zero;
            }
            else
            {
                // Adjust sensitivity based on zoom level so the view always moves with your finger
                // That means the view moves more when zoomed out than when zoomed in for the same physical movement
                // TODO: could be better to do this by just figuring out the coordinates in world space the current position and last position of the finger represent, using the vector between them? Would probably require an invisible plane just to use for this raycast though
                float moveSensitivityForCurrentZoom = SENSITIVITY_TOUCH_MOVE;
                float zoomPercent = 1;
                if (cameraMode == CameraModes.Free)
                {
                    // +1 to numerator and denominator so it never goes to 0. +1 again to denominator to make Free / TopDown match more 
                    // (since TopDown doesn't go all the way to it's theoretical max zoom out for some reason)
                    zoomPercent = (transform.position.y - MIN_HEIGHT + 1) / (MAX_HEIGHT - MIN_HEIGHT + 1 + 1);

                }
                else if (cameraMode == CameraModes.TopDown)
                {
                    // +1 to numerator and denominator so it never goes to 0
                    zoomPercent = (Camera.GetComponent<Camera>().orthographicSize - 1 + 1) / (6 + 1);
                }
                moveSensitivityForCurrentZoom = Mathf.Min(SENSITIVITY_TOUCH_MOVE,
                                            Mathf.Lerp(SENSITIVITY_TOUCH_MOVE_ZOOMED_IN,
                                                       SENSITIVITY_TOUCH_MOVE,
                                                       zoomPercent));

                if (Input.GetTouch(0).phase == TouchPhase.Moved)
                {
                    Vector2 deltaPosition = Input.GetTouch(0).deltaPosition;
                    deltaPosition = deltaPosition * -moveSensitivityForCurrentZoom;

                    // Add momentum
                    totalTouchMove += deltaPosition;
                    // TODO: Adjust how momentum works to help make momentum happen more a bit more easily when expected / desired while still being unlikely to happen when not expected / desired.
                        // This may be sufficient to do that?: Use a moving average (or a window?) to more heavily weigh the speed of movements towards the end of the gesture, or at the very end. Then lower the MOMENTUM_THRESHOLD!

                    // Move camera
                    float x = deltaPosition.x;
                    float y = deltaPosition.y;

                    if ((x != 0) || (y != 0)) WhenViewChanged();
                    transform.Translate(x, y, 0);
                }

                // Keep incrementing duration while 1 finger is down even if no movement is happening
                totalTouchMoveDuration += Time.deltaTime;

                if (totalTouchMove.magnitude / totalTouchMoveDuration > MOMENTUM_THRESHOLD * moveSensitivityForCurrentZoom)
                {
                    panningMomentum = totalTouchMove / totalTouchMoveDuration;
                }
                else
                {
                    panningMomentum = Vector2.zero;
                }
            }

        }
        else if (Input.touchCount == 0 && panningMomentum.magnitude > MOMENTUM_MINIMUM)
        {
            // Keep panning with momentum
            panningMomentum *= Mathf.Pow(FRICTION_TOUCH_MOVE_MOMENTUM, Time.deltaTime);

            float x = panningMomentum.x * Time.deltaTime;
            float y = panningMomentum.y * Time.deltaTime;

            if ((x != 0) || (y != 0)) WhenViewChanged();
            transform.Translate(x, y, 0);

        }
    }


    // Restrictions for movement and rotation 

    private float CamClampRotation(float turnX)
    {
        float currentTurnX = Camera.eulerAngles.x;
        float newTurnX = turnX + currentTurnX;
        
        if (newTurnX > MAX_ROTATION && newTurnX < 180)
        {
            turnX = MAX_ROTATION - currentTurnX;
        }
        else if (newTurnX < MIN_ROTATION && newTurnX > 180)
        {
            turnX = currentTurnX - MIN_ROTATION;
        }
        return turnX;
    }

    private void CamClampPosition()
    {
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, -BORDER_SQUARE, BORDER_SQUARE),
            Mathf.Clamp(transform.position.y, MIN_HEIGHT, MAX_HEIGHT),
            Mathf.Clamp(transform.position.z, -BORDER_SQUARE, BORDER_SQUARE)
        );
    }

    // What to do when view is changed

    private void WhenViewChanged()
    {
        UI.HideTemporaryMenus();
    }

    // Cinematic camera

    public static void SetPosition(Vector3 position, Vector3 direction)
    {
        CameraHolder.transform.position = position;
        Camera.transform.LookAt(direction, Vector3.up);
    }

    public static void AnimateChangePosition(Vector3 position, Transform directionTransform)
    {
        SetOldCameraPosition();
        if (!IsCinematic)
        {
            SetSavedCameraPosition();
            IsCinematic = true;
        }

        VirtualCameraHolder.position = position;
        VirtualCamera.transform.LookAt(directionTransform);
        NewCameraState = new CameraState(VirtualCamera.localPosition, VirtualCamera.localRotation, VirtualCameraHolder.position, VirtualCameraHolder.rotation, CameraModes.Free, directionTransform.position);
        ChangeMode(CameraModes.Free);

        TransitionTimeCounter = 0;
    }

    private static void SetOldCameraPosition()
    {
        Vector3 oldLookAt = (NewCameraState != null) ? NewCameraState.LookAtPosition : default;
        OldCameraState = new CameraState(Camera.localPosition, Camera.localRotation, CameraHolder.position, CameraHolder.rotation, CameraModes.Free, oldLookAt);
    }

    private static void SetSavedCameraPosition()
    {
        Vector3 oldLookAt = (NewCameraState != null) ? NewCameraState.LookAtPosition : default;
        SavedCameraState = new CameraState(Camera.localPosition, Camera.localRotation, CameraHolder.position, CameraHolder.rotation, cameraMode, oldLookAt);
    }

    public static void RestoreCamera()
    {
        if (SavedCameraState != null)
        {
            ChangeMode(SavedCameraState.CameraMode);
            SetCameraState(SavedCameraState);
            SavedCameraState = null;
            IsCinematic = false;
        }
    }

    private static void SetCameraState(CameraState state)
    {
        Camera.localPosition = state.CameraPosition;
        Camera.localRotation = state.CameraAngles;
        CameraHolder.position = state.CameraHolderPosition;
        CameraHolder.rotation = state.CameraHolderAngles;
    }

    private void DoCameraTransition(CameraState oldCamera, CameraState newCamera)
    {
        if (TransitionTimeCounter == 1) return;

        SetPosition(
            Vector3.Lerp(oldCamera.CameraHolderPosition, newCamera.CameraHolderPosition, TransitionTimeCounter),
            Vector3.zero
        );

        Camera.LookAt(
            Vector3.Lerp(OldCameraState.LookAtPosition, NewCameraState.LookAtPosition, TransitionTimeCounter),
            Vector3.up
        );

        CameraState currentCameraState = new CameraState(Camera.localPosition, Camera.localRotation, CameraHolder.position, CameraHolder.rotation, CameraModes.Free, NewCameraState.LookAtPosition);
        SetCameraState(currentCameraState);

        TransitionTimeCounter += Mathf.Min(Time.deltaTime * TRANSITION_TIME_SPEED, 1 - TransitionTimeCounter);
    }

}
