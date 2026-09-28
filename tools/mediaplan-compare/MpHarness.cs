using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;

// Стенд сверки медиаплана «было/стало»: запускается из папки со сборкой qd2
// (старой или новой), строит сценарии через реальный Excel и сохраняет xlsx.
class Storage : SecurityManager.ILoggedUserStorage
{
	public SecurityManager.User User { get; set; }
}

class Scenario
{
	public string Name;
	public Func<object> Create;
	public bool Sign = true, Advert = false, Hide = false;
}

static class P
{
	const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
	static Assembly M;
	static Type TMediaPlan, TAction, TCampaign, TActionOnMM, TPrintSettings;
	static string outDir;

	[STAThread]
	static int Main(string[] args)
	{
		outDir = args[0];
		Directory.CreateDirectory(outDir);
		Thread.CurrentThread.CurrentUICulture = new CultureInfo("ru-RU");
		Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("ru");
		M = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Merlin.exe"));
		TMediaPlan = M.GetType("Merlin.Classes.MediaPlan", true);
		TAction = M.GetType("Merlin.Classes.Action", true);
		TCampaign = M.GetType("Merlin.Classes.Campaign", true);
		TActionOnMM = M.GetType("Merlin.Classes.ActionOnMassmedia", true);
		TPrintSettings = M.GetType("Merlin.Classes.PrintSettings", true);
		SecurityManager.SetLoggedUserStorage(new Storage { User = SecurityManager.GetUser(3) });
		FogSoft.WinForm.DataAccess.DataAccessor.LoadProcedureConfig();

		string only = args.Length > 1 ? args[1] : null;
		int fails = 0;
		foreach (Scenario sc in Scenarios())
		{
			if (only != null && sc.Name != only) continue;
			DateTime t0 = DateTime.Now;
			try
			{
				string res = Run(sc);
				Console.WriteLine("OK   {0,-24} {1} {2:0.0}s", sc.Name, res, (DateTime.Now - t0).TotalSeconds);
			}
			catch (Exception e)
			{
				fails++;
				while (e is TargetInvocationException && e.InnerException != null) e = e.InnerException;
				Console.WriteLine("FAIL {0}: {1}", sc.Name, e);
			}
		}
		return fails;
	}

	static IEnumerable<Scenario> Scenarios()
	{
		yield return S("a178164_mm", () => CI(TAction, Act(178164)));
		yield return S("a178164_camp", () => CI(ListOf(TCampaign), CampaignsOf(Act(178164))));
		yield return S("a185888_mm", () => CI(TAction, Act(185888)));
		yield return S("a181602_mm", () => CI(TAction, Act(181602)));
		yield return S("a186271_camp", () => CI(ListOf(TCampaign), CampaignsOf(Act(186271))));
		yield return S("a186271_month", () => CI(ListOf(TCampaign), CampaignsOf(Act(186271)), ListOf(typeof(DateTime)), Months(2026, 9, 10, 11)));
		yield return S("a186271_period", () => CI(ListOf(TCampaign), CampaignsOf(Act(186271)), typeof(DateTime), new DateTime(2026, 10, 1), typeof(DateTime), new DateTime(2026, 10, 31)));
		yield return S("c412314_pack", () => CI(TCampaign, Camp(412314)));
		yield return S("c412314_pack_month", () => CI(TCampaign, Camp(412314), ListOf(typeof(DateTime)), Months(2026, 10, 11)));
		yield return S("c410509_spon", () => CI(TCampaign, Camp(410509)));
		yield return S("c410509_spon_month", () => CI(TCampaign, Camp(410509), ListOf(typeof(DateTime)), Months(2026, 10, 11)));
		yield return S("c412489_mod", () => CI(TCampaign, Camp(412489)));
		yield return S("c412170_lin", () => CI(TCampaign, Camp(412170)));
		yield return S("c412170_lin_period", () => CI(TCampaign, Camp(412170), typeof(DateTime), new DateTime(2026, 10, 1), typeof(DateTime), new DateTime(2026, 10, 15)));
		yield return S("multi3", () => CI(ListOf(TAction), Actions(186271, 185785, 181602)));
		Scenario v2;
		v2 = S("a178164_mm_v2", () => CI(TAction, Act(178164))); v2.Sign = false; v2.Advert = true; v2.Hide = true; yield return v2;
		v2 = S("a186271_camp_v2", () => CI(ListOf(TCampaign), CampaignsOf(Act(186271)))); v2.Sign = false; v2.Advert = true; v2.Hide = true; yield return v2;
		v2 = S("c412170_lin_v2", () => CI(TCampaign, Camp(412170))); v2.Sign = false; v2.Advert = true; v2.Hide = true; yield return v2;
	}

