using UnityEngine;

namespace LittleBuildersWorld.Core
{
    /// <summary>Boot scene entry point. Services already exist (see <see cref="GameServices"/>); go to the menu.</summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string m_FirstScene = SceneFlow.MainMenu;

        private void Start()
        {
            if (GameServices.Instance == null) GameServices.Create(null);
            GameServices.Instance.Scenes.Load(m_FirstScene);
        }
    }
}
