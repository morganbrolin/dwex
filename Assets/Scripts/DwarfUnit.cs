using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A specialized unit controlled by the player capable of mining hex walls.
/// </summary>
public class DwarfUnit : HexUnit
{
	/// <summary>
	/// A wall cell that the dwarf intends to mine upon arrival.
	/// </summary>
	public HexCell PendingMineTarget { get; set; }

	[SerializeField]
	float miningSpeed = 1f;
	

	void Update()
	{
		if (PendingMineTarget)
		{

			// Only perform the mining action if adjacent and not moving
			if (!IsTraveling && Location.Coordinates.DistanceTo(PendingMineTarget.Coordinates) == 1)
			{
				MineTick(PendingMineTarget);
			}
		}
	}

	/// <summary>
	/// Applies mining damage to a target cell over time.
	/// </summary>
	/// <param name="target">The cell to mine.</param>
	public void MineTick(HexCell target)
	{
		if (target && target.Values.Elevation >= 5)
		{
			float currentHealth = target.CurrentHealth;
			currentHealth -= miningSpeed * Time.deltaTime;
			target.hpSlider.enabled = true;
			target.hpSlider.gameObject.SetActive(true);
			
			target.hpSlider.value = currentHealth / target.MaxHealth;
			Debug.Log("sliders");
			Debug.Log(target.hpSlider.value);
			Debug.Log(target.hpSlider.maxValue);
			Debug.Log(target.hpSlider.minValue);
			if (currentHealth <= 0f)
			{
				// Award gems before clearing the wall
				//int reward = Grid.GetGemReward(target);
				//Grid.Gems += reward;
				//Debug.Log($"Mined {reward} gems! Total: {Grid.Gems}");

				target.SetElevation(0);
				Grid.RefreshCellWithDependents(target.Index);
				PendingMineTarget = default;
				target.hpSlider.enabled = false;
				target.hpSlider.gameObject.SetActive(false);
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
}