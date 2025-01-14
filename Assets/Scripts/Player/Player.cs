using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Grid;
using Grid.Blocks;
using Grid.Interface;
using Unity.Mathematics;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;
using Utils;
using Timer = Unity.Multiplayer.Samples.Utilities.ClientAuthority.Utils.Timer;

public class Player : NetworkBehaviour, ITopOfCell
{
    public static Player LocalInstance { get; private set; }
    public static Player OtherInstance { get; private set; }

    private const float MinPressure = 0.3f;

    private const string SPAWN_POINT_COMPONENT_ERROR =
        "Chaque spawn point de joueur doit avoir le component `BlockPlayerSpawn`";
    [SerializeField] private float cooldown = 0.1f;
    [SerializeField] private PlayerTileSelector _selector;
    [SerializeField] private GameObject _highlighter;
    [SerializeField] private float _timeToMove = 0.5f;
    [SerializeField] private Transform SK;
    private Animator animator;
    protected Utils.OwnerNetworkAnimator Network;

    private Recorder<GameObject> _highlighters;
    private Timer _timer;
    public static int Health;
    public static int Energy;
    public bool hasFinishedMoveAnimation = false;

    public int EnergyAvailable
    {
        set
        {
            _totalEnergy = value;
            _currentEnergy = value;
        }
    }

    private Vector3 SK_originPosition;
    private Quaternion SK_originRotation;
    private bool _hasFinishedToMove;

    private bool _canMove;

