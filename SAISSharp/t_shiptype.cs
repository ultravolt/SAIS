namespace SAIS
{
    public class t_shiptype
    {
        public string name { get; set; }
        public int race { get; set; }
        public int flag { get; set; }
        public int hull { get; set; }
        //	int shield { get; set; }
        public int hits { get; set; }
        public int engine { get; set; }
        public int thrust { get; set; }
        public int speed { get; set; }
        public int turn { get; set; }
        public int sensor { get; set; }
        public int num_systems { get; set; }
        public int sys_eng { get; set; }
        public int sys_thru { get; set; }
        //	int weapon[8];
        public short[] system = new short[16];
        public short[] sysdmg = new short[16];
    }
}