/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace TanksMP
{
    /// <summary>
    /// Projectile script for player shots with collision/hit logic.
    /// </summary>
    public class Bullet_Projectile : Bullet
    {


        public GameObject hitTextBonfire;
        public GameObject hitThunder;
        public GameObject hitTextThunder;
        public GameObject EffectBonfire;
        public GameObject ZoneTakeDamage;

        
        //reference to rigidbody component
        private Rigidbody rigi;
        //reference to collider component
        private SphereCollider sphere;
        //caching maximum count of bounces for restore



        //-----------------------------------------
        public Vector3 forward ;
        private Vector3 u;
        private Vector3 Origin ;

        private List<Vector3> point = new List<Vector3>(); 

        private Vector3 height; 
        //------------------------------------------
        //get component references
        void Awake()
        {
            rigi = GetComponent<Rigidbody>();
            sphere = GetComponent<SphereCollider>();
        }

        void Start()
        {
            Origin = transform.position;

        }
  
        //set initial travelling velocity
        //On Host, add automatic despawn coroutine
        void OnSpawn()
        {
            

//            Vector3 direction = Player.shotPos2.transform.forward;
//            Vector3 u = v * direction;
            PoolManager.Despawn(gameObject, despawnDelay);
        }

        void Update()
        {
            transform.forward = rigi.velocity.normalized;

        }
            

        public void MoveProjectile(Vector3 forward, float speed)
        {
            transform.forward = forward;

             u = speed * transform.forward;

            rigi.velocity = u;

 

        }
            
        //check what was hit on collisions
        void OnTriggerEnter(Collider col)
        {
            //cache corresponding gameobject that was hit
            GameObject obj = col.gameObject;
            //try to get a player component out of the collided gameobject
            Player player = obj.GetComponent<Player>();

            //we actually hit a player
            //do further checks
            if (player != null)
            {
                //ignore ourselves & disable friendly fire (same team index)
                if (player.gameObject == owner) return;
                else if (player.GetView().GetTeam() == owner.GetComponent<Player>().GetView().GetTeam()) return;

                //create clips and particles on hit
//                if (hitFX && stateWepon == 0)
//                {
//                    PoolManager.Spawn(hitFX, transform.position, Quaternion.identity);
//                    PoolManager.Spawn(ZoneTakeDamage, transform.position, Quaternion.identity);
//                }
////                else if (hitTextBonfire && stateWepon == 1)
////                {
////                    PoolManager.Spawn(hitTextBonfire, transform.position, Quaternion.identity);
////                    PoolManager.Spawn(EffectBonfire, transform.position, Quaternion.identity);
////
////                    GameObject damagebonfire = PoolManager.Spawn(EffectBonfire, transform.position, Quaternion.identity);
////                    damagebonfire.GetComponent<BonFire>().damage = TakedamageBullet();
////                    Debug.Log(TakedamageBullet());
////                }
//                else if (hitThunder && stateWepon == 2)
//                {
//                    PoolManager.Spawn(hitThunder, transform.position, Quaternion.identity);
//
//                }
//
//                if (ZoneTakeDamage)
//                {
//                    PoolManager.Spawn(ZoneTakeDamage, transform.position, Quaternion.identity);
//                    GameObject Spawndamage = PoolManager.Spawn(ZoneTakeDamage, transform.position, Quaternion.identity);
//                    Spawndamage.GetComponent<ZoneTakedamage>().damage = TakedamageBullet();
//                }

                if (hitClip) AudioManager.Play3D(hitClip, transform.position);

                //on the player that was hit, set the killing player to the owner of this bullet
                //maybe this owner really killed the player, but that check is done in the Player script
                player.killedBy = owner;
            }
            else if (bounce > 0)
            {
                //a player was not hit but something else, and we still have some bounces left
                //create a ray that points in the direction this bullet is currently flying to
                Ray ray = new Ray(transform.position - transform.forward, transform.forward);
                RaycastHit hit;

                //perform spherecast in the flying direction, on the default layer
                if (Physics.SphereCast(ray, sphere.radius, out hit, speed, 1 << 0))
                {
                    //something was hit in the direction this projectile is flying to
                    //get new reflected (bounced off) direction of the colliding object
                    Vector3 dir = Vector3.Reflect(ray.direction, hit.normal);
                    //rotate bullet to face the new direction
                    transform.rotation = Quaternion.LookRotation(dir);
                    //reassign velocity with the new direction in mind
                    OnSpawn();

                    //play clip at the collided position
                    if (hitClip) AudioManager.Play3D(hitClip, transform.position);
                    //substract bouncing count by one
                    bounce--;
                    //exit execution until next collision
                    return;
                }
            }
            else if (col.tag == "Door")
                return;

            //despawn gameobject
            PoolManager.Despawn(gameObject);

            //the previous code is not synced to clients at all, because all that clients need is the
            //initial position and direction of the bullet to calculate the exact same behavior on their end.
            //at this point, continue with the critical game aspects only on the server
            if (!PhotonNetwork.isMasterClient) return;
            //apply bullet damage to the collided player
            if (player) player.TakeDamage(this);
        }


        //set despawn effects and reset variables
        public void OnDespawn()
        {
            //create clips and particles on despawn
            if (explosionFX && stateWepon == 0)
            {
                PoolManager.Spawn(explosionFX, transform.position, transform.rotation);

                PoolManager.Spawn(ZoneTakeDamage, transform.position, Quaternion.identity);
                GameObject objTakedamage = PoolManager.Spawn(ZoneTakeDamage, transform.position, Quaternion.identity);

                objTakedamage.GetComponent<ZoneTakedamage>().damage = TakedamageBullet();
            }

            else if (hitTextBonfire && stateWepon == 1)
            {
                PoolManager.Spawn(hitTextBonfire, transform.position, Quaternion.identity);
                PoolManager.Spawn(EffectBonfire, transform.position, Quaternion.identity);

                GameObject damagebonfire = PoolManager.Spawn(EffectBonfire, transform.position, Quaternion.identity);
                damagebonfire.GetComponent<BonFire>().damage = TakedamageBullet();

            }
            else if (hitThunder && stateWepon == 2)
            {
                PoolManager.Spawn(hitThunder, transform.position, Quaternion.identity);
                PoolManager.Spawn(hitTextThunder, transform.position, Quaternion.identity);
            }


            if (explosionClip) AudioManager.Play3D(explosionClip, transform.position);

            if (ZoneTakeDamage)
            {
                PoolManager.Spawn(ZoneTakeDamage, transform.position, Quaternion.identity);
                GameObject Spawndamage = PoolManager.Spawn(ZoneTakeDamage, transform.position, Quaternion.identity);
                Spawndamage.GetComponent<ZoneTakedamage>().damage = TakedamageBullet();
            }

            if (explosionClip) AudioManager.Play3D(explosionClip, transform.position);
            //reset modified variables to the initial state
            rigi.velocity = Vector3.zero;
            rigi.angularVelocity = Vector3.zero;
        }


    }
}
