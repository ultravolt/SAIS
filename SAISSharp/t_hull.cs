namespace SAIS
{
    public partial class Game
    {
        public class t_hull
        {
            public string name { get; set; }//[32];
            public int size { get; set; }           // length in meters
            public int hits { get; set; }
            public int mass { get; set; }
            public int numh { get; set; }           // number of hardpoints
            public t_ik_sprite sprite { get; set; }
            public t_ik_sprite silu { get; set; }
            public t_hardpoint[] hardpts = new t_hardpoint[32];
        }
    }
}