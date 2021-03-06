#include <vector>
#include <map>
#include <string>
#include <memory>

static std::vector<std::shared_ptr<System_String> > _stringJumps;

void buildTypesTable();
void buildStringTable();
void mapLibs();
void RuntimeHelpersBuildConstantTable();
bool IsInstanceOf(int typeSource, int typeImplementation);


void initializeRuntime()
{	
	buildTypesTable();
	buildStringTable();
	mapLibs();
	RuntimeHelpersBuildConstantTable();
}

template<class T>
void System_Array_Resize(std::shared_ptr< Array<T> >* arr, int newSize)
{
	auto oldSize = (*arr)->Length;
	(*arr)->Length = newSize;
	for(auto i = newSize; i<oldSize;i++)
	{
		T cleanInstance;
		(*arr)->Items[i] = cleanInstance;
	}
	(*arr)->Items = (T*)realloc((*arr)->Items, sizeof(T)*newSize);
}


template<class T>
struct BoxedT : public System_Object
{
	T Data;
};
template<class T>
std::shared_ptr<System_Object> box_value(T value, int typeId){
	auto result = std::make_shared<BoxedT< T > >();
	result->_typeId = typeId;
	result->Data = value;
	return result;
}

template<class T>
T unbox_value(std::shared_ptr<System_Object> value){
	auto resultObject = value.get();
	auto castedUnboxing = (BoxedT<T>*)resultObject;
	return castedUnboxing->Data;
}


extern const wchar_t _stringTable[];
std::shared_ptr<System_String> _str(int index)
{
	return _stringJumps[index];
}

void _AddJumpAndLength(int jump, int length)
{
	auto resultData = &(_stringTable[jump]);
	
	auto result = std::make_shared<System_String>();
	auto value = std::make_shared<Array < System_Char >>(length+1, resultData);
	result->Text =  value;
	_stringJumps.push_back(result);
}

static std::vector<System_Byte*> _constTables;
System_Byte* RuntimeHelpers_GetBytes(int id)
{
	return _constTables[id];
}

void AddConstantByteArray(System_Byte* data)
{
	_constTables.push_back(data);
}
#ifdef _WIN32

#include <windows.h>

HMODULE LoadNativeLibrary(const System_Char* dllFileName)
{
	return LoadLibraryW(dllFileName);
}

void* LoadNativeMethod(HMODULE module, const char* methodName)
{
	return (void*)GetProcAddress(module, methodName);
}
#else
#include <dlfcn.h>

void* LoadNativeLibrary(const char* dllFileName)
{
	return dlopen(dllFileName, RTLD_LAZY);
}

void* LoadNativeMethod(void* module, const char* methodName)
{
	return (void*)dlsym(module, methodName);
}
#endif

#include <stdio.h>
#include <math.h>

std::shared_ptr<Array < std::shared_ptr<System_String> > > System_getArgumentsAsList(int argc, char**argv)
{
	auto result = new Array <std::shared_ptr<System_String> >(argc);
	for(auto i=0;i<argc;i++){
		//std::shared_ptr<System::String> newString (new System::String(argv[i])); 
		//(*result)[i] = newString;
	}
	return std::shared_ptr< Array <std::shared_ptr<System_String> > >(result);
}


void System_Console__WriteLine(const wchar_t* value)
{
	wprintf(L"%ls\n", value);	
}
