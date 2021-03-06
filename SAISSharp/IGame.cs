namespace SAIS
{
    public partial class Game
    {
        string[] ecard_keywords =
        {
            "CARD",
            "NAME",
            "TEXT",
            "TEX2",
            "TYPE",
            "PARM",
            "END",
        };

        string[] hull_keywords = new string[]
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

        string[] shiptype_keywords = new string[]
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

        string[] shipweapon_keywords = new string[]
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

        string[] shipweapon_flagwords = new string[]
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

        string[] shipsystem_keywords = new string[]
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

        string[] race_keywords = new string[]
        {
            "RACE",
            "NAME",
            "TEXT",
            "TXT2",
            "END",
        };
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

        public enum ecard_types
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

        t_eventcard[] ecards;

        enum combat_tactics
        {
            tac_pursue = 0,
            tac_attack,
            tac_move,
            tac_flee,
        };

        const byte MAX_COMBAT_SHIPS = 32;
        const short MAX_COMBAT_PROJECTILES = 256;
        const short MAX_COMBAT_BEAMS = 256;
        const short MAX_COMBAT_EXPLOS = 256;

        const byte COMBAT_FRAMERATE = 50;

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

        int IsMinimized;

        t_ship[] ships;       // in combat
        int num_ships;

        string[] racename;//[16][32];
        int num_races;
        int simulated = 0;
        short COMBAT_INTERFACE_COLOR = (11);// + simulated);

        // ----------------
        // GLOBAL VARIABLES
        // ----------------

        t_job jobs;
        int num_jobs;

        int num_scores;
        t_score[] scores = new t_score[20];

        int got_hiscore;

        // GFX GLOBALS

        t_ik_image screen;
        byte[] globalpal = new byte[768];
        byte[] currentpal = new byte[768];
        t_ik_image screenbuf;
        int gfx_width, gfx_height, gfx_fullscreen, gfx_switch;
        int gfx_redraw;
        int c_minx, c_miny, c_maxx, c_maxy;

        int[] gfx_transbuffer;
        int[] gfx_lightbuffer;
        int[] gfx_addbuffer;

        int[] sin1k = new int[1024];
        int[] cos1k = new int[1024];



        const string SAIS_VERSION_NUMBER = "v1.5";

        t_gamesettings settings = new t_gamesettings();


        // INTERFACE GLOBALS
        int ik_mouse_x;
        int ik_mouse_y;
        int ik_mouse_b;
        int ik_mouse_c;
        int must_quit;
        int wants_screenshot;

        int key_left;
        int key_right;
        int key_up;
        int key_down;
        int[] key_f = new int[10];
        int key_fire1;
        int key_fire2;
        int key_fire2b;

        // ----------------
        //    CONSTANTS
        // ----------------

        // for interface.cpp / interface_initsprites()
        const byte IF_BORDER_TRANS = 0;
        const byte IF_BORDER_SOLID = 9;
        const byte IF_BORDER_PORTRAIT = 18;
        const byte IF_BORDER_RADAR = 19;
        const byte IF_BORDER_FLAT = 20;
        const byte IF_BORDER_SMALL = 21;

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

        t_ik_spritepak spr_titles;

        t_ik_spritepak spr_IFborder;
        t_ik_spritepak spr_IFbutton;
        t_ik_spritepak spr_IFslider;
        t_ik_spritepak spr_IFarrows;
        t_ik_spritepak spr_IFsystem;
        t_ik_spritepak spr_IFtarget;
        t_ik_spritepak spr_IFdifnebula;
        t_ik_spritepak spr_IFdifenemy;

        t_ik_font font_4x8;
        t_ik_font font_6x8;

        int last_logdate;
        string moddir;//[256];
                      ////{{NO_DEPENDENCIES}}
                      //// Microsoft Developer Studio generated include file.
                      //// Used by Script1.rc
                      ////
                      //#define IDI_ICON1                       101

        //// Next default values for new objects
        //// 
        //#ifdef APSTUDIO_INVOKED
        //#ifndef APSTUDIO_READONLY_SYMBOLS
        //#define _APS_NEXT_RESOURCE_VALUE        102
        //#define _APS_NEXT_COMMAND_VALUE         40001
        //#define _APS_NEXT_CONTROL_VALUE         1000
        //#define _APS_NEXT_SYMED_VALUE           101
        //#endif
        //#endif
        const byte CHN_SFX = 0;
        const byte NUM_SFX = 15;


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

        const sfxsamples SND_BEAMS = sfxsamples.WAV_BEAM1;
        const sfxsamples SND_PROJS = sfxsamples.WAV_PROJ1;
        const sfxsamples SND_HITS = sfxsamples.WAV_HIT1;
        const sfxsamples SND_ITEMS = sfxsamples.WAV_WEAPON;
        const sfxsamples SND_ARTIF = sfxsamples.WAV_PLATINUM;

        t_sfxchannel[] sfxchan = new t_sfxchannel[NUM_SFX];
        t_wavesound[] wavesnd = new t_wavesound[(int)sfxsamples.WAV_MAX];

        int[] m_freq = new int[4];
        byte m_mainvol;
        byte s_volume;
        t_song m_song;
        byte m_playing;


        const byte STARMAP_FRAMERATE = 50;
        const byte SM_MAP_X = 160;
        const byte SM_MAP_Y = 0;
        const byte SM_SHIP_X = 0;
        const byte SM_SHIP_Y = 0;
        const byte SM_INV_X = 0;
        const short SM_INV_Y = 256;
        const byte SM_SEL_X = 0;
        const short SM_SEL_Y = 384;

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

        const byte STARMAP_INTERFACE_COLOR = 11;

        const byte STARMAP_MAX_FLEETS = 8;

        const byte RC_MUCRON = 9;
        const byte RC_PLANET = 10;
        const byte RC_FOMAX = 11;
        const byte RC_BLOWUP = 12;
        const byte RC_HOLED = 13;
        const byte RC_LOST = 14;

        const byte NUM_FLEETS = 5;
        const byte NUM_STARSYSTEMS = 16;
        const byte NUM_EVENTS = 2;
        const byte NUM_ALLIES = 2;
        const byte NUM_ITEMS = 6;
        const byte NUM_RAREITEMS = 2;
        const byte NUM_LIFEFORMS = 3;

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

        t_month[] months = new t_month[12];

        t_race[] races = new t_race[16];

        t_racefleet[] racefleets = new t_racefleet[16];
        int num_racefleets;

        t_planettype[] platypes;
        int num_platypes;

        t_startype[] startypes;
        int num_startypes;

        t_itemtype[] itemtypes;
        int num_itemtypes;

        t_starsystem[] sm_stars;
        int num_stars;

        t_blackhole[] sm_holes;
        int num_holes;

        t_nebula[] sm_nebula;
        int num_nebula;

        t_fleet[] sm_fleets = new t_fleet[STARMAP_MAX_FLEETS];

        t_ik_spritepak spr_SMstars;
        t_ik_spritepak spr_SMstars2;
        t_ik_spritepak spr_SMplanet;
        t_ik_spritepak spr_SMplanet2;
        t_ik_spritepak spr_SMnebula;
        t_ik_spritepak spr_SMraces;

        byte[] sm_nebulamap;
        t_ik_image sm_nebulagfx;
        t_ik_image sm_starfield;

        int[,] star_env = new int[8, 8];
        // char						pltype_name[10][32];
        int[] plgfx_type = new int[256];
        int num_plgfx;

        int homesystem;
        t_player player;
        t_hud hud;

        int[] kla_items = new int[32];
        int kla_numitems;

        string[] captnames = new string[16];
        int num_captnames;
        string[] shipnames = new string[16];
        int num_shipnames;

        int starmap_tutorialtype;

        const int STRINGBUFFER_SIZE = 128 * 1024;

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

        string textbuffer;
        string textstring;

        public short COMBAT_INTERFACE_COLOR1 { get => COMBAT_INTERFACE_COLOR; set => COMBAT_INTERFACE_COLOR = value; }
    }
}