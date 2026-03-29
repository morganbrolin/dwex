using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component that generates hex maps.
/// </summary>
public class HexMapGenerator : MonoBehaviour
{
	[SerializeField]
	HexGrid grid;

	[SerializeField]
	bool useFixedSeed;

	[SerializeField]
	int seed;

	[SerializeField, Range(0f, 0.5f)]
	float jitterProbability = 0.25f;

	[SerializeField, Range(20, 200)]
	int chunkSizeMin = 30;

	[SerializeField, Range(20, 200)]
	int chunkSizeMax = 100;

	[SerializeField, Range(0f, 1f)]
	float highRiseProbability = 0.25f;

	[SerializeField, Range(0f, 0.4f)]
	float sinkProbability = 0.2f;

	[SerializeField, Range(5, 95)]
	int landPercentage = 50;
	
	[SerializeField, Range(-4, 0)]
	int elevationMinimum = -2;

	[SerializeField, Range(6, 10)]
	int elevationMaximum = 8;

	[SerializeField, Range(0, 10)]
	int mapBorderX = 5;

	[SerializeField, Range(0, 10)]
	int mapBorderZ = 5;

	[SerializeField, Range(0, 10)]
	int regionBorder = 5;

	[SerializeField, Range(1, 4)]
	int regionCount = 1;

	[SerializeField, Range(0, 100)]
	int erosionPercentage = 50;

	[SerializeField, Range(0f, 1f)]
	float startingMoisture = 0.1f;

	[SerializeField, Range(0f, 1f)]
	float evaporationFactor = 0.5f;

	[SerializeField, Range(0f, 1f)]
	float precipitationFactor = 0.25f;

	[SerializeField, Range(0f, 1f)]
	float runoffFactor = 0.25f;

	[SerializeField, Range(0f, 1f)]
	float seepageFactor = 0.125f;

	[SerializeField]
	HexDirection windDirection = HexDirection.NW;

	[SerializeField, Range(1f, 10f)]
	float windStrength = 4f;
	
	[SerializeField, Range(0f, 1f)]
	float lowTemperature = 0f;

	[SerializeField, Range(0f, 1f)]
	float highTemperature = 1f;

	public enum HemisphereMode
	{
		Both, North, South
	}

	[SerializeField]
	HemisphereMode hemisphere;

	[SerializeField, Range(0f, 1f)]
	float temperatureJitter = 0.1f;

	HexCellPriorityQueue searchFrontier;

	int searchFrontierPhase;

	int cellCount, landCells;

	int temperatureJitterChannel;

	struct MapRegion
	{
		public int xMin, xMax, zMin, zMax;
	}

	List<MapRegion> regions;

	struct ClimateData {
		public float clouds, moisture;
	}

	List<ClimateData> climate = new();
	List<ClimateData> nextClimate = new();

	List<HexDirection> flowDirections = new();

	struct Biome
	{
		public int terrain, enemyQuality;

		public Biome(int terrain, int enemyQuality)
		{
			this.terrain = terrain;
			this.enemyQuality = enemyQuality;
		}
	}

	static readonly float[] temperatureBands = { 0.1f, 0.3f, 0.6f };

	static readonly float[] moistureBands = { 0.12f, 0.28f, 0.85f };

	static readonly Biome[] biomes = {
		new(0, 0), new(4, 0), new(4, 0), new(4, 0),
		new(0, 0), new(2, 0), new(2, 1), new(2, 2),
		new(0, 0), new(1, 0), new(1, 1), new(1, 2),
		new(0, 0), new(1, 1), new(1, 2), new(1, 3)
	};

	/// <summary>
	/// Generate a random hex map.
	/// </summary>
	/// <param name="x">X size of the map.</param>
	/// <param name="z">Z size of the map.</param>
	public void GenerateMap(int x, int z)
	{
		Random.State originalRandomState = Random.state;
		if (!useFixedSeed)
		{
			seed = Random.Range(0, int.MaxValue);
			seed ^= (int)System.DateTime.Now.Ticks;
			seed ^= (int)Time.unscaledTime;
			seed &= int.MaxValue;
		}
		Random.InitState(seed);

		cellCount = x * z;
		grid.CreateMap(x, z);
		searchFrontier ??= new HexCellPriorityQueue(grid);

		CreateRegions();
		CreateLand();
		ErodeLand();
		CreateClimate();
		SetTerrainType();
		grid.RefreshAllCells();

		Random.state = originalRandomState;
	}

