// ----------------
//     INCLUDES
// ----------------

//#include <stdlib.h>
//#include <stdio.h>
//#include <string.h>
//#include <time.h>
//#include <math.h>
//
//#include "typedefs.h"
//#include "iface_globals.h"
//#include "is_fileio.h"
//#include "gfx.h"
//#include "snd.h"
//#include "interface.h"
//#include "starmap.h"
//#include "textstr.h"
//
//#include "combat.h"
using System;
using System.Collections.Generic;

namespace DigitalEeel
{
    public static partial class SAIS
    {
        // ----------------
        //		CONSTANTS
        // ----------------

        // ----------------
        // LOCAL VARIABLES
        // ----------------
        public static t_combatcamera camera;
        public static t_ship[] cships = new t_ship[MAX_COMBAT_SHIPS];
        public static t_wepbeam[] cbeams = new t_wepbeam[MAX_COMBAT_BEAMS];
        public static t_wepproj[] cprojs = new t_wepproj[MAX_COMBAT_PROJECTILES];
        public static t_explosion[] cexplo = new t_explosion[MAX_COMBAT_EXPLOS];

        public static Int32 numships;
        public static Int32 playership;
        public static Int32[] sortship = new Int32[MAX_COMBAT_SHIPS];

        public static Int32 t_move, t_disp, pause;

        public static Int32 nebula;
        public static Int32 retreat;
        public static Int32 rett;
        public static Int32 klaktime;
        public static Int32 klakavail;
        public static Int32 gongavail;

        public static Int32 simulated;
        private static readonly bool DEBUG_COMBAT=false;
        private static readonly bool COMBAT_BUILD_HELP = false;


        /*
		# ifdef DEBUG_COMBAT
				char combatdebug[64];
		#endif
		*/



        // ----------------
        // GLOBAL FUNCTIONS
        // ----------------

        public static Int32 combat(Int32 flt, Int32 sim)
        {
            Int32 t, t0;
            Int32 c, mc, s;
            Int32 b;
            Int32 f;
            Int32 end;
            Int32 klak, rett;

            simulated = sim;

            combat_start(flt);

            ik_inkey();

            t_move = 0; t_disp = 0; pause = 0; end = 0; rett = 0;

            if (AsBool(simulated))
                Play_Sound((int)sfxsamples.WAV_MUS_SIMULATOR, 15, 1, 85);
            else if (!AsBool(nebula))
                Play_Sound((int)sfxsamples.WAV_MUS_COMBAT, 15, 1);
            else
                Play_Sound((int)sfxsamples.WAV_MUS_NEBULA, 15, 1);
            start_ik_timer(1, 1000 / COMBAT_FRAMERATE); t0 = t = 0;
            while (!must_quit && (t < end || end == 0))
            {
                t0 = t;
                ik_eventhandler();  // always call every frame
                t = get_ik_timer(1);

                if (must_quit)
                {
                    must_quit = false;
                    Play_SoundFX((int)sfxsamples.WAV_DESELECT);
                    if (AsBool(simulated))
                    {
                        if (!AsBool(interface_popup(font_6x8, 240, 200, 160, 72, COMBAT_INTERFACE_COLOR, 0,
                                textstring[(int)textstrings.STR_QUIT_TITLE], textstring[(int)textstrings.STR_QUIT_SIMULATION],
                                textstring[(int)textstrings.STR_YES], textstring[(int)textstrings.STR_NO])))
                        { must_quit = true; player.death = 666; }
                    }
                    else
                    {
                        if (!AsBool(interface_popup(font_6x8, 240, 200, 160, 72, COMBAT_INTERFACE_COLOR, 0,
                                textstring[(int)textstrings.STR_QUIT_TITLE], textstring[(int)textstrings.STR_QUIT_CONFIRM],
                                textstring[(int)textstrings.STR_YES], textstring[(int)textstrings.STR_NO])))
                        { must_quit = true; player.death = 666; }
                    }
                    ik_eventhandler();  // always call every frame
                    t = get_ik_timer(1);
                    t0 = t;
                }

                mc = ik_mclick();
                c = ik_inkey();
                b = ik_mouse_b;

                klak = klakavail;

                if (AsBool(IsMinimized))
                    pause = 1;

                if (c == 32)
                {
                    if (pause == 1)
                        pause = 0;
                    else
                        pause = 1;
                }

                if (AsBool(key_pressed(key_f[0])))
                {
                    combat_help_screen();
                    ik_eventhandler();  // always call every frame
                    t = get_ik_timer(1);
                    t0 = t;
                }

                if (DEBUG_COMBAT)
                {
                    if (camera.ship_trg > -1)
                    {
                        s = camera.ship_trg;
                        if (cships[s].type > -1 && cships[s].hits >= 0)
                        {
                            switch (c)
                            {
                                case 'w':
                                    cships[s].syshits[0] = 0;
                                    combat_updateshipstats(s, t_move);
                                    break;

                                case 'd':
                                    combat_damageship(s, playership, cships[s].hits, t, shipweapons[0], 1);
                                    break;

                                case 'e':
                                    combat_damageship(s, playership, cships[s].hits + hulls[shiptypes[cships[c].type].hull].hits + 1, t, shipweapons[0], 1);
                                    break;

                                case 'f':
                                    cships[s].flee = 1;
                                    cships[s].tac = (int)combat_tactics.tac_flee;
                                    cships[s].target = -1;
                                    break;
                            }
                        }
                    }
                }

                f = 0;
                for (s = 0; s < MAX_COMBAT_SHIPS; s++)
                    if (cships[s].hits > 0 && cships[s].type > -1 && cships[s].escaped == 0 && cships[s].flee == 0)
                    {
                        if ((cships[s].own & 1) == 0)
                            f |= 1;
                        else
                            f |= 2;
                    }
                if ((f == 1 || cships[playership].type == -1) && AsBool(retreat))
                {
                    retreat = 0;
                    Play_SoundFX((int)sfxsamples.WAV_SELECT, t);

                    if (AsBool(simulated))
                        Play_Sound((int)sfxsamples.WAV_MUS_SIMULATOR, 15, 1, 85);
                    else if (!AsBool(nebula))
                        Play_Sound((int)sfxsamples.WAV_MUS_COMBAT, 15, 1);
                    else
                        Play_Sound((int)sfxsamples.WAV_MUS_NEBULA, 15, 1);
                    for (s = 0; s < MAX_COMBAT_SHIPS; s++)
                    {
                        if ((cships[s].own & 1) == 0 && s != playership)
                            combat_findstuff2do(s, t);
                    }
                }

                if (ik_mouse_x < 160 && AsBool(mc & 1))
                {
                    if (ik_mouse_y > 24 && ik_mouse_y < 40)
                    {   // select ship by icon
                        s = (ik_mouse_x - 16) / 16;
                        if (s >= 0 && s < player.num_ships)
                        {
                            if (cships[s].type > -1 && cships[s].hits > 0)
                            {
                                select_ship(s, t);
                            }
                        }
                    }
                    if (ik_mouse_y > 288 && ik_mouse_y < 320 && cships[playership].hits > 0)
                    {
                        s = (ik_mouse_x / 80) + ((ik_mouse_y - 288) / 16) * 2;
                        switch (s)
                        {
                            case 0:                 // cloak button
                                if (cships[playership].clo_type > 0 && cships[playership].syshits[cships[playership].sys_clo] >= 5 && t_move > cships[playership].cloaktime + 100)
                                {
                                    Play_SoundFX((int)sfxsamples.WAV_DOT, 0);
                                    if (AsBool(cships[playership].cloaked)) // uncloak
                                    {
                                        cships[playership].cloaked = 0;
                                        cships[playership].cloaktime = t_move;
                                        //Play_SoundFX((int)sfxsamples.WAV_CLOAKOUT, t);
                                    }
                                    else    // cloak
                                    {

                                        cships[playership].cloaked = 1;
                                        cships[playership].cloaktime = t_move;
                                        //Play_SoundFX((int)sfxsamples.WAV_CLOAKIN, t);
                                    }
                                }
                                break;

                            case 1: // gong
                                if (gongavail == 2)
                                {
                                    combat_use_gong(t_move);
                                    gongavail = 1;
                                }
                                break;

                            case 2: // retreat button
                                if (f == 3)
                                {
                                    if (!AsBool(retreat))
                                    {
                                        retreat = 1;
                                        rett = t_move;
                                        Play_SoundFX((int)sfxsamples.WAV_SELECT, t);
                                        Play_Sound((int)sfxsamples.WAV_FLARE, 15, 1);

                                        for (s = 0; s < MAX_COMBAT_SHIPS; s++)
                                            if ((cships[s].own & 1) == 0)
                                                combat_findstuff2do(s, t);
                                    }
                                    else
                                    {
                                        retreat = 0;
                                        Play_SoundFX((int)sfxsamples.WAV_SELECT, t);
                                        if (AsBool(simulated))
                                            Play_Sound((int)sfxsamples.WAV_MUS_SIMULATOR, 15, 1, 85);
                                        else if (!AsBool(nebula))
                                            Play_Sound((int)sfxsamples.WAV_MUS_COMBAT, 15, 1);
                                        else
                                            Play_Sound((int)sfxsamples.WAV_MUS_NEBULA, 15, 1);

                                        for (s = 0; s < MAX_COMBAT_SHIPS; s++)
                                        {
                                            if ((cships[s].own & 1) == 0 && s != playership)
                                                combat_findstuff2do(s, t);
                                        }
                                    }
                                }
                                break;

                            case 3: // summon klakar
                                if (klak == 1)
                                {
                                    combat_summon_klakar(t_move);
                                    klakavail = 0;
                                }
                                break;
                        }
                    }
                }

                if (ik_mouse_x > 160 && ik_mouse_x < 640)
                {
                    if (ik_mouse_x > 170 && ik_mouse_x < 218 && ik_mouse_y > 456 && ik_mouse_y < 472)
                    {
                        if (AsBool(mc & 1))
                        {
                            if (ik_mouse_x < 186)
                                pause = 1;
                            else if (ik_mouse_x > 202)
                                pause = -1;
                            else
                                pause = 0;
                        }
                    }
                    else if (AsBool(mc & 1))
                    {
                        if (!AsBool(camera.drag_trg))
                        {
                            s = combat_findship(ik_mouse_x, ik_mouse_y);
                            if (s > -1)
                            {
                                if (cships[s].own == 0) // select friendly
                                {
                                    select_ship(s, t);
                                }
                                else if (camera.ship_sel > -1)  // target enemy
                                {
                                    camera.ship_trg = s;
                                    cships[camera.ship_sel].target = camera.ship_trg;
                                    cships[camera.ship_sel].tac = 0;
                                    camera.time_trg = t;
                                    camera.drag_trg = 1;
                                }
                            }
                            else
                            {
                                // check if dragging waypoint
                                if (camera.ship_sel > -1)
                                {
                                    if (cships[camera.ship_sel].target > -1)
                                    {
                                        s = cships[camera.ship_sel].target;
                                        if (Math.Abs(cships[s].ds_x - ((((sin1k[cships[camera.ship_sel].angle] * cships[camera.ship_sel].dist) >> 16) * camera.z) >> 12) - ik_mouse_x) < 8 &&
                                                Math.Abs(cships[s].ds_y + ((((cos1k[cships[camera.ship_sel].angle] * cships[camera.ship_sel].dist) >> 16) * camera.z) >> 12) - ik_mouse_y) < 8)
                                            camera.drag_trg = 1;
                                    }
                                    else
                                    {
                                        s = camera.ship_sel;
                                        if (Math.Abs(160 + 240 + ((((cships[s].wp_x - camera.x) >> 10) * camera.z) >> 12) - ik_mouse_x) < 8 &&
                                                Math.Abs(244 - ((((cships[s].wp_y - camera.y) >> 10) * camera.z) >> 12) - ik_mouse_y) < 8)
                                            camera.drag_trg = 1;
                                    }

                                }

                                if (!AsBool(camera.drag_trg))
                                {
                                    if (camera.ship_sel > -1)
                                    {
                                        camera.drag_trg = 1;
                                        cships[camera.ship_sel].target = -1;
                                        cships[camera.ship_sel].tac = 2;
                                        cships[camera.ship_sel].wp_x = camera.x + ((((ik_mouse_x - 400) << 12) / camera.z) << 10);
                                        cships[camera.ship_sel].wp_y = camera.y + ((((244 - ik_mouse_y) << 12) / camera.z) << 10);
                                    }
                                    //						camera.ship_sel = -1;
                                    //						camera.ship_trg = -1;
                                }
                            }
                        }
                    }
                    else if ((mc & 2) > 0 && camera.ship_sel > -1)
                    {
                        // return to formation
                        if (camera.ship_sel != playership && cships[playership].hits > 0 && cships[playership].type > -1)
                        {
                            cships[camera.ship_sel].target = playership;
                            cships[camera.ship_sel].tac = 0;
                            camera.ship_trg = cships[camera.ship_sel].target;
                            camera.time_trg = t;
                            camera.drag_trg = 1;
                        }
                        /*
                        if (camera.ship_trg > -1)
                        {
                            s = camera.ship_trg;
                            camera.ship_trg = combat_findship(ik_mouse_x, ik_mouse_y);
                            if (camera.ship_trg > -1 && camera.ship_trg != camera.ship_sel)
                            {	cships[camera.ship_sel].target = camera.ship_trg; }
                            cships[camera.ship_sel].tac = 0;
                            camera.ship_trg = cships[camera.ship_sel].target;
                            if (s != camera.ship_trg)
                                camera.time_trg = t;
                            camera.drag_trg = 1;
                        }*/
                    }

                    if (!AsBool(b & 3) && camera.drag_trg > 0)
                    {
                        camera.drag_trg = 0;
                        if (camera.ship_sel > -1)
                        {
                            if (cships[camera.ship_sel].target > -1)
                            {
                                s = camera.ship_sel;
                                cships[s].angle = get_direction(cships[camera.ship_trg].ds_x - ik_mouse_x,
                                                                                                 ik_mouse_y - cships[camera.ship_trg].ds_y);
                                cships[s].dist = get_distance(cships[camera.ship_trg].ds_x - ik_mouse_x,
                                                                                             ik_mouse_y - cships[camera.ship_trg].ds_y);
                                if (cships[s].dist <= cships[camera.ship_trg].ds_s >> 1)
                                {
                                    if ((cships[s].own & 1) == (cships[camera.ship_trg].own & 1))
                                        cships[s].dist = 64;
                                    else
                                        cships[s].dist = 0;
                                }
                                else
                                    cships[s].dist = (cships[s].dist << 12) / camera.z;
                            }
                            else
                            {
                                cships[camera.ship_sel].wp_x = camera.x + ((((ik_mouse_x - 400) << 12) / camera.z) << 10);
                                cships[camera.ship_sel].wp_y = camera.y + ((((244 - ik_mouse_y) << 12) / camera.z) << 10);
                            }
                        }
                    }
                }

                if (t > t0)
                {
                    combat_checkescapes(t_move);
                    f = 0;
                    for (s = 0; s < MAX_COMBAT_SHIPS; s++)
                        if (cships[s].type > -1 && cships[s].escaped == 0 && cships[s].active > 0)
                        {
                            if ((cships[s].own & 1) == 0)
                            {
                                if (cships[playership].type > -1)
                                    f |= 1;
                            }
                            else
                                f |= 2;
                        }

                    if (f != 3)
                    {
                        if (end == 0)
                        {
                            end = t + 100;
                        }
                    }
                    else
                        end = 0;

                    if (AsBool(key_pressed(key_up)) && camera.z < 256)
                        camera.z++;
                    if (AsBool(key_pressed(key_down)) && camera.z > 4)
                        camera.z--;

                    prep_screen();
                    if (AsBool(wants_screenshot))
                        ik_save_screenshot(screen, globalpal);

                    ik_drawbox(screen, 0, 0, 640, 480, 0);

                    if (pause < 1)
                    {
                        while (t0 < t)
                        {
                            t0++;
                            s = 1 + 2 * AsInt(pause == -1);
                            while (s-->0)
                            {
                                t_move++;
                                combat_movement(t_move);
                                if (t_move == klaktime + 1 && klaktime > 0)
                                    Play_SoundFX((int)sfxsamples.WAV_HYPERDRIVE, get_ik_timer(1));
                            }
                        }
                    }
                    if (t_move > t_disp || (pause == 1))
                    {
                        t_disp = t_move;
                        combat_display(t_disp);
                    }

                    ik_blit();
                    if (AsBool(settings.random_names & 4))
                    {
                        interface_tutorial((int)tutorial_pages.tut_combat);
                        ik_eventhandler();  // always call every frame
                        t = get_ik_timer(1);
                        t0 = t;
                    }
                }
            }

            combat_end(flt);

            if (!AsBool(simulated))
                Stop_All_Sounds();

            return 1;
        }

