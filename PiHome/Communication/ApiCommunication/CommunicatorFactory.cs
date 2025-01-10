using System.Collections.Concurrent;
using System.Net;

namespace Communication.ApiCommunication;

public class CommunicatorFactory
{
    private readonly ConcurrentDictionary<IPAddress, LedCommunicator> ledCommunicators = new ();
    private readonly ConcurrentDictionary<IPAddress, SensorCommunicator> sensorCommunicators = new ();

    public LedCommunicator GetLedCommunicator(IPAddress address)
    {
        return ledCommunicators.GetOrAdd(address, x => new LedCommunicator(x));
    }

    public SensorCommunicator GetSensorCommunicator(IPAddress address)
    {
        return sensorCommunicators.GetOrAdd(address, x => new SensorCommunicator(x));
    }
}