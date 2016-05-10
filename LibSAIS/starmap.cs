using System;
using System.Collections.Generic;

namespace DigitalEeel
{
    public static partial class SAIS
    {
        // ----------------
        //    CONSTANTS
        // ----------------

        public const int STARMAP_FRAMERATE = 50;

        public const int SM_MAP_X = 160;
        public const int SM_MAP_Y = 0;
        public const int SM_SHIP_X = 0;
        public const int SM_SHIP_Y = 0;
        public const int SM_INV_X = 0;
        public const int SM_INV_Y = 256;
        public const int SM_SEL_X = 0;
        public const int SM_SEL_Y = 384;

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

        public const int STARMAP_INTERFACE_COLOR = 11;

        public const int STARMAP_MAX_FLEETS = 8;

        public const int RC_MUCRON = 9;
        public const int RC_PLANET = 10;
        public const int RC_FOMAX = 11;
        public const int RC_BLOWUP = 12;
        public const int RC_HOLED = 13;
        public const int RC_LOST = 14;

        public const int NUM_FLEETS = 5;
        public const int NUM_STARSYSTEMS = 16;
        public const int NUM_EVENTS = 2;
        public const int NUM_ALLIES = 2;
        public const int NUM_ITEMS = 6;
        public const int NUM_RAREITEMS = 2;
        public const int NUM_LIFEFORMS = 3;


        // ----------------
        //     TYPEDEFS
        // ----------------

        public class t_hud
        {
            Int32 invslider;
            Int32 invselect;
            Int32 sysslider;
            Int32 sysselect;
        }


        public class t_starsystem
        {
            char[] starname = new char[16];
            char[] planetname = new char[16];
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
        }

        public class t_planettype
        {
            char[] name = new char[16];
            char[] text = new char[172];
            Int32 bonus;
        }

        public class t_startype
        {
            char[] name = new char[16];
            char[] text = new char[176];
        }

        public class t_blackhole
        {
            char[] name = new char[20];
            Int32 x, y;
            Int16 size;
            Int16 explored;
        }

        public class t_nebula
        {
            Int32 x, y;
            Int32 sprite;
        }

        public class t_player
        {
            char[] captname = new char[32];
            char[] shipname = new char[32];
            char[] deathmsg = new char[64];

            Int32 x, y, a;
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
            Int32[] ships = new Int32[8];
            Int32 sel_ship;
            Int32 sel_ship_time;

            Int32[] items = new Int32[32];
            Int32[] itemflags = new Int32[32];
            Int32 num_items;
            Int32 bonusdata;
        }

        public class t_itemtype
        {
            char[] name = new char[32];
            char[] text = new char[256];
            char[] clas = new char[32];
            Int32 type;
            Int32 cost;
            Int32 index;
            Int32 flag;
            Int32 sound;
            Int32 loopsnd;
        }

        public class t_fleet
        {
            Int32 system;
            Int32 target;
            Int32 enroute;
            Int32 distance;
            Int32 num_ships;
            Int32[] ships = new Int32[16];
            Int32 race;
            Int32 explored;
            Int32 blowtime;
        }               // enemy "fleet"

        public class t_month
        {
            char[] name = new char[16];
            char[] longname = new char[16];
            Int32 sd, le;
        }

        public class t_race
        {
            char[] name = new char[16];
            char[] text = new char[256];
            char[] text2 = new char[64];
            Int32 met;
            Int32 fleet;
        }

        public class t_racefleet
        {
            Int32 race;
            Int32 num_fleets;
            Int32[] stype = new Int32[3];
            Int32[,] fleets = new Int32[9, 3];
            Int32[,] diff = new Int32[3, 10];
        }

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

        public static t_month[] months=new t_month[12];

        public static t_race[] races=new t_race[16];
        public static Int32 num_races;

        public static t_racefleet[] racefleets=new t_racefleet[16];
        public static Int32 num_racefleets;

        public static List<t_planettype> platypes;
        public static Int32 num_platypes;

        public static t_startype startypes;
        public static Int32 num_startypes;

        public static List<t_itemtype> itemtypes;
        public static Int32 num_itemtypes;

        public static List<t_starsystem> sm_stars;
        public static Int32 num_stars;

        public static List<t_blackhole> sm_holes;
        public static Int32 num_holes;

        public static List<t_nebula> sm_nebula;
        public static Int32 num_nebula;

        public static t_fleet[] sm_fleets = new t_fleet[STARMAP_MAX_FLEETS];

        public static t_ik_spritepak spr_SMstars;
        public static t_ik_spritepak spr_SMstars2;
        public static t_ik_spritepak spr_SMplanet;
        public static t_ik_spritepak spr_SMplanet2;
        public static t_ik_spritepak spr_SMnebula;
        public static t_ik_spritepak spr_SMraces;

