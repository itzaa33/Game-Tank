using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TanksMP
{
    public class ItemStun : Powerup 
    {
        public float TimeStun;

        public override bool Apply(Player p)
        {
            BotSpawner botspawne = FindObjectOfType<BotSpawner>();

            for (int i = 0; i < botspawne.A_LifeBots.Count; i++)
            {
                
                GameObject obj = botspawne.A_LifeBots[i];

                PlayerBot bot = obj.GetComponent<PlayerBot>();

                bot.CheckStatestun = true;
                bot.countStun = TimeStun;
            }

            return true;
        }
    }
}
