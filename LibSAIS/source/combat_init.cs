// ----------------
//     INCLUDES
// ----------------

//#include <stdlib.h>
//#include <stdio.h>
//#include <string.h>

//#include "typedefs.h"
//#include "iface_globals.h"
//#include "is_fileio.h"
//#include "gfx.h"
//#include "snd.h"
//#include "starmap.h"
//#include "combat.h"
using System;
using System.Collections.Generic;

namespace DigitalEeel
{
    public static partial class SAIS
    {
        // ----------------
        //     CONSTANTS
        // ----------------

        public static string[] hull_keywords = new string[]
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

        public static string[] shiptype_keywords = new string[]
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

        public static string[] shipweapon_keywords = new string[]
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

        public static string[] shipweapon_flagwords = new string[]
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

        public static string[] shipsystem_keywords = new string[]
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

        public static string[] race_keywords = new string[]
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
        /*
        public static t_ik_image combatbg1;
        public static t_ik_image combatbg2;

        public static t_ik_spritepak spr_ships;
        public static t_ik_spritepak spr_shipsilu;
        public static t_ik_spritepak spr_weapons;
        public static t_ik_spritepak spr_explode1;
        public static t_ik_spritepak spr_shockwave;
        public static t_ik_spritepak spr_shield;

        t_hull* hulls;
        int num_hulls;

        t_shiptype* shiptypes;
        int num_shiptypes;

        t_shipweapon* shipweapons;
        int num_shipweapons;

        t_shipsystem* shipsystems;
        int num_shipsystems;

        //char						racename[16][32];
        //int							num_races;
        int enemies[16];
        int num_enemies;
        */
        // ----------------
        // LOCAL PROTOTYPES
        // ----------------
        /*
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
        */
        // ----------------
        // GLOBAL FUNCTIONS
        // ----------------

        public static void combat_init()
        {
            initraces();
            combat_initsprites();
            combat_initshipweapons();
            combat_initshipsystems();
            combat_inithulls();
            combat_initshiptypes();
        }

        public static void combat_deinit()
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

        public static void combat_inithulls()
        {
            FILE ini;
            char[] s1 = new char[64];
            char[] s2 = new char[256];
            char end;
            int num;
            int flag;
            int n, com;

            char[] ts1 = new char[64];
            int tv1 = 0, tv2 = 0, tv3 = 0, tv4 = 0, tv5 = 0;

            ini = myopen("gamedata/hulls.ini", "rb");
            if (ini == null)
                return;

            end = (char)0; num = 0;
            while (!AsBool(end))
            {
                end = (char)read_line(ini, s1, s2);
                if (!AsBool(strcmp(s1, hull_keywords[(int)hull_keyids.hlkBegin])))
                    num++;
            }
            fclose(ini);

            hulls = new List<t_hull>();// (t_hull*)calloc(num, sizeof(t_hull));
            if (hulls == null)
                return;
            num_hulls = num;

            ini = myopen("gamedata/hulls.ini", "rb");

            end = (char)0; num = 0; flag = 0;
            while (!AsBool(end))
            {
                end = (char)read_line(ini, s1, s2);
                com = -1;
                for (n = 0; n < (int)hull_keyids.hlkMax; n++)
                    if (!AsBool(strcmp(s1, hull_keywords[n])))
                        com = n;

                if (flag == 0)
                {
                    if (com == (int)hull_keyids.hlkBegin)
                    {
                        hulls[num].numh = 0;
                        flag = 1;
                    }
                }
                else switch (com)
                    {
                        case (int)hull_keyids.hlkName:
                            strcpy(hulls[num].name, s2);
                            break;

                        case (int)hull_keyids.hlkSize:
                            sscanf(s2, "%d", tv1);
                            hulls[num].size = tv1;
                            break;

                        case (int)hull_keyids.hlkHits:
                            sscanf(s2, "%d", tv1);
                            hulls[num].hits = tv1;
                            break;

                        case (int)hull_keyids.hlkMass:
                            sscanf(s2, "%d", tv1);
                            hulls[num].mass = tv1;
                            break;

                        case (int)hull_keyids.hlkSprite:
                            sscanf(s2, "%s %d", ts1, tv2);
                            hulls[num].sprite = spr_ships.spr[tv2];
                            break;

                        case (int)hull_keyids.hlkSilu:
                            sscanf(s2, "%s %d", ts1, tv2);
                            hulls[num].silu = spr_shipsilu.spr[tv2];
                            break;

                        case (int)hull_keyids.hlkWeapon:
                        case (int)hull_keyids.hlkEngine:
                        case (int)hull_keyids.hlkThruster:
                        case (int)hull_keyids.hlkFighter:
                            sscanf(s2, "%d %d %d %d %d", tv1, tv2, tv3, tv4, tv5);
                            hulls[num].hardpts[hulls[num].numh].type = (byte)((int)hull_hardpttypes.hdpWeapon + (com - (int)hull_keyids.hlkWeapon));
                            hulls[num].hardpts[hulls[num].numh].x = (byte)tv1;
                            hulls[num].hardpts[hulls[num].numh].y = (byte)tv2;
                            hulls[num].hardpts[hulls[num].numh].a = (short)((tv3 * 1024) / 360);
                            hulls[num].hardpts[hulls[num].numh].f = (short)((tv4 * 1024) / 360);
                            hulls[num].hardpts[hulls[num].numh].size = (byte)tv5;
                            hulls[num].numh++;
                            break;

                        case (int)hull_keyids.hlkEnd:
                            num++; flag = 0;
                            break;

                        default: break;
                    }

            }
            fclose(ini);
        }

