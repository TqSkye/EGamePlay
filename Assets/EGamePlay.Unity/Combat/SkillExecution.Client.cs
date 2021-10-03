﻿using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using EGamePlay.Combat;
using ET;
using Log = EGamePlay.Log;
using UnityEngine.Timeline;
using UnityEngine.Playables;

namespace EGamePlay.Combat
{
    /// <summary>
    /// 技能执行体
    /// </summary>
    public partial class SkillExecution : AbilityExecution
    {
        public List<AnimationData> AnimationDatas { get; set; } = new List<AnimationData>();
        public List<ColliderSpawnData> ColliderSpawnDatas { get; set; } = new List<ColliderSpawnData>();
        
        
        public override void Awake(object initData)
        {
            base.Awake(initData);

            this.ExecutionEffectComponent = AddComponent<ExecutionEffectComponent>(SkillAbility.AbilityEffectComponent.AbilityEffects);

            OriginTime = ET.TimeHelper.Now();
        }

        public override void Update()
        {
            if (SkillAbility.Spelling == false)
            {
                return;
            }

            var nowSeconds = (double)(ET.TimeHelper.Now() - OriginTime) / 1000;

            if (nowSeconds >= SkillAbility.SkillExecuteTime)
            {
                EndExecute();
            }
        }

        public override void BeginExecute()
        {
            GetParent<CombatEntity>().CurrentSkillExecution = this;
            SkillAbility.Spelling = true;

            if (SkillExecutionAsset == null)
                return;
            var timelineAsset = SkillExecutionAsset.GetComponent<PlayableDirector>().playableAsset as TimelineAsset;
            if (timelineAsset == null)
                return;
            var skillExecutionObj = GameObject.Instantiate(SkillExecutionAsset, OwnerEntity.Position, Quaternion.Euler(0, OwnerEntity.Direction, 0));
            GameObject.Destroy(skillExecutionObj, (float)timelineAsset.duration);
            base.BeginExecute();
        }

        public override void EndExecute()
        {
            GetParent<CombatEntity>().CurrentSkillExecution = null;
            SkillAbility.Spelling = false;
            SkillTargets.Clear();
            base.EndExecute();
        }

        public void SpawnCollisionItem(ColliderSpawnEmitter colliderSpawnEmitter)
        {
            if (colliderSpawnEmitter.ColliderType == ColliderType.TargetFly) TargetFlyProccess(colliderSpawnEmitter);
            if (colliderSpawnEmitter.ColliderType == ColliderType.ForwardFly) ForwardFlyProccess(colliderSpawnEmitter);
            if (colliderSpawnEmitter.ColliderType == ColliderType.FixedPosition) FixedPositionProccess(colliderSpawnEmitter);
            if (colliderSpawnEmitter.ColliderType == ColliderType.FixedDirection) FixedDirectionProccess(colliderSpawnEmitter);
        }

        private void TargetFlyProccess(ColliderSpawnEmitter colliderSpawnEmitter)
        {
            var abilityItem = Entity.Create<AbilityItem>(colliderSpawnEmitter.ColliderName);
            abilityItem.AbilityEntity = SkillAbility;
            abilityItem.TargetEntity = InputTarget;
            abilityItem.Position = OwnerEntity.Position;
            abilityItem.FillAbilityEffects(SkillAbility);
            CreateAbilityItemObj(abilityItem);
            abilityItem.AddComponent<MoveWithDotweenComponent>().DoMoveTo(InputTarget);
        }

        private void ForwardFlyProccess(ColliderSpawnEmitter colliderSpawnEmitter)
        {
            var abilityItem = Entity.Create<AbilityItem>(colliderSpawnEmitter.ColliderName);
            abilityItem.AbilityEntity = SkillAbility;
            abilityItem.Position = OwnerEntity.Position;
            abilityItem.FillAbilityEffects(SkillAbility);
            CreateAbilityItemObj(abilityItem);
            var x = Mathf.Sin(Mathf.Deg2Rad * InputDirection);
            var z = Mathf.Cos(Mathf.Deg2Rad * InputDirection);
            var destination = abilityItem.Position + new Vector3(x, 0, z) * 30;
            abilityItem.AddComponent<MoveWithDotweenComponent>().DoMoveTo(destination, 1f).OnMoveFinish(()=> { Entity.Destroy(abilityItem); });
        }

        private void FixedPositionProccess(ColliderSpawnEmitter colliderSpawnEmitter)
        {
            var abilityItem = Entity.Create<AbilityItem>(colliderSpawnEmitter.ColliderName);
            abilityItem.AbilityEntity = SkillAbility;
            abilityItem.Position = InputPoint;
            abilityItem.FillAbilityEffects(SkillAbility);
            CreateAbilityItemObj(abilityItem);
            abilityItem.AddComponent<LifeTimeComponent>(colliderSpawnEmitter.ExistTime);
        }

        private void FixedDirectionProccess(ColliderSpawnEmitter colliderSpawnEmitter)
        {
            var abilityItem = Entity.Create<AbilityItem>(colliderSpawnEmitter.ColliderName);
            abilityItem.AbilityEntity = SkillAbility;
            abilityItem.Position = OwnerEntity.Position;
            abilityItem.Direction = OwnerEntity.Direction;
            abilityItem.FillAbilityEffects(SkillAbility);
            CreateAbilityItemObj(abilityItem);
            abilityItem.AddComponent<LifeTimeComponent>(colliderSpawnEmitter.ExistTime);
        }

        public void CreateAbilityItemObj(AbilityItem abilityItem)
        {
            var abilityItemObj = GameObject.Instantiate(Resources.Load<GameObject>($"AbilityItems/{abilityItem.Name}"), abilityItem.Position, Quaternion.Euler(0, abilityItem.Direction, 0));
            abilityItemObj.GetComponent<AbilityItemProxyObj>().AbilityItem = abilityItem;
            abilityItemObj.GetComponent<Collider>().enabled = false;
            abilityItemObj.GetComponent<OnTriggerEnterCallback>().OnTriggerEnterCallbackAction = (other) => {
                var combatEntity = CombatContext.Instance.Object2Entities[other.gameObject];
                abilityItem.OnCollision(combatEntity);
            };
            abilityItemObj.GetComponent<Collider>().enabled = true;
        }
    }
}