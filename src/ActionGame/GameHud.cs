using System.Numerics;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace ActionGame
{
    // 화면 기준 900×620. 기존 렌더 타깃 배율을 그대로 사용한다.
    internal sealed class GameHud : IDisposable
    {
        private readonly ID2D1SolidColorBrush brush;
        private readonly G2Font small = new("HY견고딕", 11, FontWeight.Bold);
        private readonly G2Font body = new("HY견고딕", 14, FontWeight.Normal);

        private readonly G2Font number = new("HY견고딕", 22, FontWeight.Bold);
        private readonly G2Font centered = new("HY견고딕", 16, FontWeight.Bold,
            textAlignment: TextAlignment.Center);
        private readonly G2Font resultTitle = new("HY헤드라인M", 32, FontWeight.Bold,
            textAlignment: TextAlignment.Center);
        private readonly G2Font resultNumber = new("HY견고딕", 40, FontWeight.Bold,
            textAlignment: TextAlignment.Center);
        private static Color4 C(int r, int g, int b, float a = 1) => new(r / 255f, g / 255f, b / 255f, a);
        private static readonly Color4 Cyan = C(73, 217, 235), Gold = C(255, 202, 87);
        private static readonly Color4 Muted = C(174, 166, 148), White = C(239, 232, 215), Red = C(255, 110, 100);
        private ID2D1HwndRenderTarget Target => G2AppBase.Instance!.RenderTarget;

        public GameHud() => brush = Target.CreateSolidColorBrush(White);
        private void Box(float x, float y, float w, float h, Color4 color)
        { brush.Color = color; Target.FillRectangle(new Rect(x, y, w, h), brush); }
        private void Text(G2Font font, string text, float x, float y, float w, float h, Color4 color)
        {
            // 참고 로고처럼 굵은 글자의 가장자리에 짙은 잉크선을 두른다.
            bool display = font == resultTitle || font == resultNumber || font == number;
            if (display)
            {
                float edge = font == resultTitle ? 1.5f : 0.8f;
                Color4 ink = C(14, 21, 16);
                font.DrawText(text, new Rect(x + 2, y + 3, w, h), ink);
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * MathF.PI / 4;
                    font.DrawText(text, new Rect(x + MathF.Cos(angle) * edge,
                        y + MathF.Sin(angle) * edge, w, h), ink);
                }
                color = C(202, 203, 180);
            }
            font.DrawText(text, new Rect(x, y, w, h), color);
        }

        public void DrawStatus(GameAssets assets, Player player, int score, int remaining)
        {
            // 하나의 손으로 그린 철판 위에 실제 수치만 올린다.
            assets.DrawHud(new Rect(10, 0, 880, 90));
            assets.DrawTitle(new Rect(55, 19, 156, 52));
            bool danger = player.Hp <= 2;
            Text(small, danger ? "체력 · 위험!" : "체력", 240, 24, 150, 17, danger ? Red : Muted);
            for (int i = 0; i < 5; i++)
                assets.DrawSprite(i < player.Hp ? "hp_full" : "hp_empty", 254 + i * 29, 52, 28);

            bool ready = player.KickCooldown <= 0;
            assets.DrawSprite(ready ? "kick_ready" : "kick_cooldown", 433, 45, 38);
            Text(small, "J  발차기", 461, 24, 108, 17, Muted);
            Text(body, ready ? "준비 완료" : "다시 준비 중", 461, 42, 110, 22, ready ? Gold : White);
            Box(461, 64, 95, 3, C(64, 57, 45));
            Box(461, 64, 95 * Math.Clamp(1 - player.KickCooldown / 0.4f, 0, 1), 3, Gold);

            Text(small, "점수", 605, 24, 100, 17, Muted);
            Text(number, score.ToString(), 605, 39, 110, 30, White);
            Text(small, "남은 적", 742, 24, 100, 17, Muted);
            Text(number, $"{remaining}대", 742, 39, 90, 30, White);

            Text(body, "WASD  이동     J  발차기", 38, 586, 260, 24, White);
            Text(body, "벽에 날려 부딪히면 추가 피해!", 315, 586, 310, 24, Muted);
            Text(body, "Alt + Enter  전체 화면", 704, 586, 174, 24, Muted);
        }
        public void DrawEnemyHealth(float x, float y, int hp, bool flying)
        {
            Box(x - 15, y - 32, 30, 6, C(7, 13, 20, 0.9f));
            for (int i = 0; i < 2; i++)
                Box(x - 13 + i * 14, y - 30, 12, 2, i < hp ? (flying ? Gold : Red) : C(60, 68, 80));
        }

        public void DrawResult(GameAssets assets, bool lost, int score, int hp)
        {
            Box(0, 0, 900, 620, C(4, 10, 18, 0.76f));
            assets.DrawPanel(new Rect(150, 130, 600, 400));
            Color4 accent = lost ? Red : Gold;
            Text(centered, lost ? "로봇 작동 중지" : "모든 적 처치", 230, 211, 440, 24, accent);
            Text(resultTitle, lost ? "게임 오버" : "폐기장 정리 완료!", 220, 239, 460, 48, White);
            Box(310, 296, 280, 1, C(89, 78, 60));
            Text(centered, "최종 점수", 230, 303, 440, 24, Muted);
            Text(resultNumber, score.ToString(), 230, 326, 440, 55, Gold);
            Text(centered, lost ? "한 번 더 도전해 보세요" : $"남은 에너지  {hp} / 5", 230, 386, 440, 24, Muted);

            Text(centered, "Enter 키로 다시 도전", 280, 420, 340, 26, accent);
        }

        public void Dispose()
        {
            resultNumber.Dispose(); resultTitle.Dispose(); centered.Dispose(); number.Dispose();
            body.Dispose(); small.Dispose(); brush.Dispose();
        }
    }
}
