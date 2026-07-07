namespace ModularVehicleSimulator.Vehicle
{
    public enum Gear
    {
        Reverse = -1,
        Park = 0,
        Neutral = 1,
        Drive = 2,
        Second = 3,
        Third = 4,
        Fourth = 5,
        Fifth = 6
    }

    public static class GearExtensions
    {
        public static string ToLetter(this Gear gear)
        {
            switch (gear)
            {
                case Gear.Reverse: 
                    return "R";
                case Gear.Park: 
                    return "P";
                case Gear.Neutral: 
                    return "N";
                case Gear.Drive: 
                    return "D";
                case Gear.Second: 
                    return "2";
                case Gear.Third: 
                    return "3";
                case Gear.Fourth:
                    return "4";
                case Gear.Fifth:
                    return "5";
                default:
                    return gear.ToString()[0].ToString();
            }
            
        }
    }
}
