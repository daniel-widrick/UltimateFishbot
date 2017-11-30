using System;

namespace UltimateFishBot.Classes.BodyParts
{
    public class Mouth
    {
        private readonly IProgress<string> _progressHandle;
        private readonly TextToSpeech _textToSpeech = new TextToSpeech();

        public Mouth(IProgress<string> progressHandle)
        {
            _progressHandle = progressHandle;
        }

        public void Say(string text)
        {
            _progressHandle.Report(text);
            _textToSpeech.Say(text);
        }
    }
}
