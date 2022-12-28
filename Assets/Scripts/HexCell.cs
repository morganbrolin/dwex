using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Container component for hex cell data.
/// </summary>
public class HexCell : MonoBehaviour {

	/// <summary>
	/// Hexagonal coordinates unique to the cell.
	/// </summary>
	public HexCoordinates Coordinates { get; set; }

	/// <summary>
	/// Transform component for the cell's UI visiualization. 
	/// </summary>
	public RectTransform UIRect { get; set; }

	/// <summary>
	/// Grid chunk that contains the cell.
	/// </summary>
	public HexGridChunk Chunk { get; set; }

	/// <summary>
	/// Unique global index of the cell.
	/// </summary>
	public int Index { get; set; }

	/// <summary>
	/// Map column index of the cell.
	/// </summary>
	public int ColumnIndex { get; set; }

	/// <summary>
	/// Surface elevation level.
	/// </summary>
	public int Elevation {
		get => elevation;
		set {
			if (elevation == value) {
				return;
			}
			int originalViewElevation = ViewElevation;
			elevation = value;
			if (ViewElevation != originalViewElevation) {
				ShaderData.ViewElevationChanged();
			}
			RefreshPosition();
			ValidateRivers();

			for (int i = 0; i < roads.Length; i++) {
				if (roads[i] && GetElevationDifference((HexDirection)i) > 1) {
					SetRoad(i, false);
				}
			}

			Refresh();
		}
	}

	/// <summary>
	/// Water elevation level.
	/// </summary>
	public int WaterLevel {
		get => waterLevel;
		set {
			if (waterLevel == value) {
				return;
			}
			int originalViewElevation = ViewElevation;
			waterLevel = value;
			if (ViewElevation != originalViewElevation) {
				ShaderData.ViewElevationChanged();
			}
			ValidateRivers();
			Refresh();
		}
	}

	/// <summary>
	/// Elevation at which the cell is visible. Highest of surface and water level.
	/// </summary>
	public int ViewElevation => elevation >= waterLevel ? elevation : waterLevel;

	/// <summary>
	/// Whether the cell counts as underwater, which is when water is higher than surface.
	/// </summary>
	public bool IsUnderwater => waterLevel > elevation;

	/// <summary>
	/// Whether there is an incoming river.
	/// </summary>
	public bool HasIncomingRiver => hasIncomingRiver;

	/// <summary>
	/// Whether there is an outgoing river.
	/// </summary>
	public bool HasOutgoingRiver => hasOutgoingRiver;

	/// <summary>
	/// Whether there is a river, either incoming, outgoing, or both.
	/// </summary>
	public bool HasRiver => hasIncomingRiver || hasOutgoingRiver;

	/// <summary>
	/// Whether a river begins or ends in the cell.
	/// </summary>
	public bool HasRiverBeginOrEnd => hasIncomingRiver != hasOutgoingRiver;

	/// <summary>
	/// The direction of the incoming or outgoing river, if applicable.
	/// </summary>
	public HexDirection RiverBeginOrEndDirection =>
		hasIncomingRiver ? incomingRiver : outgoingRiver;

