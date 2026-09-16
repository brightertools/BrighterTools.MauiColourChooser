using SkiaSharp.Views.Maui.Controls.Hosting;
namespace ChooserDemo;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp() => MauiApp.CreateBuilder().UseMauiApp<DemoApp>().UseSkiaSharp().Build();
}
