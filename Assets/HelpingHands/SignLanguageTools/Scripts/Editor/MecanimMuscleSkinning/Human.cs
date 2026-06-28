#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace HelpingHandsVR.SignLanguageTools.MecanimMuscleSkinning {

/// <summary>
/// An encapsulation of a Mecanim human biped.
///
/// This is a more abstracted/genericized form than what Unity exposes by default.
/// </summary>
public static class Human
{
    public class LegPose
    {
        public float upperFrontBack;
        public float upperInOut;
        public float upperTwistInOut;
        public float lowerStretch;
        public float lowerInOut;
        public float footStretch;
        public float footInOut;
        public float toesUpDown;

        // [todo]
    }

    public enum Finger
    {
        Thumb,
        Index,
        Middle,
        Ring,
        Little,
    }

    public enum FingerPart
    {
        Proximal,
        Intermediate,
        Distal,
    }

    /// <summary>
    /// Mapping of finger to its mecanim animation name
    /// </summary>
    public static readonly Dictionary<Finger, string> FINGER_MUSCLE_NAME_MAPPING = new()
    {
        {Finger.Thumb, "Thumb"},
        {Finger.Index, "Index"},
        {Finger.Middle, "Middle"},
        {Finger.Ring, "Ring"},
        {Finger.Little, "Little"},
    };

    /// <summary>
    /// Mapping of finger part to the mecanim muscle stretch name
    /// </summary>
    public static readonly Dictionary<FingerPart, string> FINGER_PART_STRETCH_MUSCLE_MAPPING = new()
    {
        {FingerPart.Proximal, "1 Stretched"},
        {FingerPart.Intermediate, "2 Stretched"},
        {FingerPart.Distal, "3 Stretched"},
    };

    public static readonly string FINGER_SPREAD_MUSCLE_MAPPING = "Spread";

    /// <summary>
    /// Mapping of (<is right finger>, <finger>, <finger part>) to the corresponding HumanBodyBones entry
    /// </summary>
    public static readonly Dictionary<(bool, Finger, FingerPart), HumanBodyBones> FINGER_HUMANBODYBONES_MAPPING = new()
    {
        {(false, Finger.Thumb, FingerPart.Proximal), HumanBodyBones.LeftThumbProximal},
        {(false, Finger.Thumb, FingerPart.Intermediate), HumanBodyBones.LeftThumbIntermediate},
        {(false, Finger.Thumb, FingerPart.Distal), HumanBodyBones.LeftThumbDistal},
        {(false, Finger.Index, FingerPart.Proximal), HumanBodyBones.LeftIndexProximal},
        {(false, Finger.Index, FingerPart.Intermediate), HumanBodyBones.LeftIndexIntermediate},
        {(false, Finger.Index, FingerPart.Distal), HumanBodyBones.LeftIndexDistal},
        {(false, Finger.Middle, FingerPart.Proximal), HumanBodyBones.LeftMiddleProximal},
        {(false, Finger.Middle, FingerPart.Intermediate), HumanBodyBones.LeftMiddleIntermediate},
        {(false, Finger.Middle, FingerPart.Distal), HumanBodyBones.LeftMiddleDistal},
        {(false, Finger.Ring, FingerPart.Proximal), HumanBodyBones.LeftRingProximal},
        {(false, Finger.Ring, FingerPart.Intermediate), HumanBodyBones.LeftRingIntermediate},
        {(false, Finger.Ring, FingerPart.Distal), HumanBodyBones.LeftRingDistal},
        {(false, Finger.Little, FingerPart.Proximal), HumanBodyBones.LeftLittleProximal},
        {(false, Finger.Little, FingerPart.Intermediate), HumanBodyBones.LeftLittleIntermediate},
        {(false, Finger.Little, FingerPart.Distal), HumanBodyBones.LeftLittleDistal},

        {(true, Finger.Thumb, FingerPart.Proximal), HumanBodyBones.RightThumbProximal},
        {(true, Finger.Thumb, FingerPart.Intermediate), HumanBodyBones.RightThumbIntermediate},
        {(true, Finger.Thumb, FingerPart.Distal), HumanBodyBones.RightThumbDistal},
        {(true, Finger.Index, FingerPart.Proximal), HumanBodyBones.RightIndexProximal},
        {(true, Finger.Index, FingerPart.Intermediate), HumanBodyBones.RightIndexIntermediate},
        {(true, Finger.Index, FingerPart.Distal), HumanBodyBones.RightIndexDistal},
        {(true, Finger.Middle, FingerPart.Proximal), HumanBodyBones.RightMiddleProximal},
        {(true, Finger.Middle, FingerPart.Intermediate), HumanBodyBones.RightMiddleIntermediate},
        {(true, Finger.Middle, FingerPart.Distal), HumanBodyBones.RightMiddleDistal},
        {(true, Finger.Ring, FingerPart.Proximal), HumanBodyBones.RightRingProximal},
        {(true, Finger.Ring, FingerPart.Intermediate), HumanBodyBones.RightRingIntermediate},
        {(true, Finger.Ring, FingerPart.Distal), HumanBodyBones.RightRingDistal},
        {(true, Finger.Little, FingerPart.Proximal), HumanBodyBones.RightLittleProximal},
        {(true, Finger.Little, FingerPart.Intermediate), HumanBodyBones.RightLittleIntermediate},
        {(true, Finger.Little, FingerPart.Distal), HumanBodyBones.RightLittleDistal},
    };

