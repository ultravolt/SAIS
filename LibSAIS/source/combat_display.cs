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
//#include "snd.h"
//#include "interface.h"
//#include "starmap.h"
//#include "textstr.h"

//#include "combat.h"

// ----------------
// GLOBAL FUNCTIONS
// ----------------
using System;
using System.Collections.Generic;

namespace DigitalEeel
{
	public static partial class SAIS
	{
        public static void combat_autocamera(int t)
		{
			int minx, maxx, miny, maxy;
			int x, y, z;
			int s;
			int c;

			minx = (30000) << 10; maxx = (-30000) << 10;
			miny = (30000) << 10; maxy = (-30000) << 10;

			for (c = 0; c < MAX_COMBAT_SHIPS; c++)
				if (cships[c].type > -1 && cships[c].flee < 2)
				{
					s = hulls[shiptypes[cships[c].type].hull].size << 9;
					minx = Math.Min(minx, cships[c].x - s);
					maxx = Math.Max(maxx, cships[c].x + s);
					miny = Math.Min(miny, cships[c].y - s);
					maxy = Math.Max(maxy, cships[c].y + s);
				}
			for (c = 0; c < MAX_COMBAT_EXPLOS; c++)
				if (cexplo[c].spr!=null)
				{
					if (AsBool(cexplo[c].cam))
					{
						s = cexplo[c].size << 9;
						minx = Math.Min(minx, cexplo[c].x - s);
						maxx = Math.Max(maxx, cexplo[c].x + s);
						miny = Math.Min(miny, cexplo[c].y - s);
						maxy = Math.Max(maxy, cexplo[c].y + s);
					}
				}

			if (maxx > minx && maxy > miny)
			{
				x = (minx + maxx) >> 1;
				y = (miny + maxy) >> 1;
				c = Math.Max(maxx - minx, maxy - miny) >> 10;
				z = (256 << 12) / c;

				camera.x = (camera.x * 15 + x) >> 4;
				camera.y = (camera.y * 15 + y) >> 4;
				camera.z = (camera.z * 15 + z) >> 4;
			}
		}

