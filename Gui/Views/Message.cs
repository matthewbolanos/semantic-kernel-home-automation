using System;
using Terminal.Gui;
public class Message
{
	private View _renderedView;
	private View _parentView;
	private string _text = "";
	private string _sender = "";
	private DateTime _sentAt = DateTime.Now;

	public int Height
	{
		get
		{
			var height = 3;
			// Split the text by new lines
			string[] lines = _text.Split('\n');

			foreach (var line in lines)
			{
				height++;

				// Split the text by spaces
				string[] words = line.Split(' ');

				var currentLineLength = 0;

				foreach (var word in words)
				{
					if (currentLineLength + word.Length > Width - 2)
					{
						height++;
						currentLineLength = 0;
					}
					currentLineLength += word.Length + 1;
				}
			}

			return height;
		}
	}

	public int Width
	{
		get
		{
			int parentWidth = _parentView.Frame.Width;

			if (_text.Length < _sender.Length + 1)
			{
				return _sender.Length + 1 + 2;
			}
			if (_text.Length < parentWidth / 4 * 3)
			{
				return _text.Length + 2;
			}

			return (int)(parentWidth / 4 * 3);
		}
	}

	public Message(View parentView, string text, string sender, DateTime sentAt, Pos position)
	{
		_text = text;
		_sender = sender;
		_sentAt = sentAt;
		_parentView = parentView;

		_renderedView = new View()
		{
			X = _sender == "You" ? Pos.Right(_parentView) - Width - 2 : 2,
			Y = position + 1,
			Width = Width - 2,
			Height = Height - 2,
			CanFocus = false,
			Text = $"{_sender}:\n{_text}",
			ColorScheme = new ColorScheme
			{
				Normal = new Terminal.Gui.Attribute(sender == "You" ? Color.BrightCyan : Color.White, Color.Black)
			},
			Border = new Border()
			{
				BorderStyle = BorderStyle.Rounded,
				BorderBrush = sender == "You" ? Color.BrightCyan : Color.White
			}

		};

		_parentView.Add(_renderedView);
	}

	public void Scroll(int offset)
	{
		_renderedView.Y += offset;
	}

	public void AppendContent(string text)
	{
		_text += text;
		_renderedView.Text = $"{_sender}:\n{_text}";
		_renderedView.Width = Width - 2;
		_renderedView.Height = Height - 2;
		_renderedView.X = _sender == "You" ? Pos.Right(_parentView) - Width - 2 : 2;
	}
}