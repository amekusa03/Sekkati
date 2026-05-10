using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Sekkati;

internal static class SingleInstance
{
    private const string PipeName = "sekkati-ipc";

    // 既存インスタンスに "show" を送る。送れた場合 true（呼び出し元はすぐ終了すること）
    public static bool TrySendShow()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(300);
            using var writer = new StreamWriter(client);
            writer.WriteLine("show");
            return true;
        }
        catch
        {
            return false;
        }
    }

    // バックグラウンドで IPC サーバーを起動し、"show" を受け取ったら onShow を呼ぶ
    public static void StartListening(Action onShow, CancellationToken token)
    {
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        PipeName, PipeDirection.In, 1,
                        PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(token);

                    using var reader = new StreamReader(server);
                    var line = await reader.ReadLineAsync(token);
                    if (line == "show")
                        onShow();
                }
                catch (OperationCanceledException) { break; }
                catch { /* パイプエラーは無視してリトライ */ }
            }
        }, token);
    }
}
