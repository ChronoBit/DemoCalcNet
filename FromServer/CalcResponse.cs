using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using NetUtils.ToClient;

namespace DemoCalcNet.FromServer; 

public class CalcResponse : NetUtils.ToClient.CalcResponse {
    public override async Task Handle() {
        var w = MainWindow.Instance;
        w.IsSending = false;

        switch (Error) {
            case CalcError.Ok:
                await w.Dispatcher.InvokeAsync(() => {
                    w.Operations.Clear();
                    w.Operations.Add(Result.ToString(CultureInfo.CurrentCulture).Replace(',', '.'));
                    w.Refresh();
                });
                break;
            case CalcError.DivByZero:
                MessageBox.Show("Attempt to division by zero!", "Calculation error", MessageBoxButton.OK, MessageBoxImage.Error);
                break;
            case CalcError.InvalidInput:
                MessageBox.Show("Invalid input", "Calculation error", MessageBoxButton.OK, MessageBoxImage.Error);
                break;
        }
    }
}