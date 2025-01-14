using System;
using System.Collections;
using System.Collections.Generic;
using Grid;
using Grid.Interface;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Type = Grid.Type;

public class BuildingTowerOnGridUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button closeUIButton;
    [SerializeField] private Button buildButton;
    
    [Header("Arrows")]
    [SerializeField] private Button upArrowButton;
    [SerializeField] private Button downArrowButton;
    [SerializeField] private Button rightArrow;
    [SerializeField] private Button leftArrow;

    [Header("Error")]
    [SerializeField] private GameObject errorUI;
    [SerializeField] private TextMeshProUGUI errorText;

    private LinkedList<Cell> _buildableCells;
    private LinkedListNode<Cell> _selectedCell;
    private BuildableObjectSO _towerToBuild;

    private GameObject _preview;

    private void Awake()
    {
        closeUIButton.onClick.AddListener(CloseUI);
        
        buildButton.onClick.AddListener(OnBuildButtonClicked);
        
        rightArrow.onClick.AddListener(ChangeSelectedCellRight);
        leftArrow.onClick.AddListener(ChangeSelectedCellLeft);
        upArrowButton.onClick.AddListener(ChangeSelectedCellUp);
        downArrowButton.onClick.AddListener(ChangeSelectedCellDown);
    }

    private void Start()
    {
        SynchronizeBuilding.Instance.OnBuildingBuilt += SynchronizeBuilding_OnBuildingBuilt;

        InputManager.Instance.OnUserInterfaceCancelPerformed += InputManager_OnUserInterfaceCancelPerformed;
        
        InputManager.Instance.OnUserInterfaceLeftPerformed += InputManager_OnUserInterfaceLeftPerformed;
        InputManager.Instance.OnUserInterfaceRightPerformed += InputManager_OnUserInterfaceRightPerformed;
        InputManager.Instance.OnUserInterfaceUpPerformed += InputManager_OnUserInterfaceUpPerformed;
        InputManager.Instance.OnUserInterfaceDownPerformed += InputManager_OnUserInterfaceDownPerformed;
        
        _buildableCells = TilingGrid.grid.GetBuildableCells();
        //SortBuildableCells();
        
        _selectedCell = _buildableCells.First;
        
        errorText.color = ColorPaletteUI.Instance.ColorPaletteSo.errorColor;
        
        BasicShowHide.Hide(gameObject);
    }

    public void Show(BuildableObjectSO buildableObjectSO)
    {
        InputManager.Instance.DisablePlayerInputMap();

        _towerToBuild = buildableObjectSO;
        
        BasicShowHide.Show(gameObject);
        
        UpdateUI();
        
        buildButton.Select();
    }

    public void Hide()
    {
        HidePreview();
        
        BasicShowHide.Hide(gameObject);
    }
    
    private void OnBuildButtonClicked()
    {
        Debug.Log("BUILD BUTTON CLICKED !");
        
        if (IsAbleToBuild())
        {
            SynchronizeBuilding.Instance.SpawnBuildableObject(_towerToBuild, _selectedCell.Value);
            
            UpdateSelectedCell(_selectedCell.Value.position);
        }
    }

    private bool IsAbleToBuild()
    {
        _selectedCell.Value = TilingGrid.grid.GetCell(_selectedCell.Value.position);
        
        return _selectedCell.Value.HasNotBuildingOnTop() &&
               CentralizedInventory.Instance.HasResourcesForBuilding(_towerToBuild) &&
               ! _selectedCell.Value.HasTopOfCellOfType(TypeTopOfCell.Enemy) &&
               ! _selectedCell.Value.HasObjectOfTypeOnTop(TypeTopOfCell.Obstacle);
    }

    private void SynchronizeBuilding_OnBuildingBuilt(object sender, SynchronizeBuilding.OnBuildingBuiltEventArgs e)
    {
        if (gameObject.activeSelf)
        {
            UpdateSelectedCell(e.BuildingPosition);
        }
    }

    private void UpdateSelectedCell(Vector2Int cellToUpdate)
    {
        if (_selectedCell.Value.position == cellToUpdate)
        {
            _selectedCell.Value = TilingGrid.grid.GetCell(cellToUpdate);
        }
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        Debug.Assert(_buildableCells.Count != 0);
        Debug.Assert(_selectedCell != null);
        
        CameraController.Instance.MoveCameraToPosition
            (TilingGrid.GridPositionToLocal(_selectedCell.Value.position));
        
        if (TryShowMissingResourceError()) { return; }
        
        if (TryShowAlreadyHasBuildingError()) { return; }

        if (TryShowHasEnemyError()) { return; }
        
        BasicShowHide.Hide(errorUI.gameObject);
            
        ShowPreview();
    }

    private const string ALREADY_HAS_BUILDING_ERROR = "Already Has a Building !";
    private bool TryShowAlreadyHasBuildingError()
    {
        if (_selectedCell.Value.HasNotBuildingOnTop() &&
            !_selectedCell.Value.HasTopOfCellOfType(TypeTopOfCell.Obstacle))
        {
            return false;
        } 
        
        ShowErrorText(ALREADY_HAS_BUILDING_ERROR);

        return true;
    }
    
    private const string HAS_ENEMY_ERROR = "Building Spot Has Enemy On It !";
    private bool TryShowHasEnemyError()
    {
        if (!_selectedCell.Value.HasTopOfCellOfType(TypeTopOfCell.Enemy)) { return false; }
        
        ShowErrorText(HAS_ENEMY_ERROR);

        return true;
    }

    private const string MISSING_RESOURCE_ERROR = "Resources Missing For Building !";
    private bool TryShowMissingResourceError()
    {
        if (CentralizedInventory.Instance.HasResourcesForBuilding(_towerToBuild)) { return false; } 
        
        ShowErrorText(MISSING_RESOURCE_ERROR);

        return true;
    }
    
    private int _showErrorTextTweening;
    private void ShowErrorText(string toShow)
    {
        HidePreview();
            
        LeanTween.cancel(_showErrorTextTweening);
        errorUI.transform.localScale = Vector3.zero;
        
        errorText.text = toShow;
        
        BasicShowHide.Show(errorUI.gameObject);
        _showErrorTextTweening =
            errorUI.transform.LeanScale(Vector3.one, 0.4f).setEaseOutExpo().id;
    }

    private void ShowPreview()
    {
        HidePreview();

        Vector3 previewPosition = TilingGrid.GridPositionToLocal(_selectedCell.Value.position);

        _preview = Instantiate(_towerToBuild.visuals);

        BuildableObjectVisuals previewVisualsComponent = _preview.GetComponent<BuildableObjectVisuals>();
        
        previewVisualsComponent.ShowPreview(previewPosition);
    }

    private void HidePreview()
    {
        if (_preview != null)
        {
            Destroy(_preview);
            _preview = null;
        }
    }
    
    private void InputManager_OnUserInterfaceRightPerformed(object sender, EventArgs e)
    {
        if (CanChangeSelectedCell())
        {
            ChangeSelectedCellRight();
        }
    }
    
    private void InputManager_OnUserInterfaceLeftPerformed(object sender, EventArgs e)
    {
        if (CanChangeSelectedCell())
        {
            ChangeSelectedCellLeft();
        }
    }
    
    private void InputManager_OnUserInterfaceUpPerformed(object sender, EventArgs e)
    {
        if (CanChangeSelectedCell())
        {
            ChangeSelectedCellUp();
        }
    }

    private void InputManager_OnUserInterfaceDownPerformed(object sender, EventArgs e)
    {
        if (CanChangeSelectedCell())
        {
            ChangeSelectedCellDown();
        }
    }

    private bool CanChangeSelectedCell()
    {
        return gameObject.activeSelf;
    }
    
    private void InputManager_OnUserInterfaceCancelPerformed(object sender, EventArgs e)
    {
        if (gameObject.activeSelf)
        {
            CloseUI();
        }
    }

    public event EventHandler OnCloseUI;
    private void CloseUI()
    {
        InputManager.Instance.EnablePlayerInputMap();
            
        Hide();
            
        CentralizedInventory.Instance.ClearAllMaterialsCostUI();
        
        OnCloseUI?.Invoke(this, EventArgs.Empty);
    }
    
    private void ChangeSelectedCellRight()
    {
        ChangeSelectedCell(Vector2Int.right);
    }
    
    private void ChangeSelectedCellLeft()
    {
        ChangeSelectedCell(Vector2Int.left);
    }
    
    private void ChangeSelectedCellUp()
    {
        ChangeSelectedCell(Vector2Int.up);
    }
    
    private void ChangeSelectedCellDown()
    {
        ChangeSelectedCell(Vector2Int.down);
    }

    private void ChangeSelectedCell(Vector2Int direction)
    {
        
        if (direction == Vector2Int.right)
        {
            _selectedCell = _selectedCell.Previous ?? _buildableCells.Last;
        }
        if (direction == Vector2Int.left)
        {
            _selectedCell = _selectedCell.Next ?? _buildableCells.First;
        }

        UpdateSelectedCell(_selectedCell.Value.position);
    }

}
