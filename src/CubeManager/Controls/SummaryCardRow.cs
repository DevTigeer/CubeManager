using System.Drawing;
using CubeManager.Helpers;

namespace CubeManager.Controls;

/// <summary>
/// SummaryCard 4개를 가로 배치하는 컨테이너.
/// 부모 리사이즈 시 카드 폭 자동 조정 (균등 분배).
/// </summary>
public class SummaryCardRow : Panel
{
    private readonly List<SummaryCard> _cards = [];

    public SummaryCardRow()
    {
        Dock = DockStyle.Top;
        Height = 116; // 100 card + 8*2 margin
        BackColor = ColorPalette.Background;
        Padding = new Padding(8, 8, 8, 0);
    }

    /// <summary>카드 추가 (최대 4개 권장)</summary>
    public SummaryCard AddCard(string title, string value, Color accentMain, Color accentLight)
    {
        var card = new SummaryCard
        {
            Title = title,
            Value = value
        };
        card.SetAccent(accentMain, accentLight);
        _cards.Add(card);
        Controls.Add(card);
        LayoutCards();
        return card;
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        LayoutCards();
    }

    private void LayoutCards()
    {
        if (_cards.Count == 0) return;

        var gap = 12;
        var availableWidth = ClientSize.Width - Padding.Horizontal;
        var cardWidth = (availableWidth - gap * (_cards.Count - 1)) / _cards.Count;
        var y = Padding.Top;

        for (var i = 0; i < _cards.Count; i++)
        {
            _cards[i].SetBounds(
                Padding.Left + i * (cardWidth + gap),
                y,
                cardWidth,
                100);
            _cards[i].Margin = Padding.Empty;
        }
    }

    /// <summary>인덱스로 카드 접근 (값 업데이트용)</summary>
    public SummaryCard this[int index] => _cards[index];
}
