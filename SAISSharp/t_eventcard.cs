namespace SAIS
{
    public partial class Game
    {
        // ----------------
        //     TYPEDEFS
        // ----------------

        public class t_eventcard
        {
            public string name { get; set; }//[32];
            public string text { get; set; }//[256];
            public string text2 { get; set; }//[256];
            public ecard_types type { get; set; }//
            public int parm { get; set; }//
        }
    }
}