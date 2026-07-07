
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HelpingHandsVR.SignLanguageTools.Debug {

public class MecanimAvatarDebug : MonoBehaviour
{
    public Animator target;

    [NonSerialized]
    public bool genericMecanimBones_Show = false;
    [NonSerialized]
    public Vector2 genericMecanimBones_Scroll;
    [NonSerialized]
    public bool genericMecanimMuscles_Show = false;
    [NonSerialized]
    public Vector2 genericMecanimMuscles_Scroll;
    [NonSerialized]
    public bool muscleDefinitionInfo_Show = false;
    [NonSerialized]
    public Vector2 muscleDefinitionInfo_Scroll;


    [NonSerialized]
    public bool lineSkeleton_Gizmo = false;
    [NonSerialized]
    public Vector2 lineSkeleton_Scroll;
    [NonSerialized]
    public Color lineSkeleton_Color = Color.blue;
    [NonSerialized]
    public float lineSkeleton_BallSize = 0.0025f;

    [NonSerialized]
    public bool poseSkeleton_Gizmo = false;
    [NonSerialized]
    public Vector2 poseSkeleton_Scroll;
    [NonSerialized]
    public Color poseSkeleton_Color = Color.yellow;
    [NonSerialized]
    public float poseSkeleton_BallSize = 0.0025f;
    [NonSerialized]
    public float poseSkeleton_GlobalMuscleValue = 0.0f;
    [NonSerialized]
    public float[] poseSkeleton_MuscleValues = null;

    [NonSerialized]
    public bool quaternionProbe_Gizmo = false;
    [NonSerialized]
    public bool quaternionProbe_SkeletonGizmo = false;
    [NonSerialized]
    public Color quaternionProbe_SkeletonColor = Color.magenta;
    [NonSerialized]
    public float quaternionProbe_SkeletonBallSize = 0.0025f;

    public enum QuaternionProbeRotationKind
    {
        CustomEuler,
        CustomQuaternion,
        RigRotation,
        PreRotation,
        PostRotation,
        ZYPostQ,
        ZYRoll,
        CopiedFromPoseSkeleton,
    }

    public class QuaternionProbeStep
    {
        public QuaternionProbeRotationKind kind;
        public bool inverse;
        public bool appliesToEnd;
        public bool appliesToParents;
        public bool appliesToMuscles;
        public bool appliesToNonMuscles;
        public Vector3 customEulerValue;
        public Quaternion customQuaternionValue;

        public QuaternionProbeStep(QuaternionProbeRotationKind kind, bool inverse, bool appliesToEnd, bool appliesToParents, bool appliesToMuscles, bool appliesToNonMuscles)
        {
            this.kind = kind;
            this.inverse = inverse;
            this.appliesToEnd = appliesToEnd;
            this.appliesToParents = appliesToParents;
            this.appliesToMuscles = appliesToMuscles;
            this.appliesToNonMuscles = appliesToNonMuscles;
            this.customEulerValue = Vector3.zero;
            this.customQuaternionValue = Quaternion.identity;
        }

        public QuaternionProbeStep(QuaternionProbeRotationKind kind) : this(kind, false, true, true, true, true)
        {
        }

        public QuaternionProbeStep(Vector3 customEulerValue, bool inverse, bool appliesToEnd, bool appliesToParents, bool appliesToMuscles, bool appliesToNonMuscles)
        {
            this.kind = QuaternionProbeRotationKind.CustomEuler;
            this.inverse = inverse;
            this.appliesToEnd = appliesToEnd;
            this.appliesToParents = appliesToParents;
            this.appliesToMuscles = appliesToMuscles;
            this.appliesToNonMuscles = appliesToNonMuscles;
            this.customEulerValue = customEulerValue;
            this.customQuaternionValue = Quaternion.identity;
        }

        public QuaternionProbeStep(Vector3 customEulerValue) : this(customEulerValue, false, true, true, true, true)
        {
        }

        public QuaternionProbeStep(Quaternion customQuaternionValue, bool inverse, bool appliesToEnd, bool appliesToParents, bool appliesToMuscles, bool appliesToNonMuscles)
        {
            this.kind = QuaternionProbeRotationKind.CustomQuaternion;
            this.inverse = inverse;
            this.appliesToEnd = appliesToEnd;
            this.appliesToParents = appliesToParents;
            this.appliesToMuscles = appliesToMuscles;
            this.appliesToNonMuscles = appliesToNonMuscles;
            this.customEulerValue = Vector3.zero;
            this.customQuaternionValue = customQuaternionValue;
        }

        public QuaternionProbeStep(Quaternion customQuaternionValue) : this(customQuaternionValue, false, true, true, true, true)
        {
        }

        public Quaternion GetRotation(MecanimMuscleSkinning.HumanMuscleDefinition muscleDefinition, int skeletonIndex, float[] muscleValues)
        {
            var st = muscleDefinition.skeletonTransforms[skeletonIndex];

            var isMuscle = st.humanBoneIndex != null;

            if ((!appliesToMuscles && isMuscle) || (!appliesToNonMuscles && !isMuscle))
                return Quaternion.identity;

            Quaternion stepQuaternion = Quaternion.identity;

            switch (kind)
            {
                case QuaternionProbeRotationKind.CustomEuler:
                    stepQuaternion = Quaternion.Euler(customEulerValue);
                    break;
                case QuaternionProbeRotationKind.CustomQuaternion:
                    stepQuaternion = customQuaternionValue;
                    break;
                case QuaternionProbeRotationKind.RigRotation:
                    stepQuaternion = st.localRotation;
                    break;
                case QuaternionProbeRotationKind.PreRotation:
                    if (st.humanBoneIndex != null)
                    {
                        stepQuaternion = muscleDefinition.boneInfos[st.humanBoneIndex ?? 0].preRotation;
                    }
                    break;
                case QuaternionProbeRotationKind.PostRotation:
                    if (st.humanBoneIndex != null)
                    {
                        stepQuaternion = muscleDefinition.boneInfos[st.humanBoneIndex ?? 0].postRotation;
                    }
                    break;
                case QuaternionProbeRotationKind.ZYPostQ:
                    if (st.humanBoneIndex != null)
                    {
                        stepQuaternion = muscleDefinition.boneInfos[st.humanBoneIndex ?? 0].zyPostQ;
                    }
                    break;
                case QuaternionProbeRotationKind.ZYRoll:
                    if (st.humanBoneIndex != null)
                    {
                        stepQuaternion = muscleDefinition.boneInfos[st.humanBoneIndex ?? 0].zyRoll;
                    }
                    break;
                case QuaternionProbeRotationKind.CopiedFromPoseSkeleton:
                    if (st.humanBoneIndex != null)
                    {
                        var boneInfo = muscleDefinition.boneInfos[st.humanBoneIndex ?? 0];

                        float xValue = 0.0f;
                        float yValue = 0.0f;
                        float zValue = 0.0f;

                        if (boneInfo.xMuscleIndex != null && muscleValues != null && muscleValues.Length > boneInfo.xMuscleIndex)
                        {
                            var muscleInfo = muscleDefinition.muscleInfos[boneInfo.xMuscleIndex ?? 0];
                            var muscleValue = muscleValues[boneInfo.xMuscleIndex ?? 0];

                            var min = boneInfo.useDefaultRange ? muscleInfo.defaultRangeMin : boneInfo.customRangeMin.x;
                            var max = boneInfo.useDefaultRange ? muscleInfo.defaultRangeMax : boneInfo.customRangeMax.x;
                            var center = boneInfo.useDefaultRange ? 0.0f : boneInfo.customRangeCenter.x;
                            xValue = center + boneInfo.limitSign.x * (muscleValue >= 0.0f ? (max - center) * muscleValue : (center - min) * muscleValue);
                        }

                        if (boneInfo.yMuscleIndex != null && muscleValues != null && muscleValues.Length > boneInfo.yMuscleIndex)
                        {
                            var muscleInfo = muscleDefinition.muscleInfos[boneInfo.yMuscleIndex ?? 0];
                            var muscleValue = muscleValues[boneInfo.yMuscleIndex ?? 0];

                            var min = boneInfo.useDefaultRange ? muscleInfo.defaultRangeMin : boneInfo.customRangeMin.x;
                            var max = boneInfo.useDefaultRange ? muscleInfo.defaultRangeMax : boneInfo.customRangeMax.x;
                            var center = boneInfo.useDefaultRange ? 0.0f : boneInfo.customRangeCenter.x;
                            yValue = center + boneInfo.limitSign.y * (muscleValue >= 0.0f ? (max - center) * muscleValue : (center - min) * muscleValue);
                        }

                        if (boneInfo.zMuscleIndex != null && muscleValues != null && muscleValues.Length > boneInfo.zMuscleIndex)
                        {
                            var muscleInfo = muscleDefinition.muscleInfos[boneInfo.zMuscleIndex ?? 0];
                            var muscleValue = muscleValues[boneInfo.zMuscleIndex ?? 0];

                            var min = boneInfo.useDefaultRange ? muscleInfo.defaultRangeMin : boneInfo.customRangeMin.x;
                            var max = boneInfo.useDefaultRange ? muscleInfo.defaultRangeMax : boneInfo.customRangeMax.x;
                            var center = boneInfo.useDefaultRange ? 0.0f : boneInfo.customRangeCenter.x;
                            zValue = center + boneInfo.limitSign.z * (muscleValue >= 0.0f ? (max - center) * muscleValue : (center - min) * muscleValue);
                        }

                        stepQuaternion = Quaternion.Euler(
                            xValue, yValue, zValue
                        );
                    }
                    break;
                default:
                    break;
            }

            return inverse ? Quaternion.Inverse(stepQuaternion) : stepQuaternion;
        }
    }

    [NonSerialized]
    public List<QuaternionProbeStep> quaternionProbe_RotationSteps = new()
    {
        new(QuaternionProbeRotationKind.RigRotation)
    };
    [NonSerialized]
    public int quaternionProbe_Index = 0;
    [NonSerialized]
    public bool quaternionProbe_IgnoreAbsoluteParent = false;
    [NonSerialized]
    public bool quaternionProbe_ShiftByMass = false;
    [NonSerialized]
    public bool quaternionProbe_RotateByAlignment = false;


#if UNITY_EDITOR
    [MenuItem("CONTEXT/Animator/HelpingHandsVR/Debug/PrintMecanimInfo")]
#endif
    static void Debug_PrintInfo_Command(MenuCommand command)
    {
        Animator animator = (Animator)command.context;
        Debug_PrintInfo(animator);
    }

    static void Debug_PrintInfo(Animator animator)
    {
        Avatar animatorAvatar = animator.avatar;

        MuscleHandle[] muscleHandles = new MuscleHandle[MuscleHandle.muscleHandleCount];
        MuscleHandle.GetMuscleHandles(muscleHandles);

        UnityEngine.Debug.Log(
            $"Muscle handles: {muscleHandles.Length}\n" +
            string.Join("\n", muscleHandles.Select((v) => $"{v.name} <humanPartDof={v.humanPartDof}, dof={v.dof}>"))
        );

        string[] muscleNames = Enumerable.Range(0, HumanTrait.MuscleCount).Select((i) => HumanTrait.MuscleName[i]).ToArray();

        UnityEngine.Debug.Log(
            $"Muscle names: {muscleNames.Length}\n" +
            string.Join("\n", muscleNames)
        );

        string[] boneNames = Enumerable.Range(0, HumanTrait.BoneCount).Select((i) => HumanTrait.BoneName[i]).ToArray();

        UnityEngine.Debug.Log(
            $"Bone names: {boneNames.Length}\n" +
            string.Join("\n", boneNames)
        );

        UnityEngine.Debug.Log("Done");
    }

    void OnDrawGizmosSelected()
    {
        if (!target || target == null || !target.avatar || target.avatar == null || !target.avatar.isHuman)
        {
            return;
        }

        var oldGizmo_Color = Gizmos.color;
        var oldGizmo_Matrix = Gizmos.matrix;
        var oldHandle_Color = Handles.color;
        var oldHandle_Matrix = Handles.matrix;

        var avatar = target.avatar;
        var muscleDefinition = new MecanimMuscleSkinning.HumanMuscleDefinition(avatar);

        if (lineSkeleton_Gizmo) {
            foreach (var st in muscleDefinition.skeletonTransforms)
            {
                Gizmos.color = lineSkeleton_Color;

                var positionOfThis = (target.transform.localToWorldMatrix * st.localToWorldMatrix).MultiplyPoint(Vector3.zero);

                if (st.parentIndex != null)
                {
                    var positionOfParent = (target.transform.localToWorldMatrix * muscleDefinition.skeletonTransforms[st.parentIndex ?? 0].localToWorldMatrix).MultiplyPoint(Vector3.zero);

                    Gizmos.DrawLine(
                        positionOfParent,
                        positionOfThis
                    );
                }

                Gizmos.DrawSphere((target.transform.localToWorldMatrix * st.localToWorldMatrix).MultiplyPoint(Vector3.zero), lineSkeleton_BallSize);
            }
        }

        (MecanimMuscleSkinning.HumanMuscleDefinition.HumanSkeletonTransform skeletonTransform, Vector3 localPosedPosition, Quaternion localPosedRotation, Matrix4x4 posedLocalToWorldMatrix)[] maybePosed = null;

        if (poseSkeleton_Gizmo)
        {
            HumanPose pose = new()
            {
                muscles = poseSkeleton_MuscleValues,
                bodyPosition = Vector3.zero,
                bodyRotation = Quaternion.identity,
            };

            var posed = muscleDefinition.ApplyPose(avatar, pose);
            maybePosed = posed;

            foreach (var tuple in posed)
            {
                Gizmos.color = poseSkeleton_Color;

                var positionOfThis = (target.transform.localToWorldMatrix * tuple.posedLocalToWorldMatrix).MultiplyPoint(Vector3.zero);

                if (tuple.skeletonTransform.parentIndex != null)
                {
                    var positionOfParent = (target.transform.localToWorldMatrix * posed[tuple.skeletonTransform.parentIndex ?? 0].posedLocalToWorldMatrix).MultiplyPoint(Vector3.zero);

                    Gizmos.DrawLine(
                        positionOfParent,
                        positionOfThis
                    );
                }

                Gizmos.DrawSphere((target.transform.localToWorldMatrix * tuple.posedLocalToWorldMatrix).MultiplyPoint(Vector3.zero), poseSkeleton_BallSize);
            }
        }

        if (quaternionProbe_Gizmo)
        {
            var parentPartMatrices = Enumerable.Range(0, muscleDefinition.skeletonTransforms.Length).Select((skeletonIndex) =>
            {
                var st = muscleDefinition.skeletonTransforms[skeletonIndex];

                Quaternion calculatedRotation = Quaternion.identity;

                foreach (var step in quaternionProbe_RotationSteps)
                {
                    if (!step.appliesToParents)
                        continue;

                    calculatedRotation *= step.GetRotation(muscleDefinition, skeletonIndex, poseSkeleton_MuscleValues);
                }

                return Matrix4x4.TRS(
                    st.localPosition,
                    calculatedRotation,
                    st.localScale
                );
            }).ToList();

            var endPartMatrices = Enumerable.Range(0, muscleDefinition.skeletonTransforms.Length).Select((skeletonIndex) =>
            {
                var st = muscleDefinition.skeletonTransforms[skeletonIndex];

                Quaternion calculatedRotation = Quaternion.identity;

                foreach (var step in quaternionProbe_RotationSteps)
                {
                    if (!step.appliesToEnd)
                        continue;

                    calculatedRotation *= step.GetRotation(muscleDefinition, skeletonIndex, poseSkeleton_MuscleValues);
                }

                return Matrix4x4.TRS(
                    st.localPosition,
                    calculatedRotation,
                    st.localScale
                );
            }).ToList();

            var worldMatrices = Enumerable.Range(0, muscleDefinition.skeletonTransforms.Length).Select((index) =>
            {
                Gizmos.color = quaternionProbe_SkeletonColor;

                List<int> hierarchyIndices = new() { index };
                hierarchyIndices.AddRange(muscleDefinition.skeletonTransforms[index].parentIndices);

                var matrixStack = hierarchyIndices.Select(
                    (matrixIndex, hierarchyIndex) => hierarchyIndex == 0 ? endPartMatrices[matrixIndex] : parentPartMatrices[matrixIndex]
                ).ToList();

                if (quaternionProbe_IgnoreAbsoluteParent && matrixStack.Count > 1)
                {
                    matrixStack.RemoveAt(matrixStack.Count - 1);
                }

                // Accumulate matrices backwards
                for (int i = 1; i < matrixStack.Count; ++i)
                {
                    matrixStack[matrixStack.Count - 1 - i] = matrixStack[matrixStack.Count - i] * matrixStack[matrixStack.Count - 1 - i];
                }

                var positionOfThis = (target.transform.localToWorldMatrix * matrixStack[0]).MultiplyPoint(Vector3.zero);

                if (matrixStack.Count > 1)
                {
                    var positionOfParent = (target.transform.localToWorldMatrix * matrixStack[1]).MultiplyPoint(Vector3.zero);
                }

                return matrixStack[0];
            }).ToList();

            if (quaternionProbe_RotateByAlignment)
            {
                var leftHip = worldMatrices[muscleDefinition.boneInfos[(int)HumanBodyBones.LeftUpperLeg].skeletonIndex ?? 0].MultiplyPoint(Vector3.zero);
                var rightHip = worldMatrices[muscleDefinition.boneInfos[(int)HumanBodyBones.RightUpperLeg].skeletonIndex ?? 0].MultiplyPoint(Vector3.zero);
                var leftShoulder = worldMatrices[muscleDefinition.boneInfos[(int)HumanBodyBones.LeftUpperArm].skeletonIndex ?? 0].MultiplyPoint(Vector3.zero);
                var rightShoulder = worldMatrices[muscleDefinition.boneInfos[(int)HumanBodyBones.RightUpperArm].skeletonIndex ?? 0].MultiplyPoint(Vector3.zero);

                var middleHip = (leftHip + rightHip) * 0.5f;
                var middleShoulder = (leftShoulder + rightShoulder) * 0.5f;

                var upVector = (middleShoulder - middleHip).normalized;

                var acrossHip = rightHip - leftHip;
                var acrossShoulder = rightShoulder - leftShoulder;

                var rightVector = (acrossHip + acrossShoulder).normalized;

                // Get z
                var forwardVector = Vector3.Cross(rightVector, upVector);

                // Orthogonalize
                var newRightVector = rightVector; //Vector3.Cross(upVector, forwardVector);
                var newUpVector = upVector; //Vector3.Cross(forwardVector, rightVector);
                var newForwardVector = forwardVector;

                Vector3.OrthoNormalize(ref newRightVector, ref newUpVector, ref newForwardVector);

                var alignmentMatrix = new Matrix4x4(
                    new Vector4(newRightVector.x, newRightVector.y, newRightVector.z, 0.0f),
                    new Vector4(newUpVector.x, newUpVector.y, newUpVector.z, 0.0f),
                    new Vector4(newForwardVector.x, newForwardVector.y, newForwardVector.z, 0.0f),
                    new Vector4(0.0f, 0.0f, 0.0f, 1.0f)
                );
                var correctionMatrix = alignmentMatrix.inverse;

                for (int i = 0; i < worldMatrices.Count; ++i)
                {
                    worldMatrices[i] = correctionMatrix * worldMatrices[i];
                }
            }

            if (quaternionProbe_ShiftByMass)
            {
                Vector3 shiftAmount = Vector3.zero;

                for (int i = 0; i < muscleDefinition.skeletonTransforms.Length; ++i)
                {
                    var st = muscleDefinition.skeletonTransforms[i];

                    if (st.humanBoneIndex == null)
                        continue;

                    shiftAmount += muscleDefinition.boneInfos[st.humanBoneIndex ?? 0].mass * worldMatrices[i].GetPosition();
                }

                for (int i = 0; i < worldMatrices.Count; ++i)
                {
                    worldMatrices[i] = Matrix4x4.Translate(-shiftAmount) * worldMatrices[i];
                }
            }

            if (quaternionProbe_SkeletonGizmo)
            {
                Gizmos.color = quaternionProbe_SkeletonColor;

                for (int i = 0; i < muscleDefinition.skeletonTransforms.Length; ++i)
                {
                    var st = muscleDefinition.skeletonTransforms[i];

                    var positionOfThis = (target.transform.localToWorldMatrix * worldMatrices[i]).MultiplyPoint(Vector3.zero);

                    if (st.parentIndex != null)
                    {
                        var positionOfParent = (target.transform.localToWorldMatrix * worldMatrices[st.parentIndex ?? 0]).MultiplyPoint(Vector3.zero);

                        Gizmos.DrawLine(
                            positionOfParent,
                            positionOfThis
                        );
                    }

                    Gizmos.DrawSphere((target.transform.localToWorldMatrix * worldMatrices[i]).MultiplyPoint(Vector3.zero), quaternionProbe_SkeletonBallSize);
                }
            }

            Handles.matrix = transform.localToWorldMatrix * worldMatrices[quaternionProbe_Index];
            Handles.RotationHandle(Quaternion.identity, Vector3.zero);

            GUIStyle qStyle = new()
            {
                richText = true
            };
            qStyle.normal.textColor = quaternionProbe_SkeletonColor;
            var qMat = worldMatrices[quaternionProbe_Index];
            var qPos = qMat.GetPosition();
            var qRot = qMat.rotation.eulerAngles;
            var qScale = qMat.lossyScale;
            Handles.Label(
                Vector3.zero,
                $"<b>POS:</b> ({qPos.x:0.00000}, {qPos.y:0.00000}, {qPos.z:0.00000})\n<b>ROT:</b> ({qRot.x:0.00000}, {qRot.y:0.00000}, {qRot.z:0.00000})\n<b>SCL:</b> ({qScale.x:0.00000}, {qScale.y:0.00000}, {qScale.z:0.00000})",
                qStyle
            );

            if (maybePosed != null)
            {
                Handles.matrix = transform.localToWorldMatrix * maybePosed[quaternionProbe_Index].posedLocalToWorldMatrix;
                Handles.RotationHandle(Quaternion.identity, Vector3.zero);

                GUIStyle pStyle = new()
                {
                    richText = true
                };
                pStyle.normal.textColor = poseSkeleton_Color;
                var pMat = maybePosed[quaternionProbe_Index].posedLocalToWorldMatrix;
                var pPos = pMat.GetPosition();
                var pRot = pMat.rotation.eulerAngles;
                var pScale = pMat.lossyScale;
                Handles.Label(
                    Vector3.zero,
                    $"<b>POS:</b> ({pPos.x:0.00000}, {pPos.y:0.00000}, {pPos.z:0.00000})\n<b>ROT:</b> ({pRot.x:0.00000}, {pRot.y:0.00000}, {pRot.z:0.00000})\n<b>SCL:</b> ({pScale.x:0.00000}, {pScale.y:0.00000}, {pScale.z:0.00000})",
                    pStyle
                );

            }
        }

        Gizmos.color = oldGizmo_Color;
        Gizmos.matrix = oldGizmo_Matrix;
        Handles.color = oldHandle_Color;
        Handles.matrix = oldHandle_Matrix;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MecanimAvatarDebug))]
    public class MecanimAvatarDebugEditor : Editor
    {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                var settings = target as MecanimAvatarDebug;

                EditorGUILayout.Separator();

                if (!settings.target)
                {
                    EditorGUILayout.HelpBox("Select an animator to target", MessageType.Warning);
                    return;
                }

                if (!settings.target.avatar)
                {
                    EditorGUILayout.HelpBox("Select an animator that has an Avatar", MessageType.Warning);
                    return;
                }

                if (!settings.target.avatar.isHuman)
                {
                    EditorGUILayout.HelpBox("Select an animator whose Avatar is Humanoid", MessageType.Warning);
                    return;
                }

                var avatar = settings.target.avatar;
                var humanDescription = avatar.humanDescription;

                if (GUILayout.Button("Print debug info"))
                {
                    Debug_PrintInfo(settings.target);
                }

                if (GUILayout.Button("Export human muscle definition"))
                {
                    string tempFilePath = System.IO.Path.GetTempFileName();
                    System.IO.File.WriteAllText(tempFilePath, JsonUtility.ToJson(new MecanimMuscleSkinning.HumanMuscleDefinition(settings.target.avatar), true));
                    UnityEngine.Debug.Log("Human muscle definition exported to: " + tempFilePath);
                }

                EditorGUILayout.Separator();

                GUIStyle mixedStyle = new(EditorStyles.label)
                {
                    richText = true
                };

                MuscleHandle[] muscleHandles = new MuscleHandle[MuscleHandle.muscleHandleCount];
                MuscleHandle.GetMuscleHandles(muscleHandles);
                Dictionary<string, HumanBone> boneMap = humanDescription.human.ToDictionary((b) => b.humanName);
                var muscleDefinition = new MecanimMuscleSkinning.HumanMuscleDefinition(avatar);

                settings.genericMecanimBones_Show = EditorGUILayout.Foldout(settings.genericMecanimBones_Show, "Show generic Mecanim bones");
                if (settings.genericMecanimBones_Show) {
                    EditorGUI.indentLevel++;
                    settings.genericMecanimBones_Scroll = EditorGUILayout.BeginScrollView(settings.genericMecanimBones_Scroll, GUILayout.Height(400.0f));
                    for (int i = 0; i < HumanTrait.BoneCount; ++i)
                    {
                        EditorGUILayout.BeginVertical("box");

                        EditorGUILayout.BeginHorizontal("AC BoldHeader");
                        EditorGUILayout.LabelField($"[{i}] <b>{HumanTrait.BoneName[i]}</b>", mixedStyle);
                        EditorGUILayout.EndHorizontal();

                        var x_muscle = HumanTrait.MuscleFromBone(i, 0);
                        var y_muscle = HumanTrait.MuscleFromBone(i, 1);
                        var z_muscle = HumanTrait.MuscleFromBone(i, 2);
                        EditorGUILayout.LabelField($"<b>X Muscle:</b>  {x_muscle}{(x_muscle == -1 ? "" : " (" + HumanTrait.MuscleName[x_muscle] + ")")}", mixedStyle);
                        EditorGUILayout.LabelField($"<b>Y Muscle:</b>  {y_muscle}{(y_muscle == -1 ? "" : " (" + HumanTrait.MuscleName[y_muscle] + ")")}", mixedStyle);
                        EditorGUILayout.LabelField($"<b>Z Muscle:</b>  {z_muscle}{(z_muscle == -1 ? "" : " (" + HumanTrait.MuscleName[z_muscle] + ")")}", mixedStyle);
                        ShowBoneInfo(avatar, boneMap, muscleDefinition, i);

                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndScrollView();
                    EditorGUI.indentLevel--;
                }

                settings.genericMecanimMuscles_Show = EditorGUILayout.Foldout(settings.genericMecanimMuscles_Show, "Show generic Mecanim muscles");
                if (settings.genericMecanimMuscles_Show) {
                    EditorGUI.indentLevel++;
                    settings.genericMecanimMuscles_Scroll = EditorGUILayout.BeginScrollView(settings.genericMecanimMuscles_Scroll, GUILayout.Height(400.0f));
                    for (int i = 0; i < HumanTrait.MuscleCount; ++i)
                    {
                        EditorGUILayout.BeginVertical("box");

                        EditorGUILayout.BeginHorizontal("AC BoldHeader");
                        EditorGUILayout.LabelField($"[{i}] <b>{HumanTrait.MuscleName[i]}</b>", mixedStyle);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.LabelField($"<b>Muscle handle name:</b>  {muscleHandles[i].name}", mixedStyle);
                        EditorGUILayout.LabelField($"<b>Muscle handle part:</b>  {muscleHandles[i].humanPartDof}", mixedStyle);
                        switch (muscleHandles[i].humanPartDof)
                        {
                            case HumanPartDof.Body:
                                EditorGUILayout.LabelField($"<b>Muscle handle DoF:</b>  {muscleHandles[i].dof} ({(BodyDof)muscleHandles[i].dof})", mixedStyle);
                                break;
                            case HumanPartDof.Head:
                                EditorGUILayout.LabelField($"<b>Muscle handle DoF:</b>  {muscleHandles[i].dof} ({(HeadDof)muscleHandles[i].dof})", mixedStyle);
                                break;
                            case HumanPartDof.LeftLeg:
                            case HumanPartDof.RightLeg:
                                EditorGUILayout.LabelField($"<b>Muscle handle DoF:</b>  {muscleHandles[i].dof} ({(LegDof)muscleHandles[i].dof})", mixedStyle);
                                break;
                            case HumanPartDof.LeftArm:
                            case HumanPartDof.RightArm:
                                EditorGUILayout.LabelField($"<b>Muscle handle DoF:</b>  {muscleHandles[i].dof} ({(ArmDof)muscleHandles[i].dof})", mixedStyle);
                                break;
                            case HumanPartDof.LeftThumb:
                            case HumanPartDof.LeftIndex:
                            case HumanPartDof.LeftMiddle:
                            case HumanPartDof.LeftRing:
                            case HumanPartDof.LeftLittle:
                            case HumanPartDof.RightThumb:
                            case HumanPartDof.RightIndex:
                            case HumanPartDof.RightMiddle:
                            case HumanPartDof.RightRing:
                            case HumanPartDof.RightLittle:
                                EditorGUILayout.LabelField($"<b>Muscle handle DoF:</b>  {muscleHandles[i].dof} ({(FingerDof)muscleHandles[i].dof})", mixedStyle);
                                break;
                            default:
                                EditorGUILayout.LabelField($"<b>Muscle handle DoF:</b>  {muscleHandles[i].dof}", mixedStyle);
                                break;
                        }

                        EditorGUILayout.LabelField($"<b>Muscle default range:</b>  {HumanTrait.GetMuscleDefaultMin(i)}-{HumanTrait.GetMuscleDefaultMax(i)}", mixedStyle);

                        var associatedBone = HumanTrait.BoneFromMuscle(i);

                        EditorGUILayout.LabelField($"<b>Bone index:</b>  {associatedBone}", mixedStyle);
                        EditorGUILayout.LabelField($"<b>Bone name:</b>  {HumanTrait.BoneName[associatedBone]}", mixedStyle);

                        ShowBoneInfo(avatar, boneMap, muscleDefinition, associatedBone);

                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndScrollView();
                    EditorGUI.indentLevel--;
                }

                settings.muscleDefinitionInfo_Show = EditorGUILayout.Foldout(settings.muscleDefinitionInfo_Show, "Show generated HumanMuscleDefinition info");
                if (settings.muscleDefinitionInfo_Show) {
                    EditorGUI.indentLevel++;
                    settings.muscleDefinitionInfo_Scroll = EditorGUILayout.BeginScrollView(settings.muscleDefinitionInfo_Scroll, GUILayout.Height(400.0f));
                    for (int i = 0; i < muscleDefinition.skeletonTransforms.Length; ++i)
                    {
                        var st = muscleDefinition.skeletonTransforms[i];

                        EditorGUILayout.BeginVertical("box");

                        EditorGUILayout.BeginHorizontal("AC BoldHeader");
                        EditorGUILayout.LabelField($"[{i}] <b>{st.FullName}</b>", mixedStyle);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.LabelField($"<b>Name:</b>  {st.name}", mixedStyle);
                        EditorGUILayout.LabelField($"<b>Position:</b>  {st.localPosition}", mixedStyle);
                        EditorGUILayout.LabelField($"<b>Rotation:</b>  {st.localRotation}", mixedStyle);
                        EditorGUILayout.LabelField($"<b>Scale:</b>  {st.localScale}", mixedStyle);

                        EditorGUILayout.LabelField($"<b>Parent ambiguous:</b>  {st.parentAmbiguous}", mixedStyle);
                        EditorGUILayout.LabelField($"<b>Parents:</b>  {string.Join(", ", st.parentIndices.Select(s => s.ToString()))}", mixedStyle);
                        EditorGUILayout.LabelField($"<b>Children:</b>  {string.Join(", ", st.childIndices.Select(s => s.ToString()))}", mixedStyle);

                        EditorGUILayout.LabelField($"<b>Human bone index:</b>  {st.humanBoneIndex}", mixedStyle);
                        if (st.humanBoneIndex != null)
                        {
                            EditorGUI.indentLevel++;
                            var hb = muscleDefinition.boneInfos[st.humanBoneIndex ?? 0];

                            EditorGUILayout.LabelField($"<b>Human bone bones:</b>  {hb.humanBodyBones}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>Trait name:</b>  {hb.traitBoneName}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>Mass:</b>  {hb.mass:0.00000}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>Axis length:</b>  {hb.axisLength}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>Pre rotation:</b>  {hb.preRotation}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>Post rotation:</b>  {hb.postRotation}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>ZY Post Q:</b>  {hb.zyPostQ}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>ZY Roll:</b>  {hb.zyRoll}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>Limit sign:</b>  {hb.limitSign}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>Custom range minimum:</b>  {hb.customRangeMin}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>Custom range maximum:</b>  {hb.customRangeMax}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>Custom range center:</b>  {hb.customRangeCenter}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>Use default range:</b>  {hb.useDefaultRange}", mixedStyle);

                            var muscles = new (string, int?)[]
                            {
                                ("X", hb.xMuscleIndex),
                                ("Y", hb.yMuscleIndex),
                                ("Z", hb.zMuscleIndex),
                            };

                            foreach (var (muscleAxis, muscleIndex) in muscles)
                            {
                                EditorGUILayout.LabelField($"<b>{muscleAxis} muscle index:</b>  {muscleIndex}", mixedStyle);
                                if (muscleIndex != null)
                                {
                                    EditorGUI.indentLevel++;
                                    var hm = muscleDefinition.muscleInfos[muscleIndex ?? 0];

                                    EditorGUILayout.LabelField($"<b>Trait name:</b>  {hm.traitMuscleName}", mixedStyle);
                                    EditorGUILayout.LabelField($"<b>Handle name:</b>  {hm.handleMuscleName}", mixedStyle);
                                    EditorGUILayout.LabelField($"<b>Part DoF:</b>  {hm.humanPartDof}", mixedStyle);
                                    EditorGUILayout.LabelField($"<b>DoF:</b>  {hm.dof}", mixedStyle);
                                    EditorGUILayout.LabelField($"<b>Default range minimum:</b>  {hm.defaultRangeMin}", mixedStyle);
                                    EditorGUILayout.LabelField($"<b>Default range maximum:</b>  {hm.defaultRangeMax}", mixedStyle);
                                    EditorGUI.indentLevel--;
                                }
                            }

                            EditorGUI.indentLevel--;
                        }

                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndScrollView();
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Separator();

                settings.lineSkeleton_Gizmo = EditorGUILayout.Foldout(settings.lineSkeleton_Gizmo, "Show line skeleton");
                if (settings.lineSkeleton_Gizmo) {
                    EditorGUI.indentLevel++;

                    settings.lineSkeleton_Color = EditorGUILayout.ColorField("Color", settings.lineSkeleton_Color);
                    settings.lineSkeleton_BallSize = EditorGUILayout.FloatField("Ball size", settings.lineSkeleton_BallSize);

                    EditorGUI.indentLevel--;
                }

                settings.poseSkeleton_Gizmo = EditorGUILayout.Foldout(settings.poseSkeleton_Gizmo, "Show pose skeleton");
                if (settings.poseSkeleton_Gizmo) {
                    EditorGUI.indentLevel++;
                    settings.poseSkeleton_Scroll = EditorGUILayout.BeginScrollView(settings.poseSkeleton_Scroll, GUILayout.Height(400.0f));
                    settings.poseSkeleton_Color = EditorGUILayout.ColorField("Color", settings.poseSkeleton_Color);
                    settings.poseSkeleton_BallSize = EditorGUILayout.FloatField("Ball size", settings.poseSkeleton_BallSize);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"<b>Global value: </b>", mixedStyle);
                    settings.poseSkeleton_GlobalMuscleValue = EditorGUILayout.FloatField(settings.poseSkeleton_GlobalMuscleValue);
                    if (GUILayout.Button("Apply"))
                    {
                        settings.poseSkeleton_MuscleValues = Enumerable.Range(0, HumanTrait.MuscleCount).Select(_ => settings.poseSkeleton_GlobalMuscleValue).ToArray();
                    }
                    EditorGUILayout.EndHorizontal();

                    if (settings.poseSkeleton_MuscleValues == null || settings.poseSkeleton_MuscleValues.Length < HumanTrait.MuscleCount)
                    {
                        settings.poseSkeleton_MuscleValues = Enumerable.Range(0, HumanTrait.MuscleCount).Select(_ => settings.poseSkeleton_GlobalMuscleValue).ToArray();
                    }

                    for (int i = 0; i < HumanTrait.MuscleCount; ++i)
                    {
                        EditorGUILayout.BeginVertical("box");

                        EditorGUILayout.BeginHorizontal("AC BoldHeader");
                        EditorGUILayout.LabelField($"[{i}] <b>{HumanTrait.MuscleName[i]}</b>", mixedStyle);
                        EditorGUILayout.EndHorizontal();

                        settings.poseSkeleton_MuscleValues[i] = EditorGUILayout.Slider(settings.poseSkeleton_MuscleValues[i], -1.0f, 1.0f);

                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndScrollView();
                    EditorGUI.indentLevel--;
                }

                settings.quaternionProbe_Gizmo = EditorGUILayout.Foldout(settings.quaternionProbe_Gizmo, "Show quaternion probe");
                if (settings.quaternionProbe_Gizmo) {
                    EditorGUI.indentLevel++;

                    settings.quaternionProbe_SkeletonGizmo = EditorGUILayout.Toggle("Skeleton", settings.quaternionProbe_SkeletonGizmo);
                    settings.quaternionProbe_SkeletonColor = EditorGUILayout.ColorField("Color", settings.quaternionProbe_SkeletonColor);
                    settings.quaternionProbe_SkeletonBallSize = EditorGUILayout.FloatField("Ball size", settings.quaternionProbe_SkeletonBallSize);

                    EditorGUILayout.Separator();

                    settings.quaternionProbe_IgnoreAbsoluteParent = EditorGUILayout.Toggle("Ignore absolute parent", settings.quaternionProbe_IgnoreAbsoluteParent);

                    int? itemToDelete = null;

                    for (int i = 0; i < settings.quaternionProbe_RotationSteps.Count; ++i)
                    {
                        EditorGUILayout.BeginVertical("box");

                        settings.quaternionProbe_RotationSteps[i].kind = (QuaternionProbeRotationKind)EditorGUILayout.Popup(
                            (int)settings.quaternionProbe_RotationSteps[i].kind,
                            ((QuaternionProbeRotationKind[])Enum.GetValues(typeof(QuaternionProbeRotationKind))).Select(v => v.ToString()).ToArray()
                        );

                        if (settings.quaternionProbe_RotationSteps[i].kind == QuaternionProbeRotationKind.CustomEuler)
                        {
                            settings.quaternionProbe_RotationSteps[i].customEulerValue = EditorGUILayout.Vector3Field(
                                "Value", settings.quaternionProbe_RotationSteps[i].customEulerValue
                            );
                        }

                        if (settings.quaternionProbe_RotationSteps[i].kind == QuaternionProbeRotationKind.CustomQuaternion)
                        {
                            var oldQuat = settings.quaternionProbe_RotationSteps[i].customQuaternionValue;
                            var newQuat = EditorGUILayout.Vector4Field(
                                "Value", new Vector4(oldQuat.x, oldQuat.y, oldQuat.z, oldQuat.w)
                            );
                            settings.quaternionProbe_RotationSteps[i].customQuaternionValue = new Quaternion(
                                newQuat.x,
                                newQuat.y,
                                newQuat.z,
                                newQuat.w
                            );
                        }

                        settings.quaternionProbe_RotationSteps[i].inverse = EditorGUILayout.Toggle(
                            "Inverse", settings.quaternionProbe_RotationSteps[i].inverse
                        );

                        settings.quaternionProbe_RotationSteps[i].appliesToEnd = EditorGUILayout.Toggle(
                            "Applies to end", settings.quaternionProbe_RotationSteps[i].appliesToEnd
                        );

                        settings.quaternionProbe_RotationSteps[i].appliesToParents = EditorGUILayout.Toggle(
                            "Applies to parents", settings.quaternionProbe_RotationSteps[i].appliesToParents
                        );

                        settings.quaternionProbe_RotationSteps[i].appliesToMuscles = EditorGUILayout.Toggle(
                            "Applies to muscle bones", settings.quaternionProbe_RotationSteps[i].appliesToMuscles
                        );

                        settings.quaternionProbe_RotationSteps[i].appliesToNonMuscles = EditorGUILayout.Toggle(
                            "Applies to non-muscle bones", settings.quaternionProbe_RotationSteps[i].appliesToNonMuscles
                        );

                        EditorGUILayout.BeginHorizontal();

                        if (GUILayout.Button("↑") && i > 0)
                        {
                            var thisOne = settings.quaternionProbe_RotationSteps[i];
                            var otherOne = settings.quaternionProbe_RotationSteps[i - 1];

                            settings.quaternionProbe_RotationSteps[i - 1] = thisOne;
                            settings.quaternionProbe_RotationSteps[i] = otherOne;
                        }

                        if (GUILayout.Button("↓") && i + 1 < settings.quaternionProbe_RotationSteps.Count)
                        {
                            var thisOne = settings.quaternionProbe_RotationSteps[i];
                            var otherOne = settings.quaternionProbe_RotationSteps[i + 1];

                            settings.quaternionProbe_RotationSteps[i + 1] = thisOne;
                            settings.quaternionProbe_RotationSteps[i] = otherOne;
                        }

                        if (GUILayout.Button("X"))
                        {
                            itemToDelete = i;
                        }

                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.EndVertical();
                    }

                    if (itemToDelete != null)
                    {
                        settings.quaternionProbe_RotationSteps.RemoveAt(itemToDelete ?? 0);
                    }

                    if (GUILayout.Button("+ Add step"))
                    {
                        settings.quaternionProbe_RotationSteps.Add(new QuaternionProbeStep(Vector3.zero));
                    }

                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Set rig default"))
                    {
                        settings.quaternionProbe_RotationSteps = new()
                        {
                            new QuaternionProbeStep(QuaternionProbeRotationKind.RigRotation)
                        };
                        settings.quaternionProbe_IgnoreAbsoluteParent = false;
                        settings.quaternionProbe_RotateByAlignment = false;
                        settings.quaternionProbe_ShiftByMass = false;
                    }

                    if (GUILayout.Button("Set pose default"))
                    {
                        settings.quaternionProbe_RotationSteps = new()
                        {
                            //new QuaternionProbeStep(QuaternionProbeRotationKind.RigRotation, false, true, true, false, true),
                            new QuaternionProbeStep(QuaternionProbeRotationKind.PreRotation, false, true, true, true, false),
                            new QuaternionProbeStep(QuaternionProbeRotationKind.PostRotation, true, true, true, true, false)
                        };
                        settings.quaternionProbe_IgnoreAbsoluteParent = true;
                        settings.quaternionProbe_RotateByAlignment = true;
                        settings.quaternionProbe_ShiftByMass = true;
                    }

                    if (GUILayout.Button("Set pose matching pose skeleton"))
                    {
                        settings.quaternionProbe_RotationSteps = new()
                        {
                            //new QuaternionProbeStep(QuaternionProbeRotationKind.RigRotation, false, true, true, false, true),
                            new QuaternionProbeStep(QuaternionProbeRotationKind.PreRotation, false, true, true, true, false),
                            new QuaternionProbeStep(QuaternionProbeRotationKind.CopiedFromPoseSkeleton, false, true, true, true, false),
                            new QuaternionProbeStep(QuaternionProbeRotationKind.PostRotation, true, true, true, true, false)
                        };
                        settings.quaternionProbe_IgnoreAbsoluteParent = true;
                        settings.quaternionProbe_RotateByAlignment = true;
                        settings.quaternionProbe_ShiftByMass = true;
                    }

                    EditorGUILayout.EndHorizontal();

                    settings.quaternionProbe_ShiftByMass = EditorGUILayout.Toggle("Shift position by mass", settings.quaternionProbe_ShiftByMass);
                    settings.quaternionProbe_RotateByAlignment = EditorGUILayout.Toggle("Rotate by alignment", settings.quaternionProbe_RotateByAlignment);

                    EditorGUILayout.Separator();

                    settings.quaternionProbe_Index = EditorGUILayout.Popup(
                        "Skeleton transform",
                        settings.quaternionProbe_Index,
                        muscleDefinition.skeletonTransforms.Select((v) => v.FullName).ToArray()
                    );

                    EditorGUI.indentLevel--;
                }
            }

            public void ShowBoneInfo(
                Avatar avatar,
                Dictionary<string, HumanBone> boneMap,
                MecanimMuscleSkinning.HumanMuscleDefinition muscleDefinition,
                int index
            )
            {
                GUIStyle mixedStyle = new(EditorStyles.label)
                {
                    richText = true
                };

                var method_GetPreRotation = typeof(Avatar).GetMethod("GetPreRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
	            var method_GetPostRotation = typeof(Avatar).GetMethod("GetPostRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
	            var method_GetLimitSign = typeof(Avatar).GetMethod("GetLimitSign", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                EditorGUILayout.LabelField($"<b>Human Body Bones</b>:  {(HumanBodyBones)index}", mixedStyle);
                EditorGUILayout.LabelField($"<b>Required?</b>:  {HumanTrait.RequiredBone(index)}", mixedStyle);

                var preRotation = (Quaternion)method_GetPreRotation.Invoke(avatar, new object[]{ (HumanBodyBones)index });
                var postRotation = (Quaternion)method_GetPostRotation.Invoke(avatar, new object[]{ (HumanBodyBones)index });
                var limitSign = (Vector3)method_GetLimitSign.Invoke(avatar, new object[]{ (HumanBodyBones)index });

                EditorGUILayout.LabelField($"<b>Pre rotation</b>:  {preRotation}", mixedStyle);
                EditorGUILayout.LabelField($"<b>Post rotation</b>:  {postRotation}", mixedStyle);
                EditorGUILayout.LabelField($"<b>Limit sign</b>:  {limitSign}", mixedStyle);

                EditorGUILayout.LabelField($"<b>Default hierarchy mass:</b>  {HumanTrait.GetBoneDefaultHierarchyMass(index)}", mixedStyle);

                if (boneMap.TryGetValue(HumanTrait.BoneName[index], out HumanBone boneMapped))
                {
                    EditorGUILayout.LabelField($"<b>Mapped to avatar bone:</b>  {boneMapped.boneName}", mixedStyle);
                    EditorGUILayout.LabelField($"<size=10>({muscleDefinition[boneMapped.boneName].Value.skeletonTransform.FullName})</size>", mixedStyle);
                    EditorGUILayout.LabelField($"<b>Range in avatar:</b>  {boneMapped.limit.min}-{boneMapped.limit.max} [c: {boneMapped.limit.center}, l: {boneMapped.limit.axisLength}, d: {boneMapped.limit.useDefaultValues}]", mixedStyle);
                } else
                {
                    EditorGUILayout.LabelField($"<b>Mapped to avatar bone:</b>  <i><null></i>", mixedStyle);
                }

            }
    }
#endif
}

}
