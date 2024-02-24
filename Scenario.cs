using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;

namespace SKSampleCatalog
{
	public class Scenario : IDisposable
	{
		private bool _disposedValue;

		protected Task _programTask;

		/// <summary>
		/// The Window for the <see cref="Scenario"/>. This should be set to <see cref="Terminal.Gui.Application.Top"/> in most cases.
		/// </summary>
		public Window Win { get; set; }

		/// <summary>
		/// Helper that provides the default <see cref="Terminal.Gui.Window"/> implementation with a frame and 
		/// label showing the name of the <see cref="Scenario"/> and logic to exit back to 
		/// the Scenario picker UI.
		/// Override <see cref="Init"/> to provide any <see cref="Terminal.Gui.Toplevel"/> behavior needed.
		/// </summary>
		/// <param name="colorScheme">The colorscheme to use.</param>
		/// <remarks>
		/// <para>
		/// The base implementation calls <see cref="Application.Init"/> and creates a <see cref="Window"/> for <see cref="Win"/> 
		/// and adds it to <see cref="Application.Top"/>.
		/// </para>
		/// <para>
		/// Overrides that do not call the base.<see cref="Run"/>, must call <see cref="Application.Init"/> 
		/// before creating any views or calling other Terminal.Gui APIs.
		/// </para>
		/// </remarks>
		public virtual void Init(ColorScheme colorScheme)
		{
			Application.Init();
			InitGui(colorScheme);
		}

		/// <summary>
		/// Override this method to initialize the <see cref="Scenario"/> with a custom GUI.
		/// </summary>
		public virtual void InitGui(ColorScheme colorScheme)
		{
			Win = new Window($"CTRL-C to Close - Scenario: {GetName()}")
			{
				X = 0,
				Y = 0,
				Width = Dim.Fill(),
				Height = Dim.Fill(),
				ColorScheme = colorScheme,
			};
			Application.Top.Add(Win);
		}

		/// <summary>
		/// Defines the metadata (Name and Description) for a <see cref="Scenario"/>
		/// </summary>
		[System.AttributeUsage(System.AttributeTargets.Class)]
		public class ScenarioMetadata : System.Attribute
		{
			/// <summary>
			/// <see cref="Scenario"/> Name
			/// </summary>
			public string Name { get; set; }

			/// <summary>
			/// <see cref="Scenario"/> Description
			/// </summary>
			public string Description { get; set; }

			/// <summary>
			/// <see cref="Scenario"/> Order Priority
			/// </summary>
			public int OrderPriority { get; set; }

			public ScenarioMetadata(string Name, string Description, int OrderPriority = 99999)
			{
				this.Name = Name;
				this.Description = Description;
				this.OrderPriority = OrderPriority;
			}

			/// <summary>
			/// Static helper function to get the <see cref="Scenario"/> Name given a Type
			/// </summary>
			/// <param name="t"></param>
			/// <returns></returns>
			public static string GetName(Type t) => ((ScenarioMetadata)System.Attribute.GetCustomAttributes(t)[0]).Name;

			/// <summary>
			/// Static helper function to get the <see cref="Scenario"/> Description given a Type
			/// </summary>
			/// <param name="t"></param>
			/// <returns></returns>
			public static string GetDescription(Type t) => ((ScenarioMetadata)System.Attribute.GetCustomAttributes(t)[0]).Description;

			/// <summary>
			/// Static helper function to get the <see cref="Scenario"/> Order Priority given a Type
			/// </summary>
			/// <param name="t"></param>
			/// <returns></returns>
			public static int GetOrderPriority(Type t) => ((ScenarioMetadata)System.Attribute.GetCustomAttributes(t)[0]).OrderPriority;
		}

		/// <summary>
		/// Helper to get the <see cref="Scenario"/> Name (defined in <see cref="ScenarioMetadata"/>)
		/// </summary>
		/// <returns></returns>
		public string GetName() => ScenarioMetadata.GetName(this.GetType());

		/// <summary>
		/// Helper to get the <see cref="Scenario"/> Description (defined in <see cref="ScenarioMetadata"/>)
		/// </summary>
		/// <returns></returns>
		public string GetDescription() => ScenarioMetadata.GetDescription(this.GetType());

		/// <summary>
		/// Helper to get the <see cref="Scenario"/> Order Priority (defined in <see cref="ScenarioMetadata"/>)
		/// </summary>
		/// <returns></returns>
		public int GetOrderPriority() => ScenarioMetadata.GetOrderPriority(this.GetType());

