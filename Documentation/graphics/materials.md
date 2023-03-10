# Materials

The conversion from Built-in RP to URP means that the contents of *Materials* has changed a lot. The only exception is the *Highlights* subfolder, which didn't change because Unity UI is RP agnostic.

## HLSL Files

The *HexMetrics* and *HexCellData* include files used to be CGINC but are now HLSL files. *HexMetrics* is the same. *HexCellData* uses new macros for texturing and edit mode is now controlled via a boolean function parameter.

All shader graphs use two custom functions, one for the vertex stage and one for the fragment state. The matching HLSL functions are named `GetVertexCellData` and `GetFragmentData` with suffixes when needed. These are contained the *CustomFunctions* HLSL file, except for the terrain and feature shaders graphs, which each have their own HSLS file.

### Hex Cell Data

Per-cell visibility, exploration, encoded water level, and terrain type index are provided via the RGBA channels of a global texture.

### Hex Grid Data

The `HexGridData` struct provides information about a given cell in the hex grid for a world XZ position. It is used to draw grid lines and retrieve hex cell data.

### Vertex Data

All materials except features rely on custom mesh vertex data.

- Cell indices are stored in UV2 UVW.
- Cell weights are stored in vertex color RGB.

This information is used by `GetVertexCellData` to determine visibility per vertex, which is a combination of observation and exploration status. It outputs four components for terrain and two for all other materials. Terrain also outputs terrain indices.

### Fragment Data

The input needed by `GetFragmentData` varies per shader graph, but it always requires the interpolated visibility data. It outputs a base color, plus separate alpha when needed, along with an exploration factor. This factor is used to hide unexplored cells. How that is done varies per shader graph.

## Shader Graphs

Each material type has its own shader graph.

### Terrain

The terrain is most complex because it needs to blend terrain textures of up to three adjacent cells. It is an opaque material and hides unexplored cells by turning off all lighting and switching to an emissive color matching the background.

### Feature

The feature material is the only one that doesn't rely on custom mesh data. It instead relies on the world position and a grid coordinates offset texture to retrieve per-vertex cell data. It hides unexplored parts the same way as the terrain does.

### Road

The road material is transparent and sits on top of the terrain. To avoid Z fighting a vertex offset is added that pulls vertices a tiny bit toward the camera. Alpha and exploration are both used to fade out the roads. UV0 is used to control road opacity. Roads are drawn before all other transparent materials, using render queue *Transparent-10*.

### Estuary, River, Water Shore, and Water

The water materials fade the same way as roads. UV0 and UV1 are used to control river flow and shore lines.

The different water materials are put in separate render queues, from *Transparent-9* to *Transparent-6*. This is done to help the SRP batcher be most efficient. It ensures that the SRP batcher doesn't switch back and forth between different materials. If everything was in the same queue batches would be broken due to depth sorting. The exact draw order doesn't matter, except that rivers should be drawn last because waterfalls are the only case of overlapping water.

