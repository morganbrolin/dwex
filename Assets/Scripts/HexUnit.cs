using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Component representing a unit that occupies a cell of the hex map.
/// </summary>
public class HexUnit : MonoBehaviour
{
	const float rotationSpeed = 180f;
	const float travelSpeed = 3f;

	public static HexUnit unitPrefab;

	public static HexUnit enemyUnitPrefab;

	public HexGrid Grid { get; set; }

	protected int locationCellIndex = -1;
	protected int currentTravelLocationCellIndex = -1;
	
	[SerializeField]
	public float miningSpeed;
	
	/// <summary>
	/// A wall cell that the dwarf intends to mine upon arrival.
	/// </summary>
	public HexCell PendingMineTarget { get; set; }

	private bool isMining = false;

	/// <summary>
	/// Cell that the unit occupies.
	/// </summary>
	public virtual HexCell Location
	{
		get => Grid.GetCell(locationCellIndex);
		set
		{
			if (locationCellIndex >= 0)
			{
				HexCell location = Grid.GetCell(locationCellIndex);
				Grid.DecreaseVisibility(location, VisionRange);
				location.Unit = null;
			}
			locationCellIndex = value.Index;
			value.Unit = this;
			Grid.IncreaseVisibility(value, VisionRange);
			transform.localPosition = value.Position;
			Grid.MakeChildOfColumn(transform, value.Coordinates.ColumnIndex);
		}
	}

	

	/// <summary>
	/// Orientation that the unit is facing.
	/// </summary>
	public float Orientation
	{
		get => orientation;
		set
		{
			orientation = value;
			transform.localRotation = Quaternion.Euler(0f, value, 0f);
		}
	}

	/// <summary>
	/// Speed of the unit, in cells per turn.
	/// </summary>
	public int Speed => 24;

	/// <summary>
	/// Vision range of the unit, in cells.
	/// </summary>
	public int VisionRange => 3;

	float orientation;

	List<int> _pathToTravel;

	/// <summary>
	/// Whether the unit is currently moving along a path.
	/// </summary>
	public bool IsTraveling => _pathToTravel != null;

	
	/// <summary>
	/// Validate the position of the unit.
	/// </summary>
	public void ValidateLocation() =>
		transform.localPosition = Grid.GetCell(locationCellIndex).Position;

	/// <summary>
	/// Checl whether a cell is a valid destination for the unit.
	/// </summary>
	/// <param name="cell">Cell to check.</param>
	/// <returns>Whether the unit could occupy the cell.</returns>
	public bool IsValidDestination(HexCell cell) =>
		cell.Flags.HasAll(HexFlags.Explored | HexFlags.Explorable);

	/// <summary>
	/// Travel along a path.
	/// </summary>
	/// <param name="path">List of cells that describe a valid path.</param>
	public void Travel(List<int> path)
	{
		
		// TODO currently the only not buggy stopallcoroutines 
		// i want the same for traveling but it jumps around
		if (IsTraveling && isMining)
		{
			StopAllCoroutines();
			Location = Grid.GetCell((path[0]));
			_pathToTravel = path;
			StartCoroutine(TravelPath());
			
		}

		if (!IsTraveling)
		{
			isMining = false;
			//StopTravel();
			Location = Grid.GetCell((path[0]));
			_pathToTravel = path;
			StartCoroutine(TravelPath());
		}
		else
		{
			Debug.Log("isTraveling");
			
		}


	}

	public IEnumerator TravelPath()
	{
		return TravelPath(_pathToTravel);
	}

