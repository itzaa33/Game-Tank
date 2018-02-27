/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Photon;

namespace TanksMP
{          
    /// <summary>
    /// Responsible for spawning AI bots when in offline mode, otherwise gets disabled.
    /// </summary>
    [System.Serializable]
    public class  upStatusplayer : UnityEvent<int>{} 

	public class BotSpawner : PunBehaviour
    {                

        private Player m_player;
        /// <summary>
        /// Amount of bots to spawn across all teams.
        /// </summary>
        public int maxBots;

        public int BonusClearEnamy = 0;
        
        /// <summary>
        /// Selection of bot prefabs to choose from.
        /// </summary>
        private float m_timewave = 9;

        public float TimeWave 
        { 
            get {return m_timewave; }

            set
            {
                m_timewave = value;


            }

        } 

        private int m_saveTime;
        private bool m_StateTime = true;

        public AudioClip[] SetcountTime;

        public GameObject[] prefabs;

        public GameManager gamemanager;

        public List<GameObject> A_LifeBots = new List<GameObject>();
        public Queue<GameObject> A_DeadBots = new Queue<GameObject>();

        [SerializeField]
        private GameObject m_PL_Bots;
        [SerializeField]
        private GameObject m_PD_Bots;

        public static UnityEvent upStatusBot = new UnityEvent();

        public static upStatusplayer OnupStatusplayer = new upStatusplayer();

        public static UnityEvent ChangElement = new UnityEvent();


        private static int waveLavel = 9;

        private int pointUpstatus = 0;

        public int WaveLavel 
        { 
          get { return waveLavel; }
            
            set { 
                
                if (waveLavel < 25)
                {
                    waveLavel = value;
                }

                if (waveLavel % 5 == 0 )
                {
                    upStatusBot.Invoke();
                    OnupStatusplayer.Invoke(pointUpstatus);
                    pointUpstatus++;

                    if (waveLavel == 10)
                    {
                        ChangElement.Invoke();
                    }
                }

                if (waveLavel == 25)
                {
                    PlayerBot bot = GameObject.FindGameObjectWithTag("Bot").GetComponent<PlayerBot>();

                    Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
                    
                    bot.lastwaveUpdateStatusBot(player);
                }

              }
        }

        void Awake()
        {
            //disabled when not in offline mode
            if ((NetworkMode)PlayerPrefs.GetInt(PrefsKeys.networkMode) != NetworkMode.Offline)
                this.enabled = false;

        }

        void Update()
        {
            if(GameManager.maxScore > 0)    
            CountWave();
        }


        void Start()
        {
            m_saveTime = (int)TimeWave;

            AddBots();
        }

           

        void AddBots()
        {
            int CountBot = maxBots - A_DeadBots.Count;

            for(int i = 0; i < CountBot; i++)
            {
                //randomly choose bot from array of bot prefabs
                //spawn bot across the simulated private network
                int randIndex = Random.Range(0, prefabs.Length);
                GameObject obj = PhotonNetwork.Instantiate(prefabs[randIndex].name, Vector3.zero, Quaternion.identity, 0);

                A_DeadBots.Enqueue(obj);
                obj.transform.SetParent(m_PD_Bots.transform);
                obj.SetActive(false);

                //let the local host determine the team assignment
                Player p = obj.GetComponent<Player>();
                int team = GameManager.GetInstance().GetTeamFill(1);
                //Debug.Log("Team: " + team);
                p.GetView().SetTeam(team);

                //increase corresponding team size
                PhotonNetwork.room.AddSize(p.GetView().GetTeam(), +1);

            }
                
        }

//        IEnumerator Start()
//        {
//
//            m_saveTime = (int)TimeWave;
////            wait a second for all script to initialize
//            yield return new WaitForSeconds(1);
//
////            loop over bot count
//			for(int i = 0; i < maxBots; i++)
//            {
//                //randomly choose bot from array of bot prefabs
//                //spawn bot across the simulated private network
//                int randIndex = Random.Range(0, prefabs.Length);
//                GameObject obj = PhotonNetwork.Instantiate(prefabs[randIndex].name, Vector3.zero, Quaternion.identity, 0);
//
//                A_DeadBots.Enqueue(obj);
//                obj.transform.SetParent(m_PD_Bots.transform);
//                obj.SetActive(false);
//
//                //let the local host determine the team assignment
//                Player p = obj.GetComponent<Player>();
//                int team = GameManager.GetInstance().GetTeamFill(1);
//                //Debug.Log("Team: " + team);
//                p.GetView().SetTeam(team);
//
//                //increase corresponding team size
//                PhotonNetwork.room.AddSize(p.GetView().GetTeam(), +1);
//
//                yield return new WaitForSeconds(0.25f);
//            }
//
//        }

