using UnityEngine;

namespace DialogSystem.Runtime.Core
{
    public class DialogPlayer : MonoBehaviour
    {
        public GameObject mainMenu;
        public void StartDialog(string dialogID)
        {
            DialogManager.Instance.PlayDialogByID(dialogID, OnDialogEnded);
        }

        public void OnDialogEnded()
        {
            mainMenu.SetActive(true);
        }

        public void QuitGame(string payloadJson)
        {
            var payload = JsonUtility.FromJson<string>(string.IsNullOrEmpty(payloadJson) ? "{}" : payloadJson);
           Debug.Log("Quit Game, " + payload);
        }
    }

    
}