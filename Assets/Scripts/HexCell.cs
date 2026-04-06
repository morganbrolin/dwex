using UnityEngine;

/// <summary>
/// Struct that identifies a hex cell.
/// </summary>
[System.Serializable]
public class HexCell : MonoBehaviour
{
	int index;

	HexGrid grid;

	public float CurrentHealth  { get; set; }
	
	public float MaxHealth  { get; set; }
	


	/// <summary>
	/// Creates a cell given an index and grid.
	/// </summary>
	/// <param name="index">Index of the cell.</param>
	/// <param name="grid">Grid the cell is a part of.</param>
	
	public void Init(int index, HexGrid grid)
	{
		Debug.Log("init");
		this.index = index;
		this.grid = grid;	}

	/// <summary>
	/// Hexagonal coordinates unique to the cell.
	/// </summary>
	public HexCoordinates Coordinates =>
		grid.CellData[index].coordinates;

	/// <summary>
	/// Unique global index of the cell.
	/// </summary>
	public  int Index => index;

	/// <summary>
	/// Local position of this cell.
	/// </summary>
	public  Vector3 Position => grid.CellPositions[index];

	/// <summary>
	/// Set the elevation level.
	/// </summary>
	/// <param name="elevation">Elevation level.</param>
	public  void SetElevation (int elevation)
	{
		if (Values.Elevation != elevation)
		{
			Values = Values.WithElevation(elevation);
			grid.ShaderData.ViewElevationChanged(index);
			grid.RefreshCellPosition(index);
			grid.RefreshCellWithDependents(index);
		}
	}



	/// <summary>
	/// Set the EnemyQuantity level.
	/// </summary>
	/// <param name="enemyQuantityLevel">enemyQuantity level.</param>
	public  void SetEnemyQuantityLevel (int enemyQuantity)
	{
		if (Values.EnemyQuantityLevel != enemyQuantity)
		{
			Values = Values.WithEnemyQuantityLevel(enemyQuantity);
			Refresh();
		}
	}

	/// <summary>
	/// Set the gemQuality level.
	/// </summary>
	/// <param name="gemQualityevel">GemQuality level.</param>
	public  void SetGemQualityLevel (int gemQualityLevel)
	{
		if (Values.GemQualityLevel != gemQualityLevel)
		{
			Values = Values.WithGemQualityLevel(gemQualityLevel);
			Refresh();
		}
	}

	/// <summary>
	/// Set the enemyQuality level.
	/// </summary>
	/// <param name="enemyQualityLevel">EnemyQuality level.</param>
	public  void SetEnemyQualityLevel(int enemyQualityLevel)
	{
		if (Values.EnemyQualityLevel != enemyQualityLevel)
		{
			Values = Values.WithEnemyQualityLevel(enemyQualityLevel);
			Refresh();
		}
	}
	

	/// <summary>
	/// Set the terrain type index.
	/// </summary>
	/// <param name="terrainTypeIndex">Terrain type index.</param>
	public  void SetTerrainTypeIndex (int terrainTypeIndex)
	{
		if (Values.TerrainTypeIndex != terrainTypeIndex)
		{
			Values = Values.WithTerrainTypeIndex(terrainTypeIndex);
			grid.ShaderData.RefreshTerrain(index);
		}
	}

	/// <summary>
	/// Unit currently occupying the cell, if any.
	/// </summary>
	public  HexUnit Unit
	{
		get => grid.CellUnits[index];
		set => grid.CellUnits[index] = value;
	}

	/// <summary>
	/// Flags of the cell.
	/// </summary>
	public  HexFlags Flags
	{
		get => grid.CellData[index].flags;
		set => grid.CellData[index].flags = value;
	}

	/// <summary>
	/// Values of the cell.
	/// </summary>
	public  HexValues Values
	{
		get => grid.CellData[index].values;
		set => grid.CellData[index].values = value;
	}

	/// <summary>
	/// Get one of the neighbor cells. Only valid if that neighbor exists.
	/// </summary>
	/// <param name="direction">Neighbor direction relative to the cell.</param>
	/// <returns>Neighbor cell, if it exists.</returns>
	public  HexCell GetNeighbor(HexDirection direction) =>
		grid.GetCell(Coordinates.Step(direction));

	/// <summary>
	/// Try to get one of the neighbor cells.
	/// </summary>
	/// <param name="direction">Neighbor direction relative to the cell.</param>
	/// <param name="cell">The neighbor cell, if it exists.</param>
	/// <returns>Whether the neighbor exists.</returns>
	public  bool TryGetNeighbor(
		HexDirection direction, out HexCell cell) =>
		grid.TryGetCell(Coordinates.Step(direction), out cell);
	

	 void Refresh() => grid.RefreshCell(index);

	/// <inheritdoc/>
	public  override bool Equals(object obj) =>
		obj is HexCell cell && this == cell;

	/// <inheritdoc/>
	public  override int GetHashCode() =>
		grid != null ? index.GetHashCode() ^ grid.GetHashCode() : 0;
	

	public static bool operator ==(HexCell a, HexCell b) =>
		a.index == b.index && a.grid == b.grid;
	
	public static bool operator !=(HexCell a, HexCell b) =>
		a.index != b.index || a.grid != b.grid;
}
