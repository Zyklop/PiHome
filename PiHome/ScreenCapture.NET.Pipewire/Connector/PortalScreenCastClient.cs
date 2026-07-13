using Microsoft.Win32.SafeHandles;
using Tmds.DBus.Protocol;

namespace ScreenCapture.NET.Pipewire.Connector;

public sealed class PortalScreenCastClient : IDisposable
{
    private const string PortalService = "org.freedesktop.portal.Desktop";
    private const string PortalPath = "/org/freedesktop/portal/desktop";
    private const string ScreenCastInterface = "org.freedesktop.portal.ScreenCast";
    private const string RequestInterface = "org.freedesktop.portal.Request";
    
    private DBusConnection? connection;

    public async Task<PortalCaptureSession> StartSessionAsync(CancellationToken cancellationToken)
    {
        EnsureLinuxWayland();

        connection = new DBusConnection(new DBusConnectionOptions(DBusAddress.Session!)
        {
            AutoConnect = false
        });

        await connection.ConnectAsync().ConfigureAwait(false);

        var sessionHandleToken = $"s{Guid.NewGuid():N}";
        var createHandleToken = $"r{Guid.NewGuid():N}";
        var createRequestPath = BuildRequestPath(connection.UniqueName, createHandleToken);

        var createResponseTask = WaitForRequestResponseAsync(connection, createRequestPath, cancellationToken);

        var returnedCreateRequestPath = await CreateSessionAsync(
            connection,
            new Dictionary<string, VariantValue>
            {
                ["session_handle_token"] = sessionHandleToken,
                ["handle_token"] = createHandleToken
            }).ConfigureAwait(false);

        Console.WriteLine($"Create expected request path: {createRequestPath}");
        Console.WriteLine($"Create returned request path: {returnedCreateRequestPath}");

        var createResponse = await createResponseTask.ConfigureAwait(false);

        if (createResponse.Response != 0)
            throw new InvalidOperationException($"Portal CreateSession failed with response code {createResponse.Response}.");

        var sessionHandle = GetRequiredObjectPathString(createResponse.Results, "session_handle");

        var selectHandleToken = $"r{Guid.NewGuid():N}";
        var selectRequestPath = BuildRequestPath(connection.UniqueName, selectHandleToken);

        var selectResponseTask = WaitForRequestResponseAsync(connection, selectRequestPath, cancellationToken);

        var returnedSelectRequestPath = await SelectSourcesAsync(
            connection,
            sessionHandle,
            new Dictionary<string, VariantValue>
            {
                ["handle_token"] = selectHandleToken,
                ["types"] = ((uint)1),
                ["multiple"] = false
            }).ConfigureAwait(false);

        Console.WriteLine($"Select expected request path: {selectRequestPath}");
        Console.WriteLine($"Select returned request path: {returnedSelectRequestPath}");

        var selectResponse = await selectResponseTask.ConfigureAwait(false);

        if (selectResponse.Response != 0)
            throw new InvalidOperationException($"Portal SelectSources failed with response code {selectResponse.Response}.");

        var startHandleToken = $"r{Guid.NewGuid():N}";
        var startRequestPath = BuildRequestPath(connection.UniqueName, startHandleToken);

        var startResponseTask = WaitForRequestResponseAsync(connection, startRequestPath, cancellationToken);

        var returnedStartRequestPath = await StartAsync(
            connection,
            sessionHandle,
            string.Empty,
            new Dictionary<string, VariantValue>
            {
                ["handle_token"] = startHandleToken
            }).ConfigureAwait(false);

        Console.WriteLine($"Start expected request path: {startRequestPath}");
        Console.WriteLine($"Start returned request path: {returnedStartRequestPath}");

        var startResponse = await startResponseTask.ConfigureAwait(false);

        if (startResponse.Response != 0)
            throw new InvalidOperationException($"Portal Start failed with response code {startResponse.Response}.");

        var streamNode = ParseStreamNode(startResponse.Results)
                         ?? throw new InvalidOperationException("Could not parse PipeWire stream node from portal response.");

        var pipewireFd = await OpenPipeWireRemoteAsync(sessionHandle, cancellationToken).ConfigureAwait(false);

        return new PortalCaptureSession(sessionHandle, streamNode, pipewireFd);
    }
    
    public async Task<int> OpenPipeWireRemoteAsync(string sessionHandle, CancellationToken cancellationToken)
    {
        MessageBuffer message;
        using (var writer = connection!.GetMessageWriter())
        {
            writer.WriteMethodCallHeader(
                destination: PortalService,
                path: PortalPath,
                @interface: ScreenCastInterface,
                member: "OpenPipeWireRemote",
                signature: "oa{sv}");

            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(new Dictionary<string, VariantValue>());
            message = writer.CreateMessage();
        }

        return await connection.CallMethodAsync(
            message,
            static (Message m, object? _) =>
            {
                var reader = m.GetBodyReader();
                var handle = reader.ReadHandle<SafeFileHandle>();
                var fd = handle.DangerousGetHandle().ToInt32();
                // Detach so the SafeFileHandle finalizer won't close the fd.
                // pw_context_connect_fd takes ownership and closes it on disconnect.
                handle.SetHandleAsInvalid();
                return fd;
            }).ConfigureAwait(false);
    }