		/// <summary>
		/// Defines the category names used to catagorize a <see cref="Scenario"/>
		/// </summary>
		[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
		public class ScenarioCategory : System.Attribute
		{
			/// <summary>
			/// Category Name
			/// </summary>
			public string Name { get; set; }

			public ScenarioCategory(string Name) => this.Name = Name;

			/// <summary>
			/// Static helper function to get the <see cref="Scenario"/> Name given a Type
			/// </summary>
			/// <param name="t"></param>
			/// <returns>Name of the category</returns>
			public static string GetName(Type t) => ((ScenarioCategory)System.Attribute.GetCustomAttributes(t)[0]).Name;

			/// <summary>
			/// Static helper function to get the <see cref="Scenario"/> Categories given a Type
			/// </summary>
			/// <param name="t"></param>
			/// <returns>list of category names</returns>
			public static List<string> GetCategories(Type t) => System.Attribute.GetCustomAttributes(t)
				.ToList()
				.Where(a => a is ScenarioCategory)
				.Select(a => ((ScenarioCategory)a).Name)
				.ToList();
		}

		/// <summary>
		/// Helper function to get the list of categories a <see cref="Scenario"/> belongs to (defined in <see cref="ScenarioCategory"/>)
		/// </summary>
		/// <returns>list of category names</returns>
		public List<string> GetCategories() => ScenarioCategory.GetCategories(this.GetType());

		private static int _maxScenarioNameLen = 30;

		/// <summary>
		/// Gets the Scenario Name + Description with the Description padded
		/// based on the longest known Scenario name.
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"{GetName().PadRight(_maxScenarioNameLen)}{GetDescription()}";

		/// <summary>
		/// Sets up and runs the <see cref="Scenario"/>. 
		/// </summary>
		public virtual void Setup()
		{
			// Create a thread to ensure the UI is responsive
			_programTask = Task.Run(async () =>
			{
				await Run().ConfigureAwait(false);
			});
		}


		/// <summary>
		/// Override this to implement the <see cref="Scenario"/> run logic.
		/// </summary>
		/// <remarks>This is typically the best place to put scenario logic code.</remarks>
		public virtual Task Run()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Runs the <see cref="Scenario"/>. Override to start the <see cref="Scenario"/> using a <see cref="Toplevel"/> different than `Top`.
		/// 
		/// </summary>
		/// <remarks>
		/// Overrides that do not call the base.<see cref="StartSample"/>, must call <see cref="Application.Shutdown"/> before returning.
		/// </remarks>
		public virtual void StartSample()
		{
			// Must explicit call Application.Shutdown method to shutdown.
			Application.Run(Application.Top);
		}

		/// <summary>
		/// Stops the scenario. Override to change shutdown behavior for the <see cref="Scenario"/>.
		/// </summary>
		public virtual void RequestStop()
		{
			Application.RequestStop();
		}

		/// <summary>
		/// Returns a list of all Categories set by all of the <see cref="Scenario"/>s defined in the project.
		/// </summary>
		internal static List<string> GetAllCategories()
		{
			List<string> categories = new List<string>();
			foreach (Type type in typeof(Scenario).Assembly.GetTypes()
			 .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Scenario))))
			{
				List<System.Attribute> attrs = System.Attribute.GetCustomAttributes(type).ToList();
				categories = categories.Union(attrs.Where(a => a is ScenarioCategory).Select(a => ((ScenarioCategory)a).Name)).ToList();
			}

			// Sort
			categories = categories.OrderBy(c => c).ToList();

			// Put "All" at the top
			categories.Insert(0, "All Scenarios");
			return categories;
		}

		/// <summary>
		/// Returns a list of all <see cref="Scenario"/> instanaces defined in the project, sorted by <see cref="ScenarioMetadata.Name"/>.
		/// https://stackoverflow.com/questions/5411694/get-all-inherited-classes-of-an-abstract-class
		/// </summary>
		public static List<Scenario> GetScenarios()
		{
			List<Scenario> objects = new List<Scenario>();
			foreach (Type type in typeof(Scenario).Assembly.ExportedTypes
			 .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Scenario))))
			{
				var scenario = (Scenario)Activator.CreateInstance(type);
				objects.Add(scenario);
				_maxScenarioNameLen = Math.Max(_maxScenarioNameLen, scenario.GetName().Length + 1);
			}
			return objects.OrderBy(s => s.GetOrderPriority().ToString("D5") + s.GetName()).ToList();
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_programTask.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				_disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
