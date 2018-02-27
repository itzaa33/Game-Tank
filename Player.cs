/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */
using System;
using UnityEngine;
using UnityEngine.UI;
using Photon;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;

namespace TanksMP
{
    /// <summary>
    /// Networked player class implementing movement control and shooting.
    /// Contains both server and client logic in an authoritative approach.
    /// </summary> 

    public class Player : PunBehaviour
    {
        /// <summary>
        /// UI Text displaying the player name.
        /// </summary>    
        public Text label;

        /// <summary>
        /// Maximum health value at game start.
        /// </summary>
        public int maxHealth = 10;

        public int Hpbonus = 0;
        /// <summary>
        /// Current turret rotation and shooting direction.
        /// </summary>
        [HideInInspector]
        public short turretRotation;

        /// <summary>
        /// Delay between shots.
        /// </summary>
        public float fireRate = 0.75f;

        /// <summary>
        /// Movement speed in all directions.
        /// </summary>
        public float moveSpeed = 8f;

        public int Damage = 0;

        /// <summary>
        /// UI Slider visualizing health value.
        /// </summary>
        public Slider healthSlider;

        /// <summary>
        /// UI Slider visualizing shield value.
        /// </summary>
        public Slider shieldSlider;

        /// <summary>
        /// Clip to play when a shot has been fired.
        /// </summary>
        public AudioClip shotClip;

        /// <summary>
        /// Clip to play on player death.
        /// </summary>
        public AudioClip explosionClip;

        /// <summary>
        /// Object to spawn on shooting.
        /// </summary>
        public GameObject shotFX;

        /// <summary>
        /// Object to spawn on player death.
        /// </summary>
        public GameObject explosionFX;

        /// <summary>
        /// Turret to rotate with look direction.
        /// </summary>
        public Transform turret;

        public GameObject ZoneBome;

        /// <summary>
        /// Position to spawn new bullets at.
        /// </summary>
        public Transform shotPos;
        public Transform shotPos2;
        public Transform lastProjectilePosition;

        private Vector3 Vec_ZoneBom;

        /// <summary>
        /// Array of available bullets for shooting.
        /// </summary>

        public GameObject[] bullets;



        public int currentBullet = 1;

        public int StateWepon = 0;

        /// <summary>
        /// MeshRenderers that should be highlighted in team color.
        /// </summary>
        public Material[] material;
        public MeshRenderer[] renderers;

        public float defaultBulletSpeed = 10;

        /// <summary>
        /// Last player gameobject that killed this one.
        /// </summary>
        [HideInInspector]
            public GameObject killedBy;

        /// <summary>
        /// Reference to the camera following component.
        /// </summary>
        [HideInInspector]
        public FollowTarget camFollow;

        //timestamp when next shot should happen
        private float nextFire;
        private float m_BulletSpeed;
        
        //reference to this rigidbody
        #pragma warning disable 0649
		private Rigidbody rb;
		#pragma warning restore 0649

        private List<Vector3> point = new List<Vector3>(); 
        private LineRenderer renderLine;
        private bool drawLine;

        public bool landmine = false;
        public bool projectile = false;

        public int multiplyDamage_Status = 1;
        private int Hp_Status = 0;
        private int Speed_Status = 0;

        //initialize server values for this player
        protected virtual void Awake()
        {
            //only let the master do initialization
            if(!PhotonNetwork.isMasterClient)
                return;
            
            //set players current health value after joining
            GetView().SetHealth(maxHealth);
            MVC.EventManager.onHitTriggerEvent.AddListener(Apply);
        }