    private static ObjectPath BuildRequestPath(string? uniqueName, string handleToken)
    {
        if (string.IsNullOrWhiteSpace(uniqueName))
            throw new InvalidOperationException("DBus unique name is not available.");

        var sender = uniqueName.TrimStart(':').Replace('.', '_');
        return new ObjectPath($"/org/freedesktop/portal/desktop/request/{sender}/{handleToken}");
    }

    private static Task<string> CreateSessionAsync(
        DBusConnection connection,
        Dictionary<string, VariantValue> options)
    {
        MessageBuffer message;
        using (var writer = connection.GetMessageWriter())
        {
            writer.WriteMethodCallHeader(
                destination: PortalService,
                path: PortalPath,
                @interface: ScreenCastInterface,
                member: "CreateSession",
                signature: "a{sv}");

            writer.WriteDictionary(options);
            message = writer.CreateMessage();
        }

        return connection.CallMethodAsync(
            message,
            static (Message m, object? _) => m.GetBodyReader().ReadObjectPathAsString());
    }

    private static Task<string> SelectSourcesAsync(
        DBusConnection connection,
        string sessionHandle,
        Dictionary<string, VariantValue> options)
    {
        MessageBuffer message;
        using (var writer = connection.GetMessageWriter())
        {
            writer.WriteMethodCallHeader(
                destination: PortalService,
                path: PortalPath,
                @interface: ScreenCastInterface,
                member: "SelectSources",
                signature: "oa{sv}");

            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            message = writer.CreateMessage();
        }

        return connection.CallMethodAsync(
            message,
            static (Message m, object? _) => m.GetBodyReader().ReadObjectPathAsString());
    }

    private static Task<string> StartAsync(
        DBusConnection connection,
        string sessionHandle,
        string parentWindow,
        Dictionary<string, VariantValue> options)
    {
        MessageBuffer message;
        using (var writer = connection.GetMessageWriter())
        {
            writer.WriteMethodCallHeader(
                destination: PortalService,
                path: PortalPath,
                @interface: ScreenCastInterface,
                member: "Start",
                signature: "osa{sv}");

            writer.WriteObjectPath(sessionHandle);
            writer.WriteString(parentWindow);
            writer.WriteDictionary(options);
            message = writer.CreateMessage();
        }

        return connection.CallMethodAsync(
            message,
            static (Message m, object? _) => m.GetBodyReader().ReadObjectPathAsString());
    }
    
    private static async Task<PortalResponse> WaitForRequestResponseAsync(
        DBusConnection connection,
        ObjectPath requestPath,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<PortalResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var ctr = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        using var subscription = await connection.WatchSignalAsync(
            sender: PortalService,
            path: requestPath.ToString(),
            @interface: RequestInterface,
            signal: "Response",
            reader: static (Message m, object? _) => ReadPortalResponse(m),
            handler: notification =>
            {
                if (notification.IsCompletion)
                {
                    tcs.TrySetException(notification.Exception);
                    return;
                }

                if (notification.HasValue)
                {
                    tcs.TrySetResult(notification.Value);
                }
            },
            flags: ObserverFlags.EmitOnConnectionFailed | ObserverFlags.EmitOnConnectionClosed,
            emitOnCapturedContext: false
        ).ConfigureAwait(false);

        return await tcs.Task.ConfigureAwait(false);
    }

    private static PortalResponse ReadPortalResponse(Message message)
    {
        var reader = message.GetBodyReader();
        var response = reader.ReadUInt32();
        var results = ReadVariantDictionary(ref reader);
        return new PortalResponse(response, results);
    }

    private static Dictionary<string, VariantValue> ReadVariantDictionary(ref Reader reader)
    {
        var results = new Dictionary<string, VariantValue>(StringComparer.Ordinal);
        var arrayEnd = reader.ReadArrayStart(DBusType.DictEntry);

        while (reader.HasNext(arrayEnd))
        {
            var key = reader.ReadString();
            var value = reader.ReadVariantValue();
            results[key] = value;
        }

        return results;
    }

    private static string GetRequiredObjectPathString(
        IReadOnlyDictionary<string, VariantValue> results,
        string key)
    {
        if (!results.TryGetValue(key, out var value))
            throw new InvalidOperationException($"Portal result '{key}' was not returned.");

        return value.Type switch
        {
            VariantValueType.ObjectPath => value.GetObjectPathAsString(),
            VariantValueType.String => value.GetString(),
            _ => throw new InvalidOperationException(
                $"Portal result '{key}' had unexpected type {value.Type}.")
        };
    }

    private static uint? ParseStreamNode(IReadOnlyDictionary<string, VariantValue> results)
    {
        if (!results.TryGetValue("streams", out var streamsVariant))
            return null;

        if (streamsVariant.Count <= 0)
            return null;

        var firstStreamStruct = streamsVariant.GetItem(0);
        if (firstStreamStruct.Count <= 0)
            return null;

        return firstStreamStruct.GetItem(0).GetUInt32();
    }

    private static void EnsureLinuxWayland()
    {
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("Wayland screen capture requires Linux.");

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
            throw new InvalidOperationException("WAYLAND_DISPLAY is not set. A Wayland desktop session is required.");
    }
    
    public void Dispose()
    {
        connection?.Dispose();
        connection = null;
    }

    private sealed record PortalResponse(uint Response, Dictionary<string, VariantValue> Results);
}