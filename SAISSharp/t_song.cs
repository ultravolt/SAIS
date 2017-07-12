namespace SAIS
{
    public partial class Game
    {
        public class t_song
        {

            public byte[] volseq = new byte[64];
            public byte[] panseq = new byte[64];
            public byte[] sync = new byte[4];
            public byte[] samp = new byte[4];
        }
    }
}