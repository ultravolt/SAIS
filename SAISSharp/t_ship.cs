namespace SAIS
{
    public partial class Game
    {
        public class t_ship
        {
            public string name;
            public int type;
            public int hits;

            public int shld;
            public int shld_type;
            public int shld_time;
            public int shld_charge;

            public int damage_time;
            public int dmgc_type;
            public int dmgc_time;

            public int cpu_type;
            public int ecm_type;
            public int clo_type;

            public int sys_thru;
            public int sys_shld;
            public int sys_dmgc;
            public int sys_cpu;
            public int sys_ecm;
            public int sys_clo;

            public int speed, turn;

            public int[] wepfire = new int[8];
            public int[] syshits = new int[16];
            public int own;

            public int x, y, a;            // location
            public int vx, vy, va;     // movement

            public int ds_x, ds_y, ds_s;       // display x, y, size (for mouse clicking)
            public int wp_x, wp_y, escaped, wp_time, flee;
            public int patx, paty; // patrol

            public int cloaked, cloaktime; // cloaktime is last time you cloaked/decloaked

            public int tel_x, tel_y, teltime;  // teleportation of zorg

            public int active; // for spacehulk
            public int aistart;

            public int target;
            public int tac;
            public int angle;
            public int dist;

            public int launchtime; // for carrier
            public int frange;

            public int bong_start, bong_end;   // for babulon's bong artifact

        }
    }
}