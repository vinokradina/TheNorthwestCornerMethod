using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TheNorthwestCornerMethod
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int rows, cols;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void CreateTable_Click(object sender, RoutedEventArgs e)
        {
            InputGrid.Children.Clear();
            InputGrid.RowDefinitions.Clear();
            InputGrid.ColumnDefinitions.Clear();

            if (!int.TryParse(SuppliersTextBox.Text, out rows) || !int.TryParse(ConsumersTextBox.Text, out cols))
            {
                MessageBox.Show("Введите корректные числа");
                return;
            }

            // Создание столбцов
            for (int c = 0; c <= cols; c++)
            {
                InputGrid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(90) 
                });
            }

            // Создание строк
            for (int r = 0; r <= rows; r++)
                InputGrid.RowDefinitions.Add(new RowDefinition());
            InputGrid.RowDefinitions.Add(new RowDefinition());

            // Создание ячеек стоимостей и запасов
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var tb = new TextBox
                    {
                        Margin = new Thickness(2),
                        Width = 70,
                        Height = 30,
                        TextAlignment = TextAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center
                    };
                    Grid.SetRow(tb, r);
                    Grid.SetColumn(tb, c);
                    InputGrid.Children.Add(tb);
                }

                var tbSupply = new TextBox
                {
                    Margin = new Thickness(2),
                    Width = 70,
                    Height = 30,
                    Background = Brushes.LightBlue,
                    TextAlignment = TextAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(tbSupply, r);
                Grid.SetColumn(tbSupply, cols);
                InputGrid.Children.Add(tbSupply);
            }

            // Создание ячеек потребностей
            for (int c = 0; c < cols; c++)
            {
                var tbDemand = new TextBox
                {
                    Margin = new Thickness(2),
                    Width = 70,
                    Height = 30,
                    Background = Brushes.LightBlue,
                    TextAlignment = TextAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(tbDemand, rows);
                Grid.SetColumn(tbDemand, c);
                InputGrid.Children.Add(tbDemand);
            }
        }

        private void Solve_Click(object sender, RoutedEventArgs e)
        {
            ResultTextBlock.Text = "";

            if (!ValidateInputCells())
            {
                MessageBox.Show("Проверьте, что все ячейки таблицы, запасов и потребностей заполнены корректными числами.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int[,] cost = GetCostMatrix();
            int[] supply = GetSupplyArray();
            int[] demand = GetDemandArray();

            int totalSupply = supply.Sum();
            int totalDemand = demand.Sum();

            string balanceMessage = "";
            bool balanced = true;
            
            // Балансировка задачи
            if (totalSupply > totalDemand)
            {
                balanced = false;
                balanceMessage = $"Задача не сбалансирована. Добавлен фиктивный потребитель с потребностью {totalSupply - totalDemand}.\n";
                int[,] newCost = new int[rows, cols + 1];
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                        newCost[r, c] = cost[r, c];
                    newCost[r, cols] = 0;
                }
                int[] newDemand = new int[demand.Length + 1];
                demand.CopyTo(newDemand, 0);
                newDemand[demand.Length] = totalSupply - totalDemand;

                cost = newCost;
                demand = newDemand;
                cols += 1;
            }
            else if (totalDemand > totalSupply)
            {
                balanced = false;
                balanceMessage = $"Задача не сбалансирована. Добавлен фиктивный поставщик с запасом {totalDemand - totalSupply}.\n";
                int[,] newCost = new int[rows + 1, cols];
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        newCost[r, c] = cost[r, c];
                for (int c = 0; c < cols; c++)
                    newCost[rows, c] = 0;

                int[] newSupply = new int[supply.Length + 1];
                supply.CopyTo(newSupply, 0);
                newSupply[supply.Length] = totalDemand - totalSupply;

                cost = newCost;
                supply = newSupply;
                rows += 1;
            }

            int[] originalSupply = (int[])supply.Clone();
            int[] originalDemand = (int[])demand.Clone();
            int[,] result = SolveNorthWestCorner(cost, supply, demand);

            ResultTextBlock.Text = balanceMessage + GenerateTextTable(result, cost, originalSupply, originalDemand);
        }

        // Отображение результатов в виде таблицы
        private string GenerateTextTable(int[,] result, int[,] cost, int[] supply, int[] demand)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\t");
            for (int c = 0; c < demand.Length; c++)
                sb.Append($"B{c + 1}\t");
            sb.AppendLine("Запасы");

            for (int r = 0; r < supply.Length; r++)
            {
                sb.Append($"A{r + 1}\t");
                for (int c = 0; c < demand.Length; c++)
                {
                    sb.Append($"{result[r, c]}\t");
                }
                sb.AppendLine(supply[r].ToString());
            }

            sb.Append("Потр\t");
            for (int c = 0; c < demand.Length; c++)
                sb.Append($"{demand[c]}\t");
            sb.AppendLine();

            int totalCost = 0;
            for (int r = 0; r < result.GetLength(0); r++)
                for (int c = 0; c < result.GetLength(1); c++)
                    totalCost += result[r, c] * cost[r, c];

            sb.AppendLine($"\nОбщие затраты: {totalCost}");

            return sb.ToString();
        }

        // Получение TextBox по координатам
        private TextBox GetTextBox(int row, int col)
        {
            foreach (UIElement element in InputGrid.Children)
            {
                if (Grid.GetRow(element) == row && Grid.GetColumn(element) == col && element is TextBox)
                    return (TextBox)element;
            }
            return null;
        }

        // Реализация метода северо-западного угла
        private int[,] SolveNorthWestCorner(int[,] cost, int[] supply, int[] demand)
        {
            int m = supply.Length;
            int n = demand.Length;
            int[,] result = new int[m, n];
            int i = 0, j = 0;

            while (i < m && j < n)
            {
                int qty = Math.Min(supply[i], demand[j]);
                result[i, j] = qty;
                supply[i] -= qty;
                demand[j] -= qty;

                if (supply[i] == 0) i++;
                else j++;
            }

            return result;
        }

        // Сохранение результата в файл
        private void SaveToFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog { Filter = "Text files (*.txt)|*.txt" };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, ResultTextBlock.Text);
            }
        }

        // Проверка всех ячеек ввода
        private bool ValidateInputCells()
        {
            if (!int.TryParse(SuppliersTextBox.Text, out rows) || !int.TryParse(ConsumersTextBox.Text, out cols))
            {
                MessageBox.Show("Некорректное количество поставщиков или потребителей.");
                return false;
            }

            // Проверка ячеек стоимости
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var tb = GetTextBox(r, c);
                    if (tb == null || string.IsNullOrWhiteSpace(tb.Text) || !int.TryParse(tb.Text, out _))
                        return false;
                }
            }

            // Проверка запасов
            for (int r = 0; r < rows; r++)
            {
                var tb = GetTextBox(r, cols);
                if (tb == null || string.IsNullOrWhiteSpace(tb.Text) || !int.TryParse(tb.Text, out _))
                    return false;
            }

            // Проверка потребностей
            for (int c = 0; c < cols; c++)
            {
                var tb = GetTextBox(rows, c);
                if (tb == null || string.IsNullOrWhiteSpace(tb.Text) || !int.TryParse(tb.Text, out _))
                    return false;
            }

            return true;
        }

        // Получение матрицы стоимостей
        private int[,] GetCostMatrix()
        {
            if (!int.TryParse(SuppliersTextBox.Text, out rows) || !int.TryParse(ConsumersTextBox.Text, out cols))
            {
                MessageBox.Show("Некорректное количество поставщиков или потребителей.");
                return null;
            }

            int[,] cost = new int[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var tb = GetTextBox(r, c);
                    if (tb != null)
                        cost[r, c] = int.Parse(tb.Text);
                }
            }
            return cost;
        }

        // Получение массива запасов
        private int[] GetSupplyArray()
        {
            if (!int.TryParse(SuppliersTextBox.Text, out rows) || !int.TryParse(ConsumersTextBox.Text, out cols))
            {
                MessageBox.Show("Некорректное количество поставщиков или потребителей.");
                return null;
            }

            int[] supply = new int[rows];
            for (int r = 0; r < rows; r++)
            {
                var supplyBox = GetTextBox(r, cols);
                if (supplyBox != null)
                    supply[r] = int.Parse(supplyBox.Text);
            }
            return supply;
        }

        // Получение массива потребностей
        private int[] GetDemandArray()
        {
            if (!int.TryParse(SuppliersTextBox.Text, out rows) || !int.TryParse(ConsumersTextBox.Text, out cols))
            {
                MessageBox.Show("Некорректное количество поставщиков или потребителей.");
                return null;
            }

            int[] demand = new int[cols];
            for (int c = 0; c < cols; c++)
            {
                var demandBox = GetTextBox(rows, c);
                if (demandBox != null)
                    demand[c] = int.Parse(demandBox.Text);
            }
            return demand;
        }

        private void LoadFromFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt" };
            if (dlg.ShowDialog() == true)
            {
                string[] lines = File.ReadAllLines(dlg.FileName);

                if (lines.Length < 2)
                {
                    MessageBox.Show("Файл не содержит достаточно данных.");
                    return;
                }

                int supplierCount = lines.Length - 1;
                int consumerCount = lines[0].Split('\t').Length - 1;

                SuppliersTextBox.Text = supplierCount.ToString();
                ConsumersTextBox.Text = consumerCount.ToString();
                CreateTable_Click(null, null);

                for (int r = 0; r <= supplierCount; r++)
                {
                    var values = lines[r].Split('\t');
                    for (int c = 0; c <= consumerCount; c++)
                    {
                        var tb = GetTextBox(r, c);
                        if (tb != null && c < values.Length)
                            tb.Text = values[c];
                    }
                }
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (UIElement element in InputGrid.Children)
            {
                if (element is TextBox tb)
                    tb.Text = "";
            }

            ResultTextBlock.Text = "";
            rows = 0;
            cols = 0;
        }

    }
}
