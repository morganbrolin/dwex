using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Component that manages the map feature visualizations for a hex grid chunk.
/// </summary>
public class HexFeatureManager : MonoBehaviour
{
	[System.Serializable]
	public struct HexFeatureCollection
	{
		public Transform[] prefabs;

		public readonly Transform Pick(float choice) =>
			prefabs[(int)(choice * prefabs.Length)];
	}

	[SerializeField]
	HexFeatureCollection[] enemyQuantityCollections;

	[SerializeField]
	HexFeatureCollection[] gemQualityCollections;

	[SerializeField]
	HexFeatureCollection[] enemyQualityCollections;

	[SerializeField]
	HexMesh walls;

	[SerializeField]
	Transform wallTower, bridge;
	
	Transform container;

	/// <summary>
	/// Clear all features.
	/// </summary>
	public void Clear()
	{
		if (container)
		{
			Destroy(container.gameObject);
		}
		container = new GameObject("Features Container").transform;
		container.SetParent(transform, false);
		walls.Clear();
	}

	/// <summary>
	/// Apply triangulation.
	/// </summary>
	public void Apply() => walls.Apply();

	Transform PickPrefab(
		HexFeatureCollection[] collection, int level, float hash, float choice)
	{
		if (level > 0)
		{
			float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
			for (int i = 0; i < thresholds.Length; i++)
			{
				if (hash < thresholds[i])
				{
					Debug.Log($"Level: {level}, Thresholds: {thresholds.Length}, Collection: {collection.Length}");
					return collection[i].Pick(choice);
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Add a bridge between two road centers.
	/// </summary>
	/// <param name="roadCenter1">Center position of first road.</param>
	/// <param name="roadCenter2">Center position of second road.</param>
	public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2)
	{
		roadCenter1 = HexMetrics.Perturb(roadCenter1);
		roadCenter2 = HexMetrics.Perturb(roadCenter2);
		Transform instance = Instantiate(bridge);
		instance.localPosition = (roadCenter1 + roadCenter2) * 0.5f;
		instance.forward = roadCenter2 - roadCenter1;
		float length = Vector3.Distance(roadCenter1, roadCenter2);
		instance.localScale = new Vector3(
			1f,	1f, length * (1f / HexMetrics.bridgeDesignLength));
		instance.SetParent(container, false);
	}

	/// <summary>
	/// Add a feature for a cell.
	/// </summary>
	/// <param name="cell">Cell with one or more features.</param>
	/// <param name="position">Feature position.</param>
	public void AddFeature(HexCellData cell, Vector3 position)
	{

		HexHash hash = HexMetrics.SampleHashGrid(position);
		Transform prefab = PickPrefab(
			enemyQuantityCollections, cell.EnemyQuantityLevel, hash.a, hash.d);
		Transform otherPrefab = PickPrefab(
			gemQualityCollections, cell.GemQualityLevel, hash.b, hash.d);
		float usedHash = hash.a;
		if (prefab)
		{
			if (otherPrefab && hash.b < hash.a)
			{
				prefab = otherPrefab;
				usedHash = hash.b;
			}
		}
		else if (otherPrefab)
		{
			prefab = otherPrefab;
			usedHash = hash.b;
		}
		otherPrefab = PickPrefab(
			enemyQualityCollections, cell.EnemyQualityLevel, hash.c, hash.d);
		if (prefab)
		{
			if (otherPrefab && hash.c < usedHash)
			{
				prefab = otherPrefab;
			}
		}
		else if (otherPrefab)
		{
			prefab = otherPrefab;
		}
		else
		{
			return;
		}

		Transform instance = Instantiate(prefab);
		position.y += instance.localScale.y * 0.5f;
		instance.SetLocalPositionAndRotation(
			HexMetrics.Perturb(position),
			Quaternion.Euler(0f, 360f * hash.e, 0f));
		instance.SetParent(container, false);
	}




}
