namespace UltimateFishBot.Classes
{
    public class FishingStats
    {
        public int TotalSuccessFishing { get; private set; }
        public int TotalNotFoundFish { get; private set; }
        public int TotalNotHeardFish { get; private set; }

        public void Reset()
        {
            TotalSuccessFishing = 0;
            TotalNotFoundFish   = 0;
            TotalNotHeardFish   = 0;
        }

        public void RecordSuccess()
        {
            TotalSuccessFishing++;
        }

        public void RecordBobberNotFound()
        {
            TotalNotFoundFish++;
        }

        public void RecordNotHeard()
        {
            TotalNotHeardFish++;
        }

        public int Total()
        {
            return TotalSuccessFishing + TotalNotFoundFish + TotalNotHeardFish;
        }
    }
}
