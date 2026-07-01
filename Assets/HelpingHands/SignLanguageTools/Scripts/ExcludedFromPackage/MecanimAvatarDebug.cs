

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HelpingHandsVR.SignLanguageTools.Debug {

public class MecanimAvatarDebug : MonoBehaviour
{
    public Animator target;

    public bool genericMecanimBones_Show = false;
    public Vector2 genericMecanimBones_Scroll;
    public bool genericMecanimMuscles_Show = false;
    public Vector2 genericMecanimMuscles_Scroll;

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

    static Dictionary<string, (string fullName, string parent, Matrix4x4 local, Matrix4x4 transformation)> CreateSkeletonMap(HumanDescription humanDescription)
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
                var fullName = p.Key;
                var transformation = p.Value.Item1;
                var parent = p.Value.Item2;

                while (!string.IsNullOrWhiteSpace(parent))
                {
                    fullName = $"{parent}/{fullName}";

                    if (skeletonMap.TryGetValue(parent, out var newParent))
                    {
                        transformation = newParent.Item1 * transformation;
                        parent = newParent.Item2;
                    } else
                    {
                        parent = null;
                    }
                }

                return (fullName, string.IsNullOrWhiteSpace(p.Value.Item2) ? null : p.Value.Item2, p.Value.Item1, transformation);
            }
        );

        return skeletonFullMap;
    }

    void OnDrawGizmosSelected()
    {
        if (!target.avatar || !target.avatar.isHuman)
        {
            return;
        }

        var humanDescription = target.avatar.humanDescription;

        var skeletonFullMap = CreateSkeletonMap(humanDescription);

        foreach (var pair in skeletonFullMap)
        {
            Gizmos.color = Color.blue;

            var positionOfThis = (target.transform.localToWorldMatrix * pair.Value.transformation).MultiplyPoint(Vector3.zero);

            if (!string.IsNullOrWhiteSpace(pair.Value.parent))
            {
                var positionOfParent = (target.transform.localToWorldMatrix * skeletonFullMap[pair.Value.parent].transformation).MultiplyPoint(Vector3.zero);

                Gizmos.DrawLine(
                    positionOfParent,
                    positionOfThis
                );
            }

            Gizmos.DrawSphere(pair.Value.transformation.MultiplyPoint(Vector3.zero) + target.transform.position, 0.01f);
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
                        ShowBoneInfo(boneMap, skeletonFullMap, i);

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
                        EditorGUILayout.LabelField($"<b>Muscle handle DoF:</b>  {muscleHandles[i].dof}", mixedStyle);

                        EditorGUILayout.LabelField($"<b>Muscle default range:</b>  {HumanTrait.GetMuscleDefaultMin(i)}-{HumanTrait.GetMuscleDefaultMax(i)}", mixedStyle);

                        var associatedBone = HumanTrait.BoneFromMuscle(i);

                        EditorGUILayout.LabelField($"<b>Bone index:</b>  {associatedBone}", mixedStyle);
                        EditorGUILayout.LabelField($"<b>Bone name:</b>  {HumanTrait.BoneName[associatedBone]}", mixedStyle);

                        ShowBoneInfo(boneMap, skeletonFullMap, associatedBone);

                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndScrollView();
                    EditorGUI.indentLevel--;
                }
            }

            public void ShowBoneInfo(
                Dictionary<string, HumanBone> boneMap,
                Dictionary<string, (string fullName, string parent, Matrix4x4 local, Matrix4x4 transformation)> skeletonFullMap,
                int index
            )
            {
                GUIStyle mixedStyle = new(EditorStyles.label)
                {
                    richText = true
                };

                EditorGUILayout.LabelField($"<b>Required?</b>:  {HumanTrait.RequiredBone(index)}", mixedStyle);

                if (boneMap.TryGetValue(HumanTrait.BoneName[index], out HumanBone boneMapped))
                {
                    EditorGUILayout.LabelField($"<b>Mapped to avatar bone:</b>  {boneMapped.boneName}", mixedStyle);
                    EditorGUILayout.LabelField($"<size=10>({skeletonFullMap[boneMapped.boneName].fullName})</size>", mixedStyle);
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
