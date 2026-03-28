using System.IO;

/// <summary>
/// Flags that describe the contents of a cell.
/// </summary>
[System.Flags]
public enum HexFlags
{

	Explored   = 0b010_000000_000000_000000,
	Explorable = 0b100_000000_000000_000000
}

public static class HexFlagsExtensions
{
	/// <summary>
	/// Whether any flags of a mask are set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="mask">Mask.</param>
	/// <returns>Whether any of the flags are set.</returns>
	public static bool HasAny(this HexFlags flags, HexFlags mask) =>
		(flags & mask) != 0;

	/// <summary>
	/// Whether all flags of a mask are set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="mask">Mask.</param>
	/// <returns>Whether all of the flags are set.</returns>
	public static bool HasAll(this HexFlags flags, HexFlags mask) =>
		(flags & mask) == mask;

	/// <summary>
	/// Whether no flags of a mask are set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="mask">Mask.</param>
	/// <returns>Whether none of the flags are set.</returns>
	public static bool HasNone(this HexFlags flags, HexFlags mask) =>
		(flags & mask) == 0;

	/// <summary>
	/// Returns flags with bits of the given mask set.
	/// </summary>
	/// <param name="flags"><Flags./param>
	/// <param name="mask">Mask to set.</param>
	/// <returns>The set flags.</returns>
	public static HexFlags With(this HexFlags flags, HexFlags mask) =>
		flags | mask;

	/// <summary>
	/// Returns flags with bits of the given mask cleared.
	/// </summary>
	/// <param name="flags"><Flags./param>
	/// <param name="mask">Mask to clear.</param>
	/// <returns>The cleared flags.</returns>
	public static HexFlags Without(this HexFlags flags, HexFlags mask) =>
		flags & ~mask;




	/// <summary>
	/// Save the flags.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="writer"><see cref="BinaryWriter"/> to use.</param>
	public static void Save(this HexFlags flags, BinaryWriter writer)
	{
		writer.Write((byte)0);
		writer.Write(flags.HasAll(HexFlags.Explored | HexFlags.Explorable));
	}

		/// <summary>
	/// Load the cell data.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="reader"><see cref="BinaryReader"/> to use.</param>
	/// <param name="header">Header version.</param>
	public static HexFlags Load(
		this HexFlags basis, BinaryReader reader, int header)
	{
		HexFlags flags = basis & HexFlags.Explorable;
		flags |= (HexFlags)reader.ReadByte();

		if (header >= 3 && reader.ReadBoolean())
		{
			flags = flags.With(HexFlags.Explored);
		}
		return flags;
	}
}
