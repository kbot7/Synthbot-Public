using System;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Synthbot.Common;
using Synthbot.Common.Authentication;
using Synthbot.Common.SignalR;
using Synthbot.DAL;
using Synthbot.DAL.Models;
using Synthbot.DAL.Repositories;
using Synthbot.WebApp.Hubs;
using ReferralTokenReceipt = Synthbot.DAL.Models.ReferralTokenReceipt;

namespace Synthbot.WebApp.Areas.Identity.Pages.Account
{
	[AllowAnonymous]
	public class ExternalLoginModel : PageModel
	{
		private readonly SignInManager<SynthbotUser> _signInManager;
		private readonly UserManager<SynthbotUser> _userManager;
		private readonly ILogger<ExternalLoginModel> _logger;
		private readonly IConfiguration _config;
		private readonly ApplicationDbContext _db;
		private readonly IHubContext<DiscordBotHub> _botHub;
		private readonly DiscordUserRepository _discordUserRepo;

		public ExternalLoginModel(
			SignInManager<SynthbotUser> signInManager,
			UserManager<SynthbotUser> userManager,
			ILogger<ExternalLoginModel> logger,
			IConfiguration config,
			ApplicationDbContext db,
			IHubContext<DiscordBotHub> botHub,
			DiscordUserRepository discordUserRepo)
		{
			_signInManager = signInManager;
			_userManager = userManager;
			_logger = logger;
			_config = config;
			_db = db;
			_botHub = botHub;
			_discordUserRepo = discordUserRepo;
		}

		[BindProperty]
		public InputModel Input { get; set; }

		public string LoginProvider { get; set; }

		public string ReturnUrl { get; set; }

		[TempData]
		public string ErrorMessage { get; set; }

		public class InputModel
		{
			[Required]
			[EmailAddress]
			public string Email { get; set; }
		}

		public async Task<IActionResult> OnGetAsync(string provider, string returnUrl = null, string referralToken = null)
		{
			// Validate Referrer Token
			var key = Encoding.ASCII.GetBytes(_config["synthbot.token.sharedsecret"]);
			var handler = new JwtSecurityTokenHandler();
			var validations = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidAudiences = new[] { "Synthbot.WebApp" },
				ValidIssuers = new[] { "Synthbot.DiscordBot" }
			};
			ClaimsPrincipal referralTokenPrincipal;
			try
			{
				referralTokenPrincipal = handler.ValidateToken(referralToken, validations, out SecurityToken tokenSecure);
			}
			catch (Exception ex)
			{
				_logger.Log(LogLevel.Error, "Referral Token validation failed", ex);
				return new BadRequestResult();
			}

			// Add referral token
			var token = new ReferralTokenReceipt()
			{
				ReferralUserId = referralTokenPrincipal.GetDiscordUserId(),
				ReceivedTS = DateTime.UtcNow,
				ReferrerSignalrUser = referralTokenPrincipal.GetHubReplyUserId()
			};

			// TODO - Move to a service layer
			await _db.ReferralTokenReceipts.AddAsync(token);
			await _db.SaveChangesAsync();

			// Request a redirect to the external login provider.
			var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
			var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

			// Add referral token to spotify auth properties
			properties.Items.Add("state", token.Id.ToString());

			return new ChallengeResult(provider, properties);
		}

		public IActionResult OnPost(string provider, string returnUrl = null)
		{
			// Request a redirect to the external login provider.
			var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
			var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
			return new ChallengeResult(provider, properties);
		}

		public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
		{
			returnUrl = returnUrl ?? Url.Content("~/");
			if (remoteError != null)
			{
				ErrorMessage = $"Error from external provider: {remoteError}";
				return RedirectToPage("./Login", new {ReturnUrl = returnUrl });
			}
			var info = await _signInManager.GetExternalLoginInfoAsync();
			if (info == null)
			{
				ErrorMessage = "Error loading external login information.";
				return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
			}

			// Sign in the user with this external login provider if the user already has a login.
			var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor : true);
			if (result.Succeeded)
			{
				await _signInManager.UpdateExternalAuthenticationTokensAsync(info);
				_logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);

				// Update referral token
				var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
				var referralToken = await _db.ReferralTokenReceipts.FindAsync(info.Principal.GetClaimValueFromType("ReferralTokenId"));
				if (referralToken != null)
				{
					referralToken.SynthbotUserId = user.Id;
					referralToken.Claimed = true;
					referralToken.ClaimedTS = DateTime.UtcNow;
					referralToken = await SendSignalRAuthReply(referralToken, AuthReplyMethod.Reauthenticated);
					_db.ReferralTokenReceipts.Update(referralToken);
					await _db.SaveChangesAsync();
				}

