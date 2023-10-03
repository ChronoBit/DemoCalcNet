using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NetUtils.ToServer;
using PropertyChanged;

namespace DemoCalcNet;

[AddINotifyPropertyChangedInterface]
public partial class MainWindow : Window {
    public static MainWindow Instance { get; private set; } = null!;
    public readonly List<string> Operations = new();

    public MainWindow() {
        Instance = this;
        InitializeComponent();
        DataContext = this;
        InitNet();
    }

    [AlsoNotifyFor(nameof(ConLabelV), nameof(NotConLabelV))]
    public bool IsConnected { get; set; }
    public Visibility ConLabelV => IsConnected ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NotConLabelV => !IsConnected ? Visibility.Visible : Visibility.Collapsed;

    [AlsoNotifyFor(nameof(IsEqualEnabled))]
    public bool IsSending { get; set; }
    public Visibility SendV => IsSending ? Visibility.Visible : Visibility.Collapsed;

    public bool IsEqualEnabled => !IsSending;

    public string Display { get; set; } = "0";

    public void Refresh() {
        if (Operations.Count == 0) {
            Display = "0";
            return;
        }

        Display = string.Join("", Operations);
        resultBox.ScrollToHorizontalOffset(resultBox.ExtentWidth);
    }

    private void PushNumber(string number) {
        if (IsSending) return;

        if (Operations.Count > 0) {
            switch (Operations[^1][0]) {
                case '+':
                case '-':
                case '*':
                case '/':
                    if (number == ".") return;
                    Operations.Add(number);
                    break;
                default:
                    var last = Operations.Last();
                    if (last.Contains('.') && number == ".") return;
                    if (last.StartsWith('0') && number == "0") return;
                    if (last == "0" && number != ".")
                        Operations[^1] = number;
                    else
                        Operations[^1] += number;

                    break;
            }
        } else {
            if (number == ".") return;
            Operations.Add(number);
        }

        Refresh();
    }

    private void PushOperator(string content) {
        if (Operations.Count == 0) return;
        if (IsSending) return;

        switch (Operations[^1][0]) {
            case '+':
            case '-':
            case '*':
            case '/':
                return;
        }

        Operations.Add(content);

        Refresh();
    }

    private void NumberButton_Click(object sender, RoutedEventArgs e) {
        var number = ((Button)sender).Content.ToString()!;
        PushNumber(number);
    }

    private void OperatorButton_Click(object sender, RoutedEventArgs e) {
        var content = ((Button)sender).Content.ToString()!;
        PushOperator(content);
    }

    private void EqualOperations() {
        if (Operations.Count == 0) return;

        IsSending = true;
        if (!IsConnected) return;

        _ = Service.Control?.Send(new CalcRequest {
            Operations = Operations
        });
    }

    private void EqualButton_Click(object sender, RoutedEventArgs e) {
        EqualOperations();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e) {
        if (IsSending) return;
        Display = "0";
        Operations.Clear();
    }

    private void DelLast() {
        if (Operations.Count == 0) return;
        if (IsSending) return;

        var last = Operations.Last();
        last = last[..^1];
        if (string.IsNullOrEmpty(last))
            Operations.RemoveAt(Operations.Count - 1);
        else
            Operations[^1] = last;

        Refresh();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e) {
        DelLast();
    }

    private void MainWindow_OnKeyDown(object sender, KeyEventArgs e) {
        switch (e.Key) {
            case Key.D0:
            case Key.NumPad0:
                PushNumber("0");
                break;
            case Key.D1:
            case Key.NumPad1:
                PushNumber("1");
                break;
            case Key.D2:
            case Key.NumPad2:
                PushNumber("2");
                break;
            case Key.D3:
            case Key.NumPad3:
                PushNumber("3");
                break;
            case Key.D4:
            case Key.NumPad4:
                PushNumber("4");
                break;
            case Key.D5:
            case Key.NumPad5:
                PushNumber("5");
                break;
            case Key.D6:
            case Key.NumPad6:
                PushNumber("6");
                break;
            case Key.D7:
            case Key.NumPad7:
                PushNumber("7");
                break;
            case Key.D8:
            case Key.NumPad8:
                PushNumber("8");
                break;
            case Key.D9:
            case Key.NumPad9:
                PushNumber("9");
                break;

            case Key.Add:
                PushOperator("+");
                break;
            case Key.Subtract:
            case Key.OemMinus:
                PushOperator("-");
                break;
            case Key.Multiply:
                PushOperator("*");
                break;
            case Key.Divide:
                PushOperator("/");
                break;

            case Key.OemPlus:
                if (!Keyboard.IsKeyDown(Key.LeftShift))
                    EqualOperations();
                else
                    PushOperator("+");
                break;

            case Key.Back:
                DelLast();
                break;
            case Key.Enter:
                EqualOperations();
                break;
        }
    }
}