        void CountWave()
        {

            if (m_StateTime)
            {
                
                if(TimeWave >= 0)TimeWave -= Time.deltaTime;

                if (TimeWave <= 0)
                {
                    if (A_DeadBots.Count < maxBots)
                    {
                        AddBots();

                        StartCoroutine(RelaxState(3)); 
                    }
                    else
                        StartCoroutine(RelaxState(3)); 
                }
                else
                {

                    if(TimeWave >= 30 )
                    {
                        BonusClearEnamy += 200;
                    }
                    else if(TimeWave >= 20 && TimeWave < 30)
                    {
                        BonusClearEnamy += 150;
                    }
                    else if(TimeWave >= 10 && TimeWave < 20)
                    {
                        BonusClearEnamy += 100;
                    }

                    if (A_LifeBots.Count == 0)
                    {
                        m_StateTime = false;

                        StartCoroutine(RelaxState(8));

                    }
                }
            }

        }

        IEnumerator RelaxState(int count)
        {

            yield return new WaitForSeconds(count);

            while(A_DeadBots.Count > 0)
            {

                Spawn();

            }
        }

        public void Spawn()
        {
            WaveLavel++;

            for(int i = 0; i < maxBots ; i++)
            {

                var respawn = A_DeadBots.Dequeue();

                A_LifeBots.Add(respawn);

                respawn.SetActive(true);

                respawn.transform.SetParent(m_PL_Bots.transform);

                TimeWave = m_saveTime;
//                StartCoroutine(VoidCountTimeWave());

                m_StateTime = true;


            }
        }



        public void Despawn(GameObject respawn)
        {
            gamemanager.amountbotdie++;

            respawn.SetActive(false);

            A_LifeBots.Remove(respawn.gameObject);
            A_DeadBots.Enqueue(respawn.gameObject);

            respawn.transform.SetParent(m_PD_Bots.transform);
        }

//        private void VoidCountTimeWave()
//        {
//
//            Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
//
//            if (TimeWave == 5f)
//            {
//
//                if (SetcountTime[5])
//                    AudioManager.Play3D(SetcountTime[5], player.transform.position, 0.01f);
//            }
//            else if (TimeWave == 4)
//            {
//                if (SetcountTime[4])
//                    AudioManager.Play3D(SetcountTime[4], player.transform.position, 0.1f);
//            }
//            else if (TimeWave == 3)
//            {
//                if (SetcountTime[3])
//                    AudioManager.Play3D(SetcountTime[3], player.transform.position, 0.1f);
//            }
//            else if (TimeWave == 2)
//            {
//                if (SetcountTime[2])
//                    AudioManager.Play3D(SetcountTime[2], player.transform.position, 0.1f);
//            }
//            else if (TimeWave == 1)
//            {
//                if (SetcountTime[1])
//                    AudioManager.Play3D(SetcountTime[1], player.transform.position, 0.1f);
//            }
//            else if (TimeWave == 0)
//            {
//                if (SetcountTime[0])
//                    AudioManager.Play3D(SetcountTime[0], player.transform.position, 0.8f);
//            }
//
//        }

        IEnumerator VoidCountTimeWave()
        {
            yield return new WaitWhile(() => TimeWave > 6f);

            Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();

            for (int i = 5; i >= 0 ; i--)
            {
                AudioManager.Play3D(SetcountTime[i], player.transform.position, 0.01f);
                
                yield return new WaitForSeconds(1);
            }


        }

    }
}
