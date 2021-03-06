// ----------------
//     TYPEDEFS
// ----------------

typedef struct _t_eventcard
{
	char name[32];
	char text[256];
	char text2[256];
	int32 type;
	int32 parm;
} t_eventcard;

enum ecard_keyids
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

enum ecard_types
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

extern t_eventcard		*ecards;
extern int32					num_ecards;

// ----------------
//    PROTOTYPES
// ----------------

void cards_init();
void cards_deinit();
void card_display(int n);
//#define DEBUG_COMBAT

//#define COMBAT_BUILD_HELP

// ----------------
//		CONSTANTS
// ----------------

enum combat_tactics
{
	tac_pursue=0,
	tac_attack,
	tac_move,
	tac_flee,
};

#define MAX_COMBAT_SHIPS 32
#define MAX_COMBAT_PROJECTILES 256
#define MAX_COMBAT_BEAMS 256
#define MAX_COMBAT_EXPLOS 256

#ifndef MOVIE
#define COMBAT_FRAMERATE 50
#else
#define COMBAT_FRAMERATE 17
#endif

#define COMBAT_INTERFACE_COLOR (11+simulated)

// ----------------
//     TYPEDEFS
// ----------------

typedef struct _t_hardpoint
{
	int8 type;
	int8 size;
	int8 x;
	int8 y;
	int16 a;		// angle
	int16 f;		// field of vision / fire
} t_hardpoint;

typedef struct _t_hull
{
	char name[32];
	int32 size;			// length in meters
	int32 hits;
	int32 mass;
	int32 numh;			// number of hardpoints
	t_ik_sprite *sprite;
	t_ik_sprite *silu;
	t_hardpoint hardpts[32];
} t_hull;

typedef struct _t_shiptype
{
	char name[32];
	int32 race;
	int32 flag;
	int32 hull;
//	int32 shield;
	int32 hits;
	int32 engine;
	int32 thrust;
	int32 speed;
	int32 turn;
	int32 sensor;
	int32 num_systems;
	int32 sys_eng, sys_thru;
//	int32 weapon[8];
	int16 system[16];
	int16 sysdmg[16];
} t_shiptype;

typedef struct _t_ship
{
	char name[32];
	int32 type;
	int32 hits;

	int32 shld;
	int32 shld_type;
	int32 shld_time;
	int32 shld_charge;

	int32 damage_time;
	int32	dmgc_type;
	int32 dmgc_time;

	int32 cpu_type;
	int32 ecm_type;
	int32 clo_type;

	int32 sys_thru;
	int32 sys_shld;
	int32 sys_dmgc;
	int32 sys_cpu;
	int32 sys_ecm;
	int32 sys_clo;

	int32 speed, turn;

	int32 wepfire[8];
	int32 syshits[16];
	int32 own;

	int32 x, y, a;			// location
	int32 vx, vy, va;		// movement

	int32 ds_x, ds_y, ds_s;		// display x, y, size (for mouse clicking)
	int32 wp_x, wp_y, escaped, wp_time, flee;
	int32 patx, paty;	// patrol

	int32 cloaked, cloaktime;	// cloaktime is last time you cloaked/decloaked

	int32 tel_x, tel_y, teltime;	// teleportation of zorg

	int32 active;	// for spacehulk
	int32 aistart;

	int32 target;
	int32 tac;
	int32 angle;
	int32 dist;

	int32 launchtime;	// for carrier
	int32 frange;

	int32 bong_start, bong_end;	// for babulon's bong artifact

} t_ship;

typedef struct _t_shipweapon
{
	char name[32];
	int32 stage;
	int32 type;
	int32 flags;
	t_ik_sprite *sprite;
	int32 size;
	int32 sound1;
	int32 sound2;
	int32 rate;
	int32 speed;
	int32 damage;
	int32 range;
	int32 item;
} t_shipweapon;

typedef struct _t_shipsystem
{
	char name[32];
	int16 type;
	int16 size;
	int32 par[4];
	int32 item;
} t_shipsystem;

typedef struct _t_wepbeam
{
	t_shipweapon *wep;
	t_ship *src;
	t_ship *dst;

	int32 stg;					// if staged from a projectile
	int32 ang, len;			// angle and length if missed
	int32 stp, dsp;			// start, destination hardpoint (-1 = hull / shield)
	int32 str, dmt, end;			// start, damage, expire time
} t_wepbeam;

typedef struct _t_wepproj
{
	t_shipweapon *wep;
	t_ship *src;
	t_ship *dst;				// target (for missiles)

	int32 str, end;			// start, expire time

	int32 x, y, a;			// location
	int32 vx, vy, va;		// movement
	int32 hits;					// used for dispersing weapons
} t_wepproj;

typedef struct _t_explosion
{
	t_ik_spritepak *spr;

	int32 x, y, a;
	int32 vx, vy, va;
	int32 str, end;
	int32 size, zoom;
	int32 spin, fade;
	int32 anim;
	int32 cam;
} t_explosion;

typedef struct _t_combatcamera
{
	int32 x, y, z;
	int32 ship_sel;
	int32 ship_trg;
	int32 time_sel;
	int32 time_trg;
	int32 drag_trg;
} t_combatcamera;


enum hull_keyids
{
	hlkBegin,
	hlkName,
	hlkSize,
	hlkHits,
	hlkMass,
	hlkSprite,
	hlkSilu,
	hlkWeapon,
	hlkEngine,
	hlkThruster,
	hlkFighter,
	hlkEnd,
	hlkMax,
};

enum hull_hardpttypes
{
	hdpWeapon,
	hdpEngine,
	hdpThruster,
	hdpFighter,
	hdpMax,
};

enum shiptype_keyids
{
	shkBegin,
	shkName,
	shkRace,
	shkFlag,
	shkHull,
	shkArmor,
	shkSystem,
	shkEngine,
	shkThruster,
	shkWeapon,
	shkEnd,
	shkMax,
};

enum weapon_keyids
{
	wpkBegin,
	wpkName,
	wpkStage,
	wpkType,
	wpkFlag,
	wpkSprite,
	wpkSize,
	wpkSound1,
	wpkSound2,
	wpkRate,
	wpkSpeed,
	wpkDamage,
	wpkRange,
	wpkEnd,
	wpkMax,
};

enum weapon_flagids
{
	wpfTrans = 1,
	wpfSpin = 2,
	wpfDisperse = 4,
	wpfImplode = 8,
	wpfHoming = 16,
	wpfSplit = 32,
	wpfShock1 = 64,
	wpfShock2 = 128,
	wpfNova = 256,
	wpfWiggle = 512,
	wpfStrail = 1024,
	wpfNoclip = 2048,
	wpfMax = 12,
};

enum system_keyids
{
	sykBegin,
	sykName,
	sykType,
	sykSize,
	sykParam1,
	sykParam2,
	sykParam3,
	sykParam4,
	sykEnd,
	sykMax,
};

enum system_types
{
	sys_weapon,
	sys_thruster,
	sys_engine,
	sys_shield,
	sys_computer,
	sys_ecm,
	sys_sensor,
	sys_damage,
	sys_misc,
};

enum race_keyids
{
	rckBegin,
	rckName,
	rckText,
	rckText2,
	rckEnd,
	rckMax,
};

enum race_ids
{
	race_none,
	race_terran,
	race_klakar,
	race_zorg,
	race_muktian,
	race_garthan,
	race_tanru,
	race_urluquai,
	race_kawangi,
	race_unknown,
	race_drone,
	race_max,
};



