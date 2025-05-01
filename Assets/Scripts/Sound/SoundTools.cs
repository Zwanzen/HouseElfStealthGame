using UnityEngine;
using System.Collections;
using System;
/// <summary>
/// Custom tools for handling sound effects.
/// </summary>
public static class SoundTools
{

    // Private Methods
    /// <summary>
    /// Returns the index used to play the correct material sound for FMOD.
    /// </summary>
    private static int GetMaterialIndex(EMaterialTag material)
    {
        switch (material)
        {
            case EMaterialTag.Wood:
                return 4;
            case EMaterialTag.Metal:
                return 1;
            case EMaterialTag.Stone:
                return 2;
            case EMaterialTag.Water:
                return 3;
            case EMaterialTag.Carpet:
                return 0;
        }

        Debug.LogError("Invalid material tag");
        return -1;
    }

    private static float GetSoundRange(EMaterialTag material, float velocityMagnitude)
    {
        switch (material)
        {
            case EMaterialTag.Wood:
                return 3f;
            case EMaterialTag.Metal:
                return 6f;
            case EMaterialTag.Stone:
                return 3f;
            case EMaterialTag.Water:
                return 5f;
            case EMaterialTag.Carpet:
                return 0.3f;
        }

        Debug.LogError("Invalid material tag");
        return -1;
    }

    private static float GetSoundAmplitude(EMaterialTag material, float velocityMagnitude)
    {
        switch (material)
        {
            case EMaterialTag.Wood:
                return 10f;
            case EMaterialTag.Metal:
                return 20f;
            case EMaterialTag.Stone:
                return 10f;
            case EMaterialTag.Water:
                return 20f;
            case EMaterialTag.Carpet:
                return 2f;
        }
        Debug.LogError("Invalid material tag");
        return -1;
    }

    public static EMaterialTag GetMaterialFromTag(string tag)
    {
        switch (tag)
        {
            case "Wood":
                return EMaterialTag.Wood;
            case "Metal":
                return EMaterialTag.Metal;
            case "Stone":
                return EMaterialTag.Stone;
            case "Water":
                return EMaterialTag.Water;
            case "Carpet":
                return EMaterialTag.Carpet;
        }
        Debug.LogError($"Invalid material tag {tag}");
        return EMaterialTag.None; // Default to Wood if invalid
    }

    public enum EMaterialTag
    {
        None = -1,
        Wood,
        Metal,
        Stone,
        Water,
        Carpet
    }

    public struct FootSoundInfo
    {
        public readonly int MaterialIndex;
        public readonly Sound Sound;

        public FootSoundInfo(int materialIndex, Sound sound)
        {
            MaterialIndex = materialIndex;
            Sound = sound;
        }
    }

    public static FootSoundInfo GetFootSound(EMaterialTag material, Vector3 position, float velocityMagnitude)
    {
        int materialIndex = GetMaterialIndex(material);
        float range = GetSoundRange(material, velocityMagnitude);
        float amplitude = GetSoundAmplitude(material, velocityMagnitude);
        Sound sound = new Sound(position, range, amplitude);
        return new FootSoundInfo(materialIndex, sound);
    }

}