	public IEnumerator TravelPath(List<int> pathToTravel)
	{

		
		Vector3 a, b, c = Grid.GetCell(pathToTravel[0]).Position;
		yield return LookAt(Grid.GetCell(pathToTravel[1]).Position);

		if (currentTravelLocationCellIndex < 0)
		{
			currentTravelLocationCellIndex = pathToTravel[0];
		}
		HexCell currentTravelLocation = Grid.GetCell(
			currentTravelLocationCellIndex);
		int currentColumn = currentTravelLocation.Coordinates.ColumnIndex;

		float t = Time.deltaTime * travelSpeed;
		for (int i = 1; i < pathToTravel.Count; i++)
		{
			HexCell nextCell = Grid.GetCell(pathToTravel[i]);
			if (nextCell.isWall)
			{
				// While waiting/mining, ensure we are standing at the previous cell logically
				yield return StartCoroutine(OnCellBlocked(nextCell));
			}

			// Decrease visibility of the cell we are LEAVING
			Grid.DecreaseVisibility(Grid.GetCell(pathToTravel[i - 1]), VisionRange);

			// Update logical position as we traverse
			Grid.GetCell(locationCellIndex).Unit = null;
			locationCellIndex = pathToTravel[i];
			Grid.GetCell(locationCellIndex).Unit = this;

			currentTravelLocation = Grid.GetCell(pathToTravel[i]);
			currentTravelLocationCellIndex = currentTravelLocation.Index;
			a = c;
			b = Grid.GetCell(pathToTravel[i - 1]).Position;

			int nextColumn = currentTravelLocation.Coordinates.ColumnIndex;
			if (currentColumn != nextColumn)
			{
				Grid.MakeChildOfColumn(transform, nextColumn);
				currentColumn = nextColumn;
			}

			c = (b + currentTravelLocation.Position) * 0.5f;
			// Increase visibility of the cell we are ENTERING
			Grid.IncreaseVisibility(currentTravelLocation, VisionRange);

			for (; t < 1f; t += Time.deltaTime * travelSpeed)
			{
				transform.localPosition = Bezier.GetPoint(a, b, c, t);
				Vector3 d = Bezier.GetDerivative(a, b, c, t);
				d.y = 0f;
				transform.localRotation = Quaternion.LookRotation(d);
				yield return null;
			}
			t -= 1f;
		}

		currentTravelLocationCellIndex = -1;

		HexCell location = Grid.GetCell(locationCellIndex);
		a = c;
		b = location.Position;
		c = b;
		Grid.IncreaseVisibility(location, VisionRange);
		for (; t < 1f; t += Time.deltaTime * travelSpeed)
		{
			transform.localPosition = Bezier.GetPoint(a, b, c, t);
			Vector3 d = Bezier.GetDerivative(a, b, c, t);
			d.y = 0f;
			transform.localRotation = Quaternion.LookRotation(d);
			yield return null;
		}

		transform.localPosition = location.Position;
		orientation = transform.localRotation.eulerAngles.y;
		ListPool<int>.Add(pathToTravel);
		_pathToTravel = null;
		
	}
	

	IEnumerator LookAt(Vector3 point)
	{

		point.y = transform.localPosition.y;
		Quaternion fromRotation = transform.localRotation;
		Quaternion toRotation =
			Quaternion.LookRotation(point - transform.localPosition);
		float angle = Quaternion.Angle(fromRotation, toRotation);

		if (angle > 0f)
		{
			float speed = rotationSpeed / angle;
			for (float t = Time.deltaTime * speed;
				t < 1f; t += Time.deltaTime * speed)
			{
				transform.localRotation = Quaternion.Slerp(
					fromRotation, toRotation, t);
				yield return null;
			}
		}

		transform.LookAt(point);
		orientation = transform.localRotation.eulerAngles.y;
	}
	
	
	public void StopTravel()
	{
		if (PendingMineTarget)
		{
			PendingMineTarget.hpSlider.gameObject.SetActive(false);
			PendingMineTarget = null;
		}
	}



