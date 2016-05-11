// ----------------
//     INCLUDES
// ----------------

//#include <stdlib.h>
//#include <stdio.h>
//#include <string.h>
//#include <time.h>
//#include <math.h>

//#include "typedefs.h"
//#include "iface_globals.h"
//#include "is_fileio.h"
//#include "gfx.h"
//#include "interface.h"
//#include "starmap.h"
//#include "combat.h"

//#include "cards.h"

using System;
using System.Collections.Generic;

namespace DigitalEeel
{
    public static partial class SAIS
    {

        // ----------------
        //     CONSTANTS
        // ----------------

        public static string[] ecard_keywords = new string[]
        {
            "CARD",
            "NAME",
            "TEXT",
            "TEX2",
            "TYPE",
            "PARM",
            "END",
        };

        // ----------------
        // GLOBAL VARIABLES
        // ----------------

        public static List<t_eventcard> ecards = new List<t_eventcard>();
        public static Int32 num_ecards { get { return ecards.Count; } }

        // ----------------
        // LOCAL PROTOTYPES
        // ----------------

        // ----------------
        // GLOBAL FUNCTIONS
        // ----------------

        public static void cards_init()
        {
            FILE ini;
            char[] s1 = new char[64];
            char[] s2 = new char[256];
            char end;
            int num;
            int flag=0, n=0, com=0;
            int numtypes = 0;
            List<char[]> cardtypenames = new List<char[]>();

            ini = myopen("gamedata/cards.ini", "rb");
            if (ini == null)
                return;

            end = (char)0; num = 0; flag = 0;
            while (!AsBool(end))
            {
                end = (char)read_line(ini, s1, s2);
                if (!strcmp(s1, ecard_keywords[(int)ecard_keyids.eckBegin].ToCharArray()))
                    num++;
                else if (!strcmp(s1, "CARDTYPES".ToCharArray()))
                {
                    flag = 1; n = 0;
                }
                else if (flag == 1)
                {
                    if (!strcmp(s1, "END".ToCharArray()))
                    { numtypes = n; flag = 0; }
                    else

                        strcpy(cardtypenames[n++], s1);
                }
            }

            fclose(ini);

            //ecards = (t_eventcard*)calloc(num, sizeof(t_eventcard));
            if (ecards == null)
                return;
            //num_ecards = num;

            ini = myopen("gamedata/cards.ini", "rb");

            end = (char)0; num = 0; flag = 0;
            while (!AsBool(end))
            {
                end = (char)read_line(ini, s1, s2);
                com = -1;
                for (n = 0; n < (int)ecard_keyids.eckMax; n++)
                    if (!strcmp(s1, ecard_keywords[n].ToCharArray()))
                        com = n;

                if (flag == 0)
                {
                    if (com == (int)ecard_keyids.eckBegin)
                    {
                        flag = 1;

                        strcpy(ecards[num].text, "\0".ToCharArray());
                        ecards[num].parm = 0;
                    }
                }
                else switch (com)
                    {
                        case (int)ecard_keyids.eckName:

                            strcpy(ecards[num].name, s2);
                            break;

                        case (int)ecard_keyids.eckText:

                            strcpy(ecards[num].text, s2);
                            ecards[num].text[strlen(s2)] = (char)0;
                            break;

                        case (int)ecard_keyids.eckText2:

                            strcpy(ecards[num].text2, s2);
                            ecards[num].text2[strlen(s2)] = (char)0;
                            break;

                        case (int)ecard_keyids.eckType:
                            for (n = 0; n < numtypes; n++)
                                if (!strcmp(s2, cardtypenames[n]))
                                    ecards[num].type = n;
                            break;

                        case (int)ecard_keyids.eckParam:
                            if (ecards[num].type == (int)ecard_types.card_ally)
                            {
                                ecards[num].parm = 0;
                                for (n = 0; n < num_shiptypes; n++)
                                    if (!strcmp(s2, shiptypes[n].name))
                                        ecards[num].parm = n;
                            }
                            else if (ecards[num].type == (int)ecard_types.card_event)
                            {

                                sscanf(s2, "%d".ToCharArray(), n);
                                ecards[num].parm = n;
                            }
                            break;

                        case (int)ecard_keyids.eckEnd:
                            if ((ecards[num].type == (int)ecard_types.card_item) || (ecards[num].type == (int)ecard_types.card_rareitem) || (ecards[num].type == (int)ecard_types.card_lifeform))
                            {
                                for (n = 0; n < num_itemtypes; n++)
                                    if (!strcmp(ecards[num].name, itemtypes[n].name))
                                        ecards[num].parm = n;
                            }

                            num++; flag = 0;
                            break;

                        default:
                            break;
                    }

            }

            fclose(ini);
        }

        private static void sscanf(char[] s2, char[] v, int n)
        {
            throw new NotImplementedException();
        }
        private static void sscanf(string s2,  string v, int n)
        {
            throw new NotImplementedException();
        }
        private static bool strcmp(char[] s1, char[] v)
        {
            throw new NotImplementedException();
        }

        public static void cards_deinit()
        {
            //num_ecards = 0;
            //free(ecards);
        }

        public static void card_display(int n)
        {
            Int32 mc, c;
            Int32 end = 0;

            prep_screen();
            interface_drawborder(screen,
                                                     224, 112, 416, 368,
                                                     1, STARMAP_INTERFACE_COLOR, ecards[n].name);
            interface_textbox(screen, font_6x8,
                                                240, 136, 160, 224, 0,
                                                ecards[n].text);
            ik_blit();

            while (!must_quit && !AsBool(end))
            {
                ik_eventhandler();  // always call every frame
                mc = ik_mclick();
                c = ik_inkey();

                if (c == 13)
                    end = 1;
            }

        }
    }
}