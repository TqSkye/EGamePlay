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
    public partial class SkillExecution
    {
        public GameObject SkillExecutionAsset => SkillAbility.SkillExecutionAsset;


        public override void Awake(object initData)
        {
            base.Awake(initData);

            this.ExecutionEffectComponent = AddComponent<ExecutionEffectComponent>(SkillAbility.AbilityEffects);

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

        /// <summary>
        /// 技能碰撞体生成事件
        /// </summary>
        /// <param name="colliderSpawnEmitter"></param>
        public void SpawnCollisionItem(ExecutionEventEmitter colliderSpawnEmitter)
        {
            if (colliderSpawnEmitter.ColliderType == ColliderType.TargetFly) TargetFlyProcess(colliderSpawnEmitter);
            if (colliderSpawnEmitter.ColliderType == ColliderType.ForwardFly) ForwardFlyProcess(colliderSpawnEmitter);
            if (colliderSpawnEmitter.ColliderType == ColliderType.FixedPosition) FixedPositionProcess(colliderSpawnEmitter);
            if (colliderSpawnEmitter.ColliderType == ColliderType.FixedDirection) FixedDirectionProcess(colliderSpawnEmitter);
        }

        /// <summary>
        /// 目标飞行碰撞体
        /// </summary>
        /// <param name="colliderSpawnEmitter"></param>
        private void TargetFlyProcess(ExecutionEventEmitter colliderSpawnEmitter)
        {
            var abilityItem = Entity.Create<AbilityItem>(this);
            abilityItem.Name = colliderSpawnEmitter.ColliderName;
            abilityItem.TargetEntity = InputTarget;
            abilityItem.Position = OwnerEntity.Position;
            CreateAbilityItemObj(abilityItem);
            abilityItem.AddComponent<MoveWithDotweenComponent>().DoMoveTo(InputTarget);
        }

        /// <summary>
        /// 前向飞行碰撞体
        /// </summary>
        /// <param name="colliderSpawnEmitter"></param>
        private void ForwardFlyProcess(ExecutionEventEmitter colliderSpawnEmitter)
        {
            var abilityItem = Entity.Create<AbilityItem>(this);
            abilityItem.Name = colliderSpawnEmitter.ColliderName;
            abilityItem.Position = OwnerEntity.Position;
            CreateAbilityItemObj(abilityItem);
            var x = Mathf.Sin(Mathf.Deg2Rad * InputDirection);
            var z = Mathf.Cos(Mathf.Deg2Rad * InputDirection);
            var destination = abilityItem.Position + new Vector3(x, 0, z) * 30;
            abilityItem.AddComponent<MoveWithDotweenComponent>().DoMoveTo(destination, 1f).OnMoveFinish(()=> { Entity.Destroy(abilityItem); });
        }

        /// <summary>
        /// 固定位置碰撞体
        /// </summary>
        /// <param name="colliderSpawnEmitter"></param>
        private void FixedPositionProcess(ExecutionEventEmitter colliderSpawnEmitter)
        {
            var abilityItem = Entity.Create<AbilityItem>(this);
            abilityItem.Name = colliderSpawnEmitter.ColliderName;
            abilityItem.Position = InputPoint;
            CreateAbilityItemObj(abilityItem);
            abilityItem.AddComponent<LifeTimeComponent>(colliderSpawnEmitter.ExistTime);
        }

        /// <summary>
        /// 固定方向碰撞体
        /// </summary>
        /// <param name="colliderSpawnEmitter"></param>
        private void FixedDirectionProcess(ExecutionEventEmitter colliderSpawnEmitter)
        {
            var abilityItem = Entity.Create<AbilityItem>(this);
            abilityItem.Name = colliderSpawnEmitter.ColliderName;
            abilityItem.Position = OwnerEntity.Position;
            abilityItem.Direction = OwnerEntity.Direction;
            CreateAbilityItemObj(abilityItem);
            abilityItem.AddComponent<LifeTimeComponent>(colliderSpawnEmitter.ExistTime);
        }

        /// <summary>
        /// 创建技能碰撞体
        /// </summary>
        /// <param name="abilityItem"></param>
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
