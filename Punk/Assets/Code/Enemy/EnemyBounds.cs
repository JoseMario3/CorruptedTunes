using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Punk
{
    public class EnemyBounds : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.GetComponent<EnemyController>())
            {
                other.gameObject.GetComponent<EnemyController>().directionLeft = !other.gameObject.GetComponent<EnemyController>().directionLeft;
            }

            if (other.gameObject.GetComponent<MeleeEnemyController>())
            {
                other.gameObject.GetComponent<MeleeEnemyController>().directionLeft = !other.gameObject.GetComponent<MeleeEnemyController>().directionLeft;
            }

            if (other.gameObject.GetComponent<IdleEnemyController>())
            {
                other.gameObject.GetComponent<IdleEnemyController>().directionLeft = !other.gameObject.GetComponent<IdleEnemyController>().directionLeft;
;           }

            if (other.gameObject.GetComponent<GreenBossRobotController>())
            {
                other.gameObject.GetComponent<GreenBossRobotController>().directionLeft = !other.gameObject.GetComponent<GreenBossRobotController>().directionLeft;
            }
        }
    }
}
