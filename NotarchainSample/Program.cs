using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lacuna.BStamper.Api.Transactions;
using Newtonsoft.Json;
using SHA3.Net;

namespace NotarchainSample {
	class Program {

		private static HttpClient client = new HttpClient();
		private const string apiKey = "--------API KEY------ Ask for a trial key send a e-mail to support@lacunasoftware.com---- ";

		public static async Task Main(string[] args) {
			client.BaseAddress = new Uri("https://blockchain.e-notariado.org.br/");
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
			var (inputData,resultPersist) = await Persist("e-ElectionLog","texto e-election");
			Console.WriteLine($"Transaction ID {resultPersist.Id}");
			Console.WriteLine($"Sha2 -512 {BitConverter.ToString(inputData.Sha2).Replace("-", "").ToLower()}");
			Console.WriteLine($"Sha3 -512 {BitConverter.ToString(inputData.Sha3).Replace("-", "").ToLower()}");
			Console.WriteLine("Waiting 15s for persistence");
			Thread.Sleep(15_000);
			var resultStatus = await GetInfo(resultPersist.Id);
			Console.WriteLine(JsonConvert.SerializeObject(resultStatus, Formatting.Indented));
			Console.WriteLine("Link para a página da transação");
			Console.WriteLine($"https://blockchain.e-notariado.org.br/transactions/{resultStatus.TransactionHash}");
		}

		private static async Task<(TransactionInputData,TransactionModel)> Persist(string documentName, string text) {
			var request = new TransactionInputData() {
				DocumentName = documentName,
				Sha2 = SHA2_512(text),
				Sha3 = Sha3.Sha3512().ComputeHash(Encoding.UTF8.GetBytes(text))
			};
			var jsonToSend = JsonConvert.SerializeObject(request, Newtonsoft.Json.Formatting.None);
			var body = new StringContent(jsonToSend, Encoding.UTF8, "application/json");
			var response = await client.PostAsync("/api/documents", body);
			response.EnsureSuccessStatusCode();
			var data = await response.Content.ReadAsStringAsync();
			var result = JsonConvert.DeserializeObject<TransactionModel>(data);
			return (request,result);
		}

		private static async Task<TransactionDetailedModel> GetInfo(Guid transactionId) {
			var response = await client.GetStringAsync($"/api/documents/{transactionId}");
			var result = JsonConvert.DeserializeObject<TransactionDetailedModel>(response);
			return result;
		}

		public static byte[] SHA2_512(string text) {
			using var algo = new SHA512Managed();
			algo.ComputeHash(Encoding.UTF8.GetBytes(text));
			var result = algo.Hash;
			return result;
		}
	}
}
