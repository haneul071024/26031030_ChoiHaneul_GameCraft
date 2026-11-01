// -------------------------------------------------------------------------------------------------------------------------------------------------------------
// Author: 3dapi (https://github.com/3dapi)
// -------------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using Vortice.WinForms;

abstract class G2AppBase : IDisposable
{
	public static G2AppBase? Instance { get; private set; }

	public ID2D1HwndRenderTarget RenderTarget => _graphics.RenderTarget;
	public IDWriteFactory DWriteFactory => _graphics.DWriteFactory;
	public G2InputContext Input => _inputContext;

	public virtual System.Drawing.Size ScreenSize => new(640, 480);
	public virtual string GameName => "G2 Game";
	public virtual Color4 ClearColor { get; set; } = new(0.0f, 0.0f, 0.0f, 1.0f);

	public double DeltaTime { get; private set; }
	public double TotalTime { get; private set; }

	// 가로와 세로를 따로 늘리면 캐릭터와 글자가 찌그러진다.
	// 더 작은 배율 하나를 두 방향에 똑같이 적용하고 남는 공간은 양쪽에 나눈다.
	public System.Drawing.RectangleF Viewport => CalculateViewport(_mainForm.ClientSize, ScreenSize);
	public float ScaleX => Viewport.Width / ScreenSize.Width;
	public float ScaleY => ScaleX;

	internal static System.Drawing.RectangleF CalculateViewport(System.Drawing.Size client, System.Drawing.Size game)
	{
		// 최소화된 창도 0으로 나누지 않도록 계산용 크기는 최소 1로 둔다.
		float scale = Math.Min((float)Math.Max(1, client.Width) / game.Width,
			(float)Math.Max(1, client.Height) / game.Height);
		float width = game.Width * scale;
		float height = game.Height * scale;
		return new System.Drawing.RectangleF((client.Width - width) / 2,
			(client.Height - height) / 2, width, height);
	}

	public static float ScreenScaleX => Instance?.ScaleX ?? throw new InvalidOperationException("ScreenScaleX::G2AppBase instance is not initialized.");
	public static float ScreenScaleY => Instance?.ScaleY ?? throw new InvalidOperationException("ScreenScaleY::G2AppBase instance is not initialized.");
	//-----------------------------------------------------------------------------------------------------------------------------------------
	private readonly RenderForm _mainForm;
	private readonly G2D2DContext _graphics;
	private readonly G2AudioContext _audioContext;
	private readonly G2InputContext _inputContext;
	private readonly Stopwatch _stopwatch = new();

	private bool _isFullscreen = false;
	private FormBorderStyle _originalFormBorderStyle = FormBorderStyle.Sizable;
	private Rectangle _originalWindowBounds;
	private double _previousTime;

	//-----------------------------------------------------------------------------------------------------------------------------------------
	// Abstract methods to be implemented by derived classes
	protected abstract void Initialize();
	protected abstract void Update();
	protected abstract void Render();

	//-----------------------------------------------------------------------------------------------------------------------------------------
	// Constructor
	protected G2AppBase()
	{
		if (Instance != null)
		{
			throw new InvalidOperationException("G2AppBase instance already exists.");
		}
		_mainForm = new RenderForm
		{
			Text = GameName,
			StartPosition = FormStartPosition.CenterScreen,
			ClientSize = ScreenSize
		};
		G2D2DContext? graphics = null;
		G2AudioContext? audioContext = null;
		G2InputContext? inputContext = null;
		try
		{
			// 먼저 창 핸들을 만들면 현재 모니터의 DPI를 알 수 있다.
			// 예: 배율 200%에서 논리 크기 900은 실제 픽셀 1800에 해당한다.
			// 화면보다 커지지 않도록 작업 영역 안으로 제한한다.
			IntPtr windowHandle = _mainForm.Handle;
			float dpiScale = _mainForm.DeviceDpi / 96f;
			Rectangle workArea = Screen.FromHandle(windowHandle).WorkingArea;
			float fit = Math.Min(dpiScale, Math.Min(
				(workArea.Width - 40f) / ScreenSize.Width,
				(workArea.Height - 100f) / ScreenSize.Height));
			_mainForm.ClientSize = new System.Drawing.Size(
				(int)(ScreenSize.Width * fit), (int)(ScreenSize.Height * fit));
			graphics = new G2D2DContext(
				_mainForm.Handle,
				_mainForm.ClientSize.Width,
				_mainForm.ClientSize.Height);
			audioContext = new G2AudioContext();
			inputContext = new G2InputContext(_mainForm);
			_audioContext = audioContext;
			_graphics = graphics;
			_inputContext = inputContext;
			_mainForm.Resize += MainFormResize;
		}
		catch
		{
			inputContext?.Dispose();
			audioContext?.Dispose();
			graphics?.Dispose();
			_mainForm.Dispose();
			throw;
		}
		Instance = this;
	}

