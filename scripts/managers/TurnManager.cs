using Godot;
using System;
using System.Threading.Tasks;
using Utilities.Patterns;

public partial class TurnManager : Node3D
{
    const float showcaseAwaitDuration = .8f;

    private Unit currentUnit, currentTarget, nextPossessedUnit;
    private PlayerUnit swappedUnit;
    private Task currentTask;
    private Vector2I? cursorGridPos;
    private InputManager inputManager;
    private LevelData levelData;
    private EnemyUnit[] enemyUnits;
    private int enemyIndex;
    private StateMachine<State> sm = new StateMachine<State>(State.PlayerSelectUnit);

    [Export] private UnitHUD unitHUD;
    [Export] private Label turnLabel;
    [Export] private RichTextLabel hintLabel;
    [Export] private PackedScene minionPackedScene;
    [Export] private int minionCount = 4;
    [Export] private HealthBar bossHealthBar;

    enum MeshColor { Red, Yellow, Green }
    enum State
    {
        // Player
        PlayerTurn, PlayerCanEndTurn,  // Base states
        PlayerSelectUnit, PlayerUnitContext,
        PlayerSelectMove, PlayerAwaitMove,
        PlayerSelectSpecial, PlayerAwaitSpecial,
        PlayerSelectAttack, PlayerAwaitAttack,
        PlayerAwaitReaction,

        // Enemy
        EnemyTurn,  // Base states
        AIInit, AISpawnMinions, AIContext, AIShowWalkable, AIMove, AIShowHittable, AISelectSwap, AIAwaitReaction, AIAttack
    }

    public delegate TurnState TurnState();

    public override void _EnterTree()
    {
        inputManager = GetNode<InputManager>("../InputManager");
        levelData = GetNode<LevelData>("../Level");

        if (minionPackedScene == null)
        {
            GD.PrintErr("You need to assign a minion PackedScene in this TurnManager instance!", this);
        }
    }

    public override void _Ready()
    {
        InitPlayerStates();
        InitEnemyStates();

        PickNextPossessedUnit();

        bossHealthBar.Depleted += OnBossDefeated;

        sm.StateChanged += s => GD.Print($"FSM >> Changed to: \"{s}\"");
        sm.StateChanged += s => SetHintLabel("");
    }

    public override void _Process(double delta)
    {
        sm.Process();
    }

    private void OnBossDefeated()
    {
        AudioManager.Instance.PlayBossDeath();
        Global.Instance.LoadNextScene();
    }

    private void SetTurnLabel(string text)
    {
        turnLabel.Text = text;
    }

    private void SetHintLabel(string text)
    {
        if (!text.Contains("\n"))
        {
            text = "\n" + text;
        }
        hintLabel.Text = $"[center]{text}[/center]";
    }

    #region Helper methods

    private MeshInstance3D DisplayMesh(Mesh mesh, MeshColor color)
    {
        if (mesh == null) return null;

        var m = new MeshInstance3D();
        m.Mesh = mesh;
        m.MaterialOverride = GetMaterialFrom(color);
        m.Position = Vector3.Up * .05f;
        AddChild(m);
        return m;
    }

    private void DestroyChildren()
    {
        foreach (var child in GetChildren())
        {
            child.Free();
        }
    }

    private void ResetCurrentUnit()
    {
        currentUnit?.SetSelected(false);
        currentUnit = null;
    }

    private Material GetMaterialFrom(MeshColor color)
    {
        return GD.Load<Material>($"graphics/materials/fade_{color.ToString().ToLower()}_mat.tres");
    }

    #endregion

}
