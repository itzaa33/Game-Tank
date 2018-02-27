/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using System.Collections;
using System.Collections.Generic; 

namespace TanksMP
{
    /// <summary>
    /// Projectile script for player shots with collision/hit logic.
    /// </summary>
    public class Thunder : ModelWepon
    {

        void OnTriggerEnter(Collider col)
        {
            GameObject obj = col.gameObject;

            PlayerBot bot = obj.GetComponent<PlayerBot>();

            if (bot)
            {
                bot.TakeDamageBot(TakedamageBullet());
                bot.CheckStatestun = true;
            }
        }
    }
}
