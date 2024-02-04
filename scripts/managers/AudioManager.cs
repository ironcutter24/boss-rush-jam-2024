using Godot;
using System;

public partial class AudioManager : Node3D
{
    [Export] private Node3D GDAudio;

    public static AudioManager Instance { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;
    }

    #region Player events

    public void PlayBerserkerAttack() { Play("play_berserker_attack"); }
    public void PlayHealerAttack() { Play("play_healer_attack"); }
    public void PlayTankAttack() { Play("play_tank_attack"); }
    public void PlayReactionCharge() { Play("play_reaction_charge"); }
    public void PlayPlayerDeath() { Play("play_player_attack"); }

    #endregion

    #region Enemy events

    public void PlayEnemyMovement() { Play("play_enemy_movement"); }
    public void PlayEnemyTransform() { Play("play_enemy_transform"); }
    public void PlayEnemyAttack() { Play("play_enemy_attack"); }
    public void PlayBossDeath() { Play("play_boss_death"); }

    #endregion

    #region Helpers

    private void Play(string name)
    {
        GDAudio.Call(name);
    }

    #endregion

}
