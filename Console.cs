using System;

namespace SKSampleCatalog
{
    public static class Console
    {
        private static string input;
        private static Action<string, string> outputAction;
        private static Action<string, string> streamingOutputAction;

        private static string activeActor;

        public static void WriteLine(string message)
        {
            // Grab the name of the actor sending the message
            activeActor = message.Split('>')[0].Trim();

            // Grab the message content, including subsequent '>' characters
            message = message.Substring(activeActor.Length + 2).Trim();

            // If the message is empty, don't print it	
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            outputAction?.Invoke(message, activeActor);
        }

        public static void Write(string message)
        {
            // Check if message has ">" character
            if (message.Contains(" > "))
            {
                // Get the name of the actor sending the message
                activeActor = message.Split('>')[0].Trim();

                streamingOutputAction?.Invoke(null, activeActor);
                return;
            }

            streamingOutputAction?.Invoke(message, activeActor);
        }

        public static string ReadLine()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(100);
                if (input != null)
                {
                    var temp = input;
                    input = null;
                    return temp;
                }
            }
        }

        public static void CollectInput(string input)
        {
            Console.input = input;
        }

        public static void SetAddOutputChannel(Action<string, string> output)
        {
            outputAction = output;
        }

        public static void SetAppendOutputChannel(Action<string, string> streamingOutput)
        {
            streamingOutputAction = streamingOutput;
        }
    }
}