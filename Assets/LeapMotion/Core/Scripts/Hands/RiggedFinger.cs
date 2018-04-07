/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity {

  /// <summary>
  /// Manages the position and orientation of the bones in a model rigged for skeletal
  /// animation.
  ///  
  /// The class expects that the graphics model's bones that correspond to the bones in
  /// the Leap Motion hand model are in the same order in the bones array.
  /// </summary>
  public class RiggedFinger : FingerModel {
    
    /// <summary>
    /// Allows the mesh to be stretched to align with finger joint positions.
    /// Only set to true when mesh is not visible.
    /// </summary>
    [HideInInspector]
    public bool deformPosition = false;

    [HideInInspector]
    public bool scaleLastFingerBone = false;

    public Vector3 modelFingerPointing = Vector3.forward;
    public Vector3 modelPalmFacing = -Vector3.up;

    public Quaternion Reorientation() {
      return Quaternion.Inverse(Quaternion.LookRotation(modelFingerPointing, -modelPalmFacing));
    }
    
    /// <summary> Backing store for s_standardFingertipLengths, don't touch. </summary>
    private static float[] s_backingStandardFingertipLengths = null;
    /// <summary>
    /// Lazily-calculated fingertip lengths for the standard edit-time hand.
    /// </summary>
    private static float[] s_standardFingertipLengths {
      get {
        if (s_backingStandardFingertipLengths == null) {
          // Calculate standard fingertip lengths.
          s_backingStandardFingertipLengths = new float[5];
          var testHand = TestHandFactory.MakeTestHand(isLeft: true,
                           unitType: TestHandFactory.UnitType.UnityUnits);
          for (int i = 0; i < 5; i++) {
            var fingertipBone = testHand.Fingers[i].bones[3];
            s_backingStandardFingertipLengths[i] = fingertipBone.Length;
          }
        }
        return s_backingStandardFingertipLengths;
      }
    }

    /// <summary>
    /// Updates model bone positions and rotations based on tracked hand data.
    /// </summary>
    public override void UpdateFinger() {
      for (int i = 0; i < bones.Length; ++i) {
        if (bones[i] != null) {
          bones[i].rotation = GetBoneRotation(i) * Reorientation();
          if (deformPosition) {
            var boneRootPos = GetJointPosition(i);
            bones[i].position = boneRootPos;

            if (i == 3 && scaleLastFingerBone) {
              // Set fingertip base bone scale to match the bone length to the fingertip.
              // This will only scale correctly if the model was constructed to match
              // the standard "test" edit-time hand model from the TestHandFactory.
              var boneTipPos = GetJointPosition(i + 1);
              var boneVec = boneTipPos - boneRootPos;
              var boneLen = boneVec.magnitude;
              var standardLen = s_standardFingertipLengths[(int)this.fingerType];
              var newScale = bones[i].transform.localScale;
              newScale.x = boneLen / standardLen;
              bones[i].transform.localScale = newScale;
              if (this.fingerType == Finger.FingerType.TYPE_INDEX
                  && this.hand_ != null && this.hand_.IsLeft
                  && Application.isPlaying) {
                Debug.Log("Set left index tip bone scale to: " + newScale);
              }
            }
          }
        }
      }
    }

    public void SetupRiggedFinger (bool useMetaCarpals) {
      findBoneTransforms(useMetaCarpals);
      modelFingerPointing = calulateModelFingerPointing();
    }

    private void findBoneTransforms(bool useMetaCarpals) {
      if (!useMetaCarpals || fingerType == Finger.FingerType.TYPE_THUMB) {
        bones[1] = transform;
        bones[2] = transform.GetChild(0).transform;
        bones[3] = transform.GetChild(0).transform.GetChild(0).transform;
      }
      else {
        bones[0] = transform;
        bones[1] = transform.GetChild(0).transform;
        bones[2] = transform.GetChild(0).transform.GetChild(0).transform;
        bones[3] = transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform;

      }
    }

    private Vector3 calulateModelFingerPointing() {
      Vector3 distance = transform.InverseTransformPoint(transform.position) - transform.InverseTransformPoint(transform.GetChild(0).transform.position);
      Vector3 zeroed = RiggedHand.CalculateZeroedVector(distance);
      return zeroed;
    }

  } 
}
