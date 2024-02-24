using System;
using Terminal.Gui;

public class ChatView : Window
{
	private Action<string> _onInput;
	private MessageHistory _messageHistory;

	public ChatView(Action<string> onInput, string title = "Semantic Kernel")
	{
		Title = title;

		ColorScheme = new ColorScheme
		{
			Normal = new Terminal.Gui.Attribute(Color.White, Color.Black)
		};

		_onInput += onInput;

		_messageHistory = new MessageHistory(this);

		Action<string> onEnter = (string input) =>
		{
			// Create a new message
			_messageHistory.AddMessage(input, "You", DateTime.Now);
			onInput.Invoke(input);
		};

		// Create the input window
		var InputField = new InputField(this, onEnter, _messageHistory.Bottom);

		InputField.SetFocus();
	}

	public void AddResponse(string reply, string name = "Bot")
	{
		// Create a new message
		_messageHistory.AddMessage(reply, name, DateTime.Now);
	}

	public void AppendResponse(string partial, string name = "Bot")
	{
		if (partial == null)
		{
			// Create a new message
			_messageHistory.AddMessage(string.Empty, name, DateTime.Now);
			return;
		}
		else
		{
			// Append to the last message
			_messageHistory.AppendToMessage(partial);
		}
	}
}