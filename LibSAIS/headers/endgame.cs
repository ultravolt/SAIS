// ----------------
//    CONSTANTS
// ----------------

// ----------------
//     TYPEDEFS
// ----------------
using System;

namespace DigitalEeel
{
    public static partial class SAIS
    {
        public class t_job
        {
            public char[] name = new char[64];
            public Int32 value;
        }

        public class t_score
        {
            public char[] cname=new char[16];
            public char[] sname=new char[16];
            public char[] deathmsg=new char[64];
            public Int32 score;
            public Int32 date;
        }

        // ----------------
        // GLOBAL VARIABLES
        // ----------------

        public static t_job jobs;
        public static Int32 num_jobs;

        public static Int32 num_scores;
        public static t_score[] scores=new t_score[20];

        public static Int32 got_hiscore;

        // ----------------
        //    PROTOTYPES
        // ----------------

        public static void game_over() { throw new NotImplementedException(); }

        public static void endgame_init() { throw new NotImplementedException(); }
        public static void endgame_deinit() { throw new NotImplementedException(); }
    }
}