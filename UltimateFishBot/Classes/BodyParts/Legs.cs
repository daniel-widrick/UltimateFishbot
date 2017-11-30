using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using UltimateFishBot.Classes.Helpers;
using UltimateFishBot.Properties;

namespace UltimateFishBot.Classes.BodyParts
{
    public class Legs
    {
        public enum Path
        {
            FrontBack = 0,
            LeftRight = 1,
            Jump      = 2
        }

        public async Task DoMovement(TextToSpeech textToSpeech, CancellationToken cancellationToken)
        {
            switch ((Path)Settings.Default.AntiAfkMoves)
            {
                case Path.FrontBack:
                    await MovePath(new[] { Keys.Up, Keys.Down }, cancellationToken);
                    break;
                case Path.Jump:
                    await MovePath(new[] { Keys.Space }, cancellationToken);
                    break;
                default:
                    await MovePath(new[] { Keys.Left, Keys.Right }, cancellationToken);
                    break;
            }

            textToSpeech?.Say("Anti A F K");
        }

        private async Task MovePath(IEnumerable<Keys> moves, CancellationToken cancellationToken)
        {
            foreach (var move in moves)
            {
                await SingleMove(move, cancellationToken);
                await Task.Delay(250, cancellationToken);
            }
        }

        private async Task SingleMove(Keys move, CancellationToken cancellationToken)
        {
            Win32.SendKeyboardAction(move, Win32.KeyState.Keydown);
            await Task.Delay(250, cancellationToken);
            Win32.SendKeyboardAction(move, Win32.KeyState.Keyup);
        }
    }
}
