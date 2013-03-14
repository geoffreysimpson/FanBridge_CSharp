using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RestSharp;
using System.Web.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace FanBridgeInterface
{
  public class FanBridgeInterface
	{
		public static string Send(string pPackage, string pMethod, Dictionary<string, string> pValues, string pHttpMethod = "GET")
		{
			var client = new RestClient("https://api.fanbridge.com/");
			var request = new RestRequest("{apiversion}/{package}/{method}");

			request.AddUrlSegment("apiversion", WebConfigurationManager.AppSettings["FanBridgeAPIVersion"]);
			request.AddUrlSegment("package", pPackage);
			request.AddUrlSegment("method", pMethod);
			
			if (pHttpMethod.CompareTo("GET") == 0)
			{
				request.Method = Method.GET;
			}
			else
			{
				request.Method = Method.POST;
			}

			foreach (KeyValuePair<String, String> entry in pValues)
			{
				request.AddParameter(entry.Key, entry.Value);
			}

			var response = client.Execute(request);

			if (response.StatusCode != System.Net.HttpStatusCode.OK)
			{
				throw new Exception("Error while communicating to FanBridge", response.ErrorException);
			}

			return response.Content;
		}

		public static string GetUserTokenFromSecretToken(string pSecretToken)
		{
			Dictionary<String, String> parameters = new Dictionary<string, string>();
			parameters.Add("secret", pSecretToken);

			string clientToken = Send("auth", "request_token", parameters, "POST");

			if (clientToken.Length != 32)
			{
				throw new Exception("Invalid Token returned");
			}

			return clientToken;
		}

		private static string generateSignature(Dictionary<string, string> pValues, string pSecret)
		{
			System.Text.StringBuilder request_string = new System.Text.StringBuilder();

			foreach (KeyValuePair<String, String> entry in pValues)
			{
				request_string.Append(entry.Key.ToString()).Append("=").Append(entry.Value.ToString());
			}
			request_string.Append(pSecret);

			string return_string = PHP_MD5(request_string.ToString());
			
			return return_string;
		}

		public static bool IsValid(string pSecret, string pToken)
		{
			//we are just going to try and get the user account from the token....so if it is successful, we call it valid
			Dictionary<String, String> parameters = new Dictionary<string, string>();

			parameters.Add("token", pToken);
			parameters.Add("signature", generateSignature(parameters, pSecret));

			string retval = Send("account", "me", parameters);

			return true;
		}

		public static string GetEmailGroupsJson(string pSecret, string pToken)
		{
			Dictionary<String, String> parameters = new Dictionary<string, string>();
			parameters.Add("token", pToken);
			parameters.Add("signature", generateSignature(parameters, pSecret));

			return Send("email_group", "fetch_all", parameters);
		}

		public static void AddNewEmailContact(string pSecret, string pToken, string pEmail, int pGroupId)
		{
			Dictionary<String, String> parameters = new Dictionary<string, string>();
			parameters.Add("email", pEmail);
			//if (pGroupId != -1)
			//{
			//	parameters.Add("groups", "['" + pGroupId.ToString() + "']");
			//}
			parameters.Add("token", pToken);
			parameters.Add("signature", generateSignature(parameters, pSecret));

			Send("subscriber", "add", parameters, "POST");

			return;
		}

		static readonly Lazy<MD5> _md5 = new Lazy<MD5>(MD5.Create);
		public static string PHP_MD5(string pToHash)
		{
			return string.Join(null, _md5.Value.ComputeHash(Encoding.UTF8.GetBytes(pToHash.Trim())).Select(x => x.ToString("x2")));
		}
	}
}
