using System;
using System.Collections;
using System.Collections.Generic;
using Grid;
using Grid.Interface;
using TMPro;
using UI;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BuildingTrapOnGridUI : MonoBehaviour
{
    [Header("Build")]
    [SerializeField] private Button buildButton;
    
    [Header("Arrows button")]
    [SerializeField] private Button upArrowButton;
    [SerializeField] private Button downArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private Button leftArrowButton;

    [Header("Error")]
    [SerializeField] private GameObject errorUI;
    [SerializeField] private TextMeshProUGUI errorText;
    
    [Header("Other")]
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject highlighterGameObject;
    
    private LinkedList<Cell> _enemyWalkableCells;
    private LinkedListNode<Cell> _selectedCell;

    private BuildableObjectSO _trapSO;
    private GameObject _trapPreview;
    private GameObject _currentHighlighter;

    private void Awake()
    {
        buildButton.onClick.AddListener(BuildTrapOnButtonClick);
        
        upArrowButton.onClick.AddListener(ChangeSelectedCellUp);
        downArrowButton.onClick.AddListener(ChangeSelectedCellDown);
        leftArrowButton.onClick.AddListener(ChangeSelectedCellLeft);
        rightArrowButton.onClick.AddListener(ChangeSelectedCellRight);

        closeButton.onClick.AddListener(CloseUI);
    }
    
    private void Start()
    {
        SynchronizeBuilding.Instance.OnBuildingBuilt += SynchronizeBuilding_OnBuildingBuilt;
        
        InputManager.Instance.OnUserInterfaceLeftPerformed += InputManager_OnUserInterfaceLeftPerformed;
        InputManager.Instance.OnUserInterfaceRightPerformed += InputManager_OnUserInterfaceRightPerformed;
        InputManager.Instance.OnUserInterfaceUpPerformed += InputManager_OnUserInterfaceUpPerformed;
        InputManager.Instance.OnUserInterfaceDownPerformed += InputManager_OnUserInterfaceDownPerformed;
        
        InputManager.Instance.OnUserInterfaceCancelPerformed += InputManager_OnUserInterfaceCancelPerformed;

        errorText.color = ColorPaletteUI.Instance.ColorPaletteSo.errorColor;
        
        BasicShowHide.Hide(gameObject);
    }

    public void Show(BuildableObjectSO trapSO)
    {
        Debug.Log(trapSO.objectName);
        _trapSO = trapSO;
        
        _enemyWalkableCells = TilingGrid.grid.GetEnemyWalkableCells();
        _selectedCell = _enemyWalkableCells.First;
        
        UpdateUI();
        
        buildButton.Select();
        
        BasicShowHide.Show(gameObject);
    }
    
    public void Hide()
    {
        DestroyPreview();
        
        BasicShowHide.Hide(gameObject);
    }

    private void UpdateUI()
    {
        CameraController.Instance.MoveCameraToPosition(TilingGrid.GridPositionToLocal(_selectedCell.Value.position));
        
        if (TryShowMissingResourceError()) { return; }
        
        if (TryShowAlreadyHasBuildingError()) { return; }

        if (TryShowHasEnemyError()) { return; }
        
        BasicShowHide.Hide(errorUI.gameObject);
        ShowPreviewOnSelectedCell();
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
        if (CentralizedInventory.Instance.HasResourcesForBuilding(_trapSO)) { return false; } 

        ShowErrorText(MISSING_RESOURCE_ERROR);
        
        return true;
    }

    private int _showErrorTextTweening;
    private void ShowErrorText(string toShow)
    {
        DestroyPreview();
            
        errorText.text = toShow;
        
        LeanTween.cancel(_showErrorTextTweening);
        errorUI.transform.localScale = Vector3.zero;
        
        BasicShowHide.Show(errorUI.gameObject);
        _showErrorTextTweening =
            errorUI.transform.LeanScale(Vector3.one, 0.4f).setEaseOutExpo().id;
    }
    
    private void ShowPreviewOnSelectedCell()
    {
        DestroyLastPreview();
        
        Vector3 previewDestination = TilingGrid.CellPositionToLocal(_selectedCell.Value);
        
        _trapPreview = Instantiate(_trapSO.visuals);
        _trapPreview.GetComponent<BuildableObjectVisuals>().ShowPreview(previewDestination);
        
        AddHighlighter(previewDestination);
    }

    private void DestroyLastPreview()
    {
        Destroy(_trapPreview);
    }
    
    private void AddHighlighter(Vector3 position)
    {
        Destroy(_currentHighlighter);
        _currentHighlighter = Instantiate(highlighterGameObject, position, quaternion.identity);
    }
    
    private void BuildTrapOnButtonClick()
    {
        if (IsAbleToBuild())
        {
            SynchronizeBuilding.Instance.SpawnBuildableObject(_trapSO, _selectedCell.Value);
            
            UpdateSelectedCell();
        }
    }

    private bool IsAbleToBuild()
    {
        _selectedCell.Value = TilingGrid.grid.GetCell(_selectedCell.Value.position);
        
        return _selectedCell.Value.HasNotBuildingOnTop() &&
               CentralizedInventory.Instance.HasResourcesForBuilding(_trapSO) &&
               ! _selectedCell.Value.HasTopOfCellOfType(TypeTopOfCell.Enemy) &&
               ! _selectedCell.Value.HasObjectOfTypeOnTop(TypeTopOfCell.Obstacle);
    }
    
    private void SynchronizeBuilding_OnBuildingBuilt(object sender, SynchronizeBuilding.OnBuildingBuiltEventArgs e)
    {
        if (!gameObject.activeSelf) { return; }
        
        if (e.BuildingPosition == _selectedCell.Value.position)
        {
            UpdateSelectedCell();
        }
    }
    
    private void UpdateSelectedCell()
    {
        _selectedCell.Value = TilingGrid.grid.GetCell(_selectedCell.Value.position);
        
        UpdateUI();
    }

    private void ChangeSelectedCellUp()
    {
        ChangeSelectedCell(Vector2Int.up);
    }
    
    private void ChangeSelectedCellDown()
    {
        ChangeSelectedCell(Vector2Int.down);
    }
    
    private void ChangeSelectedCellRight()
    {
        ChangeSelectedCell(Vector2Int.right);
    }
    
    private void ChangeSelectedCellLeft()
    {
        ChangeSelectedCell(Vector2Int.left);
    }

    private void ChangeSelectedCell(Vector2Int direction)
    {
        Cell nextCell = TilingGrid.grid.GetCell(_selectedCell.Value.position + direction);

        if (nextCell.Has(BlockType.EnemyWalkable) && !nextCell.Has(BlockType.Buildable))
        {
            _selectedCell.Value = nextCell;
            UpdateUI();
        }
    }

    private void DestroyPreview()
    {
        Destroy(_trapPreview);

        RemoveHighlighter();
    }

    private void RemoveHighlighter()
    {
        if (_currentHighlighter != null)
        {
            Destroy(_currentHighlighter.gameObject);
        }
    }
    
    private void InputManager_OnUserInterfaceLeftPerformed(object sender, EventArgs e)
    {
        if (!gameObject.activeSelf) { return; }
        
        SetSelectedCellAtDirection(Vector2Int.left);
    }
    
    private void InputManager_OnUserInterfaceRightPerformed(object sender, EventArgs e)
    {
        if (!gameObject.activeSelf) { return; }
        
        SetSelectedCellAtDirection(Vector2Int.right);
    }
    
    private void InputManager_OnUserInterfaceUpPerformed(object sender, EventArgs e)
    {
        if (!gameObject.activeSelf) { return; }
        
        SetSelectedCellAtDirection(Vector2Int.up);
    }

    private void InputManager_OnUserInterfaceDownPerformed(object sender, EventArgs e)
    {
        if (!gameObject.activeSelf) { return; }
        
        SetSelectedCellAtDirection(Vector2Int.down);
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

    private void SetSelectedCellAtDirection(Vector2Int direction)
    {
        Cell dirCell = TilingGrid.grid.GetCell(_selectedCell.Value.position + direction);

        LinkedListNode<Cell> dirCellNode = _enemyWalkableCells.Find(dirCell);

        if (dirCellNode != null)
        {
            _selectedCell = dirCellNode;
            UpdateSelectedCell();
        }
    }

}
