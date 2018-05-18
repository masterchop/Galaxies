﻿using Leap.Unity;
using Leap.Unity.Attributes;
using Leap.Unity.Gestures;
using Leap.Unity.Infix;
using Leap.Unity.RuntimeGizmos;
using UnityEngine;

using Pose = Leap.Unity.Pose;

public class LeapTRS2 : MonoBehaviour, IRuntimeGizmoComponent {

  #region Inspector

  [Header("Transform To Manipulate")]

  public Transform objectTransform;

  [Header("Grab Switches OR Pinch Gestures (pinch gestures override)")]

  //[SerializeField]
  //private GrabSwitch _switchA;
  //public GrabSwitch switchA { get {  return _switchA; } }

  //[SerializeField]
  //private GrabSwitch _switchB;
  //public GrabSwitch switchB { get { return _switchB; } }

  public PinchGesture pinchGestureA;
  public PinchGesture pinchGestureB;

  [Header("Scale")]

  [SerializeField]
  private bool _allowScale = true;

  [SerializeField]
  private float _minScale = 0.01f;

  [SerializeField]
  private float _maxScale = 500f;

  [Header("Position Constraint")]
  
  [SerializeField]
  [Tooltip("Constrains the maximum distance from the TRS object, only if the user isn't "
         + "actively performing the TRS action.")]
  private bool _constrainPosition = false;

  [SerializeField]
  [Tooltip("The maximum distance from the base position in the OBJECT'S space.")]
  private float _constraintDistance = 60f;

  [SerializeField]
  [Tooltip("The maximum distance from the base position in world space.")]
  private float _maxWorldDistance = 120f;

  [SerializeField]
  [DisableIf("_constrainPosition", isEqualTo: false)]
  private float _constraintStrength = 4f;

  [SerializeField]
  private float _maxConstraintSpeed = 1f;

  [SerializeField, Disable]
  private Vector3 _basePosition = Vector3.zero;

  [SerializeField]
  private Transform _overrideBaseTransform;

  [SerializeField, Disable]
  private float _localDistanceFromBase = 1f;

  [Header("Momentum (when not pinching)")]

  [SerializeField]
  private bool _allowMomentum = false;
  public bool allowMomentum {
    get { return _allowMomentum; }
    set { _allowMomentum = value; }
  }

  [SerializeField]
  private float      _linearFriction = 1f;

  [SerializeField]
  private float      _angularFriction = 1f;

  [SerializeField]
  private float      _scaleFriction = 1f;

  [SerializeField, Disable]
  private Vector3    _positionMomentum;

  [SerializeField, Disable]
  private Vector3    _rotationMomentum;

  [SerializeField, Disable, MinValue(0.001f)]
  private float      _scaleMomentum = 1f;

  [SerializeField, Disable]
  private Vector3   _lastKnownCentroid = Vector3.zero;

  [Header("Debug Runtime Gizmos")]

  [SerializeField]
  private bool _drawDebug = false;
  public bool drawDebug {
    get { return _drawDebug; }
    set { _drawDebug = value; }
  }

  [SerializeField]
  private bool _drawPositionConstraints = false;

  #endregion

  #region Unity Events

  void Update() {
    updateTRS();
  }

  #endregion

  #region TRS Getters (for data feedback)

  /// <summary>
  /// Gets whether TRS could happen if hands did the proper gestures.
  /// </summary>
  public bool isEnabledAndConfigured {
    get {
      return this.isActiveAndEnabled
        && pinchGestureA != null
        && pinchGestureB != null
        && pinchGestureA.isActiveAndEnabled
        && pinchGestureA.provider != null
        && pinchGestureB.isActiveAndEnabled
        && pinchGestureB.provider != null;
    }
  }

  /// <summary>
  /// Gets whether TRS is imminent because the attached pinch gestures are both eligible
  /// to activate pinches. This is only different from isEnabledAndConfigured if
  /// the pinch gestures have specific eligibility requirements beyond basic pinching.
  /// </summary>
  public bool isEligible {
    get {
      return isEnabledAndConfigured
        && pinchGestureA.isEligible
        && pinchGestureB.isEligible;
    }
  }

  /// <summary>
  /// Gets whether either the left or right pinch is actively tracked by this TRS
  /// controller.
  /// </summary>
  public bool isSinglyActive {
    get {
      return _aPoses.Count > 0 || _bPoses.Count > 0;
    }
  }

