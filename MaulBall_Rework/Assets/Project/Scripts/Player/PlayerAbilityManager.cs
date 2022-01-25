using Project.Scripts.Ability;
using Project.Scripts.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Player
{
    public abstract class PlayerAbilityManager : MonoBehaviour
    {
        [Header("Necessary - ability stats")]
        [SerializeField] private AbilityStats stats;
        [Space]
        private float abilityDuration;
        private float targetAppliedDuration;
        private float abilityCooldown;
        private float abilityRepeatTime;
        private float abilitySpeed;
        [Space]
        private Vector3 abilitySize;
        private Vector3 abilitySpawnOffset;
        private GameObject prefabObj;

        //management variables
        private float currentCooldown = 0;
        private bool m_usedAbility;

        public void InitializeAbility()
        {
            if (stats == null)
                return;

            abilityDuration = stats.duration;
            targetAppliedDuration = stats.effect_Duration;
            abilityCooldown = stats.cooldown;
            abilityRepeatTime = stats.repeat_Time;
            abilitySpeed = stats.ability_Speed;
            abilitySize = stats.ability_Size;
            abilitySpawnOffset = stats.spawning_Offset;
            prefabObj = stats.prefabObj;

            currentCooldown = abilityCooldown;
        }

        private void CustomUpdate()
        {
            if (currentCooldown > 0 && m_usedAbility)
            {
                currentCooldown -= Time.deltaTime;
            }

            if (currentCooldown <= 0 && m_usedAbility)
            {
                ResetAbility();
            }
        }

        private void ResetAbility()
        {
            m_usedAbility = false;
            currentCooldown = abilityCooldown;
        }

        public void DoAbility()
        {
            m_usedAbility = true;
        }

        public abstract void AbilityAction();


    }
}
