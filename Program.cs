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
            client.DefaultRequestHeaders.Add("Cookie",
                "pixel-ubrid=v2.0-7c192fd8b04a3be6177c7a73d815df27-24644-24650-1689735327873-0001681933-1692825587721; optum_analytics_optout=false; Tv6TR2qd=Ax6buYKMAQAAKvwyZEHrHiQEtNQHdUAlzfYqMBihfLNpRIeMmHoDb90qZV8yAcbLr-sXTtQxwH8AAEB3AAAAAA|1|0|d1d693e606e1483f2479764765588c4e92077479; _gcl_au=1.1.808685684.1703000257; cjConsent=MHxOfDB8Tnww; cjUser=35021592-2bd6-4fa5-b634-58544999e35d; cjLiveRampLastCall=2023-12-19T15:37:37.899Z; AMCV_8E391C8B533058250A490D4D%40AdobeOrg=359503849%7CMCIDTS%7C19711%7CMCMID%7C14648001493231744803359264392437447730%7CMCAAMLH-1703679794%7C9%7CMCAAMB-1703679794%7C6G1ynYcLPuiQxYZrsz_pkqfLG9yMXBpb2zX5dvJdYQJzPXImdj0y%7CMCOPTOUT-1703082194s%7CNONE%7CMCSYNCSOP%7C411-19718%7CvVersion%7C5.0.1; fs_uid=#o-1GJF9D-na1#abe1d396-c3a3-4731-a4ef-4c48979c8acc:49f2f276-8458-44c8-8812-1f2933e51363:1703074992024::4#/1734536253; mbox=PC#1408cd201803407697f4d2e805a282e7.35_0#1766319958|session#0d27209e9c2643b08a422319e92cbadd#1703077019; XSRF-TOKEN=g5gq0lj5s5v66dsl47isgg2282; JWT-SESSION=eyJhbGciOiJIUzI1NiJ9.eyJsYXN0UmVmcmVzaFRpbWUiOjE3MDgwMzQ3MzM5MjgsInhzcmZUb2tlbiI6Imc1Z3EwbGo1czV2NjZkc2w0N2lzZ2cyMjgyIiwianRpIjoiQVkydXpiZGxfRDkwWVhZYmY5ai0iLCJzdWIiOiJqdm9sbGFyIiwiaWF0IjoxNzA4MDM0NzMzLCJleHAiOjE3MDgxMjExMzN9.fUkLkqFcpL2w3bBGcWC2Mm7R9cyS2YkMcY3rg4irQww");
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
