using UnityEngine;

/*
 * 特效处理器
 *
 * 走项目现有的对象池 + YooAsset 路径加载（ObjectPoolManager.GetAsync），
 * 复用池化与自动回收，不引入新的资源框架。
 *
 * 挂点规则：
 *   - AttachPoint 非空：在施法者子节点中找同名骨骼，特效跟随骨骼
 *   - 为空：生成在施法者位置，在池中 3 秒后自动回收（TODO 改为按粒子时长自动回收）
 */

namespace Game.SkillTimeline
{
    public sealed class SpawnVfxHandler : ISkillTimelineEventHandler
    {
        /// <summary>TODO 粒子时长未知时的兜底回收时间</summary>
        private const float FallbackAutoRecycle = 3f;

        public SkillEventType Type => SkillEventType.SpawnVfx;

        public void Execute(SkillTimelineEvent evt, SkillExecutionContext context)
        {
            if (evt == null || context == null) return;
            if (string.IsNullOrEmpty(evt.AssetPath)) return;

            if (context.IsPreview)
            {
                Debug.Log($"[SkillTimeline][预览] 生成特效 {evt.AssetPath} @ {evt.AttachPoint}（不实际生成）");
                return;
            }

            var attach = ResolveAttach(context.CasterRoot, evt.AttachPoint);
            if (attach == null)
            {
                Debug.LogWarning($"[SkillTimeline] 施法者为空，特效 {evt.AssetPath} 被跳过。");
                return;
            }

            Vector3 position = attach.position;
            Quaternion rotation = attach.rotation;

            ObjectPoolManager.EnsureInstance().GetAsync(evt.AssetPath, attach, go =>
            {
                if (go == null) return;

                go.transform.SetPositionAndRotation(position, rotation);
            }, FallbackAutoRecycle);
        }

        public void Finish(SkillTimelineEvent evt, SkillExecutionContext context)
        {
            //特效由池自动回收，无需收尾
        }

        private static Transform ResolveAttach(Transform root, string boneName)
        {
            if (root == null) return null;
            if (string.IsNullOrEmpty(boneName)) return root;

            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == boneName) return all[i];
            }

            Debug.LogWarning($"[SkillTimeline] 找不到挂点骨骼 {boneName}，特效改为生成在施法者位置。");
            return root;
        }
    }
}
