using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RusCosm.Model;

namespace RusCosm;

public partial class MainWindow : Window
{
    private readonly User6Context db = new();
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void LoginBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var login = LoginTxt.Text;
        var password = PasswordTxt.Text;
        var users = db.Employees.ToList();
        foreach (var user in users)
        {
            if (user.Login == login && user.Password == password)
            {
                OrderWindow order = new();
                order.Show();
                this.Close();
            }
            else
            {
                ErrorBox.Text = "Неверный логин или пароль";
            }
        }
    }
}