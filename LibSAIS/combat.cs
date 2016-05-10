//#define DEBUG_COMBAT

//#define COMBAT_BUILD_HELP

// ----------------
//		CONSTANTS
// ----------------
using System;
namespace DigitalEeel
{
    public static partial class SAIS
    {

        public enum combat_tactics
        {
            tac_pursue = 0,
            tac_attack,
            tac_move,
            tac_flee,
        };

        public const Int32 MAX_COMBAT_SHIPS = 32;
        public const Int32 MAX_COMBAT_PROJECTILES = 256;
        public const Int32 MAX_COMBAT_BEAMS = 256;
        public const Int32 MAX_COMBAT_EXPLOS = 256;

        //# ifndef MOVIE
        public const Int32 COMBAT_FRAMERATE = 50;
        //#else
        //#define COMBAT_FRAMERATE 17
        //#endif

        public static Int32 COMBAT_INTERFACE_COLOR = (11 + simulated);

        // ----------------
        //     TYPEDEFS
        // ----------------

        public class t_hardpoint
        {
            byte type;
            byte size;
            byte x;
            byte y;
            Int16 a;        // angle
            Int16 f;        // field of vision / fire
        }


        public class t_hull
        {
            public char[] name = new char[32];
            public Int32 size;         // length in meters
            public Int32 hits;
            public Int32 mass;
            public Int32 numh;         // number of hardpoints
            public t_ik_sprite sprite;
            public t_ik_sprite silu;
            public t_hardpoint[] hardpts = new t_hardpoint[32];
        }


        public class t_shiptype
        {
            char[] name = new char[32];
            Int32 race;
            Int32 flag;
            Int32 hull;
            //	Int32 shield;
            Int32 hits;
            Int32 engine;
            Int32 thrust;
            Int32 speed;
            Int32 turn;
            Int32 sensor;
            Int32 num_systems;
            Int32 sys_eng, sys_thru;
            //	Int32 weapon[8];
            Int16[] system = new Int16[16];
            Int16[] sysdmg = new Int16[16];
        }


        public class t_ship
        {
            char[] name = new char[32];
            Int32 type;
            Int32 hits;

            Int32 shld;
            Int32 shld_type;
            Int32 shld_time;
            Int32 shld_charge;

            Int32 damage_time;
            Int32 dmgc_type;
            Int32 dmgc_time;

            Int32 cpu_type;
            Int32 ecm_type;
            Int32 clo_type;

            Int32 sys_thru;
            Int32 sys_shld;
            Int32 sys_dmgc;
            Int32 sys_cpu;
            Int32 sys_ecm;
            Int32 sys_clo;

            Int32 speed, turn;

            Int32[] wepfire = new Int32[8];
            Int32[] syshits = new Int32[16];
            Int32 own;

            Int32 x, y, a;          // location
            Int32 vx, vy, va;       // movement

            Int32 ds_x, ds_y, ds_s;     // display x, y, size (for mouse clicking)
            Int32 wp_x, wp_y, escaped, wp_time, flee;
            Int32 patx, paty;   // patrol

            Int32 cloaked, cloaktime;   // cloaktime is last time you cloaked/decloaked

            Int32 tel_x, tel_y, teltime;    // teleportation of zorg

            Int32 active;   // for spacehulk
            Int32 aistart;

            Int32 target;
            Int32 tac;
            Int32 angle;
            Int32 dist;

            Int32 launchtime;   // for carrier
            Int32 frange;

            Int32 bong_start, bong_end; // for babulon's bong artifact

        }


        public class t_shipweapon
        {
            char[] name = new char[32];
            Int32 stage;
            Int32 type;
            Int32 flags;
            t_ik_sprite sprite;
            Int32 size;
            Int32 sound1;
            Int32 sound2;
            Int32 rate;
            Int32 speed;
            Int32 damage;
            Int32 range;
            Int32 item;
        }


        public class t_shipsystem
        {
            char[] name = new char[32];
            Int16 type;
            Int16 size;
            Int32[] par = new Int32[4];
            Int32 item;
        }


        public class t_wepbeam
        {
            t_shipweapon wep;
            t_ship src;
            t_ship dst;

            Int32 stg;                  // if staged from a projectile
            Int32 ang, len;         // angle and length if missed
            Int32 stp, dsp;         // start, destination hardpoint (-1 = hull / shield)
            Int32 str, dmt, end;            // start, damage, expire time
        }


        public class t_wepproj
        {
            t_shipweapon wep;
            t_ship src;
            t_ship dst;                // target (for missiles)

            Int32 str, end;         // start, expire time

