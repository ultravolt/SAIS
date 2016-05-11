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
#using <LibSAIS.dll>
//another using namespace directive.
using namespace System;
//using namespace LibSAIS;