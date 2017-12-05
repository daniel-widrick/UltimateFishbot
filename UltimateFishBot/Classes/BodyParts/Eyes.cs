using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using UltimateFishBot.Classes.Helpers;
using UltimateFishBot.Properties;

namespace UltimateFishBot.Classes.BodyParts
{
    public class NoFishFoundException : Exception
    {
    }

    public class Eyes
    {
        int _xPosMin;
        int _xPosMax;
        int _yPosMin;
        int _yPosMax;
        Rectangle _wowRectangle;
        private Win32.CursorInfo _mNoFishCursor;

        public async Task<bool> LookForBobber(CancellationToken cancellationToken)
        {
            _mNoFishCursor = Win32.GetNoFishCursor();
            _wowRectangle = Win32.GetWowRectangle();

            if (!Settings.Default.customScanArea)
            {
                _xPosMin = _wowRectangle.Width / 4;
                _xPosMax = _xPosMin * 3;
                _yPosMin = _wowRectangle.Height / 4;
                _yPosMax = _yPosMin * 3;
                Console.Out.WriteLine("Using default area");
            }
            else
            {
                _xPosMin = Settings.Default.minScanXY.X;
                _yPosMin = Settings.Default.minScanXY.Y;
                _xPosMax = Settings.Default.maxScanXY.X;
                _yPosMax = Settings.Default.maxScanXY.Y;
                Console.Out.WriteLine("Using custom area");
            }
            Console.Out.WriteLine("Scanning area: " + _xPosMin + " , " + _yPosMin + " , " + _xPosMax + " , " +
                                  _yPosMax + " , ");
            try
            {

                if (Settings.Default.AlternativeRoute)
                    await LookForBobberSpiralImpl(cancellationToken);
                else
                    await LookForBobberImpl(cancellationToken);

                // Found the fish!
                return true;
            }
            catch (NoFishFoundException)
            {
                // Didn't find the fish
                return false;
            }

        }

        private async Task LookForBobberImpl(CancellationToken cancellationToken)
        {
            int xposstep = (_xPosMax - _xPosMin) / Settings.Default.ScanningSteps;
            int yposstep = (_yPosMax - _yPosMin) / Settings.Default.ScanningSteps;
            int xoffset = xposstep / Settings.Default.ScanningRetries;

            bool heardFish = false;

            AsyncEars.Instance.HeardFish += HeardFish;

            void HeardFish(object sender, EventArgs e)
            {
                heardFish = true;
            }

            AsyncEars.Instance.StartListening();

            try
            {
                for (int tryCount = 0; tryCount < Settings.Default.ScanningRetries; ++tryCount)
                {
                    if (Settings.Default.customScanArea)
                    {
                        for (int x = _xPosMin + (xoffset * tryCount); x < _xPosMax; x += xposstep)
                        {
                            for (int y = _yPosMin; y < _yPosMax; y += yposstep)
                            {
                                if (heardFish) // Abort looking if the fish splashes before the bobber is found
                                    throw new NoFishFoundException();

                                if (await MoveMouseAndCheckCursor(x, y, cancellationToken))
                                    return;
                            }
                        }
                    }
                    else
                    {
                        for (int x = _xPosMin + (xoffset * tryCount); x < _xPosMax; x += xposstep)
                        {
                            for (int y = _yPosMin; y < _yPosMax; y += yposstep)
                            {
                                if (heardFish) // Abort looking if the fish splashes before the bobber is found
                                    throw new NoFishFoundException();

                                if (await MoveMouseAndCheckCursor(_wowRectangle.X + x, _wowRectangle.Y + y,
                                    cancellationToken))
                                    return;
                            }
                        }

                    }
                }
            }
            finally
            {
                AsyncEars.Instance.HeardFish -= HeardFish;
            }

            throw new NoFishFoundException();
        }

