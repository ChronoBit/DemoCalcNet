using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using NetUtils;
using NetUtils.ToServer;

namespace DemoCalcNet.Net; 

public class ClientService : IDisposable {
    private const int BufferSize = 65536;

    private readonly IPAddress _ip;
    private readonly int _port;

    public TcpClient Client { get; private set; }
    public NetControl? Control { get; private set; }

    public delegate void SimpleEvent(ClientService sender);
    public event SimpleEvent? OnConnect;
    public event SimpleEvent? OnDisconnect;

    public ClientService(IPAddress? ip, int port = 1111) {
        ip ??= IPAddress.Any;
        _ip = ip;
        _port = port;
        Client = new TcpClient();
    }

    public async Task Run(CancellationToken token) {
        try {
            await Client.ConnectAsync(_ip, _port, token);

            await using var stream = Client.GetStream();
            var headerBuffer = new byte[2];

            Control = new NetControl(stream);
            OnConnect?.Invoke(this);

            async Task AlivePusher(TcpClient client, NetControl control) {
                await Task.Delay(1000, token);
                try {
                    if (client.Connected) {
                        await control.Send(new AliveTick());
                        _ = AlivePusher(client, control);
                    }
                } catch (Exception) {
                    // ignored
                }
            }
            _ = AlivePusher(Client, Control);

            while (await stream.ReadAsync(headerBuffer, 0, headerBuffer.Length, token) > 0) {
                var packetHead = BitConverter.ToInt16(headerBuffer, 0);
                if (packetHead != 0x6529) {
                    // Invalid packet
                    break;
                }

                var lengthBuffer = new byte[4];
                if (await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length, token) == 0) {
                    // Server disconnected
                    break;
                }

                var packetLength = BitConverter.ToInt32(lengthBuffer, 0);
                if (packetLength > 64 * 1024 * 1024) break; // Too large packet

                var dataBuffer = new byte[packetLength];

                var bytesReadTotal = 0;
                while (bytesReadTotal < packetLength) {
                    var bytesToRead = Math.Min(dataBuffer.Length - bytesReadTotal, BufferSize);
                    var bytesReadPartial = await stream.ReadAsync(dataBuffer, bytesReadTotal, bytesToRead, token);
                    if (bytesReadPartial == 0) {
                        // Server disconnected
                        break;
                    }

                    bytesReadTotal += bytesReadPartial;
                }

                if (bytesReadTotal != packetLength) continue; // Invalid total data

                // Process data
                await Control.ParseData(dataBuffer);
            }
        } catch (OperationCanceledException) {
            // Ignore cancellation
        } catch (Exception) {
            // Failed
        } finally {
            OnDisconnect?.Invoke(this);
            Client.Dispose();
            Control?.Dispose();
            Client = new TcpClient();
        }
    }

    public void Dispose() {
        Client.Dispose();
    }
}