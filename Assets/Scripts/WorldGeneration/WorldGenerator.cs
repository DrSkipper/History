using UnityEngine;
using System.Collections;

public class WorldGenerator : VoBehavior
{
	public const uint TILE_TYPE_A 	= 0x000002;
	public const uint TILE_TYPE_B 	= 0x000004;
	public const uint TILE_TYPE_C   = 0x000008;
	public const uint TILE_TYPE_D 	= 0x000010;
	public const uint TILE_TYPE_E 	= 0x000020;
	public const uint TILE_TYPE_F 	= 0x000040;
	
	public int mapSizeX = 50;
	public int mapSizeY = 50;
	
	public WorldGenMap map;

	public bool generationComplete { get; protected set; }

	public delegate void MapUpdateDelegate();
	public MapUpdateDelegate updateDelegate;

	public virtual void clearMap()
	{
		this.map = new WorldGenMap(mapSizeX, mapSizeY);
		this.generationComplete = false;
		_frames = 0;
	}

	public virtual void runGenerationFrames(int frames)
	{
		// Override in subclass
	}

	public virtual void generateEntireMap()
	{
		while (!this.generationComplete)
		{
			this.runGenerationFrames(4096);
		}
	}

	/**
	 * Protected
	 */
	protected int _frames;
}
