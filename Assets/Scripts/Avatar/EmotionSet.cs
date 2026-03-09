using System;
using UnityEngine;

[Serializable]
public class EmotionSet
{
    public Emotion Emotion;
    public string AnimationTriggerName;
    public FaceBlendshapePreset FaceBlendshape;
    public int ServoAngle;
}

[Serializable]
public class FaceBlendshapePreset
{
    public FaceBlendshapeSetting[] Settings;
}

[Serializable]
public class FaceBlendshapeSetting
{
    public string Name;
    [Range(0f, 100f)]
    public float Weight;
}