        public static void combat_displayships()
		{
			int s, t;
			int bx, by, h;
			int y, sy;
			int ty;
			int hp;
			int l;
			int klak;
			int f;
			string top=null;
			t_hull hull;

			t = get_ik_timer(1);

			// check for klakar button

			klak = klakavail;
			for (s = 0; s < MAX_COMBAT_SHIPS; s++)
			{
				if ((int)shiptypes[cships[s].type].race == 2)
					klak = 2;
			}
			if (cships[playership].hits <= 0)
				klak = 0;

			// player ship selection

			bx = 0; by = 0; h = 288;

			s = camera.ship_sel;
			if (s == -1)
			{
				select_ship(0, t); s = 0;
			}

			ty = cships[s].type;
			if (ty == -1)
			{
				select_ship(0, t); s = 0; ty = (int)player.ships[0];
			}

			if (s == 0 && cships[s].type == -1)
			{
				camera.ship_sel = -1; camera.ship_trg = -1;
			}

			if (ty > -1)
				sprintf(top, shiptypes[ty].name);
			else
				sprintf(top, player.shipname);
			interface_drawborder(screen,
				bx, by, bx + 160, by + h,
				1, COMBAT_INTERFACE_COLOR, top); // shipname
			if (ty > -1)
			{
				hull = hulls[shiptypes[ty].hull];
				ik_dsprite(screen, bx + 16, by + 40, hulls[shiptypes[ty].hull].silu, 2 + (COMBAT_INTERFACE_COLOR << 8));
				hp = Math.Max(0, cships[s].hits) * 100 / hulls[shiptypes[ty].hull].hits;
				ik_drawmeter(screen, bx + 12, by + 40, bx + 15, by + 166, 0, hp, COMBAT_INTERFACE_COLOR, 28);
				if (cships[s].shld_type > -1)
				{
					hp = (cships[s].shld * 100) / shipsystems[cships[s].shld_type].par[0];
					ik_drawmeter(screen, bx + 144, by + 40, bx + 147, by + 166, 0, hp, COMBAT_INTERFACE_COLOR, 92);
				}
				for (hp = 0; hp < hull.numh; hp++)
				{
					l = 0;
					if (hull.hardpts[hp].type == (byte)hull_hardpttypes.hdpWeapon && shipsystems[shiptypes[ty].system[hp]].item > -1)
						l = AsInt((cships[s].syshits[hp] > 0));
					if (hull.hardpts[hp].type == (byte)hull_hardpttypes.hdpEngine && shiptypes[ty].engine > -1)
						l = 3 * AsInt((cships[s].syshits[shiptypes[ty].sys_eng] > 0));
					if (hull.hardpts[hp].type == (byte)hull_hardpttypes.hdpThruster && shiptypes[ty].thrust > -1)
						l = 2 * AsInt(cships[s].syshits[shiptypes[ty].sys_thru] > 0);

					ik_dsprite(screen, bx + 8 + hull.hardpts[hp].x * 2, by + 32 + hull.hardpts[hp].y * 2,
						spr_IFsystem.spr[hull.hardpts[hp].type * 4 + 1], 2 + (l << 8));
				}

				// show system damage
				ik_print(screen, font_4x8, bx + 16, by + 176, COMBAT_INTERFACE_COLOR, textstring[(int)textstrings.STR_COMBAT_STATUS]);
				y = 0;
				for (sy = 0; sy < shiptypes[ty].num_systems; sy++)
				{
					if (shipsystems[shiptypes[ty].system[sy]].item > -1)
					{
						if (cships[s].syshits[sy] == 10)
							l = 4;
						else if (cships[s].syshits[sy] >= 5)
							l = 3;
						else if (cships[s].syshits[sy] > 0)
							l = 1;
						else
							l = 0;
						ik_print(screen, font_4x8, bx + 16, by + y * 8 + 184, l, shipsystems[shiptypes[ty].system[sy]].name);
						y++;
					}
				}
				ik_print(screen, font_4x8, bx + 16, by + 264, COMBAT_INTERFACE_COLOR, textstring[(int)textstrings.STR_COMBAT_DMGKEY]);
				ik_print(screen, font_4x8, bx + 16, by + 272, 0, textstring[(int)textstrings.STR_COMBAT_DMG1]);
				ik_print(screen, font_4x8, bx + 48, by + 272, 1, textstring[(int)textstrings.STR_COMBAT_DMG2]);
				ik_print(screen, font_4x8, bx + 80, by + 272, 3, textstring[(int)textstrings.STR_COMBAT_DMG3]);
				ik_print(screen, font_4x8, bx + 112, by + 272, 4, textstring[(int)textstrings.STR_COMBAT_DMG4]);

			}

			// small ships
			for (y = 0; y < player.num_ships; y++)
			{
				l = 0;
				if (cships[y].hits <= 0)
					l = 1 + (24 << 8);
				ik_drsprite(screen, bx + y * 16 + 24, by + 32, 0, 16, hulls[shiptypes[(int)player.ships[y]].hull].sprite, l);
				if (y == camera.ship_sel)
				{
					l = AsInt((get_ik_timer(1) & 31) > 23);
					ik_drsprite(screen, bx + y * 16 + 24, by + 32, 0, 16 + l * 2, spr_IFtarget.spr[8], 5 + ((8 + l * 7) << 8));
				}
			}

			// cloak button

			if (cships[playership].clo_type > 0 && cships[playership].hits > 0)
			{
				if (!AsBool(cships[playership].cloaked))
					interface_drawbutton(screen, bx, 288, 80, COMBAT_INTERFACE_COLOR, textstring[(int)textstrings.STR_COMBAT_CLOAK]);
				else
					interface_drawbutton(screen, bx, 288, 80, COMBAT_INTERFACE_COLOR, textstring[(int)textstrings.STR_COMBAT_UNCLOAK]);
			}
			else
				interface_drawbutton(screen, bx, 288, 80, 0, "");

			// gong button

			if (gongavail == 2)	// available
				interface_drawbutton(screen, bx + 80, 288, 80, COMBAT_INTERFACE_COLOR, textstring[(int)textstrings.STR_COMBAT_GONG]);
			else if (gongavail == 1)	// used
				interface_drawbutton(screen, bx + 80, 288, 80, 1, textstring[(int)textstrings.STR_COMBAT_GONG]);
			else	// none
				interface_drawbutton(screen, bx + 80, 288, 80, 0, "");

			// retreat button

			f = 0;
			for (s = 0; s < MAX_COMBAT_SHIPS; s++)
				if (cships[s].hits > 0 && cships[s].type > -1)
				{
					if ((cships[s].own & 1) == 0)
						f |= 1;
					else
						f |= 2;
				}
			if (cships[playership].hits > 0 && f > 1)
			{
				if (!AsBool(retreat))
					interface_drawbutton(screen, bx, 304, 80, COMBAT_INTERFACE_COLOR, textstring[(int)textstrings.STR_COMBAT_RETREAT]);
				else
					interface_drawbutton(screen, bx, 304, 80, 1, textstring[(int)textstrings.STR_COMBAT_RETREAT]);
			}
			else
				interface_drawbutton(screen, bx, 304, 80, 0, "");

			// klakar button

			switch (klak)
			{
			case 0:	// no klakar available
				interface_drawbutton(screen, bx + 80, 304, 80, 0, "");
				break;
			case 1: // klakar available
				interface_drawbutton(screen, bx + 80, 304, 80, COMBAT_INTERFACE_COLOR, textstring[(int)textstrings.STR_COMBAT_KLAKAR]);
				break;
			case 2: // already summoned
				interface_drawbutton(screen, bx + 80, 304, 80, 1, textstring[(int)textstrings.STR_COMBAT_KLAKAR]);
				break;
			default:
                break;
			}


			// target ship

			bx = 0; by = 320; h = 160;

			s = camera.ship_trg;
			if (s == -1)
				ty = -1;
			else
				ty = cships[s].type;

			if (ty > -1)
				sprintf(top, shiptypes[ty].name);
			else
				sprintf(top, textstring[(int)textstrings.STR_COMBAT_NOTARGET]);
			interface_drawborder(screen,
				bx, by, bx + 160, by + h,
				1, COMBAT_INTERFACE_COLOR, top); // shipname
			if (ty > -1)
			{
				hull = hulls[shiptypes[ty].hull];
				ik_dsprite(screen, bx + 16, by + 24, hulls[shiptypes[ty].hull].silu, 2 + (COMBAT_INTERFACE_COLOR << 8));
				if (cships[s].active > 1 || shiptypes[ty].race != race_ids.race_unknown)
				{
					hp = Math.Max(0, cships[s].hits) * 100 / hulls[shiptypes[ty].hull].hits;
					if (cships[s].active == 1 && cships[s].hits == 1)
						hp = 100;

					ik_drawmeter(screen, bx + 12, by + 24, bx + 15, by + 152, 0, hp, COMBAT_INTERFACE_COLOR, 28);
					if (cships[s].shld_type > -1)
					{
						hp = (cships[s].shld * 100) / shipsystems[cships[s].shld_type].par[0];
						ik_drawmeter(screen, bx + 144, by + 24, bx + 147, by + 152, 0, hp, COMBAT_INTERFACE_COLOR, 92);
					}
					for (hp = 0; hp < hull.numh; hp++)
					{
						l = 0;
						if (hull.hardpts[hp].type == (byte)hull_hardpttypes.hdpWeapon)
							l = AsInt(cships[s].syshits[hp] > 0);
						if (hull.hardpts[hp].type == (byte)hull_hardpttypes.hdpEngine)
							l = 3 * AsInt(cships[s].syshits[shiptypes[ty].sys_eng] > 0);
						if (hull.hardpts[hp].type == (byte)hull_hardpttypes.hdpThruster)
							l = 2 * AsInt (cships[s].syshits[shiptypes[ty].sys_thru] > 0);
						ik_dsprite(screen, bx + 8 + hull.hardpts[hp].x * 2, by + 16 + hull.hardpts[hp].y * 2,
							spr_IFsystem.spr[hull.hardpts[hp].type * 4 + 1], 2 + (l << 8));
					}
				}
			}


		}

