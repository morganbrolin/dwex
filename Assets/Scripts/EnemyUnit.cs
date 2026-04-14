using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// An enemy unit that does not reveal map visibility to the player.
/// </summary>
///
public class EnemyUnit : HexUnit
{
	private void Start()
	{
		Grid.GenerateMiningFlowMap(this.miningSpeed,this.Speed,Grid.currentHomeIndex);
	}

	void Update()
	{
		if (IsTraveling || Grid.currentHomeIndex == -1)
		{
			return;
		}
		
		string key = miningSpeed + " " + Speed;

		if (Grid.miningMapFlowMap.TryGetValue(key, out HexCellFlowData[] flowMap))
		{
			HexCellFlowData currentData = flowMap[Location.Index];


			if (currentData.totalCost > 0 && currentData.totalCost < 999999f)
			{
				HexCell nextCell = Location.GetNeighbor(currentData.direction);
				if (!(nextCell is null))
				{
					List<int> stepPath = ListPool<int>.Get();
					stepPath.Add(Location.Index);
					stepPath.Add(nextCell.Index);
					Travel(stepPath);
				}
			}
		}
	}
}