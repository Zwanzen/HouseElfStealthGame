using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class UniqueColorInstance : MonoBehaviour
{
    [SerializeField] private Color color = Color.white;
    [SerializeField] private bool randomizeOnce = true;
    [SerializeField] private bool hasBeenRandomized = false;
    [SerializeField] private bool overrideManually = false;

    private void OnEnable()
    {
        ApplyColor();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyColor();
    }
#endif

    private void ApplyColor()
    {
        if (overrideManually == false && (randomizeOnce && !hasBeenRandomized))
        {
            color = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.7f, 1f);
            hasBeenRandomized = true;
        }

        var renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor("_ObjectColor", color);
        renderer.SetPropertyBlock(block);
    }
}
