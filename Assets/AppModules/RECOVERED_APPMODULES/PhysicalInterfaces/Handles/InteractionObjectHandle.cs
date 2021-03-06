﻿using Leap.Unity.Interaction;
using Leap.Unity.PhysicalInterfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Leap.Unity.PhysicalInterfaces {

  public class InteractionObjectHandle : MonoBehaviour,
                                         IHandle {

    public InteractionBehaviour intObj;
    public AnchorableBehaviour anchObj;

    [Header("Runtime Gizmo Debugging")]
    public bool drawDebugGizmos = false;

    #region Unity Events

    void Reset() {
      if (intObj == null) intObj = GetComponent<InteractionBehaviour>();
      if (anchObj == null) anchObj = GetComponent<AnchorableBehaviour>();
    }

    void OnValidate() {
      if (intObj == null) intObj = GetComponent<InteractionBehaviour>();
      if (anchObj == null) anchObj = GetComponent<AnchorableBehaviour>();
    }

    void Start() {
      intObj.OnGraspBegin += onGraspBegin;

      intObj.OnGraspedMovement += onGraspedMovement;

      if (anchObj != null) {
        anchObj.OnPostTryAnchorOnGraspEnd += onGraspEnd;
      }
      else {
        intObj.OnGraspEnd += onGraspEnd;
      }
    }

    private Pose? _intObjPose0;
    private Pose? _intObjPose1;
    private Pose _intObjDeltaPose = Pose.identity;

    private void FixedUpdate() {
      if (intObj.isActiveAndEnabled) {
        if (!_intObjPose0.HasValue) {
          _intObjPose0 = pose;
        }
        else if (!_intObjPose1.HasValue) {
          _intObjPose1 = pose;
        }
        else {
          Utils.Swap(ref _intObjPose0, ref _intObjPose1);
          _intObjPose1 = pose;
        }
      }
      else {
        _intObjPose0 = null;
        _intObjPose1 = null;
      }

      if (_intObjPose0.HasValue && _intObjPose1.HasValue) {
        var p0 = _intObjPose0.Value; var p1 = _intObjPose1.Value;
        _intObjDeltaPose
          = new Pose(p1.position - p0.position,
                     Quaternion.Inverse(p0.rotation) * p1.rotation);
      }
    }

    private void onGraspBegin() {
      fireOnPickedUp();
    }

    private void onGraspedMovement(Vector3 preMovedPos, Quaternion preMovedRot,
                                   Vector3 postMovedPos, Quaternion postMovedRot,
                                   List<InteractionController> graspingControllers) {
      fireOnMoved(new Pose(postMovedPos, postMovedRot));
    }

    private void onGraspEnd() {
      if (anchObj != null && anchObj.preferredAnchor != null) {
        OnPlacedInContainer();
        OnPlacedHandleInContainer(this);
      }
      else if (intObj.rigidbody.velocity.magnitude > PhysicalInterfaceUtils.MIN_THROW_SPEED) {
        OnThrown(intObj.rigidbody.velocity);
        OnThrownHandle(this, intObj.rigidbody.velocity);
      }
      else {
        OnPlaced();
        OnPlacedHandle(this);
      }

      if (drawDebugGizmos) {
        DebugPing.Ping(intObj.transform.position, LeapColor.orange, 0.5f);
      }
    }

    private void fireOnPickedUp() {
      if (anchObj.isAttached) {
        OnPlaced();
      }

      OnPickedUp();
      OnPickedUpHandle(this);

      if (drawDebugGizmos) {
        DebugPing.Ping(intObj.transform.position, LeapColor.cyan, 0.5f);
      }
    }

    private void fireOnMoved(Pose movedToPose) {
      OnMoved();
      OnMovedHandle(this, movedToPose);

      if (drawDebugGizmos) {
        DebugPing.Ping(intObj.transform.position, LeapColor.blue, 0.075f);
      }
    }

    #endregion

    #region IHandle

    public Pose pose {
      get { return intObj.transform.ToWorldPose(); }
    }

    public void SetPose(Pose pose) {
      intObj.transform.SetWorldPose(pose);
      intObj.rigidbody.position = pose.position;
      intObj.rigidbody.rotation = pose.rotation;
      //intObj.rigidbody.MovePosition(pose.position);
      //intObj.rigidbody.MoveRotation(pose.rotation);
    }

    public Movement movement {
      get { return new Movement(pose,
                                pose * deltaPose,
                                Time.fixedDeltaTime); }
    }

    private Pose getWorldPose(InteractionBehaviour intObj) {
      return new Pose(intObj.rigidbody.position, intObj.rigidbody.rotation);
    }

    public Pose deltaPose {
      get { return _intObjDeltaPose; }
    }

    public bool isHeld {
      get { return intObj.isGrasped || (anchObj != null && anchObj.isAttached); }
    }

    public Vector3 heldPosition {
      get { return intObj.isGrasped ? intObj.graspingController.position
                                    : anchObj.anchor.transform.position; }
    }

    public event Action OnPickedUp = () => { };
    public event Action OnMoved = () => { };
    public event Action OnPlaced = () => { };
    public event Action OnPlacedInContainer = () => { };
    public event Action<Vector3> OnThrown = (v) => { };

    public event Action<IHandle> OnPickedUpHandle = (x) => { };
    public event Action<IHandle, Pose> OnMovedHandle = (x, p) => { };
    public event Action<IHandle> OnPlacedHandle = (x) => { };
    public event Action<IHandle> OnPlacedHandleInContainer = (x) => { };
    public event Action<IHandle, Vector3> OnThrownHandle = (x, v) => { };

    #endregion

  }

}
