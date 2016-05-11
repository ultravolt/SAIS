using System;
using System.Runtime.InteropServices;

#region Includes
// ----------------
//     INCLUDES
// ----------------

//#include <stdlib.h>
//#include <stdio.h>
//#include <string.h>
//#include <time.h>
//#include <math.h>

//#include "typedefs.h"
//#include "is_fileio.h"

//#include "textstr.h"
//#include "iface_globals.h"
//#include "gfx.h"
//#include "snd.h"
//#include "interface.h"
//#include "combat.h"
//#include "cards.h"
//#include "starmap.h"

//#include "startgame.h"  
#endregion
namespace DigitalEeel
{
    public static partial class SAIS
    {


        public static Int32 startgame()
        {
            Int32 end;
            Int32 c, mc;
            Int32 bx = 192, by = 88, h = 300;
            Int32 y;
            Int32 upd = 1;
            Int32 mx, my;
            Int32 cn = 0;
            Int32 ti, ot;
            t_ik_image bg;
            char[] name = new char[32];

            loadconfig();

            start_ik_timer(1, 31);
            while (get_ik_timer(1) < 2 && !must_quit)
            {
                ik_eventhandler();
            }
            Play_Sound((int)sfxsamples.WAV_MUS_TITLE, 15, 1, 100, 22050, -1000);
            while (get_ik_timer(1) < 4 && !must_quit)
            {
                ik_eventhandler();
            }
            Play_Sound((int)sfxsamples.WAV_MUS_TITLE, 14, 1, 80, 22050, 1000);


            if (AsBool(settings.random_names) & true)
                strcpy(settings.captname, captnames[rand() % num_captnames]);

            if (AsBool(settings.random_names & 2))
                strcpy(settings.shipname, shipnames[rand() % num_shipnames]);

            bg = ik_load_pcx("graphics/starback.pcx".ToCharArray(), null);

            end = 0; ti = get_ik_timer(2);
            while (!AsBool(end) && !must_quit)
            {
                ik_eventhandler();
                c = ik_inkey();
                mc = ik_mclick();
                mx = ik_mouse_x - bx;
                my = ik_mouse_y - by;

                ot = ti;
                ti = get_ik_timer(2);
                if (ti != ot)
                {
                    prep_screen();
                    ik_blit();
                }

                if (c == 13 || c == 32)
                    end = 2;
                if ((AsBool(mc) & true) && mx > 0 && mx < 240)
                {
                    if (my > h - 24 && my < h - 8) // buttons
                    {
                        if (mx > 16 && mx < 64) // cancel
                            end = 1;
                        else if (mx > 176 && mx < 224) // ok
                        { end = 2; Play_SoundFX((int)sfxsamples.WAV_DOT); }
                    }
                    else if (my > 32 && my < 40) // captain
                    {
                        if (mx < 216)
                        {
                            cn |= 1;
                            prep_screen();
                            ik_drawbox(screen, bx + 70, by + 32, bx + 215, by + 39, STARMAP_INTERFACE_COLOR * 16 + 3);
                            ik_blit();
                            strcpy(name, settings.captname);
                            ik_text_input(bx + 70, by + 32, 14, font_6x8, String.Empty.ToCharArray(), name, STARMAP_INTERFACE_COLOR * 16 + 3, STARMAP_INTERFACE_COLOR);
                            if (strlen(name) > 0)
                                strcpy(settings.captname, name);
                        }
                        else
                        {
                            settings.random_names ^= 1;
                            Play_SoundFX((int)sfxsamples.WAV_LOCK, 0);
                        }
                        upd = 1; must_quit = false;
                    }
                    else if (my > 40 && my < 48) // ship
                    {
                        if (mx < 216)
                        {
                            cn |= 2;
                            prep_screen();
                            ik_drawbox(screen, bx + 70, by + 40, bx + 215, by + 47, STARMAP_INTERFACE_COLOR * 16 + 3);
                            ik_blit();
                            strcpy(name, settings.shipname);
                            ik_text_input(bx + 70, by + 40, 14, font_6x8, String.Empty.ToCharArray(), name, STARMAP_INTERFACE_COLOR * 16 + 3, STARMAP_INTERFACE_COLOR);
                            if (strlen(name) > 0)
                                strcpy(settings.shipname, name);
                        }
                        else
                        {
                            settings.random_names ^= 2;
                            Play_SoundFX((int)sfxsamples.WAV_LOCK, 0);
                        }
                        upd = 1; must_quit = false;
                    }
                    else if (my > 64 && my < 96)    // ship
                    {
                        settings.dif_ship = (mx - 16) / 72;
                        Play_SoundFX((int)sfxsamples.WAV_SLIDER, 0);
                        upd = 1;
                    }
                    else if (my > 112 && my < 176) // nebula
                    {
                        settings.dif_nebula = (mx - 16) / 72;
                        Play_SoundFX((int)sfxsamples.WAV_SLIDER, 0);
                        upd = 1;
                    }
                    else if (my > 192 && my < 224) // enemies
                    {
                        settings.dif_enemies = (mx - 16) / 72;
                        Play_SoundFX((int)sfxsamples.WAV_SLIDER, 0);
                        upd = 1;
                    }
                    else if (my > 232 && my < 240)  // easy/hard
                    {
                        c = (mx - 40) / 32;
                        if (c < 0) c = 0;
                        if (c > 4) c = 4;
                        settings.dif_nebula = (c + 1) / 2;
                        settings.dif_enemies = c / 2;
                        upd = 1;
                        Play_SoundFX((int)sfxsamples.WAV_SLIDER, 0);
                    }
                    else if (my > 256 && my < 264)  // enable tutorial
                    {
                        if (mx > 16 && mx < 24)
                        {
                            settings.random_names ^= 4;
                            Play_SoundFX((int)sfxsamples.WAV_LOCK, 0);
                            upd = 1;
                        }
                    }
                }
                if (AsBool(upd))
                {
                    upd = 0;
                    prep_screen();
                    ik_copybox(bg, screen, 0, 0, 640, 480, 0, 0);

                    y = by + 16;
                    interface_drawborder(screen, bx, by, bx + 240, by + h, 1, STARMAP_INTERFACE_COLOR, "Start new adventure".ToCharArray());
                    ik_print(screen, font_6x8, bx + 16, y += 8, 0, textstring[(int)textstrings.STR_STARTGAME_IDENTIFY]);
                    ik_print(screen, font_6x8, bx + 16, y += 8, 0, textstring[(int)textstrings.STR_STARTGAME_CAPTAIN]);
                    ik_print(screen, font_6x8, bx + 70, y, 3, settings.captname);
                    if (!AsBool(cn & 1))
                        ik_print(screen, font_4x8, bx + 216 - strlen(textstring[(int)textstrings.STR_STARTGAME_RENAME]) * 4, y, 3, textstring[(int)textstrings.STR_STARTGAME_RENAME]);
                    ik_dsprite(screen, bx + 216, y, spr_IFslider.spr[8 + (settings.random_names & 1)], 2 + ((3 - 3 * (settings.random_names & 1)) << 8));

                    ik_print(screen, font_6x8, bx + 16, y += 8, 0, textstring[(int)textstrings.STR_STARTGAME_STARSHIP]);
                    ik_print(screen, font_6x8, bx + 70, y, 3, settings.shipname);
                    if (!AsBool(cn & 2))
                        ik_print(screen, font_4x8, bx + 216 - strlen(textstring[(int)textstrings.STR_STARTGAME_RENAME]) * 4, y, 3, textstring[(int)textstrings.STR_STARTGAME_RENAME]);
                    ik_dsprite(screen, bx + 216, y, spr_IFslider.spr[8 + (settings.random_names & 2) / 2], 2 + ((3 - 3 * (settings.random_names & 2) / 2) << 8));

                    ik_print(screen, font_6x8, bx + 16, y += 16, 0, textstring[(int)textstrings.STR_STARTGAME_LOADOUT], textstring[(int)textstrings.STR_STARTGAME_LOADOUT1 + settings.dif_ship]);
                    y += 8;
                    for (c = 0; c < 3; c++)
                    {
                        ik_dsprite(screen, bx + 16 + c * 72, y, spr_IFdifenemy.spr[c + 3], 0);
                        ik_dsprite(screen, bx + 16 + c * 72, y, spr_IFborder.spr[20], 2 + (3 << 8) * AsInt(c == settings.dif_ship));
                    }

                    ik_print(screen, font_6x8, bx + 16, y += 40, 0, textstring[(int)textstrings.STR_STARTGAME_NEBULA]);
                    y += 8;
                    for (c = 0; c < 3; c++)
                    {
                        ik_dsprite(screen, bx + 16 + c * 72, y, spr_IFdifnebula.spr[c], 0);
                        ik_dsprite(screen, bx + 16 + c * 72, y, spr_IFborder.spr[18], 2 + (3 << 8) * AsInt(c == settings.dif_nebula));
                    }

                    ik_print(screen, font_6x8, bx + 16, y += 72, 0, textstring[(int)textstrings.STR_STARTGAME_ENEMIES]);
                    y += 8;
                    for (c = 0; c < 3; c++)
                    {
                        ik_dsprite(screen, bx + 16 + c * 72, y, spr_IFdifenemy.spr[c], 0);
                        ik_dsprite(screen, bx + 16 + c * 72, y, spr_IFborder.spr[20], 2 + (3 << 8) * AsInt(c == settings.dif_enemies));
                    }

                    y += 40;
                    ik_print(screen, font_6x8, bx + 16, y, 0, textstring[(int)textstrings.STR_STARTGAME_EASY]);
                    ik_print(screen, font_6x8, bx + 224 - 6 * strlen(textstring[(int)textstrings.STR_STARTGAME_HARD]), y, 0, textstring[(int)textstrings.STR_STARTGAME_HARD]);
                    ik_print(screen, font_6x8, bx + 16, y + 12, 0, textstring[(int)textstrings.STR_STARTGAME_LOSCORE]);
                    ik_print(screen, font_6x8, bx + 224 - 6 * strlen(textstring[(int)textstrings.STR_STARTGAME_HISCORE]), y + 12, 0, textstring[(int)textstrings.STR_STARTGAME_HISCORE]);
                    interface_drawslider(screen, bx + 56, y, 0, 128, 4, settings.dif_enemies + settings.dif_nebula, STARMAP_INTERFACE_COLOR);

                    y += 24;
                    ik_dsprite(screen, bx + 12, y - 5, spr_IFbutton.spr[AsInt((settings.random_names & 4) > 0)], 0);
                    ik_print(screen, font_6x8, bx + 32, y, 0, "TUTORIAL MODE".ToCharArray());


                    interface_drawbutton(screen, bx + 16, by + h - 24, 48, STARMAP_INTERFACE_COLOR, textstring[(int)textstrings.STR_CANCEL]);
                    interface_drawbutton(screen, bx + 240 - 64, by + h - 24, 48, STARMAP_INTERFACE_COLOR, textstring[(int)textstrings.STR_OK]);

                    ik_blit();
                    update_palette();
                }
            }

            interface_cleartuts();

            if (must_quit)
                end = 1;

            if (AsBool(settings.opt_mucrontext & 1))
            {


                if (end > 1)
                {
                    bx = 192; by = 72; h = 328;
                    by = 220 - h / 2;

                    prep_screen();
                    ik_copybox(bg, screen, 0, 0, 640, 480, 0, 0);

                    y = 3;
                    interface_drawborder(screen, bx, by, bx + 256, by + h, 1, STARMAP_INTERFACE_COLOR, textstring[(int)textstrings.STR_STARTGAME_TITLE1]);
                    y += 1 + interface_textbox(screen, font_6x8, bx + 84, by + y * 8, 160, 88, 0,
                                            textstring[(int)textstrings.STR_STARTGAME_MUCRON1]);
                    y += 1 + interface_textbox(screen, font_6x8, bx + 16, by + y * 8, 224, 88, 0,
                                            textstring[(int)textstrings.STR_STARTGAME_MUCRON2]);
                    y += 1 + interface_textbox(screen, font_6x8, bx + 16, by + y * 8, 224, 88, 0,
                                            textstring[(int)textstrings.STR_STARTGAME_MUCRON3]);
                    y += 1 + interface_textbox(screen, font_6x8, bx + 16, by + y * 8, 224, 88, 0,
                                            textstring[(int)textstrings.STR_STARTGAME_MUCRON4]);
                    interface_drawbutton(screen, bx + 256 - 64, by + h - 24, 48, STARMAP_INTERFACE_COLOR, textstring[(int)textstrings.STR_OK]);
                    ik_dsprite(screen, bx + 16, by + 24, spr_SMraces.spr[(int)race_ids.race_unknown], 0);
                    ik_dsprite(screen, bx + 16, by + 24, spr_IFborder.spr[18], 2 + (STARMAP_INTERFACE_COLOR << 8));

                    ik_blit();
                    update_palette();
                    end = waitclick(bx + 256 - 64, by + h - 24, bx + 256 - 16, by + h - 8);
                }

                if (end > 1)
                {
                    bx = 192; by = 96; h = 168;
                    by = 220 - h / 2;

                    prep_screen();
                    ik_copybox(bg, screen, 0, 0, 640, 480, 0, 0);

                    y = 3;
                    interface_drawborder(screen, bx, by, bx + 256, by + h, 1, STARMAP_INTERFACE_COLOR, textstring[(int)textstrings.STR_STARTGAME_TITLE2]);
                    y += 1 + interface_textbox(screen, font_6x8, bx + 84, by + y * 8, 160, 88, 0,
                                            textstring[(int)textstrings.STR_STARTGAME_MUCRON5]);
                    y += 1 + interface_textbox(screen, font_6x8, bx + 16, by + y * 8, 224, 88, 0,
                                            textstring[(int)textstrings.STR_STARTGAME_MUCRON6]);
                    interface_drawbutton(screen, bx + 256 - 64, by + h - 24, 48, STARMAP_INTERFACE_COLOR, textstring[(int)textstrings.STR_OK]);
                    ik_dsprite(screen, bx + 16, by + 24, spr_IFdifnebula.spr[1], 0);
                    ik_dsprite(screen, bx + 16, by + 24, hulls[shiptypes[0].hull].sprite, 0);
                    ik_dsprite(screen, bx + 16, by + 24, spr_IFborder.spr[18], 2 + (STARMAP_INTERFACE_COLOR << 8));

                    ik_blit();
                    update_palette();
                    end = waitclick(bx + 256 - 64, by + h - 24, bx + 256 - 16, by + h - 8);
                }



                if (end > 1)
                {
                    bx = 192; by = 120; h = 112;
                    by = 220 - h / 2;

                    prep_screen();
                    ik_copybox(bg, screen, 0, 0, 640, 480, 0, 0);

                    y = 3;
                    interface_drawborder(screen, bx, by, bx + 256, by + h, 1, STARMAP_INTERFACE_COLOR, textstring[(int)textstrings.STR_STARTGAME_TITLE3]);
                    y += 1 + interface_textbox(screen, font_6x8, bx + 84, by + y * 8, 160, 88, 0,
                                            textstring[(int)textstrings.STR_STARTGAME_MUCRON7]);
                    interface_drawbutton(screen, bx + 256 - 64, by + h - 24, 48, STARMAP_INTERFACE_COLOR, textstring[(int)textstrings.STR_OK]);
                    ik_dsprite(screen, bx + 16, by + 24, spr_SMraces.spr[RC_PLANET], 4);
                    ik_dsprite(screen, bx + 16, by + 24, spr_IFborder.spr[18], 2 + (STARMAP_INTERFACE_COLOR << 8));

                    ik_blit();
                    update_palette();
                    end = waitclick(bx + 256 - 64, by + h - 24, bx + 256 - 16, by + h - 8);
                }

            }

            del_image(bg);

            if (end > 1)
            {
                starmap_create();
                player_init();
            }

            saveconfig();

            return end - 1;
        }

