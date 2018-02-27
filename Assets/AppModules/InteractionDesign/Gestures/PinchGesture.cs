﻿using System;
using Leap.Unity.Attributes;
using Leap.Unity.Infix;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Gestures {

  public class PinchGesture : OneHandedGesture, IPoseGesture, IStream<Pose> {

    // TODO: Incorporate intention system for exclusivity
    //[Header("Intention System")]
    //[SerializeField]
    //private bool _requireIntent = true;

    #region Inspector

    #region Core Pinch Heuristic Checks

    private const string CATEGORY_PINCH_HEURISTIC = "Pinch Heuristic";

    [Header("Activation")]

    [DevGui.DevCategory(CATEGORY_PINCH_HEURISTIC)]
    [DevGui.DevValue]
    public bool drawDebugPinchDistance = false;

    [DevGui.DevCategory(CATEGORY_PINCH_HEURISTIC)]
    [DevGui.DevValue]
    [Range(0f, 0.3f)]
    public float activationPinchDistance = 0.01f;

    [DevGui.DevCategory(CATEGORY_PINCH_HEURISTIC)]
    [DevGui.DevValue]
    public bool useVelocities = false;

    #endregion

    #region Safety Checks (Pinky Checks)

    private const string CATEGORY_PINKY_SAFETY = "Pinky Safety Pinch";

    [DevGui.DevCategory(CATEGORY_PINKY_SAFETY)]
    [DevGui.DevValue]
    public bool requirePinkySafetyPinch = false;

    [DevGui.DevCategory(CATEGORY_PINKY_SAFETY)]
    [DevGui.DevValue]
    [Range(0f, 1f)]
    [DisableIf("requirePinkySafetyPinch", isEqualTo: false)]
    public float minPinkySafetyProduct = 0.50f;

    [DevGui.DevCategory(CATEGORY_PINKY_SAFETY)]
    [DevGui.DevValue]
    [Tooltip("Higher = pinky must be opened further out to begin a pinch")]
    [Range(0f, 1f)]
    [DisableIf("requirePinkySafetyPinch", isEqualTo: false)]
    public float maxPinkyCurl = 0.2f;

    [DevGui.DevCategory(CATEGORY_PINKY_SAFETY)]
    [DevGui.DevValue]
    [Tooltip("Higher = index must curl faster relative to pinky curl velocity to pinch")]
    [Range(-1f, 7f)]
    [DisableIf("requirePinkySafetyPinch", isEqualTo: false)]
    public float minIndexMinusPinkyCurlVel = 1.5f;

    [DevGui.DevCategory(CATEGORY_PINKY_SAFETY)]
    [DevGui.DevValue]
    [Range(0f, 5f)]
    [DisableIf("requirePinkySafetyPinch", isEqualTo: false)]
    public float minIndexCurlVel = 0.5f;

    #endregion

    #region Safety Checks (Middle Finger Checks)

    private const string CATEGORY_MIDDLE_SAFETY = "Middle Finger Safety Pinch";

    [DevGui.DevCategory(CATEGORY_MIDDLE_SAFETY)]
    [DevGui.DevValue]
    public bool requireMiddleFingerAngle = true;

    [DevGui.DevCategory(CATEGORY_MIDDLE_SAFETY)]
    [DevGui.DevValue]
    [Range(-20f, 30f)]
    [DisableIf("requireMiddleFingerAngle", isEqualTo: false)]
    public float minSignedMiddleIndexAngle = -20f;

    [DevGui.DevCategory(CATEGORY_MIDDLE_SAFETY)]
    [DevGui.DevValue]
    [Range(0f, 90f)]
    [DisableIf("requireMiddleFingerAngle", isEqualTo: false)]
    public float minPalmMiddleAngle = 65f;

    #endregion

    #region Safety Checks (Ring Finger Checks)

    private const string CATEGORY_RING_SAFETY = "Ring Finger Safety Pinch";

    [DevGui.DevCategory(CATEGORY_RING_SAFETY)]
    [DevGui.DevValue]
    public bool requireRingFingerAngle = true;

    [DevGui.DevCategory(CATEGORY_RING_SAFETY)]
    [DevGui.DevValue]
    [Range(-20f, 30f)]
    [DisableIf("requireRingFingerAngle", isEqualTo: false)]
    public float minSignedRingIndexAngle = -20f;

    [DevGui.DevCategory(CATEGORY_RING_SAFETY)]
    [DevGui.DevValue]
    [Range(0f, 90f)]
    [DisableIf("requireRingFingerAngle", isEqualTo: false)]
    public float minPalmRingAngle = 65f;

    #endregion
    
    #region Finger Safety Eligibility Hysteresis

    private const string CATEGORY_GLOBAL_HYSTERESIS = "Ring & Middle Safety Hysteresis";

    [DevGui.DevCategory(CATEGORY_GLOBAL_HYSTERESIS)]
    [DevGui.DevValue]
    [Range(0.6f, 1f)]
    public float ringMiddleSafetyHysteresisMult = 0.8f;

    #endregion

    #region Palm Vs Leap Angle

    private const string CATEGORY_PALM_ANGLE = "Palm Normal Angle";

    [DevGui.DevCategory(CATEGORY_PALM_ANGLE)]
    [DevGui.DevValue]
    public bool requirePalmVsLeapAngle = false;

    [DevGui.DevCategory(CATEGORY_PALM_ANGLE)]
    [DevGui.DevValue]
    [Range(10f, 181f)]
    [DisableIf("requirePalmVsLeapAngle", isEqualTo: false)]
    public float maxPalmVsLeapAngle = 181f;

    #endregion

    #region Index Angle (Eligibility Only)

    private const string CATEGORY_INDEX_ANGLE = "Index Angle (Eligibility Only)";

    [DevGui.DevCategory(CATEGORY_INDEX_ANGLE)]
    [DevGui.DevValue]
    [Range(45f, 130f)]
    public float maxIndexAngleForEligibilityActivation = 98f;

    [DevGui.DevCategory(CATEGORY_INDEX_ANGLE)]
    [DevGui.DevValue]
    [Range(45f, 130f)]
    public float maxIndexAngleForEligibilityDeactivation = 110f;

    #endregion

    #region Thumb Angle (Eligibility Only)

    private const string CATEGORY_THUMB_ANGLE = "Thumb Angle (Eligibility Only)";

    [DevGui.DevCategory(CATEGORY_THUMB_ANGLE)]
    [DevGui.DevValue]
    [Range(45f, 130f)]
    public float maxThumbAngleForEligibilityActivation = 85f;

    [DevGui.DevCategory(CATEGORY_THUMB_ANGLE)]
    [DevGui.DevValue]
    [Range(45f, 130f)]
    public float maxThumbAngleForEligibilityDeactivation = 100f;

    #endregion

    #region Deactivation

    [Header("Deactivation")]

    [DevGui.DevCategory("Pinch Heuristic")]
    [DevGui.DevValue]
    [Range(0.01f, 0.08f)]
    public float pinchDeactivateDistance = 0.035f;

    #endregion

    #region Feedback

    [Header("Feedback")]
    public Color activeColor = LeapColor.lime;
    public Color readyColor = Color.Lerp(LeapColor.lime, LeapColor.red, 0.3f);
    public Color inactiveColor = LeapColor.red;
    public Material feedbackMaterial = null;

    private Pose _lastPinchPose = Pose.identity;

    [Header("General Feedback")]

    public FloatEvent OnPinchStrengthEvent;
    [System.Serializable] public class FloatEvent : UnityEvent<float> { }

    #endregion

    #endregion

    #region Custom Pinch Strength

    public static Vector3 PinchSegment2SegmentDisplacement(Hand h,
                                                           out Vector3 c0,
                                                           out Vector3 c1) {
      var indexDistal = h.GetIndex().bones[3].PrevJoint.ToVector3();
      var indexTip = h.GetIndex().TipPosition.ToVector3();
      var thumbDistal = h.GetThumb().bones[3].PrevJoint.ToVector3();
      var thumbTip = h.GetThumb().TipPosition.ToVector3();

      return Segment2SegmentDisplacement(indexDistal, indexTip, thumbDistal, thumbTip,
                                         out c0, out c1);
    }

    public float GetCustomPinchStrength(Hand h) {
      Vector3 c0, c1;
      var pinchDistance = PinchSegment2SegmentDisplacement(h, out c0, out c1).magnitude;

      pinchDistance -= 0.01f;
      pinchDistance = pinchDistance.Clamped01();

      if (Input.GetKeyDown(KeyCode.C)) {
        Debug.Log(pinchDistance);
      }

      if (drawDebugPinchDistance) {
        DebugPing.Line("RH pinch",  c0, c1, LeapColor.blue);
        DebugPing.Label("RH pinch", ((c1 + c0) / 2f), LeapColor.blue);
      }

      return pinchDistance.MapUnclamped(0.0168f, 0.08f, 1f, 0f);
    }

    #region Segment-to-Segment Displacement (John S)

    public static Vector3 Segment2SegmentDisplacement(Vector3 a1, Vector3 a2,
                                                      Vector3 b1, Vector3 b2,
                                                      out Vector3 c0, out Vector3 c1) {
      float outTimeToA2 = 0f, outTimeToB2 = 0f;
      return Segment2SegmentDisplacement(a1, a2, b1, b2,
                                         out outTimeToA2, out outTimeToB2,
                                         out c0, out c1);
    }

    public static Vector3 Segment2SegmentDisplacement(Vector3 a1, Vector3 a2,
                                                      Vector3 b1, Vector3 b2,
                                                      out float timeToa2,
                                                      out float timeTob2,
                                                      out Vector3 c0, out Vector3 c1) {
      Vector3 u = a2 - a1; //from a1 to a2
      Vector3 v = b2 - b1; //from b1 to b2
      Vector3 w = a1 - b1;
      float a = Vector3.Dot(u, u);         // always >= 0
      float b = Vector3.Dot(u, v);
      float c = Vector3.Dot(v, v);         // always >= 0
      float d = Vector3.Dot(u, w);
      float e = Vector3.Dot(v, w);
      float D = a * c - b * b;        // always >= 0
      float sc, sN, sD = D;       // sc = sN / sD, default sD = D >= 0
      float tc, tN, tD = D;       // tc = tN / tD, default tD = D >= 0

      // compute the line parameters of the two closest points
      if (D < Mathf.Epsilon) { // the lines are almost parallel
        sN = 0.0f;         // force using point P0 on segment S1
        sD = 1.0f;         // to prevent possible division by 0.0 later
        tN = e;
        tD = c;
      }
      else {                 // get the closest points on the infinite lines
        sN = (b * e - c * d);
        tN = (a * e - b * d);
        if (sN < 0.0f) {        // sc < 0 => the s=0 edge is visible
          sN = 0.0f;
          tN = e;
          tD = c;
        }
        else if (sN > sD) {  // sc > 1  => the s=1 edge is visible
          sN = sD;
          tN = e + b;
          tD = c;
        }
      }

      if (tN < 0.0) {            // tc < 0 => the t=0 edge is visible
        tN = 0.0f;
        // recompute sc for this edge
        if (-d < 0.0)
          sN = 0.0f;
        else if (-d > a)
          sN = sD;
        else {
          sN = -d;
          sD = a;
        }
      }
      else if (tN > tD) {      // tc > 1  => the t=1 edge is visible
        tN = tD;
        // recompute sc for this edge
        if ((-d + b) < 0.0)
          sN = 0;
        else if ((-d + b) > a)
          sN = sD;
        else {
          sN = (-d + b);
          sD = a;
        }
      }
      // finally do the division to get sc and tc
      sc = (Mathf.Abs(sN) < Mathf.Epsilon ? 0.0f : sN / sD);
      tc = (Mathf.Abs(tN) < Mathf.Epsilon ? 0.0f : tN / tD);

      // get the difference of the two closest points
      Vector3 dP = w + (sc * u) - (tc * v);  // =  S1(sc) - S2(tc)
      timeToa2 = sc; timeTob2 = tc;

      // output the closest points on each segment
      c0 = a1 + (sc * u);
      c1 = b1 + (tc * v);

      return dP;   // return the closest distance
    }

    #endregion

    #endregion

    #region Safety Pinch (Pinky)

    //private float _middleSafetyAmt = 0f;
    //private float _ringSafetyAmt   = 0f;
    private float _pinkySafetyAmt  = 0f;

    //private float _safetySum = 0f;

    private void updateSafetyPinch(Hand hand) {
      var knuckleDir = hand.DistalAxis();

      //_middleSafetyAmt = hand.GetMiddle().bones[3].Direction
      //                     .Dot(knuckleDir.ToVector()).Map(0f, 1f, 0f, 1f);
      //_ringSafetyAmt = hand.GetRing().bones[3].Direction
      //                     .Dot(knuckleDir.ToVector()).Map(0f, 1f, 0f, 1f);
      _pinkySafetyAmt = hand.GetPinky().bones[1].Direction
                           .Dot(knuckleDir.ToVector()).Map(0f, 1f, 0f, 1f);

      if (hand.GetPinky().bones[1].Direction.ToVector3().Dot(-hand.PalmarAxis()) > 0f) {
        _pinkySafetyAmt = 1f;
      }

      //_safetySum = _middleSafetyAmt + _ringSafetyAmt + _pinkySafetyAmt;
    }

    private bool isSafetyActivationSatisfied() {
      return _pinkySafetyAmt > minPinkySafetyProduct;
    }

    #endregion

    #region Finger Curl Buffers

    private DeltaFloatBuffer _pinkyCurlBuffer = new DeltaFloatBuffer(5);
    private DeltaFloatBuffer _indexCurlBuffer = new DeltaFloatBuffer(5);
    private DeltaFloatBuffer _middleCurlBuffer = new DeltaFloatBuffer(5);

    private void updatePinkyCurl(Hand h) {
      var pinky = h.GetPinky();
      var pinkyCurl = getCurl(h, pinky);

      _pinkyCurlBuffer.Add(pinkyCurl, Time.time);
    }

    private void updateIndexCurl(Hand h) {
      var index = h.GetIndex();
      var indexCurl = getCurl(h, index);

      _indexCurlBuffer.Add(indexCurl, Time.time);
    }

    private void updateMiddleCurl(Hand h) {
      var middle = h.GetMiddle();
      var middleCurl = getCurl(h, middle);

      _middleCurlBuffer.Add(middleCurl, Time.time);
    }

    private float getCurl(Hand h, Finger f) {
      //return (getBaseCurl(h, f) + getGripCurl(h, f)) / 2f;
      return getBaseCurl(h, f);
    }

    private float getBaseCurl(Hand h, Finger f) {
      var palmAxis = h.PalmarAxis();
      var leftPositiveThumbAxis = h.RadialAxis() * (h.IsLeft ? 1f : -1f);
      int baseBoneIdx = 1;
      if (f.Type == Finger.FingerType.TYPE_THUMB) baseBoneIdx = 2;
      var baseCurl = f.bones[baseBoneIdx].Direction.ToVector3()
                      .SignedAngle(palmAxis, leftPositiveThumbAxis)
                      .Map(0f, 90f, 1f, 0f);

      return baseCurl;
    }

    private float getGripCurl(Hand h, Finger f) {
      var leftPositiveThumbAxis = h.RadialAxis() * (h.IsLeft ? 1f : -1f);
      int baseBoneIdx = 1;
      if (f.Type == Finger.FingerType.TYPE_THUMB) baseBoneIdx = 2;
      var baseDir = f.bones[baseBoneIdx].Direction.ToVector3();

      var gripAngle = baseDir.SignedAngle(
                                f.bones[3].Direction.ToVector3(),
                                leftPositiveThumbAxis);

      if (gripAngle < -30f) {
        gripAngle += 360f;
      }

      var gripCurl = gripAngle.Map(0f, 150f, 0f, 1f);
      return gripCurl;
    }

    #endregion

    #region OneHandedGesture

    [Header("Debug")]
    public bool _drawDebug = false;
    public bool _drawDebugPath = false;

    private DeltaFloatBuffer pinchStrengthBuffer = new DeltaFloatBuffer(5);

    private DeltaBuffer handPositionBuffer = new DeltaBuffer(5);

    private const int MIN_REACTIVATE_TIME = 5;
    private int minReactivateTimer = 0;

    private const int MIN_REACTIVATE_TIME_SINCE_DEGENERATE_CONDITIONS = 6;
    private int minReactivateSinceDegenerateConditionsTimer = 0;

    private bool requiresRepinch = false;

    private bool _isGestureEligible = false;
    public override bool isEligible {
      get {
        return base.isEligible && (isActive || _isGestureEligible);
      }
    }

    protected override bool ShouldGestureActivate(Hand hand) {
      bool shouldActivate = false;

      var wasEligibleLastCheck = _isGestureEligible;
      _isGestureEligible = false;

      updateSafetyPinch(hand);

      updatePinkyCurl(hand);
      updateIndexCurl(hand);
      updateMiddleCurl(hand);

      if (minReactivateTimer > MIN_REACTIVATE_TIME) {

        if (minReactivateSinceDegenerateConditionsTimer
            > MIN_REACTIVATE_TIME_SINCE_DEGENERATE_CONDITIONS) {
          var latestPinchStrength = GetCustomPinchStrength(hand);
          OnPinchStrengthEvent.Invoke(latestPinchStrength);

          pinchStrengthBuffer.Add(latestPinchStrength, Time.time);
          handPositionBuffer.Add(hand.PalmPosition.ToVector3(), Time.time);

          if (pinchStrengthBuffer.IsFull) {
            var pinchStrengthVelocity = pinchStrengthBuffer.Delta();

            var handFOVAngle = Vector3.Angle(Camera.main.transform.forward,
            hand.PalmPosition.ToVector3() - Camera.main.transform.position);
            var handWithinFOV = handFOVAngle < Camera.main.fieldOfView / 2.2f;

            if (_drawDebug) {
              RuntimeGizmos.BarGizmo.Render(pinchStrengthVelocity,
                Camera.main.transform.position
                + Camera.main.transform.forward * 1f, Vector3.up, Color.red, 0.02f);
            }

            var pinchActivateVelocity = 3.5f;
            var handVelocity = handPositionBuffer.Delta();

            pinchActivateVelocity = handVelocity.magnitude.Map(0f, 2f, 1.5f, 8f);

            var pinchDist = PinchSegment2SegmentDisplacement(hand).magnitude;

            pinchActivateVelocity *= pinchDist.Map(0f, 0.02f, 0f, 1f);


            var pinkyCurlSample = _pinkyCurlBuffer.GetLatest();
            if (_pinkyCurlBuffer.IsFull) {
              //var pinkyCurlVelocity = _pinkyCurlBuffer.Delta();
            }

            var indexMinusPinkyCurlVel = 10f;
            var indexCurlVel = 10f;
            if (_pinkyCurlBuffer.IsFull && _indexCurlBuffer.IsFull) {
              var pinkyCurlVel = _pinkyCurlBuffer.Delta();
              indexCurlVel = _indexCurlBuffer.Delta();

              indexMinusPinkyCurlVel = indexCurlVel - pinkyCurlVel;
            }

            #region Pinky (safety pinch) feedback
            if (feedbackMaterial != null) {
              if (requirePinkySafetyPinch
                  && (pinkyCurlSample < maxPinkyCurl)
                  && (indexMinusPinkyCurlVel > minIndexMinusPinkyCurlVel)
                  && (indexCurlVel > minIndexCurlVel)) {
                feedbackMaterial.color = activeColor;
              }
              else if (requirePinkySafetyPinch
                       && (pinkyCurlSample < maxPinkyCurl)) {
                feedbackMaterial.color = readyColor;

                RuntimeGizmos.RuntimeGizmoDrawer drawer;
                if (RuntimeGizmos.RuntimeGizmoManager.TryGetGizmoDrawer(out drawer)) {
                  drawer.color = readyColor;
                  drawer.DrawWireCapsule(hand.GetThumb().TipPosition.ToVector3(),
                                         hand.GetIndex().TipPosition.ToVector3(),
                                         0.005f);
                }
              }
              else {
                feedbackMaterial.color = inactiveColor;
              }
            }
            #endregion

            #region Middle Finger Safety

            var middleDir = hand.GetMiddle().bones[1].Direction.ToVector3();
            var indexDir = hand.GetIndex().bones[1].Direction.ToVector3();
            var signedMiddleIndexAngle = Vector3.SignedAngle(indexDir,
                                                             middleDir,
                                                             hand.RadialAxis());
            if (hand.IsLeft) { signedMiddleIndexAngle *= -1f; }

            var palmDir = hand.PalmarAxis();

            var signedMiddlePalmAngle = Vector3.SignedAngle(palmDir,
                                                            middleDir,
                                                            hand.RadialAxis());
            if (hand.IsLeft) { signedMiddlePalmAngle *= -1f; }


            #region Middle Safety Feedback
            if (requireMiddleFingerAngle) {
              if (signedMiddleIndexAngle >= minSignedMiddleIndexAngle
                  && signedMiddlePalmAngle >= minPalmMiddleAngle) {
                if (feedbackMaterial != null) {
                  feedbackMaterial.color = readyColor;
                }
              }
            }
            #endregion

            //var isMiddleCurlVelocityLow = true;
            //if (_middleCurlBuffer.IsFull) {
            //  var middleCurlVel = _middleCurlBuffer.Delta();
            //  //if (Mathf.Abs(middleCurlVel) <
            //  // WOULDN'T IT BE NICE IF THIS WOULD JUST WORK
            //  // DebugPing.PingReadout("middleCurlVel",
            //  //                       Mathf.Abs(middleCurlVel),
            //  //                       hand.GetMiddle().bones[1].PrevJoint.ToVector3());
            //  if (Input.GetKeyDown(KeyCode.D)) { Debug.Log(Mathf.Abs(middleCurlVel)); }
            //}

            #endregion

            #region Ring Finger Safety

            var ringDir = hand.GetRing().bones[1].Direction.ToVector3();
            var signedRingIndexAngle = Vector3.SignedAngle(indexDir,
                                                             ringDir,
                                                             hand.RadialAxis());
            if (hand.IsLeft) { signedRingIndexAngle *= -1f; }

            var signedRingPalmAngle = Vector3.SignedAngle(palmDir,
                                                            ringDir,
                                                            hand.RadialAxis());
            if (hand.IsLeft) { signedRingPalmAngle *= -1f; }


            #region Ring Safety Feedback
            if (requireRingFingerAngle) {
              if (signedRingIndexAngle >= minSignedRingIndexAngle
                  && signedRingPalmAngle >= minPalmRingAngle) {
                if (feedbackMaterial != null) {
                  feedbackMaterial.color = readyColor;
                }
              }
            }
            #endregion

            #endregion

            #region Palm-Facing Eligibility
            
            var palmNormalCameraAngle = Vector3.Angle(hand.PalmarAxis(),
                                                      provider.transform.forward);

            #endregion

            #region Index Angle (Eligibility Only)

            // Note: obviously pinching already requires the index finger to
            // close relative to the palm -- this check simply drives the
            // isEligible state for this pinch gesture so that the gesture isn't
            // "eligible" when the hand is fully open.

            var indexPalmAngle = Vector3.Angle(indexDir, palmDir);

            #endregion

            #region Thumb Angle (Eligibility Only)

            // Note: obviously pinching already requires the thumb finger to
            // close to touch the index finger -- this check simply drives the
            // isEligible state for this pinch gesture so that the gesture isn't
            // "eligible" when the hand is fully open.

            var thumbDir = hand.GetThumb().bones[2].Direction.ToVector3();
            var thumbPalmAngle = Vector3.Angle(thumbDir, palmDir);

            #endregion

            // Eligibility.
            if (
              
                // Pinky-style safety pinch
                   (isSafetyActivationSatisfied()
                    || !requirePinkySafetyPinch)
                && (pinkyCurlSample < maxPinkyCurl
                    || !requirePinkySafetyPinch)

                // Middle-style safety pinch (no velocities).
                //&& ((!wasEligibleLastCheck
                //      && signedMiddleIndexAngle >= minSignedMiddleIndexAngle)
                //    || (wasEligibleLastCheck
                //        && signedMiddleIndexAngle >= minSignedMiddleIndexAngle
                //                                     * ringMiddleSafetyHysteresisMult)
                //    || !requireMiddleFingerAngle)

                && ((!wasEligibleLastCheck
                     && signedMiddlePalmAngle >= minPalmMiddleAngle)
                    || (wasEligibleLastCheck
                        && signedMiddlePalmAngle >= minPalmMiddleAngle
                                                    * ringMiddleSafetyHysteresisMult)
                    || !requireMiddleFingerAngle)

                // Ring-style safety pinch (no velocities).
                //&& ((!wasEligibleLastCheck
                //     && signedRingIndexAngle >= minSignedRingIndexAngle)
                //    || (wasEligibleLastCheck
                //        && signedRingIndexAngle >= minSignedRingIndexAngle
                //                                   * ringMiddleSafetyHysteresisMult)
                //    || !requireRingFingerAngle)0.04

                && ((!wasEligibleLastCheck
                     && signedRingPalmAngle >= minPalmRingAngle)
                    || (wasEligibleLastCheck
                        && signedRingPalmAngle >= minPalmRingAngle
                                                  * ringMiddleSafetyHysteresisMult)
                    || !requireRingFingerAngle)

                // Palm normal vs Leap provider angle.
                && (palmNormalCameraAngle <= maxPalmVsLeapAngle
                    || !requirePalmVsLeapAngle)

                // Index angle (eligibility state only)
                && ((!wasEligibleLastCheck
                     && indexPalmAngle < maxIndexAngleForEligibilityActivation)
                    || (wasEligibleLastCheck
                        && indexPalmAngle < maxIndexAngleForEligibilityDeactivation))

                // Thumb angle (eligibility state only)
                && ((!wasEligibleLastCheck
                     && thumbPalmAngle < maxThumbAngleForEligibilityActivation)
                    || (wasEligibleLastCheck
                        && thumbPalmAngle < maxThumbAngleForEligibilityDeactivation))

                // FOV.
                && (handWithinFOV)

                // Must cross pinch threshold from a non-pinching / non-fist pose.
                && (!requiresRepinch)
                
                ) {
              _isGestureEligible = true;
            }

            if (_isGestureEligible

                // Absolute pinch strength.
                && (latestPinchStrength > 0.8f)

                // Pinch strength velocity.
                && ((pinchStrengthVelocity > pinchActivateVelocity)
                    || !useVelocities)

                // (Pinky velocity constraints.)
                && (indexMinusPinkyCurlVel > minIndexMinusPinkyCurlVel
                    || !useVelocities || !requirePinkySafetyPinch)
                && (indexCurlVel > minIndexCurlVel
                    || !useVelocities || !requirePinkySafetyPinch)
                    
                    ) {
              shouldActivate = true;
              
              if (_drawDebug) {
                DebugPing.Ping(hand.GetPredictedPinchPosition(), Color.red, 0.20f);
              }
            }
            else {
              if (_isGestureEligible) {
                if (feedbackMaterial != null) {
                  feedbackMaterial.color = activeColor;
                }
              }
            }

            // "requiresRepinch" prevents a closed-finger configuration from beginning
            // a pinch when the index and thumb never actually actively close from a
            // valid position -- think, closed-fist to safety-pinch, as opposed to
            // open-hand to safety-pinch -- without introducing any velocity-based
            // requirement.
            if (latestPinchStrength > 0.88f && !shouldActivate) {
              requiresRepinch = true;
            }
            if (requiresRepinch && latestPinchStrength < 0.88f) {
              // changed from 0.8 to 0.88 because testing showed the requirement is a
              // bit too strict 
              requiresRepinch = false;
            }
          }
        }
        else {
          minReactivateSinceDegenerateConditionsTimer += 1;
        }

      }
      else {
        minReactivateTimer += 1;
      }

      if (shouldActivate) {
        minDeactivateTimer = 0;
      }

      return shouldActivate;
    }

    private const int MIN_DEACTIVATE_TIME = 5;
    private int minDeactivateTimer = 0;

    protected override bool ShouldGestureDeactivate(Hand hand,
                                                    out DeactivationReason?
                                                      deactivationReason) {
      deactivationReason = DeactivationReason.FinishedGesture;

      bool shouldDeactivate = false;

      OnPinchStrengthEvent.Invoke(1f);

      if (minDeactivateTimer > MIN_DEACTIVATE_TIME) {
        var pinchDistance = PinchSegment2SegmentDisplacement(hand).magnitude;

        if (pinchDistance > pinchDeactivateDistance) {
          shouldDeactivate = true;

          if (feedbackMaterial != null) {
            feedbackMaterial.color = inactiveColor;
          }

          if (_drawDebug) {
            DebugPing.Ping(hand.GetPredictedPinchPosition(), Color.black, 0.20f);
          }
        }
      }
      else {
        minDeactivateTimer++;
      }

      if (shouldDeactivate) {
        minReactivateTimer = 0;
      }

      return shouldDeactivate;
    }
    // TODO: OneHandedGesture should implement IPoseGesture AND IStream<Pose> by default!

    protected override void WhenGestureActivated(Hand hand) {
      base.WhenGestureActivated(hand);

      OnOpen();
    }

    protected override void WhileGestureActive(Hand hand) {
      if (_drawDebugPath) {
        DebugPing.Ping(hand.GetPredictedPinchPosition(), LeapColor.amber, 0.05f);
      }

      // TODO: Make this a part of OneHandedGesture so this doesn't have to be explicit!
      OnSend(this.pose);
    }

    protected override void WhenGestureDeactivated(Hand maybeNullHand,
                                                   DeactivationReason reason) {
      pinchStrengthBuffer.Clear();

      OnClose();
    }

    protected override void WhileHandTracked(Hand hand) {

      // Update pose with the position of the pinch, which is theoretical if there's no
      // pinch but absolute when a pinch is actually occuring.
      var pinchPosition = hand.GetPredictedPinchPosition();
      if (pinchStrengthBuffer.Count > 0) {
        var latestPinchStrength = pinchStrengthBuffer.GetLatest();
        var avgIndexThumbTip = ((hand.GetIndex().TipPosition
                                 + hand.GetThumb().TipPosition) / 2f).ToVector3();
        pinchPosition = Vector3.Lerp(pinchPosition, avgIndexThumbTip, latestPinchStrength);
      }
      _lastPinchPose = new Pose() {
        position = pinchPosition,
        rotation = hand.Rotation.ToQuaternion()
      };

      // Reset the "degenerate conditions" timer if we detect that we're looking down
      // the wrist of the hand; here fingers are usually occluded, so we want to ignore
      // pinch information in this case.
      var lookingDownWrist = Vector3.Angle(hand.DistalAxis(),
         hand.PalmPosition.ToVector3() - Camera.main.transform.position) < 25f;
      if (lookingDownWrist) {
        if (_drawDebug) {
          DebugPing.Ping(hand.WristPosition.ToVector3(), Color.black, 0.10f);
        }
        minReactivateSinceDegenerateConditionsTimer = 0;
      }
    }

    #endregion

    #region IPoseGesture

    public Pose pose {
      get {
        return _lastPinchPose;
      }
    }

    #endregion

    #region IStream<Pose>

    public event Action OnOpen = () => { };
    public event Action<Pose> OnSend = (pose) => { };
    public event Action OnClose = () => { };

    #endregion

  }

}