using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Utilities.Patterns;

public partial class TurnManager : Node3D
{
    const float showcaseAwaitDuration = .8f;

    public delegate TurnState TurnState();

    private Unit currentUnit, currentTarget;
    private Task currentTask;
    private Vector2I? cursorGridPos;
    private InputManager inputManager;
    private LevelData levelData;
    private EnemyUnit[] enemyUnits;
    private int enemyIndex;

    [Export] private UnitHUD unitHUD;
    [Export] private RichTextLabel hintLabel;

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
        AIInit, AIContext, AIShowWalkable, AIMove, AIShowHittable, AISelectSwap, AIAwaitReaction, AIAttack
    }
    StateMachine<State> sm = new StateMachine<State>(State.PlayerSelectUnit);


    public override void _EnterTree()
    {
        inputManager = GetNode<InputManager>("../InputManager");
        levelData = GetNode<LevelData>("../Level");
    }

    public override void _Ready()
    {
        InitPlayerStates();
        InitEnemyStates();

        sm.StateChanged += s => GD.Print($"FSM >> Changed to: \"{s}\"");
        sm.StateChanged += s => SetHint("");
    }

    public override void _Process(double delta)
    {
        sm.Process();
    }

    private void SetHint(string text)
    {
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
