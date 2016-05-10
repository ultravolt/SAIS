using System;

namespace DigitalEeel
{
    public static partial class SAIS
    {


        public const int CHN_SFX = 0;
        public const int NUM_SFX = 15;


        public enum sfxsamples
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

        public const sfxsamples SND_BEAMS = sfxsamples.WAV_BEAM1;
        public const sfxsamples SND_PROJS = sfxsamples.WAV_PROJ1;
        public const sfxsamples SND_HITS = sfxsamples.WAV_HIT1;
        public const sfxsamples SND_ITEMS = sfxsamples.WAV_WEAPON;
        public const sfxsamples SND_ARTIF = sfxsamples.WAV_PLATINUM;
        public const int WAV_MAX = 0;//No definition?

        public class t_wavesound
        {
            char[] name = new char[64];
            object wave;
        };

        public class t_sfxchannel
        {
            int id;      // sample playing
            int st, et;  // start and end time
        }

        public static t_sfxchannel[] sfxchan = new t_sfxchannel[NUM_SFX];
        public static t_wavesound[] wavesnd = new t_wavesound[WAV_MAX];

        // ******** SOUND *********

        public static int Load_WAV(char[] filename, int id) { throw new NotImplementedException(); }
        public static void load_all_sfx() { throw new NotImplementedException(); }
        public static int Delete_Sound(int id) { throw new NotImplementedException(); }
        public static int Delete_All_Sounds() { throw new NotImplementedException(); }

        public static int Play_Sound(int id, int ch, int flags = 0, int volume = -1, int rate = -1, int pan = 0) { throw new NotImplementedException(); }
        public static int Play_SoundFX(int id, int t = 0, int volume = -1, int rate = -1, int pan = 0, int cutoff = 30) { throw new NotImplementedException(); }
        public static int Set_Sound_Volume(int ch, int vol) { throw new NotImplementedException(); }
        public static int Set_Sound_Freq(int ch, int freq) { throw new NotImplementedException(); }
        public static int Set_Sound_Pan(int ch, int pan) { throw new NotImplementedException(); }
        public static int Stop_Sound(int ch) { throw new NotImplementedException(); }
        public static int Stop_All_Sounds() { throw new NotImplementedException(); }
        public static int Status_Sound(int ch) { throw new NotImplementedException(); }
        public static int Get_Sound_Size(int id) { throw new NotImplementedException(); }
        public static int Get_Sound_Rate(int id) { throw new NotImplementedException(); }

        // ********* MUSIC ********

        public class t_song
        {
            byte[] volseq = new byte[64];
            byte[] panseq = new byte[64];
            byte[] sync = new byte[4];
            byte[] samp = new byte[4];
        }

        public static Int32[] m_freq = new Int32[4];
        public static byte m_mainvol;
        public static byte s_volume;
        public static t_song m_song;
        public static byte m_playing;

        public static void start_music() { throw new NotImplementedException(); }
        public static void upd_music(int pos) { throw new NotImplementedException(); }
        public static int m_get_pan(int ch, int pos) { throw new NotImplementedException(); }
        public static int m_get_vol(int ch, int pos) { throw new NotImplementedException(); }

        public static void save_cur_music(char[] fname) { throw new NotImplementedException(); }
        public static void load_cur_music(char[] fname) { throw new NotImplementedException(); }
        public static void prep_music(int n) { throw new NotImplementedException(); } // copy from songs[] to song
        public static void plop_music(int n) { throw new NotImplementedException(); } // copy from song to songs[]
    }
}