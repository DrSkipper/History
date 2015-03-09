using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BeaconsWorldGenerator : WorldGenerator
{
	public int areaTypes = 5;
	public int overlayTypes = 1;

	public float areaInitialConversionRate = 0.65f;
	public int areaStepIterations = 30;
	public int areaDeathLimit = 7;
	public int areaBirthLimit = 4;

	public float overlayInitialConversionRate = 0.5f;
	public int overlayStepIterations = 20;
	public int overlayDeathLimit = 7;
	public int overlayBirthLimit = 4;

	private const int NUM_STAGES = 5;
	private delegate void RunStageDelegate(int stageIndex, int frames);

	void Start() { }
	void Update() { }
	
	public override void runGenerationFrames(int frames)
	{
		if (_stagesCompleted[_currentStage])
			++_currentStage;

		if (_currentStage >= NUM_STAGES)
		{
			this.generationComplete = true;
		}
		else
		{
			_stageDelegates[_currentStage](_currentStage, frames);
			if (_stagesCompleted[_currentStage])
			{
				++_currentStage;

				if (_currentStage >= NUM_STAGES)
					this.generationComplete = true;
			}
		}
		
		if (this.updateDelegate != null)
		{
			this.updateDelegate();
		}
	}

	public override void clearMap()
	{
		base.clearMap();
		_islands = null;
		_islandBoundryTiles = null;
		_possibleIslandTiles = null;
		_stagesCompleted = new bool[NUM_STAGES];
		_framesRunByStage = new int[NUM_STAGES];
		_currentStage = 0;

		if (_stageDelegates == null)
		{
			_stageDelegates = new RunStageDelegate[] {
				randomConversionStage, 
				areaCellularAutomataStage, 
				islandAssignmentStage, 
				islandExpansionStage, 
				overlayCellularAutomataStage
			};
		}
	}

	/**
	 * Private
	 */
	private List<List<WorldGenTile>> _islands;
	private List<List<WorldGenTile>> _islandBoundryTiles;
	private List<WorldGenTile> _possibleIslandTiles;
	private bool[] _stagesCompleted;
	private int[] _framesRunByStage;
	private int _currentStage;
	private RunStageDelegate[] _stageDelegates;

	private void randomConversionStage(int stageIndex, int frames)
	{
		map.randomlyConvertTiles(WorldGenMap.TILE_TYPE_DEFAULT, WorldGenerator.TILE_TYPE_A, this.areaInitialConversionRate);
		_framesRunByStage[stageIndex] += 1;
		_stagesCompleted[stageIndex] = true;
	}

	// Returns frames run
	private void areaCellularAutomataStage(int stageIndex, int frames)
	{
		int startingIteration = _framesRunByStage[stageIndex];
		int iterations = startingIteration + frames;

		if (iterations >= this.areaStepIterations)
		{
			iterations = areaStepIterations;
			_stagesCompleted[stageIndex] = true;
		}
		
		for (int i = startingIteration; i < iterations; ++i)
			map.runAutomataStep(WorldGenMap.TILE_TYPE_DEFAULT, WorldGenerator.TILE_TYPE_A, this.areaDeathLimit, this.areaBirthLimit, true, false);

		_framesRunByStage[stageIndex] = iterations;
	}

	// Returns frames run
	private void islandAssignmentStage(int stageIndex, int frames)
	{
		if (_islands == null)
			_islands = new List<List<WorldGenTile>>();

		if (_possibleIslandTiles == null)
			_possibleIslandTiles = this.map.allTilesMatchingType(TILE_TYPE_A);

		uint[] types = new uint[] {TILE_TYPE_F, TILE_TYPE_E, TILE_TYPE_D, TILE_TYPE_C, TILE_TYPE_B, TILE_TYPE_A};

		if (_islands.Count >= areaTypes || _islands.Count >= types.Length || _possibleIslandTiles.Count == 0)
		{
			// If we're already done, mark stage complete
			_stagesCompleted[stageIndex] = true;
		}
		else
		{
			for (int i = 0; i < frames; ++i)
			{
				uint nextIslandType = types[_islands.Count];
				WorldGenTile islandRootTile = _possibleIslandTiles[Random.Range(0, _possibleIslandTiles.Count - 1)];

				// Fill an island
				List<WorldGenTile> island = floodFill(islandRootTile, true);

				//Debug.Log("type = " + nextIslandType + ", root type = " + islandRootTile.type + ", island size = " + island.Count);

				// Remove filled tiles from possible island roots, and set the new type
				foreach (WorldGenTile tile in island)
				{
					_possibleIslandTiles.Remove(tile);
					tile.type = nextIslandType;
				}

				// Store the island
				_islands.Add(island);

				// Check if we're done
				if (_islands.Count >= areaTypes || _islands.Count >= types.Length || _possibleIslandTiles.Count == 0)
				{
					_stagesCompleted[stageIndex] = true;
					break;
				}
			}
		}

        //if (_stagesCompleted[stageIndex])
        //    this.map.printMap();

		// Once the stage is done, eliminate any remaining islands
		if (_stagesCompleted[stageIndex])
		{
			foreach (WorldGenTile tile in _possibleIslandTiles)
				tile.type = WorldGenMap.TILE_TYPE_DEFAULT;

			_possibleIslandTiles = null;
		}

		//TODO - fcole - If num islands < areaTypes / 2 + 1 then clear map and set us back to stage 1 (or use some other "good enough" tolerance)
	}
	
	private void islandExpansionStage(int stageIndex, int frames)
	{
		if (_islandBoundryTiles == null)
		{
			_islandBoundryTiles = new List<List<WorldGenTile>>();
			foreach (List<WorldGenTile> island in _islands)
			{
				_islandBoundryTiles.Add(new List<WorldGenTile>(island));
			}
		}

		for (int i = 0; i < frames; ++i)
		{
			bool allEmpty = true;
			foreach (List<WorldGenTile> boundryTiles in _islandBoundryTiles)
			{
				List<WorldGenTile> boundryTilesCopy = new List<WorldGenTile>(boundryTiles);
				foreach (WorldGenTile tile in boundryTilesCopy)
				{
					expandCell(tile, boundryTiles);
				}

				if (boundryTiles.Count > 0)
					allEmpty = false;
			}

			if (allEmpty)
			{
				_stagesCompleted[stageIndex] = true;
				break;
			}
		}

		
	}
	
	private void overlayCellularAutomataStage(int stageIndex, int frames)
	{
		_stagesCompleted[stageIndex] = true;
	}
	
	/*
	 * Returns list of tiles with same time as rootTile within reach of flood fill traversal
	 * (returns an "island" that includes rootTile)
	 * 
	 * http://en.wikipedia.org/wiki/Flood_fill
	 * 
	 * Flood-fill (node, target-color, replacement-color):
	 * 1. If target-color is equal to replacement-color, return.
	 * 2. Set Q to the empty queue.
	 * 3. Add node to the end of Q.
	 * 4. While Q is not empty: 
	 * 5.     Set n equal to the last element of Q.
	 * 6.     Remove last element from Q.
	 * 7.     If the color of n is equal to target-color:
	 * 8.         Set the color of n to replacement-color and mark "n" as processed.
	 * 9.         Add west node to end of Q if west has not been processed yet.
	 * 10.        Add east node to end of Q if east has not been processed yet.
	 * 11.        Add north node to end of Q if north has not been processed yet.
	 * 12.        Add south node to end of Q if south has not been processed yet.
	 * 13. Return.
 	 */
	private List<WorldGenTile> floodFill(WorldGenTile rootTile, bool allowWrapping)
	{
		this.map.clearProcessedFlags();

		List<WorldGenTile> reachedTiles = new List<WorldGenTile>();
		List<WorldGenTile> stack = new List<WorldGenTile>();
		uint checkType = rootTile.type;
		processIntoStack(stack, rootTile);

		while (stack.Count > 0)
		{
			WorldGenTile currentTile = stack[0];
			stack.RemoveAt(0);

			//if ((currentTile.type & checkType) != 0x000000) // More lenient/inclusive check
			if (currentTile.type == checkType) // Exact equality
			{
				reachedTiles.Add(currentTile);
				processIntoStack(stack, this.map.tileAtLocation(currentTile.x - 1, currentTile.y, allowWrapping));
				processIntoStack(stack, this.map.tileAtLocation(currentTile.x + 1, currentTile.y, allowWrapping));
				processIntoStack(stack, this.map.tileAtLocation(currentTile.x, currentTile.y - 1, allowWrapping));
				processIntoStack(stack, this.map.tileAtLocation(currentTile.x, currentTile.y + 1, allowWrapping));
			}
		}

		return reachedTiles;
	}

	private void processIntoStack(List<WorldGenTile> stack, WorldGenTile tile)
	{
		if (tile != null && !tile.processed)
		{
			stack.Insert(0, tile);
			tile.processed = true;
		}
	}

	private void expandCell(WorldGenTile tile, List<WorldGenTile> container)
	{
		WorldGenTile up = this.map.tileAtLocation(tile.x, tile.y - 1, true);
		WorldGenTile down = this.map.tileAtLocation(tile.x, tile.y + 1, true);
		WorldGenTile left = this.map.tileAtLocation(tile.x - 1, tile.y, true);
		WorldGenTile right = this.map.tileAtLocation(tile.x + 1, tile.y, true);

		bool upGood = true;
		bool downGood = true;
		bool leftGood = true;
		bool rightGood = true;

		if (up.type == WorldGenMap.TILE_TYPE_DEFAULT)
		{
			if (Random.Range(0, 2) == 0)
			{
				up.type = tile.type;
				container.Add(up);
			}
			else
			{
				upGood = false;
			}
		}
		
		if (down.type == WorldGenMap.TILE_TYPE_DEFAULT)
		{
			if (Random.Range(0, 2) == 0)
			{
				down.type = tile.type;
				container.Add(down);
			}
			else
			{
				downGood = false;
			}
		}
		
		if (left.type == WorldGenMap.TILE_TYPE_DEFAULT)
		{
			if (Random.Range(0, 2) == 0)
			{
				left.type = tile.type;
				container.Add(left);
			}
			else
			{
				leftGood = false;
			}
		}
		
		if (right.type == WorldGenMap.TILE_TYPE_DEFAULT)
		{
			if (Random.Range(0, 2) == 0)
			{
				right.type = tile.type;
				container.Add(right);
			}
			else
			{
				rightGood = false;
			}
		}

		if (upGood && downGood && leftGood && rightGood)
			container.Remove(tile);
	}
}
