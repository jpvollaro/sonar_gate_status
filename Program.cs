namespace sonar_gate_status
{
	internal class Program
    {
        static void Main(string[] args)
        {
			Console.Write("Getting SonarQube status for products: ");

			HttpClientWrapper httpClientWrapper = new();

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

					ProductStatus productStatus = httpClientWrapper.GetSonarCodeCoverage(projectKey);
					if (productStatus.isSuccessful())
					{ 
						productStatus.LastAnalysisDate = DateTime.Parse(httpClientWrapper.GetSonarAnalysesDate(projectKey));
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