            Int32 x, y, a;          // location
            Int32 vx, vy, va;       // movement
            Int32 hits;                 // used for dispersing weapons
        }


        public class t_explosion
        {
            t_ik_spritepak spr;

            Int32 x, y, a;
            Int32 vx, vy, va;
            Int32 str, end;
            Int32 size, zoom;
            Int32 spin, fade;
            Int32 anim;
            Int32 cam;
        }


        public class t_combatcamera
        {
            public Int32 x, y, z;
            public Int32 ship_sel;
            public Int32 ship_trg;
            public Int32 time_sel;
            public Int32 time_trg;
            public Int32 drag_trg;
        }



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

        public static int IsMinimized;


        public static t_ik_image combatbg1;
        public static t_ik_image combatbg2;

        public static t_ik_spritepak spr_ships;
        public static t_ik_spritepak spr_shipsilu;
        public static t_ik_spritepak spr_weapons;
        public static t_ik_spritepak spr_explode1;
        public static t_ik_spritepak spr_shockwave;
        public static t_ik_spritepak spr_shield;

        public static t_hull hulls;
        public static int num_hulls;

        public static t_shiptype shiptypes;
        public static int num_shiptypes;

        public static t_shipweapon shipweapons;
        public static int num_shipweapons;

        public static t_shipsystem shipsystems;
        public static int num_shipsystems;

        public static t_ship ships;       // in combat
        public static int num_ships;

        public static char[,] racename = new char[16, 32];
        
        public static int[] enemies = new int[16];
        public static int num_enemies;

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
        public static Int32 klaktime;
        public static Int32 klakavail;
        public static Int32 gongavail;

        public static Int32 simulated;

        //# ifdef DEBUG_COMBAT
        public static char[] combatdebug = new char[64];
        //#endif

        // ----------------
        //    PROTOTYPES
        // ----------------

        // combat_init.cpp

        public static void combat_init() { throw new NotImplementedException(); }
        public static void combat_deinit() { throw new NotImplementedException(); }
        public static void sort_shiptype_systems(Int32 num) { throw new NotImplementedException(); }

        // combat.cpp

        public static Int32 combat(Int32 flt, Int32 sim) { throw new NotImplementedException(); }
        public static void select_ship(Int32 s, Int32 t) { throw new NotImplementedException(); }
        public static void combat_updateshipstats(Int32 s, Int32 t) { throw new NotImplementedException(); }
        public static void combat_findstuff2do(Int32 s, Int32 t) { throw new NotImplementedException(); }
        public static void combat_help_screen() { throw new NotImplementedException(); }
        public static void combat_SoundFX(int id, int srcx = 0, int volume = -1, int rate = -1)
        //Default value for srx should be camera.x
        { throw new NotImplementedException(); }

        // combat_sim.cpp

        public static void combat_sim() { throw new NotImplementedException(); }
        public static void combat_sim_end() { throw new NotImplementedException(); }

        // combat_display.cpp

        public static void combat_autocamera(Int32 t) { throw new NotImplementedException(); }
        public static void combat_display(Int32 t) { throw new NotImplementedException(); }

        // combat_weapons.cpp

        public static Int32 combat_findtarget(t_ship ship, Int32 hdp) { throw new NotImplementedException(); }
        public static void combat_fire(t_ship src, Int32 hdp, t_ship trg, Int32 start) { throw new NotImplementedException(); }
        public static Int32 combat_addbeam(t_shipweapon wep, t_ship src, Int32 hdp, t_ship trg, Int32 start, Int32 stg = -1) { throw new NotImplementedException(); }
        public static Int32 combat_addproj(t_ship src, Int32 hdp, t_ship trg, Int32 start) { throw new NotImplementedException(); }
        public static void combat_launchstages(Int32 p, Int32 num, Int32 start) { throw new NotImplementedException(); }
        public static Int32 combat_addexplo(Int32 x, Int32 y, t_ik_spritepak spr, Int32 spin, Int32 size, Int32 zoom, Int32 start, Int32 end, Int32 anim = -1, Int32 cam = 1) { throw new NotImplementedException(); }
        public static void combat_damageship(Int32 s, Int32 src, Int32 dmg, Int32 t, t_shipweapon wep, Int32 deb = 0) { throw new NotImplementedException(); }
        public static void combat_gethardpoint(t_ship ship, Int32 hdp, Int32 rx, Int32 ry) { throw new NotImplementedException(); }
        public static void combat_killship(Int32 s, Int32 t, Int32 quiet = 0) { throw new NotImplementedException(); }
    }
}