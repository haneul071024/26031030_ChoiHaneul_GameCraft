using System.Collections;
using System.Reflection;
using ActionGame;

class Program
{
    static readonly Type Game = typeof(Player).Assembly.GetType("ActionGame.GameMain")!;
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static object game = null!;
    static object Get(string name) => Game.GetField(name, Flags)!.GetValue(game)!;
    static void Set(string name, object value) => Game.GetField(name, Flags)!.SetValue(game, value);
    static void Call(string name) => Game.GetMethod(name, Flags)!.Invoke(game, null);
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine("PASS " + message); }
    static IList Enemies => (IList)Get("enemies");
    static Player Player => (Player)Get("player");
    static string State => Get("state").ToString()!;
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        game = Activator.CreateInstance(Game, true)!;
        // Developer-only visual preview; production game has no debug shortcuts.
        if (args.Contains("--electric") || args.Contains("--clear"))
        {
            using var timer = new System.Windows.Forms.Timer { Interval = 200 };
            timer.Tick += (_, _) => {
                timer.Stop();
                Call("StartGame");
                if (args.Contains("--clear")) { Enemies.Clear(); Call("CheckRoundEnd"); }
                else { Call("StartRound"); Call("StartRound"); Set("roundTime", 3.8f); Player.InvincibleTime = 999; }
            };
            timer.Start();
            try { Game.GetMethod("Run", Flags)!.Invoke(game, null); }
            finally { ((IDisposable)game).Dispose(); }
            return;
        }
        try
        {
            var viewportMethod = Game.BaseType!.GetMethod("CalculateViewport", BindingFlags.Static | BindingFlags.NonPublic)!;
            foreach (var size in new[] { new System.Drawing.Size(900, 620), new System.Drawing.Size(1920, 1080), new System.Drawing.Size(2560, 1080), new System.Drawing.Size(800, 1200) })
            {
                var viewport = (System.Drawing.RectangleF)viewportMethod.Invoke(null, new object[] { size, new System.Drawing.Size(900, 620) })!;
                float scaleX = viewport.Width / 900;
                float scaleY = viewport.Height / 620;
                Check(Math.Abs(scaleX - scaleY) < 0.0001f && viewport.X >= -0.01f && viewport.Y >= -0.01f
                    && viewport.Right <= size.Width + 0.01f && viewport.Bottom <= size.Height + 0.01f
                    && Math.Abs(viewport.X + viewport.Width / 2 - size.Width / 2f) < 0.01f
                    && Math.Abs(viewport.Y + viewport.Height / 2 - size.Height / 2f) < 0.01f,
                    $"viewport {size.Width}x{size.Height}: uniform scale, centered, uncropped");
            }
            Call("Initialize");
            Check(State == "Lobby", "initial lobby and all resources load");
            Call("StartGame");
            Check(Enemies.Count == 6 && Player.Hp == 5 && (int)Get("round") == 1, "new game round 1");
            Check((float)Get("readyTime") == 1.5f, "round starts with preparation time");
            Player.Hp = 3;
            Set("roundTime", 12f);
            Enemies.Clear();
            Call("CheckRoundEnd");
            Check(State == "RoundClear" && Player.Hp == 4 && (int)Get("score") == 930, "round bonus uses pre-heal HP and heals once");
            Check((int)Get("recoveredHp") == 1 && (int)Get("healthBonus") == 150, "recovery and bonus details match actual values");
            Call("CheckRoundEnd");
            Check((int)Get("score") == 930 && Player.Hp == 4, "no duplicate clear reward");
            Call("StartRound");
            Check(Enemies.Count == 8 && (int)Get("score") == 930, "round 2 keeps score");
            Player.X = 450; Player.Y = 200;
            Set("roundTime", 3f); Call("UpdateHazard");
            Check(Player.Hp == 4, "warning is harmless");
            foreach (object item in Enemies)
            {
                item.GetType().GetField("X")!.SetValue(item, 100f);
                item.GetType().GetField("Y")!.SetValue(item, 150f);
            }
            object enemy = Enemies[0]!;
            var et = enemy.GetType();
            et.GetField("X")!.SetValue(enemy, 450f); et.GetField("Y")!.SetValue(enemy, 200f);
            Set("roundTime", 4.1f); Call("UpdateHazard"); Call("UpdateHazard");
            Check(Player.Hp == 3 && (int)et.GetField("Hp")!.GetValue(enemy)! == 1, "active hazard and invulnerability / per-cycle enemy damage");
            Set("roundTime", 6.1f); Call("UpdateHazard");
            Set("roundTime", 10.1f); Call("UpdateHazard"); Call("RemoveDefeatedEnemies");
            Check(Enemies.Count == 7 && (int)Get("score") == 1030, "next electric cycle kills enemy and awards score");
            Enemies.Clear(); Call("CheckRoundEnd"); Call("StartRound");
            Check(Enemies.Count == 10 && (int)Get("round") == 3, "round 3 spawn");
            Player.X = 200; Player.Y = 330; Player.InvincibleTime = 0;
            Set("roundTime", 4.1f); int hp = Player.Hp; Call("UpdateHazard");
            Check(Player.Hp == hp - 1, "round 3 horizontal electric strip");
            Enemies.Clear(); Call("CheckRoundEnd");
            Check(State == "Result" && Player.Hp > 0, "third round final victory");
            int best = (int)Get("bestScore"); Call("StartGame");
            Check((int)Get("score") == 0 && Player.Hp == 5 && (int)Get("bestScore") == best, "restart resets run and keeps session record");
            Enemies.Clear(); Player.Hp = 0; Call("CheckRoundEnd");
            Check(State == "Result" && (int)Get("score") == 0, "simultaneous death takes priority without clear bonus");
            Call("StartGame"); Enemies.Clear(); Call("CheckRoundEnd");
            Check((int)Get("recoveredHp") == 0 && Player.Hp == 5, "full health does not claim a repair");
            Call("StartGame");
            Game.GetMethod("AddScore", Flags)!.Invoke(game, new object[] { 100, 200f, 200f, "test" });
            Check((int)Get("score") == 100 && ((IList)Get("scorePopups")).Count == 1, "score popup accompanies actual points");
            Game.GetMethod("UpdateEffects", Flags)!.Invoke(game, new object[] { 0.9f });
            Check(((IList)Get("scorePopups")).Count == 0 && (int)Get("score") == 100, "expired popup is removed without changing score");

            // 3차 핵심 전투 검증: 실제 전투 함수를 호출하여 피해와 점수를 확인한다.
            Call("StartGame");
            Enemies.Clear();
            object AddEnemy(float x, float y)
            {
                object item = Activator.CreateInstance(et, new object[] { x, y })!;
                Enemies.Add(item);
                return item;
            }
            int EnemyHp(object item) => (int)et.GetField("Hp")!.GetValue(item)!;
            var kicked = AddEnemy(Player.X + 42, Player.Y);
            Call("Kick");
            Check(EnemyHp(kicked) == 1 && (float)et.GetField("FlyTime")!.GetValue(kicked)! > 0,
                "kick damages and launches enemy in front");
            Call("Kick");
            Check(EnemyHp(kicked) == 1, "kick cooldown prevents repeated damage");
            et.GetField("X")!.SetValue(kicked, 850f);
            Game.GetMethod("MoveEnemies", Flags)!.Invoke(game, new object[] { 0.05f });
            Call("RemoveDefeatedEnemies");
            Check(Enemies.Count == 0 && (int)Get("score") == 100, "wall impact finishes launched enemy and awards points");

            Call("StartGame"); Enemies.Clear();
            var flyingEnemy = AddEnemy(500, 300);
            var targetEnemy = AddEnemy(520, 300);
            Game.GetMethod("LaunchEnemy", Flags)!.Invoke(game, new object[] { flyingEnemy, 1f, 0f });
            Call("CheckEnemyCollisions");
            Check(EnemyHp(targetEnemy) == 1 && (int)Get("score") == 50,
                "chain impact damages target and awards chain points");
            Call("CheckEnemyCollisions");
            Check(EnemyHp(targetEnemy) == 1 && (int)Get("score") == 50,
                "same overlapping pair cannot repeatedly score");

            Call("StartGame"); Enemies.Clear(); AddEnemy(Player.X, Player.Y);
            Game.GetMethod("MoveEnemies", Flags)!.Invoke(game, new object[] { 0.01f });
            Game.GetMethod("MoveEnemies", Flags)!.Invoke(game, new object[] { 0.01f });
            Check(Player.Hp == 4 && Player.InvincibleTime > 0, "enemy contact respects player invulnerability");
        }
        finally { ((IDisposable)game).Dispose(); }
    }
}
