using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Project.Scripts.Player;
using Project.Scripts.Misc;


namespace Project.Scripts.Manager
{
    public class Team_Manager : MonoBehaviour
    {

        public enum Status
        {
            FOLLOW_PLAYER,
            FOLLOW_ENEMY,
            DEFEND,


        };

        public int Team_ID, current_Status_ID = -1;
        public List<Player_Behaviour> teammates, enemy_Team;
        public List<Goal_Behaviour> goals;
        public Goal_Behaviour enemy_Goal;
        public Ball_Effects ball_Ref;
        public GameObject first_Target;
        public Status current_Status;
        public Transform max_Z, min_Z, min_X, max_X;


        private void Awake()
        {
            foreach (Player_Behaviour g in Resources.FindObjectsOfTypeAll(typeof(Player_Behaviour)))
            {
                if (g.GetComponent<Player_Behaviour>().Team_ID == this.Team_ID)
                {
                    teammates.Add(g);
                }

            }

            foreach (Player_Behaviour g in Resources.FindObjectsOfTypeAll(typeof(Player_Behaviour)))
            {
                if (g.GetComponent<Player_Behaviour>().Team_ID != this.Team_ID)
                {
                    enemy_Team.Add(g);
                }
            }

            foreach (Goal_Behaviour g in Resources.FindObjectsOfTypeAll(typeof(Goal_Behaviour)))
            {
                if (g.team_ID == this.Team_ID)
                {
                    goals.Add(g);
                }
                else
                {
                    enemy_Goal = g;
                }
            }

            for (int i = 0; i < teammates.Count; i++)
            {
                teammates[i].team_Manager = this;
                teammates[i].goal = goals[0];

                if(enemy_Goal != null)
                    teammates[i].enemy_Goal = enemy_Goal;

                teammates[i].Set_Min_Max(min_Z.position.z, max_Z.position.z, min_X.position.x, max_X.position.x);
                teammates[i].ball_reference = ball_Ref;

                if (teammates[i].player_Controlled)
                {
                    first_Target = teammates[i].gameObject;
                }
            }

            Change_Status(first_Target);





        }

        public void Ball_Pickup()
        {
            for (int i = 0; i < teammates.Count; i++)
            {
                //teammates[i].Random_Target_Pos(enemy_Team[i].transform);
            }

            for (int i = 0; i < enemy_Team.Count; i++)
            {
                //enemy_Team[i].Defend_Goal_Pos(teammates[i].transform);
            }
        }

        public void Ball_Drop()
        {
            for (int i = 0; i < teammates.Count; i++)
            {
                //teammates[i].Go_To_Ball();
            }

            for (int i = 0; i < enemy_Team.Count; i++)
            {
                //enemy_Team[i].Go_To_Ball();
            }
        }

        /// <summary>
        /// Change the current status of AI on player's team.
        /// </summary>
        /// <param name="_new_Status">0 = follow, 1 = Defend</param>
        public void Change_Status(GameObject _sending_Gameobject)
        {
            current_Status_ID++;

            int _new_Status = current_Status_ID;

            if (_new_Status > (int)Status.DEFEND)
            {
                current_Status_ID = 0;
                _new_Status = current_Status_ID;
            }

            switch (_new_Status)
            {
                case 2:
                    current_Status = Status.DEFEND;
                    Change_All_Status(_new_Status, _sending_Gameobject);
                    break;
                case 1:
                    Change_All_Status(_new_Status, _sending_Gameobject);
                    current_Status = Status.FOLLOW_ENEMY;
                    break;
                default:
                    Change_All_Status(_new_Status, _sending_Gameobject);
                    current_Status = Status.FOLLOW_PLAYER;
                    break;
            }

            Debug.Log("New status is: " + current_Status);

        }

        void Change_All_Status(int _status_ID, GameObject sending_Gameobject)
        {
            switch (_status_ID)
            {
                case 2:
                    for (int i = 0; i < teammates.Count; i++)
                    {
                        teammates[i].Update_Target_Pos(sending_Gameobject.transform);
                    }
                    break;
                case 1:
                    for (int i = 0; i < teammates.Count; i++)
                    {
                        teammates[i].Update_Target_Pos(enemy_Team[i].transform);
                    }
                    break;
                default:
                    for (int i = 0; i < teammates.Count; i++)
                    {
                        teammates[i].Update_Target_Pos(sending_Gameobject.transform);
                    }
                    break;
            }
        }

    }
}
