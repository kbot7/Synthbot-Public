using System;
using System.Collections;
using Synthbot.WebApp.Models;

namespace Synthbot.WebApp.Services
{
	public class UserIdCache
	{
		private readonly Hashtable _synthbotIds;
		private readonly Hashtable _spotifyIds;
		private readonly Hashtable _discordIds;

		public UserIdCache()
		{
			_synthbotIds = new Hashtable();
			_spotifyIds = new Hashtable();
			_discordIds = new Hashtable();
		}

		/// <summary>
		/// Add a cached association between at least 2 ids. Using less than 2 IDs will cause an exception
		/// </summary>
		/// <param name="synthbotId"></param>
		/// <param name="spotifyId"></param>
		/// <param name="discordId"></param>
		public void Add(string synthbotId = null, string spotifyId = null, string discordId = null)
		{
			// If there are less than 2 params specified, throw exception
			// We cant make an association with only 1 id
			var synthbotIdNull = string.IsNullOrWhiteSpace(synthbotId) ? 0 : 1;
			var spotifyIdNull = string.IsNullOrWhiteSpace(spotifyId) ? 0 : 1;
			var discordIdNull = string.IsNullOrWhiteSpace(discordId) ? 0 : 1;
			var nullParamCount = synthbotIdNull + spotifyIdNull + discordIdNull;
			if (nullParamCount < 2)
			{
				string name = string.Empty;
				name = synthbotIdNull == 1 ? nameof(synthbotId) : name;
				name = spotifyIdNull == 1 ? nameof(spotifyId) : name;
				name = discordIdNull == 1 ? nameof(discordId) : name;
				throw new ArgumentNullException(name);
			}

			var group = new UserIdGroup()
			{
				SynthbotId = synthbotId,
				DiscordId = discordId,
				SpotifyId = spotifyId
			};
			Add(group);
		}

		public void Add(UserIdGroup group)
		{
			if (!string.IsNullOrWhiteSpace(group.SynthbotId))
			{
				_synthbotIds[group.SynthbotId] = group;
			}
			if (!string.IsNullOrWhiteSpace(group.SpotifyId))
			{
				_spotifyIds[group.SpotifyId] = group;
			}
			if (!string.IsNullOrWhiteSpace(group.DiscordId))
			{
				_discordIds[group.DiscordId] = group;
			}
		}

		public UserIdGroup FromSynthbotId(string synthbotId)
		{
			var group = _synthbotIds[synthbotId];
			return (UserIdGroup) @group;
		}

		public UserIdGroup FromSpotifyId(string spotifyId)
		{
			var group = _spotifyIds[spotifyId];
			return (UserIdGroup)@group;
		}

		public UserIdGroup FromDiscordId(string discordId)
		{
			var group = _discordIds[discordId];
			return (UserIdGroup)@group;
		}
	}
}
