using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    public class RecentGames : MonoBehaviour
    {


        public Button mainMenu;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            mainMenu.onClick.AddListener(() =>
            {
                HomeScreenUI.Instance.ShowMenu();
            });
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
