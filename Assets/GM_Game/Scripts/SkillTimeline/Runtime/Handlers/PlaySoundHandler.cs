using UnityEngine;
using YooAsset;

/*
 * 音效处理器
 *
 * 项目当前没有任何音效播放基础设施（角色上没有 AudioSource），
 * 这里用最小实现：在施法者身上创建一个常驻 AudioSource，通过 YooAsset 加载 AudioClip，
 * 加载失败只打警告，不影响战斗流程。
 *
 * 资源地址与项目现有约定一致：写 Assets/... 完整路径（YooAsset 默认包按文件名寻址，
 * AssetBundleCollectorSetting 里 JsonData/Textures 等都是 AddressByFileName）。
 */

namespace Game.SkillTimeline
{
    public sealed class PlaySoundHandler : ISkillTimelineEventHandler
    {
        public SkillEventType Type => SkillEventType.PlaySound;

        private static AssetOperationCache _cache = new AssetOperationCache();

        public void Execute(SkillTimelineEvent evt, SkillExecutionContext context)
        {
            if (evt == null || context == null) return;
            if (string.IsNullOrEmpty(evt.AssetPath)) return;

            var source = GetOrCreateSource(context.CasterRoot);
            if (source == null) return;

            if (context.IsPreview)
            {
                Debug.Log($"[SkillTimeline][预览] 播放音效 {evt.AssetPath}（不实际加载）");
                return;
            }

            _cache.LoadAudioClip(evt.AssetPath, clip =>
            {
                if (clip == null || source == null) return;
                source.PlayOneShot(clip, Mathf.Clamp01(evt.Volume));
            });
        }

        public void Finish(SkillTimelineEvent evt, SkillExecutionContext context)
        {
            //音效无需收尾
        }

        private static AudioSource GetOrCreateSource(Transform root)
        {
            if (root == null) return null;
            if (root.TryGetComponent(out AudioSource existing)) return existing;
            return root.gameObject.AddComponent<AudioSource>();
        }

        /// <summary>音频资源句柄缓存，避免同一音效重复加载</summary>
        private sealed class AssetOperationCache
        {
            private readonly System.Collections.Generic.Dictionary<string, AudioClip> _clips
                = new System.Collections.Generic.Dictionary<string, AudioClip>();

            public void LoadAudioClip(string path, System.Action<AudioClip> onLoaded)
            {
                if (_clips.TryGetValue(path, out var cached))
                {
                    onLoaded?.Invoke(cached);
                    return;
                }

                /*var package = Global.Instance != null ? Global.Instance.YooPackage : null;
                if (package == null)
                {
                    Debug.LogWarning("[SkillTimeline] YooAsset 未初始化，音效无法加载。");
                    onLoaded?.Invoke(null);
                    return;
                }*/

                /*package.LoadAssetAsync<AudioClip>(path).Completed += handle =>
                {
                    if (handle.Status != YooAsset.EOperationStatus.Succeed)
                    {
                        Debug.LogWarning($"[SkillTimeline] 音效加载失败：{path}");
                        onLoaded?.Invoke(null);
                        return;
                    }

                    var clip = handle.AssetObject as AudioClip;
                    _clips[path] = clip;
                    onLoaded?.Invoke(clip);
                };*/
                
                //修改的音频加载
                var handle = YooAssets.LoadAssetAsync<AudioClip>(path);
                handle.Completed += (h) =>
                {
                    if (handle.Status != YooAsset.EOperationStatus.Succeed)
                    {
                        Debug.LogWarning($"[SkillTimeline] 音效加载失败：{path}");
                        onLoaded?.Invoke(null);
                        return;
                    }

                    var clip = handle.AssetObject as AudioClip;
                    _clips[path] = clip;
                    onLoaded?.Invoke(clip);
                };
            }
        }
    }
}
