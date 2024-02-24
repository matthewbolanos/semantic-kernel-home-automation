using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Contents;
using Options;
using Terminal.Gui;

public class InputField
{
	private View _renderedView;
	private TextField _inputWindow;
	private RecordButton _recordButton;
	private Action<string> _onEnter;
	private View _parentView;

	private Label _label;

	public InputField(View parentView, Action<string> onEnter, Pos Y)
	{
		_parentView = parentView;

		// Create a frame view that will hold the text field
		_renderedView = new FrameView()
		{
			X = 0,
			Y = Y + 1,
			Width = Dim.Fill(),
			Height = 3, // Increase the height to accommodate the border
			Border = new Border()
			{
				BorderStyle = BorderStyle.Single,
				BorderBrush = Color.White
			},
		};

		// Create label for the input window
		_label = new Label("Enter your message:")
		{
			X = 1,
			Y = Y,
			Width = Dim.Fill(),
			Height = 1,
			TextAlignment = TextAlignment.Left,
			ColorScheme = new ColorScheme
			{
				Normal = new Terminal.Gui.Attribute(Color.White, Color.Black)
			}
		};
		_parentView.Add(_label);

		// Create the input window
		_inputWindow = new TextField("")
		{
			Y = 0,
			Width = Dim.Fill() - 6,
			Height = 1
		};

		_inputWindow.KeyPress += (args) =>
		{
			if (args.KeyEvent.Key == Key.Enter && _inputWindow.Text.Length > 0)
			{
				AddMessage();
			}
		};
		_renderedView.Add(_inputWindow);

		// Create the record button
		_recordButton = new RecordButton
		{
			X = Pos.Right(_inputWindow) + 1, // Position the button to the right of the input window
			Y = 0,
			Width = 1, // Adjust the width as needed
			Height = 1,
			OnRecordingStoppedAsync = async (audioFilePath) =>
			{
				// Get the OpenAI options
				// TODO: Move this to a service
				HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder();
				hostBuilder.Services.AddOptions<OpenAIOptions>()
							.Bind(hostBuilder.Configuration.GetSection(nameof(OpenAIOptions)))
							.ValidateDataAnnotations();
				var host = hostBuilder.Build();
				var openAIOptions = host.Services.GetService<IOptions<OpenAIOptions>>()?.Value;

				// Convert the audio file into text
				// Create a kernel builder
				OpenAIAudioToTextService audioToTextService = new OpenAIAudioToTextService(
					modelId: openAIOptions.SpeechToTextModelId,
					apiKey: openAIOptions.ApiKey
				);

				ReadOnlyMemory<byte> audioData = await File.ReadAllBytesAsync(audioFilePath).ConfigureAwait(false);
				AudioContent audioContent = new(new BinaryData(audioData));

				OpenAIAudioToTextExecutionSettings executionSettings = new("input.wav")
				{
					Language = "en"
				};

				TextContent transcription = await audioToTextService.GetTextContentAsync(audioContent, executionSettings).ConfigureAwait(false);

				_inputWindow.Text = transcription.ToString();
				AddMessage();
			}
		};
		_renderedView.Add(_recordButton);

		_parentView.Add(_renderedView);

		_onEnter = onEnter;
	}

	public void Redraw()
	{
		//_parentView.Redraw(_renderedView.Bounds);
	}

	public void SetFocus()
	{
		if (_inputWindow != null)
		{
			//_inputWindow.SetFocus();
		}
	}

	private void AddMessage()
	{
		if (_inputWindow != null && _inputWindow.Text.Length > 0)
		{
			if (_onEnter != null)
			{
				_onEnter(_inputWindow.Text.ToString()!);
			}

			_inputWindow.Text = string.Empty; // Clear the input window
		}
	}
}