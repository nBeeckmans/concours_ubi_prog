using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using Debug = System.Diagnostics.Debug;

public class LevelSelectionUI : MonoBehaviour
{
    [SerializeField] private LevelSelectListSO selectableLevelsListSO;

    [SerializeField] private SingleLevelSelectUI singleLevelTemplateUI;
    
    [Header("Layouts")]
    [SerializeField] private int maxHorizonalLayout;
    [SerializeField] private Transform levelVerticalLayout;
    [SerializeField] private Transform levelHorizontalLayout;

    private LinkedList<SingleLevelSelectUI> _levelsSelectUI;
    private LinkedListNode<SingleLevelSelectUI> _selectedLevel;

    private void Awake()
    {
        _levelsSelectUI = new LinkedList<SingleLevelSelectUI>();
        
    }

    private void Start()
    {
        ShowInitialUI();

        foreach (SingleLevelSelectUI levelSelectUI in _levelsSelectUI)
        {
            levelSelectUI.UpdateAmuletsToShowClientSide();
        }
        
        LevelSelectionInputManager.Instance.OnLeftUI += InputManager_OnLeftUI;
        LevelSelectionInputManager.Instance.OnRightUI += InputManager_OnRightUI;
        LevelSelectionInputManager.Instance.OnUpUI += InputManager_OnUpUI;
        LevelSelectionInputManager.Instance.OnDownUI += InputManager_OnDownUI;
        
        LevelSelectionInputManager.Instance.OnSelectUI += InputManager_OnSelectUI;

        EventSystem.current.sendNavigationEvents = false;
    }

    private void ShowInitialUI()
    {
        int horizontalLayoutCount = 0;
        Transform currentHorizontalLayout = Instantiate(levelHorizontalLayout, levelVerticalLayout);
        currentHorizontalLayout.gameObject.SetActive(true);
        
        foreach (LevelSelectSO levelSO in selectableLevelsListSO.levels)
        {
            AddHorizontalLayoutWhenFull(ref currentHorizontalLayout, ref horizontalLayoutCount);
            
            InstantiateSingleLevelSelectTemplate(levelSO, currentHorizontalLayout);
            
            horizontalLayoutCount++;
        }

        _selectedLevel = _levelsSelectUI.First;
        EventSystem.current.SetSelectedGameObject(_selectedLevel.Value.gameObject);
    }
    
    public void Show()
    {
        EventSystem.current.sendNavigationEvents = false;
        
        BasicShowHide.Show(gameObject);
        
        EventSystem.current.SetSelectedGameObject(_selectedLevel.Value.gameObject);
    }

    private void AddHorizontalLayoutWhenFull(ref Transform currentHorizontalLayout, ref int horizontalLayoutCount)
    {
            if (horizontalLayoutCount >= maxHorizonalLayout)
            { 
                currentHorizontalLayout = Instantiate(levelHorizontalLayout, levelVerticalLayout);
                
                currentHorizontalLayout.gameObject.SetActive(true);
                
                horizontalLayoutCount = 0;
            }
    }

    private void InstantiateSingleLevelSelectTemplate(LevelSelectSO levelSO, Transform currentHorizontalLayout)
    {
        SingleLevelSelectUI templateInstance = Instantiate(singleLevelTemplateUI, currentHorizontalLayout);
                
        templateInstance.gameObject.SetActive(true);
                
        templateInstance.Show(levelSO);
            
        _levelsSelectUI.AddLast(templateInstance);
    }
    
    private void InputManager_OnLeftUI(object sender, LevelSelectionInputManager.FromServerEventArgs e)
    {
        if (! CanHandleInput(e.SyncrhonizedCall)) { return; }

        if (e.SyncrhonizedCall)
        {
            LevelSelectionSynchronizer.Instance.CopyInputClientRpc(LevelSelectionInputManager.Input.Left);
        }

        UpdateSelectedLevel(_selectedLevel.Next ?? _levelsSelectUI.First);
    }
    
    private void InputManager_OnRightUI(object sender, LevelSelectionInputManager.FromServerEventArgs e)
    {
        if (! CanHandleInput(e.SyncrhonizedCall)) { return; }
        
        if (! e.SyncrhonizedCall)
        {
            LevelSelectionSynchronizer.Instance.CopyInputClientRpc(LevelSelectionInputManager.Input.Right);
        }
            
        UpdateSelectedLevel(_selectedLevel.Previous ?? _levelsSelectUI.Last);
    }
    
    private void InputManager_OnUpUI(object sender, LevelSelectionInputManager.FromServerEventArgs e)
    {
        if (! CanHandleInput(e.SyncrhonizedCall)) { return; }

        if (! e.SyncrhonizedCall)
        {
            LevelSelectionSynchronizer.Instance.CopyInputClientRpc(LevelSelectionInputManager.Input.Up);
        }
        
        LinkedListNode<SingleLevelSelectUI> newSelectedLevel = null;
        
        for (int i = 0; i < maxHorizonalLayout; i++)
        {
            newSelectedLevel = _selectedLevel.Previous;

            if (newSelectedLevel == null) { return; }
        }

        UpdateSelectedLevel(newSelectedLevel);
    }
    private void InputManager_OnDownUI(object sender, LevelSelectionInputManager.FromServerEventArgs e)
    {
        if (! CanHandleInput(e.SyncrhonizedCall)) { return; }

        if (! e.SyncrhonizedCall)
        {
            LevelSelectionSynchronizer.Instance.CopyInputClientRpc(LevelSelectionInputManager.Input.Down);
        }
        
        LinkedListNode<SingleLevelSelectUI> newSelectedLevel = null;
        
        for (int i = 0; i < maxHorizonalLayout; i++)
        {
            newSelectedLevel = _selectedLevel.Next;

            if (newSelectedLevel == null) { return; }
        }

        UpdateSelectedLevel(newSelectedLevel);
    }
    
    [Header("Level Focus UI")]
    [SerializeField] private LevelFocusUI levelFocusUI;

    private void InputManager_OnSelectUI(object sender, LevelSelectionInputManager.FromServerEventArgs e)
    {
        if (! CanHandleInput(e.SyncrhonizedCall)) { return; }

        if (! e.SyncrhonizedCall)
        {
            LevelSelectionSynchronizer.Instance.CopyInputClientRpc(LevelSelectionInputManager.Input.Select);
        }
        
        BasicShowHide.Hide(gameObject);
        
        levelFocusUI.Show(_selectedLevel.Value.AssociatedLevelSO);
    }

    private bool CanHandleInput(bool synchronizedCall)
    {
        return gameObject.activeSelf && (NetworkManager.Singleton.IsServer || synchronizedCall);
    }

    private void UpdateSelectedLevel(LinkedListNode<SingleLevelSelectUI> newSelectedLevel)
    {
        Debug.Assert(_selectedLevel != null, nameof(_selectedLevel) + " != null");
        
        _selectedLevel = newSelectedLevel;
        EventSystem.current.SetSelectedGameObject(_selectedLevel.Value.gameObject);
    }
}