        private static int rand()
        {
            throw new NotImplementedException();
        }

        private static int AsInt(bool v)
        {
            if (v == false)
                return 0;
            return 1;
        }

        private static int strlen(char[] text)
        {
            return text.Length;
        }

        private static void strcpy(char[] source, char[] destination)
        {
            source.CopyTo(destination, 0);
        }

        public static Int32 waitclick(int left, int top, int right, int bottom)
        {
            int end = 0;
            int c, mc;
            int t;

            t = get_ik_timer(2);
            while (!AsBool(end) && !must_quit)
            {
                ik_eventhandler();
                c = ik_inkey();
                mc = ik_mclick();
                if (c == 13 || c == 32)
                { end = 2; Play_SoundFX((int)sfxsamples.WAV_DOT); }

                if (AsBool(mc & 1))
                {
                    if (ik_mouse_x >= left && ik_mouse_x < right && ik_mouse_y >= top && ik_mouse_y < bottom)
                    { end = 2; Play_SoundFX((int)sfxsamples.WAV_DOT); }
                }

                c = t;
                t = get_ik_timer(2);
                //if (settings.opt_mousemode!=1)
                if (t != c)
                {
                    prep_screen();
                    ik_blit();
                }
            }

            if (must_quit)
                end = 1;

            return end;
        }

