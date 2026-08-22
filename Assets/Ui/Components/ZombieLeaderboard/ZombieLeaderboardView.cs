using System.Collections.Generic;
using MyGame.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.ZombieLeaderboard
{
    internal sealed class ZombieLeaderboardView
    {
        private readonly VisualElement panel;
        private readonly VisualElement rows;
        private readonly Label emptyLabel;

        public ZombieLeaderboardView(VisualElement root)
        {
            panel = root.Q<VisualElement>("leaderboardPanel");
            rows = root.Q<VisualElement>("leaderboardRows");
            emptyLabel = root.Q<Label>("leaderboardEmptyLabel");

            if (panel != null)
                UiGameplayInputGuard.Apply(panel);

            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (panel == null)
                return;

            panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetRows(IReadOnlyList<ZombieLeaderboardRow> entries)
        {
            if (rows == null || emptyLabel == null)
                return;

            rows.Clear();
            if (entries == null || entries.Count == 0)
            {
                emptyLabel.style.display = DisplayStyle.Flex;
                return;
            }

            emptyLabel.style.display = DisplayStyle.None;
            for (var i = 0; i < entries.Count; i++)
                rows.Add(CreateRow(entries[i], i + 1));
        }

        private static VisualElement CreateRow(ZombieLeaderboardRow entry, int rank)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    width = Length.Percent(100),
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 4,
                    paddingBottom = 4,
                    marginBottom = 2,
                    backgroundColor = rank % 2 == 0
                        ? new Color(0.11f, 0.11f, 0.11f, 0.6f)
                        : new Color(0.06f, 0.06f, 0.06f, 0.6f)
                }
            };

            var playerName = entry.IsConnected ? entry.PlayerName : $"{entry.PlayerName} (left)";
            row.Add(CreateFixedCell(rank.ToString(), 34));
            row.Add(CreateFlexibleCell(playerName));
            row.Add(CreateFixedCell(entry.Points.ToString(), 60));
            row.Add(CreateFixedCell(entry.Kills.ToString(), 46));
            row.Add(CreateFixedCell(entry.Deaths.ToString(), 56));
            row.Add(CreateFixedCell(entry.GoldGathered.ToString(), 60));
            return row;
        }

        private static Label CreateFixedCell(string text, float width)
        {
            return new Label(text)
            {
                style =
                {
                    width = width,
                    minWidth = width,
                    color = Color.white,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    fontSize = 12
                }
            };
        }

        private static Label CreateFlexibleCell(string text)
        {
            return new Label(text)
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    minWidth = 90,
                    color = Color.white,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    fontSize = 12,
                    whiteSpace = WhiteSpace.NoWrap,
                    overflow = Overflow.Hidden,
                    textOverflow = TextOverflow.Ellipsis
                }
            };
        }
    }
}
