using Newtonsoft.Json.Linq;
using System.Net.Security;

namespace sonar_gate_status
{
	internal class HttpClientWrapper
	{
		private static string sonar_url = @"https://sonar.optum.com/api/measures/component?component=~PROJECT_KEY~&metricKeys=coverage";
		private static string sonar_activity_url = @"https://sonar.optum.com/api/project_analyses/search?project=~PROJECT_KEY~";

		private static void SetDefaultRequestHeaders(HttpClient client)
		{
			// From Network tab Development tools
			client.DefaultRequestHeaders.Add("Cookie", "");
		}

		private static HttpClientHandler CreateHttpHandler()
		{
			return new HttpClientHandler
			{
				UseDefaultCredentials = true,
				AllowAutoRedirect = true,
				ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
				{
					switch (sslPolicyErrors)
					{
						case SslPolicyErrors.None:
							return true;

						default:
							return false;
					}
				}
			};
		}

		public JObject GetContent(string url)
		{
			string responseErrorMessage = string.Empty;
			using (var client = new HttpClient(CreateHttpHandler()))
			{
				SetDefaultRequestHeaders(client);
				var page = client.GetAsync(url).Result;
				var content = page.Content.ReadAsStringAsync().Result;
				return JObject.Parse(content);
			}
		}

		public string GetSonarCodeCoverage(string sonarKey)
		{
			JToken? coverageToken = null;
			string url = sonar_url.Replace("~PROJECT_KEY~", sonarKey);
			JObject productInfo = GetContent(url);
			if (productInfo != null)
			{
				JToken? token = productInfo.SelectToken("component.measures");
				if (token != null)
				{
					coverageToken = token.SelectToken("[?(@.metric == 'coverage')].value");
				}
			}

			return coverageToken != null ? coverageToken.ToString() : "UNK";
		}

		public string GetSonarAnalysesDate(string sonarKey)
		{
			JToken? dateToken = null;
			string url = sonar_activity_url.Replace("~PROJECT_KEY~", sonarKey);
			JObject productInfo = GetContent(url);
			if (productInfo != null)
			{
				dateToken = productInfo.SelectToken("analyses");
			}

			return dateToken != null ? dateToken[0]!.SelectToken("date")!.ToString() : "UNK";
		}
	}
}
