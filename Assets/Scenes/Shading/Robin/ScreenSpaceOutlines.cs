using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// CREDIT : https://youtu.be/LMqio9NsqmM?si=yCaXHfYUGkrZZ_2f

public class ScreenSpaceOutlines : ScriptableRendererFeature
{
    [System.Serializable]
    private class ViewSpaceNormalTextureSettings
    {
        
        public RenderTextureFormat ColorFormat = RenderTextureFormat.ARGBHalf;
        public int DepthBufferBits = 0;
        public Color BackgroundColor = Color.clear;
        
    }
    
    private class ViewSpaceNormalTexturePass : ScriptableRenderPass
    {
        private ViewSpaceNormalTextureSettings
            _normalTextureSettings;
        private readonly List<ShaderTagId> _shaderTagIdList;
        private readonly RTHandle _normals;
        private readonly int _normalsID;
        private readonly Material _normalsMaterial;
        private FilteringSettings _filteringSettings;
        
        public ViewSpaceNormalTexturePass(RenderPassEvent evt, LayerMask outlineLayerMask, ViewSpaceNormalTextureSettings normalTextureSettings)
        {
            this.renderPassEvent = evt;
            _normals = RTHandles.Alloc("_SceneSpaceNormals", name: "_SceneSpaceNormals");
            _normalTextureSettings = normalTextureSettings;
            _normalsID = Shader.PropertyToID(_normals.name);
            _normalsMaterial = new Material(Shader.Find("Hidden/ViewSpaceNormalsShader"));
            _filteringSettings = new FilteringSettings(RenderQueueRange.opaque, outlineLayerMask);
            
            _shaderTagIdList = new List<ShaderTagId>
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("LightweightForward"),
                new ShaderTagId("SRPDefaultUnlit")
            };
        }
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor normalsTextureDescriptor = cameraTextureDescriptor;
            normalsTextureDescriptor.colorFormat = _normalTextureSettings.ColorFormat;
            normalsTextureDescriptor.depthBufferBits = _normalTextureSettings.DepthBufferBits;
            cmd.GetTemporaryRT(_normalsID, normalsTextureDescriptor, FilterMode.Point);
            ConfigureTarget(_normals);
            ConfigureClear(ClearFlag.All, _normalTextureSettings.BackgroundColor);
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler("ScreenViewSpaceNormalsTextureCreation")))
            {
                if(!_normalsMaterial)
                    return;
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                DrawingSettings drawSettings = 
                    CreateDrawingSettings(_shaderTagIdList, ref renderingData,
                        renderingData.cameraData.defaultOpaqueSortFlags);
                drawSettings.overrideMaterial = _normalsMaterial;
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref _filteringSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_normalsID);
        }
    }
    private class ScreenSpaceOutlinePass : ScriptableRenderPass
    {
        public ScreenSpaceOutlinePass(RenderPassEvent evt)
        {
            this.renderPassEvent = evt;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope())
            {
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    [SerializeField] private RenderPassEvent _renderPassEvent;
    [SerializeField] private ViewSpaceNormalTextureSettings _viewSpaceNormalTextureSettings;
    [SerializeField] private LayerMask _outlineLayerMask;
    
    private ViewSpaceNormalTexturePass _viewSpaceNormalTexturePass;
    private ScreenSpaceOutlinePass _screenSpaceOutlinePass;
    
    public override void Create()
    {
        _viewSpaceNormalTexturePass = new ViewSpaceNormalTexturePass(_renderPassEvent, _outlineLayerMask, _viewSpaceNormalTextureSettings);
        _screenSpaceOutlinePass = new ScreenSpaceOutlinePass(_renderPassEvent); 
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_viewSpaceNormalTexturePass);
        renderer.EnqueuePass(_screenSpaceOutlinePass);
    }

}
