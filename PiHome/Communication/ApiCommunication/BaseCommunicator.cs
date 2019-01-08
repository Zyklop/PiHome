using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Communication.ApiCommunication
{
	public abstract class BaseCommunicator
	{
		private readonly IPAddress addr;
		private readonly int port;

		protected BaseCommunicator(IPAddress moduleIp, int port = 8080)
		{
			addr = moduleIp;
			this.port = port;
		}

		protected abstract string BasePath { get; }

		protected string AbsoluteUri => $"http://{addr}:{port}/{BasePath}";

		protected WebClient GetClient()
		{
			var wc = new WebClient{BaseAddress = AbsoluteUri};
			return wc;
		}

		protected TResponse GetRequest<TResponse>(string path, params string[] allParams)
		{
			using (var wc = GetClient())
			{
				var uri = BasePath + path;
				if (allParams != null && allParams.Any())
				{
					uri += "/" + string.Join("/", allParams);
				}
				var resp = wc.DownloadString(uri);
				var serialized = JsonConvert.DeserializeObject<TResponse>(resp);
				return serialized;
			}
		}

		protected TResponse PostRequest<TRequest, TResponse>(string path, TRequest reqObject)
		{
			using (var wc = GetClient())
			{
				var serialized = JsonConvert.SerializeObject(reqObject);
				var resp = wc.UploadString(BasePath + path, serialized);
				return JsonConvert.DeserializeObject<TResponse>(resp);
			}
		}

		protected TResponse PostRequest<TResponse>(string path, byte[] data)
		{
			using (var wc = GetClient())
			{
				var respData = wc.UploadData(BasePath + path, data);
				var resp = Encoding.UTF8.GetString(respData);
				return JsonConvert.DeserializeObject<TResponse>(resp);
			}
		}
	}
}