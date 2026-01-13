#if UNITY_SERVICES || STEAM_SERVICES
using UnityEngine;
namespace Ignitives.MultiplayerEngine{ 
public class DoorSwitch : Interactable
{
        public PickupUI interactUi;
        public Door doorObject;

        public override void HideUI()
        {
            interactUi.gameObject.SetActive(false);
        }

        public override void Interact()
        {
            base.Interact();
            doorObject.ToggleDoor();
        }

        public override void ShowUI()
        {
            interactUi.gameObject.SetActive(true);
        }
    }
}
#endif