        private static void ik_save_screenshot(t_ik_image screen, t_paletteentry globalpal)
        {
            throw new NotImplementedException();
        }

        // ----------------
        // LOCAL FUNCTIONS
        // ----------------

        public static void select_ship(Int32 s, Int32 t)
        {
            camera.ship_sel = s;
            camera.time_sel = t;
            camera.ship_trg = cships[s].target;
            camera.time_trg = t;
        }

        public static void combat_start(Int32 flt)
        {
            int t, p;
            int r, s;
            int x, y;
            int nc, rc, nf;
            Int32 angle;

            //srand((unsigned)time(null));

            retreat = 0;

            if (AsBool(simulated))
                nebula = 0;
            else if (sm_nebulamap[((240 - player.y) << 9) + (240 + player.x)] > 0)
                nebula = 1;
            else
                nebula = 0;


            for (t = 0; t < MAX_COMBAT_SHIPS; t++)
            {
                cships[t].type = -1;
                cships[t].own = -1;
                for (p = 0; p < 8; p++)
                    cships[t].wepfire[p] = 0;
                cships[t].target = -1;
                cships[t].tac = -1;
                cships[t].teltime = 0;
                cships[t].bong_start = 0;
                cships[t].bong_end = 0;
                cships[t].aistart = 0;
                cships[t].launchtime = 100;
                cships[t].flee = 0;
            }
            for (t = 0; t < MAX_COMBAT_PROJECTILES; t++)
                cprojs[t].wep = null;
            for (t = 0; t < MAX_COMBAT_BEAMS; t++)
                cbeams[t].wep = null;
            for (t = 0; t < MAX_COMBAT_EXPLOS; t++)
                cexplo[t].spr = null;
            camera.x = 0;
            camera.y = 0;
            camera.z = 4096;

            camera.ship_sel = -1;
            camera.ship_trg = -1;

            angle = rand() % 1024;

            t = 0;
            for (p = 0; p < player.num_ships; p++)
            {
                if (p == 0)
                { cships[t].x = 0; playership = t; }
                else
                {
                    cships[t].x = ((t + 1) / 2) * 128;
                    if (AsBool(p & 1))
                        cships[t].x = -cships[t].x;
                }
                cships[t].y = -700;
                cships[t].a = 0;
                cships[t].type = player.ships[p];
                cships[t].own = 0;
                t++;
            }

            s = sm_fleets[flt].num_ships;       //rand()%4 + 1;
            rc = sm_fleets[flt].race;

            // place enemy ships
            nc = 0; nf = 0;
            for (p = 0; p < s; p++)
            {
                if (hulls[shiptypes[sm_fleets[flt].ships[p]].hull].size >= 32)
                {
                    if (!AsBool(shiptypes[sm_fleets[flt].ships[p]].flag & 128))   // if not deep hunter
                        nc++;
                }
                else
                { nf++; }
            }

            for (p = 0; p < s; p++)
            {
                cships[t].type = sm_fleets[flt].ships[p];
                cships[t].own = 1;
                if (hulls[shiptypes[cships[t].type].hull].size >= 32)
                { r = 1; }
                else
                { r = 0; }
                if (AsBool(shiptypes[cships[t].type].flag & 128))
                { r = 2; }

                switch (rc)
                {
                    case (int)race_ids.race_garthan:  // V formation
                        cships[t].x = ((p + 1) / 2) * 96;
                        if (AsBool(p & 1))
                            cships[t].x = -cships[t].x;
                        cships[t].y = 700 + ((p + 1) / 2) * 64;
                        cships[t].a = 512;
                        break;


                    case (int)race_ids.race_muktian:
                        if (AsBool(r))  // corvette
                        {
                            if (nc == 3)
                                cships[t].x = 96 + 96 * (AsInt(p == 1) - AsInt(p == 2));
                            else
                                cships[t].x = p * 96;
                            cships[t].y = 700;
                        }
                        else    // fighter circle
                        {
                            y = 1024 * (p - nc) / (sm_fleets[flt].num_ships - nc);
                            x = Math.Max(nc - 1, 0);
                            cships[t].x = x * 48 + ((sin1k[y] * (128 + x * 64)) >> 16);
                            cships[t].y = 700 - ((cos1k[y] * 128) >> 16);
                        }
                        cships[t].a = 512;
                        break;

                    case (int)race_ids.race_tanru:    // grids
                        if (AsBool(r))  // corvette
                        {
                            cships[t].x = -64 + 128 * (p & 1);
                            cships[t].y = 700 + 128 * (p / 2);
                        }
                        else    // fighter
                        {
                            cships[t].x = -512 + 1024 * (flt & 1) + 128 * ((p - nc) & 1);
                            cships[t].y = 700 + 128 * ((p - nc) / 2);
                        }
                        cships[t].a = 512;
                        break;

                    case (int)race_ids.race_urluquai:
                        x = p;
                        if (r == 2)
                        {
                            y = rand() % 1024;
                        }
                        else
                        {
                            //if (sm_fleets[flt].num_ships != nc+nf) x = p - (sm_fleets[flt].num_ships-(nc+nf));	// don't count deep hunters in formation
                            // 	y = ((x+1)/2)*512/((sm_fleets[flt].num_ships+1)/2);
                            //if (x&1)
                            //	y = -y;
                            x = p - (sm_fleets[flt].num_ships - (nc + nf));
                            y = (x * 1024) / (nc + nf);
                            y = y - ((nc / 2) * 512) / (nc + nf);
                        }
                        y = (1024 + y) & 1023;
                        cships[t].x = (sin1k[y] * 1400) >> 16;
                        cships[t].y = ((cos1k[y] * 1400) >> 16) - 700;
                        cships[t].a = (y + 512) & 1023;
                        break;


                    default:
                        cships[t].x = ((p + 1) / 2) * 128;
                        if (AsBool(p & 1))
                            cships[t].x = -cships[t].x;
                        cships[t].y = 700;
                        cships[t].a = 512;
                        break;
                }
                cships[t].aistart = 100;

                t++;
            }

            for (t = 0; t < MAX_COMBAT_SHIPS; t++)
                if (cships[t].type > -1)
                {
                    x = cships[t].x; y = cships[t].y;
                    cships[t].x = (x * cos1k[angle] + y * sin1k[angle]) >> 6;
                    cships[t].y = (y * cos1k[angle] - x * sin1k[angle]) >> 6;
                    cships[t].a = cships[t].a + angle & 1023;
                }

            for (t = 0; t < MAX_COMBAT_SHIPS; t++)
                if (cships[t].type > -1)
                {
                    cships[t].vx = 0;
                    cships[t].vy = 0;
                    cships[t].va = 0;
                    cships[t].hits = shiptypes[cships[t].type].hits / 256; //hulls[shiptypes[cships[t].type].hull].hits;
                    cships[t].shld = 0;
                    cships[t].shld_time = 0;
                    cships[t].shld_charge = 0;
                    cships[t].damage_time = 0;
                    cships[t].dmgc_time = 0;
                    cships[t].tac = 0;
                    cships[t].escaped = 0;
                    cships[t].active = 2;
                    if (shiptypes[cships[t].type].race == (int)race_ids.race_unknown)
                    {
                        cships[t].active = 1; cships[t].va = (rand() % 3 + 1) * ((rand() & 1) * 2 - 1);
                        if (AsBool(sm_fleets[flt].system & 1))  // 50% chance of being dead
                        {
                            cships[t].hits = 1;
                        }
                    }
                    cships[t].cloaked = 0;
                    cships[t].cloaktime = 0;

                    for (x = 0; x < shiptypes[cships[t].type].num_systems; x++)
                    {
                        cships[t].syshits[x] = 10 * AsInt(shiptypes[cships[t].type].sysdmg[x] == 0);
                    }
                    combat_updateshipstats(t, 0);

                    if (cships[t].clo_type > 0)
                    { cships[t].cloaked = 1; cships[t].cloaktime = 0; }
                    if (cships[t].shld_type > -1)
                        cships[t].shld = shipsystems[cships[t].shld_type].par[0];

                    combat_findstuff2do(t, 0);
                }


            klaktime = 0;
            klakavail = 0;
            for (t = 0; t < STARMAP_MAX_FLEETS; t++)
                if (sm_fleets[t].race == (int)race_ids.race_klakar && sm_fleets[t].num_ships > 0)
                {
                    for (s = 0; s < player.num_items; s++)
                    {
                        if (AsBool(itemtypes[player.items[s]].flag & 4))
                            klakavail = 1;
                    }
                }

            gongavail = 0;
            for (t = 0; t < player.num_items; t++)
            {
                if (AsBool(itemtypes[player.items[t]].flag & (int)item_deviceflags.device_gong))
                    gongavail = 2;
            }

            camera.ship_sel = playership;
            camera.time_sel = 0;

        }

