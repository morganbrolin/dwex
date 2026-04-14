using System.ComponentModel;

/// <summary>
/// Cell data for searching using flow.
/// </summary>
[System.Serializable]
public class HexCellFlowData
{
	public HexCellFlowData()
	{
		// TODO maybe some other way
		totalCost = 9999999f;
	}

	/// <summary>
	/// Cost of reaching the cell from home.
	/// </summary>
	public float totalCost;
	
	/// <summary>
	/// Cost of traversing the cell.
	/// </summary>
	public float cost;

	/// <summary>
	/// Index of cell for lowest cost path
	/// </summary>
	public int pathFrom;
	
	/// <summary>
	/// Direction for lowest cost path
	/// </summary>
	public HexDirection direction;




}
