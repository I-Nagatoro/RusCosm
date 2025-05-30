using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Lopushok.Models;
using Microsoft.EntityFrameworkCore;

namespace Lopushok
{
    public partial class MainWindow : Window
    {
        private const int PageSize = 20;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private List<ProductDisplay> _allProducts = new();
        private static readonly RemoteDatabaseContext _db = new();

        public MainWindow()
        {
            InitializeComponent();
            Resources.Add("BoolToRedConverter", new BoolToRedConverter());
            LoadData();
        }

        private void LoadData()
        {
            var products = _db.Products
                .Include(p => p.ProductType)
                .Include(p => p.ProductMaterials).ThenInclude(pm => pm.Material)
                .Select(p => new ProductDisplay
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ProductType = p.ProductType.ProductType,
                    Article = p.Article,
                    WorkshopNumber = p.WorkshopNumber,
                    MinAgentCost = p.MinAgentCost,
                    ImagePath = p.ImagePath,
                    LastSaleDate = _db.ProductSales
                        .Where(s => s.product_id == p.ProductId)
                        .OrderByDescending(s => s.sale_date)
                        .Select(s => s.sale_date)
                        .FirstOrDefault(),
                    IsStale = !_db.ProductSales
                        .Any(s => s.product_id == p.ProductId && s.sale_date >= DateOnly.FromDateTime(DateTime.Today.AddMonths(-1))),
                    Cost = p.ProductMaterials.Sum(pm => pm.Material.Cost * pm.RequiredQuantity)
                })
                .AsNoTracking()
                .ToList();

            _allProducts = products;
            InitFilters();
            ApplyFilters();
        }

        private void InitFilters()
        {
            NameSortBox.ItemsSource = new List<ComboBoxItem>
            {
                new ComboBoxItem { Content = "Наименование ↑", Tag = "NameAsc" },
                new ComboBoxItem { Content = "Наименование ↓", Tag = "NameDesc" }
            };
            NumWorkSortBox.ItemsSource = new List<ComboBoxItem>
            {
                new ComboBoxItem { Content = "Цех ↑", Tag = "WorkshopAsc" },
                new ComboBoxItem { Content = "Цех ↓", Tag = "WorkshopDesc" }
            };
            MinCostBox.ItemsSource = new List<ComboBoxItem>
            {
                new ComboBoxItem { Content = "Цена ↑", Tag = "PriceAsc" },
                new ComboBoxItem { Content = "Цена ↓", Tag = "PriceDesc" }
            };

            NameSortBox.SelectedIndex = 0;
            NumWorkSortBox.SelectedIndex = 0;
            MinCostBox.SelectedIndex = 0;

            var types = _db.ProductTypes.Select(t => t.ProductType).ToList();
            TypeFilterBox.ItemsSource = new[] { "Все типы" }.Concat(types).ToList();
            TypeFilterBox.SelectedIndex = 0;
        }

