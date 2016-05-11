// ----------------
//     TYPEDEFS
// ----------------
using System;
using System.Collections.Generic;

namespace DigitalEeel
{
    public static partial class SAIS
    {
        public class t_eventcard
        {
            public char[] name = new char[32];
            public char[] text = new char[256];
            public char[] text2 = new char[256];
            public Int32 type;
            public Int32 parm;
        }

        public enum ecard_keyids
        {
            eckBegin,
            eckName,
            eckText,
            eckText2,
            eckType,
            eckParam,
            eckEnd,
            eckMax,
        };

        public enum ecard_types
        {
            card_event,
            card_ally,
            card_item,
            card_rareitem,
            card_lifeform,
            card_max,
        };

        
        // ----------------
        //    PROTOTYPES
        // ----------------

        //public static void cards_init() { throw new NotImplementedException(); }
        //public static void cards_deinit() { throw new NotImplementedException(); }
        //public static void card_display(int n) { throw new NotImplementedException(); }

    }
}