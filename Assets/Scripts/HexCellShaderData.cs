using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component that manages cell data used by shaders.
/// </summary>
public class HexCellShaderData : MonoBehaviour {

	const float transitionSpeed = 255f;

	Texture2D cellTexture;

	Color32[] cellTextureData;

	bool[] visibilityTransitions;

	List<HexCell> transitioningCells = new List<HexCell>();

	bool needsVisibilityReset;

	public HexGrid Grid { get; set; }

	public bool ImmediateMode { get; set; }

	/// <summary>
	/// Initialze the map data.
	/// </summary>
	/// <param name="x">Map X size.</param>
	/// <param name="z">Map Z size.</param>
	public void Initialize (int x, int z) {
		if (cellTexture) {
			cellTexture.Reinitialize(x, z);
		}
		else {
			cellTexture = new Texture2D(
				x, z, TextureFormat.RGBA32, false, true
			);
			cellTexture.filterMode = FilterMode.Point;
			cellTexture.wrapModeU = TextureWrapMode.Repeat;
			cellTexture.wrapModeV = TextureWrapMode.Clamp;
			Shader.SetGlobalTexture("_HexCellData", cellTexture);
		}
		Shader.SetGlobalVector(
			"_HexCellData_TexelSize",
			new Vector4(1f / x, 1f / z, x, z)
		);

		if (cellTextureData == null || cellTextureData.Length != x * z) {
			cellTextureData = new Color32[x * z];
			visibilityTransitions = new bool[x * z];
		}
		else {
			for (int i = 0; i < cellTextureData.Length; i++) {
				cellTextureData[i] = new Color32(0, 0, 0, 0);
				visibilityTransitions[i] = false;
			}
		}

		transitioningCells.Clear();
		enabled = true;
	}

	/// <summary>
	/// Refresh the terrain data of a cell. Supports water surfaces up to 30 units high.
	/// </summary>
	/// <param name="cell">Cell with changed terrain type.</param>
	public void RefreshTerrain (HexCell cell) {
		Color32 data = cellTextureData[cell.Index];
		data.b = cell.IsUnderwater ? (byte)(cell.WaterSurfaceY * (255f / 30f)) : (byte)0;
		data.a = (byte)cell.TerrainTypeIndex;
		cellTextureData[cell.Index] = data;
		enabled = true;
	}

	/// <summary>
	/// Refresh visibility of a cell.
	/// </summary>
	/// <param name="cell">Cell with changed visibility.</param>
	public void RefreshVisibility (HexCell cell) {
		int index = cell.Index;
		if (ImmediateMode) {
			cellTextureData[index].r = cell.IsVisible ? (byte)255 : (byte)0;
			cellTextureData[index].g = cell.IsExplored ? (byte)255 : (byte)0;
		}
		else if (!visibilityTransitions[index]) {
			visibilityTransitions[index] = true;
			transitioningCells.Add(cell);
		}
		enabled = true;
	}

	/// <summary>
	/// Set arbitrary map data of a cell, overriding water data.
	/// </summary>
	/// <param name="cell">Cell to apply data for.</param>
	/// <param name="data">Cell data value, 0-1 inclusive.</param>
	public void SetMapData (HexCell cell, float data) {
		cellTextureData[cell.Index].b =
			data < 0f ? (byte)0 : (data < 1f ? (byte)(data * 255f) : (byte)255);
		enabled = true;
	}

	/// <summary>
	/// Indicate that view elevation data has changed, requiring a visibility reset.
	/// Supports water surfaces up to 30 units high.
	/// </summary>
	/// <param name="cell">Changed cell.</param>
	public void ViewElevationChanged (HexCell cell) {
		cellTextureData[cell.Index].b = cell.IsUnderwater ?
			(byte)(cell.WaterSurfaceY * (255f / 30f)) : (byte)0;
		needsVisibilityReset = true;
		enabled = true;
	}

	void LateUpdate () {
		if (needsVisibilityReset) {
			needsVisibilityReset = false;
			Grid.ResetVisibility();
		}

		int delta = (int)(Time.deltaTime * transitionSpeed);
		if (delta == 0) {
			delta = 1;
		}
		for (int i = 0; i < transitioningCells.Count; i++) {
			if (!UpdateCellData(transitioningCells[i], delta)) {
				transitioningCells[i--] =
					transitioningCells[transitioningCells.Count - 1];
				transitioningCells.RemoveAt(transitioningCells.Count - 1);
			}
		}

		cellTexture.SetPixels32(cellTextureData);
		cellTexture.Apply();
		enabled = transitioningCells.Count > 0;
	}

	bool UpdateCellData (HexCell cell, int delta) {
		int index = cell.Index;
		Color32 data = cellTextureData[index];
		bool stillUpdating = false;

		if (cell.IsExplored && data.g < 255) {
			stillUpdating = true;
			int t = data.g + delta;
			data.g = t >= 255 ? (byte)255 : (byte)t;
		}

		if (cell.IsVisible) {
			if (data.r < 255) {
				stillUpdating = true;
				int t = data.r + delta;
				data.r = t >= 255 ? (byte)255 : (byte)t;
			}
		}
		else if (data.r > 0) {
			stillUpdating = true;
			int t = data.r - delta;
			data.r = t < 0 ? (byte)0 : (byte)t;
		}

		if (!stillUpdating) {
			visibilityTransitions[index] = false;
		}
		cellTextureData[index] = data;
		return stillUpdating;
	}
}