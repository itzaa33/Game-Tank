/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 *  otherwise make available to any third party the Service or the Content. */

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Photon;
using UnityEngine.UI;

#if UNITY_ADS
using UnityEngine.Advertisements;
#endif

namespace TanksMP
{
    /// <summary>
    /// Manages game workflow and provides high-level access to networked logic during a game.
    /// It manages functions such as team fill, scores and ending a game, but also video ad results.
    /// </summary>
    public class GameManager : PunBehaviour
    {
        //reference to this script instance
        private static GameManager instance;

        public BotSpawner botspawner;
        /// <summary>
        /// The local player instance spawned for this client.
        /// </summary>
        [HideInInspector]
        public Player localPlayer;

        /// <summary>
        /// Reference to the UI script displaying game stats.
        /// </summary>
        public UIGame ui;

        /// <summary>
        /// Definition of playing teams with additional properties.
        /// </summary>
        public Team[] teams;

        /// <summary>
        /// The maximum amount of kills to reach before ending the game.
        /// </summary>

        public GameObject audioObj;

        public static int maxScore = 30;

        public static UnityEvent OnUpdateStatus; 

        public static int multiplyDamage_Status = 0;
        public static int Hp_Status = 0;
        public static int Speed_Status = 0;
        /// <summary>
        /// The delay in seconds before respawning a player after it got killed.
        /// </summary>
        public int respawnTime = 5;

        public int amountbotdie = 0;

        //initialize variables
        void Awake()
        {
            instance = this;

            UIGame.OnReUiValue.AddListener(ReUiValue);
            //if Unity Ads is enabled, hook up its result callback
            //            #if UNITY_ADS
            //                UnityAdsManager.adResultEvent += HandleAdResult;***********************************
            //            #endif
        }

        void Update()
        {
            IsGameOver();
        }

        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static GameManager GetInstance()
        {
            return instance;
        }


        /// <summary>
        /// Global check whether this client is the match master or not.
        /// </summary>
        public static bool isMaster()
        {
            return PhotonNetwork.isMasterClient;
        }


        /// <summary>
        /// Returns the next team index a player should be assigned to.
        /// </summary>
        public int GetTeamFill(int offset)
        {
            //init variables
            int[] size = PhotonNetwork.room.GetSize();
            int teamNo = offset;

            int min = size[1];
            //loop over teams to find the lowest fill
            for (int i = offset; i < teams.Length; i++)
            {
                //if fill is lower than the previous value
                //store new fill and team for next iteration
                if (size[i] < min)
                {
                    min = size[i];
                    teamNo = i;
                }

            }

            //return index of lowest team
            return teamNo;
        }


        /// <summary>
        /// Returns a random spawn position within the team's spawn area.
        /// </summary>
        public Vector3 GetSpawnPosition(int teamIndex)
        {
            //init variables
            Vector3 pos = teams[teamIndex].spawn.position;
            BoxCollider col = teams[teamIndex].spawn.GetComponent<BoxCollider>();

            if(col != null)
            {
                //find a position within the box collider range, first set fixed y position
                //the counter determines how often we are calculating a new position if out of range
                pos.y = col.transform.position.y;
                int counter = 10;

                //try to get random position within collider bounds
                //if it's not within bounds, do another iteration
                do
                {
                    pos.x = UnityEngine.Random.Range(col.bounds.min.x, col.bounds.max.x);
                    pos.z = UnityEngine.Random.Range(col.bounds.min.z, col.bounds.max.z);
                    counter--;
                }
                while(!col.bounds.Contains(pos) && counter > 0);
            }

            print(pos);
            return pos;


        }


        //implements what to do when an ad view completes
        #if UNITY_ADS
        void HandleAdResult(ShowResult result)
        {
            switch (result)
            {
                //in case the player successfully watched an ad,
                //it sends a request for it be respawned
                case ShowResult.Finished:
                case ShowResult.Skipped:
                    localPlayer.CmdRespawn();
                    break;

                    //in case the ad can't be shown, just handle it
                    //like we wouldn't have tried showing a video ad
                    //with the regular death countdown (force ad skip)
                case ShowResult.Failed:
                    DisplayDeath(true);
                    break;
            }
        }
        #endif


        /// <summary>
        /// Returns whether a team reached the maximum game score.
        /// </summary>
        public bool IsGameOver()
        {
            //init variables
            //            bool isOver = false;
            //            int[] score = PhotonNetwork.room.GetScore();
            //            
            //            //loop over teams to find the highest score
            //            for(int i = 0; i < teams.Length; i++)
            //            {
            //                //score is greater or equal to max score,
            //                //which means the game is finished
            //                if(score[i] >= maxScore)
            //                {
            //                    isOver = true;
            //                    break;
            //                }
            //            }

            bool isOver = false;

            if(maxScore == 0)
            {
                isOver = true;
                StartCoroutine(DisplayGameOver(0.5f));
            }

            //return the result
            return isOver;
        }