        public static void klakar_pissoff()
        {
            Int32 mc, c;
            Int32 end = 0;
            Int32 bx = 216, by = 152;
            Int32 mx = 0, my = 0;
            Int32 r = 0, t = 0;
            char[] str=new char[256];

            r = (int)race_ids.race_klakar;

            halfbritescreen();

            // trader greeting screen
            prep_screen();
            sprintf(str, textstring[(int)textstrings.STR_VIDCAST], races[r].name);
            interface_drawborder(screen,
                                                     bx, by, bx + 208, by + 144,
                                                     1, STARMAP_INTERFACE_COLOR, str);
            ik_print(screen, font_6x8, bx + 16, by + 26, 3, textstring[(int)textstrings.STR_VIDCAST2]);
            interface_textbox(screen, font_4x8,
                                                bx + 88, by + 40, 104, 64, 0,
                                                textstring[(int)textstrings.STR_KLAK_NOPAY]);

            ik_dsprite(screen, bx + 16, by + 40, spr_SMraces.spr[(int)race_ids.race_klakar], 0);
            ik_dsprite(screen, bx + 16, by + 40, spr_IFborder.spr[18], 2 + (STARMAP_INTERFACE_COLOR << 8));
            interface_drawbutton(screen, bx + 128, by + 116, 64, STARMAP_INTERFACE_COLOR, textstring[(int)textstrings.STR_OK]);

            ik_blit();

            Play_Sound((int)sfxsamples.WAV_MESSAGE, 15, 1);

            while (!must_quit && !AsBool(end))
            {
                ik_eventhandler();  // always call every frame
                mc = ik_mclick();
                c = ik_inkey();
                mx = ik_mouse_x - bx; my = ik_mouse_y - by;

                if (mc == 1 && mx > 128 && mx < 192 && my > 116 && my < 132)
                { end = 2; Play_SoundFX((int)sfxsamples.WAV_DOT, get_ik_timer(0)); }

                c = t; t = get_ik_timer(2);
                if (t != c)
                { prep_screen(); ik_blit(); }
            }

            Stop_Sound(15);

            reshalfbritescreen();

            prep_screen();
            ik_blit();
        }

        private static void sprintf(char[] str, char[] v, char[] name)
        {
            throw new NotImplementedException();
        }

        public static void combat_removeenemyship(Int32 flt, Int32 s)
        {
            Int32 c;

            for (c = s; c < sm_fleets[flt].num_ships; c++)
            {
                sm_fleets[flt].ships[c] = sm_fleets[flt].ships[c + 1];
            }
            sm_fleets[flt].num_ships--;
        }

        public static void combat_sim_end()
        {
            Int32 end;
            Int32 c, mc;
            Int32 bx = 192, by = 96, h = 232;
            Int32 mx, my;
            Int32 t, ot;
            Int32 s, co;
            Int32[] en=new Int32[3];
            t_ik_image bg;

            if (must_quit)
                return;

            Stop_All_Sounds();
            bg = ik_load_pcx("graphics/starback.pcx".ToCharArray(), null);

            Play_SoundFX((int)sfxsamples.WAV_ENDSIMULATION);

            end = 0; t = get_ik_timer(2);
            while (!AsBool(end) && !must_quit)
            {
                ik_eventhandler();
                c = ik_inkey();
                mc = ik_mclick();
                mx = ik_mouse_x - bx;
                my = ik_mouse_y - by;

                if (c == 13 || c == 32)
                    end = 1;

                if (AsBool(mc & 1))
                    if (mx > 240 - 64 && mx < 240 - 16 && my > h - 24 && my < h - 8)
                    { end = 1; Play_SoundFX((int)sfxsamples.WAV_DOT2, 0, 50); }

                ot = t;
                t = get_ik_timer(2);
                if (t != ot)
                {
                    prep_screen();
                    ik_copybox(bg, screen, 0, 0, 640, 480, 0, 0);

                    interface_drawborder(screen, bx, by, bx + 240, by + h, 1, STARMAP_INTERFACE_COLOR, textstring[(int)textstrings.STR_COMBAT_SIMEND]);

                    ik_print(screen, font_6x8, bx + 120 - 12 * 3, by + 24, 4, textstring[(int)textstrings.STR_COMBAT_SIMALLY]);
                    for (c = 0; c < player.num_ships; c++)
                    {
                        s = AsInt(cships[c].hits > 0);
                        interface_thinborder(screen, bx + 16 + c * 72, by + 36, bx + 80 + c * 72, by + 108, s * STARMAP_INTERFACE_COLOR + (1 - s), 0);
                        ik_drsprite(screen, bx + 48 + c * 72, by + 76, 0, 64, hulls[shiptypes[player.ships[c]].hull].sprite, 1 + ((s * 15 + (1 - s) * 26) << 8));
                        if (AsBool(s))
                            ik_print(screen, font_6x8, bx + 20 + c * 72, by + 40, 4, textstring[(int)textstrings.STR_COMBAT_SIMSURV]);
                        else
                            ik_print(screen, font_6x8, bx + 20 + c * 72, by + 40, 1, textstring[(int)textstrings.STR_COMBAT_SIMDEST]);
                    }
                    for (; c < 3; c++)
                    {
                        interface_thinborder(screen, bx + 16 + c * 72, by + 36, bx + 80 + c * 72, by + 108, STARMAP_INTERFACE_COLOR, STARMAP_INTERFACE_COLOR * 16 + 2);
                    }

                    ik_print(screen, font_6x8, bx + 120 - 11 * 3, by + 120, 1, textstring[(int)textstrings.STR_COMBAT_SIMENMY]);
                    ik_print(screen, font_6x8, bx + 16, by + 140, 0, textstring[(int)textstrings.STR_COMBAT_SIMSURV]);
                    ik_print(screen, font_6x8, bx + 16, by + 162, 0, textstring[(int)textstrings.STR_COMBAT_SIMESCP]);
                    ik_print(screen, font_6x8, bx + 16, by + 184, 0, textstring[(int)textstrings.STR_COMBAT_SIMDEST]);

                    // count enemies for each row
                    for (c = 0; c < 3; c++)
                    {
                        interface_thinborder(screen, bx + 76, by + 134 + c * 22, bx + 79 + 144, by + 153 + c * 22, STARMAP_INTERFACE_COLOR, 0);
                        en[c] = 0;
                    }
                    for (c = 0; c < sm_fleets[0].num_ships; c++)
                    {
                        s = 0;
                        if (cships[player.num_ships + c].hits <= 0) s = 2;
                        else if (cships[player.num_ships + c].type == -1) s = 1;

                        co = 15 * AsInt(s == 0) + 58 * AsInt(s == 1) + 26 * AsInt(s == 2);
                        ik_drsprite(screen, bx + 83 + en[s] * 12, by + 143 + s * 22, 0, 16, hulls[shiptypes[sm_fleets[0].ships[c]].hull].sprite, 1 + (co << 8));

                        en[s]++;
                    }
                    for (c = 0; c < 3; c++)
                        if (en[c] == 0)
                            interface_thinborder(screen, bx + 76, by + 134 + c * 22, bx + 79 + 144, by + 153 + c * 22, STARMAP_INTERFACE_COLOR, STARMAP_INTERFACE_COLOR * 16 + 2);

                    interface_drawbutton(screen, bx + 240 - 64, by + h - 24, 48, STARMAP_INTERFACE_COLOR, textstring[(int)textstrings.STR_OK]);

                    ik_blit();
                }
            }

            if (must_quit)
                must_quit = false;

            del_image(bg);
        }