	public void Run()
	{
		try
		{
			Initialize();
			DeltaTime = 0.0;
			TotalTime = 0.0;
			_previousTime = 0.0;
			_stopwatch.Restart();
			RenderLoop.Run(_mainForm, MainRender);
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "Game Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		_stopwatch.Stop();
	}

	private void MainRender()
	{
		UpdateTime();
		_inputContext.Update();
		G2InputContext.InputState altState = _inputContext.KeyState(Keys.Menu);
		if (_inputContext.IsKeyDown(Keys.Enter) &&
			(altState == G2InputContext.InputState.Down ||
			 altState == G2InputContext.InputState.Press))
		{
			ToggleFullscreen();
		}
		Update2D();
		Render2D();
	}

	private void UpdateTime()
	{
		double currentTime = _stopwatch.Elapsed.TotalSeconds;
		DeltaTime = currentTime - _previousTime;
		TotalTime = currentTime;
		_previousTime = currentTime;
	}

	private void Update2D()
	{
		Update();
	}

	private void Render2D()
	{
		if (_mainForm.ClientSize.Width <= 0 || _mainForm.ClientSize.Height <= 0) return;
		ID2D1HwndRenderTarget renderTarget = _graphics.RenderTarget;
		System.Drawing.RectangleF viewport = Viewport;
		float scale = viewport.Width / ScreenSize.Width;
		// 먼저 같은 비율로 확대하고, 여백만큼 이동시켜 화면 가운데에 놓는다.
		renderTarget.Transform = System.Numerics.Matrix3x2.CreateScale(scale) *
			System.Numerics.Matrix3x2.CreateTranslation(viewport.X, viewport.Y);
		renderTarget.BeginDraw();
		renderTarget.Clear(ClearColor);

		// 불꽃이나 큰 이미지도 게임 영역 밖의 여백에는 그려지지 않게 자른다.
		renderTarget.PushAxisAlignedClip(new Rect(0, 0, ScreenSize.Width, ScreenSize.Height), AntialiasMode.Aliased);
		try { Render(); }
		finally { renderTarget.PopAxisAlignedClip(); }

		renderTarget.EndDraw();
	}

	private void MainFormResize(object? sender, EventArgs e)
	{
		if (_mainForm.ClientSize.Width <= 0 || _mainForm.ClientSize.Height <= 0)
		{
			return;
		}
		_graphics.Resize(_mainForm.ClientSize.Width, _mainForm.ClientSize.Height);
	}

	public virtual void Dispose()
	{
		_stopwatch.Stop();
		_mainForm.Resize -= MainFormResize;
		_inputContext.Dispose();
		_audioContext.Dispose();
		_graphics.Dispose();
		_mainForm.Dispose();
		G2TextureLoader.Release();
		Instance = null;
	}

	private void ToggleFullscreen()
	{
		if (!_isFullscreen)
		{
			_originalFormBorderStyle = _mainForm.FormBorderStyle;
			_originalWindowBounds = _mainForm.Bounds;
			_mainForm.FormBorderStyle = FormBorderStyle.None;
			_mainForm.WindowState = FormWindowState.Maximized;
			_isFullscreen = true;
		}
		else
		{
			_mainForm.FormBorderStyle = _originalFormBorderStyle;
			_mainForm.WindowState = FormWindowState.Normal;
			_mainForm.Bounds = _originalWindowBounds;
			_isFullscreen = false;
		}
	}

	public void Close()
	{
		_mainForm?.Close();
	}
}
