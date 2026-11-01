using System.Numerics;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace ActionGame
{
    // 화면 기준 900×620. 기존 렌더 타깃 배율을 그대로 사용한다.
    internal sealed class GameHud : IDisposable
    {
        // Windows 기본 한글 글꼴을 사용하여 다른 PC에서도 같은 배치를 유지한다.
        private readonly ID2D1SolidColorBrush brush;
        private readonly G2Font small = new("맑은 고딕", 11, FontWeight.Bold);
        private readonly G2Font body = new("맑은 고딕", 14, FontWeight.Normal);

        private readonly G2Font keyFont = new("맑은 고딕", 12, FontWeight.Bold,
            textAlignment: TextAlignment.Center);
        private readonly G2Font number = new("맑은 고딕", 22, FontWeight.Bold);
        private readonly G2Font centered = new("맑은 고딕", 16, FontWeight.Bold,
            textAlignment: TextAlignment.Center);
        private readonly G2Font resultTitle = new("맑은 고딕", 32, FontWeight.Bold,
            textAlignment: TextAlignment.Center);
        private readonly G2Font resultNumber = new("맑은 고딕", 40, FontWeight.Bold,
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
            for (int i = 0; i < GameRules.MaxHp; i++)
                assets.DrawSprite(i < player.Hp ? "hp_full" : "hp_empty", 254 + i * 29, 52, 28);

            bool ready = player.KickCooldown <= 0;
            assets.DrawSprite(ready ? "kick_ready" : "kick_cooldown", 433, 45, 38);
            Text(small, "J  발차기", 461, 24, 108, 17, Muted);
            Text(body, ready ? "공격 가능" : "충전 중", 461, 42, 110, 22, ready ? Gold : White);
            Box(461, 64, 95, 3, C(64, 57, 45));
            Box(461, 64, 95 * Math.Clamp(1 - player.KickCooldown / GameRules.KickCooldown, 0, 1), 3, Gold);

            Text(small, "점수", 605, 24, 100, 17, Muted);
            Text(number, score.ToString("N0"), 605, 39, 110, 30, White);
            Text(small, "남은 적", 742, 24, 100, 17, Muted);
            Text(number, $"{remaining}대", 742, 39, 90, 30, White);

            // 키 모양과 행동 이름을 분리해서 빠르게 찾을 수 있게 한다.
            KeyHint("WASD / 방향키", "이동", 38, 583, 116);
            KeyHint("J", "발차기", 235, 583, 30);
            KeyHint("Esc", "일시정지", 390, 583, 42);
            KeyHint("M", "음악", 590, 583, 30);
            KeyHint("Alt+Enter", "전체 화면", 710, 583, 78);
        }
        public void DrawEnemyHealth(float x, float y, int hp, bool flying)
        {
            Box(x - 15, y - 32, 30, 6, C(7, 13, 20, 0.9f));
            for (int i = 0; i < 2; i++)
                Box(x - 13 + i * 14, y - 30, 12, 2, i < hp ? (flying ? Gold : Red) : C(60, 68, 80));
        }

        public void DrawResult(GameAssets assets, bool lost, int score, int hp, int round, int bestScore)
        {
            Box(0, 0, 900, 620, C(4, 10, 18, 0.82f));
            assets.DrawPanel(new Rect(135, 65, 630, 495));
            Color4 accent = Gold;
            if (lost) accent = Red;
            Text(centered, lost ? "작전 중단" : "작전 성공", 210, 157, 480, 26, accent);
            Text(resultTitle, lost ? "다시 일어설 시간" : "폐기장 정리 완료", 190, 190, 520, 50, White);
            Text(centered, "최종 점수", 300, 252, 300, 23, Muted);
            Text(resultNumber, score.ToString("N0"), 230, 278, 440, 60, accent);
            Text(centered, $"도달 구역  {round} / 3     ·     남은 에너지  {hp} / {GameRules.MaxHp}", 190, 350, 520, 26, White);
            Text(centered, $"이번 실행 최고 기록  {bestScore:N0}", 190, 387, 520, 25, Muted);
            KeyHint("Enter", "다시 도전", 290, 445, 54);
            KeyHint("Esc", "로비", 510, 445, 42);
        }
        // 로비 미리보기는 실제 리소스만 재사용하며 전투 규칙은 실행하지 않는다.
        private void DrawStagePreview(GameAssets assets, int stage, float x, float y, float time)
        {
            assets.DrawFloor(new Rect(x, y, 245, 140));
            if (stage >= 2)
            {
                int frame = 2 + (int)(time * 10) % 2;
                for (int j = 0; j < 4; j++) assets.DrawElectricTile(new Rect(x + 111, y + j * 35, 27, 35), frame);
                if (stage == 3)
                    for (int j = 0; j < 7; j++) assets.DrawElectricTile(new Rect(x + j * 35, y + 58, 35, 24), frame);
            }
            float cycle = time % 2;
            assets.DrawSprite("player_kick", x + 65, y + 80, 49);
            assets.DrawSprite("kick_arc", x + 98, y + 80, 64, 0, 0.55f + 0.35f * MathF.Sin(time * 6));
            assets.DrawSprite("enemy_flying", x + 135 + cycle * 18, y + 80, 44, -0.2f);
            assets.DrawSprite("enemy_basic", x + 192, y + 38, 35, 2.4f);
            assets.DrawFrame("hit_spark_sheet", (int)(time * 8) % 4, x + 175, y + 80, 65);
        }

        public void DrawLobby(GameAssets assets, int bestScore, bool musicEnabled, float time)
        {
            Box(0, 0, 900, 620, C(5, 12, 16, 0.78f));
            assets.DrawTitle(new Rect(240, 18, 420, 140));
            Text(centered, "폐기장 정리 작전", 100, 161, 700, 28, Gold);
            // 단계마다 같은 배치를 반복하고 이름과 적 수만 바꾼다.
            for (int i = 0; i < 3; i++)
            {
                float x = 55 + i * 267;
                DrawStagePreview(assets, i + 1, x + 5, 211, time + i * 0.3f);
                assets.DrawHud(new Rect(x, 351, 255, 48));
                Text(centered, $"0{i + 1}  {GameRules.RoundName(i + 1)}", x + 5, 364, 245, 26, White);
                assets.DrawSprite("enemy_basic", x + 88, 418, 27);
                Text(body, $"× {GameRules.EnemyCount(i + 1)}", x + 110, 407, 85, 25, Muted);
            }
            KeyHint("WASD / 방향키", "이동", 175, 449, 116);
            KeyHint("J", "발차기", 425, 449, 32);
            KeyHint("Esc", "일시정지", 585, 449, 42);
            assets.DrawHud(new Rect(275, 489, 350, 65));
            assets.DrawSprite("kick_ready", 310, 520, 36, 0, 0.75f + MathF.Sin(time * 4) * 0.25f);
            Text(centered, "ENTER  작전 시작", 330, 507, 260, 30, Gold);
            Text(body, $"이번 실행 최고 기록  {bestScore:N0}", 58, 578, 380, 24, Muted);
            KeyHint("M", musicEnabled ? "음악 켜짐" : "음악 꺼짐", 680, 575, 30);
        }
        public void DrawRound(int round, float time, int remaining, bool musicEnabled)
        {
            Box(38, 98, 824, 37, C(7, 15, 19, 0.93f));
            Text(body, $"{round:00}  {GameRules.RoundName(round)}", 50, 104, 220, 24, Gold);
            Text(body, $"{(int)time / 60:00}:{(int)time % 60:00}", 275, 104, 70, 24, White);
            float phase = time % GameRules.HazardCycle;
            string status = "목표 · 모든 적 처치";
            Color4 statusColor = Muted;
            if (round >= 2)
            {
                if (phase >= GameRules.HazardStart)
                {
                    status = $"전류 방출  ·  종료까지 {GameRules.HazardCycle - phase:0.0}초";
                    statusColor = Cyan;
                }
                else if (phase >= GameRules.HazardWarning)
                {
                    status = $"고전압 경고  ·  작동까지 {GameRules.HazardStart - phase:0.0}초";
                    statusColor = Gold;
                }
                else status = $"충전 중  ·  작동까지 {GameRules.HazardStart - phase:0.0}초";
            }
            Text(body, status, 383, 104, 340, 24, statusColor);
            Text(small, musicEnabled ? "음악 켜짐" : "음악 꺼짐", 782, 108, 75, 19, Muted);
            // 남은 적이 0이면 처치 진행 막대가 끝까지 채워진다.
            float progress = 1 - (float)remaining / GameRules.EnemyCount(round);
            Box(38, 133, 824, 3, C(35, 45, 43));
            Box(38, 133, 824 * Math.Clamp(progress, 0, 1), 3, Gold);
        }

        public void DrawRoundClear(GameAssets assets, int round, int bonus, int score, float time,
            int recoveredHp, int timeBonus, int healthBonus)
        {
            Box(0, 0, 900, 620, C(4, 10, 18, 0.82f));
            assets.DrawPanel(new Rect(125, 45, 650, 530));
            Text(centered, $"{round:00}  {GameRules.RoundName(round)} · 정리 완료", 150, 133, 600, 27, Gold);
            Text(resultTitle, "다음 구역으로", 150, 165, 600, 50, White);
            assets.DrawSprite("player_idle", 275, 285, 88, -0.4f);
            assets.DrawSprite("hp_full", 350, 266, 37 + MathF.Sin(time * 4) * 3);
            string recovery = "에너지 가득";
            if (recoveredHp > 0) recovery = $"에너지 +{recoveredHp}";
            Text(centered, recovery, 290, 305, 120, 25, Cyan);
            DrawStagePreview(assets, round + 1, 415, 221, time);
            Text(small, $"다음 구역  {GameRules.RoundName(round + 1)}", 425, 366, 240, 20, Muted);
            Text(centered, $"구역 300  +  시간 {timeBonus}  +  체력 {healthBonus}", 150, 396, 600, 25, Muted);
            Text(centered, $"보너스 +{bonus:N0}     ·     누적 {score:N0}", 150, 428, 600, 30, Gold);
            KeyHint("Enter", "다음 구역", 280, 479, 54);
            KeyHint("Esc", "로비", 515, 479, 42);
        }

        public void DrawPause(GameAssets assets, bool musicEnabled)
        {
            Box(0, 0, 900, 620, C(4, 10, 18, 0.84f));
            assets.DrawPanel(new Rect(195, 105, 510, 430));
            Text(centered, "작전 대기", 230, 200, 440, 25, Gold);
            Text(resultTitle, "잠시 쉬어 가세요", 220, 234, 460, 50, White);
            KeyHint("Esc", "계속하기", 335, 310, 48);
            KeyHint("M", musicEnabled ? "음악 끄기" : "음악 켜기", 335, 354, 48);
            KeyHint("L", "로비로 돌아가기", 335, 398, 48);
            Text(centered, "로비로 돌아가면 현재 도전이 종료됩니다", 200, 455, 500, 23, Muted);
        }

        public void DrawReady(int round, float remaining)
        {
            Box(0, 235, 900, 150, C(4, 10, 18, 0.9f));
            Text(centered, $"구역 {round:00} / 03", 100, 249, 700, 26, Gold);
            Text(resultTitle, GameRules.RoundName(round), 100, 282, 700, 50, White);
            Text(centered, "작전 준비", 100, 338, 700, 27, Muted);
            Box(300, 375, 300 * (1 - remaining / GameRules.ReadyDuration), 3, Gold);
        }

        public void DrawScorePopup(ScorePopup popup)
        {
            // 남은 시간을 투명도로 쓰면 점수가 천천히 사라진다.
            float opacity = Math.Clamp(popup.Life / 0.4f, 0, 1);
            Text(centered, popup.Text, popup.X - 85, popup.Y - 38, 170, 26, C(255, 216, 110, opacity));
        }

        private void KeyHint(string key, string action, float x, float y, float keyWidth)
        {
            // 긴 공백으로 정렬하지 않고 키와 설명에 각각 위치를 지정한다.
            Box(x, y + 2, keyWidth, 25, C(3, 8, 12));
            Box(x, y, keyWidth, 23, C(51, 64, 58));
            Text(keyFont, key, x, y + 4, keyWidth, 19, White);
            Text(body, action, x + keyWidth + 10, y + 2, 170, 24, Muted);
        }

        public void Dispose()
        {
            // 글꼴과 붓은 그래픽 자원이므로 사용이 끝났을 때 직접 해제한다.
            resultNumber.Dispose();
            resultTitle.Dispose();
            centered.Dispose();
            number.Dispose();
            body.Dispose();
            small.Dispose();
            keyFont.Dispose();
            brush.Dispose();
        }
    }
}
