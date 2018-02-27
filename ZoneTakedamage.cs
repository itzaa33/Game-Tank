using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TanksMP
{
    public class ZoneTakedamage : ModelWepon 
    {
        //damage take from player

        void OnTriggerEnter(Collider col)
        {

                GameObject obj = col.gameObject;

                PlayerBot bot = obj.GetComponent<PlayerBot>();

                if (bot)
                { 
                        bot.TakeDamageBot(damage);
                }
        }
    }
}
