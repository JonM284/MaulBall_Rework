using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;
using Project.Scripts.Misc;
using Project.Scripts.Camera;
using Project.Scripts.Manager;

namespace Project.Scripts.Player
{
    public class Player_Behaviour : MonoBehaviour
    {


        public float speed, gravity, sprint_speed_Mod, rot_Mod;
        public float ball_Force;
        public int Player_ID, Team_ID;

        public Transform Ball_Held_Pos;
        public bool player_Controlled;
        public Vector3 vel;
        public TrailRenderer running_Trail;

        [Header("Ray variables")]
        [SerializeField] private float ground_Ray_Rad;
        [SerializeField] private float ground_Ray_Dist, m_ball_Check_Radius, m_Melee_Range;

        [Header("Passing Variables")]
        public float reg_Pass_H_Offset;
        public float lob_Pass_H_Offset, Pass_Angle;

        [Header("Player Variables")]
        public float tackle_Dur_Max;
        public float slide_Tackle_Dur_Max, attack_Speed_Cooldown_Max, pass_Range, Input_Speed, slide_Tackle_Speed_Mod,
            tackle_Speed_Mod, damage_Cooldown_Max, tackle_Damage_Cooldown = 0.5f, slide_Tackle_Damage_Cooldown = 1.5f;

        public List<Player_Behaviour> accept_Teammates, passable_Teammates;

        [HideInInspector] public Team_Manager team_Manager;
        [HideInInspector] public Goal_Behaviour goal, enemy_Goal;
        [HideInInspector] public Ball_Effects ball_reference;
        [HideInInspector] public UI_Follower my_Indicator;

        private Rigidbody rb;
        private float m_speed_Modifier = 1;
        private Vector3 rayDir;
        private float m_Input_X, m_Input_Y, m_Horizontal_Comp, m_Vertical_Comp, m_anti_Bump_Factor = 0.75f;
        private float min_Z, max_Z, min_X, max_X;
        private float m_Ball_Throw_Cooldown = 0.5f, m_Orig_Cooldown, m_Tackle_Duration, m_Slide_Tackle_Duration
            , m_original_Speed, m_Attack_Speed_Cooldown = 1f, m_Time_To_Reach, m_Damage_Cooldown, m_DC_Max_Original, m_Electric_Damage_Cooldown,
             m_Dash_Duration, m_Current_Dash_Duration, m_invulnerability_Dur, m_Cur_Invul_Dur, m_Stun_Duration, m_Cur_Stun_Dur;

        [HideInInspector] public GameObject m_Owned_Ball;
        private Rewired.Player m_Player;

        private bool m_can_Catch_Ball = true, m_Is_Holding_Lob = false, m_Is_Tackling = false, m_Is_Slide_Tackling = false
            , m_Read_Player_Inputs = true, m_Has_Attacked = false, m_Is_Attacking = false, m_Taking_Damage = false, m_Is_Dashing = false;

        [HideInInspector] public bool m_Can_Be_Attacked = true, m_Using_Prox_Ultimate = false, m_Is_Being_Stunned = false;

        private ParticleSystem impact_PS, Electrify_PS;

        [SerializeField] private bool m_Is_Moving, m_Is_Being_Passed_To = false;

        private Vector3 m_Ball_End_Position, damage_Dir;

        //AI Variables
        [HideInInspector] public Transform target_Pos, saved_Target_Pos;
        public float random_Offset;
        private Vector3 m_random_Offset, target_Vec;

        //ABILITY VARIABLES
        [HideInInspector]
        public float[] ability_Duration = new float[3];
        [HideInInspector]
        public float[] ability_Effect_Duration = new float[3];
        [HideInInspector]
        public float[] ability_Cooldown = new float[3];
        [HideInInspector]
        public float[] ability_Repeater_Time = new float[3];
        [HideInInspector]
        public float[] ability_Current_Duration = new float[3];
        [HideInInspector]
        public int[] ability_Type_ID = new int[3];
        public Ability[] all_Abilities = new Ability[3];
        public float proximity_Ability_Range;
        public enum status
        {
            IDLE,
            ATTACK,
            DEFEND,
            BALL
        }

        public status current_Status;

        private void Awake()
        {
            foreach (Player_Behaviour g in Resources.FindObjectsOfTypeAll(typeof(Player_Behaviour)))
            {
                if (g.GetComponent<Player_Behaviour>().Team_ID == this.Team_ID && g.GetInstanceID() != this.GetInstanceID())
                {
                    accept_Teammates.Add(g);
                }
            }

            //impact_PS = transform.Find("Hit_Effect").GetComponent<ParticleSystem>();
            //Electrify_PS = transform.Find("Electrified_Particles").GetComponent<ParticleSystem>();
            Stop_Shock_Particles();

            if (!player_Controlled)
            {
                Player_ID = 8;
            }

            //TEMPORARY
            transform.name = "Test_Player_Team_" + Team_ID;
        }

