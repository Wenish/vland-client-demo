using System.Collections.Generic;
using MyGame.Events;
using UnityEngine;

namespace ShadowInfection.UI.PlayerHud
{
    internal sealed class PlayerHudInfoFeedDriver
    {
        private readonly PlayerHudSettings settings;
        private readonly List<Entry> entries = new();
        private readonly List<PlayerHudInfoLineVm> renderBuffer = new();

        private PlayerHudView view;

        public PlayerHudInfoFeedDriver(PlayerHudSettings settings)
        {
            this.settings = settings;
        }

        public void Bind(PlayerHudView nextView)
        {
            Clear();
            view = nextView;
        }

        public void Unbind()
        {
            Clear();
            view = null;
        }

        public void Enqueue(PlayerHudInfoMessageEvent message)
        {
            if (view == null || message == null || string.IsNullOrWhiteSpace(message.Text))
                return;

            var duration = message.DurationSeconds > 0f
                ? message.DurationSeconds
                : settings.InfoMessageDurationSeconds;
            var expireAt = Time.unscaledTime + duration;

            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].Key != message.Key)
                    continue;

                entries[i].Text = message.Text;
                entries[i].Kind = message.Kind;
                entries[i].ExpireAt = expireAt;
                var refreshed = entries[i];
                entries.RemoveAt(i);
                entries.Add(refreshed);
                Render();
                return;
            }

            entries.Add(new Entry
            {
                Key = message.Key,
                Text = message.Text,
                Kind = message.Kind,
                ExpireAt = expireAt
            });

            var maxVisible = settings.InfoMessageMaxVisible;
            while (entries.Count > maxVisible)
                entries.RemoveAt(0);

            Render();
        }

        public void Tick()
        {
            if (view == null || entries.Count == 0)
                return;

            var now = Time.unscaledTime;
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                if (now >= entries[i].ExpireAt)
                    entries.RemoveAt(i);
            }

            Render();
        }

        private void Clear()
        {
            entries.Clear();
            if (view != null)
                view.ClearInfoLines();
        }

        private void Render()
        {
            if (view == null)
                return;

            var now = Time.unscaledTime;
            var fade = settings.InfoMessageFadeSeconds;
            renderBuffer.Clear();

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                renderBuffer.Add(new PlayerHudInfoLineVm(
                    entry.Text,
                    OpacityFor(entry.ExpireAt, now, fade),
                    entry.Kind == PlayerHudInfoKind.Error));
            }

            view.SetInfoLines(renderBuffer);
        }

        private static float OpacityFor(float expireAt, float now, float fadeSeconds)
        {
            if (fadeSeconds <= 0f)
                return 1f;

            var remaining = expireAt - now;
            if (remaining >= fadeSeconds)
                return 1f;

            return Mathf.Clamp01(remaining / fadeSeconds);
        }

        private sealed class Entry
        {
            public string Key;
            public string Text;
            public PlayerHudInfoKind Kind;
            public float ExpireAt;
        }
    }
}
