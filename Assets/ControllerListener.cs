//====================================================================================
//
// Purpose: Provide ability to grab an interactable object when it is being touched
//
// This script must be attached to a Controller within the [CameraRig] Prefab
//
// The SteamVR_ControllerEvents and SteamVR_InteractTouch scripts must also be
// attached to the Controller
//
// Press the default 'Trigger' button on the controller to grab an object
// Released the default 'Trigger' button on the controller to drop an object
//
//====================================================================================

using UnityEngine;
using System.Collections;

public class ControllerListener : MonoBehaviour
{
    public Rigidbody controllerAttachPoint = null;
    public bool hideControllerOnGrab = false;

    public event ObjectInteractEventHandler ControllerGrabInteractableObject;
    public event ObjectInteractEventHandler ControllerUngrabInteractableObject;

    Vector3 startPos;
    GameObject grabbedObject = null;

    Vector3 startWorld;
    GameObject world = null;

    SteamVR_InteractTouch interactTouch;
    SteamVR_TrackedObject trackedController;
    SteamVR_ControllerActions controllerActions;

    private int grabEnabledState = 0;

    public virtual void OnControllerGrabInteractableObject(ObjectInteractEventArgs e)
    {
        if (ControllerGrabInteractableObject != null)
            ControllerGrabInteractableObject(this, e);
    }

    public virtual void OnControllerUngrabInteractableObject(ObjectInteractEventArgs e)
    {
        if (ControllerUngrabInteractableObject != null)
            ControllerUngrabInteractableObject(this, e);
    }

    private void Awake()
    {
        if (GetComponent<SteamVR_InteractTouch>() == null)
        {
            Debug.LogError("SteamVR_InteractGrab is required to be attached to a SteamVR Controller that has the SteamVR_InteractTouch script attached to it");
            return;
        }

        interactTouch = GetComponent<SteamVR_InteractTouch>();
        trackedController = GetComponent<SteamVR_TrackedObject>();
        controllerActions = GetComponent<SteamVR_ControllerActions>();

        world = GameObject.FindGameObjectWithTag("World");
    }

    private void Start()
    {
        //If no attach point has been specified then just use the tip of the controller
        if (controllerAttachPoint == null)
        {
            controllerAttachPoint = transform.GetChild(0).Find("tip").GetChild(0).GetComponent<Rigidbody>();
        }

        if (GetComponent<SteamVR_ControllerEvents>() == null)
        {
            Debug.LogError("SteamVR_InteractGrab is required to be attached to a SteamVR Controller that has the SteamVR_ControllerEvents script attached to it");
            return;
        }

        GetComponent<SteamVR_ControllerEvents>().AliasGrabOn += new ControllerClickedEventHandler(DoGrabObject);
        GetComponent<SteamVR_ControllerEvents>().AliasGrabOff += new ControllerClickedEventHandler(DoReleaseObject);
    }

    private void Update()
    {
        if (grabbedObject == null) return;

        world.transform.position = startWorld + trackedController.transform.position - startPos;

        //Debug.Log(world.transform.position + " = " + startWorld + " + " + trackedController.transform.position + " - " + startPos);
    }

    private bool IsObjectGrabbable(GameObject obj)
    {
        return (interactTouch.IsObjectInteractable(obj) && obj.GetComponent<SteamVR_InteractableObject>().isGrabbable);
    }

    private bool IsObjectHoldOnGrab(GameObject obj)
    {
        return (obj && obj.GetComponent<SteamVR_InteractableObject>() && obj.GetComponent<SteamVR_InteractableObject>().holdButtonToGrab);
    }

    private void GrabInteractedObject()
    {
        if (grabbedObject == null && IsObjectGrabbable(interactTouch.GetTouchedObject()))
        {
            grabbedObject = interactTouch.GetTouchedObject();
            OnControllerGrabInteractableObject(interactTouch.SetControllerInteractEvent(grabbedObject));
            grabbedObject.GetComponent<SteamVR_InteractableObject>().Grabbed(this.gameObject);
            if (hideControllerOnGrab)
            {
                controllerActions.ToggleControllerModel(false);
            }

            Debug.Log("Grabbed");

            startWorld = world.transform.position;
            startPos = trackedController.transform.position;
        }
    }

    private void UngrabInteractedObject(uint controllerIndex)
    {
        if (grabbedObject != null)
        {
            OnControllerUngrabInteractableObject(interactTouch.SetControllerInteractEvent(grabbedObject));
            grabbedObject.GetComponent<SteamVR_InteractableObject>().Ungrabbed(this.gameObject);

            if (hideControllerOnGrab)
            {
                controllerActions.ToggleControllerModel(true);
            }
            grabbedObject = null;

            Debug.Log("Released");
        }
    }

    private void DoGrabObject(object sender, ControllerClickedEventArgs e)
    {
        if (interactTouch.GetTouchedObject() != null && interactTouch.IsObjectInteractable(interactTouch.GetTouchedObject()))
        {
            GrabInteractedObject();
            if(!IsObjectHoldOnGrab(interactTouch.GetTouchedObject()))
            {
                grabEnabledState++;
            }
        }
    }

    private void DoReleaseObject(object sender, ControllerClickedEventArgs e)
    {
        if (IsObjectHoldOnGrab(grabbedObject) || grabEnabledState >= 2)
        {
            UngrabInteractedObject(e.controllerIndex);
            grabEnabledState = 0;
        }
    }
}
