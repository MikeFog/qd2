using System;
using System.IO;
using System.Reflection;

namespace ConnectionStringsProtector
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 3 && (args[0] == "p" || args[0] == "up"))
			{
				if (File.Exists(args[2]))
				{
					try
					{
						ConnectionStringProtector.ToggleConnectionStringProtection(args[2], args[0] == "p", args[1],ConfigurationFileType.WinForms);
						Console.WriteLine("Строка {0}.", args[0] == "p" ? "зашифрована" : "расшифрована");
					}
					catch (Exception e)
					{
						Console.WriteLine("В программе произошла ошибка: {0}", e.Message);
					}
				}
				else
					Console.WriteLine("Немогу найти файл: {0}", args[1]);
			}
			else
			{
				PrintHelp();
			}
			Console.WriteLine("Нажмите любую клавишу...");
			Console.ReadKey();
		}

		private static void PrintHelp()
		{
			Console.WriteLine("ConnectionStringsProtector ver {0}", Assembly.GetEntryAssembly().GetName().Version);
			Console.WriteLine("Written by Denis Gladkikh");
			Console.WriteLine("Шифрует строку подключения к базе данных.");
			Console.WriteLine();
			Console.WriteLine("Чтобы зашифровать строку используйте синтаксис:");
			Console.WriteLine("{0} p [Имя строки подключения] [Путь до файла приложения]", Assembly.GetEntryAssembly().GetName().Name);
			Console.WriteLine();
			Console.WriteLine("Чтобы дешифровать строку используйте синтаксис:");
			Console.WriteLine("{0} up [Имя строки подключения] [Путь до файла приложения]", Assembly.GetEntryAssembly().GetName().Name);
		}
	}
}
