/// <summary>
/// Container struct for bundled hex cell data.
/// </summary>
[System.Serializable]
public struct HexCellData
{
	/// <summary>
	/// Cell flags.
	/// </summary>
	public HexFlags flags;

	/// <summary>
	/// Cell values.
	/// </summary>
	public HexValues values;

	/// <summary>
	/// Cell coordinates.
	/// </summary>
	public HexCoordinates coordinates;

	/// <summary>
	/// Surface elevation level.
	/// </summary>
	public readonly int Elevation => values.Elevation;
	

	/// <summary>
	/// Terrain type index.
	/// </summary>
	public readonly int TerrainTypeIndex => values.TerrainTypeIndex;

	/// <summary>
	/// EnemyQuantity feature level.
	/// </summary>
	public readonly int EnemyQuantityLevel => values.EnemyQuantityLevel;

	/// <summary>
	/// GemQuality feature level.
	/// </summary>
	public readonly int GemQualityLevel => values.GemQualityLevel;

	/// <summary>
	/// EnemyQuality feature level.
	/// </summary>
	public readonly int EnemyQualityLevel => values.EnemyQualityLevel;



	/// <summary>
	/// Whether the cell counts as explored.
	/// </summary>
	public readonly bool IsExplored =>
		flags.HasAll(HexFlags.Explored | HexFlags.Explorable);





	/// <summary>
	/// Elevation at which the cell is visible.
	/// Highest of surface level.
	/// </summary>
	public readonly int ViewElevation =>
		Elevation ;
	
	// <summary>
	/// Get the <see cref="HexEdgeType"/> based on this and another cell.
	/// </summary>
	/// <param name="otherCell">Other cell to consider as neighbor.</param>
	/// <returns><see cref="HexEdgeType"/> between cells.</returns>
	public readonly HexEdgeType GetEdgeType(HexCellData otherCell) =>
		HexMetrics.GetEdgeType(values.Elevation, otherCell.values.Elevation);
	


}
