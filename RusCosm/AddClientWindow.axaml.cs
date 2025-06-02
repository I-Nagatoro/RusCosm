using Avalonia.Controls;
using Avalonia.Interactivity;
using RusCosm.Model;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using MsBox.Avalonia;

namespace RusCosm;

public partial class AddClientWindow : Window
{
    private readonly User6Context _db = new();

    public AddClientWindow()
    {
        InitializeComponent();
        ClientTypeBox.ItemsSource = new[] { "Юридическое лицо", "Физическое лицо" };
    }

    private void ClientTypeBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var type = ClientTypeBox.SelectedItem?.ToString();
        LegalFields.IsVisible = type == "Юридическое лицо";
        IndividualFields.IsVisible = type == "Физическое лицо";
    }

    private async void SaveClient_Click(object? sender, RoutedEventArgs e)
    {
        string password;
        string clientCode;

        if (ClientTypeBox.SelectedItem?.ToString() == "Юридическое лицо")
        {
            password = PasswordBoxLegal.Text?.Trim();
            clientCode = ClientCodeBoxLegal.Text?.Trim();

            if (string.IsNullOrWhiteSpace(clientCode))
            {
                await ShowError("Поле ClientCode обязательно для заполнения");
                return;
            }

            if (clientCode.Length > 10)
            {
                await ShowError("ClientCode не должен превышать 10 символов");
                return;
            }

            if (_db.Legalentities.Any(c => c.ClientCode == clientCode) ||
                _db.Individuals.Any(c => c.ClientCode == clientCode))
            {
                await ShowError("ClientCode уже используется");
                return;
            }

            if (string.IsNullOrWhiteSpace(CompanyNameBox.Text) || CompanyNameBox.Text.Length > 100)
            {
                await ShowError("Название компании обязательно и не должно превышать 100 символов");
                return;
            }

            if (string.IsNullOrWhiteSpace(AddressBox.Text) || AddressBox.Text.Length > 200)
            {
                await ShowError("Адрес обязателен и не должен превышать 200 символов");
                return;
            }

            if (string.IsNullOrWhiteSpace(INNBox.Text) || INNBox.Text.Length != 12)
            {
                await ShowError("ИНН обязателен и должен содержать ровно 12 символов");
                return;
            }

            if (string.IsNullOrWhiteSpace(AccountBox.Text) || AccountBox.Text.Length > 20)
            {
                await ShowError("Номер счета обязателен и не должен превышать 20 символов");
                return;
            }

            if (string.IsNullOrWhiteSpace(BIKBox.Text) || BIKBox.Text.Length > 9)
            {
                await ShowError("БИК обязателен и не должен превышать 9 символов");
                return;
            }

            if (string.IsNullOrWhiteSpace(DirectorBox.Text) || DirectorBox.Text.Length > 100)
            {
                await ShowError("Имя директора обязательно и не должно превышать 100 символов");
                return;
            }

            if (string.IsNullOrWhiteSpace(ContactBox.Text) || ContactBox.Text.Length > 100)
            {
                await ShowError("Контактное лицо обязательно и не должно превышать 100 символов");
                return;
            }

            if (string.IsNullOrWhiteSpace(PhoneBox.Text) || PhoneBox.Text.Length > 50)
            {
                await ShowError("Телефон обязателен и не должен превышать 50 символов");
                return;
            }

            if (string.IsNullOrWhiteSpace(EmailBox.Text) || EmailBox.Text.Length > 100 || !IsValidEmail(EmailBox.Text))
            {
                await ShowError("Введите корректный email (максимум 100 символов)");
                return;
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6 || password.Length > 50)
            {
                await ShowError("Пароль обязателен и должен быть от 6 до 50 символов");
                return;
            }

            var entity = new Legalentity
            {
                ClientCode = clientCode,
                CompanyName = CompanyNameBox.Text.Trim(),
                Address = AddressBox.Text.Trim(),
                Inn = INNBox.Text.Trim(),
                AccountNumber = AccountBox.Text.Trim(),
                Bik = BIKBox.Text.Trim(),
                DirectorName = DirectorBox.Text.Trim(),
                ContactPerson = ContactBox.Text.Trim(),
                ContactPhone = PhoneBox.Text.Trim(),
                Email = EmailBox.Text.Trim(),
                Password = password
            };

            _db.Legalentities.Add(entity);
        }
        else
        {
            password = PasswordBoxInd.Text?.Trim();
            clientCode = ClientCodeBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(clientCode))
            {
                await ShowError("ClientCode обязателен для физического лица");
                return;
            }

            if (clientCode.Length > 10)
            {
                await ShowError("ClientCode не должен превышать 10 символов");
                return;
            }

            if (_db.Legalentities.Any(c => c.ClientCode == clientCode) ||
                _db.Individuals.Any(c => c.ClientCode == clientCode))
            {
                await ShowError("ClientCode уже используется");
                return;
            }

            if (string.IsNullOrWhiteSpace(FullNameBox.Text) || FullNameBox.Text.Length > 100)
            {
                await ShowError("ФИО обязательно и не должно превышать 100 символов");
                return;
            }

            if (string.IsNullOrWhiteSpace(PassportBox.Text) || PassportBox.Text.Length!=10)
            {
                await ShowError("Введите корректные паспортные данные");
                return;
            }

            if (BirthDatePicker.SelectedDate == null || BirthDatePicker.SelectedDate > DateTime.Today)
            {
                await ShowError("Выберите корректную дату рождения");
                return;
            }

            if (string.IsNullOrWhiteSpace(AddressIndBox.Text) || AddressIndBox.Text.Length > 200)
            {
                await ShowError("Адрес обязателен и не должен превышать 200 символов");
                return;
            }

            if (string.IsNullOrWhiteSpace(EmailIndBox.Text) || EmailIndBox.Text.Length > 100 ||
                !IsValidEmail(EmailIndBox.Text))
            {
                await ShowError("Введите корректный email (максимум 100 символов)");
                return;
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6 || password.Length > 50)
            {
                await ShowError("Пароль обязателен и должен быть от 6 до 50 символов");
                return;
            }

            var individual = new Individual
            {
                ClientCode = clientCode,
                FullName = FullNameBox.Text.Trim(),
                PassportDetails = PassportBox.Text.Trim(),
                BirthDate = DateOnly.FromDateTime(BirthDatePicker.SelectedDate.Value.Date),
                Address = AddressIndBox.Text.Trim(),
                Email = EmailIndBox.Text.Trim(),
                Password = password
            };

            _db.Individuals.Add(individual);
        }
    }


    private async System.Threading.Tasks.Task ShowError(string message)
    {
        var msg = MessageBoxManager.GetMessageBoxStandard("Ошибка", message);
        await msg.ShowAsync();
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            return Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
