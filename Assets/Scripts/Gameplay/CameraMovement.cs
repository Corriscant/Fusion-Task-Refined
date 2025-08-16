using UnityEngine;

namespace FusionTask.Gameplay
{
    public class CameraMovement : MonoBehaviour
    {
        [SerializeField] private float speed = 10f;

        private void Update()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 direction = new Vector3(horizontal, 0f, vertical);
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
        }
    }
}