  /// <summary>
  /// Gets whether the left pinch pose is actively tracked by this TRS controller.
  /// </summary>
  public bool isLeftActive {
    get {
      return _aPoses.Count > 0;
    }
  }

  /// <summary>
  /// Gets the latest left pinch pose actively tracked by this TRS controller. Will raise
  /// an error if there is no such pose (see isLeftActive).
  /// </summary>
  public Pose leftPinchPose {
    get {
      return _aPoses.Get(0);
    }
  }

  /// <summary>
  /// Gets whether the right pinch pose is actively tracked by this TRS controller.
  /// </summary>
  public bool isRightActive {
    get {
      return _bPoses.Count > 0;
    }
  }

  /// <summary>
  /// Gets the latest right pinch pose actively tracked by this TRS controller. Will raise
  /// an error if there is no such pose (see isRightActive).
  /// </summary>
  public Pose rightPinchPose {
    get {
      return _bPoses.Get(0);
    }
  }


  /// <summary>
  /// Gets whether both the left and right pinch poses are active and being tracked by
  /// this TRS controller.
  /// </summary>
  public bool isDoublyActive {
    get {
      return _aPoses.Count > 0 && _bPoses.Count > 0;
    }
  }

  #endregion

  #region TRS

  private RingBuffer<Pose> _aPoses = new RingBuffer<Pose>(2);
  private RingBuffer<Pose> _bPoses = new RingBuffer<Pose>(2);

