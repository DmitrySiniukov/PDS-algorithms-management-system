using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Enterprise.Infrastructure
{
	public static class Extentions
	{
		private static Dictionary<int, string> _month;

		public static Dictionary<int, string> Months {
			get
			{
				if (_month == null)
				{
					_month = new Dictionary<int, string>
					{
						{ 1, "січня"},
						{ 2, "лютого"},
						{ 3, "березня"},
						{ 4, "квітня"},
						{ 5, "травня"},
						{ 6, "червня"},
						{ 7, "липня"},
						{ 8, "серпня"},
						{ 9, "вересня"},
						{ 10, "жовтня"},
						{ 11, "листопада"},
						{ 12, "грудня"}
					};
				}
				return _month;
			}
		}

		public static string ToUkrDateTimeString(this DateTime dateTime)
		{
			return string.Format("{0} {1} {2}р. ({3:D2}:{4:D2})", dateTime.Day, Months[dateTime.Month], dateTime.Year, dateTime.Hour, dateTime.Minute);
		}
	}
}