// ----------------
// GLOBAL VARIABLES
// ----------------

extern int IsMinimized;


extern t_ik_image			*combatbg1;
extern t_ik_image			*combatbg2;

extern t_ik_spritepak *spr_ships;
extern t_ik_spritepak *spr_shipsilu;
extern t_ik_spritepak *spr_weapons;
extern t_ik_spritepak *spr_explode1;
extern t_ik_spritepak *spr_shockwave;
extern t_ik_spritepak	*spr_shield;

extern t_hull					*hulls;
extern int						num_hulls;

extern t_shiptype			*shiptypes;
extern int						num_shiptypes;

extern t_shipweapon		*shipweapons;
extern int						num_shipweapons;

extern t_shipsystem		*shipsystems;
extern int						num_shipsystems;

extern t_ship					*ships;		// in combat
extern int						num_ships;

extern char						racename[16][32];
extern int						num_races;
extern int						enemies[16];
extern int						num_enemies;

extern t_combatcamera		camera;
extern t_ship						cships[MAX_COMBAT_SHIPS];
extern t_wepbeam					cbeams[MAX_COMBAT_BEAMS];
extern t_wepproj					cprojs[MAX_COMBAT_PROJECTILES];
extern t_explosion				cexplo[MAX_COMBAT_EXPLOS];

extern int32 numships;
extern int32 playership;
extern int32 sortship[MAX_COMBAT_SHIPS];

extern int32 t_move, t_disp, pause;

extern int32 nebula;
extern int32 retreat;
extern int32 klaktime;
extern int32 klakavail;
extern int32 gongavail;

extern int32 simulated;

#ifdef DEBUG_COMBAT
extern char combatdebug[64];
#endif

// ----------------
//    PROTOTYPES
// ----------------

// combat_init.cpp

void combat_init();
void combat_deinit();
void sort_shiptype_systems(int32 num);

// combat.cpp

int32 combat(int32 flt, int32 sim);
void select_ship(int32 s, int32 t);
void combat_updateshipstats(int32 s, int32 t);
void combat_findstuff2do(int32 s, int32 t);
void combat_help_screen();
void combat_SoundFX(int id, int srcx = camera.x, int volume = -1, int rate = -1);

// combat_sim.cpp

void combat_sim();
void combat_sim_end();

// combat_display.cpp

void combat_autocamera(int32 t);
void combat_display(int32 t);

// combat_weapons.cpp

int32 combat_findtarget(t_ship *ship, int32 hdp);
void combat_fire(t_ship *src, int32 hdp, t_ship *trg, int32 start);
int32 combat_addbeam(t_shipweapon *wep, t_ship *src, int32 hdp, t_ship *trg, int32 start, int32 stg = -1);
int32 combat_addproj(t_ship *src, int32 hdp, t_ship *trg, int32 start);
void combat_launchstages(int32 p, int32 num, int32 start);
int32 combat_addexplo(int32 x, int32 y, t_ik_spritepak *spr, int32 spin, int32 size, int32 zoom, int32 start, int32 end, int32 anim=-1, int32 cam=1);
void combat_damageship(int32 s, int32 src, int32 dmg, int32 t, t_shipweapon *wep, int32 deb=0);
void combat_gethardpoint(t_ship *ship, int32 hdp, int32 *rx, int32 *ry);
void combat_killship(int32 s, int32 t, int32 quiet=0);
// ----------------
//    CONSTANTS
// ----------------

// ----------------
//     TYPEDEFS
// ----------------

typedef struct _t_job
{
	char name[64];	
	int32 value;
} t_job;

typedef struct _t_score
{
	char cname[16], sname[16];
	char deathmsg[64];
	int32 score;
	int32 date;
} t_score;

// ----------------
// GLOBAL VARIABLES
// ----------------

extern t_job		*jobs;
extern int32		num_jobs;

extern int32    num_scores;
extern t_score  scores[20];

extern int32		got_hiscore;

// ----------------
//    PROTOTYPES
// ----------------

void game_over();

void endgame_init();
void endgame_deinit();
//#define MOVIE

// ******** GRAPHICS *********

// GFX DATATYPES

typedef struct
{
	uint8 r;
	uint8 g;
	uint8 b;
	uint8 a;
} t_paletteentry;

typedef struct {
	int32 w,h;		// size
	int32 pitch;  // how many bytes per hline
	uint8 *data;	// linear bitmap
} t_ik_image;

typedef struct {
	int32 w,h;    // size
	uint32 co;     // average color
	uint8 *data;	// linear bitmap 
} t_ik_sprite;

typedef struct {
	int32 num;
	t_ik_sprite **spr;
} t_ik_spritepak;

typedef struct {
	uint16 w,h;		// size
	uint8 *data;	// linear bitmap 
} t_ik_font;

// GFX GLOBALS

extern t_ik_image *screen;
extern uint8 globalpal[768];
extern uint8 currentpal[768];
extern t_ik_image screenbuf;
extern int gfx_width, gfx_height, gfx_fullscreen, gfx_switch;
extern int gfx_redraw;
extern int c_minx, c_miny, c_maxx, c_maxy;

extern unsigned char *gfx_transbuffer;
extern unsigned char *gfx_lightbuffer;
extern unsigned char *gfx_addbuffer;

extern int32 sin1k[1024];
extern int32 cos1k[1024];

// load, generate or delete images
t_ik_image *new_image(int32 w, int32 h);
void del_image(t_ik_image *img);
t_ik_image *ik_load_pcx(char *fname, uint8 *pal);
t_ik_image *ik_load_tga(char *fname, uint8 *pal);
void ik_save_screenshot(t_ik_image *img, uint8 *pal);
void ik_save_tga(char *fname, t_ik_image *img, uint8 *pal);

// input/output
void ik_setclip(int32 left, int32 top, int32 right, int32 bottom);
void ik_putpixel(t_ik_image *img, int32 x, int32 y, uint32 c);
int32 ik_getpixel(t_ik_image *img, int32 x, int32 y);
uint8 *ik_image_pointer(t_ik_image *img, int32 x, int32 y);
void ik_drawline(t_ik_image *img, int32 xb, int32 yb, int32 xe, int32 ye, int32 c1, int32 c2=0, uint8 mask=255, uint8 fx=0);
void ik_drawbox(t_ik_image *img, int32 xb, int32 yb, int32 xe, int32 ye, int32 c);
void ik_copybox(t_ik_image *src, t_ik_image *dst, int32 xb, int32 yb, int32 xe, int32 ye, int32 xd, int32 yd);
void ik_drawmeter(t_ik_image *img, int32 xb, int32 yb, int32 xe, int32 ye, int32 typ, int32 val, int32 c, int32 c2);
void ik_draw_mousecursor();
void gfx_blarg();
void gfx_magnify();

// screen blits & other management
void prep_screen(); // call before drawing stuff to *screen
void free_screen(); // call after drawing, before blit
void ik_blit();         // blit from memory to hardware
int gfx_checkswitch();  // check for gfx mode switch
void halfbritescreen();
void reshalfbritescreen();
void resallhalfbritescreens();

// palette handling
void update_palette();  // blit palette entries to hardware
void set_palette_entry(int n, int r, int g, int b);
int get_palette_entry(int n);
int32 get_rgb_color(int32 r, int32 g, int32 b);
void calc_color_tables(uint8 *pal);
void del_color_tables();