  private void updateTRS() {

    // Get basic grab switch state information.
    //var aGrasped = _switchA != null && _switchA.grasped;
    //var bGrasped = _switchB != null && _switchB.grasped;
    var aGrasped = pinchGestureA != null && pinchGestureA.isActive;
    var bGrasped = pinchGestureB != null && pinchGestureB.isActive;
    var aPose = Pose.identity;
    if (aGrasped) aPose = pinchGestureA.pose;
    var bPose = Pose.identity;
    if (bGrasped) bPose = pinchGestureB.pose;

    int numGrasping = (aGrasped? 1 : 0) + (bGrasped ? 1 : 0);

    if (!aGrasped) {
      _aPoses.Clear();
    }
    else {
      _aPoses.Add(aPose);
    }

    if (!bGrasped) {
      _bPoses.Clear();
    }
    else {
      _bPoses.Add(bPose);
    }

    // Declare information for applying the TRS.
    var objectScale = objectTransform.localScale.x;
    var origCentroid = Vector3.zero;
    var nextCentroid = Vector3.zero;
    var origAxis = Vector3.zero;
    var nextAxis = Vector3.zero;
    var twist = 0f;
    var applyPositionalMomentum = false;
    var applyRotateScaleMomentum = false;
    var applyPositionConstraint = false;

    // Fill information based on grab switch state.
    if (numGrasping == 0) {
      applyPositionalMomentum  = true;
      applyRotateScaleMomentum = true;
      applyPositionConstraint = true;
    }
    else if (numGrasping == 1) {

      var poses = aGrasped ? _aPoses : (bGrasped ? _bPoses : null);

      if (poses != null && poses.IsFull) {

        // Translation.
        origCentroid = poses[0].position;
        nextCentroid = poses[1].position;

      }

      applyRotateScaleMomentum = true;

      _lastKnownCentroid = nextCentroid;
    }
    else {

      if (_aPoses.IsFull && _bPoses.IsFull) {

        // Scale changes.
        float dist0 = Vector3.Distance(_aPoses[0].position, _bPoses[0].position);
        float dist1 = Vector3.Distance(_aPoses[1].position, _bPoses[1].position);
        
        float scaleChange = dist1 / dist0;
        
        if (_allowScale && !float.IsNaN(scaleChange) && !float.IsInfinity(scaleChange)) {
          objectScale *= scaleChange;
        }

        // Translation.
        origCentroid = (_aPoses[0].position + _bPoses[0].position) / 2f;
        nextCentroid = (_aPoses[1].position + _bPoses[1].position) / 2f;

        // Axis rotation.
        origAxis = (_bPoses[0].position - _aPoses[0].position);
        nextAxis = (_bPoses[1].position - _aPoses[1].position);

        // Twist.
        var perp = Utils.Perpendicular(nextAxis);
        
        var aRotatedPerp = perp.RotatedBy(_aPoses[1].rotation.From(_aPoses[0].rotation));
        //aRotatedPerp = (_aPoses[1].rotation * Quaternion.Inverse(_aPoses[0].rotation))
        //               * perp;
        var aTwist = Vector3.SignedAngle(perp, aRotatedPerp, nextAxis);

        var bRotatedPerp = perp.RotatedBy(_bPoses[1].rotation.From(_bPoses[0].rotation));
        //bRotatedPerp = (_bPoses[1].rotation * Quaternion.Inverse(_bPoses[0].rotation))
        //               * perp;
        var bTwist = Vector3.SignedAngle(perp, bRotatedPerp, nextAxis);

        twist = (aTwist + bTwist) * 0.5f;

        _lastKnownCentroid = nextCentroid;
      }
    }


    // Calculate TRS.
    Vector3    origTargetPos = objectTransform.transform.position;
    Quaternion origTargetRot = objectTransform.transform.rotation;
    //float      origTargetScale = objectTransform.transform.localScale.x;

    // Declare delta properties.
    Vector3    finalPosDelta;
    Quaternion finalRotDelta;
    float      finalScaleRatio;

    // Translation: apply momentum, or just apply the translation and record momentum.
    finalPosDelta = (nextCentroid - origCentroid);

    // Determine base position if we expect to apply positional constraints.
    float distanceToEdge = 0f;
    if (_constrainPosition) {
      if (_overrideBaseTransform) {
        _basePosition = _overrideBaseTransform.position;
      }
      else if (objectTransform.parent != null) {
        _basePosition = objectTransform.parent.position;
      }
      else {
        _basePosition = Vector3.zero;
      }

      var worldDistanceFromBase = Vector3.Distance(_basePosition, objectTransform.position);

      _localDistanceFromBase = objectTransform.InverseTransformVector(
                                 worldDistanceFromBase
                                 * Vector3.right).magnitude;

      distanceToEdge = Mathf.Max(0f, _localDistanceFromBase - _constraintDistance);

      if (distanceToEdge == 0f) {
        var worldDistanceToEdge = Mathf.Max(0f, worldDistanceFromBase - _maxWorldDistance);
        distanceToEdge = objectTransform.InverseTransformVector(
                           worldDistanceToEdge
                           * Vector3.right).magnitude;
      }

    }

    // Apply momentum if necessary, otherwise we'll perform direct TRS.
    if ((_allowMomentum && applyPositionalMomentum)
        || (applyPositionalMomentum && _constrainPosition && distanceToEdge > 0f)) {
      
      // Constrain momentum to constrain the object's position if necessary.
      if (_constrainPosition && applyPositionConstraint) {
        var constraintDir = _basePosition.From(objectTransform.position).normalized;

        // If we're not allowed to have normal momentum, immediately cancel any momentum
        // that isn't part of the constraint momentum.
        if (!_allowMomentum) {
          _positionMomentum = Vector3.ClampMagnitude(_positionMomentum,
                                Mathf.Max(0f, Vector3.Dot(_positionMomentum, constraintDir)));
        }

        var constraintMomentum = distanceToEdge * _constraintStrength * 0.0005f
                                 * constraintDir;

        constraintMomentum = Vector3.ClampMagnitude(constraintMomentum,
                                          _maxConstraintSpeed * Time.deltaTime);

        _positionMomentum = Vector3.Lerp(_positionMomentum, constraintMomentum,
                                         2f * Time.deltaTime);
      }

      // Apply (and decay) momentum.
      objectTransform.position += _positionMomentum;

      var _frictionDir = -_positionMomentum.normalized;
      _positionMomentum += _frictionDir * _positionMomentum.magnitude
                                        * _linearFriction
                                        * Time.deltaTime;

      // Also apply some drag so we never explode...
      _positionMomentum += (_frictionDir) * _positionMomentum.sqrMagnitude * _linearFriction * 0.1f;
    }
    else {
      // Apply transformation!
      objectTransform.position = objectTransform.position.Then(finalPosDelta);

      // Measure momentum only.
      _positionMomentum = Vector3.Lerp(_positionMomentum, finalPosDelta, 20f * Time.deltaTime);
    }

    // Remember last known centroid as pivot; remember local offset, scale, rotation,
    // then correct.
    var centroid = _lastKnownCentroid;
    var centroid_local = objectTransform.worldToLocalMatrix.MultiplyPoint3x4(centroid);
    
    // Scale.
    finalScaleRatio = objectScale / objectTransform.localScale.x;

    // Rotation.
    var poleRotation = Quaternion.FromToRotation(origAxis, nextAxis);
    var poleTwist = Quaternion.AngleAxis(twist, nextAxis);
    finalRotDelta = objectTransform.rotation
                                   .Then(poleRotation)
                                   .Then(poleTwist)
                                   .From(objectTransform.rotation);

    var finalRot = poleTwist * poleRotation * objectTransform.rotation;
    finalRotDelta = Quaternion.Inverse(objectTransform.rotation) * finalRot;

    // objectTransform.rotation = objectTransform.rotation * finalRotDelta;
    // objectTransform.rotation = objectTransform.rotation.Then(finalRotDelta);



    // Apply scale and rotation, or use momentum for these properties.
    if (_allowMomentum && applyRotateScaleMomentum) {
      // Apply (and decay) momentum only.
      objectTransform.rotation = objectTransform.rotation.Then(
                                   Quaternion.AngleAxis(_rotationMomentum.magnitude,
                                                        _rotationMomentum.normalized));
      objectTransform.localScale *= _scaleMomentum;

      var rotationFrictionDir = -_rotationMomentum.normalized;
      _rotationMomentum += rotationFrictionDir * _rotationMomentum.magnitude
                                               * _angularFriction
                                               * Time.deltaTime;
      // Also add some angular drag.
      _rotationMomentum += rotationFrictionDir * _rotationMomentum.sqrMagnitude * _angularFriction * 0.1f;
      _rotationMomentum = Vector3.Lerp(_rotationMomentum, Vector3.zero, _angularFriction * 5f * Time.deltaTime);

      _scaleMomentum = Mathf.Lerp(_scaleMomentum, 1f, _scaleFriction * Time.deltaTime);
    }
    else {
      // Apply transformations.
      objectTransform.rotation = objectTransform.rotation.Then(finalRotDelta);
      objectTransform.localScale = Vector3.one * (objectTransform.localScale.x
                                                  * finalScaleRatio);

      // Measure momentum only.
      _rotationMomentum = Vector3.Lerp(_rotationMomentum, finalRotDelta.ToAngleAxisVector(), 40f * Time.deltaTime);
      _scaleMomentum = Mathf.Lerp(_scaleMomentum, finalScaleRatio, 20f * Time.deltaTime);
    }

    // Apply scale constraints.
    if (objectTransform.localScale.x < _minScale) {
      objectTransform.localScale = _minScale * Vector3.one;
      _scaleMomentum = 1f;
    }
    else if (objectTransform.localScale.x > _maxScale) {
      objectTransform.localScale = _maxScale * Vector3.one;
      _scaleMomentum = 1f;
    }

    // Restore centroid pivot.
    var movedCentroid = objectTransform.localToWorldMatrix.MultiplyPoint3x4(centroid_local);
    objectTransform.position += (centroid - movedCentroid);
  }

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    if (!_drawDebug) return;