        /// <summary>
        /// Initialize synced values on every client.
        /// Initialize camera and input for this local client.
        /// </summary>
        void Start()
        {        

            m_BulletSpeed = defaultBulletSpeed;
            lastProjectilePosition.SetParent(null);

            renderLine = GetComponent<LineRenderer>();
			//get corresponding team and colorize renderers in team color
            Team team = GameManager.GetInstance().teams[GetView().GetTeam()];
//            for(int i = 0; i < renderers.Length; i++)
//                renderers[i].material = team.material;

            //set name in label
            label.text = GetView().GetName();
            //call hooks manually to update
//            OnHealthChange(GetView().GetHealth());
            OnHealthChange(maxHealth);
            OnShieldChange(GetView().GetShield());

            //called only for this client 
            if (!photonView.isMine)
                return;

			//set a global reference to the local player
            GameManager.GetInstance().localPlayer = this;

			//get components and set camera target
            rb = GetComponent<Rigidbody>();
            camFollow = Camera.main.GetComponent<FollowTarget>();
            camFollow.target = turret;

			//initialize input controls for mobile devices
			//[0]=left joystick for movement, [1]=right joystick for shooting
            #if !UNITY_STANDALONE && !UNITY_WEBGL
            GameManager.GetInstance().ui.controls[0].onDrag += Move;
            GameManager.GetInstance().ui.controls[0].onDragEnd += MoveEnd;

            GameManager.GetInstance().ui.controls[1].onDragBegin += ShootBegin;
            GameManager.GetInstance().ui.controls[1].onDrag += RotateTurret;
            GameManager.GetInstance().ui.controls[1].onDrag += ShootAndLandMineUi;

            GameManager.GetInstance().ui.controls[1].onEnter += ShootAndLandMineUi;

            GameManager.GetInstance().ui.controls[1].onEnterDown += DrawLineProjectileUi;
            GameManager.GetInstance().ui.controls[1].onEnterUp += ShootProjectileUi;

            #endif

            StartCoroutine(OnProjectileShootRoutine());
        }
        void OnEnable()
        {
            OnHealthChange(maxHealth);
            StartCoroutine(OnProjectileShootRoutine());
        }
            
        /// <summary>
        /// This method gets called whenever player properties have been changed on the network.
        /// </summary>
//        public override void OnPhotonPlayerPropertiesChanged(object[] playerAndUpdatedProps)******************************
//        {
//            //only react on property changes for this player
//            PhotonPlayer player = playerAndUpdatedProps[0] as PhotonPlayer;
//            if(player != photonView.owner)
//                return;
//
//            //update values that could change any time for visualization to stay up to date
//            OnHealthChange(player.GetHealth());
//            OnShieldChange(player.GetShield());
//        }
        
        
        //this method gets called multiple times per second, at least 10 times or more
        void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {        
            if (stream.isWriting)
            {             
                //here we send the turret rotation angle to other clients
                stream.SendNext(turretRotation);
            }
            else
            {   
                //here we receive the turret rotation angle from others and apply it
                this.turretRotation = (short)stream.ReceiveNext();
                OnTurretRotation();
            }
        }


        //continously check for input on desktop platforms
        #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        void FixedUpdate()
        {
            //skip further calls for remote clients    
            if (!photonView.isMine)
            {
                //keep turret rotation updated for all clients
                OnTurretRotation();
                return;
            }

            //movement variables
            Vector2 moveDir;
            Vector2 turnDir;

            //reset moving input when no arrow keys are pressed down
            if (Input.GetAxisRaw("Horizontal") == 0 && Input.GetAxisRaw("Vertical") == 0)
            {
                moveDir.x = 0;
                moveDir.y = 0;
            }
            else
            {
                //read out moving directions and calculate force
                moveDir.x = Input.GetAxis("Horizontal");
                moveDir.y = Input.GetAxis("Vertical");
                Move(moveDir);
            }

            //cast a ray on a plane at the mouse position for detecting where to shoot 
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, Vector3.up);
            float distance = 0f;
            Vector3 hitPos = Vector3.zero;
            //the hit position determines the mouse position in the scene
            if (plane.Raycast(ray, out distance))
            {
                hitPos = ray.GetPoint(distance) - transform.position;
            }

            //we've converted the mouse position to a direction
            turnDir = new Vector2(hitPos.x, hitPos.z);

            //rotate turret to look at the mouse direction
            RotateTurret(new Vector2(hitPos.x, hitPos.z));

            //----------------------------------------------------------------------
//            UIJoystick Uijoy = FindObjectOfType<UIJoystick>();
//            //shoot bullet on left mouse click
//            if (projectile == false && landmine == false)
//            {
//                Uijoy.UIBullet();
//
//                if (CrossPlatformInputManager.GetButton("Fire"))
//                {
//                    Debug.Log("fire");
//                    Shoot();
//                }
//
//            }
//            else if (projectile == true && landmine == false)
//            {
//                Uijoy.UIProjectile();
//
//                if (CrossPlatformInputManager.GetButton("Fire"))
//                {
//                    m_BulletSpeed += Time.deltaTime * 5f;
//                    DrawLineProjectile();
//                    Debug.Log("Projectile");
//                }
//                else if (CrossPlatformInputManager.GetButtonUp("Fire"))
//                {
//
//                    ShootProjectile();
//                    Debug.Log("ProjectileUp");
//                    projectile = false;
//
//                    m_BulletSpeed = defaultBulletSpeed;
//                    renderLine.numPositions = 0;
//
//                    lastProjectilePosition.gameObject.SetActive(false);
//
//
//                   
//                }
//            }
//            else if (landmine == true && projectile == false)
//            {
//                Uijoy.UILandmine();
//
//                if (CrossPlatformInputManager.GetButton("Fire"))
//                {
//                    Debug.Log("Landmine");
//                    //                        PoolManager.Spawn(landMine, transform.position, Quaternion.identity);
//                    GameObject obj = PoolManager.Spawn(bullets[2], transform.position, Quaternion.identity);
//
//                    obj.GetComponent<Landmine>().stateWepon = StateWepon;
//
//                    landmine = false;
//
//                }
//            }
//            else
//            {
//                landmine = false;
//                projectile = false;
//            }
               

 