    private int _totalEnergy = Energy;
    private int _currentEnergy;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        SK_originPosition = SK.localPosition;
        SK_originRotation = SK.localRotation;
    }


    public Player()
    {
        _timer = new(cooldown);
        _highlighters = new();
    }

    public void InputMove(Vector2 direction)
    {
        if (IsMovementInvalid()) return;
        if (direction == Vector2.zero) return;
        if (_canMove)
            HandleInput(direction);
    }

    /// <summary>
    /// Check si les conditions de deplacement sont valides.
    /// </summary>
    /// <returns> true si elles sont invalides</returns>
    private bool IsMovementInvalid()
    {
        if (!IsOwner) return true;
        if (!CooldownHasPassed()) return true;

        return false;
    }

    private void ResetBoolean()
    {
        _hasFinishedToMove = false;
    }

    private void HandleInput(Vector2 direction)
    {
        Vector2Int input = TranslateToVector2Int(direction);
        var savedSelectorPosition = SaveSelectorPosition();
        MoveType hasMoved = _selector.GetTypeOfMovement(input);

        if (hasMoved == MoveType.ConsumeLast)
        {
            IncrementEnergy();
            RemovePreviousHighlighter();
            _selector.RemoveFromRecorder();
            _selector.MoveSelector();
            _timer.Start();
        }
        else if (hasMoved == MoveType.New)
        {
            if (HasEnergy())
            {
                DecrementEnergy();
                AddHighlighter(savedSelectorPosition);
                _selector.AddToRecorder(input);
                _selector.MoveSelector();
                _timer.Start();
            }
        }
        else if (hasMoved != MoveType.Invalid)
        {
            throw new Exception("Invalide input " + direction);
        }
    }

    private Vector3 SaveSelectorPosition()
    {
        return _selector.transform.position;
    }

    private void AddHighlighter(Vector3 position)
    {
        GameObject newHighlighter = Instantiate(_highlighter, position, quaternion.identity);
        _highlighters.Add(newHighlighter);
    }

    private void RemovePreviousHighlighter()
    {
        if (!_highlighters.IsEmpty())
        {
            GameObject nextHighLighter = _highlighters.RemoveFirst();
            Destroy(nextHighLighter);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalInstance = this;
            InputManager.Player = this;
        }
        else
        {
            OtherInstance = this;
        }

        InitializePlayerBasedOnCharacterSelection();

        if (IsServer)
        {
            TilingGrid.grid.PlaceObjectAtPositionOnGrid(gameObject, transform.position);
        }
    }

    private void InitializePlayerBasedOnCharacterSelection()
    {
        CharacterSelectUI.CharacterId characterSelection =
            GameMultiplayerManager.Instance.GetCharacterSelectionFromClientId(OwnerClientId);

        if (characterSelection == CharacterSelectUI.CharacterId.Monkey)
        {
            MovePlayerOnSpawnPoint(TowerDefenseManager.Instance.MonkeyBlockPlayerSpawn);
            SetReachableCells(true, transform.position);

            if (IsOwner)
            {
                CameraController.Instance.SetBonzoCameraAsMain();
            }
        }
        else
        {
            MovePlayerOnSpawnPoint(TowerDefenseManager.Instance.RobotBlockPlayerSpawn);
            SetReachableCells(false, transform.position);
            if (IsOwner)
            {
                CameraController.Instance.SetZombotCameraAsMain();
            }
        }
    }

    private void MovePlayerOnSpawnPoint(Transform spawnPoint)
    {
        bool hasComponent = spawnPoint.TryGetComponent(out BlockPlayerSpawn blockPlayerSpawn);

        if (hasComponent)
        {
            blockPlayerSpawn.SetPlayerOnBlock(transform);
        }
        else
        {
            Debug.LogError(SPAWN_POINT_COMPONENT_ERROR);
        }

        if (IsOwner)
        {
            CameraController.Instance.MoveCameraToPosition(transform.position);
        }
    }

    /// <summary>
    /// Demande au timer de verifier si le temps ecoule permet un nouveau deplacement
    /// </summary>
    /// <returns></returns>
    private bool CooldownHasPassed()
    {
        return _timer.HasTimePassed();
    }

    /// <summary>
    ///  Traduite la valeur d'input en Vector2Int
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private Vector2Int TranslateToVector2Int(Vector2 value)
    {
        Vector2Int translation = new Vector2Int();
        // pas tres jolie mais franchement ca marche 
        if (value.x > MinPressure)
        {
            translation.x = 1;
        }

        else if (value.x < -MinPressure)
        {
            translation.x = -1;
        }

        else if (value.y > MinPressure)
        {
            translation.y = +1;
        }
        else if (value.y < -MinPressure)
        {
            translation.y = -1;
        }

        return translation;
    }

    public void OnSelect()
    {
        TowerDefenseManager.Instance.SetPlayerReadyToPass(true);
        _canMove = false;
        _selector.Confirm();
        if (!_selector.isSelecting)
            _selector.Initialize(transform.position);
    }

    // Methode appellee que le joeur appuie sur le bouton de selection (A sur gamepad par defaut ou spece au clavier)
    public void OldSelect()
    {
        if (_selector.isSelecting)
            return;

        _selector.Select();
        _selector.isSelecting = true;
        _selector.Initialize(transform.position);
        TowerDefenseManager.Instance.SetPlayerReadyToPass(false);
        _canMove = true;
    }

    public IEnumerator Move()
    {
        Vector2Int characterPosition = TilingGrid.LocalToGridPosition(transform.position);
        Vector2Int? oldPosition = _selector.GetCurrentPosition();
        _selector.Disable();
        Vector2Int? nextPosition = _selector.GetNextPositionToGo();

        if (nextPosition == null || oldPosition == null)
        {
            IsReadyServerRpc();
            ResetBoolean();
            yield break;
        }

        RemoveNextHighlighter();
        UpdateMoveServerRpc((Vector2Int)characterPosition, (Vector2Int)nextPosition);
        StartCoroutine(MoveToNextPosition((Vector2Int)nextPosition));
        yield return new WaitUntil(IsReadyToPickUp);
        PickUpItems((Vector2Int)nextPosition);
        IsReadyServerRpc();
        ResetBoolean();
    }

    [ServerRpc]
    private void UpdateMoveServerRpc(Vector2Int oldPosition, Vector2Int nextPosition)
    {
        TilingGrid.UpdateMovePositionOnGrid(this.gameObject, oldPosition, nextPosition);
    }

    private bool IsReadyToPickUp()
    {
        return _hasFinishedToMove;
    }

    private static void PickUpItems(Vector2Int position)
    {
        GameMultiplayerManager.Instance.PickUpResourcesServerRpc(position);
    }

    private void CleanHighlighters()
    {
        while (_highlighters != null && !_highlighters.IsEmpty())
        {
            RemoveNextHighlighter();
        }
    }

    private void RemoveNextHighlighter()
    {
        if (!_highlighters.IsEmpty())
        {
            GameObject nextHighLighter = _highlighters.RemoveLast();
            Destroy(nextHighLighter);
        }
    }

    private bool HasEnergy()
    {
        return _currentEnergy > 0;
    }


    public void ApplyDamage(int damage)
    {
        int changedHealth = Health - damage;
        changedHealth = Mathf.Clamp(changedHealth, 0, int.MaxValue);
        
        Health = changedHealth;

        if (IsServer)
        {
            TowerDefenseManager.Instance.EmitOnPlayerHealthChangedClientRpc(changedHealth);
        }
    }

    public event EventHandler<OnPlayerEnergyChangedEventArgs> OnPlayerEnergyChanged;

    public class OnPlayerEnergyChangedEventArgs : EventArgs
    {
        public int Energy;
    }

    private void ResetEnergy()
    {
        _currentEnergy = _totalEnergy;

        OnPlayerEnergyChanged?.Invoke(this, new OnPlayerEnergyChangedEventArgs
        {
            Energy = _totalEnergy,
        });
    }

    private void DecrementEnergy()
    {
        _currentEnergy--;

        OnPlayerEnergyChanged?.Invoke(this, new OnPlayerEnergyChangedEventArgs
        {
            Energy = _currentEnergy,
        });
    }

    private void IncrementEnergy()
    {
        _currentEnergy++;

        OnPlayerEnergyChanged?.Invoke(this, new OnPlayerEnergyChangedEventArgs
        {
            Energy = _currentEnergy,
        });
    }

    public void OnCancel()
    {
        ResetEnergy();
        CleanHighlighters();
        _selector.ResetSelf();
        TowerDefenseManager.Instance.SetPlayerReadyToPass(false);
        ResetPlayer(_totalEnergy);
    }

    private IEnumerator MoveToNextPosition(Vector2Int toPosition)
    {
        _hasFinishedToMove = false;
        Vector3 cellLocalPosition = TilingGrid.GridPositionToLocal(toPosition);
        RotationAnimation rotationAnimation = new RotationAnimation();
        
        StartCoroutine(rotationAnimation.TurnAngleObjectTo(this.gameObject, cellLocalPosition));
        yield return new WaitUntil(rotationAnimation.HasMoved);
       // SetAnimatorStateServerRpc(true);
        SetAnimatorState(true);

        Vector3 origin = transform.position;

        float currentTime = 0.0f;
        while (currentTime < _timeToMove)
        {
            float f = currentTime / _timeToMove;
            transform.position = Vector3.Lerp(origin, cellLocalPosition, f);
            currentTime += Time.deltaTime;
            yield return null;
        }

        this.transform.position = cellLocalPosition;
        SetAnimatorState(false);
        //SetAnimatorStateServerRpc(false);
        _hasFinishedToMove = true;
        ResetSkStates();
    }

    
    private void SetAnimatorState(bool isMoving)
    {
        animator.SetBool("Move", isMoving);
    }


    
    public void ResetPlayer(int energy)
    {
        ResetEnergy();
        EnergyAvailable = energy;
        _canMove = false;
        _selector.ResetSelf();
        _highlighters.Reset();
        OldSelect();
    }

    public void PrepareToMove()
    {
        _selector.GetNextPositionToGo();
    }

    public TypeTopOfCell GetType()
    {
        return TypeTopOfCell.Player;
    }

    public GameObject ToGameObject()
    {
        return this.gameObject;
    }

    private void SetReachableCells(bool isMonkey, Vector3 position)
    {
        if (isMonkey)
        {
            TilingGrid.FindReachableCellsMonkey(position);
        }
        else
        {
            TilingGrid.FindReachableCellsRobot(position);
        }
    }

    public enum MoveType
    {
        New,
        ConsumeLast,
        Invalid,
    }

    [ServerRpc]
    private void IsReadyServerRpc()
    {
        EnvironmentTurnManager.Instance.IncrementPlayerFinishedMoving();
    }

    private void ResetSkStates()
    {
        LocalInstance.SK.localPosition = LocalInstance.SK_originPosition;
        LocalInstance.SK.localRotation = LocalInstance.SK_originRotation;
        OtherInstance.SK.localPosition = OtherInstance.SK_originPosition;
        OtherInstance.SK.localRotation = OtherInstance.SK_originRotation;
    }

}