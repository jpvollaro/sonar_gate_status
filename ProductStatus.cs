using Newtonsoft.Json.Linq;
using sonar_gate_status;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sonar_gate_status
{
	internal class ProductStatus
	{
		internal string ProductKey { get; set; } = string.Empty;
		internal string ProductName { get; set; } = string.Empty;

		internal int codeLines { get; set; } = 0;
		internal int coveredCodeLines { get; set; } = 0;
		internal double CodeCoverage { get; set; } = 0.0;

		internal int newCodeLines { get; set; } = 0;
		internal int coveredNewLines { get; set; } = 0;
		internal double NewCodeCoverage { get; set; } = 0.0;

		internal DateTime LastAnalysisDate { get; set; } = DateTime.MinValue;

		public static List<ProductStatus> Success = new();
		public static List<ProductStatus> Fail = new();

		internal ProductStatus(string productKey)
		{
			ProductKey = productKey;
		}

		public void SetSonarCodeCoverage(JObject productInfo)
		{
			if (productInfo != null)
			{
				JToken? componentToken = productInfo.SelectToken("component");
				if (componentToken != null)
				{
					this.ProductName = componentToken.SelectToken("name")!.ToString();

					JToken? measures = componentToken.SelectToken("measures");
					if (measures == null)
						return;

					this.codeLines = int.Parse(measures.SelectToken("[?(@.metric == 'lines_to_cover')].value")!.ToString());
					this.CodeCoverage = Double.Parse(measures.SelectToken("[?(@.metric == 'coverage')].value")!.ToString());
					this.coveredCodeLines = (int)(this.codeLines * (this.CodeCoverage / 100));

					var x = measures.SelectToken("[?(@.metric == 'new_lines_to_cover')]");
					if (x == null)
					{
						this.NewCodeCoverage = 0.0;
						this.newCodeLines = 0;
						this.coveredNewLines = 0;
						return;
					}
							
					this.newCodeLines = int.Parse(measures.SelectToken("[?(@.metric == 'new_lines_to_cover')].period.value")!.ToString());
					if (this.newCodeLines != 0)
					{
						this.NewCodeCoverage = Double.Parse(measures.SelectToken("[?(@.metric == 'new_coverage')].period.value")!.ToString());
						this.coveredNewLines = (int)(this.newCodeLines * (this.NewCodeCoverage / 100));
					}
					else
					{
						this.NewCodeCoverage = 0.0;
						this.coveredNewLines = 0;
					}
				}
			}
		}

		public static string BuildProjectKey(string line)
		{
			const string sonar_project_key = @"com.optum.PPS.EZG.Core:~TYPE~.~PRODUCT~";

			if (line.StartsWith("com.optum.PPS.EZG."))
				return line;

			var typeProductArray = line.Split('_');
			switch (typeProductArray[0])
			{
				case "control":
					return string.Empty;
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
					switch (typeProductArray[1])
					{
						case "medicare": typeProductArray[1] = "medprc"; break;
					}
					break;
				default:
					return string.Empty;
			}

			return sonar_project_key.Replace("~TYPE~", typeProductArray[0]).Replace("~PRODUCT~", typeProductArray[1]);
		}

		public bool isSuccessful()
		{
			return (CodeCoverage != 0.0);
		}

		public override string ToString()
		{
			string lastScanDate = LastAnalysisDate == DateTime.MinValue ? "" : LastAnalysisDate.ToString("yyyy-MM-dd");
			return ($"{ProductKey},{ProductName},{lastScanDate},{codeLines},{coveredCodeLines},{CodeCoverage}%,{newCodeLines},{coveredNewLines},{NewCodeCoverage}%");
		}

		public static void WriteReport()
		{
			using (StreamWriter sw = new StreamWriter("SonarEASYGroup.csv"))
			{
				sw.Write("Project Or Repo Key,");
				sw.Write("Project Or Repo,");
				sw.Write("Last Scan Date,");
				sw.Write("Overall Lines To Cover,");
				sw.Write("Overall Lines Covered,");
				sw.Write("Code Coverage %,");
				sw.Write("New Lines To Cover,");
				sw.Write("New Lines Covered,");
				sw.WriteLine("New Code Coverage %");

				foreach (ProductStatus p in Fail)
				{
					sw.WriteLine(p.ToString());
					Console.WriteLine(p.ToString());
				}

				foreach (ProductStatus p in Success)
				{
					sw.WriteLine(p.ToString());
				}
			}
		}
	}
}
