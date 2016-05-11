using System;
using System.Collections.Generic;
// ----------------
//     INCLUDES
// ----------------

//#include <stdio.h>
//#include <stdlib.h>
//#include <stdarg.h>
//#include <memory.h>
//#include <math.h>
//#include <time.h>
//#include <string.h>
//#include <malloc.h>
//#include <io.h>

//#include "typedefs.h"
//#include "iface_globals.h"
//#include "gfx.h"
//#include "is_fileio.h"
//#include "snd.h"

namespace DigitalEeel
{
    public static partial class SAIS
    {


        //		FILE *loggy;

        // --------------------------------------------
        //      SPRITE CREATION AND MANAGEMENT
        // --------------------------------------------

        // CREATE NEW SPRITE TEMPLATE
        public static t_ik_sprite new_sprite(Int32 w, Int32 h)
        {
            t_ik_sprite spr;

            spr = new t_ik_sprite();
            if (spr == null)
                return spr;

            spr.data = new byte[w * h];
            spr.w = w;
            spr.h = h;
            spr.co = 0;

            return spr;
        }

        // FIND APPROXIMATE COLOR OF SPRITE
        public static Int32 calc_sprite_color(t_ik_sprite spr)
        {
            Int32 x, y, r, g, b, npx, c;

            if (spr == null)
                return 0;

            r = 0; g = 0; b = 0; npx = 0;
            for (y = 0; y < spr.h; y++)
                for (x = 0; x < spr.w; x++)
                {
                    c = spr.data[y * spr.w + x];
                    if (AsBool(c))
                    {
                        c = get_palette_entry(c);
                        r += (c >> 16) & 255; g += (c >> 8) & 255; b += c & 255;
                        //			r+=pal[c*3]; g+=pal[c*3+1]; b+=pal[c*3+2];
                        npx++;
                    }
                }

            if (npx > 0)
            {
                spr.co = get_rgb_color(r / npx, g / npx, b / npx);
            }

            return spr.co;
        }

        // GRAB SPRITE FROM IMAGE
        public static t_ik_sprite get_sprite(t_ik_image img, Int32 x, Int32 y, Int32 w, Int32 h)
        {
            t_ik_sprite spr;
            Int32 x1, y1;

            if (img == null)
                return null;

            spr = new_sprite(w, h);
            if (spr == null)
                return null;

            for (y1 = 0; y1 < h; y1++)
                for (x1 = 0; x1 < w; x1++)
                    if (y1 + y >= 0 && y1 + y < img.h && x1 + x >= 0 && x1 + x < img.w)
                    {
                        spr.data[y1 * w + x1] = img.data[(y1 + y) * img.w + (x1 + x)];
                    }
                    else
                        spr.data[y1 * w + x1] = 0;

            calc_sprite_color(spr);

            return spr;
        }

        // DESTROY SPRITE AND FREE MEMORY
        public static void free_sprite(t_ik_sprite spr)
        {
            if (spr != null)
            {
                spr.Dispose();
                /*
                if (spr.data!=null)
                    free(spr.data);
                free(spr);
                */
            }
        }

        // --------------------------------------------
        //     SPRITEPAK CREATION AND MANAGEMENT
        // --------------------------------------------

        public static t_ik_spritepak new_spritepak(Int32 num)
        {
            t_ik_spritepak pak;

            pak = new t_ik_spritepak();
            if (pak == null)
                return null;

            pak.num = num;
            pak.spr = new List<t_ik_sprite>(num);

            return pak;
        }

        public static void free_spritepak(t_ik_spritepak pak)
        {
            int x;

            if (pak == null)
                return;

            for (x = 0; x < pak.num; x++)
            {
                free_sprite(pak.spr[x]);
                pak.spr[x] = null;
            }
            //free(pak.spr);
            //free(pak);
        }

