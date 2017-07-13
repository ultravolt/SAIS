namespace SAIS
{
    public class t_wepproj
    {
        public t_shipweapon wep;
        public t_ship src;
        public t_ship dst;                // target (for missiles)

        public int str, end;           // start, expire time

        public int x, y, a;            // location
        public int vx, vy, va;     // movement
        public int hits;                   // used for dispersing weapons
    }
}