        public static byte[] sm_nebulamap;
        public static t_ik_image sm_nebulagfx;
        public static t_ik_image sm_starfield;

        public static Int32[,] star_env = new Int32[8, 8];
        //public static char						pltype_name[10][32];
        public static Int32[] plgfx_type = new Int32[256];
        public static Int32 num_plgfx;

        public static Int32 homesystem;
        public static t_player player;
        public static t_hud hud;

        public static Int32[] kla_items = new Int32[32];
        public static Int32 kla_numitems;

        public static char[,] captnames = new char[64, 16];
        public static Int32 num_captnames;
        public static char[,] shipnames = new char[64, 16];
        public static Int32 num_shipnames;

        public static int starmap_tutorialtype;

        // ----------------
        //    PROTOTYPES
        // ----------------

        public static void player_init() { throw new NotImplementedException(); }
        public static void allies_init() { throw new NotImplementedException(); }

        public static void starmap_init() { throw new NotImplementedException(); }
        public static void starmap_create() { throw new NotImplementedException(); }
        public static void starmap_createnebulagfx() { throw new NotImplementedException(); }

        public static void starmap_deinit() { throw new NotImplementedException(); }

        public static void starmap() { throw new NotImplementedException(); }
        public static void starmap_display(Int32 t) { throw new NotImplementedException(); }


        public static void starmap_removeship(Int32 n) { throw new NotImplementedException(); }

        public static void starmap_advancedays(Int32 n) { throw new NotImplementedException(); }

        public static int starmap_stardist(Int32 s1, Int32 s2) { throw new NotImplementedException(); }
        public static int starmap_nebuladist(Int32 s1, Int32 s2) { throw new NotImplementedException(); }
        public static void starmap_sensefleets() { throw new NotImplementedException(); }


        // ---------------------
        // starmap_encounters.cpp
        // ---------------------

        // normal exploration
        public static Int32 starmap_entersystem() { throw new NotImplementedException(); }
        public static void starmap_exploreplanet() { throw new NotImplementedException(); }
        public static Int32 starmap_explorehole(Int32 h, Int32 t) { throw new NotImplementedException(); }

        // alien encounters
        public static Int32 fleet_encounter(Int32 flt, Int32 inc = 0) { throw new NotImplementedException(); }
        public static void starmap_meetrace(Int32 r) { throw new NotImplementedException(); }
        public static void klakar_encounter() { throw new NotImplementedException(); }
        public static void enemy_encounter(Int32 r) { throw new NotImplementedException(); }

        public static void starmap_mantle(Int32 flt) { throw new NotImplementedException(); }
        public static Int32 muktian_encounter() { throw new NotImplementedException(); }

        // kawangi encounter
        public static void kawangi_warning() { throw new NotImplementedException(); }
        public static void kawangi_message(Int32 flt, Int32 m) { throw new NotImplementedException(); }
        public static void starmap_kawangimove(int flt) { throw new NotImplementedException(); }

        // ---------------------
        // starmap_inventory.cpp
        // ---------------------

        // regular inventory management
        public static void starmap_installitem(Int32 n) { throw new NotImplementedException(); }
        public static void starmap_uninstallsystem(Int32 n, Int32 brk) { throw new NotImplementedException(); }
        public static void starmap_destroysystem(Int32 n) { throw new NotImplementedException(); }
        public static void starmap_additem(Int32 it, Int32 brk) { throw new NotImplementedException(); }
        public static void starmap_removeitem(Int32 n) { throw new NotImplementedException(); }
        public static Int32 ally_install(Int32 s, Int32 it, Int32 pay) { throw new NotImplementedException(); }
        public static Int32 select_weaponpoint() { throw new NotImplementedException(); }
        public static Int32 item_colorcode(Int32 it) { throw new NotImplementedException(); }

        // klakar
        public static void klakar_trade() { throw new NotImplementedException(); }

        // mercenary
        public static Int32 pay_item(char[] title, char[] text, int r, int klak = 0) { throw new NotImplementedException(); }

        // use artifact
        public static Int32 use_vacuum_collapser(char[] title) { throw new NotImplementedException(); }
        public static void vacuum_collapse(int st) { throw new NotImplementedException(); }
        public static void eledras_mirror(char[] title) { throw new NotImplementedException(); }
        public static Int32 eledras_bauble(char[] title) { throw new NotImplementedException(); }
        public static void use_conograph(char[] title) { throw new NotImplementedException(); }

        public static Int32 probe_fleet_encounter(Int32 flt) { throw new NotImplementedException(); }
        public static void probe_exploreplanet(Int32 probe) { throw new NotImplementedException(); }
        public static Int32 stellar_probe(char[] title) { throw new NotImplementedException(); }
    }
}