using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Synthbot.DiscordBot
{
	public class AsciiTableBuilder<T> where T : ITuple
	{
		private readonly IReadOnlyList<Tuple<string, int>> _columns;
		public AsciiTableBuilder(params Tuple<string, int>[] columns)
		{
			// TODO get tuple size and validate there are adequate columns
			_columns = columns.ToList();
		}


		public string GetTableString(params T[] rows)
		{
			return GetTableString(rows);
		}

		public string GetTableString(IList<T> rows)
		{
			var tableBuilder = new StringBuilder("\n");

			// Build column header
			foreach (var column in _columns)
			{
				var columnName = column.Item1;
				var columnSize = column.Item2;

				if (columnName.Length < columnSize)
				{
					tableBuilder.Append($"{columnName.PadOrTrucate(columnSize)} ");
				}
			}

			tableBuilder.Append("\n");

			// Build rows
			for (int i = 0; i < rows.Count; i++)
			{
				// Build columns
				var rowTuple = rows[i];
				for (int i2 = 0; i2 < rowTuple.Length; i2++)
				{
					var columnSize = _columns[i2].Item2;

					var itemString = (string)rowTuple[i2];

					tableBuilder.Append($"{itemString.PadOrTrucate(columnSize)} ");
				}
				tableBuilder.Append("\n");
			}

			return tableBuilder.ToString();
		}
	}

	public static class StringExtensions
	{
		public static string PadOrTrucate(this string item, int maxSize, char padCharacter = ' ')
		{
			string response;

			if (item.Length <= maxSize)
			{
				response = item.PadRight(maxSize, padCharacter);
			}
			else
			{
				response = item.Substring(0, maxSize);
			}

			return response;
		}
	}
}
