using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Net.Security;
using System.Net.Mime;
using System.Net.Http;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;

namespace sonar_gate_status
{
    internal class Program
    {
        private static string sonar_project_key = @"com.optum.PPS.EZG.Core:~TYPE~.~PRODUCT~";
        private static string sonar_url = @"https://sonar.optum.com/api/measures/component?component=~PROJECT_KEY~&metricKeys=coverage";

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

        public static void GetSonarStatus(string sonarKey)
        {
            string responseErrorMessage = string.Empty;
            try
            {
                using (var httpClientHandler = CreateHttpHandler())
                using (var client = new HttpClient(httpClientHandler))
                {
                    SetDefaultRequestHeaders(client);

                    string url = sonar_url.Replace("~PROJECT_KEY~", sonarKey);
                    var page = client.GetAsync(url).Result;
                    var content = page.Content.ReadAsStringAsync().Result;
                    JObject productInfo = JObject.Parse(content);
                    if (productInfo != null)
                    {
                        JToken? token = productInfo.SelectToken("component.measures");
                        if (token != null)
                        {
                            var coverageToken = token.SelectToken("[?(@.metric == 'coverage')].value");
                            if (coverageToken != null)
                            {
                                Console.WriteLine($"{sonarKey}  - {coverageToken.ToString()}%");
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.WriteLine(sonarKey + " - NOT FOUND");
        }


        static void Main(string[] args)
        {
            // https://github.com/OptumInsight-Provider/pps-easygroup-cicd-poc/blob/e0b3747096edd89949bdec5452b3fffc059a5fbf/cicd/build_image_easygroup_core/easygroup_lib_products.txt
            foreach (var line in File.ReadLines("easygroup_lib_products.txt"))
            {
                var typeProductArray = line.Split('_');
                switch (typeProductArray[0])
                {
                    case "analyzers": 
                        typeProductArray[0] = "Analyzer";
                        typeProductArray[1] = typeProductArray[1] == "eamnlz01" ? "EAM" : "EDC";
                        break;
                    case "dist": 
                        typeProductArray[0] = "control"; 
                        break;
                    case "editors": 
                        typeProductArray[0] = "Edit"; 
                        break;
                    case "groupers": 
                        typeProductArray[0] = "Grpr"; 
                        break;
                    case "mappers": 
                        typeProductArray[0] = "Mapr"; 
                        break;
                    case "pricers": 
                        typeProductArray[0] = "Prcr";
                        switch(typeProductArray[1])
                        {
                            case "medicare": typeProductArray[1] = "medprc"; break;
                        }
                        break;
                }
                if (typeProductArray[0] == "control")
                    continue;

                string projectKey = sonar_project_key.Replace("~TYPE~", typeProductArray[0]).Replace("~PRODUCT~", typeProductArray[1]);
                GetSonarStatus(projectKey);
            }
        }
    }
}