        /// <summary>
        /// Only for this player: sets the death text stating the killer on death.
        /// If Unity Ads is enabled, tries to show an ad during the respawn delay.
        /// By using the 'skipAd' parameter is it possible to force skipping ads.
        /// </summary>
        public void DisplayDeath(bool skipAd = false)
        {   
            //get the player component that killed us
            Player other = localPlayer.killedBy.GetComponent<Player>();

            //calculate if we should show a video ad
            //            #if UNITY_ADS
            //            if (!skipAd && UnityAdsManager.ShowAd())
            //                return;
            //            #endif

            //when no ad is being shown, set the death text
            //and start waiting for the respawn delay immediately
            ui.SetDeathText(other.GetView().GetName(), teams[other.GetView().GetTeam()]);
            StartCoroutine(SpawnRoutine());
        }


        //coroutine spawning the player after a respawn delay
        IEnumerator SpawnRoutine()
        {
            //calculate point in time for respawn
            float targetTime = Time.time + respawnTime;

            //wait for the respawn to be over,
            //while waiting update the respawn countdown
            while (targetTime - Time.time > 0)
            {
                ui.SetSpawnDelay(targetTime - Time.time);
                yield return null;
            }

            //respawn now: send request to the server
            ui.DisableDeath();
            localPlayer.CmdRespawn();
        }


        /// <summary>
        /// Only for this player: sets game over text stating the winning team.
        /// Disables player movement so no updates are sent through the network.
        /// </summary>
        public void DisplayGameOver(int teamIndex)
        {
            PhotonNetwork.isMessageQueueRunning = false;
            localPlayer.enabled = false;
            localPlayer.camFollow.HideMask(true);
            ui.SetGameOverText(teams[teamIndex]);

            //starts coroutine for displaying the game over window
            StartCoroutine(DisplayGameOver(3f));
        }


        //displays game over window after short delay
        IEnumerator DisplayGameOver(float count)
        {
            //give the user a chance to read which team won the game
            //before enabling the game over screen
            yield return new WaitForSeconds(count);

            //show game over window and disconnect from network
            ui.ShowGameOver();
            PhotonNetwork.Disconnect();
        }


        //clean up callbacks on scene switches
        //        void OnDestroy()
        //        {
        //            #if UNITY_ADS
        //                UnityAdsManager.adResultEvent -= HandleAdResult;*********************
        //            #endif
        //        }

        public void UpStatusAttack(int status)
        {
            Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<TanksMP.Player>();

            if (player)
            {

                multiplyDamage_Status = status; 

                player.UpstatusPlayer();

            }
        }
        public void UpStatusHp(int status)
        {
            Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<TanksMP.Player>();

            if (player)
            {

                Hp_Status += status; 

                player.UpstatusPlayer();

                Hp_Status = 0; 

            }
        }

        public void UpStatusSpeed(int status)
        {
            Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<TanksMP.Player>();

            if (player)
            {

                Speed_Status += status; 

                player.UpstatusPlayer();

                Speed_Status = 0; 
            }
        }


        public void ChangElement(int state)
        {
            Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<TanksMP.Player>();

            if (state == 1)
            {
                player.changeElement(state);
            }
            else
            {
                player.changeElement(state);
            }
        }

        void ReUiValue()
        {
            botspawner.TimeWave = (float)45f;
            maxScore = 30;
            gameObject.GetComponent<BotSpawner>().WaveLavel = 0;
        }

        public void StopGame()
        {
            Time.timeScale = 0;

            var objs = audioObj.GetComponentsInChildren<AudioSource>();

            for (int i = 0; i < objs.Length; i++)
            {
                objs[i].Stop();
            }

        }

        public void StartGame()
        {
            Time.timeScale = 1;

            audioObj.SetActive(true);
        }
    }




    /// <summary>
    /// Defines properties of a team.
    /// </summary>
    [System.Serializable]
    public class Team
    {
        /// <summary>
        /// The name of the team shown on game over.
        /// </summary>
        public string name;

        /// <summary>
        /// The color of a team for UI and player prefabs.
        /// </summary>   
        public Material material;

        /// <summary>
        /// The spawn point of a team in the scene. In case it has a BoxCollider
        /// component attached, a point within the collider bounds will be used.
        /// </summary>
        public Transform spawn;
    }
}