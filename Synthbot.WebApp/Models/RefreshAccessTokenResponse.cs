using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Synthbot.WebApp.Models
{
	public class RefreshAccessTokenResponse
	{
		public bool Success { get; set; }
		public string Message { get; set; }

		public static RefreshAccessTokenResponse CreateFailure(string message) => new RefreshAccessTokenResponse()
		{
			Success = false,
			Message = message
		};

		public static RefreshAccessTokenResponse CreateSuccess() => new RefreshAccessTokenResponse() {Success = true};
	}
}
