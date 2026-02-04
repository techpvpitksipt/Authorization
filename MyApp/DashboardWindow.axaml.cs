using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MyApp;

public partial class DashboardWindow : Window
{
    private TextBlock _userInfoText = null!;

    public DashboardWindow(string fio, string role)
    {
        InitializeComponent();

        _userInfoText = this.FindControl<TextBlock>("UserInfoText");
        _userInfoText.Text = $"{fio} ({role})";
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnLogoutClick(object? sender, RoutedEventArgs e)
    {
        var login = new MainWindow();
        login.Show();
        Close();
    }
}