	/// <summary>
	/// Whether the cell contains roads.
	/// </summary>
	public bool HasRoads {
		get {
			for (int i = 0; i < roads.Length; i++) {
				if (roads[i]) {
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>
	/// Incoming river direction, if applicable.
	/// </summary>
	public HexDirection IncomingRiver => incomingRiver;

	/// <summary>
	/// Outgoing river direction, if applicable.
	/// </summary>
	public HexDirection OutgoingRiver => outgoingRiver;

	/// <summary>
	/// Local position of this cell's game object.
	/// </summary>
	public Vector3 Position => transform.localPosition;

	/// <summary>
	/// Vertical positions the the stream bed, if applicable.
	/// </summary>
	public float StreamBedY =>
		(elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;

	/// <summary>
	/// Vertical position of the river's surface, if applicable.
	/// </summary>
	public float RiverSurfaceY =>
		(elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;

	/// <summary>
	/// Vertical position of the water surface, if applicable.
	/// </summary>
	public float WaterSurfaceY =>
		(waterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;

	/// <summary>
	/// Urban feature level.
	/// </summary>
	public int UrbanLevel {
		get => urbanLevel;
		set {
			if (urbanLevel != value) {
				urbanLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	/// <summary>
	/// Farm feature level.
	/// </summary>
	public int FarmLevel {
		get => farmLevel;
		set {
			if (farmLevel != value) {
				farmLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	/// <summary>
	/// Plant feature level.
	/// </summary>
	public int PlantLevel {
		get => plantLevel;
		set {
			if (plantLevel != value) {
				plantLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	/// <summary>
	/// Special feature index.
	/// </summary>
	public int SpecialIndex {
		get => specialIndex;
		set {
			if (specialIndex != value && !HasRiver) {
				specialIndex = value;
				RemoveRoads();
				RefreshSelfOnly();
			}
		}
	}

	/// <summary>
	/// Whether the cell contains a special feature.
	/// </summary>
	public bool IsSpecial => specialIndex > 0;

	/// <summary>
	/// Whether the cell is considered inside a walled region.
	/// </summary>
	public bool Walled {
		get => walled;
		set {
			if (walled != value) {
				walled = value;
				Refresh();
			}
		}
	}

	/// <summary>
	/// Terrain type index.
	/// </summary>
	public int TerrainTypeIndex {
		get => terrainTypeIndex;
		set {
			if (terrainTypeIndex != value) {
				terrainTypeIndex = value;
				ShaderData.RefreshTerrain(this);
			}
		}
	}

	/// <summary>
	/// Whether the cell counts as visible.
	/// </summary>
	public bool IsVisible => visibility > 0 && Explorable;

	/// <summary>
	/// Whether the cell counts as explored.
	/// </summary>
	public bool IsExplored {
		get => explored && Explorable;
		private set => explored = value;
	}

	/// <summary>
	/// Whether the cell is explorable. If not it never counts as explored or visible.
	/// </summary>
	public bool Explorable { get; set; }

	/// <summary>
	/// Distance data used by pathfiding algorithm.
	/// </summary>
	public int Distance {
		get => distance;
		set => distance = value;
	}

	/// <summary>
	/// Unit currently occupying the cell, if any.
	/// </summary>
	public HexUnit Unit { get; set; }

	/// <summary>
	/// Pathing data used by pathfinding algorithm.
	/// </summary>
	public HexCell PathFrom { get; set; }

	/// <summary>
	/// Heuristic data used by pathfinding algorithm.
	/// </summary>
	public int SearchHeuristic { get; set; }

	/// <summary>
	/// Search priority used by pathfinding algorithm.
	/// </summary>
	public int SearchPriority => distance + SearchHeuristic;

	/// <summary>
	/// Search phases data used by pathfinding algorithm.
	/// </summary>
	public int SearchPhase { get; set; }

	/// <summary>
	/// Linked list reference used by <see cref="HexCellPriorityQueue"/> for pathfinding.
	/// </summary>
	public HexCell NextWithSamePriority { get; set; }

	/// <summary>
	/// Reference to <see cref="HexCellShaderData"/> that contains the cell.
	/// </summary>
	public HexCellShaderData ShaderData { get; set; }

	int terrainTypeIndex;

	int elevation = int.MinValue;
	int waterLevel;

	int urbanLevel, farmLevel, plantLevel;

	int specialIndex;

	int distance;

	int visibility;

	bool explored;

	bool walled;

	bool hasIncomingRiver, hasOutgoingRiver;
	HexDirection incomingRiver, outgoingRiver;

	[SerializeField]
	HexCell[] neighbors;

	[SerializeField]
	bool[] roads;

	/// <summary>
	/// Increment visibility level.
	/// </summary>
	public void IncreaseVisibility () {
		visibility += 1;
		if (visibility == 1) {
			IsExplored = true;
			ShaderData.RefreshVisibility(this);
		}
	}

	/// <summary>
	/// Decrement visiblility level.
	/// </summary>
	public void DecreaseVisibility () {
		visibility -= 1;
		if (visibility == 0) {
			ShaderData.RefreshVisibility(this);
		}
	}

	/// <summary>
	/// Reset visibility level to zero.
	/// </summary>
	public void ResetVisibility () {
		if (visibility > 0) {
			visibility = 0;
			ShaderData.RefreshVisibility(this);
		}
	}

	/// <summary>
	/// Get one of the neighbor cells.
	/// </summary>
	/// <param name="direction">Neighbor direction relative to the cell.</param>
	/// <returns>Neighbor cell, if it exists.</returns>
	public HexCell GetNeighbor (HexDirection direction) => neighbors[(int)direction];

	/// <summary>
	/// Set a specific neighbor.
	/// </summary>
	/// <param name="direction">Neighbor direction relative to the cell.</param>
	/// <param name="cell">Neighbor.</param>
	public void SetNeighbor (HexDirection direction, HexCell cell) {
		neighbors[(int)direction] = cell;
		cell.neighbors[(int)direction.Opposite()] = this;
	}

	/// <summary>
	/// Get the <see cref="HexEdgeType"/> of a cell edge.
	/// </summary>
	/// <param name="direction">Edge direction relative to the cell.</param>
	/// <returns><see cref="HexEdgeType"/> based on the neighboring cells.</returns>
	public HexEdgeType GetEdgeType (HexDirection direction) => HexMetrics.GetEdgeType(
		elevation, neighbors[(int)direction].elevation
	);

	/// <summary>
	/// Get the <see cref="HexEdgeType"/> based on this and another cell.
	/// </summary>
	/// <param name="otherCell">Other cell to consider as neighbor.</param>
	/// <returns><see cref="HexEdgeType"/> based on this and the other cell.</returns>
	public HexEdgeType GetEdgeType (HexCell otherCell) => HexMetrics.GetEdgeType(
		elevation, otherCell.elevation
	);

	/// <summary>
	/// Whether a river goes through a specific cell edge.
	/// </summary>
	/// <param name="direction">Edge direction relative to the cell.</param>
	/// <returns></returns>
	public bool HasRiverThroughEdge (HexDirection direction) =>
		hasIncomingRiver && incomingRiver == direction ||
		hasOutgoingRiver && outgoingRiver == direction;

	/// <summary>
	/// Remove the incoming river, if it exists.
	/// </summary>
	public void RemoveIncomingRiver () {
		if (!hasIncomingRiver) {
			return;
		}
		hasIncomingRiver = false;
		RefreshSelfOnly();

		HexCell neighbor = GetNeighbor(incomingRiver);
		neighbor.hasOutgoingRiver = false;
		neighbor.RefreshSelfOnly();
	}

	/// <summary>
	/// Remove the outgoing river, if it exists.
	/// </summary>
	public void RemoveOutgoingRiver () {
		if (!hasOutgoingRiver) {
			return;
		}
		hasOutgoingRiver = false;
		RefreshSelfOnly();

		HexCell neighbor = GetNeighbor(outgoingRiver);
		neighbor.hasIncomingRiver = false;
		neighbor.RefreshSelfOnly();
	}

	/// <summary>
	/// Remove both incoming and outgoing rivers, if they exist.
	/// </summary>
	public void RemoveRiver () {
		RemoveOutgoingRiver();
		RemoveIncomingRiver();
	}

	/// <summary>
	/// Define an outgoing river.
	/// </summary>
	/// <param name="direction">Direction of the river.</param>
	public void SetOutgoingRiver (HexDirection direction) {
		if (hasOutgoingRiver && outgoingRiver == direction) {
			return;
		}

		HexCell neighbor = GetNeighbor(direction);
		if (!IsValidRiverDestination(neighbor)) {
			return;
		}

		RemoveOutgoingRiver();
		if (hasIncomingRiver && incomingRiver == direction) {
			RemoveIncomingRiver();
		}
		hasOutgoingRiver = true;
		outgoingRiver = direction;
		specialIndex = 0;

		neighbor.RemoveIncomingRiver();
		neighbor.hasIncomingRiver = true;
		neighbor.incomingRiver = direction.Opposite();
		neighbor.specialIndex = 0;

		SetRoad((int)direction, false);
	}

	/// <summary>
	/// Whether a road goes through a specific cell edge.
	/// </summary>
	/// <param name="direction">Edge direction relative to cell.</param>
	/// <returns></returns>
	public bool HasRoadThroughEdge (HexDirection direction) => roads[(int)direction];

	/// <summary>
	/// Define a road that goes in a specific direction.
	/// </summary>
	/// <param name="direction">Direction relative to cell.</param>
	public void AddRoad (HexDirection direction) {
		if (
			!roads[(int)direction] && !HasRiverThroughEdge(direction) &&
			!IsSpecial && !GetNeighbor(direction).IsSpecial &&
			GetElevationDifference(direction) <= 1
		) {
			SetRoad((int)direction, true);
		}
	}

	/// <summary>
	/// Remove all roads from the cell.
	/// </summary>
	public void RemoveRoads () {
		for (int i = 0; i < neighbors.Length; i++) {
			if (roads[i]) {
				SetRoad(i, false);
			}
		}
	}

	/// <summary>
	/// Get the elevation difference with a neighbor. The indicated neighbor must exist.
	/// </summary>
	/// <param name="direction">Direction to the neighbor, relative to the cell.</param>
	/// <returns>Absolute elevation difference.</returns>
	public int GetElevationDifference (HexDirection direction) {
		int difference = elevation - GetNeighbor(direction).elevation;
		return difference >= 0 ? difference : -difference;
	}

	bool IsValidRiverDestination (HexCell neighbor) =>
		neighbor && (elevation >= neighbor.elevation || waterLevel == neighbor.elevation);

	void ValidateRivers () {
		if (
			hasOutgoingRiver &&
			!IsValidRiverDestination(GetNeighbor(outgoingRiver))
		) {
			RemoveOutgoingRiver();
		}
		if (
			hasIncomingRiver &&
			!GetNeighbor(incomingRiver).IsValidRiverDestination(this)
		) {
			RemoveIncomingRiver();
		}
	}

	void SetRoad (int index, bool state) {
		roads[index] = state;
		neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
		neighbors[index].RefreshSelfOnly();
		RefreshSelfOnly();
	}

	void RefreshPosition () {
		Vector3 position = transform.localPosition;
		position.y = elevation * HexMetrics.elevationStep;
		position.y +=
			(HexMetrics.SampleNoise(position).y * 2f - 1f) *
			HexMetrics.elevationPerturbStrength;
		transform.localPosition = position;

		Vector3 uiPosition = UIRect.localPosition;
		uiPosition.z = -position.y;
		UIRect.localPosition = uiPosition;
	}

	void Refresh () {
		if (Chunk) {
			Chunk.Refresh();
			for (int i = 0; i < neighbors.Length; i++) {
				HexCell neighbor = neighbors[i];
				if (neighbor != null && neighbor.Chunk != Chunk) {
					neighbor.Chunk.Refresh();
				}
			}
			if (Unit) {
				Unit.ValidateLocation();
			}
		}
	}

	void RefreshSelfOnly () {
		Chunk.Refresh();
		if (Unit) {
			Unit.ValidateLocation();
		}
	}

	/// <summary>
	/// Save the cell data.
	/// </summary>
	/// <param name="writer"><see cref="BinaryWriter"/> to use.</param>
	public void Save (BinaryWriter writer) {
		writer.Write((byte)terrainTypeIndex);
		writer.Write((byte)(elevation + 127));
		writer.Write((byte)waterLevel);
		writer.Write((byte)urbanLevel);
		writer.Write((byte)farmLevel);
		writer.Write((byte)plantLevel);
		writer.Write((byte)specialIndex);
		writer.Write(walled);

		if (hasIncomingRiver) {
			writer.Write((byte)(incomingRiver + 128));
		}
		else {
			writer.Write((byte)0);
		}

		if (hasOutgoingRiver) {
			writer.Write((byte)(outgoingRiver + 128));
		}
		else {
			writer.Write((byte)0);
		}

		int roadFlags = 0;
		for (int i = 0; i < roads.Length; i++) {
			if (roads[i]) {
				roadFlags |= 1 << i;
			}
		}
		writer.Write((byte)roadFlags);
		writer.Write(IsExplored);
	}

	/// <summary>
	/// Load the cell data.
	/// </summary>
	/// <param name="reader"><see cref="BinaryReader"/> to use.</param>
	/// <param name="header">Header version.</param>
	public void Load (BinaryReader reader, int header) {
		terrainTypeIndex = reader.ReadByte();
		ShaderData.RefreshTerrain(this);
		elevation = reader.ReadByte();
		if (header >= 4) {
			elevation -= 127;
		}
		RefreshPosition();
		waterLevel = reader.ReadByte();
		urbanLevel = reader.ReadByte();
		farmLevel = reader.ReadByte();
		plantLevel = reader.ReadByte();
		specialIndex = reader.ReadByte();
		walled = reader.ReadBoolean();

		byte riverData = reader.ReadByte();
		if (riverData >= 128) {
			hasIncomingRiver = true;
			incomingRiver = (HexDirection)(riverData - 128);
		}
		else {
			hasIncomingRiver = false;
		}

		riverData = reader.ReadByte();
		if (riverData >= 128) {
			hasOutgoingRiver = true;
			outgoingRiver = (HexDirection)(riverData - 128);
		}
		else {
			hasOutgoingRiver = false;
		}

		int roadFlags = reader.ReadByte();
		for (int i = 0; i < roads.Length; i++) {
			roads[i] = (roadFlags & (1 << i)) != 0;
		}

		IsExplored = header >= 3 ? reader.ReadBoolean() : false;
		ShaderData.RefreshVisibility(this);
	}

	/// <summary>
	/// Set the cell's UI label.
	/// </summary>
	/// <param name="text">Label text.</param>
	public void SetLabel (string text) {
		UnityEngine.UI.Text label = UIRect.GetComponent<Text>();
		label.text = text;
	}

	/// <summary>
	/// Disable the cell's highlight.
	/// </summary>
	public void DisableHighlight () {
		Image highlight = UIRect.GetChild(0).GetComponent<Image>();
		highlight.enabled = false;
	}

	/// <summary>
	/// Enable the cell's highlight. 
	/// </summary>
	/// <param name="color">Highlight color.</param>
	public void EnableHighlight (Color color) {
		Image highlight = UIRect.GetChild(0).GetComponent<Image>();
		highlight.color = color;
		highlight.enabled = true;
	}

	/// <summary>
	/// Set the map data for this cell's <see cref="ShaderData"/>.
	/// </summary>
	/// <param name="data">Data value.</param>
	public void SetMapData (float data) => ShaderData.SetMapData(this, data);
}