
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
    [NonSerialized]
    public bool quaternionProbe_EnablePreRotation = false;
    [NonSerialized]
    public bool quaternionProbe_EnablePostRotation = false;
    [NonSerialized]
    public bool quaternionProbe_EnableZYPostQ = false;
    [NonSerialized]
    public bool quaternionProbe_EnableZYRoll = false;
    [NonSerialized]
    public int quaternionProbe_Index = 0;


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
        if (!target.avatar || !target.avatar.isHuman)
        {
            return;
        }

        var oldGizmo_Color = Gizmos.color;
        var oldGizmo_Matrix = Gizmos.matrix;

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

                Gizmos.DrawSphere(st.localToWorldMatrix.MultiplyPoint(Vector3.zero) + target.transform.position, lineSkeleton_BallSize);
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

            var posed = muscleDefinition.ApplyPose(avatar, pose);

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

                Gizmos.DrawSphere(tuple.posedLocalToWorldMatrix.MultiplyPoint(Vector3.zero) + target.transform.position, poseSkeleton_BallSize);
            }
        }

        if (quaternionProbe_Gizmo)
        {
            var matrices = muscleDefinition.skeletonTransforms.Select((st) =>
            {
                Quaternion rotation = st.localRotation;

                if (quaternionProbe_EnablePreRotation && st.humanBoneIndex != null)
                {
                    rotation = rotation * muscleDefinition.boneInfos[st.humanBoneIndex ?? 0].preRotation;
                }

                if (quaternionProbe_EnablePostRotation && st.humanBoneIndex != null)
                {
                    rotation = muscleDefinition.boneInfos[st.humanBoneIndex ?? 0].postRotation * rotation;
                }

                if (quaternionProbe_EnableZYPostQ && st.humanBoneIndex != null)
                {
                    rotation = muscleDefinition.boneInfos[st.humanBoneIndex ?? 0].zyPostQ * rotation;
                }

                if (quaternionProbe_EnableZYRoll && st.humanBoneIndex != null)
                {
                    rotation = muscleDefinition.boneInfos[st.humanBoneIndex ?? 0].zyRoll * rotation;
                }

                return Matrix4x4.TRS(
                    st.localPosition,
                    rotation,
                    st.localScale
                );
            }).ToList();

            var worldMatrices = Enumerable.Range(0, muscleDefinition.skeletonTransforms.Length).Select((index) =>
            {
                Gizmos.color = quaternionProbe_SkeletonColor;

                List<int> hierarchyIndices = new() { index };
                hierarchyIndices.AddRange(muscleDefinition.skeletonTransforms[index].parentIndices);

                var matrixStack = hierarchyIndices.Select((i) => matrices[i]).ToList();

                // Accumulate matrices backwards
                for (int i = 1; i < matrixStack.Count; ++i)
                {
                    matrixStack[matrixStack.Count - 1 - i] = matrixStack[matrixStack.Count - i] * matrixStack[matrixStack.Count - 1 - i];
                }

                var positionOfThis = (target.transform.localToWorldMatrix * matrixStack[0]).MultiplyPoint(Vector3.zero);

                if (matrixStack.Count > 1)
                {
                    var positionOfParent = (target.transform.localToWorldMatrix * matrixStack[1]).MultiplyPoint(Vector3.zero);

                    if (quaternionProbe_SkeletonGizmo)
                        Gizmos.DrawLine(
                            positionOfParent,
                            positionOfThis
                        );
                }

                if (quaternionProbe_SkeletonGizmo)
                    Gizmos.DrawSphere(matrixStack[0].MultiplyPoint(Vector3.zero) + target.transform.position, quaternionProbe_SkeletonBallSize);

                return matrixStack[0];
            }).ToList();

            Gizmos.matrix = transform.localToWorldMatrix * worldMatrices[quaternionProbe_Index];

            Gizmos.color = Color.red;
            Gizmos.DrawLine(Vector3.zero, Vector3.right * 0.1f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(Vector3.zero, Vector3.up * 0.1f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * 0.1f);
        }

        Gizmos.color = oldGizmo_Color;
        Gizmos.matrix = oldGizmo_Matrix;
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
                            EditorGUILayout.LabelField($"<b>Axis length:</b>  {hb.axisLength}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>Pre rotation:</b>  {hb.preRotation}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>Post rotation:</b>  {hb.postRotation}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>ZY Post Q:</b>  {hb.zyPostQ}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>ZY Roll:</b>  {hb.zyRoll}", mixedStyle);
                            EditorGUILayout.LabelField($"<b>Limit sign:</b>  {hb.limitSign}", mixedStyle);

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

                    settings.quaternionProbe_EnablePreRotation = EditorGUILayout.Toggle("PreRotation", settings.quaternionProbe_EnablePreRotation);
                    settings.quaternionProbe_EnablePostRotation = EditorGUILayout.Toggle("PostRotation", settings.quaternionProbe_EnablePostRotation);
                    settings.quaternionProbe_EnableZYPostQ = EditorGUILayout.Toggle("ZYPostQ", settings.quaternionProbe_EnableZYPostQ);
                    settings.quaternionProbe_EnableZYRoll = EditorGUILayout.Toggle("ZYRoll", settings.quaternionProbe_EnableZYRoll);

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
