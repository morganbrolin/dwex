using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Container component for hex cell data.
/// </summary>
[System.Serializable]
public class HexCell
{
	/// <summary>
	/// Hexagonal coordinates unique to the cell.
	/// </summary>
	public HexCoordinates Coordinates
	{ get; set; }

	/// <summary>
	/// Transform component for the cell's UI visiualization. 
	/// </summary>
	public RectTransform UIRect
	{ get; set; }

	/// <summary>
	/// Grid that contains the cell.
	/// </summary>
	public HexGrid Grid
	{ get; set; }

	/// <summary>
	/// Grid chunk that contains the cell.
	/// </summary>
	public HexGridChunk Chunk
	{ get; set; }

	/// <summary>
	/// Unique global index of the cell.
	/// </summary>
	public int Index
	{ get; set; }

	/// <summary>
	/// Map column index of the cell.
	/// </summary>
	public int ColumnIndex
	{ get; set; }

	/// <summary>
	/// Surface elevation level.
	/// </summary>
	public int Elevation
	{
		get => values.Elevation;
		set
		{
			if (values.Elevation == value)
			{
				return;
			}
			values = values.WithElevation(value);
			Grid.ShaderData.ViewElevationChanged(this);
			RefreshPosition();
			ValidateRivers();

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				if (flags.HasRoad(d) && GetElevationDifference(d) > 1)
				{
					RemoveRoad(d);
				}
			}

			Refresh();
		}
	}

	/// <summary>
	/// Water elevation level.
	/// </summary>
	public int WaterLevel
	{
		get => values.WaterLevel;
		set
		{
			if (values.WaterLevel == value)
			{
				return;
			}
			values = values.WithWaterLevel(value);
			Grid.ShaderData.ViewElevationChanged(this);
			ValidateRivers();
			Refresh();
		}
	}

	/// <summary>
	/// Elevation at which the cell is visible.
	/// Highest of surface and water level.
	/// </summary>
	public int ViewElevation =>
		Elevation >= WaterLevel ? Elevation : WaterLevel;

	/// <summary>
	/// Whether the cell counts as underwater,
	/// which is when water is higher than surface.
	/// </summary>
	public bool IsUnderwater => WaterLevel > Elevation;

	/// <summary>
	/// Whether there is an incoming river.
	/// </summary>
	public bool HasIncomingRiver => flags.HasAny(HexFlags.RiverIn);

	/// <summary>
	/// Whether there is an outgoing river.
	/// </summary>
	public bool HasOutgoingRiver => flags.HasAny(HexFlags.RiverOut);

	/// <summary>
	/// Whether there is a river, either incoming, outgoing, or both.
	/// </summary>
	public bool HasRiver => flags.HasAny(HexFlags.River);

	/// <summary>
	/// Whether a river begins or ends in the cell.
	/// </summary>
	public bool HasRiverBeginOrEnd => HasIncomingRiver != HasOutgoingRiver;

	/// <summary>
	/// Whether the cell contains roads.
	/// </summary>
	public bool HasRoads => flags.HasAny(HexFlags.Roads);

	/// <summary>
	/// Incoming river direction, if applicable.
	/// </summary>
	public HexDirection IncomingRiver => flags.RiverInDirection();

	/// <summary>
	/// Outgoing river direction, if applicable.
	/// </summary>
	public HexDirection OutgoingRiver => flags.RiverOutDirection();

	/// <summary>
	/// Local position of this cell.
	/// </summary>
	public Vector3 Position
	{ get; set; }

	/// <summary>
	/// Vertical positions the the stream bed, if applicable.
	/// </summary>
	public float StreamBedY =>
		(Elevation + HexMetrics.streamBedElevationOffset) *
		HexMetrics.elevationStep;

	/// <summary>
	/// Vertical position of the river's surface, if applicable.
	/// </summary>
	public float RiverSurfaceY =>
		(Elevation + HexMetrics.waterElevationOffset) *
		HexMetrics.elevationStep;

	/// <summary>
	/// Vertical position of the water surface, if applicable.
	/// </summary>
	public float WaterSurfaceY =>
		(WaterLevel + HexMetrics.waterElevationOffset) *
		HexMetrics.elevationStep;

	/// <summary>
	/// Urban feature level.
	/// </summary>
	public int UrbanLevel
	{
		get => values.UrbanLevel;
		set
		{
			if (values.UrbanLevel != value)
			{
				values = values.WithUrbanLevel(value);
				Chunk.Refresh();
			}
		}
	}

	/// <summary>
	/// Farm feature level.
	/// </summary>
	public int FarmLevel
	{
		get => values.FarmLevel;
		set
		{
			if (values.FarmLevel != value)
			{
				values = values.WithFarmLevel(value);
				Chunk.Refresh();
			}
		}
	}

	/// <summary>
	/// Plant feature level.
	/// </summary>
	public int PlantLevel
	{
		get => values.PlantLevel;
		set
		{
			if (values.PlantLevel != value)
			{
				values = values.WithPlantLevel(value);
				Chunk.Refresh();
			}
		}
	}

	/// <summary>
	/// Special feature index.
	/// </summary>
	public int SpecialIndex
	{
		get => values.SpecialIndex;
		set
		{
			if (values.SpecialIndex != value && !HasRiver)
			{
				values = values.WithSpecialIndex(value);
				RemoveRoads();
				Chunk.Refresh();
			}
		}
	}

	/// <summary>
	/// Whether the cell contains a special feature.
	/// </summary>
	public bool IsSpecial => SpecialIndex > 0;

	/// <summary>
	/// Whether the cell is considered inside a walled region.
	/// </summary>
	public bool Walled
	{
		get => flags.HasAny(HexFlags.Walled);
		set
		{
			HexFlags newFlags = value ?
				flags.With(HexFlags.Walled) : flags.Without(HexFlags.Walled);
			if (flags != newFlags)
			{
				flags = newFlags;
				Refresh();
			}
		}
	}

	/// <summary>
	/// Terrain type index.
	/// </summary>
	public int TerrainTypeIndex
	{
		get => values.TerrainTypeIndex;
		set
		{
			if (values.TerrainTypeIndex != value)
			{
				values = values.WithTerrainTypeIndex(value);
				Grid.ShaderData.RefreshTerrain(this);
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
	public bool IsExplored =>
		flags.HasAll(HexFlags.Explored | HexFlags.Explorable);

	/// <summary>
	/// Whether the cell is explorable.
	/// If not it never counts as explored or visible.
	/// </summary>
	public bool Explorable
	{
		get => flags.HasAny(HexFlags.Explorable);
		set => flags = value ?
			flags.With(HexFlags.Explorable) :
			flags.Without(HexFlags.Explorable);
	}

	/// <summary>
	/// Distance data used by pathfiding algorithm.
	/// </summary>
	public int Distance
	{
		get => distance;
		set => distance = value;
	}

	/// <summary>
	/// Unit currently occupying the cell, if any.
	/// </summary>
	public HexUnit Unit
	{ get; set; }

	/// <summary>
	/// Pathing data used by pathfinding algorithm.
	/// </summary>
	public int PathFromIndex
	{ get; set; }

	/// <summary>
	/// Heuristic data used by pathfinding algorithm.
	/// </summary>
	public int SearchHeuristic
	{ get; set; }

	/// <summary>
	/// Search priority used by pathfinding algorithm.
	/// </summary>
	public int SearchPriority => distance + SearchHeuristic;

	/// <summary>
	/// Search phases data used by pathfinding algorithm.
	/// </summary>
	public int SearchPhase
	{ get; set; }

	/// <summary>
	/// Linked list reference used by <see cref="HexCellPriorityQueue"/>
	/// for pathfinding.
	/// </summary>
	[field: System.NonSerialized]
	public HexCell NextWithSamePriority
	{ get; set; }

	HexFlags flags;

	HexValues values;

	int distance;

	int visibility;

	/// <summary>
	/// Increment visibility level.
	/// </summary>
	public void IncreaseVisibility()
	{
		visibility += 1;
		if (visibility == 1)
		{
			flags = flags.With(HexFlags.Explored);
			Grid.ShaderData.RefreshVisibility(this);
		}
	}

	/// <summary>
	/// Decrement visiblility level.
	/// </summary>
	public void DecreaseVisibility()
	{
		visibility -= 1;
		if (visibility == 0)
		{
			Grid.ShaderData.RefreshVisibility(this);
		}
	}

	/// <summary>
	/// Reset visibility level to zero.
	/// </summary>
	public void ResetVisibility()
	{
		if (visibility > 0)
		{
			visibility = 0;
			Grid.ShaderData.RefreshVisibility(this);
		}
	}

	/// <summary>
	/// Get one of the neighbor cells. Only valid if that neighbor exists.
	/// </summary>
	/// <param name="direction">Neighbor direction relative to the cell.</param>
	/// <returns>Neighbor cell, if it exists.</returns>
	public HexCell GetNeighbor(HexDirection direction) =>
		Grid.GetCell(Coordinates.Step(direction));

	/// <summary>
	/// Try to get one of the neighbor cells.
	/// </summary>
	/// <param name="direction">Neighbor direction relative to the cell.</param>
	/// <param name="cell">The neighbor cell, if it exists.</param>
	/// <returns>Whether the neighbor exists.</returns>
	public bool TryGetNeighbor(HexDirection direction, out HexCell cell) =>
		Grid.TryGetCell(Coordinates.Step(direction), out cell);

	/// <summary>
	/// Get the <see cref="HexEdgeType"/> based on this and another cell.
	/// </summary>
	/// <param name="otherCell">Other cell to consider as neighbor.</param>
	/// <returns><see cref="HexEdgeType"/> between cells.</returns>
	public HexEdgeType GetEdgeType(HexCell otherCell) =>
		HexMetrics.GetEdgeType(Elevation, otherCell.Elevation);

	/// <summary>
	/// Whether a river goes through a specific cell edge.
	/// </summary>
	/// <param name="direction">Edge direction relative to the cell.</param>
	/// <returns>Whether a river goes through the edge.</returns>
	public bool HasRiverThroughEdge(HexDirection direction) =>
		flags.HasRiverIn(direction) || flags.HasRiverOut(direction);

	/// <summary>
	/// Whether an incoming river goes through a specific cell edge.
	/// </summary>
	/// <param name="direction">Edge direction relative to the cell.</param>
	/// <returns>Whether an incoming river goes through the edge.</returns>
	public bool HasIncomingRiverThroughEdge(HexDirection direction) =>
		flags.HasRiverIn(direction);

	void RemoveIncomingRiver()
	{
		if (!HasIncomingRiver)
		{
			return;
		}
		
		HexCell neighbor = GetNeighbor(IncomingRiver);
		flags = flags.Without(HexFlags.RiverIn);
		neighbor.flags = neighbor.flags.Without(HexFlags.RiverOut);
		neighbor.Chunk.Refresh();
		Chunk.Refresh();
	}

	/// <summary>
	/// Remove the outgoing river, if it exists.
	/// </summary>
	void RemoveOutgoingRiver()
	{
		if (!HasOutgoingRiver)
		{
			return;
		}
		
		HexCell neighbor = GetNeighbor(OutgoingRiver);
		flags = flags.Without(HexFlags.RiverOut);
		neighbor.flags = neighbor.flags.Without(HexFlags.RiverIn);
		neighbor.Chunk.Refresh();
		Chunk.Refresh();
	}

	/// <summary>
	/// Remove both incoming and outgoing rivers, if they exist.
	/// </summary>
	public void RemoveRiver()
	{
		RemoveOutgoingRiver();
		RemoveIncomingRiver();
	}

	/// <summary>
	/// Define an outgoing river.
	/// </summary>
	/// <param name="direction">Direction of the river.</param>
	public void SetOutgoingRiver(HexDirection direction)
	{
		if (flags.HasRiverOut(direction))
		{
			return;
		}

		HexCell neighbor = GetNeighbor(direction);
		if (!IsValidRiverDestination(neighbor))
		{
			return;
		}

		RemoveOutgoingRiver();
		if (flags.HasRiverIn(direction))
		{
			RemoveIncomingRiver();
		}

		flags = flags.WithRiverOut(direction);
		SpecialIndex = 0;
		neighbor.RemoveIncomingRiver();
		neighbor.flags = neighbor.flags.WithRiverIn(direction.Opposite());
		neighbor.SpecialIndex = 0;

		RemoveRoad(direction);
	}

	/// <summary>
	/// Whether a road goes through a specific cell edge.
	/// </summary>
	/// <param name="direction">Edge direction relative to cell.</param>
	/// <returns>Whether a road goes through the edge.</returns>
	public bool HasRoadThroughEdge(HexDirection direction) =>
		flags.HasRoad(direction);

	/// <summary>
	/// Define a road that goes in a specific direction.
	/// </summary>
	/// <param name="direction">Direction relative to cell.</param>
	public void AddRoad(HexDirection direction)
	{
		if (
			!flags.HasRoad(direction) && !HasRiverThroughEdge(direction) &&
			!IsSpecial && !GetNeighbor(direction).IsSpecial &&
			GetElevationDifference(direction) <= 1
		)
		{
			flags = flags.WithRoad(direction);
			HexCell neighbor = GetNeighbor(direction);
			neighbor.flags = neighbor.flags.WithRoad(direction.Opposite());
			neighbor.Chunk.Refresh();
			Chunk.Refresh();
		}
	}

	/// <summary>
	/// Remove all roads from the cell.
	/// </summary>
	public void RemoveRoads()
	{
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
		{
			if (flags.HasRoad(d))
			{
				RemoveRoad(d);
			}
		}
	}

	int GetElevationDifference(HexDirection direction)
	{
		int difference = Elevation - GetNeighbor(direction).Elevation;
		return difference >= 0 ? difference : -difference;
	}

	bool IsValidRiverDestination(HexCell neighbor) => neighbor &&
		(Elevation >= neighbor.Elevation || WaterLevel == neighbor.Elevation);

	void ValidateRivers()
	{
		if (HasOutgoingRiver &&
			!IsValidRiverDestination(GetNeighbor(OutgoingRiver)))
		{
			RemoveOutgoingRiver();
		}
		if (HasIncomingRiver &&
			!GetNeighbor(IncomingRiver).IsValidRiverDestination(this))
		{
			RemoveIncomingRiver();
		}
	}

	void RemoveRoad(HexDirection direction)
	{
		flags = flags.WithoutRoad(direction);
		HexCell neighbor = GetNeighbor(direction);
		neighbor.flags = neighbor.flags.WithoutRoad(direction.Opposite());
		neighbor.Chunk.Refresh();
		Chunk.Refresh();
	}

	void RefreshPosition()
	{
		Vector3 position = Position;
		position.y = Elevation * HexMetrics.elevationStep;
		position.y +=
			(HexMetrics.SampleNoise(position).y * 2f - 1f) *
			HexMetrics.elevationPerturbStrength;
		Position = position;

		Vector3 uiPosition = UIRect.localPosition;
		uiPosition.z = -position.y;
		UIRect.localPosition = uiPosition;
	}

	void Refresh()
	{
		if (Chunk)
		{
			Chunk.Refresh();
			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				if (TryGetNeighbor(d, out HexCell neighbor) &&
					neighbor.Chunk != Chunk)
				{
					neighbor.Chunk.Refresh();
				}
			}
			if (Unit)
			{
				Unit.ValidateLocation();
			}
		}
	}

	/// <summary>
	/// Save the cell data.
	/// </summary>
	/// <param name="writer"><see cref="BinaryWriter"/> to use.</param>
	public void Save(BinaryWriter writer)
	{
		values.Save(writer);
		flags.Save(writer);
	}

	/// <summary>
	/// Load the cell data.
	/// </summary>
	/// <param name="reader"><see cref="BinaryReader"/> to use.</param>
	/// <param name="header">Header version.</param>
	public void Load(BinaryReader reader, int header)
	{
		values = HexValues.Load(reader, header);
		flags = flags.Load(reader, header);
		RefreshPosition();
		Grid.ShaderData.RefreshTerrain(this);
		Grid.ShaderData.RefreshVisibility(this);
	}

	/// <summary>
	/// Set the cell's UI label.
	/// </summary>
	/// <param name="text">Label text.</param>
	public void SetLabel(string text) =>
		UIRect.GetComponent<Text>().text = text;

	/// <summary>
	/// Disable the cell's highlight.
	/// </summary>
	public void DisableHighlight() =>
		UIRect.GetChild(0).GetComponent<Image>().enabled = false;

	/// <summary>
	/// Enable the cell's highlight. 
	/// </summary>
	/// <param name="color">Highlight color.</param>
	public void EnableHighlight(Color color)
	{
		Image highlight = UIRect.GetChild(0).GetComponent<Image>();
		highlight.color = color;
		highlight.enabled = true;
	}

	/// <summary>
	/// A cell counts as true if it is not null, otherwise as false.
	/// </summary>
	/// <param name="cell">The cell to check.</param>
	public static implicit operator bool(HexCell cell) => cell != null;
}
