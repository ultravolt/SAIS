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
            char[] name = new char[32];
            char[] text = new char[256];
            char[] text2 = new char[256];
            Int32 type;
            Int32 parm;
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
        // GLOBAL VARIABLES
        // ----------------

        public static List<t_eventcard> ecards = new List<t_eventcard>();
        public static Int32 num_ecards { get { return ecards.Count; } }

        // ----------------
        //    PROTOTYPES
        // ----------------

        public static void cards_init() { throw new NotImplementedException(); }
        public static void cards_deinit() { throw new NotImplementedException(); }
        public static void card_display(int n) { throw new NotImplementedException(); }

    }
}