// misc
int get_direction(int32 dx, int32 dy);
int get_distance(int32 dx, int32 dy);

void gfx_initmagnifier();
void gfx_deinitmagnifier();



// ------------------------
//         FONT.CPP
// ------------------------

t_ik_font *ik_load_font(char *fname, uint8 w,  uint8 h);
void ik_del_font(t_ik_font *fnt);

void ik_print(t_ik_image *img, t_ik_font *fnt, int32 x, int32 y, uint8 co, char *ln, ...);
void ik_printbig(t_ik_image *img, t_ik_font *fnt, int32 x, int32 y, uint8 co, char *ln, ...);
//void ik_text_input(int x, int y, int l, t_ik_font *fnt, char *tx);
void ik_text_input(int x, int y, int l, t_ik_font *fnt, char *pmt, char *tx, int bg=0, int co=0);
void ik_hiscore_input(int x, int y, int l, t_ik_font *fnt, char *tx);

// ------------------------
//      SPRITES.CPP
// ------------------------

// sprite management
t_ik_sprite *			new_sprite(int32 w, int32 h);
void							free_sprite(t_ik_sprite *spr);

t_ik_sprite *			get_sprite(t_ik_image *img, int32 x, int32 y, int32 w, int32 h);
int32							calc_sprite_color(t_ik_sprite *spr);

t_ik_spritepak *	new_spritepak(int32 num);
void							free_spritepak(t_ik_spritepak *pak);

t_ik_spritepak *	load_sprites(char *fname);
void							save_sprites(char *fname, t_ik_spritepak *pak);


// sprite drawing
void ik_dsprite(t_ik_image *img, int32 x, int32 y, t_ik_sprite *spr, int32 flags=0);
void ik_drsprite(t_ik_image *img, int32 x, int32 y, int32 r, int32 s, t_ik_sprite *spr, int32 flags=0);
void ik_dspriteline(t_ik_image *img, int32 xb, int32 yb, int32 xe, int32 ye, int32 s, 
										int32 offset, int32 ybits, t_ik_sprite *spr, int32 flags=0);
#define MIN(x,y)     (((x) < (y)) ? (x) : (y))
#define MAX(x,y)     (((x) > (y)) ? (x) : (y))
#define ABS(x)			 (((x) > 0) ? (x) : (0-x))

#define SAIS_VERSION_NUMBER "v1.5"

//#define WINDOWED_MODE

typedef struct _t_gamesettings
{
	int32 dif_nebula;
	int32 dif_enemies;
	int32 dif_ship;
	int32 random_names;
	int8 opt_timerwarnings;
	int8 opt_mucrontext;
	int8 opt_timeremaining;
	int8 opt_mousemode;
	int8 opt_smoketrails;
	int8 opt_lensflares;
	int16 opt_volume;
	char captname[32];
	char shipname[32];
} t_gamesettings;

extern t_gamesettings settings;

// ******** GENERAL STUFF *******

int my_main();
int ik_eventhandler();
int Game_Init(void *parms=NULL);
int Game_Shutdown(void *parms=NULL);

// inputs
int key_pressed(int vk_code);  // FIXME: GET RID OF VK CODES!
int ik_inkey();  // returns ascii
void ik_showcursor();
void ik_hidecursor();
int ik_mclick(); // returns flags when mbutton down

// timers
void start_ik_timer(int n, int f);
void set_ik_timer(int n, int v);
int get_ik_timer(int n);
int get_ik_timer_fr(int n);

// INTERFACE GLOBALS
extern int ik_mouse_x;
extern int ik_mouse_y;
extern int ik_mouse_b;
extern int ik_mouse_c;
extern int must_quit;
extern int wants_screenshot;

extern int key_left;
extern int key_right;
extern int key_up;
extern int key_down;
extern int key_f[10];
extern int key_fire1;
extern int key_fire2;
extern int key_fire2b;

// ----------------
//    CONSTANTS
// ----------------

// for interface.cpp / interface_initsprites()
#define IF_BORDER_TRANS 0
#define IF_BORDER_SOLID 9
#define IF_BORDER_PORTRAIT 18
#define IF_BORDER_RADAR 19
#define IF_BORDER_FLAT 20
#define IF_BORDER_SMALL 21

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

extern t_ik_spritepak *spr_titles;

extern t_ik_spritepak *spr_IFborder;
extern t_ik_spritepak *spr_IFbutton;
extern t_ik_spritepak *spr_IFslider;
extern t_ik_spritepak *spr_IFarrows;
extern t_ik_spritepak *spr_IFsystem;
extern t_ik_spritepak *spr_IFtarget;
extern t_ik_spritepak *spr_IFdifnebula;
extern t_ik_spritepak *spr_IFdifenemy;

extern t_ik_font	    *font_4x8;
extern t_ik_font	    *font_6x8;

// ----------------
//    PROTOTYPES
// ----------------

void interface_init();
void interface_deinit();

void interface_drawborder(t_ik_image *img, 
													int32 left, int32 top, int32 right, int32 bottom,
													int32 fill, int32 color,
													char *title);
void interface_thinborder(t_ik_image *img, 
													int32 left, int32 top, int32 right, int32 bottom,
													int32 color, int32 fill = -1);
int32 interface_textbox(t_ik_image *img, t_ik_font *fnt,
											 int32 left, int32 top, int32 w, int32 h,
											 int32 color, 
											 char *text);
int32 interface_textboxsize(t_ik_font *fnt,
														int32 w, int32 h,
														char *text);
int32 interface_popup(t_ik_font *fnt, 
										 int32 left, int32 top, int32 w, int32 h,
										 int32 co1, int32 co2, 
										 char *label, char *text,
										 char *button1 = NULL, char *button2 = NULL, char *button3 = NULL);
void interface_drawslider(t_ik_image *img, int32 left, int32 top, int32 a, int32 l, int32 rng, int32 val, int32 color);
void interface_drawbutton(t_ik_image *img, int32 left, int32 top, int32 l, int32 color, char *text);

void interface_cleartuts();
void interface_tutorial(int n);
FILE *myopen(const char *fname, const char *flags);
int read_line(FILE *in, char *out1, char *out2);
int read_line1(FILE *in, char *out1);
void ik_start_log();
void ik_print_log(char *ln, ...);

extern FILE *logfile;
extern int last_logdate;
extern char moddir[256];
//{{NO_DEPENDENCIES}}
// Microsoft Developer Studio generated include file.
// Used by Script1.rc
//
#define IDI_ICON1                       101

// Next default values for new objects
// 
#ifdef APSTUDIO_INVOKED
#ifndef APSTUDIO_READONLY_SYMBOLS
#define _APS_NEXT_RESOURCE_VALUE        102
#define _APS_NEXT_COMMAND_VALUE         40001
#define _APS_NEXT_CONTROL_VALUE         1000
#define _APS_NEXT_SYMED_VALUE           101
#endif
#endif
#define CHN_SFX 0
#define NUM_SFX 15


