/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace TanksMP
{
	    public class Powerup : Photon.MonoBehaviour 
	{	    
        /// <summary>
        /// Clip to play when this powerup is consumed by a player.
        /// </summary>
        public AudioClip useClip;

        /// <summary>
        /// Reference to the local object (script) that spawned this powerup.
        /// </summary>
        [HideInInspector]
        public ObjectSpawner spawner;
        
        
        //cross-referencing this gameobject in the spawner
        void Start()
        {
            //spawner.obj = gameObject;
        }


        //Server only: check for players colliding with the powerup
        //possible collision are defined in the Physics Matrix        
        void OnTriggerEnter(Collider col)
		{
            if (!PhotonNetwork.isMasterClient)
                return;
            
    		GameObject obj = col.gameObject;
			Player player = obj.GetComponent<Player>();

            //no need to check whether player is null
            //try to apply powerup to player. Destroy after use
            if (Apply(player))
			    spawner.photonView.RPC("Destroy", PhotonTargets.All);
		}


        /// <summary>
        /// Tries to apply the powerup to a colliding player. Returns 'true' if consumed.
        /// Override this method in your own powerup script to implement custom powerups.
        /// </summary>
        public virtual bool Apply(Player p)
		{
            //do something to the player
            //return result
            return false;
		}


        //if consumed, play audio clip. Now with the powerup despawned,
        //set the next spawn time on the managing ObjectSpawner script
        void OnDespawn()
        {
            if (useClip) AudioManager.Play3D(useClip, transform.position);
            spawner.SetRespawn();
        }
    }
}