			//replicate input to mobile controls for illustration purposes
			#if UNITY_EDITOR
				GameManager.GetInstance().ui.controls[0].position = moveDir;
				GameManager.GetInstance().ui.controls[1].position = turnDir;
			#endif
        }
        #endif

        //--------------------------------------------------
        private float GetMaxHeightFromLineProjectile()
        {
            Vector3 g = Physics.gravity;
            Vector3 p1 = shotPos2.position;

            Vector3 u = m_BulletSpeed * shotPos2.forward;

            int bulletLayer = LayerMask.NameToLayer("Bullet");

            float h = 0;

            for(float t = 0; t < 2f; t += 0.01f)
            {
                Vector3 s = (u * t) + (0.5f * g * t * t);
                Vector3 p2 = p1 + s;

                RaycastHit hit;
                if (Physics.Linecast(p1, p2, out hit))
                {
                    if (hit.collider.gameObject.layer != bulletLayer)
                    {
                        break;
                    }
                }

                if (p2.y - shotPos2.position.y > h)
                {
                    h = p2.y - shotPos2.position.y;
                }

//                point.Add(p2);

                p1 = p2;
//                Vec_ZoneBom = p2;

                u = u + (g * t);
            }

            return h+shotPos2.position.y;
        }

        public void DrawLineProjectile()
        {
//            int speed = 15;
            Vector3 g = Physics.gravity;
            Vector3 p1 = shotPos2.position;

            Vector3 u = m_BulletSpeed * shotPos2.forward;

            point.Clear();

            int bulletLayer = LayerMask.NameToLayer("Bullet");

            for(float t = 0; t < 2f; t += 0.01f)
            {
                Vector3 s = (u * t) + (0.5f * g * t * t);
                Vector3 p2 = p1 + s;

                RaycastHit hit;
                if (Physics.Linecast(p1, p2, out hit))
                {
                    point.Add(hit.point + Vector3.up);

                    if (hit.collider.gameObject.layer != bulletLayer)
                    {
                        break;
                    }
                }

                point.Add(p2);

                p1 = p2;
                Vec_ZoneBom = p2;

                u = u + (g * t);
            }
            Vector3 last = point[point.Count - 1];

            lastProjectilePosition.gameObject.SetActive(true);
            lastProjectilePosition.position = last;

            renderLine.positionCount = point.Count;
            renderLine.SetPositions(point.ToArray());

        }
            
        //-------------------------------------------------

      
        /// <summary>
        /// Helper method for getting the current object owner.
        /// </summary>
        public PhotonView GetView()
        {
            return this.photonView;
        }


