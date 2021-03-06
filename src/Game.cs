// ----------------
//     INCLUDES
// ----------------

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <math.h>

#include "typedefs.h"
#include "iface_globals.h"
#include "is_fileio.h"
#include "gfx.h"
#include "interface.h"
#include "starmap.h"
#include "combat.h"

#include "cards.h"

// ----------------
//     CONSTANTS
// ----------------

char *ecard_keywords[eckMax] =
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

t_eventcard			*ecards;
int32						num_ecards;

// ----------------
// LOCAL PROTOTYPES
// ----------------

// ----------------
// GLOBAL FUNCTIONS
// ----------------

void cards_init()
{
	FILE* ini;
	char s1[64], s2[256];
	char end;
	int num;
	int flag;
	int n, com;
	int numtypes = 0;
	char cardtypenames[16][32];

	ini = myopen("gamedata/cards.ini", "rb");
	if (!ini)
		return;
 
	end = 0; num = 0; flag = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		if (!strcmp(s1, ecard_keywords[eckBegin]))
			num++;
		else if (!strcmp(s1, "CARDTYPES"))
		{
			flag = 1; n = 0;
		}
		else if (flag == 1)
		{
			if (!strcmp(s1, "END"))
			{	numtypes = n; flag = 0; }
			else
				strcpy(cardtypenames[n++], s1);
		}
	}
	fclose(ini);

	ecards = (t_eventcard*)calloc(num, sizeof(t_eventcard));
	if (!ecards)
		return;
	num_ecards = num;

	ini = myopen("gamedata/cards.ini", "rb");

	end = 0; num = 0; flag = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		com = -1;
		for (n = 0; n < eckMax; n++)
			if (!strcmp(s1, ecard_keywords[n]))
				com = n;

		if (flag == 0)
		{
			if (com == eckBegin)
			{
				flag = 1;
				strcpy(ecards[num].text, "\0");
				ecards[num].parm = 0;
			}
		}
		else switch(com)
		{
			case eckName:
			strcpy(ecards[num].name, s2);
			break;

			case eckText:
			strcpy(ecards[num].text, s2);
			ecards[num].text[strlen(s2)]=0;
			break;

			case eckText2:
			strcpy(ecards[num].text2, s2);
			ecards[num].text2[strlen(s2)]=0;
			break;

			case eckType:
			for (n = 0; n < numtypes; n++)
				if (!strcmp(s2, cardtypenames[n]))
					ecards[num].type = n;
			break;

			case eckParam:
			if (ecards[num].type == card_ally)
			{
				ecards[num].parm = 0;
				for (n = 0; n < num_shiptypes; n++)
					if (!strcmp(s2, shiptypes[n].name))
						ecards[num].parm = n;
			}
			else if (ecards[num].type == card_event)
			{
				sscanf(s2, "%d", &n);
				ecards[num].parm = n;
			}
			break;

			case eckEnd:
			if ((ecards[num].type == card_item) || (ecards[num].type == card_rareitem) || (ecards[num].type == card_lifeform))
			{
				for (n = 0; n < num_itemtypes; n++)
					if (!strcmp(ecards[num].name, itemtypes[n].name))
						ecards[num].parm = n;
			}

			num++; flag = 0;
			break;

			default: ;
		}

	}
	fclose(ini);
}

void cards_deinit()
{
	num_ecards = 0;
	free(ecards);
}

void card_display(int n)
{
	int32 mc, c;
	int32 end = 0;

	prep_screen();
	interface_drawborder(screen,
											 224, 112, 416, 368,
											 1, STARMAP_INTERFACE_COLOR, ecards[n].name);
	interface_textbox(screen, font_6x8,
										240, 136, 160, 224, 0,
										ecards[n].text);
	ik_blit();

	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		mc = ik_mclick();	
		c = ik_inkey();

		if (c == 13)
			end = 1;
	}

}
// ----------------
//     INCLUDES
// ----------------

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <math.h>

#include "typedefs.h"
#include "iface_globals.h"
#include "is_fileio.h"
#include "gfx.h"
#include "snd.h"
#include "interface.h"
#include "starmap.h"
#include "textstr.h"

#include "combat.h"

// ----------------
//		CONSTANTS
// ----------------

// ----------------
// LOCAL VARIABLES
// ----------------

t_combatcamera		camera;
t_ship						cships[MAX_COMBAT_SHIPS];
t_wepbeam					cbeams[MAX_COMBAT_BEAMS];
t_wepproj					cprojs[MAX_COMBAT_PROJECTILES];
t_explosion				cexplo[MAX_COMBAT_EXPLOS];

int32 numships;
int32 playership;
int32 sortship[MAX_COMBAT_SHIPS];

int32 t_move, t_disp, pause;

int32 nebula;
int32 retreat;
int32 rett;				// retreat time
int32 klaktime;
int32 klakavail;
int32 gongavail;

int32 simulated;

#ifdef DEBUG_COMBAT
char combatdebug[64];
#endif

// ----------------
// LOCAL PROTOTYPES
// ----------------

void combat_start(int32 flt);
void combat_end(int32 flt);

int32 combat_prepare(int32 flt);
void combat_movement(int32 t);

void combat_checkalttargets(int32 s, int32 t);
void combat_checkescapes(int32 t);

int32 combat_findship(int32 mx, int32 my);
int32 shiptonum(t_ship *s);

void combat_summon_klakar(int32 t);
void combat_removeenemyship(int32 flt, int32 s);
int32 combat_use_gong(int32 t);

void combat_launch_fighter(int32 s, int32 t);


int32 calc_leadangle(int32 tx, int32 ty, int32 vtx, int32 vty, 
										 int32 sx, int32 sy, int32 vsx, int32 vsy,
										 int32 speed);

// ----------------
// GLOBAL FUNCTIONS
// ----------------

int32 combat(int32 flt, int32 sim)
{
	int32 t, t0;
	int32 c, mc, s;
	int32 b;
	int32 f;
	int32 end;
	int32 klak;

	simulated = sim;

	combat_start(flt);

	ik_inkey();

	t_move = 0; t_disp = 0; pause = 0; end = 0; rett=0;

	if (simulated)
		Play_Sound(WAV_MUS_SIMULATOR, 15, 1, 85);	
	else if (!nebula)
		Play_Sound(WAV_MUS_COMBAT, 15, 1);
	else
		Play_Sound(WAV_MUS_NEBULA, 15, 1);
	start_ik_timer(1, 1000/COMBAT_FRAMERATE); t0 = t = 0;
	while (!must_quit && (t<end || end==0))
	{
		t0 = t;
		ik_eventhandler();  // always call every frame
		t = get_ik_timer(1);

		if (must_quit)
		{
			must_quit = 0;
			Play_SoundFX(WAV_DESELECT);
			if (simulated)
			{
				if (!interface_popup(font_6x8, 240, 200, 160, 72, COMBAT_INTERFACE_COLOR, 0, 
						textstring[STR_QUIT_TITLE], textstring[STR_QUIT_SIMULATION], 
						textstring[STR_YES], textstring[STR_NO]))
				{	must_quit = 1; player.death = 666; }
			}
			else
			{
				if (!interface_popup(font_6x8, 240, 200, 160, 72, COMBAT_INTERFACE_COLOR, 0, 
						textstring[STR_QUIT_TITLE], textstring[STR_QUIT_CONFIRM], 
						textstring[STR_YES], textstring[STR_NO]))
				{	must_quit = 1; player.death = 666; }
			}
			ik_eventhandler();  // always call every frame
			t = get_ik_timer(1);
			t0 = t;
		}

		mc = ik_mclick();	
		c = ik_inkey();
		b = ik_mouse_b;

		klak = klakavail;

		if (IsMinimized)
			pause=1;

		if (c==32)
		{
			if (pause==1)
				pause=0;
			else
				pause=1;
		}

		if (key_pressed(key_f[0]))
		{	
			combat_help_screen();
			ik_eventhandler();  // always call every frame
			t = get_ik_timer(1);
			t0 = t;
		}

#ifdef DEBUG_COMBAT
			if (camera.ship_trg > -1)
			{
				s = camera.ship_trg;
				if (cships[s].type > -1 && cships[s].hits >= 0)
				{
					switch(c)
					{
						case 'w':
							cships[s].syshits[0] = 0;
							combat_updateshipstats(s, t_move);
						break;

						case 'd':
							combat_damageship(s, playership, cships[s].hits, t, &shipweapons[0], 1);
						break;

						case 'e':
							combat_damageship(s, playership, cships[s].hits + hulls[shiptypes[cships[c].type].hull].hits + 1, t, &shipweapons[0], 1);
						break;

						case 'f':
							cships[s].flee = 1;
							cships[s].tac = tac_flee;
							cships[s].target = -1;
						break;
					}
				}
			}
#endif

		f=0;
		for (s=0;s<MAX_COMBAT_SHIPS;s++)
		if (cships[s].hits>0 && cships[s].type>-1 && cships[s].escaped==0 && cships[s].flee==0)
		{
			if ((cships[s].own&1)==0)
				f |= 1;
			else 
				f |= 2;
		}
		if ((f == 1 || cships[playership].type==-1) && retreat)
		{
			retreat = 0;
			Play_SoundFX(WAV_SELECT, t);

			if (simulated)
				Play_Sound(WAV_MUS_SIMULATOR, 15, 1, 85);	
			else if (!nebula)
				Play_Sound(WAV_MUS_COMBAT, 15, 1);
			else
				Play_Sound(WAV_MUS_NEBULA, 15, 1);
			for (s = 0; s < MAX_COMBAT_SHIPS; s++)
			{
				if ((cships[s].own&1) == 0 && s != playership)
					combat_findstuff2do(s, t);
			}
		}

		if (ik_mouse_x < 160 && (mc&1))
		{
			if (ik_mouse_y > 24 && ik_mouse_y < 40)
			{	// select ship by icon
				s = (ik_mouse_x-16)/16;
				if (s >= 0 && s < player.num_ships)
				{
					if (cships[s].type > -1 && cships[s].hits > 0)
					{
						select_ship(s, t);
					}
				}
			}
			if (ik_mouse_y > 288 && ik_mouse_y < 320 && cships[playership].hits>0)
			{
				s = (ik_mouse_x / 80) + ((ik_mouse_y - 288)/16)*2;
				switch (s)
				{
					case 0: 				// cloak button
					if (cships[playership].clo_type>0 && cships[playership].syshits[cships[playership].sys_clo]>=5 && t_move>cships[playership].cloaktime+100)
					{
						Play_SoundFX(WAV_DOT, 0);
						if (cships[playership].cloaked)	// uncloak
						{
							cships[playership].cloaked = 0;
							cships[playership].cloaktime = t_move;
							//Play_SoundFX(WAV_CLOAKOUT, t);
						}
						else	// cloak
						{

							cships[playership].cloaked = 1;
							cships[playership].cloaktime = t_move;
							//Play_SoundFX(WAV_CLOAKIN, t);
						}
					}
					break;

					case 1:	// gong
					if (gongavail==2)
					{
						combat_use_gong(t_move);
						gongavail = 1;
					}
					break;

					case 2:	// retreat button
					if (f == 3) 
					{
						if (!retreat)
						{
							retreat = 1;
							rett = t_move;
							Play_SoundFX(WAV_SELECT, t);
							Play_Sound(WAV_FLARE, 15, 1);
							
							for (s = 0; s < MAX_COMBAT_SHIPS; s++)
							if ((cships[s].own&1) == 0)
								combat_findstuff2do(s, t);
						}
						else
						{
							retreat = 0;
							Play_SoundFX(WAV_SELECT, t);
							if (simulated)
								Play_Sound(WAV_MUS_SIMULATOR, 15, 1, 85);	
							else if (!nebula)
								Play_Sound(WAV_MUS_COMBAT, 15, 1);
							else
								Play_Sound(WAV_MUS_NEBULA, 15, 1);

							for (s = 0; s < MAX_COMBAT_SHIPS; s++)
							{
								if ((cships[s].own&1) == 0 && s != playership)
									combat_findstuff2do(s, t);
							}
						}
					}
					break;

					case 3:	// summon klakar
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
				if (mc & 1)
				{
					if (ik_mouse_x < 186)
						pause = 1;
					else if (ik_mouse_x > 202)
						pause = -1;
					else
						pause = 0;
				}
			}
			else if ( mc & 1 )
			{
				if ( !camera.drag_trg )
				{
					s = combat_findship(ik_mouse_x, ik_mouse_y);
					if (s > -1)
					{
						if (cships[s].own==0)	// select friendly
						{
							select_ship(s, t);
						}
						else if (camera.ship_sel>-1)	// target enemy
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
								if (abs (cships[s].ds_x - ((((sin1k[cships[camera.ship_sel].angle] * cships[camera.ship_sel].dist)>>16) * camera.z)>>12) - ik_mouse_x) < 8	&&
										abs (cships[s].ds_y + ((((cos1k[cships[camera.ship_sel].angle] * cships[camera.ship_sel].dist)>>16) * camera.z)>>12) - ik_mouse_y) < 8)
									camera.drag_trg = 1;
							}
							else
							{
								s = camera.ship_sel;
								if (abs (160 + 240 + ((((cships[s].wp_x-camera.x)>>10) * camera.z)>>12) - ik_mouse_x) < 8	&&
										abs (244 - ((((cships[s].wp_y-camera.y)>>10) * camera.z)>>12) - ik_mouse_y) < 8)
									camera.drag_trg = 1;
							}

						}

						if (!camera.drag_trg)
						{
							if (camera.ship_sel > -1)
							{
								camera.drag_trg = 1;
								cships[camera.ship_sel].target = -1;
								cships[camera.ship_sel].tac = 2;
								cships[camera.ship_sel].wp_x = camera.x + ((((ik_mouse_x - 400)<<12)/camera.z)<<10);
								cships[camera.ship_sel].wp_y = camera.y + ((((244-ik_mouse_y)<<12)/camera.z)<<10);
							}						
	//						camera.ship_sel = -1;
	//						camera.ship_trg = -1;
						}
					}
				}
			}
			else if ( (mc & 2)>0 && camera.ship_sel>-1)
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

			if (!(b&3) && camera.drag_trg > 0)
			{
				camera.drag_trg = 0;
				if (camera.ship_sel > -1)
				{
					if (cships[camera.ship_sel].target > -1)
					{
						s = camera.ship_sel;
						cships[s].angle = get_direction( cships[camera.ship_trg].ds_x - ik_mouse_x,
																						 ik_mouse_y - cships[camera.ship_trg].ds_y );
						cships[s].dist = get_distance( cships[camera.ship_trg].ds_x - ik_mouse_x,
																					 ik_mouse_y - cships[camera.ship_trg].ds_y );
						if (cships[s].dist <= cships[camera.ship_trg].ds_s>>1)
						{	
							if ((cships[s].own&1) == (cships[camera.ship_trg].own&1))
								cships[s].dist = 64;
							else
								cships[s].dist = 0;
						}
						else
							cships[s].dist = (cships[s].dist << 12) / camera.z;
					}
					else
					{
						cships[camera.ship_sel].wp_x = camera.x + ((((ik_mouse_x - 400)<<12)/camera.z)<<10);
						cships[camera.ship_sel].wp_y = camera.y + ((((244-ik_mouse_y)<<12)/camera.z)<<10);
					}
				}
			}
		}

		if (t>t0)
		{
			combat_checkescapes(t_move);
			f=0;
			for (s=0;s<MAX_COMBAT_SHIPS;s++)
			if (cships[s].type>-1 && cships[s].escaped==0 && cships[s].active>0)
			{
				if ((cships[s].own&1)==0)
				{
					if (cships[playership].type>-1)
						f |= 1;
				}
				else 
					f |= 2;
			}

			if (f!=3)
			{
				if (end==0)
				{
					end = t+100;
				}
			}
			else
				end = 0;

			if (key_pressed(key_up) && camera.z < 256)
				camera.z ++;
			if (key_pressed(key_down) && camera.z > 4)
				camera.z --;

			prep_screen();
			if (wants_screenshot)
				ik_save_screenshot(screen, globalpal);

			ik_drawbox(screen, 0, 0, 640, 480, 0);

			if (pause < 1)
			{
				while (t0<t)
				{
					t0++;
					s = 1 + 2*(pause == -1);
					while (s--)
					{
						t_move++; 
						combat_movement(t_move);
						if (t_move==klaktime+1 && klaktime>0) 
							Play_SoundFX(WAV_HYPERDRIVE, get_ik_timer(1));
					}
				}
			}
			if (t_move > t_disp || (pause==1))
			{
				t_disp = t_move;
				combat_display(t_disp);
			}

			ik_blit();
			if (settings.random_names & 4)
			{
				interface_tutorial(tut_combat);
				ik_eventhandler();  // always call every frame
				t = get_ik_timer(1);
				t0 = t;
			}
		}
	}

	combat_end(flt);

	if (!simulated)
		Stop_All_Sounds();

	return 1;
}

// ----------------
// LOCAL FUNCTIONS
// ----------------

void select_ship(int32 s, int32 t)
{
	camera.ship_sel = s;
	camera.time_sel = t;
	camera.ship_trg = cships[s].target;
	camera.time_trg = t;
}

void combat_start(int32 flt)
{
	int t, p;
	int r, s;
	int x, y;
	int nc, rc, nf;
	int32 angle;

	srand( (unsigned)time( NULL ) );

	retreat = 0;

	if (simulated)
		nebula = 0;
	else if (sm_nebulamap[((240-player.y)<<9)+(240+player.x)]>0)
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
		cprojs[t].wep = NULL;
	for (t = 0; t < MAX_COMBAT_BEAMS; t++)
		cbeams[t].wep = NULL;
	for (t = 0; t < MAX_COMBAT_EXPLOS; t++)
		cexplo[t].spr = NULL;
	camera.x = 0;
	camera.y = 0;
	camera.z = 4096;

	camera.ship_sel = -1;
	camera.ship_trg = -1;

	angle = rand()%1024;

	t = 0;
	for (p = 0; p < player.num_ships; p++)
	{
		if (p == 0)
		{	cships[t].x = 0; playership = t; }
		else {
			cships[t].x = ((t+1)/2)*128;
			if (p&1)
				cships[t].x = -cships[t].x;
		}
		cships[t].y = -700;
		cships[t].a = 0;
		cships[t].type = player.ships[p];
		cships[t].own = 0;
		t++;
	}

	s = sm_fleets[flt].num_ships;		//rand()%4 + 1;
	rc = sm_fleets[flt].race;

	// place enemy ships
	nc = 0; nf = 0;
	for (p = 0; p < s; p++)
	{
		if (hulls[shiptypes[sm_fleets[flt].ships[p]].hull].size>=32)	
		{ 
			if (!(shiptypes[sm_fleets[flt].ships[p]].flag & 128))	// if not deep hunter
				nc++; 
		}
		else
		{	nf++; }
	}

	for (p = 0; p < s; p++) 
	{
		cships[t].type = sm_fleets[flt].ships[p];
		cships[t].own = 1;
		if (hulls[shiptypes[cships[t].type].hull].size>=32)
		{ r = 1; }
		else
		{	r = 0; }
		if (shiptypes[cships[t].type].flag & 128)
		{	r = 2; }

		switch (rc)
		{
			case race_garthan:	// V formation
			cships[t].x = ((p+1)/2)*96;
			if (p&1)
				cships[t].x = -cships[t].x;
			cships[t].y = 700+((p+1)/2)*64;
			cships[t].a = 512;
			break;


			case race_muktian:
			if (r)	// corvette
			{
				if (nc == 3)
					cships[t].x = 96 + 96*((p==1)-(p==2));
				else
					cships[t].x = p*96;
				cships[t].y = 700;
			}
			else	// fighter circle
			{
				y = 1024 * (p-nc) / (sm_fleets[flt].num_ships-nc);
				x = MAX(nc-1, 0);
				cships[t].x = x*48 + ((sin1k[y]*(128+x*64))>>16);
				cships[t].y = 700 - ((cos1k[y]*128)>>16);
			}
			cships[t].a = 512;
			break;

			case race_tanru:	// grids
			if (r)	// corvette
			{
				cships[t].x = -64 + 128 * (p & 1);
				cships[t].y = 700 + 128 * (p/2);
			}
			else	// fighter
			{
				cships[t].x = -512 + 1024*(flt&1) + 128 * ((p - nc)&1);
				cships[t].y = 700 + 128 * ((p-nc)/2);
			}
			cships[t].a = 512;
			break;

			case race_urluquai:
			x = p; 
			if (r == 2)
			{
				y = rand()%1024;
			}
			else
			{
				//if (sm_fleets[flt].num_ships != nc+nf) x = p - (sm_fleets[flt].num_ships-(nc+nf));	// don't count deep hunters in formation
				// 	y = ((x+1)/2)*512/((sm_fleets[flt].num_ships+1)/2);
				//if (x&1)
				//	y = -y;
				x = p - (sm_fleets[flt].num_ships - (nc+nf));
				y = (x * 1024) / (nc+nf);
				y = y - ((nc/2) * 512) / (nc+nf);
			}
			y = (1024 + y) & 1023;
			cships[t].x = (sin1k[y]*1400) >> 16;
			cships[t].y = ((cos1k[y]*1400) >> 16) - 700;
			cships[t].a = (y+512) & 1023;
			break;


			default:
			cships[t].x = ((p+1)/2)*128;
			if (p&1)
				cships[t].x = -cships[t].x;
			cships[t].y = 700;
			cships[t].a = 512;
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
		cships[t].hits = shiptypes[cships[t].type].hits/256; //hulls[shiptypes[cships[t].type].hull].hits;
		cships[t].shld = 0;
		cships[t].shld_time = 0;
		cships[t].shld_charge = 0;
		cships[t].damage_time = 0;
		cships[t].dmgc_time = 0;
		cships[t].tac = 0;
		cships[t].escaped = 0;
		cships[t].active = 2;
		if (shiptypes[cships[t].type].race == race_unknown)
		{	
			cships[t].active = 1; cships[t].va = (rand()%3+1)*( (rand()&1)*2-1 ); 
			if (sm_fleets[flt].system & 1)	// 50% chance of being dead
			{
				cships[t].hits = 1;
			}
		}
		cships[t].cloaked = 0;
		cships[t].cloaktime = 0;

		for (x = 0; x < shiptypes[cships[t].type].num_systems; x++)
		{
			cships[t].syshits[x] = 10*(shiptypes[cships[t].type].sysdmg[x]==0);
		}
		combat_updateshipstats(t, 0);

		if (cships[t].clo_type > 0)
			{	cships[t].cloaked = 1; cships[t].cloaktime = 0; }
		if (cships[t].shld_type > -1)
			cships[t].shld = shipsystems[cships[t].shld_type].par[0];

		combat_findstuff2do(t, 0);
	}


	klaktime = 0;
	klakavail = 0;
	for (t = 0; t < STARMAP_MAX_FLEETS; t++)
	if (sm_fleets[t].race == race_klakar && sm_fleets[t].num_ships > 0)
	{
		for (s = 0; s < player.num_items; s++)
		{
			if (itemtypes[player.items[s]].flag & 4)
				klakavail = 1;
		}
	}

	gongavail = 0;
	for (t = 0; t < player.num_items; t++)
	{
		if (itemtypes[player.items[t]].flag & device_gong)
			gongavail = 2;
	}

	camera.ship_sel = playership;
	camera.time_sel = 0;

}

void klakar_pissoff()
{
	int32 mc, c;
	int32 end = 0;
	int32 bx = 216, by = 152;
	int32 mx = 0, my = 0;
	int32 r=0, t=0;
	char str[256];

	r = race_klakar;

	halfbritescreen();

	// trader greeting screen
	prep_screen();
	sprintf(str, textstring[STR_VIDCAST], races[r].name);
	interface_drawborder(screen,
											 bx, by, bx+208, by+144,
											 1, STARMAP_INTERFACE_COLOR, str);
	ik_print(screen, font_6x8, bx+16, by+26, 3, textstring[STR_VIDCAST2]);
	interface_textbox(screen, font_4x8,
										bx+88, by+40, 104, 64, 0,
										textstring[STR_KLAK_NOPAY]);

	ik_dsprite(screen, bx+16, by+40, spr_SMraces->spr[race_klakar], 0);
	ik_dsprite(screen, bx+16, by+40, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));
	interface_drawbutton(screen, bx+128, by+116, 64, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);

	ik_blit();

	Play_Sound(WAV_MESSAGE, 15, 1);

	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		mc = ik_mclick();	
		c = ik_inkey();
		mx = ik_mouse_x - bx; my = ik_mouse_y - by;

		if (mc == 1 && mx > 128 && mx < 192 && my > 116 && my < 132)
		{	end = 2; Play_SoundFX(WAV_DOT, get_ik_timer(0)); }

		c = t; t = get_ik_timer(2);
		if (t != c)
		{ prep_screen(); ik_blit();	}
	}

	Stop_Sound(15);

	reshalfbritescreen();

	prep_screen();
	ik_blit();
}

void combat_removeenemyship(int32 flt, int32 s)
{
	int32 c;

	for (c = s; c < sm_fleets[flt].num_ships; c++)
	{
		sm_fleets[flt].ships[c] = sm_fleets[flt].ships[c+1];
	}
	sm_fleets[flt].num_ships--;
}

void combat_sim_end()
{
	int32 end;
	int32 c, mc;
	int32 bx=192, by=96, h=232;
	int32 mx, my;
	int32 t, ot;
	int32 s, co;
	int32 en[3];
	t_ik_image *bg;

	if (must_quit)
		return;

	Stop_All_Sounds();
	bg = ik_load_pcx("graphics/starback.pcx", NULL);

	Play_SoundFX(WAV_ENDSIMULATION);

	end = 0; t = get_ik_timer(2);
	while (!end && !must_quit)
	{
		ik_eventhandler();
		c = ik_inkey();
		mc = ik_mclick();
		mx = ik_mouse_x - bx; 
		my = ik_mouse_y - by;

		if (c==13 || c==32)
			end = 1;

		if (mc & 1)
			if (mx > 240-64 && mx < 240-16 && my > h-24 && my < h-8)
			{	end = 1; Play_SoundFX(WAV_DOT2, 0, 50); }

		ot = t;
		t = get_ik_timer(2);
		if (t != ot)
		{
			prep_screen();
			ik_copybox(bg, screen, 0, 0, 640, 480, 0,0);

			interface_drawborder(screen, bx, by, bx+240, by+h, 1, STARMAP_INTERFACE_COLOR, textstring[STR_COMBAT_SIMEND]);

			ik_print(screen, font_6x8, bx+120-12*3, by+24, 4, textstring[STR_COMBAT_SIMALLY]);
			for (c = 0; c < player.num_ships; c++)
			{
				s = (cships[c].hits > 0);
				interface_thinborder(screen, bx+16+c*72, by+36, bx+80+c*72, by+108, s*STARMAP_INTERFACE_COLOR+(1-s), 0);
				ik_drsprite(screen, bx+48+c*72, by+76, 0, 64, hulls[shiptypes[player.ships[c]].hull].sprite, 1+((s*15+(1-s)*26)<<8));
				if (s)
					ik_print(screen, font_6x8, bx+20+c*72, by+40, 4, textstring[STR_COMBAT_SIMSURV]);
				else
					ik_print(screen, font_6x8, bx+20+c*72, by+40, 1, textstring[STR_COMBAT_SIMDEST]);
			}
			for (; c < 3; c++)
			{
				interface_thinborder(screen, bx+16+c*72, by+36, bx+80+c*72, by+108, STARMAP_INTERFACE_COLOR, STARMAP_INTERFACE_COLOR*16+2);
			}

			ik_print(screen, font_6x8, bx+120-11*3, by+120, 1, textstring[STR_COMBAT_SIMENMY]);
			ik_print(screen, font_6x8, bx+16, by+140, 0, textstring[STR_COMBAT_SIMSURV]);
			ik_print(screen, font_6x8, bx+16, by+162, 0, textstring[STR_COMBAT_SIMESCP]);
			ik_print(screen, font_6x8, bx+16, by+184, 0, textstring[STR_COMBAT_SIMDEST]);

			// count enemies for each row
			for (c = 0; c < 3; c++)
			{
				interface_thinborder(screen, bx+76, by+134+c*22, bx+79+144, by+153+c*22, STARMAP_INTERFACE_COLOR, 0);
				en[c] = 0;
			}
			for (c = 0; c < sm_fleets[0].num_ships; c++)
			{
				s = 0;
				if (cships[player.num_ships+c].hits <= 0) s = 2;
				else if (cships[player.num_ships+c].type == -1) s = 1;

				co = 15*(s==0)+58*(s==1)+26*(s==2);
				ik_drsprite(screen, bx+83+en[s]*12, by+143+s*22, 0, 16, hulls[shiptypes[sm_fleets[0].ships[c]].hull].sprite, 1+(co<<8));

				en[s]++;
			}
			for (c = 0; c < 3; c++)
				if (en[c] == 0)
					interface_thinborder(screen, bx+76, by+134+c*22, bx+79+144, by+153+c*22, STARMAP_INTERFACE_COLOR, STARMAP_INTERFACE_COLOR*16+2);

			interface_drawbutton(screen, bx+240-64, by+h-24, 48, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);

			ik_blit();
		}
	}

	if (must_quit)
		must_quit = 0;

	del_image(bg);
}

void combat_end(int32 flt)
{
	int32 c;
	int32 it;
	int32 b;
	int32 f;
	int32 x;
	int32 de = -1;
	char texty[256];

	if (simulated)
	{
		combat_sim_end();
		return;
	}

	for (c = 0; c < player.num_ships; c++)
	if (cships[c].type>-1)
	{
		shiptypes[cships[c].type].hits = cships[c].hits*256;
		for (x = 0; x < shiptypes[cships[c].type].num_systems; x++)
		{
			if (cships[c].syshits[x] <= 0 && shipsystems[shiptypes[cships[c].type].system[x]].item > -1)
			{
				shiptypes[cships[c].type].sysdmg[x] = 1;
				if ((de == -1) && (c==playership) && 
						(rand()%10==0) && 
						(shipsystems[shiptypes[cships[c].type].system[x]].type!=sys_shield))
				{
					de = shiptypes[cships[c].type].system[x];
					starmap_destroysystem(x);
				}
			}
		}
	}

	for (c = MAX_COMBAT_SHIPS-1; c >= player.num_ships; c--)
	{
		if (cships[c].own == 1 && (cships[c].hits<=0 || cships[c].type==-1))
		{
			if (c-player.num_ships < sm_fleets[flt].num_ships)
				combat_removeenemyship(flt, c-player.num_ships);
		}
	}

	for (c = player.num_ships-1; c >= 0; c--)
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
		if (cships[c].own == 2)			// klakar
		{
			f = -1;
			for (b = 0; b < STARMAP_MAX_FLEETS; b++)
				if (sm_fleets[b].race == race_klakar)
					f = b;
			if (cships[c].type > -1 && cships[c].hits>0)	// survived
			{
				if (player.num_ships > 0 && player.ships[0] == 0)	// player survives to pay
				{
					it = pay_item(textstring[STR_KLAK_PAYTITLE], textstring[STR_KLAK_PAYMENT], race_klakar, 1);
					if (it>-1)
					{
						kla_items[kla_numitems++] = it;
					}
					else	// take beacon away if you don't pay!
					{
						// display pissoff message
						klakar_pissoff();
						for (it=0; it < player.num_items; it++)
							if (itemtypes[player.items[it]].flag & 4)	
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
						it = rand()%num_stars;
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
								if (sm_fleets[b].num_ships>0 && b!=f && sm_fleets[b].system==it)
									it = -1;
					}
					if (it > -1)
						sm_fleets[f].system = it;				
				}
			}
			else	// klakar destroyed
			{
				sm_fleets[f].num_ships = 0;
			}
		}
	}
	
	if (de > -1 && player.num_ships > 0 && player.ships[0] == 0)
	{	// system was destroyed
		sprintf(texty, textstring[STR_SYSTEM_DESTROYED], player.shipname, shipsystems[de].name);
		interface_popup(font_6x8, 224, 192, 192, 96, STARMAP_INTERFACE_COLOR, 0, 
										textstring[STR_COMBAT_SYSDMG], texty, textstring[STR_OK]);
	}

}

void combat_movement(int32 t)
{
	int32 c;
	int32 d;
	int32 p;
	int32 a;
	int32 sys, lsys;
	int32 r, rm;
	int32 tg, wx, wy;
	int32 sp, sx, sy;
	t_hull *hull;

	// **** MOVE SHIPS ****
	for (c = 0; c < MAX_COMBAT_SHIPS; c++)
	if (cships[c].type > -1)
	{
		cships[c].x += cships[c].vx;
		cships[c].y += cships[c].vy;
		cships[c].a = (cships[c].a + cships[c].va) & 1023;

		if (cships[c].cloaktime > 0 && t == cships[c].cloaktime+1)
		{
			if (cships[c].cloaked)
				combat_SoundFX(WAV_CLOAKIN, cships[c].x);
			else
				combat_SoundFX(WAV_CLOAKOUT, cships[c].x);
		}

		if (cships[c].teltime > 0)
		{
			if (t - cships[c].teltime >= 32 && (cships[c].tel_x!=0 || cships[c].tel_y!=0))
			{
				cships[c].x += cships[c].tel_x;
				cships[c].y += cships[c].tel_y;
				cships[c].tel_x = 0;
				cships[c].tel_y = 0;
				//cexplo[combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 0, 96, 1, t, t+24, 1)].str = t-8;
				combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 1, 112, 0, t, t+10, 4);
			}
		}

		if (shiptypes[cships[c].type].flag & 16) 
		{
			if (t > cships[c].launchtime && cships[c].hits > 0)
			{
				p = racefleets[races[shiptypes[cships[c].type].race].fleet].stype[0];
				a = 0;
				for (d = 0; d < MAX_COMBAT_SHIPS; d++)
					if (cships[d].type == p && cships[d].hits > 0)
						if (cships[d].own == cships[c].own)
							a++;
				if (a < 3)	// don't launch more than 3 at once
				{
					combat_SoundFX(WAV_FIGHTERLAUNCH, cships[c].x);
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
					Play_SoundFX(WAV_FIERYFURY, t);
				else if (t > cships[c].bong_start + 50)
				{
					d = t % 5;
					p = (t-cships[c].bong_start-50);
					p = ((p+50)*(p+50)-2500) >> 6;
					a = (205*d + p) & 1023;
					r = 2000*(cships[c].bong_end-t)/(cships[c].bong_end-cships[c].bong_start-50);
					sx = cships[c].x + ((sin1k[a]*r)>>6);
					sy = cships[c].y + ((cos1k[a]*r)>>6);

					combat_addexplo(sx, sy, spr_explode1, 1, 96, 0, t, t+32,-1,0);
				}
			}
			else
			{
				if (settings.opt_lensflares)
				{
					combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 1, 112, 0, t, t+10, 4, 0);
					combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 0, 512, 1, t, t+10, 3, 0);
				}
				combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 0, 512, 1, t, t+20, 2, 0);
				combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 0, 512, 1, t, t+40, 1, 0);
				cships[c].bong_start = 0;

				if (shiptypes[cships[c].type].race == race_unknown)	// stop space hulk
				{
					cships[c].va = rand()%cships[c].turn+1;
					if (rand()&1)
						cships[c].va = -cships[c].va;

					cships[c].vx = cships[c].vy = 0;
				}

				combat_SoundFX(WAV_EXPLO1, cships[c].x);
				combat_killship(c, t);
			}
		}


	}

	// **** COMBAT AI ****
	for (c = 0; c < MAX_COMBAT_SHIPS; c++)
	if (cships[c].type > -1 && cships[c].active>0 && t > cships[c].aistart)
	{
		if (cships[c].hits > 0 && cships[c].active==2)
		{
			// shield recharge
			if (cships[c].shld_type>-1 && t > cships[c].shld_charge)
			{
				p = (shipsystems[cships[c].shld_type].par[1] * cships[c].syshits[cships[c].sys_shld]) / 10;
				if (p)
				{
					if (cships[c].shld < shipsystems[cships[c].shld_type].par[0])
					{
						cships[c].shld++;
						cships[c].shld_charge = t + 50/p;
					}
				}
			}
			// damage control
			if (t > cships[c].dmgc_time)
			{
				if (cships[c].dmgc_type>-1)
					p = (shipsystems[cships[c].dmgc_type].par[0] * cships[c].syshits[cships[c].sys_dmgc]) / 10;

				else if (shiptypes[cships[c].type].race == race_kawangi)
					p = 10;

				else
					p = 0;
				if (p > 0 && cships[c].hits < hulls[shiptypes[cships[c].type].hull].hits)
				{
					cships[c].hits++;
					cships[c].dmgc_time = t + 50/p;
				}
				
				// repair broken systems
				if (p == 0 && (shiptypes[cships[c].type].race==race_none || shiptypes[cships[c].type].race==race_terran))
					p = 1;
				if (p > 0)
				{
					sys=-1; lsys=-1;
					for (d = 0; d < shiptypes[cships[c].type].num_systems; d++)
					if (cships[c].syshits[d] < 10 && (cships[c].syshits[d]>0 || p==10))
					{	// don't repair if zero (lost) unless kawangi
						if (lsys==-1 || cships[c].syshits[d]<lsys)
						{
							lsys = cships[c].syshits[d];
							sys = d;
						}
					}
					if (sys>-1)
					{
						cships[c].syshits[sys]++;
						if (cships[c].syshits[sys]==10 && c==playership)	// fixed
							Play_SoundFX(WAV_SYSFIXED, get_ik_timer(1));

						cships[c].dmgc_time = t + 50/p;
					}
					combat_updateshipstats(c, t);
				}
			}

			if (cships[c].flee == 0)
			{
//				if (cships[c].own == 1 && cships[c].hits < hulls[shiptypes[cships[c].type].hull].hits)
				if (cships[c].own == 1 && cships[c].frange == 0)	// lost guns, flee!
				{
					cships[c].flee = 1;
					cships[c].tac = tac_flee;
					cships[c].target = -1;
				}
			}

			if (cships[c].target > -1 && cships[cships[c].target].own != cships[c].own)
			{	
				// check if lost target due to cloaking
				// change tactics here (?)
				if (cships[cships[c].target].cloaked==1)
				{	
					tg = cships[c].target;
					cships[c].patx = cships[c].wp_x = cships[tg].x - ((sin1k[cships[c].angle]*cships[c].dist)>>6);
					cships[c].paty = cships[c].wp_y = cships[tg].y - ((cos1k[cships[c].angle]*cships[c].dist)>>6);							
					cships[c].target = -1; 
					cships[c].tac = 2;
				}
				// check if wants to cloak or decloak (enemy only)
				if (c != playership && cships[c].clo_type && cships[c].syshits[cships[c].sys_clo]>=5)
				{
					rm = cships[c].frange;
					if (rm > 0 && t - cships[c].cloaktime > 100)
					{
						d = cships[c].target;
						r = get_distance( (cships[c].x - cships[d].x)>>10, (cships[c].y - cships[d].y)>>10);
						if (r < rm && cships[c].tac==1)
						{
							if (cships[c].cloaked)	// uncloak when at weapon range
							{
								cships[c].cloaked = 0;
								cships[c].cloaktime = t;
//								Play_SoundFX(WAV_CLOAKOUT, get_ik_timer(1));
							}
						}
						else
						{
							if (!cships[c].cloaked)
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
				if (cships[c].own != 0 && t%MAX_COMBAT_SHIPS == c)
					combat_checkalttargets(c, t);
				tg = cships[c].target;
				if (cships[c].tac==0) // waypoint
				{
					wx = cships[tg].x - ((sin1k[cships[c].angle]*cships[c].dist)>>6);
					wy = cships[tg].y - ((cos1k[cships[c].angle]*cships[c].dist)>>6);

					if (cships[c].own != 0 && t > cships[c].wp_time)	// frustration time
						cships[c].tac = 1;
				}
				else if (cships[c].tac==1)	// attack
				{
					wx = cships[tg].x;
					wy = cships[tg].y;
				}
				a = get_direction( (wx - cships[c].x)>>10, (wy - cships[c].y)>>10 );
				r = get_distance( (wx - cships[c].x)>>10, (wy - cships[c].y)>>10 );


				if (r > 80 && cships[c].tac==0 && (shiptypes[cships[c].type].flag & 2)>0)
				{
					if (t - cships[c].teltime > 4*50 || cships[c].teltime == 0)
					{
						cships[c].tel_x = wx - cships[c].x;
						cships[c].tel_y = wy - cships[c].y;
						cships[c].teltime = t;
						combat_SoundFX(WAV_TELEPORT, cships[c].x);
						//cexplo[combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 0, 96, 1, t, t+24, 1)].str = t-8;
						combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 1, 112, 0, t, t+10, 4);
					}
				}



				if ((cships[tg].own&1) == (cships[c].own&1)) // escort
				{
					if (r < 64)
					{
						a = cships[tg].a;
						sx = (((wx-cships[c].x)>>10)*cos1k[a] - ((wy-cships[c].y)>>10)*sin1k[a] ) >> 16;
						sy = (((wy-cships[c].y)>>10)*cos1k[a] + ((wx-cships[c].x)>>10)*sin1k[a] ) >> 16;
						if (sx < -5)  a = (1024 + a - 50) & 1023;
						if (sx > 5)		a = (1024 + a + 50) & 1023;
						sp = get_distance( (cships[tg].vx*50)>>10, (cships[tg].vy*50)>>10);
//						sp = shiptypes[cships[tg].type].speed;
						if (sy < -5)  sp = MAX(0, sp-50);
						if (sy > 5)		sp += 50;
						if (sp == 0)
							a = cships[tg].a;
					}
				}
				else // attack
				{
					if ( (r < 64 && cships[c].tac==0) || (r < 128 && cships[c].tac==1))
					{
						if (cships[c].tac==0) // reached waypoint, start attack run
							cships[c].tac=1;
						else if (cships[c].tac==1)	// close proximity, return to waypoint
						{
							cships[c].tac=0;
							cships[c].wp_time = t + 500 + rand()%500;
							if (cships[c].own != 0)	// enemy or klakar
							{	// get a new angle of attack
								cships[c].angle = (cships[tg].a + 768 + rand()%512)&1023;
							}
							else if (cships[cships[c].target].active>0 && (cships[c].cloaked == 1 || cships[cships[c].target].active==1))	// check for spacehulk sneak-up victory
							{
								if (shiptypes[cships[cships[c].target].type].flag & 256)
								{
									combat_SoundFX(WAV_BOARD, cships[c].x);
									cships[cships[c].target].active = 0;
									cships[cships[c].target].hits = 0;
								}
							}

						}
						//a = get_direction( (cships[tg].x - cships[c].x)>>10, (cships[tg].y - cships[c].y)>>10 );
					}
				}
				if (cships[c].own==2 && cships[c].target==playership)	// klakar escort
					combat_findstuff2do(c, t);

			}
			else
			{
				if (cships[c].tac==2)	// move to waypoint
				{
					a = get_direction ( (cships[c].wp_x - cships[c].x)>>10, (cships[c].wp_y - cships[c].y)>>10);					
					r = get_distance ( (cships[c].wp_x - cships[c].x)>>10, (cships[c].wp_y - cships[c].y)>>10);
					if (shiptypes[cships[c].type].flag & 2)
					{
						if (r > 80 && (t - cships[c].teltime > 4*50 || cships[c].teltime == 0))
						{
							cships[c].tel_x = cships[c].wp_x - cships[c].x;
							cships[c].tel_y = cships[c].wp_y - cships[c].y;
							cships[c].teltime = t;
							combat_SoundFX(WAV_TELEPORT, cships[c].x);
							//cexplo[combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 0, 96, 1, t, t+24, 1)].str = t-8;
							combat_addexplo(cships[c].x, cships[c].y, spr_shockwave, 1, 112, 0, t, t+10, 4);
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
							a = rand()&1023;
							cships[c].wp_x = cships[c].patx + ((sin1k[a]*cships[c].dist)>>6);
							cships[c].wp_y = cships[c].paty + ((cos1k[a]*cships[c].dist)>>6);
						}
					}
					if (cships[c].own != 0)
						combat_findstuff2do(c, t);
				}
				else if (cships[c].tac == tac_flee)
				{
					rm = -1;
					a = -1;
					for (d = 0; d < MAX_COMBAT_SHIPS; d++)
					if (!(cships[d].own&1) && cships[d].hits>0)
					{
						r = get_distance ( (cships[d].x - cships[c].x)>>10, (cships[d].y - cships[c].y)>>10);
						if (rm == -1 || r < rm)
						{ a = d; rm = r; }
					}
					if (a > -1)
					{
						a = get_direction ( (cships[c].x - cships[a].x)>>10, (cships[c].y - cships[a].y)>>10);
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
			if (a > 512) a-=1024;
			p = cships[c].turn;
			cships[c].va = ((a > 0) - (a < 0))*MIN(p,ABS(a));
			sx = (sin1k[cships[c].a] / 50 * sp) >> 6;
			sy = (cos1k[cships[c].a] / 50 * sp) >> 6;	//    sp/50 << 10

			cships[c].vx = sx; 
			cships[c].vy = sy; 

			if ((cships[c].own != 2 || t-klaktime > 100) && (!cships[c].cloaked))
			{
				hull = &hulls[shiptypes[cships[c].type].hull];
				for (p = 0; p < hull->numh; p++)
					if (hull->hardpts[p].type == hdpWeapon)
					if ( t>cships[c].wepfire[p])
					{
						tg = combat_findtarget(&cships[c], p);
						if (tg > -1)
							combat_fire(&cships[c], p, &cships[tg], t);
					}
			}
		}
		else
		{
			if (cships[c].active==2)
			{

				if (cships[c].cloaked)
				{
					cships[c].cloaked = 0;
					cships[c].cloaktime = t;
					combat_SoundFX(WAV_CLOAKOUT, cships[c].x);
				}


				hull = &hulls[shiptypes[cships[c].type].hull];
				if (!(rand()%16) || cships[c].hits < -hull->hits)
				{
					cships[c].hits--;
					combat_SoundFX(WAV_EXPLO1, cships[c].x, 50); 
					combat_addexplo(cships[c].x + ((rand()%hull->size-hull->size/2)<<9), 
													cships[c].y + ((rand()%hull->size-hull->size/2)<<9), 
													spr_explode1, 5, hull->size/2, 0, t, t+32);
					if (cships[c].hits <= -hull->hits)
					{
						combat_killship(c, t);
					}
				}
			}
			else if (cships[c].active==1 && cships[c].hits>1)
			{	// dormant space hulk waiting to activate
				for (p = 0; p < MAX_COMBAT_SHIPS; p++)
				if (cships[p].type>-1 && cships[p].own==0 && cships[p].cloaked==0)
				{
					r = get_distance ( (cships[c].x - cships[p].x)>>10, (cships[c].y - cships[p].y)>>10);
					if (r < 400)
						cships[c].active=2;
				}
			}
		}
	}

	// **** MOVE SHOTS ****
	for (c = 0; c < MAX_COMBAT_BEAMS; c++)
	if (cbeams[c].wep)
	{
		if (t > cbeams[c].dmt)
		{
			if (cbeams[c].dst)
			{
				combat_addexplo(cbeams[c].dst->x, cbeams[c].dst->y, spr_explode1, 5, 32, 0, t, t+32);
				a = shiptonum(cbeams[c].dst);
				combat_damageship(a, 0, cbeams[c].wep->damage, t, cbeams[c].wep);
				cbeams[c].dmt += 500;
				d = ((cbeams[c].wep->flags & wpfShock1)>0)+2*((cbeams[c].wep->flags & wpfShock2)>0);
				if (d==1)
				{
					cexplo[combat_addexplo(cbeams[c].dst->x, cbeams[c].dst->y, spr_shockwave, 0, 96, 1, t, t+24, 1)].str = t-8;
					if (settings.opt_lensflares)
						combat_addexplo(cbeams[c].dst->x, cbeams[c].dst->y, spr_shockwave, 1, 112, 0, t, t+10, 4);
				}
				else if (d==2)
				{
					cexplo[combat_addexplo(cbeams[c].dst->x, cbeams[c].dst->y, spr_shockwave, 0, 96, 1, t, t+32, 3)].str = t-8;
					if (settings.opt_lensflares)
						combat_addexplo(cbeams[c].dst->x, cbeams[c].dst->y, spr_shockwave, 1, 144, 0, t, t+16, 4);
				}
			}
		}
		if (t > cbeams[c].end)
		{
			cbeams[c].wep = NULL;
		}
	}

	for (c = 0; c < MAX_COMBAT_PROJECTILES; c++)
	if (cprojs[c].wep)
	{
		if ( cprojs[c].wep->flags & wpfHoming)
		{
			if (cprojs[c].dst!=NULL)
			{
				a = get_direction ( (cprojs[c].dst->x>>10)-(cprojs[c].x>>10), (cprojs[c].dst->y>>10)-(cprojs[c].y>>10) );
				a = (a + 1024 - cprojs[c].a) & 1023;
				while (a > 512) a-=1024;
				if (a < -8) a=-8;
				if (a > 8) a=8;
				cprojs[c].va = a;

				if (cprojs[c].wep->flags & wpfSplit)
				{
					if ( t > cprojs[c].str + 50)
					{
						r = get_distance ( (cprojs[c].dst->x>>10)-(cprojs[c].x>>10), (cprojs[c].dst->y>>10)-(cprojs[c].y>>10) );
						if ( r < shipweapons[cprojs[c].wep->stage].range && cprojs[c].hits>0) // split
						{
							cprojs[c].end = t;
							cprojs[c].hits = 0;
							if (cprojs[c].wep->item != -1)
							{
								if (shipsystems[itemtypes[cprojs[c].wep->item].index].par[1])
									combat_launchstages(c, shipsystems[itemtypes[cprojs[c].wep->item].index].par[1], t);
								else
									combat_launchstages(c, 5, t);
							}
							else
								combat_launchstages(c, 3, t);
						}
					}
				}

				/*
				if (cprojs[c].dst->ecm_type > -1 && cprojs[c].dst->syshits[cprojs[c].dst->sys_ecm]>0)
				{
					a = shipsystems[cprojs[c].dst->ecm_type].par[0];
					if (rand()%300 < a)
						cprojs[c].dst = NULL;
				}*/
				if (cprojs[c].dst != NULL)
					if (cprojs[c].dst->cloaked)
						cprojs[c].dst = NULL;
			}
			
			cprojs[c].vx = (cprojs[c].vx * 15 + ((sin1k[cprojs[c].a] * cprojs[c].wep->speed / COMBAT_FRAMERATE) >> 6)) >> 4;
			cprojs[c].vy = (cprojs[c].vy * 15 + ((cos1k[cprojs[c].a] * cprojs[c].wep->speed / COMBAT_FRAMERATE) >> 6)) >> 4;

		}
		if (settings.opt_smoketrails)
		if (cprojs[c].wep->flags & wpfStrail)
			if (!((t+c)&3))
			{
				d = cprojs[c].wep->size;
				combat_addexplo(cprojs[c].x-((sin1k[cprojs[c].a]*d)>>7), cprojs[c].y-((cos1k[cprojs[c].a]*d)>>7), spr_weapons, 10, (d*3)>>2, 2, t, t+35, 18, 0);
			}		
		if (cprojs[c].wep->flags & wpfImplode)
		{
			if (!(t&3))
			{
				cexplo[combat_addexplo(cprojs[c].x, cprojs[c].y, spr_shockwave, 0, 40, 1, t, t+32, 2, 0)].str = t-8;
			}
			if (!(t%25))	// shoot electric death at random targets
			{
				p = 0; d = shipweapons[cprojs[c].wep->stage].range;
				for (a = 0; a < MAX_COMBAT_SHIPS; a++)
				if (cships[a].type>-1 && (cships[a].own&1)!=(cprojs[c].src->own&1))
				{
					if (get_distance( (cships[a].x - cprojs[c].x)>>10, (cships[a].y - cprojs[c].y)>>10) < d)
						p++;
				}
				if (p > 0)
				{
					d = rand()%p;
					p = -1; 
					for (a = 0; a < MAX_COMBAT_SHIPS; a++)
					if (cships[a].type>-1 && (cships[a].own&1)!=(cprojs[c].src->own&1))
					{
						if (get_distance( (cships[a].x - cprojs[c].x)>>10, (cships[a].y - cprojs[c].y)>>10) < shipweapons[cprojs[c].wep->stage].range)
						{
							if (!d)
								p = a;
							d--;
						}
					}
					if (p > -1)
					{
						combat_addbeam(&shipweapons[cprojs[c].wep->stage], cprojs[c].src, 0, &cships[p], t, c);
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
		if (cships[p].type>-1 && (cships[p].own&1)!=(cprojs[c].src->own&1))
		{
			a = hulls[shiptypes[cships[p].type].hull].size>>1;
			if (cprojs[c].wep->flags & wpfDisperse)
			{
				a += (2 + ((cprojs[c].wep->size-4) * (t - cprojs[c].str)) / (cprojs[c].end - cprojs[c].str))>>1;
			}
			if (cprojs[c].wep->flags & (wpfImplode | wpfNoclip))
			{
				a = -100;
			}
//				if (t < cprojs[c].end - 2)
//					a = -100;
			if (get_distance( (cships[p].x>>10)-(cprojs[c].x>>10), (cships[p].y>>10)-(cprojs[c].y>>10) ) < a)
			{
				if (cprojs[c].wep->flags & wpfDisperse)
				{
					a = 2 + ((cprojs[c].wep->size-4) * (t - cprojs[c].str)) / (cprojs[c].end - cprojs[c].str);
					if (a < hulls[shiptypes[cships[p].type].hull].size)
					{
						d = 4 - ((4*a) / hulls[shiptypes[cships[p].type].hull].size);
						if (d < 1) d = 1;
					}
					else
						d = 1;	// now gives damage one point at a time - much cooler!
					//if (d > cprojs[c].hits) d = cprojs[c].hits;
					combat_damageship(p, 0, d, t, cprojs[c].wep);
					cprojs[c].hits -= d;
					a = hulls[shiptypes[cships[p].type].hull].size>>1;
					combat_addexplo(cships[p].x + ((rand()%(a*2) - a)<<8), 
													cships[p].y + ((rand()%(a*2) - a)<<8), 
													spr_explode1, 5, 32, 0, t, t+32);
					if (cprojs[c].hits <= 0)
					{	cprojs[c].wep = NULL; break; }
				}
				else
				{
					combat_damageship(p, 0, cprojs[c].wep->damage, t, cprojs[c].wep);
					combat_addexplo(cprojs[c].x, cprojs[c].y, spr_explode1, 5, 32, 0, t, t+32);
					d = ((cprojs[c].wep->flags & wpfShock1)>0)+2*((cprojs[c].wep->flags & wpfShock2)>0);
					if (d==1)
					{
						combat_addexplo(cprojs[c].x, cprojs[c].y, spr_shockwave, 0, 96, 1, t-8, t+24, 1);
						if (settings.opt_lensflares)
							combat_addexplo(cprojs[c].x, cprojs[c].y, spr_shockwave, 1, 112, 0, t, t+10, 4);
					}
					else if (d==2)
					{
						combat_addexplo(cprojs[c].x, cprojs[c].y, spr_shockwave, 0, 96, 1, t-8, t+32, 3);
						if (settings.opt_lensflares)
							combat_addexplo(cprojs[c].x, cprojs[c].y, spr_shockwave, 1, 144, 0, t, t+16, 4);
					}
					if (cprojs[c].wep->flags & wpfNova)
					{
						combat_addexplo(cprojs[c].x, cprojs[c].y, spr_shockwave, 0, 96, 1, t-8, t+16, 1);
						combat_addexplo(cprojs[c].x, cprojs[c].y, spr_shockwave, 0, 256, 1, t-8, t+40, 2);
						if (settings.opt_lensflares)
							combat_addexplo(cprojs[c].x, cprojs[c].y, spr_shockwave, 1, 160, 0, t, t+24, 4);
						if (cships[p].hits <= 0)
							cships[p].hits = 1 - hulls[shiptypes[cships[p].type].hull].hits;
					}
					cprojs[c].wep = NULL;
					break;
				}
			}
		}

		if (t > cprojs[c].end && cprojs[c].wep != NULL)
		{
			if (cprojs[c].wep->flags & wpfSplit)	// end split ("flak")
				if (!(cprojs[c].wep->flags & wpfHoming))
				{
					if (cprojs[c].wep->item != -1)
					{
						if (shipsystems[itemtypes[cprojs[c].wep->item].index].par[1])
							combat_launchstages(c, shipsystems[itemtypes[cprojs[c].wep->item].index].par[1], t);
						else
							combat_launchstages(c, 5, t);
					}
					else
						combat_launchstages(c, 3, t);
				}
			
			cprojs[c].wep = NULL;
		}
	}
}

void combat_checkalttargets(int32 s, int32 t)
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
		r = get_distance( (cships[c].x-cships[s].x)>>10, (cships[c].y-cships[s].y)>>10 );

		// if target is out of range, look for closer enemy ships
		if (r > cships[s].frange)
		{
			b = -1; rm = cships[s].frange;
			for (c = 0; c < MAX_COMBAT_SHIPS; c++)
			if (cships[c].type>-1 && cships[c].hits>0 && 
					(cships[c].own&1)!=(cships[s].own&1) && cships[c].cloaked==0 && cships[c].active==2)
			{
				r = get_distance( (cships[c].x-cships[s].x)>>10, (cships[c].y-cships[s].y)>>10 );
				if (r < rm)
				{
					b = c; rm = r;
				}
			}

			if (b > -1)
			{
				rm = -1;
				for (c = 0; c < shiptypes[cships[s].type].num_systems; c++)
				if (shipsystems[shiptypes[cships[s].type].system[c]].type == sys_weapon && 
						shipsystems[shiptypes[cships[s].type].system[c]].par[0] > -1)
				{
					r = shipweapons[shipsystems[shiptypes[cships[s].type].system[c]].par[0]].range;
					if (r < rm || rm == -1) rm=r;
				}

				cships[s].target = b;
				// shiptypes[cships[s].type].speed > shiptypes[cships[b].type].speed+3 && 
				if (cships[s].own != 2)
					cships[s].dist = rm+64;
				else	// klakar
					cships[s].dist = 0;
				cships[s].angle = get_direction(cships[b].x - cships[s].x, cships[b].y - cships[s].y);
				cships[s].tac = 0;
				cships[s].wp_time = t + 500 + rand()%500;
			}
		}

	
	}

}

void combat_findstuff2do(int32 s, int32 t)
{
	int b, c;
	int r, rm;
	int fo;
	int pla = 0;	// is this player ship?

	if (cships[playership].hits > 0 && cships[s].own == 0)
	{
		if (s < player.num_ships)
			pla = 1;
	}

	if (!pla) // enemy or klakar (or if player dead)
	{
		b = -1; rm = 30000;
		// find closest enemy
		for (c = 0; c < MAX_COMBAT_SHIPS; c++)
		if (cships[c].type>-1 && cships[c].hits>0 && (cships[c].own&1)!=(cships[s].own&1) && cships[c].cloaked==0 && cships[c].active==2)
		{
			r = get_distance( (cships[c].x-cships[s].x)>>10, (cships[c].y-cships[s].y)>>10 );
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
				cships[s].dist = cships[s].frange+64;
			else // klakar
				cships[s].dist = 0;
			// if in front, swoop to the side
			c = get_direction(cships[s].x - cships[b].x, cships[s].y - cships[b].y);
			c = (c + 1024 - cships[b].a)&1023;
			if (c > 512) c-=1024;
			if (abs(c) < 128)	// in front
			{
				cships[s].angle = (cships[b].a + 512 + 256*((c>0)-(c<0)) + rand()%256 - 128)&1023;
			}
			else		// attack directly
			{
				cships[s].angle = get_direction(cships[b].x - cships[s].x, cships[b].y - cships[s].y);
			}
			cships[s].tac = 0;
			if (t>0)
				cships[s].wp_time = t + 500 + rand()%500;
			else
				cships[s].wp_time = t + 1000 + rand()%500;
		}
		else	// couldn't find enemy. Enter search pattern or formation
		{
			if (cships[playership].hits>0 && cships[s].own==0)	// autofighters
			{
				pla = 1;
			}
			if (cships[playership].hits>0 && cships[s].own==2)	// klakar escorts player
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
				cships[s].dist = cships[s].frange+64;
				cships[s].patx = cships[s].wp_x;
				cships[s].paty = cships[s].wp_y;
			}
		}
	}
	if (pla) // friendly
	{
		if (!retreat || (shiptypes[cships[s].type].flag & 2)>0)
		{
			if (s != playership)
			{
				fo = s;
				if (fo >= player.num_ships)
				{
					fo = player.num_ships + (MAX_COMBAT_SHIPS-1-fo);
				}
				cships[s].target = playership;
				cships[s].dist = 128*((fo+1)/2);
				cships[s].angle = (cships[playership].a + 768-512*(fo&1))&1023;
				cships[s].tac = 0;
			}
			else
			{
				cships[s].target = -1;
				cships[s].tac = 2;
				cships[s].wp_x = cships[s].x + sin1k[cships[s].a]*2;
				cships[s].wp_y = cships[s].y + cos1k[cships[s].a]*2;
			}
		}
		else
		{
			b = -1; rm = 30000;
			for (c = 0; c < MAX_COMBAT_SHIPS; c++)
			if (cships[c].own == 1 && cships[c].type>-1 && cships[c].hits>0 && cships[c].cloaked==0 && cships[c].active>0)
			{
				r = get_distance( (cships[c].x-cships[s].x)>>10, (cships[c].y-cships[s].y)>>10 );
				if (r < rm)
				{
					b = c; rm = r;
				}
			}
			if (b > -1)	// escape from closest enemy ship
			{
				c = get_direction ( (cships[b].x-cships[s].x)>>10, (cships[b].y-cships[s].y)>>10 );
			}
			else	// if no enemy found, escape from "camera"
			{
				c = get_direction ( (camera.x-cships[s].x)>>10, (camera.y-cships[s].y)>>10 );
			}

			c = ( c + 512 ) & 1023;

			cships[s].target = -1;
			cships[s].tac = 2;
			cships[s].wp_x = cships[s].x + (sin1k[c]>>6)*30000;
			cships[s].wp_y = cships[s].y + (cos1k[c]>>6)*30000;
		}
	}

}

void combat_checkescapes(int32 t)
{
	int c, s; 
	int r;
	//int rm, h;

	for (s = 0; s < MAX_COMBAT_SHIPS; s++)
		if (cships[s].own == 1 && cships[s].type > -1 && cships[s].flee>0)
		{
			cships[s].flee = 2;
			for (c = 0; c < MAX_COMBAT_SHIPS; c++)
			if (cships[c].type > -1 && cships[c].own != 1 && cships[c].hits > 0)
			{
				r = get_distance( (cships[c].x-cships[s].x)>>10, (cships[c].y-cships[s].y)>>10);
				if (r < (cships[c].frange*3)/2)
					cships[s].flee = 1;
			}			
		}

	if (!retreat)
	{
		for (s = 0; s < MAX_COMBAT_SHIPS; s++)
		if ((cships[s].own & 1) == 0)
			cships[s].escaped = 0;

		return;
	}

	if (t < rett+100)
		return;

	for (s = 0; s < MAX_COMBAT_SHIPS; s++)
	if (cships[s].type > -1)
	{
		if ((cships[s].own&1) == 0)
		{
			cships[s].escaped = 1;
			if (cships[s].cloaked==0)
			{
				for (c = 0; c < MAX_COMBAT_SHIPS; c++)
				if (cships[c].type > -1 && cships[c].own == 1 && cships[c].hits > 0 && cships[c].active == 2)
				{
					r = get_distance( (cships[c].x-cships[s].x)>>10, (cships[c].y-cships[s].y)>>10);
					if (r < (cships[c].frange*3)/2)
						cships[s].escaped = 0;
				}			
			}
		}
	}
}

int32 combat_findship(int32 mx, int32 my)
{
	int32 c;
	int32 r;
	int32 d;
	int32 sz;

	r = -1;

	for (d = 0; d < numships; d++)
	{
		c = sortship[d];
		if (cships[c].hits > 0 && cships[c].type > -1 && (cships[c].cloaked==0 || cships[c].own==0))
		{
			sz = (cships[c].ds_s >> 1)+5;
			if (mx >= cships[c].ds_x-sz && mx <= cships[c].ds_x+sz && 
					my >= cships[c].ds_y-sz && my <= cships[c].ds_y+sz)
				r = c;
		}
	}

	return r;
}

int32 shiptonum(t_ship *s)
{
	int32 c;

	for (c = 0; c < MAX_COMBAT_SHIPS; c++)
	if (cships[c].type > -1)
	{
		if (s == &cships[c])
			return c;
	}

	return -1;
}	

void combat_updateshipstats(int32 s, int32 t)
{
	int x;
	int ty;

	ty = cships[s].type;

	if (ty==-1)
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
			case sys_weapon:
			if (cships[s].syshits[x]>0)
				cships[s].frange = MAX(cships[s].frange, shipweapons[shipsystems[shiptypes[cships[s].type].system[x]].par[0]].range);
			break;

			case sys_thruster:
			cships[s].sys_thru = x;
			break;

			case sys_shield:
			if (cships[s].syshits[x]>0)
			{	cships[s].shld_type = shiptypes[ty].system[x]; cships[s].sys_shld = x; }
			break;

			case sys_damage:
			if (cships[s].syshits[x]>0)
			{	cships[s].dmgc_type = shiptypes[ty].system[x]; cships[s].sys_dmgc = x; }
			break;
		
			case sys_computer:
			if (cships[s].syshits[x]>0)
			{	cships[s].cpu_type = shipsystems[shiptypes[ty].system[x]].par[0]; cships[s].sys_cpu = x; }
			break;
		
			case sys_ecm:
			if (cships[s].syshits[x]>0)
			{	cships[s].ecm_type = shiptypes[ty].system[x]; cships[s].sys_ecm = x; }
			
			case sys_misc:
			if (cships[s].syshits[x]>0)
			{
				// cloaker
				if (shipsystems[shiptypes[ty].system[x]].type == sys_misc && shipsystems[shiptypes[ty].system[x]].par[0] == 1)
					if (cships[s].syshits[x]>=5)
						{	cships[s].clo_type = 1; cships[s].sys_clo = x; }
			}
			break;

			default: ;
		}
	}

	if (cships[s].cloaked > 0 && cships[s].clo_type==0)	// decloak if cloaker destroyed
	{
		cships[s].cloaked=0; cships[s].cloaktime=t;
	}

	if (cships[s].shld > 0 && cships[s].shld_type==-1)	// drop shield if damaged
	{
		cships[s].shld=0;
	}

	if (cships[s].sys_thru > -1)
		cships[s].speed = (shiptypes[cships[s].type].speed * cships[s].syshits[cships[s].sys_thru]) / 10;
	if (cships[s].speed == 0)
	{
		cships[s].speed = 1 + (3 * 32) / hulls[shiptypes[cships[s].type].hull].mass;
	}
	cships[s].turn = 1 + ((shiptypes[cships[s].type].turn-1) * cships[s].syshits[cships[s].sys_thru]) / 10;

}

void reset_ship(int32 s, int32 st, int32 t)
{
	int32 c;

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

void combat_summon_klakar(int32 t)
{
	int32 b, c;
	int32 s, st;

	b = -1;
	for (c = 0; c < STARMAP_MAX_FLEETS; c++)
	{
		if (sm_fleets[c].race == race_klakar)
			b = c;
	}
	if (b == -1)
	{
		Play_SoundFX(WAV_DESELECT, get_ik_timer(1)); 	
		return;
	}
	Play_SoundFX(WAV_DOT, 0);
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
		if (shiptypes[c].race == race_klakar)
		{
			st = c;
		}

	// set up klakar ship

	// find location
	b = rand()%1024;

	// basic resets
	reset_ship(s, st, t);

	cships[s].x = camera.x - ((sin1k[b]*1000)>>6);
	cships[s].y = camera.y - ((cos1k[b]*1000)>>6);
	cships[s].a = b;
	cships[s].own = 2;

	combat_findstuff2do(s, 0);

//	Play_SoundFX(WAV_HYPERDRIVE, get_ik_timer(1));
	klaktime = t;
}

void combat_launch_fighter(int32 s, int32 t)
{
	int32 c, b;
	int32 st;
	int32 x, y;

	st = racefleets[races[shiptypes[cships[s].type].race].fleet].stype[0];

	/*
	b = 0;
	for (c = 0; c < MAX_COMBAT_SHIPS; c++)
		if (cships[c].type == st && cships[c].hits > 0)
			b++;

	if (b >= 3)	// don't launch more than 3 at once
		return;
	Play_SoundFX(WAV_FIGHTERLAUNCH);
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
		if (hulls[shiptypes[cships[s].type].hull].hardpts[c].type == hdpFighter)
			combat_gethardpoint(&cships[s], c, &x, &y);

	reset_ship(b, st, t);

	cships[b].a = cships[s].a;
	cships[b].x = x;
	cships[b].y = y;
	cships[b].own = cships[s].own;

	combat_findstuff2do(b, t);
}

int32 combat_use_gong(int32 t)
{
	int32 c,b;
	int32 mh;

	b = -1; mh = 0;
	for (c = 0; c < MAX_COMBAT_SHIPS; c++)
	{
		if (cships[c].type>-1 && cships[c].hits+cships[c].shld>mh && (cships[c].own&1))
		{
			mh = cships[c].hits+cships[c].shld;
			b = c;
		}
	}

	if (b==-1)
		return 1;

	Play_SoundFX(WAV_GONG, t);


	// mark ship for extreme death
	cships[b].bong_start = t;
	cships[b].bong_end = t + 200;

	return 0;
}

void combat_help_screen()
{
	int32 end;
	int32 c, mc;
	int32 t=0;
	int32 x, y;
	t_ik_image *bg;

	bg = ik_load_pcx("graphics/helpc.pcx", NULL);

	prep_screen();
	ik_copybox(bg, screen, 0, 0, 640, 480, 0,0);

#ifdef COMBAT_BUILD_HELP
	x = 150; y = 20;
	interface_thinborder(screen, x, y+4, x+120, y+228, COMBAT_INTERFACE_COLOR, 2+COMBAT_INTERFACE_COLOR*16);

	ik_print(screen, font_6x8, x+4, y+=8, COMBAT_INTERFACE_COLOR, "ALLIED SHIPS");
	ik_copybox(screen, screen, 16, 24, 48, 40, x+4, (y+=8)-1);
	ik_print(screen, font_4x8, x+4, (y+=16), 0, "Click to select ship.");

	y+=6;
	ik_print(screen, font_6x8, x+4, y+=8, COMBAT_INTERFACE_COLOR, "HULL DAMAGE");
	y+=8*interface_textbox(screen, font_4x8, x+4, y+=8, 112, 64, 0, "The red bar on the left displays the hull integrity. When it reaches the bottom, the ship will be destroyed.") - 8; 

	y+=6;
	ik_print(screen, font_6x8, x+4, y+=8, COMBAT_INTERFACE_COLOR, "SHIELD STATUS");
	y+=8*interface_textbox(screen, font_4x8, x+4, y+=8, 112, 64, 0, "If the currently selected ship is equipped with a shield, the blue bar on the right displays its strength.") - 8; 

	y+=6;
	ik_print(screen, font_6x8, x+4, y+=8, COMBAT_INTERFACE_COLOR, "SHIP SYSTEMS");
	ik_dsprite(screen, x-2, (y+=8)-5, spr_IFsystem->spr[1], 2+(1<<8));
	ik_print(screen, font_4x8, x+12, y, 1, "Weapons");
	ik_dsprite(screen, x-2, (y+=8)-5, spr_IFsystem->spr[5], 2+(3<<8));
	ik_print(screen, font_4x8, x+12, y, 3, "Star Drive");
	ik_dsprite(screen, x-2, (y+=8)-5, spr_IFsystem->spr[9], 2+(2<<8));
	ik_print(screen, font_4x8, x+12, y, 2, "Combat Thrusters");

	y+=6;
	ik_print(screen, font_6x8, x+4, y+=8, COMBAT_INTERFACE_COLOR, "SYSTEM DAMAGE");
	y+=8*interface_textbox(screen, font_4x8, x+4, y+=8, 112, 64, 0, "Damaged systems are shown in different colors. When a system is lost it ceases to function and must be repaired after the battle.") - 8; 

	x = 80; y = 308;
	interface_thinborder(screen, x, y+4, x+120, y+60, COMBAT_INTERFACE_COLOR, 2+COMBAT_INTERFACE_COLOR*16);
	ik_print(screen, font_6x8, x+4, y+=8, COMBAT_INTERFACE_COLOR, "MISC COMBAT ACTIONS");
	y+=8*interface_textbox(screen, font_4x8, x+4, y+=8, 112, 64, 0, "By default, the only button available is Retreat. As you aqcuire special items or devices you may be able to use them during combat.") - 8; 

	x = 288; y = 374;
	interface_thinborder(screen, x, y+4, x+120, y+60, COMBAT_INTERFACE_COLOR, 2+COMBAT_INTERFACE_COLOR*16);
	ik_print(screen, font_6x8, x+4, y+=8, COMBAT_INTERFACE_COLOR, "CURRENT TARGET");
	y+=8*interface_textbox(screen, font_4x8, x+4, y+=8, 112, 64, 0, "This window shows the ship you've chosen to attack (or escort in case of friendly ships) along with its hull and shield status.") - 8; 

	x = 284; y = 52;
	interface_thinborder(screen, x, y+4, x+136, y+84, COMBAT_INTERFACE_COLOR, 2+COMBAT_INTERFACE_COLOR*16);
	ik_print(screen, font_6x8, x+4, y+=8, COMBAT_INTERFACE_COLOR, "SELECTING SHIPS");
	y+=8*interface_textbox(screen, font_4x8, x+4, y+=8, 128, 64, 0, "In addition to using the ship selection icons (see ALLIED SHIPS, top left) you can select any friendly ship by clicking it with the left mouse button. The currently selected ship is marked by a translucent green reticle.") - 8; 

	x = 428; y = 52;
	interface_thinborder(screen, x, y+4, x+112, y+224, COMBAT_INTERFACE_COLOR, 2+COMBAT_INTERFACE_COLOR*16);
	ik_print(screen, font_6x8, x+4, y+=8, COMBAT_INTERFACE_COLOR, "GIVING ORDERS");
	y+=6;
	ik_print(screen, font_6x8, x+4, y+=8, 1, "ATTACK ENEMY");
	y+=8*interface_textbox(screen, font_4x8, x+4, y+=8, 104, 64, 0, "Left-click on any enemy spacecraft to order the selected ship to attack it. Hold the button and drag to add a waypoint, for example to attack from the side. The path of attack is shown in red.") - 8; 
	y+=6;
	ik_print(screen, font_6x8, x+4, y+=8, 3, "STAY IN FORMATION");
	y+=8*interface_textbox(screen, font_4x8, x+4, y+=8, 104, 64, 0, "Right-click anywhere near your ship to order the selected ally to follow you, staying on that side of your ship. The course to its escort position is shown in yellow.") - 8; 
	y+=6;
	ik_print(screen, font_6x8, x+4, y+=8, 4, "MOVE TO WAYPOINT");
	y+=8*interface_textbox(screen, font_4x8, x+4, y+=8, 104, 64, 0, "Left-click at empty space to move the selected ship to that location. The course to the waypoint is shown in green.") - 8; 

	x = 428; y = 284;
	interface_thinborder(screen, x, y+4, x+112, y+108, COMBAT_INTERFACE_COLOR, 2+COMBAT_INTERFACE_COLOR*16);
	ik_print(screen, font_6x8, x+4, y+=8, COMBAT_INTERFACE_COLOR, "PAUSE / SPEED UP");
	y+=8*interface_textbox(screen, font_4x8, x+4, y+=8, 104, 64, 0, "Click these symbols to change the speed of the game. You can give orders to your ships even when paused. Click on the single arrow head to return to normal speed.") - 8; 
	y+=6;
	y+=8*interface_textbox(screen, font_4x8, x+4, y+=8, 104, 64, 0, "You can also press the space bar to pause and unpause.") - 8; 
#endif

	ik_blit();

	update_palette();

	end = 0;
	x = key_pressed(key_f[0]); y = 0;
	while (!end && !must_quit)
	{
		ik_eventhandler();
		c = ik_inkey();
		mc = ik_mclick();
		x = key_pressed(key_f[0]); 
		if (!x)
		{
			if (!y)
				y = 1;
			else if (y==2)
				end = 1;
		}
		else if (y)
			y = 2;

		if (mc==1 || c>0)
			end = 1;

		c = t; t = get_ik_timer(2);
		if (t != c)
		{ prep_screen(); ik_blit(); }
	}

	if (must_quit)
		must_quit = 0;
}

void combat_SoundFX(int id, int srcx, int volume, int rate)
{
	int pan;

	pan = (((srcx - camera.x)>>8) * camera.z)>>11;

	if (pan < -10000)
		pan = -10000;
	if (pan > 10000)
		pan = 10000;

	Play_SoundFX(id, 0, volume, rate, pan);
}
// ----------------
//     INCLUDES
// ----------------

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <math.h>

#include "typedefs.h"
#include "iface_globals.h"
#include "is_fileio.h"
#include "gfx.h"
#include "snd.h"
#include "interface.h"
#include "starmap.h"
#include "textstr.h"

#include "combat.h"

// ----------------
// GLOBAL FUNCTIONS
// ----------------

void combat_autocamera(int32 t)
{
	int32 minx, maxx, miny, maxy;
	int32 x,y,z;
	int32 s;
	int32 c;

	minx=(30000)<<10; maxx=(-30000)<<10;
	miny=(30000)<<10; maxy=(-30000)<<10;

	for (c = 0; c < MAX_COMBAT_SHIPS; c++)
	if (cships[c].type>-1 && cships[c].flee < 2)
	{
		s = hulls[shiptypes[cships[c].type].hull].size << 9;
		minx = MIN(minx, cships[c].x - s);
		maxx = MAX(maxx, cships[c].x + s);
		miny = MIN(miny, cships[c].y - s);
		maxy = MAX(maxy, cships[c].y + s);
	}
	for (c = 0; c < MAX_COMBAT_EXPLOS; c++)
	if (cexplo[c].spr)
	{
		if (cexplo[c].cam)
		{
			s = cexplo[c].size << 9;
			minx = MIN(minx, cexplo[c].x - s);
			maxx = MAX(maxx, cexplo[c].x + s);
			miny = MIN(miny, cexplo[c].y - s);
			maxy = MAX(maxy, cexplo[c].y + s);
		}
	}

	if (maxx>minx && maxy>miny)
	{
		x = (minx+maxx)>>1;
		y = (miny+maxy)>>1;
		c = MAX(maxx-minx, maxy-miny)>>10;
		z = (256<<12)/c;

		camera.x = (camera.x*15 + x)>>4;
		camera.y = (camera.y*15 + y)>>4;
		camera.z = (camera.z*15 + z)>>4;
	}
}

void combat_displayships()
{
	int32 s, t;
	int32 bx, by, h;
	int32 y, sy;
	int32 ty;
	int32 hp;
	int32 l;
	int32 klak;
	int32 f;
	char top[32];
	t_hull *hull;

	t = get_ik_timer(1);

	// check for klakar button

	klak = klakavail;
	for (s = 0; s < MAX_COMBAT_SHIPS; s++)
	{
		if (shiptypes[cships[s].type].race == 2)
			klak = 2;
	}
	if (cships[playership].hits<=0)
		klak = 0;

	// player ship selection

	bx = 0; by = 0; h = 288;

	s = camera.ship_sel;
	if (s==-1) 
	{	select_ship(0, t); s = 0; }

	ty = cships[s].type;
	if (ty == -1)
	{ select_ship(0, t); s = 0; ty = player.ships[0]; }

	if (s == 0 && cships[s].type == -1)
	{	camera.ship_sel = -1; camera.ship_trg = -1; }

	if (ty>-1)
		sprintf(top, shiptypes[ty].name);
	else
		sprintf(top, player.shipname);
	interface_drawborder(screen,
											 bx, by, bx+160, by+h,
											 1, COMBAT_INTERFACE_COLOR, top); // shipname
	if (ty>-1)
	{
		hull = &hulls[shiptypes[ty].hull];
		ik_dsprite(screen, bx+16, by+40, hulls[shiptypes[ty].hull].silu, 2+(COMBAT_INTERFACE_COLOR<<8));
		hp = MAX(0,cships[s].hits) * 100 / hulls[shiptypes[ty].hull].hits;
		ik_drawmeter(screen, bx+12, by+40, bx+15, by+166, 0, hp, COMBAT_INTERFACE_COLOR, 28);
		if (cships[s].shld_type>-1)
		{
			hp = (cships[s].shld * 100) / shipsystems[cships[s].shld_type].par[0];
			ik_drawmeter(screen, bx+144, by+40, bx+147, by+166, 0, hp, COMBAT_INTERFACE_COLOR, 92);
		}
		for (hp = 0; hp < hull->numh; hp++)
		{
			l = 0;
			if (hull->hardpts[hp].type == hdpWeapon && shipsystems[shiptypes[ty].system[hp]].item>-1)
				l=(cships[s].syshits[hp]>0); 
			if (hull->hardpts[hp].type == hdpEngine && shiptypes[ty].engine>-1)
				l=3*(cships[s].syshits[shiptypes[ty].sys_eng]>0); 
			if (hull->hardpts[hp].type == hdpThruster && shiptypes[ty].thrust>-1)
				l=2*(cships[s].syshits[shiptypes[ty].sys_thru]>0); 

			ik_dsprite(screen, bx + 8 + hull->hardpts[hp].x*2, by + 32 + hull->hardpts[hp].y*2, 
									spr_IFsystem->spr[hull->hardpts[hp].type * 4 + 1], 2+(l<<8));
		}
	
		// show system damage
		ik_print(screen, font_4x8, bx+16, by + 176, COMBAT_INTERFACE_COLOR, textstring[STR_COMBAT_STATUS]);
		y = 0;
		for (sy = 0; sy < shiptypes[ty].num_systems; sy++)
		{
			if (shipsystems[shiptypes[ty].system[sy]].item>-1)
			{
				if (cships[s].syshits[sy]==10)
					l = 4;
				else if (cships[s].syshits[sy]>=5)
					l = 3;
				else if (cships[s].syshits[sy]>0)
					l = 1;
				else
					l = 0;
				ik_print(screen, font_4x8, bx+16, by+y*8+184, l, shipsystems[shiptypes[ty].system[sy]].name);
				y++;
			}
		}
		ik_print(screen, font_4x8, bx+16, by+264, COMBAT_INTERFACE_COLOR, textstring[STR_COMBAT_DMGKEY]);
		ik_print(screen, font_4x8, bx+16, by+272, 0, textstring[STR_COMBAT_DMG1]);
		ik_print(screen, font_4x8, bx+48, by+272, 1, textstring[STR_COMBAT_DMG2]);
		ik_print(screen, font_4x8, bx+80, by+272, 3, textstring[STR_COMBAT_DMG3]);
		ik_print(screen, font_4x8, bx+112, by+272, 4, textstring[STR_COMBAT_DMG4]);

	}

	// small ships
	for (y = 0; y < player.num_ships; y++)
	{
		l = 0;
		if (cships[y].hits <= 0)
			l = 1+(24<<8);
		ik_drsprite(screen, bx+y*16+24, by+32, 0, 16, hulls[shiptypes[player.ships[y]].hull].sprite, l);
		if (y == camera.ship_sel)
		{
			l = (get_ik_timer(1)&31)>23;
			ik_drsprite(screen, bx+y*16+24, by+32, 0, 16+l*2, spr_IFtarget->spr[8], 5+((8+l*7)<<8));
		}
	}

	// cloak button

	if (cships[playership].clo_type>0 && cships[playership].hits>0)
	{
		if (!cships[playership].cloaked)
			interface_drawbutton(screen, bx, 288, 80, COMBAT_INTERFACE_COLOR, textstring[STR_COMBAT_CLOAK]);
		else
			interface_drawbutton(screen, bx, 288, 80, COMBAT_INTERFACE_COLOR, textstring[STR_COMBAT_UNCLOAK]);
	}
	else
		interface_drawbutton(screen, bx, 288, 80, 0, "");

	// gong button

	if (gongavail == 2)	// available
		interface_drawbutton(screen, bx+80, 288, 80, COMBAT_INTERFACE_COLOR, textstring[STR_COMBAT_GONG]);
	else if (gongavail == 1)	// used
		interface_drawbutton(screen, bx+80, 288, 80, 1, textstring[STR_COMBAT_GONG]);
	else	// none
		interface_drawbutton(screen, bx+80, 288, 80, 0, "");

	// retreat button

	f=0;
	for (s=0;s<MAX_COMBAT_SHIPS;s++)
	if (cships[s].hits>0 && cships[s].type>-1)
	{
		if ((cships[s].own&1)==0)
			f |= 1;
		else 
			f |= 2;
	}
	if (cships[playership].hits > 0 && f > 1)
	{
		if (!retreat)
			interface_drawbutton(screen, bx, 304, 80, COMBAT_INTERFACE_COLOR, textstring[STR_COMBAT_RETREAT]);
		else
			interface_drawbutton(screen, bx, 304, 80, 1, textstring[STR_COMBAT_RETREAT]);
	}
	else
		interface_drawbutton(screen, bx, 304, 80, 0, "");

	// klakar button

	switch (klak)
	{
		case 0:	// no klakar available
			interface_drawbutton(screen, bx+80, 304, 80, 0, "");
		break;
		case 1: // klakar available
			interface_drawbutton(screen, bx+80, 304, 80, COMBAT_INTERFACE_COLOR, textstring[STR_COMBAT_KLAKAR]);
		break;
		case 2: // already summoned
			interface_drawbutton(screen, bx+80, 304, 80, 1, textstring[STR_COMBAT_KLAKAR]);
		break;
		default: ;
	}


	// target ship

	bx = 0; by = 320; h = 160;

	s = camera.ship_trg;
	if (s==-1) 
		ty = -1;
	else
		ty = cships[s].type;

	if (ty>-1)
		sprintf(top, shiptypes[ty].name);
	else
		sprintf(top, textstring[STR_COMBAT_NOTARGET]);
	interface_drawborder(screen,
											 bx, by, bx+160, by+h,
											 1, COMBAT_INTERFACE_COLOR, top); // shipname
	if (ty>-1)
	{
		hull = &hulls[shiptypes[ty].hull];
		ik_dsprite(screen, bx+16, by+24, hulls[shiptypes[ty].hull].silu, 2+(COMBAT_INTERFACE_COLOR<<8));
		if (cships[s].active > 1 || shiptypes[ty].race != race_unknown)
		{
			hp = MAX(0,cships[s].hits) * 100 / hulls[shiptypes[ty].hull].hits;
			if (cships[s].active == 1 && cships[s].hits == 1)
					hp = 100;

			ik_drawmeter(screen, bx+12, by+24, bx+15, by+152, 0, hp, COMBAT_INTERFACE_COLOR, 28);
			if (cships[s].shld_type>-1)
			{
				hp = (cships[s].shld * 100) / shipsystems[cships[s].shld_type].par[0];
				ik_drawmeter(screen, bx+144, by+24, bx+147, by+152, 0, hp, COMBAT_INTERFACE_COLOR, 92);
			}
			for (hp = 0; hp < hull->numh; hp++)
			{
				l = 0;
				if (hull->hardpts[hp].type == hdpWeapon)
					l=(cships[s].syshits[hp]>0); 
				if (hull->hardpts[hp].type == hdpEngine)
					l=3*(cships[s].syshits[shiptypes[ty].sys_eng]>0); 
				if (hull->hardpts[hp].type == hdpThruster)
					l=2*(cships[s].syshits[shiptypes[ty].sys_thru]>0); 
				ik_dsprite(screen, bx + 8 + hull->hardpts[hp].x*2, by + 16 + hull->hardpts[hp].y*2, 
										spr_IFsystem->spr[hull->hardpts[hp].type * 4 + 1], 2+(l<<8));
			}
		}
	}


}

void combat_display(int32 t)
{
	int32 c;
	int32 s;
	int32 p;
	int32 tx, ty;
	int32 sx, sy;
	int32 cx, cy;
	int32 x, y;
	int32 a;
	int32 l;
	int32 sz;
	int32 fr;
	int32 bab;
	t_hull *hull;
	uint8	*draw, *src;

	combat_autocamera(t);

	bab = 0;
	for (c = 0; c < player.num_items; c++)
		if (itemtypes[player.items[c]].flag & device_torc)
			bab = 1;

	ik_setclip(168,8,632,476);
	cy = 244;
	cx = 160+240;

	// nebula background

	if (nebula)
	{
		
		s = (4096<<12) / (camera.z+1) + (1<<13);
		ty = sy = ((-camera.y<<1)-232*s)&0xffffff;
		sx = ((camera.x<<1)-232*s)&0xffffff;
		for (y = 8; y < 476; y++)
		{
			src=combatbg2->data+(combatbg2->pitch*(ty>>16));
//			src=combatbg2->data+(combatbg2->pitch*(ty>>16));
			ty=(ty+s)&0xffffff;
			tx=sx;
			draw = ik_image_pointer(screen, 168, y);
			for (x = 168; x < 632; x++)
			{
				c = src[(tx>>16)];
				tx=(tx+s)&0xffffff;
				*draw++=c;
			}
		}
		//ik_copybox(combatbg2, screen, 0, 0, 256, 256, cx-232, cy-232);
	}
	else
	{
		ik_copybox(combatbg1, screen, 8, 8, 472, 472, cx-232, cy-232);
	}

	// grid
//x = camera.x + ((((ik_mouse_x - 400)<<12)/camera.z)<<10);
	c = 3 + 63*simulated;

	sy = (camera.y>>10) + ((-232<<12)/camera.z);
	sy = (sy/250)*250 - 250;
	ty = (camera.y>>10) + ((232<<12)/camera.z) + 250;
	for (; sy < ty; sy += 250)
	{
		y = cy - (((sy - (camera.y>>10)) * camera.z) >> 12);
		if (y > cy - 232 && y < cy + 232)
		{
			draw = ik_image_pointer(screen, cx - 232, y);
			for (x = 0; x < 464; x++)
			{
				*draw=gfx_addbuffer[(*draw) + (c<<8)];
				draw++;
			}
		}
	}
	sx = (camera.x>>10) + ((-232<<12)/camera.z);
	sx = (sx/250)*250 - 250;
	tx = (camera.x>>10) + ((232<<12)/camera.z) + 250;
	for (; sx < tx; sx += 250)
	{
		x = cx + (((sx - (camera.x>>10)) * camera.z) >> 12);
		if (x > cx - 232 && x < cx + 232)
		{
			draw = ik_image_pointer(screen, x, cy - 232);
			for (y = 0; y < 464; y++)
			{
				*draw=gfx_addbuffer[(*draw) + (c<<8)];
				draw+=screen->pitch;
			}
		}
	}

	// display list insertion
	numships = 0;
	for (c = 0; c < MAX_COMBAT_SHIPS; c++)
	if ((cships[c].cloaked==0 || (t-cships[c].cloaktime<50 && cships[c].cloaktime>0)) || (cships[c].own==0 || bab))
	if (cships[c].type > -1)
	{
		sortship[numships] = c;
		numships++;
	}
	// sort display list
	for (c = 0; c < numships; c++)
	{
		s = c;
		while (s > 0 && hulls[shiptypes[cships[sortship[s]].type].hull].size > hulls[shiptypes[cships[sortship[s-1]].type].hull].size)
		{
			p = sortship[s-1];
			sortship[s-1] = sortship[s];
			sortship[s] = p;
			s--;
		}
	}

	for (c = 0; c < numships; c++)
	{
		s = sortship[c];
		hull = &hulls[shiptypes[cships[s].type].hull];
		cships[s].ds_x = cx + ((((cships[s].x - camera.x)>>8) * camera.z) >> 14);
		cships[s].ds_y = cy - ((((cships[s].y - camera.y)>>8) * camera.z) >> 14);
		cships[s].ds_s = 	(hulls[shiptypes[cships[s].type].hull].size * camera.z) >> 12; 
		if (cships[s].own != 2 || t > klaktime+75)	
		{
			if (simulated)
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

			if (cships[s].cloaked)
			{
				if (t - cships[s].cloaktime < 50 && cships[s].cloaktime > 0)
					l = 15 - (t-cships[s].cloaktime)*11/50;
				else
					l = 4 + 4*(cships[s].own&1);
			}
			else if (cships[s].clo_type && cships[s].cloaktime > 0)	// has uncloaked
			{
				if (t - cships[s].cloaktime < 50 && cships[s].cloaktime > 0)
					l = 4 + (t-cships[s].cloaktime)*11/50;
				else
					l = 15;
			}

			if (cships[s].teltime>0 && t < cships[s].teltime+32)	// zorg fold effect
			{
				p = (t - cships[s].teltime);

				x = cx + ((((cships[s].x + cships[s].tel_x - camera.x)>>8) * camera.z) >> 14);
				y = cy - ((((cships[s].y + cships[s].tel_y - camera.y)>>8) * camera.z) >> 14);

				if (p < 16)
					l = p;
				else
					l = 31-p;

				ik_dspriteline(screen, 
											cships[s].ds_x,
											cships[s].ds_y,
											x,
											y,
											(20 * camera.z) >> 12,
											(-t*13)&31, 18, 
											spr_weapons->spr[1+(rand()&1)], 5 + (l << 8));

				ik_drsprite(screen, 
										(cships[s].ds_x*(32-p)+x*p)>>5,
										(cships[s].ds_y*(32-p)+y*p)>>5,
										rand()&1023,
										(64 * camera.z) >> 12,
										spr_shockwave->spr[4], 4);
				if (rand()&1)
				ik_drsprite(screen, 
										(cships[s].ds_x*(32-p)+x*p)>>5,
										(cships[s].ds_y*(32-p)+y*p)>>5,
										rand()&1023,
										(64 * camera.z) >> 12,
										spr_shockwave->spr[4], 4);

				if (p < 16)
					l = MAX(0, 15-p);
				else
				{
					l = p-16;
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
										hulls[shiptypes[cships[s].type].hull].sprite, 5+(l<<8));
		}
		if (cships[s].own == 2)
		{
			if (t - klaktime < 75)
			{
				l = (t-klaktime)*15/75;
				ik_drsprite(screen, 
										cships[s].ds_x,
										cships[s].ds_y,
										cships[s].a,
										((t-klaktime)*camera.z)>>12, 
										spr_shockwave->spr[4], 5+(l<<8));
			}
			else if (t - klaktime < 100)
			{
				l = ((klaktime+100-t)*15) / 25;
				ik_drsprite(screen, 
										cships[s].ds_x,
										cships[s].ds_y,
										cships[s].a,
										(256*camera.z)>>12, 
										spr_shockwave->spr[4], 5+(l<<8));
			}
		}
		if (cships[s].shld_type > -1 && cships[s].shld>0 && t < cships[s].shld_time+32)
		{
			p = shipsystems[cships[s].shld_type].par[0];
			l = 15; //(cships[s].shld * 15) / p;
			l = (l * (cships[s].shld_time+32-t))>>5;
			p = (p-1)/10; if (p>4) p=4;
			ik_drsprite(screen, 
									cships[s].ds_x,
									cships[s].ds_y,
									rand()%1024,
									cships[s].ds_s, 
									spr_shield->spr[p], 5+(l<<8));
		}

		// draw markers so you can see them from far
		// triangles
		if (cships[s].own==0)
			p = 72;
		else if (cships[s].own==1)
			p = 24;
		else
			p = 56;
		if (cships[s].ds_s > 10)
			p -= (cships[s].ds_s-10);
		if (cships[s].ds_s < 18 && cships[s].escaped==0)
		{
			sz = cships[s].ds_s/2+2;
			sx = cships[s].ds_x;
			sy = cships[s].ds_y;
			tx = sin1k[cships[s].a];
			ty = cos1k[cships[s].a];
			ik_drawline(screen, 
									sx - ((tx*sz*2+ty*sz*3)>>17), sy + ((ty*sz*2-tx*sz*3)>>17),
									sx + ((tx*sz*4)>>17), sy - ((ty*sz*4)>>17), 
									p, 0, 255, 1);
			ik_drawline(screen, 
									sx - ((tx*sz*2-ty*sz*3)>>17), sy + ((ty*sz*2+tx*sz*3)>>17),
									sx + ((tx*sz*4)>>17), sy - ((ty*sz*4)>>17), 
									p, 0, 255, 1);
			ik_drawline(screen, 
									sx - ((tx*sz*2+ty*sz*3)>>17), sy + ((ty*sz*2-tx*sz*3)>>17),
									sx - ((tx*sz*2-ty*sz*3)>>17), sy + ((ty*sz*2+tx*sz*3)>>17),
									p, 0, 255, 1);
		}
//		ik_print(screen, font_6x8, cships[s].ds_x, cships[s].ds_y, 0, "%s", racename[shiptypes[cships[s].type].race]);
	}

	for (c = 0; c < MAX_COMBAT_SHIPS; c++)
		if (cships[c].type > -1 && cships[c].own == 1 && cships[c].flee == 2)
		{
			if (cships[c].ds_x+cships[c].ds_s < cx-240 || 
					cships[c].ds_x-cships[c].ds_s > cx+240 ||
					cships[c].ds_y+cships[c].ds_s < cy-240 || 
					cships[c].ds_y-cships[c].ds_s > cy+240 ||
					(cships[c].cloaked && t > cships[c].cloaktime+50))
			{
				combat_killship(c, t, 1);
			}
		}

	for (c = 0; c < MAX_COMBAT_BEAMS; c++)
	if (cbeams[c].wep)
	{
		if (cbeams[c].stg > -1)	// staged from projectile
		{
			if (!cprojs[cbeams[c].stg].wep)
			{
				cbeams[c].wep = NULL;
				continue;
			}
			sx = cprojs[cbeams[c].stg].x; 
			sy = cprojs[cbeams[c].stg].y;
		}
		else
		{
			combat_gethardpoint(cbeams[c].src, cbeams[c].stp, &sx, &sy);
		}
		if (cbeams[c].dst)
		{
			combat_gethardpoint(cbeams[c].dst, cbeams[c].dsp, &tx, &ty);
			if (cbeams[c].wep->flags & wpfWiggle)
			{
				l = (t-cbeams[c].str)*(cbeams[c].stp*5+30) + (cbeams[c].stp*160);
				tx = tx + ((sin1k[l&1023]*hulls[shiptypes[cbeams[c].dst->type].hull].size)>>8);
				ty = ty + ((cos1k[l&1023]*hulls[shiptypes[cbeams[c].dst->type].hull].size)>>8);
			}
		}
		else
		{
			tx = sx + ((sin1k[cbeams[c].ang] * cbeams[c].len)>>6);
			ty = sy + ((cos1k[cbeams[c].ang] * cbeams[c].len)>>6);
		}

		if (t+t < cbeams[c].str + cbeams[c].end)
		{
			//l = ((t - cbeams[c].str)*24) / (cbeams[c].end - cbeams[c].str) + 3;
			l = 15;
		}
		else
		{
			l = ((cbeams[c].end - t)*24) / (cbeams[c].end - cbeams[c].str) + 3;
		}
		if (l < 2) l = 2;
		if (l > 15) l = 15;
		ik_dspriteline(screen, 
									cx + ((((sx - camera.x)>>8) * camera.z) >> 14),
									cy - ((((sy - camera.y)>>8) * camera.z) >> 14),
									cx + ((((tx - camera.x)>>8) * camera.z) >> 14),
									cy - ((((ty - camera.y)>>8) * camera.z) >> 14),
									(cbeams[c].wep->size * camera.z) >> 12,
									(-t*13)&31, 18, 
									cbeams[c].wep->sprite, 5 + (l << 8));
	}

	for (c = 0; c < MAX_COMBAT_PROJECTILES; c++)
	if (cprojs[c].wep)
	{
		l = 15;
		p = cprojs[c].wep->flags;
		if (p & wpfDisperse)
		{	
			sz = 4 + ((cprojs[c].wep->size-4) * (t - cprojs[c].str)) / (cprojs[c].end - cprojs[c].str); 
			l = 15 - (14 * (t - cprojs[c].str)) / (cprojs[c].end - cprojs[c].str); 
			if (cprojs[c].hits < cprojs[c].wep->damage)
			{	// fade as its hits go away
				l = 1 + ((l-1)*cprojs[c].hits)/cprojs[c].wep->damage;
			}
		}
		else if (p & wpfImplode)
		{	
			if (t - cprojs[c].str < 32)
				sz = (cprojs[c].wep->size * (t-cprojs[c].str)) >> 5;
			else
				sz = cprojs[c].wep->size;
			if (sz < 1) sz = 1;
		}
		else
		{	sz = cprojs[c].wep->size; }


		if (p & wpfTrans)
			a = 5 + (l << 8); 
		else
			a = 0;

		ik_drsprite(screen,
								cx + ((((cprojs[c].x - camera.x)>>8) * camera.z) >> 14),
								cy - ((((cprojs[c].y - camera.y)>>8) * camera.z) >> 14),
								cprojs[c].a,
								(sz * camera.z) >> 12,
								cprojs[c].wep->sprite, a);
		if (p & wpfImplode)
			ik_drsprite(screen,
									cx + ((((cprojs[c].x - camera.x)>>8) * camera.z) >> 14),
									cy - ((((cprojs[c].y - camera.y)>>8) * camera.z) >> 14),
									1023-cprojs[c].a,
									(sz * camera.z) >> 12,
									spr_shockwave->spr[4], a);

//		if (t > cprojs[c].end)
//			cprojs[c].wep = NULL;
	}

	// check for fiery furies (gong)
	for (c = 0; c < MAX_COMBAT_SHIPS; c++)
	if (cships[c].bong_start > 0 && t > cships[c].bong_start+50 && t < cships[c].bong_end && cships[c].hits > 0 && cships[c].type > -1)
	{
		for (l = 0; l < 5; l++)
		{
			p = (t-cships[c].bong_start-50);
			p = ((p+50)*(p+50)-2500) >> 6;
			a = (205*l + p) & 1023;
			sz = 2000*(cships[c].bong_end-t)/(cships[c].bong_end-cships[c].bong_start-50);
			sx = cships[c].x + ((sin1k[a]*sz)>>6);
			sy = cships[c].y + ((cos1k[a]*sz)>>6);
			ik_drsprite(screen,
									cx + ((((sx - camera.x)>>8) * camera.z) >> 14),
									cy - ((((sy - camera.y)>>8) * camera.z) >> 14),
									((t<<5)&1023),
									(32 * camera.z) >> 12,
									spr_weapons->spr[12], 4);
		}
	}

	for (c = 0; c < MAX_COMBAT_EXPLOS; c++)
	if (cexplo[c].spr)
	{
		if (!cexplo[c].zoom)
		{
			l = 15;
			sz = cexplo[c].size;
		}
		else
		{
			l = 15 - (t - cexplo[c].str)*15/(cexplo[c].end-cexplo[c].str);
			if (l<0) l=0;
			if (cexplo[c].zoom == 1)
				sz = (cexplo[c].size * (t - cexplo[c].str))/(cexplo[c].end-cexplo[c].str);
			else
				sz = cexplo[c].size;
		}
		a = (cexplo[c].a + (t - cexplo[c].str) * cexplo[c].va) & 1023;
		if (cexplo[c].anim==-1)
		{
			p = (t - cexplo[c].str)*cexplo[c].spr->num/(cexplo[c].end-cexplo[c].str);
			if (p >= cexplo[c].spr->num)
				p = cexplo[c].spr->num - 1;
		}
		else p = cexplo[c].anim;

		ik_drsprite(screen,
								cx + ((((cexplo[c].x - camera.x)>>8) * camera.z) >> 14),
								cy - ((((cexplo[c].y - camera.y)>>8) * camera.z) >> 14),
								a,
								(sz * camera.z) >> 12,
								cexplo[c].spr->spr[p], 
								5+(l<<8));
		if (t > cexplo[c].end)
			cexplo[c].spr = NULL;
	}

	if (camera.ship_sel > -1)
	{
		t = get_ik_timer(1);

		s = camera.ship_sel; 
		if (t >= camera.time_sel + 40)
		{	
			l = 6; 
			p = MAX(cships[s].ds_s, 16); 
			a = cships[s].a;
		}
		else
		{	
			l = 15 - ((t - camera.time_sel) >> 2);
			p = cships[s].ds_s + ((cships[s].ds_s * (camera.time_sel + 40 - t))>>5);
			a = (cships[s].a + (camera.time_sel + 40 - t)*16) & 1023;
		}
		ik_drsprite(screen, 
								cships[s].ds_x,
								cships[s].ds_y,
								a,
								p, 
								spr_IFtarget->spr[8], 5+(l<<8));
		if (t < camera.time_sel + 32)
		{
			l = 15 - ((t - camera.time_sel) >> 1);
			ik_drsprite(screen, 
									cships[s].ds_x,
									cships[s].ds_y,
									a,
									(cships[s].ds_s * (t - camera.time_sel))>>3, 
									spr_IFtarget->spr[8], 5+(l<<8));
		}

		fr = 2;
		s = cships[camera.ship_sel].target; 
		if (s > -1)
		{
			camera.ship_trg = s;
			if ((cships[camera.ship_sel].own&1) == (cships[s].own&1))
				fr = 1;
			else if (cships[s].active==2)
				fr = 0;
			else
				fr = 2;

			if (cships[camera.ship_sel].cloaked && shiptypes[cships[s].type].race==race_unknown)
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
				p = cships[s].ds_s + ((cships[s].ds_s * (camera.time_trg + 40 - t))>>5);
				a = (cships[s].a + (camera.time_trg + 40 - t)*16) & 1023;
			}

			if (fr==2)	// "board"
				ik_print(screen, font_6x8, cships[s].ds_x-15, cships[s].ds_y-cships[s].ds_s/2-12, 4, "BOARD");

			ik_drsprite(screen, 
									cships[s].ds_x,
									cships[s].ds_y,
									a,
									MAX(cships[s].ds_s, 16), 
									spr_IFtarget->spr[6+fr], 5+(l<<8));
			if (t < camera.time_trg + 32)
			{
				l = 15 - ((t - camera.time_trg) >> 1);
				ik_drsprite(screen, 
										cships[s].ds_x,
										cships[s].ds_y,
										a,
										(cships[s].ds_s * (t - camera.time_trg))>>3, 
										spr_IFtarget->spr[6+fr], 5+(l<<8));
			}
		}
		if (!camera.drag_trg)
		{
			l = 4 + 3*((t & 31)>24);
			p = cships[camera.ship_sel].dist;
			if (s > -1)
			{
				x = cships[s].ds_x - ((((sin1k[cships[camera.ship_sel].angle] * p)>>16) * camera.z)>>12);
				y = cships[s].ds_y + ((((cos1k[cships[camera.ship_sel].angle] * p)>>16) * camera.z)>>12);
			}
			else
			{
				x = cx + ((((cships[camera.ship_sel].wp_x - camera.x)>>8) * camera.z) >> 14);
				y = cy - ((((cships[camera.ship_sel].wp_y - camera.y)>>8) * camera.z) >> 14);
			}
		}
		else
		{
			l = 7 + 8*((t & 31)>24);
			x = ik_mouse_x;
			y = ik_mouse_y;
		}
		if (s > -1)
			a = get_distance(cships[s].ds_x-x, y-cships[s].ds_y);
		if (s > -1 && a > (cships[s].ds_s>>1) )
		{
			if (cships[camera.ship_sel].tac==0)
			{
				a = get_direction(x-cships[camera.ship_sel].ds_x, cships[camera.ship_sel].ds_y-y);
				ik_dspriteline(screen, 
											cships[camera.ship_sel].ds_x,
											cships[camera.ship_sel].ds_y,
											x - (sin1k[a]>>13),
											y + (cos1k[a]>>13),
											8, (t&15), 16, spr_IFtarget->spr[fr*2], 5+(l<<8));
				a = get_direction(cships[s].ds_x-x, y-cships[s].ds_y);
				if ((cships[s].own&1) != (cships[camera.ship_sel].own&1))
				{
					ik_dspriteline(screen, 
												x + (sin1k[a]>>13),
												y - (cos1k[a]>>13),
												cships[s].ds_x,
												cships[s].ds_y,
												8, (t&15), 16, spr_IFtarget->spr[fr*2], 5+(l<<8));
				}
			}
			else
			{
				ik_dspriteline(screen, 
											cships[camera.ship_sel].ds_x,
											cships[camera.ship_sel].ds_y,
											cships[s].ds_x,
											cships[s].ds_y,
											8, (t&15), 16, spr_IFtarget->spr[fr*2], 5+(l<<8));
			}
			ik_drsprite(screen, 
									x,
									y,
									(t*8) & 1023,
									12, 
									spr_IFtarget->spr[fr+6], 5+(l<<8));
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
									(t*8) & 1023,
									12, 
									spr_IFtarget->spr[fr+6], 5+(l<<8));

			ik_dspriteline(screen, 
										cships[camera.ship_sel].ds_x,
										cships[camera.ship_sel].ds_y,
										x,
										y,
										8, (t&15), 16, spr_IFtarget->spr[fr*2], 5+(l<<8));
		}
	}
	ik_setclip(0,0,640,480);
	interface_drawborder(screen,
											 160, 0, 640, 480,
											 0, COMBAT_INTERFACE_COLOR, textstring[STR_COMBAT_TITLE]);
	// draw selected ships

	combat_displayships();


	// pause button
	ik_dsprite(screen, 170, 456, spr_IFbutton->spr[4], 2+(COMBAT_INTERFACE_COLOR<<8));
	ik_dsprite(screen, 186, 456, spr_IFbutton->spr[5], 2+(COMBAT_INTERFACE_COLOR<<8));
	ik_dsprite(screen, 202, 456, spr_IFbutton->spr[6], 2+(COMBAT_INTERFACE_COLOR<<8));

	ik_dsprite(screen, 177, 456, spr_IFbutton->spr[7+(pause!=1)], 2+(COMBAT_INTERFACE_COLOR<<8));
	ik_dsprite(screen, 190, 456, spr_IFbutton->spr[9+(pause!=0)], 2+(COMBAT_INTERFACE_COLOR<<8));
	ik_dsprite(screen, 199, 456, spr_IFbutton->spr[17+(pause!=-1)], 2+(COMBAT_INTERFACE_COLOR<<8));

	// race portraits
	for (x = 1; x < player.num_ships; x++)
	if (!(shiptypes[player.ships[x]].flag & 8))
	{
		if (cships[x].hits > 0)
			ik_dsprite(screen, 176+(x-1)*68, 24, spr_SMraces->spr[shiptypes[player.ships[x]].race], 0);
		else
			ik_drsprite(screen, 176+(x-1)*68+32, 24+32, 0, 64, spr_SMraces->spr[shiptypes[player.ships[x]].race], 1+(24<<8));
		ik_dsprite(screen, 176+(x-1)*68, 24, spr_IFborder->spr[18], 2+(4<<8));
	}

	y = 0;
	for (x = 0; x < MAX_COMBAT_SHIPS; x++)
	{
		if (cships[x].own==2 && cships[x].type>-1) // klakar
		{
			if (cships[x].hits > 0)
				ik_dsprite(screen, 176+(player.num_ships-1)*68, 24, spr_SMraces->spr[shiptypes[cships[x].type].race], 0);
			else
				ik_drsprite(screen, 176+(player.num_ships-1)*68+32, 24+32, 0, 64, spr_SMraces->spr[shiptypes[cships[x].type].race], 1+(24<<8));
			ik_dsprite(screen, 176+(player.num_ships-1)*68, 24, spr_IFborder->spr[18], 2+(3<<8));
		}
		else if (cships[x].own==1 && y==0)	// enemy
		{
			if (cships[x].type>-1 && shiptypes[cships[x].type].race < race_unknown)
			{
				ik_dsprite(screen, 560, 24, spr_SMraces->spr[shiptypes[cships[x].type].race], 0);
				ik_dsprite(screen,560, 24, spr_IFborder->spr[18], 2+(1<<8));
				y = 1;
			}
		}
	}

#ifdef DEBUG_COMBAT
	ik_print(screen, font_6x8, 176, 16, 0, combatdebug);
#endif

}
// ----------------
//     INCLUDES
// ----------------

#include <stdlib.h>
#include <stdio.h>
#include <string.h>

#include "typedefs.h"
#include "iface_globals.h"
#include "is_fileio.h"
#include "gfx.h"
#include "snd.h"
#include "starmap.h"
#include "combat.h"

// ----------------
//     CONSTANTS
// ----------------

char *hull_keywords[hlkMax] =
{
	"HULL",
	"NAME",
	"SIZE",
	"HITS",
	"MASS",
	"SPRI",
	"SILU",
	"WEAP",
	"ENGN",
	"THRU",
	"FTRB",
	"END",
};

char *shiptype_keywords[shkMax] =
{
	"STYP",
	"NAME",
	"RACE",
	"FLAG",
	"HULL",
	"ARMR",
	"SYST",
	"ENGN",
	"THRU",
	"WEAP",
	"END",
};

char *shipweapon_keywords[wpkMax] = 
{
	"WEAP",
	"NAME",
	"STGE",
	"TYPE",
	"FLAG",
	"SPRI",
	"SIZE",
	"SND1",
	"SND2",
	"RATE",
	"SPED",
	"DAMG",
	"RANG",
	"END",
};

char *shipweapon_flagwords[wpfMax] =
{
	"trans",
	"spin",
	"disperse",
	"implode",
	"homing",
	"split",
	"shock1",
	"shock2",
	"nova",
	"wiggle",
	"strail",
	"noclip",
};

char *shipsystem_keywords[sykMax] = 
{
	"SYST",
	"NAME",
	"TYPE",
	"SIZE",
	"PAR1",
	"PAR2",
	"PAR3",
	"PAR4",
	"END",
};

char *race_keywords[rckMax] =
{
	"RACE",
	"NAME",
	"TEXT",
	"TXT2",
	"END",
};

// ----------------
// GLOBAL VARIABLES
// ----------------

t_ik_image			*combatbg1;
t_ik_image			*combatbg2;

t_ik_spritepak	*spr_ships;
t_ik_spritepak	*spr_shipsilu;
t_ik_spritepak	*spr_weapons;
t_ik_spritepak	*spr_explode1;
t_ik_spritepak	*spr_shockwave;
t_ik_spritepak	*spr_shield;

t_hull					*hulls;
int							num_hulls;

t_shiptype			*shiptypes;
int							num_shiptypes;

t_shipweapon		*shipweapons;
int							num_shipweapons;

t_shipsystem		*shipsystems;
int							num_shipsystems;

//char						racename[16][32];
//int							num_races;
int							enemies[16];
int							num_enemies;

// ----------------
// LOCAL PROTOTYPES
// ----------------

void initraces();

void combat_inithulls();
void combat_deinithulls();

void combat_initshiptypes();
void combat_deinitshiptypes();

void combat_initshipweapons();
void combat_deinitshipweapons();

void combat_initshipsystems();
void combat_deinitshipsystems();

void combat_initsprites();
void combat_deinitsprites();

// ----------------
// GLOBAL FUNCTIONS
// ----------------

void combat_init()
{
	initraces();
	combat_initsprites();
	combat_initshipweapons();
	combat_initshipsystems();
	combat_inithulls();
	combat_initshiptypes();
}

void combat_deinit()
{
	combat_deinitshiptypes();
	combat_deinithulls();
	combat_deinitshipweapons();
	combat_deinitshipsystems();
	combat_deinitsprites();
}

// ----------------
// LOCAL FUNCTIONS
// ----------------

void combat_inithulls()
{
	FILE* ini;
	char s1[64], s2[256];
	char end;
	int num;
	int flag;
	int n, com;

	char ts1[64];
	int tv1, tv2, tv3, tv4, tv5;

	ini = myopen("gamedata/hulls.ini", "rb");
	if (!ini)
		return;

	end = 0; num = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		if (!strcmp(s1, hull_keywords[hlkBegin]))
			num++;
	}
	fclose(ini);

	hulls = (t_hull*)calloc(num, sizeof(t_hull));
	if (!hulls)
		return;
	num_hulls = num;

	ini = myopen("gamedata/hulls.ini", "rb");

	end = 0; num = 0; flag = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		com = -1;
		for (n = 0; n < hlkMax; n++)
			if (!strcmp(s1, hull_keywords[n]))
				com = n;

		if (flag == 0)
		{
			if (com == hlkBegin)
			{
				hulls[num].numh = 0;
				flag = 1;
			}
		}
		else switch(com)
		{
			case hlkName:
			strcpy(hulls[num].name, s2);
			break;

			case hlkSize:
			sscanf(s2, "%d", &tv1);
			hulls[num].size = tv1;
			break;

			case hlkHits:
			sscanf(s2, "%d", &tv1);
			hulls[num].hits = tv1;
			break;

			case hlkMass:
			sscanf(s2, "%d", &tv1);
			hulls[num].mass = tv1;
			break;

			case hlkSprite:
			sscanf(s2, "%s %d", ts1, &tv2);
			hulls[num].sprite = spr_ships->spr[tv2];
			break;

			case hlkSilu:
			sscanf(s2, "%s %d", ts1, &tv2);
			hulls[num].silu = spr_shipsilu->spr[tv2];
			break;

			case hlkWeapon:
			case hlkEngine:
			case hlkThruster:
			case hlkFighter:
			sscanf(s2, "%d %d %d %d %d", &tv1, &tv2, &tv3, &tv4, &tv5);
			hulls[num].hardpts[hulls[num].numh].type = hdpWeapon + (com - hlkWeapon);
			hulls[num].hardpts[hulls[num].numh].x = tv1;
			hulls[num].hardpts[hulls[num].numh].y = tv2;
			hulls[num].hardpts[hulls[num].numh].a = (tv3 * 1024) / 360;
			hulls[num].hardpts[hulls[num].numh].f = (tv4 * 1024) / 360;
			hulls[num].hardpts[hulls[num].numh].size = tv5;
			hulls[num].numh++;
			break;

			case hlkEnd:
			num++; flag = 0;
			break;

			default: ;
		}

	}
	fclose(ini);
}

void combat_deinithulls()
{
	num_hulls = 0;
	free(hulls);
}

void combat_initshiptypes()
{
	FILE* ini;
	char s1[64], s2[256];
	char end;
	int num;
	int flag;
	int n, com;
	int wep;

	ini = myopen("gamedata/ships.ini", "rb");
	if (!ini)
		return;

	end = 0; num = 0; 
	flag = 0; num_enemies = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		if (!strcmp(s1, shiptype_keywords[shkBegin]))
			num++;
		if (!strcmp(s1, "ENEMIES"))
		{	flag = 2; n=0; }
		else if (flag>0 && strcmp(s1, "END")==0)
			flag = 0;
		else if (flag > 0)
		{
			if (flag == 2)
			{
				for (com = 0; com < num_races; com++)
					if (!strcmp(races[com].name, s2))
					{
						enemies[n]=com;
						num_enemies++;
						n++;
					}
			}
		}

	}
	fclose(ini);

	shiptypes = (t_shiptype*)calloc(num, sizeof(t_shiptype));
	if (!shiptypes)
		return;
	num_shiptypes = num;

	ini = myopen("gamedata/ships.ini", "rb");

	end = 0; num = 0; flag = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		com = -1;
		for (n = 0; n < shkMax; n++)
			if (!strcmp(s1, shiptype_keywords[n]))
				com = n;

		if (flag == 0)
		{
			if (com == shkBegin)
			{
				flag = 1;
				wep = 0;
				shiptypes[num].engine = -1;
				shiptypes[num].thrust = -1;
				shiptypes[num].num_systems = 0;
				shiptypes[num].flag = 0;
				for (n = 0; n < 16; n++)
					shiptypes[num].system[n] = -1;
			}
		}
		else switch(com)
		{
			case shkName:
			strcpy(shiptypes[num].name, s2);
			break;

			case shkRace:
			for (n = 0; n < num_races; n++)
				if (!strcmp(races[n].name, s2))
					shiptypes[num].race = n;
			break;

			case shkFlag:
			sscanf(s2, "%d", &n);
			shiptypes[num].flag = n;
			break;

			case shkHull:
			for (n = 0; n < num_hulls; n++)
				if (!strcmp(hulls[n].name, s2))
					shiptypes[num].hull = n;
			break;

			case shkSystem:
			case shkWeapon:
			case shkEngine:
			case shkThruster:
			for (n = 0; n < num_shipsystems; n++)
				if (!strcmp(shipsystems[n].name, s2))
				{	
					shiptypes[num].system[shiptypes[num].num_systems] = n;
					shiptypes[num].sysdmg[shiptypes[num].num_systems] = 0;
					shiptypes[num].num_systems++;
				}
			break;

			/*
			case shkEngine:
			for (n = 0; n < num_shipsystems; n++)
				if (!strcmp(shipsystems[n].name, s2))
					shiptypes[num].engine = n;
			break;

			case shkThruster:
			for (n = 0; n < num_shipsystems; n++)
				if (!strcmp(shipsystems[n].name, s2))
					shiptypes[num].thrust = n;
			shiptypes[num].speed = (shipsystems[shiptypes[num].thrust].par[0] * 32) / hulls[shiptypes[num].hull].mass;
			//shiptypes[num].speed = 1024 / hulls[shiptypes[num].hull].mass;
			break;

			case shkWeapon:
			for (n = 0; n < num_shipweapons; n++)
				if (!strcmp(shipweapons[n].name, s2))
					shiptypes[num].weapon[wep] = n;
			wep++;
			break;*/

			case shkEnd:
			sort_shiptype_systems(num);
			num++; flag = 0;
			break;

			default: ;
		}

	}
	fclose(ini);
}

void combat_deinitshiptypes()
{
	num_shiptypes = 0;
	free(shiptypes);
}

void combat_initshipweapons()
{
	FILE* ini;
	char s1[64], s2[256];
	char end;
	int num;
	int flag;
	int n, com;

	char ts1[64];
	char ts[4][64];
	int tv1, tv2;

	ini = myopen("gamedata/weapons.ini", "rb");
	if (!ini)
		return;

	end = 0; num = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		if (!strcmp(s1, shipweapon_keywords[wpkBegin]))
			num++;
	}
	fclose(ini);

	shipweapons = (t_shipweapon*)calloc(num, sizeof(t_shipweapon));
	if (!shipweapons)
		return;
	num_shipweapons = num;

	ini = myopen("gamedata/weapons.ini", "rb");

	end = 0; num = 0; flag = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		com = -1;
		for (n = 0; n < wpkMax; n++)
			if (!strcmp(s1, shipweapon_keywords[n]))
				com = n;

		if (flag == 0)
		{
			if (com == wpkBegin)
			{
				flag = 1;
				shipweapons[num].item = -1;
			}
		}
		else switch(com)
		{
			case wpkName:
			strcpy(shipweapons[num].name, s2);
			shipweapons[num].flags = 0;
			break;

			case wpkStage:
			for (n = 0; n < num; n++)
				if (!strcmp(shipweapons[n].name, s2))
					shipweapons[num].stage = n;
			break;

			case wpkType:
			sscanf(s2, "%d", &tv1);
			shipweapons[num].type = tv1;
			break;

			case wpkFlag:
			for (n = 0; n < 4; n++)
				strcpy(ts[n], "");
			sscanf(s2, "%s %s %s %s", ts[0], ts[1], ts[2], ts[3]);
			for (n = 0; n < 4; n++)
				for (tv1 = 0; tv1 < wpfMax; tv1++)
					if (!strcmp(shipweapon_flagwords[tv1], ts[n]))
						shipweapons[num].flags |= (1<<tv1);
			break;

			case wpkSprite:
			sscanf(s2, "%s %d", ts1, &tv2);
			shipweapons[num].sprite = spr_weapons->spr[tv2];
			break;

			case wpkSize:
			sscanf(s2, "%d", &tv1);
			shipweapons[num].size = tv1;
			break;

			case wpkSound1:
			sscanf(s2, "%d", &tv1);
			if (shipweapons[num].type==1)
				tv1+=SND_PROJS;
			else
				tv1+=SND_BEAMS;
			shipweapons[num].sound1 = tv1;
			break;

			case wpkSound2:
			sscanf(s2, "%d", &tv1);
			tv1+=SND_HITS;
			shipweapons[num].sound2 = tv1;
			break;

			case wpkRate:
			sscanf(s2, "%d", &tv1);
			shipweapons[num].rate = tv1;
			break;

			case wpkSpeed:
			sscanf(s2, "%d", &tv1);
			shipweapons[num].speed = tv1;
			break;

			case wpkDamage:
			sscanf(s2, "%d", &tv1);
			shipweapons[num].damage = tv1;
			break;

			case wpkRange:
			sscanf(s2, "%d", &tv1);
			shipweapons[num].range = tv1;
			break;

			case wpkEnd:
			num++; flag = 0;
			break;

			default: ;
		}

	}
	fclose(ini);
}

void combat_deinitshipweapons()
{
	num_shipweapons = 0;
	free(shipweapons);
}

void combat_initshipsystems()
{
	FILE* ini;
	char s1[64], s2[256];
	char end;
	int num;
	int flag;
	int n, com;
	int tv1;

	char systype[16][32];
	int32 num_systypes;

	ini = myopen("gamedata/systems.ini", "rb");
	if (!ini)
		return;

	end = 0; num = 0; 
	flag = 0; num_systypes = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		if (!strcmp(s1, shipsystem_keywords[sykBegin]))
			num++;
		if (!strcmp(s1, "SYSTEMTYPES"))
		{	flag = 1; n=0; }
		else if (flag>0 && strcmp(s1, "END")==0)
			flag = 0;
		else 	if (flag == 1)
		{
			strcpy(systype[n], s1);
			num_systypes++;
			n++;
		}

	}
	fclose(ini);

	shipsystems = (t_shipsystem*)calloc(num, sizeof(t_shipsystem));
	if (!shipsystems)
		return;
	num_shipsystems = num;

	ini = myopen("gamedata/systems.ini", "rb");

	end = 0; num = 0; flag = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		com = -1;
		for (n = 0; n < sykMax; n++)
			if (!strcmp(s1, shipsystem_keywords[n]))
				com = n;

		if (flag == 0)
		{
			if (com == sykBegin)
			{
				flag = 1;
				shipsystems[num].item = -1;
			}
		}
		else switch(com)
		{
			case sykName:
			strcpy(shipsystems[num].name, s2);
			break;

			case sykType:
			for (n = 0; n < num_systypes; n++)
				if (!strcmp(s2, systype[n]))
					shipsystems[num].type = n;
			if (shipsystems[num].type == sys_weapon)
			{
				shipsystems[num].par[0] = -1;
				for (n = 0; n < num_shipweapons; n++)
					if (!strcmp(shipsystems[num].name, shipweapons[n].name))
						shipsystems[num].par[0] = n;
			}
			break;

			case sykSize:
			sscanf(s2, "%d", &tv1);
			shipsystems[num].size = tv1;
			break;

			case sykParam1:
			case sykParam2:
			case sykParam3:
			case sykParam4:

			sscanf(s2, "%d", &tv1);
			shipsystems[num].par[com-sykParam1] = tv1;
			break;

			case sykEnd:
			num++; flag = 0;
			break;

			default: ;
		}

	}
	fclose(ini);
}

void combat_deinitshipsystems()
{
	num_shipsystems = 0;
	free(shipsystems);
}

void combat_initsprites()
{
	t_ik_image *pcx;	
	int x, y, n; 

	spr_ships = load_sprites("graphics/ships.spr");
	spr_shipsilu = load_sprites("graphics/shipsilu.spr");
	spr_weapons = load_sprites("graphics/weapons.spr");
	spr_explode1 = load_sprites("graphics/explode1.spr");
	spr_shockwave = load_sprites("graphics/shockwav.spr");
	spr_shield = load_sprites("graphics/shield.spr");

	combatbg1 = ik_load_pcx("graphics/combtbg1.pcx", NULL);
	combatbg2 = ik_load_pcx("graphics/combtbg2.pcx", NULL);

	if (!spr_ships)
	{
		spr_ships = new_spritepak(24);
		pcx = ik_load_pcx("ships.pcx", NULL);
		for (n=0;n<24;n++)

		{
			x = n%5; y = n/5;
			spr_ships->spr[n] = get_sprite(pcx, x*64, y*64, 64, 64);
		}

		del_image(pcx);
		save_sprites("graphics/ships.spr", spr_ships);
	}

	if (!spr_shipsilu)
	{
		spr_shipsilu = new_spritepak(24);
		pcx = ik_load_pcx("silu.pcx", NULL);
		for (n=0;n<24;n++)

		{
			x = n%5; y = n/5;
			spr_shipsilu->spr[n] = get_sprite(pcx, x*128, y*128, 128, 128);
		}

		del_image(pcx);
		save_sprites("graphics/shipsilu.spr", spr_shipsilu);
	}

	if (!spr_weapons)
	{
		spr_weapons = new_spritepak(19);
		pcx = ik_load_pcx("weaponfx.pcx", NULL);

		for (n=0;n<5;n++)
		{
			spr_weapons->spr[n] = get_sprite(pcx, n*32, 0, 32, 32);
		}
		for (n=0;n<5;n++)
		{
			spr_weapons->spr[n+5] = get_sprite(pcx, n*32, 32, 32, 32);
		}
		for (n=0;n<4;n++)
		{
			spr_weapons->spr[n+10] = get_sprite(pcx, n*32, 64, 32, 32);
		}
		spr_weapons->spr[14] = get_sprite(pcx, 192, 64, 128, 128);
		spr_weapons->spr[15] = get_sprite(pcx, 0, 96, 32, 32);
		spr_weapons->spr[16] = get_sprite(pcx, 160, 0, 32, 32);
		spr_weapons->spr[17] = get_sprite(pcx, 128, 64, 32, 32); 
		spr_weapons->spr[18] = get_sprite(pcx, 32, 96, 32, 32); 

		del_image(pcx);
		save_sprites("graphics/weapons.spr", spr_weapons);
	}

	if (!spr_explode1)
	{
		spr_explode1 = new_spritepak(10);
		pcx = ik_load_pcx("xplosion.pcx", NULL);
		
		for (n=0; n<10; n++)
		{
			x = n%5; y = n/5;
			spr_explode1->spr[n] = get_sprite(pcx, x*64, y*64, 64, 64);
		}

		del_image(pcx);
		save_sprites("graphics/explode1.spr", spr_explode1);
	}

	if (!spr_shockwave)
	{
		spr_shockwave = new_spritepak(5);
		pcx = ik_load_pcx("shock.pcx", NULL);
		
		for (n=0; n<5; n++)
		{
			spr_shockwave->spr[n] = get_sprite(pcx, (n%3)*128, (n/3)*128, 128, 128);
		}

		del_image(pcx);
		save_sprites("graphics/shockwav.spr", spr_shockwave);
	}

	if (!spr_shield)
	{
		spr_shield = new_spritepak(5);
		pcx = ik_load_pcx("shields.pcx", NULL);
		
		for (n=0; n<5; n++)
		{
			spr_shield->spr[n] = get_sprite(pcx, n*128, 0, 128, 128);
		}

		del_image(pcx);
		save_sprites("graphics/shield.spr", spr_shield);
	}

}

void combat_deinitsprites()
{
	free_spritepak(spr_ships);
	free_spritepak(spr_shipsilu);
	free_spritepak(spr_weapons);
	free_spritepak(spr_explode1);
	free_spritepak(spr_shockwave);
	free_spritepak(spr_shield);

	del_image(combatbg1);
	del_image(combatbg2);
}

void initraces(void)
{
	FILE* ini;
	char s1[64], s2[256];
	char end;
	int num;
	int flag;
	int n, com;

	ini = myopen("gamedata/races.ini", "rb");
	if (!ini)
		return;

	end = 0; num = 0; flag = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		com = -1;
		for (n = 0; n < rckMax; n++)
			if (!strcmp(s1, race_keywords[n]))
				com = n;

		if (flag == 0)
		{
			if (com == rckBegin)
			{
				races[num].fleet=-1;
				flag = 1;
			}
		}
		else switch(com)
		{
			case rckName:
			strcpy(races[num].name, s2);
			break;

			case rckText:
			strcpy(races[num].text, s2);
			break;

			case rckText2:
			strcpy(races[num].text2, s2);
			break;

			case rckEnd:
			num++; flag = 0;
			break;

			default: ;
		}

	}
	num_races = num;
	fclose(ini);
}

void sort_shiptype_systems(int32 num)
{
	int n, c;
	int w, t;

	w = 0; 
	// systems are sorted by type (weapons first to match hardpoints)
	for (n=0; n < shiptypes[num].num_systems; n++)
	{
		c = n;
		while (c > 0 && shipsystems[shiptypes[num].system[c]].type < shipsystems[shiptypes[num].system[c-1]].type)
		{
			t = shiptypes[num].system[c];
			shiptypes[num].system[c] = shiptypes[num].system[c-1];
			shiptypes[num].system[c-1] = t;
			t = shiptypes[num].sysdmg[c];
			shiptypes[num].sysdmg[c] = shiptypes[num].sysdmg[c-1];
			shiptypes[num].sysdmg[c-1] = t;
			c--;
		}
	}

	shiptypes[num].engine = -1;
	shiptypes[num].thrust = -1;
	shiptypes[num].speed = 1;
	shiptypes[num].turn = 1;
	shiptypes[num].sensor = 0;
	for (n = 0; n < shiptypes[num].num_systems; n++)
	{
		if (shipsystems[shiptypes[num].system[n]].type == sys_thruster)
		{
			shiptypes[num].thrust = shiptypes[num].system[n];
			shiptypes[num].speed = (shipsystems[shiptypes[num].system[n]].par[0] * 32) / hulls[shiptypes[num].hull].mass;
			shiptypes[num].turn = (shipsystems[shiptypes[num].system[n]].par[0] * 3) / hulls[shiptypes[num].hull].mass + 1;
			shiptypes[num].sys_thru = n;
		}
		else if (shipsystems[shiptypes[num].system[n]].type == sys_engine)
		{	shiptypes[num].engine = shiptypes[num].system[n]; shiptypes[num].sys_eng = n; }
		else if (shipsystems[shiptypes[num].system[n]].type == sys_sensor)
			shiptypes[num].sensor = shipsystems[shiptypes[num].system[n]].par[0];
	}
}
// ----------------
//     INCLUDES
// ----------------

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <math.h>

#include "typedefs.h"
#include "is_fileio.h"

#include "textstr.h"
#include "iface_globals.h"
#include "gfx.h"
#include "snd.h"
#include "interface.h"
#include "combat.h"
#include "cards.h"
#include "endgame.h"

#include "starmap.h"

#define CS_SHIP 24
#define CS_WING (CS_SHIP+48)
#define CS_FLEET (CS_WING+112)


int32 cs_setupbattle();
void cs_player_init();
void cs_enemy_init(int32 r);
void cs_insertship(int32 st);
void cs_delship(int32 s);

void cs_player_init()
{
	int c;
	int s;

//	strcpy(player.captname, captnames[rand()%num_captnames]);
//	strcpy(player.shipname, shipnames[rand()%num_shipnames]);

	ik_print_log("initializing player...\n");

	memcpy(&shiptypes[0], &shiptypes[1+settings.dif_ship], sizeof(t_shiptype));
	strcpy(shiptypes[0].name, settings.shipname);

	memset(&player, 0, sizeof(t_player));
	strcpy(player.shipname, settings.shipname);
	strcpy(player.captname, settings.captname);

	player.num_ships = 1;
	player.ships[0] = 0;

	player.num_items = 0; 

	for (c = 0; c < num_shiptypes; c++)
	{
		shiptypes[c].hits = hulls[shiptypes[c].hull].hits*256;
		for (s = 0; s < shiptypes[c].num_systems; s++)
			shiptypes[c].sysdmg[s] = 0;
	}

	allies_init();
}

void cs_enemy_init(int32 r)
{
	int32 flt = 0;
	int32 s;

	//r = enemies[rand()%num_enemies];

	sm_fleets[flt].race = r; //enemies[r];
	sm_fleets[flt].num_ships = 0;

	if (r == race_drone)
	{
		sm_fleets[flt].num_ships = 3;
		for (s = 0; s < 3; s++)
			sm_fleets[flt].ships[s] = racefleets[races[sm_fleets[flt].race].fleet].stype[0];
	}

}

void combat_sim()
{
	int end = 0;
	t_player playerback;

	cs_player_init();
	cs_enemy_init(race_drone);

	while (!end && !must_quit)
	{
		if (cs_setupbattle())
		{
			end = 0;
			memcpy(&sm_fleets[1], &sm_fleets[0], sizeof(t_fleet));
			memcpy(&playerback, &player, sizeof(t_player));
			combat(0, 1);
			memcpy(&sm_fleets[0], &sm_fleets[1], sizeof(t_fleet));
			memcpy(&player, &playerback, sizeof(t_player));
			if (must_quit)
				must_quit = 0;
		}
		else
			end = 1;
	}
}

void cs_insertship(int32 st)
{
	int32 x;
	int32 s;
	int32 t0, t1, t2;
	int32 f = 0;

	t0 = racefleets[races[sm_fleets[f].race].fleet].stype[0];
	t1 = racefleets[races[sm_fleets[f].race].fleet].stype[1];
	t2 = racefleets[races[sm_fleets[f].race].fleet].stype[2];

	// if fighter, add to end
	if (st==0)
	{
		sm_fleets[f].ships[sm_fleets[f].num_ships++] = t0;
		return;
	}

	// if super, add to beginning
	if (st==2)
	{
		for (x = sm_fleets[f].num_ships; x > 0; x--)
			sm_fleets[f].ships[x] = sm_fleets[f].ships[x-1];
		sm_fleets[f].ships[0] = t2;
		sm_fleets[f].num_ships++;
		return;
	}

	// if medium
	s = -1;
	for (x = 0; x < sm_fleets[f].num_ships; x++)
	{
		if (sm_fleets[f].ships[x] != t2)
		{
			s = x; break;			
		}
	}

	if (s==-1)
	{
		sm_fleets[f].ships[sm_fleets[f].num_ships++] = t1;
	}
	else
	{
		for (x = sm_fleets[f].num_ships; x > s; x--)
			sm_fleets[f].ships[x] = sm_fleets[f].ships[x-1];
		sm_fleets[f].ships[s] = t1;
		sm_fleets[f].num_ships++;
	}
}

void cs_delship(int32 s)
{
	int32 x;
	int32 f = 0;

	for (x = s; x < sm_fleets[f].num_ships-1; x++)
		sm_fleets[f].ships[x] = sm_fleets[f].ships[x+1];
	sm_fleets[f].num_ships--;
}

int32 cs_setupbattle()
{
	int32 end;
	int32 c, mc;
	int32 bx=192, by=96, h=256;
	int32 y;
	int32 upd=1;
	int32 mx, my, mo;
	int32 t=0;
	int32 s;
	int32 race = race_drone;
	int32 pship = 0;
	int32 wm[16];
	int32 nwm = 0;
	int32 f = 0;
	t_ik_image *bg;

	bg = ik_load_pcx("graphics/starback.pcx", NULL);

	if (player.ships[0]>0 && player.ships[0]<4)
		pship = player.ships[0]-1;

	for (c = 0; c < num_shiptypes; c++)
		if (shiptypes[c].flag == 9)
			wm[nwm++] = c;

	start_ik_timer(1, 31);
	while (get_ik_timer(1) < 2 && !must_quit)
	{
		ik_eventhandler();
	}
	Play_Sound(WAV_MUS_TITLE, 15, 1, 100, 22050,-1000);
	while (get_ik_timer(1) < 4 && !must_quit)
	{
		ik_eventhandler();
	}
	Play_Sound(WAV_MUS_TITLE, 14, 1, 80, 22050, 1000);
		
	end = 0; t = get_ik_timer(2);
	while (!end && !must_quit)
	{
		ik_eventhandler();
		c = ik_inkey();
		mc = ik_mclick();
		mx = ik_mouse_x - bx; 
		my = ik_mouse_y - by;

		y = t;
		t = get_ik_timer(2);
		if (t != y)
			upd = 1;

		if (sm_fleets[f].num_ships > 0)
			if (c==13 || c==32)
				end = 2;

		if ((mc & 1) && mx > 0 && mx < 240)
		{
			if (my > h-24 && my < h-8) // buttons
			{
				if (mx > 16 && mx < 64) // cancel
				{	end = 1; Play_SoundFX(WAV_DOT); }
				else if (mx > 176 && mx < 224) // ok
					if (sm_fleets[f].num_ships > 0)
					{	end = 2; Play_SoundFX(WAV_DOT); }
			}
			if (my > CS_SHIP+12 && my < CS_SHIP+44)
			{
				pship = MIN(2,(mx - 12)/72);
				player.ships[0] = pship+1;
				Play_SoundFX(WAV_DOT);
			}
			if (my > CS_WING+11 && my < CS_WING+30 && mx > 15 && mx < 15+nwm*21 && player.num_ships < 3)
			{
				c = wm[(mx-15)/21];
				if (player.num_ships < 2 || (shiptypes[c].flag & 8))
					player.ships[player.num_ships++] = c;
				else if (player.ships[1] != c)
					player.ships[player.num_ships++] = c;
				Play_SoundFX(WAV_DOT);
			}
			if (my > CS_WING+36 && my < CS_WING+100 && mx > 48 && mx < 48+(player.num_ships-1)*72)
			{
				if (mx < 120)
					player.ships[1] = player.ships[2];
				player.num_ships--;
				Play_SoundFX(WAV_DOT);
			}
			if (my > CS_FLEET+7 && my < CS_FLEET+26)
			{
				if (mx > 15 && mx < 74 && sm_fleets[f].num_ships<12)
				{
					c = racefleets[races[race].fleet].stype[(mx-14)/20];
					cs_insertship((mx-14)/20);
					Play_SoundFX(WAV_DOT);
				}
				if (mx > 77 && mx < 222)
				{
					if ((mx - 78)/12 < sm_fleets[f].num_ships)
					{	cs_delship((mx-78)/12); Play_SoundFX(WAV_DOT); }
				}

			}
		}

		if (upd)
		{
			upd = 0;
			prep_screen();
			ik_copybox(bg, screen, 0, 0, 640, 480, 0,0);

			interface_drawborder(screen, bx, by, bx+240, by+h, 1, STARMAP_INTERFACE_COLOR, textstring[STR_COMBAT_SIMTITLE]);

			y = by+CS_SHIP;
			ik_print(screen, font_6x8, bx+16, y, STARMAP_INTERFACE_COLOR, textstring[STR_COMBAT_SIMSHIP], shiptypes[pship+1].name);
			for (c = 0; c < 3; c++)
			{
				ik_dsprite(screen, bx+16+c*72, y+12, spr_IFdifenemy->spr[c+3], 0);
				ik_dsprite(screen, bx+16+c*72, y+12, spr_IFborder->spr[IF_BORDER_FLAT], 2+(((pship==c)*3)<<8));
			}

			mo = -1;
			if (my > CS_WING+11 && my < CS_WING+30 && mx > 15 && mx < 15+nwm*21)
				mo = (mx-15)/21;
			if (my > CS_WING+36 && my < CS_WING+100 && mx > 48 && mx < 48+(player.num_ships-1)*72)
			{
				for (mo = 0; mo < nwm; mo++)
					if (wm[mo] == player.ships[(mx-48)/72+1])
						break;
				if (mo >= nwm)
					mo = -1;
			}
			y = by+CS_WING;
			if (mo==-1)
				ik_print(screen, font_6x8, bx+16, y, STARMAP_INTERFACE_COLOR, textstring[STR_COMBAT_SIMWINGMEN]);
			else
				ik_print(screen, font_6x8, bx+16, y, STARMAP_INTERFACE_COLOR, "%s: %s", textstring[STR_COMBAT_SIMWINGMEN], shiptypes[wm[mo]].name);
			for (c = 0; c < nwm; c++)
			{
				s = wm[c];
				interface_thinborder(screen, bx+16+c*21, y+11, bx+35+c*21, y+30, 3*(mo==c)+STARMAP_INTERFACE_COLOR*(mo!=c), 0);
				ik_drsprite(screen, bx+25+c*21, y+20, 0, 16, hulls[shiptypes[s].hull].sprite, 0);
			}
			for (c = 0; c < 2; c++)
			{
				if (c < player.num_ships-1)
				{
					s = (mo>-1 && wm[mo]==player.ships[c+1]);
					ik_dsprite(screen, bx+52+c*72, y+36, hulls[shiptypes[player.ships[c+1]].hull].sprite, 4);
					ik_dsprite(screen, bx+52+c*72, y+36, spr_IFborder->spr[IF_BORDER_PORTRAIT], 2+(((1-s)*STARMAP_INTERFACE_COLOR+s*3)<<8));
				}
				else
					interface_thinborder(screen, bx+52+c*72, y+36, bx+116+c*72, y+100, STARMAP_INTERFACE_COLOR, STARMAP_INTERFACE_COLOR*16+2);
			}

			y = by+CS_FLEET;
			mo = -1;
			if (my > CS_FLEET+7 && my < CS_FLEET+26 && mx > 15 && mx < 74)
			{
				mo = (mx-14)/20;
				ik_print(screen, font_6x8, bx+16, y+32, STARMAP_INTERFACE_COLOR, shiptypes[racefleets[races[race].fleet].stype[mo]].name);
			}
			ik_print(screen, font_6x8, bx+16, y-4, STARMAP_INTERFACE_COLOR, textstring[STR_COMBAT_SIMENEMIES]);

			interface_thinborder(screen, bx+75, y+7, bx+224, y+26, STARMAP_INTERFACE_COLOR, 0);
			for (c = 0; c < 3; c++)
			{
				s = racefleets[races[race].fleet].stype[c];
				interface_thinborder(screen, bx+15+c*20, y+7, bx+34+c*20, y+26, (mo!=c)*STARMAP_INTERFACE_COLOR+(mo==c)*3, 0);
				ik_drsprite(screen, bx+24+c*20, y+16, 0, 16, hulls[shiptypes[s].hull].sprite, 0);
			}
			for (c = 0; c < sm_fleets[f].num_ships; c++)
			{
				ik_drsprite(screen, bx+83+c*12, y+16, 0, 16, hulls[shiptypes[sm_fleets[0].ships[c]].hull].sprite, 0);
			}

			interface_drawbutton(screen, bx+16, by+h-24, 48, STARMAP_INTERFACE_COLOR, textstring[STR_CANCEL]);
			interface_drawbutton(screen, bx+240-64, by+h-24, 48, STARMAP_INTERFACE_COLOR*(sm_fleets[f].num_ships>0), textstring[STR_START]);

			ik_blit();
		}

	}

	Stop_Sound(14);
	Stop_Sound(15);

	player.ships[0] = pship + 1;

	if (must_quit)
		end = 1;


	del_image(bg);

	return end-1;
}
// ----------------
//     INCLUDES
// ----------------

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <math.h>

#include "typedefs.h"
#include "iface_globals.h"
#include "is_fileio.h"
#include "gfx.h"
#include "snd.h"
#include "interface.h"
#include "starmap.h"
#include "textstr.h"

#include "combat.h"

// ----------------
// GLOBAL FUNCTIONS
// ----------------

void combat_gethardpoint(t_ship *ship, int32 hdp, int32 *rx, int32 *ry)
{
	t_hull *hull;

	if (hdp == -1)	// if no hardpoint specified, hit center
	{
		*rx = ship->x;
		*ry = ship->y;
		return;
	}

	hull = &hulls[shiptypes[ship->type].hull];

	*rx = ship->x + ((( (hull->hardpts[hdp].x-32) * cos1k[ship->a] + 
				(32-hull->hardpts[hdp].y) * sin1k[ship->a] ) * hull->size) >> 12);
	*ry = ship->y + ((( (32-hull->hardpts[hdp].y) * cos1k[ship->a] + 
				(32-hull->hardpts[hdp].x) * sin1k[ship->a] ) * hull->size) >> 12);
}

int32 combat_findtarget(t_ship *ship, int32 hdp)
{
	int32 e;
	int32 s;
	int32 a1, a2, a3;
	int32 md, d;
	t_shipweapon *wep;

	s = shiptypes[ship->type].system[hdp];
	if (shipsystems[s].type != sys_weapon)
		return -1;
	if (shipsystems[s].par[0] == -1)	// empty slot
		return -1;
	wep = &shipweapons[shipsystems[s].par[0]];

	md = wep->range;
	e = -1;

	for (s = 0; s < MAX_COMBAT_SHIPS; s++)
	if (&cships[s] != ship && cships[s].type>-1 && (cships[s].own&1) != (ship->own&1) && 
			cships[s].hits>0 && cships[s].active==2 && cships[s].cloaked==0)
	{
		a1 = get_direction( (cships[s].x>>10)-(ship->x>>10), (cships[s].y>>10)-(ship->y>>10) );
		d = get_distance( (cships[s].x>>10)-(ship->x>>10), (cships[s].y>>10)-(ship->y>>10) );
		a2 = (ship->a + hulls[shiptypes[ship->type].hull].hardpts[hdp].a) & 1023;
		if (d < md || (s == ship->target && d < wep->range))
		{
			a3 = a1 - a2;
			while (a3 > 512) a3-=1024;
			while (a3 <-512) a3+=1024;
			a3 = ABS(a3);
			if (a3 < hulls[shiptypes[ship->type].hull].hardpts[hdp].f)
			{
				e = s;
				md = d;
				if (s == ship->target)
				{ md = 0;	break; }
			}
		}
	}

	return e;
}

void combat_fire(t_ship *src, int32 hdp, t_ship *trg, int32 start)
{
	t_shipweapon *wep;
	int s = shiptypes[src->type].system[hdp];
	if (shipsystems[s].type != sys_weapon)
		return;
	if (src->syshits[hdp]<1)
		return;
	wep = &shipweapons[shipsystems[s].par[0]];
//	t_shipweapon *wep = &shipweapons[shiptypes[src->type].weapon[hdp]];

	if (wep->type == 0)
	{
		combat_addbeam(wep, src, hdp, trg, start);
	}
	else
	{
		combat_addproj(src, hdp, trg, start);
	}
}

int32 combat_addbeam(t_shipweapon *wep, t_ship *src, int32 hdp, t_ship *trg, int32 start, int32 stg)
{
	int32 c;
	int32 b;
	int32 cpu;
	int32 hit;
	int s = shiptypes[src->type].system[hdp];

	if (stg == -1)
	{
		if (shipsystems[s].type != sys_weapon)
			return -1;
		wep = &shipweapons[shipsystems[s].par[0]];
	//	t_shipweapon *wep = &shipweapons[shiptypes[src->type].weapon[hdp]];

		cpu = src->cpu_type;
		hit = (40*(cpu+1)*100) / (trg->speed+100);
	}
	else
		hit = 100;

	b = -1;
	for (c = 0; c < MAX_COMBAT_BEAMS; c++)
	if (!cbeams[c].wep)
	{ b = c; break; }

	if (b==-1)
		return -1;

	cbeams[b].wep = wep;
	cbeams[b].stg = stg;

	cbeams[b].src = src;
	cbeams[b].stp = hdp;
	if (rand()%100 < hit)
	{
		cbeams[b].dst = trg;
		cbeams[b].dsp = -1;
	}
	else
	{
		cbeams[b].dst = NULL;
		cbeams[b].ang = get_direction( trg->x - src->x, trg->y - src->y );
		cbeams[b].len = wep->range;
	}
	cbeams[b].str = start;
	cbeams[b].dmt = start + wep->speed * 2;
	cbeams[b].end = start + wep->speed * 5;

	if (stg == -1)
	{
		if (src->syshits[hdp]<5) // damaged
			src->wepfire[hdp] = start + wep->rate * (3 + 2*(src->cpu_type<3)) * (1+rand()%3);
		else
			src->wepfire[hdp] = start + wep->rate * (3 + 2*(src->cpu_type<3));
	}

	combat_SoundFX(wep->sound1, src->x); 
	return b;
}

int32 calc_leadangle(int32 tx, int32 ty, int32 vtx, int32 vty, 
										 int32 sx, int32 sy, int32 vsx, int32 vsy,
										 int32 speed)
{
	int32 a1, a2, a3;
	int32 r;
	int32 a;
	double sinb;

	a1 = get_direction( (tx>>10)-(sx>>10), (ty>>10)-(sy>>10) );
	a2 = get_direction( (vtx - vsx), (vty - vsy) );
	r = (get_distance( (vtx - vsx), (vty - vsy) ) * COMBAT_FRAMERATE) >> 10;		// target speed
	a3 = (a2 - a1 + 1536) & 1023; 
	if (a3 > 512) a3-=1024;
	sinb = r * sin (a3 * 3.14159 / 512) / speed;
	if (sinb > 1)
		return -1;
	a = (int32)(asin(sinb) * 512 / 3.14159);
	a = (a1 - a + 1024) & 1023;

	return a;
}

int32 combat_addproj(t_ship *src, int32 hdp, t_ship *trg, int32 start)
{
	int32 c;
	int32 b;
	int32 a;
	int32 a1, a2, a3;
	int32 r;
	int32 eta;
	int32 sx, sy;
	int32 tx, ty;
	double sinb;
	t_hull *hull;
	int32 cpu;
	int s;
	t_shipweapon *wep;
	
	s = shiptypes[src->type].system[hdp];
	if (shipsystems[s].type != sys_weapon)
		return -1;
	wep = &shipweapons[shipsystems[s].par[0]];
//	t_shipweapon *wep = &shipweapons[shiptypes[src->type].weapon[hdp]];

	b = -1;
	for (c = 0; c < MAX_COMBAT_PROJECTILES; c++)
	if (!cprojs[c].wep)
	{ b = c; break; }

	if (b==-1)
		return -1;

	cpu = src->cpu_type;
	hull = &hulls[shiptypes[src->type].hull];
	combat_gethardpoint(src, hdp, &sx, &sy);
	combat_gethardpoint(trg, -1, &tx, &ty);
	r = get_distance( (tx>>10)-(sx>>10), (ty>>10)-(sy>>10) );
	eta = (r * COMBAT_FRAMERATE) / wep->speed;

	if (cpu == 0 || (wep->flags & wpfHoming))
		a = get_direction( (tx>>10)-(sx>>10), (ty>>10)-(sy>>10) );
	else if (cpu == 1)
	{
//		tx = tx + (trg->vx - src->vx) * eta;
//		ty = ty + (trg->vy - src->vy) * eta;
		for (c = 0; c < 3; c++)
		{
			tx = trg->x + (trg->vx - src->vx) * eta;
			ty = trg->y + (trg->vy - src->vy) * eta;
			r = get_distance( (tx>>10)-(sx>>10), (ty>>10)-(sy>>10) );
			eta = (r * COMBAT_FRAMERATE) / wep->speed;
		}
		a = get_direction( (tx>>10)-(sx>>10), (ty>>10)-(sy>>10) );
	}
	else if (cpu >= 2)
	{
		a1 = get_direction( (tx>>10)-(sx>>10), (ty>>10)-(sy>>10) );
		a2 = get_direction( (trg->vx - src->vx), (trg->vy - src->vy) );
		r = (get_distance( (trg->vx - src->vx), (trg->vy - src->vy) ) * COMBAT_FRAMERATE) >> 10;		// target speed
		a3 = (a2 - a1 + 1536) & 1023; 
		if (a3 > 512) a3-=1024;
		sinb = r * sin (a3 * 3.14159 / 512) / wep->speed;
		if (sinb > 1)
			return -1;
		a = (int32)(asin(sinb) * 512 / 3.14159);

		//ty = get_distance( (tx>>10)-(sx>>10), (ty>>10)-(sy>>10) );
		tx = (cos1k[(abs(a3)+1024) & 1023]*r + cos1k[(abs(a)+1024)&1023]*wep->speed) / wep->speed;
		if (tx > 0)
		eta = (eta * 65536) / tx;

#ifdef DEBUG_COMBAT
		sprintf(combatdebug, "ETA %d  TX %d  A %d  A3 %d", eta, tx, a, a3);
#endif

		a = (a1 - a + 1024) & 1023;



		/*for (c = 0; c < 5; c++)
		{
			tx = trg->x + (trg->vx - src->vx) * eta;
			ty = trg->y + (trg->vy - src->vy) * eta;
			r = get_distance( (tx>>10)-(sx>>10), (ty>>10)-(sy>>10) );
			eta = (r * COMBAT_FRAMERATE) / wep->speed;
		}*/
	}

	if (wep->flags && wpfImplode)
		a = (1024 + a + rand()%30 - 15) & 1023;
	
//	a = (src->a + hull->hardpts[hdp].a + 1024) & 1023;

	cprojs[b].x = sx;
	cprojs[b].y = sy;
	cprojs[b].vx = src->vx + ((sin1k[a] * wep->speed / COMBAT_FRAMERATE) >> 6);
	cprojs[b].vy = src->vy + ((cos1k[a] * wep->speed / COMBAT_FRAMERATE) >> 6);
	cprojs[b].a = get_direction ( cprojs[b].vx, cprojs[b].vy );

	cprojs[b].wep = wep;
	cprojs[b].src = src;
	cprojs[b].dst = trg;
	cprojs[b].str = start;

	if (wep->flags & wpfSpin)
		cprojs[b].va = 20;
	else
		cprojs[b].va = 0;

	if (wep->flags & wpfNova)
	{
		combat_addexplo(cprojs[b].x + cprojs[b].vx * 4, 
										cprojs[b].y + cprojs[b].vy * 4, 
										spr_shockwave, 1, 64, 0, start, start+6, 4);
	}

	if (wep->flags & wpfHoming)
	{
		cprojs[b].end = start + COMBAT_FRAMERATE * (wep->range * 3/2) / wep->speed;

		if (cprojs[b].dst->ecm_type > -1 && cprojs[b].dst->syshits[cprojs[b].dst->sys_ecm]>0)
		{	// ecm
			a = shipsystems[cprojs[b].dst->ecm_type].par[0] * 10;
			if (rand()%30 < a)
			{
				//Play_SoundFX(WAV_SYSFIXED);
				cprojs[b].dst = NULL;
				cprojs[b].va = (rand()%4) + 1;
				if (rand()&1) cprojs[b].va = -cprojs[b].va;
			}
		}
	}
	else
	{
		cprojs[b].end = start + COMBAT_FRAMERATE * wep->range / wep->speed;
		if (wep->flags & wpfSplit)
		{
			cprojs[b].end = start + eta - (COMBAT_FRAMERATE * 100) / wep->speed;
			if (cprojs[b].end < start + 10)
				cprojs[b].end = start + 10;
		}
	}
	
	if (wep->flags & wpfImplode)
		cprojs[b].end = start + COMBAT_FRAMERATE * (wep->range * 3/2) / wep->speed;

	if (wep->flags & wpfDisperse)
		cprojs[b].hits = wep->damage;
	else
		cprojs[b].hits = 1;

	if (src->syshits[hdp]<5) // damaged
		src->wepfire[hdp] = start + wep->rate * (3 + 2*(src->cpu_type<3)) * (1+rand()%3);
	else
		src->wepfire[hdp] = start + wep->rate * (3 + 2*(src->cpu_type<3));

	combat_SoundFX(wep->sound1, cprojs[b].x); 
	return b;
}

void combat_launchstages(int32 p, int32 num, int32 start)
{
	int32 c;
	int32 b;
	int32 a;
	int32 f;
	int32 sx, sy;
	int32 tx, ty;
	int32 n;
	t_ship *trg;

	t_shipweapon *wep = &shipweapons[cprojs[p].wep->stage];
	if (cprojs[p].wep->flags & wpfHoming)
	{
		if (cprojs[p].dst)
		{
			trg = cprojs[p].dst;
		}
		else
			return;
	}
	else
		trg = cprojs[p].dst;

	combat_SoundFX(wep->sound1, cprojs[p].x); 
	for (n = 0; n < num; n++)
	{
		b = -1;
		for (c = 0; c < MAX_COMBAT_PROJECTILES; c++)
		if (!cprojs[c].wep)
		{ b = c; break; }

		if (b==-1)
			break;

		sx = cprojs[p].x; 
		sy = cprojs[p].y;
		combat_gethardpoint(trg, -1, &tx, &ty);

		f = 0;
		if (cprojs[p].wep->item != -1)
			if (shipsystems[itemtypes[cprojs[p].wep->item].index].par[2])
				f = shipsystems[itemtypes[cprojs[p].wep->item].index].par[2];

		if (f & 1)
			a = rand()&1023;
		else
		{
			if (cprojs[p].wep->flags & wpfHoming)
				a = get_direction( (tx>>10)-(sx>>10), (ty>>10)-(sy>>10) );
			else
				a = cprojs[p].a;
			if (num>1)
				a = (a + 768 + (512 * n) / (num-1)) & 1023;
		}

		cprojs[b].x = sx;
		cprojs[b].y = sy;
		if (f & 2)
		{
			cprojs[b].vx = ((sin1k[a] * wep->speed / COMBAT_FRAMERATE) >> 6);
			cprojs[b].vy = ((cos1k[a] * wep->speed / COMBAT_FRAMERATE) >> 6);
		}
		else
		{
			cprojs[b].vx = cprojs[p].vx + ((sin1k[a] * wep->speed / COMBAT_FRAMERATE) >> 6);
			cprojs[b].vy = cprojs[p].vy + ((cos1k[a] * wep->speed / COMBAT_FRAMERATE) >> 6);
		}
		cprojs[b].a = get_direction(cprojs[b].vx, cprojs[b].vy);

		cprojs[b].wep = wep;
		cprojs[b].src = cprojs[p].src;
		cprojs[b].dst = trg;
		cprojs[b].str = start;
		if (wep->flags & wpfHoming)
		{
			cprojs[b].end = start + COMBAT_FRAMERATE * (wep->range * 3/2) / wep->speed;
			if (cprojs[b].dst->ecm_type > -1 && cprojs[b].dst->syshits[cprojs[b].dst->sys_ecm]>0)
			{	// ecm
				a = shipsystems[cprojs[b].dst->ecm_type].par[0] * 10;
				if (rand()%30 < a)
				{
					Play_SoundFX(WAV_SYSFIXED, get_ik_timer(1));
					cprojs[b].dst = NULL;
					cprojs[b].va = (rand()%5 + 4)*((rand()&1)*2-1);
				}
			}
		}
		else
			cprojs[b].end = start + COMBAT_FRAMERATE * wep->range / wep->speed;

		if (wep->flags & wpfSpin)
			cprojs[b].va = 20;
		else
			cprojs[b].va = 0;

		if (wep->flags & wpfDisperse)
			cprojs[b].hits = wep->damage;
		else
			cprojs[b].hits = 0;
	}
}

void combat_damageship(int32 s, int32 src, int32 dmg, int32 t, t_shipweapon *wep, int32 deb)
{
	int32 sys = -1;
	int32 d1;
	int32 c;

	if (s == -1)
		return;

	if (cships[s].type == -1)	// now this would suck
		return;

	if (t > cships[s].damage_time + 10)
	{
		if (cships[s].shld_type>-1 && cships[s].shld>0)
		{
			combat_SoundFX(WAV_SHIELD, cships[s].x); 
		}
		else
		{
			if (wep->type==0) // beam
				combat_SoundFX(WAV_EXPLO1, cships[s].x); 
			else
				combat_SoundFX(wep->sound2, cships[s].x); 
		}
		cships[s].damage_time = t;
	}

	if (cships[s].hits <= 0 || cships[s].active<2)
		return;

	if (!deb)
	{
		if (cships[s].shld_type>-1 && cships[s].shld>0)
		{
			if ( (rand()%10)==0)
				sys = cships[s].sys_shld;
			else
			{
				d1 = MIN(dmg, cships[s].shld);
				dmg = dmg - d1;
				cships[s].shld -= d1;
				cships[s].shld_time = t;
				if (cships[s].shld <= 0) cships[s].shld = 0;
			}
		}

		if (dmg>0)
		{
			if ( (rand()%10)<5 )
			{	
				sys = rand()%shiptypes[cships[s].type].num_systems;
				if (cships[s].syshits[sys]<=0 || shipsystems[shiptypes[cships[s].type].system[sys]].item==-1)
					sys = -1;
			}

			if (sys==-1)
				cships[s].hits -= dmg;	// hull hit
			else
			{	
				d1 = cships[s].syshits[sys];
				cships[s].syshits[sys] = MAX (0, cships[s].syshits[sys]-dmg); 
				if (cships[s].syshits[sys]>0)
				{
					if (cships[s].syshits[sys]/5 != d1/5)	// green->yellow, yellow->red
						combat_SoundFX(WAV_SYSHIT1+(rand()&1), cships[s].x);
				}
				else	// red->grey
					combat_SoundFX(WAV_SYSDAMAGE, cships[s].x);
			}	// system damage
		}
	}
	else
	{
		cships[s].hits -= dmg;
	}

	combat_updateshipstats(s, t);

	if (cships[s].hits <= 0) // burn
	{
		cships[s].va = rand()%cships[s].turn+1;
		if (rand()&1)
			cships[s].va = -cships[s].va;
		if (shiptypes[cships[s].type].race == race_unknown)	// stop space hulk
			cships[s].vx = cships[s].vy = 0;

		// reset other ships' targets 
		for (c = 0; c < MAX_COMBAT_SHIPS; c++)
		if (cships[c].type > -1 && cships[c].hits > 0)
		{
			if (cships[c].target == s)
			{
				cships[c].target = -1;
				combat_findstuff2do(c,t);
			}
		}
	}
}

void combat_killship(int32 s, int32 t, int32 quiet)
{
	int32 c;
	int32 sz;
	int32 shu=0;

	if (s == -1)
		return;
	if (cships[s].type == -1)
		return;

	if (shiptypes[cships[s].type].flag & 256)
		shu = 1;

	if (!quiet)
		cships[s].hits = -666;
	sz = hulls[shiptypes[cships[s].type].hull].size * 2;

	if (camera.ship_sel == s)
		camera.ship_sel = -1;

	for (c = 0; c < MAX_COMBAT_SHIPS; c++)
	if (cships[c].type > -1)
	{
		if (cships[c].target == s)
		{
			cships[c].target = -1;
			combat_findstuff2do(c,t);
		}
	}

	for (c = 0; c < MAX_COMBAT_PROJECTILES; c++)
	if (cprojs[c].wep != NULL)
	{
		if (cprojs[c].wep->flags & wpfHoming)
			if (cprojs[c].dst == &cships[s])
				cprojs[c].dst = NULL;
	}

	for (c = 0; c < MAX_COMBAT_BEAMS; c++)
	if (cbeams[c].wep != NULL)
	{
		if (cbeams[c].dst == &cships[s])
			cbeams[c].wep = NULL;
		if (cbeams[c].src == &cships[s])
			cbeams[c].wep = NULL;
	}


	if (shu)
	{
		cships[s].active = 1;
		cships[s].hits = 1;
	}
	else
	{
		cships[s].active = 0;
		if (!quiet)
		{
			combat_SoundFX(WAV_EXPLO2, cships[s].x);
			combat_addexplo(cships[s].x, cships[s].y, spr_shockwave, 
									5, sz*4, 1, t, t+sz/2, 0);
			combat_addexplo(cships[s].x, cships[s].y, spr_explode1, 
									5, sz, 0, t, t+sz/4);
		}
		cships[s].type = -1;
	}
}

int32 combat_addexplo(int32 x, int32 y, t_ik_spritepak *spr, int32 spin, int32 size, int32 zoom, int32 start, int32 end, int32 anim, int32 cam)
{
	int b, c;

	b = -1;
	for (c = 0; c < MAX_COMBAT_EXPLOS; c++)
	if (!cexplo[c].spr)
	{ b = c; break; }

	if (b == -1)
		return -1;

	cexplo[b].x = x;
	cexplo[b].y = y;
	cexplo[c].a = rand()%1024,
	cexplo[b].spr = spr;
	cexplo[b].str = start;
	cexplo[b].end = end;
	cexplo[b].size = size;
	cexplo[b].zoom = zoom;
	cexplo[b].va = spin;
	cexplo[b].anim = anim;
	cexplo[b].cam = cam;

	return b;
}
// ----------------
//     INCLUDES
// ----------------

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <math.h>

#include "typedefs.h"
#include "is_fileio.h"

#include "textstr.h"
#include "iface_globals.h"
#include "gfx.h"
#include "snd.h"
#include "interface.h"
#include "combat.h"
#include "cards.h"
#include "starmap.h"

#include "endgame.h"

// ----------------
//     CONSTANTS
// ----------------

// ----------------
// GLOBAL VARIABLES
// ----------------

t_job		*jobs;
int32		num_jobs;
int32    num_scores;
t_score  scores[20];
int32		 got_hiscore;

// ----------------
// LOCAL PROTOTYPES
// ----------------

void load_scores();
void save_scores();
void checkhiscore(int32 score);

// ----------------
// GLOBAL FUNCTIONS
// ----------------

void game_over()
{
	int32 end;
	int32 c, mc;
	int32 y, h;
	int32 job;
	int32 t=0;
	int32 val, v;
	int32 w;
	int32 bx, by;
	int32 mnv, mxv, vl[7];
	int32 dif = 12 + (settings.dif_enemies + settings.dif_nebula);
	t_ik_image *bg;
	char texty[512];
	char topic[64];
	char edate[32];
	char *bargraphnames[7] = 
	{
		textstring[STR_ENDGAME_BAR1],
		textstring[STR_ENDGAME_BAR2],
		textstring[STR_ENDGAME_BAR3],
		textstring[STR_ENDGAME_BAR4],
		textstring[STR_ENDGAME_BAR5],
		textstring[STR_ENDGAME_BAR6],
		textstring[STR_ENDGAME_BAR7],
	};
	t_ik_sprite *deathpic;

	bg = ik_load_pcx("graphics/starback.pcx", NULL);

	Stop_All_Sounds();

	prep_screen();
	ik_copybox(bg, screen, 0, 0, 640, 480, 0,0);

	y = 0;

	val = 0; 

	vl[0] = -2000;
//	ik_print(screen, font_4x8, 8, y+=8, 0, "Initial loan repay: $%d", -2000);
	val -= 2000;

	c = player.stardate / 365;
	if (c >= 10)
	{
		c -= 9;
//		ik_print(screen, font_4x8, 8, y+=8, 0, "Time limit penalty: $%d", -1000*c);
		vl[1] = -c*1000;
		val -= c*1000;
	}
	else
		vl[1] = 0;


	v = 0;
	for (c = 0; c < num_races; c++)
	{
		if (races[c].met)
			v += 300;
	}
//	ik_print(screen, font_4x8, 8, y+=8, 0, "Alien encounters..: $%d", v);
	v = (v * dif) / 16;
	val+=v;
	vl[2] = v;

	v = 0;
	for (c = 0; c < num_stars; c++)
	{
		if (sm_stars[c].explored && !sm_stars[c].novadate)
			v += platypes[sm_stars[c].planet].bonus;
	}
//	ik_print(screen, font_4x8, 8, y+=8, 0, "Planet exploration: $%d", v);
	v = (v * dif) / 16;
	val+=v;
	vl[3] = v;

	v = 0;
	for (c = 0; c < shiptypes[0].num_systems; c++)
	if (shipsystems[shiptypes[0].system[c]].item > -1)
	{
		v += itemtypes[shipsystems[shiptypes[0].system[c]].item].cost;
	}
//	ik_print(screen, font_4x8, 8, y+=8, 0, "Starship systems..: $%d", v);
	v = (v * dif) / 16;
	val+=v;
	vl[4] = v;

	v = 0;
	for (c = 0; c < player.num_items; c++)
	{
		v += itemtypes[player.items[c]].cost;
	}
//	ik_print(screen, font_4x8, 8, y+=8, 0, "Inventory items...: $%d", v);
	v = (v * dif) / 16;
	val+=v;
	vl[5] = v;

//	ik_print(screen, font_4x8, 8, y+=8, 0, "Other discoveries.: $%d", player.bonusdata);
	for (c = 0; c < STARMAP_MAX_FLEETS; c++)
	if (sm_fleets[c].num_ships>0 && sm_fleets[c].race!=race_klakar && sm_fleets[c].explored>0)
	{
		player.bonusdata += sm_fleets[c].explored*100;
	}
	if (player.num_ships>1)
	{
		player.bonusdata += 300*(player.num_ships-1);
	}

	//ik_print(screen, font_6x8, 0, 0, 1, "BD %d", player.bonusdata);

	v = player.bonusdata;
	v = (v * dif) / 16;
	val += v;
	vl[6] = v;



	mnv=0;mxv=0;
	for (c = 0; c < 7; c++)
	{
		mnv = MIN(vl[c], mnv);
		mxv = MAX(vl[c], mxv);
	}

	bx = 288; by = 64; w = 160;
	v = bx+2 + (0-mnv)*(w-4)/(mxv-mnv);
	for (c = 0; c < 7; c++)
	{
		ik_print(screen, font_4x8, 192, c*16+by, STARMAP_INTERFACE_COLOR, bargraphnames[c]);
		ik_drawbox(screen, bx, c*16+by, bx+w, c*16+by+8, STARMAP_INTERFACE_COLOR*16+10);
		ik_drawbox(screen, bx+1, c*16+by+1, bx+w-1, c*16+by+7, STARMAP_INTERFACE_COLOR*16+3);
		if (vl[c]<0)
			ik_drawbox(screen, bx+2 + (vl[c]-mnv)*(w-4)/(mxv-mnv), c*16+by+2, v, c*16+by+6, 24);
		else
			ik_drawbox(screen, v, c*16+by+2, bx+2 + (vl[c]-mnv)*(w-4)/(mxv-mnv), c*16+by+6, 72);
	}
	
	v = 0; 
	for (c = 0; c < 12; c++)
		if (player.stardate%365 >= months[c].sd)
			v = c;
	c = player.stardate%365 + 1 - months[v].sd;

	if (player.system == homesystem && player.death==0)
	{	bx = 192; by = 208; h = 128; }	// returns home
	else 
	{	bx = 192; by = 208; h = 128; } // dies

	sprintf(edate, textstring[STR_ENDGAME_DATEF], c, months[v].name, player.stardate/365+4580);

	sprintf(topic,  textstring[STR_ENDGAME_OVER], 
					textstring[STR_ENDGAME_DATE], edate);

	sprintf(edate, textstring[STR_ENDGAME_DATEF2], months[v].name, c, player.stardate/365+4580);

	if (player.system == homesystem && player.death==0) // return to hope
	{
		w = 0; h = 152 + (player.stardate>365*20)*8;
		for (y = 0; y < STARMAP_MAX_FLEETS; y++)
		if (sm_fleets[y].race == race_kawangi && sm_fleets[y].num_ships > 0)	// kawangi left
		{
			w = 1; 
			val /= 2; 
			h = 208;
			break;
		}

		interface_drawborder(screen, bx, by, bx+256, by+h, 1, STARMAP_INTERFACE_COLOR, topic);

		job = 0;
		for (y = 0; y < num_jobs; y++)
		if (val > jobs[y].value)
			job = y;

		y = 0;

		ik_dsprite(screen, bx+16, by+24, spr_SMraces->spr[RC_MUCRON], 0);
		ik_dsprite(screen, bx+16, by+24, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));

		y += 1 + interface_textbox(screen, font_4x8, bx+84, by+24+y*8, 160, 128, 0, textstring[STR_ENDGAME_MUCRON1 + (player.stardate>365*20)]);

		sprintf(texty, textstring[STR_ENDGAME_MUCRON3+2*(val<0)], abs(val));
		y += 1 + interface_textbox(screen, font_4x8, bx+84, by+24+y*8, 160, 128, 0, texty);
		y = MAX(9,y);

		sprintf(texty, textstring[STR_ENDGAME_MUCRON4+2*(val<0)], jobs[job].name);
		y += 1 + interface_textbox(screen, font_4x8, bx+16, by+24+y*8, 224, 128, 0, texty);

		if (!w)
		{
			ik_print(screen, font_4x8, bx+16, by+24+y*8, 0, textstring[STR_ENDGAME_MUCRON7]);
			Play_Sound(WAV_MUS_VICTORY, 15, 1);

			sprintf(player.deathmsg, "%s %s", textstring[STR_ENDGAME_MSG1], edate);
		}
		else
		{
			ik_dsprite(screen, bx+16, by+24+y*8, spr_SMraces->spr[race_kawangi], 0);
			ik_dsprite(screen, bx+16, by+24+y*8, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));
			y += 1 + MAX(8,interface_textbox(screen, font_4x8, bx+84, by+24+y*8, 160, 128, 0, textstring[STR_ENDGAME_KAWANGI]));
			Play_Sound(WAV_KAWANGI, 15, 1);

			sprintf(player.deathmsg, "%s %s", textstring[STR_ENDGAME_MSG7], edate);
		}
	}
	else		// died
	{
		val /= 2;
		switch (player.death)
		{
			default:
			case 1:	// combat
				sprintf(texty, 
								textstring[STR_ENDGAME_DEATH1], 
								shiptypes[0].name);
				sprintf(player.deathmsg, "%s %s", textstring[STR_ENDGAME_MSG2], edate);
				deathpic = spr_SMraces->spr[RC_BLOWUP];
			break;
			case 2:	// hole
				sprintf(texty,
								textstring[STR_ENDGAME_DEATH2],
								shiptypes[0].name);
				sprintf(player.deathmsg, "%s %s", textstring[STR_ENDGAME_MSG3], edate);			
				deathpic = spr_SMraces->spr[RC_HOLED];
				break;
			case 3: // vacuum collapse
				sprintf(texty,
								textstring[STR_ENDGAME_DEATH3],
								shiptypes[0].name);
				sprintf(player.deathmsg, "%s %s", textstring[STR_ENDGAME_MSG4], edate);			
				deathpic = spr_SMraces->spr[RC_BLOWUP];
			break;
			case 4: // nova shockwave
				sprintf(texty,
								textstring[STR_ENDGAME_DEATH4],
								shiptypes[0].name);
				sprintf(player.deathmsg, "%s %s", textstring[STR_ENDGAME_MSG5], edate);			
				deathpic = spr_SMraces->spr[RC_BLOWUP];
			break;
			case 5: // sabotage
				sprintf(texty,
								textstring[STR_ENDGAME_DEATH5],
								shiptypes[0].name);
				sprintf(player.deathmsg, "%s %s", textstring[STR_ENDGAME_MSG6], edate);
				deathpic = spr_SMraces->spr[RC_BLOWUP];
			break;
			case 6: // glory harvested by kawangi
				sprintf(texty,
								textstring[STR_ENDGAME_DEATH6],
								shiptypes[0].name);
				sprintf(player.deathmsg, "%s %s", textstring[STR_ENDGAME_MSG7], edate);
				deathpic = spr_SMplanet2->spr[22];
			break;
			case 7: // glory destroyed by vacuum collapser
				sprintf(texty,
								textstring[STR_ENDGAME_DEATH7],
								shiptypes[0].name);
				sprintf(player.deathmsg, "%s %s", textstring[STR_ENDGAME_MSG8], edate);
				deathpic = spr_SMraces->spr[RC_LOST];
			break;
 		}

		h = 56 + 8*MAX(8,interface_textboxsize(font_4x8, 160, 128, texty));

		interface_drawborder(screen, bx, by, bx+256, by+h, 1, STARMAP_INTERFACE_COLOR, topic);

		ik_dsprite(screen, bx+16, by+24, deathpic, 0);
		ik_dsprite(screen, bx+16, by+24, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));

		y = 0;
		y += 1 + interface_textbox(screen, font_4x8, bx+84, by+24+y*8, 160, 128, 0, texty);

		y = MAX(9, y);
		sprintf(texty, textstring[STR_ENDGAME_SCORE], val);
		y += 1 + interface_textbox(screen, font_4x8, bx+16, by+24+y*8, 224, 128, 0, texty);
		Play_Sound(WAV_MUS_DEATH, 15, 1);
	}

	ik_blit();

	update_palette();

	start_ik_timer(1, 20);
//	Play_Sound(WAV_MUS_TITLE, 15, 1, 0);

	end = 0;
	while (!end && !must_quit)
	{
		ik_eventhandler();
		v = t;
		t = get_ik_timer(1);
		c = ik_inkey();
		mc = ik_mclick();

		/*if (t > v)
		{	// half at 17.. full at 34
			if (t < 34*50)
				v = (100*t)/(34*50);
			else
				v = 100;
			Set_Sound_Volume(15, v);
		}*/

		if (mc==1 || c==13 || c==32)
			end = 1;

		c = t; t = get_ik_timer(2);
		if (t != c)
		{ prep_screen(); ik_blit();	}

	}
	del_image(bg);
	Stop_All_Sounds();

	checkhiscore(val);
}

void checkhiscore(int32 score)
{
	int32 c;
	int32 b;

	Stop_All_Sounds();


	got_hiscore = -1;

	b = 0;
	if (num_scores>0)
	{
		while (b < 20 && b < num_scores && scores[b].score >= score)
			b++;
	}

	if (b < 20)
	{
		if (num_scores<20)
			num_scores++;
		for (c = num_scores-1; c > b; c--)
		{
			memcpy(&scores[c], &scores[c-1], sizeof(t_score));
		}
		strcpy(scores[b].cname, player.captname);
		strcpy(scores[b].sname, player.shipname);
		scores[b].score = score;
		scores[b].date = player.stardate;
		strcpy(scores[b].deathmsg, player.deathmsg);
		got_hiscore = b;
	}

	save_scores();
}

void endgame_init()
{
	FILE* ini;
	char s1[64], s2[256];
	char end;
	int num;
	int flag;
	int n;
	int tv1;
	
	ini = myopen("gamedata/jobs.ini", "rb");
	if (!ini)
		return;

	end = 0; num = 0; n = 0; flag = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		if (!strcmp(s1, "JOBS"))
			flag=1;
		else if (!strcmp(s1, "END"))
			flag = 0;
		else if (flag)
			num++;
	}
	fclose(ini);

	jobs = (t_job*)calloc(num, sizeof(t_job));
	if (!jobs)
		return;
	num_jobs = num;

	ini = myopen("gamedata/jobs.ini", "rb");
	end = 0; flag = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		if (!strcmp(s1, "JOBS"))
		{	flag = 1; num = 0; }
		else if (!strcmp(s1, "END"))
			flag = 0;
		else 
		{
			if (flag == 1)
			{
				sscanf(s1, "%d", &tv1);
				jobs[num].value = tv1;
				strcpy(jobs[num].name, s2);
				num++;
			}
		}
	}
	fclose(ini);

	load_scores();
}

void endgame_deinit()
{
	num_jobs = 0;
	free(jobs);

}

void load_scores()
{
	FILE* fil;
	char scorefile[256];
	sprintf(scorefile, "%s%s", moddir, "scores.dat");

	fil = myopen(scorefile, "rb");
	if (!fil)
	{
		num_scores = 0;
		return;
	}

	num_scores = fgetc(fil);
	fgetc(fil); fgetc(fil);	fgetc(fil);

	if (num_scores>20) num_scores=20;

	fread(scores, sizeof(t_score), num_scores, fil);
	fclose(fil);
}

void save_scores()
{
	FILE* fil;
	char scorefile[256];
	sprintf(scorefile, "%s%s", moddir, "scores.dat");

	fil = myopen(scorefile, "wb");
	if (!fil)
	{
		return;
	}

	fputc(num_scores, fil);
	fputc(0, fil); fputc(0, fil); fputc(0, fil);
	fwrite(scores, sizeof(t_score), num_scores, fil);
	fclose(fil);
}
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <memory.h>
#include <math.h>
#include <time.h>
#include <string.h>
#include <malloc.h>

#include "typedefs.h"
#include "iface_globals.h"
#include "gfx.h"
#include "snd.h"

void ik_drawfont(t_ik_image *img, t_ik_font *fnt, int32 x, int32 y, uint8 co, uint8 c);

// LOAD FONT FROM PCX
t_ik_font *ik_load_font(char *fname, uint8 w,  uint8 h)
{
	t_ik_font *fnt;
	t_ik_image *pic;
	int32 x, y, x2, y2, tmp;

	pic=ik_load_pcx(fname, NULL);
	if (!pic)
		return NULL;

	fnt=(t_ik_font *)malloc(sizeof(t_ik_font));
	if (!fnt)
	{
		del_image(pic);
		return NULL;
	}

	fnt->w=w;
	fnt->h=h; 
	fnt->data=(uint8 *)malloc(128*w*h);
	if (!fnt->data)
	{
		free(fnt);
		del_image(pic);
		return NULL;
	}

	for (y=0; y<4; y++)
		for (x=0; x<32; x++)
		{
			tmp=y*32+x;
			for (y2=0;y2<h;y2++)
				for (x2=0;x2<w;x2++)
				{
					fnt->data[tmp*w*h+y2*w+x2]=pic->data[(y*h+y2)*pic->w+(x*w+x2)];
				}
		}

	del_image(pic);
	return fnt;
}

void ik_del_font(t_ik_font *fnt)
{
	if (!fnt)
		return;
	if (fnt->data)
		free(fnt->data);
	free(fnt);
}

// DRAW SINGLE LETTER 
void ik_drawfont(t_ik_image *img, t_ik_font *fnt, int32 x, int32 y, uint8 co, uint8 c)
{
	int32 x1, y1, o;
  uint8 v;

	if (!fnt) return;
  if (x<-fnt->w || y<-fnt->h || x>gfx_width || y>gfx_height || c>127)  return;

	o=fnt->w*fnt->h;

  for (y1=0; y1<fnt->h; y1++)
  {
  	if (y1+y>=c_miny && y1+y<c_maxy)
	  	for (x1=0; x1<fnt->w; x1++)
  	  {
      	if (x1+x>=c_minx && x1+x<c_maxx)
        {
        	v=fnt->data[c*o+y1*fnt->w+x1];
          if (v)
						ik_putpixel(img, x1+x,y1+y,v+(co<<4));
        }
    	}
  }
}

void ik_drawfontbig(t_ik_image *img, t_ik_font *fnt, int32 x, int32 y, uint8 co, uint8 c)
{
	int32 x1, y1, o;
  uint8 v;

	if (!fnt) return;
  if (x<-fnt->w<<1 || y<-fnt->h<<1 || x>gfx_width || y>gfx_height || c>127)  return;

	o=fnt->w*fnt->h;

  for (y1=0; y1<fnt->h<<1; y1++)
  {
  	if (y1+y>=c_miny && y1+y<c_maxy)
	  	for (x1=0; x1<fnt->w<<1; x1++)
  	  {
      	if (x1+x>=c_minx && x1+x<c_maxx)
        {
        	v=fnt->data[c*o+(y1>>1)*fnt->w+(x1>>1)];
          if (v)
						ik_putpixel(img, x1+x,y1+y,v+(co<<4));
        }
    	}
  }
}

// PRINT A LINE
#pragma unmanaged
void ik_print(t_ik_image *img, t_ik_font *fnt, int32 x, int32 y, uint8 co, char *ln, ...)
{
	char dlin[256], *dl=dlin;
  va_list ap;
  va_start(ap, ln);
  vsprintf(dlin, ln, ap);
  va_end(ap);

	if (!fnt)
		return;
	if (!fnt->data)
		return;

  while (*dl)
	{
  	ik_drawfont(img, fnt, x, y, co, (*dl++));
		x+=fnt->w;
  }
}

void ik_printbig(t_ik_image *img, t_ik_font *fnt, int32 x, int32 y, uint8 co, char *ln, ...)
{
	char dlin[256], *dl=dlin;
  va_list ap;
  va_start(ap, ln);
  vsprintf(dlin, ln, ap);
  va_end(ap);

	if (!fnt)
		return;
	if (!fnt->data)
		return;

  while (*dl)
	{
  	ik_drawfontbig(img, fnt, x, y, co, (*dl++));
		x+=fnt->w<<1;
  }
}

void ik_text_input(int x, int y, int l, t_ik_font *fnt, char *pmt, char *tx, int bg, int co)
{
	int end=0,n,upd=1;
	char c;
	int t, ot;
	int l2 = strlen(pmt);
	uint32 p;
	t_ik_sprite *bup;

	n=strlen(tx);
	if (n>=l-l2)
	{
		n=l-1-l2;
		tx[l]=0;
	}

	prep_screen();
	bup=get_sprite(screen, x, y, fnt->w*l, fnt->h);
	free_screen();

	start_ik_timer(3, 500); 
	t=0;ot=0;

	while (!end)
	{
		ik_eventhandler();  // always call every frame
		c=ik_inkey();
		ot=t; t=get_ik_timer(2);

		if (ot!=t)
			upd=1;

		if (c>=32 && c<128 && n<l-1-l2)
		{
			tx[n]=c;
			n++;
			tx[n]=0;
			upd=1;
		}

		if (c==8 && n>0)
		{
			n--;
			tx[n]=0;
			upd=1;
		}

		if (c==27 || must_quit!=0) end=1;
		if (c==13) end=2;

		if (upd>0 && end==0)
		{
			prep_screen();
			ik_drawbox(screen, x, y, x+fnt->w*l-1, y+fnt->h-1, bg);
//			ik_dsprite(screen, x, y, bup, 0);
			if (get_ik_timer(3)&1)
				ik_print(screen,fnt,x,y,co,"%s%s",pmt,tx);
			else
				ik_print(screen,fnt,x,y,co,"%s%s_",pmt,tx);
			ik_blit();
			upd=0;
		}
	}

	prep_screen();
	ik_drawbox(screen, x, y, x+fnt->w*l-1, y+fnt->h-1, 0);
	ik_dsprite(screen, x, y, bup, 0);
	free_sprite(bup);
	ik_blit();

	if (end==1)
		tx[0]=0;
	else	// capitalize first letter of each word
	{
		for (p = 0; p < strlen(tx); p++)
		{
			if (p==0 || (p>0 && (tx[p-1]<=' ' || tx[p-1]=='-')))
			{
				if (tx[p] >= 'a' && tx[p] <= 'z')
					tx[p] += 'A' - 'a';
			}
		}
	}
}

#pragma managed
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <malloc.h>
#include <math.h>

#include "typedefs.h"
#include "iface_globals.h"
#include "gfx.h"
#include "is_fileio.h"
#include "interface.h"

//#define THICK_MAGNIFIER

// GLOBALS

t_ik_image *magni;
t_ik_image *screen;
t_ik_image screenbuf;
int gfx_width, gfx_height, gfx_fullscreen, gfx_switch;
int gfx_redraw;
int c_minx, c_miny, c_maxx, c_maxy;

#ifdef MOVIE
int aframe = 0;
#endif

unsigned char *gfx_transbuffer;
unsigned char *gfx_lightbuffer;
unsigned char *gfx_addbuffer;

t_ik_image *dims[8];
int num_dims;

uint8 globalpal[768];
uint8 currentpal[768];

int32 sin1k[1024];
int32 cos1k[1024];

// PUTPIXEL/GETPIXEL

void ik_setclip(int32 left, int32 top, int32 right, int32 bottom)
{
	c_minx = left;
	c_miny = top;
	c_maxx = right;
	c_maxy = bottom;
}

void ik_putpixel(t_ik_image *img, int32 x, int32 y, uint32 c)
{
	img->data[y*img->pitch+x]=c;
}

void ik_putpixel_add(t_ik_image *img, int32 x, int32 y, uint32 c)
{
	img->data[y*img->pitch+x]=gfx_addbuffer[img->data[y*img->pitch+x]+(c<<8)];
}

int32 ik_getpixel(t_ik_image *img, int32 x, int32 y)
{
	return img->data[y*img->pitch+x];
}

uint8 *ik_image_pointer(t_ik_image *img, int32 x, int32 y)
{
	return img->data+y*img->pitch+x;
}

void ik_drawline(t_ik_image *img, int32 xb, int32 yb, int32 xe, int32 ye, int32 c1, int32 c2, uint8 mask, uint8 fx)
{
	int32 x=xb<<16,y=yb<<16;
	int32 dx,dy,d,x1,y1;
	int32 cutb=0, cute=0;

  dx=xe-xb; dy=ye-yb;

  if (dx==0 && dy==0) return;

	x1=abs(dx); y1=abs(dy);
  if (x1>y1)
  {
  	d=abs(dx)+1;
  	dy=(dy<<16)/x1;
    dx=(dx<<16)/x1;
  }
  else
  {
  	d=abs(dy)+1;
    dx=(dx<<16)/y1;
  	dy=(dy<<16)/y1;
  }

	if (dx>0)  // clamp x
	{
		if (x<(c_minx<<16)) { cutb=MAX(((c_minx<<16)-x)/dx,cutb); }
		if (x+dx*d>(c_maxx<<16)-1) { cute=MAX((x+dx*d-(c_maxx<<16)+1)/dx, cute); }
	}
	else if (dx<0)
	{
		if (x>(c_maxx<<16)-1) { cutb=MAX((x-(c_maxx<<16)+1)/dx, cutb); }
		if (x+dx*d<(c_minx<<16)) { cute=MAX(((c_minx<<16)-x-dx*d)/dx, cute); }
	}
	else if (x>>16<c_minx || x>>16>=c_maxx)  return;

//  ik_putpixel(img, x>>16,y>>16,c1); 


	if (dy>0)  // clamp y
	{
		if (y<(c_minx<<16)) { cutb=MAX(((c_miny<<16)-y)/dy,cutb); }
		if (y+dy*d>(c_maxy<<16)-1) { cute=MAX((y+dy*d-(c_maxy<<16)+1)/dy, cute); }
	}
	else if (dy<0)
	{
		if (y>(c_maxy<<16)-1) { cutb=MAX((y-(c_maxy<<16)+1)/dy, cutb); }
		if (y+dy*d<(c_miny<<16)) { cute=MAX(((c_miny<<16)-y-dy*d)/dy, cute); }
	}
	else if (y>>16<c_miny || y>>16>=c_maxy)  return;

	if (cutb) { x+=dx*cutb; y+=dy*cutb; d-=cutb; }
	if (cute) { d-=cute; }

	if (d<0) d=0;
  while (d--)
  {
  	if ( (1<<(d&7)) & mask )  // check mask
    {
  		x1=x>>16;y1=y>>16;
	    if (x1>=c_minx && y1>=c_miny && x1<c_maxx && y1<c_maxy)
				if (!fx)
					ik_putpixel(img, x1,y1,c1); 
				else
					ik_putpixel_add(img, x1,y1,c1); 
   	}
    else if (c2>0)
    {
  		x1=x>>16;y1=y>>16;
	    if (x1>=c_minx && y1>=c_miny && x1<c_maxx && y1<c_maxy)
				if (!fx)
					ik_putpixel(img, x1,y1,c2); 
				else
					ik_putpixel_add(img, x1,y1,c2); 
   	}

  	x+=dx; y+=dy;
  }
}

void ik_drawbox(t_ik_image *img, int32 xb, int32 yb, int32 xe, int32 ye, int32 c)
{
	int32 y;
	int32 w;
	uint8 *p;

	if (xe < xb) { w = xe; xe = xb; xb = w; }
	if (ye < yb) { w = ye; ye = yb; yb = w; }

	y = MAX(yb, c_miny);
	p = img->data + y*img->pitch + MAX(xb, c_minx);
	w = MIN(xe+1, c_maxx) - MAX(xb, c_minx);

	while (y < MIN(ye+1,c_maxy))
	{
		memset(p, c, w);
		y++; p += img->pitch;
	}

	/*
	for (y=MAX(yb,c_miny); y<MIN(ye+1,c_maxy); y++)
		for (x=MAX(xb,c_minx); x<MIN(xe+1,c_maxx); x++)
		{
			ik_putpixel(img, x, y, c);
		}
	*/
}

void ik_copybox(t_ik_image *src, t_ik_image *dst, int32 xb, int32 yb, int32 xe, int32 ye, int32 xd, int32 yd)
{
	int32 y;

	for (y=0;y<ye-yb;y++)
		memcpy(ik_image_pointer(dst,xd,y+yd),ik_image_pointer(src,xb,y+yb),xe-xb);
}

void ik_drawmeter(t_ik_image *img, int32 xb, int32 yb, int32 xe, int32 ye, int32 typ, int32 val, int32 c, int32 c2)
{
	ik_drawbox(img, xb, yb, xe, ye, c*16+12);
	ik_drawbox(img, xb+1, yb+1, xe-1, ye-1, c*16+1);
	if (typ==0) // vert
	{
		val = (val * (ye-yb-2)) / 100;
		ik_drawbox(img, xb+1, ye-1-val, xe-1, ye-1, c2);
	}
	else // horiz
	{
		val = (val * (xe-xb-2)) / 100;
		ik_drawbox(img, xb+1, yb+1, xb+1+val, ye-1, c2);
	}
}

// FIND RGB COLOR

int32 get_rgb_color(int32 r, int32 g, int32 b)
{
	int32 c,e,ee,x,r1,g1,b1;
	int32 c0;

	c=0;ee=200000;
	for (x=0;x<256;x++)
	{
		c0=get_palette_entry(x);
		r1=r-((c0>>16)&255); 
		g1=g-((c0>>8)&255); 
		b1=b-(c0&255);
		e=r1*r1+g1*g1+b1*b1;
		if (e<ee)
		{ c=x; ee=e; }
	}
	
	return c;
}

// CALCULATE COLOR TABLES

void calc_color_tables(uint8 *pal)
{
	int32 x,y;
	FILE *colormap;

	gfx_addbuffer=(unsigned char*)malloc(65536);
	gfx_transbuffer=(unsigned char*)malloc(65536);
	gfx_lightbuffer=(unsigned char*)malloc(65536);

	if (gfx_transbuffer==NULL || gfx_lightbuffer==NULL || gfx_addbuffer==NULL)
		return;  // fail

	colormap=myopen("graphics/colormap.dat", "rb");
	if (colormap)
	{
		fread(gfx_transbuffer, 1, 65536, colormap);
		fread(gfx_lightbuffer, 1, 65536, colormap);
		fread(gfx_addbuffer, 1, 65536, colormap);
		fclose(colormap);
		return;
	}

	for (y=0;y<256;y++)
		for (x=0;x<256;x++)
		{			
			gfx_transbuffer[y*256+x]=get_rgb_color( ((int32)pal[y*3]+pal[x*3])>>1,
																							((int32)pal[y*3+1]+pal[x*3+1])>>1,
																							((int32)pal[y*3+2]+pal[x*3+2])>>1 );
			gfx_lightbuffer[y*256+x]=get_rgb_color( ((int32)pal[y*3]*pal[x*3])>>8,
																							((int32)pal[y*3+1]*pal[x*3+1])>>8,
																							((int32)pal[y*3+2]*pal[x*3+2])>>8 );
			gfx_addbuffer[y*256+x]=get_rgb_color( MIN((int32)pal[y*3]+pal[x*3],255),
																						MIN((int32)pal[y*3+1]+pal[x*3+1],255),
																						MIN((int32)pal[y*3+2]+pal[x*3+2],255) );
		}

	colormap=myopen("graphics/colormap.dat","wb");

	fwrite(gfx_transbuffer, 1, 65536, colormap);
	fwrite(gfx_lightbuffer, 1, 65536, colormap);
	fwrite(gfx_addbuffer, 1, 65536, colormap);

	fclose(colormap);
}

void del_color_tables()
{
	if (gfx_transbuffer)  free(gfx_transbuffer);
	if (gfx_lightbuffer)  free(gfx_lightbuffer);
	if (gfx_addbuffer)  free(gfx_addbuffer);
}

// GENERATE OR LOAD IMAGE STRUCTS

t_ik_image *new_image(int32 w, int32 h)
{
	t_ik_image *img;

	img=(t_ik_image*)calloc(1, sizeof(t_ik_image));
	if (!img)
	{
		return NULL;
	}

	img->w=w;
	img->h=h;
	img->pitch=w;

	//calloc
	img->data=(uint8*)calloc(w*h, sizeof(uint8));
	if (!img->data)
	{
		free(img);
		return NULL;
	}

	return img;
}

void del_image(t_ik_image *img)
{
	if (!img)
		return;

	if (img->data)
		free(img->data);

	free(img);
}

t_ik_image *ik_load_pcx(char *fname, uint8 *pal)
{
	int32 x,y,po;
	int32 c,ch;
	uint16 img_w, img_h, line_w;
	uint8 bpp;

	t_ik_image *image;
	uint8 *buffer;
	uint8 *line;

	FILE *img;

	img=myopen(fname, "rb");
	if (!img)
		return NULL;

	// load header
	fgetc(img);  // manuf. id
	fgetc(img);  // version
  fgetc(img);  // encoding

	bpp=fgetc(img);
	// 4	

	img_w=-fgetc(img);  // xmin
	img_w-=fgetc(img)<<8;
	img_h=-fgetc(img);  // ymin
	img_h-=fgetc(img)<<8;
	img_w+=1+fgetc(img);  // xmax    (width is xmax-xmin)
	img_w+=fgetc(img)<<8;
	img_h+=1+fgetc(img);  // ymax
	img_h+=fgetc(img)<<8;
	// 12

	fgetc(img); // hdpi
	fgetc(img);
	fgetc(img); // vdpi
	fgetc(img);
	// 16

	for (x=0;x<48;x++)
		fgetc(img);
	// 64

	fgetc(img);
	bpp=fgetc(img)*8;

	line_w=fgetc(img);
	line_w+=fgetc(img)<<8;

	for (x=0;x<60;x++)
		fgetc(img);
	// 128

	if (bpp!=8)  // can't load non-8bit pcx files ... use tga
	{
		fclose(img);
		return NULL;
	}

	// read palette from the end
	if (pal)
	{
		fseek(img, -768, SEEK_END);
		fread(pal, 1, 768, img);
	}

	// allocate buffer for the image
	image=new_image(img_w, img_h);
	if (!image)
	{
		fclose(img);	
		return NULL;
	}

	buffer=image->data;
	line=(uint8*)malloc(line_w);

	// read image data
	fseek(img, 128, SEEK_SET);

	for (y=0;y<img_h;y++)
	{
		x=0;
		while (x<line_w)
		{
			ch=fgetc(img);
			if (ch >= 0xc0)
			{
				c=ch & 0x3f;
				ch=fgetc(img);
			}
			else
				c=1;

			while (c>0 && x<line_w)
			{
				line[x++]=ch;
				c--;
			}			
		}

		po=y*img_w;
		for (x=0;x<line_w;x++)
			image->data[po++]=line[x];
	}

	fclose(img);

	free(line);
	
	return image;					
}

t_ik_image *ik_load_tga(char *fname, uint8 *pal) 
{
	t_ik_image *img;
	FILE *fil;
	int p;
	uint8 hdr[18];
	
	fil = fopen(fname, "rb");
	if (!fil) return NULL;

	fread(hdr, 1, 18, fil);
	p = 1;
	if (hdr[1] != 1) p = 0;
	if (hdr[2] != 1) p = 0;
	if (hdr[7] != 24) p = 0;
	if (hdr[16] != 8) p = 0;
//	if (hdr[17] != 8) p = 0;

	if (!p)
	{
		fclose(fil);
		printf("ERROR: Bad TGA format %s", fname);
		return NULL;
	}

	// read palette
	if (pal)
	{
		for (p = 0; p < 256; p++)
		{
			pal[p*3+2] = fgetc(fil);
			pal[p*3+1] = fgetc(fil);
			pal[p*3] = fgetc(fil);
		}
	}
	else
		fseek(fil, 768, SEEK_CUR);

	// read image data
	img = new_image(hdr[13]*256+hdr[12], hdr[15]*256+hdr[14]);
	if (!img)
	{ fclose(fil);	return NULL; }

	for (p = img->h; p > 0; p--)
	{
		fread(img->data + (p-1)*img->pitch, 1, img->w, fil);
	}

	fclose(fil);

	return img;
}


void ik_save_screenshot(t_ik_image *img, uint8 *pal)
{
	int n;
	FILE *fil;
	char fname[32];
	
	wants_screenshot = 0;

#ifdef MOVIE
	n = aframe;
	aframe++;
	sprintf(fname, "frames/fram%04d.tga", n);
	ik_save_tga(fname, img, pal);
#else
	
	n=0;
	while (n<1000)
	{
		sprintf(fname, "shot%04d.tga", n);
		fil = myopen(fname, "rb");
		if (!fil)
		{
			ik_save_tga(fname, img, pal);
			break;
		}
		else fclose(fil);

		n++;
	}
#endif
}

void ik_save_tga(char *fname, t_ik_image *img, uint8 *pal)
{
	int p;
	uint8 hdr[18] = {
		0, 1, 1,								// id_len, pal_type, img_type
		0, 0, 0, 1,	24,					// first_color, num_colors, pal_size
		0, 0, 0, 0,							// left, top
		(uint8)(img->w&255), (uint8)(img->w>>8),	// width
		(uint8)(img->h&255), (uint8)(img->h>>8),	// height
		8, 8										// bpp, des_bits
		};
	FILE *fil;

	fil = myopen(fname, "wb");
	if (!fil)
		return;

	// write header
	fwrite(hdr, 1, 18, fil);

	// write palette
	for (p = 0; p < 256; p++)
	{
		fputc(get_palette_entry(p)&255, fil);
		fputc((get_palette_entry(p)>>8)&255, fil);
		fputc(get_palette_entry(p)>>16, fil);
	}

	// write data
	for (p = img->h; p > 0; p--)
	{
		fwrite(img->data + (p-1)*img->pitch, 1, img->w, fil);
	}

	fclose(fil);

}

int get_direction(int32 dx, int32 dy)
{
	int32 a;

	if (dx==0 && dy==0)
		return 0;

	a = (int32)(atan2((double)dx, (double)dy) * 512 / 3.14159);
	a = (a + 1024) & 1023;

	return a;
}

int get_distance(int32 dx, int32 dy)
{
	int32 r;

	if (dx==0 && dy==0)
		return 0;

	r = (int32)sqrt((double)dx*dx + dy*dy);

	return r;
}

void halfbritescreen()
{
	int32 x, y;
	int32 l;
	uint8 *po;

	if (num_dims >= 8)
		return;

	prep_screen();
	dims[num_dims] = new_image(screen->w, screen->h);
	if (!dims[num_dims])
	{	free_screen(); return; }

	ik_copybox(screen, dims[num_dims], 0, 0, screen->w, screen->h, 0, 0);
	num_dims++;

	if (num_dims > 1)
		l = 15;
	else
		l = 11;

	l<<=8;

	for (y = 0; y < screen->h; y++)
	{
		po = screen->data + screen->pitch*y;
		for (x = screen->w; x; x--)
		{	
			*po = gfx_lightbuffer[l+*po];
			po++;
		}
	}
	free_screen();
}

void reshalfbritescreen()
{
	if (num_dims <= 0)
		return;

	prep_screen();

	num_dims--;
	ik_copybox(dims[num_dims], screen, 0, 0, screen->w, screen->h, 0, 0);
	del_image(dims[num_dims]);
	dims[num_dims]=NULL;

	free_screen();
}

void resallhalfbritescreens()
{
	if (num_dims <= 0)
		return;

	prep_screen();

	while (num_dims)
	{
		num_dims--;
		ik_copybox(dims[num_dims], screen, 0, 0, screen->w, screen->h, 0, 0);
		del_image(dims[num_dims]);
		dims[num_dims]=NULL;
	}
	free_screen();
}

extern t_ik_spritepak *spr_IFbutton;

void ik_draw_mousecursor()
{
	ik_dsprite(screen, ik_mouse_x, ik_mouse_y, spr_IFbutton->spr[2], 0);
}

extern t_ik_spritepak *spr_SMraces;

void gfx_blarg()
{
	ik_dsprite(screen, 564, 448, spr_SMraces->spr[7], 0);
}

void gfx_initmagnifier()
{
	int x, y, r;
#ifndef THICK_MAGNIFIER
	int p;
#endif

	magni = new_image(128, 128);
	for (y = 0; y < 128; y++)
		for (x = 0; x < 128; x++)
		{
			r = (int)sqrt (double (y+y-127)*(y+y-127) + (x+x-127)*(x+x-127) );
#ifdef THICK_MAGNIFIER
			if (r < 124)
				magni->data[y*128+x] = 1;
			else if (r < 128)
				magni->data[y*128+x] = 2;
			else 
				magni->data[y*128+x] = 0;
#else
			if (r < 124)
				magni->data[y*128+x] = 1;
			else 
				magni->data[y*128+x] = 0;
#endif
		}
#ifndef THICK_MAGNIFIER
	p = 0;
	for (y = 0; y < 128; y++)
		for (x = 0; x < 128; x++)
		{
			if (!magni->data[p])
			{
				r = 0;
				if (y > 0)
					if (magni->data[p-128] == 1)
						r++;
				if (y < 127)
					if (magni->data[p+128] == 1)
						r++;
				if (x > 0)
					if (magni->data[p-1] == 1)
						r++;
				if (x < 127)
					if (magni->data[p+1] == 1)
						r++;
				if (r)
					magni->data[p]=2;
			}
			p++;
		}
#endif
}

void gfx_deinitmagnifier()
{
	del_image(magni);
}

void gfx_magnify()
{
	t_ik_sprite *mag;
	int  y;	// x
	unsigned char *p;
	//unsigned char *m;

//	mag = get_sprite(screen, ik_mouse_x-64, ik_mouse_y-64, 128, 128);
	mag = get_sprite(screen, ik_mouse_x-96, ik_mouse_y-48, 192, 96);
	p = mag->data;
	y = mag->h * mag->w;
	while (y--)
	{
		if (!*p)
			*p = 16;
		p++;
	}
/*
	p = mag->data;
	m = magni->data;
	for (y = 0; y < mag->h; y++)
		for (x = 0; x < mag->w; x++)
		{
			//if (y == 0 || x == 0 || y == mag->h-1 || x == mag->w-1)
			//	*p = 178;
			//else

			if (*m == 1)
			{
				if (!*p)
					*p = 16;
			}
			else if (*m == 2)
			{
				*p = 178;
			}
			else
				*p = *m;
			
			p++; m++;
		}

	ik_drsprite(screen, ik_mouse_x+1, ik_mouse_y+1, 0, 256, mag, 0);
	*/
	ik_drsprite(screen, ik_mouse_x+1, ik_mouse_y+1, 0, 384, mag, 0);
	interface_thinborder(screen, ik_mouse_x-192, ik_mouse_y-96, ik_mouse_x+192, ik_mouse_y+96, 11, -1);
	/*
	ik_drawline(screen, ik_mouse_x-192, ik_mouse_y-96, ik_mouse_x+191, ik_mouse_y-96, 178, 0, 255, 0);
	ik_drawline(screen, ik_mouse_x-192, ik_mouse_y+95, ik_mouse_x+191, ik_mouse_y+95, 178, 0, 255, 0);
	ik_drawline(screen, ik_mouse_x-192, ik_mouse_y-96, ik_mouse_x-192, ik_mouse_y+95, 178, 0, 255, 0);
	ik_drawline(screen, ik_mouse_x+191, ik_mouse_y-96, ik_mouse_x+191, ik_mouse_y+95, 178, 0, 255, 0);
	*/
	free_sprite(mag);
}
// ----------------
//     INCLUDES
// ----------------

#include <stdlib.h>
#include <stdio.h>
#include <string.h>

#include "typedefs.h"
#include "iface_globals.h"
#include "is_fileio.h"
#include "gfx.h"
#include "snd.h"
#include "textstr.h"

#include "interface.h"

// ----------------
//     CONSTANTS
// ----------------

// ----------------
// GLOBAL VARIABLES
// ----------------

t_ik_spritepak *spr_IFborder;
t_ik_spritepak *spr_IFbutton;
t_ik_spritepak *spr_IFslider;
t_ik_spritepak *spr_IFarrows;
t_ik_spritepak *spr_IFsystem;
t_ik_spritepak *spr_IFtarget;
t_ik_spritepak *spr_IFdifnebula;
t_ik_spritepak *spr_IFdifenemy;


t_ik_font			 *font_4x8;
t_ik_font			 *font_6x8;

int tut_seen[tut_max];

// ----------------
// LOCAL PROTOTYPES
// ----------------

void interface_initsprites();
void interface_deinitsprites();

// ----------------
// GLOBAL FUNCTIONS
// ----------------

void interface_init()
{
	interface_initsprites();
}

void interface_deinit()
{
	interface_deinitsprites();
}

void interface_drawborder(t_ik_image *img, 
													int32 left, int32 top, int32 right, int32 bottom,
													int32 fill, int32 color,
													char *title)
{
	int32 x, y;
	int32 flag;
	char title2[128];

	strcpy(title2, title);
	y = strlen(title2);
	for (x = 0; x < y; x++)
	{	
		if (title2[x] >= 'a' && title2[x] <= 'z')
			title2[x] -= ('a' - 'A');
	}

	if (!color)
		flag = 0;
	else
		flag = 2 + (color << 8);

	if (fill) fill=9;

	for (y = top + 24; y < bottom - 16; y += 16)
	{
		if (fill)
			for (x = left + 32; x < right - 32; x += 32)
				ik_dsprite(img, x, y, spr_IFborder->spr[13], flag);

		ik_dsprite(img, left, y, spr_IFborder->spr[3 + fill], flag);
		ik_dsprite(img, right-32, y, spr_IFborder->spr[5 + fill], flag);
	}
	for (x = left+32; x < right - 32; x += 32)
	{
		ik_dsprite(img, x, top, spr_IFborder->spr[1 + fill], flag);
		ik_dsprite(img, x, bottom-16, spr_IFborder->spr[7 + fill], flag);
	}
	ik_dsprite(img, left, top,								spr_IFborder->spr[fill], flag);
	ik_dsprite(img, right - 32, top,					spr_IFborder->spr[2 + fill], flag);
	ik_dsprite(img, left, bottom - 16,				spr_IFborder->spr[6 + fill], flag);
	ik_dsprite(img, right - 32, bottom - 16,	spr_IFborder->spr[8 + fill], flag);

	if (title)
	{
		ik_print(img, font_6x8, left+16, top+6, color, title2);
	}
}

void interface_thinborder(t_ik_image *img, 
													int32 left, int32 top, int32 right, int32 bottom,
													int32 color, int32 fill)
{
	int32 x, y;
	int32 flag;

	if (!color)
		flag = 0;
	else
		flag = 2 + (color << 8);

	if (fill > -1) 
		ik_drawbox(img, left, top, right-1, bottom-1, fill);

	for (y = top + 8; y < bottom - 8; y += 8)
	{
		ik_dsprite(img, left, y, spr_IFborder->spr[IF_BORDER_SMALL + 3], flag);
		ik_dsprite(img, right-8, y, spr_IFborder->spr[IF_BORDER_SMALL + 5], flag);
	}
	for (x = left+8; x < right - 8; x += 8)
	{
		ik_dsprite(img, x, top, spr_IFborder->spr[IF_BORDER_SMALL + 1], flag);
		ik_dsprite(img, x, bottom-8, spr_IFborder->spr[IF_BORDER_SMALL + 7], flag);
	}
	ik_dsprite(img, left, top,							spr_IFborder->spr[IF_BORDER_SMALL], flag);
	ik_dsprite(img, right - 8, top,					spr_IFborder->spr[IF_BORDER_SMALL + 2], flag);
	ik_dsprite(img, left, bottom - 8,				spr_IFborder->spr[IF_BORDER_SMALL + 6], flag);
	ik_dsprite(img, right - 8, bottom - 8,	spr_IFborder->spr[IF_BORDER_SMALL + 8], flag);
}

void interface_drawslider(t_ik_image *img, int32 left, int32 top, int32 a, int32 l, int32 rng, int32 val, int32 color)
{
	int32 x,y ;

	if (a)	// vertical
	{
		for (y = top + 8; y < top + l - 8; y+=8)
			ik_dsprite(img, left, y, spr_IFslider->spr[5], 2+(color<<8));
		ik_dsprite(img, left, top, spr_IFslider->spr[4], 2+(color<<8));
		ik_dsprite(img, left, top + l - 8, spr_IFslider->spr[6], 2+(color<<8));
		ik_dsprite(img, left, (top * (rng-val) + (top + l - 8) * val) / rng, spr_IFslider->spr[7], 2+(color<<8));
	}
	else		// horizontal
	{
		for (x = left + 8; x < left + l - 8; x+=8)
			ik_dsprite(img, x, top, spr_IFslider->spr[1], 2+(color<<8));
		ik_dsprite(img, left, top, spr_IFslider->spr[0], 2+(color<<8));
		ik_dsprite(img, left + l - 8, top, spr_IFslider->spr[2], 2+(color<<8));
		ik_dsprite(img, (left * (rng-val) + (left + l - 8) * val) / rng, top, spr_IFslider->spr[3], 2+(color<<8));
	}

}

void interface_drawbutton(t_ik_image *img, int32 left, int32 top, int32 l, int32 color, char *text)
{
	int32 x;

	for (x = left + 16; x < left + l - 16; x+=16)
		ik_dsprite(img, x, top, spr_IFbutton->spr[5], 2+(color<<8));
	ik_dsprite(img, left, top, spr_IFbutton->spr[4], 2+(color<<8));
	ik_dsprite(img, left + l - 16, top, spr_IFbutton->spr[6], 2+(color<<8));
	ik_print(img, font_6x8, left + l/2 - strlen(text)*3, top + 5, color, text);
}

int32 interface_textbox(t_ik_image *img, t_ik_font *fnt,
											 int32 left, int32 top, int32 w, int32 h,
											 int32 color, 
											 char *text)
{
	char *txb;
	char *txp;
	char *spc;
	int32 x, y;
	int32 c, l;
	char lne[80];

	txb = text; 
	y = top;
	l = strlen(text);
	c = 0;
	while ( y <= top + h - fnt->h && c < l)
	{
		spc = txp = txb; x = 0;
		while ( x <= w && c < l)
		{
			if (*txp <= ' ' || *txp == '|')
			{
				spc = txp;
			}
			if (*txp == 0)
			{	c = l; break; }
			else
			{
				if (*txp == '|')
					x = w;
				x += fnt->w;
				txp++;
				c++;
			}
		}
		if (c >= l)
		{
			strncpy(lne, txb, txp-txb);
			lne[txp-txb]=0;
		}
		else if (spc > txb)
		{
			strncpy(lne, txb, spc-txb);
			lne[spc-txb]=0;
			c-=(spc-txb)-1;
			txb = spc+1;
		}
		else
		{
			strncpy(lne, txb, txp-txb);
			lne[txp-txb]=0;
			txb = txp;
		}
		for (x = 0; x < (int)strlen(lne); x++)
			if (lne[x] == '|')  
				lne[x] = ' ';
		ik_print(img, fnt, left, y, color, lne);
//		ik_print(img, fnt, left-8, y, color, "%d", (y-top)/fnt->h);

		y += fnt->h;
	}
//	ik_print(img, fnt, left, top, 2, "LEN: %d %d", l, c);
	return (y - top)/fnt->h;
}

int32 interface_textboxsize(t_ik_font *fnt,
														int32 w, int32 h,
														char *text)
{
	char *txb;
	char *txp;
	char *spc;
	int32 x, y;
	int32 c, l;
	char lne[80];

	txb = text; 
	y = 0;
	l = strlen(text);
	c = 0;
	while ( y*fnt->h <= h - fnt->h && c < l)
	{
		spc = txp = txb; x = 0;
		while ( x <= w && c < l)
		{
			if (*txp <= ' ' || *txp == '|')
			{
				spc = txp;
			}
			if (*txp == 0)
			{	c = l; break; }
			else
			{
				if (*txp == '|')
					x = w;
				x += fnt->w;
				txp++;
				c++;
			}
		}
		if (c >= l)
		{
			strncpy(lne, txb, txp-txb);
			lne[txp-txb]=0;
		}
		else if (spc > txb)
		{
			strncpy(lne, txb, spc-txb);
			lne[spc-txb]=0;
			c-=(spc-txb)-1;
			txb = spc+1;
		}
		else
		{
			strncpy(lne, txb, txp-txb);
			lne[txp-txb]=0;
			txb = txp;
		}

		y++;
	}
	return y;
}

int32 interface_popup(t_ik_font *fnt, 
										 int32 left, int32 top, int32 w, int32 h,
										 int32 co1, int32 co2, 
										 char *label, char *text, char *button1, char *button2, char *button3)
{
	int32 mc, c;
	int32 end = 0;
	int32 bl = 0;
	int32 t=0;
	t_ik_sprite *bg;
	int bc[3];

	bc[0] = bc[1] = bc[2] = 0;

	if (button1)
	{	bl = MAX((int32)strlen(button1), bl); bc[0] = button1[0]; }
	if (button2)
	{	bl = MAX((int32)strlen(button2), bl); bc[1] = button2[0]; }
	if (button3)
	{	bl = MAX((int32)strlen(button3), bl); bc[2] = button3[0]; }

	
	for (c = 0; c < 3; c++)
	{
		if (bc[c] >= 'a' && bc[c] <='z')
			bc[c] += 'A'-'a';
	}

	if (bl)
	{
		bl = bl * 6 + 16;
		bl = bl & 0xf8;
		if (bl<32) bl=32;
	}

	if (!h)
	{
		h = interface_textboxsize(fnt, w - 32, 256, text)*8 + 32 + 16*(bl>0);
	}

	if (left == -1)
	{
		left = screen->w/2 - w/2;
	}

	if (top == -1)
	{
		top = (screen->h * 2)/5 - h/2;
	}

	prep_screen();

	bg = get_sprite(screen, left, top, w, h);

	interface_drawborder(screen,
											 left, top, left+w, top+h,
											 1, co1, label);

	interface_textbox(screen, fnt,
										left+16, top+24, w - 32, h - 32 - (bl>0)*16, co2,
										text);

	if (button1)
		interface_drawbutton(screen, left+w-16-bl, top + h - 24, bl, co1, button1);
	if (button2)
		interface_drawbutton(screen, left+16, top + h - 24, bl, co1, button2);
	if (button3)
		interface_drawbutton(screen, left+w/2-bl/2, top + h - 24, bl, co1, button3);

	ik_blit();

	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		mc = ik_mclick();	
		c = ik_inkey();

		if (c >= 'a' && c <= 'z')
			c += 'A' - 'a';

		if (!button1)
		{
			if (c == 13 || mc == 1)
				end = 1;
		}
		else
		{
			if (c == bc[0])
				end = 1;

			if (!button2)
			{
				if (c == 13)
					end = 1;
			}
			else
				if (c == bc[1])
					end = 2;

			if (mc == 1 && ik_mouse_y > top+h-24 && ik_mouse_y < top+h-8)
			{
				if (ik_mouse_x > left+w-16-bl && ik_mouse_x < left+w-16)
					end = 1;
				if (button2)
				{
					if (ik_mouse_x > left+16 && ik_mouse_x < left+16+bl)
						end = 2;
				}
			}
		}	

		c = t; t = get_ik_timer(2);
		if (t != c)
		{ prep_screen(); ik_blit(); }
	}

	if (button1 && button2)	// yes/no
	{
		if (end == 1)
			Play_SoundFX(WAV_YES, get_ik_timer(0));
		else if (end == 2)
			Play_SoundFX(WAV_NO, get_ik_timer(0));
	}
	else
		Play_SoundFX(WAV_DOT, get_ik_timer(0));

	prep_screen();
	ik_dsprite(screen, left, top, bg, 4);
	ik_blit();
	free_sprite(bg);

	if (must_quit)
	{
		must_quit = 0;
		end = 2;
	}

	return end-1;
}

// ----------------
// LOCAL FUNCTIONS
// ----------------

void interface_initsprites()
{
	t_ik_image *pcx;	
	int x, y, n;

	font_4x8 = ik_load_font("graphics/fnt2.pcx", 4, 8);
	font_6x8 = ik_load_font("graphics/fnt3.pcx", 6, 8);

	spr_IFborder = load_sprites("graphics/ifborder.spr");
	spr_IFbutton = load_sprites("graphics/ifbutton.spr");
	spr_IFslider = load_sprites("graphics/ifslider.spr");
	spr_IFarrows = load_sprites("graphics/ifarrows.spr");
	spr_IFsystem = load_sprites("graphics/ifsystem.spr");
	spr_IFtarget = load_sprites("graphics/iftarget.spr");
	spr_IFdifnebula = load_sprites("graphics/ifdifneb.spr");
	spr_IFdifenemy = load_sprites("graphics/ifdifnmy.spr");

	pcx = NULL;

	if (!spr_IFborder)
	{
		pcx = ik_load_pcx("interfce.pcx", NULL);
		spr_IFborder = new_spritepak(30);
		for (n=0;n<3;n++)
		{
			spr_IFborder->spr[n] = get_sprite(pcx, n*32, 0, 32, 24);
		}
		for (n=0;n<6;n++)
		{
			x = n%3; y = n/3;
			spr_IFborder->spr[n+3] = get_sprite(pcx, x*32, y*16+24, 32, 16);
		}
		for (n=0;n<3;n++)
		{
			spr_IFborder->spr[n+9] = get_sprite(pcx, n*32, 56, 32, 24);
		}
		for (n=0;n<6;n++)
		{
			x = n%3; y = n/3;
			spr_IFborder->spr[n+12] = get_sprite(pcx, x*32, y*16+80, 32, 16);
		}

		spr_IFborder->spr[18] = get_sprite(pcx, 96, 0, 64, 64);
		spr_IFborder->spr[19] = get_sprite(pcx, 192, 0,128,128);
		spr_IFborder->spr[20] = get_sprite(pcx, 96, 64,64,32);

		for (n=0; n<9; n++)
		{
			spr_IFborder->spr[n+21] = get_sprite(pcx, 160, n*8, 8, 8);
		}

		save_sprites("graphics/ifborder.spr", spr_IFborder);
	}

	if (!spr_IFdifnebula)
	{
		if (!pcx)
			pcx = ik_load_pcx("interfce.pcx", NULL);
		spr_IFdifnebula = new_spritepak(3);
		for (n=0;n<3;n++)
		{
			spr_IFdifnebula->spr[n] = get_sprite(pcx, n*64, 192, 64, 64);
		}

		save_sprites("graphics/ifdifneb.spr", spr_IFdifnebula);
	}

	if (!spr_IFdifenemy)
	{
		if (!pcx)
			pcx = ik_load_pcx("interfce.pcx", NULL);
		spr_IFdifenemy = new_spritepak(6);
		for (n=0;n<3;n++)
		{
			spr_IFdifenemy->spr[n] = get_sprite(pcx, (n&1)*64+192, 192+(n/2)*32, 64, 32);
			spr_IFdifenemy->spr[n+3] = get_sprite(pcx, n*64, 256, 64, 32);
		}

		save_sprites("graphics/ifdifnmy.spr", spr_IFdifenemy);
	}

	if (!spr_IFbutton)
	{
		if (!pcx)
			pcx = ik_load_pcx("interfce.pcx", NULL);
		spr_IFbutton = new_spritepak(22);
		for (n=0;n<7;n++)
		{
			spr_IFbutton->spr[n] = get_sprite(pcx, n*16, 128, 16, 16);
		}
		for (n=0;n<4;n++)
		{
			spr_IFbutton->spr[n+7] = get_sprite(pcx, n*8+112, 128, 8, 16);
			spr_IFbutton->spr[n+11] = get_sprite(pcx, n*32+176, 128, 32, 16);
		}
		spr_IFbutton->spr[15] = get_sprite(pcx, 304, 128, 16, 8);
		spr_IFbutton->spr[16] = get_sprite(pcx, 0, 176, 32, 16);
		spr_IFbutton->spr[17] = get_sprite(pcx, 144, 128, 16, 16);
		spr_IFbutton->spr[18] = get_sprite(pcx, 160, 128, 16, 16);
		spr_IFbutton->spr[19] = get_sprite(pcx, 168, 0, 24, 64);
		spr_IFbutton->spr[20] = get_sprite(pcx, 256, 224, 32, 16);
		spr_IFbutton->spr[21] = get_sprite(pcx, 288, 224, 32, 16);

		save_sprites("graphics/ifbutton.spr", spr_IFbutton);
	}

	if (!spr_IFslider)
	{
		if (!pcx)
			pcx = ik_load_pcx("interfce.pcx", NULL);
		spr_IFslider = new_spritepak(10);
		for (n=0;n<10;n++)
		{
			spr_IFslider->spr[n] = get_sprite(pcx, n*8, 112, 8, 8);
		}

		save_sprites("graphics/ifslider.spr", spr_IFslider);
	}

	if (!spr_IFarrows)
	{
		if (!pcx)
			pcx = ik_load_pcx("interfce.pcx", NULL);
		spr_IFarrows = new_spritepak(16);
		for (n=0;n<16;n++)
		{
			spr_IFarrows->spr[n] = get_sprite(pcx, n*8, 120, 8, 8);
		}

		save_sprites("graphics/ifarrows.spr", spr_IFarrows);
	}

	if (!spr_IFsystem)
	{
		if (!pcx)
			pcx = ik_load_pcx("interfce.pcx", NULL);
		spr_IFsystem = new_spritepak(17);
		for (n=0;n<17;n++)
		{
			spr_IFsystem->spr[n] = get_sprite(pcx, n*16, 144, 16, 16);
		}

		save_sprites("graphics/ifsystem.spr", spr_IFsystem);
	}

	if (!spr_IFtarget)
	{
		if (!pcx)
			pcx = ik_load_pcx("interfce.pcx", NULL);
		spr_IFtarget = new_spritepak(10);
		for (n=0;n<9;n++)
		{
			spr_IFtarget->spr[n] = get_sprite(pcx, n*16, 160, 16, 16);
		}
		spr_IFtarget->spr[9] = get_sprite(pcx, 144, 160, 32, 32);

		save_sprites("graphics/iftarget.spr", spr_IFtarget);
	}

	if (pcx)
		del_image(pcx);
}

void interface_deinitsprites()
{
	free_spritepak(spr_IFborder);
	free_spritepak(spr_IFbutton);
	free_spritepak(spr_IFslider);
	free_spritepak(spr_IFarrows);
	free_spritepak(spr_IFsystem);
	free_spritepak(spr_IFtarget);
	free_spritepak(spr_IFdifnebula);
	free_spritepak(spr_IFdifenemy);

	ik_del_font(font_6x8);
	ik_del_font(font_4x8);
}

void interface_cleartuts()
{
	int i;

	for (i = 0; i < tut_max; i++)
		tut_seen[i] = 0;
}

void interface_tutorial(int n)
{
	int r;

	if (tut_seen[n])
		return;

	r = interface_popup(font_6x8, -1, -1, 288, 0, 12, 0, 
			textstring[STR_TUT_TSTARMAP + n],
			textstring[STR_TUT_STARMAP + n], 
			textstring[STR_OK], textstring[STR_TUT_END]);

	if (r)
		settings.random_names -= (settings.random_names & 4);
	/*
	switch(n)
	{
		case tut_starmap:
			interface_popup(font_6x8, -1, -1, 288, 0, 11, 0, 
				"Tutorial: starmap",
				"Your starship is located near the bottom of the screen, in the Glory star system. Left click any other star, and a green STAR LANE will appear. Try to make sure that the star lane does not pass through obstacles like a nebula or black hole. When you are ready to travel to the selected star, left click on the word ENGAGE. You can also press F1 at any time to view a comprehensive HELP SCREEN.", 
				"OK");
		break;

		case tut_explore:
			interface_popup(font_6x8, -1, -1, 288, 0, 11, 0, 
				"Tutorial: planetary exploration",
				"This window displays the most interesting planet in orbit around this star. It also lists information about the planet environment and more importantly the DISCOVERY you have made there. The discovery will be either an EVENT or an ITEM. You may also rename the planet in this window by clicking on its randomized name.", 
				"OK");
		break;

		case tut_upgrade:
			interface_popup(font_6x8, -1, -1, 288, 0, 11, 0, 
				"Tutorial: ship upgrade",
				"You have discovered an item which may be used to UPGRADE your ship. To INSTALL it, left click the yellow triangle icon next its name in the CARGO window. You may also UNINSTALL a ship component by clicking the yellow 'X' symbol next to an already installed system under the ship silhouette.", 
				"OK");
		break;

		case tut_device:
			interface_popup(font_6x8, -1, -1, 288, 0, 11, 0, 
				"Tutorial: alien device",
				"You have discovered a DEVICE of alien or unknown origin. Look at the CARGO window and left click the rectangular yellow icon next to the name of the item to USE it.", 
				"OK");
		break;

		case tut_treasure:
			interface_popup(font_6x8, -1, -1, 288, 0, 11, 0, 
				"Tutorial: treasure",
				"You have discovered a VALUABLE ITEM or an UNUSUAL LIFEFORM which has been added to your CARGO window. In the end of the game, you will sell it to Lextor Mucron for a profit.", 
				"OK");
		break;

		case tut_ally:
			interface_popup(font_6x8, -1, -1, 288, 0, 11, 0, 
				"Tutorial: allied ship",
				"A new ALLY has been added to your flotilla. Click the small ship icons in the upper left hand corner of the screen to view other starships besides your own. The icon will blink if the ship is damaged.", 
				"OK");
		break;

		case tut_encounter:
			interface_popup(font_6x8, -1, -1, 288, 0, 11, 0, 
				"Tutorial: alien encounter",
				"You have encountered one of the many alien races which inhabit the Purple Void. The RADAR SCREEN displays how many alien starships are in orbit around this star, and their relative sizes. You must choose to encounter the alien patrol or flee. If you choose to encounter the aliens, left click ENGAGE. If you wish to flee, left click AVOID and you will return to a previously explored star.", 
				"OK");
		break;

		case tut_combat:
			interface_popup(font_6x8, -1, -1, 288, 0, 11, 0, 
				"Tutorial: space combat",
				"You have engaged one or more enemy starships. Left click on a friendly ship to select it, then left click anywhere on the tactical map including enemy ships. Your ship will then begin to move towards that point at best speed. When a hostile ship is within range of your weapons, they will fire automatically. You can also press F1 at any time to view a comprehensive HELP SCREEN.", 
				"OK");
		break;

		default: return;
	}
	*/
	tut_seen[n] = 1;
}
// ----------------
//     INCLUDES
// ----------------

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <stdarg.h>

#include "typedefs.h"
#include "gfx.h"
#include "is_fileio.h"
#include "combat.h"
#include "starmap.h"

FILE *logfile;
int last_logdate;

char moddir[256];

// ----------------
// GLOBAL FUNCTIONS
// ----------------

FILE *myopen(const char *fname, const char *flags)
{
	FILE *fil;
	char realfname[256];

	sprintf(realfname, "%s%s", moddir, fname);
	fil = fopen(realfname, flags);

	if (!fil)
		fil = fopen(fname, flags);

	return fil;
}

int read_line(FILE *in, char *out1, char *out2)
{
	char c;
	char *sptr;
	int flag;
	int end;

	// flag
	// 0: start of line
	// 1: get command symbol
	// 2: between command and params
	// 3: get params

	out1[0] = out2[0] = 0;

	end = 0;
	flag = 0; sptr = out1;
	while (!end)
	{
		c = fgetc(in);
		if ((c < 32 && flag > 0) || feof(in))  // end of line
		{
			*sptr = 0;
			if (!feof(in))
				end = 1; 
			else
				end = 2;
		}
		else
		{
			if (flag == 0 && c > 32)
				flag = 1;
			if (flag == 1 && c <= 32)
				flag = 2;
			if (flag == 2 && c > 32)
			{ flag = 3; *sptr = 0; sptr = out2; }

			if (flag & 1)
				*sptr++ = c;
		}
	}

	//		printf("%s - %s\n", out1, out2);

	return end-1;
}

int read_line1(FILE *in, char *out1)
{
	char c;
	char *sptr;
	int flag;
	int end;

	// flag
	// 0: start of line
	// 1: get command symbol
	// 2: between command and params
	// 3: get params

	out1[0] = 0;

	end = 0;
	flag = 0; sptr = out1;
	while (!end)
	{
		c = fgetc(in);
		if ((c < 32 && flag > 0) || feof(in))  // end of line
		{
			*sptr = 0;
			if (!feof(in))
				end = 1; 
			else
				end = 2;
		}
		else
		{
			if (flag == 0 && c > 32)
				flag = 1;
			if (flag & 1)
				*sptr++ = c;
		}
	}

	//		printf("%s - %s\n", out1, out2);

	return end-1;
}

void ik_start_log()
{
	int n;
	FILE *fil;
	char fname[32];
	
	logfile = NULL;

	n=0;
	while (n<1000)
	{
		sprintf(fname, "log%04d.txt", n);
		fil = myopen(fname, "rt");
		if (!fil)
		{
			logfile = myopen(fname, "wt");
//			ik_save_tga(fname, img, pal);
			break;
		}
		else fclose(fil);

		n++;
	}
	last_logdate = -1;
}

#pragma unmanaged
void ik_print_log(char *ln, ...)
{
	char dlin[256], *dl=dlin;
	char text1[256], text2[256];
	int d, m, y;
	int date = player.stardate;

	if (!logfile)
		return;

  va_list ap;
  va_start(ap, ln);
  vsprintf(dlin, ln, ap);
  va_end(ap);

	if (date > last_logdate)
	{
		d = date%365;
		for (y = 0; y < 12; y++)
			if (d >= months[y].sd && d < months[y].sd+months[y].le)
				m = y;
		d = d + 1 - months[m].sd;
		y = 4580 + (date/365);
		sprintf(text1, "Captain %s of the %s", player.captname, player.shipname);
		sprintf(text2, "\n%s%%%ds %%02d %%s %%d\n", text1, 52-strlen(text1));
		fprintf(logfile, text2, "Date:", d, months[m].name, y);
		fprintf(logfile, "================================================================\n");
		last_logdate = date;
	}

	fprintf(logfile, dl);
}
#pragma managed
// ----------------
//     INCLUDES
// ----------------

//#include <iostream.h> // include important C/C++ stuff
#include <iostream> // include important C/C++ stuff
#include <conio.h>
#include <stdlib.h>
#include <malloc.h>
#include <memory.h>
#include <string.h>
#include <stdarg.h>
#include <stdio.h>
#include <math.h>
#include <io.h>
#include <fcntl.h>
#include <time.h>
#include <SDL.h>

#include "typedefs.h"
#include "is_fileio.h"

#include "textstr.h"
#include "iface_globals.h"
#include "gfx.h"
#include "snd.h"
#include "interface.h"
#include "starmap.h"
#include "combat.h"
#include "cards.h"
#include "startgame.h"
#include "endgame.h"

#define MAIN_INTERFACE_COLOR 0

#ifdef MOVIE
extern int movrecord;
#endif

//		extern FILE* loggy;
extern SDL_Surface *sdlsurf;

// ----------------
// GLOBAL VARIABLES
// ----------------

int opt_verbose = 0;

//int from_splash = 1;

//int still_running = 1;

t_ik_spritepak *spr_titles;

// ----------------
// LOCAL VARIABLES
// ----------------

// ----------------
// LOCAL PROTOTYPES
// ----------------

int32 main_menu();

void main_init();
void main_deinit();
void splash_screen();
void credits_screen();
int32 intro_screen();
int modconfig_main();

// ----------------
// GLOBAL FUNCTIONS
// ----------------

int my_main()
{
	Game_Init();

#ifdef MOVIE
	movrecord = 0;
#endif

	must_quit = 0;


	settings.opt_mousemode = 0;
	ik_hidecursor();
	if (!modconfig_main())
		must_quit = 1;

#ifdef MOVIE
	movrecord = 1;
#endif

	if (!must_quit)
	{
		main_init();

		splash_screen();

		while (!must_quit && main_menu()>0);

		credits_screen();

		main_deinit();
	}

	Game_Shutdown();

	return 0;
}

void splash_screen()
{
	int32 end;
	int32 c = 0, mc = 0;
	int32 t = 0, s = 0, l = 0, z = 0;
	int32 x = 0, y = 0, co = 0;
	t_ik_image *bg[3];
	int32 zap;

	bg[0] = ik_load_pcx("graphics/cheapass.pcx", NULL);
	bg[1] = ik_load_pcx("graphics/digieel.pcx", NULL);
	bg[2] = ik_load_pcx("graphics/title.pcx", NULL);

	prep_screen();
	ik_drawbox(screen, 0, 0, 640, 480, 0);
	ik_blit();
	update_palette();

	end = 0;
#ifdef MOVIE
	start_ik_timer(2, 60);
#else
	start_ik_timer(2, 20);
#endif
	while (!end && !must_quit)
	{
		ik_eventhandler();
		if (get_ik_timer(2) > 50)
			end = 1;
	}

	Play_SoundFX(WAV_LOGO);

	end = 0;
#ifdef MOVIE
	start_ik_timer(2, 60);
#else
	start_ik_timer(2, 20);
#endif
	while (!end && !must_quit)
	{
		ik_eventhandler();
		c = ik_inkey();
		mc = ik_mclick();

		if (mc==1 || c>0)
		{ must_quit = 1; }

		c = t; t = get_ik_timer(2);
		if (t >= 315)
			end = 1;
		else if (t > c)
		{
			prep_screen();
			s = t / 150;
			c = t % 150;

			if (c < 15)
			{
				l = c;
				for (y = 0; y < 480; y++)
					for (x = 0; x < 640; x++)
					{
						co = 0;
						if (s > 0)
							co = gfx_lightbuffer[bg[s-1]->data[y*bg[s-1]->pitch+x]+((15-l)<<8)];

						if (s < 2)
							co = gfx_addbuffer[gfx_lightbuffer[bg[s]->data[y*bg[s]->pitch+x]+(l<<8)] + (co<<8)];

						ik_putpixel(screen, x, y, co);
					}
			}
			else if (s < 2)
				ik_copybox(bg[s], screen, 0, 0, 640, 480, 0,0);
			else
				ik_drawbox(screen, 0, 0, 640, 480, 0);
			
			ik_blit();
		}
	}

	prep_screen();
	ik_drawbox(screen, 0, 0, 640, 480, 0);
	ik_blit();

	Play_Sound(WAV_MUS_START, 15, 1);

	end = 0;
	while (!end && !must_quit)
	{
		ik_eventhandler();
		if (get_ik_timer(2) > 405)
			end = 1;
	}


	zap = 340;
#ifdef MOVIE
	start_ik_timer(2, 60);
#else
	start_ik_timer(2, 20);
#endif
	end = 0; t = get_ik_timer(2);
	while (!end && !must_quit)
	{
		ik_eventhandler();
		c = ik_inkey();
		mc = ik_mclick();

		if (mc==1 || c>0)
		{ must_quit = 1; }
		c = t; t = get_ik_timer(2);
		if (t > c)
		{
			while (t > c)
			{
				c++;
				if (c == 80)
					Play_SoundFX(WAV_TITLE1);
				if (c == 180)
					Play_SoundFX(WAV_TITLE2);
				if (c == 260)
					Play_SoundFX(WAV_TITLE3);

				if (c == 340)
					Play_Sound(WAV_MUS_SPLASH, 15, 1);

				if (c == zap)
					Play_SoundFX(WAV_TITLE4+(rand()&1), 0, 50);
				if (c == zap + 100)
					zap += 150+rand()%150;
			}


			prep_screen();

			if (t > zap && t < zap + 100)
			{
				s = sin1k[((t-zap)*512)/100]>>8;
//			s = sin1k[(t*5)&1023]>>8;
//			s = (s*s*s) >> 16;
			}
			else
				s = 0;

			if (s < 1)
				s = 0;
			else
				s = (rand()%s)>>5;

			if (t < 30)
			{
				l = t/2;
				for (y = 0; y < 480; y++)
					for (x = 0; x < 640; x++)
					{
						co = gfx_lightbuffer[bg[2]->data[y*bg[2]->pitch+x]+(l<<8)];

						ik_putpixel(screen, x, y, co);
					}
			}
			else
				ik_copybox(bg[2], screen, 0, 0, 640, 480, 0,0);

			if (t > 80)
			{
				if (t-80 < 50)
				{	
					z = 192 + ((t-80)*96)/50;
					l = 13*16 + 5-(t-80)/10; 
					ik_drsprite(screen, 104, 80, 0, z, spr_titles->spr[1], 5+(l<<8));
					l = 13*16 + 15-(t-80)/5; 
				}
				else
				{	l = 13*16+6+s; }

				ik_drsprite(screen, 104, 80, 0, 192, spr_titles->spr[1], 5+(l<<8));
			}
			if (t > 180)
			{
				if (t-180 < 50)
				{	
					z = 192 + ((t-180)*96)/50;
					l = 13*16 + 5-(t-180)/10; 
					ik_drsprite(screen, 104, 224, 0, z, spr_titles->spr[2], 5+(l<<8));
					l = 13*16 + 15-(t-180)/5; 
				}
				else
				{	l = 13*16+6+s; }

				ik_drsprite(screen, 104, 224, 0, 192, spr_titles->spr[2], 5+(l<<8));
			}
			if (t > 260)
			{
				if (t-260 < 50)
				{	
					z = 256 + ((t-260)*128)/50;
					l = 13*16 + 5-(t-260)/10; 
					ik_drsprite(screen, 536, 144, 0, z, spr_titles->spr[3], 5+(l<<8));
					l = 13*16 + 15-(t-260)/5; 
				}
				else
				{	l = 13*16+6+s; }

				ik_drsprite(screen, 536, 144, 0, 256, spr_titles->spr[3], 5+(l<<8));
			}
			if (t > 340)
			{
				if (t-340 < 50)
					l = 12*16 + 15-(t-340)/5;
				else
					l = 12*16+6+s;

				ik_drsprite(screen, 320, 352, 
						0, 
						640+(sin1k[(t*8)&1023]>>12), spr_titles->spr[0], 5+(l<<8));
			}

			if (t > 500)
				ik_print(screen, font_6x8, 320-13*3, 448, 0, "PRESS ANY KEY");

			ik_blit();
		}
	}

	del_image(bg[0]);
	del_image(bg[1]);
	del_image(bg[2]);

	must_quit = 0;

}

void credits_screen()
{
	int32 end=0;
	int32 c=0, mc=0;
	int32 t=0, s=0, l=0, r=0;
	int32 x=0, y=0, co=0;
	t_ik_image *bg[3];

	must_quit = 0;

	bg[0] = ik_load_pcx("graphics/credits1.pcx", NULL);
	bg[1] = ik_load_pcx("graphics/credits2.pcx", NULL);
	bg[2] = ik_load_pcx("graphics/credits3.pcx", NULL);

	prep_screen();
	ik_drawbox(screen, 0, 0, 640, 480, 0);
	ik_blit();
	update_palette();

	Play_Sound(WAV_MUS_SPLASH, 15, 1);

	end = 0;
#ifdef MOVIE
	start_ik_timer(2, 60);
#else
	start_ik_timer(2, 20);
#endif
	while (!end && !must_quit)
	{
		ik_eventhandler();
		c = ik_inkey();
		mc = ik_mclick();
		if (get_ik_timer(2) > 10)
			end = 1;
	}

	end = 0;
#ifdef MOVIE
	start_ik_timer(2, 60);
#else
	start_ik_timer(2, 20);
#endif
	while (!end && !must_quit)
	{
		ik_eventhandler();
		c = ik_inkey();
		mc = ik_mclick();

		if (mc==1 || c>0)
		{ must_quit = 1; }

		
		c = t; t = get_ik_timer(2);
		if (t >= 1765)
			end = 1;
		else if (t > c)
		{
			while (c < t)
			{
				c++;
				if (c == 1700)
					Play_SoundFX(WAV_TITLE4+(rand()&1));
			}

			prep_screen();
			s = (t>300)+(t>1500)+(t>1750);
			if (!s)
				c = t;
			else if (s == 1)
				c = t - 300;
			else if (s == 2)
				c = t - 1500;
			else if (s == 3)
				c = t - 1750;

			if (c < 15)
			{
				l = c;
				for (y = 0; y < 480; y++)
					for (x = 0; x < 640; x++)
					{
						co = 0;
						if (s > 0)
							co = gfx_lightbuffer[bg[s-1]->data[y*bg[s-1]->pitch+x]+((15-l)<<8)];

						if (s < 3)
							co = gfx_addbuffer[gfx_lightbuffer[bg[s]->data[y*bg[s]->pitch+x]+(l<<8)] + (co<<8)];

						ik_putpixel(screen, x, y, co);
					}
			}
			else if (s < 3)
				ik_copybox(bg[s], screen, 0, 0, 640, 480, 0,0);
			else
				ik_drawbox(screen, 0, 0, 640, 480, 0);

			if (t > 1700)
			{
				r = rand()%(1+(((t-1700)*14)/65));
				r = r << 8;
				for (y = 0; y < 480; y++)
					for (x = 0; x < 640; x++)
					{
						co = screen->data[y*screen->pitch+x];
						ik_putpixel(screen, x, y, gfx_addbuffer[r + co]);
					}
			}
			
			ik_blit();
		}
	}

	prep_screen();
	ik_drawbox(screen, 0, 0, 640, 480, 0);
	ik_blit();

	del_image(bg[0]);
	del_image(bg[1]);
	del_image(bg[2]);

	must_quit = 0;

}

int32 main_menu()
{
	int i;

	i = intro_screen();

	if (i)
	{
		if (i==1)	// start game
		{
			if (startgame())	
			{	
#ifdef LOG_OUTPUT
				ik_start_log();
#endif
				Stop_All_Sounds();
				ik_print_log("launching game...\n");
				starmap();
				if (logfile)
					fclose(logfile);
			}
		}
		else	// combat sim
		{
			combat_sim();
		}
		must_quit = 0;
		return 1;
	}
	Stop_All_Sounds();

	return 0;
}

// ----------------
// LOCAL FUNCTIONS
// ----------------

void main_init()
{
	
	//LibSAIS::Log::Error(gcnew String("Testing"));
	
	int x=0;
	FILE *fil;
	must_quit=0;
	wants_screenshot=0;
	
	fil = myopen("graphics/palette.dat", "rb");
	fread(globalpal, 1, 768, fil);
	fclose(fil);
	memcpy(currentpal, globalpal, 768);

	for (x=0;x<1024;x++)
	{
		sin1k[x] = (int32)(sin(x*3.14159/512)*65536);
		cos1k[x] = (int32)(cos(x*3.14159/512)*65536);
	}

//	if (strlen(moddir))	// loading a mod, check for new frames
//	{
//		loggy = fopen("modlog.txt", "wt");
//	}

	calc_color_tables(globalpal);

	textstrings_init();
	load_all_sfx();
	combat_init();
	starmap_init();
	interface_init();
	cards_init();
	endgame_init();
	gfx_initmagnifier();

	srand( (unsigned)time( NULL ) );

	//s_volume = 85;
	got_hiscore = -2;
	loadconfig();

	spr_titles = load_sprites("graphics/titles.spr");

//	if (strlen(moddir))	// loading a mod, check for new frames
//	{
//		fclose(loggy);
//	}

	if (!(settings.opt_mousemode & 1))
		ik_hidecursor();
	else
		ik_showcursor();

#ifdef MOVIE
	start_ik_timer(2, 60);
#else
	start_ik_timer(2, 20);
#endif

}

void main_deinit()
{
	gfx_deinitmagnifier();
	endgame_deinit();
	cards_deinit();
	interface_deinit();
	starmap_deinit();
	combat_deinit();
	Delete_All_Sounds();
	textstrings_deinit();

	free_spritepak(spr_titles);

	del_color_tables();
}

int32 intro_screen()
{
	int32 end;
	int32 t0, t;
	int32 c, d[4], p, l[4];
	int32 mc, mx, my;
	int32 tx[4], ty[4], tx1[4], ty1[4];
	int32 sx[32], sy[32], sz[32], sc[32], sl;
	uint8 *gp[4];
	int32 x, y;
	int32 fr, fc = 0;
	int32 bx, by, h;
	int32 nebn = 4, starn = 32;
	uint8 *dr, *bk;
	int8 mode = 0;
	//int8 showhiscores = 0;
	int8 hiscmusic = 0;
	int32 fra = 0;

	t_ik_image *nebby;
	t_ik_image *backy;

	if (got_hiscore > -1)
	{
		mode = 1;
		//showhiscores = 1;
	}

	Stop_All_Sounds();

	if (got_hiscore > -1)
	{
		Play_Sound(WAV_MUS_HISCORE, 15, 1);
		hiscmusic = 1;
	}
	else
		Play_Sound(WAV_MUS_THEME, 15, 1);

	backy = ik_load_pcx("graphics/titback.pcx", NULL);
	

	nebby = new_image(640, 480);
	ik_drawbox(nebby, 0, 0, 639, 479, 0);

	start_ik_timer(0, 20); t = 0;

	for (c = 0; c < starn; c++)
	{
		sx[c] = (rand()%512-256)<<9;
		sy[c] = (rand()%512-256)<<9;
		sz[c] = rand()%768 + 128;
		sc[c] = rand()%8;
	}

	end = 0;
	while (!end && !must_quit)
	{
		ik_eventhandler();
		t0 = t; t = get_ik_timer(0);
		c = ik_inkey();
		mc = ik_mclick();
		mx = ik_mouse_x;
		my = ik_mouse_y;

		if (c == 13 || c == 32)
		{	end = 2; Play_SoundFX(WAV_DOT2, 0, 50); }

//		if (c == 'r')
//		{ end = 1; still_running = 1; }

		if (mc & 1)
		{
			if (my > 420 && my < 436)
			{
				if (mx > 176 && mx < 304)	// start game
				{	end = 2; Play_SoundFX(WAV_DOT2, 0, 50); }
				else if (mx > 336 && mx < 464)	// combat sim
				{ end = 3; Play_SoundFX(WAV_DOT2, 0, 50); }
			}
			else if (my > 440 && my < 456)
			{
				if (mx > 96 && mx < 224) // settings
				{
					if (mode == 2)
						mode = 0;
					else
						mode = 2;
					Play_SoundFX(WAV_DOT, 0, 50); 
					if (hiscmusic)
					{
						got_hiscore = -2;
						Play_Sound(WAV_MUS_THEME, 15, 1);
						hiscmusic = 0;
					}
				}
				if (mx > 256 && mx < 384)
				{	
					if (mode == 1)
						mode = 0;
					else
						mode = 1;
					Play_SoundFX(WAV_DOT, 0, 50); 
					if (hiscmusic)
					{
						got_hiscore = -2;
						Play_Sound(WAV_MUS_THEME, 15, 1);
						hiscmusic = 0;
					}
				}
				else if (mx > 416 && mx < 544)
					end = 1;
			}
			else
			{
				if (my > 112 && my < 320 && mx > 16 && mx < 624 && mode == 1)
				{
					Play_SoundFX(WAV_DOT, 0, 50); 
					if (hiscmusic)
					{
						got_hiscore = -2;
						Play_Sound(WAV_MUS_THEME, 15, 1);
						hiscmusic = 0;
					}
					mode = 0;
				}

			}

			if (mode == 2)	// settings
			{
				if (mx > bx+16 && mx < bx+32 && my > by+27 && my < by+27+7*16)
				{
					c = (my - (by+27)) / 16;
					switch (c)
					{
						case 0:
						settings.opt_mucrontext = settings.opt_mucrontext ^ 1;
						break;

						case 1:
						settings.opt_timerwarnings = 1 - settings.opt_timerwarnings;
						break;

						case 2:
						settings.opt_timeremaining = 1 - settings.opt_timeremaining;
						break;

						case 3:
						settings.opt_lensflares = 1 - settings.opt_lensflares;
						break;

						case 4:
						settings.opt_smoketrails = 1 - settings.opt_smoketrails;
						break;

						case 5:
						settings.opt_mousemode = settings.opt_mousemode ^ 1;
						if (settings.opt_mousemode & 1)
							ik_showcursor();
						else
							ik_hidecursor();
						break;

						case 6:
						settings.opt_mucrontext = settings.opt_mucrontext ^ 2;
						break;

						default: ;
					}
				}
				if (mx > bx+32 && mx < bx+160 && my > by+158 && my < by+166)
				{
					settings.opt_volume = ((mx - (bx+26))*10) / 128;
					s_volume = settings.opt_volume * 10;
					Set_Sound_Volume(15, 100);
					Play_SoundFX(WAV_SLIDER, 0, 50);
				}

				if (mx > bx+192 && mx < bx+240 && my > by+h-32 && my < by+h-16)
				{	Play_SoundFX(WAV_DOT, 0, 50);  mode = 0; }
			}
		}

		if (t > t0)
		{
			fr = 0;
			while (t0 < t)
			{
				for (c = 0; c < starn; c++)
				{
					sz[c]-=4;
					if (sz[c] < 128)
					{
						sz[c] = 896;
						sx[c] = (rand()%512-256)<<9;
						sy[c] = (rand()%512-256)<<9;
						sc[c] = rand()%8;
					}
				}
				t0++;
				fr++;
			}

			prep_screen();

			//ik_drawbox(screen, 0, 0, 639, 479, 0);

			//ik_copybox(backy, nebby, 0, 0, 639, 479, 0, 0);
			
			// draw zooming nebula
			fra = (fra + 1) & 3;
			for (c = 0; c < nebn; c++)
			{
				d[c] = 1024-((t*4+(c*768)/nebn)%768);	// distance to nebula plane ( 256...1024)
				tx[c] = 65536-d[c]*160+sin1k[c*300]/2; 
				ty[c] = 65536-d[c]*120+cos1k[c*300]/2;	// corner coords
				if (d[c] < 512)
					l[c] = (d[c]-256)/16;
				else if (d[c] > 768)
					l[c] = (1024-d[c])/16;
				else
					l[c] = 15;

				ty1[c] = ty[c];
			}

			for (y = 0; y < 240; y++)
			{
				dr = ik_image_pointer(nebby, fra&1, y*2+((fra&2)==2));
				bk = ik_image_pointer(backy, fra&1, y*2+((fra&2)==2));

				for (c = 0; c < nebn; c++)
				{
					tx1[c] = tx[c];
					gp[c] = combatbg2->data + ((ty1[c]>>9)&255)*combatbg2->pitch;
				}

				for (x = 0; x < 320; x++)
				{
					p = 0;

					for (c = 0; c < nebn; c++)
					{
						p = gfx_addbuffer[gfx_lightbuffer[gp[c][(tx1[c]>>9)&255]+(l[c]<<8)]+(p<<8)];
						tx1[c] += d[c];
					}

					c = *bk++;
					bk++;

					c = gfx_lightbuffer[(c<<8)+15-MIN(15,(p&15)*2)];
					p = gfx_addbuffer[(c<<8)+p];

					*dr++ = p;
					dr++;
				}

				for (c = 0; c < nebn; c++)
					ty1[c] += d[c];
			}

			ik_copybox(nebby, screen, 0, 0, 640, 480, 0, 0);

			// draw stars			
			for (c = 0; c < starn; c++)
			{
				x = 320 + sx[c] / sz[c];
				y = 240 + sy[c] / sz[c];
				p = 8192 / sz[c];
				if (sz[c] < 192)
					sl = sc[c]*16 + (sz[c]-128)/4;
				else if (sz[c] > 384)
					sl = sc[c]*16 + (896-sz[c])/32;
				else
					sl = sc[c]*16 + 15;
				ik_drsprite(screen, x, y, c<<6, p, spr_shockwave->spr[4], 5+(sl<<8));
			}

			//ik_print(screen, font_6x8, 4, 4, 0, "%d FPS", 50/fr);

			switch (mode)
			{
				case 0:
				//ik_dsprite(screen, 64, 128, spr_title->spr[0], 0);
//				ik_drsprite(screen, 320, 192, 
//						0, 
//						640+(sin1k[(t*8)&1023]>>12), spr_titles->spr[0], 5+((6*16+15)<<8));
				ik_drsprite(screen, 320, 192, 0, 640, spr_titles->spr[0], 5+((6*16+15)<<8));
				break;

				case 1:
				for (y = 112; y < 320; y++)
				{
					dr = ik_image_pointer(screen, 16, y);
					for (x = 16; x < 624; x++)
					{
						*dr++ = gfx_lightbuffer[(*dr)+(8<<8)];
					}
				}
				ik_print(screen, font_6x8, 32, 128, 11, "TOP 20 EXPLORERS");
				ik_print(screen, font_6x8, 578, 128, 11, "SCORE");
				for (c = 0; c < num_scores; c++)
				{
					// 50 +  + 12
					ik_print(screen, font_6x8, 32, 144+c*8, 11-8*(c==got_hiscore), textstring[STR_HISCORE_ENTRY], 
									scores[c].cname, scores[c].sname, scores[c].deathmsg);
					ik_print(screen, font_6x8,572, 144+c*8, 11-8*(c==got_hiscore), "%6d", scores[c].score);
				}
				ik_drawline(screen, 32, 138, 608, 138, 176+12, 0, 255, 0);
				ik_drawline(screen, 32, 139, 608, 139, 176+2, 0, 255, 0);
				break;

				case 2:
				bx = 192; by = 136; h = 184;
				interface_drawborder(screen, bx, by, bx+256, by+h, 1, MAIN_INTERFACE_COLOR, "game settings");

				y = 32;
				ik_print(screen, font_6x8, bx+32, by+y, MAIN_INTERFACE_COLOR, "DISPLAY MISSION BRIEFING");
				ik_dsprite(screen, bx+16, by+y-5, spr_IFbutton->spr[(settings.opt_mucrontext & 1)], 2+(MAIN_INTERFACE_COLOR<<8));

				y+=16;
				ik_print(screen, font_6x8, bx+32, by+y, MAIN_INTERFACE_COLOR, "ENABLE TIME LEFT WARNINGS");
				ik_dsprite(screen, bx+16, by+y-5, spr_IFbutton->spr[settings.opt_timerwarnings], 2+(MAIN_INTERFACE_COLOR<<8));

				y+=16;
				ik_print(screen, font_6x8, bx+32, by+y, MAIN_INTERFACE_COLOR, "DISPLAY TIME LEFT AS COUNTDOWN");
				ik_dsprite(screen, bx+16, by+y-5, spr_IFbutton->spr[settings.opt_timeremaining], 2+(MAIN_INTERFACE_COLOR<<8));

				y+=16;
				ik_print(screen, font_6x8, bx+32, by+y, MAIN_INTERFACE_COLOR, "ENABLE EXPLOSION HIGHLIGHTS");
				ik_dsprite(screen, bx+16, by+y-5, spr_IFbutton->spr[settings.opt_lensflares], 2+(MAIN_INTERFACE_COLOR<<8));

				y+=16;
				ik_print(screen, font_6x8, bx+32, by+y, MAIN_INTERFACE_COLOR, "ENABLE MISSILE SMOKE TRAILS");
				ik_dsprite(screen, bx+16, by+y-5, spr_IFbutton->spr[settings.opt_smoketrails], 2+(MAIN_INTERFACE_COLOR<<8));

				y+=16;
				ik_print(screen, font_6x8, bx+32, by+y, MAIN_INTERFACE_COLOR, "ENABLE HARDWARE MOUSE CURSOR");
				ik_dsprite(screen, bx+16, by+y-5, spr_IFbutton->spr[settings.opt_mousemode & 1], 2+(MAIN_INTERFACE_COLOR<<8));

				y+=16;
				ik_print(screen, font_6x8, bx+32, by+y, MAIN_INTERFACE_COLOR, "LARGE TRADING SCREEN");
				ik_dsprite(screen, bx+16, by+y-5, spr_IFbutton->spr[(settings.opt_mucrontext & 2)/2], 2+(MAIN_INTERFACE_COLOR<<8));

				y+=16;
				ik_print(screen, font_6x8, bx+32, by+y, MAIN_INTERFACE_COLOR, "SOUND VOLUME: %d%%", s_volume);
				interface_drawslider(screen, bx+32, by+y+14, 0, 128, 10, settings.opt_volume, MAIN_INTERFACE_COLOR);

				interface_drawbutton(screen, bx+256-64, by+h-32, 48, MAIN_INTERFACE_COLOR, "OK");

				//ik_print(screen, font_6x8, 224, 192, MAIN_INTERFACE_COLOR, "SHOW MISSION BRIEFING");
				break;
				default: ;
			}

			interface_drawbutton(screen, 176,  420, 128, MAIN_INTERFACE_COLOR, "START GAME");
			interface_drawbutton(screen, 336, 420, 128, MAIN_INTERFACE_COLOR, "COMBAT SIMULATOR");

			interface_drawbutton(screen, 96,  440, 128, MAIN_INTERFACE_COLOR, "SETTINGS");
			interface_drawbutton(screen, 256, 440, 128, MAIN_INTERFACE_COLOR, "HIGH SCORES");
			interface_drawbutton(screen, 416, 440, 128, MAIN_INTERFACE_COLOR, "QUIT");

			ik_print(screen, font_6x8, 3, 3, 0, SAIS_VERSION_NUMBER);

			ik_blit();	
		}
	}

	got_hiscore = -2;
	saveconfig();

	del_image(nebby);
	del_image(backy);

	if (end)
		return end-1;
	else
		return 0;
}
#include <stdio.h>
#include <io.h>
#include <malloc.h>
#include <memory.h>
#include <string.h>


#include "typedefs.h"
#include "is_fileio.h"
#include "iface_globals.h"
#include "gfx.h"
#include "interface.h"
#include "snd.h"


#define MAX_MODDIRS 64
#define MOD_INTERFACE_COLOR 11

typedef struct _t_moddir
{
	char name[32];
	char dir[224];
} t_moddir;

t_moddir *moddirs;
int n_moddirs;

void modconfig_init()
{
	int x;
	int y;
	int handle;
	_finddata_t find;
	char tmps[256];
	FILE *fil;

	moddir[0] = 0;

	// allocate memory for mod names
	moddirs = (t_moddir*)calloc(MAX_MODDIRS, sizeof(t_moddir));
	n_moddirs = 0;

	// read mod names
	handle = _findfirst("mods/*.*", &find);
	if (handle != -1)
	{
		x = handle;
		while (x != -1)
		{
			if (n_moddirs < MAX_MODDIRS)
			if (find.attrib & _A_SUBDIR)
			{
				if (strcmp(find.name, ".") && strcmp(find.name, ".."))
				{
					sprintf(moddirs[n_moddirs].dir, "mods/%s/", find.name);
					sprintf(moddirs[n_moddirs].name, find.name);
					n_moddirs++;
				}
			}
			x = _findnext(handle, &find);
		}
		_findclose(handle);
	}

	if (n_moddirs > 1)
	{
		for (x = 1; x < n_moddirs; x++)
		{
			y = x;
			while (y > 0 && strcmp(moddirs[y].name, moddirs[y-1].name) < 0)
			{
				strcpy(tmps, moddirs[y].name);
				strcpy(moddirs[y].name, moddirs[y-1].name);
				strcpy(moddirs[y-1].name, tmps);
				strcpy(tmps, moddirs[y].dir);
				strcpy(moddirs[y].dir, moddirs[y-1].dir);
				strcpy(moddirs[y-1].dir, tmps);
				y--;
			}
		}
	}

	fil = myopen("graphics/palette.dat", "rb");
	fread(globalpal, 1, 768, fil);
	fclose(fil);
	memcpy(currentpal, globalpal, 768);

	Load_WAV("sounds/beep_wait.wav",0);
	s_volume = 100;
	interface_init();
}

void modconfig_deinit()
{
	interface_deinit();
	Delete_Sound(0);

	free(moddirs);
	n_moddirs = 0;
}

int modconfig_main()
{
	int32 t, t0;
	int32 c, mc, mx, my;
	int32 bx, by, h;
	int32 x, y;
	int32 msel, mscr;
	t_ik_image *backy;
	int32 mode;
	int end;

	modconfig_init();

	if (!n_moddirs)
	{
		modconfig_deinit();
		return 1;
	}

	msel = 0; mscr = 0; mode = 0;
	backy = ik_load_pcx("graphics/starback.pcx", NULL);

	start_ik_timer(0, 20); t = 0;
	end = 0;
	while (!end && !must_quit)
	{
		ik_eventhandler();
		t0 = t; t = get_ik_timer(0);
		c = ik_inkey();
		mc = ik_mclick();
		mx = ik_mouse_x;
		my = ik_mouse_y;

		if (c == 13 || c == 32)
		{
			end = 1;
			if (mode == 1)
			{
				if (strcmp(moddir, moddirs[msel].dir))
				{	
					sprintf(moddir, moddirs[msel].dir);
					end = 1; 
				}
			}
		}

		if (mc & 1)
		{
			switch(mode)
			{
				case 0:	// main menu
				if (mx > bx+48 && mx < bx+208 && my > by+35 && my < by+108)
				{
					c = (my-(by+36))/24;
					if (c == 0)
					{
						moddir[0] = 0;
						end = 1;
						Play_SoundFX(0, 0, 100);
					}
					else if (c == 1)
					{
						mode = 1;
						Play_SoundFX(0, 0, 100);
					}
					else if (c == 2)
					{
						must_quit = 1;
						Play_SoundFX(0, 0, 100);
					}
				}
				break;

				case 1:	// mods
				if (mx > bx+16 && mx < bx+224 && my > by+35 && my < by+112)
				{
					c = mscr + (my-(by+36))/8;
					if (c >= 0 && c < n_moddirs)
					{
						msel = c;
						Play_SoundFX(0, 0, 100);
					}
					else
					{
						for (y = 0; y < n_moddirs; y++)
							if (!strcmp(moddirs[y].dir, moddir))
								msel = y;
						Play_SoundFX(0, 0, 100);
					}				
				}
				if (mx > bx+16 && mx < bx+80 && my > by+h-32 && my < by+h-16)
				{	
					mode = 0; 
					Play_SoundFX(0, 0, 100);
				}
				if (mx > bx+176 && mx < bx+240 && my > by+h-32 && my < by+h-16)
				if (strcmp(moddir, moddirs[msel].dir))
				{	
					sprintf(moddir, moddirs[msel].dir);
					Play_SoundFX(0, 0, 100);
					end = 1; 
				}
				break;

				default: ;
			}
		}

		if (t > t0)
		{
			prep_screen();
			ik_copybox(backy, screen, 0, 0, 639, 479, 0, 0);

			switch (mode)
			{
				case 0:	// main menu
				bx = 192; by = 164; h = 128;
				interface_drawborder(screen, bx, by, bx+256, by+h, 1, 
						MOD_INTERFACE_COLOR, "Strange Adventures in Infinite Space");

				interface_drawbutton(screen, bx+48, by+40, 160, MOD_INTERFACE_COLOR, "STANDARD GAME");
				interface_drawbutton(screen, bx+48, by+64, 160, MOD_INTERFACE_COLOR, "MODS");
				interface_drawbutton(screen, bx+48, by+88, 160, MOD_INTERFACE_COLOR, "EXIT");
				break;

				case 1:
				bx = 192; by = 148; h = 160;
				interface_drawborder(screen, bx, by, bx+256, by+h, 1, 
						MOD_INTERFACE_COLOR, "Strange Adventures in Infinite Space");

				y = 0;
				for (x = 0; x < n_moddirs; x++)
					if (!strcmp(moddirs[x].dir, moddir))
						y = x;

				ik_print(screen, font_6x8, bx+16, by+22, 0, "Select a mod", moddirs[y].name);
				interface_thinborder(screen, bx+16, by+32, bx+240, by+120, MOD_INTERFACE_COLOR, 0);
				
				for (x = 0; x < n_moddirs; x++)
				{
					y = x - mscr;
					if (x == msel)
						ik_drawbox(screen, bx+16, by+35+y*8, bx+239, by+43+y*8, 3); //STARMAP_INTERFACE_COLOR*16+4);
					ik_print(screen, font_6x8, bx+20, by+36+y*8, 3*(msel==x), moddirs[x].name);
				}

				if (n_moddirs > 10)
				{
					ik_dsprite(screen, bx+228, by+36, spr_IFarrows->spr[5], 2+(MOD_INTERFACE_COLOR<<8));
					ik_dsprite(screen, bx+228, by+108, spr_IFarrows->spr[4], 2+(MOD_INTERFACE_COLOR<<8));
					interface_drawslider(screen, bx + 228, by + 44, 1, 64, n_moddirs-10, mscr, MOD_INTERFACE_COLOR);
				}
				interface_thinborder(screen, bx+16, by+32, bx+240, by+120, MOD_INTERFACE_COLOR);

				interface_drawbutton(screen, bx+16, by+h-32, 64, MOD_INTERFACE_COLOR, "CANCEL");
				interface_drawbutton(screen, bx+256-80, by+h-32, 64, MOD_INTERFACE_COLOR, "RUN MOD");
				break;

				default: ;
			}

			update_palette();
			ik_blit();	
		}
	}

	prep_screen();
	ik_drawbox(screen, 0, 0, 639, 479, 0);
	ik_blit();

	del_image(backy);
	modconfig_deinit();

	return end;
}




/*
code to handle all/most of the interaction with the win32 system

- event handling
- kb and mouse input
*/

// INCLUDES ///////////////////////////////////////////////
#define WIN32_LEAN_AND_MEAN  

#include <windows.h>   // include important windows stuff
#include <windowsx.h> 
#include <mmsystem.h>
//#include <iostream.h> // include important C/C++ stuff
#include <iostream> // include important C/C++ stuff
#include <conio.h>
#include <stdlib.h>
#include <malloc.h>
#include <memory.h>
#include <string.h>
#include <stdarg.h>
#include <stdio.h>
#include <math.h>
#include <io.h>
#include <fcntl.h>
#include <SDL.h>

#include "typedefs.h"
#include "iface_globals.h"
#include "gfx.h"
#include "snd.h"

// DEFINES ////////////////////////////////////////////////

// MACROS /////////////////////////////////////////////////

// these read the keyboard asynchronously
#define KEY_DOWN(vk_code) ((GetAsyncKeyState(vk_code) & 0x8000) ? 1 : 0)
#define KEY_UP(vk_code)   ((GetAsyncKeyState(vk_code) & 0x8000) ? 0 : 1)

// TYPES //////////////////////////////////////////////////

typedef unsigned short USHORT;
typedef unsigned short WORD;
typedef unsigned char  UCHAR;
typedef unsigned char  BYTE;

typedef struct {
	int32 start, freq; 
} t_ik_timer;

// PROTOTYPES /////////////////////////////////////////////

#ifdef MOVIE
extern int movrecord;
#endif

// GLOBALS ////////////////////////////////////////////////

SDL_Surface *sdlsurf;
extern t_paletteentry pe[256];

char buffer[80];                // used to print text
int IsMinimized = 0;
int ActiveApp = 0;
int SwitchMode = 0;

int ik_mouse_x;
int ik_mouse_y;
int ik_mouse_b;
int ik_mouse_c;
int must_quit;
int wants_screenshot;

int key_left=SDLK_LEFT;
int key_right=SDLK_RIGHT;
int key_up=SDLK_UP;
int key_down=SDLK_DOWN;
int key_f[10];
int key_fire1=SDLK_TAB;
int key_fire2=SDLK_RETURN;
int key_fire2b=SDLK_SPACE;

char ik_inchar;
uint8 *keystate;

t_ik_timer ik_timer[10];

// FUNCTIONS //////////////////////////////////////////////

void eventhandler()
{
	SDL_Event event;
	int b;

	keystate = SDL_GetKeyState(NULL);

	while ( SDL_PollEvent(&event) ) 
	{
		switch (event.type) 
		{
			case SDL_KEYDOWN:
			switch(event.key.keysym.sym){
				case SDLK_F12:
					wants_screenshot=1;
					break;
				case SDLK_F2:
				case SDLK_RCTRL:
				case SDLK_LCTRL:
					settings.opt_mousemode ^= 4;
					Play_SoundFX(WAV_LOCK,0);
					break;
				case SDLK_ESCAPE :
					must_quit=1;
					break;
			}

			ik_inchar = event.key.keysym.unicode & 0xff;
			break;

			case SDL_MOUSEBUTTONDOWN:
				b = (event.button.button == SDL_BUTTON_LEFT) +
						2*(event.button.button == SDL_BUTTON_RIGHT) +
						4*(event.button.button == SDL_BUTTON_MIDDLE);
				ik_mouse_c = b;	
				ik_mouse_b |= b;
			case SDL_MOUSEMOTION:
				ik_mouse_x = event.motion.x;
				ik_mouse_y = event.motion.y;
				break;

			case SDL_MOUSEBUTTONUP:
				b = (event.button.button == SDL_BUTTON_LEFT) +
						2*(event.button.button == SDL_BUTTON_RIGHT) +
						4*(event.button.button == SDL_BUTTON_MIDDLE);
				ik_mouse_b &= (7-b);
				break;

			case SDL_ACTIVEEVENT:
			ActiveApp = event.active.gain;
			if (ActiveApp)
			{
				gfx_redraw = 1;
			}
			break;

			case SDL_VIDEOEXPOSE:
			case SDL_VIDEORESIZE:
				ActiveApp = 1;
				break;
			case SDL_QUIT:
				must_quit = 1;
				break;
			default:
				break;
		}
	}
}


// WINX GAME PROGRAMMING CONSOLE FUNCTIONS ////////////////

int Game_Init(void *parms)
{
	int x;

	for (x=0;x<10;x++)
		key_f[x]=SDLK_F1+x;

	return(1);
}

int Game_Shutdown(void *parms)
{
	return(1);
}


///////////////////////////////////////////////////////////

// call eventhandler once every frame
// to check if windows is trying to kill you (or other events)
int ik_eventhandler()
{
	eventhandler();

	if (must_quit)
		return 1;

	return 0;
}

// read key
int key_pressed(int vk_code)
{
	if (keystate)
		return keystate[vk_code];
	else
		return 0;
}

int ik_inkey()
{
	char c=ik_inchar;

	ik_inchar=0;
	return c;
}

int ik_mclick()
{
	char c=ik_mouse_c&3;

	ik_mouse_c=0;
	return c;
}

// cheesy timer functions
void start_ik_timer(int n, int f)
{
	ik_timer[n].start=SDL_GetTicks();
	ik_timer[n].freq=f;
}

void set_ik_timer(int n, int v)
{
	int x;

	x=SDL_GetTicks();
	ik_timer[n].start=x-ik_timer[n].freq*v;
}

int get_ik_timer(int n)
{
	int x;

	if (ik_timer[n].freq)
	{
		x=SDL_GetTicks();
		return ((x-ik_timer[n].start)/ik_timer[n].freq);
	}

	return 0;
}

int get_ik_timer_fr(int n)
{
	int x;

	if (ik_timer[n].freq)
	{
		x=SDL_GetTicks();
		return ((x-ik_timer[n].start)*256/ik_timer[n].freq);
	}

	return 0;
}

void ik_showcursor()
{
	SDL_ShowCursor(1);
}

void ik_hidecursor()
{
	SDL_ShowCursor(0);
}
#include <SDL.h>
#include <SDL_mixer.h>


#include "typedefs.h"
#include "gfx.h"

int my_main();
int sound_init();

extern SDL_Surface *sdlsurf;

int main(int argc, char *argv[])
{
	gfx_width=640; gfx_height=480; 
	gfx_fullscreen=0; 

	c_minx=0; 
	c_miny=0;
	c_maxx=gfx_width;
	c_maxy=gfx_height;

	if(SDL_Init(SDL_INIT_VIDEO | SDL_INIT_TIMER | SDL_INIT_AUDIO) < 0)
	{	
		//sdlerr("initialising SDL"); 
		return 1; 
	}
	SDL_WM_SetCaption("Strange Adventures In Infinite Space", "Strange Adventures In Infinite Space");

	// Enable UNICODE so we can emulate getch() in text input
	SDL_EnableUNICODE(1);

	// init SDL mixer
	if (Mix_OpenAudio(22050, AUDIO_S16, 2, 1024) < 0)
	{
		return 1;
	}
	Mix_AllocateChannels(16);
	sound_init();

	if (gfx_fullscreen)
		sdlsurf = SDL_SetVideoMode(640, 480, 8, SDL_FULLSCREEN | SDL_HWPALETTE);
	else
		sdlsurf = SDL_SetVideoMode(640, 480, 8, SDL_HWPALETTE);
	my_main();

	return 0;
}
#include <stdio.h>

#include "typedefs.h"
#include "iface_globals.h"
#include "snd.h"

int8 s_volume;

void load_all_sfx(void)
{
	// combat
	Load_WAV("sounds/combat/beam1.wav",			WAV_BEAM1);
	Load_WAV("sounds/combat/beam2.wav",			WAV_BEAM2);
	Load_WAV("sounds/combat/beam3.wav",			WAV_BEAM3);
	Load_WAV("sounds/combat/beam4.wav",			WAV_BEAM4);
	Load_WAV("sounds/combat/beam5.wav",			WAV_BEAM5);
	Load_WAV("sounds/combat/beam6.wav",			WAV_BEAM6);
	Load_WAV("sounds/combat/beam7.wav",			WAV_BEAM7);
	Load_WAV("sounds/combat/misl1.wav",			WAV_PROJ1);
	Load_WAV("sounds/combat/misl1.wav",			WAV_PROJ2);
	Load_WAV("sounds/combat/misl3.wav",			WAV_PROJ3);
	Load_WAV("sounds/combat/misl1.wav",			WAV_PROJ4);
	Load_WAV("sounds/combat/misl1.wav",			WAV_PROJ5);
	Load_WAV("sounds/combat/pgun1.wav",			WAV_PROJ6);
	Load_WAV("sounds/combat/pgun2.wav",			WAV_PROJ7);
	Load_WAV("sounds/combat/pgun3.wav",			WAV_PROJ8);
	Load_WAV("sounds/combat/pgun4.wav",			WAV_PROJ9);
	Load_WAV("sounds/combat/pgun5.wav",			WAV_PROJ10);
	Load_WAV("sounds/combat/pgun6.wav",			WAV_PROJ11);
	Load_WAV("sounds/combat/lowboom.wav",		WAV_HIT1);
	Load_WAV("sounds/combat/bulhit02.wav",	WAV_HIT2);
	Load_WAV("sounds/combat/plashit01.wav",	WAV_HIT3);
	Load_WAV("sounds/combat/lowboom.wav",		WAV_HIT4);
	Load_WAV("sounds/combat/lowboom.wav",		WAV_HIT5);
	Load_WAV("sounds/combat/explo01.wav",		WAV_EXPLO1);
	Load_WAV("sounds/combat/explo02.wav",		WAV_EXPLO2);
	Load_WAV("sounds/combat/shield02.wav",	WAV_SHIELD);
	Load_WAV("sounds/combat/cloak_in.wav",	WAV_CLOAKIN);
	Load_WAV("sounds/combat/cloak_out.wav",	WAV_CLOAKOUT);
	Load_WAV("sounds/combat/boardship.wav",	WAV_BOARD);
	Load_WAV("sounds/combat/sys_dmg.wav",		WAV_SYSDAMAGE);
	Load_WAV("sounds/combat/sys_hit.wav",		WAV_SYSHIT1);
	Load_WAV("sounds/combat/sys_hit_2.wav",	WAV_SYSHIT2);
	Load_WAV("sounds/combat/sys_fixed.wav",	WAV_SYSFIXED);
	Load_WAV("sounds/combat/teleport.wav",	WAV_TELEPORT);
	Load_WAV("sounds/combat/fieryfury.wav",	WAV_FIERYFURY);
	Load_WAV("sounds/combat/fighter.wav",		WAV_FIGHTERLAUNCH);
	Load_WAV("sounds/combat/sim_end.wav",		WAV_ENDSIMULATION);
	// interface
	Load_WAV("sounds/beep_yes.wav",       WAV_YES);
	Load_WAV("sounds/beep_no.wav",				WAV_NO);
	Load_WAV("sounds/beep_accept.wav",    WAV_ACCEPT);
	Load_WAV("sounds/beep_decline.wav",   WAV_DECLINE);
	Load_WAV("sounds/beep_dot.wav",       WAV_DOT);
	Load_WAV("sounds/beep_dot2.wav",      WAV_DOT2);
	Load_WAV("sounds/beep_select.wav",    WAV_SELECT);
	Load_WAV("sounds/beep_deselect.wav",  WAV_DESELECT);
	Load_WAV("sounds/beep_selectstar.wav",WAV_SELECTSTAR);
	Load_WAV("sounds/beep_info.wav",			WAV_INFO);
	Load_WAV("sounds/beep_selship.wav",   WAV_SELECTSHIP);
	Load_WAV("sounds/beep_wait.wav",			WAV_WAIT);
	Load_WAV("sounds/swut.wav",						WAV_SLIDER);
	Load_WAV("sounds/install.wav",				WAV_INSTALL);
	Load_WAV("sounds/install2.wav",				WAV_INSTALL2);
	Load_WAV("sounds/lock.wav",						WAV_LOCK);
	Load_WAV("sounds/depart02.wav",				WAV_DEPART);
	Load_WAV("sounds/arrive02.wav",				WAV_ARRIVE);
	Load_WAV("sounds/hyperdrv.wav",				WAV_HYPERDRIVE);
	Load_WAV("sounds/foldmove.wav",				WAV_FOLDSPACE);
	Load_WAV("sounds/radar1.wav",					WAV_RADAR);
	Load_WAV("sounds/scan_loop.wav",			WAV_SCANNER);
	Load_WAV("sounds/bridge_loop.wav",		WAV_BRIDGE);
	Load_WAV("sounds/message.wav",				WAV_MESSAGE);
	Load_WAV("sounds/message2.wav",				WAV_TANRUMESSAGE);
	Load_WAV("sounds/pay_loop.wav",				WAV_PAYMERC);
	Load_WAV("sounds/trade01.wav",				WAV_TRADE);
	Load_WAV("sounds/cash02.wav",					WAV_CASH);
	Load_WAV("sounds/probe_l.wav",				WAV_PROBE_LAUNCH);
	Load_WAV("sounds/probe_d.wav",				WAV_PROBE_DEST);
	Load_WAV("sounds/fomax_hi.wav",				WAV_FOMAX_HI);
	Load_WAV("sounds/fomax_bye.wav",			WAV_FOMAX_BYE);
	Load_WAV("sounds/fomax_wish.wav",			WAV_FOMAX_WISH);
	Load_WAV("sounds/timer.wav",					WAV_TIMER);
	Load_WAV("sounds/warning.wav",				WAV_WARNING);
	Load_WAV("sounds/opticals.wav",				WAV_OPTICALS);
	Load_WAV("sounds/alien1.wav",					WAV_TITLE1);
	Load_WAV("sounds/alien2.wav",					WAV_TITLE2);
	Load_WAV("sounds/alien3.wav",					WAV_TITLE3);
	Load_WAV("sounds/titzap01.wav",				WAV_TITLE4);
	Load_WAV("sounds/titzap02.wav",				WAV_TITLE5);
	Load_WAV("sounds/logo.wav",						WAV_LOGO);
	// race encounters
	Load_WAV("sounds/races/klakar.wav",		WAV_KLAKAR);
	Load_WAV("sounds/races/zorg.wav",			WAV_ZORG);
	Load_WAV("sounds/races/muktian.wav",	WAV_MUKTIAN);
	Load_WAV("sounds/races/garthan.wav",	WAV_GARTHAN);
	Load_WAV("sounds/races/tanru.wav",		WAV_TANRU);
	Load_WAV("sounds/races/urluquai.wav",	WAV_URLUQUAI);
	Load_WAV("sounds/races/kawangi.wav",	WAV_KAWANGI);
	// events
	Load_WAV("sounds/blakhole.wav",				WAV_BLACKHOLE);
	Load_WAV("sounds/blakhol2.wav",				WAV_BLACKHOLEDEATH);
	Load_WAV("sounds/collapse.wav",				WAV_COLLAPSER);
	// cards
	Load_WAV("sounds/cards/ally.wav",						WAV_ALLY);
	Load_WAV("sounds/cards/flare_loop.wav",			WAV_FLARE);
	Load_WAV("sounds/cards/intruder_loop.wav",	WAV_SPY);
	Load_WAV("sounds/cards/nova_loop.wav",			WAV_NOVA);
	Load_WAV("sounds/cards/intruder_loop.wav",	WAV_SABOTEUR);
	Load_WAV("sounds/cards/whales.wav",					WAV_WHALES);
	Load_WAV("sounds/cards/cube_loop.wav",			WAV_CUBE);
	Load_WAV("sounds/cards/hulk_loop.wav",			WAV_SPACEHULK);
	Load_WAV("sounds/cards/gas_loop.wav",				WAV_GASGIANT);
	Load_WAV("sounds/cards/no_planet.wav",			WAV_NOPLANET);
	// items
	Load_WAV("sounds/cards/weapon.wav",					WAV_WEAPON);
	Load_WAV("sounds/cards/system_loop.wav",		WAV_SYSTEM);
	Load_WAV("sounds/cards/device.wav",					WAV_DEVICE);
	Load_WAV("sounds/cards/lifeform.wav",				WAV_LIFEFORM);
	Load_WAV("sounds/cards/drive_loop.wav",			WAV_DRIVE);
	// artifacts
	Load_WAV("sounds/cards/platinum.wav",				WAV_PLATINUM);
	Load_WAV("sounds/cards/titanium.wav",				WAV_TITANIUM);
	Load_WAV("sounds/cards/brass.wav",					WAV_BRASS);
	Load_WAV("sounds/cards/plastic.wav",				WAV_PLASTIC);
	Load_WAV("sounds/cards/cenotaph.wav",				WAV_CENOTAPH);
	Load_WAV("sounds/cards/torc.wav",						WAV_TORC);
	Load_WAV("sounds/cards/gong.wav",						WAV_GONG);
	Load_WAV("sounds/cards/mantle.wav",					WAV_MANTLE);
	Load_WAV("sounds/cards/whistle.wav",				WAV_WHISTLE);
	Load_WAV("sounds/cards/horloge_loop.wav",		WAV_HORLOGE);
	Load_WAV("sounds/cards/toy.wav",						WAV_TOY);
	Load_WAV("sounds/cards/codex.wav",					WAV_CODEX);
	Load_WAV("sounds/cards/sculpture.wav",			WAV_SCULPTURE);
	Load_WAV("sounds/cards/conograph_loop.wav",	WAV_CONOGRAPH);
	Load_WAV("sounds/cards/conograph_use.wav",	WAV_CONOGRAPH2);
	Load_WAV("sounds/cards/monocle.wav",				WAV_MONOCLE);
	Load_WAV("sounds/cards/bauble.wav",					WAV_BAUBLE);
	Load_WAV("sounds/cards/mirror.wav",					WAV_MIRROR);
	Load_WAV("sounds/cards/mummy.wav",					WAV_MUMMY);
	Load_WAV("sounds/cards/monolith.wav",				WAV_MONOLITH);
	// music
	Load_WAV("sounds/music/start.wav",					WAV_MUS_START);
	Load_WAV("sounds/music/splash.wav",					WAV_MUS_SPLASH);
	Load_WAV("sounds/music/theme.wav",					WAV_MUS_THEME);
	Load_WAV("sounds/music/title.wav",					WAV_MUS_TITLE);
	Load_WAV("sounds/music/death.wav",					WAV_MUS_DEATH);
	Load_WAV("sounds/music/victory.wav",				WAV_MUS_VICTORY);
	Load_WAV("sounds/music/combat.wav",					WAV_MUS_COMBAT);
	Load_WAV("sounds/music/nebula.wav",					WAV_MUS_NEBULA);
	Load_WAV("sounds/music/hiscore.wav",				WAV_MUS_HISCORE);
	Load_WAV("sounds/music/rock.wav",						WAV_MUS_ROCK);
	Load_WAV("sounds/music/simulator.wav",			WAV_MUS_SIMULATOR);
}
// ----------------
//     INCLUDES
// ----------------

#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <memory.h>
#include <math.h>
#include <time.h>
#include <string.h>
#include <malloc.h>
#include <io.h>

#include "typedefs.h"
#include "iface_globals.h"
#include "gfx.h"
#include "is_fileio.h"
#include "snd.h"

//		FILE *loggy;

// --------------------------------------------
//      SPRITE CREATION AND MANAGEMENT
// --------------------------------------------

// CREATE NEW SPRITE TEMPLATE
t_ik_sprite *new_sprite(int32 w, int32 h)
{
	t_ik_sprite *spr;

	spr=(t_ik_sprite *)malloc(sizeof(t_ik_sprite));
	if (!spr)
		return NULL;

	spr->data=(uint8 *)malloc(w*h);
	spr->w=w;
	spr->h=h;
	spr->co=0;

	return spr;
}

// FIND APPROXIMATE COLOR OF SPRITE
int32 calc_sprite_color(t_ik_sprite *spr)
{
	int32 x,y,r,g,b,npx,c;

	if (!spr)
		return 0;

	r=0;g=0;b=0;npx=0;
	for (y=0;y<spr->h;y++)
	for (x=0;x<spr->w;x++)
	{
		c=spr->data[y*spr->w+x];
		if (c)
		{
			c=get_palette_entry(c);
			r+=(c>>16)&255; g+=(c>>8)&255; b+=c&255;
//			r+=pal[c*3]; g+=pal[c*3+1]; b+=pal[c*3+2];
			npx++;
		}
	}

	if (npx>0)
	{
		spr->co=get_rgb_color(r/npx,g/npx,b/npx);
	}

	return spr->co;
}

// GRAB SPRITE FROM IMAGE
t_ik_sprite *get_sprite(t_ik_image *img, int32 x, int32 y, int32 w, int32 h)
{
	t_ik_sprite *spr;
	int32 x1,y1;

	if (!img)
		return NULL;

	spr=new_sprite(w,h);
	if (!spr)
		return NULL;

	for (y1=0;y1<h;y1++)
		for (x1=0;x1<w;x1++)
		if (y1+y >= 0 && y1+y < img->h && x1+x >= 0 && x1+x < img->w)
		{
			spr->data[y1*w+x1]=img->data[(y1+y)*img->w+(x1+x)];
		}
		else
			spr->data[y1*w+x1]=0;

	calc_sprite_color(spr);

	return spr;
}

// DESTROY SPRITE AND FREE MEMORY
void free_sprite(t_ik_sprite *spr)
{
	if (spr)
	{
		if (spr->data)
			free(spr->data);
		free(spr);
	}
}

// --------------------------------------------
//     SPRITEPAK CREATION AND MANAGEMENT
// --------------------------------------------

t_ik_spritepak *new_spritepak(int32 num)
{
	t_ik_spritepak *pak;

	pak = (t_ik_spritepak*)calloc(1, sizeof(t_ik_spritepak));
	if (!pak)
		return NULL;

	pak->num = num;
	pak->spr = (t_ik_sprite**)calloc(num, sizeof(t_ik_sprite*));

	return pak;
}

void free_spritepak(t_ik_spritepak *pak)
{
	int x;

	if (!pak)
		return;

	for (x = 0; x < pak->num; x++)
	{
		free_sprite(pak->spr[x]);
		pak->spr[x]=NULL;
	}
	free(pak->spr);
	free(pak);
}

t_ik_spritepak *load_sprites(char *fname)
{
	// NOTE: load_sprites loads default .SPR, and FRAMES from the mod
	struct _finddata_t find;
	long fhandle;
	int fi;
	int fnum;
	char spritedir[256];
	char framename[256];
	int rep[256];
	int max;
	t_ik_image *img;
	uint8 *buffu;

	FILE *fil;
	t_ik_spritepak *pak;
	int32 x,num;
	int32 w,h,c;

/*
if loading a mod, look at the folder for new frames
and mark them in the replacement array.
*/
	for (x = 0; x < 256; x++)
		rep[x] = 0;
	max = 0;
	if (strlen(moddir))	
	{
		sprintf(spritedir, "%s%s", moddir, fname);
		sprintf(spritedir+strlen(spritedir)-4, "/\0");
		sprintf(framename, "%sframe*.tga", spritedir);

		fhandle = _findfirst(framename, &find);
		if (fhandle != -1)
		{
			fi = fhandle;
			while (fi != -1)
			{
				sscanf(find.name+5, "%03d", &fnum);
				if (fnum < 256)
				{
					rep[fnum] = 1;
					if (fnum+1 > max)
						max = fnum+1;
				}
				fi = _findnext(fhandle, &find);
			}
			_findclose(fhandle);
		}
	}

	fil=fopen(fname,"rb");	// don't use myopen here
	if (!fil)
		return NULL;

	num=fgetc(fil);
	num+=fgetc(fil)*256;

	if (num > max)
		max = num;

	pak = new_spritepak(max);
	if (!pak)
	{ fclose(fil); return NULL; }

	for (x=0;x<max;x++)
	{
		// header
		if (x < num)
		{
			w=fgetc(fil);
			w+=fgetc(fil)*256;
			h=fgetc(fil);
			h+=fgetc(fil)*256;
			c = fgetc(fil);
			fgetc(fil);
			fgetc(fil);
			fgetc(fil);
			buffu = (uint8*)malloc(w * h);
			fread(buffu,1,w*h,fil);

			if (!rep[x])
			{
			// if not marked as rep, make new sprite
				pak->spr[x]=new_sprite(w,h);
				pak->spr[x]->co=c;
				// data
				memcpy(pak->spr[x]->data, buffu, w*h);
			}

			free(buffu);
		}
		if (rep[x])
		{
			sprintf(framename, "%sframe%03d.tga", spritedir, x);
			img = ik_load_tga(framename, NULL);
			if (img)
			{
				pak->spr[x]=get_sprite(img, 0, 0, img->w, img->h);
				del_image(img);
			}
		}
	}

	fclose(fil);

	return pak;
}

void save_sprites(char *fname, t_ik_spritepak *pak)
{
	FILE *fil;
	int32 x;
	int32 num = pak->num;

	fil=myopen(fname,"wb");
	if (!fil)
		return;

	fputc(num&255, fil);
	fputc(num>>8, fil);

	for (x=0;x<num;x++)
	{
		// header
		fputc(pak->spr[x]->w & 255,fil);
		fputc(pak->spr[x]->w >> 8,fil);
		fputc(pak->spr[x]->h & 255,fil);
		fputc(pak->spr[x]->h >> 8,fil);
		fputc(pak->spr[x]->co,fil);
		// filler
		fputc(0,fil);
		fputc(0,fil);
		fputc(0,fil);
		// data
		fwrite(pak->spr[x]->data,1,pak->spr[x]->w*pak->spr[x]->h,fil);
	}
}

// --------------------------------------------
//	      SPRITE DRAWING FUNCTIONS
// --------------------------------------------

// basic sprite draw.. corner align, 0-masked
// flags:
// 1:  center align (move up-left by half the size)
// 2:  color 
// 4:  blank
void ik_dsprite(t_ik_image *img, int32 x, int32 y, t_ik_sprite *spr, int32 flags)
{
	int32 px,py;
	int32 yb,ye,xb,xe;
	uint8 co=0;
  uint8 *p1, *p2;

	if (flags&1) { x-=spr->w>>1; y-=spr->h>>1; }  // centered
	if (flags&2) { co=flags>>8; }  // colored
	if (flags&4) ik_drawbox(img, x, y, x+spr->w-1, y+spr->h-1, 0);

  if (x<c_minx-spr->w || y<c_miny-spr->h || x>=c_maxx || y>=c_maxy) return;

	yb=MAX(c_miny, y); ye=MIN(c_maxy, y+spr->h);
	xb=MAX(c_minx, x); xe=MIN(c_maxx, x+spr->w);

  for (py=yb; py<ye; py++)
  {
		px=xb;
    p1=ik_image_pointer(img,px,py); 
    p2=spr->data+((py-y)*spr->w)+(px-x);
		if (!co)
			for (;px<xe;px++)
      {
      	if (*p2)
					*p1=*p2;

        p1++;p2++;
      }
		else
  		for (;px<xe;px++)
      {
				if (*p2)
				{
      		if (*p2<16)
    	  	  *p1=*p2+co*16;
					else 
	        	*p1=*p2;
				}

        p1++;p2++;
      }
		}
}

// basic rsprite draw.. center align, rotation, scale (0-masked)
// flags:
// 1:  Light   (flags = 1 + lightcolor*256)
// 2:  Trans   (50% transparency)
// 4:  Add     
void ik_drsprite(t_ik_image *img, int32 x, int32 y, int32 r, int32 s, t_ik_sprite *spr, int32 flags)
{
  int32 x1,y1,x2;
	int32 size;
	int32 xt,yt,c,cx,cy;
	int32 cutleft, cutright;
	int32 dx,dy;
  uint8 *p1;

	if (s<=2) 
	{	
		c = spr->co;
		if (c)
		{
			if (y>=c_miny && x>=c_minx && x<c_maxx && y<c_maxy)
			{
				if (flags)
				{
			    p1=ik_image_pointer(img, x, y);

					if (flags&1)  c=gfx_lightbuffer[(c<<8)+(flags>>8)];
					if (flags&2)  c=gfx_transbuffer[(c<<8)+(*p1)];
					if (flags&4)  c=gfx_addbuffer[(c<<8)+(*p1)];

				  *p1=c;
				}
				else
					ik_putpixel(img, x,y,c);
			}
		}
		return;
	}

	s=(s<<10)/MAX(spr->w,spr->h);
  size=MAX(spr->w,spr->h)*s>>11; 

  if (x<c_minx-size || y<c_miny-size || x>=c_maxx+size || y>=c_maxy+size) return;

	r &= 1023;
	dx = cos1k[r]*1024/s;
	dy = -sin1k[r]*1024/s;
	cx=(spr->w+1)<<15;
	cy=(spr->h+1)<<15;

	size=(int32)(size*1.4);

  for (y1=-size; y1<size; y1++)
  {
  	if (y1+y>=c_miny && y1+y<c_maxy)
    {
      x2=x+size;if (x2>c_maxx) x2=c_maxx;
			x1=x-size;if (x1<c_minx) x1=c_minx;
			xt=cx+(x1-x)*dx-y1*dy;
			yt=cy+y1*dx+(x1-x)*dy;

			cutleft=0; cutright=0;
			// Clamp X
			if (dx>0)
			{
				if (xt+(x2-x1)*dx>spr->w<<16) 	cutright=MAX(cutright, (xt+(x2-x1)*dx-(spr->w<<16))/dx); 
				if (xt<0)												cutleft=MAX(cutleft, -xt/dx+1); 
			}
			else if (dx<0)
			{
				if (xt+(x2-x1)*dx<0)			cutright=MAX(cutright, (xt+(x2-x1)*dx)/dx); 
				if (xt>spr->w<<16)				cutleft=MAX(cutleft, -(xt-(spr->w<<16))/dx+1);
			}
			else if (xt<0 || xt>=spr->w<<16)	 x2=x1;   // don't draw hline

			// Clamp Y
			if (x2>x1)
			if (dy>0)
			{
				if (yt+(x2-x1)*dy>spr->h<<16)			cutright=MAX(cutright, (yt+(x2-x1)*dy-(spr->h<<16))/dy);
				if (yt<0)													cutleft=MAX(cutleft, -yt/dy+1);
			}
			else if (dy<0)
			{
				if (yt+(x2-x1)*dy<0)				cutright=MAX(cutright, (yt+(x2-x1)*dy)/dy); 
				if (yt>spr->h<<16)					cutleft=MAX(cutleft, -(yt-(spr->h<<16))/dy+1); 
			}
			else if (yt<0 || yt>=spr->h<<16)	x2=x1;  // don't draw hline

			// Apply clamps
			if (cutleft)
			{ xt+=dx*cutleft; yt+=dy*cutleft; x1+=cutleft; }
			if (cutright)
			{ x2-=cutright; }

      p1=ik_image_pointer(img, x1, y1+y);

			// innerloops
			if (!flags)  // "clean" .. no special fx .. fast
				for (;x1<x2;x1++)
		    {
					c=spr->data[(yt>>16)*spr->w+(xt>>16)];
					if (c)
					  *p1=c;

	        p1++;
					xt+=dx; yt+=dy;
				}
			else  // light, transparency, additive
				for (;x1<x2;x1++)
		    {
					c=spr->data[(yt>>16)*spr->w+(xt>>16)];
					if (c)
					{
						if (flags&1)  c=gfx_lightbuffer[(c<<8)+(flags>>8)];
						if (flags&2)  c=gfx_transbuffer[(c<<8)+(*p1)];
						if (flags&4)  c=gfx_addbuffer[(c<<8)+(*p1)];
					  *p1=c;
					}

	        p1++;
					xt+=dx; yt+=dy;
				}    
    }
  }
}

// sprite line draw.. line of tiled sprites (useful for laser beams etc)
// flags:
// 1:  Light   (flags = 1 + lightcolor*256)
// 2:  Trans   (50% transparency)
// 4:  Add     

void ik_dspriteline(t_ik_image *img, int32 xb, int32 yb, int32 xe, int32 ye, int32 s, 
										int32 offset, int32 ybits, t_ik_sprite *spr, int32 flags)
{
	double r;
  int32 x1,y1,x2;
	int32 size;
	int32 xt,yt,c,cx;
	int32 cutleft, cutright;
	int32 dx,dy;
	int32 xl0,yl0,xl1,yl1,topy;
  uint8 *p1;

	if (s<=2) 
	{	
		ik_drawline(img, xb,yb,xe,ye,spr->co);
		return;
	}

	s=(s<<6)/MAX(spr->w,spr->h);
  size=MAX(spr->w,spr->h)*s>>7; 

	xl0=MAX(c_minx, MIN(xb-size, xe-size));
	xl1=MIN(c_maxx, MAX(xb+size, xe+size));
	yl0=MAX(c_miny, MIN(yb-size, ye-size));
	yl1=MIN(c_maxy, MAX(yb+size, ye+size));

	if (xl0>xl1 || yl0>yl1) return;  // if clipped out
  if (xl1<c_minx || yl1<c_miny || xl0>=c_maxx || yl0>=c_maxy) return;

	r = atan2((double)xe - xb, (double)yb - ye);
	dx=(int32)(cos(r)*65536*64/s);
	dy=-(int32)(sin(r)*65536*64/s);
	cx=(spr->w+1)<<15;

//	topy=-(int32)sqrt((xe-xb)*(xe-xb)+(ye-yb)*(ye-yb))*(65536*64/s);
	topy=(ye-yb)*dx+(xe-xb)*dy;

  for (y1=yl0; y1<yl1; y1++)
  {
  	if (y1>=c_miny && y1<c_maxy)
    {
      x2=xl1;if (x2>c_maxx) x2=c_maxx;
			x1=xl0;if (x1<c_minx) x1=c_minx;
			xt=cx+(x1-xb)*dx-(y1-yb)*dy;
			yt=(y1-yb)*dx+(x1-xb)*dy;

			cutleft=0; cutright=0;
			// Clamp X
			if (dx>0)
			{
				if (xt+(x2-x1)*dx>spr->w<<16) 	cutright=MAX(cutright, (xt+(x2-x1)*dx-(spr->w<<16))/dx); 
				if (xt<0)												cutleft=MAX(cutleft, -xt/dx+1); 
			}
			else if (dx<0)
			{
				if (xt+(x2-x1)*dx<0)			cutright=MAX(cutright, (xt+(x2-x1)*dx)/dx); 
				if (xt>spr->w<<16)				cutleft=MAX(cutleft, -(xt-(spr->w<<16))/dx+1);
			}
			else if (xt<0 || xt>=spr->w<<16)	x2=x1;  // don't draw hline

			// Clamp Y
			if (x2>x1)
			if (dy>0)
			{
				if (yt+(x2-x1)*dy>0)		cutright=MAX(cutright, (yt+(x2-x1)*dy)/dy);
				if (yt<topy)						cutleft=MAX(cutleft, (topy-yt)/dy+1);
			}
			else if (dy<0)
			{
				if (yt+(x2-x1)*dy<topy)		cutright=MAX(cutright, -(topy-(yt+(x2-x1)*dy))/dy+1); 
				if (yt>0)									cutleft=MAX(cutleft, -yt/dy+1); 
			}
			else if (yt<topy || yt>=0)	 x2=x1;   // don't draw hline

			// Apply clamps
			if (cutleft)
			{ xt+=dx*cutleft; yt+=dy*cutleft; x1+=cutleft; }
			if (cutright)
			{ x2-=cutright; }

      p1=ik_image_pointer(img, x1, y1);

			// innerloops
			if (!flags)  // "clean" .. no special fx .. fast
				for (;x1<x2;x1++)
		    {
					c=spr->data[(((yt>>ybits)+offset)&(spr->h-1))*spr->w+(xt>>16)];
					if (c)
					  *p1=c;

	        p1++;
					xt+=dx; yt+=dy;
				}
			else  // light, transparency, additive
				for (;x1<x2;x1++)
		    {
					c=spr->data[(((yt>>ybits)+offset)&(spr->h-1))*spr->w+(xt>>16)];
					if (c)
					{
						if (flags&1)  c=gfx_lightbuffer[(c<<8)+(flags>>8)];
						if (flags&2)  c=gfx_transbuffer[(c<<8)+(*p1)];
						if (flags&4)  c=gfx_addbuffer[(c<<8)+(*p1)];
					  *p1=c;
					}

	        p1++;
					xt+=dx; yt+=dy;
				}    
    }
  }
}

// ----------------
//     INCLUDES
// ----------------

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <math.h>

#include "typedefs.h"
#include "is_fileio.h"

#include "textstr.h"
#include "iface_globals.h"
#include "gfx.h"
#include "snd.h"
#include "interface.h"
#include "combat.h"
#include "cards.h"
#include "endgame.h"

#include "starmap.h"

// ----------------
//		CONSTANTS
// ----------------

// ----------------
// GLOBAL VARIABLES
// ----------------

t_player		player;
t_hud				hud;

int starmap_tutorialtype;

// ----------------
// LOCAL VARIABLES
// ----------------

int32 kawangi_score;
int32 kawangi_splode;
int32 kawangi_incoming;
int32 timer_warning;

// ----------------
// LOCAL PROTOTYPES
// ----------------

void help_screen();

void starmap_displayship(int32 t, int32 st);
int starmap_findstar(int32 mx, int32 my);


void killstar(int32 c);

void starmap_flee();
int32 simulate_move(int32 star);

// ----------------
// GLOBAL FUNCTIONS
// ----------------

void starmap()
{
	int32 t, t0;
	int32 c, mc;
	int32 mx, my;
	int32 mx2, my2;
	int32 d, s;
	int32 upd;
	int32 sp1, sp2;
	int32 end = 0;
	int32 bu[3], nbu;
	char texty[256];
	char topic[32];
	char hisher[8];

	kawangi_score = 0;
	kawangi_splode = 0;
	kawangi_incoming = 0;
	timer_warning = 0;

	ik_inkey();
	start_ik_timer(0, 1000/STARMAP_FRAMERATE); t0 = t = 0; upd=1;
	Play_Sound(WAV_BRIDGE, 15, 1, 50);

	starmap_tutorialtype = tut_starmap;

	while (!must_quit && !end)
	{
		t0 = t;
		ik_eventhandler();  // always call every frame
		t = get_ik_timer(0);
		c = ik_inkey();
		mc = ik_mclick();	
		mx = ik_mouse_x; my = ik_mouse_y;

		if (must_quit)
		{
			must_quit = 0;
			Play_SoundFX(WAV_DESELECT);
			if (!interface_popup(font_6x8, 240, 200, 160, 72, STARMAP_INTERFACE_COLOR, 0, 
					textstring[STR_QUIT_TITLE], textstring[STR_QUIT_CONFIRM], 
					textstring[STR_YES], textstring[STR_NO]))
			{	must_quit = 1; player.death = 666; }
		}

		if (!player.enroute && player.num_ships>0)
		{
			if ((c == 13 || c == 32) && player.target > -1 && player.target != player.system)
			{	player.engage = 1; player.fold = 0; }

			if (key_pressed(key_f[0]))
			{	
				help_screen();
				upd = 1;
			}

			if (mx > SM_MAP_X && mx < SM_MAP_X + 480 && my < SM_MAP_Y + 480)
			{
			if (my < SM_MAP_Y + 16 && (mc&1))
			{
				if (!settings.opt_timeremaining || player.stardate >= 365*10)
				{
					if (mx > SM_MAP_X+394 && mx < SM_MAP_X+406)
					{	
						Play_SoundFX(WAV_WAIT);
						starmap_advancedays(1);
					}
					else if (mx > SM_MAP_X+412 && mx < SM_MAP_X+430)
					{
						Play_SoundFX(WAV_WAIT);
						s = 0;
						for (d = 0; d < 12; d++)
							if (player.stardate%365 >= months[d].sd)
								s = d;
						starmap_advancedays(months[s].le);
					}
					else if (mx > SM_MAP_X+436 && mx < SM_MAP_X+460)
					{
						Play_SoundFX(WAV_WAIT);
						starmap_advancedays(365);
					}
				}
				else
				{
					if (mx > SM_MAP_X+436 && mx < SM_MAP_X+460)
					{
						s = (SM_MAP_X+459-mx)/6;
						d = 1;
						while (s--)
							d*=10;
						Play_SoundFX(WAV_WAIT);
						starmap_advancedays(d);
					}
				}
			}
			else
			{
				if (mc & 1)
				{
					// check for engage or fold space
					mx2 = 0;
					bu[0] = 0; nbu = 1;
					if (player.distance <= 2380)
					{
						for (d = 0; d < player.num_ships; d++)
							if (shiptypes[player.ships[d]].flag & 4)	// kuti
								bu[nbu++] = 1;
						if (player.target>-1 && sm_stars[player.target].explored==0)
							for (d = 0; d < shiptypes[0].num_systems; d++)
								if (shipsystems[shiptypes[0].system[d]].type == sys_misc && shipsystems[shiptypes[0].system[d]].par[0]==0)
									bu[nbu++] = 2;
					}

					if (player.target>-1 && player.target!=player.system)
					if (mx > sm_stars[player.target].ds_x-16 && mx < sm_stars[player.target].ds_x+16)
					{
						d = (sm_stars[player.target].y<sm_stars[player.system].y)-(sm_stars[player.target].y>=sm_stars[player.system].y);
						my2 = sm_stars[player.target].ds_y + (12+nbu*5)*d - nbu*5;
						if (my >= my2 && my < my2 + nbu*10)
						{
							d = (my - my2) / 10;
							if (bu[d] == 0 || bu[d] == 1)
							{
								player.engage = 1;
								if (bu[d] == 1)
								{	// fold space
									player.fold = 1; 

								}
								else
								{	
									player.fold = 0; 
								}
							}
							else if (bu[d] == 2)	// scan
							{
								Stop_Sound(15);
								probe_exploreplanet(1);
								Play_Sound(WAV_BRIDGE, 15, 1, 50);
							}
							mx2 = 1;
						}
					}
					if (player.target == player.system) 				// hire and hunt buttons
					if (mx > sm_stars[player.target].ds_x-16 && mx < sm_stars[player.target].ds_x+16)
					{
						d = c; 
						c = sm_stars[player.target].card;
						if (my > sm_stars[player.target].ds_y+11 && my < sm_stars[player.target].ds_y+23)
						{
							if (ecards[c].type == card_ally) 
							{
								mx2 = 1;
								Play_Sound(WAV_ALLY, 15, 1);
								sprintf(texty, textstring[STR_MERC_DEAL], hulls[shiptypes[ecards[c].parm].hull].name, shiptypes[ecards[c].parm].name);
								if (!interface_popup(font_6x8, 224, 192, 192, 96, STARMAP_INTERFACE_COLOR, 0, 
																		textstring[STR_MERC_TITLE], texty, textstring[STR_YES], textstring[STR_NO]))
								{
									Stop_Sound(15);
									player.ships[player.num_ships] = ecards[c].parm;
									//player.sel_ship = player.num_ships;
									player.num_ships++;
									starmap_tutorialtype = tut_ally;

									if (shiptypes[player.ships[player.num_ships-1]].flag & 64)
										sprintf(hisher, textstring[STR_MERC_HER]);
									else
										sprintf(hisher, textstring[STR_MERC_HIS]);

									sprintf(texty, textstring[STR_MERC_PAYMENT], 
													hulls[shiptypes[player.ships[player.num_ships-1]].hull].name,
													shiptypes[player.ships[player.num_ships-1]].name,
													hisher);
									if (pay_item(textstring[STR_MERC_BILLING], texty, shiptypes[ecards[c].parm].race) == -1)
									{
										starmap_removeship(player.num_ships-1);
										starmap_tutorialtype = tut_starmap;
									}
									else
										sm_stars[player.target].card = 0;
									player.sel_ship = 0;
								}
								Play_Sound(WAV_BRIDGE, 15, 1, 50);
							}

							else if	(ecards[c].type == card_lifeform)
							{
								mx2 = 1;
								Play_SoundFX(WAV_LIFEFORM);
								s = ecards[c].parm;
								if (s > -1 && itemtypes[s].flag && lifeform_hard)
								{
									my2 = itemtypes[s].cost/10 + (player.target*7)%(itemtypes[s].cost/10);
									starmap_additem(s, 0);
									sprintf(texty, textstring[STR_LIFEFORM_HUNT], itemtypes[s].name, my2);
									if (!interface_popup(font_6x8, 224, 192, 192, 0, STARMAP_INTERFACE_COLOR, 0, textstring[STR_LIFEFORM_HUNTT], texty, textstring[STR_YES], textstring[STR_NO]))
									{
										starmap_advancedays(my2);
										sm_stars[player.system].card = 0;
									}
									else
										starmap_removeitem(player.num_items-1);
									Stop_All_Sounds();
								}
								Play_Sound(WAV_BRIDGE, 15, 1, 50);
							}

						}
						c = d;
					}

					// if no movement, check for new selection
					if (!mx2)
					{
						d = starmap_findstar(mx, my);
						player.target = d;
						if (player.target > -1)
						{
							player.distance = starmap_stardist(player.system, player.target);
							player.nebula = starmap_nebuladist(player.system, player.target);
							Play_SoundFX(WAV_SELECTSTAR);
						}
						else
							Play_SoundFX(WAV_DESELECT);
					}
				}
			}
			}
			else
			{
				if (mx > SM_SHIP_X && mx < SM_SHIP_X + 160 && my > SM_SHIP_Y + 24 && my < SM_SHIP_Y + 256)
				{
					mx2 = mx - SM_SHIP_X; my2 = my - SM_SHIP_Y;
					if (mc & 1)
					{
						// select ship
						if (mx > SM_SHIP_X + 16 && mx < SM_SHIP_X + 80 && my > SM_SHIP_Y + 24 && my < SM_SHIP_Y + 40)
						{
							d = (mx-16-SM_SHIP_X)/16;
							if (d < player.num_ships)
							{	player.sel_ship = d; player.sel_ship_time = t; Play_SoundFX(WAV_SELECTSHIP,t); }
						}
						// repair hull
						if (mx > SM_SHIP_X + 80 && mx < SM_SHIP_X + 144 && my > SM_SHIP_Y + 24 && my < SM_SHIP_Y + 40)
						{
							if (shiptypes[player.ships[player.sel_ship]].hits < hulls[shiptypes[player.ships[player.sel_ship]].hull].hits*256)
							{
								sp1 = (hulls[shiptypes[player.ships[player.sel_ship]].hull].hits*256 - shiptypes[player.ships[player.sel_ship]].hits) / 128;
								for (d = 0; d < shiptypes[player.ships[player.sel_ship]].num_systems; d++)
									if (shipsystems[shiptypes[player.ships[player.sel_ship]].system[d]].type == sys_damage)
										sp1 = 2;

								Play_SoundFX(WAV_INSTALL, t);
								sprintf(texty, textstring[STR_INV_REPAIR_HULL], sp1);
								sp2 = interface_popup(font_6x8, SM_SHIP_X + 32*(SM_SHIP_X==0) - 64*(SM_SHIP_X>0), SM_SHIP_Y+40, 192, 72, STARMAP_INTERFACE_COLOR, 0, 
																		textstring[STR_INV_REPAIR_TITLE], texty, textstring[STR_YES], textstring[STR_NO]);
								if (!sp2)
								{
									starmap_advancedays(sp1);
									upd=1;
								}
							}
						}
						// uninstall
						if (mx2 > 24 && mx2 < 144 && my2 > 168 && my2 < 168 + shiptypes[player.ships[player.sel_ship]].num_systems*8)
						{
							if (mx2 > 32 && mx2 < 144) // get info
							{
								upd = shipsystems[shiptypes[player.ships[player.sel_ship]].system[(my2-168)/8]].item;
								if (upd > -1)
								{
									Play_SoundFX(WAV_INFO);
									interface_popup(font_6x8, SM_SHIP_X + 32*(SM_SHIP_X==0) - 64*(SM_SHIP_X>0), SM_SHIP_Y+24, 192, 112, STARMAP_INTERFACE_COLOR, 0, 
																	itemtypes[upd].name, itemtypes[upd].text, textstring[STR_OK]);
									upd=1;
								}
								else
								{
									Play_SoundFX(WAV_DESELECT, t);
									upd = 1;
								}
							}
							if (mx2 > 24 && mx2 < 32)
							{
								if (!shiptypes[player.ships[player.sel_ship]].sysdmg[(my2-168)>>3])
								{
									if (player.sel_ship == 0)
									{
										Play_SoundFX(WAV_INSTALL, t);					
										starmap_uninstallsystem( (my2-168)>>3, 0);
									}
								}
								else	// repair damages
								{
									sp1 = (int32)(sqrt((double)itemtypes[shipsystems[shiptypes[player.ships[player.sel_ship]].system[(my - 168) >> 3]].item].cost)*.75);
									sprintf(texty, textstring[STR_INV_REPAIR_SYS], itemtypes[shipsystems[shiptypes[player.ships[player.sel_ship]].system[(my-168)>>3]].item].name, sp1);
									Play_SoundFX(WAV_INSTALL, t);
									sp2 = interface_popup(font_6x8, SM_SHIP_X + 32*(SM_SHIP_X==0) - 64*(SM_SHIP_X>0), SM_SHIP_Y+160, 192, 72, STARMAP_INTERFACE_COLOR, 0, 
																	textstring[STR_INV_REPAIR_TITLE], texty, textstring[STR_YES], textstring[STR_NO]);
									if (!sp2)
									{
										shiptypes[player.ships[player.sel_ship]].sysdmg[(my-168)>>3] = 0;
										starmap_advancedays(sp1);
										upd=1;
									}
								}
							}
						}
					}
				}
				if (mx > SM_INV_X && mx < SM_INV_X + 160 && my > SM_INV_Y + 24 && my < SM_INV_Y + 136)
				{
					mx2 = mx - SM_INV_X; my2 = my - SM_INV_Y;
					if (mc & 1)
					{
						/*if (player.sel_ship != 0)
						{
							
							player.sel_ship = 0;
							prep_screen();
							starmap_display(t);
							ik_blit();
						} */
						if (mx2 > 16 && mx2 < 136 && my2 < 120)
						{
							d = (my2 - 24)/8 + hud.invslider;
							if (d < player.num_items)
							{
								hud.invselect = d;
								if ( mx2 < 24 )
								{
									if (itemtypes[player.items[d]].type == item_system || itemtypes[player.items[d]].type == item_weapon)
									{
										if (player.sel_ship == 0)	// your ship
										{
											Play_SoundFX(WAV_INSTALL, t);
											starmap_installitem(hud.invselect);
											upd=1;
										}
										else
										{
											Play_SoundFX(WAV_DESELECT);
											if (ally_install(player.sel_ship, player.items[d], 0) > -1)
												starmap_removeitem(d);
										}
									}
									else if (itemtypes[player.items[d]].type == item_device)
									{
										Play_SoundFX(WAV_INFO);
										d = itemtypes[player.items[hud.invselect]].flag;
										if (d & device_beacon)
										{	// beacon
											Stop_Sound(15);
											if (!sm_stars[player.system].novadate || sm_stars[player.system].novatype==2)
												klakar_trade();
											else
											{
												Play_SoundFX(WAV_INFO);
												Play_Sound(WAV_MESSAGE, 15, 1);
												sprintf(texty, textstring[STR_KLAK_UNSAFE], sm_stars[player.system].starname);
												interface_popup(font_6x8, SM_INV_X + 32*(SM_INV_X==0) - 64*(SM_INV_X>0), SM_INV_Y+24, 192, 112, STARMAP_INTERFACE_COLOR, 0, 
																		textstring[STR_KLAK_UNAVAIL], texty, textstring[STR_OK]);
												upd=1;
											}
											Play_Sound(WAV_BRIDGE, 15, 1, 50);
										}
										else if (d & device_probe)
										{
											Stop_Sound(15);
											if (stellar_probe(itemtypes[player.items[hud.invselect]].name))
												starmap_removeitem(hud.invselect);
											Play_Sound(WAV_BRIDGE, 15, 1, 50);
										}

										else if (d & device_collapser)
										{
											if (use_vacuum_collapser(itemtypes[player.items[hud.invselect]].name))
											{
												starmap_removeitem(hud.invselect);
											}

											//end = 1; player.death = 3;
										}
										else if (d & device_mirror)
										{
											Stop_Sound(15);
											eledras_mirror(itemtypes[player.items[hud.invselect]].name);
											Play_Sound(WAV_BRIDGE, 15, 1, 50);
										}
										else if (d & device_bauble)
										{
											Stop_Sound(15);
											if (eledras_bauble(itemtypes[player.items[hud.invselect]].name))
												starmap_removeitem(hud.invselect);
											Play_Sound(WAV_BRIDGE, 15, 1, 50);
										}
										else if (d & device_conograph)
										{
											Stop_Sound(15);
											use_conograph(itemtypes[player.items[hud.invselect]].name);
											Play_Sound(WAV_BRIDGE, 15, 1, 50);
										}

									}
								}
								else 
								{
									Play_SoundFX(WAV_INFO);
									interface_popup(font_6x8, SM_INV_X + 32*(SM_INV_X==0) - 64*(SM_INV_X>0), SM_INV_Y+24, 192, 112, STARMAP_INTERFACE_COLOR, 0, 
																	itemtypes[player.items[hud.invselect]].name, itemtypes[player.items[hud.invselect]].text, textstring[STR_OK]);
									upd=1;
								}
							}
							else
							{
								hud.invselect = -1;
								Play_SoundFX(WAV_DESELECT);
							}
						}
						else if (mx2 > 136 && mx2 < 144 && my2 < 120)
						{
							if (player.num_items > 12)
							{
								Play_SoundFX(WAV_SLIDER);
								if (my2 > 32 && my2 < 112)
									hud.invslider = MIN(((my2 - 32)*(player.num_items-12)+40) / 80, player.num_items-12);
								else if (my2 < 32)
									hud.invslider = MAX(0, hud.invslider-1);
								else if (my2 > 112)
									hud.invslider = MIN(player.num_items-12, hud.invslider+1);
							}
						}
					}
				}
				
			}
		
			if (player.engage)
			{
				if (player.target == homesystem) // going home
				{
					Play_SoundFX(WAV_DESELECT);
					if (!interface_popup(font_4x8, 240, 200, 160, 72, STARMAP_INTERFACE_COLOR, 0, 
							textstring[STR_ENDGAME_CONFIRM1], textstring[STR_ENDGAME_CONFIRM2], textstring[STR_YES], textstring[STR_NO]))
					{
						t0 = t;
						t = get_ik_timer(0);
						Stop_All_Sounds();
						player.enroute = 1;
					}
				}
				else
				{
					player.enroute = 1;
				}
				if (player.engage != 2)
				if ((shiptypes[0].engine==-1 || shiptypes[0].sysdmg[shiptypes[0].sys_eng]) && !player.fold)
				{
					if (shiptypes[0].engine==-1)
					{
						sprintf(topic, textstring[STR_DRIVE_MISSING]);
						sprintf(texty, textstring[STR_DRIVE_MISSING2]);
					}
					else
					{
						sprintf(topic, textstring[STR_DRIVE_BROKEN]);
						sprintf(texty, textstring[STR_DRIVE_BROKEN2]);
					}

					Play_SoundFX(WAV_DESELECT);
					if (!interface_popup(font_4x8, 240, 200, 160, 72, STARMAP_INTERFACE_COLOR, 0, 
							topic, texty, textstring[STR_YES], textstring[STR_NO]))
					{
						t0 = t;
						t = get_ik_timer(0);
						Stop_All_Sounds();
						player.enroute = 1;
					}
					else
						player.enroute = 0;
				}
				if (player.enroute)
				{

					if (player.fold || (shiptypes[0].engine>-1 && shipsystems[shiptypes[0].engine].par[0]==666))
					{	
						if (player.fold)
						{
							if (player.stardate - player.foldate < 7)
							{
								if (sm_stars[player.system].novadate>0 && player.stardate - player.hypdate < 60 )
								{	// attempting to hyper out of nova
									Play_SoundFX(WAV_DESELECT);
									if (!interface_popup(font_4x8, 240, 200, 160, 72, STARMAP_INTERFACE_COLOR, 0, 
											textstring[STR_DRIVE_NOVA1], textstring[STR_DRIVE_NOVA3], textstring[STR_YES], textstring[STR_NO]))
									{
										t0 = t;
										t = get_ik_timer(0);
										Stop_All_Sounds();
									}
									else
										player.enroute = 0;
								}
								if (player.enroute)
								{
									starmap_advancedays(7 - (player.stardate - player.foldate));
									prep_screen();
									starmap_display(t);
									ik_blit();
								}
							}
							if (player.enroute)
							{
								player.foldate = player.stardate;
								Play_SoundFX(WAV_FOLDSPACE, t);
							}
						}
						else
						{
							if (player.stardate - player.hypdate < 60)
							{	
								if (sm_stars[player.system].novadate>player.stardate && player.stardate - player.hypdate < 60 )
								{	// attempting to hyper out of nova
									Play_SoundFX(WAV_DESELECT);
									if (!interface_popup(font_4x8, 240, 200, 160, 72, STARMAP_INTERFACE_COLOR, 0, 
											textstring[STR_DRIVE_NOVA1], textstring[STR_DRIVE_NOVA2], textstring[STR_YES], textstring[STR_NO]))
									{
										t0 = t;
										t = get_ik_timer(0);
										Stop_All_Sounds();
									}
									else
										player.enroute = 0;
								}
								if (player.enroute)
								{
									starmap_advancedays(60 - (player.stardate - player.hypdate));
									prep_screen();
									starmap_display(t);
									ik_blit();
								}
							}
							if (player.enroute)
							{
								player.hypdate = player.stardate;
								Play_SoundFX(WAV_HYPERDRIVE, t);
							}
						}

						if (player.enroute)
							player.hyptime = t;
					}
					else

					{
						Play_SoundFX(WAV_DEPART, t);
					}
					ik_print_log("Set course for %s system.\n", sm_stars[player.target].starname);
				}

				player.engage = 0;
			}
		}

		if (t>t0 || upd)
		{
			if (upd)
				upd = 0;

			if (player.enroute)
			{
				s = player.enroute;
				if (shiptypes[0].engine > -1 && shiptypes[0].sysdmg[shiptypes[0].sys_eng]==0)
				{
					sp1 = shipsystems[shiptypes[0].engine].par[0];
					sp2 = shipsystems[shiptypes[0].engine].par[1];
				}
				else if (shiptypes[0].thrust > -1 && shiptypes[0].sysdmg[shiptypes[0].sys_thru]==0)
				{
					sp1 = 1; sp2 = 1;
				}
				else
				{
					sp1 = 0; sp2 = 0;
				}

				if (player.fold)	// fold
				{
					if (t > player.hyptime + 125)
					{
						player.enroute = player.distance;
					}
				}
				else if (sp1 == 666)		// hyperdrive
				{
					if (t > player.hyptime + 125)
					{
						player.enroute = player.distance;
					}
				}
				else	// normal drives
				{
					if (sp1 > 0 && sp2 > 0)
					{
						if (sm_nebulamap[((240-player.y)<<9)+(240+player.x)])
							player.enroute += sp2*2;
						else
							player.enroute += sp1*2;
						starmap_advancedays(2);
					}
					else
					{
						player.enroute += 1;
						starmap_advancedays(4);
					}
					if (player.enroute >= player.distance-640 && s < player.distance-640)
						Play_SoundFX(WAV_ARRIVE, t);

					// find black holes?
					for (s = 0; s < num_holes; s++)
					if (sm_holes[s].size>0)
					{
						if (!sm_holes[s].explored)
						{
							d = get_distance(sm_holes[s].x - player.x, sm_holes[s].y - player.y);
							if (d < 32)
							{
								Stop_Sound(15);
								d = starmap_explorehole(s, t);
								Play_Sound(WAV_BRIDGE, 15, 1, 50);
								// returns 0 if [go back], 1 if [continue]
								if (!d) // turn around
								{
									player.enroute = player.distance - player.enroute;
									d = player.system;
									player.system = player.target; 
									player.target = d;
								}
							}
		//						sm_holes[s].explored = 1;
						}
						else
						{
							d = get_distance(sm_holes[s].x - player.x, sm_holes[s].y - player.y);
							if (sp1>0)
							{
								if (d < 96/(MAX(sp1,6)) )
								{
			//						player.num_ships = 0;
									player.death = 2;
									player.hole = s;
									end = 1;
								}
							}
							else if (d < 96/6)
							{
		//						player.num_ships = 0;
								player.death = 2;
								player.hole = s;
								end = 1;
							}
						}
					}
				}

				// arrive at destination?
				if (player.enroute >= player.distance)
				{
					//d = ((player.distance*(256-player.nebula)*365)/sp1 + (player.distance*player.nebula*365)/sp2)>>16;
					//player.stardate += d;

					player.system = player.target;
					player.x = sm_stars[player.system].x;
					player.y = sm_stars[player.system].y;
					player.enroute = 0;
					player.distance = 0;
					player.explore = 1;

					ik_print_log("Arrived at %s system.\n", sm_stars[player.system].starname);
/*
					if (!sm_stars[player.target].explored)
						player.card = rand()%num_ecards;
					sm_stars[player.target].explored = 1;
*/
				}
			}

			resallhalfbritescreens();	// cleanup

			prep_screen();
//		interface_popup(font_6x8, 0,0,128,64,0,0,"hump", "blarg", "ok");
			starmap_display(t);

			ik_blit();

			if (settings.random_names & 4)
				interface_tutorial(starmap_tutorialtype);
		}

		if (player.explore==1)
		{
			Stop_Sound(15);
			player.explore = 0;
			s = starmap_entersystem();
			if ((player.num_ships == 0 || player.ships[0] != 0) && player.death == 0)
			{	end = 1; player.death = 1; }
			else if (player.death)
			{	end = 1; }
			else if (s)
			{
				prep_screen();
				starmap_display(t);
				ik_blit();
				starmap_exploreplanet();
				if (player.system == homesystem)
				{	end = 1; }
			}
			else	// flee
			{
				starmap_flee();
			}
			upd=1;
			Play_Sound(WAV_BRIDGE, 15, 1, 50);
		}


		for (s = 0; s < num_stars; s++)
			if (sm_stars[s].color > -2 && sm_stars[s].novatype == 2 &&
					player.stardate >= sm_stars[s].novadate)
			{
				vacuum_collapse(s);
			}

		for (s = 0; s < STARMAP_MAX_FLEETS; s++)
		if (sm_fleets[s].race == race_kawangi) 
		{	
			if (sm_fleets[s].num_ships > 0 && sm_fleets[s].system == homesystem && sm_fleets[s].distance == 0)
			{
				kawangi_warning();
				sm_fleets[s].explored = 2;
				sm_fleets[s].distance = player.stardate + 365;
			}
			else if (sm_fleets[s].num_ships == 0)	// destroyed the kawangi
			{
				if (player.num_ships > 0)
					kawangi_message(s, 0);
				sm_fleets[s].race = race_unknown;
				player.bonusdata += 2000;
			}

			if (kawangi_splode)	// && sm_stars[sm_fleets[s].system].novatime>0 && get_ik_timer(2)>sm_stars[sm_fleets[s].system].novatime+100)
			{
				sm_stars[sm_fleets[s].system].novadate = player.stardate;
				sm_stars[sm_fleets[s].system].novatype = 1;
				kawangi_splode = 0;
				if (kawangi_score < 2)
				{
					kawangi_message(s, 1);
					Play_Sound(WAV_BRIDGE, 15, 1, 50);
				}
			}

			if (kawangi_incoming)
			{
				kawangi_incoming = 0;
				if (fleet_encounter(s, 1))
				{
					starmap_meetrace(sm_fleets[s].race);
					enemy_encounter(sm_fleets[s].race);
					must_quit = 0;
					combat(s, 0);
					player.sel_ship = 0;
					if ((player.num_ships == 0 || player.ships[0] != 0) && player.death == 0)
					{	player.death = 1; }
					if (sm_fleets[s].num_ships>0 && player.num_ships>0 && player.ships[0]==0)
					{	// flee
						starmap_flee();
					}
				}
				else
					starmap_flee();
			}

		}


		if (timer_warning)
		{
			if (timer_warning == 1)
			{
				Play_SoundFX(WAV_TIMER);
				interface_popup(font_6x8, 224, 200, 192, 80, STARMAP_INTERFACE_COLOR, 0, 
												textstring[STR_TIMER_TITLE], textstring[STR_TIMER_WARN1], textstring[STR_OK]);
			}
			else if (timer_warning == 2)
			{
				Play_SoundFX(WAV_TIMER);
				interface_popup(font_6x8, 224, 200, 192, 80, STARMAP_INTERFACE_COLOR, 0, 
												textstring[STR_TIMER_TITLE], textstring[STR_TIMER_WARN2], textstring[STR_OK]);
			}
			timer_warning = 0;
			Play_Sound(WAV_BRIDGE, 15, 1, 50);
		}

		if (player.num_ships == 0 || sm_stars[homesystem].planet == 10 || sm_stars[homesystem].color == -2)
			end=1;
		if (player.death == 666)
			must_quit=1;
	}

	// if quit, quit
	if (player.death == 666)
	{
		Stop_Sound(15);
		return;
	}

	// display effects of collapsers, black holes etc here
	if (end)
	{
		sp1 = player.stardate;
		s = t0 = t = get_ik_timer(0);
		player.deatht = get_ik_timer(2);
		end = 0;
		if (player.death == 2)
		{
			Play_SoundFX(WAV_BLACKHOLEDEATH, get_ik_timer(0));
			player.num_ships = 0;
		}
		else if (player.death == 1)
		{
			Play_SoundFX(WAV_EXPLO2, t);
			player.num_ships = 0;
		}
		else if (player.num_ships == 0 && player.death < 6)
		{
			Play_SoundFX(WAV_EXPLO2, t);
		}

		if (player.death == 7 || player.death == 3)
			end = 1;

		while (!end && !must_quit)
		{
			ik_eventhandler();
			t0 = t;
			t = get_ik_timer(0);

			ik_mclick();
			ik_inkey();

			if (t>t0)
			{
				if (player.death == 4)
					starmap_advancedays(2);
				prep_screen();
				starmap_display(t);
				ik_blit();
			}
			if (t-s > 200)
				end = 1;
			else if (t-s > 50 && player.system == homesystem && !player.enroute)
				end = 1;
		}

		player.stardate = sp1;
		Stop_Sound(15);
		game_over();
	}
}

// ----------------
// LOCAL FUNCTIONS
// ----------------

void starmap_displayship(int32 t, int32 st)
{
	int c, a;
	int cx, cy;
	int x, y;
	int u;
	int l;
	int s;
	int z;
	t_hull *hull;

	if (player.num_ships < 1)
		st = 0;

	u = 3*(st==0);	// only highlight uninstallers if player ship

	hull = &hulls[shiptypes[player.ships[player.sel_ship]].hull];
	interface_drawborder(screen,
											 SM_SHIP_X, SM_SHIP_Y, SM_SHIP_X + 160, SM_SHIP_Y + 256,
											 1, STARMAP_INTERFACE_COLOR, shiptypes[player.ships[player.sel_ship]].name);
	ik_dsprite(screen, SM_SHIP_X + 16, SM_SHIP_Y + 40, hull->silu, 2+(STARMAP_INTERFACE_COLOR<<8));

	// ship systems
	l = 160;
	cx = SM_SHIP_X; cy = SM_SHIP_Y;

//	ik_print(screen, font_4x8, SM_SHIP_X, SM_SHIP_Y, 3, "%d",shiptypes[player.ships[player.sel_ship]].num_systems);
	for (z = 0; z < shiptypes[player.ships[player.sel_ship]].num_systems; z++)
	{
		s = shiptypes[player.ships[player.sel_ship]].system[z];
		y = cy + (l+=8);
		if (!shiptypes[player.ships[player.sel_ship]].sysdmg[z])
			ik_dsprite(screen, cx + 24, y, spr_IFarrows->spr[12], 2+(u<<8));
		else
		{	
			ik_dsprite(screen, cx + 24, y, spr_IFarrows->spr[15], 2+((3-2*((t&31)> 24)) <<8));
		}
		switch(shipsystems[s].type)
		{
			case sys_weapon:				// weapons
				ik_print(screen, font_4x8, cx + 32, y, 1*(shipsystems[s].item>-1 && shiptypes[player.ships[player.sel_ship]].sysdmg[z]==0),   "%s", shipsystems[s].name);
				ik_drawline(screen, cx + 22, y + 3, cy + 20 - z*4, y + 3, 27, 0, 255);
				ik_drawline(screen, cx + 20 - z*4, y + 3, cy + 20 - z*4, cy + 46 - z*4, 27, 0, 255);
				ik_drawline(screen, cx + 20 - z*4, cy + 46 - z*4, cx + 16+hull->hardpts[z].x*2, cy + 46 - z*4, 27, 0, 255);
				ik_drawline(screen, cx + 16+hull->hardpts[z].x*2, cy + 46 - z*4, cx + 16+hull->hardpts[z].x*2, cy + 40 + hull->hardpts[z].y*2, 27, 0, 255);
			break;
			case sys_thruster:	// thrusters			
				ik_print(screen, font_4x8, cx + 32, y, 2*(shiptypes[player.ships[player.sel_ship]].sysdmg[z]==0),   "%s", shipsystems[s].name);
				ik_drawline(screen, cx + 34 + strlen(shipsystems[s].name)*4, y + 3, cx + 140, y + 3, 43, 0, 255);
				ik_drawline(screen, cx + 140, y + 3, cx + 140, cy + 166, 43, 0, 255);
				x = 64;
				for (c = 0; c < hulls[shiptypes[player.ships[player.sel_ship]].hull].numh; c++)
					if (hulls[shiptypes[player.ships[player.sel_ship]].hull].hardpts[c].type==hdpThruster)
						x = MIN(x, hulls[shiptypes[player.ships[player.sel_ship]].hull].hardpts[c].x);
				ik_drawline(screen, cx + 16+x*2, cy + 166, cx + 140, cy + 166, 43, 0, 255);
				for (c = 0; c < hulls[shiptypes[player.ships[player.sel_ship]].hull].numh; c++)
					if (hulls[shiptypes[player.ships[player.sel_ship]].hull].hardpts[c].type==hdpThruster)
						ik_drawline(screen, cx + 16+hulls[shiptypes[player.ships[player.sel_ship]].hull].hardpts[c].x*2, cy + 166, cx + 16 + hulls[shiptypes[player.ships[player.sel_ship]].hull].hardpts[c].x*2, cy + 40 + hulls[shiptypes[player.ships[player.sel_ship]].hull].hardpts[c].y*2, 43, 0, 255);
			break;
			case sys_engine:	// engine
				a = 64; x = 64;
				for (c = 0; c < hulls[shiptypes[player.ships[player.sel_ship]].hull].numh; c++)
					if (hulls[shiptypes[player.ships[player.sel_ship]].hull].hardpts[c].type==hdpEngine)
					{	a = MIN(a, hulls[shiptypes[player.ships[player.sel_ship]].hull].hardpts[c].y);
						x = MIN(x, hulls[shiptypes[player.ships[player.sel_ship]].hull].hardpts[c].x);	}
				a -= 8;
				ik_print(screen, font_4x8, cx + 32, y, 3*(shiptypes[player.ships[player.sel_ship]].sysdmg[z]==0), "%s", shipsystems[s].name);
				ik_drawline(screen, cx + 34 + strlen(shipsystems[s].name)*4, y + 3, cx + 144, y + 3, 59, 0, 255);
				ik_drawline(screen, cx + 144, y + 3, cx + 144, cy+40+a*2, 59, 255);
				ik_drawline(screen, cx + 144, cy + 40 + a*2, cx + 16 + x*2, cy+40+a*2, 59, 255);
				for (c = 0; c < hulls[shiptypes[player.ships[player.sel_ship]].hull].numh; c++)
					if (hulls[shiptypes[player.ships[player.sel_ship]].hull].hardpts[c].type==hdpEngine)
						ik_drawline(screen, cx + 16+hulls[shiptypes[player.ships[player.sel_ship]].hull].hardpts[c].x*2, cy + 40 + a*2, cx + 16 + hulls[shiptypes[player.ships[player.sel_ship]].hull].hardpts[c].x*2, cy + 40 + hulls[shiptypes[player.ships[player.sel_ship]].hull].hardpts[c].y*2, 59, 0, 255);
			break;
			
			default:	// misc systems
				ik_print(screen, font_4x8, cx + 32, y, 5*(shiptypes[player.ships[player.sel_ship]].sysdmg[z]==0), "%s", shipsystems[s].name);
		}
	}

	if (t > player.sel_ship_time && t < player.sel_ship_time + 32)
	{
		l = (32 - t + player.sel_ship_time)>>1;
		if (l>15) l=15;
		ik_drsprite(screen, SM_SHIP_X + 80, SM_SHIP_Y + 104, 
								0, 128, 
								hull->sprite, 5+(l<<8));
	}
	for (c = 0; c < hull->numh; c++)
	{
		l = 0;
		if (hull->hardpts[c].type == hdpWeapon && shipsystems[shiptypes[player.ships[player.sel_ship]].system[c]].item > -1)
			l=1;
		if (hull->hardpts[c].type == hdpEngine && shiptypes[player.ships[player.sel_ship]].engine>-1)
			l=3;
		if (hull->hardpts[c].type == hdpThruster && shiptypes[player.ships[player.sel_ship]].thrust>-1)
			l=2;
		ik_dsprite(screen, SM_SHIP_X + 8+hull->hardpts[c].x*2, SM_SHIP_Y + 32 + hull->hardpts[c].y*2, 
								spr_IFsystem->spr[hull->hardpts[c].type * 4 + 1], 2+(l<<8));
	}

	// draw all player ships (small)
	if (player.sel_ship > player.num_ships-1)
	{	player.sel_ship = 0; } //player.sel_ship_time = t; }
	for (c = 0; c < player.num_ships; c++)
	{
		l = 0;
		if (shiptypes[player.ships[c]].hits < hulls[shiptypes[player.ships[c]].hull].hits*256)
			l = 1;
		else
		for (s = 0; s < shiptypes[player.ships[c]].num_systems; s++)
		{
			if (shiptypes[player.ships[c]].sysdmg[s])
				l = 1;
		}

		if (l==0 || (t&31)>8)
			ik_drsprite(screen, SM_SHIP_X + 24 + c * 16, SM_SHIP_Y + 32, 0, 16, hulls[shiptypes[player.ships[c]].hull].sprite, 0);
		else
		{
			l = 26;
			ik_drsprite(screen, SM_SHIP_X + 24 + c * 16, SM_SHIP_Y + 32, 0, 16, hulls[shiptypes[player.ships[c]].hull].sprite, 1+(l<<8));
		}
	}
	l = (t&31)>23;
	ik_drsprite(screen, SM_SHIP_X + 24 + player.sel_ship * 16, SM_SHIP_Y + 32, 0, 16+l*2, spr_IFtarget->spr[8], 5+((8+l*7)<<8));

	// draw repair button
	c = player.sel_ship;
	if (shiptypes[player.ships[c]].hits < hulls[shiptypes[player.ships[c]].hull].hits*256)
		l = 1+(STARMAP_INTERFACE_COLOR-1)*((t&31)>8);
	else
		l = 0;
	//l = STARMAP_INTERFACE_COLOR*(shiptypes[player.ships[c]].hits < hulls[shiptypes[player.ships[c]].hull].hits*256);
	interface_drawbutton(screen, SM_SHIP_X+80, SM_SHIP_Y+24, 64, l, " ");
	ik_dsprite(screen, SM_SHIP_X+88, SM_SHIP_Y+24, spr_IFbutton->spr[16], 2+(l<<8));
	ik_dsprite(screen, SM_SHIP_X+120, SM_SHIP_Y+28, spr_IFbutton->spr[15], 2+(l<<8));
	if (shiptypes[player.ships[c]].hits/256 < hulls[shiptypes[player.ships[c]].hull].hits)
		ik_drawbox(screen, SM_SHIP_X+89+(MAX(shiptypes[player.ships[c]].hits,0)/256*22)/hulls[shiptypes[player.ships[c]].hull].hits, SM_SHIP_Y+28, 
							 SM_SHIP_X+110, SM_SHIP_Y+35,0);
	/*
	ik_drawmeter(screen, SM_SHIP_X+88, SM_SHIP_Y+29, SM_SHIP_X+112, SM_SHIP_Y+34, 1, 
			((shiptypes[player.ships[c]].hits/256) * 100) / hulls[shiptypes[player.ships[c]].hull].hits, 
			l, 28);
	*/


		//15

}

void starmap_display(int32 t)
{
	int c, a;
	int cx, cy;
	int x, y;
	int l, d;
	int sp1, sp2;
	char top[128];
	char lne[128];
	char cal[128];
	int bu[3], nbu;
	int nl;
	int ti2 = get_ik_timer(2);
	t_ik_sprite *fs1, *fs2;
	t_ik_sprite *ssp;
//	t_hull *hull;

	ssp = hulls[shiptypes[player.ships[0]].hull].sprite;
	
	// clear screen
	ik_drawbox(screen, 0, 0, 640, 480, 0);

	// draw starmap
	cy = SM_MAP_Y + 244;
	cx = SM_MAP_X + 240;
	
	ik_setclip(cx-232,cy-232,cx+232,cy+232);

	ik_copybox(sm_nebulagfx, screen, 8, 8, 472, 472, cx-232, cy-232);

	for (c = 0; c < num_holes; c++)
#ifndef STARMAP_DEBUGINFO
	if (sm_holes[c].explored && sm_holes[c].size>0)
	{
		ik_drsprite(screen, cx + sm_holes[c].x, cy - sm_holes[c].y,
								1023-((t*2)&1023), 32, spr_SMstars->spr[8], 4);
#else
	if (sm_holes[c].size>0)
	{
		ik_drsprite(screen, cx + sm_holes[c].x, cy - sm_holes[c].y,
								1023-((t*2)&1023), 32, spr_SMstars->spr[8], 5+((7+8*(sm_holes[c].explored>0))<<8) );
#endif
		if (player.explore != c+1)
		{
			if ( int32(cx+sm_holes[c].x + 12 + strlen(sm_holes[c].name)*4) < int32(cx + 232))
				ik_print(screen, font_4x8, cx + sm_holes[c].x + 12, cy - sm_holes[c].y - 3, 0, sm_holes[c].name);
			else
				ik_print(screen, font_4x8, cx + sm_holes[c].x - 12 - strlen(sm_holes[c].name)*4, cy - sm_holes[c].y - 3, 0, sm_holes[c].name);
		}
	}


	for (c = 0; c < num_stars; c++)
	if (sm_stars[c].color > -2)
	{
//	ik_dsprite(screen, 400 + (sm_stars[c].x>>8) - 16, 244 - (sm_stars[c].y>>8) - 16,
//					 spr_SMstars->spr[sm_stars[c].color], 0);
		sm_stars[c].ds_x = cx + sm_stars[c].x;
		sm_stars[c].ds_y = cy - sm_stars[c].y;

		if (sm_stars[c].color > -1)
		{
			ik_drsprite(screen, sm_stars[c].ds_x, sm_stars[c].ds_y,
									0, 32, spr_SMstars->spr[sm_stars[c].color], 4);
			ik_drsprite(screen, sm_stars[c].ds_x, sm_stars[c].ds_y,
									0, 32, spr_SMstars->spr[sm_stars[c].color], 2);
		}
		if ( int32(sm_stars[c].ds_x + 12 + strlen(sm_stars[c].starname)*4) < int32(cx + 232))
			ik_print(screen, font_4x8, sm_stars[c].ds_x + 12, sm_stars[c].ds_y - 3, 0, sm_stars[c].starname);
		else
			ik_print(screen, font_4x8, sm_stars[c].ds_x - 12 - strlen(sm_stars[c].starname)*4, sm_stars[c].ds_y - 3, 0, sm_stars[c].starname);
#ifdef STARMAP_DEBUGINFO
		if (sm_stars[c].card>-1 && c != homesystem)
			ik_print(screen, font_4x8, sm_stars[c].ds_x - 32, sm_stars[c].ds_y + 12, 0, ecards[sm_stars[c].card].name);
#endif

		if (sm_stars[c].novadate > 0)
		{
			nl = (5-4*sm_stars[c].novatype)*365;
		}

		if (sm_stars[c].novadate==0 || sm_stars[c].novadate>=player.stardate || sm_stars[c].novatype == 2)
		{
			if (sm_stars[c].explored && sm_stars[c].planet < 10)
			{
				a = ((t * (5+c/2)) & 1023);
				if (c&1)
					a = 1023 - a;
				ik_dsprite(screen, 
									 sm_stars[c].ds_x + (sin1k[a]>>12) - 8, 
									 sm_stars[c].ds_y - (cos1k[a]>>12) - 8, 
									 spr_SMplanet->spr[sm_stars[c].planet], 0);
				if (sm_stars[c].explored==1)
					ik_drsprite(screen, 
										 sm_stars[c].ds_x + (sin1k[a]>>12) , 
										 sm_stars[c].ds_y - (cos1k[a]>>12) , 
										 0, 16, spr_SMplanet->spr[sm_stars[c].planet], 
										 5+((15*((t&16)>0))<<8 ) );
			}

			if (ecards[sm_stars[c].card].type == card_ally && sm_stars[c].explored==2)
			{
				ik_drsprite(screen, 
										sm_stars[c].ds_x + (sin1k[(t*8+512) & 1023]>>12),
										sm_stars[c].ds_y - (cos1k[(t*8+512) & 1023]>>12),
										(t*8 + 768)&1023,
										24,
										hulls[shiptypes[ecards[sm_stars[c].card].parm].hull].sprite,
										0);
			}

			if (sm_stars[c].novadate > 0)
			{
				l = 8 + (sin1k[(t*12+4)&1023]>>13);
				l = MAX(MIN(l,15), 0);
				ik_drsprite(screen, sm_stars[c].ds_x, sm_stars[c].ds_y, (c*64+t*4)&1023, l+20, spr_shockwave->spr[4], 5+(l<<8));
			}

		//	ik_print(screen, font_4x8, sm_stars[c].ds_x - 4, sm_stars[c].ds_y + 8, 0, "%d", sm_stars[c].card);
		}

		else if (sm_stars[c].novadate>0 && player.stardate < sm_stars[c].novadate+nl)
		{
			if (sm_stars[c].novatime == 0)
			{
				Play_SoundFX(WAV_EXPLO2);
				sm_stars[c].novatime = ti2;
			}
			a = ti2 - sm_stars[c].novatime;
			if (a < 50)
			{
				l = 15; 
				if (a > 35) l-=a-35;
				if (l < 0) l=0;
				ik_drsprite(screen, sm_stars[c].ds_x, sm_stars[c].ds_y, (c*64)&1023, a*2, spr_shockwave->spr[3], 5+(l<<8));
				l = 15;
				if (a > 15) l-=a-15;
				if (l>0)
					ik_drsprite(screen, sm_stars[c].ds_x, sm_stars[c].ds_y, (c*64)&1023, a*2+30, spr_shockwave->spr[4], 5+(l<<8));
			}

			a = player.stardate-sm_stars[c].novadate;
			if (sm_stars[c].planet != 10)  
			{	// destroy planet and everything
				sm_stars[c].color=7;
				sm_stars[c].planet = 10;
				sm_stars[c].card = 0;
				sm_stars[c].explored = 0;
				sprintf(sm_stars[c].planetname, "No Planets");
				while (plgfx_type[sm_stars[c].planetgfx] != sm_stars[c].planet && !must_quit)
				{
					ik_eventhandler();
					sm_stars[c].planetgfx = rand()%num_plgfx;
				}
			}
			l = 15;
			if (a > nl-365)  l = 15 - ((a - (nl-365))*15)/365;
			a = (a * 38) / 365;	// size
			if (l < 1) l = 1;
			ik_drsprite(screen, sm_stars[c].ds_x, sm_stars[c].ds_y, (c*64)&1023, a, spr_shockwave->spr[1], 5+(l<<8));

			if (sm_stars[c].novatype < 2 && player.num_ships>0 && (player.stardate-sm_stars[c].novadate) < nl-365 )
			{
				// nova kills enemies
				for (l = 0; l < STARMAP_MAX_FLEETS; l++)
				{
					if (sm_fleets[l].race != race_kawangi && sm_fleets[l].num_ships>0)
					{
						if (sm_fleets[l].system == c || starmap_stardist(sm_fleets[l].system, c) < player.stardate-sm_stars[c].novadate)
							sm_fleets[l].num_ships = 0;					
					}
				}

				if (player.system == c) // in system or trying to escape
				{
					if (player.enroute>0 && player.target != c)
					{
						if (player.enroute < player.stardate - sm_stars[c].novadate &&
							  player.enroute > player.stardate - sm_stars[c].novadate - 120)
						{
							player.num_ships = 0;
							player.death = 4;
						}
					}
					else if (player.stardate-sm_stars[c].novadate < 120)
					{	// kill if staying
						player.num_ships = 0;
						player.death = 4;
					}
				}
				else 
				{	// kill player
					l = get_distance(sm_stars[c].x - player.x, sm_stars[c].y - player.y);
					if (l > a/2+1-8 && l < a/2+1)
					{
						player.num_ships = 0;
						player.death = 4;
					}
				}	
			}

			a = 64+a/2;
			l = 15;
			if (player.stardate > sm_stars[c].novadate+365)
			{	
				l -= (player.stardate - (sm_stars[c].novadate+365)) >> 5;
				if (l < 0) l = 0;
			}
			if (l)
				ik_drsprite(screen, sm_stars[c].ds_x, sm_stars[c].ds_y, (c*64)&1023, a, spr_shockwave->spr[4], 5+(l<<8));
		}
		else if (sm_stars[c].novadate>0)	// && player.stardate >= sm_stars[c].novadate+nl)
		{
			sm_stars[c].novadate = 0;
		}

	}
	else if (sm_stars[c].color == -3)	// collapser effect
	{
		d = ti2 - sm_stars[c].novatime;
		if (d > 50)
			sm_stars[c].color = -2;
		a = d*2;
		l = 15; 
		if (d > 35) l-=d-35;
		if (l < 0) l=0;
		if (sm_stars[c].novatype == 2)	// main collapser
		{
			ik_drsprite(screen, sm_stars[c].ds_x, sm_stars[c].ds_y, (c*64)&1023, a*2, spr_shockwave->spr[2], 5+(l<<8));
		}
		else	// exploding stars
		{
			ik_drsprite(screen, sm_stars[c].ds_x, sm_stars[c].ds_y, (c*64)&1023, a, spr_shockwave->spr[3], 5+(l<<8));
		}

		a = d*2+30;
		l = 15;
		if (d > 15) l-=d-15;
		if (l>0)
			ik_drsprite(screen, sm_stars[c].ds_x, sm_stars[c].ds_y, (c*64)&1023, a, spr_shockwave->spr[4], 5+(l<<8));
	}


	for (c = 0; c < STARMAP_MAX_FLEETS; c++)
	{


#ifdef STARMAP_DEBUGINFO
		if (sm_fleets[c].race == race_kawangi)
		{
			ik_print(screen, font_6x8, 176, 24, 3, "%d %d", sm_fleets[c].system, sm_fleets[c].target);
		}
#endif


		if (sm_fleets[c].explored>0 && (sm_fleets[c].num_ships>0 || (sm_fleets[c].blowtime>0 && sm_fleets[c].enroute>0)) )
		{
			l = sm_fleets[c].system;
			d = sm_fleets[c].target;
			if (sm_fleets[c].enroute)	// moving between stars (kawangi)
			{
				x = sm_stars[l].x + ((sm_stars[d].x-sm_stars[l].x)*sm_fleets[c].enroute)/sm_fleets[c].distance;
				y = sm_stars[l].y + ((sm_stars[d].y-sm_stars[l].y)*sm_fleets[c].enroute)/sm_fleets[c].distance;
				a = get_direction(sm_stars[d].x-sm_stars[l].x, sm_stars[d].y-sm_stars[l].y);
				
				if (sm_fleets[c].blowtime == 0)
				{
					ik_dspriteline(screen, cx+x+(sin1k[a]>>13), cy-y-(cos1k[a]>>13), sm_stars[d].ds_x, sm_stars[d].ds_y, 8, (t&15), 16, spr_IFtarget->spr[0], 4);

					if (sm_fleets[c].explored==2)
						ik_drsprite(screen, cx+x, cy-y, a, 24, hulls[shiptypes[sm_fleets[c].ships[0]].hull].sprite, 0);
					else
					{
						ik_drsprite(screen, cx+x, cy-y, a, 12, spr_IFtarget->spr[1], 4);
						ik_drsprite(screen, cx+x, cy-y, ((t*3+c*128)&1023), 24, spr_IFtarget->spr[9], 4);
					}
				}
				else
				{
					d = ti2 - sm_fleets[c].blowtime;
					if (d >= 100)
					{
						sm_fleets[c].blowtime = 0;
					}
					else
					{
						a = (spr_explode1->num * d) / 100;
						ik_drsprite(screen, cx+x, cy-y, (d*10)&1023, 24, spr_explode1->spr[a], 4);
						l = 15;
						if (d>40) l-=(d-40)/4;
						ik_drsprite(screen, cx+x, cy-y, (d*3)&1023, d, spr_shockwave->spr[0], 5+(l<<8));
					}
				}
			}
			else
			{
				if (sm_fleets[c].explored==2)
				{
					ik_drsprite(screen, 
											sm_stars[l].ds_x + (sin1k[(t*(c+3)+512) & 1023]>>12),
											sm_stars[l].ds_y - (cos1k[(t*(c+3)+512) & 1023]>>12),
											(t*(c+3) + 768)&1023,
											24,
											hulls[shiptypes[racefleets[races[sm_fleets[c].race].fleet].stype[0]].hull].sprite,
											0);
#ifdef STARMAP_DEBUGINFO
					ik_print(screen, font_6x8, sm_stars[l].ds_x + 8, sm_stars[l].ds_y - 16, 3, "f%d", c);
#endif
											//hulls[shiptypes[sm_fleets[c].ships[0]].hull].sprite, 0);
				}
				else if (sm_fleets[c].num_ships>0)
				{
					ik_drsprite(screen, 
											sm_stars[l].ds_x,
											sm_stars[l].ds_y,
											((t*3+c*128)&1023),
											32,
											spr_IFtarget->spr[9], 4);
#ifdef STARMAP_DEBUGINFO
					ik_print(screen, font_6x8, sm_stars[l].ds_x + 8, sm_stars[l].ds_y - 16, 3, "f%d", c);
#endif
				}
			}
		}
#ifdef STARMAP_DEBUGINFO
		else
		{
			l = sm_fleets[c].system;
			ik_drsprite(screen, 
									sm_stars[l].ds_x,
									sm_stars[l].ds_y,
									((t*3+c*128)&1023),
									32,
									spr_IFtarget->spr[9], 4);
		}
#endif
	}

	if (player.num_ships>0)
	{
		if (player.enroute)
		{
			c = player.system;
			l = player.target;
			if (player.fold)
			{	// fold
				a = rand()%32;

				if (t-player.hyptime < 96)
				{
					fs1 = new_sprite(64, 64);
					fs2 = new_sprite(64, 64);
					d = t-player.hyptime;
					for (y = 0; y < 64; y++)
					{
						if (y+d < 64)
							memcpy(fs1->data+y*64, ssp->data+(y+d)*64, 64);
						else
							memset(fs1->data+y*64, 0, 64);
						if (y < 20)
							for (x = 0; x < 64; x++)
							{
								if (fs1->data[y*64+x])
									fs1->data[y*64+x] = gfx_addbuffer[spr_weapons->spr[15]->data[y*32+((x+a)&31)]+(fs1->data[y*64+x]<<8)];
							}
						if (y+d-96 >= 0)
							memcpy(fs2->data+y*64, ssp->data+(y+d-96)*64, 64);
						else
							memset(fs2->data+y*64, 0, 64);
						if (y > 44)
							for (x = 0; x < 64; x++)
							{
								if (fs2->data[y*64+x])
									fs2->data[y*64+x] = gfx_addbuffer[spr_weapons->spr[15]->data[(63-y)*32+((x+a)&31)]+(fs2->data[y*64+x]<<8)];
							}

					}
					a = get_direction( sm_stars[l].x - sm_stars[c].x,
														 sm_stars[l].y - sm_stars[c].y);
					ik_drsprite(screen, 
											sm_stars[c].ds_x + (sin1k[a]>>12),
											sm_stars[c].ds_y - (cos1k[a]>>12),
											a,
											24,
											fs1, 0);
					ik_drsprite(screen, 
											sm_stars[l].ds_x - (sin1k[a]>>12),
											sm_stars[l].ds_y + (cos1k[a]>>12),
											a,
											24,
											fs2, 0);
					free_sprite(fs1);
					free_sprite(fs2);
				}
				else
				{
					a = get_direction( sm_stars[l].x - sm_stars[c].x,
														 sm_stars[l].y - sm_stars[c].y);
					ik_drsprite(screen, 
											sm_stars[l].ds_x - (sin1k[a]>>12),
											sm_stars[l].ds_y + (cos1k[a]>>12),
											a,
											24,
											ssp, 0);
				}
			}	
			else if (shiptypes[0].engine>-1 && shipsystems[shiptypes[0].engine].par[0]==666)
			{	// hyper
				a = get_direction( sm_stars[l].x - sm_stars[c].x,
													 sm_stars[l].y - sm_stars[c].y);
				if (t-player.hyptime < 32)
				{
					if (t-player.hyptime < 16)
						ik_drsprite(screen, 
												sm_stars[c].ds_x + (sin1k[a]>>12),
												sm_stars[c].ds_y - (cos1k[a]>>12),
												a,
												(16-(t-player.hyptime))*24/16,
												ssp, 0);
					d = 15-((t-player.hyptime)*14)/32;
					ik_drsprite(screen, 
											sm_stars[c].ds_x + (sin1k[a]>>12),
											sm_stars[c].ds_y - (cos1k[a]>>12),
											a,
											64,
											spr_shockwave->spr[4], 5+(d<<8));
				}
				else if (t-player.hyptime > 75 && t-player.hyptime < 107)
				{
					/*if (t-player.hyptime < 91)
						ik_drsprite(screen, 
												sm_stars[l].ds_x - (sin1k[a]>>12),
												sm_stars[l].ds_y + (cos1k[a]>>12),
												a,
												((t-player.hyptime)-75)*24/16,
												spr_ships->spr[2], 0);
					else*/
					ik_drsprite(screen, 
											sm_stars[l].ds_x - (sin1k[a]>>12),
											sm_stars[l].ds_y + (cos1k[a]>>12),
											a,
											24,
											ssp, 0);

					d = 15-((t-player.hyptime-75)*14)/32;
					ik_drsprite(screen, 
											sm_stars[l].ds_x - (sin1k[a]>>12),
											sm_stars[l].ds_y + (cos1k[a]>>12),
											a,
											64,
											spr_shockwave->spr[4], 5+(d<<8));
				}
				else if (t-player.hyptime >= 107)
				{
					ik_drsprite(screen, 
											sm_stars[l].ds_x - (sin1k[a]>>12),
											sm_stars[l].ds_y + (cos1k[a]>>12),
											a,
											24,
											ssp, 0);
				}
			}
			else
			{
				a = player.enroute;
				if (a < 640)
					a = 128+(((a+384) * (a+384))>>11);
				if (a > player.distance - 640)
					a = player.distance-128-(((player.distance+384-a) * (player.distance+384-a) )>>11);

				d = MAX( (abs(sm_stars[l].x-sm_stars[c].x)), (abs(sm_stars[l].y-sm_stars[c].y)) );
				a = (a * d) / player.distance;
				x = (sm_stars[c].x * (d-a) +
						sm_stars[l].x * a) / d;
				y = (sm_stars[c].y * (d-a) +
						sm_stars[l].y * a) / d;
				a = get_direction( sm_stars[l].x - sm_stars[c].x,
													 sm_stars[l].y - sm_stars[c].y);
				player.a = a;
				ik_drsprite(screen, 
										cx+x,
										cy-y,
										a,
										24,
										ssp, 0);
				a = (player.enroute * d)/player.distance;
				player.x = (sm_stars[c].x * (d-a) +
										sm_stars[l].x * a) / d;
				player.y = (sm_stars[c].y * (d-a) +
										sm_stars[l].y * a) / d;
			}

			if (player.explore)
			{
				l = 11+4*((t&31)>24);
				l = 5 + (l<<8);
				a = get_direction( sm_stars[player.target].x - x,
													 sm_stars[player.target].y - y);

				ik_dspriteline(screen, 
											cx + x + (sin1k[a]>>12),
											cy - y - (cos1k[a]>>12),
											sm_stars[player.target].ds_x - ((sin1k[a]*3)>>13),
											sm_stars[player.target].ds_y + ((cos1k[a]*3)>>13),
											8, (t&15), 16, spr_IFtarget->spr[4], l);
				ik_drsprite(screen, 
										sm_stars[player.target].ds_x - (sin1k[a]>>12),
										sm_stars[player.target].ds_y + (cos1k[a]>>12),
										a,
										12,
										spr_IFtarget->spr[5], l);
				ik_drsprite(screen, 
										cx+sm_holes[player.explore-1].x,
										cy-sm_holes[player.explore-1].y,
										1023-((t*8)&1023),
										24,
										spr_IFtarget->spr[6], 0);
			}
		}

		if (player.system>-1 && !player.enroute)
		{
			c = player.system;
			player.x = sm_stars[c].x;
			player.y = sm_stars[c].y;
			if (player.target==-1 || player.target==player.system) // in orbit
			{
				ik_drsprite(screen, 
										sm_stars[c].ds_x + (sin1k[(t*8) & 1023]>>12),
										sm_stars[c].ds_y - (cos1k[(t*8) & 1023]>>12),
										(t*8 + 256)&1023,
										24,
										ssp, 0);
				// hire and hunt buttons
				if (player.target == player.system && sm_stars[player.target].explored==2)
				{
					l = sm_stars[player.target].card;
					if (ecards[l].type == card_ally)
					{
						ik_drsprite(screen, 
												sm_stars[player.target].ds_x,
												sm_stars[player.target].ds_y + 21,
												0,
												32,
												spr_IFbutton->spr[20], 1+((11+4*((t&31)>24))<<8));
					}
					else if (ecards[l].type == card_lifeform)
					{
						ik_drsprite(screen, 
												sm_stars[player.target].ds_x,
												sm_stars[player.target].ds_y + 21,
												0,
												32,
												spr_IFbutton->spr[21], 1+((11+4*((t&31)>24))<<8));
					}
				}
			}
			else	// targeted
			{
				l = 11+4*((t&31)>24);
				l = 5 + (l<<8);
				a = get_direction( sm_stars[player.target].x - sm_stars[c].x,
													 sm_stars[player.target].y - sm_stars[c].y);
				bu[0] = 0; nbu = 1;
				if (player.distance <= 2380)
				{
					for (x = 0; x < player.num_ships; x++)
						if (shiptypes[player.ships[x]].flag & 4)	// kuti
							bu[nbu++] = 1;
					if (player.target>-1 && sm_stars[player.target].explored==0)
						for (x = 0; x < shiptypes[0].num_systems; x++)
							if (shipsystems[shiptypes[0].system[x]].type == sys_misc && shipsystems[shiptypes[0].system[x]].par[0]==0)
								bu[nbu++] = 2;
				}

				ik_dspriteline(screen, 
											sm_stars[c].ds_x + (sin1k[a]>>12),
											sm_stars[c].ds_y - (cos1k[a]>>12),
											sm_stars[player.target].ds_x - ((sin1k[a]*3)>>13),
											sm_stars[player.target].ds_y + ((cos1k[a]*3)>>13),
										8, (t&15), 16, spr_IFtarget->spr[4], l);
				ik_drsprite(screen, 
										sm_stars[player.target].ds_x - (sin1k[a]>>12),
										sm_stars[player.target].ds_y + (cos1k[a]>>12),
										a,
										12,
										spr_IFtarget->spr[5], l);

				// draw engage / fold buttons
				x = (sm_stars[player.target].y>=sm_stars[c].y)-(sm_stars[player.target].y<sm_stars[c].y);
				for (d = 0; d < nbu; d++)
				{
					y = sm_stars[player.target].ds_y - x * (12+nbu*5) - nbu*5 + d*10;
					ik_drsprite(screen, 
											sm_stars[player.target].ds_x,
											y + 9,
											0,
											32,
											spr_IFbutton->spr[12+bu[d]], 1+((11+4*((t&31)>24))<<8));
				}
				ik_drsprite(screen, 
										sm_stars[c].ds_x + (sin1k[a]>>12),
										sm_stars[c].ds_y - (cos1k[a]>>12),
										a,
										24,
										ssp, 0);

				ik_print(screen, font_6x8,
								(sm_stars[c].ds_x+sm_stars[player.target].ds_x)>>1,
								(sm_stars[c].ds_y+sm_stars[player.target].ds_y)>>1,
								0, textstring[STR_STARMAP_LYEARS], player.distance/365, ((player.distance%365)*100)/365);
	//							0, "%d.%02d LY", player.distance/256, ((player.distance&255)*100)>>8);
				if (shiptypes[0].engine>-1 && shiptypes[0].sysdmg[shiptypes[0].sys_eng]==0)
				{
					sp1 = shipsystems[shiptypes[0].engine].par[0];
					sp2 = shipsystems[shiptypes[0].engine].par[1];
				}
				else if (shiptypes[0].thrust>-1 && shiptypes[0].sysdmg[shiptypes[0].sys_thru]==0)
				{
					sp1 = sp2 = 1;
				}
				else
				{ sp1 = sp2 = 0; }
				if (sp1 == 666)	// hyperdrive
				{	
					if (player.stardate-player.hypdate > 60)
						a = 0;
					else
						a = 60-(player.stardate-player.hypdate);
				}
				else if (sp1 > 0 && sp2 > 0)
					a = ((player.distance*(256-player.nebula)*256)/sp1 + (player.distance*player.nebula*256)/sp2)>>16;
				else
					a = player.distance*4;
	//			a = ((player.distance*(256-player.nebula)*365)/sp1 + (player.distance*player.nebula*365)/sp2)>>16;
				ik_print(screen, font_6x8,
								(sm_stars[c].ds_x+sm_stars[player.target].ds_x)>>1,
								(sm_stars[c].ds_y+sm_stars[player.target].ds_y+16)>>1,
								0, textstring[STR_STARMAP_NDAYS], a);
			}
			if (player.target > -1)
			{
				ik_drsprite(screen, 
										sm_stars[player.target].ds_x,
										sm_stars[player.target].ds_y,
										1023-((t*8)&1023),
										24,
										spr_IFtarget->spr[8], 0);
			}
		}
	}
	else
	{
		d = ti2 - player.deatht;
		if (player.death == 2)
		{
			if (d < 150)
			{
				a = ((65536-cos1k[d*256/150])>>3)&1023;
				x = ((player.x-sm_holes[player.hole].x)*cos1k[a] + 
						(player.y-sm_holes[player.hole].y)*sin1k[a]) >> 16;
				y = ((player.y-sm_holes[player.hole].y)*cos1k[a] - 
						(player.x-sm_holes[player.hole].x)*sin1k[a]) >> 16;
				l = 1024*(150-d)/150;
				ik_drsprite(screen, 
										cx+sm_holes[player.hole].x+((x*l)>>10),
										cy-sm_holes[player.hole].y-((y*l)>>10),
										(player.a+a)&1023,
										(24*l)>>10,
										ssp, 0);

			}
		}
		else		// explode ship
		{
			if (d<100)
			{
				if (player.enroute)
				{ x = cx + player.x; y = cy - player.y; }
				else
				{
					x = sm_stars[player.system].ds_x + (sin1k[(player.deatht*8) & 1023]>>12);
					y = sm_stars[player.system].ds_y - (cos1k[(player.deatht*8) & 1023]>>12);
				}
				a = (spr_explode1->num * d) / 100;
				ik_drsprite(screen, x, y, (d*10)&1023, 24, spr_explode1->spr[a], 4);
				l = 15;
				if (d>40) l-=(d-40)/4;
				ik_drsprite(screen, x, y, (d*3)&1023, d, spr_shockwave->spr[0], 5+(l<<8));
			}
		}
	}

	ik_setclip(0,0,640,480);
	l = 0; a=player.stardate%365;
	for (c = 0; c < 12; c++)
		if (a >= months[c].sd)
			l = c;
	a = a + 1 - months[l].sd;
//	sprintf(lne, "%s", player.captname, 60-strlen(player.captname));

	if (!settings.opt_timeremaining || player.stardate>=10*365)
		sprintf(cal, textstring[STR_STARMAP_DATE], a, months[l].name, player.stardate/365+4580);
	else
	{
		c = 10*365 - player.stardate;
		sprintf(cal, textstring[STR_STARMAP_DAYSLEFT], c);
	}

	sprintf(lne, textstring[STR_STARMAP_CAPTAIN], player.captname, 66-strlen(player.captname));
	sprintf(top, lne, cal);
	interface_drawborder(screen,
											 SM_MAP_X, SM_MAP_Y, SM_MAP_X + 480, SM_MAP_Y + 480,
											 0, STARMAP_INTERFACE_COLOR, top);

	// draw selected ship
	starmap_displayship(t, player.ships[player.sel_ship]);

	// draw inventory
	if (player.num_items > 12)
		hud.invslider = MIN(hud.invslider, player.num_items-12);

	sprintf(top, textstring[STR_STARMAP_CARGO]);
	interface_drawborder(screen,
											 SM_INV_X, SM_INV_Y, SM_INV_X + 160, SM_INV_Y + 128,
											 1, STARMAP_INTERFACE_COLOR, top);
	for (c = 0; c < player.num_items; c++)
	{
		if (c >= hud.invslider && c < hud.invslider + 12)
		{
			ik_print(screen, font_4x8, SM_INV_X + 24, SM_INV_Y + 24 + (c-hud.invslider) * 8, 0*(hud.invselect==c), itemtypes[player.items[c]].name);
			if (!player.itemflags[c])
			{
				l = itemtypes[player.items[c] & 255].type;
				if ((l == item_weapon) || (l == item_system))
					ik_dsprite(screen, SM_INV_X + 16, SM_INV_Y + 24 + (c-hud.invslider)*8, spr_IFarrows->spr[13], 2+(3<<8));
				else if (l == item_device)
					ik_dsprite(screen, SM_INV_X + 16, SM_INV_Y + 24 + (c-hud.invslider)*8, spr_IFarrows->spr[14], 2+(3<<8));
			}
			else	// broken
			{
				ik_dsprite(screen, SM_INV_X + 16, SM_INV_Y + 24 + (c-hud.invslider)*8, spr_IFarrows->spr[15], 2+(3<<8));
			}
//		ik_print(screen, font_6x8, SM_INV_X + 16, SM_INV_Y + 24 + c * 8, 0, "%d", player.items[c]);
		}
	}
	if (player.num_items > 12)
	{
		interface_drawslider(screen, SM_INV_X + 136, SM_INV_Y + 32, 1, 80, player.num_items-12, hud.invslider, STARMAP_INTERFACE_COLOR);
		ik_dsprite(screen, SM_INV_X + 136, SM_INV_Y + 24, spr_IFarrows->spr[5], 2+(STARMAP_INTERFACE_COLOR<<8));
		ik_dsprite(screen, SM_INV_X + 136, SM_INV_Y + 112, spr_IFarrows->spr[4], 2+(STARMAP_INTERFACE_COLOR<<8));
	}
	l = STARMAP_INTERFACE_COLOR * (hud.invselect>-1);
	if (hud.invselect > -1)
		c = itemtypes[player.items[hud.invselect]].type;

	/*
	ik_dsprite(screen, SM_INV_X + 16, SM_INV_Y + 120, spr_IFbutton->spr[11], 2+(l<<8));
	if (hud.invselect > -1)
		l = STARMAP_INTERFACE_COLOR * (c == item_device);
	else
		l = 0;
	ik_dsprite(screen, SM_INV_X + 48, SM_INV_Y + 120, spr_IFbutton->spr[12], 2+(l<<8));

	if (hud.invselect > -1)
		l = STARMAP_INTERFACE_COLOR * ((c == item_weapon) || (c == item_system));
	else
		l = 0;

	ik_dsprite(screen, SM_INV_X + 80, SM_INV_Y + 120, spr_IFbutton->spr[13], 2+(l<<8));
	if (hud.invselect > -1)
		l = STARMAP_INTERFACE_COLOR * ((player.itemflags[hud.invselect]&item_broken)>0);
	else
		l = 0;
	ik_dsprite(screen, SM_INV_X + 112, SM_INV_Y + 120, spr_IFbutton->spr[14], 2+(l<<8));
	*/
	// draw selection (system) info
	sprintf(top, textstring[STR_STARMAP_SELECT]);
	if (player.target > -1)
	{
		if (sm_stars[player.target].explored)
			sprintf(top, sm_stars[player.target].planetname);
		else
			sprintf(top, sm_stars[player.target].starname);
	}
	interface_drawborder(screen,
											 SM_SEL_X, SM_SEL_Y, SM_SEL_X + 160, SM_SEL_Y + 96,
											 1, STARMAP_INTERFACE_COLOR, top);
	if (player.target > -1)
	{
		if (sm_stars[player.target].explored) // planet info
		{
			ik_dsprite(screen, SM_SEL_X + 16, SM_SEL_Y + 24, spr_SMplanet2->spr[sm_stars[player.target].planetgfx], 0);
			interface_textbox(screen, font_4x8,
												SM_SEL_X + 84, SM_SEL_Y + 24, 64, 64, 0,
												platypes[sm_stars[player.target].planet].text);
		}
		else if (sm_stars[player.target].color >= 0) // star info
		{
			ik_dsprite(screen, SM_SEL_X + 16, SM_SEL_Y + 24, spr_SMstars2->spr[sm_stars[player.target].color], 0);
			interface_textbox(screen, font_4x8,
												SM_SEL_X + 84, SM_SEL_Y + 24, 64, 64, 0,
												startypes[sm_stars[player.target].color].text);
		}
		else
		{
			ik_dsprite(screen, SM_SEL_X + 16, SM_SEL_Y + 24, spr_SMplanet2->spr[22], 0);
			interface_textbox(screen, font_4x8,
												SM_SEL_X + 84, SM_SEL_Y + 24, 64, 64, 0,
												platypes[10].text);
		}
		ik_dsprite(screen, SM_SEL_X + 16, SM_SEL_Y + 24, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));
	}

}

int starmap_findstar(int32 mx, int32 my)
{
	int c;

	for (c = 0; c < num_stars; c++)
	if (sm_stars[c].color>-1)
	{
		if (mx > sm_stars[c].ds_x - 8 && mx < sm_stars[c].ds_x + 8 &&
				my > sm_stars[c].ds_y - 8 && my < sm_stars[c].ds_y + 8)
			return c;
	}
	return -1;
}

int starmap_stardist(int32 s1, int32 s2)
{
	int r;

	if (s1==s2)
		return 0;

	r = (int32)(sqrt(double(sm_stars[s2].x-sm_stars[s1].x) * (sm_stars[s2].x-sm_stars[s1].x) +
									(sm_stars[s2].y-sm_stars[s1].y) * (sm_stars[s2].y-sm_stars[s1].y)) *
									3.26*365/64);	// "light days"

	return r;
}

int starmap_nebuladist(int32 s1, int32 s2)
{
	int x, y;
	int r, d, l;
	int dx, dy;

	if (s1 == s2)
		return 0;

	dx = sm_stars[s2].x-sm_stars[s1].x;
	dy = sm_stars[s2].y-sm_stars[s1].y;
	l = MAX(ABS(dx), ABS(dy));

	r = 0; d = l;
	while (d--)
	{
		x = (sm_stars[s1].x*(l-d) + sm_stars[s2].x*(d))/l;
		y = (sm_stars[s1].y*(l-d) + sm_stars[s2].y*(d))/l;
		r+= (sm_nebulamap[((240-y)<<9)+(240+x)]>0);
	}

	r = (r*256)/l;

	return r;
}


void starmap_removeship(int32 n)
{
	int32 c;

	if (player.sel_ship == n)
		player.sel_ship = 0;
	else if (player.sel_ship > n)
		player.sel_ship--;
	for (c = n; c < player.num_ships-1; c++)
	{
		player.ships[c] = player.ships[c+1];
	}
	player.num_ships--;
}

void killstar(int32 c)
{
	int32 n;

	sm_stars[c].color = -2;
	for (n = 0; n < STARMAP_MAX_FLEETS; n++)
	if (sm_fleets[n].num_ships > 0)
	{
		if (sm_stars[sm_fleets[n].system].color < 0)
			sm_fleets[n].num_ships = 0;
	}
}


void starmap_advancedays(int32 n)
{
	int c;
	int s, sy;
	int f;


	starmap_sensefleets();

	for (c = 0; c < n; c++)
	{
		player.stardate++;
		for (s = 0; s < player.num_ships; s++)
		{
			if (shiptypes[player.ships[s]].hits < hulls[shiptypes[player.ships[s]].hull].hits*256)
			{
				shiptypes[player.ships[s]].hits += 128;
				for (sy = 0; sy < shiptypes[player.ships[s]].num_systems; sy++)
					if (shipsystems[shiptypes[player.ships[s]].system[sy]].type == sys_damage)
						shiptypes[player.ships[s]].hits = hulls[shiptypes[player.ships[s]].hull].hits*256;
				if (shiptypes[player.ships[s]].hits >= hulls[shiptypes[player.ships[s]].hull].hits*256)
				{	
					ik_print_log("Finished hull repairs on the %s.\n", shiptypes[player.ships[s]].name);
					shiptypes[player.ships[s]].hits = hulls[shiptypes[player.ships[s]].hull].hits*256;
				}
			}
		}

		if (settings.opt_timerwarnings)
		{
			if (player.stardate == 8*365)
			{
				timer_warning = 1;
			}
			else if (player.stardate == 10*365)
			{
				timer_warning = 2;
			}
		}


		for (s = 0; s < STARMAP_MAX_FLEETS; s++)
		if (sm_fleets[s].num_ships > 0 && sm_fleets[s].enroute > 0)
		{	// kawangi moves
			sm_fleets[s].enroute += 4;
			if (sm_fleets[s].enroute >= sm_fleets[s].distance)
			{	// kawangi enters system
				sm_fleets[s].system = sm_fleets[s].target;
				sm_fleets[s].enroute = 0;
				if (sm_fleets[s].system != homesystem)
					sm_fleets[s].distance = player.stardate + 365;
				else
					sm_fleets[s].distance = 0;

				if (sm_fleets[s].system == player.system && player.enroute == 0) // meets player
				{
					kawangi_incoming = 1;
				}

				// kill any other fleets at system
				for (f = 0; f < STARMAP_MAX_FLEETS; f++)
				if (s != f && sm_fleets[f].num_ships > 0 && sm_fleets[f].system == sm_fleets[s].system)
				{
					sm_fleets[f].num_ships = 0;
				}

			}
		}
		else if (sm_fleets[s].race == race_kawangi && sm_fleets[s].num_ships > 0)
		{
			if (player.stardate > sm_fleets[s].distance && sm_fleets[s].distance > 0)
			{
				starmap_kawangimove(s);
			}
		}


	}
}

void starmap_sensefleets()
{
	int c, r;
	int x, y;

	for (c = 0; c < STARMAP_MAX_FLEETS; c++)
	if (sm_fleets[c].explored == 0 && sm_fleets[c].num_ships > 0)
	{
		if (sm_fleets[c].enroute == 0)
			r = get_distance ( sm_stars[sm_fleets[c].system].x - player.x, sm_stars[sm_fleets[c].system].y - player.y);
		else
		{
			x = sm_stars[sm_fleets[c].system].x + ((sm_stars[sm_fleets[c].target].x-sm_stars[sm_fleets[c].system].x)*sm_fleets[c].enroute)/sm_fleets[c].distance;
			y = sm_stars[sm_fleets[c].system].y + ((sm_stars[sm_fleets[c].target].y-sm_stars[sm_fleets[c].system].y)*sm_fleets[c].enroute)/sm_fleets[c].distance;

			r = get_distance ( x - player.x, y - player.y);
			if (sm_fleets[c].explored == 1)
				sm_fleets[c].explored = 0;
		}
		if (r < shiptypes[0].sensor)
		{
			sm_fleets[c].explored = 1;
		}
	}
	else if (sm_fleets[c].explored == 1 && sm_fleets[c].num_ships > 0)
	{
		if (sm_fleets[c].enroute)
		{
			x = sm_stars[sm_fleets[c].system].x + ((sm_stars[sm_fleets[c].target].x-sm_stars[sm_fleets[c].system].x)*sm_fleets[c].enroute)/sm_fleets[c].distance;
			y = sm_stars[sm_fleets[c].system].y + ((sm_stars[sm_fleets[c].target].y-sm_stars[sm_fleets[c].system].y)*sm_fleets[c].enroute)/sm_fleets[c].distance;

			r = get_distance ( x - player.x, y - player.y);
			if (r > shiptypes[0].sensor)
				sm_fleets[c].explored = 0;
		}
	}
}


void starmap_flee()
{
	int32 c;
	int32 r;
	int32 d;
	int32 t=0;
	int32 mexp, dexp;
	int32 msim, dsim;
	int32 mdis, ddis;

	d = -1; 

	dsim = ddis = dexp = -1;
	msim = mdis = mexp = 12000;

	for (c = 0; c < num_stars; c++)
	{
		if (sm_stars[c].color >= 0 && c!=homesystem && c!=player.system)
		{
			// check simulated move
			r = simulate_move(c);
			if (r > -1 && r < msim)
			{
				dsim = c; msim = r;
			}
			// check simulated move to explored star
			if (r > -1 && r < mexp && sm_stars[c].explored==2)
			{
				dexp = c; mexp = r;
			}

			// check distance
			r = get_distance( sm_stars[c].x - player.x, sm_stars[c].y - player.y);
			if (r > -1 && r < mdis)
			{
				ddis = c; mdis = r;
			}
		}
	}

	if (dexp > -1)
	{
		d = dexp;
		if (mexp > -1 && msim < mexp/2)
			d = dsim;
	}
	else if (dsim > -1)
	{
		d = dsim;
	}
	else
		d = ddis;

	if (d > -1)
	{
		player.fold = 0;
		c = starmap_stardist(player.system, d); //get_distance( sm_stars[d].x - player.x, sm_stars[d].y - player.y ) ;
		if (c <= 2380)
		{
			for (c = 0; c < player.num_ships; c++)
				if (shiptypes[player.ships[c]].flag & 4)	// kuti
					player.fold = 1;
		}
		
		t = get_ik_timer(0);
		if (shipsystems[shiptypes[0].engine].par[0]==666 || player.fold)
			player.hyptime = t;
		player.target = d;
		player.engage = 2;
		player.distance = starmap_stardist(player.system, player.target);
		player.nebula = starmap_nebuladist(player.system, player.target);
	}

}

int32 simulate_move(int32 star)
{
	int32 dt = player.stardate;
	int32 sp1, sp2;
	int32 s, d;
	int32 str = player.system, dst = star;
	int32 a,c;
	int32 x,y;
	int32 end;
	int32 fold = 0;

	d = starmap_stardist(str, dst);
	s = 1;

	if (star == player.system)
		return -1;

//	if (sm_stars[star].novadate>0)
//		return -1;

	// don't flee into an enemy fleet
	for (x = 0; x < STARMAP_MAX_FLEETS; x++)
	{
		if (sm_fleets[x].explored>0 && sm_fleets[x].race!=race_klakar && sm_fleets[x].num_ships>0)
		{
			if (sm_fleets[x].system == star)
				return -1;
		}
	}

	if (d <= 2380)
	{
		for (c = 0; c < player.num_ships; c++)
			if (shiptypes[player.ships[c]].flag & 4)	// kuti 
				fold = 1;
	}

	if (shiptypes[0].engine > -1 && shiptypes[0].sysdmg[shiptypes[0].sys_eng]==0)
	{
		sp1 = shipsystems[shiptypes[0].engine].par[0];
		sp2 = shipsystems[shiptypes[0].engine].par[1];
	}
	else if (shiptypes[0].thrust > -1 && shiptypes[0].sysdmg[shiptypes[0].sys_thru]==0)
	{
		sp1 = 1; sp2 = 1;
	}
	else
	{
		sp1 = 0; sp2 = 0;
	}
	if (fold)	// fold
	{
		if (player.stardate - player.foldate < 7)
			dt += 7 - (player.stardate - player.foldate);
		s = d;
	}
	else if (sp1 == 666)		// hyperdrive
	{
		if (player.stardate - player.hypdate < 60)
			dt += 60 - (player.stardate - player.hypdate);
		s = d;
	}

	if (s==d)
	{	// check for deaths by nova
		for (c = 0; c < num_stars; c++)
		if (sm_stars[c].novadate>0 && dt>sm_stars[c].novadate && dt<sm_stars[c].novadate+4*365)
		{
			a = ((dt-sm_stars[c].novadate) * 39) / 365;	// size
			if (get_distance(sm_stars[c].x - sm_stars[dst].x, sm_stars[c].y - sm_stars[dst].y) < a/2+1)
			{	
				return -1;	
			}
		}
		return dt-player.stardate;
	}

	end = 0;
	while (!end && !must_quit)
	{
		ik_eventhandler();

		x = (sm_stars[str].x * (d-s) +
				 sm_stars[dst].x * s) / d;
		y = (sm_stars[str].y * (d-s) +
				 sm_stars[dst].y * s) / d;
		if (sp1 > 0 && sp2 > 0)
		{
			if (sm_nebulamap[((240-y)<<9)+(240+x)])
				s += sp2*2;
			else
				s += sp1*2;
			dt+=2;
		}
		else
		{
			s += 1;
			dt+=4;
		}

		// hit black holes?
		for (c = 0; c < num_holes; c++)
		if (sm_holes[c].size>0)
		{
			a = get_distance(sm_holes[c].x - x, sm_holes[c].y - y);
			if (sp1>0)
			{
				if (a < 96/(MAX(sp1,6)) )
				{
					end = 1;
				}
			}
			else if (a < 96/6)
			{
				end = 1;
			}
		}

		// hit novas
		for (c = 0; c < num_stars; c++)
		if (sm_stars[c].novadate>0 && dt>sm_stars[c].novadate && dt<sm_stars[c].novadate+4*365)
		{
			a = ((dt-sm_stars[c].novadate) * 39) / 365;	// size
			if (get_distance(sm_stars[c].x - x, sm_stars[c].y - y) < a/2+1)
			{	
				end = 1;
			}
		}

		if (s >= d)
			end = 2;
	}

	if (end==1)
		return -1;

	return dt - player.stardate;
}


void help_screen()
{
	int32 end;
	int32 c, mc;
	int32 t=0;
	int32 x, y;
	t_ik_image *bg;

	bg = ik_load_pcx("graphics/help.pcx", NULL);

	prep_screen();
	ik_copybox(bg, screen, 0, 0, 640, 480, 0,0);

#ifdef STARMAP_BUILD_HELP
	x = 556; y = 404;
	interface_thinborder(screen, x, y+4, x+80, y+72, STARMAP_INTERFACE_COLOR, 2+STARMAP_INTERFACE_COLOR*16);
	ik_print(screen, font_6x8, x+4, y+=8, 3, "MAGNIFIER");
	interface_textbox(screen, font_6x8, x+4, y+=8, 72, 64, 0, "If you can't read small text, toggle 2X magnifier by pressing F2 or CTRL."); 

	x = 16; y = 228;
	interface_thinborder(screen, x, y+4, x+128, y+64, STARMAP_INTERFACE_COLOR, 2+STARMAP_INTERFACE_COLOR*16);
	ik_print(screen, font_6x8, x+4, y+=8, 11, "NOTE ON STAR DRIVES");
	interface_textbox(screen, font_4x8, x+4, y+=8, 112, 64, 0, "In an emergency or if you have no star drive, you can slowly travel between stars using your thrusters at roughly the speed of light."); 


	interface_thinborder(screen, 240, 176, 400, 296, STARMAP_INTERFACE_COLOR);

	interface_thinborder(screen, 248, 16, 352, 68, STARMAP_INTERFACE_COLOR, 2+STARMAP_INTERFACE_COLOR*16);
	y = 12;

	ik_print(screen, font_6x8, 252, y+=8, 11, "CURRENT DATE");
	interface_textbox(screen, font_4x8, 252, y+=8, 96, 64, 0, "Click on the day, month or year to advance date, or wait at the current location.");

	interface_thinborder(screen, 376, 56, 456, 116, STARMAP_INTERFACE_COLOR, 2+STARMAP_INTERFACE_COLOR*16);
	y = 52;
	ik_print(screen, font_6x8, 380, y+=8, 11, "NEBULA");
	interface_textbox(screen, font_4x8, 380, y+=8, 72, 64, 0, "Traveling through these clouds of dust and gas is very slow without a special drive.");

	x = 408; y = 116;
	interface_thinborder(screen, x, y+4, x+96, y+88, STARMAP_INTERFACE_COLOR, 2+STARMAP_INTERFACE_COLOR*16);
	ik_print(screen, font_6x8, x+4, y+=8, 11, "STAR LANE");
	interface_textbox(screen, font_4x8, x+4, y+=8, 88, 96, 0, "Once you select a star system by clicking it, a star lane is displayed along with distance and estimated travel time. Press the ENGAGE button to move between the stars.");

	x = 512; y = 132;
	interface_thinborder(screen, x, y+4, x+72, y+56, STARMAP_INTERFACE_COLOR, 2+STARMAP_INTERFACE_COLOR*16);
	ik_print(screen, font_6x8, x+4, y+=8, 11, "UNEXPLORED");
	interface_textbox(screen, font_4x8, x+4, y+=8, 68, 32, 0, "This star hasn't been visited yet. No planets are displayed.");

	x = 440; y = 204;
	interface_thinborder(screen, x, y+4, x+144, y+32, STARMAP_INTERFACE_COLOR, 2+STARMAP_INTERFACE_COLOR*16);
	ik_print(screen, font_6x8, x+4, y+=8, 11, "BLACK HOLE");
	interface_textbox(screen, font_4x8, x+4, y+=8, 136, 32, 0, "Dangerous ship-eating singularity."); 

	x = 440; y = 244;
	interface_thinborder(screen, x, y+4, x+144, y+56, STARMAP_INTERFACE_COLOR, 2+STARMAP_INTERFACE_COLOR*16);
	ik_print(screen, font_6x8, x+4, y+=8, 11, "YOUR STARSHIP IN ORBIT");
	interface_textbox(screen, font_4x8, x+4, y+=8, 136, 64, 0, "When no star lane has been set, your ship is shown orbiting the star in your current system, along with any planets you have found."); 

	x = 440; y = 308;
	interface_thinborder(screen, x, y+4, x+144, y+48, STARMAP_INTERFACE_COLOR, 2+STARMAP_INTERFACE_COLOR*16);
	ik_print(screen, font_6x8, x+4, y+=8, 11, "ALIEN SPACECRAFT");
	interface_textbox(screen, font_4x8, x+4, y+=8, 136, 64, 0, "When you discover an alien spacecraft, it is displayed in orbit within the star system."); 

	x = 248; y = 332;
	interface_thinborder(screen, x, y+4, x+184, y+56, STARMAP_INTERFACE_COLOR, 2+STARMAP_INTERFACE_COLOR*16);
	ik_print(screen, font_6x8, x+4, y+=8, 11, "STAR SYSTEM SELECTION");
	interface_textbox(screen, font_4x8, x+4, y+=8, 176, 64, 0, "This window describes the currently selected star system. If the system has already been explored, information about the planet you found there is shown instead."); 

	x = 64; y = 292;
	interface_thinborder(screen, x, y+4, x+160, y+64, STARMAP_INTERFACE_COLOR, 2+STARMAP_INTERFACE_COLOR*16);
	ik_print(screen, font_6x8, x+4, y+=8, 11, "CARGO LISTING");
	y+=8*interface_textbox(screen, font_4x8, x+4, y+=8, 152, 64, 0, "A list of all the items you have on board. Many can be used in some way."); 

	ik_print(screen, font_4x8, x+4, y+=8, 0, "Click    to activate a device.");
	ik_dsprite(screen, x+28, y, spr_IFarrows->spr[14], 2+(3<<8));
	ik_print(screen, font_4x8, x+4, y+=8, 0, "Click    to install a ship system.");
	ik_dsprite(screen, x+28, y, spr_IFarrows->spr[13], 2+(3<<8));

	interface_thinborder(screen, 144, 48, 232, 228, STARMAP_INTERFACE_COLOR, 2+STARMAP_INTERFACE_COLOR*16);

	y = 44;
	ik_print(screen, font_6x8, 148, y+=8, 11, "ALLIED SHIPS");
	ik_copybox(screen, screen, 16, 24, 48, 40, 148, (y+=8)-1);
	ik_print(screen, font_4x8, 148, (y+=16), 0, "Click to select ship.");

	y+=6;
	ik_print(screen, font_6x8, 148, y+=8, 11, "HULL DAMAGE");
	ik_copybox(screen, screen, 80, 24, 144, 40, 148, y+=8);
	ik_print(screen, font_4x8, 148, y+=16, 0, "Click to repair.");

	y+=6;
	ik_print(screen, font_6x8, 148, y+=8, 11, "SHIP SYSTEMS");
	ik_dsprite(screen, 142, (y+=8)-5, spr_IFsystem->spr[1], 2+(1<<8));
	ik_print(screen, font_4x8, 156, y, 1, "Weapons");
	ik_dsprite(screen, 142, (y+=8)-5, spr_IFsystem->spr[5], 2+(3<<8));
	ik_print(screen, font_4x8, 156, y, 3, "Star Drive");
	ik_dsprite(screen, 142, (y+=8)-5, spr_IFsystem->spr[9], 2+(2<<8));
	ik_print(screen, font_4x8, 156, y, 2, "Combat Thrusters");
	ik_print(screen, font_4x8, 156, y+=8, 5, "Other Systems");

	y+=6;
	ik_print(screen, font_6x8, 148, y+=8, 11, "SYSTEM DAMAGE");
	ik_print(screen, font_4x8, 148, y+=8, 0, "Click    to repair");
	ik_dsprite(screen, 172, y, spr_IFarrows->spr[15], 2+(3<<8));
	ik_print(screen, font_4x8, 148, y+=8, 0, "damaged ship systems.");

	y+=6;
	ik_print(screen, font_4x8, 148, y+=8, 0, "Click    to remove");
	ik_dsprite(screen, 172, y, spr_IFarrows->spr[12], 2+(3<<8));
	ik_print(screen, font_4x8, 148, y+=8, 0, "undamaged systems.");
#endif

	ik_blit();

	update_palette();

	end = 0;
	x = key_pressed(key_f[0]); y = 0;
	while (!end && !must_quit)
	{
		ik_eventhandler();
		c = ik_inkey();
		mc = ik_mclick();
		x = key_pressed(key_f[0]); 
		if (!x)
		{
			if (!y)
				y = 1;
			else if (y==2)
				end = 1;
		}
		else if (y)
			y = 2;

		if (mc==1 || c>0)
			end = 1;

		c = t; t = get_ik_timer(2);
		if (t != c)
		{ prep_screen(); ik_blit(); }
	}

	if (must_quit)
		must_quit = 0;
}
// ----------------
//     INCLUDES
// ----------------

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <math.h>

#include "typedefs.h"
#include "is_fileio.h"

#include "textstr.h"
#include "iface_globals.h"
#include "gfx.h"
#include "snd.h"
#include "interface.h"
#include "combat.h"
#include "cards.h"
#include "endgame.h"

#include "starmap.h"

// ----------------
//		CONSTANTS
// ----------------

// ----------------
// GLOBAL VARIABLES
// ----------------

// ----------------
// LOCAL VARIABLES
// ----------------

extern int32 kawangi_score;
extern int32 kawangi_splode;


// ----------------
// LOCAL PROTOTYPES
// ----------------

// ----------------
// GLOBAL FUNCTIONS
// ----------------

// normal exploration

int32 starmap_entersystem()
{
	int32 c;
	int32 m;
	int32 ret = 1;

	if (sm_stars[player.system].novadate>0 && 
			player.stardate>=sm_stars[player.system].novadate && 
			player.stardate<=sm_stars[player.system].novadate + 120)
	{
		player.death = 4;
		player.num_ships = 0;
		return 0;
	}

	starmap_sensefleets();

	for (c=0;c<STARMAP_MAX_FLEETS;c++)
	if (sm_fleets[c].system == player.system && sm_fleets[c].enroute==0 && sm_fleets[c].num_ships > 0)
	{
		m = 0;
		if (races[sm_fleets[c].race].met == 2)
			m = 1;
		else if (fleet_encounter(c))
			m = 1;

		if (m)
		{
			m = races[sm_fleets[c].race].met;
			starmap_meetrace(sm_fleets[c].race);

			starmap_mantle(c);
			if (sm_fleets[c].race == race_muktian)
			{
				if (muktian_encounter()) // agree to leave -> "flee"
				{	sm_fleets[c].explored = 2; return 0; }
			}

			if (sm_fleets[c].race == race_klakar && m < 2)
			{
				sm_fleets[c].explored = 2;
				klakar_encounter();
			}

			if (races[sm_fleets[c].race].met < 2)
			{
				if (m == 0)
					enemy_encounter(sm_fleets[c].race);
				must_quit = 0;
				combat(c, 0);
				player.sel_ship = 0;
				if (sm_fleets[c].num_ships>0)
					ret = 0;
			}
		}
		else	// flee
		{			
			ret = 0;
		}
	}

	return ret;
}

void starmap_exploreplanet()
{
	int32 mc, c, ti=0, ot;
	int32 h, n;
	int32 s, sh;
	int32 end = 0;
	int32 it = -1, r = -1;
	int32 bx = 224, by = 112;
	int32 i[2], cu=0;
	int32 cname=1;
	char name[32];
	char hisher[8];
	char texty[256];


	if (sm_stars[player.target].explored == 2)
		return;

	if (sm_stars[player.target].planet == 10)
	{	cname = 0; }

	Stop_All_Sounds();

	halfbritescreen();

	if (sm_stars[player.target].explored)
		cname=0;
	sm_stars[player.target].explored = 2;
	c = sm_stars[player.target].card; 
//	card_display(c);

	h = 216;
	if ((ecards[c].type == card_item) || (ecards[c].type == card_rareitem) || (ecards[c].type == card_lifeform))
	{
		it = ecards[c].parm; //-1;
		if (it > -1)
		{
			starmap_additem(it, 0);
			if (itemtypes[it].type == item_weapon || itemtypes[it].type == item_system)
				starmap_tutorialtype = tut_upgrade;
			else if (itemtypes[it].type == item_device)
				starmap_tutorialtype = tut_device;
			else 
				starmap_tutorialtype = tut_treasure;
		}
	}
	else if (ecards[c].type == card_ally)
	{
		r = shiptypes[ecards[c].parm].race;

		if (r == race_none || r == race_muktian)
			starmap_advancedays(7);
		player.ships[player.num_ships] = ecards[c].parm;
		player.sel_ship = 0; //player.num_ships;
		starmap_tutorialtype = tut_ally;

		player.num_ships++;
		ik_print_log("Found Ally %s\n", shiptypes[player.ships[player.num_ships-1]].name);
		for (n = 0; n < shiptypes[player.ships[player.num_ships-1]].num_systems; n++)
		{
			ik_print_log("%s\n", shipsystems[shiptypes[player.ships[player.num_ships-1]].system[n]].name);
		}
	}
	else if (ecards[c].type == card_event)
	{
		player.bonusdata += ecards[c].parm;

		if (!strcmp(ecards[c].name, textstring[STR_EVENT_CONE]) || !strcmp(ecards[c].name, textstring[STR_EVENT_HULK]))
			h += 32;

	}

	prep_screen();
	interface_drawborder(screen,
											 bx, by, bx+192, by+h,
											 1, STARMAP_INTERFACE_COLOR, textstring[STR_CARD_PLANET]);


	ik_print(screen, font_6x8, bx+16, by+24, 3, sm_stars[player.target].planetname);
	if (cname)
		ik_print(screen, font_4x8, bx+176-strlen(textstring[STR_CARD_RENAME])*4, by+24, 3, textstring[STR_CARD_RENAME]);
	ik_dsprite(screen, bx+16, by+36, spr_SMplanet2->spr[sm_stars[player.target].planetgfx], 0);
	ik_dsprite(screen, bx+16, by+36, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));
	interface_textbox(screen, font_6x8,
										bx+84, by+36, 96, 104, 0,
										platypes[sm_stars[player.target].planet].text);

	sprintf(texty, ecards[c].text, "A", "B");
	if (ecards[c].type == card_event)
	{	
		sprintf(name, textstring[STR_CARD_EVENT]);

		if (!strcmp(ecards[c].name, textstring[STR_EVENT_DEVA]))
		{
			Play_Sound(WAV_NOPLANET, 15, 1);
			sprintf(texty, ecards[c].text, sm_stars[player.target].starname);
		}

		if (!strcmp(ecards[c].name, textstring[STR_EVENT_FLARE]))
		{
			sh = 0;
			if (player.num_ships > 1)
			{
				sh = -1;
				while (sh == -1)
				{
					sh = rand()%player.num_ships;
					if (shiptypes[player.ships[sh]].flag & 32)
						sh = -1;
				}
			}
	
			sh = player.ships[sh];
			Play_Sound(WAV_FLARE, 15, 1);
			s = 0;
			for (n = 0; n < shiptypes[sh].num_systems; n++)
			{
				if (shipsystems[shiptypes[sh].system[n]].item > -1)
					s++;
			}

			if (s > 0)
			{
				s = rand()%s;
				r = 0;
				for (n = 0; n < shiptypes[sh].num_systems; n++)
				{
					if (shipsystems[shiptypes[sh].system[n]].item > -1)
					{	
						if (r == s)
						{	s = n; break; }
						r++; 
					}
				}
				sprintf(texty, ecards[c].text, 
								shiptypes[sh].name, 
								shipsystems[shiptypes[sh].system[s]].name);
				shiptypes[sh].sysdmg[s]=1;
				//starmap_uninstallsystem(it, 1);
			}
			else
			{
				sprintf(texty, ecards[c].text2, shiptypes[sh].name); // player.shipname);
			}
			
		}
		else if (!strcmp(ecards[c].name, textstring[STR_EVENT_THIEF]))
		{
			Play_Sound(WAV_SPY, 15, 1);
			s = -1;
			for (n = 0; n < player.num_items; n++)
			{
				if (itemtypes[player.items[n]].type != item_lifeform && itemtypes[player.items[n]].flag != 7)
					s = 1;
			}
			if (s>-1)
			{
				s = rand()%player.num_items;
				while (itemtypes[player.items[s]].type == item_lifeform || itemtypes[player.items[s]].flag == 7)
					s = rand()%player.num_items;
				n = player.items[s];
				starmap_removeitem(s);
				sprintf(texty, ecards[c].text, itemtypes[n].name);
				if (rand()&1)
					kla_items[kla_numitems++]=n;
			}
			else
				sprintf(texty, ecards[c].text2, player.shipname);
		}

		else if (!strcmp(ecards[c].name, textstring[STR_EVENT_NOVA]))
		{
			Play_Sound(WAV_NOVA, 15, 1);
			sprintf(texty, ecards[c].text, sm_stars[player.system].starname);
			sm_stars[player.system].novadate = player.stardate+30;
			sm_stars[player.system].novatype = 0;
			for (n = 0; n < STARMAP_MAX_FLEETS; n++)
			if (sm_fleets[n].race == race_klakar && sm_fleets[n].num_ships>0)
			{
				r = get_distance(sm_stars[sm_fleets[n].system].x - sm_stars[player.system].x,
												 sm_stars[sm_fleets[n].system].y - sm_stars[player.system].y);
				if (r < 100)	// move klakar to safety
				{
					s = -1;
					while (s == -1 && !must_quit)
					{
						ik_eventhandler();
						s = rand()%num_stars;
						r = get_distance(sm_stars[s].x - sm_stars[player.system].x,
														 sm_stars[s].y - sm_stars[player.system].y);
						if (r < 100)
							s = -1;
						else if (s == homesystem)
							s = -1;
						else 
							for (r = 0; r < STARMAP_MAX_FLEETS; r++)
								if (sm_fleets[r].num_ships>0 && r!=n && sm_fleets[r].system==s)
									s = -1;
					}
					if (s > -1)
						sm_fleets[n].system = s;
				}
			}
		}
		else if (!strcmp(ecards[c].name, textstring[STR_EVENT_SABOT]))
		{
			Play_Sound(WAV_SABOTEUR, 15, 1);
			s = -1;
			if (shiptypes[0].num_systems > 0)
			{
				for (r = 0; r < shiptypes[0].num_systems; r++)
				if (shipsystems[shiptypes[0].system[r]].item > -1)
					s = 1;
			}
			if (s > -1)
			{
				s = -1;
				while (s == -1)
				{
					s = rand()%shiptypes[0].num_systems;
					if (shipsystems[shiptypes[0].system[s]].item == -1)
						s = -1;
				}
				sprintf(texty, ecards[c].text, 
								shipsystems[shiptypes[0].system[s]].name);
				starmap_destroysystem(s);
			}
			else
			{
				sprintf(texty, ecards[c].text2, 
								player.shipname);
				player.death = 5;
				player.num_ships = 0;
			}
		}
		else if (!strcmp(ecards[c].name, textstring[STR_EVENT_WHALE]))
		{
			Play_Sound(WAV_WHALES, 15);
		}
		else if (!strcmp(ecards[c].name, textstring[STR_EVENT_GIANT]))
		{
			Play_Sound(WAV_GASGIANT, 15, 1);
		}
		else if (!strcmp(ecards[c].name, textstring[STR_EVENT_CONE]))
		{
			Play_Sound(WAV_CUBE, 15, 1);

			ik_print(screen, font_4x8, bx+16, by+h-48, 0, textstring[STR_EVENT_CONE2]);
			for (s = 0; s < 2; s++)
			{
				n=0;
				while (!n && !must_quit)
				{
					ik_eventhandler();
					n = 1;
					i[s]=rand()%num_itemtypes;
					if (s==1 && i[s]==i[0])
						n=0;
					else if (itemtypes[i[s]].flag & 1)
						n=0;
					else {
						for (r=0;r<num_stars;r++)
						if (ecards[sm_stars[r].card].type>=card_item)	// && !sm_stars[r].explored
							if (ecards[sm_stars[r].card].parm == i[s])
								n=0;
					}
					if (n)
					{	for (r=0;r<kla_numitems;r++)
						if (kla_items[r]==i[s])
							n=0;
					}
					if (n)
					{	for (r=0;r<player.num_items;r++)
						if (player.items[r]==i[s])
							n=0;
					}
					if (n)
					{	for (r=0;r<shiptypes[0].num_systems;r++)
							if (shipsystems[shiptypes[0].system[r]].item>-1)
								if (shipsystems[shiptypes[0].system[r]].item == i[s])
									n=0;
					}
				}
			}
			starmap_additem(i[0], 0);
			starmap_additem(i[1], 0);
			ik_print(screen, font_6x8, bx+16, by+h-40, 3, itemtypes[i[0]].name);
			ik_print(screen, font_6x8, bx+16, by+h-32, 3, itemtypes[i[1]].name);
			cu = 1; r = -r;
		}
		else if (!strcmp(ecards[c].name, textstring[STR_EVENT_HULK]))
		{
			Play_Sound(WAV_SPACEHULK, 15, 1);

			ik_print(screen, font_4x8, bx+16, by+h-48, 0, textstring[STR_EVENT_HULK2]);
			for (s = 0; s < 2; s++)
			{
				n=0;
				while (!n && !must_quit)
				{
					ik_eventhandler();
					n = 1;
					i[s]=rand()%num_itemtypes;
					if (s==1 && i[s]==i[0])
						n=0;
					else if (itemtypes[i[s]].flag & 1)
						n=0;
					else {
						for (r=0;r<num_stars;r++)
						if (ecards[sm_stars[r].card].type>=card_item) // && !sm_stars[r].explored
							if (ecards[sm_stars[r].card].parm == i[s])
								n=0;
					}
					if (n)
					{	for (r=0;r<kla_numitems;r++)
						if (kla_items[r]==i[s])
							n=0;
					}
					if (n)
					{	for (r=0;r<player.num_items;r++)
						if (player.items[r]==i[s])
							n=0;
					}
					if (n)
					{	for (r=0;r<shiptypes[0].num_systems;r++)
							if (shipsystems[shiptypes[0].system[r]].item>-1)
								if (shipsystems[shiptypes[0].system[r]].item == i[s])
									n=0;
					}
				}
			}

			starmap_additem(i[0], 0);
			starmap_additem(i[1], 0);
			ik_print(screen, font_6x8, bx+16, by+h-40, 3, itemtypes[i[0]].name);
			ik_print(screen, font_6x8, bx+16, by+h-32, 3, itemtypes[i[1]].name);
			cu = 1; r = -r;
		}

	}
	else if (ecards[c].type == card_ally)
	{
		sprintf(name, textstring[STR_CARD_ALLY]);
	}
	else
	{	
		sprintf(name, textstring[STR_CARD_DISCOVERY], itemtypes[it].clas); 
		for (n = 0; n < (int32)strlen(name); n++)
		{
			if (name[n]>='a' && name[n]<='z')
				name[n]-='a'-'A';
		}
	}
	ik_print(screen, font_6x8, bx+96-strlen(name)*3, by+108, STARMAP_INTERFACE_COLOR, name);
	ik_print(screen, font_6x8, bx+16, by+120, 3, ecards[c].name);
 	if (it == -1)
	{
		interface_textbox(screen, font_6x8,
											bx+16, by+128, 160, 64, 0,
											texty); //ecards[c].text);
	}
	else
	{
		interface_textbox(screen, font_6x8,
											bx+16, by+128, 160, 64, 0,
											itemtypes[it].text);
		if (itemtypes[it].sound<0)
			Play_Sound(-itemtypes[it].sound, 15, 1);
		else
			Play_Sound(itemtypes[it].sound, 15);
	}

	interface_drawbutton(screen, bx+144, by+h-24, 32, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);

	ik_blit();

	if (settings.random_names & 4)
	{
		interface_tutorial(tut_explore);
	}

	if (ecards[c].type == card_ally)
	{
		starmap_meetrace(r);
		prep_screen();
		ik_blit();
		Play_Sound(WAV_ALLY, 15, 1);
	}

	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		mc = ik_mclick();	

		ot = ti; ti = get_ik_timer(2);
		if (ti != ot)
		{ prep_screen(); ik_blit();	}

		if (mc == 1)
		{
			if (cname)
			if (ik_mouse_y > by+24 && ik_mouse_y < by+32 && ik_mouse_x > bx+16 && ik_mouse_x < bx+176)
			{
				strcpy(name, sm_stars[player.target].planetname);
				prep_screen();
				ik_drawbox(screen, bx+16, by+24, bx+176, by+32, STARMAP_INTERFACE_COLOR*16+3);
				free_screen();
				ik_text_input(bx+16, by+24, 16, font_6x8, "", name, STARMAP_INTERFACE_COLOR*16+3, STARMAP_INTERFACE_COLOR);
				if (strlen(name))
					strcpy(sm_stars[player.target].planetname, name);

				prep_screen();
				ik_drawbox(screen, bx+16, by+24, bx+176, by+32, STARMAP_INTERFACE_COLOR*16+3);
				ik_print(screen, font_6x8, bx+16, by+24, 3, sm_stars[player.target].planetname);

				ik_blit();
			}

			if (ik_mouse_y > by+h-24 && ik_mouse_y < by+h-8 && ik_mouse_x > bx+144 && ik_mouse_x < bx+176)
			{	end = 1; Play_SoundFX(WAV_DOT, get_ik_timer(0)); }
			if (cu)
			{
				if (ik_mouse_x > bx+16 && ik_mouse_x < bx+176 && ik_mouse_y > by+h-40 && ik_mouse_y < by+h-24)
				{
					Play_SoundFX(WAV_INFO);
					interface_popup(font_6x8, bx+16, by+h-112, 192, 96, STARMAP_INTERFACE_COLOR, 0, 
												itemtypes[i[(ik_mouse_y-(by+h-40))/8]].name, itemtypes[i[(ik_mouse_y-(by+h-40))/8]].text);
				}				
			}
		}

//		if (c == 13 || mc == 1)
//			end = 1;
	}

	ik_print_log("Exploring the star system, discovered a %s planet and named it %s.\n", 
								platypes[sm_stars[player.target].planet].name,
								sm_stars[player.target].planetname);

	must_quit = 0;

	if (ecards[c].type == card_ally)
	{
		Stop_Sound(15);

		if (r==race_terran || r==race_zorg || r==race_garthan)	// terran / zorg / garthan mercs
		{
			if (shiptypes[ecards[c].parm].flag & 64)
				sprintf(hisher, textstring[STR_MERC_HER]);
			else
				sprintf(hisher, textstring[STR_MERC_HIS]);

			sprintf(texty, textstring[STR_MERC_PAYMENT], 
							hulls[shiptypes[ecards[c].parm].hull].name,
							shiptypes[ecards[c].parm].name,
							hisher);
			n = pay_item(textstring[STR_MERC_BILLING], texty, r);

			if (n == -1)
			{
				starmap_removeship(player.num_ships-1);
				starmap_tutorialtype = tut_starmap;
				ik_print_log("Ally cancelled\n");
			}
			else
				sm_stars[player.system].card = 0;
		}
		else
			sm_stars[player.system].card = 0;
	}
	else if (ecards[c].type == card_lifeform)
	{
		if (itemtypes[it].flag & lifeform_hard)
		{
			h = itemtypes[it].cost/10 + rand()%(itemtypes[it].cost/10);
			sprintf(texty, textstring[STR_LIFEFORM_HARD], itemtypes[it].name, h);
			if (!interface_popup(font_6x8, bx+16, by+96, 192, 0, STARMAP_INTERFACE_COLOR, 0, textstring[STR_LIFEFORM_HARDT], texty, textstring[STR_YES], textstring[STR_NO]))
			{
				starmap_advancedays(h);
				sm_stars[player.system].card = 0;
			}
			else
				starmap_removeitem(player.num_items-1);
		}
		else
			sm_stars[player.system].card = 0;
	}



	Stop_Sound(15);

	reshalfbritescreen();
}

int32 starmap_explorehole(int32 h, int32 t)
{
	int32 mc, c;
	int32 end = 0;
	int32 bx = SM_MAP_X+136, by = SM_MAP_Y+48+240*(sm_holes[h].y>0), z;
	int32 mx, my;
	char name[32];

	if (sm_holes[h].explored)
		return 1;

	if (!sm_holes[h].size)
		return 1;

	// remove holes on first move
	if (player.system == homesystem)
	{
		sm_holes[h].size = 0;
		return 1;
	}

	// remove holes if escaping a nova
	if (sm_stars[player.system].novadate > 0 &&
			sm_stars[player.system].novatype == 0 &&
			player.stardate < sm_stars[player.system].novadate + 365*4)
	{
		sm_holes[h].size = 0;
		return 1;
	}

	player.explore = h+1;
	sm_holes[h].explored = 1;
	player.bonusdata += 200;

	z = 148;
	Play_Sound(WAV_BLACKHOLE, 15, 1);

	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		mc = ik_mclick();	
		mx = ik_mouse_x - bx; my = ik_mouse_y - by;

		if (must_quit)
		{
			end = 1;
			must_quit = 0;
		}

		c = t; t = get_ik_timer(0);

		if (t != c)
		{
			prep_screen();
			starmap_display(t);

			interface_drawborder(screen,
													 bx, by, bx+208, by+z,
													 1, STARMAP_INTERFACE_COLOR, textstring[STR_BLACKHOLE_TITLE]);
			ik_drawbox(screen, bx+16, by+40, bx+79, by+103, 0);
			ik_drsprite(screen, bx+48, by+72, 1023-((t*2)&1023), 64, spr_SMstars->spr[8], 4);
			ik_dsprite(screen, bx+16, by+40, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));
			ik_print(screen, font_6x8, bx+16, by+24, 3, sm_holes[h].name);
			ik_print(screen, font_4x8, bx+192-strlen(textstring[STR_CARD_RENAME])*4, by+24, 3, textstring[STR_CARD_RENAME]);
			interface_textbox(screen, font_4x8,
												bx+88, by+40, 104, 64, 0,
												textstring[STR_BLACKHOLE_DESC]);
			ik_print(screen, font_4x8, bx+16, by+112, 0, textstring[STR_BLACKHOLE_WARN]);
			interface_drawbutton(screen, bx+16, by+z-24, 64, STARMAP_INTERFACE_COLOR, textstring[STR_GOBACK]);
			interface_drawbutton(screen, bx+208-80, by+z-24, 64, STARMAP_INTERFACE_COLOR, textstring[STR_CONTINUE]);
			ik_blit();
		}

		if (mc == 1)
		{
			if (my > 24 && my < 32) // rename
			{
				strcpy(name, sm_holes[h].name);
				prep_screen();
				ik_drawbox(screen, bx+16, by+24, bx+192, by+32, STARMAP_INTERFACE_COLOR*16+3);
				free_screen();
				ik_text_input(bx+16, by+24, 16, font_6x8, "", name, STARMAP_INTERFACE_COLOR*16+3, STARMAP_INTERFACE_COLOR);
				if (strlen(name))
					strcpy(sm_holes[h].name, name);
			}
			if (my > z-24 && my < z-8)
			{
				if (mx > 208-80 && mx < 192)
				{	end = 2; Play_SoundFX(WAV_ACCEPT, get_ik_timer(0)); }
				if (mx > 16 && mx < 80)
				{	end = 1; Play_SoundFX(WAV_DECLINE, get_ik_timer(0)); }
			}
		}
	}
	/*
	ik_print_log("Discovered an uncharted black hole and named it %s.");
	if (end==2)
		ik_print_log("Determined the singularity was not close enough to endanger the %s and stayed on course toward %s system.\n",
									player.shipname, sm_stars[player.target].starname);
	else
		ik_print_log("The proximity of the black hole to our plotted course posed a danger to %s and forced us to turn back.\n",
									player.shipname);
*/
	player.explore = 0;
	Stop_Sound(15);

	return end-1;	
}

// alien encounters


int32 fleet_encounter(int32 flt, int32 inc)
{
	int32 mc, c;
	int32 end = 0;
	int32 bx = 240, by = 152;
	int32 mx, my;
	int32 r, t, t0, st;
	int32 l;
	int32 s;
	int32	upd = 1;
	int32 sx[16], sy[16];
	char texty[32];

	halfbritescreen();

	r = sm_fleets[flt].race;

	if (!sm_fleets[flt].explored)
		sm_fleets[flt].explored = 1;

	Stop_All_Sounds();

	t = sm_fleets[flt].num_ships;
	for (c = 0; c < t; c++)
	{
		sx[c] = bx + 32 + (c*96+24+rand()%48)/t;
		sy[c] =	by + 56 + rand()%16;
	}

	Play_Sound(WAV_RADAR, 15, 1);

	st = t = get_ik_timer(0);
	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		mc = ik_mclick();	
		t0 = t; t = get_ik_timer(0);
		mx = ik_mouse_x - bx; my = ik_mouse_y - by;
		if (mc == 1 && mx > 16 && mx < 64 && my > 160 && my < 176)
		{	end = 1; Play_SoundFX(WAV_DECLINE, get_ik_timer(0)); }
		if (mc == 1 && mx > 96 && mx < 144 && my > 160 && my < 176)
		{	end = 2; Play_SoundFX(WAV_ACCEPT, get_ik_timer(0)); }
		if (must_quit)
		{
			end = 1;
			must_quit = 0;
		}

		if (t > t0 || upd == 1)
		{
			upd = 0;
			prep_screen();
			if (!inc)
				sprintf(texty, textstring[STR_SCANNER_ALIENS]);
			else
				sprintf(texty, textstring[STR_SCANNER_INCOMING]);
			interface_drawborder(screen,
													 bx, by, bx+160, by+184,
													 1, STARMAP_INTERFACE_COLOR, texty);
			if (races[r].met)
			{
				sm_fleets[flt].explored = 2;
				ik_print(screen, font_6x8, bx+16, by+24, 3, textstring[STR_SCANNER_RACE], races[r].name);
			}
			else
				ik_print(screen, font_6x8, bx+16, by+24, 3, textstring[STR_SCANNER_NORACE]);

			ik_dsprite(screen, bx+16, by+32, spr_IFborder->spr[19], 2+(4<<8));
			l = 10 + 5 * (((t-st)%50)<10);
			for (c = 0; c < sm_fleets[flt].num_ships; c++)
			{
				s = hulls[shiptypes[sm_fleets[flt].ships[c]].hull].size;
				ik_drsprite(screen, sx[c], sy[c], 0, 8,
									 spr_IFarrows->spr[10-(s<32)+(s>80)], 5+(l<<8));
			}
			l = ((t&63)*3 - 95 + 1024) & 1023;
			ik_dspriteline(screen, bx + 80, by + 152, 
										bx + 80 + ((sin1k[l] * 112) >> 16), 
										by + 152 - ((cos1k[l] * 112) >> 16), 
										8, (t&31), 18, spr_weapons->spr[2], 5+(10<<8));
			if (!inc)
			{
				interface_drawbutton(screen, bx+16, by+160, 48, STARMAP_INTERFACE_COLOR, textstring[STR_SCANNER_AVOID]);
				interface_drawbutton(screen, bx+96, by+160, 48, STARMAP_INTERFACE_COLOR, textstring[STR_SCANNER_ENGAGE]);
			}
			else
			{
				interface_drawbutton(screen, bx+16, by+160, 48, STARMAP_INTERFACE_COLOR, textstring[STR_SCANNER_FLEE]);
				interface_drawbutton(screen, bx+96, by+160, 48, STARMAP_INTERFACE_COLOR, textstring[STR_SCANNER_ENGAGE]);
			}
			ik_blit();

			if (settings.random_names & 4)
			{
				interface_tutorial(tut_encounter);
			}
//			if (t/50 > t0/50)
//				Play_Sound(WAV_RADAR, 15, 1);

		}

	}

	Stop_Sound(15);

	reshalfbritescreen();
	return end - 1;
}

void starmap_meetrace(int32 r)
{
	int32 mc, c;
	int32 end = 0;
	int32 t=0;
	int32 bx = 216, by = 152;
	int32 mx, my;

	if (races[r].met)
		return;

	if (r < race_klakar)
		return;

	if (r >= race_unknown) 
		return;

	races[r].met = 1;

	halfbritescreen();

	prep_screen();
	interface_drawborder(screen,
											 bx, by, bx+208, by+152,
											 1, STARMAP_INTERFACE_COLOR, textstring[STR_ALIEN_CONTACT]);
	ik_print(screen, font_6x8, bx+104-strlen(races[r].name)*3, by+26, 3, races[r].name);
	interface_textbox(screen, font_4x8,
										bx+88, by+40, 104, 64, 0,
										races[r].text);
	ik_dsprite(screen, bx+16, by+40, spr_SMraces->spr[r], 0);
	ik_dsprite(screen, bx+16, by+40, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));
	ik_print(screen, font_4x8, bx+16, by+112, 0, textstring[STR_ALIEN_DEMEANOR], races[r].text2);
	interface_drawbutton(screen, bx+160, by+124, 32, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);

	ik_blit();

	Play_Sound(WAV_KLAKAR + r - race_klakar, 15, 1);

	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		mc = ik_mclick();	
		c = ik_inkey();
		mx = ik_mouse_x - bx; my = ik_mouse_y - by;

		if (mc == 1 && mx > 160 && mx < 192 && my > 124 && my < 140)
		{	end = 1; Play_SoundFX(WAV_DOT, get_ik_timer(0)); }

		c = t; t = get_ik_timer(2);
		if (t != c)
		{ prep_screen(); ik_blit();	}
	}

	Stop_Sound(15);
	must_quit = 0;

	ik_print_log("Made contact with a previously unknown alien race, the %s.\n", races[r].name);

	reshalfbritescreen();
}

void starmap_mantle(int32 flt)
{
	int32 mc, c;
	int32 end = 0;
	int32 bx = 216, by = 152, h = 112;
	int32 mx, my;
	int32 r = sm_fleets[flt].race;
	int32 t=0;
	char texty[256];

	int32 bab = 0;

	for (c = 0; c < player.num_items; c++)
		if (itemtypes[player.items[c]].flag & device_mantle)
			bab = 1;

	if (bab == 0 && races[r].met == 2)
	{
		if (r != race_klakar && r != race_muktian)
			races[r].met = 1;
		return;
	}

	if (bab == 1 && races[r].met == 2)
	{
		sm_fleets[flt].explored = 2;
	}

	if (bab == 1 && races[r].met != 2)
	{
		if (r == race_muktian || r == race_garthan || r == race_urluquai)
		{
			races[r].met = 2;
			Play_SoundFX(WAV_MANTLE, 0);
			sm_fleets[flt].explored = 2;

			halfbritescreen();

			prep_screen();
			sprintf(texty, textstring[STR_VIDCAST], races[r].name);
			interface_drawborder(screen,
													 bx, by, bx+208, by+h,
													 1, STARMAP_INTERFACE_COLOR, texty);
			switch(r)
			{
				case race_muktian:
				c = STR_MANTLE_MUKTIAN + (rand()&1);
				break;
				case race_garthan:
				c = STR_MANTLE_GARTHAN + (rand()&1);
				break;
				case race_urluquai:
				c = STR_MANTLE_URLUQUAI + (rand()&1);
				break;
				default:
				c = STR_MANTLE_MUKTIAN;
			}

			interface_textbox(screen, font_4x8,
												bx+88, by+24, 104, 64, 0,
												textstring[c]);
			ik_dsprite(screen, bx+16, by+24, spr_SMraces->spr[r], 0);
			ik_dsprite(screen, bx+16, by+24, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));
			interface_drawbutton(screen, bx+160, by+h-24, 32, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);

			ik_blit();

			while (!must_quit && !end)
			{
				ik_eventhandler();  // always call every frame
				mc = ik_mclick();	
				c = ik_inkey();
				mx = ik_mouse_x - bx; my = ik_mouse_y - by;

				if (mc == 1 && mx > 160 && mx < 192 && my > h-24 && my < h-8)
				{	end = 1; Play_SoundFX(WAV_DOT, get_ik_timer(0)); }

				c = t; t = get_ik_timer(2);
				if (t != c)
				{ prep_screen(); ik_blit();	}
			}

			ik_print_log("Made contact with a previously unknown alien race, the %s.\n", races[r+2].name);

			reshalfbritescreen();
		}
	}
	must_quit = 0;
	return;
}

void enemy_encounter(int32 r)
{
	int32 mc, c;
	int32 end = 0;
	int32 bx = 216, by = 152;
	int32 mx, my;
	int32 t=0;
	char texty[256];
	char *tx;

	if (r != race_garthan && r != race_urluquai && r != race_tanru)
		return;

	halfbritescreen();

	// generic enemy greeting screen
	prep_screen();
	sprintf(texty, textstring[STR_VIDCAST], races[r].name);
	interface_drawborder(screen,
											 bx, by, bx+208, by+144,
											 1, STARMAP_INTERFACE_COLOR, texty);
	ik_print(screen, font_6x8, bx+16, by+26, 3, textstring[STR_VIDCAST2]);

	if (r == race_garthan)
		sprintf(texty, textstring[STR_GARTHAN_WARN1+rand()%3]);
	else if (r == race_urluquai)
		sprintf(texty, textstring[STR_URLUQUAI_WARN1+rand()%3]);
	else // tan ru
	{
		// generate random tan ru message
		tx = texty; c = 0;
		while (c < 180)
		{
			mx = rand()%3;
			switch (mx)
			{
				case 0:
				*tx++ = 'A'+rand()%('Z'+1-'A');
				*tx++ = 'A'+rand()%('Z'+1-'A');
				*tx++ = 'A'+rand()%('Z'+1-'A');
				*tx++ = ' ';
				c+=4;
				break;

				case 1:
				mc = 1+rand()%8;
				while (mc--)
				{
					*tx++ = '0'+(rand()&1);
					c++;
				}
				*tx++ = ' ';
				c++;
				break;

				case 2:
				default:
				mc = rand()%(strlen(textstring[STR_TANRU_WARN])/4 + 1);
				*tx++ = textstring[STR_TANRU_WARN][mc*4];
				*tx++ = textstring[STR_TANRU_WARN][mc*4+1];
				*tx++ = textstring[STR_TANRU_WARN][mc*4+2];
				*tx++ = ' ';
				c+=4;

			}
		}
		*tx++ = 0;
	}
	interface_textbox(screen, font_4x8, bx+88, by+40, 104, 64, 0, texty);
	ik_dsprite(screen, bx+16, by+40, spr_SMraces->spr[r], 0);
	ik_dsprite(screen, bx+16, by+40, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));
	interface_drawbutton(screen, bx+160, by+116, 32, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);

	ik_blit();

	Play_Sound(WAV_MESSAGE+(r==race_tanru), 15, 1);

	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		mc = ik_mclick();	
		c = ik_inkey();
		mx = ik_mouse_x - bx; my = ik_mouse_y - by;

		if (mc == 1 && mx > 160 && mx < 192 && my > 116 && my < 132)
		{	end = 1; Play_SoundFX(WAV_DOT, get_ik_timer(0)); }

		c = t; t = get_ik_timer(2);
		if (t != c)
		{ prep_screen(); ik_blit();	}
	}

	Stop_Sound(15);
	must_quit = 0;

	reshalfbritescreen();
	prep_screen();
	ik_blit();
}

int32 muktian_encounter()
{
	int32 mc, c;
	int32 end = 0;
	int32 bx = 216, by = 152;
	int32 mx, my;
	int32 r=0, m=0, t=0;
	int32 o = 0;
	char str[256];

	r = race_muktian;


	m = 0; mc = -1;
	for (c = 0; c < player.num_ships; c++)
	if (shiptypes[player.ships[c]].race == 	r)
	{	
		m = 1; mc = c;
	}

	if (races[r].met == 2 && m == 0)	// if peaceful, continue
		return 0;

	halfbritescreen();

	if (m)
		starmap_removeship(mc);

	prep_screen();
	sprintf(str, textstring[STR_VIDCAST], races[r].name);
	interface_drawborder(screen,
											 bx, by, bx+208, by+144,
											 1, STARMAP_INTERFACE_COLOR, str);
	ik_print(screen, font_6x8, bx+16, by+26, 3, textstring[STR_VIDCAST2]);
	if (m)
	{
		interface_textbox(screen, font_4x8,
											bx+88, by+40, 104, 64, 0,
											textstring[STR_MUKTIAN_THANKS]);
		races[r].met = 2;
		interface_drawbutton(screen, bx+16, by+116, 64, STARMAP_INTERFACE_COLOR, textstring[STR_DECLINE]);
		interface_drawbutton(screen, bx+128, by+116, 64, STARMAP_INTERFACE_COLOR, textstring[STR_ACCEPT]);
	}
	else
	{
		interface_textbox(screen, font_4x8,
											bx+88, by+40, 104, 64, 0,
											textstring[STR_MUKTIAN_WARNING]);
		interface_drawbutton(screen, bx+16, by+116, 64, STARMAP_INTERFACE_COLOR, textstring[STR_LEAVE]);
		interface_drawbutton(screen, bx+128, by+116, 64, STARMAP_INTERFACE_COLOR, textstring[STR_STAY]);
		o = 1;
	}
	ik_dsprite(screen, bx+16, by+40, spr_SMraces->spr[r], 0);
	ik_dsprite(screen, bx+16, by+40, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));
//	interface_drawbutton(screen, bx+16, by+116, 64, STARMAP_INTERFACE_COLOR, "DECLINE");
//	interface_drawbutton(screen, bx+128, by+116, 64, STARMAP_INTERFACE_COLOR, "ACCEPT");

	ik_blit();

	Play_Sound(WAV_MESSAGE, 15, 1);

	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		mc = ik_mclick();	
		c = ik_inkey();
		mx = ik_mouse_x - bx; my = ik_mouse_y - by;

		if (must_quit)
		{
			end = 1+o;
			must_quit = 0;
		}

		if (mc == 1 && mx > 16 && mx < 80 && my > 116 && my < 132)
		{	end = 1+o; Play_SoundFX(WAV_DECLINE, get_ik_timer(0));	}
		if (mc == 1 && mx > 128 && mx < 192 && my > 116 && my < 132)
		{	end = 2-o; Play_SoundFX(WAV_ACCEPT, get_ik_timer(0));	}

		c = t; t = get_ik_timer(2);
		if (t != c)
		{ prep_screen(); ik_blit();	}
	}

	Stop_Sound(15);

	reshalfbritescreen();
	prep_screen();
	ik_blit();

	if (m)
	{
		if (end == 2)	// pick up ambassador
		{
			Play_Sound(WAV_MUKTIAN, 15, 1);

			interface_popup(font_6x8, 224, 192, 192, 0, STARMAP_INTERFACE_COLOR, 0, 
											textstring[STR_AMBASSADORT], textstring[STR_AMBASSADOR], textstring[STR_OK]);
			Stop_Sound(15);
			for (c = 0; c < num_itemtypes; c++)
			if (itemtypes[c].flag & lifeform_ambassador) 
			{
				starmap_additem(c, 0);
			}
		}

		return 0;
	}

	return end-1;
}

void klakar_encounter()
{
	int32 mc, c;
	int32 end = 0;
	int32 bx = 216, by = 152;
	int32 mx, my;
	int32 r=0, t=0;
//	t_ik_sprite *bg;

	halfbritescreen();

	r = race_klakar;

	// trader greeting screen
	prep_screen();
//	bg = get_sprite(screen, bx, by, 208, 144);
	interface_drawborder(screen,
											 bx, by, bx+208, by+144,
											 1, STARMAP_INTERFACE_COLOR, textstring[STR_TRADE_TITLE]);
	ik_print(screen, font_6x8, bx+16, by+26, 3, textstring[STR_VIDCAST2]);
	interface_textbox(screen, font_4x8,
										bx+88, by+40, 104, 64, 0,
										textstring[STR_TRADE_MESSAGE]);
	races[r].met = 2;

	ik_dsprite(screen, bx+16, by+40, spr_SMraces->spr[r], 0);
	ik_dsprite(screen, bx+16, by+40, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));
	interface_drawbutton(screen, bx+16, by+116, 64, STARMAP_INTERFACE_COLOR, textstring[STR_LEAVE]);
	interface_drawbutton(screen, bx+128, by+116, 64, STARMAP_INTERFACE_COLOR, textstring[STR_TRADE]);

	ik_blit();

	Play_Sound(WAV_MESSAGE, 15, 1);

	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		mc = ik_mclick();	
		c = ik_inkey();
		mx = ik_mouse_x - bx; my = ik_mouse_y - by;

		if (must_quit)
		{
			end = 1;
			must_quit = 0;
		}

		if (mc == 1 && mx > 16 && mx < 80 && my > 116 && my < 132)
		{	end = 1; Play_SoundFX(WAV_DECLINE, get_ik_timer(0)); }
		if (mc == 1 && mx > 128 && mx < 192 && my > 116 && my < 132)
		{	end = 2; Play_SoundFX(WAV_ACCEPT, get_ik_timer(0)); }

		c = t; t = get_ik_timer(2);
		if (t != c)
		{ prep_screen(); ik_blit();	}
	}

	Stop_Sound(15);

//	ik_dsprite(screen, bx, by, bg, 4);

	reshalfbritescreen();

	//free_sprite(bg);

	for (c = 0; c < num_itemtypes; c++)
	if (itemtypes[c].flag & device_beacon)
	{
		starmap_additem(c, 0);
	}

	if (end == 1)
	{
		prep_screen();
		ik_blit();
		return;
	}

	klakar_trade();
}

// kawangi encounter

void kawangi_warning()
{
	int32 mc, c;
	int32 end = 0;
	int32 bx = 216, by = 152, h = 208;
	int32 mx, my;
	int32 t=0;
	char texty[512];

	halfbritescreen();

	prep_screen();
	interface_drawborder(screen,
											 bx, by, bx+208, by+h,
											 1, STARMAP_INTERFACE_COLOR, "Distress Call");
	interface_textbox(screen, font_4x8,
										bx+16, by+24, 180, 64, 0,
										textstring[STR_KAWANGI_WARNING1]);

	interface_textbox(screen, font_4x8,
										bx+88, by+48, 104, 64, 0,
										textstring[STR_KAWANGI_WARNING2]);

	sprintf(texty, textstring[STR_KAWANGI_WARNING3], player.shipname);
	interface_textbox(screen, font_4x8,
										bx+16, by+112, 176, 80, 0,
										texty);

	ik_dsprite(screen, bx+16, by+44, spr_IFdifnebula->spr[1], 0);
	ik_dsprite(screen, bx+16, by+44, hulls[shiptypes[racefleets[races[race_kawangi].fleet].stype[0]].hull].sprite, 0);
	ik_dsprite(screen, bx+16, by+44, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));
	interface_drawbutton(screen, bx+144, by+h-24, 48, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);

	ik_blit();

	Play_Sound(WAV_WARNING, 15, 1);

	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		mc = ik_mclick();	
		c = ik_inkey();
		mx = ik_mouse_x - bx; my = ik_mouse_y - by;

		if (mc == 1 && mx > 144 && mx < 192 && my > h-24 && my < h-8)
		{	end = 1; Play_SoundFX(WAV_DOT); }

		c = t; t = get_ik_timer(2);
		if (t != c)
		{ prep_screen(); ik_blit();	}
	}

	Stop_Sound(15);

	must_quit = 0;
	Play_Sound(WAV_BRIDGE, 15, 1, 50);

	reshalfbritescreen();
}

void kawangi_message(int32 flt, int32 m)
{
	int32 mc, c;
	int32 end = 0;
	int32 bx = 216, by = 176, h = 128;
	int32 mx, my;
	int32 t=0;
	char texty[512];
	char topic[32];

	halfbritescreen();

	switch (m)
	{
		case 0:	// destroyed
		Play_Sound(WAV_MUS_ROCK, 15, 1);

		if (sm_fleets[flt].explored < 2)	// don't know it's the kawangi
		{
			sprintf(texty, textstring[STR_KAWANGI_KILLED1]);
			sprintf(topic, textstring[STR_KAWANGI_KILLED]);
		}
		else
		{
			sprintf(texty, textstring[STR_KAWANGI_KILLED2]);
			sprintf(topic, races[race_kawangi].name);
		}
		break;

		case 1:	// detect explosion
		Play_Sound(WAV_OPTICALS, 15, 1);

		if (kawangi_score == 0)
		{
			sprintf(texty, textstring[STR_KAWANGI_EXPLO1], sm_stars[sm_fleets[flt].system].starname);
			sprintf(topic, textstring[STR_KAWANGI_EXPLO]);
			kawangi_score++;
		}
		else
		{
			sprintf(texty, textstring[STR_KAWANGI_EXPLO2], sm_stars[sm_fleets[flt].system].starname);
			sprintf(topic, textstring[STR_KAWANGI_EXPLO]);
			kawangi_score++;
		}
		break;
		
		default: ;
	}

	h = interface_textboxsize(font_6x8, 176, 128, texty)*8 + 48;
	by = 224 - h/2;

	prep_screen();

	interface_drawborder(screen,
											 bx, by, bx+208, by+h,
											 1, STARMAP_INTERFACE_COLOR, topic);
	interface_textbox(screen, font_6x8,
										bx+16, by+24, 176, h, 0,
										texty);

	interface_drawbutton(screen, bx+144, by+h-24, 48, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);

	ik_blit();

	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		mc = ik_mclick();	
		c = ik_inkey();
		mx = ik_mouse_x - bx; my = ik_mouse_y - by;

		if (mc == 1 && mx > 144 && mx < 192 && my > h-24 && my < h-8)
		{	end = 1; Play_SoundFX(WAV_DOT); }

		c = t; t = get_ik_timer(2);
		if (t != c)
		{ prep_screen(); ik_blit();	}
	}

	must_quit = 0;
	Stop_Sound(15);
	Play_Sound(WAV_BRIDGE, 15, 1, 50);

	reshalfbritescreen();
}

void starmap_kawangimove(int flt)
{
	int32 c;
	int32 r, rr, cr;
	int32 k;

	rr = -1;
	cr = starmap_stardist(sm_fleets[flt].system, homesystem);
	sm_fleets[flt].target = sm_fleets[flt].system;

	/*
	 taking the shortest route towards hope

		the next star must be closer to hope than current one
		the closer the next star is to current one the better
		(go short hops rather than head straight for hope)

	*/

	if (sm_fleets[flt].system == homesystem) // already at hope
	{
		sm_fleets[flt].target = num_stars;
	}
	else
	{
		for (c = 0; c < num_stars; c++)
		{
			k = 1;
			if (c == sm_fleets[flt].system)
				k = 0;
			else if (ecards[sm_stars[c].card].type == card_rareitem)
				if ((itemtypes[ecards[sm_stars[c].card].parm].flag & device_collapser)>0 && sm_stars[c].explored < 2)
					k = 0;

			if (k)
			{
				r = starmap_stardist(c, homesystem);
				if (r < cr)	// only go to planets that are closer to hope
				{
					r += starmap_stardist(sm_fleets[flt].system, c) * 2;	
					// add the distance to this star from current
					if (r < rr || rr==-1)
					{	
						sm_fleets[flt].target = c;
						rr = r;
					}
				}
			}
		}
	}

	if (sm_fleets[flt].target != sm_fleets[flt].system)	// go kawangi go!
	{
		// blow up system
		if (sm_fleets[flt].system < num_stars && sm_stars[sm_fleets[flt].system].planet != 10)
		{
			kawangi_splode = 1;
		}

		sm_fleets[flt].distance = starmap_stardist(sm_fleets[flt].system, sm_fleets[flt].target);
		sm_fleets[flt].enroute = 1;

		if (sm_fleets[flt].system == homesystem)
			player.death = 6;
	}
}
// ----------------
//     INCLUDES
// ----------------

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <math.h>
#include <time.h>

#include "typedefs.h"
#include "iface_globals.h"
#include "is_fileio.h"
#include "gfx.h"
#include "snd.h"
#include "interface.h"
#include "cards.h"
#include "combat.h"
#include "textstr.h"

#include "starmap.h"

// ----------------
//     CONSTANTS
// ----------------

#define NUM_KLAITEMS	8
#define NUM_KLAWEP		2
#define NUM_KLASYS		3



char *planet_keywords[plkMax] = 
{
	"PLNT",
	"NAME",
	"TEXT",
	"VALU",
	"END",
};

char *star_keywords[stkMax] = 
{
	"STAR",
	"NAME",
	"TEXT",
	"END",
};

char *item_keywords[itkMax] = 
{
	"ITEM",
	"NAME",
	"TYPE",
	"TEXT",
	"CLAS",
	"COST",
	"FLAG",
	"SOND",
	"END",
};

char *raceflt_keywords[rflMax] = 
{
	"FSET",
	"RACE",
	"SHP1",
	"SHP2",
	"SHP3",
	"FLET",
	"EASY",
	"MEDM",
	"HARD",
	"END",
};

// ----------------
// GLOBAL VARIABLES
// ----------------

t_ik_spritepak	*spr_SMstars;
t_ik_spritepak	*spr_SMstars2;
t_ik_spritepak	*spr_SMplanet;
t_ik_spritepak	*spr_SMplanet2;
t_ik_spritepak	*spr_SMnebula;
t_ik_spritepak	*spr_SMraces;

t_gamesettings	settings;

t_race					races[16];
int32						num_races;

t_planettype		*platypes;
int32						num_platypes;

t_startype			*startypes;
int32						num_startypes;

t_itemtype			*itemtypes;
int32						num_itemtypes;

t_starsystem		*sm_stars;
int32						num_stars;

t_blackhole			*sm_holes;
int32						num_holes;

t_nebula				*sm_nebula;
int32						num_nebula;

t_racefleet			racefleets[16];
int32						num_racefleets;

uint8						*sm_nebulamap;
t_ik_image			*sm_nebulagfx;
t_ik_image			*sm_starfield;

t_fleet					sm_fleets[STARMAP_MAX_FLEETS];

int32						star_env[8][8];
//char						pltype_name[10][32];
int32						plgfx_type[256];
int32						num_plgfx;

char						planetnames[128][32];
int32						planetnametype[128];
int32						num_planetnames;
char						starnames[128][32];
int32						starnametype[128];
int32						num_starnames;
char						holenames[128][32];
int32						num_holenames;

int32						homesystem;

int32						kla_items[32];
int32						kla_numitems;

t_month				months[12] =
{
	{	"Jan", "January",		0,	31 },
	{	"Feb", "February",	31,	28 },
	{	"Mar", "March",			59,	31 },
	{	"Apr", "April",			90,	30 },
	{	"May", "May",				120,	31 },
	{	"Jun", "June",			151,	30 },
	{	"Jul", "July",			181,	31 },
	{	"Aug", "August",		212,	31 },
	{	"Sep", "September",	243,	30 },
	{	"Oct", "October",		273,	31 },
	{	"Nov", "November",	304,	30 },
	{	"Dec", "December",	334,	31 },
};

char	captnames[64][16];
int32 num_captnames;
char	shipnames[64][16];
int32 num_shipnames;

// ----------------
// LOCAL PROTOTYPES
// ----------------

void starmap_initsprites();
void starmap_deinitsprites();
void starmap_deinitterrain();
void starmap_initplanettypes();
void starmap_inititems();
void starmap_deinititems();
void starmap_initracefleets();
void starmap_deinitracefleets();

void starmap_initshipnames();

void starmap_createstars(int n);
void starmap_createholes(int n);
void starmap_createnebula(int n);
void starmap_createnebulamap();
void starmap_createfleets(int32 num);
void starmap_createcards(void);
void starmap_create_klakars(int32 num);

// ----------------
// GLOBAL FUNCTIONS
// ----------------

void starmap_init()
{
	starmap_initsprites();
	starmap_initplanettypes();
	starmap_inititems();
	starmap_initracefleets();
	starmap_initshipnames();
}

void starmap_deinit()
{
	starmap_deinitracefleets();
	starmap_deinititems();
	starmap_deinitsprites();
	starmap_deinitterrain();
}
/*
void waitsecs(int w, int l)
{
	Play_Sound(w, 15, 1);
	start_ik_timer(3, 1000);
	while (!must_quit && get_ik_timer(3)<l)
		ik_eventhandler();
	Stop_Sound(15);
}
*/
void starmap_create()
{
	int y = 0;
	prep_screen(); ik_print(screen, font_6x8, 8, y+=8, 0, "nebulas..."); ik_blit();
	ik_print_log("creating nebula...\n");
	starmap_createnebula(50+50*settings.dif_nebula);
	prep_screen(); ik_print(screen, font_6x8, 8, y+=8, 0, "stars..."); ik_blit();
	ik_print_log("creating stars...\n");
	starmap_createstars(NUM_STARSYSTEMS);
	prep_screen(); ik_print(screen, font_6x8, 8, y+=8, 0, "blackholes..."); ik_blit();
	ik_print_log("creating black holes...\n");
	starmap_createholes(4);
	prep_screen(); ik_print(screen, font_6x8, 8, y+=8, 0, "nebula gfx..."); ik_blit();
	ik_print_log("creating nebula graphics...\n");
	starmap_createnebulamap();
//	waitsecs(WAV_MUS_DEATH, 1);
#ifdef STARMAP_STEPBYSTEP
	interface_popup(font_6x8, 256,208,128,64,0,0,"pause", "nebula created", "ok");
#endif

	prep_screen(); ik_print(screen, font_6x8, 8, y+=8, 0, "discoveries..."); ik_blit();
	ik_print_log("creating discoveries...\n");
	starmap_createcards();
//	waitsecs(WAV_MUS_NEBULA, 1);
#ifdef STARMAP_STEPBYSTEP
	interface_popup(font_6x8, 256,208,128,64,0,0,"pause", "discoveries created", "ok");
#endif
	prep_screen(); ik_print(screen, font_6x8, 8, y+=8, 0, "enemies..."); ik_blit();
	ik_print_log("creating enemies...\n");
	starmap_createfleets(NUM_FLEETS);
//	waitsecs(WAV_MUS_COMBAT, 1);
#ifdef STARMAP_STEPBYSTEP
	interface_popup(font_6x8, 256,208,128,64,0,0,"pause", "enemies created", "ok");
#endif
	prep_screen(); ik_print(screen, font_6x8, 8, y+=8, 0, "klakar..."); ik_blit();
	ik_print_log("creating traders...\n");
	starmap_create_klakars(NUM_KLAITEMS);
//	waitsecs(WAV_KLAKAR, 1);

}

void player_init()
{
	int c;
	int s;

//	strcpy(player.captname, captnames[rand()%num_captnames]);
//	strcpy(player.shipname, shipnames[rand()%num_shipnames]);

	ik_print_log("initializing player...\n");

	memcpy(&shiptypes[0], &shiptypes[1+settings.dif_ship], sizeof(t_shiptype));
	strcpy(shiptypes[0].name, settings.shipname);

	memset(&player, 0, sizeof(t_player));
	strcpy(player.shipname, settings.shipname);
	strcpy(player.captname, settings.captname);

	player.system = homesystem;
	player.target = homesystem;
	player.explore = 0;
//	player.card = -1;
	
	player.x = sm_stars[player.system].x;
	player.y = sm_stars[player.system].y;

#ifndef STARMAP_DEBUGFOLD
	player.num_ships = 1;
#else
	player.num_ships = 2;
	player.ships[1] = 29;
#endif

#ifdef STARMAP_DEBUGALLIES
	player.num_ships = 2;
	player.ships[1] = 25;
#endif
	player.ships[0] = 0;

	player.stardate = 12;
	player.hypdate = 0;
	player.foldate = 0;
	player.sel_ship = 0;
	player.sel_ship_time = 0;

	player.num_items = 0; //17;
	for (c = 0; c < player.num_items; c++)
		player.items[c] = rand()%num_itemtypes;

#ifdef STARMAP_DEBUGDEVICES
	for (c = 0; c < num_itemtypes; c++)
	{
		if (itemtypes[c].type == item_device)
			player.items[player.num_items++] = c;
		if (!strcmp(itemtypes[c].name, "Continuum Renderer Array"))
			player.items[player.num_items++] = c;
		if (!strcmp(itemtypes[c].name, "Sardion Optimizer"))
			player.items[player.num_items++] = c;
	}
#endif

#ifdef	STARMAP_DEBUGSYSTEMS
	for (c = 0; c < num_itemtypes; c++)
	{/*
		if (!strcmp(itemtypes[c].name, "Hyperfoam Injectors"))
			player.items[player.num_items++] = c;
		if (!strcmp(itemtypes[c].name, "Multibot Repair Drone"))
			player.items[player.num_items++] = c;
		if (!strcmp(itemtypes[c].name, "Hyperwave Tele-Scrambler"))
			player.items[player.num_items++] = c;
		if (!strcmp(itemtypes[c].name, "Signature Projector"))
			player.items[player.num_items++] = c;*/
		if (!strcmp(itemtypes[c].name, "Hyperwave Filter Array"))
			player.items[player.num_items++] = c;
		if (!strcmp(itemtypes[c].name, "Continuum Renderer Array"))
			player.items[player.num_items++] = c;
	}
#endif

#ifdef STARMAP_DEBUGCLOAK
	for (c = 0; c < num_itemtypes; c++)
	if (itemtypes[c].type == item_system)
	{
		if (shipsystems[itemtypes[c].index].type == sys_misc && shipsystems[itemtypes[c].index].par[0]==1)
			shiptypes[0].system[shiptypes[0].num_systems++] = itemtypes[c].index;
	}
#endif

	for (c = 0; c < num_races; c++)
		races[c].met = 0;
	for (c = 0; c < num_shiptypes; c++)
	{
		shiptypes[c].hits = hulls[shiptypes[c].hull].hits*256;
		for (s = 0; s < shiptypes[c].num_systems; s++)
			shiptypes[c].sysdmg[s] = 0;
	}

	player.bonusdata = 0;

	hud.invslider = 0;
	hud.invselect = -1;
	hud.sysselect = -1;
	hud.sysslider = 0;

	allies_init();
}

void allies_init()
{
	int32 c;
	int32 f;
	int32 s;
	int32 r;
	char name[32];


	for (c = 0; c < num_shiptypes; c++)
	if (shiptypes[c].flag & 1)	// ally
	{
		f = shiptypes[c].flag;
		r = shiptypes[c].race;
		strcpy(name, shiptypes[c].name);
		for (s = 0; s < num_shiptypes; s++)
		if (!(shiptypes[s].flag & 1))
		{
			if (!strcmp(shiptypes[s].name, hulls[shiptypes[c].hull].name))
			{
				memcpy(&shiptypes[c], &shiptypes[s], sizeof(t_shiptype));
				strcpy(shiptypes[c].name, name);
				shiptypes[c].flag = f;
				shiptypes[c].race = r;
			}
		}
	}
}

// ----------------
// LOCAL FUNCTIONS
// ----------------

void starmap_initsprites()
{
	t_ik_image *pcx;	
	int n;

	spr_SMstars		= load_sprites("graphics/smstars.spr");
	spr_SMstars2	= load_sprites("graphics/smstars2.spr");
	spr_SMplanet	= load_sprites("graphics/smplanet.spr");
	spr_SMplanet2 = load_sprites("graphics/smplnet2.spr");
	spr_SMnebula	= load_sprites("graphics/smnebula.spr");
	spr_SMraces		= load_sprites("graphics/smraces.spr");
	
	pcx = NULL;

	if (!spr_SMstars)
	{
		pcx = ik_load_pcx("starmap.pcx", NULL);
		spr_SMstars = new_spritepak(9);
		for (n=0;n<8;n++)
		{
			spr_SMstars->spr[n] = get_sprite(pcx, n*32, 0, 32, 32);
		}
		spr_SMstars->spr[8] = get_sprite(pcx, 192, 64, 128, 128);
		save_sprites("graphics/smstars.spr", spr_SMstars);
	}

	if (!spr_SMplanet)
	{
		if (!pcx)
			pcx = ik_load_pcx("starmap.pcx", NULL);
		spr_SMplanet = new_spritepak(11);
		for (n=0;n<11;n++)
		{
			spr_SMplanet->spr[n] = get_sprite(pcx, n*16, 64, 16, 16);
		}

		save_sprites("graphics/smplanet.spr", spr_SMplanet);
	}

	if (!spr_SMnebula)
	{
		if (!pcx)
			pcx = ik_load_pcx("starmap.pcx", NULL);
		spr_SMnebula = new_spritepak(9);
		for (n=0;n<8;n++)
		{
			spr_SMnebula->spr[n] = get_sprite(pcx, n*32, 32, 32, 32);
		}
		spr_SMnebula->spr[8] = get_sprite(pcx, 0, 80, 128, 128);

		save_sprites("graphics/smnebula.spr", spr_SMnebula);
	}

	if (pcx)
		del_image(pcx);

	if (!spr_SMplanet2)
	{
		pcx = ik_load_pcx("planets.pcx", NULL);
		spr_SMplanet2 = new_spritepak(23);
		for (n=0;n<23;n++)
		{
			spr_SMplanet2->spr[n] = get_sprite(pcx, (n%5)*64, (n/5)*64, 64, 64);
		}

		save_sprites("graphics/smplnet2.spr", spr_SMplanet2);
		del_image(pcx);
	}
	if (!spr_SMstars2)
	{
		pcx = ik_load_pcx("suns.pcx", NULL);
		spr_SMstars2 = new_spritepak(8);
		for (n=0;n<8;n++)
		{
			spr_SMstars2->spr[n] = get_sprite(pcx, (n%4)*64, (n/4)*64, 64, 64);
		}

		save_sprites("graphics/smstars2.spr", spr_SMstars2);
		del_image(pcx);
	}
	if (!spr_SMraces)
	{
		pcx = ik_load_pcx("races.pcx", NULL);
		spr_SMraces = new_spritepak(15);
		for (n=0;n<15;n++)
		{
			spr_SMraces->spr[n] = get_sprite(pcx, (n%5)*64, (n/5)*64, 64, 64);
		}

		save_sprites("graphics/smraces.spr", spr_SMraces);
		del_image(pcx);
	}
}

void starmap_deinitsprites()
{
	free_spritepak(spr_SMstars);
	free_spritepak(spr_SMstars2);
	free_spritepak(spr_SMplanet);
	free_spritepak(spr_SMplanet2);
	free_spritepak(spr_SMnebula);
	free_spritepak(spr_SMraces);
}

void starmap_initplanettypes()
{
	FILE* ini;
	char s1[64], s2[256];
	char end;
	int num;
	int flag;
	int n;
	int com;
	int tv1;

	ini = myopen("gamedata/planets.ini", "rb");
	if (!ini)
		return;

	end = 0; num = 0; n = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		if (!strcmp(s1, planet_keywords[plkBegin]))
			num++;
		if (!strcmp(s1, star_keywords[stkBegin]))
			n++;
	}
	fclose(ini);

	platypes = (t_planettype*)calloc(num, sizeof(t_planettype));
	if (!platypes)
		return;
	num_platypes = num;

	startypes = (t_startype*)calloc(n, sizeof(t_startype));
	if (!startypes)
		return;
	num_startypes = n;

	ini = myopen("gamedata/planets.ini", "rb");
	end = 0; num = 0; flag = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		com = -1;
		for (n = 0; n < plkMax; n++)
			if (!strcmp(s1, planet_keywords[n]))
				com = n;

		if (flag == 0)
		{
			if (com == plkBegin)
			{
				flag = 1;
			}
		}
		else switch(com)
		{
			case plkName:
			strcpy(platypes[num].name, s2);
			break;

			case plkText:
			strcpy(platypes[num].text, s2);
			break;

			case plkBonus:
			sscanf(s2, "%d", &tv1);
			platypes[num].bonus = tv1;
			break;

			case plkEnd:
			num++; flag = 0;
			break;

			default: ;
		}

	}
	fclose(ini);

	ini = myopen("gamedata/planets.ini", "rb");
	end = 0; num = 0; flag = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		com = -1;
		for (n = 0; n < stkMax; n++)
			if (!strcmp(s1, star_keywords[n]))
				com = n;

		if (flag == 0)
		{
			if (com == stkBegin)
			{
				flag = 1;
			}
		}
		else switch(com)
		{
			case stkName:
			strcpy(startypes[num].name, s2);
			break;

			case stkText:
			strcpy(startypes[num].text, s2);
			break;

			case stkEnd:
			num++; flag = 0;
			break;

			default: ;
		}

	}
	fclose(ini);

	ini = myopen("gamedata/planets.ini", "rb");
	if (!ini)
		return;

	num_planetnames = 0; num_starnames = 0; num_holenames = 0; num_plgfx = 0;
	end = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		if (!strcmp(s1, "END"))
			flag = 0;
		else if (!strcmp(s1, "ENVIROS"))
		{	num=0; flag=2; }
		else if (!strcmp(s1, "PLANETGFX"))
		{	num=0; flag=3; }
		else if (!strcmp(s1, "PLANETNAMES"))
		{	num=0; flag=4; }
		else if (!strcmp(s1, "STARNAMES"))
		{	num=0; flag=5; }
		else if (!strcmp(s1, "HOLENAMES"))
		{	num=0; flag=6; }
		else
		{
			if (flag==2) // enviro
			{
				for (n=0; n<8; n++)
					star_env[num][n]=s1[n]-'0';
				num++;
			}
			if (flag==3) // planetgfx
			{
				for (n=0; n<num_platypes; n++)
					if (!strcmp(s1, platypes[n].name))
						plgfx_type[num_plgfx++]=n;
				num++;
			}
			if (flag==4) // planetnames
			{
				strcpy(planetnames[num], s2);
				for (n=0; n<num_platypes; n++)
					if (!strcmp(s1, platypes[n].name))
						planetnametype[num] = n;
				num++;
				num_planetnames = num;
			}
			if (flag==5) // starnames
			{
				strcpy(starnames[num], s2);
				for (n=0; n<8; n++)
					if (!strcmp(s1, startypes[n].name))
						starnametype[num] = n;
				num++;
				num_starnames = num;
			}
			if (flag==6) // holenames
			{
				strcpy(holenames[num], s2);
				num++;
				num_holenames = num;
			}
		}

	}
	fclose(ini);
}

void starmap_inititems()
{
	FILE* ini;
	char s1[64], s2[256];
	char end;
	int num;
	int flag;
	int n, com;
	int tv1;

	char itemtype[8][32];
	int32 num_types;

	ini = myopen("gamedata/items.ini", "rb");
	if (!ini)
		return;

	end = 0; num = 0; 
	flag = 0; num_types = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		if (!strcmp(s1, item_keywords[itkBegin]))
			num++;
		if (!strcmp(s1, "ITEMTYPES"))
		{	flag = 1; n=0; }
		else if (flag>0 && strcmp(s1, "END")==0)
			flag = 0;
		else 	if (flag == 1)
		{
			strcpy(itemtype[num_types], s1);
			num_types++;
		}

	}
	fclose(ini);

	itemtypes = (t_itemtype*)calloc(num, sizeof(t_itemtype));
	if (!itemtypes)
		return;
	num_itemtypes = num;

	ini = myopen("gamedata/items.ini", "rb");

	end = 0; num = 0; flag = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		com = -1;
		for (n = 0; n < itkMax; n++)
			if (!strcmp(s1, item_keywords[n]))
				com = n;

		if (flag == 0)
		{
			if (com == itkBegin)
			{
				itemtypes[num].flag = 0;
				itemtypes[num].index = -1;
				flag = 1;
			}
		}
		else switch(com)
		{
			case itkName:
			strcpy(itemtypes[num].name, s2);
			break;

			case itkType:
			for (n = 0; n < num_types; n++)
				if (!strcmp(s2, itemtype[n]))
					itemtypes[num].type = n;
			/*if (itemtypes[num].type == item_weapon)
			{
				for (n = 0; n < num_shipweapons; n++)
					if (!strcmp(shipweapons[n].name, itemtypes[num].name))
					{	itemtypes[num].index = n; shipweapons[n].item = num; }
			}*/
			if (itemtypes[num].type == item_weapon || itemtypes[num].type == item_system)
			{
				for (n = 0; n < num_shipsystems; n++)
					if (!strcmp(shipsystems[n].name, itemtypes[num].name))
					{	
						itemtypes[num].index = n; shipsystems[n].item = num; 
						if (shipsystems[n].type == sys_weapon)
							shipweapons[shipsystems[n].par[0]].item = num;
					}
			}
			break;

			case itkText:
			strcpy(itemtypes[num].text, s2);
			break;

			case itkClass:
			strcpy(itemtypes[num].clas, s2);
			break;

			case itkCost:
			sscanf(s2, "%d", &tv1);
			itemtypes[num].cost = tv1;
			break;

			case itkFlag:
			sscanf(s2, "%d", &tv1);
			itemtypes[num].flag = tv1;
			break;

			case itkSound:
			sscanf(s2, "%d", &tv1);
			if (!strcmp(itemtypes[num].clas, textstring[STR_INV_ARTIFACT]))
				itemtypes[num].sound = tv1 + SND_ARTIF*( (tv1>=0)-(tv1<0) );
			else
				itemtypes[num].sound = tv1 + SND_ITEMS*( (tv1>=0)-(tv1<0) );
			break;

			case itkEnd:
			num++; flag = 0;
			break;

			default: ;
		}

	}
	fclose(ini);
}

void starmap_deinititems()
{
	num_itemtypes = 0;
	free(itemtypes);
}

void starmap_deinitterrain()
{
	if (sm_stars)   free(sm_stars);
	num_stars = 0;
	if (sm_holes)   free(sm_holes);
	num_holes = 0;
	if (sm_nebula)  free(sm_nebula);
	num_nebula = 0;

	if (num_platypes)	free(platypes);
	num_platypes = 0;

	if (num_startypes)	free(startypes);
	num_startypes = 0;

	if (sm_nebulamap) free(sm_nebulamap);
	del_image(sm_nebulagfx);
	del_image(sm_starfield);
}

void starmap_createstars(int n)
{
	int32 c;
	int32 end;
	int32 r;
	int32 t=0;
	int32 h;
//	int32 x,y,x1,y1;

	num_stars = n;
	sm_stars = (t_starsystem *)calloc(n + 1, sizeof(t_starsystem));

	// generate star locations
	for (c = 0; c < num_stars; c++)
	{
		end = 0;
		while (!end && !must_quit)
		{
			end = 1;
			sm_stars[c].x = rand()%420 - 210;
			sm_stars[c].y = rand()%364 - 182;
			sm_stars[c].color = rand()%8;
			for (t = 0; t < c; t++)
			{
				r = (int32)sqrt(double(sm_stars[t].x-sm_stars[c].x)*(sm_stars[t].x-sm_stars[c].x) +
												 (sm_stars[t].y-sm_stars[c].y)*(sm_stars[t].y-sm_stars[c].y) );
				if (r < 64)
					end = 0;
			}
		}
		// create planet
		sm_stars[c].planet = star_env[sm_stars[c].color][rand()%8];
		sm_stars[c].novadate = 0;
		sm_stars[c].novatime = 0;
		end=0;
		while (!end && !must_quit)
		{
			ik_eventhandler();
			end=1;
			sm_stars[c].planetgfx = rand()%num_plgfx;
			if (plgfx_type[sm_stars[c].planetgfx]!=sm_stars[c].planet)
				end=0;
			r=1;
			for (t = 0; t < c; t++)
			{
				if (sm_stars[c].planet == sm_stars[t].planet)
					r++;
			}
			h=0;
			for (t = 0; t < num_plgfx; t++)
			{
				if (plgfx_type[t] == sm_stars[c].planet)
					h++;
			}
			if (h >= r)
			{
				for (t = 0; t < c; t++)
				{
					if (sm_stars[c].planetgfx == sm_stars[t].planetgfx)
						end=0;
				}
			}
		}
#ifndef STARMAP_DEBUGINFO
		sm_stars[c].explored = 0;
#else
		sm_stars[c].explored = 1;
#endif
		// pick names
		end = 0;
		while (!end && !must_quit)
		{
			ik_eventhandler();
			end = 0;
			while (!end)
			{
				r = rand()%num_starnames;
				if (starnametype[r] == sm_stars[c].color)
					end=1;
			}
			for (t = 0; t < c; t++)
				if (!strcmp(sm_stars[t].starname, starnames[r]))
					end = 0;
		}
		strcpy(sm_stars[c].starname, starnames[r]);
		end = 0;
		while (!end && !must_quit)
		{
			ik_eventhandler();
			end = 0;
			while (!end)
			{
				r = rand()%num_planetnames;
				if (planetnametype[r] == sm_stars[c].planet)
					end=1;
			}
			for (t = 0; t < c; t++)
				if (!strcmp(sm_stars[t].planetname, planetnames[r]))
					end = 0;
		}
		strcpy(sm_stars[c].planetname, planetnames[r]);
	}

	// find suitable home (starting) world
	h = -1;
	t = 640;
	for (c = 0; c < num_stars; c++)
	{
		if (!sm_nebulamap[((240-sm_stars[c].y)<<9)+(240+sm_stars[c].x)])
		{
			r = (int32)sqrt(double(sm_stars[c].x-0)*(sm_stars[c].x-0) +
											 (sm_stars[c].y-(-210))*(sm_stars[c].y-(-210)) );
			if (r < t)
			{
				t = r;
				h = c;
			}
		}
	}
	homesystem = h;
	if (h>-1)
	{
		sm_stars[h].color = 3;
		sm_stars[h].planet = 9;
		sm_stars[h].planetgfx = 21;
		sm_stars[h].explored = 2;
		strcpy(sm_stars[h].starname, textstring[STR_NAME_GLORY]);
		strcpy(sm_stars[h].planetname, textstring[STR_NAME_HOPE]);
	}

	// extra star for kawangi start
	sm_stars[num_stars].x = 0;
	sm_stars[num_stars].y = 255;
	sm_stars[num_stars].color = 0;
	sm_stars[num_stars].novadate = 0;
	sm_stars[num_stars].ds_x = SM_MAP_X + 240 + sm_stars[num_stars].x;
	sm_stars[num_stars].ds_y = SM_MAP_Y + 240 - sm_stars[num_stars].y;

	/*
	// draw nebular glow
	for (c = 0; c < num_stars; c++)
	{
		x = 240+sm_stars[c].x;
		y = 240-sm_stars[c].y;
		for (y1 = y-64; y1 < y+64; y1++)
		if (y1>=0 && y1<480)
			for (x1 = x-64; x1 < x+64; x1++)
			if (x1>=0 && x1<480)
			{
				if (sm_nebulamap[(y1<<9)+x1])
				{
					r = (int32)sqrt( (x1-x)*(x1-x) + (y1-y)*(y1-y) );
					if (r < 64)
					{
						t = (63-r)/8 + sm_stars[c].color*16;
						t = gfx_addbuffer[(t<<8) + ik_getpixel(sm_nebulagfx, x1, y1)];
						ik_putpixel(sm_nebulagfx, x1, y1, t);
					}
				}
			}
	}
	*/

	/*
	for (c = 0; c < num_stars; c++)
	{
		sm_stars[c].x <<= 8;
		sm_stars[c].y <<= 8;
	}*/

}

void starmap_createnebula(int n)
{
	int32 c, t, r;
	int32 end;
	int32 tries;

	int32 num_groups;

	num_groups = rand()%3 + 2;

	num_nebula = n;
	sm_nebula = (t_nebula *)calloc(num_nebula, sizeof(t_nebula));

	sm_nebulamap = (uint8 *)calloc(512*480, 1);
	sm_nebulagfx = ik_load_pcx("graphics/backgrnd.pcx", NULL); //new_image(480,480);
	sm_starfield = ik_load_pcx("graphics/backgrnd.pcx", NULL); //new_image(480,480);

	for (c = 0; c < num_groups; c++)
	{
		sm_nebula[c].x = rand()%360 + 60;
		sm_nebula[c].y = rand()%240 + 100;
		sm_nebula[c].sprite = rand()%7;
	}

	for (c = num_groups; c < num_nebula; c++)
	{
		end = 0;
		tries = 0;
		while (end != 1)
		{
			end = -1;
			tries++;
			sm_nebula[c].x = rand()%480;
			sm_nebula[c].y = rand()%360;
			sm_nebula[c].sprite = rand()%7;
			for (t = 0; t < c; t++)
			{
				r = (int32)sqrt(double(sm_nebula[t].x - sm_nebula[c].x)*(sm_nebula[t].x - sm_nebula[c].x)	+
													(sm_nebula[t].y - sm_nebula[c].y)*(sm_nebula[t].y - sm_nebula[c].y)	);
				if (r < 24)
				{
					if ((t%num_groups == c%num_groups) || (tries>=100))
					{	
						if (end == -1)
							end = 1;
						if (r < 12 && tries<100)
							end = 0;
					}
					if (t%num_groups != c%num_groups)
						end = 0;
				}
			}
		}
	}

}

void starmap_createnebulamap()
{
	int32 c, t, r;
	int32 x, y, x1, y1;
	uint8 *data;

	for (c = 0; c < num_nebula; c++)
	{
		x = sm_nebula[c].x; y = sm_nebula[c].y;
		for (y1 = sm_nebula[c].y - 32; y1 < sm_nebula[c].y + 31; y1++)
		if (y1>=0 && y1 < 480)
			for (x1 = sm_nebula[c].x - 32; x1 < sm_nebula[c].x + 31; x1++)
			if (x1>=0 && x1 < 480)
			{
				x = (x1+32-sm_nebula[c].x); y = (y1+32-sm_nebula[c].y);
				data = spr_SMnebula->spr[sm_nebula[c].sprite]->data;
				r = (data[(y>>1)*32+(x>>1)] +
						data[(y>>1)*32+(x>>1)+1]*(x&1) +
						data[(y>>1)*32+(x>>1)+32]*(y&1) +
						data[(y>>1)*32+(x>>1)+33]*(x&1)*(y&1)) * 4 / (1+(x&1)+(y&1)+(x&1)*(y&1));
				t = sm_nebulamap[(y1<<9)+x1];
				sm_nebulamap[(y1<<9)+x1] = MIN(t + r, 255);
			}
	}

	for (y = 0; y < 480; y++)
		for (x = 0; x < 480; x++)
		{
			t = sm_nebulamap[(y<<9)+x];
			if (t < 64)
				sm_nebulamap[(y<<9)+x] = 0;
			else
				sm_nebulamap[(y<<9)+x] = t - 63;
		}

	for (c = 0; c < num_holes; c++)
	{
		t = sm_nebulamap[((240-sm_holes[c].y)<<9)+(240+sm_holes[c].x)];
		if (t > 0)
			sm_holes[c].explored = 1;
		for (y1 = 240-sm_holes[c].y - 32; y1 < 240-sm_holes[c].y + 31; y1++)
		if (y1>=0 && y1 < 480)
			for (x1 = 240+sm_holes[c].x - 32; x1 < 240+sm_holes[c].x + 31; x1++)
			if (x1>=0 && x1 < 480)
			{
				x = (x1+32-(240+sm_holes[c].x)); y = (y1+32-(240-sm_holes[c].y));
				data = spr_SMnebula->spr[7]->data;
				r = (data[(y>>1)*32+(x>>1)] +
						data[(y>>1)*32+(x>>1)+1]*(x&1) +
						data[(y>>1)*32+(x>>1)+32]*(y&1) +
						data[(y>>1)*32+(x>>1)+33]*(x&1)*(y&1)) * 4 / (1+(x&1)+(y&1)+(x&1)*(y&1));
				t = sm_nebulamap[(y1<<9)+x1];
				if (r < 60)
					sm_nebulamap[(y1<<9)+x1] = (MAX(0,t-60+r) * r) / (15 * 4);
			}
	}

	starmap_createnebulagfx();
}

void starmap_createnebulagfx()
{
	int32 c, t;
	int32 x, y;

	ik_copybox(sm_starfield, sm_nebulagfx, 0, 0, 480, 480, 0, 0);

	for (y = 0; y < 480; y++)
		for (x = 0; x < 480; x++)
		{
			t = sm_nebulamap[(y<<9)+x];
			if (!t)
			{	
				c = 0; 
			}
			else
			{	
				if (t < 14)
					c = 9*16+t/2+2;
				else if (t < 70)
					c = 9*16+8-(t-14)/8;
				else
					c = 9*16+2;
				/*
				if (t < 14)
					c = 9*16+t/2+2;
				else if (t < 70)
					c = 9*16+8-(t-14)/8;
				else
					c = 9*16+2;
				*/
			}
			if (c)
				ik_putpixel(sm_nebulagfx, x, y, c);
		}

	// grid lines
	for (y = 0; y < 480; y++)
		for (x = 16; x < 480; x+=64)
		{
			ik_putpixel(sm_nebulagfx, x, y, gfx_addbuffer[(2<<8)+ik_getpixel(sm_nebulagfx, x, y)] );
		}
	for (x = 0; x < 480; x++)
		for (y = 16; y < 480; y+=64)
		{
			ik_putpixel(sm_nebulagfx, x, y, gfx_addbuffer[(2<<8)+ik_getpixel(sm_nebulagfx, x, y)] );
		}
}

void starmap_createholes(int32 n)
{
	int32 c;
	int32 end;
	int32 r;
	int32 t=0;

	num_holes = n;
	sm_holes = (t_blackhole *)calloc(num_holes, sizeof(t_blackhole));

	for (c = 0; c < num_holes; c++)
	{
		end = 0;
		while (!end)
		{
			end = 1;

			sm_holes[c].x = rand()%420 - 210;
			sm_holes[c].y = rand()%400 - 200;

			for (t = 0; t < num_stars; t++)
			{
				r = get_distance ( sm_stars[t].x - sm_holes[c].x,
													 sm_stars[t].y - sm_holes[c].y );
				if (r < 40 || (t == homesystem && r < 96))
					end = 0;
			}
			/*
			r = get_distance ( sm_stars[homesystem].x - sm_holes[c].x,
												 sm_stars[homesystem].y - sm_holes[c].y );
			if (r < 64)
				end = 0;
			*/
			for (t = 0; t < c; t++)
			{
				r = get_distance ( sm_holes[t].x - sm_holes[c].x,
													 sm_holes[t].y - sm_holes[c].y );
				if (r < 96)
					end = 0;
			}
		}
		sm_holes[c].explored = 0;
		sm_holes[c].size = 32;
		end = 0; // name
		while (!end && !must_quit)
		{
			ik_eventhandler();
			end = 1;
			r = rand()%num_holenames;
			for (t = 0; t < c; t++)
				if (!strcmp(sm_holes[t].name, holenames[r]))
					end = 0;
		}
		strcpy(sm_holes[c].name, holenames[r]);
	}
}

void starmap_createfleets(int32 num) // create enemy fleets
{
	int32 c;
	int32 s;
	int32 n;
	int32 kla;
	int32 end;
	int32 tries;
	int32 muk = 0, mukr = 0;
	int32 kaw = 0;


	int32 dif=settings.dif_enemies;	

	for (c = 0; c < num_stars; c++)
	{
		if (ecards[sm_stars[c].card].type == card_ally)
			if (shiptypes[ecards[sm_stars[c].card].parm].race == race_muktian)
				muk = shiptypes[ecards[sm_stars[c].card].parm].race;
		if (ecards[sm_stars[c].card].type == card_rareitem)
			if (itemtypes[ecards[sm_stars[c].card].parm].flag & device_collapser)
				kaw = race_kawangi;
//			!strcmp(ecards[sm_stars[c].card].name, "Limited Vacuum Collapser"))
	}

#ifdef STARMAP_KAWANGI
	kaw = race_kawangi;
#endif

	if (kaw)
		num++;
	mukr = race_muktian;
	kla = race_klakar;

	for (c = 0; c < STARMAP_MAX_FLEETS; c++)
	{
		sm_fleets[c].num_ships = 0;
		sm_fleets[c].explored = 0;
		sm_fleets[c].enroute = 0;
		sm_fleets[c].blowtime = 0;
	}

	for (c = 0; c < num; c++)
	{
		end = 0;
#ifndef STARMAP_DEBUGENEMIES
		sm_fleets[c].explored = 0;
#else
		sm_fleets[c].explored = 2;
#endif
		tries = 0;

		while (!end && !must_quit)
		{
			ik_eventhandler();
			end=1;
			tries++;

//			if (tries < 100)
//				prep_screen(); ik_print(screen, font_6x8, tries*6, 400+c*8, 0, "."); ik_blit();

			// race
			if (kla)
			{
				sm_fleets[c].race = kla;
			}

			else if (muk)
			{
				sm_fleets[c].race = muk;
			}
			else if (kaw)
			{
				sm_fleets[c].race = kaw;
				Play_SoundFX(WAV_DOT);
			}

			else
#ifndef STARMAP_DEBUGTANRU
				sm_fleets[c].race = enemies[rand()%num_enemies];
#else
				sm_fleets[c].race = race_tanru;
#endif

			// ships
			if (sm_fleets[c].race == kla || races[sm_fleets[c].race].fleet==-1)
			{
				sm_fleets[c].num_ships = 1;
				sm_fleets[c].ships[0] = 0;
				while ((shiptypes[sm_fleets[c].ships[0]].race != sm_fleets[c].race || 
								shiptypes[sm_fleets[c].ships[0]].flag == 1) &&
								!must_quit)
				{
					ik_eventhandler();
					sm_fleets[c].ships[0] = rand()%num_shiptypes;
				}
			}
			else
			{
				end = 0;
				while (!end)
				{
					sm_fleets[c].num_ships = 0;
					n = racefleets[races[sm_fleets[c].race].fleet].diff[dif][rand()%10];
					for (s=2; s>=0; s--)
					{
						end = racefleets[races[sm_fleets[c].race].fleet].fleets[n][s];
						while (end>0) // && sm_fleets[c].num_ships<8)
						{
							sm_fleets[c].ships[sm_fleets[c].num_ships] = racefleets[races[sm_fleets[c].race].fleet].stype[s];
							sm_fleets[c].num_ships++;
							end--;
						}
					}
					end = 1;
					// check for identical fleets and prevent if possible
					if (tries < 100)
					{
						for (s = 0; s < c; s++)
						if (sm_fleets[s].race == sm_fleets[c].race && sm_fleets[s].num_ships == sm_fleets[c].num_ships)
						{
							end = 0;
							for (n = 0; n < sm_fleets[c].num_ships; n++)
								if (sm_fleets[s].ships[n] != sm_fleets[c].ships[n])
									end = 1;
						}
					}
				}
				
			}

			// location

			if (sm_fleets[c].race == race_kawangi)
			{
				sm_fleets[c].system = num_stars;
				sm_fleets[c].explored = 0;
				starmap_kawangimove(c);
			}
			else
			{

				sm_fleets[c].system = rand()%num_stars;
				sm_fleets[c].target = sm_fleets[c].system;
				if (sm_fleets[c].system==homesystem)
					end = 0;
				if (tries<100)
				{
					if (sm_fleets[c].race == kla)
					{
						if (get_distance( sm_stars[sm_fleets[c].system].x - sm_stars[homesystem].x, sm_stars[sm_fleets[c].system].y - sm_stars[homesystem].y ) > 150)
							end = 0;
					}
					else
						if (get_distance( sm_stars[sm_fleets[c].system].x - sm_stars[homesystem].x, sm_stars[sm_fleets[c].system].y - sm_stars[homesystem].y ) < 150)
							end = 0;

					if (!strcmp(ecards[sm_stars[sm_fleets[c].system].card].name, textstring[STR_EVENT_NOVA]))
						end = 0;

				}
				n = 0;
				for (s=0; s<c; s++)
				{
					if (sm_fleets[s].system==sm_fleets[c].system)
						end = 0;
					if (sm_fleets[s].race==sm_fleets[c].race)
						n++;
				}
#ifdef STARMAP_DEBUGTANRU
				if (n > 1) n = 1;
#endif

				if (n > 1 || (n==1 && sm_fleets[c].race == race_muktian))
					end = 0;

				if (ecards[sm_stars[sm_fleets[s].system].card].type == card_ally)
					end = 0;

				else if (!strcmp(ecards[sm_stars[sm_fleets[s].system].card].name, textstring[STR_EVENT_HULK]))
					end = 0;
			}
		}
/*
		prep_screen(); 
//		ik_drawbox(screen, 0, 360+c*8, 128, 368+c*8, 0);
		ik_print(screen, font_6x8, 0, 360+c*16, 0, "%s %d", races[sm_fleets[c].race].name, sm_fleets[c].num_ships); 
		for (n = 0; n < sm_fleets[c].num_ships; n++)
		{
			ik_drsprite(screen, 128+n*16, 364+c*16, 0, 16, hulls[shiptypes[sm_fleets[c].ships[n]].hull].sprite, 0);
		}
		ik_blit();
*/

		if (sm_fleets[c].race == race_muktian && muk > 0)
			muk = 0;
		if (sm_fleets[c].race == race_kawangi && kaw > 0)
			kaw = 0;

		if (sm_fleets[c].race == race_klakar && kla > 0)
			kla = 0;
	}


	for (n = 0; n < num_stars; n++)
	{
		if (!strcmp(ecards[sm_stars[n].card].name, textstring[STR_EVENT_HULK]))
		{
			c = num;
			sm_fleets[c].explored = 0;
			sm_fleets[c].num_ships = 1;
			sm_fleets[c].race = race_unknown;
			sm_fleets[c].system = n;
			sm_fleets[c].enroute = 0;
			sm_fleets[c].target = n;
			end = 0;
			while (!end && !must_quit)
			{
				ik_eventhandler();
				end = 1;
				sm_fleets[c].ships[0] = rand()%num_shiptypes;
				if (shiptypes[sm_fleets[c].ships[0]].race != race_unknown)
					end = 0;
			}
		}
	}


}

void starmap_createcards(void)
{
	int32 c, s;
	int32 i;
	int32 t=0;
	int32 tries;
	int32 end;
#ifdef STARMAP_STEPBYSTEP
	char texty[256];
#endif

	int32 nit = 0, nri = 0, nev = 0;

	for (c = 0; c < num_stars; c++)
		sm_stars[c].card = -1;

	for (c = 0; c < NUM_LIFEFORMS; c++)
	{
		end = 0;
		while (!end)
		{
			end = 1;
			s = rand()%num_stars;
			if (s == homesystem)
				end = 0;
			if (sm_stars[s].planet == 0 || sm_stars[s].planet > 5)
				end = 0;
			if (sm_stars[s].card > -1)
				end = 0;
		}
		end = 0;
		while (!end)
		{
			end = 1;
			i = rand()%num_ecards;
			if (ecards[i].type != card_lifeform)
				end = 0;
			for (t = 0; t < num_stars; t++)
			if (t != homesystem && t != s)
				if (sm_stars[t].card == i)
					end = 0;
		}
		sm_stars[s].card = i;
	}

#ifdef STARMAP_STEPBYSTEP
	interface_popup(font_6x8, 256,208,128,64,0,0,"pause", "lifeforms created", "ok");
#endif

	for (c = 0; c < NUM_ALLIES; c++)
	{
		end = 0;
		while (!end)
		{
			end = 1;
			i = rand()%num_ecards;
			if (ecards[i].type != card_ally)
				end = 0;
			else
			{
				for (t = 0; t < num_stars; t++)
				if (t != homesystem && sm_stars[t].card > -1)
				{
					if (sm_stars[t].card == i)
						end = 0;
					if (ecards[sm_stars[t].card].type == card_ally)
						if (shiptypes[ecards[sm_stars[t].card].parm].race == shiptypes[ecards[i].parm].race)
							end = 0;
				}
			}
		}

#ifdef STARMAP_STEPBYSTEP
	sprintf(texty, "ally %d 50%%", c);
	interface_popup(font_6x8, 256,208,128,64,0,0,"pause", texty, "ok");
#endif

		end = 0; tries = 0;
		while (!end)
		{
			end = 1; tries++;
			s = rand()%num_stars;
			if (s == homesystem)
				end = 0;
			if (sm_stars[s].card > -1)
				end = 0;
			if (tries < 100)
			{
				if (sm_stars[s].planet == 0 || sm_stars[s].planet > 6)
					end = 0;
				if (shiptypes[ecards[i].parm].race == 1)
				{
					if (get_distance( sm_stars[s].x-sm_stars[homesystem].x, sm_stars[s].y-sm_stars[homesystem].y ) > 200)
						end = 0;
				}
			}
		}
		sm_stars[s].card = i;

#ifdef STARMAP_STEPBYSTEP
	sprintf(texty, "ally %d 100%%", c);
	interface_popup(font_6x8, 256,208,128,64,0,0,"pause", texty, "ok");
#endif


	}

#ifdef STARMAP_STEPBYSTEP
	interface_popup(font_6x8, 256,208,128,64,0,0,"pause", "allies created", "ok");
#endif

	for (c = 0; c < num_stars; c++)
	if (c != homesystem && sm_stars[c].card == -1)
	{
		end = 0;
		while (!end)
		{
			end = 1;
			sm_stars[c].card = 1+rand()%(num_ecards-1);
			t = ecards[sm_stars[c].card].type;
			if (t == card_ally || t == card_lifeform)
				end = 0;

			if (t == card_item && nit >= NUM_ITEMS)					end = 0;
			if (t == card_rareitem && nri >= NUM_RAREITEMS) end = 0;
			if (t == card_event && nev >= NUM_EVENTS)				end = 0;

			for (s = 0; s < c ; s++)
			if (s != homesystem)
				if (sm_stars[s].card == sm_stars[c].card)
					end = 0;

			if (end)
			{
				if (t == card_item)						nit++;
				else if (t == card_rareitem)  nri++;
				else if (t == card_event)			nev++;


				if (!strcmp(ecards[sm_stars[c].card].name, textstring[STR_EVENT_GIANT]))
				{
					sm_stars[c].planet = 8;
					end = 0;
					while (!end)
					{
						end = rand()%num_planetnames;
						if (planetnametype[end] == sm_stars[c].planet)
						{	strcpy(sm_stars[c].planetname, planetnames[end]); end = 1; }
						else
							end=0;
					}
					end = 0;
					while (!end)
					{
						end = 1;
						sm_stars[c].planetgfx = rand()%num_plgfx;
						if (plgfx_type[sm_stars[c].planetgfx]!=sm_stars[c].planet)
							end=0;
					}
				}

				end = 1;
			}
		}
	}
}

void starmap_create_klakars(int32 num)
{
	int32 c;
	int32 s;
	int32 t=0;
	int32 n;
	int32 end;

	kla_numitems = num;

	c = 0;
	for (n = 0; n < NUM_KLAWEP; n++)
	{
		end = 0;
		while (!end)
		{
			end = 1;
			kla_items[c] = rand()%num_itemtypes;
			if (itemtypes[kla_items[c]].flag & 1)
				end = 0;
			if (itemtypes[kla_items[c]].type != item_weapon)
				end = 0;
			for (t = 0; t < c; t++)
				if (kla_items[t] == kla_items[c])
					end = 0;
			for (s = 0; s < num_stars; s++)
			if (s != homesystem && sm_stars[s].card > -1)
			{
				t = ecards[sm_stars[s].card].type;
				if (t == card_item || t == card_rareitem || t == card_lifeform)
				{
					if (ecards[sm_stars[s].card].parm == kla_items[c])
						end = 0;
				}
			}

		}
		c++;
	}

	for (n = 0; n < NUM_KLASYS; n++)
	{
		end = 0;
		while (!end)
		{
			end = 1;
			kla_items[c] = rand()%num_itemtypes;
			if (itemtypes[kla_items[c]].flag & 1)
				end = 0;
			if (itemtypes[kla_items[c]].type != item_system)
				end = 0;
			for (t = 0; t < c; t++)
				if (kla_items[t] == kla_items[c])
					end = 0;
			for (s = 0; s < num_stars; s++)
			if (s != homesystem && sm_stars[s].card > -1)
			{
				t = ecards[sm_stars[s].card].type;
				if (t == card_item || t == card_rareitem || t == card_lifeform)
				{
					if (ecards[sm_stars[s].card].parm == kla_items[c])
						end = 0;
				}
			}

		}
		c++;
	}

	for (; c < kla_numitems; c++)
	{
		end = 0;
		while (!end)
		{
			end = 1;
			kla_items[c] = rand()%num_itemtypes;
			if (itemtypes[kla_items[c]].flag & 1)
				end = 0;
			for (t = 0; t < c; t++)
				if (kla_items[t] == kla_items[c])
					end = 0;
			for (s = 0; s < num_stars; s++)
			if (s != homesystem && sm_stars[s].card > -1)
			{
				t = ecards[sm_stars[s].card].type;
				if (t == card_item || t == card_rareitem || t == card_lifeform)
				{
					if (ecards[sm_stars[s].card].parm == kla_items[c])
						end = 0;
				}
			}

		}
	}

}

void starmap_initshipnames()
{
	FILE* ini;
	char s1[64];
	char end;
	int num;
	int flag;
	int n;

	ini = myopen("gamedata/names.ini", "rb");
	if (!ini)
		return;

	num_captnames = 0;
	num_shipnames = 0;
	end = 0; num = 0; 
	flag = 0; 
	while (!end)
	{
		end = read_line1(ini, s1);
		if (!strcmp(s1, item_keywords[itkBegin]))
			num++;
		if (!strcmp(s1, "CAPTNAMES"))
		{	flag = 1; n=0; }
		else if (!strcmp(s1, "SHIPNAMES"))
		{ flag = 2; n=0; }
		else if (flag>0 && strcmp(s1, "END")==0)
			flag = 0;
		else if (flag == 1)
		{
			strcpy(captnames[num_captnames++], s1);
		}
		else if (flag == 2)
		{
			strcpy(shipnames[num_shipnames++], s1);
		}

	}
	fclose(ini);
	
}

void starmap_initracefleets()
{
	FILE* ini;
	char s1[64], s2[256];
	char end;
	int num;
	int flag;
	int n, com;
	int tv1, tv2, tv3;

	ini = myopen("gamedata/fleets.ini", "rb");
	if (!ini)
		return;

	end = 0; num = 0; 
	flag = 0; 
	while (!end)
	{
		end = read_line(ini, s1, s2);
		if (!strcmp(s1, item_keywords[rflBegin]))
			num++;
	}
	fclose(ini);
/*
	racefleets = (t_racefleet*)calloc(num, sizeof(t_racefleet));
	if (!racefleets)
		return;
	*/
	num_racefleets = num;

	ini = myopen("gamedata/fleets.ini", "rb");

	end = 0; num = 0; flag = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		com = -1;
		for (n = 0; n < rflMax; n++)
			if (!strcmp(s1, raceflt_keywords[n]))
				com = n;

		if (flag == 0)
		{
			if (com == rflBegin)
			{
				racefleets[num].race=-1;
				racefleets[num].num_fleets=0;
				flag = 1;
			}
		}
		else switch(com)
		{
			case rflRace:
			for (n = 0; n < num_races; n++)
				if (!strcmp(s2, races[n].name))
				{	racefleets[num].race = n; races[n].fleet = num; }
			break;

			case rflShip1:
			case rflShip2:
			case rflShip3:
			for (n = 0; n < num_shiptypes; n++)
				if (!strcmp(s2, shiptypes[n].name))
					racefleets[num].stype[com-rflShip1] = n;
			break;

			case rflFleet:
			sscanf(s2, "%d %d %d", &tv1, &tv2, &tv3);
			racefleets[num].fleets[racefleets[num].num_fleets][0]=tv1;
			racefleets[num].fleets[racefleets[num].num_fleets][1]=tv2;
			racefleets[num].fleets[racefleets[num].num_fleets][2]=tv3;
			racefleets[num].num_fleets++;
			break;

			case rflEasy:
			case rflMedium:
			case rflHard:
			for (n = 0; n < 10; n++)
			{
				racefleets[num].diff[com-rflEasy][n]=s2[n]-'0';
			}
			break;

			case rflEnd:
			num++; flag = 0;
			break;

			default: ;
		}

	}
	fclose(ini);
}

void starmap_deinitracefleets()
{
	num_racefleets = 0;
//	free(racefleets);
}
// ----------------
//     INCLUDES
// ----------------

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <math.h>

#include "typedefs.h"
#include "is_fileio.h"

#include "textstr.h"
#include "iface_globals.h"
#include "gfx.h"
#include "snd.h"
#include "interface.h"
#include "combat.h"
#include "cards.h"
#include "endgame.h"

#include "starmap.h"

// ----------------
//		CONSTANTS
// ----------------

// ----------------
// GLOBAL VARIABLES
// ----------------

// ----------------
// LOCAL VARIABLES
// ----------------

// ----------------
// LOCAL PROTOTYPES
// ----------------

// ----------------
// GLOBAL FUNCTIONS
// ----------------

// regular inventory management

void starmap_installitem(int32 n)
{
	int it, sy;
	int ityp, styp;
	int s;
	int d = 0;
	int i;

	hud.invselect = -1;

	it = player.items[n];
	ityp = itemtypes[it].type;
	if (ityp == item_system)
	{
		sy = itemtypes[player.items[n]].index;
		styp = shipsystems[sy].type;
	
		if (styp != sys_misc)
		{	// misc systems don't cancel out
			for (s = 0; s < shiptypes[0].num_systems; s++)
			if (shipsystems[shiptypes[0].system[s]].type == styp)
				starmap_uninstallsystem(s, 0);
		}
		if (shiptypes[0].num_systems < 10)
		{
			d = (int32)(sqrt((double)itemtypes[it].cost)*.2);
			if (d < 1) d = 1;
			shiptypes[0].sysdmg[shiptypes[0].num_systems] = 0;
			shiptypes[0].system[shiptypes[0].num_systems] = sy;
			shiptypes[0].num_systems++;
			starmap_removeitem(n);
		}
	}
	else if (ityp == item_weapon)
	{
		s = select_weaponpoint();
		if (s > -1)
		{
			if (shiptypes[0].system[s] > -1)
			{	// place old weapon in the inventory
				i = shipsystems[shiptypes[0].system[s]].item;
				if (i > -1)
					starmap_additem(i, 0);
			}
			shiptypes[0].system[s] = itemtypes[it].index;
			shiptypes[0].sysdmg[s] = 0;
			starmap_removeitem(n);
		}
	}

	sort_shiptype_systems(0);
	starmap_advancedays(d);
//	shiptypes[0].speed = (shipsystems[shiptypes[0].thrust].par[0] * 32) / hulls[shiptypes[0].hull].mass;
}

void starmap_uninstallsystem(int32 n, int32 brk)
{
	int c, i;

	i = shipsystems[shiptypes[0].system[n]].item;
	if (i>-1)
	{
		starmap_additem(shipsystems[shiptypes[0].system[n]].item, brk);

		if (shipsystems[shiptypes[0].system[n]].type == sys_weapon)
		{
			shiptypes[0].system[n] = 0;
			shiptypes[0].sysdmg[n] = 0;
		}
		else
		{
			for (c = n; c < shiptypes[0].num_systems-1; c++)
			{
				shiptypes[0].system[c] = shiptypes[0].system[c+1];
				shiptypes[0].sysdmg[c] = shiptypes[0].sysdmg[c+1];
			}
			shiptypes[0].num_systems--;
			shiptypes[0].system[shiptypes[0].num_systems] = -1;
			shiptypes[0].sysdmg[shiptypes[0].num_systems] = 0;
		}
	}
	sort_shiptype_systems(0);
}

void starmap_destroysystem(int32 n)
{
	int c, i;

	i = shipsystems[shiptypes[0].system[n]].item;
	if (i>-1)
	{
		if (shipsystems[shiptypes[0].system[n]].type == sys_weapon)
		{
			shiptypes[0].system[n] = 0;
			shiptypes[0].sysdmg[n] = 0;
		}
		else
		{
			for (c = n; c < shiptypes[0].num_systems-1; c++)
			{
				shiptypes[0].system[c] = shiptypes[0].system[c+1];
				shiptypes[0].sysdmg[c] = shiptypes[0].sysdmg[c+1];
			}
			shiptypes[0].num_systems--;
			shiptypes[0].system[shiptypes[0].num_systems] = -1;
			shiptypes[0].sysdmg[shiptypes[0].num_systems] = 0;
		}
	}
	sort_shiptype_systems(0);
}

void starmap_additem(int32 it, int32 brk)
{
	player.items[player.num_items]=it;
	player.itemflags[player.num_items]=brk;

	player.num_items++;
}

void starmap_removeitem(int32 n)
{
	int c;

	for (c = n; c < player.num_items-1; c++)
	{
		player.items[c] = player.items[c+1];
		player.itemflags[c] = player.itemflags[c+1];
	}
	player.items[player.num_items] = -1;
	player.itemflags[player.num_items] = 0;
	player.num_items--;
}

int32 select_weaponpoint()
{
	int32 mc, c;
	int32 end = 0;
	int32 p = -1;
	int32 n;
	int32 bx = 240, by = 152;
	int32 mx, my;
	int32 upd = 1;
	int32 t=0;

	for (n = 0; n < hulls[shiptypes[0].hull].numh; n++)
	if (hulls[shiptypes[0].hull].hardpts[n].type == hdpWeapon &&
			shiptypes[0].sysdmg[n]==0 &&
			shipsystems[shiptypes[0].system[n]].item == -1)
	{
		p = n;
	}

	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		mc = ik_mclick() & 3;	
		c = ik_inkey();
		mx = ik_mouse_x - bx; my = ik_mouse_y - by;

		if (mc == 1)
		{
			upd = 1;
			if (my > 24 && my < 152)
			{
				//p = -1;
				for (n = 0; n < hulls[shiptypes[0].hull].numh; n++)
				if (hulls[shiptypes[0].hull].hardpts[n].type == hdpWeapon &&
						shiptypes[0].sysdmg[n]==0)
				{
					if (abs(16+hulls[shiptypes[0].hull].hardpts[n].x*2 - mx) < 6 &&
							abs(24+hulls[shiptypes[0].hull].hardpts[n].y*2 - my) < 6)
					{
						p = n;
						Play_SoundFX(WAV_INSTALL2, get_ik_timer(0));
					}
				}
			}
			else if (my > 152 && my < 168)
			{
				if (mx > 16 && mx < 64)
				{	end = 1; p = -1; Play_SoundFX(WAV_DECLINE, get_ik_timer(0)); }
				else if (mx > 96 && mx < 144)
				{ end = 1; Play_SoundFX(WAV_ACCEPT, get_ik_timer(0)); }
			}
		}

		if (c == 13 && p > -1)
		{	end = 1; Play_SoundFX(WAV_ACCEPT); }

		c = t; t = get_ik_timer(2);
		if (t != c)
		{ upd = 1;	}

		if (upd)
		{
			upd=0;
			prep_screen();
			interface_drawborder(screen,
													 bx, by, bx+160, by+176,
													 1, STARMAP_INTERFACE_COLOR, textstring[STR_INV_POINT]);
			interface_drawbutton(screen, bx+16, by+176-24,48, STARMAP_INTERFACE_COLOR, textstring[STR_CANCEL]);
			interface_drawbutton(screen, bx+160-64, by+176-24,48, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);

			ik_dsprite(screen, bx+16, by+24, hulls[shiptypes[0].hull].silu, 2+(STARMAP_INTERFACE_COLOR<<8));
			for (n = 0; n < hulls[shiptypes[0].hull].numh; n++)
			if (hulls[shiptypes[0].hull].hardpts[n].type == hdpWeapon)
			{
				ik_dsprite(screen, bx+8+hulls[shiptypes[0].hull].hardpts[n].x*2, 
													 by+16+hulls[shiptypes[0].hull].hardpts[n].y*2, 
													 spr_IFsystem->spr[1], 2+((shiptypes[0].sysdmg[n]==0)<<8));
				if (p==n)
				ik_dsprite(screen, bx+8+hulls[shiptypes[0].hull].hardpts[n].x*2, 
													 by+16+hulls[shiptypes[0].hull].hardpts[n].y*2, 
													 spr_IFsystem->spr[0], 0);
			}

			ik_blit();
		}
	}

	return p;
}

// klakar trade

void klakar_trade()
{
	int32 mc, c;
	int32 end = 0;
	int32 bx = 16, by = SM_INV_Y-56, h = 168;
	int32 plx = 0, klx = 212, iny = 36, inw = 180; // klx = 160; inw = 128
	int32 mx, my;
	int32 upd = 1;
	int32 t, y;
	int32 i, f;
	int32 clt;
	int32 sel1 = 0, sel2 = 0;
	int32 scr1=0, scr2=0;
	int32 co;
	int32 ploog[64];
	int32 num_ploog;
	t_ik_font *tradefont;
//	t_ik_sprite *bg;

	if (settings.opt_mucrontext & 2)
	{
		tradefont = font_6x8;
		klx = 212; inw = 180;
	}
	else
	{
		tradefont = font_4x8;
		klx = 160; inw = 128;
	}

	halfbritescreen();

	num_ploog=0;
	for (y = 0; y < player.num_items + shiptypes[0].num_systems; y++)
	{
		if (y < player.num_items)	// inventory
		{	
			if (!(itemtypes[player.items[y]].flag & 2) && !player.itemflags[y])
				ploog[num_ploog++]=y;
		}
		else		// system
		{	
			i = shipsystems[shiptypes[0].system[y-player.num_items]].item;
			if (i > -1 && !shiptypes[0].sysdmg[y-player.num_items] && !(itemtypes[i].flag & 2))
				ploog[num_ploog++]=y;
		}
	}

	if (!num_ploog)
		return;


	t = -1;
	for (c = 0; c < STARMAP_MAX_FLEETS; c++)
	if (sm_fleets[c].num_ships > 0)
		if (sm_fleets[c].race == race_klakar)
		{
			sm_fleets[c].system = player.system;
			t = c;
		}

	if (t==-1)
		return;


	Play_Sound(WAV_TRADE, 15, 1, 75);

	clt = 0;

	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		t = get_ik_timer(0);
		mc = ik_mclick();	
		c = ik_inkey();
		mx = ik_mouse_x - bx; my = ik_mouse_y - by;

		c = t; t = get_ik_timer(2);
		if (t != c)
		{ upd = 1;	}

		if (mc == 1)
		{
			if (my > iny+4 && my < iny+100) // select item or trade
			{
				c = (my - iny - 4) / 8;
				if (mx > klx+16 && mx < klx+inw+4 && c+scr1 < kla_numitems)
				{	
					y = sel1;
					sel1 = c + scr1; 
					Play_SoundFX(WAV_SELECT, t);
					if (y==sel1 && t-clt<20)	// double click for info
					{
						Play_SoundFX(WAV_INFO);
						interface_popup(font_6x8, bx+64, by+24, 192, 96, STARMAP_INTERFACE_COLOR, 0, 
														itemtypes[kla_items[sel1]].name, itemtypes[kla_items[sel1]].text);
						upd=1;
					}
				}
				if (mx > klx+inw+3 && mx < klx+inw+12 && kla_numitems > 12)
				{
					Play_SoundFX(WAV_SLIDER, t);
					if (my < iny+12)
						scr1 = MAX(0,scr1-1);
					else if (my > iny+92)
						scr1 = MIN(kla_numitems-12, scr1+1);
					else
						scr1 = MIN(((my - (iny+12))*(kla_numitems-12)+40) / 80, kla_numitems-12);
				}
				if (mx > plx+16 && mx < plx+inw+4 && c+scr2 < num_ploog)
				{	
					y = sel2;
					sel2 = c + scr2;
					Play_SoundFX(WAV_SELECT, t);
					if (y==sel2 && t-clt<20)	// double click for info
					{
						Play_SoundFX(WAV_INFO);
						if (ploog[sel2] < player.num_items) // inventory
							c = player.items[ploog[sel2]]; 
						else	// system
							c = shipsystems[shiptypes[0].system[ploog[sel2]-player.num_items]].item; 

						interface_popup(font_6x8, bx+64, by+24, 192, 96, STARMAP_INTERFACE_COLOR, 0, 
														itemtypes[c].name, itemtypes[c].text);
						upd=1;
					}
				}
				if (mx > plx+inw+3 && mx < plx+inw+12 && num_ploog > 12)
				{
					Play_SoundFX(WAV_SLIDER, t);
					if (my < iny+12)
						scr2 = MAX(0,scr2-1);
					else if (my > iny+92)
						scr2 = MIN(num_ploog-12, scr2+1);
					else
						scr2 = MIN(((my - (iny+12))*(num_ploog-12)+40) / 80, num_ploog-12);
				}

				if (mx > klx-12 && mx < klx+12) // trade (148 - 172)
				{
					Play_SoundFX(WAV_CASH, t);
					f = kla_items[sel1]; c = -1;
					if (ploog[sel2] < player.num_items) // inventory
					{
						kla_items[sel1] = player.items[ploog[sel2]]; 
						player.items[ploog[sel2]] = f;
						c = ploog[sel2];
						if (itemtypes[f].type != item_weapon && itemtypes[f].type != item_system)
							c = -1;
					}
					else	// system
					{
						t = ploog[sel2]-player.num_items;
						i = shipsystems[shiptypes[0].system[t]].item;
						kla_items[sel1] = shipsystems[shiptypes[0].system[t]].item;
						if (itemtypes[f].type == itemtypes[i].type)
						{
							if (itemtypes[f].type == item_weapon)	// swap weapons
							{
								shiptypes[0].system[t] = itemtypes[f].index;
								f = -1;
							}
							else	// other systems
							{
								if (shipsystems[itemtypes[i].index].type == shipsystems[itemtypes[f].index].type || shipsystems[itemtypes[f].index].type == sys_misc)
								{
									starmap_destroysystem(t);
									starmap_additem(f, 0);
									starmap_installitem(player.num_items-1);									
									f = -1;
								}
							}
						}
						if (f > -1)
						{
							starmap_destroysystem(t);
							starmap_additem(f, 0);
							c = player.num_items-1;
						}
					}
					if (itemtypes[f].type != item_weapon && itemtypes[f].type != item_system)
						c = -1;
					if (c > -1)
					{
						for (i = 0; i < shiptypes[0].num_systems; i++)
							if (c > -1)
								if (shipsystems[shiptypes[0].system[i]].type == shipsystems[itemtypes[player.items[c]].index].type 
										&& shipsystems[itemtypes[player.items[c]].index].type != sys_misc  
										&& itemtypes[player.items[c]].type != item_weapon)
									c = -1; 
						if (c > -1) // && itemtypes[player.items[c]].type != item_weapon)
						{
							if (itemtypes[player.items[c]].type == item_weapon)
							{
								for (i = 0; i < shiptypes[0].num_systems; i++)
									if (c > -1 && shipsystems[shiptypes[0].system[i]].type == sys_weapon && shipsystems[shiptypes[0].system[i]].item == -1)	// empty slot
									{
										shiptypes[0].system[i] = itemtypes[player.items[c]].index;
										starmap_removeitem(c);
										c = -1;
									}
							}
							else
								starmap_installitem(c);
						}
					}
					
					sort_shiptype_systems(0);
					upd = 1;
				}

				upd = 1;
			}
			if (my > h-24 && my < h-8) // button row
			{
				if (mx > klx+16 && mx < klx+48) // info
				{
					Play_SoundFX(WAV_INFO);
					interface_popup(font_6x8, bx+64, by+24, 192, 96, STARMAP_INTERFACE_COLOR, 0, 
													itemtypes[kla_items[sel1]].name, itemtypes[kla_items[sel1]].text);
					upd=1;
				}
				if (mx > plx+16 && mx < plx+48) // info
				{
					Play_SoundFX(WAV_INFO);
					if (ploog[sel2] < player.num_items) // inventory
						c = player.items[ploog[sel2]]; 
					else	// system
						c = shipsystems[shiptypes[0].system[ploog[sel2]-player.num_items]].item; 

					interface_popup(font_6x8, bx+64, by+24, 192, 96, STARMAP_INTERFACE_COLOR, 0, 
													itemtypes[c].name, itemtypes[c].text);
					upd=1;
				}
				if (mx > klx*2-80 && mx < klx*2-16) // done  (240 - 304)
				{	end = 1; Play_SoundFX(WAV_DOT, get_ik_timer(0)); }
			}
			clt = t;
		}

		if (upd)
		{
			upd = 0;

			num_ploog=0;
			for (y = 0; y < player.num_items + shiptypes[0].num_systems; y++)
			{
				if (y < player.num_items)	// inventory
				{	
					if (!(itemtypes[player.items[y]].flag & 2) && !player.itemflags[y])
						ploog[num_ploog++]=y;
				}
				else		// system
				{	
					if (shipsystems[shiptypes[0].system[y-player.num_items]].item > -1 && !shiptypes[0].sysdmg[y-player.num_items])
						ploog[num_ploog++]=y;
				}
			}

			prep_screen();

			interface_drawborder(screen, bx, by, bx + klx*2, by+h,
													 1, STARMAP_INTERFACE_COLOR, textstring[STR_TRADE_TITLE]);

			// player side
			ik_print(screen, font_6x8, bx+plx+16, by+24, STARMAP_INTERFACE_COLOR, "%s", player.shipname);
			interface_thinborder(screen, bx+plx+16, by+iny, bx+plx+inw+16, by+iny+104, STARMAP_INTERFACE_COLOR, STARMAP_INTERFACE_COLOR*16+1);	// +144

			for (c = 0; c < num_ploog; c++)
			{
				if (c >= scr2 && c < scr2 + 12)
				{
					if (sel2==c)	// highlight
						ik_drawbox(screen, bx+plx+16, by+iny+3+(c-scr2)*8, bx+plx+inw+15, by+iny+11+(c-scr2)*8, 3); //STARMAP_INTERFACE_COLOR*16+4);

					if (ploog[c] < player.num_items)
						i = player.items[ploog[c]];
					else
					{
						i = shipsystems[shiptypes[0].system[ploog[c]-player.num_items]].item;
						ik_dsprite(screen, bx+plx+15, by+iny-1+(c-scr2)*8, spr_IFsystem->spr[9], 2+(STARMAP_INTERFACE_COLOR<<8));
					}
					co = item_colorcode(i);
					ik_print(screen, tradefont, bx+plx+28, by+iny+4+(c-scr2)*8, co, itemtypes[i].name);
				}
			}
			if (num_ploog > 12)
			{
				ik_dsprite(screen, bx+plx+inw+4, by+iny+4, spr_IFarrows->spr[5], 2+(STARMAP_INTERFACE_COLOR<<8));
				ik_dsprite(screen, bx+plx+inw+4, by+iny+92, spr_IFarrows->spr[4], 2+(STARMAP_INTERFACE_COLOR<<8));
				interface_drawslider(screen, bx+plx+inw+4, by+iny+12, 1, 80, num_ploog-12, scr2, STARMAP_INTERFACE_COLOR);
			}

			interface_thinborder(screen, bx+plx+16, by+iny, bx+plx+inw+16, by+iny+104, STARMAP_INTERFACE_COLOR);

			// klakar side
			ik_print(screen, font_6x8, bx+klx+16, by+24, STARMAP_INTERFACE_COLOR, textstring[STR_TRADE_EMPORIUM]);
			interface_thinborder(screen, bx+klx+16, by+iny, bx+klx+inw+16, by+iny+104, STARMAP_INTERFACE_COLOR, STARMAP_INTERFACE_COLOR*16+1);
			for (c = 0; c < kla_numitems; c++)
			if (c >= scr1 && c < scr1 + 12)
			{
				if (sel1==c)	// highlight
					ik_drawbox(screen, bx+klx+16, by+iny+3+(c-scr1)*8, bx+klx+inw+15, by+iny+12+(c-scr1)*8, 3); //STARMAP_INTERFACE_COLOR*16+4);

				co = item_colorcode(kla_items[c]);
				ik_print(screen, tradefont, bx+klx+20, by+iny+4+(c-scr1)*8, co, itemtypes[kla_items[c]].name);
			}
			if (kla_numitems > 12)
			{
				ik_dsprite(screen, bx+klx+inw+4, by+iny+4, spr_IFarrows->spr[5], 2+(STARMAP_INTERFACE_COLOR<<8));
				ik_dsprite(screen, bx+klx+inw+4, by+iny+92, spr_IFarrows->spr[4], 2+(STARMAP_INTERFACE_COLOR<<8));
				interface_drawslider(screen, bx+klx+inw+4, by + iny+12, 1, 80, kla_numitems-12, scr1, STARMAP_INTERFACE_COLOR);
			}
			interface_thinborder(screen, bx+klx+16, by+iny, bx+klx+inw+16, by+iny+104, STARMAP_INTERFACE_COLOR);

			// trade and info buttons
			ik_dsprite(screen, bx+klx-12, by+iny+20, spr_IFbutton->spr[19], 2+(STARMAP_INTERFACE_COLOR<<8));
			ik_dsprite(screen, bx+16, by+h-24, spr_IFbutton->spr[11], 2+(STARMAP_INTERFACE_COLOR<<8));
			ik_dsprite(screen, bx+klx+16, by+h-24, spr_IFbutton->spr[11], 2+(STARMAP_INTERFACE_COLOR<<8));
			interface_drawbutton(screen, bx+klx*2-80, by+h-24, 64, STARMAP_INTERFACE_COLOR, textstring[STR_DONE]);

			ik_blit();
			if (settings.random_names & 4)
			{
				interface_tutorial(tut_trading);
			}

		}
	}

	Stop_Sound(15);

	must_quit = 0;

	reshalfbritescreen();
}


// mercenary payments

int32 pay_item(char *title, char *text, int r, char klak)
{
	int32 c, mc;
	int32 end = 0;
	int32 it = -1;
	int32 co, i;
	int32 bx = 240, by = 128, h = 224;
	int32 scr, sel;
	int32 upd = 1;
	int32 y, y1;
	int32 ploog[64];
	int32 num_ploog=0;
	int32 ti = 0, clt = 0, ot = 0;

	for (y = 0; y < player.num_items + shiptypes[0].num_systems; y++)
	{
		if (y < player.num_items)	// inventory
		{	
			if (!(itemtypes[player.items[y]].flag & 2) && (itemtypes[player.items[y]].type != item_lifeform || klak == 1))
				ploog[num_ploog++]=y;
		}
		else		// system
		{	
			if (shipsystems[shiptypes[0].system[y-player.num_items]].item > -1 && shiptypes[0].sysdmg[y-player.num_items]==0)
				ploog[num_ploog++]=y;
		}
	}

	if (!num_ploog)
		return -1;

	halfbritescreen();

//	Stop_All_Sounds();
	Play_Sound(WAV_PAYMERC, 15, 1);

	while (!end && !must_quit)
	{
		sel = -1; scr = 0;
		clt = 0;

		while (!end && !must_quit)
		{
			ik_eventhandler();
//			t = get_ik_timer(0);
			mc = ik_mclick();	

			if (must_quit)
			{
				end = 1; it = -1;
				must_quit = 0;
			}

			ot = ti; ti = get_ik_timer(2);
			if (ti != ot)
			{ upd = 1;	}

			if (mc == 1)
			{
				if (ik_mouse_y > by+96 && ik_mouse_y < by+192)
				{
					if (ik_mouse_x > bx+16 && ik_mouse_x < bx + 180)
					{
						y = sel;
						sel = scr + (ik_mouse_y - (by+96))/8;
						if (sel > num_ploog || sel < 0)
						{	
							sel = -1; 
							Play_SoundFX(WAV_DESELECT, get_ik_timer(0));
						}
						else
							Play_SoundFX(WAV_SELECT, get_ik_timer(0));
						upd = 1;
						if (y==sel && ti-clt<20) // doubleclick for info
						{
							if (ploog[sel] < player.num_items)
								y = player.items[ploog[sel]];
							else
								y = shipsystems[shiptypes[0].system[ploog[sel]-player.num_items]].item;

							Play_SoundFX(WAV_INFO);
							interface_popup(font_6x8, bx+96, by+24, 192, 96, STARMAP_INTERFACE_COLOR, 0, 
															itemtypes[y].name, itemtypes[y].text);
							upd=1;
						}
					}
					else if (ik_mouse_x > bx + 180 && ik_mouse_x < bx+188 && num_ploog>12)
					{
						if (ik_mouse_y < by + 104)
							scr = MAX(0,scr-1);
						else if (ik_mouse_y > by + 184)
							scr = MIN(num_ploog-12, scr+1);
						else
							scr = MIN(((ik_mouse_y - (by+104))*(num_ploog-12)+40) / 80, num_ploog-12);
						Play_SoundFX(WAV_SLIDER, get_ik_timer(0));
						upd = 1;
					}
				}
				if (ik_mouse_y > by+h-24 && ik_mouse_y < by+h-8)
				{	
					if (ik_mouse_x > bx+16 && ik_mouse_x < bx+80)
					{	
						end = 1; it = -1; 
						Play_SoundFX(WAV_DECLINE, get_ik_timer(0));
					}
					else if (ik_mouse_x > bx+128 && ik_mouse_x < bx+192 && sel > -1)
					{	
						end = 1; 
						sel = ploog[sel];
						if (sel == -1)
							it = -1;
						else if (sel < player.num_items)
							it = player.items[sel];
						else
							it = shipsystems[shiptypes[0].system[sel-player.num_items]].item;
						Play_SoundFX(WAV_ACCEPT, get_ik_timer(0));
					}
				}
				clt=ti;
			}

			if (upd)
			{
				upd = 0;

				prep_screen();
				interface_drawborder(screen, bx, by, bx+208, by+h, 1, STARMAP_INTERFACE_COLOR, title);

				interface_textbox(screen, font_4x8,
													bx+84, by+24, 108, 64, 0,
													text);

				ik_dsprite(screen, bx+16, by+24, spr_SMraces->spr[r], 0);
				ik_dsprite(screen, bx+16, by+24, spr_IFborder->spr[IF_BORDER_PORTRAIT], 2+(STARMAP_INTERFACE_COLOR<<8));

				interface_drawbutton(screen, bx+16, by+h-24, 64, STARMAP_INTERFACE_COLOR, textstring[STR_DECLINE]);
				interface_drawbutton(screen, bx+128, by+h-24, 64, STARMAP_INTERFACE_COLOR, textstring[STR_ACCEPT]);

				interface_thinborder(screen, bx+16, by+92, bx+192, by+196, STARMAP_INTERFACE_COLOR, STARMAP_INTERFACE_COLOR*16+1);

				for (c = 0; c < num_ploog; c++)
				{
					y1 = c-scr;
					y = ploog[c];
					if (y1 >= 0 && y1 < 12)
					{
						if (c == sel)
							ik_drawbox(screen, bx+16, by+95+(c-scr)*8, bx+191, by+103+(c-scr)*8, 3); //STARMAP_INTERFACE_COLOR*16+4);

						if (y < player.num_items)	// inventory
							i = player.items[y];
						else		// system
						{
							i = shipsystems[shiptypes[0].system[y-player.num_items]].item;
							ik_dsprite(screen, bx+15, by+91+(c-scr)*8, spr_IFsystem->spr[9], 2+(STARMAP_INTERFACE_COLOR<<8));
						}
						co = item_colorcode(i);
						ik_print(screen, font_6x8, bx + 28, by + 96 + y1 * 8, co, itemtypes[i].name);
					}
				}
				if (num_ploog > 12)
				{
					ik_dsprite(screen, bx+180, by+96, spr_IFarrows->spr[5], 2+(STARMAP_INTERFACE_COLOR<<8));
					ik_dsprite(screen, bx+180, by+184, spr_IFarrows->spr[4], 2+(STARMAP_INTERFACE_COLOR<<8));
					interface_drawslider(screen, bx + 180, by + 104, 1, 80, num_ploog-12, scr, STARMAP_INTERFACE_COLOR);
				}
				interface_thinborder(screen, bx+16, by+92, bx+192, by+196, STARMAP_INTERFACE_COLOR);

				ik_blit();
			}
		}

		if (!klak && it>-1)	// mercenary hire
		{
			ik_print_log("try to give %s to %s\n", itemtypes[it].name, shiptypes[player.ships[player.num_ships-1]].name);
			it = ally_install(player.num_ships-1, it, 1);

			if (it == -1)
			{	end = 0; ik_print_log("didn't accept\n"); }
			else
				ik_print_log("accepted %s - ", itemtypes[it].name);
		}

		if (it>-1)
		{
			end = 1;
			if (sel < player.num_items)
			{
				ik_print_log("removed item %s\n", itemtypes[player.items[sel]].name);
				starmap_removeitem(sel);
			}
			else
			{
				ik_print_log("removed system %s\n", shipsystems[shiptypes[0].system[sel-player.num_items]].name);
				starmap_destroysystem(sel - player.num_items);
			}
		}

	}

	Stop_Sound(15);

	reshalfbritescreen();

	return it;
}

int32 ally_install(int32 s, int32 it, int32 pay)
{
	int32 r = -1;
	int32 sz = 1;
	int32 sys = -1;
	int32 isys, lc;
	int32 st = player.ships[s];
	int32 c;
	int32 m = 0;

	char texty[256];
	char sname[64];

	if (hulls[shiptypes[st].hull].size >= 32)
		sz = 2;

	if (itemtypes[it].type == item_system || itemtypes[it].type == item_weapon)
	{
		sys = itemtypes[it].index;
		ik_print_log("AI: Is system %s. ", shipsystems[sys].name);
	}

	if (!pay)
	{
		if (shiptypes[st].flag & 32)
			sprintf(sname, textstring[STR_ALLY_CAPT1], shiptypes[st].name);
		else
			sprintf(sname, textstring[STR_ALLY_CAPT2], shiptypes[st].name);
		sprintf(texty, textstring[STR_ALLY_CONFIRM], itemtypes[it].name, sname);
		if (interface_popup(font_6x8, 256, 192, 192, 0, STARMAP_INTERFACE_COLOR, 0, 
												textstring[STR_ALLY_CONFIRMT], texty, textstring[STR_YES], textstring[STR_NO]))
		{
			ik_print_log("AI: User cancelled.\n");
			return -1;
		}

		if (sys > -1)
			if (shipsystems[sys].type == sys_engine)
			{ 
				sys = -1; 
				ik_print_log("AI: Refused engine. ");
			}
	}

	if (sys == -1)	// accept non-systems as is
	{
		if (pay)
		{
			sprintf(texty, textstring[STR_MERC_THANKS2], shiptypes[st].name, itemtypes[it].name);
			interface_popup(font_6x8, 256, 192, 192, 0, STARMAP_INTERFACE_COLOR, 0, 
													textstring[STR_ALLY_TITLE], texty, textstring[STR_OK]);
			return it;
		}
		else	// if a gift, refuse if unusable
		{	
			r = -1;		
			ik_print_log("AI: Refused nonsystem. ");
		}
	}
	else if (shipsystems[sys].size <= sz)	// fits in the ship
	{
		// replace same type of system if better 
		ik_print_log("AI: %s fits on ship. ", shipsystems[sys].name);
		if (shipsystems[sys].type != sys_misc)
		{
			isys = -1; lc = -1;
			for (c = 0; c < shiptypes[st].num_systems; c++)
			if (shipsystems[shiptypes[st].system[c]].type == shipsystems[sys].type)
			{
				m = 1;	// matches current system type
				ik_print_log("AI: %s matches %s on ship. ", shipsystems[sys].name, shipsystems[shiptypes[st].system[c]].name);
				// check the cost for "betterness"
				if (itemtypes[shipsystems[shiptypes[st].system[c]].item].cost < itemtypes[it].cost)
				{
					if (lc == -1 || itemtypes[shipsystems[shiptypes[st].system[c]].item].cost < lc)
					{
						isys = c;
						lc = itemtypes[shipsystems[shiptypes[st].system[c]].item].cost;
					}
				}
			}	
			
			if (isys > -1)
			{
				ik_print_log("AI: %s replaces %s. ", shipsystems[itemtypes[it].index].name, shipsystems[shiptypes[st].system[isys]].name);
				shiptypes[st].system[isys] = itemtypes[it].index;
				r = it; 
			}
		}
		
		if (!m)	// didn't match the type of any current systems, so install it!
		{
			ik_print_log("AI: No match, install %s. ", shipsystems[itemtypes[it].index].name);
			shiptypes[st].system[shiptypes[st].num_systems++] = itemtypes[it].index;
			r = it;
		}
	
	}
	else
	{
		r = -1;
		ik_print_log("AI: can't install. ");
	}

	ik_print_log("AI: sort systems. ");
	sort_shiptype_systems(st);

	if (pay)
	{
		if (r == -1)	// didn't install
		{
			if (!m)
			{	
				sprintf(texty, textstring[STR_MERC_TOOBIG], shiptypes[st].name, itemtypes[it].name);
				ik_print_log("AI: too big. ");
			}
			else
			{
				sprintf(texty, textstring[STR_MERC_NOGOOD], shiptypes[st].name, itemtypes[it].name);
				ik_print_log("AI: not good enough. ");
			}
			r = interface_popup(font_6x8, 256, 192, 192, 0, STARMAP_INTERFACE_COLOR, 0, 
													textstring[STR_MERC_NOGOODT], texty, textstring[STR_YES], textstring[STR_NO]);

			if (r == 0)
			{
				r = it;
				sprintf(texty, textstring[STR_MERC_THANKS2], shiptypes[st].name, itemtypes[it].name);
				interface_popup(font_6x8, 256, 192, 192, 0, STARMAP_INTERFACE_COLOR, 0, 
														textstring[STR_ALLY_TITLE], texty, textstring[STR_OK]);
				ik_print_log("AI: take it anyway. ");
			}
			else
			{
				r = -1;
				ik_print_log("AI: don't take it. ");
			}
		}
		else
		{
			sprintf(texty, textstring[STR_MERC_THANKS], shiptypes[st].name);
			interface_popup(font_6x8, 256, 192, 192, 0, STARMAP_INTERFACE_COLOR, 0, 
													textstring[STR_ALLY_TITLE], texty, textstring[STR_OK]);
			ik_print_log("AI: thank you ");
		}
	}
	else	// gift
	{
		if (r == -1)
		{
			if (shiptypes[st].flag & 32)
				sprintf(sname, textstring[STR_ALLY_CAPT1], shiptypes[st].name);
			else
				sprintf(sname, textstring[STR_ALLY_CAPT2], shiptypes[st].name);
			sprintf(texty, textstring[STR_ALLY_REFUSE], sname);
			interface_popup(font_6x8, 256, 192, 192, 0, STARMAP_INTERFACE_COLOR, 0, 
											textstring[STR_ALLY_REFUSET], texty, textstring[STR_OK]);
			ik_print_log("AI: refused. ");
		}
		else
		{
			if (shiptypes[st].flag & 32)
				sprintf(sname, textstring[STR_ALLY_SHIP1], shiptypes[st].name);
			else
				sprintf(sname, textstring[STR_ALLY_SHIP2], shiptypes[st].name);
			sprintf(texty, textstring[STR_ALLY_INSTALL], itemtypes[r].name, sname);
			interface_popup(font_6x8, 256, 192, 192, 0, STARMAP_INTERFACE_COLOR, 0, 
											textstring[STR_ALLY_INSTALLT], texty, textstring[STR_OK]);
			ik_print_log("AI: thanks for the gift (%s). ", itemtypes[r].name);
		}
	}
	ik_print_log("\n");
		

	return r;
}

// use artifacts


int32 use_vacuum_collapser(char *title)
{
	int32 mc, c;
	int32 end = 0;
	int32 bl = 0;
	int32 t=0;
	t_ik_sprite *bg;
	int32 bx = SM_MAP_X + 144, by = SM_MAP_Y + 200 + (1-2*(sm_stars[player.system].y < 0))*80;

	char texty[256];
	char nummy[16];
	int32 num = 0;

	sprintf(texty, textstring[STR_LVC_CONFIRM], sm_stars[player.system].starname);

	if (interface_popup(font_6x8, bx, by, 192, 80, STARMAP_INTERFACE_COLOR, 0, 
			title, texty, textstring[STR_YES], textstring[STR_NO]))
		return 0;	// cancel

	// set timer here

	prep_screen();

	bg = get_sprite(screen, bx, by, 192, 80);

	interface_drawborder(screen,
											 bx, by, bx+192, by+80,
											 1, STARMAP_INTERFACE_COLOR, title);

	sprintf(texty, textstring[STR_LVC_ASKWHEN]);
	interface_textbox(screen, font_6x8,
										bx+16, by+24, 160, 24, 0,
										texty);
	ik_print(screen, font_6x8, bx+16, by+48, 0, textstring[STR_LVC_DAYSTILL]);
	ik_blit();

	sprintf(nummy, "");
	ik_text_input(bx+176-24, by+48, 4, font_6x8, "", nummy, STARMAP_INTERFACE_COLOR*16+3, STARMAP_INTERFACE_COLOR);

	num = 0;

	if (!strlen(nummy))
		end = 1;
	for (c = 0; c < (int)strlen(nummy); c++)
	{
		if (nummy[c] >= '0' && nummy[c] <= '9')
			num = num*10 + nummy[c]-'0';
	}
	prep_screen();
	ik_drawbox(screen, bx+176-24, by+48, bx+176, by+55, STARMAP_INTERFACE_COLOR*16+3);
	ik_print(screen, font_6x8, bx+176-24, by+48, 3, "%4d", num);

	interface_drawbutton(screen, bx+192-64, by + 80 - 24, 48, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);
	interface_drawbutton(screen, bx+16, by + 80 - 24, 48, STARMAP_INTERFACE_COLOR, textstring[STR_CANCEL]);

	ik_blit();

	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		mc = ik_mclick() & 3;	
		c = ik_inkey();

		if (c == 13)
			end = 2;

		c = t; t = get_ik_timer(2);
		if (t != c)
		{ prep_screen(); ik_blit();	}

		if (mc == 1 && ik_mouse_y > by+48 && ik_mouse_y < by+56)
		{
			if (ik_mouse_x > bx+176-24 && ik_mouse_x < bx+176)
			{
				sprintf(nummy, "%d", num);
				ik_text_input(bx+176-24, by+48, 4, font_6x8, "", nummy, STARMAP_INTERFACE_COLOR*16+3, STARMAP_INTERFACE_COLOR);

				num = 0;
				if (!strlen(nummy))
					end = 1;
				for (c = 0; c < (int)strlen(nummy); c++)
				{
					if (nummy[c] >= '0' && nummy[c] <= '9')
						num = num*10 + nummy[c]-'0';
				}
				prep_screen();
				ik_drawbox(screen, bx+176-24, by+48, bx+176, by+55, STARMAP_INTERFACE_COLOR*16+3);
				ik_print(screen, font_6x8, bx+176-24, by+48, 3, "%4d", num);
				ik_blit();
			}
		}

		if (mc == 1 && ik_mouse_y > by+80-24 && ik_mouse_y < by+80-8)
		{
			if (ik_mouse_x > bx+192-64 && ik_mouse_x < bx+192-16)
				end = 2;
			else if (ik_mouse_x > bx+16 && ik_mouse_x < bx+64)
				end = 1;
		}
	}

	prep_screen();
	ik_dsprite(screen, bx, by, bg, 4);
	ik_blit();
	free_sprite(bg);

	if (must_quit)
	{	must_quit = 0; end = 1; }

	if (end == 2)	// use vacuum collapser
	{
		Play_SoundFX(WAV_YES, get_ik_timer(0));

		sm_stars[player.system].novadate = player.stardate + num;
		sm_stars[player.system].novatype = 2;
	}
	else if (end == 1)
		Play_SoundFX(WAV_NO, get_ik_timer(0));

	return end-1;
}

void vacuum_collapse(int st)
{
	int32 end = 0;
	int32 cx, cy;
	int32 x,y;
	int32 x1,y1,d,s;
	int32 tx,ty;
	int32 r, c, t, t0, ts;
	int32 f;
	//int32 ti2;
	int32 sz;
	uint8 *data;

	cx = sm_stars[st].x; cy = sm_stars[st].y;

	Play_SoundFX(WAV_COLLAPSER);

	ts = t0 = t = get_ik_timer(2);

	sm_stars[st].novatype = 2;
	sm_stars[st].novatime = t+25;

	f = 0;
	while (!end && !must_quit)
	{
		ik_eventhandler();

		t0 = t;
		t = get_ik_timer(2);

		if (t > ts + 25 && sm_stars[st].color > -2)
			sm_stars[st].color = -3;

		cx = sm_stars[st].x; cy = sm_stars[st].y;

		if (t > t0)
		{

			if (t > sm_stars[st].novatime + 150)
				end = 1;

			if (t >= sm_stars[st].novatime && t < sm_stars[st].novatime + 50)
			{
				sz = (t - sm_stars[st].novatime)*2;

				if (player.num_ships > 0)
				{
					r = get_distance(cx - player.x, cy - player.y);
					if (r < sz || (player.enroute && sm_stars[player.target].color < 0))	// kill player
					{
						player.num_ships = 0;
						player.death = 3;
						player.deatht = t;
					}
				}

				for (c = 0; c < num_holes; c++)
				{
					r = get_distance(cx - sm_holes[c].x, cy - sm_holes[c].y);
					if (r < sz)	// five light year radius of destruction
						sm_holes[c].size = 0;
				}
				for (c = 0; c < num_stars; c++)
				if (c != st && sm_stars[c].color > -2)
				{
					r = get_distance(cx - sm_stars[c].x, cy - sm_stars[c].y);
					if (r < sz)	// five light year radius of destruction
					{
						
						Play_SoundFX(WAV_EXPLO2, t);
						sm_stars[c].novatype = 3;
						sm_stars[c].novadate = player.stardate - 1;
						sm_stars[c].novatime = t;
						sm_stars[c].color = -3;
						for (r = 0; r < STARMAP_MAX_FLEETS; r++)
						if (sm_fleets[c].num_ships > 0 && sm_fleets[c].enroute == 0 && sm_fleets[c].system == c)
						{
							sm_fleets[r].num_ships = 0;
						}
					}
				}

				for (c = 0; c < STARMAP_MAX_FLEETS; c++)
				if (sm_fleets[c].num_ships > 0)
				{
					if (sm_fleets[c].enroute)
					{
						if (sm_fleets[c].distance > 0)
						{
							x = sm_stars[sm_fleets[c].system].x + ((sm_stars[sm_fleets[c].target].x-sm_stars[sm_fleets[c].system].x)*sm_fleets[c].enroute)/sm_fleets[c].distance;
							y = sm_stars[sm_fleets[c].system].y + ((sm_stars[sm_fleets[c].target].y-sm_stars[sm_fleets[c].system].y)*sm_fleets[c].enroute)/sm_fleets[c].distance;
						}
						else
						{
							x = sm_stars[sm_fleets[c].target].x;
							y = sm_stars[sm_fleets[c].target].y;
						}
					}
					else
					{
						x = sm_stars[sm_fleets[c].system].x;
						y = sm_stars[sm_fleets[c].system].y;
					}

					r = get_distance ( cx - x, cy - y );
					if (r < sz)
					{
						if (sm_fleets[c].explored < 1)
							sm_fleets[c].explored = 1;
						sm_fleets[c].num_ships = 0;
						sm_fleets[c].blowtime = t;
						Play_SoundFX(WAV_EXPLO2, t);
					}

				}
			
			}

			if (t > sm_stars[st].novatime && t < sm_stars[st].novatime + 50)
			{
				if (!f)
				{
					cx = 240 + sm_stars[st].x; cy = 240 - sm_stars[st].y;
					s = ((t - sm_stars[st].novatime)<<8)/50;
					d = (128<<8)/s;
					ty = 0;
					for (y1 = cy - s/2; y1 < cy + s/2; y1++)
					{
						tx = 0;
						if (y1>=0 && y1 < 480)
						for (x1 = cx - s/2; x1 < cx + s/2; x1++)
						{
							if (x1>=0 && x1 < 480)
							{
								data = spr_SMnebula->spr[8]->data;
								x = ((ty>>8)<<7) + (tx>>8);

//								r = ((data[x] * (256-(tx&255)) + data[x+1] * (tx&255)) * (256-(ty&255)) + 
//										(data[x+128] * (256-(tx&255)) + data[x+129] * (tx&255)) * (ty&255)) >> 16;
								r = data[x];
								t = sm_nebulamap[(y1<<9)+x1];
								if (r < 15)
									sm_nebulamap[(y1<<9)+x1] = (t * r) / 15;
							}
							tx+=d;
						}
						ty+=d;
					}
				}
				else
				{
					starmap_createnebulagfx();
				}
			}

			prep_screen();
			starmap_display(get_ik_timer(0));
			f = 1-f;

//			ik_print(screen, font_6x8, SM_MAP_X, SM_MAP_Y, 3, "%d (%d)", t - sm_stars[st].novatime, t);
			ik_blit();
		}
	}

	sm_stars[st].color = -2;
	sm_stars[st].novadate = 0;
	sm_stars[st].novatype = 0;

	if (sm_stars[homesystem].color == -2 && player.death != 3)
		player.death = 7;

	cx = 240 + sm_stars[st].x; cy = 240 - sm_stars[st].y;
	for (y1 = cy - 128; y1 < cy + 127; y1++)
		if (y1>=0 && y1 < 480)
		for (x1 = cx - 128; x1 < cx + 127; x1++)
			if (x1>=0 && x1 < 480)
			{
				x = (x1+128-cx); y = (y1+128-cy);
				data = spr_SMnebula->spr[8]->data;
				r = ((data[(y>>1)*128+(x>>1)] +
							data[(y>>1)*128+(x>>1)+1]*(x&1) +
							data[(y>>1)*128+(x>>1)+128]*(y&1) +
							data[(y>>1)*128+(x>>1)+129]*(x&1)*(y&1)) * 4) / (1+(x&1)+(y&1)+(x&1)*(y&1));
				t = sm_nebulamap[(y1<<9)+x1];
				if (r < 60)
					sm_nebulamap[(y1<<9)+x1] = (t * r) / (15 * 4);
			}

	starmap_createnebulagfx();

	must_quit = 0;
}

int32 probe_fleet_encounter(int32 flt)
{
	int32 mc, c;
	int32 end = 0;
	int32 bx = 240, by = 152;
	int32 mx, my;
	int32 r, t, t0;
	int32 l;
	int32	upd = 1;
	int32 sx[8], sy[8];
	int32 survive;
	char texty[256];

	halfbritescreen();

	r = sm_fleets[flt].race;

	if (races[r].met)
		sm_fleets[flt].explored=2;
	else
		sm_fleets[flt].explored=1;

	Stop_All_Sounds();

	Play_SoundFX(WAV_PROBE_DEST, 0);

	for (c = 0; c < sm_fleets[flt].num_ships; c++)
	{
		mc = hulls[shiptypes[sm_fleets[flt].ships[c]].hull].size;
		sx[c] = bx + 70 + c * 16 - (sm_fleets[flt].num_ships-1) * 8 + rand()%12;
		sy[c] =	by + 56 + rand()%16;
	}

	if (r == race_klakar || races[r].met==2)	// klakar or friend-muktians
		survive = 1;
	else if (rand()&1)
		survive = 1;
	else
		survive = 0;

	Play_Sound(WAV_RADAR, 15, 1);

	t = get_ik_timer(0);
	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		mc = ik_mclick();	
		t0 = t; t = get_ik_timer(0);
		mx = ik_mouse_x - bx; my = ik_mouse_y - by;
		if (mc == 1 && mx > 112 && mx < 144 && my > 184 && my < 200)
		{	end = 1; Play_SoundFX(WAV_DOT, get_ik_timer(0)); }

		if (t > t0 || upd == 1)
		{
			upd = 0;
			prep_screen();
			interface_drawborder(screen,
													 bx, by, bx+160, by+208,
													 1, STARMAP_INTERFACE_COLOR, textstring[STR_PROBE_TITLE]);
			if (races[r].met)
				ik_print(screen, font_6x8, bx+16, by+24, 3, textstring[STR_SCANNER_RACE], races[r].name);
			else
				ik_print(screen, font_6x8, bx+16, by+24, 3, textstring[STR_SCANNER_NORACE]);

			if (survive)
			{
				if (sm_fleets[flt].num_ships>1)
					sprintf(texty, textstring[STR_PROBE_FLEET1], sm_fleets[flt].num_ships);
				else
					sprintf(texty, textstring[STR_PROBE_FLEET2]);
			}
			else
			{
				sprintf(texty, textstring[STR_PROBE_FLEET3]);
			}
			interface_textbox(screen, font_4x8, bx+16, by+160, 128, 24, 0, 
												texty);

			ik_dsprite(screen, bx+16, by+32, spr_IFborder->spr[19], 2+(4<<8));
			l = 10 + 5 * ((t%50)>40);
			for (c = 0; c < sm_fleets[flt].num_ships; c++)
			{
				mc = hulls[shiptypes[sm_fleets[flt].ships[c]].hull].size;
				ik_drsprite(screen, sx[c], sy[c], 0, 8,
									 spr_IFarrows->spr[10-(mc<32)+(mc>80)], 5+(l<<8));
			}
			l = ((t&63)*3 - 95 + 1024) & 1023;
			ik_dspriteline(screen, bx + 80, by + 152, 
										bx + 80 + ((sin1k[l] * 112) >> 16), 
										by + 152 - ((cos1k[l] * 112) >> 16), 
										8, (t&31), 18, spr_weapons->spr[2], 5+(10<<8));
			interface_drawbutton(screen, bx+112, by+184, 32, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);
			ik_blit();
		}

	}

	Stop_Sound(15);
	must_quit = 0;
	reshalfbritescreen();

	return survive;
}

void probe_exploreplanet(int32 probe)
{
	int32 mc, c;
	int32 h;
	int32 end = 0;
	int32 it = -1;
	int32 bx = 224, by = 112;
	int32 tof = 0;
	int32 t=0;
	char name[32];
	char texty[256];

	if (sm_stars[player.target].explored)
		return;

//	Stop_All_Sounds();

	if (!probe)	// regular stellar probe
	{
		for (c = 0; c < STARMAP_MAX_FLEETS; c++)
		{
			if (sm_fleets[c].num_ships>0 && sm_fleets[c].system == player.target)
				it=0;
		}
		if (it)
			Play_SoundFX(WAV_PROBE_DEST, 0);
	}
	else
	{
		tof = STR_ANALYZER_MISCDATA1 - STR_PROBE_MISCDATA1;
		Play_Sound(WAV_SCANNER, 15, 1);
	}

	it = -1;

	sm_stars[player.target].explored = 1;
	c = sm_stars[player.target].card; 

	h = 192;

	prep_screen();
	halfbritescreen();
	if (!probe)
	interface_drawborder(screen,
											 bx, by, bx+192, by+h,
											 1, STARMAP_INTERFACE_COLOR, textstring[STR_PROBE_TITLE]);
	else
	interface_drawborder(screen,
											 bx, by, bx+192, by+h,
											 1, STARMAP_INTERFACE_COLOR, textstring[STR_PROBE_TITLE2]);

	ik_print(screen, font_6x8, bx+16, by+24, 3, sm_stars[player.target].planetname);
	ik_print(screen, font_4x8, bx+176-strlen(textstring[STR_CARD_RENAME])*4, by+24, 3, textstring[STR_CARD_RENAME]);
	ik_dsprite(screen, bx+16, by+36, spr_SMplanet2->spr[sm_stars[player.target].planetgfx], 0);
	ik_dsprite(screen, bx+16, by+36, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));
	interface_textbox(screen, font_6x8,
										bx+84, by+36, 96, 104, 0,
										platypes[sm_stars[player.target].planet].text);

	sprintf(name, textstring[STR_PROBE_MISCDATA]);
	sprintf(texty, textstring[STR_PROBE_MISCDATA1+tof]);
	if (ecards[c].type == card_event)
	{	

		if (!strcmp(ecards[c].name, textstring[STR_EVENT_FLARE]) || !strcmp(ecards[c].name, textstring[STR_EVENT_NOVA]))
		{
			sprintf(texty, textstring[STR_PROBE_MISCDATA2+tof], sm_stars[player.target].starname, player.shipname);
		}

	}
	else if (ecards[c].type == card_ally)
	{
		it = shiptypes[ecards[c].parm].race; 
		if (it == race_none)
			sprintf(texty, textstring[STR_PROBE_MISCDATA3+tof]);

		if (it == race_muktian)
			sprintf(texty, textstring[STR_PROBE_MISCDATA4+tof]);

	}
	else if ((ecards[c].type == card_item) || (ecards[c].type == card_rareitem) || (ecards[c].type == card_lifeform))
	{
		it = ecards[c].parm;
		if (ecards[c].type == card_lifeform)
			sprintf(texty, textstring[STR_PROBE_MISCDATA5+tof]);

		if (itemtypes[it].type == item_treasure)
			sprintf(texty, textstring[STR_PROBE_MISCDATA6+tof]);
	}

	ik_print(screen, font_6x8, bx+96-strlen(name)*3, by+108, STARMAP_INTERFACE_COLOR, name);
	interface_textbox(screen, font_6x8,
										bx+16, by+120, 160, 48, 0,
										texty); 

	interface_drawbutton(screen, bx+144, by+h-24, 32, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);

	ik_blit();

	while (!must_quit && !end)
	{
		ik_eventhandler();  // always call every frame
		mc = ik_mclick();	
		c = ik_inkey();

		if (mc == 1)
		{
			if (ik_mouse_y > by+24 && ik_mouse_y < by+32 && ik_mouse_x > bx+16 && ik_mouse_x < bx+176)
			{
				strcpy(name, sm_stars[player.target].planetname);
				prep_screen();
				ik_drawbox(screen, bx+16, by+24, bx+176, by+32, STARMAP_INTERFACE_COLOR*16+3);
				free_screen();
				ik_text_input(bx+16, by+24, 16, font_6x8, "", name, STARMAP_INTERFACE_COLOR*16+3, STARMAP_INTERFACE_COLOR);
				if (strlen(name))
					strcpy(sm_stars[player.target].planetname, name);

				prep_screen();
				ik_drawbox(screen, bx+16, by+24, bx+176, by+32, STARMAP_INTERFACE_COLOR*16+3);
				ik_print(screen, font_6x8, bx+16, by+24, 3, sm_stars[player.target].planetname);

				ik_blit();
			}

			if (ik_mouse_y > by+h-24 && ik_mouse_y < by+h-8 && ik_mouse_x > bx+144 && ik_mouse_x < bx+176)
			{	end = 1; Play_SoundFX(WAV_DOT, get_ik_timer(0)); }
		}

		c = t; t = get_ik_timer(2);
		if (t != c)
		{ prep_screen(); ik_blit();	}
	}

	ik_print_log("Launched the Stellar Probe to the %s system, discovered a %s planet and named it %s.\n", 
								sm_stars[player.target].starname,
								platypes[sm_stars[player.target].planet].name,
								sm_stars[player.target].planetname);

	must_quit = 0;
	reshalfbritescreen();
	Stop_Sound(15);
}

int32 stellar_probe(char *title)
{
	char texty[256];	
	int32 p;
	int32 c;

	if (player.target	== -1 || sm_stars[player.target].explored)
	{	// no target or bad target - don't launch
		interface_popup(font_6x8, SM_INV_X + 32*(SM_INV_X==0) - 64*(SM_INV_X>0), SM_INV_Y+40, 192, 80, STARMAP_INTERFACE_COLOR, 0, 
										title, textstring[STR_PROBE_DIALOG1], textstring[STR_OK]);
		return 0;
	}

	sprintf(texty, textstring[STR_PROBE_DIALOG2], sm_stars[player.target].starname);
	if (!interface_popup(font_6x8, SM_INV_X + 32*(SM_INV_X==0) - 64*(SM_INV_X>0), SM_INV_Y+40, 192, 80, STARMAP_INTERFACE_COLOR, 0, 
										title, texty, textstring[STR_YES], textstring[STR_NO]))
	{
		Stop_All_Sounds();
		Play_SoundFX(WAV_PROBE_LAUNCH, 0);
		start_ik_timer(3, 20);
		while (!must_quit && get_ik_timer(3)<50)
			ik_eventhandler();
		p = 2;
		for (c = 0; c < STARMAP_MAX_FLEETS; c++)
		{
			if (sm_fleets[c].num_ships>0 && sm_fleets[c].system == player.target)	// find a fleet
			{
				p = probe_fleet_encounter(c);
			}
		}
		if (p)
			probe_exploreplanet(0);
		return 1;
	}	
	must_quit = 0;

	return 0;
}


void eledras_mirror(char *title)
{
	int32 c;
	int32 f;
	int32 t=0;

	f = -1;
	for (c = 0; c < STARMAP_MAX_FLEETS; c++)
	{
		// find a fleet
		if (sm_fleets[c].explored>0 && sm_fleets[c].num_ships>0 && sm_fleets[c].system == player.target && sm_fleets[c].enroute==0)
		{
			f = c;
		}
	}


	if (f == -1 || player.target == player.system || player.system == homesystem || player.target == homesystem)
	{	// no target or bad target - don't launch
		interface_popup(font_6x8, SM_INV_X + 32*(SM_INV_X==0) - 64*(SM_INV_X>0), SM_INV_Y+40, 192, 80, STARMAP_INTERFACE_COLOR, 0, 
										title, textstring[STR_MIRROR_NOTARGET], textstring[STR_OK]);
		return;
	}

	if (sm_fleets[f].race == race_unknown)
	{	// space hulk
		c = 0;
		if (!(rand()%5))
			c = 1 + rand()%7;
		Play_Sound(WAV_MUS_COMBAT, 15, 1);
		interface_popup(font_6x8, SM_INV_X + 32*(SM_INV_X==0) - 64*(SM_INV_X>0), SM_INV_Y+40, 192, 80, STARMAP_INTERFACE_COLOR, 0, 
										title, textstring[STR_MIRROR_NOCANDO1+c], textstring[STR_OK]);
		Play_Sound(WAV_BRIDGE, 15, 1, 50);
		return;
	}

	c = sm_fleets[f].system;
	sm_fleets[f].system =	player.system;
	player.system = c;

	for (c = 0; c < STARMAP_MAX_FLEETS; c++)
	{
		if (f != c && sm_fleets[c].num_ships>0 && sm_fleets[c].race==race_klakar && sm_fleets[c].system == sm_fleets[f].system)
		{
			sm_fleets[c].system = player.system;
		}
	}

	player.x = sm_stars[player.system].x;
	player.y = sm_stars[player.system].y;

	player.explore = 1;
	starmap_advancedays(1);
	
	Play_SoundFX(WAV_MIRROR, 0);

	if (sm_stars[player.system].explored<2)
	{
		c = get_ik_timer(0); t = c;
		while (t - c < 75 && !must_quit)
		{
			ik_eventhandler();

			if (get_ik_timer(0)> t)
			{
				t = get_ik_timer(0);

				prep_screen();
				starmap_display(t);
				ik_blit();
			}
		}
	}
	must_quit = 0;

}

int32 eledras_bauble(char *title)
{
	int32 h;
	int32 end;
	int32 bx = 224, by = 112;
	int32 tof = 0;
	int32 it;
	int32 mc, c, x;
	int32 t=0;
	char itname[256];
	char texty[256];

	if (!interface_popup(font_6x8, SM_INV_X + 32*(SM_INV_X==0) - 64*(SM_INV_X>0), SM_INV_Y+40, 192, 80, STARMAP_INTERFACE_COLOR, 0, 
			title, textstring[STR_BAUBLE_CONFIRM], textstring[STR_YES], textstring[STR_NO]))
	{
		Stop_All_Sounds();

		Play_SoundFX(WAV_FOMAX_HI, 0);
		h = 120;

		prep_screen();
		halfbritescreen();
		interface_drawborder(screen,
												 bx, by, bx+192, by+h,
												 1, STARMAP_INTERFACE_COLOR, textstring[STR_BAUBLE_FOMAX]);

		ik_dsprite(screen, bx+16, by+24, spr_SMraces->spr[11], 0);
		ik_dsprite(screen, bx+16, by+24, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));
		interface_textbox(screen, font_4x8,
											bx+84, by+24, 92, 64, 0,
											textstring[STR_BAUBLE_WISH]);

		ik_print(screen, font_6x8, bx+16, by+92, 3, textstring[STR_BAUBLE_PROMPT]);

		ik_blit();

		sprintf(itname, "");
		ik_text_input(bx+16, by+104, 26, font_6x8, "", itname, STARMAP_INTERFACE_COLOR*16+3, STARMAP_INTERFACE_COLOR);

		it = -1;
		for (c = 0; c < num_itemtypes; c++)
			if (!strcmp(itname, itemtypes[c].name))
				it = c;

		if (it==-1)
		{
			for (c = 0; c < num_itemtypes; c++)
			if (strlen(itname) <= strlen(itemtypes[c].name))
			{
				for (x = 0; x <= (int)(strlen(itemtypes[c].name)-strlen(itname)); x++)
				if (!_strnicmp(itname, itemtypes[c].name + x, strlen(itname)))
				{	
					it = c;		
					break;
				}

				if (it > -1)
					break;
			}

		}
/*
		rules of wishing:

	- no muktian ambassador or klakar beacon
	- no duplicates of artifacts (.clas = "artifact") -> no eledra's bauble (obviously)
*/

		if (it>-1)	
			if (itemtypes[it].flag & 2) // unsellable (klakar beacon, ambassador)
				it = -1;

		if (it>-1)
			if (!stricmp(itemtypes[it].clas, textstring[STR_INV_ARTIFACT]))	// check for duplicate artifacts
			{
				for (c = 0; c < kla_numitems; c++)
					if (kla_items[c] == it)
						it = -1;
				for (c = 0; c < player.num_items; c++)
					if (player.items[c] == it)
						it = -1;
				for (c = 0; c < num_stars; c++)
					if (it > -1 && c != homesystem && !strcmp(ecards[sm_stars[c].card].name, itemtypes[it].name))
						it = -1;
			}

		x = 0;
		while (it == -1) // didn't find an item, produce random lifeform
		{
			it = rand()%num_itemtypes;
			if (itemtypes[it].flag & 2)
				it = -1;
			else if (itemtypes[it].type != item_lifeform)
				it = -1;
			else
			{
				for (c = 0; c < kla_numitems; c++)
					if (kla_items[c] == it)
						it = -1;
				for (c = 0; c < player.num_items; c++)
					if (player.items[c] == it)
						it = -1;
				for (c = 0; c < num_stars; c++)
					if (it > -1 && c != homesystem && !strcmp(ecards[sm_stars[c].card].name, itemtypes[it].name))
						it = -1;
			}
			x = 1;
		}

		if (it>-1)
		{
			player.items[player.num_items++]=it;
			if (x)
			{
				sprintf(texty, textstring[STR_BAUBLE_FAIL], itemtypes[it].name);
				Play_SoundFX(WAV_FOMAX_BYE, 0);
			}
			else
			{
				x = itemtypes[it].name[0];
				if (x=='A' || x=='E' || x=='I' || x=='O' || x=='U')
					sprintf(itname, textstring[STR_BAUBLE_AN], itemtypes[it].name);
				else
					sprintf(itname, textstring[STR_BAUBLE_A], itemtypes[it].name);
				sprintf(texty, textstring[STR_BAUBLE_GIFT], itname);
				Play_SoundFX(WAV_FOMAX_WISH, 0);
			}
			
			prep_screen();
			interface_drawborder(screen,
													 bx, by, bx+192, by+h,
													 1, STARMAP_INTERFACE_COLOR, textstring[STR_BAUBLE_FOMAX]);

			ik_dsprite(screen, bx+16, by+24, spr_SMraces->spr[11], 0);
			ik_dsprite(screen, bx+16, by+24, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));
			interface_textbox(screen, font_4x8,
												bx+84, by+24, 92, 64, 0,
												texty);

			interface_drawbutton(screen, bx+144, by+h-24, 32, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);
			ik_blit();

			end = 0;
			while (!must_quit && !end)
			{
				ik_eventhandler();  // always call every frame
				mc = ik_mclick();	
				c = ik_inkey();

				if (mc == 1)
				{
					if (ik_mouse_y > by+h-24 && ik_mouse_y < by+h-8 && ik_mouse_x > bx+144 && ik_mouse_x < bx+176)
					{	end = 1; Play_SoundFX(WAV_DOT, get_ik_timer(0)); }
				}

				c = t; t = get_ik_timer(2);
				if (t != c)
				{ prep_screen(); ik_blit();	}
			}
		}

		must_quit = 0;
		reshalfbritescreen();

		return 1;
	}
	must_quit = 0;

	return 0;
}

void use_conograph(char *title)
{
	Play_Sound(WAV_CONOGRAPH2, 14, 1);
	Play_Sound(WAV_CONOGRAPH, 15, 1);
	interface_popup(font_6x8, SM_INV_X + 32*(SM_INV_X==0) - 64*(SM_INV_X>0), SM_INV_Y+40, 192, 80, STARMAP_INTERFACE_COLOR, 0, 
									title, textstring[STR_CONOGRAPH_PLAY], textstring[STR_OK]);

	starmap_advancedays(365);
	Stop_All_Sounds();
}


int32 item_colorcode(int32 it)
{
	switch (itemtypes[it].type)
	{
		case item_weapon:
		return 1;
		break;

		case item_system:
		switch (shipsystems[itemtypes[it].index].type)
		{
			case sys_thruster:
			return 2;

			case sys_engine:
			return 3;

			default:
			return 5;
		}
		break;

		case item_device:
		return 6;
		break;

		case item_lifeform:
		return 4;
		break;

		case item_treasure:
		return 7;
		break;

		default: 
		return 0;
	}

	return 0;
}
// ----------------
//     INCLUDES
// ----------------

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <math.h>

#include "typedefs.h"
#include "is_fileio.h"

#include "textstr.h"
#include "iface_globals.h"
#include "gfx.h"
#include "snd.h"
#include "interface.h"
#include "combat.h"
#include "cards.h"
#include "starmap.h"

#include "startgame.h"

int32 waitclick(int left = 0, int top = 0, int right = 640, int bottom = 480);


int32 startgame()
{
	int32 end;
	int32 c, mc;
	int32 bx=192, by=88, h=300;
	int32 y;
	int32 upd=1;
	int32 mx, my;
	int32 cn=0;
	int32 ti, ot;
	t_ik_image *bg;
	char name[32];

	loadconfig();

	start_ik_timer(1, 31);
	while (get_ik_timer(1) < 2 && !must_quit)
	{
		ik_eventhandler();
	}
	Play_Sound(WAV_MUS_TITLE, 15, 1, 100, 22050,-1000);
	while (get_ik_timer(1) < 4 && !must_quit)
	{
		ik_eventhandler();
	}
	Play_Sound(WAV_MUS_TITLE, 14, 1, 80, 22050, 1000);


	if (settings.random_names & 1)
		strcpy(settings.captname, captnames[rand()%num_captnames]);

	if (settings.random_names & 2)
		strcpy(settings.shipname, shipnames[rand()%num_shipnames]);

	bg = ik_load_pcx("graphics/starback.pcx", NULL);

	end = 0; ti = get_ik_timer(2);
	while (!end && !must_quit)
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

		if (c==13 || c==32)
			end = 2;
		if ((mc & 1) && mx > 0 && mx < 240)
		{
			if (my > h-24 && my < h-8) // buttons
			{
				if (mx > 16 && mx < 64) // cancel
					end = 1;
				else if (mx > 176 && mx < 224) // ok
				{	end = 2; Play_SoundFX(WAV_DOT); }
			}
			else if (my > 32 && my < 40) // captain
			{
				if (mx < 216)
				{
					cn |= 1;
					prep_screen();
					ik_drawbox(screen, bx+70, by+32, bx+215, by+39, STARMAP_INTERFACE_COLOR*16+3);
					ik_blit();
					strcpy(name, settings.captname);
					ik_text_input(bx+70, by+32, 14, font_6x8, "", name, STARMAP_INTERFACE_COLOR*16+3, STARMAP_INTERFACE_COLOR);
					if (strlen(name)>0)
						strcpy(settings.captname, name);
				}
				else
				{
					settings.random_names ^= 1;
					Play_SoundFX(WAV_LOCK,0);
				}
				upd = 1;must_quit=0;
			}
			else if (my > 40 && my < 48) // ship
			{
				if (mx < 216)
				{
					cn |= 2;
					prep_screen();
					ik_drawbox(screen, bx+70, by+40, bx+215, by+47, STARMAP_INTERFACE_COLOR*16+3);
					ik_blit();
					strcpy(name, settings.shipname);
					ik_text_input(bx+70, by+40, 14, font_6x8, "", name, STARMAP_INTERFACE_COLOR*16+3, STARMAP_INTERFACE_COLOR);
					if (strlen(name)>0)
						strcpy(settings.shipname, name);
				}
				else
				{
					settings.random_names ^= 2;
					Play_SoundFX(WAV_LOCK,0);
				}
				upd = 1;must_quit=0;
			}
			else if (my > 64 && my < 96)	// ship
			{
				settings.dif_ship = (mx - 16)/72;
				Play_SoundFX(WAV_SLIDER,0);
				upd = 1; 
			}
			else if (my > 112 && my < 176) // nebula
			{
				settings.dif_nebula = (mx - 16)/72;
				Play_SoundFX(WAV_SLIDER,0);
				upd = 1; 
			}
			else if (my > 192 && my < 224) // enemies
			{
				settings.dif_enemies = (mx - 16)/72;
				Play_SoundFX(WAV_SLIDER,0);
				upd = 1; 
			}
			else if (my > 232 && my < 240)	// easy/hard
			{
				c = (mx-40)/32; 
				if (c < 0) c=0;
				if (c > 4) c=4;
				settings.dif_nebula = (c+1)/2;
				settings.dif_enemies = c/2;
				upd = 1;
				Play_SoundFX(WAV_SLIDER,0);
			}
			else if (my > 256 && my < 264)	// enable tutorial
			{
				if (mx > 16 && mx < 24)
				{
					settings.random_names ^= 4;
					Play_SoundFX(WAV_LOCK,0);
					upd = 1;
				}
			}
		}
		if (upd)
		{
			upd = 0;
			prep_screen();
			ik_copybox(bg, screen, 0, 0, 640, 480, 0,0);

			y = by+16;
			interface_drawborder(screen, bx, by, bx+240, by+h, 1, STARMAP_INTERFACE_COLOR, "Start new adventure");
			ik_print(screen, font_6x8, bx+16, y+=8, 0, textstring[STR_STARTGAME_IDENTIFY]);
			ik_print(screen, font_6x8, bx+16, y+=8, 0, textstring[STR_STARTGAME_CAPTAIN]);
			ik_print(screen, font_6x8, bx+70, y, 3, settings.captname);
			if (!(cn&1))
				ik_print(screen, font_4x8, bx+216-strlen(textstring[STR_STARTGAME_RENAME])*4, y, 3, textstring[STR_STARTGAME_RENAME]);
			ik_dsprite(screen, bx+216, y, spr_IFslider->spr[8+(settings.random_names&1)], 2+((3-3*(settings.random_names&1))<<8));

			ik_print(screen, font_6x8, bx+16, y+=8, 0, textstring[STR_STARTGAME_STARSHIP]);
			ik_print(screen, font_6x8, bx+70, y, 3, settings.shipname);
			if (!(cn&2))
				ik_print(screen, font_4x8, bx+216-strlen(textstring[STR_STARTGAME_RENAME])*4, y, 3, textstring[STR_STARTGAME_RENAME]);
			ik_dsprite(screen, bx+216, y, spr_IFslider->spr[8+(settings.random_names&2)/2], 2+((3-3*(settings.random_names&2)/2)<<8));

			ik_print(screen, font_6x8, bx+16, y+=16, 0, textstring[STR_STARTGAME_LOADOUT], textstring[STR_STARTGAME_LOADOUT1+settings.dif_ship]);
			y += 8;
			for (c = 0; c < 3; c++)
			{
				ik_dsprite(screen, bx+16+c*72, y, spr_IFdifenemy->spr[c+3], 0);
				ik_dsprite(screen, bx+16+c*72, y, spr_IFborder->spr[20], 2+(3<<8)*(c==settings.dif_ship));
			}

			ik_print(screen, font_6x8, bx+16, y+=40, 0, textstring[STR_STARTGAME_NEBULA]);
			y += 8;
			for (c = 0; c < 3; c++)
			{
				ik_dsprite(screen, bx+16+c*72, y, spr_IFdifnebula->spr[c], 0);
				ik_dsprite(screen, bx+16+c*72, y, spr_IFborder->spr[18], 2+(3<<8)*(c==settings.dif_nebula));
			}

			ik_print(screen, font_6x8, bx+16, y+=72, 0, textstring[STR_STARTGAME_ENEMIES]);
			y += 8;
			for (c = 0; c < 3; c++)
			{
				ik_dsprite(screen, bx+16+c*72, y, spr_IFdifenemy->spr[c], 0);
				ik_dsprite(screen, bx+16+c*72, y, spr_IFborder->spr[20], 2+(3<<8)*(c==settings.dif_enemies));
			}

			y+=40;
			ik_print(screen, font_6x8, bx+16, y, 0, textstring[STR_STARTGAME_EASY]);
			ik_print(screen, font_6x8, bx+224-6*strlen(textstring[STR_STARTGAME_HARD]), y, 0, textstring[STR_STARTGAME_HARD]);
			ik_print(screen, font_6x8, bx+16, y+12, 0, textstring[STR_STARTGAME_LOSCORE]);
			ik_print(screen, font_6x8, bx+224-6*strlen(textstring[STR_STARTGAME_HISCORE]), y+12, 0, textstring[STR_STARTGAME_HISCORE]);
			interface_drawslider(screen, bx+56, y, 0, 128, 4, settings.dif_enemies+settings.dif_nebula, STARMAP_INTERFACE_COLOR);

			y+=24;
			ik_dsprite(screen, bx+12, y-5, spr_IFbutton->spr[(settings.random_names&4)>0], 0);
			ik_print(screen, font_6x8, bx+32, y, 0, "TUTORIAL MODE");


			interface_drawbutton(screen, bx+16, by+h-24, 48, STARMAP_INTERFACE_COLOR, textstring[STR_CANCEL]);
			interface_drawbutton(screen, bx+240-64, by+h-24, 48, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);

			ik_blit();
			update_palette();
		}
	}

	interface_cleartuts();

	if (must_quit)
		end = 1;

	if (settings.opt_mucrontext & 1)
	{


	if (end > 1)
	{
		bx = 192; by = 72; h = 328;
		by = 220 - h/2;

		prep_screen();
		ik_copybox(bg, screen, 0, 0, 640, 480, 0,0);

		y = 3;
		interface_drawborder(screen, bx, by, bx+256, by+h, 1, STARMAP_INTERFACE_COLOR, textstring[STR_STARTGAME_TITLE1]);
		y +=  1 + interface_textbox(screen, font_6x8, bx+84, by+y*8, 160, 88, 0, 
								textstring[STR_STARTGAME_MUCRON1]);
		y +=  1 + interface_textbox(screen, font_6x8, bx+16, by+y*8, 224, 88, 0, 
								textstring[STR_STARTGAME_MUCRON2]);
		y +=  1 + interface_textbox(screen, font_6x8, bx+16, by+y*8, 224, 88, 0, 
								textstring[STR_STARTGAME_MUCRON3]);
		y +=  1 + interface_textbox(screen, font_6x8, bx+16, by+y*8, 224, 88, 0, 
								textstring[STR_STARTGAME_MUCRON4]);
		interface_drawbutton(screen, bx+256-64, by+h-24, 48, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);
		ik_dsprite(screen, bx+16, by+24, spr_SMraces->spr[race_unknown], 0);
		ik_dsprite(screen, bx+16, by+24, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));

		ik_blit();
		update_palette();
		end = waitclick(bx+256-64, by+h-24, bx+256-16, by+h-8);
	}

	if (end > 1)
	{
		bx = 192; by = 96; h = 168;
		by = 220 - h/2;

		prep_screen();
		ik_copybox(bg, screen, 0, 0, 640, 480, 0,0);

		y = 3;
		interface_drawborder(screen, bx, by, bx+256, by+h, 1, STARMAP_INTERFACE_COLOR, textstring[STR_STARTGAME_TITLE2]);
		y +=  1 + interface_textbox(screen, font_6x8, bx+84, by+y*8, 160, 88, 0, 
								textstring[STR_STARTGAME_MUCRON5]);
		y +=  1 + interface_textbox(screen, font_6x8, bx+16, by+y*8, 224, 88, 0, 
								textstring[STR_STARTGAME_MUCRON6]);
		interface_drawbutton(screen, bx+256-64, by+h-24, 48, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);
		ik_dsprite(screen, bx+16, by+24, spr_IFdifnebula->spr[1], 0);
		ik_dsprite(screen, bx+16, by+24, hulls[shiptypes[0].hull].sprite, 0);
		ik_dsprite(screen, bx+16, by+24, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));

		ik_blit();
		update_palette();
		end = waitclick(bx+256-64, by+h-24, bx+256-16, by+h-8);
	}



	if (end > 1)
	{
		bx = 192; by = 120; h = 112;
		by = 220 - h/2;

		prep_screen();
		ik_copybox(bg, screen, 0, 0, 640, 480, 0,0);

		y = 3;
		interface_drawborder(screen, bx, by, bx+256, by+h, 1, STARMAP_INTERFACE_COLOR, textstring[STR_STARTGAME_TITLE3]);
		y +=  1 + interface_textbox(screen, font_6x8, bx+84, by+y*8, 160, 88, 0, 
								textstring[STR_STARTGAME_MUCRON7]);
		interface_drawbutton(screen, bx+256-64, by+h-24, 48, STARMAP_INTERFACE_COLOR, textstring[STR_OK]);
		ik_dsprite(screen, bx+16, by+24, spr_SMraces->spr[RC_PLANET], 4);
		ik_dsprite(screen, bx+16, by+24, spr_IFborder->spr[18], 2+(STARMAP_INTERFACE_COLOR<<8));

		ik_blit();
		update_palette();
		end = waitclick(bx+256-64, by+h-24, bx+256-16, by+h-8);
	}

	}

	del_image(bg);

	if (end > 1)
	{
		starmap_create();
		player_init();
	}

	saveconfig();
	
	return end-1;
}

int32 waitclick(int left, int top, int right, int bottom)
{
	int end = 0;
	int c, mc;
	int t;

	t = get_ik_timer(2);
	while (!end && !must_quit)
	{
		ik_eventhandler();
		c = ik_inkey();
		mc = ik_mclick();
		if (c==13 || c==32)
		{	end = 2; Play_SoundFX(WAV_DOT); }

		if (mc & 1)
		{
			if (ik_mouse_x >= left && ik_mouse_x < right && ik_mouse_y >= top && ik_mouse_y < bottom)
			{	end = 2; Play_SoundFX(WAV_DOT); }
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

void loadconfig()
{
	FILE *cfg;

	cfg = myopen("settings.dat", "rb");
	if (!cfg)
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

	fread(&settings, sizeof(t_gamesettings), 1, cfg);
	fclose(cfg);
	s_volume = settings.opt_volume * 10;
	settings.opt_mousemode &= 1;
}

void saveconfig()
{
	FILE *cfg;

	cfg = myopen("settings.dat", "wb");
	if (!cfg)
		return;
	fwrite(&settings, sizeof(t_gamesettings), 1, cfg);
	fclose(cfg);
}
#include <stdlib.h>
#include <stdio.h>
#include <string.h>

#include "typedefs.h"
#include "iface_globals.h"
#include "is_fileio.h"

#include "textstr.h"

char *textbuffer;	
char *textstring[STR_MAX];

void textstrings_init()
{
	FILE* ini;
	char s1[64], s2[1024];
	char end;
	int num;
	int flag;
	char *str;

	ini = myopen("gamedata/strings.ini", "rb");
	if (!ini)
		return;

	textbuffer = (char *)calloc(STRINGBUFFER_SIZE,sizeof(char));

	str = textbuffer;
	end = 0; num = 0; flag = 0;
	while (!end)
	{
		end = read_line(ini, s1, s2);
		if (!strcmp(s1, "STRINGS"))
			flag=1;
		else if (!strcmp(s1, "END"))
			flag = 0;
		else if (flag)
		{
			strcpy(str, s2);
			textstring[num]=str;
			str+=strlen(s2)+1;
			num++;
		}
	}
	fclose(ini);

}

void textstrings_deinit()
{
	free(textbuffer);
}
/*
All DIRECTDRAW stuff here

- DDraw init/shutdown

*/

#define WIN32_LEAN_AND_MEAN  

#include <windows.h>   // include important windows stuff
#include <windowsx.h> 
#include <mmsystem.h>
//#include <iostream.h> // include important C/C++ stuff
#include <iostream> // include important C/C++ stuff
#include <conio.h>
#include <stdlib.h>
#include <malloc.h>
#include <memory.h>
#include <string.h>
#include <stdarg.h>
#include <stdio.h>
#include <math.h>
#include <io.h>
#include <fcntl.h>
#include <SDL.h>

//#include <ddraw.h>  // directX includes

#include "typedefs.h"
#include "iface_globals.h"
#include "gfx.h"

// DEFINES

#define SCREEN_BPP    8                // bits per pixel

// GLOBALS

extern SDL_Surface *sdlsurf;
UCHAR *drawbuffer;  // video buffer
int drawpitch;      // line pitch

#ifdef MOVIE
int when = 0;
int movrecord = 1;
#endif

// blit screen
void ik_blit()
{
	t_ik_sprite *cs;

	// take screenshots here (!)
#ifdef MOVIE
	if (get_ik_timer(2) > when && movrecord == 1)
	{
		when += 2;
		wants_screenshot = 1;
	}
#endif
	if (wants_screenshot)
	{
		ik_save_screenshot(screen, currentpal);
		wants_screenshot=0;
	}

	if ((settings.opt_mousemode&5)==0)
	{
		cs = get_sprite(screen, ik_mouse_x, ik_mouse_y, 16, 16);
		ik_draw_mousecursor();
	}
	else if (settings.opt_mousemode & 4)
	{
//		cs = get_sprite(screen, ik_mouse_x-128, ik_mouse_y-128, 256, 256);
		cs = get_sprite(screen, ik_mouse_x-192, ik_mouse_y-96, 384, 192);
		gfx_magnify();
		if (!(settings.opt_mousemode & 1))
		{
			ik_draw_mousecursor();
		}
	}

	SDL_UpdateRect(sdlsurf, 0, 0, 640, 480);
	SDL_Flip(sdlsurf);

	if ((settings.opt_mousemode&5)==0)
	{
		prep_screen();
		ik_dsprite(screen, ik_mouse_x, ik_mouse_y, cs, 4);
		free_screen();
		free_sprite(cs);
	}
	else if (settings.opt_mousemode & 4)
	{
		prep_screen();
//		ik_dsprite(screen, ik_mouse_x-128, ik_mouse_y-128, cs, 4);
		ik_dsprite(screen, ik_mouse_x-192, ik_mouse_y-96, cs, 4);
		free_screen();
		free_sprite(cs);
	}
}

// palette stuff
void update_palette()  
{
	SDL_Color spal[256];
	int i;

	for (i = 0; i < 256; i++)
	{
		spal[i].r = currentpal[i*3];
		spal[i].g = currentpal[i*3+1];
		spal[i].b = currentpal[i*3+2];
	}
	SDL_SetColors(sdlsurf, spal, 0, 256);
}

void set_palette_entry(int n, int r, int g, int b)
{
	currentpal[n*3]		= r;
	currentpal[n*3+1]	= g;
	currentpal[n*3+2]	= b;
}

int get_palette_entry(int n)
{
	return currentpal[n*3]*65536 + 
				 currentpal[n*3+1]*256 +
				 currentpal[n*3+2];
}

void prep_screen() // call before drawing stuff to *screen
{
	SDL_LockSurface(sdlsurf);

	screenbuf.data=(uint8*)sdlsurf->pixels;
	screenbuf.w=sdlsurf->w;
	screenbuf.h=sdlsurf->h;
	screenbuf.pitch=sdlsurf->pitch;
	screen=&screenbuf;
}

void free_screen() // call after drawing, before blit
{
	SDL_UnlockSurface(sdlsurf);
}

int gfx_checkswitch()
{
	/*
	// implement windowed/fullscreen switch here?
	*/
	return 0;
}


#define INITGUID

#include <windows.h>   // include important windows stuff
#include <windowsx.h> 
#include <mmsystem.h>
#include <objbase.h>
//#include <iostream.h> // include important C/C++ stuff
#include <iostream> // include important C/C++ stuff
#include <conio.h>
#include <stdlib.h>
#include <malloc.h>
#include <memory.h>
#include <string.h>
#include <stdarg.h>
#include <stdio.h>
#include <math.h>
#include <io.h>
#include <fcntl.h>
#include <SDL.h>
#include <SDL_mixer.h>

#include <dsound.h>

#include "typedefs.h"
#include "iface_globals.h"
#include "snd.h"
#include "gfx.h"




// GLOBALS ////////////////////////////////////////////////

t_sfxchannel sfxchan[NUM_SFX];
t_wavesound wavesnd[WAV_MAX];
// channels are ones actually being played
// samples are sitting in the memory and cloned into channels when needed


int sound_init()
{

	static int first_time = 1; // used to track the first time the function is entered

	// initialize the sound fx array
	for (int index=0; index<WAV_MAX; index++)
	{
		wavesnd[index].name[0] = 0;
		wavesnd[index].wave = NULL;
	} 

	// return sucess
	return(1);
}

Mix_Chunk *lsnd(int32 name)
{
	Mix_Chunk *wave;

	wave = (Mix_Chunk*)wavesnd[name].wave;
	if (!wave)
	{
		wavesnd[name].wave = Mix_LoadWAV(wavesnd[name].name);
		wave = (Mix_Chunk*)wavesnd[name].wave;
	}

	return wave;
}

int Load_WAV(char *filename, int id)
{
	sprintf(wavesnd[id].name, filename);
	lsnd(id);
	return id;
}

///////////////////////////////////////////////////////////

int Play_Sound(int id, int ch, int flags, int volume, int rate, int pan)
{
	// this function plays a sound thru a channel, set flags to make it loop..
	if (flags)
		flags=9999;

	Stop_Sound(ch);
	Mix_PlayChannel(ch, lsnd(id), flags);

	if (volume>=0) Set_Sound_Volume(ch, volume);
		else Set_Sound_Volume(ch, 100);
//	if (rate>=0) Set_Sound_Freq(ch, rate);
//		else Set_Sound_Freq(ch, sound_samples[id].rate);
	if (pan) Set_Sound_Pan(ch, pan);
		else Set_Sound_Pan(ch, 0);

	// return success
	return(1);
}


int Play_SoundFX(int id, int t, int volume, int rate, int pan, int cutoff)
{
	int x;
	int ch,tt,ch0;
	int l;
	Mix_Chunk *chunk;

	t = get_ik_timer(2);

	ch=-1;tt=cutoff;ch0=-1;
	for (x=0;x<NUM_SFX;x++)
	{
		if (sfxchan[x].id==id && sfxchan[x].st<t-10)
		{	ch=x; break; }
		if (sfxchan[x].id==-1)
		{ ch=x; break; }
		if (t>sfxchan[x].et)
		{	ch=x; break; }
	}
	if (ch==-1)
	for (x=0;x<NUM_SFX;x++)
	{
		if (sfxchan[x].et-t<tt)
		{
			ch0=x; tt=sfxchan[x].et-t;
			break;
		}
	}
	if (ch==-1 && ch0>-1)
	{
		ch=ch0;
	}

	if (ch>-1)
	{
		chunk = lsnd(id);
		if (chunk)
		{
			l = chunk->alen;
			sfxchan[ch].id=id;
			sfxchan[ch].st=t;
			if (rate==-1) rate=Get_Sound_Rate(id);
			sfxchan[ch].et=t+(l*50)/22050;
			Play_Sound(id, ch+CHN_SFX, 0, volume, rate, pan);
		}
	}
	
//	Play_Sound(id, CHN_SFX, 0, volume, rate, pan);

	return 1;
}


///////////////////////////////////////////////////////////

int Set_Sound_Volume(int ch,int vol)
{
	// this function sets the volume on a sound 0-100
	vol = (vol * s_volume * 128) / 10000;
	
	Mix_Volume(ch, vol);

	// return success
	return(1);
}

///////////////////////////////////////////////////////////

int Set_Sound_Freq(int ch,int freq)
{
	// this function sets the playback rate
	// ... not really

	return(1);
}

///////////////////////////////////////////////////////////

int Set_Sound_Pan(int ch,int pan)
{
	int lf, rt;
	// this function sets the pan, -10,000 to 10,000

	if (pan < 0)
	{
		lf = 255; rt = (int)(255 - sqrt(-pan)*2);
	}
	else
	{
		rt = 255; lf = (int)(255 - sqrt(pan)*2);
	}

	Mix_SetPanning(ch, lf, rt);

	return(1);
}

////////////////////////////////////////////////////////////

int Stop_All_Sounds(void)
{
	for (int index=0; index<16; index++)
		Stop_Sound(index);	
	for (int x=0; x<NUM_SFX; x++)
	{ sfxchan[x].et=0; sfxchan[x].st=0; sfxchan[x].id=-1; }

	return(1);
}

///////////////////////////////////////////////////////////

int Stop_Sound(int ch)
{
	Mix_HaltChannel(ch);

	return(1);
} 

///////////////////////////////////////////////////////////

int Delete_All_Sounds(void)
{
	for (int index=0; index < WAV_MAX; index++)
		Delete_Sound(index);

	return(1);
} 

///////////////////////////////////////////////////////////

int Delete_Sound(int id)
{
	if (wavesnd[id].wave)
	{
		Mix_FreeChunk((Mix_Chunk*)wavesnd[id].wave);
		wavesnd[id].wave = NULL;
		return(1);
  } 
	
	return(1);
} 

///////////////////////////////////////////////////////////

int Status_Sound(int ch)
{
	if (Mix_Playing(ch))
		return 1;
	return 0;
} 

int Get_Sound_Size(int id)
{
	Mix_Chunk *chunk;
	if (wavesnd[id].wave)
	{
		chunk = (Mix_Chunk*)wavesnd[id].wave;
		return chunk->alen;
	}

	return 0;
}

int Get_Sound_Rate(int id)
{
	return 22050;
}
