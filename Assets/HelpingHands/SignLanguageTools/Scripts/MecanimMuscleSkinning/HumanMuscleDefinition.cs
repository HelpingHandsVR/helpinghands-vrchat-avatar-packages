
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;

namespace HelpingHandsVR.SignLanguageTools.MecanimMuscleSkinning {

/// <summary>
/// Exported information about a human (Mecanim) muscle definition, obtained from a humanoid Avatar.
///
/// Unity internalizes a lot of the inner workings of Mecanim and retrieving this information in a
/// useful format is often a large hassle. This class is intended to abstract away the awkward
/// dance required to get useful information, and allows you to create a snapshot of the information
/// needed to replicate Mecanim behaviour, serialize it if needed, and work with it in a less
/// ambiguous and complex manner.
/// </summary>
[Serializable]
public class HumanMuscleDefinition
{
#region Internal class definitions
    [Serializable]
    public class HumanMuscleInfo
    {
        // Intrinsic
        public string traitMuscleName;
        public string handleMuscleName;
        public HumanPartDof humanPartDof;
        public int dof;
        public int humanBoneIndex;
    }

    [Serializable]
    public class HumanBoneInfo
    {
        // Intrinsic
        public HumanBodyBones humanBodyBones;
        public string traitBoneName;
        public float axisLength;
        public Quaternion preRotation;
        public Quaternion postRotation;
        public Quaternion zyPostQ;
        public Quaternion zyRoll;
        public Vector3 limitSign;
        public int? xMuscleIndex;
        public int? yMuscleIndex;
        public int? zMuscleIndex;

        // Calculated
        public int? skeletonIndex;
    }

    [Serializable]
    public class HumanSkeletonTransform
    {
        // Intrinsic
        public string name;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public string parentName;

        // Calculated
        public string[] path;
        public int? parentIndex;
        public bool parentAmbiguous;
        public List<int> childIndices;
        public Matrix4x4 localToWorldMatrix;
        public int? humanBoneIndex;

