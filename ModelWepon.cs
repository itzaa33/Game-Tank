using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TanksMP
{
    public class ModelWepon : MonoBehaviour 
    {
        public int stateWepon = 0;

        public float speed = 10;

        public int damage = 0;

        public int Bonusdamage = 0;

        public static int AddDamageElement;

        public AudioClip hitClip;

        public AudioClip explosionClip;

        public GameObject hitFX;

        public GameObject explosionFX;

        public GameObject TextLandmind;


        [HideInInspector]
        public GameObject owner;

        public int TakedamageBullet()
        {
            int totalDamage;

            if (GameManager.multiplyDamage_Status != 0)
                totalDamage = (damage + Bonusdamage + AddDamageElement) * GameManager.multiplyDamage_Status;
            else
                totalDamage = (damage + Bonusdamage + AddDamageElement);
                    
            return totalDamage;
        }

    }
}
