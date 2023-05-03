/// <summary>
/// Flags that describe the contents of a cell. Initially only contains roads data.
/// </summary>
[System.Flags]
public enum HexFlags
{
	Empty = 0,

	RoadNE = 0b0000_0001,
	RoadE  = 0b0000_0010,
	RoadSE = 0b0000_0100,
	RoadSW = 0b0000_1000,
	RoadW  = 0b0001_0000,
	RoadNW = 0b0010_0000,

	Roads = 0b0011_1111
}

public static class HexFlagsExtensions
{
	/// <summary>
	/// Whether any flags of a mask are set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="mask">Mask.</param>
	/// <returns>Whether any of the flags are set.</returns>
	public static bool HasAny (this HexFlags flags, HexFlags mask) => (flags & mask) != 0;

	/// <summary>
	/// Whether the flag for a road in a given direction is set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="direction">Road direction.</param>
	/// <returns>Whether the road is set.</returns>
	public static bool HasRoad (this HexFlags flags, HexDirection direction) =>
		((int)flags & (1 << (int)direction)) != 0;

	/// <summary>
	/// Returns the flags with the bit for a given road set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="direction">Road direction.</param>
	/// <returns>Flags with the road bit set.</returns>
	public static HexFlags WithRoad (this HexFlags flags, HexDirection direction) =>
		flags | (HexFlags)(1 << (int)direction);

	/// <summary>
	/// Returns the flags without the bit for a given road set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="direction">Road direction.</param>
	/// <returns>Flags without the road bit set.</returns>
	public static HexFlags WithoutRoad (this HexFlags flags, HexDirection direction) =>
		flags & ~(HexFlags)(1 << (int)direction);
}
