namespace ActionGame
{
    internal class Program
    {
        [STAThread]
        static void Main()
        {
            // glc2d도 내부적으로 Windows Forms 창을 사용하므로 먼저 초기화한다.
            ApplicationConfiguration.Initialize();
            // 교수님 예시처럼 Run이 게임 반복 실행을 담당한다.
            // using은 게임 창이 닫히면 Dispose를 호출하여 그래픽 자원을 정리한다.
            using GameMain game = new GameMain();
            game.Run();
        }
    }
}