        public static void combat_end(Int32 flt)
        {
            Int32 c;
            Int32 it;
            Int32 b;
            Int32 f;
            Int32 x;
            Int32 de = -1;
            char[] texty=new char[256];

            if (AsBool(simulated))
            {
                combat_sim_end();
                return;
            }

            for (c = 0; c < player.num_ships; c++)
                if (cships[c].type > -1)
                {
                    shiptypes[cships[c].type].hits = cships[c].hits * 256;
                    for (x = 0; x < shiptypes[cships[c].type].num_systems; x++)
                    {
                        if (cships[c].syshits[x] <= 0 && shipsystems[shiptypes[cships[c].type].system[x]].item > -1)
                        {
                            shiptypes[cships[c].type].sysdmg[x] = 1;
                            if ((de == -1) && (c == playership) &&
                                    (rand() % 10 == 0) &&
                                    (shipsystems[shiptypes[cships[c].type].system[x]].type !=(int)system_types.sys_shield))
                            {
                                de = shiptypes[cships[c].type].system[x];
                                starmap_destroysystem(x);
                            }
                        }
                    }
                }

            for (c = MAX_COMBAT_SHIPS - 1; c >= player.num_ships; c--)
            {
                if (cships[c].own == 1 && (cships[c].hits <= 0 || cships[c].type == -1))
                {
                    if (c - player.num_ships < sm_fleets[flt].num_ships)
                        combat_removeenemyship(flt, c - player.num_ships);
                }
            }

            for (c = player.num_ships - 1; c >= 0; c--)
            {
                if (cships[c].hits < 1)
                    starmap_removeship(c);
            }

            sm_fleets[flt].explored = 2;

            for (c = 0; c < MAX_COMBAT_SHIPS; c++)
            {
                if (cships[c].hits > 0 && cships[c].type > -1 && cships[c].own == 1)
                {
                    flt = -1;
                }
            }

            if (flt > -1)
            {
                sm_fleets[flt].num_ships = 0;
                player.bonusdata += 300;
            }

            for (c = 0; c < MAX_COMBAT_SHIPS; c++)
            {
                if (cships[c].own == 2)         // klakar
                {
                    f = -1;
                    for (b = 0; b < STARMAP_MAX_FLEETS; b++)
                        if (sm_fleets[b].race == (int)race_ids.race_klakar)
                            f = b;
                    if (cships[c].type > -1 && cships[c].hits > 0)  // survived
                    {
                        if (player.num_ships > 0 && player.ships[0] == 0)   // player survives to pay
                        {
                            it = pay_item(textstring[(int)textstrings.STR_KLAK_PAYTITLE], textstring[(int)textstrings.STR_KLAK_PAYMENT], (int)race_ids.race_klakar, 1);
                            if (it > -1)
                            {
                                kla_items[kla_numitems++] = it;
                            }
                            else    // take beacon away if you don't pay!
                            {
                                // display pissoff message
                                klakar_pissoff();
                                for (it = 0; it < player.num_items; it++)
                                    if (AsBool(itemtypes[player.items[it]].flag & 4))
                                        starmap_removeitem(it);
                                sm_fleets[f].num_ships = 0;
                            }
                        }
                        // check if escaped
                        if (flt == -1)
                        {
                            it = -1;
                            while (it == -1 && !must_quit)
                            {
                                ik_eventhandler();
                                it = rand() % num_stars;
                                b = get_distance(sm_stars[it].x - sm_stars[player.system].x,
                                                                 sm_stars[it].y - sm_stars[player.system].y);
                                if (b < 100)
                                    it = -1;
                                else if (it == homesystem)
                                    it = -1;
                                else if (sm_stars[it].color < 0)
                                    it = -1;
                                else
                                    for (b = 0; b < STARMAP_MAX_FLEETS; b++)
                                        if (sm_fleets[b].num_ships > 0 && b != f && sm_fleets[b].system == it)
                                            it = -1;
                            }
                            if (it > -1)
                                sm_fleets[f].system = it;
                        }
                    }
                    else    // klakar destroyed
                    {
                        sm_fleets[f].num_ships = 0;
                    }
                }
            }

            if (de > -1 && player.num_ships > 0 && player.ships[0] == 0)
            {   // system was destroyed
                sprintf(texty, textstring[(int)textstrings.STR_SYSTEM_DESTROYED], player.shipname, shipsystems[de].name);
                interface_popup(font_6x8, 224, 192, 192, 96, STARMAP_INTERFACE_COLOR, 0,
                                                textstring[(int)textstrings.STR_COMBAT_SYSDMG], texty, textstring[(int)textstrings.STR_OK]);
            }

        }

        private static void sprintf(char[] texty, char[] v, char[] shipname, char[] name)
        {
            throw new NotImplementedException();
        }

        public static void combat_movement(Int32 t)
        {
            Int32 c = 0;
            Int32 d = 0;
            Int32 p = 0;
            Int32 a = 0;
            Int32 sys = 0, lsys = 0;
            Int32 r = 0, rm = 0;
            Int32 tg=0, wx = 0, wy = 0;
            Int32 sp = 0, sx = 0, sy = 0;
            t_hull hull;

            // **** MOVE SHIPS ****
            for (c = 0; c < MAX_COMBAT_SHIPS; c++)
                if (cships[c].type > -1)
                {
                    cships[c].x += cships[c].vx;
                    cships[c].y += cships[c].vy;
                    cships[c].a = (cships[c].a + cships[c].va) & 1023;

                    if (cships[c].cloaktime > 0 && t == cships[c].cloaktime + 1)
                    {
                        if (AsBool(cships[c].cloaked))
                            combat_SoundFX((int)sfxsamples.WAV_CLOAKIN, cships[c].x);
                        else
                            combat_SoundFX((int)sfxsamples.WAV_CLOAKOUT, cships[c].x);
                    }

                    if (cships[c].teltime > 0)
                    {
                        if (t - cships[c].teltime >= 32 && (cships[c].tel_x != 0 || cships[c].tel_y != 0))
                        {
                            cships[c].x += cships[c].tel_x;
                            cships[c].y += cships[c].tel_y;
                            cships[c].tel_x = 0;
                            cships[c].tel_y = 0;
                            //cexplo[combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 0, 96, 1, t, t+24, 1)].str = t-8;
                            combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 1, 112, 0, t, t + 10, 4);
                        }
                    }

                    if (AsBool(shiptypes[cships[c].type].flag & 16))
                    {
                        if (t > cships[c].launchtime && cships[c].hits > 0)
                        {
                            p = racefleets[races[shiptypes[cships[c].type].race].fleet].stype[0];
                            a = 0;
                            for (d = 0; d < MAX_COMBAT_SHIPS; d++)
                                if (cships[d].type == p && cships[d].hits > 0)
                                    if (cships[d].own == cships[c].own)
                                        a++;
                            if (a < 3)  // don't launch more than 3 at once
                            {
                                combat_SoundFX((int)sfxsamples.WAV_FIGHTERLAUNCH, cships[c].x);
                                cships[c].launchtime = t + 200;
                            }
                        }
                        if (t == cships[c].launchtime - 150)
                            combat_launch_fighter(c, t);
                    }


                    if (cships[c].bong_start > 0 && cships[c].hits > 0 && cships[c].type > -1)
                    {
                        if (t < cships[c].bong_end)
                        {
                            if (t == cships[c].bong_start + 50)
                                Play_SoundFX((int)sfxsamples.WAV_FIERYFURY, t);
                            else if (t > cships[c].bong_start + 50)
                            {
                                d = t % 5;
                                p = (t - cships[c].bong_start - 50);
                                p = ((p + 50) * (p + 50) - 2500) >> 6;
                                a = (205 * d + p) & 1023;
                                r = 2000 * (cships[c].bong_end - t) / (cships[c].bong_end - cships[c].bong_start - 50);
                                sx = cships[c].x + ((sin1k[a] * r) >> 6);
                                sy = cships[c].y + ((cos1k[a] * r) >> 6);

                                combat_addexplo(sx, sy, spr_explode1, 1, 96, 0, t, t + 32, -1, 0);
                            }
                        }
                        else
                        {
                            if (AsBool(settings.opt_lensflares))
                            {
                                combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 1, 112, 0, t, t + 10, 4, 0);
                                combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 0, 512, 1, t, t + 10, 3, 0);
                            }
                            combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 0, 512, 1, t, t + 20, 2, 0);
                            combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 0, 512, 1, t, t + 40, 1, 0);
                            cships[c].bong_start = 0;

                            if (shiptypes[cships[c].type].race == (int)race_ids.race_unknown) // stop space hulk
                            {
                                cships[c].va = rand() % cships[c].turn + 1;
                                if (AsBool(rand() & 1))
                                    cships[c].va = -cships[c].va;

                                cships[c].vx = cships[c].vy = 0;
                            }

