using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Ability
{
    [CreateAssetMenu(menuName = "Ability/Ablity_Stats")]
    public class AbilityStats : ScriptableObject
    {
        [Header("Descriptive")]
        public string ability_Name;
        [TextArea(3, 5)]
        public string ability_Description;
        public Sprite ability_Img;

        [Header("Stats")]
        public Vector3 spawning_Offset;
        [Tooltip("Object to be instantiated, if available")]
        public GameObject prefabObj;
        [Tooltip("Duration of this ability.")]
        public float duration;
        [Tooltip("Effect Duration on other player.")]
        public float effect_Duration;
        [Tooltip("Cooldown time for this ability.")]
        public float cooldown;
        [Tooltip("Interval time for repeatable abilities.")]
        public float repeat_Time;
        [Tooltip("Speed of the ability, if it calls for it.")]
        public float ability_Speed;
        [Tooltip("Controls the size of the ability, projectiles and spawning objects")]
        public Vector3 ability_Size;
    }
}

