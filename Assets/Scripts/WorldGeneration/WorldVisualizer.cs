using UnityEngine;
using System.Collections;

public class WorldVisualizer : VoBehavior
{
	public float updateStepLength = 0.2f;
	public int generationFramesPerUpdate = 2;
	public int initialFramesToRun = 0;
	public bool runOnStartup = false;
	public bool allowInterruption = false;

	// Use this for initialization
	void Start()
	{
		_generator = this.gameObject.GetComponent<WorldGenerator>();
		_generator.updateDelegate += this.mapWasUpdated;
		if (this.runOnStartup)
			this.restart();
	}
	
	// Update is called once per frame
	void Update()
	{
		if (_running)
		{
			if (_animatingLastUpdate)
			{
				updateAnimation();
			}
			else if (_mapWasUpdated)
			{
				_mapWasUpdated = false;
				startAnimation();
			}
			else if (!_generationComplete)
			{
				if (!_initialFramesRun)
				{
					_initialFramesRun = true;
					if (this.initialFramesToRun > 0)
					{
						for (int i = 0; i < this.initialFramesToRun; ++i)
							_generator.runGenerationFrames(1);
					}
					else
					{
						_generator.runGenerationFrames(this.generationFramesPerUpdate);
					}
				}
				else
				{
					_generator.runGenerationFrames(this.generationFramesPerUpdate);
				}
			}
		}
		else if (!this.allowInterruption && Input.GetKeyUp(KeyCode.Space))
		{
			this.restart();
		}

		if (this.allowInterruption && Input.GetKeyUp(KeyCode.Space))
			this.restart();
	}

	public void mapWasUpdated()
	{
		_mapWasUpdated = true;
		if (_generator.generationComplete)
		{
			_generationComplete = true;
			_running = false;
		}
	}

	public void animationTimerCallback()
	{
		_animationTimer.paused = true;
		_animatingLastUpdate = false;
	}

	public void restart()
	{
		_generator.clearMap();
		setupMapDisplay();
		if (_animationTimer != null)
			_animationTimer.invalidate();
		_animationTimer = new Timer(this.updateStepLength, true, false, this.animationTimerCallback);
		_animatingLastUpdate = false;
		_mapWasUpdated = false;
		_generationComplete = false;
		_running = true;
		_initialFramesRun = false;
	}

	/**
	 * Private
	 */
	private bool _running;
	private bool _mapWasUpdated;
	private bool _animatingLastUpdate;
	private bool _generationComplete;
	private bool _initialFramesRun;
	private WorldGenerator _generator;

	private Sprite _defaultSprite;
	private Sprite _invalidSprite;
	private Sprite _spriteA;
	private Sprite _spriteB;
	private Sprite _spriteC;
	private Sprite _spriteD;
	private Sprite _spriteE;
	private Sprite _spriteF;
	private SpriteRenderer[,] _tileRenderers;
	private Timer _animationTimer;

	private void startAnimation()
	{
		_animatingLastUpdate = true;
		_animationTimer.paused = false;

		WorldGenTile[,] map = _generator.map.map;
		
		int width = map.GetLength(0);
		int height = map.GetLength(1);
		
		for (int x = 0; x < width; ++x)
		{
			for (int y = 0; y < height; ++y)
			{
				_tileRenderers[x, y].sprite = spriteForType(map[x, y].type);
			}
		}
	}

	private void updateAnimation()
	{
		_animationTimer.update();
	}

	private void setupMapDisplay()
	{
		if (_tileRenderers != null)
		{
			for (int x = 0; x < _tileRenderers.GetLength(0); ++x)
			{
				for (int y = 0; y < _tileRenderers.GetLength(1); ++y)
				{
					Destroy(_tileRenderers[x, y].gameObject);
				}
			}
		}

		WorldGenTile[,] map = _generator.map.map;
		
		_defaultSprite = Resources.Load<Sprite>("Sprites/blue_square");
		_invalidSprite = Resources.Load<Sprite>("Sprites/black_square");
		_spriteA = Resources.Load<Sprite>("Sprites/yellow_square");
		_spriteB = Resources.Load<Sprite>("Sprites/yellow_square");
		_spriteC = Resources.Load<Sprite>("Sprites/red_square");
		_spriteD = Resources.Load<Sprite>("Sprites/green_square");
		_spriteE = Resources.Load<Sprite>("Sprites/pink_square");
		_spriteF = Resources.Load<Sprite>("Sprites/white_square");
		
		int width = map.GetLength(0);
		int height = map.GetLength(1);
		int halfWidth = width / 2;
		int halfHeight = height / 2;

		_tileRenderers = new SpriteRenderer[width, height];
		
		for (int x = 0; x < width; ++x)
		{
			for (int y = 0; y < height; ++y)
			{
				GameObject go = new GameObject();
				SpriteRenderer sprite = go.AddComponent<SpriteRenderer>();
				sprite.sprite = spriteForType(map[x, y].type);
				Bounds bounds = sprite.bounds;
				go.transform.position = new Vector3((x - halfWidth) * bounds.size.x, (y - halfHeight) * bounds.size.y, 0);
				_tileRenderers[x, y] = sprite;
			}
		}
	}

	private Sprite spriteForType(uint type)
	{
		if (type == WorldGenMap.TILE_TYPE_DEFAULT)	return _defaultSprite;
		if (type == WorldGenerator.TILE_TYPE_A)		return _spriteA;
		if (type == WorldGenerator.TILE_TYPE_B)		return _spriteB;
		if (type == WorldGenerator.TILE_TYPE_C)		return _spriteC;
		if (type == WorldGenerator.TILE_TYPE_D)		return _spriteD;
		if (type == WorldGenerator.TILE_TYPE_E)		return _spriteE;
		if (type == WorldGenerator.TILE_TYPE_F)		return _spriteF;
		return _invalidSprite;
	}
}