        // Properties
        public Matrix4x4 LocalMatrix {
            get {
                return Matrix4x4.TRS(localPosition, localRotation, localScale);
            }
        }
    }
#endregion

#region Static convenience fields and methods
    /// <summary>
    /// Map of HumanBodyBones to what we would expect its parent to be.
    /// Hips maps to `null`, and LastBone is not included in the dictionary at all.
    /// </summary>
    public static readonly Dictionary<HumanBodyBones, HumanBodyBones?> HUMANBODYBONES_EXPECTED_PARENT = new()
    {
        { HumanBodyBones.Hips, null },
        { HumanBodyBones.LeftUpperLeg, HumanBodyBones.Hips },
        { HumanBodyBones.RightUpperLeg, HumanBodyBones.Hips },
        { HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftUpperLeg },
        { HumanBodyBones.RightLowerLeg, HumanBodyBones.RightUpperLeg },
        { HumanBodyBones.LeftFoot, HumanBodyBones.LeftLowerLeg },
        { HumanBodyBones.RightFoot, HumanBodyBones.RightLowerLeg },
        { HumanBodyBones.Spine, HumanBodyBones.Hips },
        { HumanBodyBones.Chest, HumanBodyBones.Spine },
        { HumanBodyBones.UpperChest, HumanBodyBones.Chest },
        { HumanBodyBones.Neck, HumanBodyBones.UpperChest },
        { HumanBodyBones.Head, HumanBodyBones.Neck },
        { HumanBodyBones.LeftShoulder, HumanBodyBones.UpperChest },
        { HumanBodyBones.RightShoulder, HumanBodyBones.UpperChest },
        { HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftShoulder },
        { HumanBodyBones.RightUpperArm, HumanBodyBones.RightShoulder },
        { HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftUpperArm },
        { HumanBodyBones.RightLowerArm, HumanBodyBones.RightUpperArm },
        { HumanBodyBones.LeftHand, HumanBodyBones.LeftLowerArm },
        { HumanBodyBones.RightHand, HumanBodyBones.RightLowerArm },
        { HumanBodyBones.LeftToes, HumanBodyBones.LeftFoot },
        { HumanBodyBones.RightToes, HumanBodyBones.RightFoot },
        { HumanBodyBones.LeftEye, HumanBodyBones.Head },
        { HumanBodyBones.RightEye, HumanBodyBones.Head },
        { HumanBodyBones.Jaw, HumanBodyBones.Head },
        // Left hand
        { HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftHand },
        { HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbProximal },
        { HumanBodyBones.LeftThumbDistal, HumanBodyBones.LeftThumbIntermediate },
        { HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftHand },
        { HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexProximal },
        { HumanBodyBones.LeftIndexDistal, HumanBodyBones.LeftIndexIntermediate },
        { HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftHand },
        { HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleProximal },
        { HumanBodyBones.LeftMiddleDistal, HumanBodyBones.LeftMiddleIntermediate },
        { HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftHand },
        { HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingProximal },
        { HumanBodyBones.LeftRingDistal, HumanBodyBones.LeftRingIntermediate },
        { HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftHand },
        { HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleProximal },
        { HumanBodyBones.LeftLittleDistal, HumanBodyBones.LeftLittleIntermediate },
        // Right hand
        { HumanBodyBones.RightThumbProximal, HumanBodyBones.RightHand },
        { HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbProximal },
        { HumanBodyBones.RightThumbDistal, HumanBodyBones.RightThumbIntermediate },
        { HumanBodyBones.RightIndexProximal, HumanBodyBones.RightHand },
        { HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexProximal },
        { HumanBodyBones.RightIndexDistal, HumanBodyBones.RightIndexIntermediate },
        { HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightHand },
        { HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleProximal },
        { HumanBodyBones.RightMiddleDistal, HumanBodyBones.RightMiddleIntermediate },
        { HumanBodyBones.RightRingProximal, HumanBodyBones.RightHand },
        { HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingProximal },
        { HumanBodyBones.RightRingDistal, HumanBodyBones.RightRingIntermediate },
        { HumanBodyBones.RightLittleProximal, HumanBodyBones.RightHand },
        { HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleProximal },
        { HumanBodyBones.RightLittleDistal, HumanBodyBones.RightLittleIntermediate },
    };
#endregion

#region Fields and properties
    public HumanMuscleInfo[] muscleInfos;
    public HumanBoneInfo[] boneInfos;
    public HumanSkeletonTransform[] skeletonTransforms;
#endregion

#region Constructor
    public HumanMuscleDefinition(Avatar avatar)
    {
        if (!avatar.isHuman)
            throw new InvalidOperationException("Human Muscle Definition can't be created from Avatar that is not humanoid");

        HumanDescription humanDescription = avatar.humanDescription;

        // Reflection stuff
        var SkeletonBone_parentName_Field = typeof(SkeletonBone).GetField("parentName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        var Avatar_GetAxisLength_Method = typeof(Avatar).GetMethod("GetAxisLength", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var Avatar_GetPreRotation_Method = typeof(Avatar).GetMethod("GetPreRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var Avatar_GetPostRotation_Method = typeof(Avatar).GetMethod("GetPostRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var Avatar_GetZYPostQ_Method = typeof(Avatar).GetMethod("GetZYPostQ", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var Avatar_GetZYRoll_Method = typeof(Avatar).GetMethod("GetZYRoll", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var Avatar_GetLimitSign_Method = typeof(Avatar).GetMethod("GetLimitSign", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Fetch muscle handles
        MuscleHandle[] muscleHandles = new MuscleHandle[MuscleHandle.muscleHandleCount];
        MuscleHandle.GetMuscleHandles(muscleHandles);

        // Calculate muscle info (does not depend on avatar)
        muscleInfos = Enumerable.Range(0, HumanTrait.MuscleCount).Select((muscleIndex) =>
        {
            return new HumanMuscleInfo()
            {
                traitMuscleName = HumanTrait.MuscleName[muscleIndex],
                handleMuscleName = muscleHandles[muscleIndex].name,
                humanPartDof = muscleHandles[muscleIndex].humanPartDof,
                dof = muscleHandles[muscleIndex].dof,
                humanBoneIndex = HumanTrait.BoneFromMuscle(muscleIndex)
            };
        }).ToArray();

        // Generate initial skeleton transforms array
        skeletonTransforms = humanDescription.skeleton.Select((skeletonTransform) =>
        {
            var parentName = (string)SkeletonBone_parentName_Field.GetValue(skeletonTransform);

            return new HumanSkeletonTransform()
            {
                name = skeletonTransform.name,
                localPosition = skeletonTransform.position,
                localRotation = skeletonTransform.rotation,
                localScale = skeletonTransform.scale,
                parentName = string.IsNullOrWhiteSpace(parentName) ? null : parentName,

                parentIndex = null,
                parentAmbiguous = false,
                childIndices = new List<int>(),
            };
        }).ToArray();

        // Establish parent and child relationships
        for (int index = 0; index < skeletonTransforms.Length; ++index)
        {
            if (skeletonTransforms[index].parentName != null)
            {
                var parentMatches = skeletonTransforms
                    .Select((value, index) => (value, index))
                    .Where((p) => p.value.name == skeletonTransforms[index].parentName)
                    .ToList()
                ;

                // If there is not exactly 1 parent match, the parent is ambiguous
                skeletonTransforms[index].parentAmbiguous = parentMatches.Count != 1;

                if (parentMatches.Count > 0)
                {
                    skeletonTransforms[index].parentIndex = parentMatches[0].index;
                    parentMatches[0].value.childIndices.Add(index);
                }
            }
        }

        // Calculate nested fields
        for (int index = 0; index < skeletonTransforms.Length; ++index)
        {
            var reversePath = new List<string>() { skeletonTransforms[index].name };
            var localToWorldMatrix = skeletonTransforms[index].LocalMatrix;

            var maybeParent = skeletonTransforms[index].parentIndex;

            while (maybeParent != null)
            {
                // Necessary because C# is dumb
                int parent = maybeParent ?? 0;

                reversePath.Add(skeletonTransforms[parent].name);
                localToWorldMatrix = skeletonTransforms[parent].LocalMatrix * localToWorldMatrix;
                maybeParent = skeletonTransforms[parent].parentIndex;
            }

            reversePath.Reverse();
            skeletonTransforms[index].path = reversePath.ToArray();
            skeletonTransforms[index].localToWorldMatrix = localToWorldMatrix;
        }

        // Create lookup
        var skeletonNameLookup = skeletonTransforms.Select((value, index) => (value, index)).ToDictionary(
            (st) => st.value.name
        );
        var humanToSkeletonLookup = humanDescription.human.ToDictionary((b) => b.humanName);

        // Assign bone information
        boneInfos = Enumerable.Range(0, HumanTrait.BoneCount).Select((boneIndex) =>
        {
            var humanBodyBones = (HumanBodyBones)boneIndex;

            int xMuscleIndex = HumanTrait.MuscleFromBone(boneIndex, 0);
            int yMuscleIndex = HumanTrait.MuscleFromBone(boneIndex, 1);
            int zMuscleIndex = HumanTrait.MuscleFromBone(boneIndex, 2);

            int? skeletonIndex = null;

            if (humanToSkeletonLookup.TryGetValue(HumanTrait.BoneName[boneIndex], out HumanBone boneMapped))
            {
                skeletonIndex = skeletonNameLookup[boneMapped.boneName].index;
            }

            return new HumanBoneInfo() {
                humanBodyBones = humanBodyBones,
                traitBoneName = HumanTrait.BoneName[boneIndex],
                axisLength = (float)Avatar_GetAxisLength_Method.Invoke(avatar, new object[]{ humanBodyBones }),
                preRotation = (Quaternion)Avatar_GetPreRotation_Method.Invoke(avatar, new object[]{ humanBodyBones }),
                postRotation = (Quaternion)Avatar_GetPostRotation_Method.Invoke(avatar, new object[]{ humanBodyBones }),
                zyPostQ = (Quaternion)Avatar_GetZYPostQ_Method.Invoke(avatar, new object[]{
                    humanBodyBones,
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2022.3/Editor/Mono/Inspector/Avatar/AvatarMuscleEditor.cs#L1062
                    skeletonIndex != null && skeletonTransforms[skeletonIndex ?? 0].parentIndex != null ?
                        skeletonTransforms[skeletonTransforms[skeletonIndex ?? 0].parentIndex ?? 0].localToWorldMatrix.rotation :
                        Quaternion.identity,
                    skeletonIndex != null ? skeletonTransforms[skeletonIndex ?? 0].localToWorldMatrix.rotation : Quaternion.identity,
                }),
                zyRoll = (Quaternion)Avatar_GetZYRoll_Method.Invoke(avatar, new object[]{ humanBodyBones, Vector3.zero }),
                limitSign = (Vector3)Avatar_GetLimitSign_Method.Invoke(avatar, new object[]{ humanBodyBones }),
                xMuscleIndex = xMuscleIndex >= 0 ? xMuscleIndex : null,
                yMuscleIndex = yMuscleIndex >= 0 ? yMuscleIndex : null,
                zMuscleIndex = zMuscleIndex >= 0 ? zMuscleIndex : null,

                skeletonIndex = skeletonIndex
            };
        }).ToArray();

    }
#endregion

#region Methods

#endregion

}

}
