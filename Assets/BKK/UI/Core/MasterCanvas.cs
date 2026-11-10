using System;
using UnityEngine;

namespace BKK.UI
{
    public class MasterCanvas : MonoBehaviour
    {
        public static MasterCanvas Instance;

        public Panel inGameUI;
        public Panel menuUI;

        private void Awake()
        {
            InitInstance();
        }

        private void InitInstance()
        {
            if (Instance)
            {
                Instance.gameObject.SetActive(false);
            }

            Instance = this;
        }

        private void InitInGameUI()
        {
            
        }

        public void OpenMenu()
        {
            inGameUI.Close();
            menuUI.Open();
        }

        public void CloseMenu()
        {
            menuUI.Close();
            inGameUI.Open();
            
            // 하위 패널들의 On Off 상태 초기화.
        }
    }
}
