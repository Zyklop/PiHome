using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;

namespace PiUi.Services
{
	public class NetworkSupervisor : IHostedService, IDisposable
	{
		private CancellationTokenSource canceller = new CancellationTokenSource();
		private int failedAttempts = 0;

		public Task StartAsync(CancellationToken cancellationToken)
		{
			Task.Run(() => CheckConnectivityForever(canceller.Token));
			return Task.CompletedTask;
		}

		private void CheckConnectivityForever(CancellationToken cancellerToken)
		{
			while (!cancellerToken.IsCancellationRequested)
			{
				try
				{
					if (failedAttempts > 10)
					{
						Reconnect();
					}
					var ping = new Ping();
					var res = ping.Send("192.168.1.1", 1000);
					if (res.Status == IPStatus.Success)
					{
						failedAttempts = 0;
						Thread.Sleep(30000);
					}
					else
					{
						failedAttempts++;
						Thread.Sleep(5000);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					failedAttempts++;
					Thread.Sleep(10000);
				}
			}
		}

		private void Reconnect()
		{
			Bash("ifdown --force wlan0");
			Bash("ifup wlan0");
		}

		public static string Bash(string cmd)
		{
			var escapedArgs = cmd.Replace("\"", "\\\"");

			var process = new Process()
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "/bin/bash",
					Arguments = $"-c \"{escapedArgs}\"",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				}
			};
			process.Start();
			string result = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			return result;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			canceller.Cancel();
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			canceller.Cancel();
			canceller?.Dispose();
		}
	}
}