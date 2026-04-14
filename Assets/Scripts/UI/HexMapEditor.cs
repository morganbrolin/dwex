using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Component that applies UI commands to the hex map.
/// Public methods are hooked up to the in-game UI.
/// </summary>
public class HexMapEditor : MonoBehaviour
{
	static readonly int cellHighlightingId = Shader.PropertyToID(
		"_CellHighlighting");

	[SerializeField]
	HexGrid hexGrid;

	[SerializeField]
	HexGameUI gameUI;

	[SerializeField]
	NewMapMenu newMapMenu;

	[SerializeField]
	SaveLoadMenu saveLoadMenu;

	[SerializeField]
	Material terrainMaterial;

	[SerializeField]
	UIDocument sidePanels;
	
	bool applyWall;

	int activeEnemyQuantityLevel, activeGemQualityLevel, activeEnemyQualityLevel;

	int activeTerrainTypeIndex;

	int brushSize;
	
	bool applyEnemyQuantityLevel, applyGemQualityLevel, applyEnemyQualityLevel;
	
	HexCell previousCell;

	InputAction interactAction, positionAction;
	
	InputAction createUnitAction, destroyUnitAction;
	
	InputAction createHomeAction, destroyHomeAction;
	
	InputAction createEnemyAction, destroyEnemyAction;
	void Awake()
	{
		terrainMaterial.DisableKeyword("_SHOW_GRID");
		Shader.EnableKeyword("_HEX_MAP_EDIT_MODE");

		VisualElement root = sidePanels.rootVisualElement;

		root.Q<RadioButtonGroup>("Terrain").RegisterValueChangedCallback(
			change => activeTerrainTypeIndex = change.newValue - 1);
		
		root.Q<SliderInt>("BrushSize").RegisterValueChangedCallback(
			change => brushSize = change.newValue);

		root.Q<Toggle>("ApplyEnemyQuantityLevel").RegisterValueChangedCallback(
			change => applyEnemyQuantityLevel = change.newValue);
		root.Q<SliderInt>("EnemyQuantityLevel").RegisterValueChangedCallback(
			change => activeEnemyQuantityLevel = change.newValue);
		
		root.Q<Toggle>("ApplyGemQualityLevel").RegisterValueChangedCallback(
			change => applyGemQualityLevel = change.newValue);
		root.Q<SliderInt>("GemQualityLevel").RegisterValueChangedCallback(
			change => activeGemQualityLevel = change.newValue);
		
		root.Q<Toggle>("ApplyEnemyQualityLevel").RegisterValueChangedCallback(
			change => applyEnemyQualityLevel = change.newValue);
		root.Q<SliderInt>("EnemyQualityLevel").RegisterValueChangedCallback(
			change => activeEnemyQualityLevel = change.newValue);
		
		
		root.Q<Toggle>("ApplyWall").RegisterValueChangedCallback(
			change => applyWall = change.newValue);
		
		root.Q<Button>("SaveButton").clicked += () => saveLoadMenu.Open(true);
		root.Q<Button>("LoadButton").clicked += () => saveLoadMenu.Open(false);

		root.Q<Button>("NewMapButton").clicked += newMapMenu.Open;

		root.Q<Toggle>("Grid").RegisterValueChangedCallback(change => {
			if (change.newValue)
			{
				terrainMaterial.EnableKeyword("_SHOW_GRID");
			}
			else
			{
				terrainMaterial.DisableKeyword("_SHOW_GRID");
			}
		});

		root.Q<Toggle>("EditMode").RegisterValueChangedCallback(change => {
			enabled = change.newValue;
			gameUI.SetEditMode(change.newValue);
		});

		interactAction = InputSystem.actions.FindAction("Interact");
		positionAction = InputSystem.actions.FindAction("Position");
		createUnitAction = InputSystem.actions.FindAction("CreateUnit");
		destroyUnitAction = InputSystem.actions.FindAction("DestroyUnit");
		createHomeAction = InputSystem.actions.FindAction("CreateHome");
		destroyHomeAction = InputSystem.actions.FindAction("DestroyHome");
		createEnemyAction = InputSystem.actions.FindAction("CreateEnemy");
		destroyEnemyAction = InputSystem.actions.FindAction("DestroyEnemy"); 
    }

