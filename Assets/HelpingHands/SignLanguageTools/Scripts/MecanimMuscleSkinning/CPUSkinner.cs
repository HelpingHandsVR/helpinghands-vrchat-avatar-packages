
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HelpingHandsVR.SignLanguageTools.MecanimMuscleSkinning {

/// <summary>
/// This class provides interfaces to perform "CPU skinning" of Mecanim (Humanoid)
/// avatar models in Unity.
///
/// The point of this class is to allow a given pose to be represented programmatically
/// and to be able to in a sense "predict" how that pose will deform the avatar as if
/// it were a pose included within an actual Mecanim animation.
///
/// The intent in replicating the skinning behaviour is that, in addition to the vertices
/// of the avatar itself, it is possible to predict the deformation of arbitrary points,
/// transforms, or representative gizmos. This allows for instance, the marking of key
/// points on the avatar (such as the tip of the finger or side of the cheek) to be used
/// in IK calculation with the CPU skinning routine (for instance, to create a pose where
/// the tip of the finger touches the side of the cheek).
///
/// While possible to animate in Mecanim avatars, properties not directly related to the
/// muscle deformation (such as Root T and Root Q) are not considered for this prediction
/// engine.
/// </summary>
public static class CPUSkinner
{

}

}
