//#define MOVIE

// ******** GRAPHICS *********

// GFX DATATYPES

using System;
using System.Collections.Generic;

namespace DigitalEeel
{
    public static partial class SAIS
    {
        public class t_paletteentry
        {
            byte r;
            byte g;
            byte b;
            byte a;
        }

        public class t_ik_image
        {
            Int32 w, h;     // size
            Int32 pitch;  // how many bytes per hline
            byte[] data; // linear bitmap
        }

        public class t_ik_sprite
        {
            Int32 w, h;    // size
            Int32 co;     // average color
            byte[] data;    // linear bitmap 
        }

        public class t_ik_spritepak
        {
            Int32 num;
            List<t_ik_sprite> spr;
        }

        public class t_ik_font {
            Int16 w, h;     // size
            byte[] data;    // linear bitmap 
        };

        // GFX GLOBALS

        public static t_ik_image screen;
        public static byte[] globalpal = new byte[768];
        public static byte[] currentpal=new byte[768];
        public static t_ik_image screenbuf;
        public static int gfx_width, gfx_height, gfx_fullscreen, gfx_switch;
        public static int gfx_redraw;
        public static int c_minx, c_miny, c_maxx, c_maxy;

        public static char[] gfx_transbuffer;
        public static char[] gfx_lightbuffer;
        public static char[] gfx_addbuffer;

        public static Int32[] sin1k = new Int32[1024];
        public static Int32[] cos1k = new Int32[1024];

        // load, generate or delete images
        public static t_ik_image new_image(Int32 w, Int32 h) { throw new NotImplementedException(); }
        public static void del_image(t_ik_image img) { throw new NotImplementedException(); }
        public static t_ik_image ik_load_pcx(char[] fname, byte[] pal) { throw new NotImplementedException(); }
        public static t_ik_image ik_load_tga(char[] fname, byte[] pal) { throw new NotImplementedException(); }
        public static void ik_save_screenshot(t_ik_image img, byte[] pal) { throw new NotImplementedException(); }
        public static void ik_save_tga(char[] fname, t_ik_image img, byte[] pal) { throw new NotImplementedException(); }

        // input/output
        public static void ik_setclip(Int32 left, Int32 top, Int32 right, Int32 bottom) { throw new NotImplementedException(); }
        public static void ik_putpixel(t_ik_image img, Int32 x, Int32 y, Int32 c) { throw new NotImplementedException(); }
        public static Int32 ik_getpixel(t_ik_image img, Int32 x, Int32 y) { throw new NotImplementedException(); }
        public static byte[] ik_image_pointer(t_ik_image img, Int32 x, Int32 y) { throw new NotImplementedException(); }
        public static void ik_drawline(t_ik_image img, Int32 xb, Int32 yb, Int32 xe, Int32 ye, Int32 c1, Int32 c2 = 0, byte mask = 255, byte fx = 0) { throw new NotImplementedException(); }
        public static void ik_drawbox(t_ik_image img, Int32 xb, Int32 yb, Int32 xe, Int32 ye, Int32 c) { throw new NotImplementedException(); }
        public static void ik_copybox(t_ik_image src, t_ik_image dst, Int32 xb, Int32 yb, Int32 xe, Int32 ye, Int32 xd, Int32 yd) { throw new NotImplementedException(); }
        public static void ik_drawmeter(t_ik_image img, Int32 xb, Int32 yb, Int32 xe, Int32 ye, Int32 typ, Int32 val, Int32 c, Int32 c2) { throw new NotImplementedException(); }
        public static void ik_draw_mousecursor() { throw new NotImplementedException(); }
        public static void gfx_blarg() { throw new NotImplementedException(); }
        public static void gfx_magnify() { throw new NotImplementedException(); }

