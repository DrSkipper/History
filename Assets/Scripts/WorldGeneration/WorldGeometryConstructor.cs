using UnityEngine;
using System.Collections;

public class WorldGeometryConstructor : VoBehavior
{
	public GameObject planePrefabA;
	public GameObject planePrefabB;

	// Use this for initialization
	void Start()
	{
		_generator = this.gameObject.GetComponent<WorldGenerator>();
		_generator.clearMap();
		_generator.generateEntireMap();
		setupMapDisplay();
	}
	
	// Update is called once per frame
	void Update()
	{
	}
	
	/**
	 * Private
	 */
	private WorldGenerator _generator;
	private GameObject[,] _tileObjects;
	
	private void setupMapDisplay()
	{
		WorldGenTile[,] map = _generator.map.map;
		
		int width = map.GetLength(0);
		int height = map.GetLength(1);
		int halfWidth = width / 2;
		int halfHeight = height / 2;
		Bounds tileBounds = this.planePrefabA.GetComponent<Renderer>().bounds;
		
		_tileObjects = new GameObject[width, height];
		
		for (int x = 0; x < width; ++x)
		{
			for (int y = 0; y < height; ++y)
			{
				Vector3 position = new Vector3((x - halfWidth) * tileBounds.size.x, 0, (y - halfHeight) * tileBounds.size.z);
				_tileObjects[x, y] = Instantiate(objectForType(map[x, y].type), position, Quaternion.identity) as GameObject;
			}
		}
	}
	
	private GameObject objectForType(uint type)
	{
		if (type == WorldGenerator.TILE_TYPE_A)
		{
			return this.planePrefabA;
		}
		else
		{
			return this.planePrefabB;
		}
	}
}