    public class FingerPose
    {
        public float proximalStretched;
        public float intermediateStretched;
        public float distalStretched;
        public float spread;

        public (string, FingerPart, FingerDof, HumanBodyBones, float)[] ToMappings(Finger finger, bool right)
        {
            var fingerRoot = (right ? "RightHand" : "LeftHand")
                + "." + FINGER_MUSCLE_NAME_MAPPING[finger]
            ;

            return new (string, FingerPart, FingerDof, HumanBodyBones, float)[]
            {
                (
                    fingerRoot + "." + FINGER_PART_STRETCH_MUSCLE_MAPPING[FingerPart.Proximal],
                    FingerPart.Proximal,
                    FingerDof.ProximalDownUp,
                    FINGER_HUMANBODYBONES_MAPPING[(right, finger, FingerPart.Proximal)],
                    proximalStretched
                ),
                (
                    fingerRoot + "." + FINGER_PART_STRETCH_MUSCLE_MAPPING[FingerPart.Intermediate],
                    FingerPart.Intermediate,
                    FingerDof.IntermediateCloseOpen,
                    FINGER_HUMANBODYBONES_MAPPING[(right, finger, FingerPart.Intermediate)],
                    intermediateStretched
                ),
                (
                    fingerRoot + "." + FINGER_PART_STRETCH_MUSCLE_MAPPING[FingerPart.Distal],
                    FingerPart.Distal,
                    FingerDof.DistalCloseOpen,
                    FINGER_HUMANBODYBONES_MAPPING[(right, finger, FingerPart.Distal)],
                    distalStretched
                ),
                (
                    fingerRoot + "." + FINGER_SPREAD_MUSCLE_MAPPING,
                    FingerPart.Proximal,
                    FingerDof.ProximalInOut,
                    FINGER_HUMANBODYBONES_MAPPING[(right, finger, FingerPart.Proximal)],
                    spread
                ),
            };
        }
    }

    public class HandPose
    {
        public FingerPose thumb;
        public FingerPose index;
        public FingerPose middle;
        public FingerPose ring;
        public FingerPose little;

        public float downUp;
        public float inOut;

