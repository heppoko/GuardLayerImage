using UnityEngine;
using UnityEngine.UI;

namespace HeppokoUtil
{
    /// <summary>
    /// TutorialLayerImage を使うサンプル
    /// </summary>
    public class TutorialSampleCanvas : MonoBehaviour
    {
        public GuardLayerImage tutorialLayerImage;

        public Button button1;

        public Button button2;

        public Button button3;

        public GameObject area1;

        void Start()
        {
            tutorialLayerImage.SetTargetObject(button1.gameObject);

            button1.onClick.AddListener(() =>
            {
                Debug.Log("Button1 がクリックされた");
                tutorialLayerImage.SetTargetObject(button2.gameObject);
            });

            button2.onClick.AddListener(() =>
            {
                Debug.Log("Button2 がクリックされた");
                tutorialLayerImage.SetTargetObject(area1.gameObject);
            });

            button3.onClick.AddListener(() =>
            {
                Debug.Log("Button3 がクリックされた");
                tutorialLayerImage.SetTargetObject(button1.gameObject);
            });

            tutorialLayerImage.onClickAction.AddListener(() =>
            {
                Debug.Log("ガード領域がクリックされた");
            });
        }

    }
}