enum sfxsamples
{
	// combat
	WAV_BEAM1,
	WAV_BEAM2,
	WAV_BEAM3,
	WAV_BEAM4,
	WAV_BEAM5,
	WAV_BEAM6,
	WAV_BEAM7,
	WAV_PROJ1,
	WAV_PROJ2,
	WAV_PROJ3,
	WAV_PROJ4,
	WAV_PROJ5,
	WAV_PROJ6,
	WAV_PROJ7,
	WAV_PROJ8,
	WAV_PROJ9,
	WAV_PROJ10,
	WAV_PROJ11,
	WAV_HIT1,
	WAV_HIT2,
	WAV_HIT3,
	WAV_HIT4,
	WAV_HIT5,
	WAV_EXPLO1,
	WAV_EXPLO2,
	WAV_SHIELD,
	WAV_CLOAKIN,
	WAV_CLOAKOUT,
	WAV_BOARD,
	WAV_SYSDAMAGE,
	WAV_SYSHIT1,
	WAV_SYSHIT2,
	WAV_SYSFIXED,
	WAV_TELEPORT,
	WAV_FIERYFURY,
	WAV_FIGHTERLAUNCH,
	WAV_ENDSIMULATION,
	// interface
	WAV_YES,
	WAV_NO,
	WAV_ACCEPT,
	WAV_DECLINE,
	WAV_DOT,
	WAV_DOT2,
	WAV_SELECT,
	WAV_DESELECT,
	WAV_SELECTSTAR,
	WAV_INFO,
	WAV_SELECTSHIP,
	WAV_WAIT,
	WAV_SLIDER,
	WAV_INSTALL,
	WAV_INSTALL2,
	WAV_LOCK,
	WAV_DEPART,
	WAV_ARRIVE,
	WAV_HYPERDRIVE,
	WAV_FOLDSPACE,
	WAV_RADAR,
	WAV_SCANNER,
	WAV_BRIDGE,
	WAV_MESSAGE,
	WAV_TANRUMESSAGE,
	WAV_PAYMERC,
	WAV_TRADE,
	WAV_CASH,
	WAV_PROBE_LAUNCH,
	WAV_PROBE_DEST,
	WAV_FOMAX_HI,
	WAV_FOMAX_BYE,
	WAV_FOMAX_WISH,
	WAV_TIMER,
	WAV_WARNING,
	WAV_OPTICALS,
	WAV_TITLE1,
	WAV_TITLE2,
	WAV_TITLE3,
	WAV_TITLE4,
	WAV_TITLE5,
	WAV_LOGO,
	// races
	WAV_KLAKAR,
	WAV_ZORG,
	WAV_MUKTIAN,
	WAV_GARTHAN,
	WAV_TANRU,
	WAV_URLUQUAI,
	WAV_KAWANGI,
	// events
	WAV_BLACKHOLE,
	WAV_BLACKHOLEDEATH,
	WAV_COLLAPSER,
	// cards
	WAV_ALLY,
	WAV_FLARE,
	WAV_SPY,
	WAV_NOVA,
	WAV_SABOTEUR,
	WAV_WHALES,
	WAV_CUBE,
	WAV_SPACEHULK,
	WAV_GASGIANT,
	WAV_NOPLANET,
	// normal item categories
	WAV_WEAPON,
	WAV_SYSTEM,
	WAV_DEVICE,
	WAV_LIFEFORM,
	WAV_DRIVE,
	// artifacts
	WAV_PLATINUM,
	WAV_TITANIUM,
	WAV_BRASS,
	WAV_PLASTIC,
	WAV_CENOTAPH,
	WAV_TORC,
	WAV_GONG,
	WAV_MANTLE,
	WAV_WHISTLE,
	WAV_HORLOGE,
	WAV_TOY,
	WAV_CODEX,
	WAV_SCULPTURE,
	WAV_CONOGRAPH,
	WAV_MONOCLE,
	WAV_BAUBLE,
	WAV_MIRROR,
	WAV_MUMMY,
	WAV_MONOLITH,
	WAV_CONOGRAPH2,
	// music
	WAV_MUS_START,
	WAV_MUS_SPLASH,
	WAV_MUS_THEME,
	WAV_MUS_TITLE,
	WAV_MUS_DEATH,
	WAV_MUS_VICTORY,
	WAV_MUS_COMBAT,
	WAV_MUS_NEBULA,
	WAV_MUS_HISCORE,
	WAV_MUS_ROCK,
	WAV_MUS_SIMULATOR,
	WAV_MAX
};

#define SND_BEAMS WAV_BEAM1
#define SND_PROJS WAV_PROJ1
#define SND_HITS	WAV_HIT1
#define SND_ITEMS WAV_WEAPON
#define SND_ARTIF WAV_PLATINUM


typedef struct {
	char name[64];
	void *wave;
} t_wavesound;

typedef struct 
{
	int id;      // sample playing
	int st, et;  // start and end time
} t_sfxchannel;

extern t_sfxchannel sfxchan[NUM_SFX];
extern t_wavesound wavesnd[WAV_MAX];

// ******** SOUND *********

int Load_WAV(char *filename, int id);
void load_all_sfx(void);
int Delete_Sound(int id);
int Delete_All_Sounds(void);

int Play_Sound(int id, int ch, int flags=0,int volume=-1, int rate=-1, int pan=0);
int Play_SoundFX(int id, int t=0, int volume=-1, int rate=-1, int pan=0, int cutoff=30);
int Set_Sound_Volume(int ch,int vol);
int Set_Sound_Freq(int ch,int freq);
int Set_Sound_Pan(int ch,int pan);
int Stop_Sound(int ch);
int Stop_All_Sounds(void);
int Status_Sound(int ch);
int Get_Sound_Size(int id);
int Get_Sound_Rate(int id);

// ********* MUSIC ********

typedef struct 
{
	int8 volseq[64];
	int8 panseq[64];
	int8 sync[4];
	int8 samp[4];
} t_song;

extern int32 m_freq[4];
extern int8 m_mainvol;
extern int8 s_volume;
extern t_song m_song;
extern int8 m_playing;

void start_music();
void upd_music(int pos);
int m_get_pan(int ch, int pos);
int m_get_vol(int ch, int pos);

void save_cur_music(char *fname);
void load_cur_music(char *fname);
void prep_music(int n); // copy from songs[] to song
void plop_music(int n); // copy from song to songs[]
// ----------------
//    CONSTANTS
// ----------------

#define STARMAP_FRAMERATE 50

#define SM_MAP_X 160
#define SM_MAP_Y 0
#define SM_SHIP_X 0
#define SM_SHIP_Y 0
#define SM_INV_X 0
#define SM_INV_Y 256
#define SM_SEL_X 0
#define SM_SEL_Y 384

//#define STARMAP_BUILD_HELP
//#define LOG_OUTPUT

//#define STARMAP_DEBUGINFO 
//#define STARMAP_DEBUGENEMIES
//#define STARMAP_DEBUGTANRU
//#define STARMAP_DEBUGALLIES
//#define STARMAP_DEBUGEVENTS
//#define STARMAP_DEBUGFOLD
//#define STARMAP_DEBUGDEVICES
//#define STARMAP_DEBUGCLOAK
//#define STARMAP_DEBUGRAREITEMS
//#define STARMAP_STEPBYSTEP
//#define STARMAP_DEBUGSYSTEMS
//#define STARMAP_KAWANGI

#define STARMAP_INTERFACE_COLOR 11

#define STARMAP_MAX_FLEETS 8

#define RC_MUCRON 9
#define RC_PLANET 10
#define RC_FOMAX 11
#define RC_BLOWUP 12
#define RC_HOLED 13
#define RC_LOST 14