        // screen blits & other management
        public static void prep_screen() { throw new NotImplementedException(); } // call before drawing stuff to *screen
        public static void free_screen() { throw new NotImplementedException(); } // call after drawing, before blit
        public static void ik_blit() { throw new NotImplementedException(); }         // blit from memory to hardware
        public static int gfx_checkswitch() { throw new NotImplementedException(); }  // check for gfx mode switch
        public static void halfbritescreen() { throw new NotImplementedException(); }
        public static void reshalfbritescreen() { throw new NotImplementedException(); }
        public static void resallhalfbritescreens() { throw new NotImplementedException(); }

        // palette handling
        public static void update_palette() { throw new NotImplementedException(); }  // blit palette entries to hardware
        public static void set_palette_entry(int n, int r, int g, int b) { throw new NotImplementedException(); }
        public static int get_palette_entry(int n) { throw new NotImplementedException(); }
        public static Int32 get_rgb_color(Int32 r, Int32 g, Int32 b) { throw new NotImplementedException(); }
        public static void calc_color_tables(byte[] pal) { throw new NotImplementedException(); }
        public static void del_color_tables() { throw new NotImplementedException(); }

        // misc
        public static int get_direction(Int32 dx, Int32 dy) { throw new NotImplementedException(); }
        public static int get_distance(Int32 dx, Int32 dy) { throw new NotImplementedException(); }

        public static void gfx_initmagnifier() { throw new NotImplementedException(); }
        public static void gfx_deinitmagnifier() { throw new NotImplementedException(); }



        // ------------------------
        //         FONT.CPP
        // ------------------------

        public static t_ik_font ik_load_font(char[] fname, byte w, byte h) { throw new NotImplementedException(); }
        public static void ik_del_font(t_ik_font fnt) { throw new NotImplementedException(); }

        public static void ik_print(t_ik_image img, t_ik_font fnt, Int32 x, Int32 y, byte co, char[] ln, params object[] p) { throw new NotImplementedException(); }
        public static void ik_printbig(t_ik_image img, t_ik_font fnt, Int32 x, Int32 y, byte co, char[] ln, params object[] p) { throw new NotImplementedException(); }
        //void ik_text_input(int x, int y, int l, t_ik_font *fnt, char *tx);
        public static void ik_text_input(int x, int y, int l, t_ik_font fnt, char[] pmt, char[] tx, int bg = 0, int co = 0) { throw new NotImplementedException(); }
        public static void ik_hiscore_input(int x, int y, int l, t_ik_font fnt, char[] tx) { throw new NotImplementedException(); }

        // ------------------------
        //      SPRITES.CPP
        // ------------------------

        // sprite management
        public static t_ik_sprite new_sprite(Int32 w, Int32 h) { throw new NotImplementedException(); }
        public static void free_sprite(t_ik_sprite spr) { throw new NotImplementedException(); }

        public static t_ik_sprite get_sprite(t_ik_image img, Int32 x, Int32 y, Int32 w, Int32 h) { throw new NotImplementedException(); }
        public static Int32 calc_sprite_color(t_ik_sprite spr) { throw new NotImplementedException(); }

        public static t_ik_spritepak new_spritepak(Int32 num) { throw new NotImplementedException(); }
        public static void free_spritepak(t_ik_spritepak pak) { throw new NotImplementedException(); }

        public static t_ik_spritepak load_sprites(char[] fname) { throw new NotImplementedException(); }
        public static void save_sprites(char[] fname, t_ik_spritepak pak) { throw new NotImplementedException(); }


        // sprite drawing
        public static void ik_dsprite(t_ik_image img, Int32 x, Int32 y, t_ik_sprite spr, Int32 flags = 0) { throw new NotImplementedException(); }
        public static void ik_drsprite(t_ik_image img, Int32 x, Int32 y, Int32 r, Int32 s, t_ik_sprite spr, Int32 flags = 0) { throw new NotImplementedException(); }
        public static void ik_dspriteline(t_ik_image img, Int32 xb, Int32 yb, Int32 xe, Int32 ye, Int32 s,
                                                Int32 offset, Int32 ybits, t_ik_sprite spr, Int32 flags = 0)
        { throw new NotImplementedException(); }
    }
}