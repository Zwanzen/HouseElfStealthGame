#ifndef LIGHTING_CEL_SHADED_INCLUDED
#define LIGHTING_CEL_SHADED_INCLUDED

#ifndef SHADERGRAPH_PREVIEW
struct EdgeConstants
{
    float diffuse;
    float specular;
    float specularOffset;
    float distanceAttenuation;
    float shadowAttenuation;
    float rim;
    float rimOffset;
};
struct SurfaceVariables
{
    float3 normal;
    float3 viewDir;
    float bandSmoothness;
    float smoothness;
    float shininess;
    float rimThreshold;
    EdgeConstants ec;
};
float3 CalculateCelShading(Light l, SurfaceVariables s, int numBands) {
    float shadowAttenuationSmoothstepped = smoothstep(0.0f, s.ec.shadowAttenuation, l.shadowAttenuation);
    float distanceAttenuationSmoothstepped = smoothstep(0.0f, s.ec.distanceAttenuation, l.distanceAttenuation);
    float attenuation = shadowAttenuationSmoothstepped * distanceAttenuationSmoothstepped;

    // Diffuse lighting
    float diffuse = saturate(dot(s.normal, l.direction));
    diffuse *= attenuation;

    // Specular highlights
    float3 h = SafeNormalize(s.viewDir + l.direction);
    float specular = saturate(dot(s.normal, h));
    specular = pow(specular, s.shininess);
    specular *= diffuse * s.smoothness;

    // Rim lighting
    float rim = 1.0 - dot(s.normal, s.viewDir);
    rim = pow(diffuse, s.rimThreshold);

    // Quantize the values into bands
    float bandStep = 1.0 / numBands;

    diffuse = lerp(diffuse, floor(diffuse / bandStep) * bandStep, s.bandSmoothness);
    specular = lerp(specular, floor(specular / bandStep) * bandStep, s.bandSmoothness);
    rim = lerp(rim, floor(rim / bandStep) * bandStep, s.bandSmoothness);

    // Smoothstep the values to create a cel-shaded effect
    diffuse = smoothstep(0.0, s.ec.diffuse, diffuse);
    specular = s.smoothness * smoothstep((1 - s.smoothness) * s.ec.specular + s.ec.specularOffset,
        s.ec.specular + s.ec.specularOffset, specular);

    rim = s.smoothness * smoothstep(
        s.ec.rim - 0.5 * s.ec.rimOffset,
        s.ec.rim + 0.5 * s.ec.rimOffset,
        rim);

    return l.color * (diffuse + max(specular, rim));
}
#endif

void LightingCelShaded_float(int Bands, float BandSmoothness, float3 AmbientColor, float3 HighlightColor, float Smoothness, float RimThreshold, float3 Position, float3 Normal, float3 View,
    float EdgeDiffuse, float EdgeSpecular, float EdgeSpecularOffset, float EdgeDistanceAttenuation, float EdgeShadowAttenuation,
    float EdgeRim, float EdgeRimOffset, out float3 Color){
#if defined(SHADERGRAPH_PREVIEW)
    Color = float3(0.0, 0.0, 0.0);
#else
    // Initialize the surface variables
    SurfaceVariables s;
    s.normal = Normal;
    s.viewDir = View;
    s.bandSmoothness = BandSmoothness;
    s.smoothness = Smoothness;
    s.shininess = exp2(10.0 * Smoothness + 1.0);
    s.rimThreshold = RimThreshold;
    // Edge constants
    EdgeConstants ec;
    ec.shadowAttenuation = EdgeShadowAttenuation;
    ec.distanceAttenuation = EdgeDistanceAttenuation;
    ec.diffuse = EdgeDiffuse;
    ec.specular = EdgeSpecular;
    ec.specularOffset = EdgeSpecularOffset;
    ec.rim = EdgeRim;
    ec.rimOffset = EdgeRimOffset;
    s.ec = ec;

#if SHADOWS_SCREEN
    float4 clipPos = TransformWorldToHClip(Position);
    float4 shadowCoord = TransformWorldToShadowCoord(clipPos);
#else
    float4 shadowCoord = TransformWorldToShadowCoord(Position);
#endif

    // Main light with and without shadows
    Light light = GetMainLight(shadowCoord);
    Light light2 = GetMainLight();
    // First we set the base light color to the ambient color
    Color = AmbientColor;
    // TODO: Add shine to the ambient color based on the shadowless light
    light2.color = HighlightColor;
    Color += CalculateCelShading(light2, s, Bands);
    // Then we add the main light with shadows
    Color += CalculateCelShading(light, s, Bands);
    // Then we add additional lights with shadows
    int pixelLightCount = GetAdditionalLightsCount();
    for (int i = 0; i < pixelLightCount; ++i)
    {
        light = GetAdditionalLight(i, Position, 1.0);
        Color += CalculateCelShading(light, s, Bands);
    }
#endif
}

#endif