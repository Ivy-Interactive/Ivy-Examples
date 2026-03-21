using Ivy;

namespace Test.EmojiVotingBooth.Apps;

[App(title: "Emoji Voting Booth", icon: Icons.Trophy)]
public class EmojiVotingBoothApp : ViewBase
{
    private static readonly string[] Emojis = ["🔥", "🦄", "🍕", "👻", "🎸", "🌮", "🐙", "💎"];

    public override object? Build()
    {
        var votes = UseState(Emojis.ToDictionary(e => e, _ => 0));

        var currentVotes = votes.Value;
        var maxVotes = currentVotes.Values.Max();
        var leader = maxVotes > 0
            ? currentVotes.First(kv => kv.Value == maxVotes).Key
            : null;

        var emojiRow = Layout.Horizontal().Gap(3);
        foreach (var emoji in Emojis)
        {
            var count = currentVotes[emoji];
            var isLeader = emoji == leader;
            var capturedEmoji = emoji;

            var card = Layout.Vertical().Align(Align.Center).Gap(2);
            card |= Text.H1(emoji);
            card |= Text.P($"{count} votes").Bold();
            card |= new Button("Vote", () =>
            {
                var updated = new Dictionary<string, int>(votes.Value)
                {
                    [capturedEmoji] = votes.Value[capturedEmoji] + 1
                };
                votes.Set(updated);
            }).Small();

            if (isLeader)
            {
                emojiRow |= new Box(card)
                    .BorderColor(Colors.Amber)
                    .BorderThickness(3)
                    .BorderStyle(BorderStyle.Solid)
                    .BorderRadius(BorderRadius.Rounded)
                    .Padding(4);
            }
            else
            {
                emojiRow |= new Box(card)
                    .BorderColor(Colors.Muted)
                    .BorderThickness(1)
                    .BorderStyle(BorderStyle.Solid)
                    .BorderRadius(BorderRadius.Rounded)
                    .Padding(4);
            }
        }

        var ranked = currentVotes
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => Array.IndexOf(Emojis, kv.Key))
            .Select((kv, i) => new { Position = i + 1, Emoji = kv.Key, Votes = kv.Value })
            .ToArray();

        return Layout.Vertical().Gap(6)
            | Text.H2("🏆 Emoji Popularity Contest")
            | Text.P("Click Vote to support your favorite emoji!")
            | emojiRow
            | Text.H3("Leaderboard")
            | ranked.ToTable();
    }
}
