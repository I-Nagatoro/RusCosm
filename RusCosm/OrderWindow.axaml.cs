using Avalonia.Controls;
using Avalonia.Interactivity;
using RusCosm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using MsBox.Avalonia;

namespace RusCosm
{
    public partial class OrderWindow : Window
    {
        private readonly User6Context _db;
        public readonly List<SelectedService> _selectedServices = new();
        public OrderWindow() : this(new User6Context()) {}
        public OrderWindow(User6Context dbContext)
        {
            _db = dbContext;
            InitializeComponent();
            InitializeForm();
        }

        private void InitializeForm()
        {
            TypeOrdererBox.ItemsSource = new[] { "Юридическое лицо", "Физическое лицо" };
            TypeOrdererBox.SelectedIndex = 0;

            ServiceBox.ItemsSource = _db.Services.Select(s => s.ServiceName).ToList();
            UnitBox.ItemsSource = new[] { "шт", "г", "мл", "ч" };

            int nextOrderId = (_db.Orders.Max(o => (int?)o.OrderId) ?? 0) + 1;

            VesselCodeBox.ItemsSource = new List<string> { nextOrderId.ToString() };
            VesselCodeBox.Text = nextOrderId.ToString();
        }

        private void TypeOrdererBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var selectedType = TypeOrdererBox.SelectedItem?.ToString();
            if (selectedType == "Юридическое лицо")
                NameOrdererBox.ItemsSource = _db.Legalentities.Select(c => c.CompanyName).ToList();
            else
                NameOrdererBox.ItemsSource = _db.Individuals.Select(c => c.FullName).ToList();
        }

        private void AddService_Click(object? sender, RoutedEventArgs e)
        {
            var serviceName = ServiceBox.SelectedItem?.ToString();
            if (!decimal.TryParse(QuantityBox.Text, out var qty) || qty <= 0)
            {
                ShowMessage("Ошибка", "Некорректное количество");
                return;
            }

            var unit = UnitBox.SelectedItem?.ToString();
            var service = _db.Services.FirstOrDefault(s => s.ServiceName == serviceName);
            if (service == null) return;

            var selected = new SelectedService
            {
                Service = service,
                Quantity = qty,
                Unit = unit
            };
            
            _selectedServices.Add(selected);
            var time = CalculateTotal();
            var cost = CalculateCost();
            TimeForOrder.Text = "Время выполнения заказа: "+time.ToString();
            CostForOrder.Text = "Общая стоимость заказа: "+cost.ToString();
            RefreshServiceList();
        }

        private void RefreshServiceList()
        {
            ServiceListBox.ItemsSource = _selectedServices.Select(s =>
                $"{s.Service.ServiceName} — {s.Quantity} {s.Unit} = {s.Cost} руб.").ToList();
        }

        public int CalculateTotal()
        {
            int totalExecutionTime = 0;
            foreach (var sel in _selectedServices)
            {
                totalExecutionTime += (int)(sel.Quantity * int.Parse(sel.Service.ExecutionTime));
            }
            return totalExecutionTime;
        }

        public int CalculateCost()
        {
            int cost = 0;
            foreach (var service in _selectedServices)
            {
                cost+=(int)(service.Cost);
            }
            return cost;
        }

        public async void SubmitOrder_Click(object? sender, RoutedEventArgs e)
        {
            if (NameOrdererBox.SelectedItem == null)
            {
                await ShowMessage("Ошибка", "Выберите заказчика");
                return;
            }

            if (!int.TryParse(VesselCodeBox.Text, out int enteredOrderId))
            {
                await ShowMessage("Ошибка", "Номер заказа должен быть числом");
                return;
            }

            if (_db.Orders.Any(o => o.OrderId == enteredOrderId))
            {
                await ShowMessage("Ошибка", "Номер заказа уже существует");
                return;
            }

            if (_selectedServices.Count == 0)
            {
                await ShowMessage("Ошибка", "Добавьте хотя бы одну услугу");
                return;
            }

            var employee = _db.Employees.FirstOrDefault();
            if (employee == null)
            {
                await ShowMessage("Ошибка", "Нет доступных сотрудников для назначения заказа");
                return;
            }

            string clientCode;
            var selectedType = TypeOrdererBox.SelectedItem?.ToString();
            if (selectedType == "Юридическое лицо")
            {
                var company = _db.Legalentities.FirstOrDefault(c => c.CompanyName == NameOrdererBox.SelectedItem.ToString());
                if (company == null)
                {
                    await ShowMessage("Ошибка", "Заказчик не найден");
                    return;
                }
                clientCode = company.ClientCode;
            }
            else
            {
                var individual = _db.Individuals.FirstOrDefault(c => c.FullName == NameOrdererBox.SelectedItem.ToString());
                if (individual == null)
                {
                    await ShowMessage("Ошибка", "Заказчик не найден");
                    return;
                }
                clientCode = individual.ClientCode;
            }

            string orderNumber = $"{clientCode}/{DateTime.Now:dd.MM.yyyy}";
            int totalExecutionTime = CalculateTotal();

            var newOrder = new Order
            {
                OrderId = enteredOrderId,
                OrderNumber = orderNumber,
                CreationDate = DateOnly.FromDateTime(DateTime.Now),
                ClientCode = clientCode,
                EmployeeId = employee.EmployeeId,
                Status = "Новый",
                ExecutionTime = totalExecutionTime.ToString()
            };

            _db.Orders.Add(newOrder);
            _db.SaveChanges();

            var groupedServices = _selectedServices
                .GroupBy(s => s.Service.ServiceId)
                .Select(g => g.First());

            foreach (var sel in groupedServices)
            {
                _db.Orderservices.Add(new Orderservice
                {
                    OrderId = newOrder.OrderId,
                    ServiceId = sel.Service.ServiceId
                });
            }

            _db.SaveChanges();

            await ShowMessage("Информация", "Заказ успешно создан");
            Close();
        }

        private async System.Threading.Tasks.Task ShowMessage(string title, string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, message);
            await box.ShowAsync();
        }

        private void AddClient_Click(object? sender, RoutedEventArgs e)
        {
            var addWindow = new AddClientWindow();
            addWindow.ShowDialog(this);
        }

        public class SelectedService
        {
            public Service Service { get; set; }
            public decimal Quantity { get; set; }
            public string Unit { get; set; }
            public decimal Cost => Quantity * Service.StandardPrice;
        }
    }
}
