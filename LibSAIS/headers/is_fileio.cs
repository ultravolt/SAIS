using System;

namespace DigitalEeel
{
    public partial class FILE
    {

    }
    public static partial class SAIS
    {
        public static FILE myopen(string fname, string flags) { throw new NotImplementedException(); }
        //public static FILE myopen(char[] fname, char[] flags) { throw new NotImplementedException(); }
        public static int read_line(FILE _in, char[] out1, char[] out2) { throw new NotImplementedException(); }
        public static int read_line1(FILE _in, char[] out1) { throw new NotImplementedException(); }
        public static void ik_start_log() { throw new NotImplementedException(); }
        public static void ik_print_log(char[] ln, params object[] p) { throw new NotImplementedException(); }

        public static FILE logfile;
        public static int last_logdate;
        public static char[] moddir=new char[256];
    }
}