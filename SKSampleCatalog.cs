using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace SKSampleCatalog
{
	class SKSampleCatalogApp
	{
		static void Main(string[] args)
		{
			System.Console.OutputEncoding = Encoding.Default;

			if (Debugger.IsAttached)
			{
				CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");
			}

			_scenarios = Scenario.GetScenarios();

			if (args.Length > 0 && args.Contains("-usc"))
			{
				_useSystemConsole = true;
				args = args.Where(val => val != "-usc").ToArray();
			}

			// Run the first scenario
			if (_scenarios.Count > 0)
			{
				_selectedScenario = (Scenario)Activator.CreateInstance(_scenarios[1].GetType());
				Application.UseSystemConsole = _useSystemConsole;
				Application.Init();
				_selectedScenario.Init(null);
				_selectedScenario.Setup();
				_selectedScenario.StartSample();
				_selectedScenario.Dispose();
				_selectedScenario = null;
				Application.Shutdown();
				return;
			}
		}

		static List<Scenario> _scenarios;
		static Scenario _selectedScenario = null;
		static bool _useSystemConsole = false;
	}
}
