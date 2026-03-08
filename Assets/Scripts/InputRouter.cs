using UnityEngine;
namespace DefaultNamespace
{
    public class InputRouter : MonoBehaviour
    {

        public void HandleNewMessage(string messageContent)
        {
            Debug.Log("[InputRouter] Received new message: " + messageContent);
        }
    }
}