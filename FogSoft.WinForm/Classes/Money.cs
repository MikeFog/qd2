using System;
using System.Data.SqlTypes;

namespace FogSoft.WinForm.Classes
{
	public sealed class Money
	{
		private Money() {}

		#region Arrays ------------------------------------------

		private static string[] Hundreds = {
		                                   	"",
		                                   	"сто ",
		                                   	"двести ",
		                                   	"триста ",
		                                   	"четыреста ",
		                                   	"пятьсот ",
		                                   	"шестьсот ",
		                                   	"семьсот ",
		                                   	"восемьсот ",
		                                   	"девятьсот "
		                                   };

		private static string[] Teens = {
		                                	"десять ",
		                                	"одиннадцать ",
		                                	"двенадцать ",
		                                	"тринадцать ",
		                                	"четырнадцать ",
		                                	"пятнадцать ",
		                                	"шестнадцать ",
		                                	"семнадцать ",
		                                	"восемнадцать ",
		                                	"девятнадцать "
		                                };

		private static string[] Tens = {
		                               	"",
		                               	"десять ",
		                               	"двадцать ",
		                               	"тридцать ",
		                               	"сорок ",
		                               	"пятьдесят ",
		                               	"шестьдесят ",
		                               	"семьдесят ",
		                               	"восемьдесят ",
		                               	"девяносто "
		                               };

		private static string[,] Decimals = {
		                                    	{
		                                    		"", "один ", "два ", "три ", "четыре ", "пять ", "шесть ",
		                                    		"семь ", "восемь ",
		                                    		"девять "
		                                    	},
		                                    	{
		                                    		"", "одна ", "две ", "три ", "четыре ", "пять ", "шесть ",
		                                    		"семь ", "восемь ",
		                                    		"девять "
		                                    	}
		                                    };

		private static int[] Decimals2 = {
		                                 	0,
		                                 	1,
		                                 	2,
		                                 	2,
		                                 	2,
		                                 	0,
		                                 	0,
		                                 	0,
		                                 	0,
		                                 	0
		                                 };

		private static string[,] Grands = {
		                                  	{
		                                  		"", "тысяч ", "миллионов ", "миллиардов ", "триллионов ",
		                                  		"квадриллионов "
		                                  	},
		                                  	{
		                                  		"", "тысяча ", "миллион ", "миллиард ", "триллион ",
		                                  		"квадриллион "
		                                  	},
		                                  	{
		                                  		"", "тысячи ", "миллиона ", "миллиарда ", "триллиона ",
		                                  		"квадриллиона "
		                                  	}
		                                  };

		#endregion

		public static string MoneyToString(decimal summa, bool Female)
		{
			summa = Math.Round(summa, 2);
			int rub = (int)summa;
			decimal kop = (summa - rub) * 100;

			string res = string.Empty, s = (rub).ToString();
			while(SqlInt32.Mod(s.Length, 3) > 0) s = "0" + s;

			for(int j = s.Length/3 - 1, k = 0; j >= 0; j--, k += 3)
			{
				if(s.Substring(k, 3) != "000")
				{
					if(s.Substring(k + 1, 1) == "1")
						res += Hundreds[ParseHelper.ParseToInt32(s.Substring(k, 1))] + Teens[ParseHelper.ParseToInt32(s.Substring(k + 2, 1))] +
						       Grands[0, j];
					else
					{
						res += Hundreds[ParseHelper.ParseToInt32(s.Substring(k, 1))] +
						       Tens[ParseHelper.ParseToInt32(s.Substring(k + 1, 1))] +
						       Decimals[((j == 1 || (j == 0 && Female)) ? 1 : 0), ParseHelper.ParseToInt32(s.Substring(k + 2, 1))] +
						       Grands[Decimals2[ParseHelper.ParseToInt32(s.Substring(k + 2, 1))], j];
					}
				}
			}
			return res + " руб. " + kop.ToString("00") + " коп.";
		}
	}
}