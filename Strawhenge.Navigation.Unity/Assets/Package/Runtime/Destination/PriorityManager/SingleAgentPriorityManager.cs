using UnityEngine.SceneManagement;

namespace Strawhenge.Navigation.Unity.Destination
{
    static class SingleAgentPriorityManager
    {
        static readonly AgentPriorityManager AgentPriorityManager;

        static SingleAgentPriorityManager()
        {
            AgentPriorityManager = new AgentPriorityManager();
            SceneManager.sceneUnloaded += _ => AgentPriorityManager.Clear();
        }

        public static AgentPriorityManager Instance => AgentPriorityManager;
    }
}