        //moves rigidbody in the direction passed in
        void Move(Vector2 direction = default(Vector2))
        {
            //if direction is not zero, rotate player in the moving direction relative to camera
            if (direction != Vector2.zero)
                transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y))
                                     * Quaternion.Euler(0, camFollow.camTransform.eulerAngles.y, 0);

            //create movement vector based on current rotation and speed
            Vector3 movementDir = transform.forward * moveSpeed * Time.deltaTime;
            //apply vector to rigidbody position
            rb.MovePosition(rb.position + movementDir);
        }


        //on movement drag ended
        void MoveEnd()
        {
            //reset rigidbody physics values
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }


        //rotates turret to the direction passed in
        void RotateTurret(Vector2 direction = default(Vector2))
        {
            //don't rotate without values
            if (direction == Vector2.zero)
                return;

            //get rotation value as angle out of the direction we received
            turretRotation = (short)Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y)).eulerAngles.y;
            OnTurretRotation();
        }


        //on shot drag start set small delay for first shot
        void ShootBegin()
        {
            nextFire = Time.time + 0.25f;
        }


        //shoots a bullet in the direction passed in
        //we do not rely on the current turret rotation here, because we send the direction
        //along with the shot request to the server to absolutely ensure a synced shot position
        public void Shoot(Vector2 direction = default(Vector2))
        {
            //if shot delay is over  
            if (Time.time > nextFire)
            {
                //set next shot timestamp
                nextFire = Time.time + fireRate;
                
                //send current client position and turret rotation along to sync the shot position
                //also we are sending it as a short array (only x,z - skip y) to save additional bandwidth
                short[] pos = new short[] { (short)(shotPos.position.x * 10), (short)(shotPos.position.z * 10)};
                //send shot request with origin to server
                this.photonView.RPC("CmdShoot", PhotonTargets.AllViaServer, pos, turretRotation);

            }
        }
        
        
        //called on the server first but forwarded to all clients
        [PunRPC]
        protected void CmdShoot(short[] position, short angle)
        {   
            //get current bullet type
//            int currentBullet = GetView().GetBullet();
            int currentBullet = 0;
            //calculate center between shot position sent and current server position (factor 0.6f = 40% client, 60% server)
            //this is done to compensate network lag and smoothing it out between both client/server positions
            Vector3 shotCenter = Vector3.Lerp(shotPos.position, new Vector3(position[0]/10f, shotPos.position.y, position[1]/10f), 0.6f);
            Quaternion syncedRot = turret.rotation = Quaternion.Euler(0, angle, 0);


            //spawn bullet using pooling
            GameObject obj = PoolManager.Spawn(bullets[currentBullet], shotCenter, syncedRot);

            obj.GetComponent<Bullet>().owner = gameObject;
            obj.GetComponent<Bullet>().damage = Damage * multiplyDamage_Status;

            //check for current ammunition
            //let the server decrease special ammunition, if present
            if (PhotonNetwork.isMasterClient && currentBullet != 0)
            {
                //if ran out of ammo: reset bullet automatically
                GetView().DecreaseAmmo(1);
            }

            //send event to all clients for spawning effects
            if (shotFX || shotClip)
                RpcOnShot();

            MVC.EventManager.onCheckAmountIndex.Invoke(currentBullet);
        }

        IEnumerator OnProjectileShootRoutine()
        {
            while (true)
            {

                UIJoystick Uijoy = FindObjectOfType<UIJoystick>();
                //shoot bullet on left mouse click
                if (projectile == false && landmine == false)
                {
                    Uijoy.UIBullet();

                    if (Input.GetButton("Fire1"))
                        Shoot();

                }
                else if (projectile == true && landmine == false)
                {
                    Uijoy.UIProjectile();

                    if (Input.GetButton("Fire1"))
                    {
                        m_BulletSpeed += Time.deltaTime * 5f;
                        DrawLineProjectile();

                    }
                    else if (Input.GetButtonUp("Fire1"))
                    {

                        ShootProjectile();

                        projectile = false;

                        m_BulletSpeed = defaultBulletSpeed;
                        renderLine.positionCount = 0;

                        lastProjectilePosition.gameObject.SetActive(false);

                        yield return new WaitForSeconds(fireRate);
                    }
                }
                else if (landmine == true  && projectile == false)
                {
                    Uijoy.UILandmine();

                    if (Input.GetButton("Fire1"))
                    {
//                        PoolManager.Spawn(landMine, transform.position, Quaternion.identity);
                        GameObject obj = PoolManager.Spawn(bullets[2], transform.position, Quaternion.identity);

                        obj.GetComponent<Landmine>().stateWepon = StateWepon;

                        landmine = false;

                    }
                }
                else
                {
                    landmine = false;
                    projectile = false;
                }

                yield return null;
            }
        }

        public void ShootAndLandMineUi(Vector2 direction = default(Vector2))
        {
            UIJoystick Uijoy = FindObjectOfType<UIJoystick>();
            //shoot bullet on left mouse click
            if (projectile == false && landmine == false)
            {
                Uijoy.UIBullet();

                Shoot();

            }
            else if (landmine == true && projectile == false)
            {
                    GameObject obj = PoolManager.Spawn(bullets[2], transform.position, Quaternion.identity);

                    obj.GetComponent<Landmine>().stateWepon = StateWepon;

                    landmine = false;

            }
        }

        public void DrawLineProjectileUi()
        {

            if (projectile == true && landmine == false)
            {

                m_BulletSpeed += Time.deltaTime * 5f;

                DrawLineProjectile();

            }

        }

        public void ShootProjectileUi()
        {
            if (projectile == true && landmine == false)
            {
                ShootProjectile();

                projectile = false;

                m_BulletSpeed = defaultBulletSpeed;
                renderLine.positionCount = 0;

                lastProjectilePosition.gameObject.SetActive(false);
            }
        }
            

        //---------------------------------------------------------------------------------------------
        public void ShootProjectile(Vector2 direction = default(Vector2))
        {
           
            {
                short[] pos = new short[] { (short)(shotPos2.position.x * 10), (short)(shotPos2.position.z * 10)};
                //send shot request with origin to server
                this.photonView.RPC("CmdShootProjectile", PhotonTargets.AllViaServer, pos, turretRotation);

            }
        }


        //called on the server first but forwarded to all clients
        [PunRPC]
        protected void CmdShootProjectile(short[] position, short angle)
        {   
            //get current bullet type

            //calculate center between shot position sent and current server position (factor 0.6f = 40% client, 60% server)
            //this is done to compensate network lag and smoothing it out between both client/server positions
            if (currentBullet > 0)
            {
                Vector3 shotCenter = Vector3.Lerp(shotPos2.position, new Vector3(position[0] / 10f, shotPos2.position.y, position[1] / 10f), 0.6f);
                Quaternion syncedRot = turret.rotation = Quaternion.Euler(0, angle, 0);

                //spawn bullet using pooling
                GameObject obj = PoolManager.Spawn(bullets[1], shotCenter, syncedRot);
                obj.GetComponent<Bullet>().owner = gameObject;
//                obj.GetComponent<Bullet>().damage = Damage;

                obj.GetComponent<Bullet_Projectile>().stateWepon = StateWepon;

                obj.GetComponent<Bullet_Projectile>().MoveProjectile(shotPos2.forward.normalized, m_BulletSpeed);


//            obj.GetComponent<Bullet>().transform.rotation = shotPos2.rotation;
                GameObject zone = Instantiate(ZoneBome, Vec_ZoneBom, Quaternion.identity);

                BomZone bomZone = zone.GetComponent<BomZone>();
                bomZone.owner = obj;
            }
            //bomZone.alive = true;

            //check for current ammunition
            //let the server decrease special ammunition, if present
            if (PhotonNetwork.isMasterClient && currentBullet != 0)
            {
                //if ran out of ammo: reset bullet automatically
                GetView().DecreaseAmmo(1);
            }

            //send event to all clients for spawning effects
            if (shotFX || shotClip)
                RpcOnShot();

        }

        //---------------------------------------------------------------------------------------------


        //called on all clients after bullet spawn
        //spawn effects or sounds locally, if set
        protected void RpcOnShot()
        {
            if (shotFX) PoolManager.Spawn(shotFX, shotPos.position, Quaternion.identity);
            if (shotClip) AudioManager.Play3D(shotClip, shotPos.position, 0.1f);
        }


        //hook for updating turret rotation locally
        void OnTurretRotation()
        {
            //we don't need to check for local ownership when setting the turretRotation,
            //because OnPhotonSerializeView PhotonStream.isWriting == true only applies to the owner
            turret.rotation = Quaternion.Euler(0, turretRotation, 0);
        }


        //hook for updating health locally
        //(the actual value updates via player properties)
        public void OnHealthChange(int value)
        {
           
            healthSlider.value = (float)value / maxHealth;
        }


        //hook for updating shield locally
        //(the actual value updates via player properties)
        protected void OnShieldChange(int value)
        {
            shieldSlider.value = value;
        }


        /// <summary>
        /// Server only: calculate damage to be taken by the Player,
		/// triggers score increase and respawn workflow on death.
        /// </summary>
        public void TakeDamage(Bullet bullet)
        {
            //store network variables temporary
//            int health = GetView().GetHealth();
            float health = healthSlider.value * maxHealth;
            int shield = GetView().GetShield();
            //reduce shield on hit
            if (shield > 0)
            {
                GetView().DecreaseShield(1);
                return;
            }

            //substract health by damage
            //locally for now, to only have one update later on
            health -= (bullet.damage + bullet.Bonusdamage);

            //bullet killed the player
            if (health <= 0)
            {
                //the game is already over so don't do anything
                if(GameManager.GetInstance().IsGameOver()) return;

                //get killer and increase score for that team
                Player other = bullet.owner.GetComponent<Player>();
                int otherTeam = other.GetView().GetTeam();
                PhotonNetwork.room.AddScore(otherTeam);

                //the maximum score has been reached now
                if(GameManager.GetInstance().IsGameOver())
                {
                    //close room for joining players
                    PhotonNetwork.room.IsOpen = false;
                    //tell all clients the winning team
                    this.photonView.RPC("RpcGameOver", PhotonTargets.All, (byte)otherTeam);
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
//                GetView().SetHealth(health);
                OnHealthChange((int)health);

            }
        }


        //called on all clients on both player death and respawn
        //only difference is that on respawn, the client sends the request
        [PunRPC]
        protected virtual void RpcRespawn()
        {
            //toggle visibility for player gameobject (on/off)
            gameObject.SetActive(!gameObject.activeInHierarchy);
            bool isActive = gameObject.activeInHierarchy;

            //the player has been killed
            if (!isActive)
            {
                //detect whether the current user was responsible for the kill
                //yes, that's my kill: take thumbnail via EveryPlay
                if (killedBy == GameManager.GetInstance().localPlayer.gameObject)
                    UnityEveryplayManager.TakeThumbnail();

                if (explosionFX)
                {
                    //spawn death particles locally using pooling and colorize them in the player's team color
                    GameObject particle = PoolManager.Spawn(explosionFX, transform.position, transform.rotation);
                    ParticleColor pColor = particle.GetComponent<ParticleColor>();
                    if (pColor) pColor.SetColor(GameManager.GetInstance().teams[GetView().GetTeam()].material.color);
                }

                //play sound clip on player death
                if (explosionClip) AudioManager.Play3D(explosionClip, transform.position);
            }

            //further changes only affect the local client
            if (!photonView.isMine)
                return;

            //local player got respawned so reset states
            if (isActive == true)
                ResetPosition();
            else
            {
                //local player was killed, set camera to follow the killer
                camFollow.target = killedBy.transform;
                //hide input controls and other HUD elements
                camFollow.HideMask(true);
                //display respawn window (only for local player)
                GameManager.GetInstance().DisplayDeath();
            }
        }


        /// <summary>
        /// Command telling the server and all others that this client is ready for respawn.
        /// This is when the respawn delay is over or a video ad has been watched.
        /// </summary>
        public void CmdRespawn()
        {
            this.photonView.RPC("RpcRespawn", PhotonTargets.AllViaServer);
        }


        /// <summary>
        /// Repositions in team area and resets camera & input variables.
        /// This should only be called for the local player.
        /// </summary>
        public void ResetPosition()
        {
            //start following the local player again
            camFollow.target = turret;
            camFollow.HideMask(false);

            //get team area and reposition it there
            transform.position = GameManager.GetInstance().GetSpawnPosition(GetView().GetTeam());

            //reset forces modified by input
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.rotation = Quaternion.identity;
            //reset input left over
            GameManager.GetInstance().ui.controls[0].OnEndDrag(null);
            GameManager.GetInstance().ui.controls[1].OnEndDrag(null);
        }


        //called on all clients on game end providing the winning team
        [PunRPC]
        protected void RpcGameOver(byte teamIndex)
        {
            //display game over window
            GameManager.GetInstance().DisplayGameOver(teamIndex);
        }

        public void Apply(GameObject obj,Collider col)
        {

            MVC.Model.ItemModel model = obj.GetComponent< MVC.Model.ItemModel>();
            Player player = col.GetComponent<Player>();

            if(player)
            {
                int value = player.GetView().GetAmmo();
                int index = player.GetView().GetBullet();
                    
                if (value != model.Amount && index != model.BulletIndex)
                {
                    player.GetView().SetAmmo(model.Amount, model.BulletIndex);
                    model.spawner.photonView.RPC("Destroy", PhotonTargets.All);
                }
            }

        }

        public void UpstatusPlayer()
        {
            multiplyDamage_Status += GameManager.multiplyDamage_Status;
            maxHealth += GameManager.Hp_Status;
            moveSpeed += (float) GameManager.Speed_Status;

            OnHealthChange(maxHealth);

        }

        public void changeElement(int element)
        {
            if (element == 1)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].material = material[0];
                }

                Time.timeScale = 1;

                maxHealth += 50;

                Damage += 10;

                ModelWepon.AddDamageElement += 10;

                StateWepon = 1;



                OnHealthChange(maxHealth);
            }

            else if(element == 2)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].material = material[1];
                }

                Time.timeScale = 1;

                maxHealth += 100;

                Damage += 6;

                ModelWepon.AddDamageElement += 6;

                StateWepon = 2;

                OnHealthChange(maxHealth);

            }

        }


    }
}