        public (string, Finger, FingerPart, FingerDof, HumanBodyBones, float)[] ToFingerMappings(bool right)
        {
            return thumb.ToMappings(Finger.Thumb, right).Select(
                (t) => (t.Item1, Finger.Thumb, t.Item2, t.Item3, t.Item4, t.Item5)
            ).Concat(
                index.ToMappings(Finger.Index, right).Select(
                    (t) => (t.Item1, Finger.Index, t.Item2, t.Item3, t.Item4, t.Item5)
                )
            ).Concat(
                middle.ToMappings(Finger.Middle, right).Select(
                    (t) => (t.Item1, Finger.Middle, t.Item2, t.Item3, t.Item4, t.Item5)
                )
            ).Concat(
                ring.ToMappings(Finger.Ring, right).Select(
                    (t) => (t.Item1, Finger.Ring, t.Item2, t.Item3, t.Item4, t.Item5)
                )
            ).Concat(
                little.ToMappings(Finger.Little, right).Select(
                    (t) => (t.Item1, Finger.Little, t.Item2, t.Item3, t.Item4, t.Item5)
                )
            ).ToArray();
        }

        public (string, ArmDof, HumanBodyBones, float)[] ToWristMappings(bool right)
        {
            return new (string, ArmDof, HumanBodyBones, float)[]
            {
                (
                    right ? "Right Hand Down-Up" : "Left Hand Down-Up",
                    ArmDof.HandDownUp,
                    right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand,
                    downUp
                ),
                (
                    right ? "Right Hand In-Out" : "Left Hand In-Out",
                    ArmDof.HandInOut,
                    right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand,
                    inOut
                ),
            };
        }

        public (string, HumanBodyBones, float)[] ToMappings(bool right)
        {
            return ToWristMappings(right).Select(
                (t) => (t.Item1, t.Item3, t.Item4)
            ).Concat(
                ToFingerMappings(right).Select((t) => (t.Item1, t.Item5, t.Item6))
            ).ToArray();
        }
    }

    public class ArmPose
    {
        public HandPose hand;
        public float shoulderDownUp;
        public float shoulderFrontBack;
        public float upperDownUp;
        public float upperFrontBack;
        public float upperTwistInOut;
        public float lowerStretch;
        public float lowerTwistInOut;

        public (string, ArmDof, HumanBodyBones, float)[] ToArmMappings(bool right)
        {
            return new (string, ArmDof, HumanBodyBones, float)[]
            {
                (
                    right ? "Right Shoulder Down-Up" : "Left Shoulder Down-Up",
                    ArmDof.ShoulderDownUp,
                    right ? HumanBodyBones.RightShoulder : HumanBodyBones.LeftShoulder,
                    shoulderDownUp
                ),
                (
                    right ? "Right Shoulder Front-Back" : "Left Shoulder Front-Back",
                    ArmDof.ShoulderFrontBack,
                    right ? HumanBodyBones.RightShoulder : HumanBodyBones.LeftShoulder,
                    shoulderFrontBack
                ),
                (
                    right ? "Right Arm Down-Up" : "Left Arm Down-Up",
                    ArmDof.ArmDownUp,
                    right ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm,
                    upperDownUp
                ),
                (
                    right ? "Right Arm Front-Back" : "Left Arm Front-Back",
                    ArmDof.ArmFrontBack,
                    right ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm,
                    upperFrontBack
                ),
                (
                    right ? "Right Forearm Stretch" : "Left Forearm Stretch",
                    ArmDof.ForeArmCloseOpen,
                    right ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm,
                    lowerStretch
                ),
                (
                    right ? "Right Forearm Twist In-Out" : "Left Forearm Twist In-Out",
                    ArmDof.ForeArmRollInOut,
                    right ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm,
                    lowerTwistInOut
                ),
            };
        }

        public (string, ArmDof, HumanBodyBones, float)[] ToArmAndWristMappings(bool right)
        {
            return ToArmMappings(right).Concat(hand.ToWristMappings(right)).ToArray();
        }

        public (string, HumanBodyBones, float)[] ToMappings(bool right)
        {
            return ToArmMappings(right).Select(
                (t) => (t.Item1, t.Item3, t.Item4)
            ).Concat(
                hand.ToMappings(right)
            ).ToArray();
        }

    }

    public class Pose
    {
        public LegPose leftLeg;
        public LegPose rightLeg;

        public ArmPose leftArm;
        public ArmPose rightArm;
    }
}

}

#endif
