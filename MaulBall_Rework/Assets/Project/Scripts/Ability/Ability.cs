using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Player;

[CreateAssetMenu(fileName = "Ability")]
    public class Ability : ScriptableObject
    {

        public enum ability_Type
        {
            NONE,
            PROJECTILE,
            STATUS,
            AREA,
            SPAWNING_OBJECT,
            REPEATER,
            DASH,
            PROXIMITY
        }



        public string ability_Name;
        [TextArea(3, 5)]
        public string ability_Description;
        public ability_Type a_type;
        public Vector3 spawning_Offset;
        [Tooltip("Projectile to be fired, if this ability calls for it. proj behaviour is on projectile.")]
        public GameObject projectile;
        [Tooltip("Duration of this ability.")]
        public float duration;
        [Tooltip("Effect Duration of this ability.")]
        public float effect_Duration;
        [Tooltip("Cooldown time for this ability.")]
        public float cooldown;
        [Tooltip("Interval time for repeatable abilities.")]
        public float repeat_Time;
        [Tooltip("Speed of the ability, if it calls for it.")]
        public float ability_Speed;
        [Tooltip("Controls the size of the ability, projectiles and spawning objects")]
        public Vector3 ability_Size;
        [Tooltip("Causes the user to become invulnerable for the duration of the ability")]
        public bool causes_Invul;
        public bool Is_Stun_Ultimate;

        public void SetUp_Ability(Player_Behaviour _character, int ID_Number, int _player_Instance)
        {


            _character.ability_Cooldown[ID_Number] = cooldown;
            _character.ability_Duration[ID_Number] = duration;
            _character.ability_Repeater_Time[ID_Number] = repeat_Time;
            _character.ability_Effect_Duration[ID_Number] = effect_Duration;
            switch (a_type)
            {
                case ability_Type.PROXIMITY:
                    _character.ability_Type_ID[ID_Number] = (int)ability_Type.PROXIMITY;
                    _character.GetComponent<Player_Behaviour>().proximity_Ability_Range = ability_Size.x;
                    break;
                case ability_Type.DASH:
                    _character.ability_Type_ID[ID_Number] = (int)ability_Type.DASH;
                    break;
                case ability_Type.REPEATER:
                    _character.ability_Type_ID[ID_Number] = (int)ability_Type.REPEATER;
                    break;
                case ability_Type.SPAWNING_OBJECT:
                    _character.ability_Type_ID[ID_Number] = (int)ability_Type.SPAWNING_OBJECT;
                    break;
                case ability_Type.AREA:
                    _character.ability_Type_ID[ID_Number] = (int)ability_Type.AREA;
                    break;
                case ability_Type.STATUS:
                    _character.ability_Type_ID[ID_Number] = (int)ability_Type.STATUS;
                    break;
                case ability_Type.PROJECTILE:
                    _character.ability_Type_ID[ID_Number] = (int)ability_Type.PROJECTILE;
                    break;

                default:

                    break;
            }

            Debug.Log("Fully added ability");
        }

        public void Use_Ability(int _player_Instance, Player_Behaviour _player)
        {
            switch (a_type)
            {
                case ability_Type.PROXIMITY:
                    if (Is_Stun_Ultimate)
                    {
                        _player.GetComponent<Player_Behaviour>().m_Using_Prox_Ultimate = true;

                    }
                    break;
                case ability_Type.DASH:
                    _player.Initiate_Dash_Type(false, false, true, duration, ability_Speed);
                    if (causes_Invul)
                    {
                        _player.Initiate_Invulnerability(true, duration);
                    }
                    break;
                case ability_Type.REPEATER:

                    break;
                case ability_Type.SPAWNING_OBJECT:
                    GameObject _spawned_Obj = Instantiate(projectile, _player.transform.position + _player.transform.TransformDirection(spawning_Offset), _player.transform.rotation) as GameObject;
                    _spawned_Obj.transform.localScale = new Vector3(ability_Size.x, ability_Size.y, ability_Size.z);
                    _spawned_Obj.GetComponent<Area_Effects>().Setup(_player_Instance, duration, effect_Duration);
                    break;
                case ability_Type.AREA:

                    break;
                case ability_Type.STATUS:
                    if (causes_Invul)
                    {
                        _player.Initiate_Invulnerability(true, duration);
                    }
                    break;
                case ability_Type.PROJECTILE:

                    break;
                default:

                    break;
            }

            Debug.Log(ability_Name);
        }

    }