        private static void sscanf(char[] s2, string v, int tv1, int tv2, int tv3, int tv4, int tv5)
        {
            throw new NotImplementedException();
        }

        private static void sscanf(char[] s2, string v, char[] ts1, int tv2)
        {
            throw new NotImplementedException();
        }

        private static void sscanf(char[] s2, string v, int tv1)
        {
            throw new NotImplementedException();
        }

        public static void combat_deinithulls()
        {
            num_hulls = 0;
            //free(hulls);
            hulls = null;
        }

        public static void combat_initshiptypes()
        {
            FILE ini;
            char[] s1 = new char[64];
            char[] s2 = new char[256];
            char end=(char)0;
            int num=0;
            int flag=0;
            int n=0, com=0;
            int wep=0;

            ini = myopen("gamedata/ships.ini", "rb");
            if (ini == null)
                return;

            end = (char)0; num = 0;
            flag = 0; num_enemies = 0;
            while (!AsBool(end))
            {
                end = (char)read_line(ini, s1, s2);
                if (!AsBool(strcmp(s1, shiptype_keywords[(int)shiptype_keyids.shkBegin])))
                    num++;
                if (!AsBool(strcmp(s1, "ENEMIES")))
                { flag = 2; n = 0; }
                else if (flag > 0 && (strcmp(s1, "END") == 0))
                    flag = 0;
                else if (flag > 0)
                {
                    if (flag == 2)
                    {
                        for (com = 0; com < num_races; com++)
                            if (!strcmp(races[com].name, s2))
                            {
                                enemies[n] = com;
                                num_enemies++;
                                n++;
                            }
                    }
                }

            }
            fclose(ini);

            shiptypes = new List<t_shiptype>();// (t_shiptype*)calloc(num, sizeof(t_shiptype));
            if (shiptypes == null)
                return;
            num_shiptypes = num;

            ini = myopen("gamedata/ships.ini", "rb");

            end = (char)0; num = 0; flag = 0;
            while (!AsBool(end))
            {
                end = (char)read_line(ini, s1, s2);
                com = -1;
                for (n = 0; n < (int)shiptype_keyids.shkMax; n++)
                    if (!AsBool(strcmp(s1, shiptype_keywords[n])))
                        com = n;

                if (flag == 0)
                {
                    if (com == (int)shiptype_keyids.shkBegin)
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
                else switch (com)
                    {
                        case (int)shiptype_keyids.shkName:
                            strcpy(shiptypes[num].name, s2);
                            break;

                        case (int)shiptype_keyids.shkRace:
                            for (n = 0; n < num_races; n++)
                                if (!strcmp(races[n].name, s2))
                                    shiptypes[num].race = n;
                            break;

                        case (int)shiptype_keyids.shkFlag:
                            sscanf(s2, "%d", n);
                            shiptypes[num].flag = n;
                            break;

                        case (int)shiptype_keyids.shkHull:
                            for (n = 0; n < num_hulls; n++)
                                if (!strcmp(hulls[n].name, s2))
                                    shiptypes[num].hull = n;
                            break;

                        case (int)shiptype_keyids.shkSystem:
                        case (int)shiptype_keyids.shkWeapon:
                        case (int)shiptype_keyids.shkEngine:
                        case (int)shiptype_keyids.shkThruster:
                            for (n = 0; n < num_shipsystems; n++)
                                if (!strcmp(shipsystems[n].name, s2))
                                {
                                    shiptypes[num].system[shiptypes[num].num_systems] = (short)n;
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

                        case (int)shiptype_keyids.shkEnd:
                            sort_shiptype_systems(num);
                            num++; flag = 0;
                            break;

                        default: break;
                    }

            }
            fclose(ini);
        }

        private static int strcmp(char[] s1, string s2)
        {
            throw new NotImplementedException();
        }

        private static int strcmp(string s1, char[] s2)
        {
            throw new NotImplementedException();
        }
        public static void combat_deinitshiptypes()
        {
            num_shiptypes = 0;
            shiptypes = null;
        }

        public static void combat_initshipweapons()
        {
            FILE ini;
            char[] s1 = new char[64];
            char[] s2 = new char[256];
            char end;
            int num;
            int flag;
            int n, com;

            char[] ts1 = new char[64];
            List<char[]> ts = new List<char[]>();
            int tv1=0, tv2=0;

            ini = myopen("gamedata/weapons.ini", "rb");
            if (ini == null)
                return;

            end = (char)0; num = 0;
            while (!AsBool(end))
            {
                end = (char)read_line(ini, s1, s2);
                if (!AsBool(strcmp(s1, shipweapon_keywords[(int)weapon_keyids.wpkBegin])))
                    num++;
            }

            fclose(ini);

            shipweapons = new List<t_shipweapon>();// (t_shipweapon*)calloc(num, sizeof(t_shipweapon));
            if (shipweapons == null)
                return;
            num_shipweapons = num;

            ini = myopen("gamedata/weapons.ini", "rb");

            end = (char)0; num = 0; flag = 0;
            while (!AsBool(end))
            {
                end = (char)read_line(ini, s1, s2);
                com = -1;
                for (n = 0; n < (int)weapon_keyids.wpkMax; n++)
                    if (!AsBool(strcmp(s1, shipweapon_keywords[n])))
                        com = n;

                if (flag == 0)
                {
                    if (com == (int)weapon_keyids.wpkBegin)
                    {
                        flag = 1;
                        shipweapons[num].item = -1;
                    }
                }
                else switch (com)
                    {
                        case (int)weapon_keyids.wpkName:

                            strcpy(shipweapons[num].name, s2);
                            shipweapons[num].flags = 0;
                            break;

                        case (int)weapon_keyids.wpkStage:
                            for (n = 0; n < num; n++)
                                if (!strcmp(shipweapons[n].name, s2))
                                    shipweapons[num].stage = n;
                            break;

                        case (int)weapon_keyids.wpkType:

                            sscanf(s2, "%d", tv1);
                            shipweapons[num].type = tv1;
                            break;

                        case (int)weapon_keyids.wpkFlag:
                            for (n = 0; n < 4; n++)

                                strcpy(ts[n], "".ToCharArray());

                            sscanf(s2, "%s %s %s %s", ts[0], ts[1], ts[2], ts[3]);
                            for (n = 0; n < 4; n++)
                                for (tv1 = 0; tv1 < (int)weapon_flagids.wpfMax; tv1++)
                                    if (!AsBool(strcmp(shipweapon_flagwords[tv1], ts[n])))
                                        shipweapons[num].flags |= (1 << tv1);
                            break;

                        case (int)weapon_keyids.wpkSprite:

                            sscanf(s2, "%s %d", ts1, tv2);
                            shipweapons[num].sprite = spr_weapons.spr[tv2];
                            break;

                        case (int)weapon_keyids.wpkSize:

                            sscanf(s2, "%d", tv1);
                            shipweapons[num].size = tv1;
                            break;

                        case (int)weapon_keyids.wpkSound1:

                            sscanf(s2, "%d", tv1);
                            if (shipweapons[num].type == 1)
                                tv1 += (int)SND_PROJS;
                            else
                                tv1 += (int)SND_BEAMS;
                            shipweapons[num].sound1 = tv1;
                            break;

                        case (int)weapon_keyids.wpkSound2:

                            sscanf(s2, "%d", tv1);
                            tv1 += (int)SND_HITS;
                            shipweapons[num].sound2 = tv1;
                            break;

                        case (int)weapon_keyids.wpkRate:

                            sscanf(s2, "%d", tv1);
                            shipweapons[num].rate = tv1;
                            break;

                        case (int)weapon_keyids.wpkSpeed:

                            sscanf(s2, "%d", tv1);
                            shipweapons[num].speed = tv1;
                            break;

                        case (int)weapon_keyids.wpkDamage:

                            sscanf(s2, "%d", tv1);
                            shipweapons[num].damage = tv1;
                            break;

                        case (int)weapon_keyids.wpkRange:

                            sscanf(s2, "%d", tv1);
                            shipweapons[num].range = tv1;
                            break;

                        case (int)weapon_keyids.wpkEnd:
                            num++; flag = 0;
                            break;

                        default: break;
                    }

            }

            fclose(ini);
        }

        private static void sscanf(char[] s2, string v1, char[] v2, char[] v3, char[] v4, char[] v5)
        {
            throw new NotImplementedException();
        }

        public static void combat_deinitshipweapons()
        {
            num_shipweapons = 0;
            shipweapons = null;
        }

        public static void combat_initshipsystems()
        {
            FILE ini;
            char[] s1 = new char[64];
            char[] s2 = new char[256];
            char end=(char)0;
            int num=0;
            int flag=0;
            int n=0, com=0;
            int tv1=0;

            List<char[]> systype = new List<char[]>();
            Int32 num_systypes;

            ini = myopen("gamedata/systems.ini", "rb");
            if (ini==null)
                return;

            end = (char)0; num = 0;
            flag = 0; num_systypes = 0;
            while (!AsBool(end))
            {
                end = (char)read_line(ini, s1, s2);
                if (!AsBool(strcmp(s1, shipsystem_keywords[(int)system_keyids.sykBegin])))
                    num++;
                if (!AsBool(strcmp(s1, "SYSTEMTYPES")))
                { flag = 1; n = 0; }
                else if (flag > 0 && strcmp(s1, "END") == 0)
                    flag = 0;
                else if (flag == 1)
                {

                    strcpy(systype[n], s1);
                    num_systypes++;
                    n++;
                }

            }

            fclose(ini);

            shipsystems = new List<t_shipsystem>();// (t_shipsystem*)calloc(num, sizeof(t_shipsystem));
            if (shipsystems == null)
                return;
            num_shipsystems = num;

            ini = myopen("gamedata/systems.ini", "rb");

            end = (char)0; num = 0; flag = 0;
            while (!AsBool(end))
            {
                end = (char)read_line(ini, s1, s2);
                com = -1;
                for (n = 0; n < (int)system_keyids.sykMax; n++)
                    if (!AsBool(strcmp(s1, shipsystem_keywords[n])))
                        com = n;

                if (flag == 0)
                {
                    if (com == (int)system_keyids.sykBegin)
                    {
                        flag = 1;
                        shipsystems[num].item = -1;
                    }
                }
                else switch (com)
                    {
                        case (int)system_keyids.sykName:

                            strcpy(shipsystems[num].name, s2);
                            break;

                        case (int)system_keyids.sykType:
                            for (n = 0; n < num_systypes; n++)
                                if (!strcmp(s2, systype[n]))
                                    shipsystems[num].type = (short)n;
                            if (shipsystems[num].type == (int)system_types.sys_weapon)
                            {
                                shipsystems[num].par[0] = -1;
                                for (n = 0; n < num_shipweapons; n++)
                                    if (!strcmp(shipsystems[num].name, shipweapons[n].name))
                                        shipsystems[num].par[0] = n;
                            }
                            break;

                        case (int)system_keyids.sykSize:

                            sscanf(s2, "%d", tv1);
                            shipsystems[num].size = (short)tv1;
                            break;

                        case (int)system_keyids.sykParam1:
                        case (int)system_keyids.sykParam2:
                        case (int)system_keyids.sykParam3:
                        case (int)system_keyids.sykParam4:


                            sscanf(s2, "%d", tv1);
                            shipsystems[num].par[com - (int)system_keyids.sykParam1] = tv1;
                            break;

                        case (int)system_keyids.sykEnd:
                            num++; flag = 0;
                            break;

                        default: break;
                    }

            }

            fclose(ini);
        }

        public static void combat_deinitshipsystems()
        {
            num_shipsystems = 0;
            shipsystems = null;
            //free(shipsystems);
        }

        public static void combat_initsprites()
        {
            t_ik_image pcx;
            int x, y, n;

            spr_ships = load_sprites("graphics/ships.spr");
            spr_shipsilu = load_sprites("graphics/shipsilu.spr");
            spr_weapons = load_sprites("graphics/weapons.spr");
            spr_explode1 = load_sprites("graphics/explode1.spr");
            spr_shockwave = load_sprites("graphics/shockwav.spr");
            spr_shield = load_sprites("graphics/shield.spr");

            combatbg1 = ik_load_pcx("graphics/combtbg1.pcx", null);
            combatbg2 = ik_load_pcx("graphics/combtbg2.pcx", null);

            if (spr_ships==null)
            {
                spr_ships = new_spritepak(24);
                pcx = ik_load_pcx("ships.pcx", null);
                for (n = 0; n < 24; n++)

                {
                    x = n % 5; y = n / 5;
                    spr_ships.spr[n] = get_sprite(pcx, x * 64, y * 64, 64, 64);
                }

                del_image(pcx);
                save_sprites("graphics/ships.spr", spr_ships);
            }

            if (spr_shipsilu==null)
            {
                spr_shipsilu = new_spritepak(24);
                pcx = ik_load_pcx("silu.pcx", null);
                for (n = 0; n < 24; n++)

                {
                    x = n % 5; y = n / 5;
                    spr_shipsilu.spr[n] = get_sprite(pcx, x * 128, y * 128, 128, 128);
                }

                del_image(pcx);
                save_sprites("graphics/shipsilu.spr", spr_shipsilu);
            }

            if (spr_weapons==null)
            {
                spr_weapons = new_spritepak(19);
                pcx = ik_load_pcx("weaponfx.pcx", null);

                for (n = 0; n < 5; n++)
                {
                    spr_weapons.spr[n] = get_sprite(pcx, n * 32, 0, 32, 32);
                }
                for (n = 0; n < 5; n++)
                {
                    spr_weapons.spr[n + 5] = get_sprite(pcx, n * 32, 32, 32, 32);
                }
                for (n = 0; n < 4; n++)
                {
                    spr_weapons.spr[n + 10] = get_sprite(pcx, n * 32, 64, 32, 32);
                }
                spr_weapons.spr[14] = get_sprite(pcx, 192, 64, 128, 128);
                spr_weapons.spr[15] = get_sprite(pcx, 0, 96, 32, 32);
                spr_weapons.spr[16] = get_sprite(pcx, 160, 0, 32, 32);
                spr_weapons.spr[17] = get_sprite(pcx, 128, 64, 32, 32);
                spr_weapons.spr[18] = get_sprite(pcx, 32, 96, 32, 32);

                del_image(pcx);
                save_sprites("graphics/weapons.spr", spr_weapons);
            }

            if (spr_explode1==null)
            {
                spr_explode1 = new_spritepak(10);
                pcx = ik_load_pcx("xplosion.pcx", null);

                for (n = 0; n < 10; n++)
                {
                    x = n % 5; y = n / 5;
                    spr_explode1.spr[n] = get_sprite(pcx, x * 64, y * 64, 64, 64);
                }

                del_image(pcx);
                save_sprites("graphics/explode1.spr", spr_explode1);
            }

            if (spr_shockwave==null)
            {
                spr_shockwave = new_spritepak(5);
                pcx = ik_load_pcx("shock.pcx", null);

                for (n = 0; n < 5; n++)
                {
                    spr_shockwave.spr[n] = get_sprite(pcx, (n % 3) * 128, (n / 3) * 128, 128, 128);
                }

                del_image(pcx);
                save_sprites("graphics/shockwav.spr", spr_shockwave);
            }

            if (spr_shield==null)
            {
                spr_shield = new_spritepak(5);
                pcx = ik_load_pcx("shields.pcx", null);

                for (n = 0; n < 5; n++)
                {
                    spr_shield.spr[n] = get_sprite(pcx, n * 128, 0, 128, 128);
                }

                del_image(pcx);
                save_sprites("graphics/shield.spr", spr_shield);
            }

        }

        public static void combat_deinitsprites()
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

        public static void initraces()
        {
            FILE ini;
            char[] s1 = new char[64];
            char[] s2 = new char[256];
            char end;
            int num;
            int flag;
            int n, com;

            ini = myopen("gamedata/races.ini", "rb");
            if (ini==null)
                return;

            end = (char)0; num = 0; flag = 0;
            while (!AsBool(end))
            {
                end = (char)read_line(ini, s1, s2);
                com = -1;
                for (n = 0; n < (int)race_keyids.rckMax; n++)
                    if (!AsBool(strcmp(s1, race_keywords[n])))
                        com = n;

                if (flag == 0)
                {
                    if (com == (int)race_keyids.rckBegin)
                    {
                        races[num].fleet = -1;
                        flag = 1;
                    }
                }
                else switch (com)
                    {
                        case (int)race_keyids.rckName:
                            strcpy(races[num].name, s2);
                            break;

                        case (int)race_keyids.rckText:
                            strcpy(races[num].text, s2);
                            break;

                        case (int)race_keyids.rckText2:
                            strcpy(races[num].text2, s2);
                            break;

                        case (int)race_keyids.rckEnd:
                            num++; flag = 0;
                            break;

                        default: break;
                    }

            }
            num_races = num;
            fclose(ini);
        }

        public static void sort_shiptype_systems(Int32 num)
        {
            int n=0, c=0;
            int w=0, t=0;

            
            // systems are sorted by type (weapons first to match hardpoints)
            for (n = 0; n < shiptypes[num].num_systems; n++)
            {
                c = n;
                while (c > 0 && shipsystems[shiptypes[num].system[c]].type < shipsystems[shiptypes[num].system[c - 1]].type)
                {
                    t = shiptypes[num].system[c];
                    shiptypes[num].system[c] = shiptypes[num].system[c - 1];
                    shiptypes[num].system[c - 1] = (short)t;
                    t = shiptypes[num].sysdmg[c];
                    shiptypes[num].sysdmg[c] = shiptypes[num].sysdmg[c - 1];
                    shiptypes[num].sysdmg[c - 1] = (short)t;
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
                if (shipsystems[shiptypes[num].system[n]].type == (int)system_types.sys_thruster)
                {
                    shiptypes[num].thrust = shiptypes[num].system[n];
                    shiptypes[num].speed = (shipsystems[shiptypes[num].system[n]].par[0] * 32) / hulls[shiptypes[num].hull].mass;
                    shiptypes[num].turn = (shipsystems[shiptypes[num].system[n]].par[0] * 3) / hulls[shiptypes[num].hull].mass + 1;
                    shiptypes[num].sys_thru = n;
                }
                else if (shipsystems[shiptypes[num].system[n]].type == (int)system_types.sys_engine)
                { shiptypes[num].engine = shiptypes[num].system[n]; shiptypes[num].sys_eng = n; }
                else if (shipsystems[shiptypes[num].system[n]].type == (int)system_types.sys_sensor)
                    shiptypes[num].sensor = shipsystems[shiptypes[num].system[n]].par[0];
            }
        }
    }
}