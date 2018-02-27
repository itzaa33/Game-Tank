/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace TanksMP
{          
    /// <summary>
    /// Implementation of AI bots by overriding methods of the Player class.
    /// </summary>
	public class PlayerBot : Player
    {
        //custom properties per PhotonPlayer do not work in offline mode
        //(actually they do, but for objects spawned by the master client,
        //PhotonPlayer is always the local master client. This means that
        //setting custom player properties would apply to all objects)
        [HideInInspector] public string myName;
        [HideInInspector] public int teamIndex;
        [HideInInspector] public int health ;
        [HideInInspector] public int shield;
        [HideInInspector] public int ammo;
        [HideInInspector] public int currentBullet;

        public AudioClip EF_Bornfire;
        public AudioClip Hit_TextBornfire;

        public GameObject door;
        public GameObject EffectStun;
        public GameObject HitStun;

        public GameObject HitBonFire;
        public GameObject EffectBonFire;


        public GameObject EffectFrost;
        //list of enemy players that are in range of this bot
        [HideInInspector]public List<GameObject> inRange = new List<GameObject>();

        /// <summary>
        /// Radius in units for detecting other players.
        /// </summary>
        public float range = 6f;

        //reference to the agent component
        public NavMeshAgent agent;

        public bool CheckStatestun = false; 
        public bool CheckStatestun2 = false; 

        public float countStun = 2;

        //current destination on the navigation mesh
        [HideInInspector]public Vector3 targetPoint;

        //timestamp when next shot should happen
        private float nextShot;

        //toggle for update logic
        private bool isDead = false;

        private Vector3 currentDirection;

        public const byte SEEK_STATE_ID = 1;
        public const byte ATTACK_STATE_ID = 2;
        public const byte STUN_STATE_ID = 3;

        public RaycastHit hit;
        public Fsm<PlayerBot> fsm;



        protected override void Awake()
        {
            base.Awake();

            health = maxHealth;

            fsm = new Fsm<PlayerBot>(this);

            fsm.RegisterState(SEEK_STATE_ID, new SeekState());
            fsm.RegisterState(ATTACK_STATE_ID, new AttackState());
            fsm.RegisterState(STUN_STATE_ID, new StunState());

            fsm.PushState(SEEK_STATE_ID);

        }
        //called before SyncVar updates
        void Start()
        {       

            BotSpawner.upStatusBot.AddListener(UpStatus);

            currentDirection = transform.forward;
            //get components and set camera target
            camFollow = Camera.main.GetComponent<FollowTarget>();
            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;

            //get corresponding team and colorize renderers in team color
            targetPoint = GameManager.GetInstance().GetSpawnPosition(GetView().GetTeam());
            agent.Warp(targetPoint);


            Team team = GameManager.GetInstance().teams[GetView().GetTeam()];
            for(int i = 0; i < renderers.Length; i++)
                renderers[i].material = team.material;
            
			//set name in label
            label.text = myName = "Bot" + System.String.Format("{0:0000}", Random.Range(1, 9999));
            //call hooks manually to update

            if (GetView().GetTeam() == 1)
            {
                agent.speed = 2;
                maxHealth = 50;
                Damage = 6;
            }
            else if (GetView().GetTeam() == 2)
            {
                agent.speed = 1;
                maxHealth = 40 ;
                Damage = 8;
            }
            else if (GetView().GetTeam() == 3)
            {
                agent.speed = 4;
                maxHealth = 30;
                Damage = 4;
            }

//            OnHealthChange(GetView().GetHealth());
            OnHealthChange(maxHealth);
            OnShieldChange(GetView().GetShield());

            door = GameObject.FindGameObjectWithTag("Door");

        }

        void OnTriggerEnter(Collider col)
        {
            if(col.tag == "Door")
                agent.Warp(targetPoint);
        }
            
        void FixedUpdate()
        {
            //don't execute anything if the game is over already,
            //but termine the agent and path finding routines
            if(GameManager.GetInstance().IsGameOver())
            {
                agent.Stop();
                StopAllCoroutines();
                enabled = false;
                return;
            }

            //don't continue if this bot is marked as dead
            if(isDead) return;


            if(CheckStatestun)
            {
                fsm.PushState(STUN_STATE_ID);
            }
            else
            {
                fsm.UpdateState();
            }

            //stat visualization does not update automatically
            //OnHealthChange((int)health);
            OnShieldChange(shield);

        }


        void OnEnable()
        {
            if (EffectBonFire.GetActive() == true)
            {
                EffectBonFire.SetActive(false);

            }


            if (EffectFrost.GetActive() == true)
            {
                EffectFrost.SetActive(false);
                agent.speed = agent.speed * 2;

            }
                
            if (CheckStatestun2)
            {
                fsm.PopState();
                fsm.UpdateState();
                CheckStatestun2 = false;
            }
                 
            StartCoroutine(DetectPlayers());

        }
         
        
        //sets inRange list for player detection
        IEnumerator DetectPlayers()
        {
            //wait for initialization
            yield return new WaitForEndOfFrame();
            
            //detection logic
            while(true)
            {
                //empty list on each iteration
                inRange.Clear();

                //casts a sphere to detect other player objects within the sphere radius
                Collider[] cols = Physics.OverlapSphere(transform.position, range, LayerMask.GetMask("Player"));
                //loop over players found within bot radius
                for (int i = 0; i < cols.Length; i++)
                {
                    //get other Player component
                    //only add the player to the list if its not in this team
                    Player p = cols[i].gameObject.GetComponent<Player>();
                    if(p.GetView().GetTeam() != GetView().GetTeam() && !inRange.Contains(cols[i].gameObject))
                    {
                        inRange.Add(cols[i].gameObject);   
                    }
                }
                
                //wait a second before doing the next range check
                yield return new WaitForSeconds(1);
            }
        }
            
        
        
        //calculate random point for movement on navigation mesh
        public void RandomPoint(Vector3 center, float range, out Vector3 result)
        {
            //clear previous target point
            result = Vector3.zero;
            
            //try to find a valid point on the navmesh with an upper limit (30 times)
            for (int i = 0; i < 30; i++) 
            {
                //find a point in the movement radius
                Vector3 randomPoint = center + (Vector3)Random.insideUnitCircle * range;
                NavMeshHit hit;

                //if the point found is a valid target point, set it and continue
                if (NavMesh.SamplePosition(randomPoint, out hit, 1f, NavMesh.AllAreas)) 
                {
                    result = hit.position;
                    break;
                }
            }
            
            //set the target point as the new destination
            agent.SetDestination(result);
        }


        public void Attack()
        {
                for (int i = 0; i < inRange.Count; i++)
                {
                    RaycastHit hit;
                    //raycast to detect visible enemies and shoot at their current position
                    if (Physics.Linecast(transform.position, inRange[i].transform.position, out hit))
                    {
                            Player p = inRange[i].gameObject.GetComponent<Player>();

                        if (p.GetView().GetTeam() == 0)
                        {
                            agent.destination = inRange[i].transform.position;
                        }
                    }
                }

            for (int i = 0; i < inRange.Count; i++)
            {
                RaycastHit hit;
                //raycast to detect visible enemies and shoot at their current position
                if (Physics.Linecast(transform.position, inRange[i].transform.position, out hit))
                {
                    //get current enemy position and rotate this turret
                    Vector3 lookPos = inRange[i].transform.position;
                    turret.LookAt(lookPos);
                    turret.eulerAngles = new Vector3(0, turret.eulerAngles.y, 0);
                    turretRotation = (short)turret.eulerAngles.y;

                    Vector3 shotDir = lookPos - transform.position;
                    Shoot(new Vector2(shotDir.x, shotDir.z));
                    break;
                }
            }
        }

        [PunRPC]
        protected override void RpcRespawn()
        {
//            StartCoroutine(Respawn());

            isDead = true;
            inRange.Clear();
            agent.Stop();

            if(explosionFX)
            {
                //spawn death particles locally using pooling and colorize them in the player's team color
                GameObject particle = PoolManager.Spawn(explosionFX, transform.position, transform.rotation);
                ParticleColor pColor = particle.GetComponent<ParticleColor>();
                if(pColor) pColor.SetColor(GameManager.GetInstance().teams[GetView().GetTeam()].material.color);
            }

            //play sound clip on player death
            if(explosionClip) AudioManager.Play3D(explosionClip, transform.position);

            FindObjectOfType<BotSpawner>().Despawn(this.gameObject);

            targetPoint = GameManager.GetInstance().GetSpawnPosition(GetView().GetTeam());
            transform.position = targetPoint;
            agent.Warp(targetPoint);
            agent.Resume();
            isDead = false;

        }

        //the actual respawn routine
        IEnumerator Respawn()
        {   
            //stop AI updates
            isDead = true;
            inRange.Clear();
            agent.Stop();
            
            if(explosionFX)
            {
			     //spawn death particles locally using pooling and colorize them in the player's team color
                 GameObject particle = PoolManager.Spawn(explosionFX, transform.position, transform.rotation);
                 ParticleColor pColor = particle.GetComponent<ParticleColor>();
                 if(pColor) pColor.SetColor(GameManager.GetInstance().teams[GetView().GetTeam()].material.color);
            }
				
			//play sound clip on player death
            if(explosionClip) AudioManager.Play3D(explosionClip, transform.position);

            //toggle visibility for all rendering parts (off)
            ToggleComponents(false);
            //wait global respawn delay until reactivation

            yield return new WaitForSeconds(GameManager.GetInstance().respawnTime);

            FindObjectOfType<BotSpawner>().Despawn(this.gameObject);


//            toggle visibility again (on)
            ToggleComponents(true);


            //respawn and continue with pathfinding
            targetPoint = GameManager.GetInstance().GetSpawnPosition(GetView().GetTeam());
            transform.position = targetPoint;
            agent.Warp(targetPoint);
            agent.Resume();
            isDead = false;

        }

        public void Stuner()
        {
            if (CheckStatestun)
            {
                StartCoroutine(Stun(countStun));
            }
        }

        IEnumerator Stun(float time)
        {

            agent.Stop();
//            EffectStun.SetActive(true);
           
//            HitStun.SetActive(true);
            if (EffectStun)
                PoolManager.Spawn(EffectStun, transform.position,Quaternion.identity);

            if (HitStun) 
                PoolManager.Spawn(HitStun, transform.position, Quaternion.identity);

            CheckStatestun = false;
            CheckStatestun2 = true;

            yield return new WaitForSeconds(countStun);

            CheckStatestun2 = false;

            agent.Resume();

            // Pop StunState
            fsm.PopState();
        }



        //disable rendering or blocking components
        public void ToggleComponents(bool state)
        {
            GetComponent<Rigidbody>().isKinematic = state;
            GetComponent<Collider>().enabled = state;

            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(state);

        }

//        void OnDrawGizmos()
//        {
//
//            Gizmos.color = Color.blue;
//            Gizmos.DrawWireSphere(transform.position,range);
//        }

//        void OnDrawLine(Vector3 pos,Vector3 fo, float radius,Color color)
//        {
//            Gizmos.color = color;
//            Gizmos.DrawLine(pos, pos + (fo * radius));
//        }

        public void TakeDamageBot(int damage)
        {
            //store network variables temporary
            float health = healthSlider.value * maxHealth;

            int shield = GetView().GetShield();

            //reduce shield on hit
            if (shield > 0)
            {
                GetView().DecreaseShield(1);
                return;
            }

            health -= damage;

            //bullet killed the player
            if (health <= 0)
            {
                //the game is already over so don't do anything
                if(GameManager.GetInstance().IsGameOver()) return;

                //the maximum score has been reached now
                if(GameManager.GetInstance().IsGameOver())
                {
                    //close room for joining players
                    PhotonNetwork.room.IsOpen = false;
                    return;
                }

                //the game is not over yet, reset runtime values
                //also tell all clients to despawn this player
//                GetView().SetHealth(maxHealth);
                OnHealthChange(maxHealth);
                GetView().SetBullet(0);
                this.photonView.RPC("RpcRespawn", PhotonTargets.All);
            }
            else
            {
                //we didn't die, set health to new value
                OnHealthChange((int)health);
            }

            //substract health by damage
            //locally for now, to only have one update later on

        }

        public void TakedamageBonfire(int damage)
        {
                StartCoroutine(I_Bonfire(damage));
        }

        public void TakedamageFlost(int damage)
        {
            StartCoroutine(I_Flost(damage));
        }


        IEnumerator I_Bonfire(int damage)
        {
            int hit = 3;

            if (EF_Bornfire) AudioManager.Play3D(EF_Bornfire, shotPos.position, 6f);


            for(int i = 0; i < hit; i++)
            {

                if (EffectBonFire) 
                    EffectBonFire.SetActive(true);

//                    HitBonFire.SetActive(true);
                    if (HitBonFire)
                    {
                        PoolManager.Spawn(HitBonFire, transform.position, Quaternion.identity);
                        TakeDamageBot(damage / 2);

                        if (Hit_TextBornfire) AudioManager.Play3D(Hit_TextBornfire, shotPos.position, 0.1f);
                    }

                yield return new WaitForSeconds(2);

               

                if (EffectBonFire) 
                EffectBonFire.SetActive(false);
            }
                
        }   

        IEnumerator I_Flost(int damage)
        {

            TakeDamageBot(damage);

            agent.speed = agent.speed / 2;

            if (EffectFrost) 
            EffectFrost.SetActive(true);

            yield return new WaitForSeconds(5);


            if (EffectFrost) 
            EffectFrost.SetActive(false);

            agent.speed = agent.speed * 2;

            // Pop StunState
           // fsm.PopState();
        }

        public void UpStatus()
        {
            Damage += 2;
            maxHealth += 50;
            health += 50;
            agent.speed += 1;
        }

        public void lastwaveUpdateStatusBot(Player player)
        {
            maxHealth = player.maxHealth;

            health = player.maxHealth;

            Damage = player.Damage * multiplyDamage_Status;

        }
            
    }
}