        private void ApplyFilters()
        {
            IEnumerable<ProductDisplay> query = _allProducts;

            // Поиск
            var search = SearchBox.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(searchLower) ||
                    p.Article.ToLower().Contains(searchLower));
            }

            // Фильтр по типу
            if (TypeFilterBox.SelectedIndex > 0 && TypeFilterBox.SelectedItem is string selectedType)
                query = query.Where(p => p.ProductType == selectedType);

            // Применяем сортировки (многоуровневая)
            query = ApplySorting(query);

            // Пагинация
            _totalPages = Math.Max(1, (int)Math.Ceiling(query.Count() / (double)PageSize));
            _currentPage = Math.Clamp(_currentPage, 1, _totalPages);

            ProductListView.ItemsSource = query
                .Skip((_currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            RenderPageButtons();
        }

        private IEnumerable<ProductDisplay> ApplySorting(IEnumerable<ProductDisplay> query)
        {
            string GetTag(ComboBox box) => (box.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

            string workshopTag = GetTag(NumWorkSortBox);
            string priceTag = GetTag(MinCostBox);
            string nameTag = GetTag(NameSortBox);

            IOrderedEnumerable<ProductDisplay> orderedQuery = null;

            // 1. Сортировка по цеху (первый уровень)
            if (!string.IsNullOrEmpty(workshopTag))
            {
                orderedQuery = workshopTag switch
                {
                    "WorkshopAsc" => query.OrderBy(p => p.WorkshopNumber),
                    "WorkshopDesc" => query.OrderByDescending(p => p.WorkshopNumber),
                    _ => null
                };
            }

            // 2. Сортировка по цене (второй уровень)
            if (!string.IsNullOrEmpty(priceTag))
            {
                if (orderedQuery != null)
                {
                    orderedQuery = priceTag switch
                    {
                        "PriceAsc" => orderedQuery.ThenBy(p => p.Cost),
                        "PriceDesc" => orderedQuery.ThenByDescending(p => p.Cost),
                        _ => orderedQuery
                    };
                }
                else
                {
                    orderedQuery = priceTag switch
                    {
                        "PriceAsc" => query.OrderBy(p => p.Cost),
                        "PriceDesc" => query.OrderByDescending(p => p.Cost),
                        _ => null
                    };
                }
            }

            // 3. Сортировка по имени (третий уровень)
            if (!string.IsNullOrEmpty(nameTag))
            {
                if (orderedQuery != null)
                {
                    orderedQuery = nameTag switch
                    {
                        "NameAsc" => orderedQuery.ThenBy(p => p.ProductName),
                        "NameDesc" => orderedQuery.ThenByDescending(p => p.ProductName),
                        _ => orderedQuery
                    };
                }
                else
                {
                    orderedQuery = nameTag switch
                    {
                        "NameAsc" => query.OrderBy(p => p.ProductName),
                        "NameDesc" => query.OrderByDescending(p => p.ProductName),
                        _ => null
                    };
                }
            }

            return orderedQuery ?? query;
        }

        private void RenderPageButtons()
        {
            PageButtonsPanel.Children.Clear();

            var prevBtn = new Button
            {
                Content = "Назад",
                IsEnabled = _currentPage > 1,
                Margin = new Thickness(5)
            };
            prevBtn.Click += (_, _) =>
            {
                _currentPage--;
                ApplyFilters();
            };
            PageButtonsPanel.Children.Add(prevBtn);

            for (int i = 1; i <= _totalPages; i++)
            {
                var page = i;
                var btn = new Button
                {
                    Content = i.ToString(),
                    Margin = new Thickness(5),
                    IsEnabled = _currentPage != i
                };
                btn.Click += (_, _) =>
                {
                    _currentPage = page;
                    ApplyFilters();
                };
                PageButtonsPanel.Children.Add(btn);
            }

            var nextBtn = new Button
            {
                Content = "Вперёд",
                IsEnabled = _currentPage < _totalPages,
                Margin = new Thickness(5)
            };
            nextBtn.Click += (_, _) =>
            {
                _currentPage++;
                ApplyFilters();
            };
            PageButtonsPanel.Children.Add(nextBtn);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void FilterChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private void ProductListView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = ProductListView.SelectedItems?.Count > 0;
            ChangePriceButton.IsVisible = hasSelection;
            IncreasePriceButton.IsVisible = hasSelection;
        }

        private async void ChangePriceButton_Click(object? sender, RoutedEventArgs e)
        {
            var selected = ProductListView.SelectedItems.Cast<ProductDisplay>().ToList();
            if (selected.Count == 0) return;

            var avg = selected.Average(p => p.MinAgentCost ?? 0);
            var dialog = new ChangePriceWindow(avg);
            var result = await dialog.ShowDialog<decimal?>(this);
            if (result.HasValue)
            {
                var productIds = selected.Select(p => p.ProductId).ToList();
                var productsToUpdate = _db.Products
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToList();

                foreach (var product in productsToUpdate)
                {
                    product.MinAgentCost = result.Value;
                }

                await _db.SaveChangesAsync();
                LoadData();
            }
        }

        private async void IncreasePriceButton_Click(object? sender, RoutedEventArgs e)
        {
            var selected = ProductListView.SelectedItems.Cast<ProductDisplay>().ToList();
            if (selected.Count == 0) return;

            var dialog = new ChangePriceWindow(0);
            var result = await dialog.ShowDialog<decimal?>(this);
            if (result.HasValue)
            {
                var productIds = selected.Select(p => p.ProductId).ToList();
                var productsToUpdate = _db.Products
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToList();

                foreach (var product in productsToUpdate)
                {
                    product.MinAgentCost = (product.MinAgentCost ?? 0) + result.Value;
                }

                await _db.SaveChangesAsync();
                LoadData();
            }
        }

        private class ProductDisplay
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public string ProductType { get; set; }
            public string Article { get; set; }
            public int WorkshopNumber { get; set; }
            public decimal? MinAgentCost { get; set; }
            public string ImagePath { get; set; }
            public DateOnly? LastSaleDate { get; set; }
            public bool IsStale { get; set; }
            public decimal Cost { get; set; }

            public string Price => (MinAgentCost ?? 0).ToString("0.##") + " ₽";
            public string LastSaleDateFormatted => LastSaleDate?.ToString("dd.MM.yyyy") ?? "Нет данных";

            public Bitmap Image
            {
                get
                {
                    try
                    {
                        var path = Path.Combine(AppContext.BaseDirectory, ImagePath);
                        return File.Exists(path) ? new Bitmap(path) : new Bitmap("Assets/picture.png");
                    }
                    catch
                    {
                        return new Bitmap("Assets/picture.png");
                    }
                }
            }
        }
    }
}