	/// <summary>
	/// Applies mining damage to a target cell over time.
	/// </summary>
	/// <param name="target">The cell to mine.</param>
	public void MineTick(HexCell target)
	{
		isMining = true;
		if (target && target.Values.Elevation >= 5)
		{
			float currentHealth = target.CurrentHealth;
			currentHealth -= miningSpeed * Time.deltaTime;
			target.hpSlider.enabled = true;
			target.hpSlider.gameObject.SetActive(true);
			
			target.hpSlider.value = currentHealth / target.MaxHealth;
			if (currentHealth <= 0f)
			{
				// Award gems before clearing the wall
				//int reward = Grid.GetGemReward(target);
				//Grid.Gems += reward;
				//Debug.Log($"Mined {reward} gems! Total: {Grid.Gems}");

				target.BecomeGround();
				Grid.RefreshCellWithDependents(target.Index);
				PendingMineTarget = default;
				target.hpSlider.enabled = false;
				target.hpSlider.gameObject.SetActive(false);
				isMining = false;
			}
			else
			{
				target.CurrentHealth = currentHealth;
			}
		}
		else
		{
			PendingMineTarget = default;
		}
	}

	/// <summary>
	/// Implementation of the blocked path logic for the Dwarf.
	/// Stops movement and mines the wall until it is destroyed.
	/// </summary>
	protected IEnumerator OnCellBlocked(HexCell blockedCell)
	{
		PendingMineTarget = blockedCell;
		// Wait here in the travel coroutine until the wall is ground
		while (blockedCell && blockedCell.isWall)
		{
			// Perform mining logic every frame while blocked
			MineTick(blockedCell);
			yield return null;
		}
		PendingMineTarget = null;
	}

	/// <summary>
	/// Get the movement cost of moving from one cell to another.
	/// </summary>
	/// <param name="fromCell">Cell to move from.</param>
	/// <param name="toCell">Cell to move to.</param>
	/// <param name="direction">Movement direction.</param>
	/// <returns></returns>
	public virtual int GetMoveCost(
		HexCell fromCell, HexCell toCell, HexDirection direction)
	{
		if (!IsValidDestination(toCell))
		{
			return -1;
		}
		if (toCell.isWall && miningSpeed <= 0)
		{
			return -1;
		}

		// Cost is 1 for ground, plus mining time penalty for walls.
		int moveCost = 1 + (int)(toCell.CurrentHealth / miningSpeed);
		return moveCost;
	}

	/// <summary>
	/// Terminate the unit.
	/// </summary>
	public void Die()
	{
		HexCell location = Grid.GetCell(locationCellIndex);
		Grid.DecreaseVisibility(location, VisionRange);
		location.Unit = null;
		Destroy(gameObject);
	}

	/// <summary>
	/// Save the unit data.
	/// </summary>
	/// <param name="writer"><see cref="BinaryWriter"/> to use.</param>
	public void Save(BinaryWriter writer)
	{
		//location.Coordinates.Save(writer);
		Grid.GetCell(locationCellIndex).Coordinates.Save(writer);
		writer.Write(orientation);
	}

	/// <summary>
	/// Load the unit data.
	/// </summary>
	/// <param name="reader"><see cref="BinaryReader"/> to use.</param>
	/// <param name="grid"><see cref="HexGrid"/> to add the unit to.</param>
	public static void Load(BinaryReader reader, HexGrid grid)
	{
		HexCoordinates coordinates = HexCoordinates.Load(reader);
		float orientation = reader.ReadSingle();
		grid.AddUnit(
			Instantiate(unitPrefab), grid.GetCell(coordinates), orientation);
	}

	void OnEnable()
	{
		if (locationCellIndex >= 0)
		{
			HexCell location = Grid.GetCell(locationCellIndex);
			transform.localPosition = location.Position;
			if (currentTravelLocationCellIndex >= 0)
			{
				HexCell currentTravelLocation =
					Grid.GetCell(currentTravelLocationCellIndex);
				Grid.IncreaseVisibility(location, VisionRange);
				Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
				currentTravelLocationCellIndex = -1;
			}
		}
	}
}
