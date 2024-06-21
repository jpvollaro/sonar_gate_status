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
		internal double CodeCoverage { get; set; } = 0.0;
		internal DateTime LastAnalysisDate { get; set; } = DateTime.MinValue;

		public static List<ProductStatus> Success = new();
		public static List<ProductStatus> Fail = new();

		internal ProductStatus(string productKey, string codeCoverageValue, string lastAnalysisDateValue)
		{
			double d = 0.0;
			ProductKey = productKey;

			if (codeCoverageValue == "UNK")
			{
				CodeCoverage = 0.0;
				LastAnalysisDate = DateTime.MinValue;
				return;
			}
			double.TryParse(codeCoverageValue, out d);
			CodeCoverage = d;

			if (lastAnalysisDateValue != "UNK")
				LastAnalysisDate = DateTime.Parse(lastAnalysisDateValue);
		}

		public static string BuildProjectKey(string line)
		{
			const string sonar_project_key = @"com.optum.PPS.EZG.Core:~TYPE~.~PRODUCT~";

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
			return (CodeCoverage != 0.0 && LastAnalysisDate != DateTime.MinValue);
		}

		public override string ToString()
		{
			string result = string.Empty;
			if (isSuccessful())
			{
				return ($"{ProductKey} {LastAnalysisDate} {CodeCoverage} %");
			}
			else
			{
				return ($"{ProductKey} NOT FOUND");
			}
		}

		public static void WriteReport()
		{
			using (StreamWriter sw = new StreamWriter("report.txt"))
			{
				sw.WriteLine("");
				sw.WriteLine("");
				sw.WriteLine("FAILED");
				foreach (ProductStatus p in Fail)
				{
					sw.WriteLine(p.ToString());
					Console.WriteLine(p.ToString());
				}

				sw.WriteLine("");
				sw.WriteLine("SUCCESS");
				foreach (ProductStatus p in Success)
				{
					sw.WriteLine(p.ToString());
				}
			}
		}
	}
}
