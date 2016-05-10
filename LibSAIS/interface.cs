// ----------------
//    CONSTANTS
// ----------------

// for interface.cpp / interface_initsprites()
using System;

namespace DigitalEeel
{
    public static partial class SAIS
    {
        public const int IF_BORDER_TRANS = 0;
        public const int IF_BORDER_SOLID = 9;
        public const int IF_BORDER_PORTRAIT = 18;
        public const int IF_BORDER_RADAR = 19;
        public const int IF_BORDER_FLAT = 20;
        public const int IF_BORDER_SMALL = 21;

        enum tutorial_pages
        {
            tut_starmap = 0,
            tut_explore,
            tut_upgrade,
            tut_device,
            tut_treasure,
            tut_ally,
            tut_encounter,
            tut_combat,
            tut_trading,
            tut_max,
        };

        // ----------------
        //     TYPEDEFS
        // ----------------

        // ----------------
        // GLOBAL VARIABLES
        // ----------------

        public static t_ik_spritepak spr_titles;

        public static t_ik_spritepak spr_IFborder;
        public static t_ik_spritepak spr_IFbutton;
        public static t_ik_spritepak spr_IFslider;
        public static t_ik_spritepak spr_IFarrows;
        public static t_ik_spritepak spr_IFsystem;
        public static t_ik_spritepak spr_IFtarget;
        public static t_ik_spritepak spr_IFdifnebula;
        public static t_ik_spritepak spr_IFdifenemy;

        public static t_ik_font font_4x8;
        public static t_ik_font font_6x8;

        // ----------------
        //    PROTOTYPES
        // ----------------

        public static void interface_init() { throw new NotImplementedException(); }
        public static void interface_deinit() { throw new NotImplementedException(); }

        public static void interface_drawborder(t_ik_image img,
                                                            Int32 left, Int32 top, Int32 right, Int32 bottom,
                                                            Int32 fill, Int32 color,
                                                            char[] title)
        { throw new NotImplementedException(); }
        public static void interface_thinborder(t_ik_image img,
                                                            Int32 left, Int32 top, Int32 right, Int32 bottom,
                                                            Int32 color, Int32 fill = -1)
        { throw new NotImplementedException(); }
        public static Int32 interface_textbox(t_ik_image img, t_ik_font fnt,
                                                     Int32 left, Int32 top, Int32 w, Int32 h,
                                                     Int32 color,
                                                     char[] text)
        { throw new NotImplementedException(); }
        public static Int32 interface_textboxsize(t_ik_font fnt,
                                                                Int32 w, Int32 h,
                                                                char[] text)
        { throw new NotImplementedException(); }
        public static Int32 interface_popup(t_ik_font fnt,
                                                 Int32 left, Int32 top, Int32 w, Int32 h,
                                                 Int32 co1, Int32 co2,
                                                 char[] label, char[] text,
                                                 char[] button1 = null, char[] button2 = null, char[] button3 = null)
        { throw new NotImplementedException(); }
        public static void interface_drawslider(t_ik_image img, Int32 left, Int32 top, Int32 a, Int32 l, Int32 rng, Int32 val, Int32 color)
        { throw new NotImplementedException(); }
        public static void interface_drawbutton(t_ik_image img, Int32 left, Int32 top, Int32 l, Int32 color, char[] text)
        { throw new NotImplementedException(); }

        public static void interface_cleartuts() { throw new NotImplementedException(); }
        public static void interface_tutorial(int n) { throw new NotImplementedException(); }
    }
}