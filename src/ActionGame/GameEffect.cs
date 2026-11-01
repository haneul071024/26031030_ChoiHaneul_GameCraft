namespace ActionGame
{
    // 적이 제거되어도 남은 파괴 애니메이션을 끝까지 재생한다.
    internal sealed class GameEffect
    {
        // get만 있는 속성은 외부에서 값을 바꿀 수 없다. 생성자에서 처음 값을 정한다.
        public string Sheet { get; }
        public float X { get; }
        public float Y { get; }
        public float Size { get; }
        public float Angle { get; }
        public float Duration { get; }
        public float Age { get; private set; }
        // 재생 진행률 Age / Duration에 4를 곱해 0~3 프레임 번호로 바꾼다.
        public int Frame => Math.Clamp((int)(Age / Duration * 4), 0, 3);
        public bool Finished => Age >= Duration;

        public GameEffect(string sheet, float x, float y, float size, float duration, float angle = 0)
        {
            Sheet = sheet; X = x; Y = y; Size = size; Duration = duration; Angle = angle;
        }

        public void Update(float dt)
        {
            Age += dt;
        }
    }
}
