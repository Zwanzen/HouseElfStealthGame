using UnityEngine;

public static class Sounds
{
    public static void MakeSound(Sound sound)
    {
        // Detect objects within the sound range
        Collider[] col = Physics.OverlapSphere(sound.Pos, sound.Range);
        
        for (int i = 0; i < col.Length; i++)
        {
            if (col[i].TryGetComponent(out IHear hear))
                hear.RespondToSound(sound);
        }

        // Spawn a debug prefab
        var prefab = Resources.Load<GameObject>("Debug/DebugSphere");
        if (prefab != null)
        {
            var obj = Object.Instantiate(prefab, sound.Pos, Quaternion.identity);
            obj.transform.localScale = Vector3.one * sound.Range;

            // Set the material color (int) property
            var mat = obj.GetComponent<Renderer>().material;
            var color = GetSoundTypeColor(sound.SoundType);
            mat.SetInt("_ColorInt", color);

            // Destroy the debug object after 2 seconds
            Object.Destroy(obj, 2f);
        }
        else
        {
            Debug.LogWarning("DebugSphere prefab not found in Resources/Debug.");
        }
    }

    private static int GetSoundTypeColor(Sound.ESoundType soundType)
    {
        return soundType switch
        {
            Sound.ESoundType.Environment => 2,
            Sound.ESoundType.Player => 0,
            Sound.ESoundType.Props => 1,
            _ => -1
        };
    }
}
