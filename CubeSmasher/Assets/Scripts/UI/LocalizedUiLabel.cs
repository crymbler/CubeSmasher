using TMPro;
using UnityEngine;

namespace CubeSmasher.UI
{
    public class LocalizedUiLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private string _key;

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_label != null && !string.IsNullOrEmpty(_key)) _label.text = UiText.Get(_key);
        }
    }
}
