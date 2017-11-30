using System.Speech.Synthesis;

namespace UltimateFishBot.Classes.BodyParts
{
    public class TextToSpeech
    {
        readonly SpeechSynthesizer _synthesizer = new SpeechSynthesizer { Volume = 60, Rate = 1 };
        readonly bool _useTextToSpeech = Properties.Settings.Default.Txt2speech;
        private string _lastMessage;
        
        public void Say(string message)
        {
            // Say asynchronous message through Text 2 Speech synthesizer
            if (_useTextToSpeech && _lastMessage != message && _synthesizer.State == SynthesizerState.Ready)
            {
                _synthesizer.SpeakAsync(message);
                _lastMessage = message;
            }
        }
    }
}