        public static t_ik_spritepak load_sprites(string filename)
        {
            char[] fname = filename.ToCharArray();
            // NOTE: load_sprites loads default .SPR, and FRAMES from the mod
            _finddata_t find = null;
            long fhandle;
            long fi;
            int fnum=0;
            char[] spritedir = new char[256];
            char[] framename = new char[256];
            int[] rep = new int[256];
            int max;
            t_ik_image img;
            byte[] buffu;

            FILE fil;
            t_ik_spritepak pak;
            Int32 x, num;
            Int32 w, h, c;

            /*
            if loading a mod, look at the folder for new frames
            and mark them in the replacement array.
            */
            for (x = 0; x < 256; x++)
                rep[x] = 0;
            max = 0;
            if (AsBool(strlen(moddir)))
            {

                //sprintf(spritedir, "%s%s".ToCharArray(), moddir, fname);

                //sprintf(spritedir+strlen(spritedir)-4, "/\0");

                //sprintf(framename, "%sframe*.tga", spritedir);

                fhandle = _findfirst(framename, find);
                if (fhandle != -1)
                {
                    fi = fhandle;
                    while (fi != -1)
                    {

                        sscanf(find.name + 5, "%03d".ToCharArray(), fnum);
                        if (fnum < 256)
                        {
                            rep[fnum] = 1;
                            if (fnum + 1 > max)
                                max = fnum + 1;
                        }
                        fi = _findnext(fhandle, find);
                    }

                    _findclose(fhandle);
                }
            }

            fil = fopen(fname, "rb".ToCharArray());   // don't use myopen here
            if (fil==null)
                return null;

            num = fgetc(fil);
            num += fgetc(fil) * 256;

            if (num > max)
                max = num;

            pak = new_spritepak(max);
            if (pak==null)
            { fclose(fil); return null; }

            for (x = 0; x < max; x++)
            {
                // header
                if (x < num)
                {
                    w = fgetc(fil);
                    w += fgetc(fil) * 256;
                    h = fgetc(fil);
                    h += fgetc(fil) * 256;
                    c = fgetc(fil);

                    fgetc(fil);

                    fgetc(fil);

                    fgetc(fil);
                    buffu = new byte[w * h];

                    fread(ref buffu, 1, w * h, fil);

                    if (!AsBool(rep[x]))
                    {
                        // if not marked as rep, make new sprite
                        pak.spr[x] = new_sprite(w, h);
                        pak.spr[x].co = c;
                        // data
                        memcpy(pak.spr[x].data, buffu, w * h);
                    }


                    free(buffu);
                }
                if (AsBool(rep[x]))
                {

                    sprintf(framename, "%sframe%03d.tga".ToCharArray(), spritedir, x);
                    img = ik_load_tga(framename, null);
                    if (img!=null)
                    {
                        pak.spr[x] = get_sprite(img, 0, 0, img.w, img.h);

                        del_image(img);
                    }
                }
            }


            fclose(fil);

            return pak;
        }

        private static void _findclose(long fhandle)
        {
            throw new NotImplementedException();
        }

        private static FILE fopen(char[] fname, char[] v)
        {
            throw new NotImplementedException();
        }

        private static long _findnext(long fhandle, _finddata_t find)
        {
            throw new NotImplementedException();
        }

        private static void sprintf(char[] framename, char[] v, char[] spritedir, int x)
        {
            throw new NotImplementedException();
        }

        private static int fgetc(FILE fil)
        {
            throw new NotImplementedException();
        }

        private static void free(byte[] buffu)
        {
            throw new NotImplementedException();
        }

        private static void sscanf(int v1, char[] v2, int fnum)
        {
            throw new NotImplementedException();
        }

        private static long _findfirst(char[] framename, _finddata_t find)
        {
            throw new NotImplementedException();
        }

        public static void save_sprites(string fileName, t_ik_spritepak pak)
        {
            char[] fname = fileName.ToCharArray();
            FILE fil;
            Int32 x;
            Int32 num = pak.num;

            fil = myopen(fname.ToString(), "wb");
            if (fil==null)
                return;

            fputc(num & 255, fil);
            fputc(num >> 8, fil);

            for (x = 0; x < num; x++)
            {
                // header
                fputc(pak.spr[x].w & 255, fil);
                fputc(pak.spr[x].w >> 8, fil);
                fputc(pak.spr[x].h & 255, fil);
                fputc(pak.spr[x].h >> 8, fil);
                fputc(pak.spr[x].co, fil);
                // filler
                fputc(0, fil);
                fputc(0, fil);
                fputc(0, fil);
                // data
                fwrite(ref pak.spr[x].data, 1, pak.spr[x].w * pak.spr[x].h, fil);
            }
        }

