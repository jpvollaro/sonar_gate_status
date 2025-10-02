using Newtonsoft.Json.Linq;
using System.Net.Security;
using System.Xml.Linq;

namespace sonar_gate_status
{
	internal class HttpClientWrapper
	{
		private static string sonar_activity_url = @"https://sonar.optum.com/api/project_analyses/search?project=~PROJECT_KEY~";
		private static string sonar_additional_url = @"https://sonar.optum.com/api/measures/component?additionalFields=period%2Cmetrics&component=~PROJECT_KEY~&metricKeys=coverage%2Cnew_coverage%2Clines_to_cover%2Cnew_lines_to_cover%2Clines%2Cnew_lines%2C";
		
		private static void SetDefaultRequestHeaders(HttpClient client)
		{
			// From Network tab Development tools
			client.DefaultRequestHeaders.Add("Cookie", "pixel-ubrid=v2.0-da5881d40c748258cad6d4ab93655839-1433-1444-1712027850808-0000944390-1713879366504; optum_analytics_optout=false; XSRF-TOKEN=taerb6ouvcpbdp089d7ok577r9; JWT-SESSION=eyJhbGciOiJIUzI1NiJ9.eyJsYXN0UmVmcmVzaFRpbWUiOjE3MjQwNjYzNDY5MTAsInhzcmZUb2tlbiI6InRhZXJiNm91dmNwYmRwMDg5ZDdvazU3N3I5IiwianRpIjoiMGU5MDgzZjgtYjU1Ni00MDk4LWI1YTgtMjJkOTExMWFhZTlmIiwic3ViIjoianZvbGxhciIsImlhdCI6MTcyNDA2NjM0NiwiZXhwIjoxNzI0MTUyNzQ2fQ.sXWHXa5-_ETulAfWusokMs2MMb55a6Kkbnx7TNMgvkY");
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

		public ProductStatus GetSonarCodeCoverage(string sonarKey)
		{

			string url = sonar_additional_url.Replace("~PROJECT_KEY~", sonarKey);
			ProductStatus productStatus = new(sonarKey);
			productStatus.SetSonarCodeCoverage(GetContent(url));
			return productStatus;;
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

			return dateToken != null ? dateToken[0]!.SelectToken("date")!.ToString() : "";
		}
	}
}
