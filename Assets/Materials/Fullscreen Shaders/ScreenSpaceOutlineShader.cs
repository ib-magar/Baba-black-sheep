//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Rendering;
//using UnityEngine.Rendering.Universal;

//public class ScreenSpaceOutlineShader : ScriptableRendererFeature {

//    public class OutlineSettings {
//        public float normalThreshold = 0.5f;
//        public float colorThreshold = 0.2f;

//        public Color outlineColor = Color.black;

//        public float outlineScale = 1;
//        public float depthThreshold = 1;
//    }

//    public class OutlineRenderPass : ScriptableRenderPass {

//        private readonly Material screenSpaceOutlineMaterial;

//        public OutlineRenderPass(RenderPassEvent renderPassEvent) {
//            this.renderPassEvent = renderPassEvent;

//            screenSpaceOutlineMaterial = new Material(Shader.Find("Hidden/OutlineFullScreenShader"));
//        }

//        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
//            if (!screenSpaceOutlineMaterial)
//                return;

//            CommandBuffer cmd = CommandBufferPool.Get();
//            using (new ProfilingScope(cmd, new ProfilingSampler("ScreenSpaceOutlines"))) {

//                Blit(cmd, cameraColorTarget, temporaryBuffer);
//                Blit(cmd, temporaryBuffer, cameraColorTarget, screenSpaceOutlineMaterial);
//            }

//            context.ExecuteCommandBuffer(cmd);
//            CommandBufferPool.Release(cmd);
//        }
//    }

//    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

//    OutlineRenderPass outlineRenderPass;

//    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
//        renderer.EnqueuePass(outlineRenderPass);
//    }

//    public override void Create() {
//        if (renderPassEvent < RenderPassEvent.BeforeRenderingPrePasses)
//            renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;

//        outlineRenderPass = new OutlineRenderPass(renderPassEvent);
//    }
//}

