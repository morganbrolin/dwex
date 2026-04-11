using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Component that manages the game UI.
/// </summary>
public class HexGameUI : MonoBehaviour
{
	[SerializeField]
	HexGrid grid;

	HexCell currentCell;

	HexUnit selectedUnit;

	InputAction selectAction, commandAction, positionAction;

	void Awake()
	{
		selectAction = InputSystem.actions.FindAction("Interact");
		commandAction = InputSystem.actions.FindAction("Command");
		positionAction = InputSystem.actions.FindAction("Position");
	}

	/// <summary>
	/// Set whether map edit mode is active.
	/// </summary>
	/// <param name="toggle">Whether edit mode is enabled.</param>
	public void SetEditMode(bool toggle)
	{
		enabled = !toggle;
		grid.ShowUI(!toggle);
		grid.ClearPath();
		if (toggle)
		{
			Shader.EnableKeyword("_HEX_MAP_EDIT_MODE");
		}
		else
		{
			Shader.DisableKeyword("_HEX_MAP_EDIT_MODE");
		}
	}

	void Update()
	{
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			if (selectAction.WasPerformedThisFrame())
			{
				DoSelection();
			}
			else if (selectedUnit)
			{
				if (commandAction.WasPerformedThisFrame())
				{
					DoMove();
				}
				else
				{
					DoPathfinding();
				}
			}
		}
	}

	void DoSelection()
	{
		grid.ClearPath();
		UpdateCurrentCell();
		if (currentCell)
		{
			selectedUnit = currentCell.Unit;
		}
	}

	void DoPathfinding()
	{
		if (UpdateCurrentCell())
		{
			// Allow pathfinding if it's a valid destination OR if it's a wall and we are a Dwarf
			bool canTarget = currentCell && 
				(selectedUnit.IsValidDestination(currentCell) || 
				(currentCell.Values.Elevation >= 5 && selectedUnit is DwarfUnit));

			if (canTarget)
			{
				grid.FindPath(selectedUnit.Location, currentCell, selectedUnit);
			}
			else
			{
				grid.ClearPath();
			}
		}
	}

	void DoMove()
	{
		if (grid.HasPath)
		{
			List<int> path = grid.GetPath();
			HexCell target = grid.GetCell(path[path.Count - 1]);
			
			// If the path ends in a wall, set the mining target and remove the wall from the movement path
			if (target.Values.Elevation >= 5 && selectedUnit is DwarfUnit dwarf)
			{
				dwarf.PendingMineTarget = target;
				path.RemoveAt(path.Count - 1);
			}

			// Only initiate travel if the unit actually needs to move to a new hex
			if (path.Count > 1)
			{
				selectedUnit.Travel(path);
			}
			grid.ClearPath();
		}
	}

	bool UpdateCurrentCell()
	{
		HexCell cell = grid.GetCell(
			Camera.main.ScreenPointToRay(positionAction.ReadValue<Vector2>()));
		if (cell)
		{
			currentCell = cell;
			return true;
		}
		return false;
	}
}