#define NUM_FLEETS		5
#define NUM_STARSYSTEMS 16
#define NUM_EVENTS		2
#define NUM_ALLIES		2
#define NUM_ITEMS			6
#define NUM_RAREITEMS	2
#define NUM_LIFEFORMS	3


// ----------------
//     TYPEDEFS
// ----------------

typedef struct _t_hud
{
	int32 invslider;
	int32 invselect;
	int32 sysslider;
	int32 sysselect;
} t_hud;


typedef struct _t_starsystem
{
	char starname[16];
	char planetname[16];
	int32 x, y;
	int32 color;
	int32 planet;
	int32 planetgfx;
	int32 novadate;
	int32 novatype;
	int32 novatime;

	int32 card;
	int32 explored;
	int32 ds_x, ds_y;
} t_starsystem;

typedef struct _t_planettype
{
	char name[16];
	char text[172];
	int32 bonus;
} t_planettype;

typedef struct _t_startype
{
	char name[16];
	char text[176];
} t_startype;

typedef struct _t_blackhole
{
	char name[20];
	int32 x, y;
	int16 size;
	int16 explored;
} t_blackhole;

typedef struct _t_nebula
{
	int32 x, y;
	int32 sprite;
} t_nebula;

typedef struct _t_player
{
	char captname[32];
	char shipname[32];
	char deathmsg[64];

	int32 x,y,a;
	int32 system;
	int32 target;

	int32 distance;
	int32 nebula;
	int32 enroute;

	int32 engage;
	int32 fold;
	int32 hypdate;
	int32 foldate;
	int32 hyptime;

	int32 explore;
	int32 stardate;	
	int32 death;
	int32 deatht;
	int32 hole;

	int32 num_ships;
	int32 ships[8];
	int32 sel_ship;
	int32 sel_ship_time;

	int32 items[32];
	int32 itemflags[32];
	int32 num_items;
	int32 bonusdata;
} t_player;

typedef struct _t_itemtype
{
	char name[32];
	char text[256];
	char clas[32];
	int32 type;
	int32 cost;
	int32 index;
	int32 flag;
	int32 sound;
	int32 loopsnd;
} t_itemtype;

typedef struct _t_fleet
{
	int32 system;
	int32 target;
	int32 enroute;
	int32 distance;
	int32 num_ships;
	int32 ships[16];
	int32 race;
	int32 explored;
	int32 blowtime;
} t_fleet;				// enemy "fleet"

typedef struct _t_month
{
	char name[16];
	char longname[16];
	int32 sd, le;
} t_month;

typedef struct _t_race
{
	char name[16];
	char text[256];
	char text2[64];
	int32 met;
	int32 fleet;
} t_race;

typedef struct _t_racefleet
{
	int32 race;
	int32 num_fleets;
	int32 stype[3];
	int32 fleets[9][3];
	int32 diff[3][10];
} t_racefleet;

enum planet_keyids
{
	plkBegin,
	plkName,
	plkText,
	plkBonus,
	plkEnd,
	plkMax,
};

enum star_keyids
{
	stkBegin,
	stkName,
	stkText,
	stkEnd,
	stkMax,
};

enum item_keyids
{
	itkBegin,
	itkName,
	itkType,
	itkText,
	itkClass,
	itkCost,
	itkFlag,
	itkSound,
	itkEnd,
	itkMax,
};

enum item_flags
{
	item_broken = 1,
	item_owed = 2,
};

enum item_deviceflags
{
	device_beacon = 4,
	device_probe = 8,
	device_collapser = 16,
	device_mirror = 32,
	device_bauble = 64,
	device_gong = 128,
	device_mantle = 256,
	device_torc = 512,
	device_conograph = 1024,
	lifeform_hard = 2048,
	lifeform_ambassador = 4096,
};

enum item_types
{
	item_weapon = 0,
	item_system,
	item_device,
	item_lifeform,
	item_treasure,
};

enum raceflt_keyids
{
	rflBegin,
	rflRace,
	rflShip1,
	rflShip2,
	rflShip3,
	rflFleet,
	rflEasy,
	rflMedium,
	rflHard,
	rflEnd,
	rflMax,
};

// ----------------
// GLOBAL VARIABLES
// ----------------

extern t_month				months[12];

extern t_race					races[16];
extern int32					num_races;

extern t_racefleet		racefleets[16];
extern int32					num_racefleets;

extern t_planettype		*platypes;
extern int32					num_platypes;

extern t_startype			*startypes;
extern int32					num_startypes;

extern t_itemtype			*itemtypes;
extern int32					num_itemtypes;

extern t_starsystem		*sm_stars;
extern int32					num_stars;

extern t_blackhole		*sm_holes;
extern int32					num_holes;

extern t_nebula				*sm_nebula;
extern int32					num_nebula;

extern t_fleet				sm_fleets[STARMAP_MAX_FLEETS];

extern t_ik_spritepak *spr_SMstars;
extern t_ik_spritepak *spr_SMstars2;
extern t_ik_spritepak *spr_SMplanet;
extern t_ik_spritepak *spr_SMplanet2;
extern t_ik_spritepak *spr_SMnebula;
extern t_ik_spritepak *spr_SMraces;

extern uint8					*sm_nebulamap;
extern t_ik_image			*sm_nebulagfx;
extern t_ik_image			*sm_starfield;

extern int32					star_env[8][8];
//extern char						pltype_name[10][32];
extern int32					plgfx_type[256];
extern int32					num_plgfx;

extern int32					homesystem;
extern t_player				player;
extern t_hud					hud;

extern int32					kla_items[32];
extern int32					kla_numitems;

extern char	captnames[64][16];
extern int32 num_captnames;
extern char	shipnames[64][16];
extern int32 num_shipnames;

extern int starmap_tutorialtype;

// ----------------
//    PROTOTYPES
// ----------------

void player_init();
void allies_init();

void starmap_init();
void starmap_create();
void starmap_createnebulagfx();

void starmap_deinit();

void starmap();
void starmap_display(int32 t);


void starmap_removeship(int32 n);

void starmap_advancedays(int32 n);

int starmap_stardist(int32 s1, int32 s2);
int starmap_nebuladist(int32 s1, int32 s2);
void starmap_sensefleets();


// ---------------------
// starmap_encounters.cpp
// ---------------------

// normal exploration
int32 starmap_entersystem();
void starmap_exploreplanet();
int32 starmap_explorehole(int32 h, int32 t);

// alien encounters
int32 fleet_encounter(int32 flt, int32 inc=0);
void starmap_meetrace(int32 r);
void klakar_encounter();
void enemy_encounter(int32 r);

void starmap_mantle(int32 flt);
int32 muktian_encounter();

// kawangi encounter
void kawangi_warning();
void kawangi_message(int32 flt, int32 m);
void starmap_kawangimove(int flt);

// ---------------------
// starmap_inventory.cpp
// ---------------------

// regular inventory management
void starmap_installitem(int32 n);
void starmap_uninstallsystem(int32 n, int32 brk);
void starmap_destroysystem(int32 n);
void starmap_additem(int32 it, int32 brk);
void starmap_removeitem(int32 n);
int32 ally_install(int32 s, int32 it, int32 pay);
int32 select_weaponpoint();
int32 item_colorcode(int32 it);

// klakar
void klakar_trade();

// mercenary
int32 pay_item(char *title, char *text, int r, char klak = 0);

// use artifact
int32 use_vacuum_collapser(char *title);
void vacuum_collapse(int st);
void eledras_mirror(char *title);
int32 eledras_bauble(char *title);
void use_conograph(char *title);