	static Scenario S(string name, Func<object> create) { return new Scenario { Name = name, Create = create }; }

	static string Run(Scenario sc)
	{
		object mp = sc.Create();
		object settings = Activator.CreateInstance(TPrintSettings);
		TPrintSettings.GetProperty("PrintWithSignatures").SetValue(settings, sc.Sign, null);
		TPrintSettings.GetProperty("ShowAdvertisingInfo").SetValue(settings, sc.Advert, null);
		TPrintSettings.GetProperty("HideTariffPrice").SetValue(settings, sc.Hide, null);

		FieldInfo builderField = TMediaPlan.GetField("_builder", Any);
		if (builderField == null)
		{
			// Старая сборка: построение прямо в MediaPlan.
			TMediaPlan.GetField("_printSettings", Any).SetValue(mp, settings);
			TMediaPlan.GetMethod("PrintMediaPlan", Any, null, Type.EmptyTypes, null).Invoke(mp, null);
		}
		else
		{
			object builder = builderField.GetValue(mp);
			builder.GetType().GetProperty("Settings").SetValue(builder, settings, null);
			Type docType = TMediaPlan.GetNestedType("ExcelDocument", BindingFlags.NonPublic);
			object doc = docType.GetConstructors(Any)[0].Invoke(new object[] { mp });
			builder.GetType().GetMethod("Build").Invoke(builder, new object[] { doc });
		}

		bool started = (bool)TMediaPlan.GetField("exportStarted", Any).GetValue(mp);
		string path = Path.Combine(outDir, sc.Name + ".xlsx");
		if (File.Exists(path)) File.Delete(path);
		if (!started)
		{
			File.WriteAllText(Path.Combine(outDir, sc.Name + ".empty"), "no data");
			return "(empty)";
		}
		ExportManager.Application.SaveToDisk(path);
		return path;
	}

	static object CI(params object[] typesAndArgs)
	{
		int n = typesAndArgs.Length / 2;
		Type[] types = new Type[n + 1];
		object[] vals = new object[n + 1];
		for (int i = 0; i < n; i++) { types[i] = (Type)typesAndArgs[2 * i]; vals[i] = typesAndArgs[2 * i + 1]; }
		types[n] = typeof(bool); vals[n] = false;
		MethodInfo m = TMediaPlan.GetMethod("CreateInstance", BindingFlags.Public | BindingFlags.Static, null, types, null);
		if (m == null) throw new Exception("no CreateInstance overload");
		return m.Invoke(null, vals);
	}

	static Type ListOf(Type t) { return typeof(IList<>).MakeGenericType(t); }

	static IList NewList(Type t) { return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(t)); }

	static object Act(int id) { return TActionOnMM.GetMethod("GetActionById", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { id }); }

	static object Camp(int id) { return TCampaign.GetMethod("GetCampaignById", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null).Invoke(null, new object[] { id }); }

	static object Actions(params int[] ids)
	{
		IList list = NewList(TAction);
		foreach (int id in ids) list.Add(Act(id));
		return list;
	}

	static object CampaignsOf(object action)
	{
		DataTable dt = (DataTable)TAction.GetMethod("Campaigns", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(bool) }, null).Invoke(action, new object[] { false });
		MethodInfo gc = TAction.GetMethod("GetCampaigns", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(DataTable) }, null);
		return gc.Invoke(null, new object[] { dt });
	}

	static object Months(int year, params int[] months)
	{
		IList list = NewList(typeof(DateTime));
		foreach (int m in months) list.Add(new DateTime(year, m, 1));
		return list;
	}
}
