// -------------------------------------------------------------------------------------------------------------------------------------------------------------
// Author: 3dapi (https://github.com/3dapi)
// -------------------------------------------------------------------------------------------------------------------------------------------------------------

using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;

class G2D2DContext : IDisposable
{
	public static G2D2DContext? Instance { get; private set; }

	public ID2D1HwndRenderTarget RenderTarget { get; private set; }
	public ID2D1Factory1 Factory { get; }
	public IDWriteFactory DWriteFactory { get; }

	public G2D2DContext(IntPtr hwnd, int width, int height)
	{
		if (Instance != null)
		{
			throw new InvalidOperationException("G2D2DContext instance already exists.");
		}
		Factory = D2D1.D2D1CreateFactory<ID2D1Factory1>();
		DWriteFactory = DWrite.DWriteCreateFactory<IDWriteFactory>();
		RenderTarget = Factory.CreateHwndRenderTarget(
			  new RenderTargetProperties()
			, new HwndRenderTargetProperties
			{
				Hwnd = hwnd,
				PixelSize = new SizeI(width, height)
			}
		);
		// 우리 게임은 창의 실제 픽셀 크기로 화면 배율을 직접 계산한다.
		// Direct2D가 Windows의 150%, 200% 배율까지 다시 적용하면
		// 그림이 두 번 확대되어 오른쪽과 아래쪽이 잘린다.
		// 96 DPI로 고정해 Direct2D 좌표 1을 픽셀 1에 맞춘다.
		RenderTarget.SetDpi(96, 96);
		Instance = this;
	}

	public void Resize(int width, int height)
	{
		RenderTarget.Resize(new SizeI(width, height));
	}

	public void Dispose()
	{
		RenderTarget.Dispose();
		DWriteFactory.Dispose();
		Factory.Dispose();
		Instance = null;
	}
}
