#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace HelpingHandsVR.SignLanguageTools.Debug {

public class MecanimAvatarDebug
{
    [MenuItem("CONTEXT/Animator/HelpingHandsVR/Debug/PrintMecanimInfo")]
    static void Debug_PrintInfo(MenuCommand command)
    {
        Animator animator = (Animator)command.context;

        var bonesAndAxes = Enumerable.Range(0, HumanTrait.BoneCount)
            .Select(
                i => (
                    animator.GetBoneTransform((HumanBodyBones)i)
                )
            );

    }
}

}

#endif
