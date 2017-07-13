namespace SAIS
{
    public class t_wepbeam
    {
        public t_shipweapon wep;
        public t_ship src;
        public t_ship dst;

        public int stg;                    // if staged from a projectile
        public int ang, len;           // angle and length if missed
        public int stp, dsp;           // start, destination hardpoint (-1 = hull / shield)
        public int str, dmt, end;          // start, damage, expire time
    }
}