namespace AutoService.UI;

public static class GridHelper
{
    public static void Configure(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.RowHeadersVisible = false;   // убирает пустую колонку слева
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = SystemColors.Window;
        grid.BorderStyle = BorderStyle.None;
    }

    // Колонка фиксированной ширины: для номеров и коротких значений
    public static void FixedWidth(DataGridViewColumn column, int width)
    {
        column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        column.Width = width;
    }
}