        private static void fputc(int v, FILE fil)
        {
            throw new NotImplementedException();
        }

        // --------------------------------------------
        //	      SPRITE DRAWING FUNCTIONS
        // --------------------------------------------

        // basic sprite draw.. corner align, 0-masked
        // flags:
        // 1:  center align (move up-left by half the size)
        // 2:  color 
        // 4:  blank
        public static void ik_dsprite(t_ik_image img, Int32 x, Int32 y, t_ik_sprite spr, Int32 flags)
        {
            Int32 px, py;
            Int32 yb, ye, xb, xe;
            byte co = 0;
            int p1=0, p2=0;

            if (AsBool(flags & 1)) { x -= spr.w >> 1; y -= spr.h >> 1; }  // centered
            if (AsBool(flags & 2)) { co = (byte)(flags >> 8); }  // colored
            if (AsBool(flags & 4)) ik_drawbox(img, x, y, x + spr.w - 1, y + spr.h - 1, 0);

            if (x < c_minx - spr.w || y < c_miny - spr.h || x >= c_maxx || y >= c_maxy) return;

            yb = Math.Max(c_miny, y); ye = Math.Min(c_maxy, y + spr.h);
            xb = Math.Max(c_minx, x); xe = Math.Min(c_maxx, x + spr.w);

            for (py = yb; py < ye; py++)
            {
                px = xb;
                p1 = ik_image_pointer(img, px, py);
                //p2 = spr.data + ((py - y) * spr.w) + (px - x);
                if (AsBool(co))
                    for (; px < xe; px++)
                    {
                        if (AsBool(p2))
                            p1 = p2;

                        p1++; p2++;
                    }
                else
                    for (; px < xe; px++)
                    {
                        if (AsBool(p2))
                        {
                            if (p2 < 16)
                                p1 = p2 + co * 16;
                            else
                                p1 = p2;
                        }

                        p1++; p2++;
                    }
            }
        }

