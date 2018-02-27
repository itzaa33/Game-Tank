/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using System.Collections;
using Photon;

namespace TanksMP
{
    /// <summary>
    /// Manages network-synced spawning of prefabs, in this case powerups.
    /// With the respawn time synced on all clients it supports host migration too.
    /// </summary>
    public class ObjectSpawner : PunBehaviour 
	{
        /// <summary>
        /// Prefab to sync the instantiation for over the network.
        /// </summary>
		public GameObject prefab;

        public BotSpawner  botspawner;

        /// <summary>
        /// Checkbox whether the object should be respawned after being despawned.
        /// </summary>
        public bool respawn;

        /// <summary>
        /// Delay until respawning the object again after it got despawned.
        /// </summary>
        public int respawnTime;

        /// <summary>
        /// Reference to the spawned prefab gameobject instance in the scene.
        /// </summary>
        [HideInInspector]
        public GameObject obj;

        //time value when the next respawn should happen measured in game time
        private float nextSpawn;

        private int checkLevel = 0;


        //when entering the game scene for the first time as a master client,
        //the master should spawn the object in the scene for all other clients

        void Start()
        {
            if(PhotonNetwork.isMasterClient)
                OnMasterClientSwitched(PhotonNetwork.player);
        }

        void Update()
        {
            if(botspawner.WaveLavel >= checkLevel)
            {
                Instantiate();
                checkLevel++;
            }
        }
//        
        /// <summary>
        /// Synchronizes current active state of the object to joining players.
        /// </summary>
        public override void OnPhotonPlayerConnected(PhotonPlayer player)
        {
            //don't execute as a non-master, but also don't execute it for the master itself
            if(!PhotonNetwork.isMasterClient || player.IsMasterClient)
                return;

            //the object is active in the scene on the master. Thus send an instantiate call
            //to the joining player so the object gets enabled/instantiated on that client too
            if(obj != null && obj.activeInHierarchy)
            {
                this.photonView.RPC("Instantiate", player);
            } 
            else
            {
                //on the master the object is not active in the scene. As a client we have to know the
                //remaining respawn time so we are able to take over in a host migration scenario
                this.photonView.RPC("SetRespawn", player, nextSpawn);
            }
        }


        /// <summary>
        /// Called after switching to a new MasterClient when the current one leaves.
        /// Here the new master has to decide whether to enable the object in the scene.
        /// </summary>
		public override void OnMasterClientSwitched(PhotonPlayer newMaster)
		{         
            //only execute on the new master client
            if(PhotonNetwork.player != newMaster)
                return;
            
            //the object is already enabled in the scene,
            //this means the new master client does not have to do anything
            if(obj != null && obj.activeInHierarchy)
                return;
            
            //the object is not active thus trigger a respawn coroutine
//                StartCoroutine(SpawnRoutine());*********

		}


        //calculates the remaining time until the next respawn,
        //waits for the delay to have passed and then instantiates the object
        IEnumerator SpawnRoutine()
		{    
            float delay = Mathf.Clamp(nextSpawn - (float)PhotonNetwork.time, 0, respawnTime);
            yield return new WaitForSeconds(delay);


                if (PhotonNetwork.connected)
                {
                        this.photonView.RPC("Instantiate", PhotonTargets.All);
                }
		}
		
        
        /// <summary>
        /// Instantiates the object in the scene using PoolManager functionality.
        /// </summary>
        [PunRPC]
		public void Instantiate()
		{
            if(obj != null) return;
			obj = PoolManager.Spawn(prefab, transform.position, transform.rotation);

            //set the reference on the instantiated object for cross-referencing
            if(obj.GetComponent<MVC.Model.ItemModel>())
                obj.GetComponent<MVC.Model.ItemModel>().spawner = this;
            else
                obj.GetComponent<Powerup>().spawner = this; 
		}
        
        
        /// <summary>
        /// Called by the spawned object to destroy itself on this managing component.
        /// This could be the case when it has been collected by players.
        /// </summary>
        [PunRPC]
		public void Destroy()
		{
            //despawn object and clear references
			PoolManager.Despawn(obj);
            obj = null;
			
            //if it should respawn again, trigger a new coroutine
//			if(PhotonNetwork.isMasterClient && respawn)******
//            {
//                StartCoroutine(SpawnRoutine());
//            }
		}
        
        
        /// <summary>
        /// Called by the spawned object to reset its respawn counter when it is despawned
        /// in the scene. Also called on all clients with the current counter on host migration.
        /// </summary>
        [PunRPC]
        public void SetRespawn(float init = 0f)
        {
            if(init > 0f)
                nextSpawn = init;
            else
                nextSpawn = (float)PhotonNetwork.time + respawnTime;
        }
	}
}