        public static void combat_display(int t)
		{
			int c;
			int s;
			int p;
			int tx, ty;
			int sx, sy;
			int cx, cy;
			int x, y;
			int a;
			int l;
			int sz;
			int fr;
			int bab;
			t_hull hull;
            IntPtr draw;
            IntPtr src;

			combat_autocamera(t);

			bab = 0;
			for (c = 0; c < player.num_items; c++)
				if (AsBool(itemtypes[player.items[c]].flag & (int)item_deviceflags.device_torc))
					bab = 1;

			ik_setclip(168, 8, 632, 476);
			cy = 244;
			cx = 160 + 240;

			// nebula background

			if (nebula!=0)
			{

				s = (4096 << 12) / (camera.z + 1) + (1 << 13);
				ty = sy = ((-camera.y << 1) - 232 * s) & 0xffffff;
				sx = ((camera.x << 1) - 232 * s) & 0xffffff;
				for (y = 8; y < 476; y++)
				{
					//src = new IntPtr(combatbg2.data + (combatbg2.pitch*(ty >> 16)));
					//			src=combatbg2.data+(combatbg2.pitch*(ty>>16));
					ty = (ty + s) & 0xffffff;
					tx = sx;
					draw = ik_image_pointer(screen, 168, y);
					for (x = 168; x < 632; x++)
					{
						//c = src.[(tx >> 16)];
						tx = (tx + s) & 0xffffff;
                        draw = draw + 1;
						//draw++ = c;
					}
				}
				//ik_copybox(combatbg2, screen, 0, 0, 256, 256, cx-232, cy-232);
			}
			else
			{
				ik_copybox(combatbg1, screen, 8, 8, 472, 472, cx - 232, cy - 232);
			}

			// grid
		//x = camera.x + ((((ik_mouse_x - 400)<<12)/camera.z)<<10);
			c = 3 + 63 * simulated;

			sy = (camera.y >> 10) + ((-232 << 12) / camera.z);
			sy = (sy / 250) * 250 - 250;
			ty = (camera.y >> 10) + ((232 << 12) / camera.z) + 250;
			for (; sy < ty; sy += 250)
			{
				y = cy - (((sy - (camera.y >> 10)) * camera.z) >> 12);
				if (y > cy - 232 && y < cy + 232)
				{
					draw = ik_image_pointer(screen, cx - 232, y);
					for (x = 0; x < 464; x++)
					{
						draw = gfx_addbuffer[(draw).Toint() + (c << 8)];
						//draw++;
					}
				}
			}
			sx = (camera.x >> 10) + ((-232 << 12) / camera.z);
			sx = (sx / 250) * 250 - 250;
			tx = (camera.x >> 10) + ((232 << 12) / camera.z) + 250;
			for (; sx < tx; sx += 250)
			{
				x = cx + (((sx - (camera.x >> 10)) * camera.z) >> 12);
				if (x > cx - 232 && x < cx + 232)
				{
					draw = ik_image_pointer(screen, x, cy - 232);
					for (y = 0; y < 464; y++)
					{
						draw = gfx_addbuffer[(draw.Toint()) + (c << 8)];
						draw += screen.pitch;
					}
				}
			}

			// display list insertion
			numships = 0;
			for (c = 0; c < MAX_COMBAT_SHIPS; c++)
				if ((cships[c].cloaked == 0 || (t - cships[c].cloaktime < 50 && cships[c].cloaktime>0)) || (cships[c].own == 0 || AsBool(bab)))
					if (cships[c].type > -1)
					{
						sortship[numships] = c;
						numships++;
					}
			// sort display list
			for (c = 0; c < numships; c++)
			{
				s = c;
				while (s > 0 && hulls[shiptypes[cships[sortship[s]].type].hull].size > hulls[shiptypes[cships[sortship[s - 1]].type].hull].size)
				{
					p = sortship[s - 1];
					sortship[s - 1] = sortship[s];
					sortship[s] = p;
					s--;
				}
			}

			for (c = 0; c < numships; c++)
			{
				s = sortship[c];
				hull = hulls[shiptypes[cships[s].type].hull];
				cships[s].ds_x = cx + ((((cships[s].x - camera.x) >> 8) * camera.z) >> 14);
				cships[s].ds_y = cy - ((((cships[s].y - camera.y) >> 8) * camera.z) >> 14);
				cships[s].ds_s = (hulls[shiptypes[cships[s].type].hull].size * camera.z) >> 12;
				if (cships[s].own != 2 || t > klaktime + 75)
				{
					if (AsBool(simulated))
					{
						if (s == playership)
							l = 90;
						else if (cships[s].own == 0)
							l = 74;
						else
							l = 26;
					}
					else
						l = 15;

					if (AsBool(cships[s].cloaked))
					{
						if (t - cships[s].cloaktime < 50 && cships[s].cloaktime > 0)
							l = 15 - (t - cships[s].cloaktime) * 11 / 50;
						else
							l = 4 + 4 * (cships[s].own & 1);
					}
					else if (AsBool(cships[s].clo_type) && (cships[s].cloaktime > 0))	// has uncloaked
					{
						if (t - cships[s].cloaktime < 50 && cships[s].cloaktime > 0)
							l = 4 + (t - cships[s].cloaktime) * 11 / 50;
						else
							l = 15;
					}

					if (cships[s].teltime > 0 && t < cships[s].teltime + 32)	// zorg fold effect
					{
						p = (t - cships[s].teltime);

						x = cx + ((((cships[s].x + cships[s].tel_x - camera.x) >> 8) * camera.z) >> 14);
						y = cy - ((((cships[s].y + cships[s].tel_y - camera.y) >> 8) * camera.z) >> 14);

						if (p < 16)
							l = p;
						else
							l = 31 - p;

						ik_dspriteline(screen,
							cships[s].ds_x,
							cships[s].ds_y,
							x,
							y,
							(20 * camera.z) >> 12,
							(-t * 13) & 31, 18,
							spr_weapons.spr[1 + (rand() & 1)], 5 + (l << 8));

						ik_drsprite(screen,
							(cships[s].ds_x*(32 - p) + x*p) >> 5,
							(cships[s].ds_y*(32 - p) + y*p) >> 5,
							rand() & 1023,
							(64 * camera.z) >> 12,
							spr_shockwave.spr[4], 4);
						if ((new Random().Next() & 1)!=0)
							ik_drsprite(screen,
							(cships[s].ds_x*(32 - p) + x*p) >> 5,
								(cships[s].ds_y*(32 - p) + y*p) >> 5,
								rand() & 1023,
								(64 * camera.z) >> 12,
								spr_shockwave.spr[4], 4);

						if (p < 16)
							l = Math.Max(0, 15 - p);
						else
						{
							l = p - 16;
							cships[s].ds_x = x;
							cships[s].ds_y = y;
						}
					}


					if (l == 15)
						ik_drsprite(screen,
							cships[s].ds_x,
							cships[s].ds_y,
							cships[s].a,
							cships[s].ds_s,
							hulls[shiptypes[cships[s].type].hull].sprite, 0);
					else
						ik_drsprite(screen,
							cships[s].ds_x,
							cships[s].ds_y,
							cships[s].a,
							cships[s].ds_s,
							hulls[shiptypes[cships[s].type].hull].sprite, 5 + (l << 8));
				}
				if (cships[s].own == 2)
				{
					if (t - klaktime < 75)
					{
						l = (t - klaktime) * 15 / 75;
						ik_drsprite(screen,
							cships[s].ds_x,
							cships[s].ds_y,
							cships[s].a,
							((t - klaktime)*camera.z) >> 12,
							spr_shockwave.spr[4], 5 + (l << 8));
					}
					else if (t - klaktime < 100)
					{
						l = ((klaktime + 100 - t) * 15) / 25;
						ik_drsprite(screen,
							cships[s].ds_x,
							cships[s].ds_y,
							cships[s].a,
							(256 * camera.z) >> 12,
							spr_shockwave.spr[4], 5 + (l << 8));
					}
				}
				if (cships[s].shld_type > -1 && cships[s].shld > 0 && t < cships[s].shld_time + 32)
				{
					p = shipsystems[cships[s].shld_type].par[0];
					l = 15; //(cships[s].shld * 15) / p;
					l = (l * (cships[s].shld_time + 32 - t)) >> 5;
					p = (p - 1) / 10; if (p > 4) p = 4;
					ik_drsprite(screen,
						cships[s].ds_x,
						cships[s].ds_y,
						rand() % 1024,
						cships[s].ds_s,
						spr_shield.spr[p], 5 + (l << 8));
				}

				// draw markers so you can see them from far
				// triangles
				if (cships[s].own == 0)
					p = 72;
				else if (cships[s].own == 1)
					p = 24;
				else
					p = 56;
				if (cships[s].ds_s > 10)
					p -= (cships[s].ds_s - 10);
				if (cships[s].ds_s < 18 && cships[s].escaped == 0)
				{
					sz = cships[s].ds_s / 2 + 2;
					sx = cships[s].ds_x;
					sy = cships[s].ds_y;
					tx = sin1k[cships[s].a];
					ty = cos1k[cships[s].a];
					ik_drawline(screen,
						sx - ((tx*sz * 2 + ty*sz * 3) >> 17), sy + ((ty*sz * 2 - tx*sz * 3) >> 17),
						sx + ((tx*sz * 4) >> 17), sy - ((ty*sz * 4) >> 17),
						p, 0, 255, 1);
					ik_drawline(screen,
						sx - ((tx*sz * 2 - ty*sz * 3) >> 17), sy + ((ty*sz * 2 + tx*sz * 3) >> 17),
						sx + ((tx*sz * 4) >> 17), sy - ((ty*sz * 4) >> 17),
						p, 0, 255, 1);
					ik_drawline(screen,
						sx - ((tx*sz * 2 + ty*sz * 3) >> 17), sy + ((ty*sz * 2 - tx*sz * 3) >> 17),
						sx - ((tx*sz * 2 - ty*sz * 3) >> 17), sy + ((ty*sz * 2 + tx*sz * 3) >> 17),
						p, 0, 255, 1);
				}
				//		ik_print(screen, font_6x8, cships[s].ds_x, cships[s].ds_y, 0, "%s", racename[shiptypes[cships[s].type].race]);
			}

			for (c = 0; c < MAX_COMBAT_SHIPS; c++)
				if (cships[c].type > -1 && cships[c].own == 1 && cships[c].flee == 2)
				{
					if (cships[c].ds_x + cships[c].ds_s < cx - 240 ||
						cships[c].ds_x - cships[c].ds_s > cx + 240 ||
						cships[c].ds_y + cships[c].ds_s < cy - 240 ||
						cships[c].ds_y - cships[c].ds_s > cy + 240 ||
						(AsBool(cships[c].cloaked) && t > cships[c].cloaktime + 50))
					{
						combat_killship(c, t, 1);
					}
				}

			for (c = 0; c < MAX_COMBAT_BEAMS; c++)
				if (cbeams[c].wep!=null)
				{
					if (cbeams[c].stg > -1)	// staged from projectile
					{
						if (cprojs[cbeams[c].stg].wep==null)
						{
							cbeams[c].wep = null;
							continue;
						}
						sx = cprojs[cbeams[c].stg].x;
						sy = cprojs[cbeams[c].stg].y;
					}
					else
					{
						combat_gethardpoint(cbeams[c].src, cbeams[c].stp, ref sx, ref sy);
					}
					if (cbeams[c].dst!=null)
					{
						combat_gethardpoint(cbeams[c].dst, cbeams[c].dsp, ref tx, ref ty);
						if (AsBool(cbeams[c].wep.flags & (int)weapon_flagids.wpfWiggle))
						{
							l = (t - cbeams[c].str)*(cbeams[c].stp * 5 + 30) + (cbeams[c].stp * 160);
							tx = tx + ((sin1k[l & 1023] * hulls[shiptypes[cbeams[c].dst.type].hull].size) >> 8);
							ty = ty + ((cos1k[l & 1023] * hulls[shiptypes[cbeams[c].dst.type].hull].size) >> 8);
						}
					}
					else
					{
						tx = sx + ((sin1k[cbeams[c].ang] * cbeams[c].len) >> 6);
						ty = sy + ((cos1k[cbeams[c].ang] * cbeams[c].len) >> 6);
					}

					if (t + t < cbeams[c].str + cbeams[c].end)
					{
						//l = ((t - cbeams[c].str)*24) / (cbeams[c].end - cbeams[c].str) + 3;
						l = 15;
					}
					else
					{
						l = ((cbeams[c].end - t) * 24) / (cbeams[c].end - cbeams[c].str) + 3;
					}
					if (l < 2) l = 2;
					if (l > 15) l = 15;
					ik_dspriteline(screen,
						cx + ((((sx - camera.x) >> 8) * camera.z) >> 14),
						cy - ((((sy - camera.y) >> 8) * camera.z) >> 14),
						cx + ((((tx - camera.x) >> 8) * camera.z) >> 14),
						cy - ((((ty - camera.y) >> 8) * camera.z) >> 14),
						(cbeams[c].wep.size * camera.z) >> 12,
						(-t * 13) & 31, 18,
						cbeams[c].wep.sprite, 5 + (l << 8));
				}

			for (c = 0; c < MAX_COMBAT_PROJECTILES; c++)
				if (cprojs[c].wep!=null)
				{
					l = 15;
					p = cprojs[c].wep.flags;
					if (AsBool(p & (int)weapon_flagids.wpfDisperse))
					{
						sz = 4 + ((cprojs[c].wep.size - 4) * (t - cprojs[c].str)) / (cprojs[c].end - cprojs[c].str);
						l = 15 - (14 * (t - cprojs[c].str)) / (cprojs[c].end - cprojs[c].str);
						if (cprojs[c].hits < cprojs[c].wep.damage)
						{	// fade as its hits go away
							l = 1 + ((l - 1)*cprojs[c].hits) / cprojs[c].wep.damage;
						}
					}
					else if (AsBool(p & (int)weapon_flagids.wpfImplode))
					{
						if (t - cprojs[c].str < 32)
							sz = (cprojs[c].wep.size * (t - cprojs[c].str)) >> 5;
						else
							sz = cprojs[c].wep.size;
						if (sz < 1) sz = 1;
					}
					else
					{
						sz = cprojs[c].wep.size;
					}


					if (AsBool(p & (int)weapon_flagids.wpfTrans))
						a = 5 + (l << 8);
					else
						a = 0;

					ik_drsprite(screen,
						cx + ((((cprojs[c].x - camera.x) >> 8) * camera.z) >> 14),
						cy - ((((cprojs[c].y - camera.y) >> 8) * camera.z) >> 14),
						cprojs[c].a,
						(sz * camera.z) >> 12,
						cprojs[c].wep.sprite, a);
					if (AsBool(p & (int)weapon_flagids.wpfImplode))
						ik_drsprite(screen,
							cx + ((((cprojs[c].x - camera.x) >> 8) * camera.z) >> 14),
							cy - ((((cprojs[c].y - camera.y) >> 8) * camera.z) >> 14),
							1023 - cprojs[c].a,
							(sz * camera.z) >> 12,
							spr_shockwave.spr[4], a);

					//		if (t > cprojs[c].end)
					//			cprojs[c].wep = null;
				}

			// check for fiery furies (gong)
			for (c = 0; c < MAX_COMBAT_SHIPS; c++)
				if (cships[c].bong_start > 0 && t > cships[c].bong_start + 50 && t < cships[c].bong_end && cships[c].hits > 0 && cships[c].type > -1)
				{
					for (l = 0; l < 5; l++)
					{
						p = (t - cships[c].bong_start - 50);
						p = ((p + 50)*(p + 50) - 2500) >> 6;
						a = (205 * l + p) & 1023;
						sz = 2000 * (cships[c].bong_end - t) / (cships[c].bong_end - cships[c].bong_start - 50);
						sx = cships[c].x + ((sin1k[a] * sz) >> 6);
						sy = cships[c].y + ((cos1k[a] * sz) >> 6);
						ik_drsprite(screen,
							cx + ((((sx - camera.x) >> 8) * camera.z) >> 14),
							cy - ((((sy - camera.y) >> 8) * camera.z) >> 14),
							((t << 5) & 1023),
							(32 * camera.z) >> 12,
							spr_weapons.spr[12], 4);
					}
				}

			for (c = 0; c < MAX_COMBAT_EXPLOS; c++)
				if (cexplo[c].spr!=null)
				{
					if (!AsBool(cexplo[c].zoom))
					{
						l = 15;
						sz = cexplo[c].size;
					}
					else
					{
						l = 15 - (t - cexplo[c].str) * 15 / (cexplo[c].end - cexplo[c].str);
						if (l < 0) l = 0;
						if (cexplo[c].zoom == 1)
							sz = (cexplo[c].size * (t - cexplo[c].str)) / (cexplo[c].end - cexplo[c].str);
						else
							sz = cexplo[c].size;
					}
					a = (cexplo[c].a + (t - cexplo[c].str) * cexplo[c].va) & 1023;
					if (cexplo[c].anim == -1)
					{
						p = (t - cexplo[c].str)*cexplo[c].spr.num / (cexplo[c].end - cexplo[c].str);
						if (p >= cexplo[c].spr.num)
							p = cexplo[c].spr.num - 1;
					}
					else p = cexplo[c].anim;

					ik_drsprite(screen,
						cx + ((((cexplo[c].x - camera.x) >> 8) * camera.z) >> 14),
						cy - ((((cexplo[c].y - camera.y) >> 8) * camera.z) >> 14),
						a,
						(sz * camera.z) >> 12,
						cexplo[c].spr.spr[p],
						5 + (l << 8));
					if (t > cexplo[c].end)
						cexplo[c].spr = null;
				}

			if (camera.ship_sel > -1)
			{
				t = get_ik_timer(1);

				s = camera.ship_sel;
				if (t >= camera.time_sel + 40)
				{
					l = 6;
					p = Math.Max(cships[s].ds_s, 16);
					a = cships[s].a;
				}
				else
				{
					l = 15 - ((t - camera.time_sel) >> 2);
					p = cships[s].ds_s + ((cships[s].ds_s * (camera.time_sel + 40 - t)) >> 5);
					a = (cships[s].a + (camera.time_sel + 40 - t) * 16) & 1023;
				}
				ik_drsprite(screen,
					cships[s].ds_x,
					cships[s].ds_y,
					a,
					p,
					spr_IFtarget.spr[8], 5 + (l << 8));
				if (t < camera.time_sel + 32)
				{
					l = 15 - ((t - camera.time_sel) >> 1);
					ik_drsprite(screen,
						cships[s].ds_x,
						cships[s].ds_y,
						a,
						(cships[s].ds_s * (t - camera.time_sel)) >> 3,
						spr_IFtarget.spr[8], 5 + (l << 8));
				}

				fr = 2;
				s = cships[camera.ship_sel].target;
				if (s > -1)
				{
					camera.ship_trg = s;
					if ((cships[camera.ship_sel].own & 1) == (cships[s].own & 1))
						fr = 1;
					else if (cships[s].active == 2)
						fr = 0;
					else
						fr = 2;

					if (AsBool(cships[camera.ship_sel].cloaked) && shiptypes[cships[s].type].race == race_ids.race_unknown)
						fr = 2;

					if (t >= camera.time_trg + 40)
					{
						l = 6;
						p = cships[s].ds_s;
						a = cships[s].a;
					}
					else
					{
						l = 15 - ((t - camera.time_trg) >> 2);
						p = cships[s].ds_s + ((cships[s].ds_s * (camera.time_trg + 40 - t)) >> 5);
						a = (cships[s].a + (camera.time_trg + 40 - t) * 16) & 1023;
					}

					if (fr == 2)	// "board"
						ik_print(screen, font_6x8, cships[s].ds_x - 15, cships[s].ds_y - cships[s].ds_s / 2 - 12, 4, "BOARD");

					ik_drsprite(screen,
						cships[s].ds_x,
						cships[s].ds_y,
						a,
						Math.Max(cships[s].ds_s, 16),
						spr_IFtarget.spr[6 + fr], 5 + (l << 8));
					if (t < camera.time_trg + 32)
					{
						l = 15 - ((t - camera.time_trg) >> 1);
						ik_drsprite(screen,
							cships[s].ds_x,
							cships[s].ds_y,
							a,
							(cships[s].ds_s * (t - camera.time_trg)) >> 3,
							spr_IFtarget.spr[6 + fr], 5 + (l << 8));
					}
				}
				if (!AsBool(camera.drag_trg))
				{
					l = 4 + 3 * AsInt((t & 31) > 24);
					p = cships[camera.ship_sel].dist;
					if (s > -1)
					{
						x = cships[s].ds_x - ((((sin1k[cships[camera.ship_sel].angle] * p) >> 16) * camera.z) >> 12);
						y = cships[s].ds_y + ((((cos1k[cships[camera.ship_sel].angle] * p) >> 16) * camera.z) >> 12);
					}
					else
					{
						x = cx + ((((cships[camera.ship_sel].wp_x - camera.x) >> 8) * camera.z) >> 14);
						y = cy - ((((cships[camera.ship_sel].wp_y - camera.y) >> 8) * camera.z) >> 14);
					}
				}
				else
				{
					l = 7 + 8 * AsInt((t & 31) > 24);
					x = ik_mouse_x;
					y = ik_mouse_y;
				}
				if (s > -1)
					a = get_distance(cships[s].ds_x - x, y - cships[s].ds_y);
				if (s > -1 && a > (cships[s].ds_s >> 1))
				{
					if (cships[camera.ship_sel].tac == 0)
					{
						a = get_direction(x - cships[camera.ship_sel].ds_x, cships[camera.ship_sel].ds_y - y);
						ik_dspriteline(screen,
							cships[camera.ship_sel].ds_x,
							cships[camera.ship_sel].ds_y,
							x - (sin1k[a] >> 13),
							y + (cos1k[a] >> 13),
							8, (t & 15), 16, spr_IFtarget.spr[fr * 2], 5 + (l << 8));
						a = get_direction(cships[s].ds_x - x, y - cships[s].ds_y);
						if ((cships[s].own & 1) != (cships[camera.ship_sel].own & 1))
						{
							ik_dspriteline(screen,
								x + (sin1k[a] >> 13),
								y - (cos1k[a] >> 13),
								cships[s].ds_x,
								cships[s].ds_y,
								8, (t & 15), 16, spr_IFtarget.spr[fr * 2], 5 + (l << 8));
						}
					}
					else
					{
						ik_dspriteline(screen,
							cships[camera.ship_sel].ds_x,
							cships[camera.ship_sel].ds_y,
							cships[s].ds_x,
							cships[s].ds_y,
							8, (t & 15), 16, spr_IFtarget.spr[fr * 2], 5 + (l << 8));
					}
					ik_drsprite(screen,
						x,
						y,
						(t * 8) & 1023,
						12,
						spr_IFtarget.spr[fr + 6], 5 + (l << 8));
				}
				else
				{
					if (s > -1)
					{
						x = cships[s].ds_x;
						y = cships[s].ds_y;
					}
					else
						ik_drsprite(screen,
							x,
							y,
							(t * 8) & 1023,
							12,
							spr_IFtarget.spr[fr + 6], 5 + (l << 8));

					ik_dspriteline(screen,
						cships[camera.ship_sel].ds_x,
						cships[camera.ship_sel].ds_y,
						x,
						y,
						8, (t & 15), 16, spr_IFtarget.spr[fr * 2], 5 + (l << 8));
				}
			}
			ik_setclip(0, 0, 640, 480);
			interface_drawborder(screen,
				160, 0, 640, 480,
				0, COMBAT_INTERFACE_COLOR, textstring[(int)textstrings.STR_COMBAT_TITLE]);
			// draw selected ships

			combat_displayships();


			// pause button
			ik_dsprite(screen, 170, 456, spr_IFbutton.spr[4], 2 + (COMBAT_INTERFACE_COLOR << 8));
			ik_dsprite(screen, 186, 456, spr_IFbutton.spr[5], 2 + (COMBAT_INTERFACE_COLOR << 8));
			ik_dsprite(screen, 202, 456, spr_IFbutton.spr[6], 2 + (COMBAT_INTERFACE_COLOR << 8));

			ik_dsprite(screen, 177, 456, spr_IFbutton.spr[7 + AsInt(pause != 1)], 2 + (COMBAT_INTERFACE_COLOR << 8));
			ik_dsprite(screen, 190, 456, spr_IFbutton.spr[9 + AsInt(pause != 0)], 2 + (COMBAT_INTERFACE_COLOR << 8));
			ik_dsprite(screen, 199, 456, spr_IFbutton.spr[17 + AsInt(pause != -1)], 2 + (COMBAT_INTERFACE_COLOR << 8));

			// race portraits
			for (x = 1; x < player.num_ships; x++)
				if (!AsBool(shiptypes[(int)player.ships[x]].flag & 8))
				{
					if (cships[x].hits > 0)
						ik_dsprite(screen, 176 + (x - 1) * 68, 24, spr_SMraces.spr[(int)shiptypes[(int)player.ships[x]].race], 0);
					else
						ik_drsprite(screen, 176 + (x - 1) * 68 + 32, 24 + 32, 0, 64, spr_SMraces.spr[(int)shiptypes[(int)player.ships[x]].race], 1 + (24 << 8));
					ik_dsprite(screen, 176 + (x - 1) * 68, 24, spr_IFborder.spr[18], 2 + (4 << 8));
				}

			y = 0;
			for (x = 0; x < MAX_COMBAT_SHIPS; x++)
			{
				if (cships[x].own == 2 && cships[x].type > -1) // klakar
				{
					if (cships[x].hits > 0)
						ik_dsprite(screen, 176 + (player.num_ships - 1) * 68, 24, spr_SMraces.spr[(int)shiptypes[cships[x].type].race], 0);
					else
						ik_drsprite(screen, 176 + (player.num_ships - 1) * 68 + 32, 24 + 32, 0, 64, spr_SMraces.spr[(int)shiptypes[cships[x].type].race], 1 + (24 << 8));
					ik_dsprite(screen, 176 + (player.num_ships - 1) * 68, 24, spr_IFborder.spr[18], 2 + (3 << 8));
				}
				else if (cships[x].own == 1 && y == 0)	// enemy
				{
					if (cships[x].type > -1 && (int)shiptypes[cships[x].type].race < (int)race_ids.race_unknown)
					{
						ik_dsprite(screen, 560, 24, spr_SMraces.spr[(int)shiptypes[cships[x].type].race], 0);
						ik_dsprite(screen, 560, 24, spr_IFborder.spr[18], 2 + (1 << 8));
						y = 1;
					}
				}
			}

            //#ifdef DEBUG_COMBAT
			//ik_print(screen, font_6x8, 176, 16, 0, combatdebug);
            //#endif

		}
	}
}