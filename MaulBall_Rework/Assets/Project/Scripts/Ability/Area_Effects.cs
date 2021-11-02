using Project.Scripts.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public class Area_Effects : MonoBehaviour
    {

        public enum Area_Type
        {
            NONE,
            SLOW_ZONE,
            STUN_ZONE,
            FIRE_ZONE,
            ROOT_ZONE,
        }

        public Area_Type a_type;

        [HideInInspector]
        public int player_Instance;

        [Header("Ability variables")]
        [Tooltip("Duration (sec) ability will continue to exist.")]
        public float ability_Duration;
        [Tooltip("Duration (sec) ability will affect player.")]
        public float ability_Effect_Duration;

        private void Start()
        {
            StartCoroutine(Wait_To_Initiate());
        }

        public void Setup(int _instance_ID, float _duration, float _effect_Duration)
        {
            player_Instance = _instance_ID;
            ability_Duration = _duration;
            ability_Effect_Duration = _effect_Duration;
        }

        void Initiate_Collider()
        {
            GetComponent<Collider>().enabled = true;
        }

        IEnumerator Wait_To_Initiate()
        {
            yield return new WaitForSeconds(0.1f);
            Initiate_Collider();
            StartCoroutine(Wait_To_Destroy());
        }

        IEnumerator Wait_To_Destroy()
        {
            yield return new WaitForSeconds(ability_Duration);
            GameObject.Destroy(this.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.GetInstanceID() != player_Instance && other.gameObject.tag == "Player")
            {
                //apply status effect
                switch (a_type)
                {
                    case Area_Type.STUN_ZONE:
                        other.gameObject.GetComponent<Player_Behaviour>().Initiate_Stun(ability_Effect_Duration);
                        break;
                }

            }
        }
    }

