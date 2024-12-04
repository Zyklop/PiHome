using System.Net;
using Communication.Types;

namespace Communication.ApiCommunication
{
	public class LedCommunicator : BaseCommunicator
    {
        private byte[]? current;
        private byte[]? target;
		private CancellationTokenSource tokenSource;

        internal LedCommunicator(IPAddress moduleIp) : base(moduleIp)
		{
		}

		protected override string BasePath => "/led/api";

		public void TurnOff()
		{
			GetRequest<StatusResponse>("/turnoff");
		}

		public void SetSolid(byte red, byte green, byte blue)
		{
			GetRequest<StatusResponse>("/solid", red.ToString(), green.ToString(), blue.ToString());
		}

		public void SetRGBB(byte[] data, bool fade)
        {
			tokenSource?.Cancel();
            tokenSource = new CancellationTokenSource();
            if (fade)
            {
                target = data;
                current ??= new byte[data.Length];
                Task.Run(() => FadeTo(tokenSource.Token));
            }
            else
            {
                current = data;
                target = data;
                PostCurrent();
            }
        }

        private async Task FadeTo(CancellationToken token)
        {
            bool hasChanges;
            do
            {
                hasChanges = false;
                var start = DateTime.UtcNow;
                token.ThrowIfCancellationRequested();
                for (int i = 0; i < target!.Length; i++)
                {
                    if (target[i] > current![i])
                    {
                        current[i]++;
                        hasChanges = true;
                    }
                    else if (target[i] < current[i])
                    {
                        current[i]--;
                        hasChanges = true;
                    }
                }
                token.ThrowIfCancellationRequested();
                PostCurrent();
                var wait = Math.Max(0, 50 - (int)(DateTime.UtcNow - start).TotalMilliseconds);
                await Task.Delay(wait);
            } while (hasChanges);
        }

        private void PostCurrent()
        {
            PostRequest<StatusResponse>("/customRGBB", current);
        }
    }
}