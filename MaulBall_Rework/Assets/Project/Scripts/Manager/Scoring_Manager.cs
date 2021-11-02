using Project.Scripts.Camera;
using Project.Scripts.Misc;
using Project.Scripts.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Project.Scripts.Manager
{
    public class Scoring_Manager : MonoBehaviour
    {

        public static Scoring_Manager sm_Inst;
        public GameObject ball;
        public List<Player_Behaviour> all_Players;
        public List<Vector3> all_Initial_Positions;
        public List<Goal_Behaviour> goals;

        public int[] team_ID = new int[2];
        private Vector3 m_ball_Initial_Pos;

        private void Awake()
        {
            sm_Inst = this;

            foreach (Player_Behaviour g in Resources.FindObjectsOfTypeAll(typeof(Player_Behaviour)))
            {
                all_Players.Add(g);
            }

            foreach (Goal_Behaviour g in Resources.FindObjectsOfTypeAll(typeof(Goal_Behaviour)))
            {
                goals.Add(g);
            }


            for (int i = 0; i < all_Players.Count; i++)
            {
                all_Initial_Positions.Add(all_Players[i].transform.position);
            }

            ball = GameObject.Find("Ball_Holder");

            if (ball == null)
            {
                Debug.LogAssertion("Ball not found");
            }

            m_ball_Initial_Pos = ball.transform.position;
        }



        public void Reset_All()
        {
            Debug.Log("Resetting Location of players");
            //reset player positions
            for (int i = 0; i < all_Players.Count; i++)
            {
                all_Players[i].transform.position = all_Initial_Positions[i];
                all_Players[i].m_Owned_Ball = null;
                all_Players[i].current_Status = Player_Behaviour.status.IDLE;
            }

            ball.transform.parent = null;
            ball.transform.position = m_ball_Initial_Pos;
            ball.GetComponent<Collider>().enabled = true;
            ball.GetComponent<Rigidbody>().isKinematic = false;
            ball.GetComponent<Rigidbody>().useGravity = true;
            ball.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            Camera_Behaviour.cam_Inst.Reset_Target();
            //change location of goal 

        }

        public void Add_Score(int _Team_ID, GameObject _accessing_Gameobject)
        {
            team_ID[_Team_ID - 1] += 1;

            Reset_All();
            _accessing_Gameobject.GetComponent<Goal_Behaviour>().Choose_New_Location();
            for (int i = 0; i < goals.Count; i++)
            {
                goals[i].Reset_Timer();
            }
        }

    }
}