                            combat_SoundFX((int)sfxsamples.WAV_EXPLO1, cships[c].x);
                            combat_killship(c, t);
                        }
                    }


                }

            // **** COMBAT AI ****
            for (c = 0; c < MAX_COMBAT_SHIPS; c++)
                if (cships[c].type > -1 && cships[c].active > 0 && t > cships[c].aistart)
                {
                    if (cships[c].hits > 0 && cships[c].active == 2)
                    {
                        // shield recharge
                        if (cships[c].shld_type > -1 && t > cships[c].shld_charge)
                        {
                            p = (shipsystems[cships[c].shld_type].par[1] * cships[c].syshits[cships[c].sys_shld]) / 10;
                            if (AsBool(p))
                            {
                                if (cships[c].shld < shipsystems[cships[c].shld_type].par[0])
                                {
                                    cships[c].shld++;
                                    cships[c].shld_charge = t + 50 / p;
                                }
                            }
                        }
                        // damage control
                        if (t > cships[c].dmgc_time)
                        {
                            if (cships[c].dmgc_type > -1)
                                p = (shipsystems[cships[c].dmgc_type].par[0] * cships[c].syshits[cships[c].sys_dmgc]) / 10;

                            else if (shiptypes[cships[c].type].race == (int)race_ids.race_kawangi)
                                p = 10;

                            else
                                p = 0;
                            if (p > 0 && cships[c].hits < hulls[shiptypes[cships[c].type].hull].hits)
                            {
                                cships[c].hits++;
                                cships[c].dmgc_time = t + 50 / p;
                            }

                            // repair broken systems
                            if (p == 0 && (shiptypes[cships[c].type].race == (int)race_ids.race_none || shiptypes[cships[c].type].race == (int)race_ids.race_terran))
                                p = 1;
                            if (p > 0)
                            {
                                sys = -1; lsys = -1;
                                for (d = 0; d < shiptypes[cships[c].type].num_systems; d++)
                                    if (cships[c].syshits[d] < 10 && (cships[c].syshits[d] > 0 || p == 10))
                                    {   // don't repair if zero (lost) unless kawangi
                                        if (lsys == -1 || cships[c].syshits[d] < lsys)
                                        {
                                            lsys = cships[c].syshits[d];
                                            sys = d;
                                        }
                                    }
                                if (sys > -1)
                                {
                                    cships[c].syshits[sys]++;
                                    if (cships[c].syshits[sys] == 10 && c == playership)    // fixed
                                        Play_SoundFX((int)sfxsamples.WAV_SYSFIXED, get_ik_timer(1));

                                    cships[c].dmgc_time = t + 50 / p;
                                }
                                combat_updateshipstats(c, t);
                            }
                        }

                        if (cships[c].flee == 0)
                        {
                            //				if (cships[c].own == 1 && cships[c].hits < hulls[shiptypes[cships[c].type].hull].hits)
                            if (cships[c].own == 1 && cships[c].frange == 0)    // lost guns, flee!
                            {
                                cships[c].flee = 1;
                                cships[c].tac = (int)combat_tactics.tac_flee;
                                cships[c].target = -1;
                            }
                        }

                        if (cships[c].target > -1 && cships[cships[c].target].own != cships[c].own)
                        {
                            // check if lost target due to cloaking
                            // change tactics here (?)
                            if (cships[cships[c].target].cloaked == 1)
                            {
                                tg = cships[c].target;
                                cships[c].patx = cships[c].wp_x = cships[tg].x - ((sin1k[cships[c].angle] * cships[c].dist) >> 6);
                                cships[c].paty = cships[c].wp_y = cships[tg].y - ((cos1k[cships[c].angle] * cships[c].dist) >> 6);
                                cships[c].target = -1;
                                cships[c].tac = 2;
                            }
                            // check if wants to cloak or decloak (enemy only)
                            if (c != playership && AsBool(cships[c].clo_type) && cships[c].syshits[cships[c].sys_clo] >= 5)
                            {
                                rm = cships[c].frange;
                                if (rm > 0 && t - cships[c].cloaktime > 100)
                                {
                                    d = cships[c].target;
                                    r = get_distance((cships[c].x - cships[d].x) >> 10, (cships[c].y - cships[d].y) >> 10);
                                    if (r < rm && cships[c].tac == 1)
                                    {
                                        if (AsBool(cships[c].cloaked))  // uncloak when at weapon range
                                        {
                                            cships[c].cloaked = 0;
                                            cships[c].cloaktime = t;
                                            //								Play_SoundFX(WAV_CLOAKOUT, get_ik_timer(1));
                                        }
                                    }
                                    else
                                    {
                                        if (!AsBool(cships[c].cloaked))
                                        {
                                            cships[c].cloaked = 1;
                                            cships[c].cloaktime = t;
                                            //								Play_SoundFX(WAV_CLOAKIN, get_ik_timer(1));
                                        }
                                    }
                                }
                            }
                        }

                        // movement
                        sp = cships[c].speed;
                        if (cships[c].target > -1)
                        {
                            if (cships[c].own != 0 && t % MAX_COMBAT_SHIPS == c)
                                combat_checkalttargets(c, t);
                            tg = cships[c].target;
                            if (cships[c].tac == 0) // waypoint
                            {
                                wx = cships[tg].x - ((sin1k[cships[c].angle] * cships[c].dist) >> 6);
                                wy = cships[tg].y - ((cos1k[cships[c].angle] * cships[c].dist) >> 6);

                                if (cships[c].own != 0 && t > cships[c].wp_time)    // frustration time
                                    cships[c].tac = 1;
                            }
                            else if (cships[c].tac == 1)    // attack
                            {
                                wx = cships[tg].x;
                                wy = cships[tg].y;
                            }
                            a = get_direction((wx - cships[c].x) >> 10, (wy - cships[c].y) >> 10);
                            r = get_distance((wx - cships[c].x) >> 10, (wy - cships[c].y) >> 10);


                            if (r > 80 && cships[c].tac == 0 && (shiptypes[cships[c].type].flag & 2) > 0)
                            {
                                if (t - cships[c].teltime > 4 * 50 || cships[c].teltime == 0)
                                {
                                    cships[c].tel_x = wx - cships[c].x;
                                    cships[c].tel_y = wy - cships[c].y;
                                    cships[c].teltime = t;
                                    combat_SoundFX((int)sfxsamples.WAV_TELEPORT, cships[c].x);
                                    //cexplo[combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 0, 96, 1, t, t+24, 1)].str = t-8;
                                    combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 1, 112, 0, t, t + 10, 4);
                                }
                            }



                            if ((cships[tg].own & 1) == (cships[c].own & 1)) // escort
                            {
                                if (r < 64)
                                {
                                    a = cships[tg].a;
                                    sx = (((wx - cships[c].x) >> 10) * cos1k[a] - ((wy - cships[c].y) >> 10) * sin1k[a]) >> 16;
                                    sy = (((wy - cships[c].y) >> 10) * cos1k[a] + ((wx - cships[c].x) >> 10) * sin1k[a]) >> 16;
                                    if (sx < -5) a = (1024 + a - 50) & 1023;
                                    if (sx > 5) a = (1024 + a + 50) & 1023;
                                    sp = get_distance((cships[tg].vx * 50) >> 10, (cships[tg].vy * 50) >> 10);
                                    //						sp = shiptypes[cships[tg].type].speed;
                                    if (sy < -5) sp = Math.Max(0, sp - 50);
                                    if (sy > 5) sp += 50;
                                    if (sp == 0)
                                        a = cships[tg].a;
                                }
                            }
                            else // attack
                            {
                                if ((r < 64 && cships[c].tac == 0) || (r < 128 && cships[c].tac == 1))
                                {
                                    if (cships[c].tac == 0) // reached waypoint, start attack run
                                        cships[c].tac = 1;
                                    else if (cships[c].tac == 1)    // close proximity, return to waypoint
                                    {
                                        cships[c].tac = 0;
                                        cships[c].wp_time = t + 500 + rand() % 500;
                                        if (cships[c].own != 0) // enemy or klakar
                                        {   // get a new angle of attack
                                            cships[c].angle = (cships[tg].a + 768 + rand() % 512) & 1023;
                                        }
                                        else if (cships[cships[c].target].active > 0 && (cships[c].cloaked == 1 || cships[cships[c].target].active == 1))   // check for spacehulk sneak-up victory
                                        {
                                            if (AsBool(shiptypes[cships[cships[c].target].type].flag & 256))
                                            {
                                                combat_SoundFX((int)sfxsamples.WAV_BOARD, cships[c].x);
                                                cships[cships[c].target].active = 0;
                                                cships[cships[c].target].hits = 0;
                                            }
                                        }

                                    }
                                    //a = get_direction( (cships[tg].x - cships[c].x)>>10, (cships[tg].y - cships[c].y)>>10 );
                                }
                            }
                            if (cships[c].own == 2 && cships[c].target == playership)   // klakar escort
                                combat_findstuff2do(c, t);

                        }
                        else
                        {
                            if (cships[c].tac == 2) // move to waypoint
                            {
                                a = get_direction((cships[c].wp_x - cships[c].x) >> 10, (cships[c].wp_y - cships[c].y) >> 10);
                                r = get_distance((cships[c].wp_x - cships[c].x) >> 10, (cships[c].wp_y - cships[c].y) >> 10);
                                if (AsBool(shiptypes[cships[c].type].flag & 2))
                                {
                                    if (r > 80 && (t - cships[c].teltime > 4 * 50 || cships[c].teltime == 0))
                                    {
                                        cships[c].tel_x = cships[c].wp_x - cships[c].x;
                                        cships[c].tel_y = cships[c].wp_y - cships[c].y;
                                        cships[c].teltime = t;
                                        combat_SoundFX((int)sfxsamples.WAV_TELEPORT, cships[c].x);
                                        //cexplo[combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 0, 96, 1, t, t+24, 1)].str = t-8;
                                        combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 1, 112, 0, t, t + 10, 4);
                                    }
                                }


                                if (r > 80)
                                    sp = cships[c].speed;
                                else if (r > 16)
                                {
                                    //						if (!cships[c].own&1)
                                    sp = (cships[c].speed * r) / 80 + 1;
                                    //						else
                                    //							sp = cships[c].speed;
                                }
                                else
                                {
                                    sp = 0;
                                    if (cships[c].own != 0) // reached waypoint, do search pattern
                                    {
                                        a = rand() & 1023;
                                        cships[c].wp_x = cships[c].patx + ((sin1k[a] * cships[c].dist) >> 6);
                                        cships[c].wp_y = cships[c].paty + ((cos1k[a] * cships[c].dist) >> 6);
                                    }
                                }
                                if (cships[c].own != 0)
                                    combat_findstuff2do(c, t);
                            }
                            else if (cships[c].tac == (int)combat_tactics.tac_flee)
                            {
                                rm = -1;
                                a = -1;
                                for (d = 0; d < MAX_COMBAT_SHIPS; d++)
                                    if (!AsBool(cships[d].own & 1) && cships[d].hits>0)                                    
                                    {
                                        r = get_distance((cships[d].x - cships[c].x) >> 10, (cships[d].y - cships[c].y) >> 10);
                                        if (rm == -1 || r < rm)
                                        { a = d; rm = r; }
                                    }
                                if (a > -1)
                                {
                                    a = get_direction((cships[c].x - cships[a].x) >> 10, (cships[c].y - cships[a].y) >> 10);
                                    sp = cships[c].speed;
                                }
                                else
                                {
                                    a = cships[c].a;
                                    cships[c].flee = 0;
                                    combat_findstuff2do(c, t);
                                }
                            }
                            else if (c != playership)
                                combat_findstuff2do(c, t);
                        }
                        a = (a + 1024 - cships[c].a) & 1023;
                        if (a > 512) a -= 1024;
                        p = cships[c].turn;
                        cships[c].va = (AsInt(a > 0) - AsInt(a < 0)) * Math.Min(p, Math.Abs(a));
                        sx = (sin1k[cships[c].a] / 50 * sp) >> 6;
                        sy = (cos1k[cships[c].a] / 50 * sp) >> 6;   //    sp/50 << 10

                        cships[c].vx = sx;
                        cships[c].vy = sy;

                        if ((cships[c].own != 2 || t - klaktime > 100) && (!AsBool(cships[c].cloaked)))
                        {
                            hull = hulls[shiptypes[cships[c].type].hull];
                            for (p = 0; p < hull.numh; p++)
                                if (hull.hardpts[p].type == (int)hull_hardpttypes.hdpWeapon)
                                    if (t > cships[c].wepfire[p])
                                    {
                                        tg = combat_findtarget(cships[c], p);
                                        if (tg > -1)
                                            combat_fire(cships[c], p, cships[tg], t);
                                    }
                        }
                    }
                    else
                    {
                        if (cships[c].active == 2)
                        {

                            if (AsBool(cships[c].cloaked))
                            {
                                cships[c].cloaked = 0;
                                cships[c].cloaktime = t;
                                combat_SoundFX((int)sfxsamples.WAV_CLOAKOUT, cships[c].x);
                            }


                            hull = hulls[shiptypes[cships[c].type].hull];
                            if (!AsBool(rand() % 16) || cships[c].hits < -hull.hits)
                            {
                                cships[c].hits--;
                                combat_SoundFX((int)sfxsamples.WAV_EXPLO1, cships[c].x, 50);
                                combat_addexplo(cships[c].x + ((rand() % hull.size - hull.size / 2) << 9),
                                                                cships[c].y + ((rand() % hull.size - hull.size / 2) << 9),
                                                                spr_explode1, 5, hull.size / 2, 0, t, t + 32);
                                if (cships[c].hits <= -hull.hits)
                                {
                                    combat_killship(c, t);
                                }
                            }
                        }
                        else if (cships[c].active == 1 && cships[c].hits > 1)
                        {   // dormant space hulk waiting to activate
                            for (p = 0; p < MAX_COMBAT_SHIPS; p++)
                                if (cships[p].type > -1 && cships[p].own == 0 && cships[p].cloaked == 0)
                                {
                                    r = get_distance((cships[c].x - cships[p].x) >> 10, (cships[c].y - cships[p].y) >> 10);
                                    if (r < 400)
                                        cships[c].active = 2;
                                }
                        }
                    }
                }

            // **** MOVE SHOTS ****
            for (c = 0; c < MAX_COMBAT_BEAMS; c++)
                if (cbeams[c].wep!=null)
                {
                    if (t > cbeams[c].dmt)
                    {
                        if (cbeams[c].dst!=null)
                        {
                            combat_addexplo(cbeams[c].dst.x, cbeams[c].dst.y, spr_explode1, 5, 32, 0, t, t + 32);
                            a = shiptonum(cbeams[c].dst);
                            combat_damageship(a, 0, cbeams[c].wep.damage, t, cbeams[c].wep);
                            cbeams[c].dmt += 500;
                            d = AsInt((cbeams[c].wep.flags & (int)weapon_flagids.wpfShock1) > 0) + 2 * AsInt((cbeams[c].wep.flags & (int)weapon_flagids.wpfShock2) > 0);
                            if (d == 1)
                            {
                                cexplo[combat_addexplo(cbeams[c].dst.x, cbeams[c].dst.y, spr_shockwave, 0, 96, 1, t, t + 24, 1)].str = t - 8;
                                if (AsBool(settings.opt_lensflares))
                                    combat_addexplo(cbeams[c].dst.x, cbeams[c].dst.y, spr_shockwave, 1, 112, 0, t, t + 10, 4);
                            }
                            else if (d == 2)
                            {
                                cexplo[combat_addexplo(cbeams[c].dst.x, cbeams[c].dst.y, spr_shockwave, 0, 96, 1, t, t + 32, 3)].str = t - 8;
                                if (AsBool(settings.opt_lensflares))
                                    combat_addexplo(cbeams[c].dst.x, cbeams[c].dst.y, spr_shockwave, 1, 144, 0, t, t + 16, 4);
                            }
                        }
                    }
                    if (t > cbeams[c].end)
                    {
                        cbeams[c].wep = null;
                    }
                }

            for (c = 0; c < MAX_COMBAT_PROJECTILES; c++)
                if (cprojs[c].wep!=null)
                {
                    if (AsBool(cprojs[c].wep.flags & (int)weapon_flagids.wpfHoming))
                    {
                        if (cprojs[c].dst != null)
                        {
                            a = get_direction((cprojs[c].dst.x >> 10) - (cprojs[c].x >> 10), (cprojs[c].dst.y >> 10) - (cprojs[c].y >> 10));
                            a = (a + 1024 - cprojs[c].a) & 1023;
                            while (a > 512) a -= 1024;
                            if (a < -8) a = -8;
                            if (a > 8) a = 8;
                            cprojs[c].va = a;

                            if (AsBool(cprojs[c].wep.flags & (int)weapon_flagids.wpfSplit))
                            {
                                if (t > cprojs[c].str + 50)
                                {
                                    r = get_distance((cprojs[c].dst.x >> 10) - (cprojs[c].x >> 10), (cprojs[c].dst.y >> 10) - (cprojs[c].y >> 10));
                                    if (r < shipweapons[cprojs[c].wep.stage].range && cprojs[c].hits > 0) // split
                                    {
                                        cprojs[c].end = t;
                                        cprojs[c].hits = 0;
                                        if (cprojs[c].wep.item != -1)
                                        {
                                            if (AsBool(shipsystems[itemtypes[cprojs[c].wep.item].index].par[1]))
                                                combat_launchstages(c, shipsystems[itemtypes[cprojs[c].wep.item].index].par[1], t);
                                            else
                                                combat_launchstages(c, 5, t);
                                        }
                                        else
                                            combat_launchstages(c, 3, t);
                                    }
                                }
                            }

                            /*
                            if (cprojs[c].dst.ecm_type > -1 && cprojs[c].dst.syshits[cprojs[c].dst.sys_ecm]>0)
                            {
                                a = shipsystems[cprojs[c].dst.ecm_type].par[0];
                                if (rand()%300 < a)
                                    cprojs[c].dst = null;
                            }*/
                            if (cprojs[c].dst != null)
                                if (AsBool(cprojs[c].dst.cloaked))
                                    cprojs[c].dst = null;
                        }

                        cprojs[c].vx = (cprojs[c].vx * 15 + ((sin1k[cprojs[c].a] * cprojs[c].wep.speed / COMBAT_FRAMERATE) >> 6)) >> 4;
                        cprojs[c].vy = (cprojs[c].vy * 15 + ((cos1k[cprojs[c].a] * cprojs[c].wep.speed / COMBAT_FRAMERATE) >> 6)) >> 4;

                    }
                    if (AsBool(settings.opt_smoketrails))
                        if (AsBool(cprojs[c].wep.flags & (int)weapon_flagids.wpfStrail))
                            if (!AsBool((t + c) & 3))
                            {
                                d = cprojs[c].wep.size;
                                combat_addexplo(cprojs[c].x - ((sin1k[cprojs[c].a] * d) >> 7), cprojs[c].y - ((cos1k[cprojs[c].a] * d) >> 7), spr_weapons, 10, (d * 3) >> 2, 2, t, t + 35, 18, 0);
                            }
                    if (AsBool(cprojs[c].wep.flags & (int)weapon_flagids.wpfImplode))
                    {
                        if (!AsBool(t & 3))
                        {
                            cexplo[combat_addexplo(cprojs[c].x, cprojs[c].y, spr_shockwave, 0, 40, 1, t, t + 32, 2, 0)].str = t - 8;
                        }
                        if (!AsBool(t % 25))  // shoot electric death at random targets
                        {
                            p = 0; d = shipweapons[cprojs[c].wep.stage].range;
                            for (a = 0; a < MAX_COMBAT_SHIPS; a++)
                                if (cships[a].type > -1 && (cships[a].own & 1) != (cprojs[c].src.own & 1))
                                {
                                    if (get_distance((cships[a].x - cprojs[c].x) >> 10, (cships[a].y - cprojs[c].y) >> 10) < d)
                                        p++;
                                }
                            if (p > 0)
                            {
                                d = rand() % p;
                                p = -1;
                                for (a = 0; a < MAX_COMBAT_SHIPS; a++)
                                    if (cships[a].type > -1 && (cships[a].own & 1) != (cprojs[c].src.own & 1))
                                    {
                                        if (get_distance((cships[a].x - cprojs[c].x) >> 10, (cships[a].y - cprojs[c].y) >> 10) < shipweapons[cprojs[c].wep.stage].range)
                                        {
                                            if (!AsBool(d))
                                                p = a;
                                            d--;
                                        }
                                    }
                                if (p > -1)
                                {
                                    combat_addbeam(shipweapons[cprojs[c].wep.stage], cprojs[c].src, 0, cships[p], t, c);
                                    //						cexplo[combat_addexplo(cships[p].x, cships[p].y, spr_shockwave, 0, 128, 1, t, t+32, 3)].str = t-8;
                                    //						combat_addexplo(cships[p].x, cships[p].y, spr_shockwave, 1, 128, 0, t, t+16, 4);
                                }
                            }
                        }
                    }
                    cprojs[c].x += cprojs[c].vx;
                    cprojs[c].y += cprojs[c].vy;
                    cprojs[c].a = (cprojs[c].a + cprojs[c].va + 1024) & 1023;
                    for (p = 0; p < MAX_COMBAT_SHIPS; p++)
                        if (cships[p].type > -1 && (cships[p].own & 1) != (cprojs[c].src.own & 1))
                        {
                            a = hulls[shiptypes[cships[p].type].hull].size >> 1;
                            if (AsBool(cprojs[c].wep.flags & (int)weapon_flagids.wpfDisperse))
                            {
                                a += (2 + ((cprojs[c].wep.size - 4) * (t - cprojs[c].str)) / (cprojs[c].end - cprojs[c].str)) >> 1;
                            }
                            if (AsBool(cprojs[c].wep.flags & (int)(weapon_flagids.wpfImplode | weapon_flagids.wpfNoclip)))
                            {
                                a = -100;
                            }
                            //				if (t < cprojs[c].end - 2)
                            //					a = -100;
                            if (get_distance((cships[p].x >> 10) - (cprojs[c].x >> 10), (cships[p].y >> 10) - (cprojs[c].y >> 10)) < a)
                            {
                                if (AsBool(cprojs[c].wep.flags & (int)weapon_flagids.wpfDisperse))
                                {
                                    a = 2 + ((cprojs[c].wep.size - 4) * (t - cprojs[c].str)) / (cprojs[c].end - cprojs[c].str);
                                    if (a < hulls[shiptypes[cships[p].type].hull].size)
                                    {
                                        d = 4 - ((4 * a) / hulls[shiptypes[cships[p].type].hull].size);
                                        if (d < 1) d = 1;
                                    }
                                    else
                                        d = 1;  // now gives damage one point at a time - much cooler!
                                                //if (d > cprojs[c].hits) d = cprojs[c].hits;
                                    combat_damageship(p, 0, d, t, cprojs[c].wep);
                                    cprojs[c].hits -= d;
                                    a = hulls[shiptypes[cships[p].type].hull].size >> 1;
                                    combat_addexplo(cships[p].x + ((rand() % (a * 2) - a) << 8),
                                                                    cships[p].y + ((rand() % (a * 2) - a) << 8),
                                                                    spr_explode1, 5, 32, 0, t, t + 32);
                                    if (cprojs[c].hits <= 0)
                                    { cprojs[c].wep = null; break; }
                                }
                                else
                                {
                                    combat_damageship(p, 0, cprojs[c].wep.damage, t, cprojs[c].wep);
                                    combat_addexplo(cprojs[c].x, cprojs[c].y, spr_explode1, 5, 32, 0, t, t + 32);
                                    d = AsInt((cprojs[c].wep.flags & (int)weapon_flagids.wpfShock1) > 0) + 2 * AsInt((cprojs[c].wep.flags & (int)weapon_flagids.wpfShock2) > 0);
                                    if (d == 1)
                                    {
                                        combat_addexplo(cprojs[c].x, cprojs[c].y, spr_shockwave, 0, 96, 1, t - 8, t + 24, 1);
                                        if (AsBool(settings.opt_lensflares))
                                            combat_addexplo(cprojs[c].x, cprojs[c].y, spr_shockwave, 1, 112, 0, t, t + 10, 4);
                                    }
                                    else if (d == 2)
                                    {
                                        combat_addexplo(cprojs[c].x, cprojs[c].y, spr_shockwave, 0, 96, 1, t - 8, t + 32, 3);
                                        if (AsBool(settings.opt_lensflares))
                                            combat_addexplo(cprojs[c].x, cprojs[c].y, spr_shockwave, 1, 144, 0, t, t + 16, 4);
                                    }
                                    if (AsBool(cprojs[c].wep.flags & (int)weapon_flagids.wpfNova))
                                    {
                                        combat_addexplo(cprojs[c].x, cprojs[c].y, spr_shockwave, 0, 96, 1, t - 8, t + 16, 1);
                                        combat_addexplo(cprojs[c].x, cprojs[c].y, spr_shockwave, 0, 256, 1, t - 8, t + 40, 2);
                                        if (AsBool(settings.opt_lensflares))
                                            combat_addexplo(cprojs[c].x, cprojs[c].y, spr_shockwave, 1, 160, 0, t, t + 24, 4);
                                        if (cships[p].hits <= 0)
                                            cships[p].hits = 1 - hulls[shiptypes[cships[p].type].hull].hits;
                                    }
                                    cprojs[c].wep = null;
                                    break;
                                }
                            }
                        }

                    if (t > cprojs[c].end && cprojs[c].wep != null)
                    {
                        if (AsBool(cprojs[c].wep.flags & (int)weapon_flagids.wpfSplit))    // end split ("flak")
                            if (!AsBool(cprojs[c].wep.flags & (int)weapon_flagids.wpfHoming))
                            {
                                if (cprojs[c].wep.item != -1)
                                {
                                    if (AsBool(shipsystems[itemtypes[cprojs[c].wep.item].index].par[1]))
                                        combat_launchstages(c, shipsystems[itemtypes[cprojs[c].wep.item].index].par[1], t);
                                    else
                                        combat_launchstages(c, 5, t);
                                }
                                else
                                    combat_launchstages(c, 3, t);
                            }

                        cprojs[c].wep = null;
                    }
                }
        }

        private static void combat_SoundFX(int wAV_CLOAKOUT, int x)
        {
            throw new NotImplementedException();
        }

        private static void combat_SoundFX(int wAV_EXPLO1, int x, int v)
        {
            throw new NotImplementedException();
        }

        public static void combat_checkalttargets(Int32 s, Int32 t)
        {
            int b, c;
            int r, rm;

            if (cships[s].type == -1 || cships[s].hits <= 0)
                return;

            // has current target, but check to make sure if need to change
            if (cships[s].target > -1 && cships[cships[s].target].hits > 0)
            {

                // check range of target
                c = cships[s].target;
                r = get_distance((cships[c].x - cships[s].x) >> 10, (cships[c].y - cships[s].y) >> 10);

                // if target is out of range, look for closer enemy ships
                if (r > cships[s].frange)
                {
                    b = -1; rm = cships[s].frange;
                    for (c = 0; c < MAX_COMBAT_SHIPS; c++)
                        if (cships[c].type > -1 && cships[c].hits > 0 &&
                                (cships[c].own & 1) != (cships[s].own & 1) && cships[c].cloaked == 0 && cships[c].active == 2)
                        {
                            r = get_distance((cships[c].x - cships[s].x) >> 10, (cships[c].y - cships[s].y) >> 10);
                            if (r < rm)
                            {
                                b = c; rm = r;
                            }
                        }

                    if (b > -1)
                    {
                        rm = -1;
                        for (c = 0; c < shiptypes[cships[s].type].num_systems; c++)
                            if (shipsystems[shiptypes[cships[s].type].system[c]].type == (int)system_types.sys_weapon &&
                                    shipsystems[shiptypes[cships[s].type].system[c]].par[0] > -1)
                            {
                                r = shipweapons[shipsystems[shiptypes[cships[s].type].system[c]].par[0]].range;
                                if (r < rm || rm == -1) rm = r;
                            }

                        cships[s].target = b;
                        // shiptypes[cships[s].type].speed > shiptypes[cships[b].type].speed+3 && 
                        if (cships[s].own != 2)
                            cships[s].dist = rm + 64;
                        else    // klakar
                            cships[s].dist = 0;
                        cships[s].angle = get_direction(cships[b].x - cships[s].x, cships[b].y - cships[s].y);
                        cships[s].tac = 0;
                        cships[s].wp_time = t + 500 + rand() % 500;
                    }
                }


            }

        }

        public static void combat_findstuff2do(Int32 s, Int32 t)
        {
            int b, c;
            int r, rm;
            int fo;
            int pla = 0;    // is this player ship?

            if (cships[playership].hits > 0 && cships[s].own == 0)
            {
                if (s < player.num_ships)
                    pla = 1;
            }

            if (!AsBool(pla)) // enemy or klakar (or if player dead)
            {
                b = -1; rm = 30000;
                // find closest enemy
                for (c = 0; c < MAX_COMBAT_SHIPS; c++)
                    if (cships[c].type > -1 && cships[c].hits > 0 && (cships[c].own & 1) != (cships[s].own & 1) && cships[c].cloaked == 0 && cships[c].active == 2)
                    {
                        r = get_distance((cships[c].x - cships[s].x) >> 10, (cships[c].y - cships[s].y) >> 10);
                        if (r < rm)
                        {
                            b = c; rm = r;
                        }
                    }
                if (b > -1)
                {
                    cships[s].target = b;
                    //	shiptypes[cships[s].type].speed > shiptypes[cships[b].type].speed+3 && 
                    if (cships[s].own != 2)
                        cships[s].dist = cships[s].frange + 64;
                    else // klakar
                        cships[s].dist = 0;
                    // if in front, swoop to the side
                    c = get_direction(cships[s].x - cships[b].x, cships[s].y - cships[b].y);
                    c = (c + 1024 - cships[b].a) & 1023;
                    if (c > 512) c -= 1024;
                    if (Math.Abs(c) < 128)   // in front
                    {
                        cships[s].angle = (cships[b].a + 512 + 256 * (AsInt(c > 0) - AsInt(c < 0)) + rand() % 256 - 128) & 1023;
                    }
                    else        // attack directly
                    {
                        cships[s].angle = get_direction(cships[b].x - cships[s].x, cships[b].y - cships[s].y);
                    }
                    cships[s].tac = 0;
                    if (t > 0)
                        cships[s].wp_time = t + 500 + rand() % 500;
                    else
                        cships[s].wp_time = t + 1000 + rand() % 500;
                }
                else    // couldn't find enemy. Enter search pattern or formation
                {
                    if (cships[playership].hits > 0 && cships[s].own == 0)  // autofighters
                    {
                        pla = 1;
                    }
                    if (cships[playership].hits > 0 && cships[s].own == 2)  // klakar escorts player
                    {
                        cships[s].tac = 0;
                        cships[s].target = playership;
                        cships[s].dist = 128;
                        cships[s].angle = (cships[cships[s].target].a + 512) & 1023;
                    }
                    else if (cships[s].tac < 2)
                    {
                        cships[s].tac = 2;
                        cships[s].wp_x = cships[s].x + sin1k[cships[s].a];
                        cships[s].wp_y = cships[s].y + cos1k[cships[s].a];
                        cships[s].dist = cships[s].frange + 64;
                        cships[s].patx = cships[s].wp_x;
                        cships[s].paty = cships[s].wp_y;
                    }
                }
            }
            if (AsBool(pla)) // friendly
            {
                if (!AsBool(retreat) || (shiptypes[cships[s].type].flag & 2) > 0)
                {
                    if (s != playership)
                    {
                        fo = s;
                        if (fo >= player.num_ships)
                        {
                            fo = player.num_ships + (MAX_COMBAT_SHIPS - 1 - fo);
                        }
                        cships[s].target = playership;
                        cships[s].dist = 128 * ((fo + 1) / 2);
                        cships[s].angle = (cships[playership].a + 768 - 512 * (fo & 1)) & 1023;
                        cships[s].tac = 0;
                    }
                    else
                    {
                        cships[s].target = -1;
                        cships[s].tac = 2;
                        cships[s].wp_x = cships[s].x + sin1k[cships[s].a] * 2;
                        cships[s].wp_y = cships[s].y + cos1k[cships[s].a] * 2;
                    }
                }
                else
                {
                    b = -1; rm = 30000;
                    for (c = 0; c < MAX_COMBAT_SHIPS; c++)
                        if (cships[c].own == 1 && cships[c].type > -1 && cships[c].hits > 0 && cships[c].cloaked == 0 && cships[c].active > 0)
                        {
                            r = get_distance((cships[c].x - cships[s].x) >> 10, (cships[c].y - cships[s].y) >> 10);
                            if (r < rm)
                            {
                                b = c; rm = r;
                            }
                        }
                    if (b > -1) // escape from closest enemy ship
                    {
                        c = get_direction((cships[b].x - cships[s].x) >> 10, (cships[b].y - cships[s].y) >> 10);
                    }
                    else    // if no enemy found, escape from "camera"
                    {
                        c = get_direction((camera.x - cships[s].x) >> 10, (camera.y - cships[s].y) >> 10);
                    }

                    c = (c + 512) & 1023;

                    cships[s].target = -1;
                    cships[s].tac = 2;
                    cships[s].wp_x = cships[s].x + (sin1k[c] >> 6) * 30000;
                    cships[s].wp_y = cships[s].y + (cos1k[c] >> 6) * 30000;
                }
            }

        }

        public static void combat_checkescapes(Int32 t)
        {
            int c, s;
            int r;
            //int rm, h;

            for (s = 0; s < MAX_COMBAT_SHIPS; s++)
                if (cships[s].own == 1 && cships[s].type > -1 && cships[s].flee > 0)
                {
                    cships[s].flee = 2;
                    for (c = 0; c < MAX_COMBAT_SHIPS; c++)
                        if (cships[c].type > -1 && cships[c].own != 1 && cships[c].hits > 0)
                        {
                            r = get_distance((cships[c].x - cships[s].x) >> 10, (cships[c].y - cships[s].y) >> 10);
                            if (r < (cships[c].frange * 3) / 2)
                                cships[s].flee = 1;
                        }
                }

            if (!AsBool(retreat))
            {
                for (s = 0; s < MAX_COMBAT_SHIPS; s++)
                    if ((cships[s].own & 1) == 0)
                        cships[s].escaped = 0;

                return;
            }

            if (t < rett + 100)
                return;

            for (s = 0; s < MAX_COMBAT_SHIPS; s++)
                if (cships[s].type > -1)
                {
                    if ((cships[s].own & 1) == 0)
                    {
                        cships[s].escaped = 1;
                        if (cships[s].cloaked == 0)
                        {
                            for (c = 0; c < MAX_COMBAT_SHIPS; c++)
                                if (cships[c].type > -1 && cships[c].own == 1 && cships[c].hits > 0 && cships[c].active == 2)
                                {
                                    r = get_distance((cships[c].x - cships[s].x) >> 10, (cships[c].y - cships[s].y) >> 10);
                                    if (r < (cships[c].frange * 3) / 2)
                                        cships[s].escaped = 0;
                                }
                        }
                    }
                }
        }

        public static Int32 combat_findship(Int32 mx, Int32 my)
        {
            Int32 c;
            Int32 r;
            Int32 d;
            Int32 sz;

            r = -1;

            for (d = 0; d < numships; d++)
            {
                c = sortship[d];
                if (cships[c].hits > 0 && cships[c].type > -1 && (cships[c].cloaked == 0 || cships[c].own == 0))
                {
                    sz = (cships[c].ds_s >> 1) + 5;
                    if (mx >= cships[c].ds_x - sz && mx <= cships[c].ds_x + sz &&
                            my >= cships[c].ds_y - sz && my <= cships[c].ds_y + sz)
                        r = c;
                }
            }

            return r;
        }

        public static Int32 shiptonum(t_ship s)
        {
            Int32 c;

            for (c = 0; c < MAX_COMBAT_SHIPS; c++)
                if (cships[c].type > -1)
                {
                    if (s == cships[c])
                        return c;
                }

            return -1;
        }

        public static void combat_updateshipstats(Int32 s, Int32 t)
        {
            int x;
            int ty;

            ty = cships[s].type;

            if (ty == -1)
                return;

            cships[s].shld_type = -1;
            cships[s].dmgc_type = -1;
            cships[s].cpu_type = 0;
            cships[s].ecm_type = -1;
            cships[s].clo_type = 0;
            cships[s].frange = 0;

            cships[s].speed = 0;
            cships[s].sys_thru = -1;

            for (x = 0; x < shiptypes[ty].num_systems; x++)
            {
                switch (shipsystems[shiptypes[ty].system[x]].type)
                {
                    case (int)system_types.sys_weapon:
                        if (cships[s].syshits[x] > 0)
                            cships[s].frange = Math.Max(cships[s].frange, shipweapons[shipsystems[shiptypes[cships[s].type].system[x]].par[0]].range);
                        break;

                    case (int)system_types.sys_thruster:
                        cships[s].sys_thru = x;
                        break;

                    case (int)system_types.sys_shield:
                        if (cships[s].syshits[x] > 0)
                        { cships[s].shld_type = shiptypes[ty].system[x]; cships[s].sys_shld = x; }
                        break;

                    case (int)system_types.sys_damage:
                        if (cships[s].syshits[x] > 0)
                        { cships[s].dmgc_type = shiptypes[ty].system[x]; cships[s].sys_dmgc = x; }
                        break;

                    case (int)system_types.sys_computer:
                        if (cships[s].syshits[x] > 0)
                        { cships[s].cpu_type = shipsystems[shiptypes[ty].system[x]].par[0]; cships[s].sys_cpu = x; }
                        break;

                    case (int)system_types.sys_ecm:
                        if (cships[s].syshits[x] > 0)
                        { cships[s].ecm_type = shiptypes[ty].system[x]; cships[s].sys_ecm = x; }
                        break;

                    case (int)system_types.sys_misc:
                        if (cships[s].syshits[x] > 0)
                        {
                            // cloaker
                            if (shipsystems[shiptypes[ty].system[x]].type == (int)system_types.sys_misc && shipsystems[shiptypes[ty].system[x]].par[0] == 1)
                                if (cships[s].syshits[x] >= 5)
                                { cships[s].clo_type = 1; cships[s].sys_clo = x; }
                        }
                        break;

                    default:
                        break;
                }
            }

            if (cships[s].cloaked > 0 && cships[s].clo_type == 0)   // decloak if cloaker destroyed
            {
                cships[s].cloaked = 0; cships[s].cloaktime = t;
            }

            if (cships[s].shld > 0 && cships[s].shld_type == -1)    // drop shield if damaged
            {
                cships[s].shld = 0;
            }

            if (cships[s].sys_thru > -1)
                cships[s].speed = (shiptypes[cships[s].type].speed * cships[s].syshits[cships[s].sys_thru]) / 10;
            if (cships[s].speed == 0)
            {
                cships[s].speed = 1 + (3 * 32) / hulls[shiptypes[cships[s].type].hull].mass;
            }
            cships[s].turn = 1 + ((shiptypes[cships[s].type].turn - 1) * cships[s].syshits[cships[s].sys_thru]) / 10;

        }

        public static void reset_ship(Int32 s, Int32 st, Int32 t)
        {
            Int32 c;

            cships[s].hits = hulls[shiptypes[st].hull].hits;
            cships[s].type = st;
            cships[s].va = 0;
            cships[s].vx = 0;
            cships[s].vy = 0;
            cships[s].shld = 0;
            cships[s].shld_time = 0;
            cships[s].shld_charge = 0;
            cships[s].damage_time = 0;
            cships[s].dmgc_time = 0;
            cships[s].tac = 0;
            cships[s].escaped = 0;
            cships[s].active = 2;
            cships[s].flee = 0;

            // find systems
            for (c = 0; c < shiptypes[st].num_systems; c++)
            {
                cships[s].syshits[c] = 10;
            }

            combat_updateshipstats(s, t);

            if (cships[s].shld_type > -1)
                cships[s].shld = shipsystems[cships[s].shld_type].par[0];
        }

        public static void combat_summon_klakar(Int32 t)
        {
            Int32 b, c;
            Int32 s, st;

            b = -1;
            for (c = 0; c < STARMAP_MAX_FLEETS; c++)
            {
                if (sm_fleets[c].race == (int)race_ids.race_klakar)
                    b = c;
            }
            if (b == -1)
            {
                Play_SoundFX((int)sfxsamples.WAV_DESELECT, get_ik_timer(1));
                return;
            }
            Play_SoundFX((int)sfxsamples.WAV_DOT, 0);
            sm_fleets[b].system = player.system;

            b = -1;
            for (c = 0; c < MAX_COMBAT_SHIPS; c++)
            {
                if (c >= player.num_ships && cships[c].type == -1)
                    b = c;
            }

            if (b == -1)
                return;

            s = b;
            st = -1;
            for (c = 0; c < num_shiptypes; c++)
                if (shiptypes[c].race == (int)race_ids.race_klakar)
                {
                    st = c;
                }

            // set up klakar ship

            // find location
            b = rand() % 1024;

            // basic resets
            reset_ship(s, st, t);

            cships[s].x = camera.x - ((sin1k[b] * 1000) >> 6);
            cships[s].y = camera.y - ((cos1k[b] * 1000) >> 6);
            cships[s].a = b;
            cships[s].own = 2;

            combat_findstuff2do(s, 0);

            //	Play_SoundFX((int)sfxsamples.WAV_HYPERDRIVE, get_ik_timer(1));
            klaktime = t;
        }

        public static void combat_launch_fighter(Int32 s, Int32 t)
        {
            Int32 c, b;
            Int32 st;
            Int32 x, y;

            st = racefleets[races[shiptypes[cships[s].type].race].fleet].stype[0];

            /*
            b = 0;
            for (c = 0; c < MAX_COMBAT_SHIPS; c++)
                if (cships[c].type == st && cships[c].hits > 0)
                    b++;

            if (b >= 3)	// don't launch more than 3 at once
                return;
            Play_SoundFX((int)sfxsamples.WAV_FIGHTERLAUNCH);
            */

            b = -1;
            for (c = 0; c < MAX_COMBAT_SHIPS; c++)
                if (c >= player.num_ships && cships[c].type == -1)
                {
                    b = c;
                }

            if (b == -1)
                return;

            x = cships[s].x; y = cships[s].y;

            for (c = 0; c < hulls[shiptypes[cships[s].type].hull].numh; c++)
                if (hulls[shiptypes[cships[s].type].hull].hardpts[c].type == (int)hull_hardpttypes.hdpFighter)
                    combat_gethardpoint(cships[s], c, x, y);

            reset_ship(b, st, t);

            cships[b].a = cships[s].a;
            cships[b].x = x;
            cships[b].y = y;
            cships[b].own = cships[s].own;

            combat_findstuff2do(b, t);
        }

        public static Int32 combat_use_gong(Int32 t)
        {
            Int32 c, b;
            Int32 mh;

            b = -1; mh = 0;
            for (c = 0; c < MAX_COMBAT_SHIPS; c++)
            {
                if ((cships[c].type > -1) && (cships[c].hits + cships[c].shld > mh) && AsBool(cships[c].own & 1))
                {
                    mh = cships[c].hits + cships[c].shld;
                    b = c;
                }
            }

            if (b == -1)
                return 1;

            Play_SoundFX((int)sfxsamples.WAV_GONG, t);


            // mark ship for extreme death
            cships[b].bong_start = t;
            cships[b].bong_end = t + 200;

            return 0;
        }

        public static void combat_help_screen()
        {
            Int32 end;
            Int32 c, mc;
            Int32 t = 0;
            Int32 x, y;
            t_ik_image bg;

            bg = ik_load_pcx("graphics/helpc.pcx".ToCharArray(), null);

            prep_screen();
            ik_copybox(bg, screen, 0, 0, 640, 480, 0, 0);

            if (COMBAT_BUILD_HELP)
            {
                x = 150; y = 20;
                interface_thinborder(screen, x, y + 4, x + 120, y + 228, COMBAT_INTERFACE_COLOR, 2 + COMBAT_INTERFACE_COLOR * 16);

                ik_print(screen, font_6x8, x + 4, y += 8, COMBAT_INTERFACE_COLOR, "ALLIED SHIPS".ToCharArray());
                ik_copybox(screen, screen, 16, 24, 48, 40, x + 4, (y += 8) - 1);
                ik_print(screen, font_4x8, x + 4, (y += 16), 0, "Click to select ship.".ToCharArray());

                y += 6;
                ik_print(screen, font_6x8, x + 4, y += 8, COMBAT_INTERFACE_COLOR, "HULL DAMAGE".ToCharArray());
                y += 8 * interface_textbox(screen, font_4x8, x + 4, y += 8, 112, 64, 0, "The red bar on the left displays the hull integrity. When it reaches the bottom, the ship will be destroyed.".ToCharArray()) - 8;

                y += 6;
                ik_print(screen, font_6x8, x + 4, y += 8, COMBAT_INTERFACE_COLOR, "SHIELD STATUS".ToCharArray());
                y += 8 * interface_textbox(screen, font_4x8, x + 4, y += 8, 112, 64, 0, "If the currently selected ship is equipped with a shield, the blue bar on the right displays its strength.".ToCharArray()) - 8;

                y += 6;
                ik_print(screen, font_6x8, x + 4, y += 8, COMBAT_INTERFACE_COLOR, "SHIP SYSTEMS".ToCharArray());
                ik_dsprite(screen, x - 2, (y += 8) - 5, spr_IFsystem.spr[1], 2 + (1 << 8));
                ik_print(screen, font_4x8, x + 12, y, 1, "Weapons".ToCharArray());
                ik_dsprite(screen, x - 2, (y += 8) - 5, spr_IFsystem.spr[5], 2 + (3 << 8));
                ik_print(screen, font_4x8, x + 12, y, 3, "Star Drive".ToCharArray());
                ik_dsprite(screen, x - 2, (y += 8) - 5, spr_IFsystem.spr[9], 2 + (2 << 8));
                ik_print(screen, font_4x8, x + 12, y, 2, "Combat Thrusters".ToCharArray());

                y += 6;
                ik_print(screen, font_6x8, x + 4, y += 8, COMBAT_INTERFACE_COLOR, "SYSTEM DAMAGE".ToCharArray());
                y += 8 * interface_textbox(screen, font_4x8, x + 4, y += 8, 112, 64, 0, "Damaged systems are shown in different colors. When a system is lost it ceases to function and must be repaired after the battle.".ToCharArray()) - 8;

                x = 80; y = 308;
                interface_thinborder(screen, x, y + 4, x + 120, y + 60, COMBAT_INTERFACE_COLOR, 2 + COMBAT_INTERFACE_COLOR * 16);
                ik_print(screen, font_6x8, x + 4, y += 8, COMBAT_INTERFACE_COLOR, "MISC COMBAT ACTIONS".ToCharArray());
                y += 8 * interface_textbox(screen, font_4x8, x + 4, y += 8, 112, 64, 0, "By default, the only button available is Retreat. As you aqcuire special items or devices you may be able to use them during combat.".ToCharArray()) - 8;

                x = 288; y = 374;
                interface_thinborder(screen, x, y + 4, x + 120, y + 60, COMBAT_INTERFACE_COLOR, 2 + COMBAT_INTERFACE_COLOR * 16);
                ik_print(screen, font_6x8, x + 4, y += 8, COMBAT_INTERFACE_COLOR, "CURRENT TARGET".ToCharArray());
                y += 8 * interface_textbox(screen, font_4x8, x + 4, y += 8, 112, 64, 0, "This window shows the ship you've chosen to attack (or escort in case of friendly ships) along with its hull and shield status.".ToCharArray()) - 8;

                x = 284; y = 52;
                interface_thinborder(screen, x, y + 4, x + 136, y + 84, COMBAT_INTERFACE_COLOR, 2 + COMBAT_INTERFACE_COLOR * 16);
                ik_print(screen, font_6x8, x + 4, y += 8, COMBAT_INTERFACE_COLOR, "SELECTING SHIPS".ToCharArray());
                y += 8 * interface_textbox(screen, font_4x8, x + 4, y += 8, 128, 64, 0, "In addition to using the ship selection icons (see ALLIED SHIPS, top left) you can select any friendly ship by clicking it with the left mouse button. The currently selected ship is marked by a translucent green reticle.".ToCharArray()) - 8;

                x = 428; y = 52;
                interface_thinborder(screen, x, y + 4, x + 112, y + 224, COMBAT_INTERFACE_COLOR, 2 + COMBAT_INTERFACE_COLOR * 16);
                ik_print(screen, font_6x8, x + 4, y += 8, COMBAT_INTERFACE_COLOR, "GIVING ORDERS".ToCharArray());
                y += 6;
                ik_print(screen, font_6x8, x + 4, y += 8, 1, "ATTACK ENEMY".ToCharArray());
                y += 8 * interface_textbox(screen, font_4x8, x + 4, y += 8, 104, 64, 0, "Left-click on any enemy spacecraft to order the selected ship to attack it. Hold the button and drag to add a waypoint, for example to attack from the side. The path of attack is shown in red.".ToCharArray()) - 8;
                y += 6;
                ik_print(screen, font_6x8, x + 4, y += 8, 3, "STAY IN FORMATION".ToCharArray());
                y += 8 * interface_textbox(screen, font_4x8, x + 4, y += 8, 104, 64, 0, "Right-click anywhere near your ship to order the selected ally to follow you, staying on that side of your ship. The course to its escort position is shown in yellow.".ToCharArray()) - 8;
                y += 6;
                ik_print(screen, font_6x8, x + 4, y += 8, 4, "MOVE TO WAYPOINT".ToCharArray());
                y += 8 * interface_textbox(screen, font_4x8, x + 4, y += 8, 104, 64, 0, "Left-click at empty space to move the selected ship to that location. The course to the waypoint is shown in green.".ToCharArray()) - 8;

                x = 428; y = 284;
                interface_thinborder(screen, x, y + 4, x + 112, y + 108, COMBAT_INTERFACE_COLOR, 2 + COMBAT_INTERFACE_COLOR * 16);
                ik_print(screen, font_6x8, x + 4, y += 8, COMBAT_INTERFACE_COLOR, "PAUSE / SPEED UP".ToCharArray());
                y += 8 * interface_textbox(screen, font_4x8, x + 4, y += 8, 104, 64, 0, "Click these symbols to change the speed of the game. You can give orders to your ships even when paused. Click on the single arrow head to return to normal speed.".ToCharArray()) - 8;
                y += 6;
                y += 8 * interface_textbox(screen, font_4x8, x + 4, y += 8, 104, 64, 0, "You can also press the space bar to pause and unpause.".ToCharArray()) - 8;
            }

            ik_blit();

            update_palette();

            end = 0;
            x = key_pressed(key_f[0]); y = 0;
            while (!AsBool(end) && !must_quit)
            {
                ik_eventhandler();
                c = ik_inkey();
                mc = ik_mclick();
                x = key_pressed(key_f[0]);
                if (!AsBool(x))
                {
                    if (!AsBool(y))
                        y = 1;
                    else if (y == 2)
                        end = 1;
                }
                else if (AsBool(y))
                    y = 2;

                if (mc == 1 || c > 0)
                    end = 1;

                c = t; t = get_ik_timer(2);
                if (t != c)
                { prep_screen(); ik_blit(); }
            }

            if (must_quit)
                must_quit = false;
        }

        public static void combat_SoundFX(int id, int srcx, int volume, int rate)
        {
            int pan;

            pan = (((srcx - camera.x) >> 8) * camera.z) >> 11;

            if (pan < -10000)
                pan = -10000;
            if (pan > 10000)
                pan = 10000;

            Play_SoundFX(id, 0, volume, rate, pan);
        }
    }
}