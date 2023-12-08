/// <summary>
/// Values that describe the contents of a cell.
/// </summary>
[System.Serializable]
public struct HexValues
{
	/// <summary>
	/// Seven values stored in 32 bits.
	/// TTTTTTTT SSSSSSSS PPFFUUWW WWWEEEEE.
	/// </summary>
	/// <remarks>Not readonly to support hot reloading in Unity.</remarks>
#pragma warning disable IDE0044 // Add readonly modifier
	int values;
#pragma warning restore IDE0044 // Add readonly modifier

	HexValues(int v) => values = v;
	
	readonly HexValues With(int value, int mask, int shift) =>
		new((values & ~(mask << shift)) | ((value & mask) << shift));

	readonly int Get(int mask, int shift) =>
		(int)((uint)values >> shift) & mask;

	public readonly int Elevation => Get(31, 0) - 15;

	public readonly HexValues WithElevation(int value) =>
		With(value + 15, 31, 0);

	public readonly int WaterLevel => Get(31, 5);

	public readonly HexValues WithWaterLevel(int value) => With(value, 31, 5);
	
	public readonly int UrbanLevel => Get(3, 10);

	public readonly HexValues WithUrbanLevel(int value) => With(value, 3, 10);

	public readonly int FarmLevel => Get(3, 12);

	public readonly HexValues WithFarmLevel(int value) => With(value, 3, 12);

	public readonly int PlantLevel => Get(3, 14);

	public readonly HexValues WithPlantLevel(int value) => With(value, 3, 14);

	public readonly int SpecialIndex => Get(255, 16);

	public readonly HexValues WithSpecialIndex(int index) =>
		With(index, 255, 16);
	
	public readonly int TerrainTypeIndex => Get(255, 24);
	
	public readonly HexValues WithTerrainTypeIndex(int index) =>
		With(index, 255, 24);
}
