#include <boost/function.hpp>

using namespace boost;

class SelfCleanVector
{
public:
    SelfCleanVector()
    {
    }

    void Insert(const char* p_str, const boost::function<void(const char*)>& p_cleanupCallback);
};