    if (objectTransform == null) return;

    drawer.PushMatrix();
    drawer.matrix = objectTransform.localToWorldMatrix;

    drawer.color = LeapColor.coral;
    drawer.DrawWireCube(Vector3.zero, Vector3.one * 0.20f);

    drawer.color = LeapColor.jade;
    drawer.DrawWireCube(Vector3.zero, Vector3.one * 0.10f);

    drawer.PopMatrix();

    if (_constrainPosition && _drawPositionConstraints) { 
      drawer.color = LeapColor.lavender;
      var dir = (Camera.main.transform.position.From(_basePosition)).normalized;
      drawer.DrawWireSphere(_basePosition,
                            objectTransform.TransformVector(Vector3.right * _constraintDistance).magnitude);

      drawer.color = LeapColor.violet;
      drawer.DrawWireSphere(_basePosition,
                            _maxWorldDistance);
    }
  }

  #endregion

  // TODO: Put this somewhere else -- Kabsch with scaling is useful, but it's unused here!!
  #region KabschSolver (with scaling)

  public class KabschSolver {
    Vector3[] QuatBasis = new Vector3[3];
    Vector3[] DataCovariance = new Vector3[3];
    Quaternion OptimalRotation = Quaternion.identity;
    public float scaleRatio = 1f;
    public Matrix4x4 SolveKabsch(Vector3[] inPoints, Vector4[] refPoints, bool solveRotation = true, bool solveScale = false) {
      if (inPoints.Length != refPoints.Length) { return Matrix4x4.identity; }

      //Calculate the centroid offset and construct the centroid-shifted point matrices
      Vector3 inCentroid = Vector3.zero; Vector3 refCentroid = Vector3.zero;
      float inTotal = 0f, refTotal = 0f;
      for (int i = 0; i < inPoints.Length; i++) {
        inCentroid += new Vector3(inPoints[i].x, inPoints[i].y, inPoints[i].z) * refPoints[i].w;
        inTotal += refPoints[i].w;
        refCentroid += new Vector3(refPoints[i].x, refPoints[i].y, refPoints[i].z) * refPoints[i].w;
        refTotal += refPoints[i].w;
      }
      inCentroid /= inTotal;
      refCentroid /= refTotal;

      //Calculate the scale ratio
      if (solveScale) {
        float inScale = 0f, refScale = 0f;
        for (int i = 0; i < inPoints.Length; i++) {
          inScale += (new Vector3(inPoints[i].x, inPoints[i].y, inPoints[i].z) - inCentroid).magnitude;
          refScale += (new Vector3(refPoints[i].x, refPoints[i].y, refPoints[i].z) - refCentroid).magnitude;
        }
        scaleRatio = (refScale / inScale);
      }

      //Calculate the 3x3 covariance matrix, and the optimal rotation
      if (solveRotation) {
        extractRotation(TransposeMultSubtract(inPoints, refPoints, inCentroid, refCentroid, DataCovariance), ref OptimalRotation);
      }

      return Matrix4x4.TRS(refCentroid, Quaternion.identity, Vector3.one * scaleRatio) *
             Matrix4x4.TRS(Vector3.zero, OptimalRotation, Vector3.one) *
             Matrix4x4.TRS(-inCentroid, Quaternion.identity, Vector3.one);
    }

    //https://animation.rwth-aachen.de/media/papers/2016-MIG-StableRotation.pdf
    //Iteratively apply torque to the basis using Cross products (in place of SVD)
    void extractRotation(Vector3[] A, ref Quaternion q) {
      for (int iter = 0; iter < 9; iter++) {
        q.FillMatrixFromQuaternion(ref QuatBasis);
        Vector3 omega = (Vector3.Cross(QuatBasis[0], A[0]) +
                       Vector3.Cross(QuatBasis[1], A[1]) +
                       Vector3.Cross(QuatBasis[2], A[2])) *
       (1f / Mathf.Abs(Vector3.Dot(QuatBasis[0], A[0]) +
                       Vector3.Dot(QuatBasis[1], A[1]) +
                       Vector3.Dot(QuatBasis[2], A[2]) + 0.000000001f));

        float w = omega.magnitude;
        if (w < 0.000000001f)
          break;
        q = Quaternion.AngleAxis(w * Mathf.Rad2Deg, omega / w) * q;
        q = Quaternion.Lerp(q, q, 0f); //Normalizes the Quaternion; critical for error suppression
      }
    }

    //Calculate Covariance Matrices --------------------------------------------------
    public static Vector3[] TransposeMultSubtract(Vector3[] vec1, Vector4[] vec2, Vector3 vec1Centroid, Vector3 vec2Centroid, Vector3[] covariance) {
      for (int i = 0; i < 3; i++) { //i is the row in this matrix
        covariance[i] = Vector3.zero;
      }

      for (int k = 0; k < vec1.Length; k++) {//k is the column in this matrix
        Vector3 left = (vec1[k] - vec1Centroid) * vec2[k].w;
        Vector3 right = (new Vector3(vec2[k].x, vec2[k].y, vec2[k].z) - vec2Centroid) * Mathf.Abs(vec2[k].w);

        covariance[0][0] += left[0] * right[0];
        covariance[1][0] += left[1] * right[0];
        covariance[2][0] += left[2] * right[0];
        covariance[0][1] += left[0] * right[1];
        covariance[1][1] += left[1] * right[1];
        covariance[2][1] += left[2] * right[1];
        covariance[0][2] += left[0] * right[2];
        covariance[1][2] += left[1] * right[2];
        covariance[2][2] += left[2] * right[2];
      }

      return covariance;
    }
  }

  #endregion
}

// TODO: Part of KabschSolver implementation that includes scaling
public static class FromMatrixExtension {
  public static Vector3 GetVector3(this Matrix4x4 m) { return m.GetColumn(3); }
  public static Quaternion GetQuaternion(this Matrix4x4 m) {
    if (m.GetColumn(2) == m.GetColumn(1)) { return Quaternion.identity; }
    return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
  }
  public static void FillMatrixFromQuaternion(this Quaternion q, ref Vector3[] covariance) {
    covariance[0] = q * Vector3.right;
    covariance[1] = q * Vector3.up;
    covariance[2] = q * Vector3.forward;
  }
}
