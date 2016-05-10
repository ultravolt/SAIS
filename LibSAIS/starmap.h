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

public class _t_hud
{
	Int32 invslider;
	Int32 invselect;
	Int32 sysslider;
	Int32 sysselect;
} t_hud;


public class _t_starsystem
{
	char starname[16];
	char planetname[16];
	Int32 x, y;
	Int32 color;
	Int32 planet;
	Int32 planetgfx;
	Int32 novadate;
	Int32 novatype;
	Int32 novatime;

	Int32 card;
	Int32 explored;
	Int32 ds_x, ds_y;
} t_starsystem;

public class _t_planettype
{
	char name[16];
	char text[172];
	Int32 bonus;
} t_planettype;

public class _t_startype
{
	char name[16];
	char text[176];
} t_startype;

public class _t_blackhole
{
	char name[20];
	Int32 x, y;
	Int16 size;
	Int16 explored;
} t_blackhole;

public class _t_nebula
{
	Int32 x, y;
	Int32 sprite;
} t_nebula;

public class _t_player
{
	char captname[32];
	char shipname[32];
	char deathmsg[64];

	Int32 x,y,a;
	Int32 system;
	Int32 target;

	Int32 distance;
	Int32 nebula;
	Int32 enroute;

	Int32 engage;
	Int32 fold;
	Int32 hypdate;
	Int32 foldate;
	Int32 hyptime;

	Int32 explore;
	Int32 stardate;	
	Int32 death;
	Int32 deatht;
	Int32 hole;

	Int32 num_ships;
	Int32 ships[8];
	Int32 sel_ship;
	Int32 sel_ship_time;

	Int32 items[32];
	Int32 itemflags[32];
	Int32 num_items;
	Int32 bonusdata;
} t_player;

public class _t_itemtype
{
	char name[32];
	char text[256];
	char clas[32];
	Int32 type;
	Int32 cost;
	Int32 index;
	Int32 flag;
	Int32 sound;
	Int32 loopsnd;
} t_itemtype;

public class _t_fleet
{
	Int32 system;
	Int32 target;
	Int32 enroute;
	Int32 distance;
	Int32 num_ships;
	Int32 ships[16];
	Int32 race;
	Int32 explored;
	Int32 blowtime;
} t_fleet;				// enemy "fleet"

public class _t_month
{
	char name[16];
	char longname[16];
	Int32 sd, le;
} t_month;

public class _t_race
{
	char name[16];
	char text[256];
	char text2[64];
	Int32 met;
	Int32 fleet;
} t_race;

public class _t_racefleet
{
	Int32 race;
	Int32 num_fleets;
	Int32 stype[3];
	Int32 fleets[9][3];
	Int32 diff[3][10];
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

public static t_month				months[12];

public static t_race					races[16];
public static Int32					num_races;

public static t_racefleet		racefleets[16];
public static Int32					num_racefleets;

public static t_planettype		*platypes;
public static Int32					num_platypes;

public static t_startype			*startypes;
public static Int32					num_startypes;

public static t_itemtype			*itemtypes;
public static Int32					num_itemtypes;

public static t_starsystem		*sm_stars;
public static Int32					num_stars;

public static t_blackhole		*sm_holes;
public static Int32					num_holes;

public static t_nebula				*sm_nebula;
public static Int32					num_nebula;

public static t_fleet				sm_fleets[STARMAP_MAX_FLEETS];

public static t_ik_spritepak spr_SMstars;
public static t_ik_spritepak spr_SMstars2;
public static t_ik_spritepak spr_SMplanet;
public static t_ik_spritepak spr_SMplanet2;
public static t_ik_spritepak spr_SMnebula;
public static t_ik_spritepak spr_SMraces;

public static byte					*sm_nebulamap;
public static t_ik_image			*sm_nebulagfx;
public static t_ik_image			*sm_starfield;

public static Int32					star_env[8][8];
//public static char						pltype_name[10][32];
public static Int32					plgfx_type[256];
public static Int32					num_plgfx;

public static Int32					homesystem;
public static t_player				player;
public static t_hud					hud;

public static Int32					kla_items[32];
public static Int32					kla_numitems;

public static char	captnames[64][16];
public static Int32 num_captnames;
public static char	shipnames[64][16];
public static Int32 num_shipnames;

public static int starmap_tutorialtype;

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
void starmap_display(Int32 t);


void starmap_removeship(Int32 n);

void starmap_advancedays(Int32 n);

int starmap_stardist(Int32 s1, Int32 s2);
int starmap_nebuladist(Int32 s1, Int32 s2);
void starmap_sensefleets();


// ---------------------
// starmap_encounters.cpp
// ---------------------

// normal exploration
Int32 starmap_entersystem();
void starmap_exploreplanet();
Int32 starmap_explorehole(Int32 h, Int32 t);

// alien encounters
Int32 fleet_encounter(Int32 flt, Int32 inc=0);
void starmap_meetrace(Int32 r);
void klakar_encounter();
void enemy_encounter(Int32 r);

void starmap_mantle(Int32 flt);
Int32 muktian_encounter();

// kawangi encounter
void kawangi_warning();
void kawangi_message(Int32 flt, Int32 m);
void starmap_kawangimove(int flt);

// ---------------------
// starmap_inventory.cpp
// ---------------------

// regular inventory management
void starmap_installitem(Int32 n);
void starmap_uninstallsystem(Int32 n, Int32 brk);
void starmap_destroysystem(Int32 n);
void starmap_additem(Int32 it, Int32 brk);
void starmap_removeitem(Int32 n);
Int32 ally_install(Int32 s, Int32 it, Int32 pay);
Int32 select_weaponpoint();
Int32 item_colorcode(Int32 it);

// klakar
void klakar_trade();

// mercenary
Int32 pay_item(char *title, char *text, int r, char klak = 0);

// use artifact
Int32 use_vacuum_collapser(char *title);
void vacuum_collapse(int st);
void eledras_mirror(char *title);
Int32 eledras_bauble(char *title);
void use_conograph(char *title);

Int32 probe_fleet_encounter(Int32 flt);
void probe_exploreplanet(Int32 probe);
Int32 stellar_probe(char *title);
