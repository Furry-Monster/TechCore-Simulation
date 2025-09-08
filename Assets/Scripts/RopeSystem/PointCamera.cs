using UnityEngine;

namespace RopeSystem
{
    public class PointCamera : MonoBehaviour
    {
        public float speed = 15f;
        public Transform[] cameraPoses;
        public Transform target;
        private int current;

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, cameraPoses[current].position,
                Time.deltaTime * speed);
            transform.rotation = Quaternion.Lerp(transform.rotation, cameraPoses[current].rotation,
                Time.deltaTime * speed);
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Q))
            {
                --current;
                if (current == -1)
                    current = cameraPoses.Length - 1;
            }
            else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.E))
            {
                ++current;
                if (current == cameraPoses.Length)
                    current = 0;
            }

            current = Mathf.Clamp(current, 0, cameraPoses.Length - 1);
        }
    }
}