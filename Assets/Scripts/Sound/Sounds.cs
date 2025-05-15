using UnityEngine;

public static class Sounds
{
    private static Collider[] _colliders = new Collider[10];

    public static void MakeSound(Sound sound)
    {
        // Detect objects within the sound range
        var layerMask = LayerMask.GetMask("NPC");
        var amount = Physics.OverlapSphereNonAlloc(sound.Pos, sound.Range, _colliders, layerMask);

        //SpawnDebug(sound.Pos, sound.Range, GetSoundTypeColor(sound.SoundType));

        for (int i = 0; i < amount; i++)
        {
            var col = _colliders[i];
            if (col.TryGetComponent(out IHear hear))
            {
                hear.RespondToSound(sound);
            }
        }

    }

    private static void SpawnDebug(Vector3 pos, float radius, int color)
    {
        // Spawn a debug prefab
        var prefab = Resources.Load<GameObject>("Debug/DebugSphere");
        if (prefab != null)
        {
            var obj = Object.Instantiate(prefab, pos, Quaternion.identity);
            obj.transform.localScale = Vector3.one * radius;

            // Set the material color (int) property
            var mat = obj.GetComponent<Renderer>().material;
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
