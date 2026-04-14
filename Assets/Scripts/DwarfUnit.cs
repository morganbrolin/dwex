using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// A specialized unit controlled by the player capable of mining hex walls.
/// </summary>
public class DwarfUnit : HexUnit
{



	

	void Update()
	{
		// Mining is now handled via the OnCellBlocked coroutine during travel.
		// Update can be used for manual/non-pathfinding mining if needed, 
		// but should check if we are already mining via the coroutine.
	}


}