	void CreateRegions()
	{
		if (regions == null)
		{
			regions = new();
		}
		else
		{
			regions.Clear();
		}

		int borderX =  mapBorderX;
		MapRegion region;
		switch (regionCount)
		{
		default:
			region.xMin = borderX;
			region.xMax = grid.CellCountX - borderX;
			region.zMin = mapBorderZ;
			region.zMax = grid.CellCountZ - mapBorderZ;
			regions.Add(region);
			break;
		case 2:
			if (Random.value < 0.5f)
			{
				region.xMin = borderX;
				region.xMax = grid.CellCountX / 2 - regionBorder;
				region.zMin = mapBorderZ;
				region.zMax = grid.CellCountZ - mapBorderZ;
				regions.Add(region);
				region.xMin = grid.CellCountX / 2 + regionBorder;
				region.xMax = grid.CellCountX - borderX;
				regions.Add(region);
			}
			else
			{
				region.xMin = borderX;
				region.xMax = grid.CellCountX - borderX;
				region.zMin = mapBorderZ;
				region.zMax = grid.CellCountZ / 2 - regionBorder;
				regions.Add(region);
				region.zMin = grid.CellCountZ / 2 + regionBorder;
				region.zMax = grid.CellCountZ - mapBorderZ;
				regions.Add(region);
			}
			break;
		case 3:
			region.xMin = borderX;
			region.xMax = grid.CellCountX / 3 - regionBorder;
			region.zMin = mapBorderZ;
			region.zMax = grid.CellCountZ - mapBorderZ;
			regions.Add(region);
			region.xMin = grid.CellCountX / 3 + regionBorder;
			region.xMax = grid.CellCountX * 2 / 3 - regionBorder;
			regions.Add(region);
			region.xMin = grid.CellCountX * 2 / 3 + regionBorder;
			region.xMax = grid.CellCountX - borderX;
			regions.Add(region);
			break;
		case 4:
			region.xMin = borderX;
			region.xMax = grid.CellCountX / 2 - regionBorder;
			region.zMin = mapBorderZ;
			region.zMax = grid.CellCountZ / 2 - regionBorder;
			regions.Add(region);
			region.xMin = grid.CellCountX / 2 + regionBorder;
			region.xMax = grid.CellCountX - borderX;
			regions.Add(region);
			region.zMin = grid.CellCountZ / 2 + regionBorder;
			region.zMax = grid.CellCountZ - mapBorderZ;
			regions.Add(region);
			region.xMin = borderX;
			region.xMax = grid.CellCountX / 2 - regionBorder;
			regions.Add(region);
			break;
		}
	}

