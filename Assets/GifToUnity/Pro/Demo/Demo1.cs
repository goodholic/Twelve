using GifImporter;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace GifForUnity.Pro.Demo
{
    public class Demo1 : MonoBehaviour
    {
        public  TMP_InputField  inputField;
        public  Button          Button;
        public  Image           Image;
        private GifPlayer       _player;
        private UnityWebRequest _unityWebRequest;

        void Start()
        {
            Button.onClick.AddListener(OnClick);
            _player              = Image.gameObject.AddComponent<GifPlayer>();
            Image.preserveAspect = true;
        }

        private void OnClick()
        {
            if (_unityWebRequest != null) return;

            _unityWebRequest = UnityWebRequest.Get(inputField.text);
            _unityWebRequest.SendWebRequest().completed += p =>
            {
                if (_unityWebRequest.result == UnityWebRequest.Result.Success)
                {
                    var bytes = _unityWebRequest.downloadHandler.data;
                    var gif   = GifRuntime.GetGif(bytes, inputField.text);
                    _player.Gif      = gif;
                    _unityWebRequest = null;
                }
            };
        }
    }
}