        // Use this for initialization
        void Start()
        {
            rb = GetComponent<Rigidbody>();
            m_Player = ReInput.players.GetPlayer(Player_ID - 1);
            m_Orig_Cooldown = m_Ball_Throw_Cooldown;
            m_original_Speed = speed;
            m_Attack_Speed_Cooldown = attack_Speed_Cooldown_Max;
            m_DC_Max_Original = damage_Cooldown_Max;
            m_Electric_Damage_Cooldown = damage_Cooldown_Max + 1.5f;

            for (int i = 0; i < all_Abilities.Length; i++)
            {
                if (all_Abilities[i] != null)
                {
                    all_Abilities[i].SetUp_Ability(this, i, this.GetInstanceID());
                }
            }
        }

        private void FixedUpdate()
        {
            if (m_Read_Player_Inputs)
            {
                Movement();
            }
            else
            {
                Debug.Log($"<color=cyan>Damage?{m_Taking_Damage} Stunned?{m_Is_Being_Stunned} Dashing?{m_Is_Dashing} Name:{gameObject.name}</color>");
                if (m_Is_Dashing && !m_Is_Being_Stunned && !m_Taking_Damage)
                {
                    Do_Dash(transform.forward);
                }
                else if (m_Taking_Damage && !m_Is_Being_Stunned)
                {
                    Do_Dash(damage_Dir);
                    Debug.Log($"<color=cyan>Taking damage {damage_Dir} {gameObject.name}</color>");
                }
                else if (m_Is_Being_Stunned)
                {
                    m_Horizontal_Comp = 0;
                    m_Vertical_Comp = 0;
                    Debug.Log("<color=yellow>Stunned</color>");
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (player_Controlled)
            {
                m_Horizontal_Comp = m_Player.GetAxisRaw("Horizontal");
                m_Vertical_Comp = m_Player.GetAxisRaw("Vertical");
            }
            else
            {
                AI_Movement();
            }

            Check_Inputs();
            Check_Cooldowns();

        }




        void Movement()
        {

            m_Input_X = Mathf.Lerp(m_Input_X, m_Horizontal_Comp, Time.deltaTime * Input_Speed);
            m_Input_Y = Mathf.Lerp(m_Input_Y, m_Vertical_Comp, Time.deltaTime * Input_Speed);


            vel.x = m_Input_X * speed;
            vel.z = m_Input_Y * speed;


            if (!m_Is_Grounded())
            {
                vel.y -= gravity * Time.deltaTime;
            }
            else
            {
                vel.y = 0;
            }


            m_speed_Modifier = (m_Player.GetButton("Sprint") && m_Owned_Ball != null) ? sprint_speed_Mod : 1;

            if (running_Trail != null)
            {
                running_Trail.enabled = (m_Player.GetButton("Sprint") && m_Owned_Ball != null) ? true : false;
            }


            ///change forward direction
            Vector3 tempDir = new Vector3(m_Input_X, 0, m_Input_Y);


            if (tempDir.magnitude > 0.1f)
            {
                rayDir = tempDir.normalized;
            }
            if (m_Is_Grounded())
            {
                transform.forward = Vector3.Slerp(transform.forward, rayDir, Time.deltaTime * rot_Mod);
            }

            if (player_Controlled)
            {
                Find_Players_In_Range(rayDir);
            }

            rb.MovePosition(rb.position + new Vector3(Mathf.Clamp(vel.x, -speed, speed),
                vel.y, Mathf.Clamp(vel.z, -speed, speed)) * m_speed_Modifier * Time.deltaTime);


        }

        public void Do_Dash(Vector3 _dash_Dir)
        {
            vel.x = _dash_Dir.x * speed;
            vel.z = _dash_Dir.z * speed;

            rb.MovePosition(rb.position + new Vector3(Mathf.Clamp(vel.x, -speed, speed),
                vel.y, Mathf.Clamp(vel.z, -speed, speed)) * Time.deltaTime);
        }



        void Check_Inputs()
        {
            //press w/o ball to pass, press w/ ball to swap players
            if (m_Player.GetButtonDown("S_Pass") && m_Owned_Ball != null && !m_Wall_In_Front())
            {
                //throw ball at closer teammate
                Throw_Ball();
            }
            else if (m_Player.GetButtonDown("S_Pass") && m_Owned_Ball == null && player_Controlled)
            {
                //Swap players
                Swap_Players();
            }

            // press to steal
            if (m_Player.GetButtonDown("D_Tackle") && m_Owned_Ball == null && player_Controlled && !m_Taking_Damage && !m_Has_Attacked)
            {
                //attempt to steal
                Tackle();
            }

            //hold to preform a lob pass
            m_Is_Holding_Lob = (m_Player.GetButton("S_Lob") && m_Owned_Ball != null) ? true : false;

            //press without ball to preform a MAUL
            if (m_Player.GetButtonDown("S_Lob") && !m_Player.GetButtonDown("Sprint") && m_Owned_Ball == null && player_Controlled && !m_Taking_Damage && !m_Has_Attacked)
            {
                //Maul
                Slide_Tackle();
            }

            /*if (m_Player.GetButtonSinglePressDown("Sprint") && player_Controlled)
            {
                team_Manager.Change_Status(this.gameObject);
            }*/

            //press to use primary ability
            if (m_Player.GetButtonDown("Ability_1") && !m_Player.GetButtonDown("Ability_2") && !m_Taking_Damage)
            {
                //Do ability
                Do_Ability(0);
                //Start Cooldown
            }

            //press to use secondary ability
            if (m_Player.GetButtonDown("Ability_2") && !m_Player.GetButtonDown("Ability_1") && !m_Taking_Damage)
            {
                //Do ability
                Do_Ability(1);
                //Start Cooldown
            }

            if (m_Player.GetButtonDown("Ability_1") && m_Player.GetButtonDown("Ability_2"))
            {
                Do_Ability(2);
            }

            //check whether or not player is moving
            if (Mathf.Abs(m_Horizontal_Comp) > 0.1f || Mathf.Abs(m_Vertical_Comp) > 0.1f)
            {
                m_Is_Moving = true;
            }
            else if (m_Horizontal_Comp == 0 && m_Vertical_Comp == 0)
            {
                m_Is_Moving = false;
            }


        }

        void Check_Cooldowns()
        {
            if (!m_can_Catch_Ball && m_Ball_Throw_Cooldown > 0)
            {
                m_Ball_Throw_Cooldown -= Time.deltaTime;
            }

            if (m_Ball_Throw_Cooldown <= 0 && !m_can_Catch_Ball)
            {
                m_can_Catch_Ball = true;
                m_Ball_Throw_Cooldown = m_Orig_Cooldown;
            }

            if (!m_Read_Player_Inputs && m_Is_Dashing && m_Current_Dash_Duration < m_Dash_Duration)
            {
                m_Current_Dash_Duration += Time.deltaTime;
            }

            if (!m_Read_Player_Inputs && m_Is_Dashing && m_Current_Dash_Duration >= m_Dash_Duration)
            {
                m_Current_Dash_Duration = 0;
                Reset_Dash_Variables();
            }

            if (!m_Can_Be_Attacked && m_Cur_Invul_Dur < m_invulnerability_Dur)
            {
                m_Cur_Invul_Dur += Time.deltaTime;
            }

            if (!m_Can_Be_Attacked && m_Cur_Invul_Dur >= m_invulnerability_Dur)
            {
                m_Can_Be_Attacked = true;
                m_Cur_Invul_Dur = 0;
            }

            /* if (m_Slide_Tackle_Duration <= slide_Tackle_Dur_Max && m_Is_Slide_Tackling)
             {
                 m_Slide_Tackle_Duration += Time.deltaTime;
             }
             if ((m_Slide_Tackle_Duration >= slide_Tackle_Dur_Max && m_Is_Slide_Tackling) || (m_Wall_In_Front() && m_Is_Slide_Tackling))
             {
                 m_Slide_Tackle_Duration = 0;
                 m_Is_Slide_Tackling = false;
                 m_Read_Player_Inputs = true;
                 Slow_Speed();
             }
             if (m_Tackle_Duration <= tackle_Dur_Max && m_Is_Tackling)
             {
                 m_Tackle_Duration += Time.deltaTime;
             }
             if ((m_Tackle_Duration >= tackle_Dur_Max && m_Is_Tackling) || (m_Wall_In_Front() && m_Is_Tackling))
             {
                 m_Tackle_Duration = 0;
                 m_Is_Tackling = false;
                 m_Read_Player_Inputs = true;
                 Slow_Speed();
             }*/

            if (m_Attack_Speed_Cooldown >= 0 && m_Has_Attacked)
            {
                m_Attack_Speed_Cooldown -= Time.deltaTime;
            }

            if (m_Attack_Speed_Cooldown <= 0 && m_Has_Attacked)
            {
                Reset_Speed();
            }


            if (!player_Controlled && m_Is_Being_Passed_To && m_Time_To_Reach > 0)
            {
                m_Time_To_Reach -= Time.deltaTime;
                Vector3 dir_To_Ball = m_Ball_End_Position - transform.position;
                rayDir.x = dir_To_Ball.normalized.x;
                rayDir.z = dir_To_Ball.normalized.z;
                Debug.Log("Moving Dir: " + rayDir);
            }

            if (m_Time_To_Reach <= 0 && !player_Controlled && m_Is_Being_Passed_To)
            {
                player_Controlled = true;
                m_Is_Being_Passed_To = false;
            }

            if (m_Damage_Cooldown <= damage_Cooldown_Max && m_Taking_Damage)
            {
                m_Damage_Cooldown += Time.deltaTime;
                Debug.Log($"<color=yellow>Speed:{speed} TakingDamage{m_Taking_Damage} PlayerInput:{m_Read_Player_Inputs} Stunned:{m_Is_Being_Stunned} {gameObject.name}</color>");
                if (!m_Wall_In_Damage_Dir())
                {
                    float prc = m_Damage_Cooldown / damage_Cooldown_Max;
                    speed = Mathf.Lerp(speed, 0, prc);

                }
                else
                {
                    Hault_Speed();
                    damage_Cooldown_Max = m_Electric_Damage_Cooldown;
                    if (Electrify_PS != null && !Electrify_PS.isPlaying)
                    {
                        Play_Shock_Particles();
                    }
                }
            }

            if (m_Damage_Cooldown >= damage_Cooldown_Max && m_Taking_Damage)
            {
                m_Damage_Cooldown = 0;
                if (damage_Cooldown_Max != m_DC_Max_Original) damage_Cooldown_Max = m_DC_Max_Original;
                m_Taking_Damage = false;
                m_Read_Player_Inputs = true;
                Slow_Speed();
                if (Electrify_PS != null && Electrify_PS.isPlaying && !m_Is_Being_Stunned)
                {
                    Stop_Shock_Particles();
                }
            }

            if (m_Cur_Stun_Dur < m_Stun_Duration && m_Is_Being_Stunned)
            {
                m_Cur_Stun_Dur += Time.deltaTime;
                Hault_Speed();
            }

            if (m_Cur_Stun_Dur >= m_Stun_Duration && m_Is_Being_Stunned)
            {
                Reset_Stun();
            }

            if (m_Using_Prox_Ultimate && ability_Current_Duration[2] < ability_Duration[2])
            {
                ability_Current_Duration[2] += Time.deltaTime;
                Stun_Prox_Players();
            }

            if (m_Using_Prox_Ultimate && ability_Current_Duration[2] >= ability_Duration[2])
            {
                m_Using_Prox_Ultimate = false;
                ability_Current_Duration[2] = 0;

            }

        }

        public void Hault_Speed()
        {
            speed = 0;
        }

        public void Slow_Speed()
        {
            m_Has_Attacked = true;
            m_Is_Attacking = false;
            float _Slowed_Speed = m_original_Speed / 3f;
            speed = _Slowed_Speed;

            m_Attack_Speed_Cooldown = attack_Speed_Cooldown_Max;
        }

        public void Reset_Speed()
        {
            speed = m_original_Speed;
            m_Has_Attacked = false;
        }

        public void Initiate_Stun(float _Stun_Duration)
        {
            m_Stun_Duration = _Stun_Duration;
            m_Read_Player_Inputs = false;
            m_Is_Being_Stunned = true;
            Hault_Speed();
            Play_Shock_Particles();
        }

        /// <summary>
        /// Reset variable
        /// </summary>
        private void Reset_Stun()
        {
            m_Read_Player_Inputs = true;
            m_Is_Being_Stunned = false;
            m_Cur_Stun_Dur = 0;
            Reset_Speed();
            Stop_Shock_Particles();
        }

        public void Initiate_Invulnerability(bool _Is_Player_Invulnerable, float _Invul_Duration)
        {
            m_invulnerability_Dur = _Invul_Duration;
            m_Can_Be_Attacked = !_Is_Player_Invulnerable;

        }

        public void Initiate_Dash_Type(bool _Slide_Tackle, bool _Steal, bool _Ability_Use, float _duration, float _speed)
        {
            m_Is_Slide_Tackling = _Slide_Tackle;
            m_Is_Tackling = _Steal;
            m_Is_Dashing = true;
            if (m_Is_Slide_Tackling || m_Is_Tackling)
            {
                m_Is_Attacking = true;
            }
            else
            {
                m_Is_Attacking = false;
            }

            m_Read_Player_Inputs = false;
            m_Dash_Duration = _duration;
            speed = _speed;
        }

        void Reset_Dash_Variables()
        {
            m_Is_Slide_Tackling = false;
            m_Is_Tackling = false;
            m_Is_Dashing = false;
            m_Read_Player_Inputs = true;
            Slow_Speed();
        }

        void Tackle()
        {
            float _tackle_Speed = m_original_Speed * tackle_Speed_Mod;
            Initiate_Dash_Type(false, true, false, tackle_Dur_Max, _tackle_Speed);
        }

        void Slide_Tackle()
        {
            float _tackle_Speed = m_original_Speed * slide_Tackle_Speed_Mod;
            Initiate_Dash_Type(true, false, false, slide_Tackle_Dur_Max, _tackle_Speed);
        }

        public void Swap_Possessor(GameObject _new_Owner)
        {
            m_Owned_Ball.transform.parent = null;
            _new_Owner.GetComponent<Player_Behaviour>().m_Owned_Ball = this.m_Owned_Ball;
            m_Owned_Ball.transform.parent = _new_Owner.GetComponent<Player_Behaviour>().Ball_Held_Pos;
            m_Owned_Ball.transform.position = _new_Owner.GetComponent<Player_Behaviour>().Ball_Held_Pos.position;
            m_Owned_Ball = null;

            Camera_Behaviour.cam_Inst.Update_Target(_new_Owner.transform);
        }

        Vector3 Find_Players_In_Range(Vector3 _dir)
        {
            if (m_Owned_Ball != null)
            {
                for (int i = 0; i < accept_Teammates.Count; i++)
                {
                    Vector3 dir_To_Team = accept_Teammates[i].transform.position - transform.position;
                    float angle = Vector3.Angle(_dir, dir_To_Team);

                    if (angle < Pass_Angle)
                    {
                        Debug.Log("Found Pass Match");
                        Debug.DrawRay(transform.position, dir_To_Team, Color.red);
                        if (!passable_Teammates.Contains(accept_Teammates[i]))
                        {
                            passable_Teammates.Add(accept_Teammates[i]);
                            return passable_Teammates[0].transform.position;
                        }

                    }
                    else if (angle > Pass_Angle && passable_Teammates.Contains(accept_Teammates[i]))
                    {
                        passable_Teammates.Remove(accept_Teammates[i]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < accept_Teammates.Count; i++)
                {
                    Vector3 dir_To_Team = accept_Teammates[i].transform.position - transform.position;
                    float angle = Vector3.Angle(_dir, dir_To_Team);

                    if (angle < Pass_Angle && !accept_Teammates[i].player_Controlled)
                    {
                        Debug.Log("Found Swap Match");
                        Debug.DrawRay(transform.position, dir_To_Team, Color.red);
                        if (!passable_Teammates.Contains(accept_Teammates[i]))
                        {
                            passable_Teammates.Add(accept_Teammates[i]);
                            return passable_Teammates[0].transform.position;
                        }

                    }
                    else if (angle > Pass_Angle && passable_Teammates.Contains(accept_Teammates[i]))
                    {
                        passable_Teammates.Remove(accept_Teammates[i]);
                    }
                }
            }

            return transform.forward;
        }

        void Swap_Players()
        {

            if (passable_Teammates.Count >= 2)
            {
                float angle1 = Vector3.Angle(rayDir, passable_Teammates[0].transform.position - transform.position);
                float angle2 = Vector3.Angle(rayDir, passable_Teammates[1].transform.position - transform.position);
                if (angle2 < angle1)
                {
                    passable_Teammates[0] = passable_Teammates[1];
                }
            }

            if (passable_Teammates.Count > 0)
            {
                player_Controlled = false;
                passable_Teammates[0].player_Controlled = true;
                passable_Teammates[0].Player_ID = this.Player_ID;
                passable_Teammates[0].Update_Player_ID();
                //ToDo: Implement indicators
                //my_Indicator.Change_Target(passable_Teammates[0].gameObject);
                my_Indicator = null;
                Player_ID = 8;
                Update_Player_ID();
            }

            if (passable_Teammates.Count > 0)
            {
                for (int i = 0; i < passable_Teammates.Count; i++)
                {
                    passable_Teammates.Remove(passable_Teammates[i]);
                }
            }
        }

        /// <summary>
        /// apply force to the ball
        /// </summary>
        void Throw_Ball()
        {
            m_can_Catch_Ball = false;
            m_Owned_Ball.transform.parent = null;
            if (!m_Taking_Damage)
            {
                m_Owned_Ball.GetComponent<Ball_Effects>().Play_Catch_Kick();
            }
            m_Owned_Ball.GetComponent<Rigidbody>().isKinematic = false;
            float _mag = 0;

            if (passable_Teammates.Count > 0)
            {
                if (passable_Teammates.Count >= 2)
                {
                    float dist1 = Vector3.Magnitude(passable_Teammates[0].transform.position - transform.position);
                    float dist2 = Vector3.Magnitude(passable_Teammates[1].transform.position - transform.position);
                    if (dist2 < dist1)
                    {
                        passable_Teammates[0] = passable_Teammates[1];
                    }
                }
                _mag = Vector3.Magnitude(passable_Teammates[0].transform.position - transform.position);
            }
            Debug.Log("Dist: " + _mag);

            if ((passable_Teammates.Count <= 0 || _mag > pass_Range) && !m_Taking_Damage)
            {
                m_Owned_Ball.GetComponent<Rigidbody>().AddForce(transform.forward * ball_Force * m_speed_Modifier);
            }
            else if (passable_Teammates.Count > 0 && _mag < pass_Range && !m_Taking_Damage)
            {
                Physics.gravity = Vector3.up * -gravity;
                m_Owned_Ball.GetComponent<Rigidbody>().AddForce(Calc_Vel(), ForceMode.VelocityChange);


                if (!passable_Teammates[0].player_Controlled && passable_Teammates[0].my_Indicator == null)
                {
                    player_Controlled = false;
                    passable_Teammates[0].player_Controlled = true;
                    passable_Teammates[0].Player_ID = this.Player_ID;
                    passable_Teammates[0].Update_Player_ID();
                    //ToDo:Add indicators
                    //my_Indicator.Change_Target(passable_Teammates[0].gameObject);
                    my_Indicator = null;
                    Player_ID = 8;
                    Update_Player_ID();
                }
            }
            else if (m_Taking_Damage)
            {
                Vector3 random_Dir = Vector3.zero;
                if (m_Wall_In_Damage_Dir())
                {
                    damage_Dir *= -1;
                    random_Dir = new Vector3(damage_Dir.x + Random.Range(-0.4f, 0.4f), Random.Range(0f, 0.5f), damage_Dir.z + Random.Range(-0.4f, 0.4f));
                }
                else
                {
                    random_Dir = new Vector3(damage_Dir.x + Random.Range(-0.4f, 0.4f), Random.Range(0f, 0.5f), damage_Dir.z + Random.Range(-0.4f, 0.4f));
                }
                float random_Force = Random.Range(ball_Force / 2, ball_Force);
                m_Owned_Ball.GetComponent<Rigidbody>().AddForce((random_Dir) * random_Force);
            }
            m_Owned_Ball.GetComponent<Collider>().enabled = true;
            m_Owned_Ball.GetComponent<Ball_Effects>().Activate_Trail();
            m_Owned_Ball.GetComponent<Rigidbody>().useGravity = true;
            m_Owned_Ball.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;

            m_Owned_Ball = null;

            if (passable_Teammates.Count > 0)
            {
                for (int i = 0; i < passable_Teammates.Count; i++)
                {
                    passable_Teammates.Remove(passable_Teammates[i]);
                }
            }
            Camera_Behaviour.cam_Inst.Reset_Target();
        }

        public void Passed_To(Vector3 _end_Pos, float _time)
        {
            m_Is_Being_Passed_To = true;
            m_Ball_End_Position = _end_Pos;
            m_Time_To_Reach = _time;
        }

        public void Take_Damage(Vector3 _attacker_Pos, float _damage_Cooldown, bool _Is_Stealing)
        {
            damage_Dir = (_attacker_Pos - transform.position) * -1f;

            float _damage_Speed = m_original_Speed * tackle_Speed_Mod * 2f;
            speed = _damage_Speed;
            m_Taking_Damage = true;
            m_Read_Player_Inputs = false;
            damage_Cooldown_Max = _damage_Cooldown;

            
            if (m_Owned_Ball != null)
            {
                if (!_Is_Stealing)
                {
                    Throw_Ball();
                    team_Manager.Ball_Drop();
                }
                Camera_Behaviour.cam_Inst.Do_Camera_Shake(0.3f, 0.6f);
            }
            else
            {
                Camera_Behaviour.cam_Inst.Do_Camera_Shake(0.1f, 0.2f);
            }

            if(impact_PS != null)
                       impact_PS.Play();
        }



        //this calculation was made with the help of this youtube video https://www.youtube.com/watch?v=IvT8hjy6q4o
        Vector3 Calc_Vel()
        {
            float h = 0f;
            if (m_Is_Holding_Lob)
            {
                h = passable_Teammates[0].transform.position.y + lob_Pass_H_Offset;
            }
            else
            {
                h = passable_Teammates[0].transform.position.y + reg_Pass_H_Offset;
            }
            float displacementY = passable_Teammates[0].transform.position.y - m_Owned_Ball.transform.position.y;

            Vector3 displacementXZ = new Vector3(passable_Teammates[0].transform.position.x - m_Owned_Ball.transform.position.x, 0,
            passable_Teammates[0].transform.position.z - m_Owned_Ball.transform.position.z)
            + vel;

            float _time = Mathf.Sqrt(-2 * h / -gravity) + Mathf.Sqrt(2 * (displacementY - h) / -gravity);
            Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * -gravity * h);
            Vector3 velocityXZ = displacementXZ / _time;


            return velocityXZ + velocityY;
        }

        public void Do_Ability(int ability_ID)
        {
            all_Abilities[ability_ID].Use_Ability(this.gameObject.GetInstanceID(), this);
        }


        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.tag == "Ball" && other.gameObject.transform.parent == null && m_can_Catch_Ball && !m_Taking_Damage)
            {
                Pick_Up_Ball(other.gameObject);
                //ToDo: Add team manager
                //team_Manager.Ball_Pickup();
            }


            if (other.gameObject.tag == "Player" && (m_Is_Tackling || m_Is_Slide_Tackling))
            {
                if (other.gameObject.GetComponent<Player_Behaviour>().m_Can_Be_Attacked)
                {
                    if (m_Is_Tackling)
                    {
                        other.gameObject.GetComponent<Player_Behaviour>().Take_Damage(transform.position, tackle_Damage_Cooldown, true);
                        if (other.gameObject.GetComponent<Player_Behaviour>().m_Owned_Ball != null)
                        {
                            other.gameObject.GetComponent<Player_Behaviour>().Swap_Possessor(this.gameObject);
                        }
                        //team_Manager.Ball_Pickup();
                    }
                    else if (m_Is_Slide_Tackling)
                    {
                        other.gameObject.GetComponent<Player_Behaviour>().Take_Damage(transform.position, slide_Tackle_Damage_Cooldown, false);
                        m_can_Catch_Ball = false;

                    }
                }


            }

            rb.velocity = Vector3.zero;
        }

        private void OnCollisionStay(Collision other)
        {
            if (other.gameObject.tag == "Ball" && other.gameObject.transform.parent == null && m_can_Catch_Ball && !m_Taking_Damage)
            {
                Pick_Up_Ball(other.gameObject);
                team_Manager.Ball_Pickup();
            }

            if (other.gameObject.tag == "Player" && (m_Is_Tackling || m_Is_Slide_Tackling)
                && !other.gameObject.GetComponent<Player_Behaviour>().m_Taking_Damage)
            {
                if (other.gameObject.GetComponent<Player_Behaviour>().m_Can_Be_Attacked)
                {
                    if (m_Is_Tackling)
                    {
                        other.gameObject.GetComponent<Player_Behaviour>().Take_Damage(transform.position, tackle_Damage_Cooldown, true);
                        if (other.gameObject.GetComponent<Player_Behaviour>().m_Owned_Ball != null)
                        {
                            other.gameObject.GetComponent<Player_Behaviour>().Swap_Possessor(this.gameObject);
                        }
                        team_Manager.Ball_Pickup();
                    }
                    else if (m_Is_Slide_Tackling)
                    {
                        other.gameObject.GetComponent<Player_Behaviour>().Take_Damage(transform.position, slide_Tackle_Damage_Cooldown, false);
                        m_can_Catch_Ball = false;

                    }
                }


            }
        }

        private void OnCollisionExit(Collision collision)
        {
            rb.velocity = Vector3.zero;
        }


        void Stun_Prox_Players()
        {
            Collider[] hit_Colliders = Physics.OverlapSphere(transform.position, proximity_Ability_Range);
            int i = 0;
            while (i < hit_Colliders.Length)
            {
                if (hit_Colliders[i].tag == "Player" && hit_Colliders[i].gameObject.GetComponent<Player_Behaviour>().Team_ID != Team_ID)
                {
                    hit_Colliders[i].GetComponent<Player_Behaviour>().Initiate_Stun(ability_Effect_Duration[2]);
                }
                i++;
            }
        }

        void Pick_Up_Ball(GameObject other)
        {
            if (!player_Controlled)
            {
                player_Controlled = true;
            }

            for (int i = 0; i < accept_Teammates.Count; i++)
            {
                if (Player_ID == 8)
                {
                    if (accept_Teammates[i].Player_ID != 8 && accept_Teammates[i].player_Controlled && accept_Teammates[i].my_Indicator != null)
                    {
                        Player_ID = accept_Teammates[i].Player_ID;
                        Update_Player_ID();
                        accept_Teammates[i].Player_ID = 8;
                        accept_Teammates[i].Update_Player_ID();
                        accept_Teammates[i].player_Controlled = false;
                        my_Indicator = accept_Teammates[i].my_Indicator;
                        accept_Teammates[i].my_Indicator.Change_Target(this.gameObject);
                        accept_Teammates[i].my_Indicator = null;
                    }
                }
            }
            m_Owned_Ball = other.gameObject;
            m_Owned_Ball.GetComponent<Collider>().enabled = false;
            m_Owned_Ball.GetComponent<Rigidbody>().useGravity = false;
            m_Owned_Ball.transform.parent = Ball_Held_Pos;
            m_Owned_Ball.transform.position = Ball_Held_Pos.position;
            m_Owned_Ball.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
            m_Owned_Ball.GetComponent<Rigidbody>().isKinematic = true;

            m_Owned_Ball.GetComponent<Ball_Effects>().Deactivate_Trail();
            m_Owned_Ball.GetComponent<Ball_Effects>().Play_Catch_Kick();
            Camera_Behaviour.cam_Inst.Update_Target(this.transform);
        }



        bool m_Is_Grounded()
        {
            RaycastHit hit;
            if (Physics.SphereCast(transform.position, ground_Ray_Rad, Vector3.down, out hit, ground_Ray_Dist))
            {
                if (hit.collider.tag == "Ground")
                {
                    return true;
                }
            }
            return false;


        }

        bool m_Wall_In_Front()
        {
            RaycastHit hit;
            if (Physics.SphereCast(transform.position, ground_Ray_Rad * 1.5f, transform.forward, out hit, ground_Ray_Dist))
            {
                if (hit.collider.tag == "Wall")
                {
                    return true;
                }
            }
            return false;
        }

        bool m_Wall_In_Damage_Dir()
        {
            RaycastHit hit;
            if (Physics.SphereCast(transform.position, ground_Ray_Rad, damage_Dir, out hit, ground_Ray_Dist))
            {
                if (hit.collider.tag == "Wall")
                {
                    return true;
                }
            }
            return false;
        }

        //check to see if there is a player to cover near the AI
        bool m_Player_In_Prox()
        {
            Collider[] hit_Colliders = Physics.OverlapSphere(transform.position, m_Melee_Range);
            int i = 0;
            while (i < hit_Colliders.Length)
            {
                if (hit_Colliders[i].tag == "Player" && hit_Colliders[i].gameObject.GetComponent<Player_Behaviour>().Team_ID != Team_ID)
                {

                    return true;
                }
                i++;
            }

            return false;
        }

        //check to see if the ball is near the AI
        bool m_Ball_In_Prox()
        {
            Collider[] hit_Colliders = Physics.OverlapSphere(transform.position, m_ball_Check_Radius);
            int i = 0;
            while (i < hit_Colliders.Length)
            {
                if (hit_Colliders[i].tag == "Ball")
                {
                    target_Pos = hit_Colliders[i].transform;
                    target_Vec = new Vector3(target_Pos.position.x, transform.position.y, target_Pos.position.y);
                    return true;
                }
                i++;
            }
            return false;
        }

        //check to see if the ball is in a "Maulable" range
        bool m_In_Melee_Range()
        {
            Collider[] hit_Colliders = Physics.OverlapSphere(transform.position, m_Melee_Range);
            int i = 0;
            while (i < hit_Colliders.Length)
            {
                if (hit_Colliders[i].tag == "Player" && hit_Colliders[i].gameObject.GetComponent<Player_Behaviour>().m_Owned_Ball != null)
                {
                    Direct_Enemy_Target(hit_Colliders[i].transform);
                    return true;
                }
                i++;
            }

            return false;
        }

        public void Update_Player_ID()
        {
            m_Player = ReInput.players.GetPlayer(Player_ID - 1);
        }

        public void Direct_Enemy_Target(Transform _target_Transform)
        {
            target_Pos = _target_Transform;
            saved_Target_Pos = _target_Transform;
            Debug.Log("Now updated to be player position");
            target_Vec = target_Pos.transform.position;
        }

        public void Random_Target_Pos(Transform _target_Transform)
        {
            target_Pos = _target_Transform;
            saved_Target_Pos = _target_Transform;
            target_Vec = new Vector3(Random.Range(target_Pos.position.x, enemy_Goal.transform.position.x), 0, Random.Range(min_Z, max_Z));
            current_Status = status.ATTACK;
        }

        public void Defend_Goal_Pos(Transform _target_Transform)
        {
            target_Pos = _target_Transform;
            saved_Target_Pos = _target_Transform;
            target_Vec = target_Pos.transform.position + (new Vector3(Random.insideUnitSphere.x,
                0, Random.insideUnitSphere.z) * random_Offset / 1.5f);
            if ((target_Vec.z <= min_Z || target_Vec.z >= max_Z) || (target_Vec.x <= min_X || target_Vec.x >= max_X))
            {
                Debug.Log("Z position was outside of arena");
                Defend_Goal_Pos(_target_Transform);
            }
            current_Status = status.DEFEND;

        }

        public void Go_To_Ball()
        {
            target_Pos = ball_reference.transform;
            target_Vec = target_Pos.transform.position;
            current_Status = status.BALL;
        }

        public void Set_Min_Max(float _min_Z, float _max_Z, float _min_X, float _max_X)
        {
            min_Z = _min_Z;
            max_Z = _max_Z;
            min_X = _min_X;
            max_X = _max_X;
        }


        void AI_Movement()
        {
            if (current_Status == status.ATTACK)
            {

                if (Vector3.Magnitude(target_Vec - transform.position) > 4)
                {
                    Vector3 _dir_To_Target = target_Vec - transform.position;
                    Vector3 _Norm_Dir = _dir_To_Target.normalized;
                    m_Horizontal_Comp = _Norm_Dir.x;
                    m_Vertical_Comp = _Norm_Dir.z;
                }
                else
                {
                    Random_Target_Pos(target_Pos);
                }
            }
            else if (current_Status == status.DEFEND && !m_In_Melee_Range() && !m_Taking_Damage && !m_Is_Being_Stunned)
            {
                if (Vector3.Magnitude(target_Vec - transform.position) > 4)
                {
                    Vector3 _dir_To_Target = target_Vec - transform.position;
                    Vector3 _Norm_Dir = _dir_To_Target.normalized;
                    m_Horizontal_Comp = _Norm_Dir.x;
                    m_Vertical_Comp = _Norm_Dir.z;
                }
                else
                {
                    Defend_Goal_Pos(target_Pos);
                }
            }
            else if (current_Status == status.DEFEND && m_In_Melee_Range() && !m_Is_Attacking && !m_Has_Attacked && !m_Taking_Damage && !m_Is_Being_Stunned)
            {
                Vector3 _dir_To_Target = target_Vec - transform.position;
                Vector3 _Norm_Dir = _dir_To_Target.normalized;
                m_Horizontal_Comp = _Norm_Dir.x;
                m_Vertical_Comp = _Norm_Dir.z;
                transform.forward = _dir_To_Target;
                Slide_Tackle();
            }
            else if (current_Status == status.BALL)
            {
                if (m_Ball_In_Prox() && !m_Taking_Damage && !m_Is_Being_Stunned)
                {
                    target_Vec = target_Pos.position;
                    Vector3 _dir_To_Target = target_Vec - transform.position;
                    Vector3 _Norm_Dir = _dir_To_Target.normalized;
                    m_Horizontal_Comp = _Norm_Dir.x;
                    m_Vertical_Comp = _Norm_Dir.z;
                }
                else if (!m_Ball_In_Prox() && !m_Taking_Damage && !m_Is_Being_Stunned)
                {
                    if (Vector3.Magnitude(target_Vec - transform.position) > 4)
                    {
                        Vector3 _dir_To_Target = target_Vec - transform.position;
                        Vector3 _Norm_Dir = _dir_To_Target.normalized;
                        m_Horizontal_Comp = _Norm_Dir.x;
                        m_Vertical_Comp = _Norm_Dir.z;
                    }
                    else
                    {
                        Defend_Goal_Pos(target_Pos);
                    }
                }
            }
            else
            {
                m_Horizontal_Comp = 0;
                m_Vertical_Comp = 0;
            }
        }

        /// <summary>
        /// Update the current transform that the AI will be going for.
        /// </summary>
        /// <param name="_target_Transform"></param>
        public void Update_Target_Pos(Transform _target_Transform)
        {
            target_Pos = _target_Transform;
            m_random_Offset = new Vector3(Random.Range(-random_Offset, random_Offset),
                m_random_Offset.y, Random.Range(-random_Offset, random_Offset));

        }

        /// <summary>
        /// EFFECTS ARE AFTER THIS POINT, particle systems turn on and off here.
        /// </summary>

        void Play_Shock_Particles()
        {
            Electrify_PS.Play();
        }

        void Stop_Shock_Particles()
        {
            if(Electrify_PS != null)
                    Electrify_PS.Stop();
        }

        ///////////////////////////////// GIZMO DRAWS
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.down * ground_Ray_Dist, ground_Ray_Rad);
            Gizmos.DrawWireSphere(transform.position + Vector3.down * ground_Ray_Dist, m_Melee_Range);
            Gizmos.DrawRay(transform.position, damage_Dir);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + transform.forward * ground_Ray_Dist, ground_Ray_Rad * 1.5f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.down * ground_Ray_Dist, m_ball_Check_Radius);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, proximity_Ability_Range);
        }
    }
}