				return LocalRedirect(returnUrl);
			}
			if (result.IsLockedOut)
			{
				return RedirectToPage("./Lockout");
			}
			else
			{
				// ReferralTokenId
				if (info.Principal.GetClaimFromType("ReferralTokenId") != null)
				{
					// Get the referral token
					var referralToken = await _db.ReferralTokenReceipts.FindAsync(info.Principal.GetClaimValueFromType("ReferralTokenId"));

					// Create new user
					var user = new SynthbotUser
					{
						UserName = info.Principal.FindFirstValue(ClaimTypes.Email),
						Email = info.Principal.FindFirstValue(ClaimTypes.Email),
						DiscordUserId = referralToken.ReferralUserId
					};

					var createResult = await _userManager.CreateAsync(user);

					if (createResult.Succeeded)
					{
						// Update referral token receipt to show it's been claimed
						referralToken.SynthbotUserId = (await _userManager.FindByEmailAsync(user.Email)).Id;
						referralToken.Claimed = true;
						referralToken.ClaimedTS = DateTime.UtcNow;

						referralToken = await SendSignalRAuthReply(referralToken, AuthReplyMethod.InitialRegistration);

						_db.ReferralTokenReceipts.Update(referralToken);
						await _db.SaveChangesAsync();

						// Add the external login provider information
						var addLoginResult = await _userManager.AddLoginAsync(user, info);
						await _signInManager.UpdateExternalAuthenticationTokensAsync(info);
						if (addLoginResult.Succeeded)
						{

							await _signInManager.SignInAsync(user, isPersistent: false);
							_logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

							var discordUser = await _db.DiscordUsers
								.FirstOrDefaultAsync(u => u.DiscordUserId == user.DiscordUserId);

							discordUser.UserStatus = DiscordUserStatus.RegisteredWithoutNotify;


							await _db.SaveChangesAsync();

							return LocalRedirect(returnUrl);
						}
					}
					foreach (var error in createResult.Errors)
					{
						ModelState.AddModelError(string.Empty, error.Description);
					}
				}
				else
				{
					ModelState.AddModelError(string.Empty, "Discord ID was missing. Ensure you logged in via the Discord PM Link");
				}

				ReturnUrl = returnUrl;
				LoginProvider = info.LoginProvider;
				return Page();
			}
		}

		// TODO Verify if this method is actually needed. If Login can only be external and can only come from Discord, this should never be hit
		public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
		{
			returnUrl = returnUrl ?? Url.Content("~/");
			// Get the information about the user from the external login provider
			var info = await _signInManager.GetExternalLoginInfoAsync();
			if (info == null)
			{
				ErrorMessage = "Error loading external login information during confirmation.";
				return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
			}

			if (ModelState.IsValid)
			{
				var user = new SynthbotUser { UserName = Input.Email, Email = Input.Email };
				var result = await _userManager.CreateAsync(user);

				if (result.Succeeded)
				{
					result = await _userManager.AddLoginAsync(user, info);
					await _signInManager.UpdateExternalAuthenticationTokensAsync(info);
					if (result.Succeeded)
					{
						await _signInManager.SignInAsync(user, isPersistent: false);
						_logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
						return LocalRedirect(returnUrl);
					}
				}
				foreach (var error in result.Errors)
				{
					ModelState.AddModelError(string.Empty, error.Description);
				}
			}

			LoginProvider = info.LoginProvider;
			ReturnUrl = returnUrl;
			return Page();
		}

		private async Task<ReferralTokenReceipt> SendSignalRAuthReply(ReferralTokenReceipt referralToken, AuthReplyMethod methodEnum)
		{
			string methodName = null;
			switch (methodEnum)
			{
				case AuthReplyMethod.InitialRegistration:
					methodName = SignalrMethodNames.InitialRegistration;
					break;
				case AuthReplyMethod.Reauthenticated:
					methodName = SignalrMethodNames.Reauthenticated;
					break;
			}
			try
			{
				var replyPayload = new TokenPayload()
				{
					DiscordUserId = referralToken.ReferralUserId
				};
				await _botHub.Clients.User(SignalrUsernames.BotUsername).SendAsync(methodName, replyPayload);
				referralToken.ReplySent = true;
			}
			catch (Exception ex)
			{
				_logger.Log(LogLevel.Error, "Failed sending SignalR auth response", ex);
				referralToken.ReplyError = true;
			}

			return referralToken;
		}
	}
	enum AuthReplyMethod
	{
		InitialRegistration,
		Reauthenticated
	};
}
	
