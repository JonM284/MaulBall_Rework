using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Character/CharacterStats")]
public class CharacterStats : ScriptableObject
{
    [Header("Base variables")]
    public string character_name;
    public float speed;
    public float runningSpeed;

    [Header("Tackle variables")]
    [Tooltip("How long the player preforms the tackle for")]
    public float tackleDuration;
    public float tackleSpeedMod;
    [Tooltip("How long the player preforms the maul for")]
    public float maulDuration;
    public float maulSpeedMod;
    [Tooltip("How much force used to throw the ball")]
    public float ballForce;
    public float attackSpeedCooldownMax;
    public float damageCooldownMax;
    public float tackleDamageCooldown;
    public float maulDamageCooldown;

}
