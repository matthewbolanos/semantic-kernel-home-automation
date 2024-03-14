using System;
using System.Collections.Generic;
using Terminal.Gui;
using static Terminal.Gui.View;

public class MessageHistory
{
	public List<Message> Messages { get; set; } = new List<Message>();
	public Pos Bottom => Pos.Bottom(_renderedView);
	private ScrollView _renderedView;
	private View _parentView;
	private int _scrollHeight = 0;

	public MessageHistory(View parentView)
	{
		_parentView = parentView;

		// Create a frame view that will hold the text field
		_renderedView = new ScrollView()
		{
			X = 0,
			Y = 0,
			Width = Dim.Fill(),
			Height = Dim.Fill() - 4,
			ContentSize = new Size(0, 0),
		};

		parentView.Add(_renderedView);

		parentView.LayoutComplete += (LayoutEventArgs args) =>
		{
			GetNewContentHeight();
		};

	}

	public void AddMessage(string input, string sender, DateTime sentAt)
	{
		Message message = new Message(_renderedView, input, sender, DateTime.Now, _scrollHeight);
		Messages.Add(message);
		_scrollHeight = _scrollHeight + message.Height;
		GetNewContentHeight();
	}

	public void AppendToMessage(string input)
	{
		// Substract the height of the last message
		_scrollHeight = _scrollHeight - Messages[^1].Height;
		Messages[^1].AppendContent(input);
		_scrollHeight = _scrollHeight + Messages[^1].Height;
		GetNewContentHeight();
	}

	public void Redraw()
	{
		_parentView.Redraw(_renderedView.Bounds);
	}

	public void GetNewContentHeight()
	{
		var currentScrollPosition = _renderedView.ContentOffset.Y;
		var currentScrollHeight = _renderedView.ContentSize.Height;

		// get larger of parentView.Frame.Height - 6 or _scrollHeight
		var height = _parentView.Frame.Height - 6;
		if (_scrollHeight > height)
		{
			height = _scrollHeight;
		}

		_renderedView.ContentSize = new Size(_parentView.Frame.Width - 3, height);

		// If the user is at the bottom, scroll down
		if (_renderedView.Frame.Height - currentScrollPosition >= currentScrollHeight)
		{
			_renderedView.ScrollDown(height);
		}
	}
}