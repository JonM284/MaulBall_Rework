using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Project.Scripts.Player;

namespace Project.Scripts.Misc
{
    public class UI_Follower : MonoBehaviour
    {

        public GameObject target;
        public Vector3 offset;
        private UnityEngine.Camera cam;

        public Player_Behaviour my_Followed_Player;
        public int player_ID;

        // Use this for initialization
        void Start()
        {
            cam = UnityEngine.Camera.main;



        }

        public void Update_Player_To_Use()
        {
            target.GetComponent<Player_Behaviour>().my_Indicator = this;
            Update_Color();
        }

        public void Update_Color()
        {
            switch (target.GetComponent<Player_Behaviour>().Player_ID)
            {
                case 4:
                    this.GetComponent<TMP_Text>().color = Color.green;
                    break;
                case 3:
                    this.GetComponent<TMP_Text>().color = Color.yellow;
                    break;
                case 2:
                    this.GetComponent<TMP_Text>().color = Color.cyan;
                    break;
                case 1:
                    this.GetComponent<TMP_Text>().color = Color.red;
                    break;
                default:
                    this.GetComponent<TMP_Text>().color = Color.white;
                    break;
            }
            this.GetComponent<TMP_Text>().text = player_ID.ToString();
        }

        private void LateUpdate()
        {
            transform.position = cam.WorldToScreenPoint(new Vector3(target.transform.position.x + offset.x,
                   target.transform.position.y + offset.y, target.transform.position.z + offset.z));
        }

        public void Change_Target(GameObject _new_Target)
        {
            target = _new_Target;
            Update_Player_To_Use();

        }
    }
}
