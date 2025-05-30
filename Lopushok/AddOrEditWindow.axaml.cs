using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Lopushok.Models;

namespace Lopushok
{
    public partial class AddOrEditWindow : Window
    {
        private ProductDAO _product;
        private List<MaterialDAO> _allMaterials;
        private string? _imagePath;
        private RemoteDatabaseContext _context;

        public AddOrEditWindow()
        {
            InitializeComponent();
        }

        public AddOrEditWindow(ProductDAO product, List<ProductTypeDAO> productTypes, List<MaterialDAO> materials, RemoteDatabaseContext context)
        {
            InitializeComponent();

            _product = product;
            _allMaterials = materials;
            _context = context;

            // Привязка типов продуктов
            ProductTypeBox.ItemsSource = productTypes;
            ProductTypeBox.SelectedItem = product.ProductType;

            // Заполнение текстовых полей
            TitleBox.Text = product.ProductName;
            ArticleNumberBox.Text = product.Article;
            MinCostBox.Text = product.MinAgentCost.ToString();
            ProdPersonCountBox.Text = product.WorkersRequired.ToString();

            // Изображение
            ImagePreview.Source = LoadImage(product.ImagePath);
            _imagePath = product.ImagePath;

            // Материалы
            MaterialBox.ItemsSource = _allMaterials;
            MaterialList.ItemsSource = _product.ProductMaterials;

            // Артикул редактируется только при создании
            ArticleNumberBox.IsEnabled = string.IsNullOrEmpty(product.Article);
            Console.WriteLine("Типов продуктов: " + productTypes?.Count);
            Console.WriteLine("Материалов: " + _allMaterials?.Count);
            Console.WriteLine("Материалов у продукта: " + _product.ProductMaterials?.Count);

        }

        private Bitmap? LoadImage(string? path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var fullPath = Path.Combine(AppContext.BaseDirectory, "Images", path);
            if (!File.Exists(fullPath)) return null;

            using var stream = File.OpenRead(fullPath);
            return new Bitmap(stream);
        }

        private async void SelectImage_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Images", Extensions = { "jpg", "png", "jpeg" } }
                },
                AllowMultiple = false
            };

            var result = await dialog.ShowAsync(this);
            var file = result?.FirstOrDefault();

            if (!string.IsNullOrEmpty(file))
            {
                _imagePath = Path.GetFileName(file);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    using var stream = File.OpenRead(file);
                    ImagePreview.Source = new Bitmap(stream);
                });
            }
        }

        private void AddMaterial_Click(object? sender, RoutedEventArgs e)
        {
            if (MaterialBox.SelectedItem is not MaterialDAO material) return;
            if (!int.TryParse(MaterialQuantityBox.Text, out int quantity) || quantity <= 0) return;

            var existing = _product.ProductMaterials.FirstOrDefault(pm => pm.Material == material);
            if (existing != null)
                existing.RequiredQuantity += quantity;
            else
                _product.ProductMaterials.Add(new ProductMaterialDAO
                {
                    Material = material,
                    RequiredQuantity = quantity
                });

            // Обновление источника
            MaterialList.ItemsSource = null;
            MaterialList.ItemsSource = _product.ProductMaterials;
        }

        private void Save_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleBox.Text) ||
                string.IsNullOrWhiteSpace(ArticleNumberBox.Text) ||
                string.IsNullOrWhiteSpace(MinCostBox.Text) ||
                string.IsNullOrWhiteSpace(ProdPersonCountBox.Text) ||
                ProductTypeBox.SelectedItem is not ProductTypeDAO type)
            {
                return;
            }

            _product.ProductName = TitleBox.Text;
            _product.Article = ArticleNumberBox.Text;

            if (decimal.TryParse(MinCostBox.Text, out var minCost))
                _product.MinAgentCost = minCost;

            if (int.TryParse(ProdPersonCountBox.Text, out var count))
                _product.WorkersRequired = count;

            _product.ProductType = type;
            _product.ImagePath = _imagePath;

            Close(_product);
        }

        private void Delete_Click(object? sender, RoutedEventArgs e)
        {
            // Пример: не даём удалять, если есть продажи
            // if (_product.Sales.Any())
            // {
            //     MessageBoxAvalon.Show(this, "Нельзя удалить продукт с продажами.", "Ошибка");
            //     return;
            // }

            Close(null);
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
