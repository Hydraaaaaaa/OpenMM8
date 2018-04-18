﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace Assets.OpenMM8.Scripts.Gameplay
{
    public class Character
    {
        public CharacterModel CharacterModel;
        public CharacterUI CharacterUI;
        public PlayerParty PlayerParty;
        public CharacterSounds CharacterSounds;
        public CharacterSprites CharacterSprites;        

        public float TimeUntilRecovery = 0.0f;

        private float TimeInOtherAvatar = 0.0f;
        private float MinIdleAvatar = 2.0f;
        private float MaxIdleAvatar = 7.0f;
        private float TimeUntilIdleAvatar = 7.0f;

        public static Character Create(CharacterModel characterModel, CharacterUI characterUI, CharacterType type)
        {
            Character character = new Character();
            character.CharacterModel = characterModel;
            character.CharacterUI = characterUI;
            character.CharacterSounds = GameMgr.Instance.GetCharacterSounds(type);
            character.CharacterSprites = GameMgr.Instance.GetCharacterSprites(type);

            character.CharacterUI.PlayerCharacter.sprite = character.CharacterSprites.ConditionToSpriteMap[Condition.Good];

            return character;
        }

        public void OnFixedUpdate(float secDiff)
        {
            if (CharacterUI.PlayerCharacter.sprite != CharacterSprites.ConditionToSpriteMap[CharacterModel.Condition])
            {
                TimeInOtherAvatar += secDiff;
                if (TimeInOtherAvatar > 1.0f)
                {
                    CharacterUI.PlayerCharacter.sprite = CharacterSprites.ConditionToSpriteMap[CharacterModel.Condition];
                    TimeUntilIdleAvatar = UnityEngine.Random.Range(MinIdleAvatar, MaxIdleAvatar);
                    TimeInOtherAvatar = 0.0f;
                }
            }
            else if (CharacterModel.Condition == Condition.Good)
            {
                TimeUntilIdleAvatar -= secDiff;
                if (TimeUntilIdleAvatar < 0.0f)
                {
                    CharacterUI.PlayerCharacter.sprite = CharacterSprites.Idle[UnityEngine.Random.Range(0, CharacterSprites.Idle.Count)];
                }
            }
        }

        public void OnUpdate(float secDiff)
        {
            TimeUntilRecovery -= secDiff;
            if (IsRecovered() && CharacterUI.AgroStatus.enabled == false)
            {
                CharacterUI.AgroStatus.enabled = true;
            }
            else if (!IsRecovered() && CharacterUI.AgroStatus.enabled == true)
            {
                CharacterUI.AgroStatus.enabled = false;
            }
            /*if (!IsRecovered() && CharacterUI.SelectionRing.enabled == true)
            {
                CharacterUI.SelectionRing.enabled = false;
            }
            else if (IsRecovered() && CharacterUI.SelectionRing.enabled == false)
            {
                CharacterUI.SelectionRing.enabled = true;
            }*/
        }

        public bool IsRecovered()
        {
            return TimeUntilRecovery <= 0.0f;
        }

        public bool Attack(Damageable victim)
        {
            if (TimeUntilRecovery > 0.0f)
            {
                return false;
            }

            TimeUntilIdleAvatar = UnityEngine.Random.Range(MinIdleAvatar, MaxIdleAvatar);

            TimeUntilRecovery = 1.0f;
            PlayerParty.PlayerAudioSource.PlayOneShot(PlayerParty.SwordAttacks[UnityEngine.Random.Range(0, PlayerParty.SwordAttacks.Count)]);

            if (victim)
            {
                AttackInfo attackInfo = new AttackInfo();
                attackInfo.MinDamage = 38;
                attackInfo.MaxDamage = 64;
                attackInfo.AttackMod = 10000;
                attackInfo.DamageType = SpellElement.Physical;

                AttackResult result = victim.ReceiveAttack(attackInfo, PlayerParty.gameObject);
                string hitText = "";
                switch (result.Type)
                {
                    case AttackResultType.Hit:
                        hitText = CharacterModel.Name + " hits " + result.HitObjectName + " for " + result.DamageDealt + " damage";
                        break;

                    case AttackResultType.Kill:
                        hitText = CharacterModel.Name + " inflicts " + result.DamageDealt + " points killing " + result.HitObjectName;
                        break;

                    case AttackResultType.Miss:
                        hitText = CharacterModel.Name + " missed attack on " + result.HitObjectName;
                        break;
                }

                PlayerParty.SetPartyInfoText(hitText);
            }

            return true;
        }

        // Events
        void UnequipItem(ItemData item)
        {

        }

        void EquipItem(ItemData item)
        {

        }

        bool CanEquipItem(ItemData item)
        {
            return true;
        }

        public void OnItemPickedUp(ItemData item, bool fromPartyMember)
        {

        }

        public void ModifyCurrentHitPoints(int numHitPoints)
        {
            CharacterModel.CurrHitPoints += numHitPoints;
            int maxHP = CharacterModel.DefaultStats.MaxHitPoints + CharacterModel.BonusStats.MaxHitPoints;
            float healthPercent = ((float)CharacterModel.CurrHitPoints / (float)maxHP) * 100.0f;
            CharacterUI.SetHealth(healthPercent);
        }

        public void ModifyCurrentSpellPoints(int numSpellPoints)
        {

        }

        public void AddLevel()
        {

        }

        public void IncreaseSkillLevel(SkillType skillType, int amount = 1)
        {

        }

        public void ModifyAttribute(Attribute attribute, int amount)
        {

        }

        public void ModifyResistance(SpellElement element, int amount)
        {

        }

        // Avatar expressions
        public void Smile()
        {
            CharacterUI.PlayerCharacter.sprite =
                CharacterSprites.Smile[UnityEngine.Random.Range(0, CharacterSprites.Smile.Count)];
        }

        public void TakeDamage()
        {
            CharacterUI.PlayerCharacter.sprite =
                CharacterSprites.TakeDamage[UnityEngine.Random.Range(0, CharacterSprites.TakeDamage.Count)];
        }
    }
}
