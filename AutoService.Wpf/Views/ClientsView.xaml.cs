using System.Windows;
using System.Windows.Controls;
using AutoService.BLL;

namespace AutoService.Wpf.Views;

// Одна строка таблицы клиентов
public record ClientRow(int Id, string FullName, string Phone, string Cars);

public partial class ClientsView : UserControl
{
    private readonly AppServices _app = App.Services;

    public ClientsView()
    {
        InitializeComponent();
        RefreshData();
    }

    public void RefreshData()
    {
        ClientsGrid.ItemsSource = _app.Clients.GetAll()
            .Select(c => new ClientRow(
                c.Id, c.FullName, c.Phone,
                string.Join(", ", _app.Clients.GetCars(c.Id))))
            .ToList();
    }

    private void ClearCarFields()
    {
        BrandBox.Clear();
        ModelBox.Clear();
        PlateBox.Clear();
    }

    private void AddClient_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _app.Clients.AddWithCar(NameBox.Text, PhoneBox.Text, BrandBox.Text, ModelBox.Text, PlateBox.Text);
            NameBox.Clear();
            PhoneBox.Clear();
            ClearCarFields();
            RefreshData();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка ввода");
        }
    }

    private void AddCar_Click(object sender, RoutedEventArgs e)
    {
        if (ClientsGrid.SelectedItem is not ClientRow row)
        {
            MessageBox.Show("Выберите клиента в таблице.");
            return;
        }

        try
        {
            _app.Clients.AddCar(row.Id, BrandBox.Text, ModelBox.Text, PlateBox.Text);
            ClearCarFields();
            RefreshData();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка ввода");
        }
    }
}