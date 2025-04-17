using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class AutoTilingByScale : MonoBehaviour
{
    public enum AxisMapping
    {
        XZ, // Vanlig gulv eller tak
        XY, // Vegger stående rett opp
        YZ  // Vertikale flater på siden
    }

    [SerializeField]
    private bool useObjectScale = true;

    [SerializeField]
    private AxisMapping axisMapping = AxisMapping.XZ;

    [SerializeField]
    private Vector2 tilingMultiplier = Vector2.one;

    private Renderer rend;
    private MaterialPropertyBlock props;

    private static readonly int TilingID = Shader.PropertyToID("_Tiling");

    void OnEnable()
    {
        ApplyTiling();
    }

    void OnValidate()
    {
        ApplyTiling();
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            ApplyTiling();
#endif
    }

    void ApplyTiling()
    {
        if (rend == null) rend = GetComponent<Renderer>();
        if (props == null) props = new MaterialPropertyBlock();

        Vector3 scale = transform.localScale;
        Vector2 tiling;

        switch (axisMapping)
        {
            case AxisMapping.XY:
                tiling = new Vector2(scale.x, scale.y);
                break;
            case AxisMapping.YZ:
                tiling = new Vector2(scale.z, scale.y);
                break;
            default: // XZ
                tiling = new Vector2(scale.x, scale.z);
                break;
        }

        tiling *= tilingMultiplier;

        rend.GetPropertyBlock(props);
        props.SetVector(TilingID, tiling);
        rend.SetPropertyBlock(props);
    }
}
