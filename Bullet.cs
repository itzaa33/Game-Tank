/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace TanksMP
{
    /// <summary>
    /// Projectile script for player shots with collision/hit logic.
    /// </summary>
    public class Bullet : ModelWepon
    {

        public float despawnDelay = 1f;

        /// <summary>
        /// Bounce count of walls and other environment obstactles.
        /// </summary>
        public int bounce = 0;


        //reference to rigidbody component
        private Rigidbody myRigidbody;
        //reference to collider component
        private SphereCollider sphereCol;

    
        //get component references
        public void Awake()
        {
            myRigidbody = GetComponent<Rigidbody>();
            sphereCol = GetComponent<SphereCollider>();
        }


        //set initial travelling velocity
        //On Host, add automatic despawn coroutine
        void OnSpawn()
        {
            myRigidbody.velocity = speed * transform.forward;
            PoolManager.Despawn(gameObject, despawnDelay);
        }
            


        //check what was hit on collisions
        void OnTriggerEnter(Collider col)
        {
            //cache corresponding gameobject that was hit
            GameObject obj = col.gameObject;

            //try to get a player component out of the collided gameobject
            Player player = obj.GetComponent<Player>();

            ZoneTakedamage Zonedamage = obj.GetComponent<ZoneTakedamage>();

//            PlayerBot bot = obj.GetComponent<PlayerBot>();
            //we actually hit a player
            //do further checks
            if (player != null)
            { 
                //ignore ourselves & disable friendly fire (same team index)
                if (player.gameObject == owner)
                {
                    return;
                }
                else if (obj.tag == owner.tag)
                    return;
                
//                else if (player.GetView().GetTeam() == owner.GetComponent<Player>().GetView().GetTeam()) return;
//                else if (null != bot && bot == owner.GetComponent<PlayerBot>()) return;

                //create clips and particles on hit
                if (hitFX) PoolManager.Spawn(hitFX, transform.position, Quaternion.identity);
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
                if (Physics.SphereCast(ray, sphereCol.radius, out hit, speed, 1 << 0))
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
            else if (Zonedamage)
                return;
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
        void OnDespawn()
        {
            //create clips and particles on despawn
            if (explosionFX) PoolManager.Spawn(explosionFX, transform.position, transform.rotation);

            if (explosionClip) AudioManager.Play3D(explosionClip, transform.position);

            //reset modified variables to the initial state
            myRigidbody.velocity = Vector3.zero;
            myRigidbody.angularVelocity = Vector3.zero;
        }
            
    }
}
