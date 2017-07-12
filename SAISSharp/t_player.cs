namespace SAIS
{
    public partial class Game
    {
        public class t_player
        {
            public string captname;//[32];
            public string shipname;//[32];
            public string deathmsg;//[64];

            public int x, y, a;
            public int system;
            public int target;

            public int distance;
            public int nebula;
            public int enroute;

            public int engage;
            public int fold;
            public int hypdate;
            public int foldate;
            public int hyptime;

            public int explore;
            public int stardate;
            public int death;
            public int deatht;
            public int hole;

            public int num_ships;
            public int[] ships = new int[8];
            public int sel_ship;
            public int sel_ship_time;

            public int[] items = new int[32];
            public int[] itemflags = new int[32];
            public int num_items;
            public int bonusdata;
        }
    }
}