class_name GDAudio
extends Node3D


@export_group("HUD sounds")
@export var hud_select : EventAsset
func play_hud_select():
	play(hud_select)

@export var hud_cancel : EventAsset
func play_hud_cancel():
	play(hud_cancel)


@export_group("Player sounds")
@export var berserker_attack : EventAsset
func play_berserker_attack():
	play(berserker_attack)

@export var healer_attack : EventAsset
func play_healer_attack():
	play(healer_attack)

@export var tank_attack : EventAsset
func play_tank_attack():
	play(tank_attack)

@export var reaction_charge : EventAsset
func play_reaction_charge():
	play(reaction_charge)

@export var player_death : EventAsset
func play_player_death():
	play(player_death)


@export_group("Enemy sounds")
@export var enemy_movement : EventAsset
func play_enemy_movement():
	play(enemy_movement)

@export var enemy_transform : EventAsset
func play_enemy_transform():
	play(enemy_transform)

@export var enemy_attack : EventAsset
func play_enemy_attack():
	play(enemy_attack)

@export var boss_death : EventAsset
func play_boss_death():
	play(boss_death)


func play(event:EventAsset):
	FMODRuntime.play_one_shot(event)
