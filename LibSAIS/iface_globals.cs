
using System;

namespace DigitalEeel
{
    public static partial class SAIS
    {
        //#define MIN(x,y)     (((x) < (y)) ? (x) : (y))
        //#define MAX(x,y)     (((x) > (y)) ? (x) : (y))
        //#define ABS(x)			 (((x) > 0) ? (x) : (0-x))

        public const string SAIS_VERSION_NUMBER = "v1.5";

        //#define WINDOWED_MODE

        public class t_gamesettings
        {
            Int32 dif_nebula;
            Int32 dif_enemies;
            Int32 dif_ship;
            Int32 random_names;
            byte opt_timerwarnings;
            byte opt_mucrontext;
            byte opt_timeremaining;
            byte opt_mousemode;
            byte opt_smoketrails;
            byte opt_lensflares;
            Int16 opt_volume;
            char[] captname = new char[32];
            char[] shipname = new char[32];
        }

        public static t_gamesettings settings;

        // ******** GENERAL STUFF *******

        public static int my_main() { throw new NotImplementedException(); }
        public static int ik_eventhandler() { throw new NotImplementedException(); }
        public static int Game_Init(params object[] parms) { throw new NotImplementedException(); }
        public static int Game_Shutdown(params object[] parms) { throw new NotImplementedException(); }

        // inputs
        public static int key_pressed(int vk_code) { throw new NotImplementedException(); }  // FIXME: GET RID OF VK CODES!
        public static int ik_inkey() { throw new NotImplementedException(); }  // returns ascii
        public static void ik_showcursor() { throw new NotImplementedException(); }
        public static void ik_hidecursor() { throw new NotImplementedException(); }
        public static int ik_mclick() { throw new NotImplementedException(); } // returns flags when mbutton down

        // timers
        public static void start_ik_timer(int n, int f) { throw new NotImplementedException(); }
        public static void set_ik_timer(int n, int v) { throw new NotImplementedException(); }
        public static int get_ik_timer(int n) { throw new NotImplementedException(); }
        public static int get_ik_timer_fr(int n) { throw new NotImplementedException(); }

        // INTERFACE GLOBALS
        public static int ik_mouse_x;
        public static int ik_mouse_y;
        public static int ik_mouse_b;
        public static int ik_mouse_c;
        public static int must_quit;
        public static int wants_screenshot;

        public static int key_left;
        public static int key_right;
        public static int key_up;
        public static int key_down;
        public static int[] key_f = new int[10];
        public static int key_fire1;
        public static int key_fire2;
        public static int key_fire2b;

    }
}