        // basic rsprite draw.. center align, rotation, scale (0-masked)
        // flags:
        // 1:  Light   (flags = 1 + lightcolor*256)
        // 2:  Trans   (50% transparency)
        // 4:  Add     
        public static void ik_drsprite(t_ik_image img, Int32 x, Int32 y, Int32 r, Int32 s, t_ik_sprite spr, Int32 flags)
        {
            Int32 x1, y1, x2;
            Int32 size;
            Int32 xt, yt, c, cx, cy;
            Int32 cutleft, cutright;
            Int32 dx, dy;
            int p1;

            if (s <= 2)
            {
                c = spr.co;
                if (AsBool(c))
                {
                    if (y >= c_miny && x >= c_minx && x < c_maxx && y < c_maxy)
                    {
                        if (AsBool(flags))
                        {
                            p1 = ik_image_pointer(img, x, y);

                            if (AsBool(flags & 1)) c = gfx_lightbuffer[(c << 8) + (flags >> 8)];
                            if (AsBool(flags & 2)) c = gfx_transbuffer[(c << 8) + (p1)];
                            if (AsBool(flags & 4)) c = gfx_addbuffer[(c << 8) + (p1)];

                            p1 = c;
                        }
                        else
                            ik_putpixel(img, x, y, c);
                    }
                }
                return;
            }

            s = (s << 10) / Math.Max(spr.w, spr.h);
            size = Math.Max(spr.w, spr.h) * s >> 11;

            if (x < c_minx - size || y < c_miny - size || x >= c_maxx + size || y >= c_maxy + size) return;

            r &= 1023;
            dx = cos1k[r] * 1024 / s;
            dy = -sin1k[r] * 1024 / s;
            cx = (spr.w + 1) << 15;
            cy = (spr.h + 1) << 15;

            size = (Int32)(size * 1.4);

            for (y1 = -size; y1 < size; y1++)
            {
                if (y1 + y >= c_miny && y1 + y < c_maxy)
                {
                    x2 = x + size; if (x2 > c_maxx) x2 = c_maxx;
                    x1 = x - size; if (x1 < c_minx) x1 = c_minx;
                    xt = cx + (x1 - x) * dx - y1 * dy;
                    yt = cy + y1 * dx + (x1 - x) * dy;

                    cutleft = 0; cutright = 0;
                    // Clamp X
                    if (dx > 0)
                    {
                        if (xt + (x2 - x1) * dx > spr.w << 16) cutright = Math.Max(cutright, (xt + (x2 - x1) * dx - (spr.w << 16)) / dx);
                        if (xt < 0) cutleft = Math.Max(cutleft, -xt / dx + 1);
                    }
                    else if (dx < 0)
                    {
                        if (xt + (x2 - x1) * dx < 0) cutright = Math.Max(cutright, (xt + (x2 - x1) * dx) / dx);
                        if (xt > spr.w << 16) cutleft = Math.Max(cutleft, -(xt - (spr.w << 16)) / dx + 1);
                    }
                    else if (xt < 0 || xt >= spr.w << 16) x2 = x1;   // don't draw hline

                    // Clamp Y
                    if (x2 > x1)
                        if (dy > 0)
                        {
                            if (yt + (x2 - x1) * dy > spr.h << 16) cutright = Math.Max(cutright, (yt + (x2 - x1) * dy - (spr.h << 16)) / dy);
                            if (yt < 0) cutleft = Math.Max(cutleft, -yt / dy + 1);
                        }
                        else if (dy < 0)
                        {
                            if (yt + (x2 - x1) * dy < 0) cutright = Math.Max(cutright, (yt + (x2 - x1) * dy) / dy);
                            if (yt > spr.h << 16) cutleft = Math.Max(cutleft, -(yt - (spr.h << 16)) / dy + 1);
                        }
                        else if (yt < 0 || yt >= spr.h << 16) x2 = x1;  // don't draw hline

                    // Apply clamps
                    if (AsBool(cutleft))
                    { xt += dx * cutleft; yt += dy * cutleft; x1 += cutleft; }
                    if (AsBool(cutright))
                    { x2 -= cutright; }

                    p1 = ik_image_pointer(img, x1, y1 + y);

                    // innerloops
                    if (!AsBool(flags))  // "clean" .. no special fx .. fast
                        for (; x1 < x2; x1++)
                        {
                            c = spr.data[(yt >> 16) * spr.w + (xt >> 16)];
                            if (AsBool(c))
                                p1 = c;

                            p1++;
                            xt += dx; yt += dy;
                        }
                    else  // light, transparency, additive
                        for (; x1 < x2; x1++)
                        {
                            c = spr.data[(yt >> 16) * spr.w + (xt >> 16)];
                            if (AsBool(c))
                            {
                                if (AsBool(flags & 1)) c = gfx_lightbuffer[(c << 8) + (flags >> 8)];
                                if (AsBool(flags & 2)) c = gfx_transbuffer[(c << 8) + (p1)];
                                if (AsBool(flags & 4)) c = gfx_addbuffer[(c << 8) + (p1)];
                                p1 = c;
                            }

                            p1++;
                            xt += dx; yt += dy;
                        }
                }
            }
        }

        // sprite line draw.. line of tiled sprites (useful for laser beams etc)
        // flags:
        // 1:  Light   (flags = 1 + lightcolor*256)
        // 2:  Trans   (50% transparency)
        // 4:  Add     

