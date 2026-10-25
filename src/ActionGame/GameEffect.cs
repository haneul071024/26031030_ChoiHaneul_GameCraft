namespace ActionGame
{
    // 적이 제거되어도 남은 파괴 애니메이션을 끝까지 재생한다.
    internal sealed class GameEffect
    {
        public string Sheet { get; }
        public float X { get; }
        public float Y { get; }
        public float Size { get; }
        public float Angle { get; }
        public float Duration { get; }
        public float Age { get; private set; }
        public int Frame => Math.Clamp((int)(Age / Duration * 4), 0, 3);
        public bool Finished => Age >= Duration;

        public GameEffect(string sheet, float x, float y, float size, float duration, float angle = 0)
        {
            Sheet = sheet; X = x; Y = y; Size = size; Duration = duration; Angle = angle;
        }

        public void Update(float dt) => Age += dt;
    }
}
