using System;
using Terminal.Gui;
using OpenTK.Audio.OpenAL;
using System.Threading;
using System.IO;
using System.Threading.Tasks;

public class RecordButton : Button
{
	private bool _isRecording = false;

	private readonly ColorScheme _recordingColorScheme = new ColorScheme
	{
		Normal = new Terminal.Gui.Attribute(Color.White, Color.Red),
		HotNormal = new Terminal.Gui.Attribute(Color.White, Color.Red),
		Focus = new Terminal.Gui.Attribute(Color.White, Color.Red),
		HotFocus = new Terminal.Gui.Attribute(Color.White, Color.Red),
	};

	private readonly ColorScheme _notRecordingColorScheme = new ColorScheme
	{
		Normal = new Terminal.Gui.Attribute(Color.White, Color.Gray),
		HotNormal = new Terminal.Gui.Attribute(Color.White, Color.Gray),
		Focus = new Terminal.Gui.Attribute(Color.White, Color.Gray),
		HotFocus = new Terminal.Gui.Attribute(Color.White, Color.Gray),
	};

	private readonly int _sampleRate = 44100; // CD quality
	private readonly ALFormat _format = ALFormat.Mono16;
	private short[] _buffer;

	private int totalSamplesCaptured = 0;
	private ALCaptureDevice _device;
	private Thread _recordingThread;
	public Func<string, Task> OnRecordingStoppedAsync;

	public RecordButton() : base("◯")
	{
		this.ColorScheme = _notRecordingColorScheme;
		_device = ALC.CaptureOpenDevice(null, _sampleRate, _format, _sampleRate); // Buffer length set for 1 second for dynamic capture
		_buffer = new short[_sampleRate * 60]; // Initialize buffer to hold up to 60 seconds of audio
	}

	private void StartRecording()
	{
		_isRecording = true;
		this.Text = "●";
		this.ColorScheme = _recordingColorScheme;
		SetNeedsDisplay();

		_recordingThread = new Thread(() =>
		{
			ALC.CaptureStart(_device);

			totalSamplesCaptured = 0;
			while (_isRecording)
			{
				ALC.GetInteger(_device, AlcGetInteger.CaptureSamples, 1, out int samplesAvailable);
				if (samplesAvailable > 0)
				{
					int samplesToRead = Math.Min(samplesAvailable, _buffer.Length - totalSamplesCaptured);
					ALC.CaptureSamples(_device, ref _buffer[totalSamplesCaptured], samplesToRead);
					totalSamplesCaptured += samplesToRead;
				}
				Thread.Sleep(100); // Reduce CPU usage
			}
		})
		{ IsBackground = true };
		_recordingThread.Start();
	}

	private async Task StopRecording()
	{
		_isRecording = false;
		this.Text = "◯";
		this.ColorScheme = _notRecordingColorScheme;
		SetNeedsDisplay();

		// Wait for the recording thread to finish
		_recordingThread?.Join();

		ALC.CaptureStop(_device);

		var tempFileName = Path.GetTempFileName() + ".wav";

		SaveBufferToWav(_buffer, _sampleRate, tempFileName);

		await (OnRecordingStoppedAsync?.Invoke(tempFileName)).ConfigureAwait(false);
	}

	public override bool OnMouseEvent(MouseEvent me)
	{
		if ((me.Flags & MouseFlags.Button1Pressed) != 0 && !_isRecording)
		{
			StartRecording();
			return true;
		}
		else if ((me.Flags & MouseFlags.Button1Released) != 0 && _isRecording)
		{
			Task _ = StopRecording();
			return true;
		}

		return base.OnMouseEvent(me); // Call base method for unhandled events
	}

	public bool IsRecording()
	{
		return _isRecording;
	}

	private void SaveBufferToWav(short[] buffer, int sampleRate, string filePath)
	{
		using (var fileStream = new FileStream(filePath, FileMode.Create))
		using (var writer = new BinaryWriter(fileStream))
		{
			int byteRate = sampleRate * 2; // 16-bit mono
			int blockAlign = 2; // 16-bit mono
			int subchunk2Size = buffer.Length * 2; // 16-bit samples
			int chunkSize = 36 + subchunk2Size;

			// Write the WAV header
			writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
			writer.Write(chunkSize);
			writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
			writer.Write(new char[4] { 'f', 'm', 't', ' ' });
			writer.Write(16); // Subchunk1Size
			writer.Write((short)1); // AudioFormat (1 is PCM)
			writer.Write((short)1); // NumChannels (mono)
			writer.Write(sampleRate);
			writer.Write(byteRate);
			writer.Write((short)blockAlign);
			writer.Write((short)16); // BitsPerSample
			writer.Write(new char[4] { 'd', 'a', 't', 'a' });
			writer.Write(subchunk2Size);

			// Write the audio data
			for (int i = 0; i < totalSamplesCaptured; i++)
			{
				writer.Write(buffer[i]);
			}
		}
	}
}
