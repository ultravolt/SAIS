//#include <stdlib.h>
//#include <stdio.h>
//#include <string.h>
//
//#include "typedefs.h"
//#include "iface_globals.h"
//#include "is_fileio.h"
//
//#include "textstr.h"

namespace DigitalEeel
{
	public static partial class SAIS
	{
		//public static char[] textbuffer;
        //public static char[] textstring=new char[STR_MAX];

        public static void textstrings_init()
		{
			FILE ini;
            char[] s1 = new char[64];
            char[] s2 = new char[1024];
			char end;
			int num;
			int flag;
			char[] str;

			ini = myopen("gamedata/strings.ini", "rb");
			if (ini==null)
				return;

			//textbuffer = (char *)calloc(STRINGBUFFER_SIZE, sizeof(char));

			str = textbuffer;
			end = (char)0; num = 0; flag = 0;
			while (!AsBool(end))
			{
				end = (char)read_line(ini, s1, s2);
				if (!strcmp(s1, "STRINGS".ToCharArray()))
					flag = 1;
				else if (!strcmp(s1, "END".ToCharArray()))
					flag = 0;
				else if (AsBool(flag))
				{
					strcpy(str, s2);
					textstring[num] = str;
					//str += strlen(s2) + 1;
					num++;
				}
			}
			fclose(ini);

		}

        public static void textstrings_deinit()
		{
			//free(textbuffer);
		}
	}
}