# URP Settings

Note that URP inspectors hide some configuration options by default! You can show these *Additional Properties* per inspector section or globally via *Preferences / Core Render Pipeline*. 

## URP Settings Asset

Hex Map uses a single URP asset to control its RP settings.

### Rendering

There is a single renderer, the *URP Renderer* asset.

Only *SRP Batcher* is enabled, everything else is disabled. *Store Actions* are set to *Auto*.

*Dynamic Batching* can be considered legacy technology at this point. The SRP batcher does a good job, with the help of render queues to group renderers.

### Quality

*HDR* is disabled, because Hex Map is entirely LDR.

*Anti Aliasing (MSAA)* is set to 4x and *Render Scale* is set to 1. You can of course change this to trade quality for performance.

### Lighting

The *Main Light / Shadow Resolution* is set to 2048, which is used for two 1024 cascade maps. It could be lowered to improve performance.

*Addition Lights* are left at default values as Hex Map only uses a single directional light.

*Reflection Probes* aren't used thus its options are disabled. Likewise *Mixed Lighting* is disabled because there is no baked lighting. *Light Layers* aren't used either.

### Shadows

The directional shadows are configured so shadows are visible quite far away, with higher resolution for close-ups and nearby high-altitude terrain. This is achieved with *Max Distance* of 150, a *Cascade Count* of 2 and *Split 1* set to 35%. *Last Border* is set to 5% to provide a small fade region. Note that, unlike what the URP documentation and inspector indicate, the fade region is a percentage of the **entire** shadow distance.

A *Depth Bias* of 1 is sufficient. *Normal Bias* must be zero otherwise holes appear in the terrain shadows.

*Soft Shadows* are enabled as they look better than hard shadows, but could be disabled to improve performance.

*Conservative Enclosing Sphere* is disabled as it results in far too many shadow casters being rendered beyond the max distance, making the last cascade mostly useless.

### Post-processing

Currently no post FX are used. *Volume Update Mode* is set to *Via Scripting* and is never updated, so volume system overhead is avoided.

## URP Renderer

The single *URP Renderer Data* asset is configured so a forward render path without depth priming is used. This approach work fine for the fairly simple Hex Map visuals.

*Native RenderPass* is disabled because this would only provide a benefit for TBDR GPUs when deferred rendering or similar complex approaches are used.

*Transparent Receive Shadows* is enabled so water and roads receive shadows.

*Post-processing* is disabled and there are no additional render features.

## Main Camera

The single camera sits at the deepest level of the *Hex Map Camera* game object hierarchy in the scene. It is set up to use the rely on the URP settings and renderer assets, overriding nothing.

Note that *Rendering / Anti-aliasing* is set to *No Anti-aliasing*, but this refers to post-FX AA like FXAA or SMAA. The *MSAA* setting is found under *Output* because it affects the render target type.

### Environment

Hex Map relies on a uniform solid background color to hide unexplored areas. Besides that the camera always looks down such that the sky is never visible. Thus *Background Type* is set to *Solid Color*. The *Background* color is RGB 68615B.


