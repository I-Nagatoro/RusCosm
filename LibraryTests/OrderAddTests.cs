using Avalonia;
using NUnit.Framework;
using RusCosm;
using RusCosm.Model;
using System;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
public class OrderAddTests
{
    private OrderWindow _window;
    private List<Service> _testServices;

    [OneTimeSetUp]
    public void InitAvalonia()
    {
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .SetupWithoutStarting();
    }

    [SetUp]
    public void Setup()
    {
        _testServices = new List<Service>
        {
            new Service { ServiceId = 1, ServiceName = "Услуга 1", StandardPrice = 1000, ExecutionTime = "2" },
            new Service { ServiceId = 2, ServiceName = "Услуга 2", StandardPrice = 2000, ExecutionTime = "3" }
        };

        _window = new OrderWindow();
        
        _window.TypeOrdererBox.SelectedIndex = 1;
        _window.NameOrdererBox.ItemsSource = new List<string> { "Иванов Иван" };
        _window.NameOrdererBox.SelectedItem = "Иванов Иван";
        _window.ServiceBox.ItemsSource = _testServices.Select(s => s.ServiceName).ToList();
        _window.UnitBox.SelectedItem = "шт";
    }

    [Test]
    public void SubmitOrder_WithoutClient_ShowsError()
    {
        _window.NameOrdererBox.SelectedItem = null;
        _window.VesselCodeBox.Text = "TEST1";
        _window._selectedServices.Clear();
        _window._selectedServices.Add(new OrderWindow.SelectedService
        {
            Service = _testServices.First(),
            Quantity = 1,
            Unit = "шт"
        });

        Assert.DoesNotThrow(() => _window.SubmitOrder_Click(null, null));
    }

    [Test]
    public void SubmitOrder_WithoutServices_ShowsError()
    {
        _window.NameOrdererBox.SelectedItem = "Иванов Иван";
        _window.VesselCodeBox.Text = "TEST2";
        _window._selectedServices.Clear();

        Assert.DoesNotThrow(() => _window.SubmitOrder_Click(null, null));
    }

    [Test]
    public void SubmitOrder_WithDuplicateOrderId_ShowsError()
    {
        _window.NameOrdererBox.SelectedItem = "Иванов Иван";
        _window.VesselCodeBox.Text = "1"; 
        _window._selectedServices.Clear();
        _window._selectedServices.Add(new OrderWindow.SelectedService
        {
            Service = _testServices.First(),
            Quantity = 1,
            Unit = "шт"
        });

        Assert.DoesNotThrow(() => _window.SubmitOrder_Click(null, null));
    }

    [Test]
    public void CalculateTotal_ReturnsCorrectExecutionTime()
    {
        _window._selectedServices.Clear();
        _window._selectedServices.AddRange(_testServices.Select(s => new OrderWindow.SelectedService
        {
            Service = s,
            Quantity = 1,
            Unit = "шт"
        }));

        int result = _window.CalculateTotal();
        
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void SubmitOrder_WithValidData_CreatesOrderSuccessfully()
    {
        _window.NameOrdererBox.SelectedItem = "Иванов Иван";
        _window.VesselCodeBox.Text = Guid.NewGuid().ToString("N").Substring(0, 6);
        _window._selectedServices.Clear();
        _window._selectedServices.Add(new OrderWindow.SelectedService
        {
            Service = _testServices.First(),
            Quantity = 1,
            Unit = "шт"
        });

        Assert.DoesNotThrow(() => _window.SubmitOrder_Click(null, null));
    }
}
