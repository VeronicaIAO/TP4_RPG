using UnityEngine;
using UnityEngine.UI;

namespace TPRPG
{
    [RequireComponent(typeof(Button))]
    public class ChangeTeammateButton : MonoBehaviour
    {
        public BattleManager manager;

        Button button;

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }

        void Update()
        {
            if (manager != null) button.interactable = manager.CanSwap;
        }

        void OnClick()
        {
            Debug.Log("[ChangeTeammate] click. CanSwap = " + (manager != null && manager.CanSwap));
            if (manager == null) return;
            foreach (var r in manager.reserve)
            {
                if (r != null && r.IsAlive)
                {
                    manager.SwapActivePlayer(r);
                    return;
                }
            }
        }
    }
}