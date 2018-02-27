using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TanksMP
{

    public class Landmine : ModelWepon 
    {

        //reference to collider component
        private SphereCollider sphereCol1;
        //caching maximum count of bounces for restore
    	// Use this for initialization

        public GameObject zoneTakedamage;

        public GameObject TextBonfire;
        public GameObject Bonfire;

        public GameObject TextFlost;
        public GameObject Flost;

        //get component references
          void Start()
        {
            sphereCol1 = GetComponent<SphereCollider>();

        }

        void OnTriggerEnter(Collider col)
        {
            //cache corresponding gameobject that was hit
            GameObject obj = col.gameObject;

            //try to get a player component out of the collided gameobject
            Player player = obj.GetComponent<Player>();

            //            PlayerBot bot = obj.GetComponent<PlayerBot>();
            //we actually hit a player
            //do further checks
            if (player != null && col.tag == "Bot")
            { 
                //ignore ourselves & disable friendly fire (same team index)
//                if (player.gameObject == owner)
//                    return;
//                else if (obj.tag == owner.tag)
//                    return;
                //                else if (player.GetView().GetTeam() == owner.GetComponent<Player>().GetView().GetTeam()) return;
                //                else if (null != bot && bot == owner.GetComponent<PlayerBot>()) return;

                //create clips and particles on hit
                if (hitFX && stateWepon == 0)
                {
                    PoolManager.Spawn(hitFX, transform.position, Quaternion.identity);
                }
                else if (hitFX && stateWepon == 1)
                {
                    PoolManager.Spawn(TextBonfire, transform.position, Quaternion.identity);
                    PoolManager.Spawn(Bonfire, transform.position, Quaternion.identity);

                    GameObject damagebonfire = PoolManager.Spawn(Bonfire, transform.position, Quaternion.identity);
                    damagebonfire.GetComponent<BonFire>().damage = TakedamageBullet();

                }
                else if (hitFX && stateWepon == 2)
                {
                    PoolManager.Spawn(TextFlost, transform.position, Quaternion.identity);
                    PoolManager.Spawn(Flost, transform.position, Quaternion.identity);
                }

                if(TextLandmind)PoolManager.Spawn(TextLandmind, transform.position, Quaternion.identity);
               
                if (zoneTakedamage)
                {
                    PoolManager.Spawn(zoneTakedamage, transform.position, Quaternion.identity);
                    GameObject Spawndamage = PoolManager.Spawn(zoneTakedamage, transform.position, Quaternion.identity);

                    if(stateWepon == 1)
                        Spawndamage.GetComponent<ZoneTakedamage>().damage = (int)(TakedamageBullet() + (TakedamageBullet() * 0.5));
                    else
                        Spawndamage.GetComponent<ZoneTakedamage>().damage = TakedamageBullet();
                }

                if (hitClip) AudioManager.Play3D(hitClip, transform.position);

                //on the player that was hit, set the killing player to the owner of this bullet
                //maybe this owner really killed the player, but that check is done in the Player script
                player.killedBy = owner;

                gameObject.SetActive(false);
            
            }

            //despawn gameobject

            //the previous code is not synced to clients at all, because all that clients need is the
            //initial position and direction of the bullet to calculate the exact same behavior on their end.
            //at this point, continue with the critical game aspects only on the server
            if (!PhotonNetwork.isMasterClient) return;
            //apply bullet damage to the collided player
        }


            
    }
}