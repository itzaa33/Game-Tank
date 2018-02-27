/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace TanksMP
{
    /// <summary>
    /// This class extends Photon's PhotonPlayer object by custom properties.
    /// Provides several methods for setting and getting variables out of them.
    /// </summary>
    public static class PlayerExtensions
    {
        //keys for saving and accessing values in custom properties Hashtable
        public const string team = "team";
        public const string health = "health";
        public const string shield = "shield";
        public const string ammo = "ammo";
        public const string bullet = "bullet";


        /// <summary>
        /// Returns the networked player nick name.
        /// Offline: bot name. Online: PhotonPlayer name.
        /// </summary>
        public static string GetName(this PhotonView player)
        {
            if (PhotonNetwork.offlineMode == true)
            {
                PlayerBot bot = player.GetComponent<PlayerBot>();
                if (bot != null)
                {
                    return bot.myName;
                }
            }

            return player.owner.NickName;
        }

        /// <summary>
        /// Offline: returns the team number of a bot stored in PlayerBot.
        /// Fallback to online mode for the master or in case offline mode was turned off.
        /// </summary>
        public static int GetTeam(this PhotonView player)
        {
            if (PhotonNetwork.offlineMode == true)
            {
                PlayerBot bot = player.GetComponent<PlayerBot>();
                if (bot != null)
                {
                    return bot.teamIndex;
                }
            }

            return player.owner.GetTeam();
        }

        /// <summary>
        /// Online: returns the networked team number of the player out of properties.
        /// </summary>
        public static int GetTeam(this PhotonPlayer player)
        {
            return System.Convert.ToInt32(player.CustomProperties[team]);
        }

        /// <summary>
        /// Offline: synchronizes the team number of a PlayerBot locally.
        /// Fallback to online mode for the master or in case offline mode was turned off.
        /// </summary>
        public static void SetTeam(this PhotonView player, int teamIndex)
        {
            if (PhotonNetwork.offlineMode == true)
            {
                PlayerBot bot = player.GetComponent<PlayerBot>();
                if (bot != null)
                {
                    bot.teamIndex = teamIndex;
                    return;
                }
            }

            player.owner.SetTeam(teamIndex);
        }

        /// <summary>
        /// Online: synchronizes the team number of the player for all players via properties.
        /// </summary>
        public static void SetTeam(this PhotonPlayer player, int teamIndex)
        {
            player.SetCustomProperties(new Hashtable() { { team, (byte)teamIndex } });
        }

        /// <summary>
        /// Offline: returns the health value of a bot stored in PlayerBot.
        /// Fallback to online mode for the master or in case offline mode was turned off.
        /// </summary>
        public static int GetHealth(this PhotonView player)
        {
            if (PhotonNetwork.offlineMode == true)
            {
                PlayerBot bot = player.GetComponent<PlayerBot>();
                if (bot != null)
                {
                    return bot.health;
                }
            }

            return player.owner.GetHealth();
        }

        /// <summary>
        /// Online: returns the networked health value of the player out of properties.
        /// </summary>
        public static int GetHealth(this PhotonPlayer player)
        {
            return System.Convert.ToInt32(player.CustomProperties[health]);
        }

        /// <summary>
        /// Offline: synchronizes the health value of a PlayerBot locally.
        /// Fallback to online mode for the master or in case offline mode was turned off.
        /// </summary>
        public static void SetHealth(this PhotonView player, int value)
        {
            if (PhotonNetwork.offlineMode == true)
            {
                PlayerBot bot = player.GetComponent<PlayerBot>();
                if (bot != null)
                {
                    bot.health = value;
                    return;
                }
            }

            player.owner.SetHealth(value);
        }

        /// <summary>
        /// Online: synchronizes the health value of the player for all players via properties.
        /// </summary>
        public static void SetHealth(this PhotonPlayer player, int value)
        {
            player.SetCustomProperties(new Hashtable() { { health, (byte)value } });
        }

        /// <summary>
        /// Offline: returns the shield value of a bot stored in PlayerBot.
        /// Fallback to online mode for the master or in case offline mode was turned off.
        /// </summary>
        public static int GetShield(this PhotonView player)
        {
            if (PhotonNetwork.offlineMode == true)
            {
                PlayerBot bot = player.GetComponent<PlayerBot>();
                if (bot != null)
                {
                    return bot.shield;
                }
            }

            return player.owner.GetShield();
        }

        /// <summary>
        /// Online: returns the networked shield value of the player out of properties.
        /// </summary>
        public static int GetShield(this PhotonPlayer player)
        {
            return System.Convert.ToInt32(player.CustomProperties[shield]);
        }

        /// <summary>
        /// Offline: synchronizes the shield value of a PlayerBot locally.
        /// Fallback to online mode for the master or in case offline mode was turned off.
        /// </summary>
        public static void SetShield(this PhotonView player, int value)
        {
            if (PhotonNetwork.offlineMode == true)
            {
                PlayerBot bot = player.GetComponent<PlayerBot>();
                if (bot != null)
                {
                    bot.shield = value;
                    return;
                }
            }

            player.owner.SetShield(value);
        }

        /// <summary>
        /// Online: synchronizes the shield value of the player for all players via properties.
        /// </summary>
        public static void SetShield(this PhotonPlayer player, int value)
        {
            player.SetCustomProperties(new Hashtable() { { shield, (byte)value } });
        }

        /// <summary>
        /// Decreases the networked shield value of the player or bot by the amount passed in.
        /// </summary>
        public static int DecreaseShield(this PhotonView player, int value)
        {
            int newShield = player.GetShield();
            newShield -= value;

            player.SetShield(newShield);
            return newShield;
        }

        /// <summary>
        /// Offline: returns the ammo value of a bot stored in PlayerBot.
        /// Fallback to online mode for the master or in case offline mode was turned off.
        /// </summary>
        public static int GetAmmo(this PhotonView player)
        {
            if (PhotonNetwork.offlineMode == true)
            {
                PlayerBot bot = player.GetComponent<PlayerBot>();
                if (bot != null)
                {
                    return bot.ammo;
                }
            }

            return player.owner.GetAmmo();
        }

        /// <summary>
        /// Online: returns the networked ammo value of the player out of properties.
        /// </summary>
        public static int GetAmmo(this PhotonPlayer player)
        {
            return System.Convert.ToInt32(player.CustomProperties[ammo]);
        }

        /// <summary>
        /// Offline: synchronizes the ammo count of a PlayerBot locally.
        /// Provides an optional index parameter for setting a new bullet and ammo together.
        /// Fallback to online mode for the master or in case offline mode was turned off.
        /// </summary>
        public static void SetAmmo(this PhotonView player, int value, int index = -1)
        {
            if (PhotonNetwork.offlineMode == true)
            {
                PlayerBot bot = player.GetComponent<PlayerBot>();
                if (bot != null)
                {
                    bot.ammo = value;
                    if (index >= 0)
                        bot.currentBullet = index;
                    return;
                }
            }

            player.owner.SetAmmo(value, index);
        }

        /// <summary>
        /// Online: synchronizes the ammo count of the player for all players via properties.
        /// Provides an optional index parameter for setting a new bullet and ammo together.
        /// </summary>
        public static void SetAmmo(this PhotonPlayer player, int value, int index = -1)
        {
            Hashtable hash = new Hashtable();
            hash.Add(ammo, (byte)value);
            if (index >= 0)
                hash.Add(bullet, (byte)index);

            player.SetCustomProperties(hash);
        }

        /// <summary>
        /// Decreases the networked ammo value of the player or bot by the amount passed in.
        /// If the player runs out of ammo, the bullet index is set to the default automatically.
        /// </summary>
        public static int DecreaseAmmo(this PhotonView player, int value)
        {
            int newAmmo = player.GetAmmo();
            newAmmo -= value;

            if (newAmmo <= 0)
                player.SetAmmo(newAmmo, 0);
            else
                player.SetAmmo(newAmmo);

            return newAmmo;
        }

        /// <summary>
        /// Offline: returns the bullet index of a bot stored in PlayerBot.
        /// Fallback to online mode for the master or in case offline mode was turned off.
        /// </summary>
        public static int GetBullet(this PhotonView player)
        {
            if (PhotonNetwork.offlineMode == true)
            {
                PlayerBot bot = player.GetComponent<PlayerBot>();
                if (bot != null)
                {
                    return bot.currentBullet;
                }
            }

            return player.owner.GetBullet();
        }

        /// <summary>
        /// Online: returns the networked bullet index of the player out of properties.
        /// </summary>
        public static int GetBullet(this PhotonPlayer player)
        {
            return System.Convert.ToInt32(player.CustomProperties[bullet]);
        }

        /// <summary>
        /// Offline: synchronizes the currently selected bullet of a PlayerBot locally.
        /// Fallback to online mode for the master or in case offline mode was turned off.
        /// </summary>
        public static void SetBullet(this PhotonView player, int value)
        {
            if (PhotonNetwork.offlineMode == true)
            {
                PlayerBot bot = player.GetComponent<PlayerBot>();
                if (bot != null)
                {
                    bot.currentBullet = value;
                    return;
                }
            }

            player.owner.SetBullet(value);
        }

        /// <summary>
        /// Online: Synchronizes the currently selected bullet of the player for all players via properties.
        /// </summary>
        public static void SetBullet(this PhotonPlayer player, int value)
        {
            player.SetCustomProperties(new Hashtable() { { bullet, (byte)value } });
        }


        /// <summary>
        /// Offline: clears all properties of a PlayerBot locally.
        /// Fallback to online mode for the master or in case offline mode was turned off.
        /// </summary>
        public static void Clear(this PhotonView player)
        {
            if (PhotonNetwork.offlineMode == true)
            {
                PlayerBot bot = player.GetComponent<PlayerBot>();
                if (bot != null)
                {
                    bot.currentBullet = 0;
                    bot.health = 0;
                    bot.shield = 0;
                    return;
                }
            }

            player.owner.Clear();
        }


        /// <summary>
        /// Online: Clears all networked variables of the player via properties in one instruction.
        /// </summary>
        public static void Clear(this PhotonPlayer player)
        {
            player.SetCustomProperties(new Hashtable() { { PlayerExtensions.bullet, (byte)0 },
                                                         { PlayerExtensions.health, (byte)0 },
                                                         { PlayerExtensions.shield, (byte)0 } });
        }
    }
}
