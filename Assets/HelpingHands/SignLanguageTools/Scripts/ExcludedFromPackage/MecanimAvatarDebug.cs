

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using Unity.Collections;
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

    static Dictionary<string, (string[] fullName, string parent, Matrix4x4 local, Matrix4x4 transformation)> CreateSkeletonMap(HumanDescription humanDescription)
    {
        var skeletonBoneParentField = typeof(SkeletonBone).GetField("parentName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        var skeletonMap = humanDescription.skeleton.ToDictionary(
            (k) => k.name,
            (k) => (Matrix4x4.TRS(k.position, k.rotation, k.scale), (string)skeletonBoneParentField.GetValue(k))
        );
        var skeletonFullMap = skeletonMap.ToDictionary(
            (p) => p.Key,
            (p) =>
            {
                var fullName = new List<string>
                {
                    p.Key
                };
                var transformation = p.Value.Item1;
                var parent = p.Value.Item2;

                while (!string.IsNullOrWhiteSpace(parent))
                {
                    fullName.Add(parent);

                    if (skeletonMap.TryGetValue(parent, out var newParent))
                    {
                        transformation = newParent.Item1 * transformation;
                        parent = newParent.Item2;
                    } else
                    {
                        parent = null;
                    }
                }

                fullName.Reverse();
                return (fullName.ToArray(), string.IsNullOrWhiteSpace(p.Value.Item2) ? null : p.Value.Item2, p.Value.Item1, transformation);
            }
        );

        return skeletonFullMap;
    }

    static Dictionary<string, (string[] fullName, string parent, Matrix4x4 local, Matrix4x4 transformation)> CreateSkeletonMapFromPose(Avatar avatar, HumanPose pose)
    {
        // ugly!
        var originalSkeletonMap = CreateSkeletonMap(avatar.humanDescription);
        var paths = avatar.humanDescription.skeleton.Select((b) => string.Join("/", originalSkeletonMap[b.name].fullName.Skip(1))).ToArray();

        HumanPoseHandler humanPoseHandler = new(avatar, paths);
        NativeArray<float> avatarPose = new(paths.Length * 7, Allocator.Persistent);

        for (int i = 0; i < paths.Length; ++i)
        {
            Vector3 position = avatar.humanDescription.skeleton[i].position;
            Quaternion rotation = avatar.humanDescription.skeleton[i].rotation;
            avatarPose[7 * i] = position.x;
            avatarPose[7 * i + 1] = position.y;
            avatarPose[7 * i + 2] = position.z;
            avatarPose[7 * i + 3] = rotation.x;
            avatarPose[7 * i + 4] = rotation.y;
            avatarPose[7 * i + 5] = rotation.z;
            avatarPose[7 * i + 6] = rotation.w;
        }

        humanPoseHandler.SetInternalAvatarPose(avatarPose);
        humanPoseHandler.SetInternalHumanPose(ref pose);
        humanPoseHandler.GetInternalAvatarPose(avatarPose);

        var skeletonBoneParentField = typeof(SkeletonBone).GetField("parentName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        var skeletonMap = avatar.humanDescription.skeleton.Select((x, i) => new { Item = x, Index = i }).ToDictionary(
            (k) => k.Item.name,
            (k) => (Matrix4x4.TRS(
                new Vector3(avatarPose[7 * k.Index], avatarPose[7 * k.Index + 1], avatarPose[7 * k.Index + 2]),
                new Quaternion(avatarPose[7 * k.Index + 3], avatarPose[7 * k.Index + 4], avatarPose[7 * k.Index + 5], avatarPose[7 * k.Index + 6]),
                k.Item.scale
            ), (string)skeletonBoneParentField.GetValue(k.Item))
        );
        var skeletonFullMap = skeletonMap.ToDictionary(
            (p) => p.Key,
            (p) =>
            {
                var fullName = new List<string>
                {
                    p.Key
                };
                var transformation = p.Value.Item1;
                var parent = p.Value.Item2;

                while (!string.IsNullOrWhiteSpace(parent))
                {
                    fullName.Add(parent);

                    if (skeletonMap.TryGetValue(parent, out var newParent))
                    {
                        transformation = newParent.Item1 * transformation;
                        parent = newParent.Item2;
                    } else
                    {
                        parent = null;
                    }
                }

                fullName.Reverse();
                return (fullName.ToArray(), string.IsNullOrWhiteSpace(p.Value.Item2) ? null : p.Value.Item2, p.Value.Item1, transformation);
            }
        );

        avatarPose.Dispose();
        humanPoseHandler.Dispose();

        return skeletonFullMap;
    }

    void OnDrawGizmosSelected()
    {
        if (!target.avatar || !target.avatar.isHuman)
        {
            return;
        }

        var avatar = target.avatar;
        var humanDescription = target.avatar.humanDescription;

        var skeletonFullMap = CreateSkeletonMap(humanDescription);

        if (lineSkeleton_Gizmo) {
            foreach (var pair in skeletonFullMap)
            {
                Gizmos.color = lineSkeleton_Color;

                var positionOfThis = (target.transform.localToWorldMatrix * pair.Value.transformation).MultiplyPoint(Vector3.zero);

                if (!string.IsNullOrWhiteSpace(pair.Value.parent))
                {
                    var positionOfParent = (target.transform.localToWorldMatrix * skeletonFullMap[pair.Value.parent].transformation).MultiplyPoint(Vector3.zero);

                    Gizmos.DrawLine(
                        positionOfParent,
                        positionOfThis
                    );
                }

                Gizmos.DrawSphere(pair.Value.transformation.MultiplyPoint(Vector3.zero) + target.transform.position, lineSkeleton_BallSize);
            }
        }

        if (poseSkeleton_Gizmo)
        {
            HumanPose pose = new()
            {
                muscles = poseSkeleton_MuscleValues,
                bodyPosition = Vector3.zero,
                bodyRotation = Quaternion.identity,
            };

            var skeletonMap2 = CreateSkeletonMapFromPose(avatar, pose);

            foreach (var pair in skeletonMap2)
            {
                Gizmos.color = poseSkeleton_Color;

                var positionOfThis = (target.transform.localToWorldMatrix * pair.Value.transformation).MultiplyPoint(Vector3.zero);

                if (!string.IsNullOrWhiteSpace(pair.Value.parent))
                {
                    var positionOfParent = (target.transform.localToWorldMatrix * skeletonMap2[pair.Value.parent].transformation).MultiplyPoint(Vector3.zero);

                    Gizmos.DrawLine(
                        positionOfParent,
                        positionOfThis
                    );
                }

                Gizmos.DrawSphere(pair.Value.transformation.MultiplyPoint(Vector3.zero) + target.transform.position, poseSkeleton_BallSize);
            }
        }
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

                EditorGUILayout.Separator();

                GUIStyle mixedStyle = new(EditorStyles.label)
                {
                    richText = true
                };

                MuscleHandle[] muscleHandles = new MuscleHandle[MuscleHandle.muscleHandleCount];
                MuscleHandle.GetMuscleHandles(muscleHandles);
                Dictionary<string, HumanBone> boneMap = humanDescription.human.ToDictionary((b) => b.humanName);
                var skeletonFullMap = CreateSkeletonMap(humanDescription);

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
                        ShowBoneInfo(avatar, boneMap, skeletonFullMap, i);

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

                        ShowBoneInfo(avatar, boneMap, skeletonFullMap, associatedBone);

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
            }

            public void ShowBoneInfo(
                Avatar avatar,
                Dictionary<string, HumanBone> boneMap,
                Dictionary<string, (string[] fullName, string parent, Matrix4x4 local, Matrix4x4 transformation)> skeletonFullMap,
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
                    EditorGUILayout.LabelField($"<size=10>({string.Join("/", skeletonFullMap[boneMapped.boneName].fullName)})</size>", mixedStyle);
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
