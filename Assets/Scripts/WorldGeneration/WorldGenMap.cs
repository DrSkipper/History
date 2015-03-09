using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//NOTE - fcole - Cellular Automata level generation code heavily inspired by this link:
// http://gamedev.tutsplus.com/tutorials/implementation/cave-levels-cellular-automata/
public class WorldGenMap
{
	public const uint TILE_TYPE_DEFAULT = 0x000001;
	public const uint TILE_TYPE_INVALID = 0xFFFFFF;

	public WorldGenTile[,] map;

	private int _sizeX;
	private int _sizeY;

	public WorldGenMap(int sizeX, int sizeY, uint defaultType = TILE_TYPE_DEFAULT)
	{
		_sizeX = sizeX;
		_sizeY = sizeY;
		this.map = createEmptyMap(defaultType);
	}

	public WorldGenTile tileAtLocation(int x, int y, bool allowWrapping = false)
	{
		if (x >= 0 && x < _sizeX && 
		    y >= 0 && y < _sizeY)
			return map[x, y];

		if (allowWrapping)
		{
			if 		(x < 0) 	  x += _sizeX;
			else if (x >= _sizeX) x -= _sizeX;
			if 		(y < 0) 	  y += _sizeY;
			else if (y >= _sizeY) y -= _sizeY;

			return this.tileAtLocation(x, y, false);
		}

		return new WorldGenTile(x, y, TILE_TYPE_INVALID);
	}

	//TODO - fcole - Change from chance to have guaranteed percentage of tiles to convert, maybe with leeway parameter.
	//   Will probably require changing to array of tile objects, getting lists of them and shuffling the lists.
	public void randomlyConvertTiles(uint validBaseTypesMask, uint typeToConvertTo, float chance)
	{
		for (int x = 0; x < _sizeX; ++x)
		{
			for (int y = 0; y < _sizeY; ++y)
			{
				// If the type at this location matches a valid type, run a die roll to convert it
				if ((map[x, y].type | validBaseTypesMask) == validBaseTypesMask && Random.value <= chance)
					map[x, y].type = typeToConvertTo;
			}
		}
	}

	public void runAutomataStep(uint baseType, uint checkType, int limitForTileDeath, int limitForTileBirth, bool allowWrapping = true, bool countInvalids = false)
	{
		WorldGenTile[,] newMap = createEmptyMap();

		for (int x = 0; x < _sizeX; ++x)
		{
			for (int y = 0; y < _sizeY; ++y)
			{
				uint oldType = map[x, y].type;
				uint newType = oldType;
				
				bool matchesBaseType = (oldType | baseType) == baseType;
				bool matchesCheckType = (oldType | checkType) == checkType;

				if (matchesBaseType || matchesCheckType)
				{
					int count = neighborTypeCount(x, y, checkType, allowWrapping, countInvalids);

					if (matchesBaseType)
						newType = count < limitForTileDeath ? checkType : baseType;
					else // if (matchesCheckType)
						newType = count > limitForTileBirth ? baseType : checkType;
				}

				newMap[x, y].type = newType;
			}
		}

		this.map = newMap;
	}

	public List<WorldGenTile> allTilesMatchingType(uint type)
	{
		List<WorldGenTile> tiles = new List<WorldGenTile>();

		for (int x = 0; x < _sizeX; ++x)
		{
			for (int y = 0; y < _sizeY; ++y)
			{
				if ((map[x, y].type | type) == type)
					tiles.Add(map[x, y]);
			}
		}

		return tiles;
	}
	
	public void clearProcessedFlags()
	{
		for (int x = 0; x < _sizeX; ++x)
		for (int y = 0; y < _sizeY; ++y)
			map[x, y].processed = false;
	}

	public void printMap()
	{
		string logString = "\n";
		for (int x = 0; x < _sizeX; ++x)
		{
			logString += ".";
			for (int y = 0; y < _sizeY; ++y)
			{
				logString += "" + map[x, y].type.ToString().PadLeft(2, '0') + ".";
			}
			logString += "\n";
		}

		Debug.Log(logString);
	}

	/**
	 * Private
	 */
	private int neighborTypeCount(int centerX, int centerY, uint searchType, bool wrapMap = true, bool countInvalids = false)
	{
		int count = 0;
		for (int x = centerX - 1; x <= centerX + 1; ++x)
		{
			for (int y = centerY - 1; y <= centerY + 1; ++y)
			{
				// Skip middle
				if (x == centerX && y == centerY)
					continue;

				uint type = this.tileAtLocation(x, y, wrapMap).type;
				if (type == TILE_TYPE_INVALID)
				{
					if (countInvalids)
						++count;
				}
				else if ((type | searchType) == searchType)
				{
					++count;
				}
			}
		}
		return count;
	}

	private WorldGenTile[,] createEmptyMap(uint fillType = WorldGenMap.TILE_TYPE_DEFAULT)
	{
		WorldGenTile[,] newMap = new WorldGenTile[_sizeX, _sizeY];

		for (int x = 0; x < _sizeX; ++x)
		{
			for (int y = 0; y < _sizeY; ++y)
			{
				newMap[x, y] = new WorldGenTile(x, y, fillType);
			}
		}

		return newMap;
	}
}
