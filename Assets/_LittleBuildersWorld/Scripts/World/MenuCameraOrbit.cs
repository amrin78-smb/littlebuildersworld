using UnityEngine;

namespace LittleBuildersWorld.World
{
    /// <summary>Slow, calm orbit around the main menu diorama.</summary>
    public sealed class MenuCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Vector3 m_Centre = Vector3.zero;
        [SerializeField] private float m_Radius = 16f;
        [SerializeField] private float m_Height = 7f;
        [SerializeField] private float m_DegreesPerSecond = 4f;
        [SerializeField] private float m_Yaw = 200f;

        private void LateUpdate()
        {
            m_Yaw += m_DegreesPerSecond * Time.unscaledDeltaTime;
            Quaternion r = Quaternion.Euler(0f, m_Yaw, 0f);
            transform.position = m_Centre + r * new Vector3(0f, m_Height, -m_Radius);
            transform.LookAt(m_Centre + Vector3.up * 1.5f);
        }
    }
}
