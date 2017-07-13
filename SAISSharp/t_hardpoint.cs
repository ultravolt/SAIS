namespace SAIS
{
    public class t_hardpoint
    {
        public byte type { get; set; }
        public byte size { get; set; }
        public byte x { get; set; }
        public byte y { get; set; }
        public short a { get; set; }        // angle
        public short f { get; set; }        // field of vision / fire
    }
}