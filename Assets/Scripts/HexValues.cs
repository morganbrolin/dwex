using System.IO;
using UnityEngine;

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

	readonly int Get(int mask, int shift) =>
		(int)((uint)values >> shift) & mask;

	readonly HexValues With(int value, int mask, int shift) => new()
	{
		values = (values & ~(mask << shift)) | ((value & mask) << shift)
	};

	public readonly int Elevation => Get(31, 0) - 15;

	public readonly HexValues WithElevation(int value) =>
		With(value + 15, 31, 0);
	

	public readonly int ViewElevation => Mathf.Max(Elevation);
	
	public readonly int EnemyQuantityLevel => Get(3, 10);

	public readonly HexValues WithEnemyQuantityLevel(int value) => With(value, 3, 10);

	public readonly int GemQualityLevel => Get(3, 12);

	public readonly HexValues WithGemQualityLevel(int value) => With(value, 3, 12);

	public readonly int EnemyQualityLevel => Get(3, 14);

	public readonly HexValues WithEnemyQualityLevel(int value) => With(value, 3, 14);
	
	
	public readonly int TerrainTypeIndex => Get(255, 24);
	
	public readonly HexValues WithTerrainTypeIndex(int index) =>
		With(index, 255, 24);

	/// <summary>
	/// Save the values.
	/// </summary>
	/// <param name="writer"><see cref="BinaryWriter"/> to use.</param>
	public readonly void Save(BinaryWriter writer)
	{
		writer.Write((byte)TerrainTypeIndex);
		writer.Write((byte)(Elevation + 127));
		writer.Write((byte)EnemyQuantityLevel);
		writer.Write((byte)GemQualityLevel);
		writer.Write((byte)EnemyQualityLevel);
	}

	/// <summary>
	/// Load the values.
	/// </summary>
	/// <param name="reader"><see cref="BinaryReader"/> to use.</param>
	/// <param name="header">Header version.</param>
	public static HexValues Load(BinaryReader reader, int header)
	{
		HexValues values = default;
		values = values.WithTerrainTypeIndex(reader.ReadByte());
		int elevation = reader.ReadByte();
		if (header >= 4)
		{
			elevation -= 127;
		}
		values = values.WithElevation(elevation);
		values = values.WithEnemyQuantityLevel(reader.ReadByte());
		values = values.WithGemQualityLevel(reader.ReadByte());
		return values.WithEnemyQualityLevel(reader.ReadByte());
	}
}
