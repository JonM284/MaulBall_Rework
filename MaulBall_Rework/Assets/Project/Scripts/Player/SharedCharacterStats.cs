using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Character/BaseCharacterStats")]
public class SharedCharacterStats : ScriptableObject
{
    //stats that are the same between all players
    [Header("Shared stats")]
    public float gravity;
    public float groundRayRad;
    public float groundRayDist;
    public float rotSpeedMod;
    public float ballCheckRad;
    public float meleeRange;
    public float inputSpeed;

    [Header("Passing variables")]
    public float regularPassHeightOffset;
    public float lobPassHeightOffset;
    public float passAngle;
    public float passRange;

}
