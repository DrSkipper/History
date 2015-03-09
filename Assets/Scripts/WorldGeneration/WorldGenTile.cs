using UnityEngine;
using System.Collections;

public class WorldGenTile
{
	public int x;
	public int y;
	public uint type;

	public bool processed; // To be used by various graph algorithms, which are responsible for ensuring initial state of this property

	public WorldGenTile(int x, int y, uint type)
	{
		this.x = x;
		this.y = y;
		this.type = type;
	}
}
