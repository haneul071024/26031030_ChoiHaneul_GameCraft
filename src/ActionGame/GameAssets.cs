using System.Numerics;
using Vortice.Mathematics;

namespace ActionGame
{
    // 텍스처·효과음을 한 번 로딩하고 종료할 때 함께 해제한다.
    internal sealed class GameAssets : IDisposable
    {
        private readonly Dictionary<string, G2Texture> images = new();
        private readonly Dictionary<string, G2Audio> sounds = new();
        private readonly HashSet<string> pendingSounds = new();
        private readonly Dictionary<string, double> lastPlayed = new();
        public bool SoundEnabled => sounds.Count > 0;

        public GameAssets()
        {
            try
            {
                // 게임에서 사용하는 역할별 리소스만 읽는다.
                foreach (string path in new[] {
                    "player/player_idle", "player/player_kick", "player/player_walk_sheet",
                    "enemy/enemy_basic", "enemy/enemy_flying", "enemy/enemy_walk_sheet",
                    "stage/arena_floor", "effect/kick_arc", "effect/hit_spark_sheet",
                    "effect/enemy_break_sheet", "effect/wall_hit_sheet",
                    "ui/hp_full", "ui/hp_empty", "ui/kick_ready", "ui/kick_cooldown",
                    "ui/scrap_hud", "ui/scrap_result", "ui/title_logo" })
                    images.Add(Path.GetFileName(path), new G2Texture(ResourcePath("image/" + path + ".png")));
                // 출력 장치가 없는 PC에서도 게임은 실행된다.
                if (G2AudioContext.Instance?.Audio != null)
                    foreach (string name in new[] { "kick_swing", "metal_hit", "wall_hit", "player_hurt",
                        "enemy_break", "clear", "game_over", "restart" })
                        sounds.Add(name, new G2Audio(ResourcePath("sound/sfx/" + name + ".wav")));
                G2AudioContext.Instance?.MasteringVoice?.SetVolume(0.65f);
            }
            catch { Dispose(); throw; }
        }

        private static string ResourcePath(string relative) =>
            Path.Combine(AppContext.BaseDirectory, "resource", relative.Replace('/', Path.DirectorySeparatorChar));

        public void DrawFloor(Rect destination) => images["arena_floor"].Draw(destination, new Rect(0, 0, 840, 480));

        // 로컬 회전 뒤 기존 화면 배율을 곱해야 전체 화면에서도 중심이 맞는다.
        public void DrawSprite(string name, float x, float y, float size, float angle = 0, float opacity = 1)
        {
            var target = G2AppBase.Instance!.RenderTarget;
            Matrix3x2 saved = target.Transform;
            try
            {
                target.Transform = Matrix3x2.CreateRotation(angle) * Matrix3x2.CreateTranslation(x, y) * saved;
                float sourceSize = name == "kick_arc" ? 96 : 64;
                // 자세별 여백 차이를 보정해 몸 중심과 충돌 중심을 맞춘다.
                float offsetX = name == "player_kick" ? size * 0.075f : 0;
                images[name].Draw(new Rect(-size / 2 + offsetX, -size / 2, size, size),
                    new Rect(0, 0, sourceSize, sourceSize), opacity);
            }
            finally { target.Transform = saved; }
        }

        public void DrawTitle(Rect destination) => images["title_logo"].Draw(destination, new Rect(0, 0, 2172, 724));

        public void DrawPanel(Rect destination) => images["scrap_result"].Draw(destination, new Rect(0, 0, 1536, 1024));

        public void DrawHud(Rect destination)
        {
            // 양 끝 볼트의 비율을 보존하고 가운데 철판만 늘린다.
            float cap = destination.Height * 240 / 724;
            images["scrap_hud"].Draw(new Rect(destination.X, destination.Y, cap, destination.Height), new Rect(0, 0, 240, 724));
            images["scrap_hud"].Draw(new Rect(destination.X + cap, destination.Y, destination.Width - cap * 2, destination.Height), new Rect(240, 0, 1692, 724));
            images["scrap_hud"].Draw(new Rect(destination.X + destination.Width - cap, destination.Y, cap, destination.Height), new Rect(1932, 0, 240, 724));
        }

        // 2×2 시트의 프레임을 선택한다. 벽 이펙트만 접촉점이 오른쪽에 있다.
        public void DrawFrame(string sheet, int frame, float x, float y, float size, float angle = 0, float opacity = 1)
        {
            frame = Math.Clamp(frame, 0, 3);
            var target = G2AppBase.Instance!.RenderTarget;
            Matrix3x2 saved = target.Transform;
            try
            {
                target.Transform = Matrix3x2.CreateRotation(angle) * Matrix3x2.CreateTranslation(x, y) * saved;
                float anchorX = sheet == "wall_hit_sheet" ? 0.83f : 0.5f;
                images[sheet].Draw(new Rect(-size * anchorX, -size / 2, size, size),
                    new Rect(frame % 2 * 128, frame / 2 * 128, 128, 128), opacity);
            }
            finally { target.Transform = saved; }
        }

        public void QueueSound(string name) => pendingSounds.Add(name);

        public void FlushSounds(double time)
        {
            foreach (string name in pendingSounds)
            {
                // 연쇄 충돌 시 같은 음이 매 프레임 끊겨 재시작되지 않게 한다.
                if (lastPlayed.TryGetValue(name, out double previous) && time - previous < 0.06) continue;
                if (sounds.TryGetValue(name, out G2Audio? sound)) sound.Play();
                lastPlayed[name] = time;
            }
            pendingSounds.Clear();
        }

        public void StopSounds()
        {
            pendingSounds.Clear();
            lastPlayed.Clear();
            foreach (G2Audio sound in sounds.Values) sound.Stop();
        }

        public void Dispose()
        {
            foreach (G2Audio sound in sounds.Values) sound.Dispose();
            sounds.Clear();
            foreach (G2Texture image in images.Values) image.Dispose();
            images.Clear();
            pendingSounds.Clear();
        }
    }
}
