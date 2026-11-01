namespace ActionGame
{
    // 여러 파일에서 같은 숫자를 쓰므로 한곳에 모은다.
    // const는 실행 중 바뀌지 않는 값이다. static 클래스는 new 없이 사용한다.
    internal static class GameRules
    {
        public const int MaxHp = 5;
        public const int RoundCount = 3;
        public const float KickCooldown = 0.4f;
        public const float LaunchSpeed = 600;
        public const float FlyDuration = 0.65f;
        public const int DefeatScore = 100;
        public const int ChainScore = 50;
        public const float HazardCycle = 6;
        public const float HazardWarning = 2.5f;
        public const float HazardStart = 4;
        public const float ReadyDuration = 1.5f;

        public static int EnemyCount(int round)
        {
            return 4 + round * 2; // 1, 2, 3라운드에 각각 6, 8, 10대
        }

        public static string RoundName(int round)
        {
            if (round == 1) return "고철 정리";
            if (round == 2) return "전력 누출";
            return "과부하";
        }

        public static int TimeBonus(float elapsed)
        {
            // (int)는 소수 부분을 버린다. 시간이 길어져도 보너스는 음수가 되지 않는다.
            return Math.Max(0, 600 - (int)elapsed * 10);
        }
    }
}
