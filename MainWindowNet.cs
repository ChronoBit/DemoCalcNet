using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DemoCalcNet.Net;
using NetUtils.ToServer;

namespace DemoCalcNet; 

public partial class MainWindow : Window {
    public ClientService Service { get; } = new(IPAddress.Parse("127.0.0.1"), 1000);

    private async Task NetTask() {
        await Service.Run(CancellationToken.None);
        await Task.Delay(1000);
        _ = Task.Run(NetTask);
    }

    public void InitNet() {
        Service.OnConnect += (s) => {
            IsConnected = true;
            if (IsSending) {
                _ = Service.Control?.Send(new CalcRequest {
                    Operations = Operations
                });
            }
        };
        Service.OnDisconnect += (s) => {
            IsConnected = false;
        };
        _ = NetTask();
    }
}