/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace TanksMP
{
    /// <summary>
    /// Custom implementation of the most Photon callback handlers for network workflows. This script is
    /// responsible for connecting to Photon's Cloud, spawning players and handling disconnects.
    /// </summary>
	public class NetworkManagerCustom : PunBehaviour
    {
        //reference to this script instance
        private static NetworkManagerCustom instance;

        /// <summary>
        /// Scene index that gets loaded when disconnecting from a game.
        /// </summary>
        public int offlineSceneIndex = 0;

        /// <summary>
        /// Scene index that gets loaded after a connection has been established.
        /// </summary>
        public int onlineSceneIndex = 1;

        /// <summary>
        /// Maximum amount of players per room.
        /// </summary>
        public int maxPlayers = 12;

        /// <summary>
        /// References to the available player prefabs located in a Resources folder.
        /// </summary>
        public GameObject[] playerPrefabs;

        public GameObject[] BulletPrefabs;

        public GameObject[] BulletSilverPrefabs;

        public GameObject[] BulletGoldPrefabs;

        /// <summary>
        /// App version. Only players with the same version will find each other.
        /// </summary>
        public const string appVersion = "1.0";

        /// <summary>
        /// Event fired when a connection to the matchmaker service failed.
        /// </summary>
        public static event Action connectionFailedEvent;


        //initialize network view
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            //adding a view to this gameobject with a unique viewID
            //this is to avoid having the same ID in a scene
            PhotonView view = gameObject.AddComponent<PhotonView>();
            view.viewID = 999;
        }


        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static NetworkManagerCustom GetInstance()
        {
            return instance;
        }


        /// <summary>
        /// Starts initializing and connecting to a game. Depends on the selected network mode.
        /// Sets the current player name prior to connecting to the servers.
        /// </summary>
        public static void StartMatch(NetworkMode mode)
        {
            PhotonNetwork.automaticallySyncScene = true;
            PhotonNetwork.playerName = PlayerPrefs.GetString(PrefsKeys.playerName);

            switch (mode)
            {
                //connects to a cloud game available on the Photon servers
                case NetworkMode.Online:
                    #if UNITY_WEBGL
                    //best region is not supported on WebGL, choose the location where your server is located,
                    //or implement an option in your game for players to select their preferred location
                    PhotonNetwork.ConnectToRegion(CloudRegionCode.us, appVersion);
                    #else
                        PhotonNetwork.PhotonServerSettings.HostType = ServerSettings.HostingOption.BestRegion;
                        PhotonNetwork.ConnectToBestCloudServer(appVersion);
                    #endif
                    break;

                //search for open LAN games on the current network, otherwise open a new one
                case NetworkMode.LAN:
                    PhotonNetwork.ConnectToMaster(PlayerPrefs.GetString(PrefsKeys.serverAddress), 5055, PhotonNetwork.PhotonServerSettings.AppID, appVersion);
                    break;

                //enable Photon offline mode to not send any network messages at all
                case NetworkMode.Offline:
                    PhotonNetwork.offlineMode = true;
                    break;
            }
        }


        /// <summary>
        /// Called if a connect call to the Photon server failed before the connection was established.
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnFailedToConnectToPhoton(DisconnectCause cause)
        {
            if (connectionFailedEvent != null)
                connectionFailedEvent();
        }


        /// <summary>
        /// Called when something causes the connection to fail (after it was established).
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnConnectionFail(DisconnectCause cause)
        {
            if (connectionFailedEvent != null)
                connectionFailedEvent();
        }


        /// <summary>
        /// Called after the connection to the master is established.
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnConnectedToMaster()
        {
            //set my own name and try joining a game
            PhotonNetwork.playerName = PlayerPrefs.GetString(PrefsKeys.playerName);
            PhotonNetwork.JoinRandomRoom();
        }


        /// <summary>
        /// Called when a joining a random room failed.
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
        {
            Debug.Log("Photon did not find any matches on the Master Client we are connected to. Creating our own room... (ignore the warning above).");

            //joining failed so try to create our own room
            PhotonNetwork.CreateRoom(null, new RoomOptions() { MaxPlayers = (byte)this.maxPlayers }, null);
        }


        /// <summary>
        /// Called when a creating a room failed. 
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnPhotonCreateRoomFailed(object[] codeAndMsg)
        {
            if (connectionFailedEvent != null)
                connectionFailedEvent();
        }


        /// <summary>
        /// Called when this client created a room and entered it.
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnCreatedRoom()
        {
            //we created a room so we have to set the initial room properties for this room,
            //such as populating the team fill and score arrays
            Hashtable roomProps = new Hashtable();
            roomProps.Add(RoomExtensions.size, new int[RoomExtensions.initialArrayLength]);
            roomProps.Add(RoomExtensions.score, new int[RoomExtensions.initialArrayLength]);
            PhotonNetwork.room.SetCustomProperties(roomProps);

            //load the online scene
            PhotonNetwork.LoadLevel(onlineSceneIndex);
        }


        /// <summary>
        /// Called on entering a lobby on the Master Server.
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnJoinedLobby()
        {
            //when connecting to the master, try joining a room
            PhotonNetwork.JoinRandomRoom();
        }


        /// <summary>
        /// Called when entering a room (by creating or joining it).
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnJoinedRoom()
        {
            //we've joined a finished room, disconnect immediately
            if (GameManager.GetInstance() && GameManager.GetInstance().IsGameOver())
            {
                PhotonNetwork.Disconnect();
                return;
            }

            if (!PhotonNetwork.isMasterClient)
                return;

            //add ourselves to the game. This is only called for the master client
            //because other clients will trigger the OnPhotonPlayerConnected callback directly
            StartCoroutine(WaitForSceneChange());
        }


        //this wait routine is needed on offline mode for waiting on completed scene change,
        //because in offline mode Photon does not pause network messages. But it doesn't hurt
        //leaving this in for all other network modes too
        IEnumerator WaitForSceneChange()
        {
            while (SceneManager.GetActiveScene().buildIndex != onlineSceneIndex)
            {
                yield return null;
            }

            //we connected ourselves
            OnPhotonPlayerConnected(PhotonNetwork.player);
        }


        /// <summary>
        /// Called when a remote player entered the room. 
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnPhotonPlayerConnected(PhotonPlayer player)
        {
            //only let the master client handle this connection
            if (!PhotonNetwork.isMasterClient)
                return;

            //get the next team index which the player should belong to
            //assign it to the player and update player properties
            int teamIndex = GameManager.GetInstance().GetTeamFill(0);
            PhotonNetwork.room.AddSize(teamIndex, +1);
            player.SetTeam(teamIndex);

            //also player properties are not cleared when disconnecting and connecting
            //automatically, so we have to set all existing properties to null
            //these default values will get overriden by correct data soon
            player.Clear();

            //the master client sends an instruction to this player for adding him to the game
            this.photonView.RPC("AddPlayer", player);
            AddBullet();
            //**************
        }


        //received from the master client, for this player, after successfully joining a game
		[PunRPC]
		void AddPlayer()
		{
            //get our selected player prefab index
			int prefabId = int.Parse(Encryptor.Decrypt(PlayerPrefs.GetString(PrefsKeys.activeTank)));
            
            //get the spawn position where our player prefab should be instantiated at, depending on the team assigned
            //if we cannot get a position, spawn it in the center of that team area - otherwise use the calculated position
			Transform startPos = GameManager.GetInstance().teams[PhotonNetwork.player.GetTeam()].spawn;
			if (startPos != null) PhotonNetwork.Instantiate(playerPrefabs[prefabId].name, startPos.position, startPos.rotation, 0);
			else PhotonNetwork.Instantiate(playerPrefabs[prefabId].name, Vector3.zero, Quaternion.identity, 0);
		}


        public  void AddBullet()
        {
            //get our selected player prefab index
            int prefabId = int.Parse(Encryptor.Decrypt(PlayerPrefs.GetString(PrefsKeys.activeBullet)));

            Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();

            if(player)
            {
                if(prefabId == 0)
                {
                    player.bullets = BulletPrefabs;
                }
                else if(prefabId == 1)
                {
                    player.bullets = BulletSilverPrefabs;
                }
                else if(prefabId == 2)
                {
                    player.bullets = BulletGoldPrefabs;
                }
            }
        }


        /// <summary>
        /// Called when a remote player left the room.
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnPhotonPlayerDisconnected(PhotonPlayer player)
        {
            //only let the master client handle this connection
            if (!PhotonNetwork.isMasterClient)
				return;
			
            //decrease the team fill for the team of the leaving player and update room properties
            PhotonNetwork.room.AddSize(player.GetTeam(), -1);
        }


        /// <summary>
        /// Called after disconnecting from the Photon server.
        /// See the official Photon docs for more details.
        /// </summary>
        public override void OnDisconnectedFromPhoton()
        {
            //do not switch scenes automatically when the game over screen is being shown already
            if (GameManager.GetInstance() && GameManager.GetInstance().ui.gameOverMenu.activeInHierarchy)
                return;

            //switch from the online to the offline scene after connection is closed
            if(SceneManager.GetActiveScene().buildIndex != offlineSceneIndex)
                SceneManager.LoadScene(offlineSceneIndex);
        }
    }


    /// <summary>
    /// PhotonNetwork Mode selection for preferred network type.
    /// </summary>
    public enum NetworkMode
    {
        Online,
        LAN,
        Offline
    }


    /// <summary>
    /// This class extends Photon's Room object by custom properties.
    /// Provides several methods for setting and getting variables out of them.
    /// </summary>
    public static class RoomExtensions
    {
        /// <summary>
        /// The initial team size of the game for the server creating a new room.
        /// Unfortunately this cannot be set via the GameManager because it does not exist at that point.
        /// </summary>
        public const short initialArrayLength = 4;
        
        /// <summary>
        /// The key for accessing team fill per team out of the room properties.
        /// </summary>
        public const string size = "size";
        
        /// <summary>
        /// The key for accessing player scores per team out of the room properties.
        /// </summary>
        public const string score = "score";
        
        
        /// <summary>
        /// Returns the networked team fill for all teams out of properties.
        /// </summary>
        public static int[] GetSize(this Room room)
        {
            return (int[])room.CustomProperties[size];
        }
        
        /// <summary>
        /// Increases the team fill for a team by one when a new player joined the game.
        /// This is also being used on player disconnect by using a negative value.
        /// </summary>
        public static int[] AddSize(this Room room, int teamIndex, int value)
        {
            int[] sizes = room.GetSize();
            sizes[teamIndex] += value;
            
            room.SetCustomProperties(new Hashtable() {{size, sizes}});
            return sizes;
        }
        
        /// <summary>
        /// Returns the networked team scores for all teams out of properties.
        /// </summary>
        public static int[] GetScore(this Room room)
        {
            return (int[])room.CustomProperties[score];
        }
        
        /// <summary>
        /// Increase the score for a team by one when a new player scored a point for his team.
        /// </summary>
        public static int[] AddScore(this Room room, int teamIndex)
        {
            int[] scores = room.GetScore();
            scores[teamIndex] += 1;
            
            room.SetCustomProperties(new Hashtable() {{score, scores}});
            return scores;
        }
    }
}