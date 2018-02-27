using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TanksMP
{ 
    public class UIGameEnd : MonoBehaviour 
    {

        public BotSpawner botspawn;
        public GameManager gameManager;

        public Text name;
        public Text EnamyQill;
        public Text LevelWave;
        public Text Money;
        public Text Score;
    	// Use this for initialization
    	void Start () 
        {
            name.text = PlayerPrefs.GetString(PrefsKeys.playerName);
    	}
    	
    	// Update is called once per frame
    	void Update () 
        {
            EnamyQill.text = gameManager.amountbotdie.ToString();

            LevelWave.text = botspawn.WaveLavel.ToString();

            int socre = (gameManager.amountbotdie + botspawn.BonusClearEnamy) * botspawn.WaveLavel;
            Score.text = socre.ToString();
    	}

        void OnEnable()
        {
            EnamyQill.text = gameManager.amountbotdie.ToString();

            LevelWave.text = botspawn.WaveLavel.ToString();

            Money.text = (gameManager.amountbotdie * botspawn.WaveLavel).ToString();

            int money = PlayerPrefs.GetInt(PrefsKeys.Money);

            money = money + (gameManager.amountbotdie * botspawn.WaveLavel);

            PlayerPrefs.SetInt(PrefsKeys.Money,money);



            int socre = (gameManager.amountbotdie + botspawn.BonusClearEnamy) * botspawn.WaveLavel;
            Score.text = socre.ToString();
        }
    }
}