	void CreateLand()
	{
		int landBudget = Mathf.RoundToInt(cellCount * landPercentage * 0.01f);
		landCells = landBudget;
		for (int guard = 0; guard < 10000; guard++)
		{
			bool sink = Random.value < sinkProbability;
			for (int i = 0; i < regions.Count; i++)
			{
				MapRegion region = regions[i];
				int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);
					if (sink)
				{
					landBudget = SinkTerrain(chunkSize, landBudget, region);
				}
				else
				{
					landBudget = RaiseTerrain(chunkSize, landBudget, region);
					if (landBudget == 0)
					{
						return;
					}
				}
			}
		}
		if (landBudget > 0)
		{
			Debug.LogWarning(
				"Failed to use up " + landBudget + " land budget.");
			landCells -= landBudget;
		}
	}

	int RaiseTerrain(int chunkSize, int budget, MapRegion region)
	{
		searchFrontierPhase += 1;
		int firstCellIndex = GetRandomCellIndex(region);
		grid.SearchData[firstCellIndex] = new HexCellSearchData
		{
			searchPhase = searchFrontierPhase
		};
		searchFrontier.Enqueue(firstCellIndex);
		HexCoordinates center = grid.CellData[firstCellIndex].coordinates;

		int rise = Random.value < highRiseProbability ? 2 : 1;
		int size = 0;
		while (size < chunkSize && searchFrontier.TryDequeue(out int index))
		{
			HexCellData current = grid.CellData[index];
			int originalElevation = current.Elevation;
			int newElevation = originalElevation + rise;
			if (newElevation > elevationMaximum)
			{
				continue;
			}
			grid.CellData[index].values =
				current.values.WithElevation(newElevation);

			size += 1;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				if (grid.TryGetCellIndex(
					current.coordinates.Step(d), out int neighborIndex) &&
					grid.SearchData[neighborIndex].searchPhase <
						searchFrontierPhase)
				{
					grid.SearchData[neighborIndex] = new HexCellSearchData
					{
						searchPhase = searchFrontierPhase,
						distance = grid.CellData[neighborIndex].coordinates.
							DistanceTo(center),
						heuristic = Random.value < jitterProbability ? 1 : 0
					};
					searchFrontier.Enqueue(neighborIndex);
				}
			}
		}
		searchFrontier.Clear();
		return budget;
	}

	int SinkTerrain(int chunkSize, int budget, MapRegion region)
	{
		searchFrontierPhase += 1;
		int firstCellIndex = GetRandomCellIndex(region);
		grid.SearchData[firstCellIndex] = new HexCellSearchData
		{
			searchPhase = searchFrontierPhase
		};
		searchFrontier.Enqueue(firstCellIndex);
		HexCoordinates center = grid.CellData[firstCellIndex].coordinates;

		int sink = Random.value < highRiseProbability ? 2 : 1;
		int size = 0;
		while (size < chunkSize && searchFrontier.TryDequeue(out int index))
		{
			HexCellData current = grid.CellData[index];
			int originalElevation = current.Elevation;
			int newElevation = current.Elevation - sink;
			if (newElevation < elevationMinimum)
			{
				continue;
			}
			grid.CellData[index].values =
				current.values.WithElevation(newElevation);
			size += 1;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				if (grid.TryGetCellIndex(
					current.coordinates.Step(d), out int neighborIndex) &&
					grid.SearchData[neighborIndex].searchPhase <
						searchFrontierPhase)
				{
					grid.SearchData[neighborIndex] = new HexCellSearchData
					{
						searchPhase = searchFrontierPhase,
						distance = grid.CellData[neighborIndex].coordinates.
							DistanceTo(center),
						heuristic = Random.value < jitterProbability ? 1 : 0
					};
					searchFrontier.Enqueue(neighborIndex);
				}
			}
		}
		searchFrontier.Clear();
		return budget;
	}

	void ErodeLand()
	{
		List<int> erodibleIndices = ListPool<int>.Get();
		for (int i = 0; i < cellCount; i++)
		{
			if (IsErodible(i, grid.CellData[i].Elevation))
			{
				erodibleIndices.Add(i);
			}
		}

		int targetErodibleCount =
			(int)(erodibleIndices.Count * (100 - erosionPercentage) * 0.01f);
		
		while (erodibleIndices.Count > targetErodibleCount)
		{
			int index = Random.Range(0, erodibleIndices.Count);
			int cellIndex = erodibleIndices[index];
			HexCellData cell = grid.CellData[cellIndex];
			int targetCellIndex = GetErosionTarget(cellIndex, cell.Elevation);

			grid.CellData[cellIndex].values = cell.values =
				cell.values.WithElevation(cell.Elevation - 1);

			HexCellData targetCell = grid.CellData[targetCellIndex];
			grid.CellData[targetCellIndex].values = targetCell.values =
				targetCell.values.WithElevation(targetCell.Elevation + 1);

			if (!IsErodible(cellIndex, cell.Elevation))
			{
				int lastIndex = erodibleIndices.Count - 1;
				erodibleIndices[index] = erodibleIndices[lastIndex];
				erodibleIndices.RemoveAt(lastIndex);
			}

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				if (grid.TryGetCellIndex(
					cell.coordinates.Step(d), out int neighborIndex) &&
					grid.CellData[neighborIndex].Elevation ==
						cell.Elevation + 2 &&
					!erodibleIndices.Contains(neighborIndex))
				{
					erodibleIndices.Add(neighborIndex);
				}
			}

			if (IsErodible(targetCellIndex, targetCell.Elevation) &&
				!erodibleIndices.Contains(targetCellIndex))
			{
				erodibleIndices.Add(targetCellIndex);
			}

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				if (grid.TryGetCellIndex(
					targetCell.coordinates.Step(d), out int neighborIndex) &&
					neighborIndex != cellIndex &&
					grid.CellData[neighborIndex].Elevation ==
						targetCell.Elevation + 1 &&
					!IsErodible(
						neighborIndex, grid.CellData[neighborIndex].Elevation))
				{
					erodibleIndices.Remove(neighborIndex);
				}
			}
		}

		ListPool<int>.Add(erodibleIndices);
	}

	bool IsErodible(int cellIndex, int cellElevation)
	{
		int erodibleElevation = cellElevation - 2;
		HexCoordinates coordinates = grid.CellData[cellIndex].coordinates;
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
		{
			if (grid.TryGetCellIndex(
				coordinates.Step(d), out int neighborIndex) &&
				grid.CellData[neighborIndex].Elevation <= erodibleElevation)
			{
				return true;
			}
		}
		return false;
	}

	int GetErosionTarget (int cellIndex, int cellElevation)
	{
		List<int> candidates = ListPool<int>.Get();
		int erodibleElevation = cellElevation - 2;
		HexCoordinates coordinates = grid.CellData[cellIndex].coordinates;
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
		{
			if (grid.TryGetCellIndex(
				coordinates.Step(d), out int neighborIndex) &&
				grid.CellData[neighborIndex].Elevation <= erodibleElevation
			)
			{
				candidates.Add(neighborIndex);
			}
		}
		int target = candidates[Random.Range(0, candidates.Count)];
		ListPool<int>.Add(candidates);
		return target;
	}

	void CreateClimate()
	{
		climate.Clear();
		nextClimate.Clear();
		var initialData = new ClimateData
		{
			moisture = startingMoisture
		};
		var clearData = new ClimateData();
		for (int i = 0; i < cellCount; i++)
		{
			climate.Add(initialData);
			nextClimate.Add(clearData);
		}

		for (int cycle = 0; cycle < 40; cycle++)
		{
			for (int i = 0; i < cellCount; i++)
			{
				EvolveClimate(i);
			}
			(nextClimate, climate) = (climate, nextClimate);
		}
	}

	void EvolveClimate(int cellIndex)
	{
		HexCellData cell = grid.CellData[cellIndex];
		ClimateData cellClimate = climate[cellIndex];
		
		float evaporation = cellClimate.moisture * evaporationFactor;
		cellClimate.moisture -= evaporation;
		cellClimate.clouds += evaporation;
		

		float precipitation = cellClimate.clouds * precipitationFactor;
		cellClimate.clouds -= precipitation;
		cellClimate.moisture += precipitation;

		float cloudMaximum = 1f - cell.ViewElevation / (elevationMaximum + 1f);
		if (cellClimate.clouds > cloudMaximum)
		{
			cellClimate.moisture += cellClimate.clouds - cloudMaximum;
			cellClimate.clouds = cloudMaximum;
		}

		HexDirection mainDispersalDirection = windDirection.Opposite();
		float cloudDispersal = cellClimate.clouds * (1f / (5f + windStrength));
		float runoff = cellClimate.moisture * runoffFactor * (1f / 6f);
		float seepage = cellClimate.moisture * seepageFactor * (1f / 6f);
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
		{
			if (!grid.TryGetCellIndex(
				cell.coordinates.Step(d), out int neighborIndex))
			{
				continue;
			}
			ClimateData neighborClimate = nextClimate[neighborIndex];
			if (d == mainDispersalDirection)
			{
				neighborClimate.clouds += cloudDispersal * windStrength;
			}
			else
			{
				neighborClimate.clouds += cloudDispersal;
			}

			int elevationDelta = grid.CellData[neighborIndex].ViewElevation -
				cell.ViewElevation;
			if (elevationDelta < 0)
			{
				cellClimate.moisture -= runoff;
				neighborClimate.moisture += runoff;
			}
			else if (elevationDelta == 0)
			{
				cellClimate.moisture -= seepage;
				neighborClimate.moisture += seepage;
			}

			nextClimate[neighborIndex] = neighborClimate;
		}

		ClimateData nextCellClimate = nextClimate[cellIndex];
		nextCellClimate.moisture += cellClimate.moisture;
		if (nextCellClimate.moisture > 1f)
		{
			nextCellClimate.moisture = 1f;
		}
		nextClimate[cellIndex] = nextCellClimate;
		climate[cellIndex] = new ClimateData();
	}
	

	void SetTerrainType()
	{
		temperatureJitterChannel = Random.Range(0, 4);
		int rockDesertElevation =
			elevationMaximum;
		
		for (int i = 0; i < cellCount; i++)
		{
			HexCellData cell = grid.CellData[i];
			float temperature = DetermineTemperature(i, cell);
			int terrain;
			if (cell.Elevation < 0)
			{
				terrain = 3;
			}
			else
			{
				terrain = 2;
			}

			if (terrain == 1 && temperature < temperatureBands[0])
			{
				terrain = 2;
			}
			grid.CellData[i].values =
				cell.values.WithTerrainTypeIndex(terrain);
		
		}
	}

	float DetermineTemperature(int cellIndex, HexCellData cell)
	{
		float latitude = (float)cell.coordinates.Z /
			grid.CellCountZ;
		if (hemisphere == HemisphereMode.Both)
		{
			latitude *= 2f;
			if (latitude > 1f)
			{
				latitude = 2f - latitude;
			}
		}
		else if (hemisphere == HemisphereMode.North)
		{
			latitude = 1f - latitude;
		}

		float temperature =
			Mathf.LerpUnclamped(lowTemperature, highTemperature, latitude);

		float jitter = HexMetrics.SampleNoise(
			grid.CellPositions[cellIndex] * 0.1f)[temperatureJitterChannel];

		temperature += (jitter * 2f - 1f) * temperatureJitter;

		return temperature;
	}

	int GetRandomCellIndex (MapRegion region) => grid.GetCellIndex(
		Random.Range(region.xMin, region.xMax),
		Random.Range(region.zMin, region.zMax));
}
