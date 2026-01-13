#if UNITY_SERVICES || STEAM_SERVICES
using Unity.Netcode;
using UnityEngine.Events;

namespace Ignitives.MultiplayerEngine
{
    public class Interactable : NetworkBehaviour
    {
        public virtual void Interact()
        {
            OnInteract?.Invoke();
        }

        public virtual void ShowUI()
        {
            // Use for showing UI or something when player looks at this object
        }

        public virtual void HideUI()
        {
            // Use for hiding UI or something when player looks away from this object
        }

        public UnityEvent OnInteract;
    }
}
#endif
