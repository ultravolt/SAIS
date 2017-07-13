namespace SAIS
{
    // enemy "fleet"
    public class t_fleet
    {
        public int system;
        public int target;
        public int enroute;
        public int distance;
        public int num_ships;
        public int[] ships = new int[16];
        public int race;
        public int explored;
        public int blowtime;
    }
}