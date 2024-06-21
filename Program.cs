namespace sonar_gate_status
{
	internal class Program
    {
        static void Main(string[] args)
        {
			Console.Write("Getting SonarQube status for products: ");

			HttpClientWrapper httpClientWrapper = new();

			// https://github.com/OptumInsight-Provider/pps-easygroup-cicd-poc/blob/e0b3747096edd89949bdec5452b3fffc059a5fbf/cicd/build_image_easygroup_core/easygroup_lib_products.txt
			foreach (var line in File.ReadLines("easygroup_lib_products.txt"))
            {
				Console.Write(".");
				string projectKey = string.Empty;	
				try
				{
					projectKey = ProductStatus.BuildProjectKey(line);
					if (string.IsNullOrEmpty(projectKey))
					{
						continue;
					}

					var codeCoverage = httpClientWrapper.GetSonarCodeCoverage(projectKey);
					var codeDate = httpClientWrapper.GetSonarAnalysesDate(projectKey);
					ProductStatus productStatus = new(projectKey, codeCoverage, codeDate);
					if (productStatus.isSuccessful())
					{
						ProductStatus.Success.Add(productStatus);
					}
					else
					{
						ProductStatus.Fail.Add(productStatus);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"{projectKey} NOT FOUND {ex.Message}" );
				}
            }
			Console.WriteLine("");

			ProductStatus.WriteReport();
		}
    }
}