    void Update()
	{
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			if (interactAction.inProgress)
			{
				HandleInput();
				return;
			}
			else
			{
				// Potential optimization:
				// only do this if camera or cursor has changed.
				UpdateCellHighlightData(GetCellUnderCursor());
			}
			if (destroyUnitAction.WasPerformedThisFrame())
			{
				DestroyUnit();
				return;
			}
			if (createUnitAction.WasPerformedThisFrame())
			{
				CreateUnit();
				return;
			}
			if (createHomeAction.WasPerformedThisFrame())
			{
				CreateHome();
				return;
			}
			if (destroyHomeAction.WasPerformedThisFrame())
			{
				DestroyHome();
				return;
			}
			
			if (createEnemyAction.WasPerformedThisFrame())
			{
				CreateEnemy();
				return;
			}
			if (destroyEnemyAction.WasPerformedThisFrame())
			{
				DestroyEnemy();
				return;
			}
		}
		else
		{
			ClearCellHighlightData();
		}
		previousCell = default;
	}

	HexCell GetCellUnderCursor() => hexGrid.GetCell(
		Camera.main.ScreenPointToRay(positionAction.ReadValue<Vector2>()),
		previousCell);

	void CreateUnit()
	{
		HexCell cell = GetCellUnderCursor();
		if (cell && !cell.Unit && HexUnit.unitPrefab)
		{
			hexGrid.AddUnit(
				Instantiate(HexUnit.unitPrefab), cell, Random.Range(0f, 360f)
			);
		}
		else if (!HexUnit.unitPrefab)
		{
			Debug.LogError("Unit Prefab is missing on the HexGrid!");
		}
	}
	
	void CreateEnemy()
	{
		HexCell cell = GetCellUnderCursor();
		if (cell && !cell.Unit && HexUnit.enemyUnitPrefab)
		{
			hexGrid.AddUnit(
				Instantiate(HexUnit.enemyUnitPrefab), cell, Random.Range(0f, 360f)
			);
		}
		else if (!HexUnit.enemyUnitPrefab)
		{
			Debug.LogError("Unit Prefab is missing on the HexGrid!");
		}
	}
	
	void CreateHome()
	{
		HexCell cell = GetCellUnderCursor();
		if (cell)
		{
			hexGrid.CreateHome(cell.Index);
		}
	}
	
	void DestroyHome()
	{
		HexCell cell = GetCellUnderCursor();
		if (cell)
		{
			hexGrid.DestroyHome(cell.Index);
		}
	}
	

	void DestroyUnit()
	{
		HexCell cell = GetCellUnderCursor();
		if (cell && cell.Unit)
		{
			hexGrid.RemoveUnit(cell.Unit);
		}
	}
	
	void DestroyEnemy()
	{
		HexCell cell = GetCellUnderCursor();
		if (cell && cell.Unit)
		{
			hexGrid.RemoveUnit(cell.Unit);
		}
	}

	void HandleInput()
	{
		HexCell currentCell = GetCellUnderCursor();
		if (currentCell)
		{
			EditCells(currentCell);
			previousCell = currentCell;
		}
		else
		{
			previousCell = default;
		}
		UpdateCellHighlightData(currentCell);
	}

	void UpdateCellHighlightData(HexCell cell)
	{
		if (!cell)
		{
			ClearCellHighlightData();
			return;
		}

		// Works up to brush size 6.
		Shader.SetGlobalVector(
			cellHighlightingId,
			new Vector4(
				cell.Coordinates.HexX,
				cell.Coordinates.HexZ,
				brushSize * brushSize + 0.5f
			)
		);
	}

	void ClearCellHighlightData() => Shader.SetGlobalVector(
		cellHighlightingId, new Vector4(0f, 0f, -1f, 0f));
	

	void EditCells(HexCell center)
	{
		int centerX = center.Coordinates.X;
		int centerZ = center.Coordinates.Z;

		for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
		{
			for (int x = centerX - r; x <= centerX + brushSize; x++)
			{
				EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
			}
		}
		for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
		{
			for (int x = centerX - brushSize; x <= centerX + r; x++)
			{
				EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
			}
		}
	}

	void EditCell(HexCell cell)
	{
		if (cell)
		{
			if (activeTerrainTypeIndex >= 0)
			{
				cell.SetTerrainTypeIndex(activeTerrainTypeIndex);
			}
			
			
			if(applyWall)
			{cell.BecomeWall();}
			else
			{
				cell.BecomeGround();
			}

			
			
			if (applyEnemyQuantityLevel)
			{
				cell.SetEnemyQuantityLevel(activeEnemyQuantityLevel);
			}
			if (applyGemQualityLevel)
			{
				cell.SetGemQualityLevel(activeGemQualityLevel);
			}
			if (applyEnemyQualityLevel)
			{
				cell.SetEnemyQualityLevel(activeEnemyQualityLevel);
			}
			
		}
	}
}
