#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HelpingHandsVR.SignLanguageTools.Debug {

public class MecanimAvatarDebug
{
    [MenuItem("CONTEXT/SkinnedMeshRenderer/HelpingHandsVR/Debug/PrintMecanimInfo")]
    static void Debug_PrintInfo(MenuCommand command)
    {
        UnityEngine.Debug.Log("placeholder");

    }
}

}

#endif