int32 probe_fleet_encounter(int32 flt);
void probe_exploreplanet(int32 probe);
int32 stellar_probe(char *title);
// ----------------
//    CONSTANTS
// ----------------

// ----------------
//     TYPEDEFS
// ----------------

// ----------------
// GLOBAL VARIABLES
// ----------------

// ----------------
//    PROTOTYPES
// ----------------

int32 startgame();

void startgame_init();
void startgame_deinit();

void loadconfig();
void saveconfig();
#define STRINGBUFFER_SIZE 128*1024

#ifndef DEMO_VERSION
enum textstrings
{
	STR_YES,
	STR_NO,
	STR_OK,
	STR_CANCEL,
	STR_ACCEPT,
	STR_DECLINE,
	STR_LEAVE,
	STR_STAY,
	STR_CONTINUE,
	STR_GOBACK,
	STR_START,
	STR_TRADE,
	STR_DONE,
	STR_HISCORE_ENTRY,
	STR_QUIT_TITLE,
	STR_QUIT_CONFIRM,
	STR_QUIT_SIMULATION,
	STR_STARTGAME_MUCRON1,
	STR_STARTGAME_MUCRON2,
	STR_STARTGAME_MUCRON3,
	STR_STARTGAME_MUCRON4,
	STR_STARTGAME_MUCRON5,
	STR_STARTGAME_MUCRON6,
	STR_STARTGAME_MUCRON7,
	STR_STARTGAME_TITLE1,
	STR_STARTGAME_TITLE2,
	STR_STARTGAME_TITLE3,
	STR_STARTGAME_IDENTIFY,
	STR_STARTGAME_CAPTAIN,
	STR_STARTGAME_STARSHIP,
	STR_STARTGAME_RENAME,
	STR_STARTGAME_LOADOUT,
	STR_STARTGAME_LOADOUT1,
	STR_STARTGAME_LOADOUT2,
	STR_STARTGAME_LOADOUT3,
	STR_STARTGAME_NEBULA,
	STR_STARTGAME_ENEMIES,
	STR_STARTGAME_EASY,
	STR_STARTGAME_HARD,
	STR_STARTGAME_LOSCORE,
	STR_STARTGAME_HISCORE,
	STR_ENDGAME_CONFIRM1,
	STR_ENDGAME_CONFIRM2,
	STR_ENDGAME_OVER,
	STR_ENDGAME_DATE,
	STR_ENDGAME_DATEF,
	STR_ENDGAME_DATEF2,
	STR_ENDGAME_SCORE,
	STR_ENDGAME_MUCRON1,
	STR_ENDGAME_MUCRON2,
	STR_ENDGAME_MUCRON3,
	STR_ENDGAME_MUCRON4,
	STR_ENDGAME_MUCRON5,
	STR_ENDGAME_MUCRON6,
	STR_ENDGAME_MUCRON7,
	STR_ENDGAME_KAWANGI,
	STR_ENDGAME_DEATH1,
	STR_ENDGAME_DEATH2,
	STR_ENDGAME_DEATH3,
	STR_ENDGAME_DEATH4,
	STR_ENDGAME_DEATH5,
	STR_ENDGAME_DEATH6,
	STR_ENDGAME_DEATH7,
	STR_ENDGAME_MSG1,
	STR_ENDGAME_MSG2,
	STR_ENDGAME_MSG3,
	STR_ENDGAME_MSG4,
	STR_ENDGAME_MSG5,
	STR_ENDGAME_MSG6,
	STR_ENDGAME_MSG7,
	STR_ENDGAME_MSG8,
	STR_ENDGAME_BAR1,
	STR_ENDGAME_BAR2,
	STR_ENDGAME_BAR3,
	STR_ENDGAME_BAR4,
	STR_ENDGAME_BAR5,
	STR_ENDGAME_BAR6,
	STR_ENDGAME_BAR7,
	STR_PROBE_TITLE,
	STR_PROBE_TITLE2,
	STR_PROBE_DIALOG1,
	STR_PROBE_DIALOG2,
	STR_PROBE_FLEET1,
	STR_PROBE_FLEET2,
	STR_PROBE_FLEET3,
	STR_PROBE_MISCDATA,
	STR_PROBE_MISCDATA1,
	STR_PROBE_MISCDATA2,
	STR_PROBE_MISCDATA3,
	STR_PROBE_MISCDATA4,
	STR_PROBE_MISCDATA5,
	STR_PROBE_MISCDATA6,
	STR_ANALYZER_MISCDATA1,
	STR_ANALYZER_MISCDATA2,
	STR_ANALYZER_MISCDATA3,
	STR_ANALYZER_MISCDATA4,
	STR_ANALYZER_MISCDATA5,
	STR_ANALYZER_MISCDATA6,
	STR_SCANNER_RACE,
	STR_SCANNER_NORACE,
	STR_SCANNER_ALIENS,
	STR_SCANNER_INCOMING,
	STR_SCANNER_AVOID,
	STR_SCANNER_ENGAGE,
	STR_SCANNER_FLEE,
	STR_MIRROR_NOTARGET,
	STR_MIRROR_NOCANDO1,
	STR_MIRROR_NOCANDO2,
	STR_MIRROR_NOCANDO3,
	STR_MIRROR_NOCANDO4,
	STR_MIRROR_NOCANDO5,
	STR_MIRROR_NOCANDO6,
	STR_MIRROR_NOCANDO7,
	STR_MIRROR_NOCANDO8,
	STR_BAUBLE_FOMAX,
	STR_BAUBLE_CONFIRM,
	STR_BAUBLE_WISH,
	STR_BAUBLE_PROMPT,
	STR_BAUBLE_FAIL,
	STR_BAUBLE_GIFT,
	STR_BAUBLE_A,
	STR_BAUBLE_AN,
	STR_CONOGRAPH_PLAY,
	STR_MANTLE_MUKTIAN,
	STR_MANTLE_MUKTIAN2,
	STR_MANTLE_GARTHAN,
	STR_MANTLE_GARTHAN2,
	STR_MANTLE_URLUQUAI,
	STR_MANTLE_URLUQUAI2,
	STR_TRADE_TITLE,
	STR_TRADE_MESSAGE,
	STR_TRADE_EMPORIUM,
	STR_MERC_HIS,
	STR_MERC_HER,
	STR_MERC_TITLE,
	STR_MERC_BILLING,
	STR_MERC_PAYMENT,
	STR_MERC_DEAL,
	STR_MERC_TOOBIGT,
	STR_MERC_TOOBIG,
	STR_MERC_NOGOODT,
	STR_MERC_NOGOOD,
	STR_MERC_THANKS,
	STR_MERC_THANKS2,
	STR_ALLY_TITLE,
	STR_ALLY_CONFIRMT,
	STR_ALLY_CONFIRM,
	STR_ALLY_REFUSET,
	STR_ALLY_REFUSE,
	STR_ALLY_INSTALLT,
	STR_ALLY_INSTALL,
	STR_ALLY_CAPT1,
	STR_ALLY_CAPT2,
	STR_ALLY_SHIP1,
	STR_ALLY_SHIP2,
	STR_VIDCAST,
	STR_VIDCAST2,
	STR_KLAK_PAYTITLE,
	STR_KLAK_PAYMENT,
	STR_KLAK_NOPAY,
	STR_KLAK_UNSAFE,
	STR_KLAK_UNAVAIL,
	STR_MUKTIAN_THANKS,
	STR_MUKTIAN_WARNING,
	STR_GARTHAN_WARN1,
	STR_GARTHAN_WARN2,
	STR_GARTHAN_WARN3,
	STR_URLUQUAI_WARN1,
	STR_URLUQUAI_WARN2,
	STR_URLUQUAI_WARN3,
	STR_TANRU_WARN,
	STR_BLACKHOLE_TITLE,
	STR_BLACKHOLE_DESC,
	STR_BLACKHOLE_WARN,
	STR_TIMER_TITLE,
	STR_TIMER_WARN1,
	STR_TIMER_WARN2,
	STR_SYSTEM_DESTROYED,
	STR_DRIVE_MISSING,
	STR_DRIVE_MISSING2,
	STR_DRIVE_BROKEN,
	STR_DRIVE_BROKEN2,
	STR_DRIVE_NOVA1,
	STR_DRIVE_NOVA2,
	STR_DRIVE_NOVA3,
	STR_KAWANGI_WARNING1,
	STR_KAWANGI_WARNING2,
	STR_KAWANGI_WARNING3,
	STR_KAWANGI_KILLED,
	STR_KAWANGI_KILLED1,
	STR_KAWANGI_KILLED2,
	STR_KAWANGI_EXPLO,
	STR_KAWANGI_EXPLO1,
	STR_KAWANGI_EXPLO2,
	STR_LIFEFORM_HARD,
	STR_LIFEFORM_HARDT,
	STR_LIFEFORM_HUNT,
	STR_LIFEFORM_HUNTT,
	STR_AMBASSADOR,
	STR_AMBASSADORT,
	STR_LVC_CONFIRM,
	STR_LVC_ASKWHEN,
	STR_LVC_DAYSTILL,
	STR_INV_POINT,
	STR_INV_ARTIFACT,
	STR_INV_REPAIR_HULL,
	STR_INV_REPAIR_SYS,
	STR_INV_REPAIR_TITLE,
	STR_ALIEN_CONTACT,
	STR_ALIEN_DEMEANOR,
	STR_CARD_PLANET,
	STR_CARD_EVENT,
	STR_CARD_ALLY,
	STR_CARD_DISCOVERY,
	STR_CARD_RENAME,
	STR_EVENT_DEVA,
	STR_EVENT_FLARE,
	STR_EVENT_THIEF,
	STR_EVENT_NOVA,
	STR_EVENT_SABOT,
	STR_EVENT_WHALE,
	STR_EVENT_CONE,
	STR_EVENT_CONE2,
	STR_EVENT_HULK,
	STR_EVENT_HULK2,
	STR_EVENT_GIANT,
	STR_COMBAT_TITLE,
	STR_COMBAT_NOTARGET,
	STR_COMBAT_RETREAT,
	STR_COMBAT_CLOAK,
	STR_COMBAT_UNCLOAK,
	STR_COMBAT_GONG,
	STR_COMBAT_KLAKAR,
	STR_COMBAT_STATUS,
	STR_COMBAT_DMGKEY,
	STR_COMBAT_DMG1,
	STR_COMBAT_DMG2,
	STR_COMBAT_DMG3,
	STR_COMBAT_DMG4,
	STR_COMBAT_SYSDMG,
	STR_COMBAT_SIMTITLE,
	STR_COMBAT_SIMSHIP,
	STR_COMBAT_SIMWINGMEN,
	STR_COMBAT_SIMENEMIES,
	STR_COMBAT_SIMEND,
	STR_COMBAT_SIMALLY,
	STR_COMBAT_SIMENMY,
	STR_COMBAT_SIMSURV,
	STR_COMBAT_SIMDEST,
	STR_COMBAT_SIMESCP,
	STR_STARMAP_LYEARS,
	STR_STARMAP_NDAYS,
	STR_STARMAP_DATE,
	STR_STARMAP_DAYSLEFT,
	STR_STARMAP_CAPTAIN,
	STR_STARMAP_CARGO,
	STR_STARMAP_SELECT,
	STR_NAME_GLORY,
	STR_NAME_HOPE,
	STR_TUT_STARMAP,
	STR_TUT_EXPLORE,
	STR_TUT_UPGRADE,
	STR_TUT_DEVICE,
	STR_TUT_TREASURE,
	STR_TUT_ALLYSHIP,
	STR_TUT_ENCOUNTER,
	STR_TUT_COMBAT,
	STR_TUT_TRADING,
	STR_TUT_TSTARMAP,
	STR_TUT_TEXPLORE,
	STR_TUT_TUPGRADE,
	STR_TUT_TDEVICE,
	STR_TUT_TTREASURE,
	STR_TUT_TALLYSHIP,
	STR_TUT_TENCOUNTER,
	STR_TUT_TCOMBAT,
	STR_TUT_TTRADING,
	STR_TUT_END,
	STR_MAX
};
#else
enum textstrings	// reduced for DEMO_VERSION
{
	STR_YES,
	STR_NO,
	STR_OK,
	STR_CANCEL,
	STR_ACCEPT,
	STR_DECLINE,
	STR_LEAVE,
	STR_STAY,
	STR_CONTINUE,
	STR_GOBACK,
	STR_START,
	STR_TRADE,
	STR_DONE,
	STR_HISCORE_ENTRY,
	STR_QUIT_TITLE,
	STR_QUIT_CONFIRM,
	STR_QUIT_SIMULATION,
	STR_STARTGAME_MUCRON1,
	STR_STARTGAME_MUCRON2,
	STR_STARTGAME_MUCRON3,
	STR_STARTGAME_MUCRON7,
	STR_STARTGAME_TITLE1,
	STR_STARTGAME_TITLE2,
	STR_STARTGAME_TITLE3,
	STR_STARTGAME_IDENTIFY,
	STR_STARTGAME_CAPTAIN,
	STR_STARTGAME_STARSHIP,
	STR_STARTGAME_RENAME,
	STR_STARTGAME_LOADOUT,
	STR_STARTGAME_LOADOUT1,
	STR_STARTGAME_LOADOUT2,
	STR_STARTGAME_LOADOUT3,
	STR_STARTGAME_NEBULA,
	STR_STARTGAME_ENEMIES,
	STR_STARTGAME_EASY,
	STR_STARTGAME_HARD,
	STR_STARTGAME_LOSCORE,
	STR_STARTGAME_HISCORE,
	STR_ENDGAME_CONFIRM1,
	STR_ENDGAME_CONFIRM2,
	STR_ENDGAME_OVER,
	STR_ENDGAME_DATE,
	STR_ENDGAME_DATEF,
	STR_ENDGAME_DATEF2,
	STR_ENDGAME_SCORE,
	STR_ENDGAME_MUCRON1,
	STR_ENDGAME_MUCRON2,
	STR_ENDGAME_MUCRON3,
	STR_ENDGAME_MUCRON4,
	STR_ENDGAME_MUCRON7,
	STR_ENDGAME_DEATH1,
	STR_ENDGAME_DEATH2,
	STR_ENDGAME_DEATH3,
	STR_ENDGAME_DEATH4,
	STR_ENDGAME_DEATH5,
	STR_ENDGAME_DEATH6,
	STR_ENDGAME_DEATH7,
	STR_ENDGAME_MSG1,
	STR_ENDGAME_MSG2,
	STR_ENDGAME_MSG3,
	STR_ENDGAME_MSG4,
	STR_ENDGAME_MSG5,
	STR_ENDGAME_MSG6,
	STR_ENDGAME_MSG7,
	STR_ENDGAME_MSG8,
	STR_ENDGAME_BAR1,
	STR_ENDGAME_BAR2,
	STR_ENDGAME_BAR3,
	STR_ENDGAME_BAR4,
	STR_ENDGAME_BAR5,
	STR_ENDGAME_BAR6,
	STR_ENDGAME_BAR7,
	STR_PROBE_TITLE,
	STR_PROBE_TITLE2,
	STR_PROBE_DIALOG1,
	STR_PROBE_DIALOG2,
	STR_PROBE_FLEET1,
	STR_PROBE_FLEET2,
	STR_PROBE_FLEET3,
	STR_PROBE_MISCDATA,
	STR_PROBE_MISCDATA1,
	STR_PROBE_MISCDATA2,
	STR_PROBE_MISCDATA3,
	STR_PROBE_MISCDATA4,
	STR_PROBE_MISCDATA5,
	STR_PROBE_MISCDATA6,
	STR_ANALYZER_MISCDATA1,
	STR_ANALYZER_MISCDATA2,
	STR_ANALYZER_MISCDATA3,
	STR_ANALYZER_MISCDATA4,
	STR_ANALYZER_MISCDATA5,
	STR_ANALYZER_MISCDATA6,
	STR_SCANNER_RACE,
	STR_SCANNER_NORACE,
	STR_SCANNER_ALIENS,
	STR_SCANNER_INCOMING,
	STR_SCANNER_AVOID,
	STR_SCANNER_ENGAGE,
	STR_SCANNER_FLEE,
	STR_TRADE_TITLE,
	STR_TRADE_MESSAGE,
	STR_TRADE_EMPORIUM,
	STR_MERC_HIS,
	STR_MERC_HER,
	STR_MERC_TITLE,
	STR_MERC_BILLING,
	STR_MERC_PAYMENT,
	STR_MERC_DEAL,
	STR_MERC_TOOBIGT,
	STR_MERC_TOOBIG,
	STR_MERC_NOGOODT,
	STR_MERC_NOGOOD,
	STR_MERC_THANKS,
	STR_MERC_THANKS2,
	STR_ALLY_TITLE,
	STR_ALLY_CONFIRMT,
	STR_ALLY_CONFIRM,
	STR_ALLY_REFUSET,
	STR_ALLY_REFUSE,
	STR_ALLY_INSTALLT,
	STR_ALLY_INSTALL,
	STR_ALLY_CAPT1,
	STR_ALLY_CAPT2,
	STR_ALLY_SHIP1,
	STR_ALLY_SHIP2,
	STR_VIDCAST,
	STR_VIDCAST2,
	STR_KLAK_PAYTITLE,
	STR_KLAK_PAYMENT,
	STR_KLAK_NOPAY,
	STR_KLAK_UNSAFE,
	STR_KLAK_UNAVAIL,
	STR_GARTHAN_WARN1,
	STR_GARTHAN_WARN2,
	STR_GARTHAN_WARN3,
	STR_BLACKHOLE_TITLE,
	STR_BLACKHOLE_DESC,
	STR_BLACKHOLE_WARN,
	STR_TIMER_TITLE,
	STR_TIMER_WARN1,
	STR_TIMER_WARN2,
	STR_SYSTEM_DESTROYED,
	STR_DRIVE_MISSING,
	STR_DRIVE_MISSING2,
	STR_DRIVE_BROKEN,
	STR_DRIVE_BROKEN2,
	STR_DRIVE_NOVA1,
	STR_DRIVE_NOVA2,
	STR_DRIVE_NOVA3,
	STR_LIFEFORM_HARD,
	STR_LIFEFORM_HARDT,
	STR_LIFEFORM_HUNT,
	STR_LIFEFORM_HUNTT,
	STR_INV_POINT,
	STR_INV_ARTIFACT,
	STR_INV_REPAIR_HULL,
	STR_INV_REPAIR_SYS,
	STR_INV_REPAIR_TITLE,
	STR_ALIEN_CONTACT,
	STR_ALIEN_DEMEANOR,
	STR_CARD_PLANET,
	STR_CARD_EVENT,
	STR_CARD_ALLY,
	STR_CARD_DISCOVERY,
	STR_CARD_RENAME,
	STR_EVENT_DEVA,
	STR_EVENT_FLARE,
	STR_EVENT_THIEF,
	STR_COMBAT_TITLE,
	STR_COMBAT_NOTARGET,
	STR_COMBAT_RETREAT,
	STR_COMBAT_CLOAK,
	STR_COMBAT_UNCLOAK,
	STR_COMBAT_GONG,
	STR_COMBAT_KLAKAR,
	STR_COMBAT_STATUS,
	STR_COMBAT_DMGKEY,
	STR_COMBAT_DMG1,
	STR_COMBAT_DMG2,
	STR_COMBAT_DMG3,
	STR_COMBAT_DMG4,
	STR_COMBAT_SYSDMG,
	STR_COMBAT_SIMTITLE,
	STR_COMBAT_SIMSHIP,
	STR_COMBAT_SIMWINGMEN,
	STR_COMBAT_SIMENEMIES,
	STR_COMBAT_SIMEND,
	STR_COMBAT_SIMALLY,
	STR_COMBAT_SIMENMY,
	STR_COMBAT_SIMSURV,
	STR_COMBAT_SIMDEST,
	STR_COMBAT_SIMESCP,
	STR_STARMAP_LYEARS,
	STR_STARMAP_NDAYS,
	STR_STARMAP_DATE,
	STR_STARMAP_DAYSLEFT,
	STR_STARMAP_CAPTAIN,
	STR_STARMAP_CARGO,
	STR_STARMAP_SELECT,
	STR_NAME_GLORY,
	STR_NAME_HOPE,
	STR_TUT_STARMAP,
	STR_TUT_EXPLORE,
	STR_TUT_UPGRADE,
	STR_TUT_DEVICE,
	STR_TUT_TREASURE,
	STR_TUT_ALLYSHIP,
	STR_TUT_ENCOUNTER,
	STR_TUT_COMBAT,
	STR_TUT_TRADING,
	STR_TUT_TSTARMAP,
	STR_TUT_TEXPLORE,
	STR_TUT_TUPGRADE,
	STR_TUT_TDEVICE,
	STR_TUT_TTREASURE,
	STR_TUT_TALLYSHIP,
	STR_TUT_TENCOUNTER,
	STR_TUT_TCOMBAT,
	STR_TUT_TTRADING,
	STR_TUT_END,
	STR_MAX
};
#endif

extern char *textbuffer;	
extern char *textstring[STR_MAX];


void textstrings_init();
void textstrings_deinit();
/*

The basic datatypes

*/

//#define DEMO_VERSION

//#ifdef _MSC_VER
#define _CRT_SECURE_NO_WARNINGS
#pragma warning(disable: 4996)
//#endif

typedef signed char int8;
typedef unsigned char uint8;
typedef signed short int16;
typedef unsigned short uint16;
typedef signed int int32;
typedef unsigned int uint32;
typedef float float32;
typedef double float64;

//new using directive
#using <mscorlib.dll>
//#using <LibSAIS.dll>
//another using namespace directive.
using namespace System;
//using namespace LibSAIS;