        public static void ik_dspriteline(t_ik_image img, Int32 xb, Int32 yb, Int32 xe, Int32 ye, Int32 s,
                                        Int32 offset, Int32 ybits, t_ik_sprite spr, Int32 flags)
        {
            double r;
            Int32 x1, y1, x2;
            Int32 size;
            Int32 xt, yt, c, cx;
            Int32 cutleft, cutright;
            Int32 dx, dy;
            Int32 xl0, yl0, xl1, yl1, topy;
            int p1;

            if (s <= 2)
            {
                ik_drawline(img, xb, yb, xe, ye, spr.co);
                return;
            }

            s = (s << 6) / Math.Max(spr.w, spr.h);
            size = Math.Max(spr.w, spr.h) * s >> 7;

            xl0 = Math.Max(c_minx, Math.Min(xb - size, xe - size));
            xl1 = Math.Min(c_maxx, Math.Max(xb + size, xe + size));
            yl0 = Math.Max(c_miny, Math.Min(yb - size, ye - size));
            yl1 = Math.Min(c_maxy, Math.Max(yb + size, ye + size));

            if (xl0 > xl1 || yl0 > yl1) return;  // if clipped out
            if (xl1 < c_minx || yl1 < c_miny || xl0 >= c_maxx || yl0 >= c_maxy) return;

            r = Math.Atan2((double)xe - xb, (double)yb - ye);
            dx = (Int32)(Math.Cos(r) * 65536 * 64 / s);
            dy = -(Int32)(Math.Sin(r) * 65536 * 64 / s);
            cx = (spr.w + 1) << 15;

            //	topy=-(Int32)sqrt((xe-xb)*(xe-xb)+(ye-yb)*(ye-yb))*(65536*64/s);
            topy = (ye - yb) * dx + (xe - xb) * dy;

            for (y1 = yl0; y1 < yl1; y1++)
            {
                if (y1 >= c_miny && y1 < c_maxy)
                {
                    x2 = xl1; if (x2 > c_maxx) x2 = c_maxx;
                    x1 = xl0; if (x1 < c_minx) x1 = c_minx;
                    xt = cx + (x1 - xb) * dx - (y1 - yb) * dy;
                    yt = (y1 - yb) * dx + (x1 - xb) * dy;

                    cutleft = 0; cutright = 0;
                    // Clamp X
                    if (dx > 0)
                    {
                        if (xt + (x2 - x1) * dx > spr.w << 16) cutright = Math.Max(cutright, (xt + (x2 - x1) * dx - (spr.w << 16)) / dx);
                        if (xt < 0) cutleft = Math.Max(cutleft, -xt / dx + 1);
                    }
                    else if (dx < 0)
                    {
                        if (xt + (x2 - x1) * dx < 0) cutright = Math.Max(cutright, (xt + (x2 - x1) * dx) / dx);
                        if (xt > spr.w << 16) cutleft = Math.Max(cutleft, -(xt - (spr.w << 16)) / dx + 1);
                    }
                    else if (xt < 0 || xt >= spr.w << 16) x2 = x1;  // don't draw hline

                    // Clamp Y
                    if (x2 > x1)
                        if (dy > 0)
                        {
                            if (yt + (x2 - x1) * dy > 0) cutright = Math.Max(cutright, (yt + (x2 - x1) * dy) / dy);
                            if (yt < topy) cutleft = Math.Max(cutleft, (topy - yt) / dy + 1);
                        }
                        else if (dy < 0)
                        {
                            if (yt + (x2 - x1) * dy < topy) cutright = Math.Max(cutright, -(topy - (yt + (x2 - x1) * dy)) / dy + 1);
                            if (yt > 0) cutleft = Math.Max(cutleft, -yt / dy + 1);
                        }
                        else if (yt < topy || yt >= 0) x2 = x1;   // don't draw hline

                    // Apply clamps
                    if (AsBool(cutleft))
                    { xt += dx * cutleft; yt += dy * cutleft; x1 += cutleft; }
                    if (AsBool(cutright))
                    { x2 -= cutright; }

                    p1 = ik_image_pointer(img, x1, y1);

                    // innerloops
                    if (!AsBool(flags))  // "clean" .. no special fx .. fast
                        for (; x1 < x2; x1++)
                        {
                            c = spr.data[(((yt >> ybits) + offset) & (spr.h - 1)) * spr.w + (xt >> 16)];
                            if (AsBool(c))
                                p1 = c;

                            p1++;
                            xt += dx; yt += dy;
                        }
                    else  // light, transparency, additive
                        for (; x1 < x2; x1++)
                        {
                            c = spr.data[(((yt >> ybits) + offset) & (spr.h - 1)) * spr.w + (xt >> 16)];
                            if (AsBool(c))
                            {
                                if (AsBool(flags & 1)) c = gfx_lightbuffer[(c << 8) + (flags >> 8)];
                                if (AsBool(flags & 2)) c = gfx_transbuffer[(c << 8) + (p1)];
                                if (AsBool(flags & 4)) c = gfx_addbuffer[(c << 8) + (p1)];
                                p1 = c;
                            }

                            p1++;
                            xt += dx; yt += dy;
                        }
                }
            }
        }

    }
}