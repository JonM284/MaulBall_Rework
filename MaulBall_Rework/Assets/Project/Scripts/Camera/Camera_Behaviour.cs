using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Camera
{
    public class Camera_Behaviour : MonoBehaviour
    {

        [Header("Situation Target")]
        [Tooltip("Usually will be the ball")]
        public Transform target;
        public Transform main_Target;

        [Header("Positional Min/Max")]
        [Tooltip("How far the cameras position will go Forward, Backward, Left, and Right")]
        public Transform min_depth_Pos;
        public Transform max_depth_Pos;

        [Header("Forward Dir")]
        public bool Watch_Ball;

        [Header("Variables")]
        [Tooltip("How fast or slow camera will follow target. (Higher = slower)")]
        public float follow_Speed;
        [Tooltip("Offset camera position, from target position")]
        public Vector3 offset;

        public List<Vector3> average_positions;

        private Vector3 vel = Vector3.zero, follow_Point, origin = Vector3.zero, center_Depth = Vector3.zero, original_Look_Pos;

        private float m_min_Z_Offset, m_max_Z_Offset, m_min_X_Offset, m_max_X_Offset, stregnth, m_Camera_Shake_Timer, m_Camera_Shake_Max;

        private bool m_shaking_Screen = false;

        public float slowedTime = 0.05f;
        private float effectDuration = 0.5f;
        private float original_Time_Scale;

        public static Camera_Behaviour cam_Inst;



        // Use this for initialization
        void Start()
        {

            cam_Inst = this;

            m_min_Z_Offset = min_depth_Pos.transform.position.z;
            m_max_Z_Offset = max_depth_Pos.transform.position.z;

            m_min_X_Offset = min_depth_Pos.transform.position.x;
            m_max_X_Offset = max_depth_Pos.transform.position.x;

            original_Time_Scale = Time.timeScale;

            average_positions[1] = origin;


        }

        private void Update()
        {
            if (Time.timeScale < original_Time_Scale)
            {
                Time.timeScale += (1f / effectDuration) * Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
            }


        }

        // Update is called once per frame
        void LateUpdate()
        {
            Vector3 targetPosition = new Vector3(target.position.x + offset.x, target.position.y + offset.y, target.position.z + offset.z);
            Vector3 smooth_Out_Pos = Vector3.SmoothDamp(transform.position,
                targetPosition, ref vel, Time.deltaTime * follow_Speed);

            if (Watch_Ball)
            {
                average_positions[0] = target.transform.position;

                center_Depth = new Vector3(transform.position.x - offset.x, target.position.y, Center_Origin().z);

                transform.LookAt(center_Depth);
            }




            if (smooth_Out_Pos.x <= m_max_X_Offset && smooth_Out_Pos.x >= m_min_X_Offset)
            {
                follow_Point.x = smooth_Out_Pos.x;
            }

            if (smooth_Out_Pos.z <= m_max_Z_Offset && smooth_Out_Pos.z >= m_min_Z_Offset)
            {
                follow_Point.z = smooth_Out_Pos.z;

            }


            follow_Point.y = smooth_Out_Pos.y;
            transform.position = follow_Point;
        }


        public void Update_Target(Transform _new_Target)
        {
            target = _new_Target;
        }

        public void Reset_Target()
        {
            target = main_Target;
        }

        Vector3 Center_Origin()
        {
            if (average_positions.Count == 1)
            {
                return average_positions[0];
            }

            var bounds = new Bounds(average_positions[0], Vector3.zero);
            for (int i = 0; i < average_positions.Count; i++)
            {
                bounds.Encapsulate(average_positions[i]);


            }

            return bounds.center;
        }


        public void Do_Camera_Shake(float _Max_Time, float _magnitude)
        {
            stregnth = _magnitude;
            effectDuration = _Max_Time;
            Time.timeScale = slowedTime;
            StartCoroutine(CameraShake());
        }

        IEnumerator CameraShake()
        {
            Debug.Log("Camera Shake called");
            while (Time.timeScale < 0.8f)
            {
                float Xposition = Random.Range(-1f, 1f) * stregnth;
                float Yposition = Random.Range(-1f, 1f) * stregnth;

                transform.localPosition = new Vector3(Xposition + follow_Point.x, Yposition + follow_Point.y, follow_Point.z);
                yield return null;
            }

            transform.position = follow_Point;


        }
    }
}
