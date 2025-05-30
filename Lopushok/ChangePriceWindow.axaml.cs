using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;


namespace Lopushok
{
    public partial class ChangePriceWindow : Window
    {
        public decimal? EnteredPrice { get; private set; }

        public ChangePriceWindow(decimal suggestedPrice)
        {
            InitializeComponent();
            PriceBox.Text = suggestedPrice.ToString("0.##");
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }

        private void Apply_Click(object? sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(PriceBox.Text?.Replace(',', '.'), out var price) && price >= 0)
            {
                EnteredPrice = price;
                Close(EnteredPrice);
            }
            else
            {
                MessageBoxAvalon.Show(this, "Введите корректное число.", "Ошибка", MessageBoxAvalon.MessageBoxButtons.Ok);
            }
        }

    }
}