        public static void loadconfig()
        {
            FILE cfg;

            cfg = myopen("settings.dat", "rb");
            if (cfg == null)
            {
                settings.dif_enemies = 0;
                settings.dif_nebula = 0;
                settings.dif_ship = 0;
                settings.random_names = 7;
                settings.opt_mucrontext = 1;
                settings.opt_timerwarnings = 1;
                settings.opt_mousemode = 0;
                settings.opt_timeremaining = 0;
                settings.opt_lensflares = 1;
                settings.opt_smoketrails = 1;
                settings.opt_volume = 8;
                s_volume = 80;

                return;
            }

            fread(ref settings, Marshal.SizeOf(typeof(t_gamesettings)), 1, cfg);
            fclose(cfg);
            s_volume = settings.opt_volume * 10;
            settings.opt_mousemode &= 1;
        }

        private static void fread(ref t_gamesettings data, int v1, int v2, FILE cfg)
        {
            throw new NotImplementedException();
        }

        private static void fread(ref byte[] data, int v1, int v2, FILE cfg)
        {
            throw new NotImplementedException();
        }
        private static void fread(ref t_paletteentry data, int v1, int v2, FILE cfg)
        {
            throw new NotImplementedException();
        }

        private static void fclose(FILE cfg)
        {
            throw new NotImplementedException();
        }

        public static void saveconfig()
        {
            FILE cfg;

            cfg = myopen("settings.dat", "wb");
            if (cfg == null)
                return;
            fwrite(ref settings, Marshal.SizeOf(typeof(t_gamesettings)), 1, cfg);
            fclose(cfg);
        }

        private static void fwrite(ref t_gamesettings settings, int v1, int v2, FILE cfg)
        {
            throw new NotImplementedException();
        }
        private static void fwrite(ref byte[] settings, int v1, int v2, FILE cfg)
        {
            throw new NotImplementedException();
        }
        public static bool AsBool(int value)
        {
            if (value == 0)
                return false;
            return true;
        }
    }
}