        private async Task LookForBobberSpiralImpl(CancellationToken cancellationToken)
        {

            int xposstep = (_xPosMax - _xPosMin) / Settings.Default.ScanningSteps;
            int yposstep = (_yPosMax - _yPosMin) / Settings.Default.ScanningSteps;
            int xoffset = xposstep / Settings.Default.ScanningRetries;
            int yoffset = yposstep / Settings.Default.ScanningRetries;

            bool heardFish = false;

            AsyncEars.Instance.HeardFish += HeardFish;

            void HeardFish(object sender, EventArgs e)
            {
                heardFish = true;
            }

            AsyncEars.Instance.StartListening();

            try
            {
                if (Settings.Default.customScanArea)
                {
                    for (int tryCount = 0; tryCount < Settings.Default.ScanningRetries; tryCount++)
                    {
                        int x = (_xPosMin + _xPosMax) / 2 + xoffset * tryCount;
                        int y = (_yPosMin + _yPosMax) / 2 + yoffset * tryCount;

                        for (int i = 0; i <= 2 * Settings.Default.ScanningSteps; i++)
                        {
                            for (int j = 0; j <= (i / 2); j++)
                            {
                                int dx = 0, dy = 0;

                                if (i % 2 == 0)
                                {
                                    if ((i / 2) % 2 == 0)
                                    {
                                        dx = xposstep;
                                        dy = 0;
                                    }
                                    else
                                    {
                                        dx = -xposstep;
                                        dy = 0;
                                    }
                                }
                                else
                                {
                                    if ((i / 2) % 2 == 0)
                                    {
                                        dx = 0;
                                        dy = yposstep;
                                    }
                                    else
                                    {
                                        dx = 0;
                                        dy = -yposstep;
                                    }
                                }

                                x += dx;
                                y += dy;

                                if (heardFish) // Abort looking if the fish splashes before the bobber is found
                                    throw new NoFishFoundException();

                                if (await MoveMouseAndCheckCursor(x, y, cancellationToken))
                                    return;
                            }
                        }
                    }
                }
                else
                {
                    for (int tryCount = 0; tryCount < Settings.Default.ScanningRetries; ++tryCount)
                    {
                        int x = (_xPosMin + _xPosMax) / 2 + xoffset * tryCount;
                        int y = (_yPosMin + _yPosMax) / 2 + yoffset * tryCount;

                        for (int i = 0; i <= 2 * Settings.Default.ScanningSteps; i++)
                        {
                            for (int j = 0; j <= (i / 2); j++)
                            {
                                int dx = 0, dy = 0;

                                if (i % 2 == 0)
                                {
                                    if ((i / 2) % 2 == 0)
                                    {
                                        dx = xposstep;
                                        dy = 0;
                                    }
                                    else
                                    {
                                        dx = -xposstep;
                                        dy = 0;
                                    }
                                }
                                else
                                {
                                    if ((i / 2) % 2 == 0)
                                    {
                                        dx = 0;
                                        dy = yposstep;
                                    }
                                    else
                                    {
                                        dx = 0;
                                        dy = -yposstep;
                                    }
                                }

                                x += dx;
                                y += dy;

                                if (heardFish) // Abort looking if the fish splashes before the bobber is found
                                    throw new NoFishFoundException();

                                if (await MoveMouseAndCheckCursor(_wowRectangle.X + x, _wowRectangle.Y + y,
                                    cancellationToken))
                                    return;
                            }
                        }
                    }
                }
            }
            finally
            {
                AsyncEars.Instance.HeardFish -= HeardFish;
            }

            throw new NoFishFoundException();
        }

        private async Task<bool> MoveMouseAndCheckCursor(int x, int y, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new TaskCanceledException();

            Win32.MoveMouse(x, y);

            // Pause (give the OS a chance to change the cursor)
            await Task.Delay(Settings.Default.ScanningDelay, cancellationToken);

            Win32.CursorInfo actualCursor = Win32.GetCurrentCursor();

            if (actualCursor.flags == _mNoFishCursor.flags &&
                actualCursor.hCursor == _mNoFishCursor.hCursor)
                return false;

            // Compare the actual icon with our fishIcon if user want it
            if (Settings.Default.CheckCursor &&
                !ImageCompare(Win32.GetCursorIcon(actualCursor), Resources.fishIcon35x35))
                return false;

            // We found a fish !
            await Task.Delay(300);
            return true;
        }

        private bool ImageCompare(Bitmap firstImage, Bitmap secondImage)
        {
            if (firstImage.Width != secondImage.Width || firstImage.Height != secondImage.Height)
                return false;

            for (int i = 0; i < firstImage.Width; i++)
                for (int j = 0; j < firstImage.Height; j++)
                    if (firstImage.GetPixel(i, j).ToString() != secondImage.GetPixel(i, j).ToString())
                        return false;

            return true;
        }
    }
}