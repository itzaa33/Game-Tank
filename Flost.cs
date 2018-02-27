using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TanksMP
{
    public class Flost : ModelWepon 
    {

        void OnTriggerEnter(Collider col)
        {
            GameObject obj = col.gameObject;

            PlayerBot bot = obj.GetComponent<PlayerBot>();

            if (bot)
            {
                bot.TakedamageFlost(TakedamageBullet());
            }
        }
    }
}
