namespace SAIS
{
    public partial class Game
    {
        public class t_ik_image
        {

            public int w, h;       // size
            public int pitch;  // how many bytes per hline
            public byte[] data;    // linear bitmap
        }
    }
}