namespace ModularVehicleSimulator.Vehicle
{
    public enum Gear
    {
        Park = 0,
        Reverse = 1,
        Neutral = 2,
        Drive = 3,
        Second = 4,
        Third = 5,
        Fourth = 6,
        Fifth = 7
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
