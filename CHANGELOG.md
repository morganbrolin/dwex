# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- *HexFlags* enumeration type to represent bit flags for cell data.
- *HexCell.Grid* property, which is used to retrieve neighbor cells.
- *HexCell.HasIncomingRiverThroughEdge* method.
- *HexCell.TryGetNeighbor* and *HexGrid.TryGetCell* methods, to make cell retrieval more efficient and convenient.
- *HexCoordinates.Step* method, to help retrieve cell neighbors based on coordinates.

### Changed

- Upgraded to Unity 2021.3.24f and URP 12.1.11.
- Started changing code style to newer tutorial style, more like standard C# style.

### Removed

- *HexCell* neighbors and roads arrays.
- *HexCell* incoming and outgoing river boolean and direction fields.
- *HexCell* walled and explored fields.
- *HexCell.RiverBeginOrEndDirection* property.

## [2.2.0] - 2023-03-10

### Added

- Store world-space water level in B channel of shader data.
- Colorize underwater terrain based on submergence.
- Highlight affected cells while in edit mode, based on cursor position and brush size.
- The concept of hex space, where the distance between cell centers of east-west neighbors is one unit.
- Documentation page to showcase feature-level changes visible to players.

### Changed

- Upgraded to Unity 2021.3.20f1 and URP 12.1.10.
- *HexCellData.hlsl* relies upon and includes *HexMetrics.hlsl*.
- *HexCellShaderData* uses a separate boolean array to track visibility transitions.
- Shaders use world position XZ to calculate hex grid position data analytically.
- Grid visualization no longer uses a texture.
- Material documentation update.

### Removed

- Hex grid texture.
- Hex grid offsets texture.
- Unused *Feature* material.

## [2.1.0] - 2022-12-28

### Added

- Code documentation for top-level public types and public members.

### Changed

- Upgraded to Unity 2021.3.16f1.
- Public configuration fields are now private serializable.
- Non-static public fields that are referenced outside their containing classes have become properties.
- *HexFeatureCollection* has become a nested type inside *HexFeatureManager*.
- Default map has wrapping disabled, as it makes no sense for small maps.

### Fixed

- Units are no longer selectable in edit mode on startup, so no more unwanted pathfinding while editing.

## [2.0.0] - 2022-11-04

### Added

- Dependency on Universal RP package version 12.1.7 and created URP assets.
- URP package dependencies, Burst updated to version 1.8.1 and Mathematics to 1.2.6.
- Shader graphs that replace all terrain, water, and feature shaders.
- HLSL files to replace CGINC files, plus new files for shader graph custom function nodes.
- CHANGELOG file.
- Initial documentation for URP and materials.

### Changed

- Switched from Built-in RP to URP.
- Expanded README file.

### Removed

- Surface shaders for all terrain, water, and feature materials.
- All CGINC files.

### Fixed

- Set *Scale Factor* of *Save Load Menu* to 1, so it is no longer twice as large as the rest of the GUI.

## [1.0.0] - 2022-10-24

### Added

- Created Unity 2021 LTS project with Hex Map 27 unitypackage imported and input controls configured. 
